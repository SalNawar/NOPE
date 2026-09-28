using UnityEngine;

// Presentation only. Gameplay owns destinations, availability, localisation and timing.
public sealed class HallDisplayView : MonoBehaviour
{
    public TextMesh heading;
    public TextMesh[] gateLabels, destinations, statuses, wayfinding;
    public Renderer[] portalEnergy;
    public SpriteRenderer[] flagCloth, flagLogos;
    public void SetGate(int index,string gate,string destination,bool open)
    {
        if(index<0 || index>=destinations.Length)return;
        gateLabels[index].text=gate;destinations[index].text=destination;
        statuses[index].text=open?"OPEN":"CLOSED";
        statuses[index].color=open?new Color(.65f,.85f,1):new Color(1,.55f,.32f);
        portalEnergy[index].enabled=open;
    }
    public void SetStatusText(int index,string value){if(index>=0&&index<statuses.Length)statuses[index].text=value;}
    public void SetWayfinding(int index,string value){if(index>=0&&index<wayfinding.Length)wayfinding[index].text=value;}
    public void SetFlag(Color colour,Sprite logo)
    {
        foreach(var cloth in flagCloth){var properties=new MaterialPropertyBlock();cloth.GetPropertyBlock(properties);properties.SetColor("_Color",colour);cloth.SetPropertyBlock(properties);}
        foreach(var mark in flagLogos){mark.sprite=logo;mark.enabled=logo!=null;}
    }
}
