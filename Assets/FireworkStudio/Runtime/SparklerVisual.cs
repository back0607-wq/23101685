using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireworkStudio
{
    /// <summary>
    /// Independent sparkler effects. Does not access rocket materials, the camera or global volumes.
    /// Explicit Configure/Ignite avoid AddComponent/Awake and Start-order races.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class SparklerVisual : MonoBehaviour
    {
        public Transform tip;
        public bool autoPlay;
        [Tooltip("Legacy local light level; unrelated to emissive particle brightness.")]
        public float sparkleIntensity = 1.6f;
        [SerializeField] SparklerOptions options = new SparklerOptions();

        SparklerBody body;
        GameObject fx;
        ParticleSystem sparks, glints, embers, smoke, core;
        TrailRenderer writingCore, writingHalo;
        Light flameLight;
        AudioSource fizz;
        AudioClip ignitionClip;
        readonly List<Material> materials = new List<Material>();
        System.Random rng;
        bool built, burning, haveMotionSample, requestedAutoPlay;
        float startTime, burnDuration=24f, nextGlint, nextCore, nextTint, visualEnvelope;
        Vector3 lastMotionPoint, referenceLocal;
        int suppressWritingFrames;
        Color currentTint;

        public bool IsBurning { get { return burning; } }
        public Transform EffectAnchor { get { return tip; } }
        public int LiveParticleCount { get { return built ? sparks.particleCount+glints.particleCount+embers.particleCount+smoke.particleCount+core.particleCount : 0; } }

        public void Configure(Transform assignedTip, SparklerOptions settings)
        {
            if(built) return; // An active effect is never rebuilt while it is playing.
            options=(settings ?? new SparklerOptions()).CopyClamped();
            body=GetComponent<SparklerBody>();
            tip=ResolveTip(assignedTip);
            requestedAutoPlay=autoPlay;
        }
        void Start()
        {
            // FireworkProp calls Configure before Ignite. Standalone use also works.
            if(options==null) options=new SparklerOptions();
            if(tip==null || tip==transform) tip=ResolveTip(tip);
            if(autoPlay || requestedAutoPlay) Ignite(burnDuration);
        }
        Transform ResolveTip(Transform candidate)
        {
            if(body==null) body=GetComponent<SparklerBody>();
            if(body!=null && body.flameAnchor!=null) return body.flameAnchor;
            if(candidate!=null && candidate!=transform && !transform.IsChildOf(candidate)) return candidate;
            Transform named=transform.Find("Visual/Tip");
            if(named!=null) return named;
            // Legacy prop fallback: compute the local mesh's top, then transform it once.
            // Renderer.bounds is a rotated world AABB and is deliberately not used here.
            var shaft=transform.Find("Visual/Shaft");
            var mesh=shaft!=null ? shaft.GetComponent<MeshFilter>() : null;
            Vector3 world=mesh!=null && mesh.sharedMesh!=null
                ? shaft.TransformPoint(mesh.sharedMesh.bounds.center+Vector3.up*mesh.sharedMesh.bounds.extents.y)
                : transform.TransformPoint(Vector3.up*.205f);
            var anchor=new GameObject("Sparkler Tip (resolved)");
            anchor.transform.SetParent(transform,false);
            anchor.transform.position=world;
            anchor.transform.rotation=transform.rotation;
            return anchor.transform;
        }
        Material Glow(string name,float shape,float intensity)
        {
            // This shader asset is inside Resources so it is also referenced in player builds.
            Shader shader=Resources.Load<Shader>("FireworkSparkler/SparklerGlow");
            if(shader==null) shader=Shader.Find("FireworkStudio/SparklerGlow");
            if(shader==null) throw new InvalidOperationException("FireworkStudio/SparklerGlow is missing. Install the full firework.zip package.");
            var m=new Material(shader) { name=name, hideFlags=HideFlags.DontSave };
            m.SetFloat("_Shape",shape); m.SetFloat("_Intensity",intensity*options.brightness);
            materials.Add(m); return m;
        }
        ParticleSystem Layer(string name,Material mat,float rate,Vector2 life,Vector2 speed,Vector2 size,float gravity,int cap)
        {
            var go=new GameObject(name); go.transform.SetParent(fx.transform,false);
            // Configure while inactive AND stopped. No default PlayOnAwake emission at the origin.
            go.SetActive(false);
            var ps=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=ps.main;
            m.playOnAwake=false; m.loop=true; m.duration=1f;
            m.simulationSpace=ParticleSystemSimulationSpace.World;
            m.scalingMode=ParticleSystemScalingMode.Shape;
            m.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            m.startLifetime=new ParticleSystem.MinMaxCurve(life.x,life.y);
            m.startSpeed=new ParticleSystem.MinMaxCurve(speed.x,speed.y);
            m.startSize=new ParticleSystem.MinMaxCurve(size.x,size.y);
            m.startColor=Color.white; m.gravityModifier=gravity; m.maxParticles=cap;
            m.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2f);
            var e=ps.emission; e.enabled=rate>0; e.rateOverTime=rate*options.density;
            var s=ps.shape; s.enabled=true; s.shapeType=ParticleSystemShapeType.Sphere; s.radius=.0018f;
            var r=go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial=mat; r.renderMode=ParticleSystemRenderMode.Billboard;
            r.alignment=ParticleSystemRenderSpace.View; r.sortMode=ParticleSystemSortMode.None;
            r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=false;
            r.lightProbeUsage=LightProbeUsage.Off; r.reflectionProbeUsage=ReflectionProbeUsage.Off;
            r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
            go.SetActive(true);
            return ps;
        }
        bool EnsureBuilt()
        {
            if(built) return true;
            if(!Application.isPlaying || !isActiveAndEnabled) return false;
            options=(options ?? new SparklerOptions()).CopyClamped();
            tip=ResolveTip(tip);
            rng=new System.Random(GetInstanceID() ^ 196613);
            fx=new GameObject("Sparkler FX - tip anchored"); fx.transform.SetParent(tip,false);
            try
            {
                var point=Glow("Sparkler - hot sparks",0,6.2f);
                var streak=Glow("Sparkler - short spark trails",1,4.3f);
                var glitter=Glow("Sparkler - irregular glints",2,7.2f);
                sparks=Layer("Hot gold sparks",point,185f,new Vector2(.14f,.38f),new Vector2(.65f,2.35f),new Vector2(.011f,.023f)*options.sparkSize,.48f,700);
                var trails=sparks.trails;
                trails.enabled=true; trails.mode=ParticleSystemTrailMode.PerParticle;
                trails.ratio=.62f;
                // Unity interprets this as a fraction of particle lifetime, not seconds.
                trails.lifetime=.26f; trails.worldSpace=true; trails.dieWithParticles=true;
                trails.minVertexDistance=.008f; trails.sizeAffectsWidth=true; trails.sizeAffectsLifetime=false;
                trails.inheritParticleColor=true; trails.widthOverTrail=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0,.36f,1,0));
                sparks.GetComponent<ParticleSystemRenderer>().trailMaterial=streak;
                glints=Layer("Micro flashes",glitter,0,new Vector2(.12f,.25f),Vector2.zero,new Vector2(.017f,.031f)*options.sparkSize,.22f,200);
                embers=Layer("Amber embers",Glow("Sparkler - embers",0,2.8f),19f,new Vector2(.26f,.64f),new Vector2(.20f,.8f),new Vector2(.007f,.013f)*options.sparkSize,.70f,180);
                core=Layer("Burning tip",Glow("Sparkler - ember core",0,7.0f),0,new Vector2(.10f,.14f),Vector2.zero,new Vector2(.029f,.037f),0f,6);
                var cm=core.main; cm.simulationSpace=ParticleSystemSimulationSpace.Local;
                var cs=core.shape; cs.enabled=false;
                BuildSmoke();
                writingCore=WritingTrail("Light writing core",Glow("Sparkler - writing core",1,5.4f),options.writingWidth);
                writingHalo=WritingTrail("Light writing halo",Glow("Sparkler - writing halo",1,1.05f),options.writingWidth*3.1f);
                var lightGO=new GameObject("Local ember light"); lightGO.transform.SetParent(fx.transform,false);
                flameLight=lightGO.AddComponent<Light>(); flameLight.type=LightType.Point;
                flameLight.range=1.35f; flameLight.shadows=LightShadows.None; flameLight.intensity=0;
                flameLight.enabled=options.localLight;
                var audioGO=new GameObject("Quiet spatial crackle"); audioGO.transform.SetParent(fx.transform,false);
                fizz=audioGO.AddComponent<AudioSource>(); fizz.playOnAwake=false; fizz.loop=true;
                fizz.spatialBlend=1f; fizz.dopplerLevel=0f; fizz.rolloffMode=AudioRolloffMode.Linear;
                fizz.minDistance=.25f; fizz.maxDistance=4f; fizz.volume=0;
                fizz.clip=Resources.Load<AudioClip>("FireworkSparkler/CrackleLoop");
                ignitionClip=Resources.Load<AudioClip>("FireworkSparkler/Ignite");
                built=true;
                currentTint=options.BaseTint(); UpdateColors(currentTint);
                referenceLocal=transform.InverseTransformPoint(tip.position);
                ResetMotionHistory();
                return true;
            }
            catch(Exception ex)
            {
                Debug.LogError("[Firework Sparkler] " + ex.Message,this);
                if(fx!=null) Destroy(fx);
                foreach(var m in materials) if(m!=null) Destroy(m);
                materials.Clear(); enabled=false; return false;
            }
        }
        void BuildSmoke()
        {
            Shader s=Resources.Load<Shader>("FireworkSparkler/SparklerSmoke");
            if(s==null) throw new InvalidOperationException("SparklerSmoke shader is missing.");
            var m=new Material(s) { name="Sparkler - fine smoke",hideFlags=HideFlags.DontSave };
            m.SetTexture("_MainTex",Resources.Load<Texture2D>("FireworkSparkler/SmokeMask"));
            materials.Add(m);
            smoke=Layer("Fine smoke",m,7f*options.smokeAmount,new Vector2(.7f,1.15f),new Vector2(.03f,.09f),new Vector2(.048f,.085f),0,40);
            var mm=smoke.main; mm.startColor=new Color(.60f,.62f,.66f,.16f);
            var v=smoke.velocityOverLifetime; v.enabled=true; v.space=ParticleSystemSimulationSpace.World; v.y=.12f; v.x=.035f;
            var z=smoke.sizeOverLifetime; z.enabled=true; z.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0,.5f,1,2.8f));
            var c=smoke.colorOverLifetime; c.enabled=true;
            c.color=Grad(Color.white,Color.white,new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.18f),new GradientAlphaKey(0,1)});
        }
        TrailRenderer WritingTrail(string name,Material material,float width)
        {
            var go=new GameObject(name); go.transform.SetParent(fx.transform,false);
            var t=go.AddComponent<TrailRenderer>(); t.sharedMaterial=material;
            t.time=options.afterglowSeconds; t.minVertexDistance=.004f;
            t.startWidth=width; t.endWidth=width*.08f;
            t.widthCurve=new AnimationCurve(new Keyframe(0,1f),new Keyframe(.45f,.70f),new Keyframe(1,0f));
            t.widthMultiplier=width; t.numCapVertices=3; t.numCornerVertices=2;
            t.alignment=LineAlignment.View; t.textureMode=LineTextureMode.Stretch;
            t.shadowCastingMode=ShadowCastingMode.Off; t.receiveShadows=false;
            t.lightProbeUsage=LightProbeUsage.Off; t.emitting=false; t.Clear();
            return t;
        }
        static Gradient Grad(Color a,Color b,GradientAlphaKey[] alphas)
        {
            var g=new Gradient();
            g.SetKeys(new[]{new GradientColorKey(a,0),new GradientColorKey(b,1)},alphas);
            return g;
        }
        void UpdateColors(Color color)
        {
            Color hot=Color.Lerp(color,Color.white,.30f);
            Color end=options.tint==SparklerTint.WarmGold ? new Color(1f,.22f,.035f) : color*.55f;
            var c=sparks.colorOverLifetime; c.enabled=true;
            var g=new Gradient(); g.SetKeys(new[]{new GradientColorKey(hot,0),new GradientColorKey(color,.30f),new GradientColorKey(end,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.86f,.48f),new GradientAlphaKey(0,1)}); c.color=g;
            var e=embers.colorOverLifetime; e.enabled=true;
            e.color=Grad(color,end,new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(.4f,.55f),new GradientAlphaKey(0,1)});
            var gc=glints.colorOverLifetime; gc.enabled=true;
            gc.color=Grad(Color.white,color,new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.25f,.30f),new GradientAlphaKey(1,.48f),new GradientAlphaKey(0,1)});
            var cc=core.colorOverLifetime; cc.enabled=true;
            cc.color=Grad(hot,color,new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(.65f,.55f),new GradientAlphaKey(0,1)});
            var trailColor=Grad(hot,color,new[]{new GradientAlphaKey(.85f,0),new GradientAlphaKey(.48f,.4f),new GradientAlphaKey(0,1)});
            writingCore.colorGradient=trailColor; writingHalo.colorGradient=trailColor;
            flameLight.color=Color.Lerp(color,new Color(1f,.75f,.46f),.3f);
        }
        public bool Ignite(float seconds)
        {
            if(burning) return false;
            if(!EnsureBuilt()) return false;
            // Relighting starts a fresh virtual coating. Existing airborne sparks are cleared.
            body=GetComponent<SparklerBody>(); if(body!=null) body.SetProgress(0f);
            referenceLocal=transform.InverseTransformPoint(tip.position);
            burnDuration=SparklerOptions.Safe(seconds,24f,1f,180f);
            startTime=Time.time; nextGlint=startTime; nextCore=startTime; nextTint=startTime;
            visualEnvelope=0; burning=true;
            sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            glints.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            core.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            sparks.Play(); glints.Play(); embers.Play(); smoke.Play(); core.Play();
            ResetMotionHistory();
            if(options.audio && fizz!=null)
            {
                fizz.volume=options.audioVolume;
                if(fizz.clip!=null) fizz.Play();
                if(ignitionClip!=null) fizz.PlayOneShot(ignitionClip,.6f);
            }
            return true;
        }
        // Kept for scenes/scripts made by earlier versions.
        public void SetActive(bool active)
        { if(active) Ignite(burnDuration); else Extinguish(false); }

        public void Extinguish(bool clear)
        {
            burning=false;
            if(!built) return;
            var stop=clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
            sparks.Stop(true,stop); glints.Stop(true,stop); embers.Stop(true,stop); smoke.Stop(true,stop); core.Stop(true,stop);
            writingCore.emitting=false; writingHalo.emitting=false;
            if(clear) { ResetMotionHistory(); flameLight.intensity=0; if(fizz!=null) fizz.Stop(); }
        }
        public void ResetMotionHistory()
        {
            haveMotionSample=false; suppressWritingFrames=2;
            if(writingCore!=null) { writingCore.emitting=false; writingCore.Clear(); }
            if(writingHalo!=null) { writingHalo.emitting=false; writingHalo.Clear(); }
        }
        void LateUpdate()
        {
            if(!built || tip==null) return;
            float dt=Time.deltaTime;
            if(dt<=0) return;
            if(burning)
            {
                float elapsed=Time.time-startTime;
                float progress=Mathf.Clamp01(elapsed/burnDuration);
                if(body!=null && options.burnDown) body.SetProgress(progress);
                float edge=Mathf.SmoothStep(0,1,elapsed/.16f) * (1-Mathf.SmoothStep(0,1,(progress-.94f)/.06f));
                visualEnvelope=edge;
                float jitter=.78f+.22f*Mathf.PerlinNoise(startTime*.17f,Time.time*21f);
                var em=sparks.emission; em.rateOverTime=185f*options.density*edge*jitter;
                if(Time.time>=nextTint && options.tint==SparklerTint.Aurora)
                { currentTint=options.BaseTint(elapsed); UpdateColors(currentTint); nextTint=Time.time+.14f; }
                if(Time.time>=nextGlint && edge>.18f)
                { EmitGlints(); nextGlint=Time.time+Range(.055f,.145f); }
                if(Time.time>=nextCore && edge>.01f)
                {
                    var ep=new ParticleSystem.EmitParams();
                    ep.position=Vector3.zero; // LOCAL only: tip core follows the flame anchor.
                    ep.velocity=Vector3.zero; ep.startLifetime=.12f;
                    ep.startSize=Range(.027f,.036f)*Mathf.Lerp(.6f,1f,edge);
                    ep.startColor=Color.Lerp(currentTint,Color.white,.35f); core.Emit(ep,1);
                    nextCore=Time.time+.065f;
                }
                if(progress>=1f) Extinguish(false);
            }
            else visualEnvelope=Mathf.MoveTowards(visualEnvelope,0f,dt*5f);
            float pulse=.76f+.24f*Mathf.PerlinNoise(GetInstanceID()*.01f,Time.time*16f);
            if(flameLight!=null) flameLight.intensity=options.localLight ? Mathf.Clamp(sparkleIntensity,0,2.5f)*visualEnvelope*pulse : 0;
            if(fizz!=null)
            {
                fizz.volume=options.audio ? options.audioVolume*visualEnvelope : 0;
                if(!burning && visualEnvelope<=0 && fizz.isPlaying) fizz.Stop();
            }
            UpdateWriting(dt);
        }
        void UpdateWriting(float dt)
        {
            // Track the prop's original tip point, not slow burn-down of the coating.
            Vector3 motion=transform.TransformPoint(referenceLocal);
            float delta=haveMotionSample ? Vector3.Distance(motion,lastMotionPoint) : 0;
            bool discontinuity=haveMotionSample && (delta>options.teleportDistance || dt>.12f);
            if(discontinuity) ResetMotionHistory();
            bool draw=burning && options.lightWriting && haveMotionSample && !discontinuity && delta/dt>.045f && suppressWritingFrames<=0;
            writingCore.emitting=draw; writingHalo.emitting=draw;
            lastMotionPoint=motion; haveMotionSample=true;
            if(suppressWritingFrames>0) suppressWritingFrames--;
        }
        void EmitGlints()
        {
            int count=Mathf.Clamp(Mathf.RoundToInt(Range(3f,7f)*options.density),1,13);
            for(int i=0;i<count;i++)
            {
                var ep=new ParticleSystem.EmitParams();
                Vector3 direction=RandomDirection();
                ep.position=tip.position+direction*Range(.003f,.018f); // WORLD space, including XR Origin offsets.
                ep.velocity=direction*Range(.25f,1.15f);
                ep.startLifetime=Range(.11f,.24f);
                ep.startSize=Range(.017f,.033f)*options.sparkSize;
                ep.startColor=Color.Lerp(currentTint,Color.white,Range(.12f,.42f));
                ep.rotation=Range(0,360f);
                glints.Emit(ep,1);
            }
        }
        float Range(float min,float max) { return min+(max-min)*(float)rng.NextDouble(); }
        Vector3 RandomDirection()
        {
            float y=Range(-1f,1f),a=Range(0,Mathf.PI*2);
            float r=Mathf.Sqrt(Mathf.Max(0,1-y*y));
            return new Vector3(r*Mathf.Cos(a),y,r*Mathf.Sin(a));
        }
        void OnDisable() { Extinguish(true); }
        void OnDestroy()
        {
            foreach(var m in materials) if(m!=null) Destroy(m);
            materials.Clear();
            if(fx!=null) Destroy(fx);
        }
    }
}
