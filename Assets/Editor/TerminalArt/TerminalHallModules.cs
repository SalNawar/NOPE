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
    // Screen-space quadrilateral follows the supporting plane, rather than facing
    // every furnishing squarely toward the player. Dense tessellation avoids a diagonal UV seam.
    static MeshRenderer Projected(string name,Sprite sprite,Vector2 tl,Vector2 tr,Vector2 br,Vector2 bl,Material material,int order)
    {
        var go=new GameObject("LH_"+name);go.transform.SetParent(root);
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        const int steps=16;Rect rect=sprite.rect;
        for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++){
            float u=x/(float)steps,v=y/(float)steps;
            Vector2 p=Vector2.Lerp(Vector2.Lerp(bl,br,u),Vector2.Lerp(tl,tr,u),v);
            vertices.Add(cam.ViewportToWorldPoint(new Vector3(p.x,1-p.y,40)));
            uv.Add(new Vector2((rect.x+u*rect.width)/sprite.texture.width,(rect.y+v*rect.height)/sprite.texture.height));
            if(x<steps&&y<steps){int k=y*(steps+1)+x;indices.AddRange(new[]{k,k+1,k+steps+2,k,k+steps+2,k+steps+1});}
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        string path=Folder+name+" projection.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(mesh,path);
        // Centre local geometry so the normal hall pan also moves these layers correctly.
        var centre=mesh.bounds.center;var points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i]-=centre;mesh.vertices=points;mesh.RecalculateBounds();go.transform.position=centre;EditorUtility.SetDirty(mesh);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mat=Mat(name+" surface",material.GetColor("_Color"));mat.SetFloat("_Mode",material.GetFloat("_Mode"));mat.SetTexture("_MainTex",sprite.texture);mat.SetFloat("_Atmosphere",.18f);EditorUtility.SetDirty(mat);
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;return r;
    }
    static SpriteRenderer Box(string name,float x,float y,float w,float h,Material mat,int order=10)=>Image(name,white,x,y,w,h,mat,order);
    static TextMesh Text(string name,string value,float x,float y,float size,Color color)
    {
        var go=new GameObject("LH_"+name);go.transform.SetParent(root);go.transform.SetPositionAndRotation(cam.ViewportToWorldPoint(new Vector3(x,1-y,39)),cam.transform.rotation);
        var t=go.AddComponent<TextMesh>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");t.text=value;t.fontSize=72;t.characterSize=size;t.anchor=TextAnchor.MiddleLeft;t.color=color;
        var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;r.sortingOrder=45;return t;
    }
    static void Rail(Vector2 a,Vector2 b,int posts,float heightA=.028f,float heightB=.028f)
    {
        var steel=Mat("Railing graphite",new Color(.22f,.25f,.28f));var brass=Mat("Railing worn brass",new Color(.55f,.43f,.26f));
        for(int i=0;i<=posts;i++){float t=i/(float)posts;var p=Vector2.Lerp(a,b,t);float h=Mathf.Lerp(heightA,heightB,t);Box("Railing post",p.x,p.y-h*.5f,.0018f,h,steel,15);}
        foreach(float fraction in new[]{1f,.36f}){var start=a-Vector2.up*heightA*fraction;var end=b-Vector2.up*heightB*fraction;var centre=(start+end)*.5f;var delta=end-start;float width=Mathf.Sqrt(delta.x*delta.x+delta.y*delta.y/(cam.aspect*cam.aspect));var rail=Box("Rail continuous handrail",centre.x,centre.y,width,.0025f,brass,16);rail.transform.Rotate(0,0,Mathf.Atan2(-delta.y,delta.x*cam.aspect)*Mathf.Rad2Deg);}
    }
    public static void Build(Transform parent,Camera camera,SpriteRenderer shell,Texture2D floor,Texture2D fixtures,Texture2D portalViews,Texture2D storageView)
    {
        root=parent;cam=camera;atlas=fixtures;
        string texPath=Folder+"ModuleWhite.asset";var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if(!tex){tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();AssetDatabase.CreateAsset(tex,texPath);}white=Sprite("ModuleWhiteSprite",tex,new Rect(0,0,1,1));
        var floorObj=Object.Instantiate(shell.gameObject,parent);floorObj.name="LH_Separate worn floor";
        var fr=floorObj.GetComponent<SpriteRenderer>();fr.sprite=Sprite("Separate floor",floor,new Rect(0,0,floor.width,floor.height));fr.sharedMaterial=Mat("Floor independent finish",Color.white,4);fr.sortingOrder=-2;
        var portalRects=new[]{new Rect(550,405,510,340),new Rect(944,374,177,137),new Rect(1209,151,114,103),new Rect(1357,90,184,156)};
        var portals=new Sprite[4];
        for(int i=0;i<4;i++){var r=portalRects[i];portals[i]=Sprite("Painted portal view "+i,portalViews,new Rect(r.x,portalViews.height-r.y-r.height,r.width,r.height));}
        // Exclude the neighbouring portal from the main sprite's rectangular atlas region.
        var outline=new[]{new Vector2(0,0),new Vector2(510,0),new Vector2(510,230),new Vector2(394,230),new Vector2(394,340),new Vector2(0,340)};
        for(int i=0;i<outline.Length;i++)outline[i]=(outline[i]-new Vector2(255,170))/100;
        portals[0].OverrideGeometry(outline,new ushort[]{0,1,3,1,2,3,0,3,5,3,4,5});EditorUtility.SetDirty(portals[0]);
        var lamp=Sprite("Sconce cutout",atlas,new Rect(1070,1024-485,180,455));
        var lockers=Sprite("Soft station storage",storageView,new Rect(500,724-645,1170,560));
        var cloth=Sprite("Flag cloth cutout",atlas,new Rect(994,1024-1005,300,508));
        var view=parent.gameObject.AddComponent<HallDisplayView>();view.portalEnergy=new Renderer[4];view.destinations=new TextMesh[4];view.statuses=new TextMesh[4];view.gateLabels=new TextMesh[4];
        var metal=Mat("Portal independent graphite",Color.white);
        float[,] gates={{.493f,.478f,.175f,.205f},{.586f,.444f,.067f,.09f},{.752f,.221f,.055f,.090f},{.85f,.177f,.075f,.130f}};
        float[,] holes={{.496f,.432f,.383f,.606f},{.486f,.431f,.43f,.55f},{.43f,.515f,.28f,.56f},{.39f,.48f,.38f,.60f}};
        for(int i=0;i<4;i++){
            float x=gates[i,0],y=gates[i,1],w=gates[i,2],h=gates[i,3];
            var frameMaterial=Mat("Painted portal "+i+" ambient",Color.white);frameMaterial.SetFloat("_Atmosphere",i<2?.14f:.24f);frameMaterial.SetVector("_ClipCorner",i==0?new Vector4(944f/portalViews.width,(portalViews.height-512f)/portalViews.height,0,0):new Vector4(1,1,0,0));EditorUtility.SetDirty(frameMaterial);
            if(i<2)Box("Portal "+(i+1)+" contact",x,y+h*.47f,w*1.02f,h*.095f,Mat("Portal contact ambient",new Color(.18f,.15f,.13f,.30f),7),11);
            Image("Portal "+(i+1)+" painted frame",portals[i],x,y,w,h,frameMaterial,13);
            var energy=Mat("Portal "+(i+1)+" energy",new Color(.12f,.29f,.40f),6);
            view.portalEnergy[i]=Box("Portal "+(i+1)+" energy",x+(holes[i,0]-.5f)*w,y+(holes[i,1]-.5f)*h,w*holes[i,2],h*holes[i,3],energy,12);
        }
        var cloths=new List<SpriteRenderer>();var logos=new List<SpriteRenderer>();
        foreach(float x in new[]{.311f,.683f}){
            cloths.Add(Image("Editable flag cloth",cloth,x,.115f,.04f,.235f,Mat("Flag neutral fabric",new Color(.63f,.22f,.27f)),9));cloths[cloths.Count-1].color=Color.white;
            var logo=Box("Replaceable flag emblem",x,.12f,.012f,.034f,Mat("Flag emblem gold",new Color(.8f,.62f,.30f)),10);logo.transform.Rotate(0,0,45);logos.Add(logo);
        }view.flagCloth=cloths.ToArray();view.flagLogos=logos.ToArray();
        var lm=Mat("Lamps independent metal and glass",Color.white);lm.SetFloat("_Mode",2.2f);lm.SetFloat("_Atmosphere",.14f);EditorUtility.SetDirty(lm);
        foreach(var p in new[]{new Vector2(.282f,.342f),new Vector2(.678f,.342f),new Vector2(.365f,.39f),new Vector2(.627f,.39f)})Image("Paired bay sconce",lamp,p.x,p.y,.014f,.065f,lm,14);
        Rail(new Vector2(.374f,.247f),new Vector2(.633f,.247f),12);
        Rail(new Vector2(.724f,.273f),new Vector2(.944f,.205f),12,.019f,.030f);
        Rail(new Vector2(.04f,.446f),new Vector2(.236f,.414f),10);
        Projected("Station lockers following left wall",lockers,new Vector2(.145f,.540f),new Vector2(.274f,.450f),new Vector2(.274f,.650f),new Vector2(.145f,.683f),Mat("Station lockers enamel",Color.white),18);
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
