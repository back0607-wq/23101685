using UnityEngine;

namespace FireworkStudio.Secondary
{
    [DisallowMultipleComponent]
    public sealed class AfterburstSettings : MonoBehaviour
    {
        public static AfterburstSettings Instance { get; private set; }
        [Header("Master switch")]
        public bool enabledAfterburst = true;
        [Header("Look")]
        public AfterburstStyle style = AfterburstStyle.Random;
        [Range(0.2f, 1.5f)] public float startDelay = 0.48f;
        [Range(0.5f, 2f)] public float sizeMultiplier = 1.2f;
        [Range(0.2f, 2f)] public float brightness = 1.35f;
        [Range(0.2f, 1.5f)] public float density = 0.8f;
        [Range(0.1f, 1f)] public float tailDuration = 0.65f;
        [Header("VR performance")]
        [Range(40, 700)] public int maxParticlesPerBurst = 330;
        [Range(1, 8)] public int simultaneousAfterbursts = 3;
        public bool inheritMainPalette = true;
        [Header("Terminal crackle - short secondary pops")]
        public bool enableTerminalCrackle = true;
        [Range(3, 32)] public int terminalClusters = 15;
        [Range(3, 12)] public int sparksPerCluster = 7;
        [Range(0.1f, 0.8f)] public float terminalSpreadDelay = 0.36f;
        [Range(0.7f, 5f)] public float terminalSparkBrightness = 2.7f;
        [Range(0.5f, 3f)] public float terminalSizeMultiplier = 1.75f;
        [Range(1f, 5f)] public float terminalGlowIntensity = 3.6f;
        [Header("Tip crackle timing relative to main streak disappearance")]
        [Range(0.8f, 3.0f)] public float terminalFirstPopDelay = 1.55f;
        [Range(0.1f, 1f)] public float terminalRandomness = 0.4f;
        public Color customTint = new Color(1f, 0.46f, 0.8f);

        int activeSequences;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState() { Instance = null; }
        public static AfterburstSettings GetOrCreate()
        {
            if (Instance != null) return Instance;
            Instance = Object.FindFirstObjectByType<AfterburstSettings>();
            if (Instance == null)
                Instance = new GameObject("Firework Afterburst Settings (Auto)").AddComponent<AfterburstSettings>();
            Instance.activeSequences = 0;
            return Instance;
        }
        public bool TryReserve() { if (activeSequences >= simultaneousAfterbursts) return false; activeSequences++; return true; }
        public void Release() { activeSequences = Mathf.Max(0,activeSequences-1); }
        void Awake() { if (Instance != null && Instance != this) { Debug.LogWarning("[Firework] Multiple afterburst settings. Using first instance."); return; } Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }

}
