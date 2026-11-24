using System.Collections;
using UnityEngine;

namespace FireworkStudio
{
    // Particle-based fireworks: world-space emission must use the sky burst position.
    public sealed class VideoBurstController : MonoBehaviour
    {
        FireworkPattern pattern;
        FireworkPalette palette;
        bool customMode;
        Color customColor;
        Color[] CurrentColors() { return customMode ? new[] {customColor, Color.Lerp(customColor, new Color(0.25f,0.25f,0.25f), 0.12f), customColor * 0.83f} : FireworkPaletteUtil.GetColors(palette); }
        Color CurrentColor() { var colors=CurrentColors(); return colors[Random.Range(0,colors.Length)]; }
        Light flash;
        ParticleSystem mainBurst;
        ParticleSystem highlights;
        ParticleSystem embers;
        ParticleSystem smoke;
        Material smokeMaterial;
        Material mainGlowMaterial;
        Material twinkleGlowMaterial;

        Material NewGlowMaterial(float intensity)
        {
            var material = new Material(SharedVisuals.TrailMaterial);
            if (material.HasProperty("_Intensity")) material.SetFloat("_Intensity", intensity);
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", Color.white);
            return material;
        }

        public static void Spawn(Vector3 position, FireworkPattern pattern, FireworkPalette palette)
        {
            Spawn(position, pattern, palette, false, Color.white, true);
        }

        public static void Spawn(Vector3 position, FireworkPattern pattern, FireworkPalette palette,
            bool useCustomColor, Color custom)
        {
            Spawn(position, pattern, palette, useCustomColor, custom, true);
        }

        // Per-shot switch. Never modify the scene's global AfterburstSettings here.
        public static void Spawn(Vector3 position, FireworkPattern pattern, FireworkPalette palette,
            bool useCustomColor, Color custom, bool includeAfterburst)
        {
            var go = new GameObject(includeAfterburst ? "Firework - Combined" : "Firework - Main Trails Only");
            go.transform.position = position;
            var fx = go.AddComponent<VideoBurstController>();
            fx.pattern = pattern;
            fx.palette = palette;
            fx.customMode = useCustomColor;
            fx.customColor = custom;
            fx.Build();
            if (includeAfterburst)
                FireworkStudio.Secondary.AfterburstSequence.Attach(go, position, palette);
        }

        void Build()
        {
            // A dedicated material keeps main-burst tuning separate from the terminal crackle.
            mainGlowMaterial = NewGlowMaterial(5.2f);
            twinkleGlowMaterial = NewGlowMaterial(4.5f);
            mainBurst = NewLayer("Main glow", 0.32f, 0.26f, 1300, false);
            highlights = NewLayer("Twinkles", 0.17f, 0.12f, 350, false);
            var mainRenderer = mainBurst.GetComponent<ParticleSystemRenderer>();
            mainRenderer.sharedMaterial = mainGlowMaterial;
            mainRenderer.trailMaterial = mainGlowMaterial;
            var twinkleRenderer = highlights.GetComponent<ParticleSystemRenderer>();
            twinkleRenderer.sharedMaterial = twinkleGlowMaterial;
            twinkleRenderer.trailMaterial = twinkleGlowMaterial;
            var mainTrails = mainBurst.trails;
            mainTrails.ratio = 0.70f;
            mainTrails.lifetime = 0.20f;
            var twinkleTrails = highlights.trails;
            twinkleTrails.ratio = 0.42f;
            twinkleTrails.lifetime = 0.11f;
            embers = NewLayer("Falling embers", 0.15f, 0.38f, 350, true);
            BuildSmoke();
            var lightGo = new GameObject("Short flash");
            lightGo.transform.SetParent(transform, false);
            flash = lightGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = CurrentColor();
            flash.range = 22f;
            flash.intensity = 0;
            flash.shadows = LightShadows.None;
            StartCoroutine(PlayBurst());
        }

