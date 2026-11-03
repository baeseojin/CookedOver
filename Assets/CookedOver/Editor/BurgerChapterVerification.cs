using System;
using UnityEditor;
using UnityEngine;
namespace CookedOver.Editor
{
 public static class BurgerChapterVerification
 {
  [MenuItem("CookedOver/Verify Burger Chapter")]
  public static void Run()
  {
   var sim=new KitchenSimulation();sim.Reset(KitchenChapter.Burgers);sim.Start();var state=sim.State;var chef=state.chefs[0];
   Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("Burger chapter verification failed: "+label);};
   StationState grill=state.stations.Find(s=>s.kind==StationKind.Grill),counter=state.stations.Find(s=>s.kind==StationKind.Counter);
   Action<StationState> interact=s=>{chef.target=state.stations.IndexOf(s);sim.Interact(chef);};
   Action<StationKind> supply=kind=>interact(state.stations.Find(s=>s.kind==kind));
   Action<float> advance=seconds=>{for(int i=0;i<Mathf.CeilToInt(seconds/.05f);i++)sim.Tick(.05f,new ChefInput[4]);};
   Action<StationKind> chopped=kind=>
   {
    supply(kind);var board=state.stations.Find(s=>s.kind==StationKind.Board&&s.item.Empty);interact(board);
    sim.Work(chef,board,KitchenSimulation.ChopSeconds+.01f);check(board.item.chopped,"chopped topping");interact(board);interact(counter);
   };
   check(state.chapter==KitchenChapter.Burgers&&state.stations.Count==16,"burger kitchen layout");
   check(state.orders[0].dish==Food.Burger&&state.orders[1].dish==Food.Cheeseburger,"chapter-specific initial orders");
   for(int recipe=0;recipe<3;recipe++)
   {
    supply(StationKind.Plates);check(chef.held.food==Food.Plate,"take clean plate");interact(counter);
    supply(StationKind.Meat);interact(counter);check(chef.held.food==Food.RawPatty&&counter.item.patty==0,"raw patty cannot be plated");
    interact(grill);check(chef.held.Empty&&grill.item.food==Food.RawPatty,"load grill");
    advance(5.5f);check(grill.item.food==Food.RawPatty,"undercooked patty stays raw");
    advance(.65f);check(grill.item.food==Food.CookedPatty,"patty cooks after six seconds");
    interact(grill);interact(counter);supply(StationKind.Buns);interact(counter);
    check(counter.item.food==Food.Burger,"basic burger assembled in flexible ingredient order");
    if(recipe>=1){supply(StationKind.Cheese);interact(counter);check(counter.item.food==Food.Cheeseburger,"cheeseburger upgrade");}
    if(recipe==2)
    {
     supply(StationKind.Cheese);interact(counter);check(chef.held.food==Food.Cheese&&counter.item.cheese==1,"duplicate topping rejected");supply(StationKind.Trash);
     chopped(StationKind.Tomato);check(!counter.item.Dish,"half-added deluxe toppings cannot serve as a basic burger");
     chopped(StationKind.Lettuce);check(counter.item.food==Food.DeluxeBurger,"deluxe burger completes");
    }
    interact(counter);Food expected=recipe==0?Food.Burger:recipe==1?Food.Cheeseburger:Food.DeluxeBurger;check(chef.held.food==expected,"pick assembled burger");
    if(!state.orders.Exists(o=>o.dish==expected))state.orders.Add(new OrderState{dish=expected,reward=190});
    state.experience=0;int delivered=state.delivered,score=state.score;supply(StationKind.Serve);check(state.delivered==delivered+1&&state.score>score&&chef.held.Empty,"serve matching recipe and award score");
   }
   check(state.dirtyPlates==3&&state.cleanPlates==1,"plate conservation after three burger deliveries");
   var sink=state.stations.Find(s=>s.kind==StationKind.Sink);for(int i=0;i<3;i++)sim.Work(chef,sink,KitchenSimulation.WashSeconds+.01f);
   check(state.cleanPlates==4&&state.dirtyPlates==0,"wash burger plates");
   supply(StationKind.Meat);interact(grill);advance(14.2f);check(grill.fire>0&&grill.item.food==Food.Burnt,"overcooking starts grill fire");
   interact(grill);check(chef.held.Empty,"burning grill cannot be collected");supply(StationKind.Extinguisher);sim.Work(chef,grill,2);check(grill.fire==0,"extinguish grill");
   supply(StationKind.Extinguisher);interact(grill);check(chef.held.food==Food.Burnt&&grill.item.Empty&&grill.heat==0,"remove burnt patty");supply(StationKind.Trash);
   chef.held=new Item(Food.Plate){bun=1,patty=1,cheese=1,tomato=1};grill.item=new Item(Food.CookedPatty);grill.heat=8;
   var restored=JsonUtility.FromJson<KitchenState>(JsonUtility.ToJson(state));
   check(restored.chapter==KitchenChapter.Burgers&&restored.chefs[0].held.bun==1&&restored.chefs[0].held.cheese==1&&restored.chefs[0].held.tomato==1,"chapter and burger assembly survive network snapshot serialization");
   check(restored.stations[state.stations.IndexOf(grill)].item.food==Food.CookedPatty,"grill survives network snapshot serialization");
   sim.Reset(KitchenChapter.Classic);check(sim.State.stations.Count==14&&sim.State.stations.Exists(s=>s.kind==StationKind.Stove),"return to classic chapter");
   KitchenStageBuilder.Verify();
   Debug.Log("BURGER CHAPTER PASS: all three recipes, supply, chop, raw rejection, grill timing, topping rules, serving, dishes, fire cleanup, snapshot serialization and classic regression.");
  }
 }
}
