using System;
using System.Reflection;
using UnityEngine;
class Program
{
    static int Main()
    {
        var settings = new HallLightingSO();
        var lamp = new HallMountedFixture {rig = new HallLightingRig {Settings=settings}, drawing = new SpriteRenderer()};
        var source = new HallLight {order=7, SinceOn=1};
        lamp.component=source;
        int failures=0;
        void Check(string name, float hour, bool on, float expected)
        {
            settings.lightingOn=on; lamp.rig.Hour=hour;
            typeof(HallMountedFixture).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lamp,null);
            var properties = new MaterialPropertyBlock(); lamp.drawing.GetPropertyBlock(properties);
            float actual=properties.GetFloat(Shader.PropertyToID("_FixtureLevel"));
            if(Math.Abs(actual-expected)>0.0001f){Console.WriteLine($"FAIL {name}: emission {actual}, expected {expected}"); failures++;}
            else Console.WriteLine($"PASS {name}: emission {actual}");
        }
        Check("noon unlit",12,true,0);
        Check("night lit",22,true,1);
        Check("night lighting disabled",22,false,0);
        settings.sunsetHour=23;
        Check("custom sunset keeps 20h unlit",20,true,0);
        settings.sunsetHour=16.5f;
        source.SinceOn=.1f;
        Check("strike flicker follows rig",22,true,.15f);
        settings.fixtureFlicker=false;
        Check("flicker preference disabled",22,true,1);
        lamp.component=null;
        Check("missing fixture light is unlit",22,true,0);
        return failures==0?0:1;
    }
}
