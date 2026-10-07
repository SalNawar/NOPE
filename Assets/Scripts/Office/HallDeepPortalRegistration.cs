using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Registers gameplay portal glow to the approved painted openings: each
/// PortalEffect's sprite draws in <see cref="clippedGlow"/>, which confines the glow
/// to the ring's inner opening. The effects' sprites are found once per loaded scene
/// (the gameplay layer loads after the hall), not every frame.</summary>
[DefaultExecutionOrder(230)]
public sealed class HallDeepPortalRegistration : MonoBehaviour
{
    /// <summary>The hall's registered architecture drawing; the glow is applied only while it has a sprite.</summary>
    [SerializeField] SpriteRenderer architecture;

    /// <summary>The clipped portal glow material (NOPE/Hall Deep Portal Glow).</summary>
    [SerializeField] Material clippedGlow;

    SpriteRenderer[] glows;

    /// <summary>Wires the drawing and the glow material (the art's DeepRoom authoring).</summary>
    public void Configure(SpriteRenderer drawing,Material glow){architecture=drawing;clippedGlow=glow;}

    void OnEnable()
    {
        glows=null;
        SceneManager.sceneLoaded+=Forget;
    }

    void OnDisable()=>SceneManager.sceneLoaded-=Forget;

    void Forget(Scene scene,LoadSceneMode mode)=>glows=null;

    void LateUpdate()
    {
        if(architecture==null || architecture.sprite==null || clippedGlow==null)return;
        if(glows==null || !Application.isPlaying)Collect();
        foreach(var renderer in glows)
            if(renderer!=null && renderer.sharedMaterial!=clippedGlow)renderer.sharedMaterial=clippedGlow;
    }

    void Collect()
    {
        var effects=FindObjectsByType<PortalEffect>(FindObjectsSortMode.None);
        glows=new SpriteRenderer[effects.Length];
        for(int i=0;i<effects.Length;i++)
            glows[i]=effects[i].GetComponentInChildren<SpriteRenderer>(true);
    }
}
