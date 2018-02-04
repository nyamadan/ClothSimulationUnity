Shader "Hidden/Simulation"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _PrevTex("Prev Texture", 2D) = "white" {}
        _Gravity("Gravity", Float) = -0.02
        _DeltaT("Delta T", Float) = 1.0
        _SpringConstraint("Spring Constraint", Float) = 1.0
        _SpringLength("Spring Length", Float) = 1.0
        _NeighborOffset("Neighbor Offset", Vector) = (1.0, 0.0, 0.0, 0.0)
        _Resistance("Resistance", Float) = 0.0
        _SphereRadius("Sphere Radius", Float) = 0.0
        _SpherePosition("Sphere Position", Float) = (0.0, 0.0, 0.0, 0.0)
    }

    SubShader
    {
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float       _Gravity;
            float       _DeltaT;
            float       _Resistance;
            sampler2D   _MainTex;
            sampler2D   _PrevTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 pos = tex2D(_MainTex, i.uv);
                float4 prevPos = tex2D(_PrevTex, i.uv);
                float3 dx = pos.xyz - prevPos.xyz;

                float3 f = -dx * _Resistance + float3(0.0, _Gravity, 0.0);

                pos.xyz += (_DeltaT * dx + 0.5 * f * _DeltaT * _DeltaT) * pos.w;

                return pos;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float   _SpringLength;
            float   _SpringConstraint;
            float   _DeltaT;
            float4  _NeighborOffset;
            float4  _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float2 neighbor(float2 uv)
            {
                float c = _MainTex_TexelSize * max(abs(_NeighborOffset.x), abs(_NeighborOffset.y));
                float2 s = 2.0 * ( 1.0 - fmod( floor( uv / c ), 2.0 ) ) - 1.0;
                return uv + s * _NeighborOffset * _MainTex_TexelSize.xy;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 p1 = tex2D(_MainTex, i.uv);
                float2 uv = neighbor(i.uv);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
                    return p1;
                }

                float4 p2 = tex2D(_MainTex, uv);

                float f = ((distance(p1.xyz, p2.xyz) - _SpringLength) * _SpringConstraint);

                float3 dx = normalize(p2.xyz - p1.xyz) * f * 0.5 * _DeltaT * _DeltaT;

                p1.xyz += dx * p1.w;

                return p1;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D   _MainTex;
            float4      _SpherePosition;
            float       _SphereRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 pos = tex2D(_MainTex, i.uv);
                float3 v = pos.xyz - _SpherePosition.xyz;
                float d = length(v);

                pos.xyz += pos.w * max(_SphereRadius - d, 0.0) * v / d;

                return pos;
            }
            ENDCG
        }
    }
}
