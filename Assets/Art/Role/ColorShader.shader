Shader "ColorShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _ClipEnabled ("Enable Alpha Clip", Int) = 0

    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        LOD 100
        Pass // 前向渲染 Base Pass
        {
           // Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            
            #pragma multi_compile_fwdbase
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 t2w_0 : TEXCOORD1;
                fixed4 t2w_1 : TEXCOORD2;
                fixed4 t2w_2 : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Cutoff;
            int _ClipEnabled;

            v2f vert (appdata v)
            {
                v2f o;
                //顶点从模型空间到裁剪空间
                o.pos = UnityObjectToClipPos(v.vertex);
                //计算UV变换
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //顶点从模型空间到世界空间
                fixed3 worldVertex = mul(unity_ObjectToWorld, v.vertex);
                //法线从模型空间到世界空间
                fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
                //切线从模型空间到切线空间
                fixed3 worldTangent = UnityObjectToWorldDir(v.tangent);
                //根据 世界空间法线、世界空间切线、切线方向 计算世界空间副切线 
                fixed3 worldBiTangent = cross(worldNormal,worldTangent) * v.tangent.w;

                //将 切线空间到世界空间的转换矩阵写入插值器，为了尽可能的利用插值器，把三个插值器的w分量存储世界空间顶点
                o.t2w_0 = float4(worldTangent.x,worldBiTangent.x,worldNormal.x, worldVertex.x);
                o.t2w_1 = float4(worldTangent.y,worldBiTangent.y,worldNormal.y, worldVertex.y);
                o.t2w_2 = float4(worldTangent.z,worldBiTangent.z,worldNormal.z, worldVertex.z);

                //使用Unity内置宏，传递阴影坐标到像素着色器
                /*
                 *由于内置宏 TRANSFER_SHADOW 中会使用上下文变量来进行相关计算
                 *此处 顶点着色器（vert）的输入结构体 appdata 必须命名为v，且输入结构体 appdata 内的顶点坐标必须命名为 vertex
                 *输出结构体 v2f 的顶点坐标必须命名为 pos 
                 */
                TRANSFER_SHADOW(o);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //_MainTex 贴图采样，并于颜色参数混合
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
    
                //世界空间顶点
                fixed3 worldVector = fixed3(i.t2w_0.w, i.t2w_1.w, i.t2w_2.w);
                //计算世界空间光照方向
                fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(worldVector));
                //计算光照颜色
                fixed3 diffuse = albedo.rgb;
                
                //世界空间视角方向
                fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldVector));
                clip(albedo.a - _Cutoff);
                // 归一化 世界空间光照方向和视角方向之和
                fixed3 hDir = normalize(worldLightDir + worldViewDir);
                
                //计算光照衰减和阴影(包含了阴影衰减，故无需再单独使用 SHADOW_ATTENUATION 内置宏来计算阴影)
                UNITY_LIGHT_ATTENUATION(atten, i, worldVector);
                
                fixed4 col = fixed4( diffuse * atten, 1.0);
                
                
                
                return col;
            }
            ENDCG
        }
        

        

    }
    Fallback "VertexLit"
}