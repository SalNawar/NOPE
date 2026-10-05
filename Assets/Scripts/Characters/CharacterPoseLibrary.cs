using System;
using System.Linq;
using UnityEngine;

/// <summary>Complete authored pose sprites matched to the character's exact art keys.</summary>
public sealed class CharacterPoseLibrary : ScriptableObject
{
    [Serializable] public sealed class Pose
    {
        public Sprite sprite;
        public float scale=1;
        public float footOffset;
    }
    [Serializable] public sealed class Entry
    {
        public string signature;
        public Pose explaining, guarded;
    }
    public Entry[] entries=Array.Empty<Entry>();
    public static string Signature(TravellerLook look)=>string.Join("|",look.Keys.Select(k=>k.Name).OrderBy(k=>k,StringComparer.Ordinal));
    public Entry Find(TravellerLook look)
    {
        if(look==null) return null;
        string signature=Signature(look);
        return entries.FirstOrDefault(e=>e.signature==signature);
    }
}
