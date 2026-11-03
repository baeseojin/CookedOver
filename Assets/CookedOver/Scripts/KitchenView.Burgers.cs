using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenView
 {
  void BurgerIngredient(Transform parent,Food food)
  {
   if(food==Food.Bun){BunHalf(parent,.04f,false);BunHalf(parent,.18f,true);return;}
   if(food==Food.Cheese)
   {var slice=Part(parent,"Cheddar slice",PrimitiveType.Cube,Vector3.up*.10f,new Vector3(.49f,.045f,.49f),new Color(1,.64f,.08f),.025f);slice.localRotation=Quaternion.Euler(0,20,0);return;}
   bool cooked=food==Food.CookedPatty;
   Part(parent,cooked?"Grilled beef patty":"Raw beef patty",PrimitiveType.Sphere,Vector3.up*.12f,new Vector3(.53f,.18f,.50f),cooked?new Color(.30f,.15f,.065f):new Color(.69f,.25f,.20f));
   for(int i=0;i<4;i++)
   {
    if(cooked){var mark=Part(parent,"Grill sear",PrimitiveType.Cube,new Vector3((i-1.5f)*.10f,.207f,0),new Vector3(.021f,.012f,.31f),new Color(.13f,.075f,.035f),.004f);mark.localRotation=Quaternion.Euler(0,-18,0);}
    else Part(parent,"Beef marbling",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*2.4f)*.14f,.20f,Mathf.Cos(i*2.4f)*.14f),new Vector3(.08f,.015f,.04f),new Color(.91f,.63f,.49f));
   }
  }
  void BunHalf(Transform parent,float height,bool top)
  {
   Part(parent,top?"Golden sesame bun":"Bottom bun",PrimitiveType.Sphere,Vector3.up*height,new Vector3(.64f,top?.28f:.13f,.61f),top?new Color(.88f,.53f,.20f):new Color(.94f,.66f,.31f));
   if(top)for(int i=0;i<9;i++)
   {float a=i*2.4f,r=.06f+(i%3)*.065f;var seed=Part(parent,"Sesame seed",PrimitiveType.Sphere,new Vector3(Mathf.Sin(a)*r,height+.134f-r*.12f,Mathf.Cos(a)*r),new Vector3(.043f,.012f,.018f),new Color(1,.88f,.56f));seed.localRotation=Quaternion.Euler(0,i*31,0);}
  }
  void AssembledBurger(Transform parent,Item item)
  {
   bool complete=KitchenSimulation.IsBurger(item.food);int bun=complete?1:item.bun,patty=item.food==Food.DoubleBurger?2:item.food==Food.ShrimpBurger?0:complete?1:item.patty;
   int cheese=item.cheese>0||item.food==Food.Cheeseburger||item.food==Food.DeluxeBurger||item.food==Food.DoubleBurger?1:0;
   int tomato=item.tomato>0||item.food==Food.DeluxeBurger?1:0,lettuce=item.lettuce>0||item.food==Food.DeluxeBurger||item.food==Food.BulgogiBurger||item.food==Food.ShrimpBurger?1:0;
   float y=.10f;if(bun>0){BunHalf(parent,y,false);y+=.10f;}
   for(int n=0;n<patty;n++){var beef=Root("Burger patty",parent,Vector3.up*(y-.10f));BurgerIngredient(beef,Food.CookedPatty);y+=.12f;}
   if(item.shrimp>0||item.food==Food.ShrimpBurger){var shrimp=Root("Crispy shrimp patty",parent,Vector3.up*y);KioskIngredient(shrimp,Food.CookedShrimp);y+=.18f;}
   if(item.sauce>0||item.food==Food.BulgogiBurger){Part(parent,"Bulgogi glaze",PrimitiveType.Cylinder,Vector3.up*y,new Vector3(.49f,.014f,.49f),new Color(.35f,.17f,.05f));y+=.03f;}
   if(cheese>0){var slice=Part(parent,"Melted cheddar",PrimitiveType.Cube,Vector3.up*y,new Vector3(.56f,.035f,.53f),new Color(1,.64f,.08f),.022f);slice.localRotation=Quaternion.Euler(0,32,0);y+=.055f;}
   if(lettuce>0){for(int i=0;i<5;i++){float a=i*1.256f;Part(parent,"Burger lettuce",PrimitiveType.Sphere,new Vector3(Mathf.Sin(a)*.15f,y,Mathf.Cos(a)*.15f),new Vector3(.32f,.07f,.29f),i%2==0?new Color(.43f,.67f,.22f):new Color(.65f,.79f,.31f));}y+=.065f;}
   if(tomato>0){for(int i=0;i<3;i++){float a=i*2.1f;Part(parent,"Tomato slice",PrimitiveType.Cylinder,new Vector3(Mathf.Sin(a)*.12f,y,Mathf.Cos(a)*.12f),new Vector3(.28f,.027f,.27f),new Color(.91f,.23f,.12f));}y+=.07f;}
   if(bun>0)BunHalf(parent,y+.06f,true);
  }
  void BuildGrill(Vector3 p)
  {
   Box("Griddle steel surround",p+Vector3.up*1.22f,new Vector3(1.49f,.12f,1.18f),Steel,.065f);
   Box("Cast iron grill plate",p+Vector3.up*1.30f,new Vector3(1.30f,.05f,.97f),new Color(.15f,.18f,.18f),.045f);
   for(int i=0;i<7;i++)Box("Raised grill rib",p+new Vector3((i-3)*.17f,1.345f,0),new Vector3(.040f,.045f,.83f),new Color(.26f,.29f,.28f),.012f);
   for(int side=-1;side<=1;side+=2){Sphere("Grill temperature dial",p+new Vector3(side*.43f,1.21f,-.61f),new Vector3(.17f,.14f,.10f),Gold);Sphere("Heat indicator",p+new Vector3(side*.65f,1.25f,-.60f),Vector3.one*.055f,new Color(.97f,.30f,.05f));}
   Box("Spatula blade",p+new Vector3(.68f,1.38f,.10f),new Vector3(.18f,.025f,.28f),Steel,.018f);
   Box("Spatula handle",p+new Vector3(.68f,1.39f,.39f),new Vector3(.07f,.06f,.30f),Dark,.02f);
  }
 }
}
