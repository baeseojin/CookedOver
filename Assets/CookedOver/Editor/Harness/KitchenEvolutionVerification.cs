using System;
using UnityEngine;
namespace CookedOver.Editor
{
    public static class KitchenEvolutionVerification
    {
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
        static KitchenSimulation New(KitchenChapter chapter=KitchenChapter.Classic){var sim=new KitchenSimulation();sim.Reset(chapter);sim.Start();return sim;}
        static void Ranks(KitchenSimulation sim,KitchenSkill main,int rank,KitchenSkill partner){sim.State.skillRanks[(int)main]=rank;sim.State.skillRanks[(int)partner]=1;}
        static void Target(KitchenSimulation sim,StationKind kind){sim.State.chefs[0].target=sim.State.stations.FindIndex(s=>s.kind==kind);}
        public static void Automation()
        {
            var sim=New();Ranks(sim,KitchenSkill.Knife,2,KitchenSkill.Heat);Check(!sim.Evolved(KitchenEvolution.Automation),"R2 does not evolve");
            sim.State.skillChoices.Add((int)KitchenSkill.Knife);Check(sim.ChooseSkill((int)KitchenSkill.Knife)&&sim.Evolved(KitchenEvolution.Automation),"actual R3 selection evolves");
            var board=sim.State.stations.Find(s=>s.kind==StationKind.Board);board.item=new Item(Food.Tomato);var chef=sim.State.chefs[0];chef.position=new Vector3(-5,0,0);sim.Tick(.05f,null);Check(board.progress==0,"distant board is not automated");
            chef.position=board.position+Vector3.back*1.4f;sim.State.chefs[1].active=true;sim.State.chefs[1].position=board.position+Vector3.back*1.7f;sim.Tick(.05f,null);
            Check(Mathf.Abs(board.progress-.1625f)<.001f,"two nearby chefs do not duplicate automation rate");
            float progress=board.progress;sim.State.paused=true;sim.Tick(.05f,null);Check(board.progress==progress,"pause freezes automation");sim.State.paused=false;
            for(int i=0;i<14;i++)sim.Tick(.05f,null);Check(board.item.chopped&&board.progress==0&&sim.State.cleanPlates==4,"automation completes chopping without creating plates");
            var restored=new KitchenSimulation{State=KitchenSimulation.FromSnapshot(JsonUtility.ToJson(sim.State))};Check(restored.Evolved(KitchenEvolution.Automation),"evolution derived from received snapshot");
            sim.RestartRun();Check(sim.EvolutionMask==0,"restart clears evolutions");sim.EnableKioskMode();Ranks(sim,KitchenSkill.Knife,3,KitchenSkill.Heat);Check(!sim.Evolved(KitchenEvolution.Automation),"kiosk never evolves");
        }
        public static void Flame()
        {
            var sim=New(KitchenChapter.Burgers);Ranks(sim,KitchenSkill.Heat,3,KitchenSkill.Safety);var state=sim.State;var chef=state.chefs[0];var grill=state.stations.Find(s=>s.kind==StationKind.Grill);
            grill.item=new Item(Food.CookedPatty);grill.heat=18.9f;Target(sim,StationKind.Grill);sim.Interact(chef);Check(!chef.held.flameKissed,"early pickup has no flame bonus");
            chef.held=new Item();grill.item=new Item(Food.CookedPatty);grill.heat=19.1f;sim.Interact(chef);Check(chef.held.flameKissed,"last three seconds marks cooked patty");
            Target(sim,StationKind.Counter);sim.Interact(chef);Target(sim,StationKind.Plates);sim.Interact(chef);Target(sim,StationKind.Counter);sim.Interact(chef);sim.Interact(chef);
            Target(sim,StationKind.Buns);sim.Interact(chef);Target(sim,StationKind.Counter);sim.Interact(chef);sim.Interact(chef);
            Check(chef.held.food==Food.Burger&&chef.held.flameKissed,"flame mark survives burger assembly");
            state.orders.Clear();state.orders.Add(new OrderState{dish=Food.Burger,reward=100,remaining=100,total=100});Target(sim,StationKind.Serve);sim.Interact(chef);
            Check(state.score==260&&state.cleanPlates==3&&state.dirtyPlates==1,"flame delivery doubles actual reward and preserves plates");
            var direct=New(KitchenChapter.Burgers);Ranks(direct,KitchenSkill.Heat,3,KitchenSkill.Safety);var directGrill=direct.State.stations.Find(s=>s.kind==StationKind.Grill);directGrill.item=new Item(Food.CookedPatty);directGrill.heat=19.1f;Target(direct,StationKind.Plates);direct.Interact(direct.State.chefs[0]);Target(direct,StationKind.Grill);direct.Interact(direct.State.chefs[0]);Check(direct.State.chefs[0].held.flameKissed,"plating directly from grill preserves flame bonus");
            sim=New();Ranks(sim,KitchenSkill.Heat,3,KitchenSkill.Safety);chef=sim.State.chefs[0];var stove=sim.State.stations.Find(s=>s.kind==StationKind.Stove);stove.soup=Food.Tomato;stove.portions=3;stove.heat=23.1f;
            Target(sim,StationKind.Plates);sim.Interact(chef);Target(sim,StationKind.Stove);sim.Interact(chef);Check(chef.held.food==Food.TomatoSoup&&chef.held.flameKissed,"soup pickup receives flame mark");
            var clone=KitchenSimulation.FromSnapshot(JsonUtility.ToJson(sim.State));Check(clone.chefs[0].held.flameKissed,"flame mark survives real snapshot decoder");
            sim=New(KitchenChapter.Burgers);Ranks(sim,KitchenSkill.Heat,3,KitchenSkill.Safety);grill=sim.State.stations.Find(s=>s.kind==StationKind.Grill);grill.item=new Item(Food.CookedPatty);grill.heat=22.1f;sim.Tick(.05f,null);Target(sim,StationKind.Grill);sim.Interact(sim.State.chefs[0]);Check(grill.fire>0&&grill.item.food==Food.Burnt&&sim.State.chefs[0].held.Empty,"burnt food cannot claim flame bonus");
        }
        static KitchenSimulation Courier()
        {
            var sim=New();Ranks(sim,KitchenSkill.FleetFeet,3,KitchenSkill.Dash);var c=sim.State.chefs[0];Target(sim,StationKind.Plates);sim.Interact(c);c.held.food=Food.Salad;
            sim.State.orders.Clear();sim.State.orders.Add(new OrderState{dish=Food.Salad,reward=80,remaining=100,total=100});c.position=new Vector3(3,0,-.8f);c.facing=Vector3.back;c.dashTime=.1f;return sim;
        }
        public static void DashDelivery()
        {
            var sim=Courier();sim.Toss(sim.State.chefs[0]);Check(sim.State.delivered==1&&sim.State.dirtyPlates==1&&sim.State.cleanPlates==3&&sim.State.loose.Count==0,"dash toss submits matching order once");
            sim=Courier();sim.State.chefs[0].dashTime=0;sim.Toss(sim.State.chefs[0]);Check(sim.State.delivered==0&&sim.State.loose.Count==1,"ordinary throw remains an ordinary throw");
            sim=Courier();sim.State.chefs[0].facing=Vector3.forward;sim.Toss(sim.State.chefs[0]);Check(sim.State.delivered==0,"wrong direction cannot dash serve");
            sim=Courier();sim.State.chefs[0].position=new Vector3(-3,0,-.8f);sim.Toss(sim.State.chefs[0]);Check(sim.State.delivered==0,"out of range cannot dash serve");
            sim=Courier();sim.State.orders.Clear();sim.Toss(sim.State.chefs[0]);Check(sim.State.delivered==0&&sim.State.loose.Count==1&&sim.State.health==100,"unmatched throw preserves food without remote penalty");
            sim=Courier();sim.State.stations.Add(new StationState{kind=StationKind.Counter,position=new Vector3(3,0,-1.5f)});sim.Toss(sim.State.chefs[0]);Check(sim.State.delivered==0,"obstructed line cannot dash serve");
            sim=Courier();sim.BeginBossOrder();sim.State.boss.dish=Food.Salad;sim.Toss(sim.State.chefs[0]);Check(sim.State.boss.completed==1&&sim.State.delivered==1,"dash serving advances boss quantity");
        }
    }
}
