using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FireworkStudio;

namespace FireworkStudio.Secondary
{
    public enum AfterburstStyle
    {
        Random, StarSphere, TwinRings, FivePetalFlower, GoldenWillow,
        CrystalSnow, RainbowCascade, CrackleConstellation, HeartHalo
    }

    public sealed class AfterburstSequence : MonoBehaviour
    {
        Vector3 origin;
        FireworkPalette palette;
        AfterburstSettings settings;
        Material material;
        Material crackleMaterial;
        Texture2D texture;
        bool reserved;
        FireworkPattern? sourcePattern;
        Quaternion orientation = Quaternion.identity;
        Color? sourceTint;
        GameObject effectRoot;
        ParticleSystem terminalRibbons;
        int terminalRibbonCount;
        struct StreakEndpoint
        {
            public Vector3 position;
            public float arrivalTime;
        }
        readonly System.Collections.Generic.List<StreakEndpoint> streakEnds = new System.Collections.Generic.List<StreakEndpoint>();

        public static void Attach(Vector3 origin, FireworkPalette palette,
            FireworkPattern? sourcePattern = null, Quaternion? orientation = null, Color? sourceTint = null,
            ParticleSystem terminalRibbons = null, int terminalRibbonCount = 0)
        {
            var settings = AfterburstSettings.GetOrCreate();
            if (!settings.enabledAfterburst) { Debug.LogWarning("[Firework Crackle] Afterburst disabled in Settings."); return; }
            if (!settings.TryReserve()) { Debug.LogWarning("[Firework Crackle] Simultaneous afterburst limit reached."); return; }
            // Independent lifetime: deleting the main burst cannot cut off late crackles.
            var host = new GameObject("Firework - Combined Afterburst");
            host.transform.position = origin;
            var sequence = host.AddComponent<AfterburstSequence>();
            sequence.reserved = true;
            sequence.origin = origin;
            sequence.palette = palette;
            sequence.settings = settings;
            sequence.sourcePattern = sourcePattern;
            sequence.orientation = orientation ?? Quaternion.identity;
            sequence.sourceTint = sourceTint;
            sequence.terminalRibbons = terminalRibbons;
            sequence.terminalRibbonCount = terminalRibbonCount;
            sequence.StartCoroutine(sequence.Play());
        }

        IEnumerator Play()
        {
            yield return new WaitForSeconds(settings.startDelay);
            if (!settings || !settings.enabledAfterburst) { ReleaseSlot(); Destroy(gameObject); yield break; }
            var shader = Shader.Find("FireworkStudio.Secondary/SoftAdditiveGlint");
            if (!shader) { Debug.LogError("[Firework] Missing SoftAdditiveGlint shader."); ReleaseSlot(); Destroy(gameObject); yield break; }
            texture = MakeSoftTexture();
            material = new Material(shader) { name = "Afterburst VFX (runtime)" };
            material.SetTexture("_MainTex",texture);
            material.SetFloat("_Intensity",settings.brightness * 2.2f);
            // Independent glow so the main explosion remains untouched.
            crackleMaterial = new Material(material) { name = "Terminal Crackle Glow (runtime)" };
            crackleMaterial.SetFloat("_Intensity", settings.terminalGlowIntensity);
            effectRoot = new GameObject("Afterburst - " + settings.style);
            effectRoot.transform.position = origin;
            var layerA = Layer(effectRoot.transform,"Afterburst - colored starlets",0.16f,0.05f);
            var layerB = Layer(effectRoot.transform,"Afterburst - flash points",0.05f,0.20f);
            var layerC = Layer(effectRoot.transform,"Afterburst - falling embers",0.28f,0.48f);
            var crackle = Layer(effectRoot.transform,"Terminal crackle - rapid microbursts",0.10f,0.17f, crackleMaterial);
            var flashes = Layer(effectRoot.transform,"Terminal crackle - bright terminal pops",0.02f,0.32f, crackleMaterial);
            var chosen = sourcePattern.HasValue ? AfterburstStyle.StarSphere : ChooseStyle(settings.style);
            int budget = Mathf.Max(40, settings.maxParticlesPerBurst);
            Coroutine ribbonPops = null;
            if (settings.enableTerminalCrackle && terminalRibbons != null)
                ribbonPops = StartCoroutine(EmitRibbonCrackles(crackle, flashes, chosen));
            // This is relative to the afterburst start, not the rocket launch.
            EmitStyled(layerA, chosen, Mathf.RoundToInt(Mathf.Min(budget * .62f, 240) * settings.density),false, true);
            FireworkStudio.FireworkAudio.Afterburst(origin); // only when this layer actually emits
            yield return new WaitForSeconds(0.31f);
            EmitStyled(layerB, chosen, Mathf.RoundToInt(Mathf.Min(budget * .22f, 120) * settings.density),true);
            yield return new WaitForSeconds(0.42f);
            EmitStyled(layerC, chosen, Mathf.RoundToInt(Mathf.Min(budget * .16f, 90) * settings.density),true);
            if (ribbonPops != null)
                yield return ribbonPops;
            else if (settings.enableTerminalCrackle)
            {
                // Three layer timings above take 0.73s. Wait until the primary
                // streaks are nearly extinguished before terminal microbursts.
                yield return new WaitForSeconds(Mathf.Max(0f, settings.terminalFirstPopDelay - 0.73f));
                Debug.Log($"[Firework Crackle] Playing endpoint pops: endpoints={streakEnds.Count} clusters={settings.terminalClusters}");
                yield return StartCoroutine(EmitTerminalCrackle(crackle, flashes, chosen));
            }
            ReleaseSlot();
            Destroy(effectRoot, 5.5f);
            Destroy(material, 6f);
            Destroy(crackleMaterial, 6f);
            Destroy(texture, 6f);
            Destroy(gameObject, 6.1f);
        }

