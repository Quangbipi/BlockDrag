// Viền glow cầu vồng cho preview phá nhiều hàng/cột: alpha lấy từ sprite, màu lấy từ gradient theo vị trí world trên bàn
Shader "BlockDrag/LineClearRainbowGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _GradientTex ("Gradient", 2D) = "white" {}
        _BoundsMin ("Bounds Min (world)", Vector) = (0, 0, 0, 0)
        _BoundsSize ("Bounds Size (world)", Vector) = (1, 1, 0, 0)
        _GradientDir ("Gradient Direction", Vector) = (0.6, 1, 0, 0)
        _ScrollSpeed ("Scroll Speed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _GradientTex;
            float4 _BoundsMin;
            float4 _BoundsSize;
            float4 _GradientDir;
            float _ScrollSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv01 = (i.worldPos - _BoundsMin.xy) / max(_BoundsSize.xy, 1e-4);
                float t = frac(dot(uv01 - 0.5, _GradientDir.xy) + 0.5 + _Time.y * _ScrollSpeed);
                fixed3 rgb = tex2D(_GradientTex, float2(t, 0.5)).rgb * i.color.rgb;
                fixed a = tex2D(_MainTex, i.uv).a * i.color.a;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
