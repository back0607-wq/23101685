using UnityEngine;

namespace FireworkStudio
{
    // All geometry anchors are in model space. Never use Renderer.bounds (a world AABB).
    // The visible ember and particle emitter share the same FlameAnchor transform.
    [DisallowMultipleComponent]
    public sealed class SparklerBody : MonoBehaviour
    {
        public Transform burnStart;
        public Transform burnEnd;
        public Transform flameAnchor;
        public Transform unburnedCoating;
        public Transform burnedCoating;
        [HideInInspector] public float coatingDiameter = .0075f;
        [HideInInspector] public float ashDiameter = .0068f;
        public float Progress { get; private set; }

        public void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);
            if(burnStart==null || burnEnd==null || flameAnchor==null) return;
            Vector3 front = Vector3.Lerp(burnStart.position,burnEnd.position,Progress);
            flameAnchor.position = front;
            flameAnchor.rotation = burnStart.rotation;
            Fit(unburnedCoating, burnEnd.position, front, coatingDiameter);
            Fit(burnedCoating, front, burnStart.position, ashDiameter);
        }
        static void Fit(Transform segment, Vector3 worldA, Vector3 worldB, float diameter)
        {
            if(segment==null || segment.parent==null) return;
            Vector3 a=segment.parent.InverseTransformPoint(worldA);
            Vector3 b=segment.parent.InverseTransformPoint(worldB);
            float length=(b-a).magnitude;
            segment.localPosition=(a+b)*.5f;
            if(length>.00001f) segment.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a)/length);
            // Imported mesh is exactly one metre high with diameter one, centred on the origin.
            segment.localScale=new Vector3(diameter,Mathf.Max(.00001f,length),diameter);
            var renderer=segment.GetComponent<Renderer>();
            if(renderer!=null) renderer.enabled=length>.0002f;
        }
    }
}
