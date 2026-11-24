Shader "FireworkStudio/SparklerGlow"
{
    Properties
    {
        _Intensity("HDR Emission",Range(0,16))=6
        _Shape("0 Point / 1 Ribbon / 2 Glint",Float)=0
    }
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
            struct A { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
                half _Intensity; half _Shape;
            CBUFFER_END
            V vert(A v)
            {
                V o=(V)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.color=v.color; o.uv=v.uv;
                return o;
            }
            half4 frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=i.uv*2-1;
                float r2=dot(p,p);
                float pointGlow=exp2(-r2*24)+.19*exp2(-r2*4.8);
                float edge=1-smoothstep(.70,1.0,max(abs(p.x),abs(p.y)));
                float crossed=(exp2(-p.x*p.x*185-p.y*p.y*17)+exp2(-p.y*p.y*185-p.x*p.x*17))*.16;
                float ribbon=(exp2(-p.y*p.y*30)+.15*exp2(-p.y*p.y*4.8));
                ribbon*=1-smoothstep(.68,1.0,abs(p.y));
                ribbon*=smoothstep(0,.025,i.uv.x)*smoothstep(0,.05,1-i.uv.x);
                float mask=_Shape>.5 && _Shape<1.5 ? ribbon : (pointGlow+(_Shape>1.5?crossed:0))*edge;
                // HDR is material-side. Particle Color32 stores only normalized chroma/alpha.
                return half4(max(i.color.rgb,0)*_Intensity,saturate(mask*i.color.a));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
