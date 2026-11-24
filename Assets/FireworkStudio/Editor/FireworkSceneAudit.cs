using UnityEditor;
using UnityEngine;
using FireworkStudio;

public static class FireworkSceneAudit
{
    [MenuItem("Tools/Firework Studio/Diagnostics/List Scene Fireworks")]
    public static void Report()
    {
        var props = Object.FindObjectsByType<FireworkProp>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("[Firework AUDIT] FireworkProp instances=" + props.Length);
        foreach (var prop in props)
        {
            var go = prop.gameObject;
            string path = go.name;
            var t = go.transform.parent;
            while (t != null) { path = t.name + "/" + path; t = t.parent; }
            Debug.Log($"[Firework AUDIT] {path}, active={go.activeInHierarchy}, kind={prop.kind}, position={go.transform.position:F2}, minSkyHeight={prop.minimumBurstHeightAboveLaunch}", go);
        }
        var legacy = Object.FindObjectsByType<PrettyProjectileFlight>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("[Firework AUDIT] Active projectile scripts=" + legacy.Length);
    }
}
