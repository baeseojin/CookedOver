using System;
using System.Collections.Generic;
using UnityEngine;
namespace CookedOver.Editor
{
    public static class KitchenModeVerification
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static bool Vessel(Item item)=>item.food==Food.Plate||item.food==Food.DirtyPlate||item.Dish&&KitchenSimulation.UsesPlate(item.food);
        static int PlateCount(KitchenState state){int total=state.cleanPlates+state.dirtyPlates;foreach(var chef in state.chefs)if(Vessel(chef.held))total++;foreach(var station in state.stations)if(Vessel(station.item))total++;foreach(var loose in state.loose)if(Vessel(loose.item))total++;return total;}
        public static void Plates()
        {
            var sim=new KitchenSimulation();sim.Start();var state=sim.State;var chef=state.chefs[0];
            int supply=state.stations.FindIndex(s=>s.kind==StationKind.Plates),serve=state.stations.FindIndex(s=>s.kind==StationKind.Serve),sink=state.stations.FindIndex(s=>s.kind==StationKind.Sink);
            state.equipmentChoice=true;Check(sim.ChooseEquipment(1)&&PlateCount(state)==4,"selecting dishwasher does not create plates");
            for(int i=0;i<20;i++){
                chef.target=supply;sim.Interact(chef);Check(chef.held.food==Food.Plate&&PlateCount(state)==4,"taking a plate conserves total");
                // The existing plate is prepared as a completed dish; no extra vessel is inserted.
                chef.held.food=Food.Salad;state.orders.Clear();state.orders.Add(new OrderState{dish=Food.Salad,reward=80});state.experience=0;
                chef.target=serve;sim.Interact(chef);Check(state.dirtyPlates==1&&PlateCount(state)==4,"serving returns the same vessel as dirty");
                state.autoWashTime=4;sim.Tick(.05f,null);Check(state.cleanPlates==4&&state.dirtyPlates==0&&PlateCount(state)==4,"automatic wash preserves exactly four plates over repeated deliveries");
            }
            chef.target=supply;sim.Interact(chef);chef.held.food=Food.DirtyPlate;
            state.equipmentChoice=true;sim.ChooseEquipment(1);Check(chef.held.food==Food.DirtyPlate&&state.cleanPlates==3&&PlateCount(state)==4,"upgrade cannot duplicate a held dirty plate");
            chef.target=sink;sim.Interact(chef);state.skillChoices.Add((int)KitchenSkill.Wash);sim.ChooseSkill((int)KitchenSkill.Wash);
            Check(state.cleanPlates==4&&state.dirtyPlates==0&&PlateCount(state)==4,"wash card only converts existing dirty plates");
            state.skillChoices.Add((int)KitchenSkill.Recovery);sim.ChooseSkill((int)KitchenSkill.Recovery);Check(PlateCount(state)==4,"healing card cannot create plates");
            sim.RestartRun();Check(PlateCount(sim.State)==4,"restart restores four plates");
        }
        public static void Modes()
        {
            var sim=new KitchenSimulation();sim.Start();sim.State.remaining=.01f;sim.Tick(.05f,null);
            Check(!sim.State.ended&&sim.State.wave==2,"roguelike has no overall service end");
            sim.State.orders.Clear();sim.State.orders.Add(new OrderState{remaining=.01f});sim.Tick(.05f,null);
            Check(sim.State.missed==1&&sim.State.health==70,"roguelike order deadlines preserved");
            sim.BeginBossOrder();sim.State.boss.remaining=.01f;sim.Tick(.05f,null);Check(sim.State.boss==null&&sim.State.health==30,"boss deadline preserved");
            sim=new KitchenSimulation();sim.EnableKioskMode();sim.Start();var state=sim.State;
            sim.ApplyKioskOrders(new[]{new KioskTask{task_id="local-test",order_id="local",recipe="Cheeseburger",quantity=2}},new HashSet<string>());
            int acknowledgements=0;sim.KioskServing=order=>{acknowledgements++;return true;};
            sim.GrantExperience(1000);sim.BeginBossOrder();sim.BeginKitchenEvent(KitchenEvent.Oil);
            Check(state.level==1&&!sim.ChoosingReward&&state.boss==null&&state.kitchenEvent==KitchenEvent.None,"kiosk is pure cooking without growth or encounters");
            state.remaining=.01f;sim.Tick(.05f,null);Check(state.ended&&state.remaining==0&&state.orders.Count==1&&state.orders[0].completed==0&&acknowledgements==0,"kiosk timer ends locally without falsely fulfilling external orders");
            var chef=state.chefs[0];chef.target=state.stations.FindIndex(s=>s.kind==StationKind.Serve);chef.held=new Item(Food.Cheeseburger);sim.Interact(chef);Check(acknowledgements==0,"cannot submit external orders after service ends");
            sim.RestartRun();Check(sim.State.kiosk&&sim.State.remaining==240&&sim.State.level==1&&sim.State.orders.Count==0,"manual kiosk restart starts a clean timed service");
        }
    }
}
