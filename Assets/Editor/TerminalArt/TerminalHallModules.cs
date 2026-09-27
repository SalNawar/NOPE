using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class TerminalHallModules
{
    const string Folder="Assets/Art/Office/TerminalLayered/";
    static Transform root;static Camera cam;static Texture2D atlas;
    static Material Mat(string name,Color color,int mode=2)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("NOPE/Layered Hall Lighting"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_Color",color);m.SetFloat("_Mode",mode);EditorUtility.SetDirty(m);return m;
    }
    static Sprite Sprite(string name,Texture2D tex,Rect rect)
    {
        string path=Folder+name+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(!s){s=UnityEngine.Sprite.Create(tex,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(s,path);}return s;
    }
    static SpriteRenderer Image(string name,Sprite sprite,float x,float y,float w,float h,Material mat,int order=10)
    {
        var go=new GameObject("LH_"+name);go.transform.SetParent(root);go.transform.SetPositionAndRotation(cam.ViewportToWorldPoint(new Vector3(x,1-y,40)),cam.transform.rotation);
        float height=80*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f);
        go.transform.localScale=new Vector3(w*height*cam.aspect/sprite.bounds.size.x,h*height/sprite.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=mat;r.sortingOrder=order;return r;
    }
    static Sprite white;
    static SpriteRenderer Box(string name,float x,float y,float w,float h,Material mat,int order=10)=>Image(name,white,x,y,w,h,mat,order);
    static TextMesh Text(string name,string value,float x,float y,float size,Color color)
    {
        var go=new GameObject("LH_"+name);go.transform.SetParent(root);go.transform.SetPositionAndRotation(cam.ViewportToWorldPoint(new Vector3(x,1-y,39)),cam.transform.rotation);
        var t=go.AddComponent<TextMesh>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");t.text=value;t.fontSize=72;t.characterSize=size;t.anchor=TextAnchor.MiddleLeft;t.color=color;
        var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;r.sortingOrder=45;return t;
    }
    static void Rail(Vector2 a,Vector2 b,int posts)
    {
        var steel=Mat("Railing graphite",new Color(.22f,.25f,.28f));var brass=Mat("Railing worn brass",new Color(.55f,.43f,.26f));
        for(int i=0;i<=posts;i++){var p=Vector2.Lerp(a,b,i/(float)posts);Box("Railing post",p.x,p.y-.014f,.0018f,.028f,steel,15);}
        var centre=(a+b)*.5f;var delta=b-a;
        float width=Mathf.Sqrt(delta.x*delta.x+delta.y*delta.y/(cam.aspect*cam.aspect));
        foreach(float offset in new[]{.028f,.010f}){var rail=Box("Rail continuous handrail",centre.x,centre.y-offset,width,.0025f,brass,16);rail.transform.Rotate(0,0,Mathf.Atan2(-delta.y,delta.x*cam.aspect)*Mathf.Rad2Deg);}
    }
    public static void Build(Transform parent,Camera camera,SpriteRenderer shell,Texture2D floor,Texture2D fixtures)
    {
        root=parent;cam=camera;atlas=fixtures;
        string texPath=Folder+"ModuleWhite.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if(!tex){tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();AssetDatabase.CreateAsset(tex,texPath);}white=Sprite("ModuleWhiteSprite",tex,new Rect(0,0,1,1));
        var floorObj=Object.Instantiate(shell.gameObject,parent);floorObj.name="LH_Separate worn floor";
        var fr=floorObj.GetComponent<SpriteRenderer>();fr.sprite=Sprite("Separate floor",floor,new Rect(0,0,floor.width,floor.height));fr.sharedMaterial=Mat("Floor independent finish",Color.white,4);fr.sortingOrder=-2;
        var portal=Sprite("Portal frame cutout",atlas,new Rect(105,1024-495,645,475));
        var lamp=Sprite("Sconce cutout",atlas,new Rect(1070,1024-485,180,455));
        var lockers=Sprite("Station storage cutout",atlas,new Rect(108,1024-992,646,458));
        var cloth=Sprite("Flag cloth cutout",atlas,new Rect(994,1024-1005,300,508));
        var view=parent.gameObject.AddComponent<HallDisplayView>();view.portalEnergy=new Renderer[4];view.destinations=new TextMesh[4];view.statuses=new TextMesh[4];view.gateLabels=new TextMesh[4];
        var metal=Mat("Portal independent graphite",Color.white);
        float[,] gates={{.475f,.51f,.195f,.235f},{.586f,.447f,.067f,.09f},{.752f,.215f,.056f,.09f},{.85f,.172f,.07f,.12f}};
        for(int i=0;i<4;i++){
            float x=gates[i,0],y=gates[i,1],w=gates[i,2],h=gates[i,3];
            Image("Portal "+(i+1)+" frame",portal,x,y,w,h,metal,13);
            view.portalEnergy[i]=Box("Portal "+(i+1)+" energy",x,y-h*.095f,w*.47f,h*.64f,Mat("Portal "+(i+1)+" energy",new Color(.06f,.30f,.50f),6),12);
        }
        var cloths=new List<SpriteRenderer>();var logos=new List<SpriteRenderer>();
        foreach(float x in new[]{.311f,.683f}){
            cloths.Add(Image("Editable flag cloth",cloth,x,.115f,.04f,.235f,Mat("Flag neutral fabric",new Color(.63f,.22f,.27f)),9));cloths[cloths.Count-1].color=Color.white;
            var logo=Box("Replaceable flag emblem",x,.12f,.012f,.034f,Mat("Flag emblem gold",new Color(.8f,.62f,.30f)),10);logo.transform.Rotate(0,0,45);logos.Add(logo);
        }view.flagCloth=cloths.ToArray();view.flagLogos=logos.ToArray();
        var lm=Mat("Lamps independent metal and glass",Color.white);lm.SetFloat("_Mode",2.2f);EditorUtility.SetDirty(lm);
        foreach(var p in new[]{new Vector2(.282f,.342f),new Vector2(.709f,.342f),new Vector2(.365f,.39f),new Vector2(.627f,.39f)})Image("Paired bay sconce",lamp,p.x,p.y,.014f,.065f,lm,14);
        Rail(new Vector2(.374f,.247f),new Vector2(.633f,.247f),12);
        Rail(new Vector2(.724f,.246f),new Vector2(.944f,.182f),12);
        Rail(new Vector2(.04f,.446f),new Vector2(.236f,.414f),10);
        Box("Locker contact shadow",.202f,.662f,.162f,.027f,Mat("Storage contact shadow",new Color(.16f,.12f,.11f,.28f),6),17);
        Image("Grounded station lockers",lockers,.202f,.598f,.155f,.14f,Mat("Station lockers enamel",Color.white),18);
        var board=Mat("Departure board housing",new Color(.08f,.10f,.13f),5);var amber=new Color(1,.77f,.36f);
        Box("Board outer metal frame",.503f,.125f,.316f,.191f,Mat("Board brushed frame",new Color(.29f,.31f,.33f)),39);
        Box("Close departure board",.503f,.125f,.31f,.18f,board,40);
        Box("Board header brass rule",.503f,.078f,.285f,.0015f,Mat("Board brass trim",new Color(.62f,.47f,.27f),5),41);
        for(int row=0;row<4;row++)Box("Board row separator",.503f,.112f+row*.029f,.285f,.0007f,Mat("Board row rules",new Color(.19f,.23f,.25f),5),41);
        Box("Board left suspension",.363f,.022f,.003f,.05f,metal,39);Box("Board right suspension",.643f,.022f,.003f,.05f,metal,39);
        view.heading=Text("Board heading","DEPARTURES",.362f,.06f,.12f,amber);
        for(int i=0;i<4;i++){
            float y=.097f+i*.029f;
            view.gateLabels[i]=Text("Gate label "+i,"",.362f,y,.085f,Color.white);
            view.destinations[i]=Text("Destination "+i,"",.402f,y,.085f,new Color(.83f,.85f,.85f));
            view.statuses[i]=Text("Status "+i,"",.584f,y,.085f,amber);
            view.SetGate(i,(i+1).ToString("00"),new[]{"DESTINATION A","DESTINATION B","DESTINATION C","DESTINATION D"}[i],i!=1);
        }
        Box("Wayfinding panel",.856f,.301f,.175f,.04f,board,30);
        view.wayfinding=new[]{Text("Wayfinding","MEDBAY / JAIL / C-SUITES  >",.778f,.301f,.052f,Color.white)};
    }
}
