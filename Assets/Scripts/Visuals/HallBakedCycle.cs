using System;

/// <summary>Four lighting states, with a held night and smooth cyclic dawn/dusk.</summary>
public static class HallBakedCycle
{
    public readonly struct Weights4
    {
        public readonly float x,y,z,w;
        public Weights4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
    }
    static readonly float[] Hours = {0,5.5f,8,12,16.5f,19.5f,24};
    static readonly int[] States = {3,3,0,1,2,3,3};
    public static Weights4 Weights(float hour)
    {
        if(float.IsNaN(hour)||float.IsInfinity(hour)) hour=12;
        hour=((hour%24)+24)%24;
        for(int i=0;i<Hours.Length-1;i++)
        {
            if(hour>Hours[i+1]) continue;
            float t=(hour-Hours[i])/(Hours[i+1]-Hours[i]);t=t*t*(3-2*t);
            return new Weights4(Share(0),Share(1),Share(2),Share(3));
            float Share(int state)=>(States[i]==state?1-t:0)+(States[i+1]==state?t:0);
        }
        return new Weights4(0,0,0,1);
    }
}

