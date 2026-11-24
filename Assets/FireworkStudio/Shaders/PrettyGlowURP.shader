Shader "FireworkStudio/PrettyGlowURP"
{
    Properties { _Tint("Tint",Color)=(1,1,1,1) _Intensity("Intensity",Range(0,8))=2 _StarAmount("Starlight",Range(0,1))=0 }
    SubShader
    {
      Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
      Pass
      {
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
            #pragma multi_compile_instancing
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct A { float4 pos:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0;  UNITY_VERTEX_INPUT_INSTANCE_ID
};
        struct V { float4 pos:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0;  UNITY_VERTEX_OUTPUT_STEREO
};
        CBUFFER_START(UnityPerMaterial)
        half4 _Tint; half _Intensity; half _StarAmount;
        CBUFFER_END
        V vert(A a){V o; UNITY_SETUP_INSTANCE_ID(a); o=(V)0; UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=TransformObjectToHClip(a.pos.xyz);o.color=a.color;o.uv=a.uv;return o;}
        half4 frag(V v):SV_Target
        {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
          float2 p=(v.uv-0.5)*2;
          float r=dot(p,p);
          half core=exp2(-r*13);
          half halo=exp2(-r*3)*0.24;
          half star = exp2(-p.x*p.x*110-p.y*p.y*2.8) + exp2(-p.y*p.y*110-p.x*p.x*2.8);
          half edge = 1-smoothstep(0.70,1.0,max(abs(p.x),abs(p.y)));
          half alpha=saturate((core+halo+star*_StarAmount*0.7)*edge*v.color.a*_Tint.a);
          return half4(v.color.rgb*_Tint.rgb*_Intensity,alpha);
        }
        ENDHLSL
      }
    }
    Fallback Off
}
