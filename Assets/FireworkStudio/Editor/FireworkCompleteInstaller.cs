using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FireworkStudio;

public static class FireworkCompleteInstaller
{
    [MenuItem("Tools/Firework Studio/Advanced/Create Original Rocket and Sparkler")]
    public static void InstallCompleteFireworks()
    {
        var root = GameObject.Find("Firework Studio") ?? new GameObject("Firework Studio");
        Undo.RegisterCreatedObjectUndo(root, "Install Firework Studio Props");

        if (root.transform.Find("Firework Studio Sparkler") == null)
            CreateSparkler(root.transform, new Vector3(-0.42f, 1.22f, 1.7f));
        if (root.transform.Find("Firework Studio Rocket") == null)
            CreateRocket(root.transform, new Vector3(0.42f, 1.22f, 1.7f));
        EnsureVolume();

        Selection.activeGameObject = root;
        Debug.Log("[Firework Studio] Installed complete standalone props + bloom volume.");
    }

    [MenuItem("Tools/Firework Studio/Rendering/Add or Refresh Bloom Volume")]
    public static void EnsureVolume()
    {
        var volumeGo = GameObject.Find("Firework Studio PostFX") ?? new GameObject("Firework Studio PostFX");
        var volume = volumeGo.GetComponent<Volume>() ?? volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;

        if (volume.sharedProfile == null)
            volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();

        ConfigureProfile(volume.sharedProfile);
    }

    static void ConfigureProfile(VolumeProfile profile)
    {
        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.78f);
        bloom.threshold.Override(0.92f);
        bloom.scatter.Override(0.78f);
        bloom.tint.Override(Color.white);

        if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.15f);
        colorAdjustments.contrast.Override(10f);
        colorAdjustments.saturation.Override(5f);
    }

    static void CreateSparkler(Transform parent, Vector3 pos)
    {
        var root = new GameObject("Firework Studio Sparkler");
        root.transform.SetParent(parent, false);
        root.transform.position = pos;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(visual.transform, false);
        shaft.transform.localScale = new Vector3(0.005f, 0.20f, 0.005f);
        shaft.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.70f, 0.72f, 0.76f), 0.75f, 0.03f, 0.15f);

        var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        grip.name = "Grip";
        grip.transform.SetParent(visual.transform, false);
        grip.transform.localPosition = new Vector3(0f, -0.16f, 0f);
        grip.transform.localScale = new Vector3(0.012f, 0.055f, 0.012f);
        grip.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.18f, 0.18f, 0.19f), 0.45f, 0.0f, 0f);

        var tip = new GameObject("Tip");
        tip.transform.SetParent(visual.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0.205f, 0f);

        AddInteractable(root, FireworkPropKind.Sparkler, visual.transform, tip.transform, null, FireworkPalette.Ice, 0.03f, 0.42f);
    }

    static void CreateRocket(Transform parent, Vector3 pos)
    {
        var root = new GameObject("Firework Studio Rocket");
        root.transform.SetParent(parent, false);
        root.transform.position = pos;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "Stick";
        stick.transform.SetParent(visual.transform, false);
        stick.transform.localScale = new Vector3(0.006f, 0.33f, 0.006f);
        stick.transform.localPosition = new Vector3(0f, -0.20f, 0f);
        stick.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.76f, 0.63f, 0.40f), 0.45f, 0.0f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(visual.transform, false);
        body.transform.localScale = new Vector3(0.045f, 0.15f, 0.045f);
        body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        body.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.80f, 0.15f, 0.18f), 0.28f, 0.04f, 0f);

        var capTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        capTop.name = "CapTop";
        capTop.transform.SetParent(visual.transform, false);
        capTop.transform.localScale = new Vector3(0.047f, 0.018f, 0.047f);
        capTop.transform.localPosition = new Vector3(0f, 0.21f, 0f);
        capTop.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.98f, 0.78f, 0.24f), 0.42f, 0.08f, 0f);

        var capBottom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        capBottom.name = "CapBottom";
        capBottom.transform.SetParent(visual.transform, false);
        capBottom.transform.localScale = new Vector3(0.047f, 0.018f, 0.047f);
        capBottom.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        capBottom.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.98f, 0.78f, 0.24f), 0.42f, 0.08f, 0f);

        var nose = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        nose.name = "Nose";
        nose.transform.SetParent(visual.transform, false);
        nose.transform.localScale = new Vector3(0.026f, 0.04f, 0.026f);
        nose.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        nose.GetComponent<MeshRenderer>().sharedMaterial = SharedVisuals.CreateLitMaterial(new Color(0.89f, 0.87f, 0.90f), 0.26f, 0.02f, 0f);

        var ignition = new GameObject("IgnitionDirection");
        ignition.transform.SetParent(visual.transform, false);
        ignition.transform.localPosition = new Vector3(0f, 0.29f, 0f);
        ignition.transform.localRotation = Quaternion.identity;

        var tail = new GameObject("Tail");
        tail.transform.SetParent(visual.transform, false);
        tail.transform.localPosition = new Vector3(0f, -0.06f, 0f);

        AddInteractable(root, FireworkPropKind.Rocket, visual.transform, ignition.transform, tail.transform, FireworkPalette.Mixed, 0.05f, 0.78f);
    }

    static void AddInteractable(GameObject go, FireworkPropKind kind, Transform visualRoot, Transform pointA, Transform pointB, FireworkPalette palette, float colliderRadius, float colliderHeight)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var col = go.AddComponent<CapsuleCollider>();
        col.radius = colliderRadius;
        col.height = colliderHeight;
        col.direction = 1;

        go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        var prop = go.AddComponent<FireworkProp>();
        prop.kind = kind;
        prop.palette = palette;
        if (kind == FireworkPropKind.Sparkler)
        {
            prop.tipPoint = pointA;
            prop.hoverAmplitude = 0.04f;
            prop.rotateSpeed = 22f;
        }
        else
        {
            prop.ignitionPoint = pointA;
            prop.tailPoint = pointB;
            prop.hoverAmplitude = 0.05f;
            prop.rotateSpeed = 28f;
        }
    }
}
