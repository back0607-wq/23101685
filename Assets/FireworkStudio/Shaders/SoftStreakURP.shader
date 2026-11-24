Shader "FireworkStudio/SoftStreakURP"
{
    Properties { _Tint ("Tint", Color) = (1,1,1,1) _Intensity ("Intensity", Range(0,5)) = 1.5 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0;  UNITY_VERTEX_INPUT_INSTANCE_ID
};
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0;  UNITY_VERTEX_OUTPUT_STEREO
};
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Intensity;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); o=(Varyings)0; UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // A soft, narrow luminous filament; transparent edges, no rectangular ribbon.
                half across = abs(i.uv.y * 2.0h - 1.0h);
                half core = exp2(-across * across * 17.0h);
                half halo = exp2(-across * across * 5.0h) * 0.15h;
                half opacity = saturate(i.color.a * _Tint.a * (core + halo));
                return half4(i.color.rgb * _Tint.rgb * _Intensity, opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