        IEnumerator EmitRibbonCrackles(ParticleSystem sparks, ParticleSystem flashes, AfterburstStyle style)
        {
            const float remainingLifeAtPop = 0.12f;
            var heads = new ParticleSystem.Particle[terminalRibbonCount];
            var popped = new HashSet<uint>();
            var colors = sourceTint.HasValue ? new[] { sourceTint.Value } : FireworkPaletteUtil.GetColors(palette);

            // Read the live head in world space. Each ribbon pops once, as its own light fades.
            while (terminalRibbons != null && terminalRibbons.IsAlive(false) && popped.Count < terminalRibbonCount)
            {
                int count = terminalRibbons.GetParticles(heads);
                for (int i = 0; i < count; i++)
                {
                    var head = heads[i];
                    if (head.remainingLifetime > head.startLifetime * remainingLifeAtPop || !popped.Add(head.randomSeed))
                        continue;

                    EmitCrackle(sparks, flashes, style, colors, head.position);
                }
                yield return null;
            }
            Debug.Log($"[Firework Crackle] Ribbon endpoint pops: {popped.Count}/{terminalRibbonCount}");
        }

        void ReleaseSlot()
        {
            if (!reserved) return;
            reserved = false;
            if (settings != null) settings.Release();
        }
        void OnDestroy()
        {
            ReleaseSlot();
            if (effectRoot != null) Destroy(effectRoot);
            if (material != null) Destroy(material);
            if (crackleMaterial != null) Destroy(crackleMaterial);
            if (texture != null) Destroy(texture);
        }

        IEnumerator EmitTerminalCrackle(ParticleSystem ps, ParticleSystem flashes, AfterburstStyle chosen)
        {
            // Pop near ACTUAL primary-streak end locations, not the shell center.
            // Start after the streaks dim. Vary cluster position/time/energy.
            int clusters = Mathf.Max(1, settings.terminalClusters);
            var colors = sourceTint.HasValue ? new[] { sourceTint.Value } : FireworkPaletteUtil.GetColors(palette);
            float spread = Mathf.Max(0.08f, settings.terminalSpreadDelay);
            float step = spread / clusters;
            if (streakEnds.Count == 0)
            {
                Debug.LogWarning("[Firework Crackle] No tracked endpoints; fallback to outer sphere.");
                for (int j = 0; j < clusters; j++)
                    streakEnds.Add(new StreakEndpoint { position = origin + UnityEngine.Random.onUnitSphere * (9f * settings.sizeMultiplier), arrivalTime = 1.5f });
            }

            for (int c = 0; c < clusters; c++)
            {
                var end = streakEnds[UnityEngine.Random.Range(0, streakEnds.Count)];
                Vector3 centre = end.position + UnityEngine.Random.insideUnitSphere *
                    Mathf.Lerp(0.18f, 1.1f, settings.terminalRandomness);
                EmitCrackle(ps, flashes, chosen, colors, centre);
                // Distant little pops are intentionally irregular, not a uniform ring.
                yield return new WaitForSeconds(step * UnityEngine.Random.Range(0.3f, 1.7f));
            }
        }

