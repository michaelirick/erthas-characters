Shader "BoZo/BMAC_Toon"
{
    Properties
    {
        [MainTexture][NoScaleOffset]_Texture2D("Texture2D", 2D) = "white" {}
        [NoScaleOffset]_IDMap ("IDMap", 2D) = "red" {}
        [Normal][NoScaleOffset]_NormalMap("NormalMap", 2D) = "bump" {}
        [Header(Colors Options)]
        [Space(10)]

        [Toggle(_USECUSTOMTEXTURE)]_UseCustomTexture("_UseCustomTexture", Float) = 0
        _Color_1("Color_1", Color) = (1, 0, 0, 0)
        _Color_2("Color_2", Color) = (0, 1, 0, 0)
        _Color_3("Color_3", Color) = (0, 0, 1, 0)
        _Color_4("Color_4", Color) = (0.9727613, 1, 0, 0)
        _Color_5("Color_5", Color) = (0, 0.9845986, 1, 0)
        _Color_6("Color_6", Color) = (1, 0, 0.988061, 0)
        _Color_7("Color_7", Color) = (1, 1, 1, 0)
        _Color_8("Color_8", Color) = (0.5031446, 0.5031446, 0.5031446, 0)
        _Color_9("Color_9", Color) = (0, 0, 0, 0)

        [Header(Outline Options)]
        [Space(10)]
        [Toggle(_USEOUTLINE)]_UseOutline("useOutline", Float) = 1
        _OutlineColor("OutlineColor", Color) = (0, 0, 0, 0)
        _OutlineDistance("OutlineDistance", Float) = 2

        [Header(Lighting Options)]
        [Space(10)]
        [Toggle(_USENORMALMAP)]_UseNormalMap("UseNormalMap", Float) = 0
        _LightHeightDirectionStrength("LightHeightDirectionStrength", Range(0, 1)) = 1
        _Brightness("Brightness",  Range(0, 2)) = 1

        [Header(Rimlight Options)]
        [Space(10)]
        [Toggle(_USERIMLIGHT)]_UseRimlight("Use Rimlight", Float) = 0
        _BaseColor("BaseColor", Color) = (1, 1, 1, 0)
        _RimlightPower("Rimlight Power", Range(0, 10)) = 0.5
        _RimlightSharpness("Rimlight Sharpness", Range(0, 1)) = 0.5
        _RimlightColor("RimLightColor", Color) = (0, 0, 0, 0)
        [Toggle(_RIMLIGHTOVERLAY)]_UseRimlightOverlay("Use Rimlight Overlay", Float) = 1
        [Header(Shadow Options)]
        [Space(10)]
        _ShadowColor("ShadowColor", Color) = (0.8862745, 0.8862745, 0.8862745, 0)
        _ShadowEdgeColor("ShadowEdgeColor", Color) = (1, 0.9058824, 0.827451, 0)
        _ShadowEdge("ShadowEdge", Range(0.001, 1)) = 0.1
        _ShadowSharpness("ShadowSharpness", Range(0.001, 1)) = 0.1
        _RecieveShadows("RecieveShadows",  Range(0, 1)) = 1

        [Header(Ambiant Options)]
        [Space(10)]
        [ToggleUI]_UseAmbiant("UseAmbiant", Float) = 1
        _AmbiantStrength("AmbiantStrength", Range(0, 3)) = 0.5
        [Header(MultiLight Options)]
        [ToggleUI]_UseMultipleLights("UseMultipleLights", Float) = 0
        _MultipleLightSharpness("MultipleLightSharpness", Range(0, 1)) = 0
        _MultipleStrength("MultipleStrength", Float) = 1

        [Header(Decal Options)]
        [Space(10)]
        [NoScaleOffset]_DecalMap("DecalMap", 2D) = "black" {}
        _DecalUVSet("DecalUVSet", Range(0, 1)) = 0
        _DecalBlend("DecalBlend", Range(0, 1)) = 0
        _DecalScale("DecalScale", Vector) = (1, 1, 0, 0)
        _DecalColor_1("DecalColor_1", Color) = (0, 0, 0, 0)
        _DecalColor_2("DecalColor_2", Color) = (0, 0, 0, 0)
        _DecalColor_3("DecalColor_3", Color) = (0, 0, 0, 0)
        [Header(Pattern Options)]
        [Space(10)]
        [NoScaleOffset]_PatternMap("PatternMap", 2D) = "black" {}
        _PatternUVSet("PatternUVSet", Range(0, 1)) = 0
        _PatternBlend("PatternBlend", Range(0, 1)) = 0
        _PatternScale("PatternScale", Vector) = (1, 1, 0, 0)
        _PatternColor_1("PatternColor_1", Color) = (0, 0, 0, 0)
        _PatternColor_2("PatternColor_2", Color) = (0, 0, 0, 0)
        _PatternColor_3("PatternColor_3", Color) = (0, 0, 0, 0)

        [Space(10)]
        [Enum(UnityEngine.Rendering.CullMode)] _Culling("Culling", int) = 2
		[Enum(On, 1, Off, 0)] _ZWrite("ZWrite", int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", int) = 0


        [Header(Stencil Options)]
        [Space(10)]
        _RefVal ("ID", int ) = 221
        [Enum(UnityEngine.Rendering.CompareFunction)] _Compa("Compare Function", int) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Pass", Float) = 2
        _StencilWriteMask ("Stencil Write Mask", Float) = 221
        _StencilReadMask ("Stencil Read Mask", Float) = 255

    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags 
            {
                "LightMode" = "UniversalForwardOnly"
                "RenderType"="TransparentCutout"
                "Queue"="AlphaTest"
            }

            Cull [_Culling]
		    ZWrite[_ZWrite]
		    ZTest[_ZTest]

            Blend Off

            Stencil
            {
                Ref [_RefVal]
                WriteMask [_StencilWriteMask]
                ReadMask [_StencilReadMask]
                Comp [_Compa]
                Pass [_StencilPass]             
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP multi-compile directives
            #pragma multi_compile _ _USEOUTLINE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
		    #pragma multi_compile _ _FORWARD_PLUS
		    #pragma multi_compile _ _USENORMALMAP
		    #pragma multi_compile _ _USEMASKMAP
		    #pragma multi_compile _ _USECUSTOMTEXTURE
		    #pragma multi_compile _ _USERIMLIGHT
		    #pragma multi_compile _ _RIMLIGHTOVERLAY
            #pragma multi_compile_fog


            // Additional lights support (required for Forward+)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // Include URP libraries
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;

                float3 normalOS : NORMAL;
                float4 tangentOS  : TANGENT;
                float4 vertexColor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD5;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float4 screenPos   : TEXCOORD4;
                float3x3 tangentToWorld : TEXCOORD6;
                float3 viewDirWS   : TEXCOORD9;
                float2 fogCoord : TEXCOORD10;
                float4 vertexColor : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _Texture2D;
            float4 _Texture2D_ST;

            //BaseColor
            float4 _Color_1;
            float4 _Color_2;
            float4 _Color_3;
            float4 _Color_4;
            float4 _Color_5;
            float4 _Color_6;
            float4 _Color_7;
            float4 _Color_8;
            float4 _Color_9;

            //decal
            sampler2D _DecalMap;
            float4 _DecalMap_ST;
            float  _DecalUVSet;
            float  _DecalBlend;
            float4 _DecalScale;
            float4 _DecalColor_1;
            float4 _DecalColor_2;
            float4 _DecalColor_3;

            //pattern
            sampler2D _PatternMap;
            float4 _PatternMap_ST;
            float  _PatternUVSet;
            float  _PatternBlend;
            float4 _PatternScale;
            float4 _PatternColor_1;
            float4 _PatternColor_2;
            float4 _PatternColor_3;

            //lighting
            sampler2D _NormalMap;
            float _Brightness;
            float3 _BaseColor;
            float _LightHeightDirectionStrength;
            float _ShadowSharpness;
            float _ShadowEdge;
            float4 _ShadowColor;
            float4 _ShadowEdgeColor;
            float _AmbiantStrength;
            float _RecieveShadows;
            float _RimlightPower;
            float _RimlightSharpness;
            float3 _RimlightColor;


            Varyings vert (Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionHCS = TransformObjectToHClip(input.positionOS);
                output.positionCS = positionHCS;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _Texture2D);
                output.uv1 = TRANSFORM_TEX(input.uv1, _Texture2D);
                output.vertexColor = input.vertexColor;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.viewDirWS = normalize(GetWorldSpaceViewDir(output.positionWS));


                float3 tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                float3 bitangentWS = cross(output.normalWS, tangentWS) * input.tangentOS.w;

                output.fogCoord = ComputeFogFactor(positionHCS.z);

                output.tangentToWorld = float3x3(tangentWS, bitangentWS, output.normalWS);


                // Compute shadow coordinates
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                return output;
            }

            float4 ApplyPattern(float4 tex, float flatTexture, float mask, Varyings i)
            {

                float2 uv = lerp(i.uv, i.uv1, _PatternUVSet);
                //float2 scaleduv = uv * _DecalScale;
                float2 scaleduv = uv * _PatternScale + ((_PatternScale * -1) / 2) + 0.5;

                float4 pattern = tex2D(_PatternMap, scaleduv);

                float4 color1 = lerp(0, _PatternColor_1, pattern.x);
                float4 color2 = lerp(0, _PatternColor_2, pattern.y);
                float4 color3 = lerp(0, _PatternColor_3, pattern.z);
                
                float4 combine = color1 + color2 + color3;

                float steppedMask = step(0.01, mask);
                steppedMask = steppedMask  * i.vertexColor.x;
                steppedMask = steppedMask * pattern.a;
                steppedMask = lerp(pattern.a, steppedMask, _PatternBlend);
                float4 blend  = lerp(combine, flatTexture * combine, _PatternBlend);

                float4 final = lerp(tex, blend, pattern.w * steppedMask);
                return float4(final.rgb, tex.a + pattern.a);
            }

            float4 ApplyDecal(float4 tex, float flatTexture, float mask, Varyings i)
            {
                float2 uv = lerp(i.uv, i.uv1, _DecalUVSet);
                //float2 scaleduv = uv * _DecalScale;
                float2 scaleduv = uv * _DecalScale + ((_DecalScale * -1) / 2) + 0.5;

                float4 decal = tex2D(_DecalMap, scaleduv);

                float4 color1 = lerp(0, _DecalColor_1, decal.x);
                float4 color2 = lerp(0, _DecalColor_2, decal.y);
                float4 color3 = lerp(0, _DecalColor_3, decal.z);

                float4 combine = color1 + color2 + color3;

                float steppedMask = step(0.06, mask);

                float4 blend  = lerp(combine, flatTexture * combine, _DecalBlend);

                
                float4 final = lerp(tex, blend, decal.a);

                return float4(final.rgb, tex.a);
            }

            float4 CustomColors(float4 tex, float4 vertexColors)
            {
               float4 color1 = lerp(0, _Color_1, tex.x);
               float4 color2 = lerp(0, _Color_2, tex.y);
               float4 color3 = lerp(0, _Color_3, tex.z);
               float4 color4 = lerp(0, _Color_4, tex.x);
               float4 color5 = lerp(0, _Color_5, tex.y);
               float4 color6 = lerp(0, _Color_6, tex.z);
               float4 color7 = lerp(0, _Color_7, tex.x);
               float4 color8 = lerp(0, _Color_8, tex.y);
               float4 color9 = lerp(0, _Color_9, tex.z);

               float4 combine1 = color1 + color2 + color3;
               float4 combine2 = color4 + color5 + color6;
               float4 combine3 = color7 + color8 + color9;

               float4 layer1 = lerp(0, combine1, vertexColors.x);
               float4 layer2 = lerp(layer1, combine2, vertexColors.y);
               float4 layer3 = lerp(layer2, combine3, vertexColors.z);

               return float4(layer3.rgb, tex.a);
            }

            float Dither(float In, float4 ScreenPosition)
            {
                float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                };
                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                float o = In - DITHER_THRESHOLDS[index];

                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Normalize normal
                half3 normalWS = normalize(input.normalWS);

                #ifdef _USENORMALMAP
                float3 normalTS = UnpackNormal(tex2D(_NormalMap, input.uv));
                normalWS = normalize(mul(normalTS, input.tangentToWorld));
                #endif


                // Sample albedo texture
                half4 map = tex2D(_Texture2D, input.uv);
                half4 albedo = map;

                #ifdef _USECUSTOMTEXTURE
                half4 flat = map.x + map.y + map.z;
                albedo = CustomColors(map, input.vertexColor);
                albedo = ApplyPattern(albedo, flat, map.x, input);
                albedo = ApplyDecal(albedo, flat, map.x, input);
                #endif

                // Initialize lighting
                half3 diffuse = 0;
                half3 specular = 0;
                half3 ambient = SampleSH(input.normalWS) * _AmbiantStrength;
                ambient = ambient * albedo.rgb;

                // View direction
                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // Main light
                Light mainLight = GetMainLight(input.shadowCoord);

                half3 lightDir = mainLight.direction;
                half3 heightlessLightDir = half3(mainLight.direction.x,0,mainLight.direction.z);
                float3 dir = lerp(heightlessLightDir, lightDir, _LightHeightDirectionStrength);
                half3 lightColor = mainLight.color;
                half shadowAtten = mainLight.shadowAttenuation;

                // Toon Lighting
                half NdotL = dot(normalWS, dir);

                float shadow = smoothstep(0, _ShadowSharpness, NdotL);
                float shadowEdge = smoothstep(0 - _ShadowEdge, _ShadowSharpness - _ShadowEdge, NdotL);

                float3 shadowColors = lerp(_ShadowColor, _ShadowEdgeColor, shadowEdge);
                float3 litColor = lerp(shadowColors, _BaseColor, shadow);
                float3 recievedShadows = lerp(_ShadowColor, litColor, shadowAtten);

                float3 finalLight = lerp(litColor, recievedShadows, _RecieveShadows);
                finalLight = finalLight * _Brightness;

                diffuse += finalLight;

                #if USE_FORWARD_PLUS
	                InputData inputData = (InputData)0;
	                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    inputData.normalWS = input.normalWS;
	                inputData.positionWS = input.positionWS;
                #endif


                float3 forwardMultiLights = 0;
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
		            //get light color and direction
		            #if !USE_FORWARD_PLUS
			            lightIndex = GetPerObjectLightIndex(lightIndex);
		            #endif
		            Light light = GetAdditionalPerObjectLight(lightIndex, input.positionWS); 
	
		            //calculate shadows
		            light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, input.positionWS, light.direction);
		            float atten = light.distanceAttenuation * light.shadowAttenuation;

		            //calculate diffuse and specular
		            float NdotL = saturate(dot(input.normalWS, light.direction));
		            NdotL *= atten;
                    NdotL = NdotL;
		            //accumulate light

                    forwardMultiLights += light.color * (NdotL * albedo.rgb);
                LIGHT_LOOP_END

                // Combine lighting
                half3 finalColor = albedo.rgb * (diffuse * mainLight.color) + ambient;
                finalColor = finalColor + forwardMultiLights;

                #if _USERIMLIGHT
                    float fresnel = pow(1.0 - saturate(dot(input.normalWS, input.viewDirWS)), _RimlightPower);
                    fresnel = smoothstep(0, _RimlightSharpness, fresnel);
                    #if _RIMLIGHTOVERLAY
                        finalColor = lerp(finalColor, _RimlightColor, fresnel);
                    #elif !_RIMLIGHTOVERLAY
                        finalColor += fresnel * finalColor * _RimlightColor;
                    #endif
                #endif

                finalColor = MixFog(finalColor, input.fogCoord);

                float mask = albedo.a;
                clip(mask - 0.5);
                //return float4(input.vertexColor.agb, albedo.a);
                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" "RenderType"="TransparentCutout" "Queue"="Geometry" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _Texture2D;
            float4 _Texture2D_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                // Simple clip-space transform is fine for shadows here
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(input.uv, _Texture2D);
                return o;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                // Use base texture alpha as cutout mask (threshold 0.5, same as Forward pass)
                half alpha = tex2D(_Texture2D, input.uv).a;
                clip(alpha - 0.5);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Culling]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _Texture2D;

            Varyings vert (Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                return o;
            }

            half4 fragDepth (Varyings input) : SV_Target
            {
                half alpha = tex2D(_Texture2D, input.uv).a;
                clip(alpha - 0.5);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On
            Cull [_Culling]

            HLSLPROGRAM
            #pragma vertex vertDepthNormals
            #pragma fragment fragDepthNormals
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _Texture2D;

            Varyings vertDepthNormals(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                return o;
            }

            half4 fragDepthNormals(Varyings input) : SV_Target
            {
                half alpha = tex2D(_Texture2D, input.uv).a;
                clip(alpha - 0.5);

                half3 normalWS = normalize(input.normalWS);
                return half4(normalWS * 0.5h + 0.5h, 1.0h);
            }
            ENDHLSL
        }



        Pass
        {
            Name "Outline"
            Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
            Cull Front
		    ZWrite[_ZWrite]
		    ZTest[_ZTest]


            Stencil
            {
                Ref 221
                WriteMask 221
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USEOUTLINE
            # include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 vertexColor : COLOR; 
                float2 uv: TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 vertexColor : COLOR;
                float2 uv: TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _Texture2D;
            float4 _Texture2D_ST;
            float _OutlineDistance;
            float4 _OutlineColor;


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertexColor = IN.vertexColor;
                float3 norm = normalize(IN.normalOS);
                float3 pos = IN.positionOS.xyz + norm * (_OutlineDistance * 0.001) * IN.vertexColor.w;
                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                #ifdef _USEOUTLINE

                    half4 map = tex2D(_Texture2D, IN.uv);
                    float finalMask = 1;
                    finalMask = finalMask * IN.vertexColor.w;

                    float4 finalColor = lerp(_OutlineColor, map, 0);

                    clip(IN.vertexColor.a * map.a - 0.5);
                    return float4(finalColor.rgb , IN.vertexColor.w);

                #else

                    discard;
                    //clip(1 - 0.5);
                    return float4(0,0,0,0);

                #endif
            }
                    ENDHLSL

            }

    }
    //FallBack "Universal Render Pipeline/Lit"
}