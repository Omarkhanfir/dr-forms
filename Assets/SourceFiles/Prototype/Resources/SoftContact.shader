Shader "DrForms/Soft Contact" {
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" } Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 position:POSITION; half4 color:COLOR; };
 struct V { float4 position:SV_POSITION; half4 color:COLOR; };
 V vert(A i) { V o; o.position=TransformObjectToHClip(i.position.xyz); o.color=i.color; return o; }
 half4 frag(V i):SV_Target { return i.color; }
 ENDHLSL
 } }
}
