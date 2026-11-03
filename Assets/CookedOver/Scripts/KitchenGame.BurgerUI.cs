using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenGame
 {
  public void SelectChapter(KitchenChapter chapter)
  {
   if(State.started||connecting)return;
   selectedChapter=chapter;simulation.Reset(chapter);
  }
  void DrawChapterSelector()
  {
   if(Button(new Rect(794,78,268,43),"01  수프 키친",selectedChapter==KitchenChapter.Classic?Mint:new Color(.19f,.34f,.34f),!connecting,19))SelectChapter(KitchenChapter.Classic);
   if(Button(new Rect(1080,78,270,43),"02  버거 하우스",selectedChapter==KitchenChapter.Burgers?Orange:new Color(.19f,.34f,.34f),!connecting,19))SelectChapter(KitchenChapter.Burgers);
  }
  bool DrawBurgerFoodIcon(Rect r,Food food)
  {
   float x=r.x,y=r.y,w=r.width,h=r.height;
   if(KitchenSimulation.IsBurger(food))
   {
    Disc(new Rect(x,y+h*.77f,w,h*.20f),new Color(.74f,.81f,.76f));
    Round(new Rect(x+w*.10f,y+h*.71f,w*.80f,h*.15f),new Color(.94f,.63f,.25f),h*.06f);
    Round(new Rect(x+w*.09f,y+h*.57f,w*.82f,h*.15f),food==Food.ShrimpBurger?new Color(.91f,.60f,.20f):new Color(.33f,.18f,.09f),h*.07f);
    if(food==Food.DoubleBurger)Round(new Rect(x+w*.09f,y+h*.38f,w*.82f,h*.15f),new Color(.33f,.18f,.09f),h*.07f);
    if(food==Food.Cheeseburger||food==Food.DeluxeBurger||food==Food.DoubleBurger)Round(new Rect(x+w*.07f,y+h*.48f,w*.86f,h*.09f),new Color(1,.73f,.10f),h*.02f);
    if(food==Food.DeluxeBurger||food==Food.BulgogiBurger||food==Food.ShrimpBurger)
    {for(int i=0;i<4;i++)Disc(new Rect(x+w*(.08f+i*.2f),y+h*.36f,w*.25f,h*.16f),new Color(.43f,.70f,.25f));Round(new Rect(x+w*.13f,y+h*.32f,w*.74f,h*.10f),Red,h*.03f);}
    float bunY=food==Food.Burger?.13f:food==Food.DeluxeBurger?-.12f:.04f;
    BunIcon(new Rect(x+w*.10f,y+h*bunY,w*.80f,h*.44f));return true;
   }
   if(food==Food.Bun){BunIcon(new Rect(x+w*.05f,y+h*.10f,w*.9f,h*.65f));Round(new Rect(x+w*.05f,y+h*.75f,w*.9f,h*.12f),new Color(.94f,.63f,.25f),h*.04f);return true;}
   if(food==Food.RawPatty||food==Food.CookedPatty)
   {
    Disc(new Rect(x+w*.08f,y+h*.28f,w*.84f,h*.56f),food==Food.RawPatty?new Color(.72f,.29f,.23f):new Color(.33f,.18f,.09f));
    for(int i=0;i<3;i++)Line(new Vector2(x+w*(.27f+i*.2f),y+h*.41f),new Vector2(x+w*(.21f+i*.2f),y+h*.67f),w*.05f,food==Food.RawPatty?new Color(.95f,.63f,.49f):new Color(.17f,.09f,.04f));return true;
   }
   if(food==Food.Cheese)
   {Round(new Rect(x+w*.10f,y+h*.22f,w*.80f,h*.58f),new Color(1,.69f,.10f),w*.07f);Disc(new Rect(x+w*.28f,y+h*.32f,w*.15f,h*.15f),new Color(.89f,.50f,.06f));Disc(new Rect(x+w*.61f,y+h*.53f,w*.12f,h*.12f),new Color(.89f,.50f,.06f));return true;}
   return false;
  }
  void BunIcon(Rect r)
  {
   Disc(new Rect(r.x,r.y,r.width,r.height),new Color(.94f,.63f,.25f));
   Flat(new Rect(r.x,r.y+r.height*.53f,r.width,r.height*.45f),new Color(.94f,.63f,.25f));
   for(int i=0;i<5;i++){float x=r.x+r.width*(.20f+i*.14f),y=r.y+r.height*(i%2==0?.27f:.45f);Line(new Vector2(x,y),new Vector2(x+r.width*.05f,y-r.height*.10f),r.width*.035f,Paper);}
  }
  static Food[] BurgerIngredients(Food dish)
  {
   if(dish==Food.DeluxeBurger)return new[]{Food.Bun,Food.CookedPatty,Food.Cheese,Food.Tomato,Food.Lettuce};
   if(dish==Food.Cheeseburger)return new[]{Food.Bun,Food.CookedPatty,Food.Cheese};
   return new[]{Food.Bun,Food.CookedPatty};
  }
  void DrawBurgerIngredients(Food dish,float x,float y)
  {
   var ingredients=BurgerIngredients(dish);
   for(int i=0;i<ingredients.Length;i++)FoodIcon(new Rect(x+16+i*22,y+85,22,23),ingredients[i],ingredients[i]==Food.Tomato||ingredients[i]==Food.Lettuce);
  }
 }
}

