Shader "Custom/GradientShader" {
	Properties {
		_MainTex ("Unused", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows

		#define MAX_STOPS 16

		uniform fixed3 _colors[MAX_STOPS];
		uniform float _colors_positions[MAX_STOPS];
		uniform int _stop_count;
		uniform float4 _params;
		// Set and emitted by Unity:
		// uniform float4 _Time;
		// uniform float4 _SinTime;
		// uniform float4 _CosTime;
		
		float mod1(float x)
		{
			x -= floor(x);
			return x;
		}

		fixed3 follow_gradient(float t)
		{
			t = mod1(t);
			fixed3 col;
			bool done = false;

			for(int i = 0; i < _stop_count; i++)
			{
				col = done ? col : _colors[i];
				done = t < _colors_positions[i];
			}

			return col;
		}

		float find_position(float2 uv)
		{
			uv += float2(_SinTime.z, _CosTime.z);
			uv *= _params.x;
			return cos(_params.y) * uv.x + sin(_params.y) * uv.y + sin(_params.z * uv.x) + sin(_params.w * uv.x);
		}

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo = follow_gradient(find_position(IN.uv_MainTex));
			o.Emission = o.Albedo * .15;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
