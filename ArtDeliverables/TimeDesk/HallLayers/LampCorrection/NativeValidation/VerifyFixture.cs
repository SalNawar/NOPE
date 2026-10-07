using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.IO;
public static class VerifyFixture
{
 public static void Run()
 {
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  string path="Assets/Fixture/GalleryMountedUnlit.png";
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
  settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.36f);
  settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
  importer.spritePixelsPerUnit=2205.9375f;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
  var platform=importer.GetDefaultPlatformTextureSettings();platform.maxTextureSize=4096;platform.textureCompression=TextureImporterCompression.Uncompressed;
  importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
  var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
  if(sprite==null)throw new Exception("Fixture sprite did not import");
  Debug.Log("FIXTURE_IMPORT rect="+sprite.rect+" pivot="+sprite.pivot+" ppu="+sprite.pixelsPerUnit);
  if(Mathf.Abs(sprite.pixelsPerUnit-2205.9375f)>.01f)throw new Exception("Wrong sprite PPU");
  if(Mathf.Abs(sprite.pivot.y/sprite.rect.height-.36f)>.001f)throw new Exception("Wrong pivot");
  AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long fileID);
  Debug.Log("FIXTURE_SPRITE_REFERENCE guid="+guid+" fileID="+fileID);
  foreach(string name in new[]{"NOPE/Hall Mounted Fixture","NOPE/Hall Four State","NOPE/Hall Waiting Bay Repair","NOPE/Hall Deep Layout"})
  {
   var shader=Shader.Find(name);
   if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Shader import failed: "+name);
   Debug.Log("FIXTURE_SHADER_IMPORT_PASS "+name);
  }
  Debug.Log("FIXTURE_NATIVE_IMPORT_PASS (not a game-scene capture)");
  NativeStateChecks(sprite);
 }
 static void NativeStateChecks(Sprite sprite)
 {
  var root=new GameObject("Fixture test rig");var rig=root.AddComponent<HallLightingRig>();
  var settings=ScriptableObject.CreateInstance<HallLightingSO>();rig.Settings=settings;
  var go=new GameObject("Gallery fixture test");go.transform.SetParent(root.transform);
  var light=go.AddComponent<HallLight>();light.kind=HallLightKind.Fixture;light.order=7;
  var drawing=go.AddComponent<SpriteRenderer>();drawing.sprite=sprite;
  var mounted=go.AddComponent<HallMountedFixture>();mounted.rig=rig;mounted.drawing=drawing;
  mounted.floorPool=new Vector4(.5f,.5f,.4f,.4f);
  typeof(HallLight).GetField("SinceOn",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(light,1f);
  void Check(string name,float hour,bool on,float expected)
  {
   rig.Hour=hour;settings.lightingOn=on;go.SendMessage("LateUpdate");
   var block=new MaterialPropertyBlock();drawing.GetPropertyBlock(block);
   var actual=block.GetFloat("_FixtureLevel");if(Mathf.Abs(actual-expected)>.0001f)throw new Exception(name+" emission="+actual);
   Debug.Log("FIXTURE_NATIVE_STATE_PASS "+name+"="+actual);
  }
  Check("noon",12,true,0);Check("night",22,true,1);Check("night lights off",22,false,0);
  settings.sunsetHour=23;Check("changed sunset",20,true,0);settings.sunsetHour=16.5f;
  var material=new Material(Shader.Find("NOPE/Hall Deep Layout"));
  var art=root.AddComponent<AnimeHallPresentation>();art.layers.Add(new AnimeHallPresentation.Layer{id="test",renderer=drawing});drawing.sharedMaterial=material;
  settings.lightingOn=false;
  var baked=root.AddComponent<HallBakedLighting>();baked.Configure(rig,art,material);
  var properties=new MaterialPropertyBlock();drawing.GetPropertyBlock(properties);
  if(properties.GetInt("_ScheduledFixtureCount")!=1 || properties.GetFloatArray("_FixtureLevels")[0]!=0)throw new Exception("Live hall ignored lighting off");
  settings.lightingOn=true;rig.Hour=22;baked.Apply();drawing.GetPropertyBlock(properties);
  if(Mathf.Abs(properties.GetFloatArray("_FixtureLevels")[0]-1)>.0001f)throw new Exception("Live hall missing night level");
  rig.Hour=12;baked.Apply();drawing.GetPropertyBlock(properties);
  if(properties.GetFloatArray("_FixtureLevels")[0]!=0)throw new Exception("Live hall fixture lit at noon");
  Debug.Log("FIXTURE_NATIVE_LIVE_HALL_PASS registered centers and live on/off/noon levels");
  UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(settings);UnityEngine.Object.DestroyImmediate(material);
 }
}