        ParticleSystem NewLayer(string name, float size, float gravity, int max, bool falling)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=ps.main;
            m.playOnAwake=false;
            m.loop=false;
            m.simulationSpace=ParticleSystemSimulationSpace.World;
            m.startSpeed=0;
            m.startSize=size;
            m.startLifetime=1.7f;
            m.gravityModifier=gravity;
            m.maxParticles=max;
            var e=ps.emission;
            e.enabled=false;
            var shape=ps.shape;
            shape.enabled=false;
            var r=ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial=SharedVisuals.TrailMaterial;
            r.renderMode=ParticleSystemRenderMode.Billboard;
            r.sortMode=ParticleSystemSortMode.Distance;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            // Short, thin trails only. Do not use wide stretch ribbons.
            var tr=ps.trails;
            tr.enabled=!falling;
            tr.mode=ParticleSystemTrailMode.PerParticle;
            tr.ratio=falling?0f:0.35f;
            tr.lifetime=0.12f;
            tr.dieWithParticles=false;
            tr.sizeAffectsWidth=true;
            tr.worldSpace=true;
            r.trailMaterial=SharedVisuals.TrailMaterial;
            var color=ps.colorOverLifetime;
            color.enabled=true;
            var grad=new Gradient();
            grad.SetKeys(new [] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new [] {new GradientAlphaKey(0.9f,0),new GradientAlphaKey(0.9f,0.2f),new GradientAlphaKey(0.3f,0.7f),new GradientAlphaKey(0,1)});
            color.color=grad;
            return ps;
        }

        IEnumerator PlayBurst()
        {
            flash.intensity=11f;
            FireworkAudio.MainBurst(transform.position, pattern); // actual main VFX onset
            EmitSmoke(18, 0.10f);
            Emit(mainBurst, 155, 8.5f, 13.0f, 1.9f, 2.8f, 0.19f, 0.34f, true);
            yield return new WaitForSeconds(0.11f);
            Emit(highlights, 85, 4.0f, 9.0f, 0.6f, 1.6f, 0.095f, 0.18f, false);
            EmitSmoke(10, 0.02f);
            yield return new WaitForSeconds(0.25f);
            Emit(embers, 48, 1.5f, 4.2f, 1.2f, 2.3f, 0.05f, 0.11f, false);
            float t=0;
            while(t<0.65f)
            {
                t += Time.deltaTime;
                flash.intensity=Mathf.Lerp(9f,0,Mathf.Clamp01(t/0.40f));
                if(smokeMaterial!=null && smokeMaterial.HasProperty("_Glow"))
                    smokeMaterial.SetFloat("_Glow",Mathf.Lerp(0.85f,0.025f,Mathf.Clamp01(t/0.65f)));
                yield return null;
            }
            Destroy(gameObject, 5f);
        }

        void OnDestroy()
        {
            if (mainGlowMaterial != null) Destroy(mainGlowMaterial);
            if (twinkleGlowMaterial != null) Destroy(twinkleGlowMaterial);
            if (smokeMaterial != null) Destroy(smokeMaterial);
        }

