Shader "FireworkStudio/CrackleOnlyGlow"
{
    Properties { _Intensity("HDR Emission",Range(0,12))=5.8 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {
                float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct V {
                float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
            CBUFFER_END
            V vert(A a)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(a);
                o=(V)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(a.positionOS.xyz);
                o.color=a.color; o.uv=a.uv;
                return o;
            }
            half4 frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=i.uv*2-1;
                float r2=dot(p,p);
                half core=exp2(-r2*22);
                half halo=exp2(-r2*5)*.20;
                half crossGlow=(exp2(-p.x*p.x*170-p.y*p.y*14)+exp2(-p.y*p.y*170-p.x*p.x*14))*.10;
                half edge=1-smoothstep(.70,1.0,max(abs(p.x),abs(p.y)));
                half alpha=saturate((core+halo+crossGlow)*edge*i.color.a);
                // HDR lives in the material, not in clamped Color32 particle channels.
                return half4(i.color.rgb*_Intensity,alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
