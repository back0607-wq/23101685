using UnityEditor;
using UnityEngine;
using FireworkStudio;

namespace FireworkStudio.EditorTools
{
    [CustomEditor(typeof(FireworkProp)), CanEditMultipleObjects]
    public sealed class FireworkPropInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool allSparklers=true, allRockets=true;
            foreach(var t in targets)
            {
                allSparklers&=((FireworkProp)t).kind==FireworkPropKind.Sparkler;
                allRockets&=((FireworkProp)t).kind==FireworkPropKind.Rocket;
            }
            if(allSparklers)
            {
                EditorGUILayout.HelpBox("SPARKLER: Grip to hold, Trigger to ignite. The coating burns down for the selected duration, then can be relit. No sky projectile or terminal crackle. Smoke, sound and the local light can each be disabled.",MessageType.Info);
                foreach(string key in new[]{"kind","tipPoint","sparklerBurnSeconds","sparklerOptions","hoverAmplitude","hoverSpeed","rotateSpeed"})
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(key),true);
            }
            else
            {
                if(allRockets)
                {
                    var prop=(FireworkProp)target;
                    string message=prop.showMode==FireworkShowMode.CrackleOnly
                        ? "CRACKLE ONLY: independent shaped microbursts. No main streaks or old afterburst."
                        : prop.showMode==FireworkShowMode.TrailsOnly
                        ? "MAIN TRAILS ONLY: existing main burst. No secondary burst or terminal crackle."
                        : "COMBINED: existing main burst + afterburst + terminal crackle. Scene Afterburst Settings still apply.";
                    EditorGUILayout.HelpBox(message,MessageType.Info);
                }
                if(allRockets) DrawPropertiesExcluding(serializedObject,"m_Script","crackleOnly","sparklerOptions","sparklerBurnSeconds");
                else DrawPropertiesExcluding(serializedObject,"m_Script","crackleOnly");
                bool showOptions=false;
                foreach(var t in targets)
                    if(((FireworkProp)t).kind==FireworkPropKind.Rocket&&((FireworkProp)t).showMode==FireworkShowMode.CrackleOnly) showOptions=true;
                if(showOptions) EditorGUILayout.PropertyField(serializedObject.FindProperty("crackleOnly"),true);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
