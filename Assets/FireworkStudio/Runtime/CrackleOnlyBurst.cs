using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireworkStudio
{
    // Independent emitter: does not invoke VideoBurstController or AfterburstSequence.
    // The SHAPE is a cloud of pop locations; no main streaks are emitted from its center.
    public sealed class CrackleOnlyBurst : MonoBehaviour
    {
        static int activeBursts;
        static float nextLimitWarning;
        bool ownsSlot;
        ParticleSystem sparks, flashes;
        Material glow;
        CrackleOnlyOptions options;
        List<CrackleOnlyLayout.Pop> plan;
        Color[] colors;
        Quaternion orientation;
        Vector3 origin;
        System.Random rng;
        int cursor, remainingBudget;
        float elapsed, finishAt;
        bool ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { activeBursts=0; nextLimitWarning=0f; }

        public static bool Spawn(Vector3 position, Quaternion rotation, FireworkPattern pattern,
            FireworkPalette palette, bool useCustomColor, Color customColor, CrackleOnlyOptions settings)
        {
            var o=(settings ?? new CrackleOnlyOptions()).CopyClamped();
            if (activeBursts>=o.maxSimultaneousBursts)
            {
                // Keep every trigger shot visible. Reduce density for rapid-fire overload;
                // never substitute another mode or silently omit this sky explosion.
                o.clusters=24;
                o.sparksPerPop=4;
                o.maxParticlesPerShot=256;
                o.repeatProbability=.1f;
                if (Time.unscaledTime>=nextLimitWarning)
                { Debug.Log("[Firework] Rapid fire: extra crackle-only bursts use the reduced-density budget."); nextLimitWarning=Time.unscaledTime+10f; }
            }
            Shader shader=Shader.Find("FireworkStudio/CrackleOnlyGlow");
            if (shader==null)
            { Debug.LogError("[Firework] CrackleOnlyGlow shader is missing. Run Tools > Firework Studio > 0 - Create Three Fireworks."); return false; }
            var go=new GameObject("Firework - Crackle Only - " + pattern);
            go.transform.position=position;
            var fx=go.AddComponent<CrackleOnlyBurst>();
            fx.ownsSlot=true;
            activeBursts++;
            try { fx.Initialize(position,rotation,pattern,palette,useCustomColor,customColor,o,shader); }
            catch (System.Exception e) { Debug.LogException(e); Destroy(go); return false; }
            return true;
        }

        void Initialize(Vector3 position, Quaternion rotation, FireworkPattern pattern,
            FireworkPalette palette, bool useCustomColor, Color customColor, CrackleOnlyOptions o, Shader shader)
        {
            options=o; origin=position; orientation=rotation;
            int seed=Random.Range(1,int.MaxValue);
            rng=new System.Random(seed ^ 0x27d4eb2d);
            plan=CrackleOnlyLayout.BuildPlan(pattern,o,seed);
            Color[] source=useCustomColor ? new[]{customColor} : FireworkPaletteUtil.GetColors(palette);
            colors=new Color[source.Length];
            for(int i=0;i<source.Length;i++) colors[i]=NormalizeHue(source[i]);
            glow=new Material(shader) { name="Crackle Only Glow (owned)" };
            glow.SetFloat("_Intensity",o.emission);
            remainingBudget=o.maxParticlesPerShot;
            sparks=Layer("Short colored sparks",o.maxParticlesPerShot,.12f,false);
            flashes=Layer("Distributed pop cores",plan.Count,.015f,true);
            finishAt=plan.Count==0 ? 0f : plan[plan.Count-1].time+.95f;
            ready=true;
        }

        ParticleSystem Layer(string label,int budget,float gravity,bool core)
        {
            var go=new GameObject(label);
            go.transform.SetParent(transform,false);
            go.SetActive(false);
            var ps=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;
            main.playOnAwake=false; main.loop=false; main.duration=5f;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.scalingMode=ParticleSystemScalingMode.Shape;
            main.maxParticles=Mathf.Max(16,budget);
            main.startSpeed=0f; main.startSize=1f; main.startLifetime=1f;
            main.gravityModifier=gravity;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission; emission.enabled=false;
            var shape=ps.shape; shape.enabled=false;
            var trails=ps.trails; trails.enabled=false; // no laser-like ribbons, including on re-pops
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=glow;
            renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.sortMode=ParticleSystemSortMode.None;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            var col=ps.colorOverLifetime; col.enabled=true;
            var g=new Gradient();
            g.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},
                core ? new[]{new GradientAlphaKey(.2f,0f),new GradientAlphaKey(1f,.10f),new GradientAlphaKey(.8f,.3f),new GradientAlphaKey(0f,1f)} :
                new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(.7f,.22f),new GradientAlphaKey(.14f,.35f),new GradientAlphaKey(.85f,.44f),new GradientAlphaKey(.12f,.58f),new GradientAlphaKey(.50f,.67f),new GradientAlphaKey(0f,1f)});
            col.color=g;
            var size=ps.sizeOverLifetime; size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(
                new Keyframe(0f,core?.65f:1f),new Keyframe(.12f,1f),new Keyframe(1f,.12f)));
            go.SetActive(true);
            ps.Play(false);
            return ps;
        }

        void Update()
        {
            if(!ready) return;
            elapsed+=Time.deltaTime;
            // Sorted absolute schedule: frame rate changes do not stretch the crackle sequence.
            while(cursor<plan.Count && plan[cursor].time<=elapsed)
            { EmitPop(plan[cursor]); cursor++; }
            if(elapsed>=finishAt) { ready=false; Destroy(gameObject); }
        }

        void EmitPop(CrackleOnlyLayout.Pop pop)
        {
            if(remainingBudget<2) return;
            // Critical: EmitParams.position is an ABSOLUTE WORLD coordinate in these layers.
            Vector3 centre=origin + orientation * pop.localPosition;
            Color tint=colors[pop.cluster % colors.Length];
            var ep=new ParticleSystem.EmitParams();
            ep.position=centre;
            ep.velocity=Vector3.zero;
            ep.startLifetime=Next(.14f,.24f);
            ep.startSize=Next(.70f,1.05f)*options.sizeMultiplier*(pop.repeat?.8f:1f);
            ep.startColor=Color.Lerp(tint,Color.white,.12f);
            flashes.Emit(ep,1); remainingBudget--;
            FireworkAudio.Crackle(centre, pop.repeat ? .72f : 1f); // independent pop/re-pop schedule
            FireworkWaterReflection.ReflectCrackle(centre, tint);
            int n=Mathf.Min(remainingBudget,rng.Next(Mathf.Max(3,options.sparksPerPop-2),options.sparksPerPop+1));
            for(int i=0;i<n;i++)
            {
                ep.position=centre;
                ep.velocity=OnSphere()*Next(2.0f,5.2f);
                ep.startLifetime=Next(.38f,.72f);
                ep.startSize=Next(.32f,.48f)*options.sizeMultiplier;
                Color c=Color.Lerp(tint,colors[(pop.cluster+i)%colors.Length],.18f);
                c*=Next(.82f,1f); c.a=1f;
                ep.startColor=c;
                sparks.Emit(ep,1);
            }
            remainingBudget-=n;
        }
        float Next(float min,float max) { return Mathf.Lerp(min,max,(float)rng.NextDouble()); }
        Vector3 OnSphere()
        {
            float z=Next(-1f,1f), a=Next(0f,2f*Mathf.PI), r=Mathf.Sqrt(Mathf.Max(0f,1f-z*z));
            return new Vector3(Mathf.Cos(a)*r,z,Mathf.Sin(a)*r);
        }
        static Color NormalizeHue(Color c)
        {
            if(float.IsNaN(c.r)||float.IsNaN(c.g)||float.IsNaN(c.b)||float.IsInfinity(c.r)||float.IsInfinity(c.g)||float.IsInfinity(c.b))
                return new Color(1f,.2f,.6f,1f);
            float m=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
            if(m>1f) { c.r/=m; c.g/=m; c.b/=m; }
            c.r=Mathf.Clamp01(c.r); c.g=Mathf.Clamp01(c.g); c.b=Mathf.Clamp01(c.b); c.a=1f;
            return c;
        }
        void OnDestroy()
        {
            if(ownsSlot) { activeBursts=Mathf.Max(0,activeBursts-1); ownsSlot=false; }
            if(glow!=null) Destroy(glow);
        }
    }
}
