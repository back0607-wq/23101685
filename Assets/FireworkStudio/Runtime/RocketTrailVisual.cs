using UnityEngine;

namespace FireworkStudio
{
    public class RocketTrailVisual : MonoBehaviour
    {
        public Transform tail;
        public FireworkPalette palette = FireworkPalette.Mixed;
        public bool autoPlay = false;

        TrailRenderer _trail;
        ParticleSystem _sparkle;
        Light _light;
        GameObject _fxRoot;

        void Start()
        {
            if (tail == null) tail = transform;
            Build();
            SetActive(autoPlay);
        }

        void Build()
        {
            _fxRoot = new GameObject("RocketTrailFX");
            _fxRoot.transform.SetParent(tail, false);

            _trail = tail.gameObject.GetComponent<TrailRenderer>();
            if (_trail == null) _trail = tail.gameObject.AddComponent<TrailRenderer>();
            _trail.material = new Material(SharedVisuals.TrailMaterial);
            _trail.time = 0.52f;
            _trail.startWidth = 0.030f;
            _trail.endWidth = 0.004f;
            _trail.minVertexDistance = 0.008f;
            _trail.numCapVertices = 4;
            _trail.alignment = LineAlignment.View;
            var color = FireworkPaletteUtil.RandomColor(palette);
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(Color.white * 1.9f, 0f),
                    new GradientColorKey(color, 0.28f),
                    new GradientColorKey(color * 0.7f, 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.55f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            _trail.colorGradient = grad;

            var psGo = new GameObject("Sparkle");
            psGo.transform.SetParent(_fxRoot.transform, false);
            _sparkle = psGo.AddComponent<ParticleSystem>();
            var ren = psGo.GetComponent<ParticleSystemRenderer>();
            ren.material = SharedVisuals.TrailMaterial;
            ren.renderMode = ParticleSystemRenderMode.Stretch;
            ren.lengthScale = 2.5f;
            ren.velocityScale = 0.9f;
            var main = _sparkle.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startColor = color;
            main.maxParticles = 260;
            var emission = _sparkle.emission;
            emission.rateOverTime = 22f;
            var shape = _sparkle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.006f;
            var col = _sparkle.colorOverLifetime;
            col.enabled = true;
            var cg = new Gradient();
            cg.SetKeys(
                new[] {
                    new GradientColorKey(Color.white * 1.6f, 0f),
                    new GradientColorKey(color, 0.3f),
                    new GradientColorKey(color * 0.65f, 0.85f)
                },
                new[] {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.45f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(cg);

            var lightGo = new GameObject("RocketLight");
            lightGo.transform.SetParent(_fxRoot.transform, false);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 3.5f;
            _light.intensity = 3.0f;
            _light.color = color;
            lightGo.AddComponent<LightPulse>().baseIntensity = 3.0f;
        }

        public void SetActive(bool active)
        {
            if (_fxRoot == null) Build();
            _fxRoot.SetActive(active);
            _trail.emitting = active;
            if (active) _sparkle.Play();
            else _sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
