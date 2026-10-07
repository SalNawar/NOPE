using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>Left opens the existing city pan; Right returns. Office overlays retain their own keyboard input.</summary>
[DefaultExecutionOrder(150)]
public sealed class HallCityLookView : MonoBehaviour
{
    AnimeHallPresentation art;
    OfficeViewController office;
    DeskView desk;
    TravellerWheel wheel;
    float target,velocity;
    public float Target=>target;
    void Awake(){art=GetComponent<AnimeHallPresentation>();target=art.lookLeft;}
    public void LookLeft()=>target=1;
    public void Return()=>target=0;
    public void Step(float delta)
    {
        if(art==null)art=GetComponent<AnimeHallPresentation>();
        float next=MotionPreference.Reduced?target:Mathf.SmoothDamp(art.lookLeft,target,ref velocity,.45f,10,delta);
        if(Mathf.Abs(next-target)<.0001f)next=target;
        art.SetPan(next);
    }
    void Update()
    {
        if(office==null)office=FindFirstObjectByType<OfficeViewController>();
        if(desk==null)desk=FindFirstObjectByType<DeskView>();
        if(wheel==null)wheel=FindFirstObjectByType<TravellerWheel>();
        bool blocked=(office!=null && office.Current!=OfficeView.OfficeFocus)||(desk!=null&&desk.IsOn)||(wheel!=null&&wheel.IsOpen);
        var selected=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        blocked|=selected!=null&&(selected.GetComponent<TMP_InputField>()!=null||selected.GetComponent<InputField>()!=null);
        if(blocked)Return();
        else
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.leftArrowKey.wasPressedThisFrame)LookLeft();
                if(keyboard.rightArrowKey.wasPressedThisFrame)Return();
            }
        }
        Step(Time.unscaledDeltaTime);
    }
}
