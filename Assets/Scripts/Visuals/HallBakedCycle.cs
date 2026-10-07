using System;

/// <summary>Four lighting states, with a held night and smooth cyclic dawn/dusk.</summary>
public static class HallBakedCycle
{
    /// <summary>How much of each baked state shows: x morning, y noon, z evening, w night (they sum to 1).</summary>
    public readonly struct Weights4
    {
        /// <summary>The morning state's share.</summary>
        public readonly float x;
        /// <summary>The noon state's share.</summary>
        public readonly float y;
        /// <summary>The evening state's share.</summary>
        public readonly float z;
        /// <summary>The night state's share.</summary>
        public readonly float w;

        /// <summary>The four shares, morning to night.</summary>
        public Weights4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
    }
    static readonly float[] Hours = {0,5.5f,8,12,16.5f,19.5f,24};
    static readonly int[] States = {3,3,0,1,2,3,3};
    /// <summary>The states' shares at <paramref name="hour"/> (any real hour, wrapped to the day; NaN or infinity reads as noon): night held until 5:30, morning at 8, noon at 12, evening at 16:30, night again from 19:30, smoothstepped between keys.</summary>
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

