Shader "FireworkStudio/IlluminatedSmokeURP"
{
 Properties
 {
  _SmokeColor("Smoke Color",Color)=(0.22,0.25,0.36,0.5)
  _LightColor("Firework Light",Color)=(0.5,0.65,1,1)
  _Glow("Illumination",Range(0,2))=0.65
  _Opacity("Density",Range(0,1))=0.32
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
            #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0;  UNITY_VERTEX_INPUT_INSTANCE_ID
};
   struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0;  UNITY_VERTEX_OUTPUT_STEREO
};
   CBUFFER_START(UnityPerMaterial)
    half4 _SmokeColor;
    half4 _LightColor;
    half _Glow;
    half _Opacity;
   CBUFFER_END
   float hash21(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}
   float noise2(float2 p)
   {
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
   }
   Varyings vert(Attributes a)
   {
    Varyings o; UNITY_SETUP_INSTANCE_ID(a); o=(Varyings)0; UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS=TransformObjectToHClip(a.positionOS.xyz);
    o.color=a.color;
    o.uv=a.uv;
    return o;
   }
   half4 frag(Varyings i):SV_Target
   {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 q=i.uv*2-1;
    float r=length(q);
    float edge=1-smoothstep(0.30,0.98,r);
    float n=noise2(i.uv*5.1)+0.45*noise2(i.uv*11.3+8);
    float detail=saturate((n-0.22)*1.25);
    float density=edge*detail*_Opacity*i.color.a;
    // Illumination is concentrated toward the inner cloud, rather than making an opaque colored disc.
    float inner=saturate(1.0-r*1.3);
    float radiance=_Glow*inner*inner;
    half3 tint=_SmokeColor.rgb*0.65+_LightColor.rgb*radiance;
    return half4(tint,density*_SmokeColor.a);
   }
   ENDHLSL
  }
 }
 Fallback Off
}
