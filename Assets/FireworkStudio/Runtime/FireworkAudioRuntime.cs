using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireworkStudio
{
    // Runtime-owned voice pool. Audio tails outlive visual/projectile objects.
    // Every non-flight source stays at its emitted WORLD position.
    [DisallowMultipleComponent]
    public sealed class FireworkAudioRuntime : MonoBehaviour
    {
        public const string SettingsPath = "FireworkAudio/FireworkAudioSettings";
        public FireworkAudioSettings settings;
        sealed class Voice
        {
            public AudioSource source;
            public AudioLowPassFilter lowPass;
            public bool busy, flight;
            public Transform follow;
            public FireworkSoundKind kind;
            public double start, end, releaseAt;
            public float energy, variation;
        }
        sealed class Bank
        {
            public AudioClip[] clips;
            public int last = -1;
        }
        readonly List<Voice> voices = new List<Voice>(32);
        readonly Dictionary<FireworkSoundKind, Bank> banks = new Dictionary<FireworkSoundKind, Bank>();
        // A separate RNG is essential: audio must not change existing VFX random sequences.
        readonly System.Random rng = new System.Random(unchecked(Environment.TickCount ^ 0x175E71));
        AudioListener listener;
        double nextListenerSearch;
        float overlapGain = 1f;
        bool spatializerAvailable, ownsSettings;
        public int ActiveVoiceCount { get { int n=0; foreach(var v in voices) if(v.busy) n++; return n; } }
        public int DroppedVoiceRequests { get; private set; }

        void Awake()
        {
            if (settings == null) settings = Resources.Load<FireworkAudioSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<FireworkAudioSettings>(); ownsSettings = true; }
            spatializerAvailable = !string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName());
            LoadBank(FireworkSoundKind.Launch, "Launch", 4);
            LoadBank(FireworkSoundKind.Flight, "Flight", 2);
            LoadBank(FireworkSoundKind.Burst, "Burst", 4);
            LoadBank(FireworkSoundKind.Bloom, "Bloom", 3);
            LoadBank(FireworkSoundKind.Pop, "Pop", 8);
            ResolveListener();
            // Small initial pool; grows only to the configured hard limit.
            for (int i=0; i<8; i++) CreateVoice();
        }
        void LoadBank(FireworkSoundKind kind, string prefix, int count)
        {
            var found = new List<AudioClip>();
            for(int i=1;i<=count;i++)
            {
                string path = "FireworkAudio/" + prefix + "_" + i.ToString("00");
                var clip = Resources.Load<AudioClip>(path);
                if(clip == null) { Debug.LogWarning("[Firework Audio] Missing clip: " + path); continue; }
                if(clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                found.Add(clip);
            }
            banks[kind] = new Bank { clips = found.ToArray() };
        }
        Voice CreateVoice()
        {
            var go = new GameObject("Sky sound " + voices.Count);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false; a.loop = false;
            a.spatialBlend = 1f; a.dopplerLevel = 0f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.volume = 0f; a.spread = 0f;
            a.reverbZoneMix = .35f;
            a.ignoreListenerPause = false;
            a.ignoreListenerVolume = false;
            var lp = go.AddComponent<AudioLowPassFilter>();
            lp.lowpassResonanceQ = 1f; lp.cutoffFrequency = 18000f;
            var voice = new Voice { source = a, lowPass = lp };
            voices.Add(voice);
            return voice;
        }
        Voice Reserve(FireworkSoundKind kind)
        {
            int same=0;
            foreach(var v in voices) if(v.busy && v.kind == kind) same++;
            int cap = kind == FireworkSoundKind.Pop ? Mathf.Clamp(settings.maxCrackleVoices,6,32) :
                kind == FireworkSoundKind.Flight ? Mathf.Clamp(settings.maxFlightVoices,1,8) : 8;
            if(same >= cap) { DroppedVoiceRequests++; return null; }
            int max = Mathf.Clamp(settings.maxVoices,12,64);
            // Reserve four active slots for launch / major reports during dense crackles.
            int allowed = kind == FireworkSoundKind.Pop || kind == FireworkSoundKind.Flight ? max-4 : max;
            if(ActiveVoiceCount >= allowed) { DroppedVoiceRequests++; return null; }
            foreach(var v in voices) if(!v.busy) return v;
            if(voices.Count < max) return CreateVoice();
            DroppedVoiceRequests++;
            return null; // Never abruptly cut an existing report just to fit another crackle.
        }
        AudioClip Choose(FireworkSoundKind kind)
        {
            Bank b;
            if(!banks.TryGetValue(kind,out b) || b.clips.Length == 0) return null;
            int i = rng.Next(b.clips.Length);
            if(b.clips.Length > 1 && i == b.last) i = (i + 1 + rng.Next(b.clips.Length-1)) % b.clips.Length;
            b.last = i;
            return b.clips[i];
        }
        float Range(float a,float b) { return Mathf.Lerp(a,b,(float)rng.NextDouble()); }
        bool Enabled(FireworkSoundKind kind)
        {
            if(settings == null || !settings.audioEnabled || settings.masterVolume <= 0f) return false;
            switch(kind)
            {
                case FireworkSoundKind.Launch: return settings.launchEnabled;
                case FireworkSoundKind.Flight: return settings.flightEnabled;
                case FireworkSoundKind.Burst: return settings.burstEnabled;
                case FireworkSoundKind.Bloom: return settings.afterburstEnabled;
                default: return settings.crackleEnabled;
            }
        }
        float LayerVolume(FireworkSoundKind kind)
        {
            switch(kind)
            {
                case FireworkSoundKind.Launch: return FireworkAudioSettings.Safe(settings.launchVolume,.40f,0,1);
                case FireworkSoundKind.Flight: return FireworkAudioSettings.Safe(settings.flightVolume,.18f,0,1);
                case FireworkSoundKind.Burst: return FireworkAudioSettings.Safe(settings.burstVolume,.68f,0,1);
                case FireworkSoundKind.Bloom: return FireworkAudioSettings.Safe(settings.afterburstVolume,.26f,0,1);
                default: return FireworkAudioSettings.Safe(settings.crackleVolume,.42f,0,1);
            }
        }
        void Configure(Voice v, FireworkSoundKind kind, AudioClip clip, Vector3 worldPosition, float energy)
        {
            v.kind=kind; v.busy=true; v.flight=false; v.follow=null; v.releaseAt=-1;
            v.energy=FireworkAudioSettings.Safe(energy,1f,.1f,1.3f); v.variation=Range(.90f,1f);
            var a=v.source;
            a.Stop(); a.transform.position=worldPosition; a.clip=clip;
            a.outputAudioMixerGroup=settings.outputMixer;
            a.loop=false;
            a.spatialize=settings.useProjectSpatializer && spatializerAvailable;
            a.pitch=kind==FireworkSoundKind.Pop?Range(.88f,1.15f):Range(.94f,1.06f);
            a.priority=kind==FireworkSoundKind.Burst?80:kind==FireworkSoundKind.Launch?110:kind==FireworkSoundKind.Flight?210:160;
            float far=FireworkAudioSettings.Safe(settings.skyAudibleDistance,165f,80f,300f);
            a.minDistance=kind==FireworkSoundKind.Launch || kind==FireworkSoundKind.Flight ? 1f : 8f;
            a.maxDistance=kind==FireworkSoundKind.Launch?24f:kind==FireworkSoundKind.Flight?80f:far;
            a.spread=kind==FireworkSoundKind.Burst?25f:kind==FireworkSoundKind.Bloom?15f:0f;
            v.lowPass.enabled=settings.distanceFiltering;
            v.lowPass.cutoffFrequency=Cutoff(worldPosition);
            a.volume=Gain(v);
        }
        internal void OneShot(FireworkSoundKind kind, Vector3 worldPosition, float energy)
        {
            if(!Enabled(kind) || !Finite(worldPosition)) return;
            float distance=Distance(worldPosition);
            float far=kind==FireworkSoundKind.Launch?24f:FireworkAudioSettings.Safe(settings.skyAudibleDistance,165f,80f,300f);
            if(distance>=far) return;
            var clip=Choose(kind); if(clip==null) return;
            var v=Reserve(kind); if(v==null) return;
            Configure(v,kind,clip,worldPosition,energy);
            double delay=kind==FireworkSoundKind.Launch || !settings.propagationDelay?0:
                Math.Min(1.0,distance/FireworkAudioSettings.Safe(settings.soundSpeed,343f,100f,600f));
            double now=AudioSettings.dspTime;
            if(delay>=.035)
            {
                v.start=now+delay;
                v.source.PlayScheduled(v.start);
            }
            else
            {
                v.start=now;
                v.source.Play(); // Preloaded hand ignition: no unnecessary schedule/input latency.
            }
            v.end=v.start+clip.length/v.source.pitch+.05;
        }
        internal void Launch(Transform projectile,float flightSeconds)
        {
            if(projectile==null) return;
            OneShot(FireworkSoundKind.Launch,projectile.position,1f);
            if(!Enabled(FireworkSoundKind.Flight)) return;
            var clip=Choose(FireworkSoundKind.Flight); if(clip==null) return;
            var v=Reserve(FireworkSoundKind.Flight); if(v==null) return;
            Configure(v,FireworkSoundKind.Flight,clip,projectile.position,1f);
            v.flight=true; v.follow=projectile;
            v.start=AudioSettings.dspTime;
            v.end=v.start+FireworkAudioSettings.Safe(flightSeconds,2f,.2f,120f);
            v.source.volume=0; v.source.loop=true; v.source.Play();
        }
        float Gain(Voice v)
        {
            float master=FireworkAudioSettings.Safe(settings.masterVolume,.55f,0,1);
            return Mathf.Clamp01(master*LayerVolume(v.kind)*v.energy*v.variation*overlapGain);
        }
        void Update()
        {
            double now=AudioSettings.dspTime;
            if(settings==null) return;
            if(now>=nextListenerSearch) { ResolveListener(); nextListenerSearch=now+1.0; }
            int audible=0;
            foreach(var v in voices) if(v.busy && now>=v.start && now<v.end) audible++;
            float target=settings.reduceOverlap?1f/Mathf.Sqrt(1f+Mathf.Max(0,audible-3)*.12f):1f;
            overlapGain=Mathf.MoveTowards(overlapGain,target,Time.unscaledDeltaTime*5f);
            foreach(var v in voices)
            {
                if(!v.busy) continue;
                if(!Enabled(v.kind)) { Release(v); continue; }
                if(v.flight)
                {
                    if(v.follow!=null) v.source.transform.position=v.follow.position;
                    if(v.releaseAt<0 && (v.follow==null || now>=v.end)) v.releaseAt=now;
                    float envelope=Mathf.Clamp01((float)(now-v.start)/.12f);
                    if(v.releaseAt>=0)
                    {
                        envelope*=1f-Mathf.Clamp01((float)(now-v.releaseAt)/.16f);
                        if(now>=v.releaseAt+.16) { Release(v); continue; }
                    }
                    v.source.volume=Gain(v)*envelope;
                }
                else
                {
                    if(now>=v.end) { Release(v); continue; }
                    v.source.volume=Gain(v);
                }
                v.lowPass.enabled=settings.distanceFiltering;
                if(v.lowPass.enabled) v.lowPass.cutoffFrequency=Cutoff(v.source.transform.position);
            }
        }
        static bool Finite(Vector3 p)
        {
            return !float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&
                !float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
        }
        void ResolveListener()
        {
            if(listener!=null && listener.isActiveAndEnabled) return;
            listener=null;
            var all=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach(var a in all) if(a.isActiveAndEnabled) { listener=a; break; }
        }
        float Distance(Vector3 position)
        {
            if(listener==null || !listener.isActiveAndEnabled) ResolveListener();
            // Camera is only a fallback measurement, NEVER the sound-source location.
            var t=listener!=null?listener.transform:Camera.main!=null?Camera.main.transform:null;
            return t!=null?Vector3.Distance(position,t.position):0f;
        }
        float Cutoff(Vector3 position)
        { return Mathf.Lerp(18000f,5800f,Mathf.Clamp01(Distance(position)/120f)); }
        void Release(Voice v)
        {
            v.source.Stop(); v.source.clip=null; v.source.loop=false; v.source.volume=0;
            v.busy=false; v.follow=null; v.flight=false;
        }
        void OnDisable() { foreach(var v in voices) if(v.busy) Release(v); }
        void OnApplicationQuit() { FireworkAudio.Shutdown(this,true); }
        void OnDestroy()
        {
            FireworkAudio.Shutdown(this,false);
            if(ownsSettings && settings!=null) Destroy(settings);
        }
    }
}