        void EmitCrackle(ParticleSystem sparks, ParticleSystem flashes, AfterburstStyle style, Color[] colors, Vector3 position)
        {
            Color tint = style == AfterburstStyle.GoldenWillow
                ? new Color(1f, 0.68f, 0.24f) : colors[UnityEngine.Random.Range(0, colors.Length)];
            int maximumSparks = Mathf.Max(2, settings.sparksPerCluster);
            int sparkCount = UnityEngine.Random.Range(Mathf.Max(2, maximumSparks / 2), maximumSparks + 1);

            var emission = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = Vector3.zero,
                startSize = UnityEngine.Random.Range(0.48f, 0.78f) * settings.sizeMultiplier * settings.terminalSizeMultiplier,
                startLifetime = UnityEngine.Random.Range(0.11f, 0.20f),
                startColor = Color.Lerp(tint, Color.white, 0.35f) * settings.terminalSparkBrightness
            };
            flashes.Emit(emission, 1);
            FireworkAudio.Crackle(position);
            FireworkWaterReflection.ReflectCrackle(position, tint);

            for (int i = 0; i < sparkCount; i++)
            {
                emission.velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1.8f, 5.5f);
                emission.startSize = UnityEngine.Random.Range(0.19f, 0.34f) * settings.sizeMultiplier * settings.terminalSizeMultiplier;
                emission.startLifetime = UnityEngine.Random.Range(0.24f, 0.56f);
                Color flash = Color.Lerp(tint, Color.white, UnityEngine.Random.Range(0.12f, 0.35f));
                emission.startColor = flash * settings.terminalSparkBrightness * 1.5f;
                sparks.Emit(emission, 1);
            }
        }

        static AfterburstStyle ChooseStyle(AfterburstStyle setting)
        {
            if (setting != AfterburstStyle.Random) return setting;
            return (AfterburstStyle)UnityEngine.Random.Range(1,Enum.GetValues(typeof(AfterburstStyle)).Length);
        }

        ParticleSystem Layer(Transform parent,string label,float gravity,float size, Material overrideMaterial = null)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent,false);
            var ps=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;
            main.playOnAwake=false; main.loop=false;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startSpeed=0f; main.startSize=size;
            main.startLifetime=2f; main.gravityModifier=gravity;
            main.maxParticles=900;
            var emission=ps.emission; emission.enabled=false;
            var shape=ps.shape; shape.enabled=false;
            var r=ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial=overrideMaterial != null ? overrideMaterial : material; r.renderMode=ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows=false;
            var col=ps.colorOverLifetime; col.enabled=true;
            var g=new Gradient();
            g.SetKeys(new [] { new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                      new [] { new GradientAlphaKey(0.15f,0),new GradientAlphaKey(1f,0.1f),new GradientAlphaKey(0.7f,0.55f),new GradientAlphaKey(0f,1f)});
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sol=ps.sizeOverLifetime; sol.enabled=true;
            sol.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.EaseInOut(0,0.45f,0.25f,1f));
            return ps;
        }

        void EmitStyled(ParticleSystem ps,AfterburstStyle style,int count,bool late,bool trackEndpoints=false)
        {
            var pal = sourceTint.HasValue ? new[] { sourceTint.Value } : FireworkPaletteUtil.GetColors(palette);
            var p=new ParticleSystem.EmitParams();
            count=Mathf.Max(1,count);
            for(int i=0;i<count;i++)
            {
                Vector3 direction = sourcePattern.HasValue
                    ? orientation * CrackleOnlyLayout.SampleVolume(sourcePattern.Value, i, count)
                    : Direction(style,i,count);
                if(style==AfterburstStyle.CrackleConstellation) direction=UnityEngine.Random.onUnitSphere;
                if (!sourcePattern.HasValue) direction=(direction+UnityEngine.Random.insideUnitSphere*.055f).normalized;
                float speed = sourcePattern.HasValue
                    ? UnityEngine.Random.Range(late ? 5.8f : 7.8f, late ? 6.2f : 8.2f) * settings.sizeMultiplier
                    : UnityEngine.Random.Range(late?2.3f:4.6f,late?5.2f:8.0f)*settings.sizeMultiplier;
                if(style==AfterburstStyle.GoldenWillow) speed*=.76f;
                if(style==AfterburstStyle.CrystalSnow) speed*=.8f;
                p.position=origin; // world-space, sky location
                p.velocity=direction*speed;
                p.startLifetime=UnityEngine.Random.Range(late?0.65f:1.35f,late?1.3f:2.4f);
                if (trackEndpoints && !sourcePattern.HasValue)
                {
                    // Unity particles: gravity = Physics.gravity * main.gravityModifier.
                    // Estimate positions at roughly 75-95% of their life.
                    float t = p.startLifetime * UnityEngine.Random.Range(0.75f, 0.95f);
                    Vector3 estimated = origin + p.velocity * t + 0.5f * Physics.gravity * 0.16f * t * t;
                    streakEnds.Add(new StreakEndpoint { position = estimated, arrivalTime = t });
                }
                p.startSize=UnityEngine.Random.Range(late?.10f:.12f,late?.20f:.27f)*settings.sizeMultiplier;
                Color c;
                if(style==AfterburstStyle.GoldenWillow) c=Color.Lerp(new Color(1f,.65f,.15f),Color.white,UnityEngine.Random.Range(0f,.34f));
                else if(style==AfterburstStyle.RainbowCascade) c=Color.HSVToRGB(i/(float)count,.72f,1f);
                else if(!settings.inheritMainPalette) c=Color.Lerp(settings.customTint,Color.white,UnityEngine.Random.Range(0f,.35f));
                else c=pal[UnityEngine.Random.Range(0,pal.Length)];
                p.startColor=new Color(c.r,c.g,c.b,late?.9f:1f);
                ps.Emit(p,1);
            }
            ps.Play();
        }

        static Vector3 Direction(AfterburstStyle style,int i,int n)
        {
            float a=i*Mathf.PI*2f/n;
            switch(style)
            {
                case AfterburstStyle.TwinRings:
                    return new Vector3(Mathf.Cos(a)*(i%2==0?1f:.63f), (i%2==0?.14f:-.14f), Mathf.Sin(a)).normalized;
                case AfterburstStyle.FivePetalFlower:
                    return new Vector3(Mathf.Cos(a)*(0.55f+0.45f*Mathf.Cos(5*a)),UnityEngine.Random.Range(-.15f,.30f),Mathf.Sin(a)*(0.55f+0.45f*Mathf.Cos(5*a))).normalized;
                case AfterburstStyle.GoldenWillow:
                    var willow=Fibonacci(i,n); willow.y=Mathf.Abs(willow.y)*.72f+.25f; return willow.normalized;
                case AfterburstStyle.CrystalSnow:
                    float k=Mathf.Floor(i/Mathf.Max(1f,n/6f)); a=k*Mathf.PI/3f; return new Vector3(Mathf.Cos(a),UnityEngine.Random.Range(-.1f,.1f),Mathf.Sin(a)).normalized;
                case AfterburstStyle.HeartHalo:
                    {float x=16f*Mathf.Pow(Mathf.Sin(a),3f)/18f; float y=(13f*Mathf.Cos(a)-5f*Mathf.Cos(2*a)-2f*Mathf.Cos(3*a)-Mathf.Cos(4*a))/18f; return new Vector3(x,y,0.22f*Mathf.Sin(a)).normalized;}
                case AfterburstStyle.StarSphere:
                case AfterburstStyle.RainbowCascade:
                case AfterburstStyle.CrackleConstellation:
                default: return Fibonacci(i,n);
            }
        }
        static Vector3 Fibonacci(int i,int n)
        {
            float y=1f-(i+.5f)*2f/n;
            float radius=Mathf.Sqrt(Mathf.Max(0f,1f-y*y));
            float angle=i*2.39996323f;
            return new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
        }
        static Texture2D MakeSoftTexture()
        {
            const int side=64;
            var t=new Texture2D(side,side,TextureFormat.RGBA32,false,true);
            t.wrapMode=TextureWrapMode.Clamp; t.filterMode=FilterMode.Bilinear;
            var pix=new Color[side*side];
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)
            {
                float dx=(x+.5f-side*.5f)/(side*.5f),dy=(y+.5f-side*.5f)/(side*.5f);
                float d=dx*dx+dy*dy;
                pix[x+y*side]=new Color(1f,1f,1f,Mathf.Clamp01(.2f*Mathf.Exp(-d*5f)+.82f*Mathf.Exp(-d*23f)));
            }
            t.SetPixels(pix); t.Apply(false,true);
            return t;
        }
    }
}
