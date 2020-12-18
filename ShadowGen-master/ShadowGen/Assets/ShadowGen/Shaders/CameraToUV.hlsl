#ifndef CameraToUV
#define CameraToUV

void CameraToUV_float(float3 Position, float3 CameraPosition, float2 CameraSize, out float2 UV) {
	UV = ((Position.xy - CameraSize) - CameraPosition.xy) / (CameraSize * 2);
}

#endif