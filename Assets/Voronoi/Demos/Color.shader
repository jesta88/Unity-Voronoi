Shader "Custom/Color" 
{
	Properties 
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Opaque" }
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
	
			fixed4 _Color;
			
			float4 vert(float4 v:POSITION) : SV_POSITION 
			{
				return mul (UNITY_MATRIX_MVP, v);
			}
			
			half4 frag () : COLOR
			{
				return _Color;
			}
			ENDCG
		}
	}
}
