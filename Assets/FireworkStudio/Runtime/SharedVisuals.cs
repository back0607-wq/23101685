using UnityEngine;

namespace FireworkStudio
{
    public static class SharedVisuals
    {
        static Material _trailMat;
        static Material _alphaMat;

        public static Material TrailMaterial
        {
            get
            {
                if (_trailMat == null) _trailMat = CreateTrailMaterial();
                return _trailMat;
            }
        }

        public static Material AlphaParticleMaterial
        {
            get
            {
                if (_alphaMat == null) _alphaMat = CreateAlphaMaterial();
                return _alphaMat;
            }
        }

        public static Material CreateTrailMaterial()
        {
            var shader = Shader.Find("FireworkStudio/PrettyGlowURP");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", 3.1f);
            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", Color.white);
            return mat;
        }

        public static Material CreateAlphaMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            mat.enableInstancing = true;
            return mat;
        }

        public static Material CreateLitMaterial(Color baseColor, float smoothness = 0.35f, float metallic = 0.06f, float emission = 0f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (emission > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", baseColor * emission);
            }
            return mat;
        }
    }
}