        void BuildSmoke()
        {
            var smokeShader = Shader.Find("FireworkStudio/IlluminatedSmokeURP");
            if (smokeShader == null)
            {
                Debug.LogWarning("[Fireworks] IlluminatedSmokeURP shader missing; smoke effect skipped.");
                return;
            }
            var go = new GameObject("Soft illuminated powder smoke");
            go.transform.SetParent(transform,false);
            smoke = go.AddComponent<ParticleSystem>();
            smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=smoke.main;
            main.playOnAwake=false;
            main.loop=false;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startSpeed=0f;
            main.startLifetime=2.6f;
            main.startSize=1f;
            main.maxParticles=120;
            var emission=smoke.emission;
            emission.enabled=false;
            var shape=smoke.shape;
            shape.enabled=false;
            var size=smoke.sizeOverLifetime;
            size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(
                new Keyframe(0f,0.16f),new Keyframe(0.24f,0.75f),new Keyframe(1f,1.75f)));
            var c=smoke.colorOverLifetime;
            c.enabled=true;
            var g=new Gradient();
            g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0f,0f),new GradientAlphaKey(0.48f,0.16f),new GradientAlphaKey(0.30f,0.53f),new GradientAlphaKey(0f,1f)});
            c.color=g;
            var r=go.GetComponent<ParticleSystemRenderer>();
            r.renderMode=ParticleSystemRenderMode.Billboard;
            r.sortMode=ParticleSystemSortMode.Distance;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            smokeMaterial=new Material(smokeShader);
            smokeMaterial.SetColor("_SmokeColor",new Color(0.50f,0.53f,0.63f,0.45f));
            smokeMaterial.SetColor("_LightColor",CurrentColor());
            smokeMaterial.SetFloat("_Opacity",0.55f);
            smokeMaterial.SetFloat("_Glow",0.85f);
            r.sharedMaterial=smokeMaterial;
        }

        void EmitSmoke(int count,float verticalBias)
        {
            if(smoke==null)return;
            for(int i=0;i<count;i++)
            {
                var ep=new ParticleSystem.EmitParams();
                ep.position=transform.position + Random.insideUnitSphere*0.2f; // World-space smoke origin.
                ep.velocity=Random.onUnitSphere*Random.Range(0.5f,1.9f)+Vector3.up*verticalBias;
                ep.startLifetime=Random.Range(1.45f,3.1f);
                ep.startSize=Random.Range(0.45f,1.15f);
                ep.startColor=Color.white;
                smoke.Emit(ep,1);
            }
            smoke.Play();
        }

        void Emit(ParticleSystem ps, int count,float sMin,float sMax,float lMin,float lMax,float zMin,float zMax,bool shaped)
        {
            var colors=CurrentColors();
            for(int i=0;i<count;i++)
            {
                var dir=shaped?Direction(i,count):Random.onUnitSphere;
                var ep=new ParticleSystem.EmitParams();
                ep.position=transform.position; // World-space simulation: emit at actual sky burst coordinates.
                ep.velocity=dir*Random.Range(sMin,sMax);
                ep.startLifetime=Random.Range(lMin,lMax);
                ep.startSize=Random.Range(zMin,zMax);
                // Keep emission vivid: excessive HDR particle RGB can clip channels to white.
                ep.startColor=colors[Random.Range(0,colors.Length)]*Random.Range(1.1f,1.55f);
                ps.Emit(ep,1);
            }
            ps.Play();
        }

        Vector3 Direction(int i,int count)
        {
            float a=i*2f*Mathf.PI/count;
            switch(pattern)
            {
                case FireworkPattern.Ring:
                    return new Vector3(Mathf.Cos(a),Random.Range(-0.06f,0.06f),Mathf.Sin(a)).normalized;
                case FireworkPattern.DoubleRing:
                    return new Vector3(Mathf.Cos(a), i%2==0?0.20f:-0.20f,Mathf.Sin(a)).normalized;
                case FireworkPattern.Flower:
                    float radius=0.55f+0.45f*Mathf.Cos(a*6f);
                    return new Vector3(Mathf.Cos(a)*radius,Random.Range(-0.14f,0.14f),Mathf.Sin(a)*radius).normalized;
                case FireworkPattern.Willow:
                    var up=Random.onUnitSphere;up.y=Mathf.Abs(up.y)*0.7f+0.3f;return up.normalized;
                case FireworkPattern.Star:
                {
                    float r=(i%2==0)?1f:0.46f;
                    return new Vector3(Mathf.Cos(a)*r, Mathf.Sin(a)*r, Random.Range(-0.04f,0.04f)).normalized;
                }
                case FireworkPattern.Heart:
                {
                    float t=a;
                    float x=16f*Mathf.Pow(Mathf.Sin(t),3f);
                    float y=13f*Mathf.Cos(t)-5f*Mathf.Cos(2f*t)-2f*Mathf.Cos(3f*t)-Mathf.Cos(4f*t);
                    return new Vector3(x,y,Random.Range(-0.20f,0.20f)).normalized;
                }
                case FireworkPattern.Spiral:
                    return new Vector3(Mathf.Cos(a)*Mathf.Lerp(0.25f,1f,i/(float)count),
                        (i/(float)count-0.5f)*0.9f,Mathf.Sin(a)*Mathf.Lerp(0.25f,1f,i/(float)count)).normalized;
                case FireworkPattern.Crown:
                    return new Vector3(Mathf.Cos(a),Mathf.Abs(Mathf.Sin(a*5f))*0.8f+0.2f,Mathf.Sin(a)).normalized;
                case FireworkPattern.Snowflake:
                {
                    float ray=Mathf.Round(a/(Mathf.PI/3f))*(Mathf.PI/3f);
                    return new Vector3(Mathf.Cos(ray),Mathf.Sin(ray),Random.Range(-0.05f,0.05f)).normalized;
                }
                case FireworkPattern.Butterfly:
                {
                    float wing = (i % 2 == 0) ? -1f : 1f;
                    float t = (i / (float)count) * Mathf.PI * 4f;
                    float x = Mathf.Sin(t) * (1.1f + .4f*Mathf.Cos(3f*t));
                    float y = Mathf.Cos(t) * (.65f + .25f*Mathf.Cos(2f*t));
                    return new Vector3(wing*Mathf.Abs(x), y, Random.Range(-.18f,.18f)).normalized;
                }
                case FireworkPattern.Palm:
                {
                    float spoke = Mathf.Floor(i / 8f) * (Mathf.PI*2f/10f);
                    return new Vector3(Mathf.Cos(spoke)*Random.Range(.45f,1f), Random.Range(.45f,1f), Mathf.Sin(spoke)*Random.Range(.45f,1f)).normalized;
                }
                case FireworkPattern.Galaxy:
                {
                    float turn = a*3.0f;
                    float arm = (i%3)*Mathf.PI*2f/3f;
                    float elev = Mathf.Lerp(-.35f,.35f,(i%23)/22f);
                    return new Vector3(Mathf.Cos(turn+arm), elev, Mathf.Sin(turn+arm)).normalized;
                }
                case FireworkPattern.Diamond:
                {
                    float x = Mathf.Cos(a); float y = Mathf.Sin(a);
                    return new Vector3(x, y, Random.Range(-.10f,.10f)).normalized;
                }
                case FireworkPattern.FourLeaf:
                {
                    float petal = .48f + .52f * Mathf.Abs(Mathf.Sin(a*2f));
                    return new Vector3(Mathf.Cos(a)*petal, Mathf.Sin(a)*petal, Random.Range(-.10f,.10f)).normalized;
                }
                case FireworkPattern.Cross:
                {
                    float ray = Mathf.Round(a/(Mathf.PI/2f))*(Mathf.PI/2f);
                    return new Vector3(Mathf.Cos(ray), Mathf.Sin(ray), Random.Range(-.12f,.12f)).normalized;
                }
                case FireworkPattern.Fan:
                {
                    float spread = Mathf.Lerp(-1.28f,1.28f,i/(float)Mathf.Max(1,count-1));
                    return new Vector3(Mathf.Sin(spread), .40f+Mathf.Cos(spread), Random.Range(-.17f,.17f)).normalized;
                }
                case FireworkPattern.DoubleFlower:
                {
                    float petal = .45f + .55f*Mathf.Abs(Mathf.Sin(a*7f));
                    return new Vector3(Mathf.Cos(a)*petal, Mathf.Sin(a)*petal, Random.Range(-.12f,.12f)).normalized;
                }
                default: return Random.onUnitSphere;
            }
        }
    }
}
