using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenView
 {
  void BuildChef(int index)
  {
   var root=Root("Chef "+(index+1),generated);chefs.Add(root);previousPositions.Add(KitchenSimulation.Spawn(index));var body=Root("Animated chef",root);bodies.Add(body);
   Color skin=new[]{new Color(.91f,.64f,.40f),new Color(.70f,.42f,.25f),new Color(.96f,.75f,.55f),new Color(.80f,.56f,.39f)}[index];
   Part(body,"White chef jacket",PrimitiveType.Sphere,new Vector3(0,.72f,0),new Vector3(.78f,.86f,.63f),Ivory);
   Part(body,"Color apron",PrimitiveType.Sphere,new Vector3(0,.61f,.19f),new Vector3(.66f,.65f,.44f),ChefColors[index]);
   Part(body,"Apron bib",PrimitiveType.Cube,new Vector3(0,.91f,.29f),new Vector3(.39f,.35f,.08f),ChefColors[index],.035f);
   Part(body,"Apron pocket",PrimitiveType.Cube,new Vector3(0,.60f,.40f),new Vector3(.31f,.17f,.05f),Color.Lerp(ChefColors[index],Ivory,.2f),.03f);
   Part(body,"Scarf",PrimitiveType.Sphere,new Vector3(0,1.02f,0),new Vector3(.53f,.16f,.48f),ChefColors[index]);Part(body,"Scarf knot",PrimitiveType.Sphere,new Vector3(.13f,1.0f,.28f),Vector3.one*.16f,ChefColors[index]);
   Part(body,"Head",PrimitiveType.Sphere,new Vector3(0,1.30f,0),new Vector3(.70f,.66f,.64f),skin);Part(body,"Hair",PrimitiveType.Sphere,new Vector3(0,1.50f,-.05f),new Vector3(.72f,.36f,.64f),new Color(.22f,.13f,.075f));
   for(int side=-1;side<=1;side+=2)
   {
    Part(body,"Ear",PrimitiveType.Sphere,new Vector3(.34f*side,1.30f,.01f),Vector3.one*.17f,skin);
    Part(body,"Eye",PrimitiveType.Sphere,new Vector3(.15f*side,1.36f,.287f),new Vector3(.075f,.10f,.045f),Dark);
    Part(body,"Eye sparkle",PrimitiveType.Sphere,new Vector3(.15f*side-.014f,1.39f,.311f),Vector3.one*.021f,Color.white);
    Part(body,"Brow",PrimitiveType.Cube,new Vector3(.15f*side,1.45f,.28f),new Vector3(.12f,.035f,.035f),new Color(.28f,.16f,.08f),.015f);
    Part(body,"Cheek",PrimitiveType.Sphere,new Vector3(.25f*side,1.27f,.25f),new Vector3(.10f,.06f,.025f),new Color(.88f,.43f,.32f));
   }
   Part(body,"Round nose",PrimitiveType.Sphere,new Vector3(0,1.28f,.35f),new Vector3(.18f,.15f,.19f),skin);Part(body,"Smile",PrimitiveType.Sphere,new Vector3(0,1.17f,.30f),new Vector3(.13f,.045f,.04f),new Color(.43f,.20f,.12f));
   Part(body,"Hat band",PrimitiveType.Cylinder,new Vector3(0,1.64f,0),new Vector3(.74f,.14f,.67f),Ivory);
   for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Part(body,"Puffy toque",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.23f,1.86f+(i%2)*.08f,Mathf.Sin(a)*.20f),new Vector3(.48f,.45f,.44f),Ivory);}
   var l=Root("Left arm",body,new Vector3(-.41f,.91f,0));var r=Root("Right arm",body,new Vector3(.41f,.91f,0));leftArms.Add(l);rightArms.Add(r);
   foreach(var arm in new[]{l,r}){Part(arm,"Sleeve",PrimitiveType.Sphere,new Vector3(0,-.10f,.02f),new Vector3(.26f,.38f,.27f),Ivory);Part(arm,"Hand",PrimitiveType.Sphere,new Vector3(0,-.28f,.08f),Vector3.one*.23f,skin);}
   leftFeet.Add(Part(root,"Left shoe",PrimitiveType.Cube,new Vector3(-.21f,.17f,.08f),new Vector3(.28f,.25f,.43f),Dark,.09f));rightFeet.Add(Part(root,"Right shoe",PrimitiveType.Cube,new Vector3(.21f,.17f,.08f),new Vector3(.28f,.25f,.43f),Dark,.09f));
   var ring=Ring("Player color ring",Vector3.zero,.43f,.035f,ChefColors[index]);ring.SetParent(root,false);ring.localPosition=Vector3.up*.09f;
   held.Add(new ItemVisual(Root("Carried ingredient",body,new Vector3(0,.93f,.64f))));
   var number=Root("Player number",root,new Vector3(0,2.36f,0));number.rotation=Quaternion.Euler(48,0,0);var text=number.gameObject.AddComponent<TextMesh>();text.text="P"+(index+1);text.fontSize=48;text.characterSize=.055f;text.anchor=TextAnchor.MiddleCenter;text.color=Dark;
  }
  void Ingredient(Transform parent,Food f,bool cut)
  {
   if(KioskIngredient(parent,f))return;
   if(f==Food.Bun||f==Food.RawPatty||f==Food.CookedPatty||f==Food.Cheese){BurgerIngredient(parent,f);return;}
   Color color=FoodColor(f);
   if(cut){for(int j=0;j<6;j++)Part(parent,"Diced vegetable",PrimitiveType.Cube,new Vector3((j%3-1)*.12f,.08f+(j%2)*.015f,(j/3-.5f)*.16f),new Vector3(.13f,.13f,.12f),Color.Lerp(color,Ivory,j*.035f),.025f);return;}
   if(f==Food.Tomato)
   {
    Part(parent,"Ripe tomato",PrimitiveType.Sphere,Vector3.up*.21f,new Vector3(.48f,.41f,.45f),color);
    for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;var leaf=Part(parent,"Tomato leaf",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.09f,.41f,Mathf.Sin(a)*.09f),new Vector3(.09f,.035f,.22f),new Color(.22f,.42f,.16f));leaf.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
    Part(parent,"Tomato stem",PrimitiveType.Cylinder,Vector3.up*.46f,new Vector3(.035f,.065f,.035f),new Color(.26f,.42f,.18f));
   }
   else if(f==Food.Onion)
   {
    Part(parent,"Onion bulb",PrimitiveType.Sphere,Vector3.up*.22f,new Vector3(.44f,.43f,.43f),new Color(.86f,.71f,.77f));Part(parent,"Onion tip",PrimitiveType.Sphere,Vector3.up*.41f,new Vector3(.17f,.24f,.16f),new Color(.91f,.79f,.79f));
    for(int j=0;j<3;j++){var sprout=Part(parent,"Onion sprout",PrimitiveType.Capsule,new Vector3((j-1)*.045f,.57f,0),new Vector3(.035f,.12f,.035f),new Color(.36f,.52f,.22f));sprout.localRotation=Quaternion.Euler(0,0,(j-1)*18);}
   }
   else
   {
    for(int j=0;j<7;j++){float a=j*2.4f;Part(parent,"Lettuce leaf",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.11f,.15f+(j%3)*.065f,Mathf.Sin(a)*.11f),new Vector3(.33f,.32f,.30f),j%2==0?new Color(.45f,.67f,.24f):new Color(.62f,.79f,.32f));}
    Part(parent,"Lettuce heart",PrimitiveType.Sphere,new Vector3(0,.33f,0),Vector3.one*.26f,new Color(.71f,.83f,.40f));
   }
  }
  void SetItem(ItemVisual visual,Item item)
  {
   visual.root.gameObject.SetActive(!item.Empty);if(item.Empty)return;string key=item.food+"_"+item.chopped+"_"+item.tomato+"_"+item.lettuce+"_"+item.bun+"_"+item.patty+"_"+item.cheese+"_"+item.shrimp+"_"+item.sauce;if(visual.key==key)return;visual.key=key;
   for(int i=visual.root.childCount-1;i>=0;i--){var child=visual.root.GetChild(i);child.gameObject.SetActive(false);Delete(child.gameObject);}
   var parent=visual.root;if(KioskIngredient(parent,item.food))return;if(item.Raw||item.food==Food.Bun||item.food==Food.RawPatty||item.food==Food.CookedPatty||item.food==Food.Cheese){Ingredient(parent,item.food,item.chopped);return;}if(item.food==Food.Extinguisher){Extinguisher(Vector3.zero,.60f,parent);return;}
   var plate=Plate(Vector3.zero,.68f);plate.SetParent(parent,false);
   if(KitchenSimulation.IsBurger(item.food)||item.bun>0||item.patty>0||item.cheese>0||item.shrimp>0||item.sauce>0){AssembledBurger(parent,item);return;}
   if(item.food==Food.TomatoSoup||item.food==Food.OnionSoup)
   {
    var bowl=Lathe("Soup bowl",Vector3.zero,new[]{new Vector2(0,0),new Vector2(.20f,0),new Vector2(.32f,.18f),new Vector2(.30f,.20f),new Vector2(.23f,.06f),new Vector2(0,.04f)},Ivory);bowl.SetParent(parent,false);bowl.localPosition=Vector3.up*.08f;
    Part(parent,"Soup",PrimitiveType.Sphere,new Vector3(0,.23f,0),new Vector3(.56f,.035f,.56f),FoodColor(item.food));for(int i=0;i<3;i++)Part(parent,"Herb garnish",PrimitiveType.Sphere,new Vector3((i-1)*.08f,.26f,.035f),new Vector3(.06f,.012f,.09f),new Color(.28f,.48f,.17f));
   }
   else if(item.food==Food.Salad||item.tomato>0||item.lettuce>0)
   {
    if(item.food==Food.Salad||item.lettuce>0)for(int j=0;j<5;j++)Part(parent,"Salad leaf",PrimitiveType.Sphere,new Vector3(Mathf.Sin(j*2.4f)*.16f,.10f,Mathf.Cos(j*2.4f)*.16f),new Vector3(.23f,.09f,.23f),new Color(.51f,.72f,.28f));
    if(item.food==Food.Salad||item.tomato>0)for(int j=0;j<3;j++)Part(parent,"Tomato wedge",PrimitiveType.Cube,new Vector3(Mathf.Sin(j*2.1f)*.15f,.17f,Mathf.Cos(j*2.1f)*.15f),new Vector3(.14f,.09f,.10f),FoodColor(Food.Tomato),.025f);
   }
   else if(item.food==Food.DirtyPlate||item.food==Food.Burnt)Part(parent,"Dirty food scraps",PrimitiveType.Sphere,Vector3.up*.08f,new Vector3(.35f,.025f,.30f),new Color(.40f,.30f,.12f));
  }
  void Extinguisher(Vector3 p,float scale,Transform parent)
  {
   var root=Root("Extinguisher",parent,p);root.localScale=Vector3.one*scale;
   Part(root,"Red bottle",PrimitiveType.Capsule,new Vector3(0,.40f,0),new Vector3(.33f,.40f,.33f),new Color(.84f,.12f,.09f));Part(root,"Label band",PrimitiveType.Cylinder,new Vector3(0,.40f,0),new Vector3(.338f,.09f,.338f),Ivory);
   Part(root,"Valve",PrimitiveType.Cylinder,new Vector3(0,.82f,0),new Vector3(.12f,.08f,.12f),Dark);Part(root,"Handle",PrimitiveType.Cube,new Vector3(.06f,.91f,0),new Vector3(.25f,.06f,.10f),Dark,.02f);Part(root,"Hose",PrimitiveType.Capsule,new Vector3(.23f,.60f,0),new Vector3(.055f,.23f,.055f),Dark);
  }
  Transform Plate(Vector3 p,float diameter)
  {float r=diameter/2;return Lathe("Porcelain plate",p,new[]{new Vector2(0,0),new Vector2(r*.65f,0),new Vector2(r,.06f),new Vector2(r,.09f),new Vector2(r*.72f,.065f),new Vector2(0,.05f)},Ivory);}
  void Plant(Vector3 p,float scale)
  {
   var pot=Lathe("Herb planter",p,new[]{new Vector2(0,0),new Vector2(.16f,0),new Vector2(.23f,.30f),new Vector2(.19f,.31f),new Vector2(0,.24f)},new Color(.67f,.32f,.20f));pot.localScale=Vector3.one*scale;
   for(int i=0;i<6;i++){float a=i*2.4f;var leaf=Sphere("Herb leaves",p+new Vector3(Mathf.Sin(a)*.15f,.36f,Mathf.Cos(a)*.15f)*scale,new Vector3(.19f,.35f,.17f)*scale,i%2==0?new Color(.28f,.48f,.22f):new Color(.45f,.62f,.25f));leaf.rotation=Quaternion.Euler(Mathf.Cos(a)*30,0,Mathf.Sin(a)*30);}
  }
 }
}
