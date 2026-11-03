using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenSimulation
 {
  public const float GrillCookSeconds=6, GrillBurnSeconds=14;
  void BuildBurgerLayout()
  {
   Add("BUNS",StationKind.Buns,-5,3);Add("PATTIES",StationKind.Meat,-3,3);Add("CHEESE",StationKind.Cheese,-1,3);
   Add("TOMATO",StationKind.Tomato,1,3);Add("LETTUCE",StationKind.Lettuce,3,3);Add("BIN",StationKind.Trash,5,3);
   Add("PLATES",StationKind.Plates,-5,-3);Add("WASH",StationKind.Sink,-3,-3);Add("GRILL 1",StationKind.Grill,-1,-3);Add("GRILL 2",StationKind.Grill,1,-3);
   Add("SERVE",StationKind.Serve,3,-3);Add("FIRE KIT",StationKind.Extinguisher,5,-3);
   Add("CHOP",StationKind.Board,-1,0);Add("CHOP",StationKind.Board,1,0);
   Add("ASSEMBLE",StationKind.Counter,-5,0);Add("ASSEMBLE",StationKind.Counter,5,0);
  }
  void AddBurgerOrder()
  {
   var dishes=ThreatTier==0?new[]{Food.Burger,Food.Cheeseburger,Food.DeluxeBurger}:ThreatTier==1?new[]{Food.Burger,Food.Cheeseburger,Food.DeluxeBurger,Food.Cheeseburger,Food.DeluxeBurger}:new[]{Food.Cheeseburger,Food.DeluxeBurger,Food.DeluxeBurger};
   Food dish=dishes[State.sequence++%dishes.Length];var order=new OrderState{dish=dish,reward=dish==Food.Burger?100:dish==Food.Cheeseburger?140:190,remaining=115,total=115};ConfigureOrder(order);State.orders.Add(order);
  }
  void TickGrill(StationState grill,float dt)
  {
   if(grill.fire>0){grill.fire=Mathf.Min(1,grill.fire+dt*.08f*FireGrowth);return;}
   if(grill.item.food!=Food.RawPatty && grill.item.food!=Food.CookedPatty)return;
   grill.heat+=dt;
   if(grill.heat>=GrillBurnSeconds+BurnGrace){grill.item=new Item(Food.Burnt);grill.fire=1;State.combo=0;Notice("FIRE! Hold work with the extinguisher.");}
   else if(grill.heat>=GrillCookSeconds/CookingSpeed && grill.item.food==Food.RawPatty){grill.item.food=Food.CookedPatty;Notice("Patty ready! Collect before it burns.");}
  }
  void InteractGrill(ChefState chef,StationState grill)
  {
   if(chef.held.Empty && !grill.item.Empty)
   {if(grill.item.food==Food.CookedPatty)grill.item.flameKissed|=FlameWindow(grill.heat,GrillBurnSeconds+BurnGrace);chef.held=grill.item;grill.item=new Item();grill.heat=0;return;}
   if(chef.held.food==Food.RawPatty && grill.item.Empty)
   {grill.item=chef.held;chef.held=new Item();grill.heat=0;return;}
   if(!grill.item.Empty && TryBurger(chef.held,grill.item))
   {chef.held.flameKissed|=grill.item.food==Food.CookedPatty&&FlameWindow(grill.heat,GrillBurnSeconds+BurnGrace);grill.item=new Item();grill.heat=0;return;}
   Notice("Grill a raw patty for 6s; burns at 14s.");
  }
  public static bool IsBurger(Food food)=>food==Food.Burger||food==Food.Cheeseburger||food==Food.DeluxeBurger||food==Food.BulgogiBurger||food==Food.ShrimpBurger||food==Food.DoubleBurger;
  static bool TryBurger(Item plate,Item ingredient)
  {
   if(plate.food!=Food.Plate && !IsBurger(plate.food))return false;
   switch(ingredient.food)
   {
    case Food.Bun:if(plate.bun!=0)return false;plate.bun=1;break;
    case Food.CookedPatty:if(plate.patty>=2||plate.shrimp!=0)return false;plate.patty++;break;
    case Food.CookedShrimp:if(plate.shrimp!=0||plate.patty!=0)return false;plate.shrimp=1;break;
    case Food.BulgogiSauce:if(plate.sauce!=0)return false;plate.sauce=1;break;
    case Food.Cheese:if(plate.cheese!=0)return false;plate.cheese=1;break;
    case Food.Tomato:if(!ingredient.chopped||plate.tomato!=0)return false;plate.tomato=1;break;
    case Food.Lettuce:if(!ingredient.chopped||plate.lettuce!=0)return false;plate.lettuce=1;break;
    default:return false;
   }
   plate.flameKissed|=ingredient.flameKissed;
   // Extra or incomplete toppings never masquerade as a simpler completed recipe.
   plate.food=Food.Plate;
   if(plate.bun==1 && plate.shrimp==1 && plate.patty==0 && plate.lettuce==1 && plate.tomato==0 && plate.cheese==0 && plate.sauce==0)plate.food=Food.ShrimpBurger;
   if(plate.bun==1 && plate.patty==2 && plate.cheese==1 && plate.lettuce==0 && plate.tomato==0 && plate.shrimp==0 && plate.sauce==0)plate.food=Food.DoubleBurger;
   if(plate.bun==1 && plate.patty==1 && plate.shrimp==0 && plate.sauce==1 && plate.lettuce==1 && plate.tomato==0 && plate.cheese==0)plate.food=Food.BulgogiBurger;
   if(plate.bun==1 && plate.patty==1 && plate.shrimp==0 && plate.sauce==0)
   {
    if(plate.cheese==1 && plate.tomato==1 && plate.lettuce==1)plate.food=Food.DeluxeBurger;
    else if(plate.tomato==0 && plate.lettuce==0)plate.food=plate.cheese==1?Food.Cheeseburger:Food.Burger;
   }
   return true;
  }
 }
}
