using System;
using System.Collections.Generic;
using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenSimulation
 {
  public Func<OrderState,bool> KioskServing;
  public static bool IsKioskDish(Food f) => f==Food.BulgogiBurger||f==Food.ShrimpBurger||f==Food.DoubleBurger||f==Food.Cheeseburger||f==Food.Fries||f==Food.CheeseSticks||f==Food.Cola||f==Food.Sprite;
  public static bool UsesPlate(Food f) => f!=Food.Cola && f!=Food.Sprite;
  public static readonly Food[] FrozenSupplies={Food.RawFries,Food.RawCheeseSticks};
  public static readonly Food[] DrinkSupplies={Food.Cola,Food.Sprite};
  public static Food[] SuppliesFor(StationKind kind)=>kind==StationKind.DrinkSupplies?DrinkSupplies:FrozenSupplies;
  public void EnableKioskMode()
  {
   State.kiosk=true;State.chapter=KitchenChapter.Burgers;State.orders.Clear();State.stations.Clear();
   Add("BUNS",StationKind.Buns,-5,3);Add("PATTIES",StationKind.Meat,-3,3);Add("CHEESE",StationKind.Cheese,-1,3);
   Add("LETTUCE",StationKind.Lettuce,1,3);Add("SHRIMP",StationKind.Shrimp,3,3);Add("BULGOGI",StationKind.Sauce,5,3);
   Add("PLATES",StationKind.Plates,-5,-3);Add("WASH",StationKind.Sink,-3,-3);Add("GRILL",StationKind.Grill,-1,-3);
   Add("FRYER",StationKind.Fryer,1,-3);Add("SERVE",StationKind.Serve,3,-3);Add("FIRE KIT",StationKind.Extinguisher,5,-3);
   Add("ASSEMBLE",StationKind.Counter,-5,0);Add("CHOP",StationKind.Board,-1,0);Add("FROZEN SIDES",StationKind.KioskSupplies,1,0);Add("DRINKS",StationKind.DrinkSupplies,5,0);
   Add("BIN",StationKind.Trash,5,-1.5f);
  }
  public void ApplyKioskOrders(KioskTask[] tasks,HashSet<string> pending)
  {
   if(!State.kiosk||State.ended)return;
   State.orders.Clear();
   foreach(var task in tasks ?? Array.Empty<KioskTask>())
   {
    if(task.served>=task.quantity)continue;
    Food dish;bool supported=Enum.TryParse(task.recipe,out dish)&&IsKioskDish(dish);
    State.orders.Add(new OrderState{dish=supported?dish:Food.None,externalTaskId=task.task_id,externalOrderId=task.order_id,
     externalName=task.label,required=task.quantity,completed=task.served,pendingAck=pending.Contains(task.task_id),remaining=1,total=1,reward=100});
   }
  }
  bool TryServeKiosk(ChefState chef)
  {
   if(!State.kiosk)return false;
   var task=State.orders.Find(o=>o.dish==chef.held.food && !o.pendingAck && o.completed<o.required);
   if(task==null){Notice("해당 메뉴의 대기 주문이 없거나 완료 전송 중입니다.");return true;}
   if(KioskServing==null || !KioskServing(task)){Notice("서빙 기록을 저장하지 못했습니다. 다시 시도해 주세요.");return true;}
   task.pendingAck=true;State.delivered++;State.score+=100;
   if(UsesPlate(chef.held.food))State.dirtyPlates++;
   chef.held=new Item();Notice("서빙 완료 · 키오스크 주문에 반영 중");return true;
  }
  bool InteractKioskStation(ChefState chef,StationState station)
  {
   if(station.kind==StationKind.Shrimp || station.kind==StationKind.Sauce)
   {if(chef.held.Empty)chef.held=new Item(station.kind==StationKind.Shrimp?Food.RawShrimp:Food.BulgogiSauce);return true;}
   if(station.kind==StationKind.KioskSupplies||station.kind==StationKind.DrinkSupplies)
   {
    var choices=SuppliesFor(station.kind);
    if(chef.held.Empty)chef.held=new Item(choices[station.portions%choices.Length]);
    else if(Array.IndexOf(choices,chef.held.food)>=0)chef.held=new Item();
    else Notice("빈손으로 가져오세요. 버릴 물건은 BIN에 넣어 주세요.");
    return true;
   }
   if(station.kind!=StationKind.Fryer)return false;
   var h=chef.held;
   if(station.item.Empty && (h.food==Food.RawShrimp||h.food==Food.RawFries||h.food==Food.RawCheeseSticks))
   {station.item=h;station.heat=0;chef.held=new Item();return true;}
   if(station.item.food==Food.CookedShrimp&&h.Empty){chef.held=station.item;station.item=new Item();station.heat=0;return true;}
   if((station.item.food==Food.Fries||station.item.food==Food.CheeseSticks)&&h.food==Food.Plate&&h.bun==0&&h.patty==0&&h.cheese==0&&h.shrimp==0&&h.sauce==0&&h.tomato==0&&h.lettuce==0)
   {chef.held=station.item;station.item=new Item();station.heat=0;return true;}
   if(station.item.food==Food.Burnt&&h.Empty){chef.held=station.item;station.item=new Item();station.heat=0;return true;}
   Notice("새우·감자·치즈스틱을 5초 튀기세요. 사이드는 빈 접시로 가져갑니다.");return true;
  }
  void TickFryer(StationState s,float dt)
  {
   if(s.item.Empty||s.item.food==Food.Burnt)return;
   s.heat+=dt;
   if(s.heat>=16){s.item=new Item(Food.Burnt);s.fire=1;Notice("FIRE! Hold work with the extinguisher.");return;}
   if(s.heat<5)return;
   if(s.item.food==Food.RawShrimp)s.item.food=Food.CookedShrimp;
   if(s.item.food==Food.RawFries)s.item.food=Food.Fries;
   if(s.item.food==Food.RawCheeseSticks)s.item.food=Food.CheeseSticks;
  }
 }
 [Serializable] public class KioskTask
 {public string task_id,order_id,recipe,label,order_type;public int quantity,served;}
}
