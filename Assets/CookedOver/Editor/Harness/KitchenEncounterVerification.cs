using System;
using UnityEngine;
namespace CookedOver.Editor
{
    public static class KitchenEncounterVerification
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static KitchenSimulation New(KitchenChapter chapter=KitchenChapter.Classic){var s=new KitchenSimulation();s.Reset(chapter);s.Start();return s;}
        public static void Events()
        {
            var sim=New();sim.State.remaining=215;sim.Tick(.05f,null);Check(sim.State.kitchenEvent==KitchenEvent.Oil&&sim.State.eventTime>24,"first event scheduled after 25 seconds");
            var c=sim.State.chefs[0];c.position=new Vector3(0,0,1.6f);sim.Tick(.05f,new[]{new ChefInput{move=Vector2.right}});
            Check(Mathf.Abs(c.position.x-3.7f*.45f*.05f)<.001f,"oil actually slows movement");
            sim.State.equipmentRanks[2]=1;c.position=new Vector3(0,0,1.6f);sim.Tick(.05f,new[]{new ChefInput{move=Vector2.right}});
            Check(c.position.x>3.7f*.05f,"floor reward grants oil immunity and faster movement");
            sim.State.eventTime=.01f;sim.Tick(.05f,null);Check(sim.State.kitchenEvent==KitchenEvent.None&&sim.State.eventTime==0,"event ends without repeating in same wave");
            sim=New(KitchenChapter.Burgers);sim.BeginKitchenEvent(KitchenEvent.Blackout);
            var grill=sim.State.stations[sim.State.blackoutStation];grill.item=new Item(Food.RawPatty);sim.Tick(.05f,null);
            Check(grill.heat==0,"blackout stops affected cooker");c=sim.State.chefs[0];c.target=sim.State.blackoutStation;sim.Interact(c);Check(c.held.Empty,"blackout prevents collection");
            var other=sim.State.stations.Find(s=>s.kind==StationKind.Grill&&s!=grill);other.item=new Item(Food.RawPatty);sim.Tick(.05f,null);Check(other.heat>0,"other cooker continues during blackout");
            sim=New();sim.BeginKitchenEvent(KitchenEvent.Rush);sim.State.nextOrder=.01f;int count=sim.State.orders.Count;sim.Tick(.05f,null);
            Check(sim.State.orders.Count==count+1&&sim.State.nextOrder<9,"rush accelerates actual arrivals");
            sim.State.remaining=150;sim.Tick(.05f,null);Check(sim.State.boss!=null&&sim.State.boss.required==3,"boss scheduled 90 seconds into wave");
            var boss=sim.State.boss;sim.State.paused=true;float time=boss.remaining;sim.Tick(.05f,null);Check(boss.remaining==time,"pause freezes boss");
            sim=New();sim.EnableKioskMode();sim.BeginKitchenEvent(KitchenEvent.Oil);sim.BeginBossOrder();sim.Tick(.05f,null);
            Check(sim.State.boss==null&&sim.State.kitchenEvent==KitchenEvent.None,"kiosk isolated from encounters");
        }
        public static void BossProtectsNormalOrders()
        {
            var sim=New();sim.State.orders.Clear();var urgent=new OrderState{dish=Food.Salad,remaining=.01f,total=95};sim.State.orders.Add(urgent);sim.State.nextOrder=.01f;
            sim.BeginBossOrder();float bossTime=sim.State.boss.remaining;
            for(int i=0;i<20;i++)sim.Tick(.05f,null);
            Check(sim.NormalOrdersOnHold&&sim.State.orders.Count==1&&urgent.remaining==.01f&&sim.State.nextOrder==.01f&&sim.State.health==100&&sim.State.missed==0,"boss freezes urgent normal orders and arrivals without penalty");
            Check(sim.State.boss.remaining<bossTime,"boss deadline still advances");
            sim.State.boss.completed=sim.State.boss.required-1;var chef=sim.State.chefs[0];chef.target=sim.State.stations.FindIndex(s=>s.kind==StationKind.Serve);chef.held=new Item(sim.State.boss.dish);sim.Interact(chef);
            Check(!sim.NormalOrdersOnHold&&sim.State.equipmentChoice&&urgent.remaining==.01f,"boss victory preserves ordinary remaining time through reward choice");
            sim.ChooseEquipment(0);sim.Tick(.05f,null);Check(sim.State.missed==1&&sim.State.health==70,"ordinary deadlines resume after reward choice");
            sim=New();sim.State.orders.Clear();urgent=new OrderState{dish=Food.Salad,remaining=.01f};sim.State.orders.Add(urgent);sim.BeginBossOrder();sim.State.boss.remaining=.01f;sim.Tick(.05f,null);
            Check(sim.State.missed==1&&sim.State.health==60&&urgent.remaining==.01f,"boss failure does not also expire ordinary orders in same frame");
            sim.Tick(.05f,null);Check(sim.State.missed==2&&sim.State.health==30,"ordinary time resumes next frame after boss failure");
            sim=New();sim.State.orders.Clear();sim.State.nextOrder=-1;sim.BeginBossOrder();sim.Tick(.05f,null);Check(sim.State.orders.Count==0&&sim.State.nextOrder==-1,"empty ordinary queue is not refilled during boss");
            sim=New();urgent=sim.State.orders[0];urgent.remaining=.01f;sim.State.remaining=150;sim.Tick(.05f,null);Check(sim.NormalOrdersOnHold&&urgent.remaining==.01f,"automatic boss spawn freezes normal deadlines immediately");
        }
        public static void BossAndRewards()
        {
            foreach(KitchenChapter chapter in Enum.GetValues(typeof(KitchenChapter))) {
                var sim=New(chapter);sim.BeginBossOrder();var state=sim.State;var boss=state.boss;var chef=state.chefs[0];chef.target=state.stations.FindIndex(s=>s.kind==StationKind.Serve);
                for(int i=0;i<3;i++) {
                    state.experience=0;chef.held=new Item(boss.dish);sim.Interact(chef);
                    Check(boss.completed==i+1&&chef.held.Empty,"each actual boss delivery advances quantity");
                    if(i<2)Check(state.boss!=null&&!state.equipmentChoice,"partial boss delivery is not completion");
                }
                Check(state.boss==null&&state.bossesDefeated==1&&state.equipmentChoice&&state.delivered==3&&state.score>=600&&state.dirtyPlates==3,"boss completion gives score and equipment offer once");
                float clock=state.remaining;sim.Tick(.05f,new[]{new ChefInput{move=Vector2.right}});Check(state.remaining==clock,"equipment choice pauses kitchen");
                Check(!sim.ChooseEquipment(-1)&&!sim.ChooseEquipment(3),"invalid equipment rejected");
                Check(sim.ChooseEquipment(0)&&sim.CookingSpeed==1.25f&&!sim.ChooseEquipment(0),"equipment applies exactly once");
                state.equipmentChoice=true;int plates=state.cleanPlates+state.dirtyPlates;Check(sim.ChooseEquipment(1)&&state.cleanPlates==plates&&state.dirtyPlates==0,"dishwasher washes existing plates only");
                state.dirtyPlates=1;state.autoWashTime=3.99f;sim.Tick(.05f,null);Check(state.dirtyPlates==0&&state.cleanPlates==plates+1,"dishwasher automatically washes actual dirty plate");
                state.health=50;state.equipmentChoice=true;sim.ChooseEquipment(2);Check(state.health==70&&sim.MoveSpeed>3.7f,"floor heals and accelerates movement");
                sim.RestartRun();Check(sim.State.boss==null&&!sim.State.equipmentChoice&&sim.State.bossesDefeated==0&&sim.State.kitchenEvent==KitchenEvent.None&&Array.TrueForAll(sim.State.equipmentRanks,r=>r==0),"restart clears encounters and gear");
            }
            var failure=New();failure.BeginBossOrder();failure.State.boss.remaining=.01f;failure.Tick(.05f,null);
            Check(failure.State.health==60&&failure.State.missed==1&&failure.State.boss==null&&!failure.State.equipmentChoice,"expired boss penalizes once without reward");failure.Tick(.05f,null);Check(failure.State.missed==1,"no repeated boss failure");
            var queued=New();queued.BeginBossOrder();queued.State.boss.completed=2;queued.State.experience=25;
            var c=queued.State.chefs[0];c.target=queued.State.stations.FindIndex(s=>s.kind==StationKind.Serve);c.held=new Item(queued.State.boss.dish);queued.Interact(c);
            Check(queued.State.equipmentChoice&&queued.ChoosingSkill,"boss completion may queue both rewards");
            int skill=queued.State.skillChoices[0];Check(!queued.ChooseSkill(skill),"gear reward has priority");queued.ChooseEquipment(0);Check(queued.ChooseSkill(skill),"pending skill choice survives gear selection");
        }
    }
}
