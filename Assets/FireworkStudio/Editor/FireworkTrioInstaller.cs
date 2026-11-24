using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using FireworkStudio;
using FireworkStudio.Secondary;

namespace FireworkStudio.EditorTools
{
    public static class FireworkTrioInstaller
    {
        const string Root = "Assets/FireworkStudio";
        const string Generated = Root + "/Generated/Trio";
        static readonly string[] Labels = {"01 - Main Trails", "02 - Crackle Only", "03 - Combined"};
        static readonly FireworkShowMode[] Modes = {FireworkShowMode.TrailsOnly,FireworkShowMode.CrackleOnly,FireworkShowMode.Combined};
        static readonly Color[] Accents = {new Color(.08f,.64f,.94f),new Color(1f,.39f,.07f),new Color(.64f,.15f,.92f)};

        [MenuItem("Tools/Firework Studio/Advanced/Create Three Rockets Only", false, 0)]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorUtility.DisplayDialog("Firework Studio", "Stop Play mode before creating scene props.", "OK"); return; }
            if (PrefabStageUtility.GetCurrentPrefabStage()!=null)
            { EditorUtility.DisplayDialog("Firework Studio", "Open the scene (exit Prefab Mode) before creating the trio.", "OK"); return; }
            EnsureFolder(Generated);
            EnsureShaderReferences();
            FireworkTrioMarker marker = null;
            foreach (var m in UnityEngine.Object.FindObjectsByType<FireworkTrioMarker>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(m.gameObject.scene==SceneManager.GetActiveScene()) { marker=m; break; }
            var template=FindTemplate();
            Undo.IncrementCurrentGroup();
            int undo=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Firework Trio");
            if (marker==null)
            {
                var group=new GameObject("Firework Studio - Three Types");
                Undo.RegisterCreatedObjectUndo(group,"Create Firework Trio");
                marker=group.AddComponent<FireworkTrioMarker>();
                Camera c=Camera.main;
                Vector3 f=c!=null ? Vector3.ProjectOnPlane(c.transform.forward,Vector3.up) : Vector3.forward;
                if(f.sqrMagnitude<.001f) f=Vector3.forward;
                f.Normalize();
                Vector3 centre=c!=null ? c.transform.position+f*1.2f : new Vector3(0,1.15f,1.5f);
                if(c!=null) centre.y=c.transform.position.y+(c.transform.position.y<.6f ? 1.15f : -.35f);
                group.transform.SetPositionAndRotation(centre,Quaternion.LookRotation(f,Vector3.up));
            }
            int created=0;
            for(int i=0;i<3;i++)
            {
                Transform child=marker.transform.Find(Labels[i]);
                FireworkProp prop;
                if(child==null)
                {
                    prop=MakeRocket(marker.transform,Labels[i],(i-1)*.55f,Accents[i],template);
                    prop.showMode=Modes[i];
                    created++;
                    Undo.RegisterCreatedObjectUndo(prop.gameObject,"Create " + Labels[i]);
                    CreateLabel(marker.transform,i);
                }
                else
                {
                    prop=child.GetComponent<FireworkProp>();
                    if(prop==null) { Debug.LogWarning("[Firework] Existing child has no FireworkProp; left untouched: " + child.name,child); continue; }
                    Undo.RecordObject(prop,"Set firework role");
                    prop.showMode=Modes[i];
                }
                EditorUtility.SetDirty(prop);
                string prefab=Generated+"/"+(i==0?"MainTrails":i==1?"CrackleOnly":"Combined")+".prefab";
                // Do not overwrite prefabs the user may already have customized.
                if(!File.Exists(prefab)) PrefabUtility.SaveAsPrefabAsset(prop.gameObject,prefab);
            }
            EnsureCombinedSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
            Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject=marker.gameObject;
            Debug.Log("[Firework] Trio ready. Blue=Main Trails, Orange=Crackle Only, Purple=Combined. Created="+created+
                ". Existing props, XR Origin, flight target and post-processing were not replaced. Save the scene.",marker);
        }

