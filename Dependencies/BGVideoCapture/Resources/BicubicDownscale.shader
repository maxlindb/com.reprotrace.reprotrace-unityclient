Shader "Custom/BicubicDownscale"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
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
            float2 _MainTex_TexelSize;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Bicubic interpolation support functions
            float CatmullRom(float a, float b, float c, float d, float t)
            {
                return 0.5 * (
                    (2.0 * b) +
                    (-a + c) * t +
                    (2.0 * a - 5.0 * b + 4.0 * c - d) * t * t +
                    (-a + 3.0 * b - 3.0 * c + d) * t * t * t
                );
            }

            float4 SampleBicubic(sampler2D tex, float2 uv, float2 texelSize)
            {
                float2 f = frac(uv / texelSize - 0.5);
                uv -= f * texelSize;

                float4 sum = float4(0,0,0,0);
                for(int y = -1; y <= 2; ++y)
                {
                    float4 row = float4(0,0,0,0);
                    for(int x = -1; x <= 2; ++x)
                    {
                        row[x+1] = tex2D(tex, uv + texelSize * float2(x, y)).r;
                    }
                    sum[y+1] = CatmullRom(row.x, row.y, row.z, row.w, f.x);
                }

                return CatmullRom(sum.x, sum.y, sum.z, sum.w, f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                return SampleBicubic(_MainTex, i.uv, texelSize);
            }
            ENDCG
        }
    }
}
