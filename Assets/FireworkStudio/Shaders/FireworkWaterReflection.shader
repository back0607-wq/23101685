Shader "FireworkStudio/WaterReflection"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldXZ : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.worldXZ = world.xz;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float depth = input.uv.y;
                float x = input.uv.x * 2.0 - 1.0;
                float phase = _Time.y * 4.0 + dot(input.worldXZ, float2(0.15, 0.21));
                float drift = (sin(depth * 57.0 - phase) + 0.55 * sin(depth * 103.0 + phase)) * 0.12;
                float width = lerp(0.30, 0.72, depth);
                float across = (x - drift) / width;
                float edge = (1.0 - smoothstep(0.60, 1.0, abs(x)))
                    * smoothstep(0.0, 0.14, depth) * (1.0 - smoothstep(0.46, 1.0, depth));
                float ripple = sin(depth * 127.0 + x * 3.0 - phase)
                    + 0.58 * sin(depth * 221.0 - x * 8.0 + phase * 0.68);
                float bands = smoothstep(0.04, 1.0, ripple);
                float broken = 0.55 + 0.45 * sin(x * 23.0 + depth * 41.0 + phase);
                float glow = exp2(-across * across * 2.6) * edge
                    * (0.11 + 0.89 * bands * broken);
                return half4(input.color.rgb * (input.color.a * glow), 0);
            }
            ENDHLSL
        }
    }
}
