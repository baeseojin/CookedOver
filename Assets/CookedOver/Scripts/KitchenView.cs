using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CookedOver
{
 public sealed partial class KitchenView : MonoBehaviour
 {
  public KitchenGame game;
  Transform generated;
  KitchenChapter renderedChapter;
  readonly List<Transform> chefs=new List<Transform>(),bodies=new List<Transform>(),leftFeet=new List<Transform>(),rightFeet=new List<Transform>(),leftArms=new List<Transform>(),rightArms=new List<Transform>();
  readonly List<Vector3> previousPositions=new List<Vector3>();
  readonly List<float> previousProgress=new List<float>();
  readonly List<ItemVisual> held=new List<ItemVisual>(),foods=new List<ItemVisual>(),loose=new List<ItemVisual>();
  readonly List<Transform> flames=new List<Transform>(),steam=new List<Transform>(),progress=new List<Transform>(),liquids=new List<Transform>(),knives=new List<Transform>(),highlights=new List<Transform>();
  readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
  readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
  public static readonly Color Teal=new Color(.15f,.48f,.43f),Cream=new Color(.94f,.91f,.79f),Wood=new Color(.56f,.32f,.16f);
  static readonly Color Ivory=new Color(.98f,.96f,.88f),Steel=new Color(.53f,.65f,.65f),Dark=new Color(.12f,.22f,.22f),Gold=new Color(.86f,.62f,.28f);
  static readonly Color[] ChefColors={new Color(.99f,.51f,.17f),new Color(.20f,.60f,.84f),new Color(.67f,.40f,.78f),new Color(.35f,.64f,.30f)};
  sealed class ItemVisual {public Transform root;public string key="";public ItemVisual(Transform value){root=value;}}
  public void Build() => Build(game!=null ? game.State : new KitchenSimulation().State);
  void Build(KitchenState state)
  {
   var old=transform.Find("Kitchen Geometry");if(old!=null){old.gameObject.SetActive(false);Delete(old.gameObject);}
   generated=Root("Kitchen Geometry",transform);
   chefs.Clear();bodies.Clear();leftFeet.Clear();rightFeet.Clear();leftArms.Clear();rightArms.Clear();previousPositions.Clear();held.Clear();foods.Clear();loose.Clear();
   previousProgress.Clear();flames.Clear();steam.Clear();progress.Clear();liquids.Clear();knives.Clear();highlights.Clear();
   renderedChapter=state.chapter;BuildRoom();
   foreach(var s in state.stations)BuildStation(s);
   for(int i=0;i<4;i++)BuildChef(i);
   for(int i=0;i<24;i++)loose.Add(new ItemVisual(Root("Loose food "+i,generated)));
   BuildEncounterVisuals(state);StyleLighting();Render(state);
  }
  void BuildRoom()
  {
   Box("Oak stage base",new Vector3(0,-.33f,0),new Vector3(13.8f,.60f,9.7f),Wood,.17f);
   Box("Foundation band",new Vector3(0,-.18f,0),new Vector3(13.65f,.19f,9.55f),new Color(.36f,.22f,.13f),.08f);
   Box("Tile grout",new Vector3(0,-.035f,0),new Vector3(13.15f,.12f,9.15f),new Color(.72f,.72f,.63f),.04f);
   for(int x=-6;x<=6;x++)for(int z=-4;z<=4;z++)Box("Ceramic tile",new Vector3(x,.025f,z),new Vector3(.974f,.08f,.974f),(x+z)%2==0?new Color(.85f,.83f,.70f):new Color(.73f,.78f,.67f),.025f);
   Box("Back wall",new Vector3(0,.72f,4.5f),new Vector3(13.6f,1.65f,.25f),Teal,.08f);
   Box("Back wall top",new Vector3(0,1.55f,4.5f),new Vector3(13.7f,.15f,.37f),Wood,.04f);
   for(int x=-12;x<=12;x++)for(int y=0;y<2;y++)Box("Backsplash tile",new Vector3(x*.51f,1.04f+y*.28f,4.345f),new Vector3(.49f,.265f,.045f),Ivory,.012f);
   Box("Left skirting",new Vector3(-6.64f,.25f,0),new Vector3(.20f,.54f,9.2f),Teal,.06f);
   Box("Right skirting",new Vector3(6.64f,.25f,0),new Vector3(.20f,.54f,9.2f),Teal,.06f);
   Box("Front oak rail",new Vector3(0,.13f,-4.5f),new Vector3(13.65f,.29f,.23f),Wood,.07f);
   Box("Window frame",new Vector3(0,2.05f,4.47f),new Vector3(4.8f,1.25f,.15f),Wood,.08f);
   Box("Window sky",new Vector3(0,2.06f,4.35f),new Vector3(4.53f,1.03f,.07f),new Color(.57f,.77f,.78f),.04f);
   Box("Window clouds",new Vector3(-.8f,2.33f,4.29f),new Vector3(1.15f,.16f,.03f),new Color(.85f,.92f,.85f),.06f);
   Box("Window mullion",new Vector3(0,2.06f,4.27f),new Vector3(.10f,1.05f,.08f),Ivory,.02f);
   Box("Window crossbar",new Vector3(0,2.04f,4.25f),new Vector3(4.58f,.08f,.10f),Ivory,.02f);
   Box("Window sill",new Vector3(0,1.45f,4.2f),new Vector3(5.04f,.14f,.57f),Wood,.04f);
   for(int i=0;i<3;i++)Plant(new Vector3(-1.7f+i*1.7f,1.53f,4.12f),.42f);
   Plant(new Vector3(-6.25f,.08f,3.65f),.65f);Plant(new Vector3(6.25f,.08f,3.65f),.65f);
   for(int i=0;i<2;i++)
   {
    float x=i==0?-4.7f:4.7f;
    Box("Framed menu",new Vector3(x,2.02f,4.34f),new Vector3(1.32f,1.12f,.13f),Wood,.06f);
    Box("Chalkboard",new Vector3(x,2.02f,4.25f),new Vector3(1.14f,.94f,.05f),Dark,.03f);
    for(int j=0;j<3;j++)Box("Chalk menu line",new Vector3(x,2.27f-j*.24f,4.20f),new Vector3(.72f-j*.1f,.045f,.015f),new Color(.78f,.82f,.67f),.01f);
   }
  }
  void BuildStation(StationState s)
  {
   Vector3 p=s.position;Color paint=s.kind==StationKind.Serve?new Color(.86f,.38f,.20f):Teal;
   Box("Cabinet plinth",p+Vector3.up*.14f,new Vector3(1.49f,.22f,1.15f),new Color(.13f,.27f,.25f),.045f);
   Box(s.name+" cabinet",p+Vector3.up*.52f,new Vector3(1.66f,.88f,1.33f),paint,.08f);
   for(int side=-1;side<=1;side+=2)
   {
    Box("Inset cabinet door",p+new Vector3(0,.52f,.681f*side),new Vector3(1.44f,.64f,.065f),Color.Lerp(paint,Ivory,.10f),.045f);
    Box("Door panel",p+new Vector3(0,.52f,.723f*side),new Vector3(1.27f,.45f,.025f),paint,.035f);
    Box("Brass handle",p+new Vector3(0,.78f,.754f*side),new Vector3(.40f,.058f,.065f),Gold,.025f);
   }
   Box("Counter wood edge",p+Vector3.up*1.015f,new Vector3(1.83f,.14f,1.49f),Wood,.055f);
   Box("Ivory countertop",p+Vector3.up*1.105f,new Vector3(1.86f,.13f,1.53f),Ivory,.065f);
   Transform knife=null,liquid=null;
   switch(s.kind)
   {
    case StationKind.Shrimp:Crate(p,Food.RawShrimp);break;
    case StationKind.Sauce:Crate(p,Food.BulgogiSauce);break;
    case StationKind.Fryer:BuildFryer(p);break;
    case StationKind.KioskSupplies:BuildFrozenSupplies(p);break;
    case StationKind.DrinkSupplies:BuildDrinkSupplies(p);break;
    case StationKind.Buns:Crate(p,Food.Bun);break;
    case StationKind.Meat:Crate(p,Food.RawPatty);break;
    case StationKind.Cheese:Crate(p,Food.Cheese);break;
    case StationKind.Grill:BuildGrill(p);break;
    case StationKind.Tomato:case StationKind.Onion:case StationKind.Lettuce:
     Crate(p,s.kind==StationKind.Tomato?Food.Tomato:s.kind==StationKind.Onion?Food.Onion:Food.Lettuce);break;
    case StationKind.Board:
     Box("Beech chopping board",p+Vector3.up*1.21f,new Vector3(1.22f,.09f,.88f),new Color(.71f,.48f,.25f),.09f);
     for(int j=0;j<4;j++)Box("Board wood grain",p+new Vector3(-.41f+j*.28f,1.26f,0),new Vector3(.014f,.006f,.65f),new Color(.62f,.40f,.19f),.003f);
     knife=Root("Animated knife",generated);knife.position=p+new Vector3(.40f,1.33f,.02f);
     Part(knife,"Steel knife blade",PrimitiveType.Cube,new Vector3(0,.03f,.06f),new Vector3(.025f,.18f,.40f),Steel,.018f);
     Part(knife,"Knife grip",PrimitiveType.Cube,new Vector3(0,.02f,-.25f),new Vector3(.075f,.09f,.24f),Dark,.025f);break;
    case StationKind.Stove:
     Box("Enamel hob",p+Vector3.up*1.21f,new Vector3(1.45f,.12f,1.16f),new Color(.22f,.31f,.33f),.065f);
     Ring("Burner ring",p+Vector3.up*1.285f,.48f,.035f,Dark);
     Box("Burner cross",p+Vector3.up*1.29f,new Vector3(1.07f,.05f,.08f),Dark,.015f);Box("Burner cross",p+Vector3.up*1.29f,new Vector3(.08f,.05f,1.02f),Dark,.015f);
     Lathe("Stockpot",p+Vector3.up*1.31f,new[]{new Vector2(0,0),new Vector2(.33f,0),new Vector2(.42f,.06f),new Vector2(.45f,.42f),new Vector2(.40f,.43f),new Vector2(.37f,.10f),new Vector2(0,.10f)},new Color(.58f,.68f,.65f));
     Ring("Pot lip",p+Vector3.up*1.74f,.425f,.035f,Steel);liquid=Sphere("Soup surface",p+Vector3.up*1.65f,new Vector3(.75f,.055f,.75f),Dark);
     for(int side=-1;side<=1;side+=2){Box("Pot handle",p+new Vector3(.57f*side,1.60f,0),new Vector3(.32f,.12f,.23f),Dark,.045f);Sphere("Hob dial",p+new Vector3(.40f*side,1.21f,-.60f),Vector3.one*.14f,Gold);}break;
    case StationKind.Sink:
     Box("Sink steel rim",p+Vector3.up*1.20f,new Vector3(1.38f,.08f,1.09f),Steel,.13f);
     Box("Recessed sink",p+Vector3.up*1.235f,new Vector3(1.19f,.035f,.86f),new Color(.20f,.35f,.37f),.13f);
     Box("Sink water",p+Vector3.up*1.26f,new Vector3(1.0f,.015f,.66f),new Color(.38f,.66f,.69f),.12f);Sphere("Drain",p+new Vector3(0,1.28f,0),new Vector3(.13f,.012f,.13f),Dark);
     Tube("Tap",p+new Vector3(.40f,1.20f,.43f),p+new Vector3(.40f,1.83f,.43f),.07f,Steel);Tube("Tap arch",p+new Vector3(.40f,1.83f,.43f),p+new Vector3(.12f,1.93f,.28f),.07f,Steel);Tube("Tap spout",p+new Vector3(.12f,1.93f,.28f),p+new Vector3(0,1.76f,.08f),.07f,Steel);
     Sphere("Tap handle",p+new Vector3(.64f,1.26f,.42f),new Vector3(.20f,.10f,.20f),Gold);break;
    case StationKind.Plates:
     for(int j=0;j<5;j++)Plate(p+Vector3.up*(1.20f+j*.065f),.95f);
     Box("Folded towel",p+new Vector3(.62f,1.22f,.32f),new Vector3(.29f,.08f,.48f),new Color(.82f,.49f,.31f),.035f);break;
    case StationKind.Serve:
     Box("Serving mat",p+Vector3.up*1.21f,new Vector3(1.23f,.025f,.84f),new Color(.88f,.76f,.54f),.06f);
     Sphere("Service bell base",p+new Vector3(.61f,1.24f,.40f),new Vector3(.31f,.07f,.31f),Dark);Sphere("Brass service bell",p+new Vector3(.61f,1.32f,.40f),new Vector3(.25f,.20f,.25f),Gold);Sphere("Bell button",p+new Vector3(.61f,1.44f,.40f),Vector3.one*.07f,Gold);break;
    case StationKind.Trash:Ring("Waste chute rim",p+Vector3.up*1.22f,.44f,.045f,Steel);Sphere("Waste chute",p+Vector3.up*1.20f,new Vector3(.82f,.045f,.82f),new Color(.075f,.12f,.11f));break;
    case StationKind.Extinguisher:Extinguisher(p+Vector3.up*1.18f,.85f,generated);break;
    default:if(renderedChapter==KitchenChapter.Burgers)Box("Burger assembly mat",p+Vector3.up*1.20f,new Vector3(1.28f,.035f,.90f),new Color(.75f,.62f,.39f),.07f);else Box("Folded kitchen cloth",p+new Vector3(.57f,1.20f,.41f),new Vector3(.33f,.04f,.43f),new Color(.86f,.67f,.35f),.035f);break;
   }
   knives.Add(knife);previousProgress.Add(0);liquids.Add(liquid);foods.Add(new ItemVisual(Root("Station ingredients",generated,p+Vector3.up*(s.kind==StationKind.Grill?1.37f:1.30f))));
   var fire=Root("Fire effect",generated,p+Vector3.up*1.50f);
   for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;Part(fire,"Flame",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*.28f,.25f,Mathf.Sin(angle)*.28f),new Vector3(.30f,.8f,.30f),i%2==0?new Color(1,.34f,.035f):new Color(1,.72f,.08f));}
   flames.Add(fire);fire.gameObject.SetActive(false);
   var vapor=Root("Steam",generated,p+Vector3.up*1.87f);for(int i=0;i<5;i++)Part(vapor,"Steam puff",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.1f,new Color(.82f,.90f,.84f,.27f));steam.Add(vapor);vapor.gameObject.SetActive(false);
   progress.Add(Box("Work progress",p+new Vector3(0,1.9f,-.4f),new Vector3(1.0f,.07f,.10f),new Color(.40f,.84f,.42f),.025f));
   var hi=Ring("Interaction halo",p+Vector3.up*.09f,.99f,.025f,new Color(1,.72f,.31f));hi.localScale=new Vector3(1,1,.81f);highlights.Add(hi);hi.gameObject.SetActive(false);
  }
  void Crate(Vector3 p,Food food)
  {
   Box("Crate bottom",p+Vector3.up*1.21f,new Vector3(1.38f,.12f,1.04f),Wood,.035f);
   for(int side=-1;side<=1;side+=2){Box("Crate long rim",p+new Vector3(0,1.34f,.48f*side),new Vector3(1.40f,.25f,.09f),new Color(.67f,.43f,.22f),.025f);Box("Crate end",p+new Vector3(.65f*side,1.34f,0),new Vector3(.10f,.25f,.99f),new Color(.67f,.43f,.22f),.025f);}
   for(int j=0;j<5;j++){var root=Root("Fresh produce",generated,p+new Vector3((j%3-1)*.37f,1.32f,j<3?-.19f:.20f));root.localScale=Vector3.one*.85f;Ingredient(root,food,false);}
  }
 }
}


