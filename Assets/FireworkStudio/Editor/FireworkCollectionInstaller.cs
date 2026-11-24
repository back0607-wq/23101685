using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using FireworkStudio;

namespace FireworkStudio.EditorTools
{
    public static class FireworkCollectionInstaller
    {
        const string Root="Assets/FireworkStudio";
        const string AssetsFolder=Root+"/Generated/Sparkler";
        const string SparklerName="04 - Sparkler";

        [MenuItem("Tools/Firework Studio/0 - Create All Four", false, 0)]
        public static void Install()
        {
            if(!CanInstall()) return;
            // Fail early before changing the scene if the imported assets are incomplete.
            Preflight();
            FireworkTrioMarker before=FindGroup();
            bool newGroup=before==null;
            FireworkTrioInstaller.Install();
            var marker=FindGroup();
            if(marker==null) throw new InvalidOperationException("Cannot find the generated firework group.");
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Sparkler to Firework Collection");
            Undo.RecordObject(marker.gameObject,"Name firework collection");
            marker.gameObject.name="Firework Studio - Four Types";
            Undo.RecordObject(marker,"Set collection version"); marker.schema=2;
            if(newGroup)
            {
                // Centre a fresh four-item row. Existing user-placed rockets are never moved.
                Undo.RecordObject(marker.transform,"Centre new collection");
                marker.transform.position-=marker.transform.right*.275f;
            }
            FireworkTrioInstaller.EnsureFolder(AssetsFolder);
            Transform existing=marker.transform.Find(SparklerName);
            if(existing==null)
            {
                var p=CreateSparkler(marker.transform,new Vector3(1.10f,0,0),FindRocket(marker));
                Undo.RegisterCreatedObjectUndo(p.gameObject,"Create sparkler");
                string prefab=AssetsFolder+"/Sparkler.prefab";
                if(!File.Exists(prefab)) PrefabUtility.SaveAsPrefabAsset(p.gameObject,prefab);
            }
            else if(existing.GetComponent<SparklerBody>()==null)
                Debug.LogWarning("[Firework] Existing 04 - Sparkler has no burn markers. It was not replaced. Use a fresh scene or rename that object before generating a new sparkler.",existing);
            CreateLabel(marker.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject=marker.gameObject;
            Debug.Log("[Firework] Four types ready: Main Trails / Crackle Only / Combined / Sparkler. Grip to grab, Trigger to ignite. Existing rocket effects and post-processing were not changed. Save your scene.",marker);
        }

        [MenuItem("Tools/Firework Studio/1 - Add Sparkler Only", false, 1)]
        public static void AddSparklerOnly()
        {
            if(!CanInstall()) return;
            Preflight();
            var marker=FindGroup();
            if(marker!=null)
            {
                if(marker.transform.Find(SparklerName)!=null)
                { Selection.activeGameObject=marker.transform.Find(SparklerName).gameObject; return; }
                var p=CreateSparkler(marker.transform,new Vector3(1.10f,0,0),FindRocket(marker));
                Undo.RegisterCreatedObjectUndo(p.gameObject,"Add sparkler");
                CreateLabel(marker.transform); Selection.activeGameObject=p.gameObject;
                EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
            }
            else
            {
                // Do not silently generate rockets when the user chose Sparkler Only.
                foreach(var existingRoot in SceneManager.GetActiveScene().GetRootGameObjects())
                    if(existingRoot.name=="Firework Studio - Sparkler Display" && existingRoot.transform.Find(SparklerName)!=null)
                    { Selection.activeGameObject=existingRoot.transform.Find(SparklerName).gameObject; return; }
                var root=new GameObject("Firework Studio - Sparkler Display");
                Undo.RegisterCreatedObjectUndo(root,"Create sparkler display");
                var cam=Camera.main;
                var f=cam!=null ? Vector3.ProjectOnPlane(cam.transform.forward,Vector3.up) : Vector3.forward;
                if(f.sqrMagnitude<.001f) f=Vector3.forward;
                f.Normalize();
                root.transform.position=cam!=null ? cam.transform.position+f*1.15f : new Vector3(0,1.2f,1.5f);
                if(cam!=null) { var pos=root.transform.position; pos.y+=cam.transform.position.y<.6f ? 1.15f : -.35f; root.transform.position=pos; }
                root.transform.rotation=Quaternion.LookRotation(f,Vector3.up);
                var p=CreateSparkler(root.transform,Vector3.zero,null);
                Selection.activeGameObject=p.gameObject;
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
            AssetDatabase.SaveAssets();
        }
        static bool CanInstall()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null)
            { EditorUtility.DisplayDialog("Firework Studio","Stop Play mode and exit Prefab Mode before installing.","OK"); return false; }
            return true;
        }
        static void Preflight()
        {
            if(LoadCylinder()==null) throw new InvalidOperationException("Missing Models/Sparkler/SparklerCylinder.obj. Extract and install the full ZIP first.");
            if(Resources.Load<Shader>("FireworkSparkler/SparklerGlow")==null || Resources.Load<Shader>("FireworkSparkler/SparklerSmoke")==null)
                throw new InvalidOperationException("Missing sparkler shaders in Resources/FireworkSparkler.");
        }
        public static FireworkTrioMarker FindGroup()
        {
            foreach(var g in UnityEngine.Object.FindObjectsByType<FireworkTrioMarker>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(g.gameObject.scene==SceneManager.GetActiveScene()) return g;
            return null;
        }
        static FireworkProp FindRocket(FireworkTrioMarker group)
        {
            foreach(var p in group.GetComponentsInChildren<FireworkProp>(true)) if(p.kind==FireworkPropKind.Rocket) return p;
            return null;
        }
        static Mesh LoadCylinder()
        {
            foreach(var a in AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/Sparkler/SparklerCylinder.obj"))
                if(a is Mesh mesh) return mesh;
            return null;
        }
        static Transform Point(Transform parent,string name,Vector3 position)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; return go.transform; }
        static Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size;
            go.AddComponent<MeshFilter>().sharedMesh=LoadCylinder();
            var r=go.AddComponent<MeshRenderer>(); r.sharedMaterial=material;
            return go.transform;
        }
        static Material Mat(string name,Color color,float rough,float metallic,Texture2D map=null)
        {
            string path=AssetsFolder+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m!=null) return m;
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null) throw new InvalidOperationException("This package requires the Universal Render Pipeline.");
            m=new Material(shader) { name=name };
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",rough); m.SetFloat("_Metallic",metallic);
            if(map!=null) m.SetTexture("_BaseMap",map);
            AssetDatabase.CreateAsset(m,path); return m;
        }
        static FireworkProp CreateSparkler(Transform parent,Vector3 position,FireworkProp rocket)
        {
            FireworkTrioInstaller.EnsureFolder(AssetsFolder);
            var root=new GameObject(SparklerName); root.transform.SetParent(parent,false); root.transform.localPosition=position;
            var visual=Point(root.transform,"Visual",Vector3.zero);
            Texture2D grain=Resources.Load<Texture2D>("FireworkSparkler/FuelGrain");
            var rod=Mat("Brushed steel",new Color(.55f,.58f,.62f),.67f,.78f);
            var fuel=Mat("Textured gray coating",new Color(.82f,.78f,.70f),.14f,.02f,grain);
            var ash=Mat("Charcoal ash",new Color(.22f,.21f,.20f),.07f,.01f,grain);
            var grip=Mat("Matte wrapped grip",new Color(.075f,.071f,.067f),.24f,.05f);
            var gold=Mat("Gold collar",new Color(.91f,.58f,.16f),.5f,.52f);
            Part(visual,"Shaft",Vector3.zero,new Vector3(.0035f,.44f,.0035f),rod);
            Part(visual,"Grip",new Vector3(0,-.1625f,0),new Vector3(.0105f,.095f,.0105f),grip);
            // Subtle physical grip rings, not a gun/launcher silhouette.
            for(int i=0;i<5;i++)
                Part(visual,"Grip ring "+i,new Vector3(0,-.195f+i*.016f,0),new Vector3(.0108f,.0016f,.0108f),grip);
            Part(visual,"Collar",new Vector3(0,-.11f,0),new Vector3(.008f,.007f,.008f),gold);
            var unburned=Part(visual,"Unburned coating",new Vector3(0,.1075f,0),new Vector3(.0075f,.225f,.0075f),fuel);
            var burned=Part(visual,"Burned coating",new Vector3(0,.22f,0),new Vector3(.0068f,.00001f,.0068f),ash);
            var top=Point(visual,"Burn Start",new Vector3(0,.22f,0));
            var bottom=Point(visual,"Burn End",new Vector3(0,-.005f,0));
            var tip=Point(visual,"Tip",new Vector3(0,.22f,0));
            var attach=Point(root.transform,"Grip Attach",new Vector3(0,-.1625f,0));
            var rb=root.AddComponent<Rigidbody>(); rb.mass=.04f; rb.useGravity=false; rb.isKinematic=true;
            rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var c=root.AddComponent<CapsuleCollider>(); c.direction=1; c.radius=.025f; c.height=.45f;
            var grab=root.AddComponent<XRGrabInteractable>(); grab.colliders.Clear(); grab.colliders.Add(c);
            grab.attachTransform=attach; grab.useDynamicAttach=false; grab.throwOnDetach=false;
            grab.movementType=XRBaseInteractable.MovementType.Kinematic;
            if(rocket!=null)
            { var other=rocket.GetComponent<XRGrabInteractable>(); if(other!=null) grab.interactionLayers=other.interactionLayers; }
            var b=root.AddComponent<SparklerBody>(); b.burnStart=top; b.burnEnd=bottom;
            b.flameAnchor=tip; b.unburnedCoating=unburned; b.burnedCoating=burned; b.SetProgress(0);
            var prop=root.AddComponent<FireworkProp>(); prop.kind=FireworkPropKind.Sparkler; prop.tipPoint=tip;
            prop.sparklerBurnSeconds=24f; prop.sparklerOptions=new SparklerOptions();
            prop.hoverAmplitude=.035f; prop.hoverSpeed=1.3f; prop.rotateSpeed=18f;
            var v=root.AddComponent<SparklerVisual>(); v.tip=tip; v.autoPlay=false;
            return prop;
        }
        static void CreateLabel(Transform parent)
        {
            if(parent.Find("Label 3")!=null) return;
            Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SparklerLabel.png");
            var shader=Shader.Find("FireworkStudio/TrioLabel");
            if(tex==null||shader==null) return;
            var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetsFolder+"/SparklerLabel.mat");
            if(mat==null) { mat=new Material(shader); mat.SetTexture("_MainTex",tex); AssetDatabase.CreateAsset(mat,AssetsFolder+"/SparklerLabel.mat"); }
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name="Label 3"; go.transform.SetParent(parent,false);
            go.transform.localPosition=new Vector3(1.1f,-.6f,0); go.transform.localScale=new Vector3(.46f,.115f,1);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial=mat;
            Undo.RegisterCreatedObjectUndo(go,"Create sparkler label");
        }
    }
}
