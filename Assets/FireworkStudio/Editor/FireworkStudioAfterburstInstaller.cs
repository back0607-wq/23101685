using UnityEditor;
using UnityEngine;
using FireworkStudio.Secondary;

public static class FireworkStudioInstaller
{
    [MenuItem("Tools/Firework Studio/Combined/Select Afterburst Settings")]
    public static void Install()
    {
        var objects = Object.FindObjectsByType<AfterburstSettings>(FindObjectsSortMode.None);
        AfterburstSettings setting = objects.Length>0 ? objects[0] : null;
        if(setting==null)
        {
            var go=new GameObject("Firework Afterburst Settings");
            Undo.RegisterCreatedObjectUndo(go,"Install afterburst settings");
            setting=go.AddComponent<AfterburstSettings>();
        }
        Selection.activeGameObject=setting.gameObject;
        EditorGUIUtility.PingObject(setting);
        Debug.Log("[Firework] Afterburst enabled. Customize style/colors in Firework Afterburst Settings.");
    }

    [MenuItem("Tools/Firework Studio/Combined/Disable Legacy Afterburst")]
    public static void Disable()
    {
        foreach(var setting in Object.FindObjectsByType<AfterburstSettings>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(setting,"Disable afterburst");
            setting.enabledAfterburst=false;
            EditorUtility.SetDirty(setting);
        }
    }

    [MenuItem("Tools/Firework Studio/Advanced/Create Original Props If Missing")]
    public static void CreateProps()
    {
        FireworkCompleteInstaller.InstallCompleteFireworks();
    }
}
