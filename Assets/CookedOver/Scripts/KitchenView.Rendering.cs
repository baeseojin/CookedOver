using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CookedOver
{
 public sealed partial class KitchenView
 {
  public void Render(KitchenState state)
  {
   if(generated==null||chefs.Count!=4||renderedChapter!=state.chapter||foods.Count!=state.stations.Count){Build(state);return;}
   float dt=Application.isPlaying?Mathf.Min(Time.deltaTime,.05f):1;float time=Application.isPlaying?Time.time:0;bool animate=state.started&&!state.paused&&!state.ended;
   RenderEncounterVisuals(state,time);
   for(int i=0;i<4;i++)
   {
    var c=state.chefs[i];chefs[i].gameObject.SetActive(c.active);if(!c.active)continue;float speed=dt>0?Vector3.Distance(c.position,previousPositions[i])/dt:0;previousPositions[i]=c.position;float stride=animate?Mathf.Clamp01(speed/3):0;
    chefs[i].localPosition=Vector3.Lerp(chefs[i].localPosition,c.position,Application.isPlaying?Mathf.Clamp01(dt*22):1);if(c.facing.sqrMagnitude>.1f)chefs[i].rotation=Quaternion.Slerp(chefs[i].rotation,Quaternion.LookRotation(c.facing),Mathf.Clamp01(dt*18));
    float walk=Mathf.Sin(time*16+i);bodies[i].localPosition=Vector3.up*(Mathf.Abs(walk)*.06f*stride);bodies[i].localRotation=Quaternion.Euler(0,0,-walk*stride*4);
    leftFeet[i].localPosition=new Vector3(-.21f,.17f+Mathf.Max(0,walk)*.11f*stride,.08f+walk*.14f*stride);rightFeet[i].localPosition=new Vector3(.21f,.17f+Mathf.Max(0,-walk)*.11f*stride,.08f-walk*.14f*stride);
    float carry=c.held.Empty?0:-53;leftArms[i].localRotation=Quaternion.Euler(carry+walk*stride*24,0,-8);rightArms[i].localRotation=Quaternion.Euler(carry-walk*stride*24,0,8);SetItem(held[i],c.held);
   }
   for(int i=0;i<state.stations.Count;i++)
   {
    var s=state.stations[i];SetItem(foods[i],s.item);float work=s.kind==StationKind.Fryer?s.heat/5:s.kind==StationKind.Grill?s.heat/(KitchenSimulation.GrillCookSeconds/(game!=null?game.simulation.CookingSpeed:1)):s.kind==StationKind.Stove?s.heat/(KitchenSimulation.CookSeconds/(game!=null?game.simulation.CookingSpeed:1)):s.progress/(s.kind==StationKind.Sink?KitchenSimulation.WashSeconds:KitchenSimulation.ChopSeconds);
    progress[i].gameObject.SetActive(work>0);progress[i].localScale=new Vector3(Mathf.Clamp01(work),1,1);highlights[i].gameObject.SetActive(state.started&&state.chefs.Exists(c=>c.active&&c.target==i));flames[i].gameObject.SetActive(s.fire>0);
    if(s.fire>0)for(int j=0;j<flames[i].childCount;j++)flames[i].GetChild(j).localScale=new Vector3(.29f,.60f+Mathf.Sin(time*13+j*2)*.25f,.29f);
    bool offline=state.kitchenEvent==KitchenEvent.Blackout&&!state.kiosk&&state.eventTime>0&&state.blackoutStation==i;
    bool simmer=!offline&&((s.kind==StationKind.Stove&&s.portions==3&&s.fire==0&&s.soup!=Food.Burnt)||(s.kind==StationKind.Grill&&!s.item.Empty&&s.item.food!=Food.Burnt&&s.heat>.3f&&s.fire==0));steam[i].gameObject.SetActive(simmer);
    if(simmer)for(int j=0;j<steam[i].childCount;j++){float t=Mathf.Repeat(time*.50f+j*.2f,1);var puff=steam[i].GetChild(j);puff.localPosition=new Vector3(Mathf.Sin(time+j)*.14f,t*.67f,Mathf.Cos(time*.7f+j)*.12f);puff.localScale=Vector3.one*(.07f+Mathf.Sin(t*Mathf.PI)*.12f);}
    if(liquids[i]!=null)liquids[i].GetComponent<Renderer>().sharedMaterial=Mat(s.portions==0?Dark:FoodColor(s.soup));if(knives[i]!=null)knives[i].localRotation=Quaternion.Euler(0,0,animate&&s.progress>previousProgress[i]?Mathf.Sin(time*25)*30:0);previousProgress[i]=s.progress;
   }
   for(int i=0;i<loose.Count;i++)
   {
    loose[i].root.gameObject.SetActive(i<state.loose.Count);if(i>=state.loose.Count)continue;var l=state.loose[i];SetItem(loose[i],l.item);loose[i].root.localPosition=l.position+Vector3.up*(.08f+Mathf.Max(0,.4f-l.age)*1.8f);loose[i].root.localRotation=Quaternion.Euler(0,l.velocity.sqrMagnitude>.5f?time*220:0,0);
   }
  }
  void StyleLighting()
  {
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.73f,.79f,.76f);RenderSettings.ambientEquatorColor=new Color(.59f,.66f,.60f);RenderSettings.ambientGroundColor=new Color(.38f,.39f,.30f);RenderSettings.reflectionIntensity=.32f;
   var sun=GameObject.Find("Warm Kitchen Light");if(sun!=null){var l=sun.GetComponent<Light>();l.intensity=1.1f;l.color=new Color(1,.92f,.80f);l.shadows=LightShadows.Soft;l.shadowStrength=.65f;l.shadowBias=.045f;l.shadowNormalBias=.20f;sun.transform.rotation=Quaternion.Euler(53,-32,0);}
   var camera=Camera.main;if(camera!=null){var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;}
   var root=Root("Kitchen color grading",generated);var volume=root.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
   var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.10f);grade.contrast.Override(8);grade.saturation.Override(5);var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.05f);bloom.intensity.Override(.12f);bloom.scatter.Override(.35f);
  }
  public static Color FoodColor(Food f)
  {
   switch(f){case Food.Tomato:case Food.TomatoSoup:return new Color(.88f,.19f,.10f);case Food.Onion:return new Color(.84f,.66f,.77f);case Food.OnionSoup:return new Color(.75f,.48f,.20f);case Food.Lettuce:case Food.Salad:return new Color(.43f,.68f,.23f);case Food.Plate:return Ivory;case Food.DirtyPlate:return new Color(.46f,.34f,.16f);case Food.Extinguisher:return new Color(.87f,.13f,.08f);case Food.Burnt:return new Color(.10f,.09f,.07f);default:return Cream;}
  }
  Transform Root(string name,Transform parent,Vector3 position=default){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
  static void Delete(Object obj){if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
  Material Mat(Color color)
  {
   if(materials.TryGetValue(color,out var m)&&m!=null)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard")){color=color,name="Kitchen Art "+ColorUtility.ToHtmlStringRGBA(color)};m.SetFloat("_Smoothness",.33f);
   if(color.a<1){m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent;m.SetShaderPassEnabled("ShadowCaster",false);}
   materials[color]=m;return m;
  }
  Transform Box(string name,Vector3 p,Vector3 size,Color color,float bevel=.04f)=>Part(generated,name,PrimitiveType.Cube,p,size,color,bevel);
  Transform Sphere(string name,Vector3 p,Vector3 size,Color color)=>Part(generated,name,PrimitiveType.Sphere,p,size,color);
  Transform Part(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Color color,float bevel=.04f)
  {
   Transform t;
   if(type==PrimitiveType.Cube){t=Root(name,parent,p);t.gameObject.AddComponent<MeshFilter>().sharedMesh=RoundedBox(size,bevel);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);}
   else{var obj=GameObject.CreatePrimitive(type);t=obj.transform;t.name=name;t.SetParent(parent,false);t.localPosition=p;t.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=Mat(color);Delete(obj.GetComponent<Collider>());}
   return t;
  }
  void Tube(string name,Vector3 a,Vector3 b,float radius,Color color){var t=Part(generated,name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),color);t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);}
  Mesh RoundedBox(Vector3 size,float radius)
  {
   string key="Bevel_"+size.ToString("F3")+"_"+radius.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);if(meshes.TryGetValue(key,out var mesh))return mesh;
   radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.48f);Vector3 half=size*.5f,inner=half-Vector3.one*radius;
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int steps=8;
   Vector3[] normal={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};Vector3[] u={Vector3.back,Vector3.forward,Vector3.right,Vector3.right,Vector3.right,Vector3.left};Vector3[] v={Vector3.up,Vector3.up,Vector3.back,Vector3.forward,Vector3.up,Vector3.up};
   for(int face=0;face<6;face++)
   {
    int first=vertices.Count;
    for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
    {
     Vector3 p=Vector3.Scale(normal[face]+u[face]*(x/(float)steps*2-1)+v[face]*(y/(float)steps*2-1),half);Vector3 q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));Vector3 n=(p-q).normalized;
     vertices.Add(q+n*radius);normals.Add(n);uv.Add(new Vector2(x/(float)steps,y/(float)steps));
    }
    for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int a=first+y*(steps+1)+x,b=a+1,c=a+steps+1,d=c+1;triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);}
   }
   mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();meshes[key]=mesh;return mesh;
  }
  Transform Lathe(string name,Vector3 p,Vector2[] profile,Color color)
  {
   string key="Lathe_";foreach(var point in profile)key+=point.ToString("F3");
   if(!meshes.TryGetValue(key,out var mesh))
   {
    int count=32;var vertices=new List<Vector3>();var triangles=new List<int>();for(int row=0;row<profile.Length;row++)for(int i=0;i<=count;i++){float a=i*Mathf.PI*2/count;vertices.Add(new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x));}
    for(int row=0;row<profile.Length-1;row++)for(int i=0;i<count;i++){int a=row*(count+1)+i,b=a+count+1;triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);}
    mesh=new Mesh{name="Kitchen lathe "+meshes.Count};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;
   }
   return MeshObject(name,p,mesh,color);
  }
  Transform Ring(string name,Vector3 p,float radius,float thickness,Color color)
  {
   string key="Ring_"+radius+"_"+thickness;
   if(!meshes.TryGetValue(key,out var mesh))
   {
    var vertices=new List<Vector3>();var triangles=new List<int>();int rings=32,sides=8;
    for(int i=0;i<=rings;i++)for(int j=0;j<=sides;j++){float a=i*Mathf.PI*2/rings,b=j*Mathf.PI*2/sides;float r=radius+Mathf.Cos(b)*thickness;vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*thickness,Mathf.Sin(a)*r));}
    for(int i=0;i<rings;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);}
    mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh;
   }
   return MeshObject(name,p,mesh,color);
  }
  Transform MeshObject(string name,Vector3 p,Mesh mesh,Color color){var t=Root(name,generated,p);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);return t;}
 }
}

