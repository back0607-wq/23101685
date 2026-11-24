using UnityEngine;

namespace FireworkStudio
{
    // Deterministic, world-space ascent. Only this detached object may spawn a burst.
    public sealed class PrettyProjectileFlight : MonoBehaviour
    {
        Vector3 start;
        Vector3 target;
        float duration;
        float elapsed;
        FireworkPattern pattern;
        FireworkPalette palette;
        bool custom;
        Color chosenColor;
        bool initialized;
        bool burst;
        TrailRenderer trail;
        FireworkShowMode showMode = FireworkShowMode.Combined;
        CrackleOnlyOptions crackleOptions;
        Quaternion shapeRotation;

        public void Init(Vector3 origin, Vector3 skyPosition, float seconds,
            FireworkPattern selectedPattern, FireworkPalette selectedPalette,
            bool useCustomColor, Color customColor,
            FireworkShowMode mode = FireworkShowMode.Combined,
            CrackleOnlyOptions options = null, Quaternion? layoutRotation = null)
        {
            start = origin;
            target = skyPosition;
            duration = Mathf.Max(2f, seconds);
            pattern = selectedPattern;
            palette = selectedPalette;
            custom = useCustomColor;
            chosenColor = customColor;
            showMode = mode;
            // Snapshot settings: changing the held prop cannot change an in-flight shot.
            crackleOptions = (options ?? new CrackleOnlyOptions()).CopyClamped();
            shapeRotation = layoutRotation ?? Quaternion.identity;
            initialized = true;
            transform.position = start;
            trail = GetComponent<TrailRenderer>();
            if (trail != null) { trail.Clear(); trail.emitting = true; }
            FireworkAudio.Launch(transform, duration); // audio only; follows this detached projectile
        }

        void Update()
        {
            if (!initialized || burst) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Arcing ascent, but never tied to hand/controller position after launch.
            float eased = t * t * (3f - 2f * t);
            Vector3 pos = Vector3.LerpUnclamped(start, target, eased);
            pos.y += Mathf.Sin(Mathf.PI * t) * 2.0f;
            transform.position = pos;
            if (t < 1f) return;
            burst = true;
            if (trail != null) trail.emitting = false;
            Vector3 actual = transform.position;
            float rise = actual.y - start.y;
            float distance = Vector3.Distance(actual, start);
            Debug.Log($"[Firework SKY] BURST at={actual:F2}, rise={rise:F1}m, distance={distance:F1}m, cameraDistance={(Camera.main != null ? Vector3.Distance(actual, Camera.main.transform.position) : -1f):F1}m");
            FireworkBurstDispatcher.Spawn(actual, pattern, palette, custom, chosenColor,
                showMode, crackleOptions, shapeRotation);
            Destroy(gameObject);
        }
    }
}
