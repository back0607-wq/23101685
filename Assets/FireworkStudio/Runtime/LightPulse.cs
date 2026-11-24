using UnityEngine;

namespace FireworkStudio
{
    public class LightPulse : MonoBehaviour
    {
        public float baseIntensity = 2f;
        public float amount = 0.35f;
        public float speed = 16f;

        Light _light;
        float _offset;

        void Awake()
        {
            _light = GetComponent<Light>();
            _offset = Random.Range(0f, 100f);
        }

        void Update()
        {
            if (_light == null) return;
            _light.intensity = baseIntensity + Mathf.Sin(Time.time * speed + _offset) * amount + Random.Range(-amount * 0.15f, amount * 0.15f);
        }
    }
}
