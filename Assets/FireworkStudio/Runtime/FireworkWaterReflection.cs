using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireworkStudio
{
    [DisallowMultipleComponent]
    public sealed class FireworkWaterReflection : MonoBehaviour
    {
        const int MaxPulses = 64;
        [SerializeField] Shader rippleShader;
        [SerializeField] float waterLevel;
        [SerializeField, Range(0f, 2f)] float strength = 1f;

        struct Pulse
        {
            public Vector3 top;
            public Color color;
            public float width;
            public float length;
            public float started;
            public float duration;
            public bool burst;
        }

        static FireworkWaterReflection active;
        readonly List<Pulse> pulses = new(MaxPulses);
        readonly List<Vector3> vertices = new(MaxPulses * 4);
        readonly List<Vector2> uv = new(MaxPulses * 4);
        readonly List<Color> colors = new(MaxPulses * 4);
        readonly List<int> triangles = new(MaxPulses * 6);
        GameObject surface;
        Mesh mesh;
        Material material;

        void OnEnable()
        {
            if (rippleShader != null)
                active = this;
        }

        void OnDisable()
        {
            if (active == this)
                active = null;
            pulses.Clear();
            if (surface != null)
                surface.SetActive(false);
        }

        void OnDestroy()
        {
            Destroy(mesh);
            Destroy(material);
        }

        public static void ReflectBurst(Vector3 position, Color tint) => active?.Add(position, tint, true);
        public static void ReflectCrackle(Vector3 position, Color tint) => active?.Add(position, tint, false);

        void Add(Vector3 position, Color tint, bool burst)
        {
            if (strength <= 0f || position.y <= waterLevel)
                return;

            if (pulses.Count == MaxPulses)
            {
                // Keep a broad burst reflection even when many small crackles overlap.
                int replace = pulses.FindIndex(pulse => !pulse.burst);
                if (replace < 0 && !burst)
                    return;
                pulses.RemoveAt(Mathf.Max(0, replace));
            }

            float altitude = position.y - waterLevel;
            float length = burst ? Mathf.Clamp(altitude * 0.58f, 8f, 22f) : 3.8f;
            position.y = waterLevel - (burst ? 1.2f : 1.5f + altitude * 0.38f);
            pulses.Add(new Pulse
            {
                top = position, color = tint, width = burst ? 15f : 3.8f,
                length = length, duration = burst ? 1.45f : 0.36f,
                started = Time.time, burst = burst
            });
        }

        void LateUpdate()
        {
            float now = Time.time;
            for (int i = pulses.Count - 1; i >= 0; i--)
                if (now >= pulses[i].started + pulses[i].duration)
                    pulses.RemoveAt(i);

            Camera camera = pulses.Count > 0 ? Camera.main : null;
            if (camera == null)
            {
                if (surface != null)
                    surface.SetActive(false);
                return;
            }
            EnsureSurface();
            surface.SetActive(true);
            Vector3 eye = camera.transform.position;
            vertices.Clear();
            uv.Clear();
            colors.Clear();
            triangles.Clear();
            foreach (var pulse in pulses)
                AddQuad(pulse, eye, now);

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
        }

        void EnsureSurface()
        {
            if (surface != null)
                return;
            surface = new GameObject("Firework reflection ribbons");
            surface.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Firework reflections" };
            mesh.MarkDynamic();
            material = new Material(rippleShader);
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        void AddQuad(Pulse pulse, Vector3 eye, float now)
        {
            float progress = (now - pulse.started) / pulse.duration;
            Color tint = pulse.color;
            tint.a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress * 15f))
                * Mathf.Pow(1f - progress, pulse.burst ? 1.7f : 1.2f)
                * (pulse.burst ? 1.15f : 0.64f) * strength;

            Vector3 facing = Vector3.ProjectOnPlane(pulse.top - eye, Vector3.up);
            Vector3 right = Vector3.Cross(Vector3.up, facing.normalized);
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.right;
            right *= pulse.width * (0.5f + 0.11f * progress);
            Vector3 bottom = pulse.top + Vector3.down * pulse.length;

            // The water is painted into the skybox, so the ribbon extends into the virtual
            // reflected image below the waterline. Scene geometry still occludes it.
            int first = vertices.Count;
            AddVertex(pulse.top - right, 0f, 0f, tint);
            AddVertex(pulse.top + right, 1f, 0f, tint);
            AddVertex(bottom + right, 1f, 1f, tint);
            AddVertex(bottom - right, 0f, 1f, tint);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        void AddVertex(Vector3 position, float x, float y, Color tint)
        {
            vertices.Add(transform.InverseTransformPoint(position));
            uv.Add(new Vector2(x, y));
            colors.Add(tint);
        }
    }
}
