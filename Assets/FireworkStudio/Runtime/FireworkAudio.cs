using UnityEngine;

namespace FireworkStudio
{
    // Explicit visual-event hooks. No polling by object names, fixed crackle timers,
    // UnityEngine.Random calls, camera audio sources, or edits to the XR input pipeline.
    public static class FireworkAudio
    {
        static FireworkAudioRuntime runtime;
        static bool quitting, failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { runtime = null; quitting = false; failed = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Warmup()
        {
            try { if (Object.FindFirstObjectByType<FireworkProp>() != null) GetRuntime(); }
            catch (System.Exception e) { Fail(e); }
        }

        static FireworkAudioRuntime GetRuntime()
        {
            if (!Application.isPlaying || quitting || failed) return null;
            if (runtime != null) return runtime;
            runtime = Object.FindFirstObjectByType<FireworkAudioRuntime>();
            if (runtime == null)
                runtime = new GameObject("Firework Audio (pooled)").AddComponent<FireworkAudioRuntime>();
            return runtime;
        }

        public static void Launch(Transform projectile, float flightSeconds)
        {
            if (projectile == null) return;
            try { var r = GetRuntime(); if (r != null) r.Launch(projectile, flightSeconds); }
            catch (System.Exception e) { Fail(e); }
        }
        public static void MainBurst(Vector3 worldPosition, FireworkPattern pattern)
        {
            try
            {
                var r = GetRuntime(); if (r == null) return;
                float energy = pattern == FireworkPattern.Ring || pattern == FireworkPattern.Heart ? .88f : 1f;
                r.OneShot(FireworkSoundKind.Burst, worldPosition, energy);
            }
            catch (System.Exception e) { Fail(e); }
        }
        public static void Afterburst(Vector3 worldPosition)
        {
            try { var r = GetRuntime(); if (r != null) r.OneShot(FireworkSoundKind.Bloom, worldPosition, 1f); }
            catch (System.Exception e) { Fail(e); }
        }
        public static void Crackle(Vector3 actualPopPosition, float energy = 1f)
        {
            try { var r = GetRuntime(); if (r != null) r.OneShot(FireworkSoundKind.Pop, actualPopPosition, energy); }
            catch (System.Exception e) { Fail(e); }
        }
        static void Fail(System.Exception e)
        {
            if (failed) return;
            failed = true;
            Debug.LogError("[Firework Audio] Audio stopped after an error; visuals are unaffected. " + e.Message);
            if (runtime != null) runtime.enabled = false;
        }
        internal static void Shutdown(FireworkAudioRuntime owner, bool isQuitting)
        {
            if (runtime == owner) runtime = null;
            if (isQuitting) quitting = true;
        }
    }

    internal enum FireworkSoundKind { Launch, Flight, Burst, Bloom, Pop }
}
