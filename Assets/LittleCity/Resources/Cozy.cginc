#ifndef LITTLE_CITY_COZY_INCLUDED
#define LITTLE_CITY_COZY_INCLUDED
float _CozyStyle;
float3 CozyPigment(float3 c)
{
 float l=dot(c,float3(.2126,.7152,.0722));
 float3 soft=lerp(c,l.xxx,.13);
 soft=lerp(soft,float3(.91,.85,.74),.10*saturate(l*2));
 return lerp(c,soft,_CozyStyle);
}
#endif
