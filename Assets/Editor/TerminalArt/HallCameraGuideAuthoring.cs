using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class HallCameraGuideAuthoring
{
    [MenuItem("Tools/Terminal Art/City/Build Desk Camera Hall Reference")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var scene=EditorSceneManager.NewPreviewScene();
        var materials=new List<Material>();
        Material Mat(Color colour)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor",colour);m.SetFloat("_Cull",0);
            materials.Add(m);return m;
        }
        var floor=Mat(new Color(.18f,.22f,.25f));
        var teal=Mat(new Color(.025f,.24f,.30f));
        var black=Mat(new Color(.065f,.08f,.095f));
        var gold=Mat(new Color(.60f,.43f,.15f));
        var red=Mat(new Color(.50f,.15f,.14f));
        var glass=Mat(new Color(.29f,.55f,.66f));
        var white=Mat(new Color(.87f,.92f,.91f));
        GameObject Cube(string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=p;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        void Bar(string name,Vector3 a,Vector3 b,float radius,Material m)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;
            SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=(a+b)*.5f;
            go.transform.up=(b-a).normalized;go.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);
            go.GetComponent<Renderer>().sharedMaterial=m;
        }
        void Ring(Vector3 center,float radius)
        {
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                Bar("Portal ring",center+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,
                    center+new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*radius,.10f,black);
            }
            Cube("Portal base",new Vector3(center.x,center.y-radius-.12f,center.z),new Vector3(radius*2.5f,.24f,1.5f),black);
            float ground=center.y-radius-.24f;
            for(int side=-1;side<=1;side+=2)
            {
                Cube("Bay glass side",new Vector3(center.x+side*radius*1.45f,ground+.6f,center.z),new Vector3(.08f,1.1f,3.6f),glass);
                Bar("Bay top rail",new Vector3(center.x+side*radius*1.45f,ground+1.2f,center.z-1.8f),
                    new Vector3(center.x+side*radius*1.45f,ground+1.2f,center.z+1.8f),.04f,gold);
            }
            Cube("Bay glass front",new Vector3(center.x,ground+.6f,center.z-1.8f),new Vector3(radius*2.9f,1.1f,.08f),glass);
            Bar("Bay front handrail",new Vector3(center.x-radius*1.45f,ground+1.2f,center.z-1.8f),
                new Vector3(center.x+radius*1.45f,ground+1.2f,center.z-1.8f),.04f,gold);
        }
        RenderTexture target=null;Texture2D image=null;
        try
        {
            Cube("Floor",new Vector3(0,-.15f,25),new Vector3(22,.3f,50),floor);
            // Correct camera-projected floor joints, all from a single level plane.
            for(int z=0;z<=50;z+=2)Cube("Tile joint",new Vector3(0,.002f,z),new Vector3(22,.005f,.025f),black);
            for(int x=-10;x<=10;x+=2)Cube("Tile joint",new Vector3(x,.004f,25),new Vector3(.025f,.005f,50),black);
            Cube("Right wall",new Vector3(10,4,25),new Vector3(.25f,8,50),teal);
            Cube("Ceiling",new Vector3(0,8.15f,25),new Vector3(22,.3f,50),teal);
            for(int z=1;z<=49;z+=6)
            {
                Cube("Ceiling cross beam",new Vector3(0,7.8f,z),new Vector3(22,.35f,.6f),black);
                Cube("Ceiling diffuser",new Vector3(0,7.58f,z),new Vector3(2,.06f,.5f),white);
                Cube("Left window mullion",new Vector3(-10,4,z),new Vector3(.25f,8,.25f),black);
                Cube("Right column",new Vector3(9.6f,4,z),new Vector3(.55f,8,.55f),gold);
            }
            for(int x=-8;x<=8;x+=4)Cube("Ceiling longitudinal beam",new Vector3(x,7.85f,25),new Vector3(.25f,.3f,50),black);
            Cube("Near black left pier",new Vector3(-6.8f,4.1f,12),new Vector3(1.3f,8.2f,1.3f),black);
            Cube("Upper bridge",new Vector3(0,4.05f,19),new Vector3(20,.55f,3),black);
            Bar("Bridge brass rail",new Vector3(-10,5.2f,17.5f),new Vector3(10,5.2f,17.5f),.04f,gold);
            for(int x=-10;x<=10;x+=2)Bar("Bridge post",new Vector3(x,4.3f,17.5f),new Vector3(x,5.2f,17.5f),.04f,gold);
            Cube("Upper right gallery",new Vector3(7.5f,4.05f,31),new Vector3(5,.55f,26),black);
            Bar("Gallery railing",new Vector3(5,5.2f,19),new Vector3(5,5.2f,44),.04f,gold);
            for(int i=0;i<15;i++)
            {
                float z=5.4f+i*.8f,h=(i+1)*4.3f/15;
                Cube("Left stairs",new Vector3(-7.4f,h*.5f,z),new Vector3(2.1f,h,.8f),floor);
            }
            Bar("Stair handrail",new Vector3(-6.25f,1.2f,5),new Vector3(-6.25f,5.5f,17.5f),.045f,gold);
            Ring(new Vector3(0,1.75f,12.5f),1.45f);
            Ring(new Vector3(-4.1f,1.55f,25),1.25f);
            Ring(new Vector3(4.1f,1.55f,25),1.25f);
            Ring(new Vector3(6.4f,5.8f,28),1.3f);
            Ring(new Vector3(6.4f,5.8f,40),1.3f);
            for(int z=10;z<=30;z+=10)
            {
                Cube("Right service door",new Vector3(9.83f,1.8f,z),new Vector3(.05f,3.6f,2.6f),red);
                Cube("Right service sign",new Vector3(9.75f,3.3f,z),new Vector3(.03f,.7f,2.5f),white);
            }
            Cube("Departure display",new Vector3(0,6.4f,12),new Vector3(6,1.7f,.15f),black);
            Cube("Banner",new Vector3(-4.7f,6.15f,10),new Vector3(.9f,3.7f,.08f),red);
            Cube("Banner",new Vector3(4.7f,6.15f,10),new Vector3(.9f,3.7f,.08f),red);
            Bar("Front platform handrail",new Vector3(-10,1.15f,2),new Vector3(10,1.15f,2),.05f,gold);
            for(int x=-10;x<=10;x+=2)Bar("Front platform post",new Vector3(x,0,2),new Vector3(x,1.15f,2),.05f,gold);
            Bar("Window side handrail",new Vector3(-9.75f,1.15f,0),new Vector3(-9.75f,1.15f,45),.05f,gold);
            // Outside towers serve only as a silhouette/scale reference.
            for(int i=0;i<16;i++)
            {
                float z=3+i*3.4f,x=-17-(i%3)*5,h=8+(i%5)*4;
                Cube("Exterior megatower",new Vector3(x,h*.5f-5,z),new Vector3(2.5f,h,2.5f),i%2==0?gold:teal);
            }
            var cameraGo=new GameObject("Desk camera reference");SceneManager.MoveGameObjectToScene(cameraGo,scene);
            var camera=cameraGo.AddComponent<Camera>();
            cameraGo.transform.SetPositionAndRotation(new Vector3(0,2.16f,-2.62f),Quaternion.Euler(4,0,0));
            camera.scene=scene;camera.cameraType=CameraType.Preview;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.35f,.69f,.88f);
            camera.fieldOfView=55;camera.nearClipPlane=.05f;camera.farClipPlane=300;camera.aspect=2172f/1080;
            camera.allowHDR=false;camera.allowMSAA=false;
            cameraGo.AddComponent<UniversalAdditionalCameraData>().SetRenderer(1);
            target=new RenderTexture(2172,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);camera.targetTexture=target;
            camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            image=new Texture2D(2172,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2172,1080),0,0);image.Apply();RenderTexture.active=old;
            Directory.CreateDirectory("ArtDeliverables/TimeDesk/City/DeeperRoom");
            File.WriteAllBytes("ArtDeliverables/TimeDesk/City/DeeperRoom/desk-camera-geometry-reference.png",image.EncodeToPNG());
        }
        finally
        {
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            EditorSceneManager.ClosePreviewScene(scene);
            foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);
        }
    }
}
