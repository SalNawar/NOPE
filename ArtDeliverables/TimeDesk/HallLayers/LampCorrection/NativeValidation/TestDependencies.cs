using System.Collections.Generic;
using UnityEngine;
// Isolated test dependencies, not game source. Native Unity rendering/import APIs remain real.
public class HallLightingRig:MonoBehaviour {public HallLightingSO Settings;public float Hour=12;public float Evening=>1-HallDayCycle.Daylight(Hour,Settings.Cycle);}
public class HallLightingSO:ScriptableObject {public bool lightingOn=true,fixtureFlicker=true,previewHourOn;public float previewHour,sunriseHour=7,sunsetHour=16.5f,fixtureOffShare=0;public Color fixtureColour=new(1,.79f,.54f);public HallDayCycle.Settings Cycle=>new(sunriseHour,sunsetHour,1.5f,.6f,.1f,4);}
public class AnimeHallPresentation:MonoBehaviour {public class Layer {public string id;public SpriteRenderer renderer;}public List<Layer> layers=new();public Light daylight;public float lightingAmount=1;public void SetTime(float t){}}
public static class MotionPreference {public static bool Reduced=>false;}
