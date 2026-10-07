using System;
using System.Collections.Generic;
namespace UnityEngine
{
 public class ExecuteAlways:Attribute {} public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int n){}}
 public class MonoBehaviour {public object component; public T GetComponent<T>() where T:class => component as T;}
 public struct Vector4 {} public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color operator*(Color c,float v)=>new Color(c.r*v,c.g*v,c.b*v,c.a*v);}
 public static class Mathf {public static float Lerp(float a,float b,float t)=>a+(b-a)*Math.Clamp(t,0,1);public static float Clamp01(float x)=>Math.Clamp(x,0,1);}
 public static class Shader {public static int PropertyToID(string s)=>s.GetHashCode();}
 public class MaterialPropertyBlock {internal Dictionary<int,float> floats=new();public void SetFloat(int id,float v)=>floats[id]=v;public float GetFloat(int id)=>floats.TryGetValue(id,out var v)?v:0;public void Copy(MaterialPropertyBlock b)=>floats=new(b.floats);}
 public class SpriteRenderer {public Color color; MaterialPropertyBlock block=new();public void GetPropertyBlock(MaterialPropertyBlock b)=>b.Copy(block);public void SetPropertyBlock(MaterialPropertyBlock b)=>block.Copy(b);}
}
public class HallLight {public int order;public float SinceOn;}
public class HallLightingRig {public HallLightingSO Settings;public float Hour;}
public class HallLightingSO {public bool lightingOn=true,fixtureFlicker=true;public float sunriseHour=7,sunsetHour=16.5f;public float fixtureOffShare=0;public UnityEngine.Color fixtureColour=new(1,.79f,.54f);public HallDayCycle.Settings Cycle=>new(sunriseHour,sunsetHour,1.5f,.6f,.1f,4);}
public static class MotionPreference {public static bool Reduced=>false;}
