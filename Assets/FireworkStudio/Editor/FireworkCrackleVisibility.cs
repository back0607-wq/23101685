using UnityEditor;
using UnityEngine;
using FireworkStudio.Secondary;

public static class FireworkCrackleVisibility
{
    [MenuItem("Tools/Firework Studio/Combined/Apply Bright Terminal Crackle")]
    public static void Apply()
    {
        var all = Object.FindObjectsByType<AfterburstSettings>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all.Length == 0)
        {
            var go = new GameObject("Firework Afterburst Settings");
            Undo.RegisterCreatedObjectUndo(go, "Create Afterburst Settings");
            all = new[] { go.AddComponent<AfterburstSettings>() };
        }
        foreach (var settings in all)
        {
            Undo.RecordObject(settings, "Apply Bright Crackle");
            settings.enabledAfterburst = true;
            settings.enableTerminalCrackle = true;
            settings.terminalClusters = 22;
            settings.sparksPerCluster = 9;
            settings.terminalSpreadDelay = 0.50f;
            settings.terminalSparkBrightness = 2.7f;
            settings.terminalSizeMultiplier = 1.75f;
            settings.terminalGlowIntensity = 3.6f;
            EditorUtility.SetDirty(settings);
        }
        Debug.Log("[Firework Studio] Applied larger/brighter endpoint crackle to " + all.Length + " settings object(s). Main burst unchanged.");
    }
}
