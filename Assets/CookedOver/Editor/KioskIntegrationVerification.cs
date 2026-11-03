using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace CookedOver.Editor
{
 public static class KioskIntegrationVerification
 {
  [MenuItem("CookedOver/Verify Kiosk Integration")]
  public static void Run()
  {
   BurgerChapterVerification.Run();
   var sim=new KitchenSimulation();sim.Reset(KitchenChapter.Burgers);sim.EnableKioskMode();sim.Start();
   var state=sim.State;var chef=state.chefs[0];
   Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception("KIOSK VERIFY: "+text);};
   check(state.orders.Count==0,"no random orders in kiosk mode");
   Action<StationState> interact=s=>{chef.target=state.stations.IndexOf(s);sim.Interact(chef);};
   Func<StationKind,StationState> station=k=>state.stations.Find(s=>s.kind==k);
   Action<StationKind> supply=k=>interact(station(k));
   Action<float> advance=seconds=>{for(int i=0;i<Mathf.CeilToInt(seconds/.05f);i++)sim.Tick(.05f,new ChefInput[4]);};
   check(station(StationKind.KioskSupplies)!=null&&station(StationKind.DrinkSupplies)!=null,"distinct frozen and drink stations");
   check(Array.TrueForAll(KitchenSimulation.FrozenSupplies,f=>f==Food.RawFries||f==Food.RawCheeseSticks),"freezer contains no drinks");
   check(Array.TrueForAll(KitchenSimulation.DrinkSupplies,f=>f==Food.Cola||f==Food.Sprite),"drink station contains no frozen sides");
   foreach(var kind in new[]{StationKind.KioskSupplies,StationKind.DrinkSupplies})
   {
    var supplyStation=station(kind);var choices=KitchenSimulation.SuppliesFor(kind);
    supplyStation.portions=0;sim.Work(chef,supplyStation,.36f);check(supplyStation.portions==1,"F switches within station group");
    interact(supplyStation);check(chef.held.food==choices[1],"E picks station selection");interact(supplyStation);check(chef.held.Empty,"same-group item can be returned");
   }
   chef.held=new Item(Food.CookedPatty);interact(station(StationKind.KioskSupplies));check(chef.held.food==Food.CookedPatty,"freezer cannot silently discard cooked food");supply(StationKind.Trash);
   var tasks=new List<KioskTask>();var dishes=new[]{Food.BulgogiBurger,Food.Cheeseburger,Food.ShrimpBurger,Food.DoubleBurger,Food.Fries,Food.CheeseSticks,Food.Cola,Food.Sprite};
   foreach(var dish in dishes)tasks.Add(new KioskTask{task_id=Guid.NewGuid().ToString(),order_id="TEST",recipe=dish.ToString(),label=KitchenGame.Recipe(dish),quantity=2});
   sim.ApplyKioskOrders(tasks.ToArray(),new HashSet<string>());
   sim.KioskServing=order=>{var task=tasks.Find(t=>t.task_id==order.externalTaskId);check(task.served==order.completed,"expected revision on serve");task.served++;return true;};
   Action<StationKind> plateIngredient=k=>{supply(k);interact(station(StationKind.Counter));};
   Action beef=()=>{supply(StationKind.Meat);interact(station(StationKind.Grill));advance(6.1f);check(station(StationKind.Grill).item.food==Food.CookedPatty,"beef cooking");interact(station(StationKind.Grill));interact(station(StationKind.Counter));};
   Action lettuce=()=>{supply(StationKind.Lettuce);interact(station(StationKind.Board));sim.Work(chef,station(StationKind.Board),KitchenSimulation.ChopSeconds+.1f);interact(station(StationKind.Board));interact(station(StationKind.Counter));};
   Action<Food> sideSupply=f=>{var kind=(f==Food.Cola||f==Food.Sprite)?StationKind.DrinkSupplies:StationKind.KioskSupplies;var s=station(kind);s.portions=Array.IndexOf(KitchenSimulation.SuppliesFor(kind),f);interact(s);};
   foreach(var dish in dishes)
   {
    if(state.cleanPlates==0)for(int i=0;i<4;i++)sim.Work(chef,station(StationKind.Sink),KitchenSimulation.WashSeconds+.1f);
    if(KitchenSimulation.IsBurger(dish))
    {
     supply(StationKind.Plates);interact(station(StationKind.Counter));
     if(dish==Food.ShrimpBurger){supply(StationKind.Shrimp);interact(station(StationKind.Fryer));advance(5.1f);check(station(StationKind.Fryer).item.food==Food.CookedShrimp,"shrimp fries");interact(station(StationKind.Fryer));interact(station(StationKind.Counter));}
     else beef();
     plateIngredient(StationKind.Buns);
     if(dish==Food.DoubleBurger)beef();
     if(dish==Food.Cheeseburger||dish==Food.DoubleBurger)plateIngredient(StationKind.Cheese);
     if(dish==Food.BulgogiBurger)plateIngredient(StationKind.Sauce);
     if(dish==Food.BulgogiBurger||dish==Food.ShrimpBurger)lettuce();
     interact(station(StationKind.Counter));
    }
    else if(dish==Food.Fries||dish==Food.CheeseSticks)
    {
     sideSupply(dish==Food.Fries?Food.RawFries:Food.RawCheeseSticks);interact(station(StationKind.Fryer));advance(5.1f);
     supply(StationKind.Plates);interact(station(StationKind.Fryer));
    }
    else sideSupply(dish);
    check(chef.held.food==dish,"exact recipe: "+dish);
    var held=JsonUtility.FromJson<Item>(JsonUtility.ToJson(chef.held));check(held.food==dish&&held.shrimp==chef.held.shrimp&&held.sauce==chef.held.sauce,"new ingredients serialize");
    int dirty=state.dirtyPlates;supply(StationKind.Serve);
    check(chef.held.Empty&&tasks.Find(t=>t.recipe==dish.ToString()).served==1,"serve exact menu: "+dish);
    check(state.dirtyPlates==dirty+(KitchenSimulation.UsesPlate(dish)?1:0),"drink must not create a plate");
    check(state.orders.Find(o=>o.dish==dish).pendingAck,"pending acknowledgement blocks duplicate serving");
    sim.ApplyKioskOrders(tasks.ToArray(),new HashSet<string>());
   }
   sim.ApplyKioskOrders(Array.Empty<KioskTask>(),new HashSet<string>());check(state.orders.Count==0,"cancelled/completed tasks are removed during service");
   sim.ApplyKioskOrders(tasks.ToArray(),new HashSet<string>());
   advance(250);check(state.ended&&state.remaining==0&&state.orders.Count==8&&state.orders.TrueForAll(o=>o.required==2&&o.completed==1),"four-minute service ends without completing or losing external tasks");
   sim.ApplyKioskOrders(Array.Empty<KioskTask>(),new HashSet<string>());check(state.orders.Count==8,"results remain stable after service ends");
   Debug.Log("KIOSK INTEGRATION PASS: 8 exact recipes, plate conservation, quantity and pending ack, snapshots, timed service, no random orders or external task expiry; classic/burger regression passed.");
  }
 }
}