        static FireworkProp FindTemplate()
        {
            var chosen=Selection.activeGameObject!=null ? Selection.activeGameObject.GetComponent<FireworkProp>() : null;
            if(chosen!=null && chosen.kind==FireworkPropKind.Rocket && chosen.GetComponentInParent<FireworkTrioMarker>()==null) return chosen;
            foreach(var p in UnityEngine.Object.FindObjectsByType<FireworkProp>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(p.kind==FireworkPropKind.Rocket && p.gameObject.scene==SceneManager.GetActiveScene() && p.GetComponentInParent<FireworkTrioMarker>()==null) return p;
            return null;
        }

        static FireworkProp MakeRocket(Transform parent,string label,float x,Color accent,FireworkProp template)
        {
            var root=new GameObject(label);
            root.transform.SetParent(parent,false);
            root.transform.localPosition=new Vector3(x,0,0);
            var visual=new GameObject("Visual"); visual.transform.SetParent(root.transform,false);
            Part(visual.transform,"Stick",PrimitiveType.Cylinder,new Vector3(0,-.20f,0),new Vector3(.006f,.33f,.006f),MaterialAsset("Wood",new Color(.76f,.63f,.40f),.35f,0f));
            Part(visual.transform,"Body",PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(.045f,.15f,.045f),MaterialAsset("Red Paper",new Color(.8f,.15f,.18f),.28f,.04f));
            Material band=MaterialAsset("Band " + label,accent,.42f,.08f);
            Part(visual.transform,"CapTop",PrimitiveType.Cylinder,new Vector3(0,.21f,0),new Vector3(.047f,.018f,.047f),band);
            Part(visual.transform,"CapBottom",PrimitiveType.Cylinder,new Vector3(0,-.05f,0),new Vector3(.047f,.018f,.047f),band);
            Part(visual.transform,"Nose",PrimitiveType.Capsule,new Vector3(0,.26f,0),new Vector3(.026f,.04f,.026f),MaterialAsset("Silver Cap",new Color(.89f,.87f,.90f),.26f,.02f));
            Transform ignition=Point(visual.transform,"IgnitionDirection",new Vector3(0,.29f,0));
            Transform tail=Point(visual.transform,"Tail",new Vector3(0,-.06f,0));
            Transform attach=Point(root.transform,"Grip Attach",new Vector3(0,-.29f,0));
            var rb=root.AddComponent<Rigidbody>(); rb.mass=.15f; rb.useGravity=false; rb.isKinematic=true;
            rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative; rb.interpolation=RigidbodyInterpolation.Interpolate;
            var collider=root.AddComponent<CapsuleCollider>(); collider.direction=1; collider.radius=.045f; collider.height=.86f; collider.center=new Vector3(0,-.115f,0);
            var grab=root.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear(); grab.colliders.Add(collider);
            grab.attachTransform=attach; grab.throwOnDetach=false; grab.useDynamicAttach=true;
            if(template!=null)
            {
                var other=template.GetComponent<XRGrabInteractable>();
                if(other!=null) { grab.interactionLayers=other.interactionLayers; grab.movementType=other.movementType; }
            }
            var prop=root.AddComponent<FireworkProp>(); prop.kind=FireworkPropKind.Rocket;
            prop.ignitionPoint=ignition; prop.tailPoint=tail;
            if(template!=null)
            {
                prop.randomPattern=template.randomPattern; prop.fixedPattern=template.fixedPattern;
                prop.randomPalette=template.randomPalette; prop.fixedPalette=template.fixedPalette;
                prop.palette=template.palette; prop.useCustomColor=template.useCustomColor; prop.customColor=template.customColor;
                prop.minimumBurstHeightAboveLaunch=template.minimumBurstHeightAboveLaunch;
                prop.rocketSpeed=template.rocketSpeed; prop.rocketFlightSeconds=template.rocketFlightSeconds;
                prop.rocketShotCooldown=template.rocketShotCooldown;
            }
            return prop;
        }

        static void Part(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name;
            go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=size;
            var col=go.GetComponent<Collider>(); if(col!=null) UnityEngine.Object.DestroyImmediate(col);
            go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        static Transform Point(Transform parent,string label,Vector3 pos)
        { var go=new GameObject(label); go.transform.SetParent(parent,false); go.transform.localPosition=pos; return go.transform; }

        static Material MaterialAsset(string label,Color color,float smooth,float metal)
        {
            string path=Generated+"/"+label+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            material=SharedVisuals.CreateLitMaterial(color,smooth,metal);
            material.name=label; AssetDatabase.CreateAsset(material,path); return material;
        }
        static void CreateLabel(Transform parent,int i)
        {
            string name="Label "+i;
            if(parent.Find(name)!=null) return;
            Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/TrioLabel"+i+".png");
            var shader=Shader.Find("FireworkStudio/TrioLabel");
            if(tex==null||shader==null) return;
            string path=Generated+"/Label"+i+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) { mat=new Material(shader); mat.SetTexture("_MainTex",tex); AssetDatabase.CreateAsset(mat,path); }
            Part(parent,name,PrimitiveType.Quad,new Vector3((i-1)*.55f,-.60f,0),new Vector3(.46f,.115f,1),mat);
        }
        static void EnsureCombinedSettings()
        {
            var active=UnityEngine.Object.FindFirstObjectByType<AfterburstSettings>();
            if(active!=null) return; // Preserve the user's terminal timing/brightness, including an intentional off switch.
            var go=new GameObject("Firework Afterburst Settings");
            Undo.RegisterCreatedObjectUndo(go,"Create combined settings");
            var setting=go.AddComponent<AfterburstSettings>();
            setting.terminalClusters=22; setting.sparksPerCluster=9; setting.terminalSpreadDelay=.50f;
        }

        // Referenced assets in Resources keep Shader.Find-only shaders in player builds.
        public static void EnsureShaderReferences()
        {
            string path=Root+"/Resources/FireworkShaderReferences";
            EnsureFolder(path);
            string[] names={"FireworkStudio/PrettyGlowURP","FireworkStudio/SoftStreakURP","FireworkStudio/IlluminatedSmokeURP",
                "FireworkStudio.Secondary/SoftAdditiveGlint","FireworkStudio/CrackleOnlyGlow","FireworkStudio/TrioLabel"};
            for(int i=0;i<names.Length;i++)
            {
                Shader shader=Shader.Find(names[i]);
                if(shader==null) throw new InvalidOperationException("Missing package shader: "+names[i]);
                string asset=path+"/Reference"+i+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(asset);
                if(mat==null) AssetDatabase.CreateAsset(new Material(shader),asset);
                else if(mat.shader!=shader) { mat.shader=shader; EditorUtility.SetDirty(mat); }
            }
        }
        public static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
