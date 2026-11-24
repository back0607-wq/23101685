using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using FireworkStudio;

namespace FireworkStudio.EditorTools
{
    public static class FireworkSparklerValidation
    {
        [MenuItem("Tools/Firework Studio/Diagnostics/Validate Four Types (No Playback)")]
        public static void Validate()
        {
            int checks=0;
            var marker=FireworkCollectionInstaller.FindGroup();
            Check(marker!=null,"Create All Four has not been run in this scene.",ref checks);
            int main=0,crackle=0,combined=0,sparkler=0;
            foreach(var prop in marker.GetComponentsInChildren<FireworkProp>(true))
            {
                var grab=prop.GetComponent<XRGrabInteractable>();
                Check(grab!=null,"XR Grab Interactable missing: "+prop.name,ref checks);
                Check(grab.colliders.Count>0,"No pickup collider: "+prop.name,ref checks);
                Check(prop.GetComponent<Rigidbody>()!=null,"Rigidbody missing: "+prop.name,ref checks);
                if(prop.kind==FireworkPropKind.Rocket)
                {
                    if(prop.showMode==FireworkShowMode.TrailsOnly) main++;
                    else if(prop.showMode==FireworkShowMode.CrackleOnly) crackle++;
                    else combined++;
                    continue;
                }
                sparkler++;
                Check(prop.tipPoint!=null&&prop.tipPoint!=prop.transform,"Invalid sparkler tip",ref checks);
                var body=prop.GetComponent<SparklerBody>();
                Check(body!=null&&body.flameAnchor==prop.tipPoint,"Sparkler model/FX anchor mismatch",ref checks);
                Check(body.burnStart!=null&&body.burnEnd!=null,"Burn anchors missing",ref checks);
                Check(prop.GetComponents<SparklerVisual>().Length==1,"Sparkler must have one effect controller",ref checks);
                foreach(var mf in prop.GetComponentsInChildren<MeshFilter>(true))
                    Check(mf.sharedMesh!=null&&!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mf.sharedMesh)),"Mesh not saved as an asset",ref checks);
                foreach(var mr in prop.GetComponentsInChildren<MeshRenderer>(true))
                    Check(mr.sharedMaterial!=null&&!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mr.sharedMaterial)),"Material not saved",ref checks);
            }
            Check(main==1&&crackle==1&&combined==1&&sparkler==1,"Expected one of each of the four types under the generated group",ref checks);
            foreach(string shader in new[]{"SparklerGlow","SparklerSmoke"})
                Check(Resources.Load<Shader>("FireworkSparkler/"+shader)!=null,"Missing shader resource "+shader,ref checks);
            Check(Resources.Load<Texture2D>("FireworkSparkler/SmokeMask")!=null,"Smoke mask missing",ref checks);
            Check(Resources.Load<AudioClip>("FireworkSparkler/CrackleLoop")!=null,"Crackle audio missing",ref checks);
            Check(Resources.Load<AudioClip>("FireworkSparkler/Ignite")!=null,"Ignition audio missing",ref checks);
            var bad=new SparklerOptions { brightness=float.NaN, density=float.PositiveInfinity, writingWidth=-10f, afterglowSeconds=100f };
            var good=bad.CopyClamped();
            Check(good.brightness==1f&&good.density==1f&&good.writingWidth==.002f&&good.afterglowSeconds==.85f,"Sparkler settings must clamp unsafe values",ref checks);
            Debug.Log("[Firework] Passed "+checks+" scene/reference checks. No particles were played. This does not measure rendering quality or HMD frame rate.",marker);
        }
        static void Check(bool ok,string message,ref int count)
        { count++; if(!ok) throw new InvalidOperationException("[Firework validation] "+message); }
    }
}
