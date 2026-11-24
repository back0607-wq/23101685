using UnityEditor;
using UnityEngine;

namespace FireworkStudio.EditorTools
{
    public static class FireworkAudioTools
    {
        [MenuItem("Tools/Firework Studio/Audio/Settings")]
        public static void SelectSettings()
        {
            var settings=Resources.Load<FireworkAudioSettings>(FireworkAudioRuntime.SettingsPath);
            if(settings==null)
            { Debug.LogError("[Firework Audio] Audio settings asset is missing. Re-run INSTALL.cmd from the full ZIP."); return; }
            Selection.activeObject=settings;
            EditorGUIUtility.PingObject(settings);
        }
        [MenuItem("Tools/Firework Studio/Audio/Check Setup")]
        public static void CheckSetup()
        {
            int count=Resources.LoadAll<AudioClip>("FireworkAudio").Length;
            int listeners=0;
            foreach(var a in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if(a.isActiveAndEnabled) listeners++;
            Debug.Log("[Firework Audio] Clips="+count+"/21; active listeners="+listeners+
                "; spatializer="+AudioSettings.GetSpatializerPluginName()+". Existing sparkler audio is unchanged.");
            if(count!=21) Debug.LogWarning("[Firework Audio] Expected 21 sky-audio WAV clips. Check the Resources/FireworkAudio folder.");
            if(listeners!=1) Debug.LogWarning("[Firework Audio] Expected ONE enabled AudioListener, normally on the XR head camera. This tool does not add/remove listeners.");
        }
    }
}
