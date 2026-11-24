using System;
using UnityEngine;

namespace FireworkStudio
{
    [Serializable]
    public sealed class CrackleOnlyOptions
    {
        [Header("Spatial shape - uses the prop's pattern and palette selectors")]
        [Range(5f, 20f)] public float radius = 12f;
        [Range(24, 120)] public int clusters = 64;
        [Range(0f, 0.18f)] public float positionJitter = 0.025f;
        [Header("Irregular pop timing (seconds after reaching the sky)")]
        [Range(0f, 0.6f)] public float startDelay = 0.08f;
        [Range(0.3f, 2.2f)] public float spreadDuration = 1.05f;
        [Range(0f, 0.75f)] public float repeatProbability = 0.35f;
        [Header("Colored flash + short sparks, NOT long main streaks")]
        [Range(3, 16)] public int sparksPerPop = 8;
        [Range(0.5f, 2f)] public float sizeMultiplier = 1f;
        [Range(1f, 10f)] public float emission = 5.8f;
        [Header("Limits for this independent effect only")]
        [Range(256, 4096)] public int maxParticlesPerShot = 1400;
        [Tooltip("Full-quality live burst budget. Further shots use lower density instead of being dropped.")]
        [Range(1, 8)] public int maxSimultaneousBursts = 4;

        public CrackleOnlyOptions CopyClamped()
        {
            var copy = new CrackleOnlyOptions {
                radius = ClampFinite(radius, 5f, 20f, 12f),
                clusters = Mathf.Clamp(clusters, 24, 120),
                positionJitter = ClampFinite(positionJitter, 0f, .18f, .025f),
                startDelay = ClampFinite(startDelay, 0f, .6f, .08f),
                spreadDuration = ClampFinite(spreadDuration, .3f, 2.2f, 1.05f),
                repeatProbability = ClampFinite(repeatProbability, 0f, .75f, .35f),
                sparksPerPop = Mathf.Clamp(sparksPerPop, 3, 16),
                sizeMultiplier = ClampFinite(sizeMultiplier, .5f, 2f, 1f),
                emission = ClampFinite(emission, 1f, 10f, 5.8f),
                maxParticlesPerShot = Mathf.Clamp(maxParticlesPerShot, 256, 4096),
                maxSimultaneousBursts = Mathf.Clamp(maxSimultaneousBursts, 1, 8)
            };
            // Reserve enough points to read a shape even on a reduced particle budget.
            int affordable = Mathf.Max(3, copy.maxParticlesPerShot / (2 * 24) - 1);
            copy.sparksPerPop = Mathf.Min(copy.sparksPerPop, affordable);
            return copy;
        }
        static float ClampFinite(float v, float min, float max, float fallback)
        { return float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max); }
    }
}
