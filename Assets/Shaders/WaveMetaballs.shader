
Shader "Custom/WaveMetaballs"
{
    Properties
    {
        [MainColor] _BaseColor ("Water Color", Color) = (0.10, 0.55, 0.85, 0.9)
        _EdgeColor ("Edge Color", Color) = (0.40, 0.85, 1.0, 1.0)

        [Toggle] _UseMask ("Enable Rect Mask", Float) = 0
        _MaskCenter ("Mask Center XZ", Vector) = (0, 0, 0, 0)
        _MaskSize ("Mask Width / Height", Vector) = (10, 5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Metaballs"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                float _UseMask;
                float4 _MaskCenter;
                float4 _MaskSize;
            CBUFFER_END

            float4 _MetaballCenters[64];
            float _MetaballCount;
            float _Blend;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float SmoothMin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 pixel = input.positionWS.xz;

                // Rectangular mask in world XZ coordinates
                if (_UseMask > 0.5)
                {
                    float2 halfSize = max(_MaskSize.xy, 0.001) * 0.5;
                    float2 offset = abs(pixel - _MaskCenter.xy);

                    if (offset.x > halfSize.x ||
                        offset.y > halfSize.y)
                    {
                        discard;
                    }
                }

                float distanceToWater = 10000.0;

                [loop]
                for (int i = 0; i < 64; i++)
                {
                    if (i >= (int)_MetaballCount)
                        break;

                    float distanceToBall =
                        length(pixel - _MetaballCenters[i].xy)
                        - _MetaballCenters[i].z;

                    distanceToWater = SmoothMin(
                        distanceToWater,
                        distanceToBall,
                        max(_Blend, 0.001)
                    );
                }

                float antialias = max(fwidth(distanceToWater), 0.001);

                float alpha = 1.0 -
                    smoothstep(-antialias, antialias, distanceToWater);

                float edge = 1.0 -
                    smoothstep(0.0, max(_Blend, 0.001),
                    abs(distanceToWater));

                half3 color = lerp(
                    _BaseColor.rgb,
                    _EdgeColor.rgb,
                    edge * 0.4
                );

                return half4(color, _BaseColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
