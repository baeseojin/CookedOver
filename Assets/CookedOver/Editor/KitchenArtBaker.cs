using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CookedOver.Editor
{
 public static class KitchenArtBaker
 {
  const string Folder="Assets/CookedOver/Art";
  [MenuItem("CookedOver/Refresh Kitchen Artwork")]
  public static void Refresh()
  {
   if(Application.isPlaying)throw new InvalidOperationException("Stop play mode before updating artwork.");
   var game=UnityEngine.Object.FindFirstObjectByType<KitchenGame>();
   if(game==null || game.gameObject.scene.path!=KitchenStageBuilder.ScenePath)throw new InvalidOperationException("Open KitchenStage first.");
   game.view=game.GetComponent<KitchenView>();game.view.game=game;game.view.Build();SaveArt(game.gameObject);
   EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
   Debug.Log("Kitchen artwork saved: rounded furniture, characters, food models and lighting.");
  }
  public static void SaveArt(GameObject root)
  {
   Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
   var savedMeshes=new System.Collections.Generic.Dictionary<Mesh,Mesh>();
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
   {
    var mesh=filter.sharedMesh;if(mesh==null||AssetDatabase.Contains(mesh))continue;
    if(savedMeshes.TryGetValue(mesh,out var saved)){filter.sharedMesh=saved;continue;}
    string path=Folder+"/Meshes/"+MeshKey(mesh)+".asset";saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
    savedMeshes[mesh]=saved;filter.sharedMesh=saved;
   }
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
   {
    var material=renderer.sharedMaterial;if(material==null||AssetDatabase.Contains(material)||!material.name.StartsWith("Kitchen Art "))continue;
    string path=Folder+"/Materials/"+material.name.Replace("Kitchen Art ","Color_")+".mat";
    var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(saved==null){AssetDatabase.CreateAsset(material,path);saved=material;}else saved.CopyPropertiesFromMaterial(material);
    renderer.sharedMaterial=saved;
   }
   foreach(var volume in root.GetComponentsInChildren<Volume>(true))
   {
    var profile=volume.sharedProfile;if(profile==null||AssetDatabase.Contains(profile))continue;
    string path=Folder+"/KitchenLighting.asset";var saved=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
    if(saved==null){AssetDatabase.CreateAsset(profile,path);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);saved=profile;}
    else foreach(var component in profile.components)
    {
     var destination=saved.components.Find(c=>c.GetType()==component.GetType());
     if(destination==null){destination=UnityEngine.Object.Instantiate(component);saved.components.Add(destination);AssetDatabase.AddObjectToAsset(destination,saved);}
     else EditorUtility.CopySerialized(component,destination);
     EditorUtility.SetDirty(destination);EditorUtility.SetDirty(saved);
    }
    volume.sharedProfile=saved;
   }
   var camera=Camera.main;
   if(camera!=null)
   {
    camera.orthographicSize=7;camera.transform.position=new Vector3(0,14,-10.8f);camera.transform.LookAt(new Vector3(0,0,1.6f));camera.backgroundColor=new Color(.16f,.31f,.30f);
    var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
   }
   AssetDatabase.SaveAssets();
  }
  static string MeshKey(Mesh mesh)
  {
   using(var stream=new MemoryStream())
   {
    using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))
    {foreach(var v in mesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}foreach(int index in mesh.triangles)writer.Write(index);}
    using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","").Substring(0,24);
   }
  }
 }
}
