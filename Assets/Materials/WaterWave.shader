Shader "Custom/WaterWave"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
 
		CGINCLUDE
#include "UnityCG.cginc"
	uniform sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	uniform float _distanceFactor;	//距离系数
	uniform float _timeFactor;		//时间系数
	uniform float _totalFactor;		//sin函数结果系数
 
	uniform float _waveWidth;		//波纹宽度
	uniform float _curWaveDis;		//扩散速度
	uniform float4 _startPos;		//开始位置
 
	fixed4 frag(v2f_img i) : SV_Target
	{
		//DX下纹理坐标反向问题
		//UNITY_UV_STARTS_AT_TOP总是用1或0定义，Direct3D平台为1，OpenGL平台为0
		//在Direct3D中，坐标顶点为零，向下增加
		//OpenGL坐标底部为零，向上增加
#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
		_startPos.y = 1 - _startPos.y;
#endif
	//计算uv到中间点的向量(向外扩，反过来就是向里缩)
	float2 dv = _startPos.xy - i.uv;
	//按照屏幕长宽比进行缩放
	dv = dv * float2(_ScreenParams.x / _ScreenParams.y, 1);
	//计算像素点距中点的距离
	float dis = sqrt(dv.x * dv.x + dv.y * dv.y);
	//用sin函数计算出波形的偏移值factor
	//dis在这里都是小于1的，所以我们需要乘以一个比较大的数，比如60，这样就有多个波峰波谷
	//sin函数是（-1，1）的值域，我们希望偏移值很小，所以这里我们缩小100倍，据说乘法比较快,so...
	float sinFactor = sin(dis * _distanceFactor + _Time.y * _timeFactor) * _totalFactor * 0.01;
	//距离当前波纹运动点的距离，如果小于waveWidth才予以保留，否则已经出了波纹范围，factor通过clamp设置为0
	float discardFactor = clamp(_waveWidth - abs(_curWaveDis - dis), 0, 1) / _waveWidth;
	//归一化
	float2 dv1 = normalize(dv);
	//计算每个像素uv的偏移值
	float2 offset = dv1  * sinFactor * discardFactor;
	//像素采样时偏移offset
	float2 uv = offset + i.uv;
	return tex2D(_MainTex, uv);
	}
		ENDCG
 
		SubShader
	{
		Pass
		{
			ZTest Always
 
			CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG
		}
	}
	Fallback off
}