using UnityEngine;
using UnityEngine.Audio;

namespace FireworkStudio
{
    // Only the THREE SKY FIREWORKS use this asset. SparklerOptions and its audio are untouched.
    [CreateAssetMenu(menuName = "Firework Studio/Sky Audio Settings", fileName = "FireworkAudioSettings")]
    public sealed class FireworkAudioSettings : ScriptableObject
    {
        [Header("Sky fireworks only - sparkler sound is unchanged")]
        public bool audioEnabled = true;
        [Range(0f, 1f)] public float masterVolume = .55f;
        [Header("Sound layers")]
        public bool launchEnabled = true;
        [Range(0f, 1f)] public float launchVolume = .40f;
        public bool flightEnabled = true;
        [Range(0f, 1f)] public float flightVolume = .18f;
        public bool burstEnabled = true;
        [Range(0f, 1f)] public float burstVolume = .68f;
        public bool afterburstEnabled = true;
        [Range(0f, 1f)] public float afterburstVolume = .26f;
        public bool crackleEnabled = true;
        [Range(0f, 1f)] public float crackleVolume = .42f;
        [Header("Spatial sound")]
        [Tooltip("Uses the existing AudioListener; never creates another listener or changes XR settings.")]
        public bool useProjectSpatializer = true;
        public bool distanceFiltering = true;
        [Tooltip("Short sound-arrival delay for sky explosions. At 55 m it is about 0.16 seconds. No added delay for hand ignition.")]
        public bool propagationDelay = true;
        [Range(100f, 600f)] public float soundSpeed = 343f;
        [Range(80f, 300f)] public float skyAudibleDistance = 165f;
        [Tooltip("Optional existing AudioMixerGroup. Null routes directly to the normal audio output.")]
        public AudioMixerGroup outputMixer;
        [Header("Voice budget - only this package's sky audio")]
        [Range(12, 64)] public int maxVoices = 32;
        [Range(6, 32)] public int maxCrackleVoices = 20;
        [Range(1, 8)] public int maxFlightVoices = 4;
        [Tooltip("Gently reduces overlapping sounds. Not a hardware or system-volume limiter.")]
        public bool reduceOverlap = true;

        internal static float Safe(float v, float fallback, float min, float max)
        { return float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max); }
    }
}
