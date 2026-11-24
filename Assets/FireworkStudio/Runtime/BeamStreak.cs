using UnityEngine;

namespace FireworkStudio
{
    // Soft ribbon renderer, without one expensive light and sphere for every ray.
    public sealed class BeamStreak : MonoBehaviour
    {
        public Vector3 velocity;
        public float drag = 0.45f;
        public float gravity = 0f;
        public float life = 1.5f;
        public float headIntensity = 0f;

        TrailRenderer trail;
        float age;
        float originalWidth;

        public void Init(Color color, float width, float trailTime, float headScale, float intensity)
        {
            originalWidth = Mathf.Clamp(width * 0.28f, 0.008f, 0.022f);
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = SharedVisuals.TrailMaterial;
            trail.time = Mathf.Clamp(trailTime, 0.20f, 0.62f);
            trail.widthMultiplier = originalWidth;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 0.06f, 1f, 1f);
            trail.minVertexDistance = 0.018f;
            trail.numCapVertices = 2;
            trail.numCornerVertices = 1;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.42f), 0f),
                    new GradientColorKey(color, 0.38f),
                    new GradientColorKey(color * 0.60f, 1f)
                },
                new[] {
                    new GradientAlphaKey(0.08f, 0f),
                    new GradientAlphaKey(0.65f, 0.3f),
                    new GradientAlphaKey(0.95f, 1f)
                });
            trail.colorGradient = gradient;
            trail.Clear();
        }

        void Update()
        {
            var dt = Time.deltaTime;
            age += dt;
            if (age >= life)
            {
                if (trail != null) trail.emitting = false;
                Destroy(gameObject, trail != null ? trail.time + 0.1f : 0.1f);
                enabled = false;
                return;
            }
            velocity *= Mathf.Exp(-Mathf.Max(0f, drag) * dt);
            velocity += Vector3.down * gravity * dt;
            transform.position += velocity * dt;
            if (trail != null) trail.widthMultiplier = originalWidth * Mathf.Clamp01((life - age) / 0.45f);
        }
    }
}
