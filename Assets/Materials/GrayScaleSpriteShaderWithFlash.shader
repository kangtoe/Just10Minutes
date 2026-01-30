Shader "Custom/GrayScaleSpriteShaderWithFlash"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Contrast("Contrast", Range(0, 2)) = 1.0
        _FlashAmount("Flash Amount", Range(0, 1)) = 0
    }
        SubShader
        {
            Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
            LOD 100

            Pass
            {
                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _Contrast;
                float _FlashAmount;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    float gray = dot(col.rgb, float3(0.3, 0.59, 0.11)); // 흑백 변환

                    // 대비 적용
                    gray = (gray - 0.5) * _Contrast + 0.5;
                    gray = saturate(gray); // 0과 1 사이로 클램핑

                    // 점멸 효과 적용 (흑백 색상과 흰색을 lerp)
                    float3 finalColor = lerp(float3(gray, gray, gray), float3(1, 1, 1), _FlashAmount);

                    return fixed4(finalColor, col.a); // 알파 채널 유지
                }
                ENDCG
            }
        }
}
