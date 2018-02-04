Shader "Hidden/Cloth"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D   _MainTex;
            float4      _MainTex_ST;
            float4      _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            v2f vert(appdata v)
            {
                float4 uv = float4(v.uv, 0.0, 0.0);
                float4 vertex = tex2Dlod(_MainTex, uv);

                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);

                float3 va = tex2Dlod(_MainTex, uv + float4(_MainTex_TexelSize.x, 0.0, 0.0, 0.0)).xyz - tex2Dlod(_MainTex, uv - float4(_MainTex_TexelSize.x, 0.0, 0.0, 0.0)).xyz;
                float3 vb = tex2Dlod(_MainTex, uv + float4(0.0, _MainTex_TexelSize.y, 0.0, 0.0)).xyz - tex2Dlod(_MainTex, uv - float4(0.0, _MainTex_TexelSize.y, 0.0, 0.0)).xyz;
                o.normal = float4(normalize(cross(va, vb)), 1.0).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = i.normal;
                float dotNL = max(dot(normal, normalize(float3(1.0,2.0,3.0))), 0.0);
                float3 color = float3(0.2, 0.8, 0.3);
                return fixed4(color * pow(0.5 * dotNL + 0.5, 2.0), 1.0);
            }
            ENDCG
        }
    }
}
