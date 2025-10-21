Shader "Custom/BoxFilterDownscale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
            float4 _MainTex_TexelSize;
            float _DownscaleRate;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;                

                int boxSize = 2;
                float2 boxOffset = texelSize * boxSize;
                
                fixed4 col = fixed4(0,0,0,0);

                // Sample the pixels within the box
                for (int x = -boxSize; x <= boxSize; x++)
                {
                    for (int y = -boxSize; y <= boxSize; y++)
                    {
                        col += tex2D(_MainTex, i.uv + (float2(x, y) * (1 - _DownscaleRate)) * texelSize);
                    }
                }

                // Average the color values
                col /= (float)((boxSize * 2 + 1) * (boxSize * 2 + 1));

                return col;
            }
            ENDCG
        }
    }
}
