using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenView
 {
  bool KioskIngredient(Transform parent,Food f)
  {
   if(f==Food.RawShrimp||f==Food.CookedShrimp)
   {Part(parent,"Shrimp patty",PrimitiveType.Sphere,Vector3.up*.15f,new Vector3(.55f,.22f,.50f),f==Food.RawShrimp?new Color(1,.62f,.55f):new Color(.88f,.55f,.13f));return true;}
   if(f==Food.BulgogiSauce)
   {Part(parent,"Bulgogi sauce bottle",PrimitiveType.Cylinder,Vector3.up*.22f,new Vector3(.23f,.25f,.23f),new Color(.34f,.16f,.05f));return true;}
   if(f==Food.Cola||f==Food.Sprite)
   {Part(parent,"Drink cup",PrimitiveType.Cylinder,Vector3.up*.23f,new Vector3(.34f,.23f,.34f),f==Food.Cola?new Color(.88f,.15f,.12f):new Color(.17f,.62f,.37f));Part(parent,"Cup lid",PrimitiveType.Cylinder,Vector3.up*.47f,new Vector3(.38f,.025f,.38f),Ivory);Part(parent,"Straw",PrimitiveType.Cylinder,new Vector3(.04f,.60f,0),new Vector3(.035f,.14f,.035f),Ivory);return true;}
   if(f==Food.RawFries||f==Food.Fries||f==Food.RawCheeseSticks||f==Food.CheeseSticks)
   {if(f==Food.Fries||f==Food.CheeseSticks){var plate=Plate(Vector3.zero,.68f);plate.SetParent(parent,false);}for(int i=0;i<5;i++)Part(parent,"Fried side",PrimitiveType.Cube,new Vector3((i-2)*.095f,.14f,i%2*.04f),new Vector3(.065f,.09f,.42f),f==Food.RawFries||f==Food.RawCheeseSticks?Ivory:Gold,.025f);return true;}
   return false;
  }
  void BuildFrozenSupplies(Vector3 p)
  {
   Box("Freezer cabinet",p+Vector3.up*1.35f,new Vector3(1.48f,.36f,1.06f),new Color(.47f,.73f,.88f),.07f);
   Box("Freezer glass lid",p+Vector3.up*1.55f,new Vector3(1.34f,.04f,.92f),new Color(.79f,.91f,.97f),.04f);
   for(int i=0;i<2;i++){var tray=Root("Frozen side display",generated,p+new Vector3(i==0?-.35f:.35f,1.56f,0));tray.localScale=Vector3.one*.60f;KioskIngredient(tray,i==0?Food.RawFries:Food.RawCheeseSticks);}
  }
  void BuildDrinkSupplies(Vector3 p)
  {
   Box("Drink dispenser base",p+new Vector3(0,1.20f,.36f),new Vector3(1.50f,.14f,.48f),new Color(.16f,.58f,.49f),.05f);
   for(int i=0;i<2;i++)
   {
    Color color=i==0?new Color(.88f,.15f,.12f):new Color(.17f,.62f,.37f);
    Box(i==0?"Cola dispenser":"Sprite dispenser",p+new Vector3(-.46f+i*.48f,1.60f,.35f),new Vector3(.39f,.70f,.39f),color,.05f);
    Box("Drink tap",p+new Vector3(-.46f+i*.48f,1.43f,.10f),new Vector3(.12f,.16f,.15f),Steel,.025f);
   }

   var cup=Root("Drink display",generated,p+new Vector3(.48f,1.22f,-.21f));cup.localScale=Vector3.one*.75f;KioskIngredient(cup,Food.Cola);
  }
  void BuildFryer(Vector3 p)
  {
   Box("Fryer steel rim",p+Vector3.up*1.25f,new Vector3(1.48f,.18f,1.15f),Steel,.05f);
   Box("Frying oil",p+Vector3.up*1.35f,new Vector3(1.20f,.025f,.85f),new Color(.63f,.37f,.08f),.03f);
   for(int i=0;i<6;i++)Box("Fryer basket wire",p+new Vector3((i-2.5f)*.18f,1.38f,0),new Vector3(.025f,.025f,.79f),Dark,.006f);
   Box("Fryer handle",p+new Vector3(0,1.47f,-.75f),new Vector3(.15f,.10f,.60f),Dark,.02f);
  }
 }
}
