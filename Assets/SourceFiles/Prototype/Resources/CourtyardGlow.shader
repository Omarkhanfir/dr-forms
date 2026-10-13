Shader "DrForms/Courtyard Glow" {
 Properties { _MainTex("Sprite",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader { Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" } Pass {
 Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _BaseColor;
 struct A{float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.color=i.color;return o;}
 half4 frag(V i):SV_Target{return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_BaseColor*i.color;}
 ENDHLSL
 } }
}
