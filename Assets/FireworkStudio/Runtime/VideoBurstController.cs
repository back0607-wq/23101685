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
        Quaternion shapeRotation;
        float terminalTime = 2f;
        readonly System.Collections.Generic.List<Vector3> endpoints = new System.Collections.Generic.List<Vector3>();
        Light flash;
        ParticleSystem mainBurst;
        ParticleSystem highlights;
        ParticleSystem embers;
        ParticleSystem smoke;
        ParticleSystem ornaments;
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
            bool useCustomColor, Color custom, bool includeAfterburst, Quaternion? orientation = null)
        {
            var go = new GameObject(includeAfterburst ? "Firework - Combined" : "Firework - Main Trails Only");
            go.transform.position = position;
            var fx = go.AddComponent<VideoBurstController>();
            fx.pattern = pattern;
            fx.palette = palette;
            fx.customMode = useCustomColor;
            fx.customColor = custom;
            var viewer = Camera.main;
            fx.shapeRotation = orientation ?? (viewer != null ? Quaternion.LookRotation((position - viewer.transform.position).normalized, Vector3.up) : Quaternion.identity);
            if (includeAfterburst)
            {
                var afterburst = FireworkStudio.Secondary.AfterburstSettings.GetOrCreate();
                fx.terminalTime = afterburst.startDelay + afterburst.terminalFirstPopDelay;
            }
            fx.Build();
            if (includeAfterburst)
                FireworkStudio.Secondary.AfterburstSequence.Attach(go, position, palette, pattern, fx.shapeRotation, useCustomColor ? (Color?)custom : null, fx.endpoints);
        }

        void Build()
        {
            // A dedicated material keeps main-burst tuning separate from the terminal crackle.
            mainGlowMaterial = NewGlowMaterial(5.6f);
            twinkleGlowMaterial = NewGlowMaterial(7f);
            if (twinkleGlowMaterial.HasProperty("_StarAmount")) twinkleGlowMaterial.SetFloat("_StarAmount", 1f);
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
            mainTrails.lifetime = 0.16f;
            mainTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.9f, 1f, 0f));
            var twinkleTrails = highlights.trails;
            twinkleTrails.ratio = 0.42f;
            twinkleTrails.lifetime = 0.09f;
            embers = NewLayer("Falling embers", 0.15f, 0.38f, 350, true);
            ornaments = NewLayer("Starlight rings and glints", 0.6f, 0f, 256, true);
            ornaments.GetComponent<ParticleSystemRenderer>().sharedMaterial = twinkleGlowMaterial;
            var ornamentColor = ornaments.colorOverLifetime;
            var starlight = new Gradient();
            starlight.SetKeys(new[] { new GradientColorKey(new Color(0.7f,0.9f,1f),0f), new GradientColorKey(Color.white,0.3f), new GradientColorKey(new Color(1f,0.5f,0.85f),1f) },
                new[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(1f,0.12f), new GradientAlphaKey(0.7f,0.6f), new GradientAlphaKey(0f,1f) });
            ornamentColor.color = starlight;
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
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.12f, 1f), new Keyframe(0.65f, 0.55f), new Keyframe(1f, 0.08f)));
            return ps;
        }

        IEnumerator PlayBurst()
        {
            flash.intensity=11f;
            FireworkAudio.MainBurst(transform.position, pattern); // actual main VFX onset
            EmitSmoke(18, 0.10f);
            Emit(mainBurst, 220, 13.2f, 13.8f, 2.0f, 2.6f, 0.42f, 0.62f, true);
            yield return new WaitForSeconds(0.11f);
            EmitOrnaments();
            Emit(highlights, 72, 12.4f, 13.2f, 0.7f, 1.4f, 0.11f, 0.20f, true);
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

        void EmitOrnaments()
        {
            var colors = CurrentColors();
            var ep = new ParticleSystem.EmitParams();
            // Two delicate rings frame the silhouette while large glints linger around it.
            for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2f / 64f;
                Quaternion tilt = ring == 0 ? Quaternion.Euler(55f, 0f, 20f) : Quaternion.Euler(-30f, 65f, -15f);
                Vector3 radial = shapeRotation * (tilt * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
                ep.position = transform.position + radial * (ring == 0 ? 4.2f : 7.6f);
                ep.velocity = radial * (ring == 0 ? 1.4f : 2.1f);
                ep.startLifetime = ring == 0 ? 1.15f : 1.55f;
                ep.startSize = ring == 0 ? 0.8f : 1.0f;
                ep.startColor = colors[ring % colors.Length];
                ornaments.Emit(ep, 1);
            }
            for (int i = 0; i < 16; i++)
            {
                Vector3 point = shapeRotation * CrackleOnlyLayout.SampleVolume(pattern, i, 16);
                ep.position = transform.position + point * 11.5f;
                ep.velocity = point * 0.75f;
                ep.startLifetime = Random.Range(1.1f, 1.8f);
                ep.startSize = Random.Range(1.7f, 2.4f);
                ep.startColor = Color.Lerp(colors[i % colors.Length], Color.white, 0.18f);
                ornaments.Emit(ep, 1);
            }
            ornaments.Play();
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
                ep.position=transform.position + Random.insideUnitSphere*2.2f; // A small 3D cloud around the world-space burst.
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
                var dir = shaped ? shapeRotation * CrackleOnlyLayout.SampleVolume(pattern, i, count) : Random.onUnitSphere;
                var ep=new ParticleSystem.EmitParams();
                ep.position=transform.position; // World-space simulation: emit at actual sky burst coordinates.
                ep.velocity=dir*Random.Range(sMin,sMax);
                ep.startLifetime=Random.Range(lMin,lMax);
                if (ps == mainBurst)
                {
                    float endTime = Mathf.Min(terminalTime, ep.startLifetime * 0.94f);
                    endpoints.Add(transform.position + ep.velocity * endTime + 0.5f * Physics.gravity * 0.26f * endTime * endTime);
                }
                ep.startSize=Random.Range(zMin,zMax);
                // Keep emission vivid: excessive HDR particle RGB can clip channels to white.
                Color tint = colors[i % colors.Length];
                if (shaped)
                {
                    float depth = (Quaternion.Inverse(shapeRotation) * dir).z;
                    // Bright foreground sparks and cooler back-shell sparks support depth cues.
                    tint = Color.Lerp(tint, new Color(0.32f, 0.48f, 1f), Mathf.Clamp01(depth) * 0.25f);
                    tint *= Mathf.Lerp(1.15f, 0.78f, Mathf.InverseLerp(-0.72f, 0.72f, depth));
                }
                ep.startColor=tint*Random.Range(1.05f,1.30f);
                ps.Emit(ep,1);
            }
            ps.Play();
        }

    }
}
