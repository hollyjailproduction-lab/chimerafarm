Shader "Unlit/WorldGrid"
{
    Properties
    {
        _GridSize ("Grid Size", Float) = 1
        _LineWidth ("Line Width", Range(0.001,0.1)) = 0.02
        _Color ("Line Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _GridSize;
            float _LineWidth;
            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float3 worldPos : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 grid = frac(i.worldPos.xz / _GridSize);
                float lineX = step(grid.x, _LineWidth) + step(1 - _LineWidth, grid.x);
                float lineZ = step(grid.y, _LineWidth) + step(1 - _LineWidth, grid.y);
                float gridline = saturate(lineX + lineZ);

                fixed4 col = _Color;
                col.a *= gridline;
                return col;
            }
            ENDCG
        }
    }
}