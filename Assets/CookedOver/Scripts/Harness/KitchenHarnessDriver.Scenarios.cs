using System;
using System.Collections;
using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenHarnessDriver
 {
  bool SlotsReady()
  {
   if(game.HarnessConnections!=4)return false;var slots=new System.Collections.Generic.HashSet<int>();
   foreach(ulong id in game.HarnessClientIds)if(id!=0)slots.Add(game.HarnessSlot(id));return slots.Count==3&&!slots.Contains(-1)&&!slots.Contains(0);
  }
  ulong IdInSlot(int slot){foreach(ulong id in game.HarnessClientIds)if(game.HarnessSlot(id)==slot)return id;throw new Exception("Slot owner missing: "+slot);}
  Food SupplyFor(int slot)=>chapter==KitchenChapter.Burgers?new[]{Food.None,Food.Bun,Food.RawPatty,Food.Cheese}[slot]:new[]{Food.None,Food.Tomato,Food.Onion,Food.Lettuce}[slot];
  IEnumerator HostScenario()
  {
   Check(game.HarnessListening&&game.HarnessConnections==1,"host-started");Signal("host-ready");
   yield return Await(SlotsReady,"three clients connect");AssignSlots();SetPhase("capacity");
   yield return Await(()=>EveryClientMarked("initial"),"clients receive initial snapshot");
   Check(game.State.chefs.FindAll(c=>c.active).Count==4,"four-unique-chefs");Signal("ready-for-overflow");
   yield return Await(()=>System.IO.File.Exists(FileAt("overflow.json")),"overflow client rejected");
   var overflow=JsonUtility.FromJson<KitchenHarnessReport>(System.IO.File.ReadAllText(FileAt("overflow.json")));
   Check(overflow.status=="passed"&&game.HarnessConnections==4,"capacity-rejects-fifth-player",overflow.error);
   SetPhase("forgery",IdInSlot(1));yield return Await(()=>Marked("forgery-sent"),"client sends forged state");
   yield return Await(()=>game.HarnessRejectedStates>0,"server rejects client state");
   Check(game.State.score==0&&game.HarnessRejectedStates>0,"host-authority-rejects-client-score");
   for(int slot=1;slot<4;slot++){var c=game.State.chefs[slot];c.position=new Vector3(-7+slot*2,0,1.6f);c.facing=Vector3.forward;c.held=new Item();}
   SetPhase("pickup");
   yield return Await(()=>game.State.chefs[1].held.food==SupplyFor(1)&&game.State.chefs[2].held.food==SupplyFor(2)&&game.State.chefs[3].held.food==SupplyFor(3),"remote E inputs collect supplies");
   yield return Await(()=>EveryClientMarked("picked"),"pickup broadcast reaches all clients");
   Check(true,"remote-input-and-item-ownership");
   var before=new Vector3[4];for(int i=1;i<4;i++)before[i]=game.State.chefs[i].position;
   SetPhase("move");yield return Delay(.45f);
   for(int i=1;i<4;i++)Check(Vector3.Distance(before[i],game.State.chefs[i].position)>.3f,"remote-movement-slot-"+i);
   for(int i=1;i<4;i++)game.State.chefs[i].position=new Vector3(-6.5f+i*2,0,1.6f);
   SetPhase("stale");yield return Delay(1.15f);
   var stopped=new Vector3[4];for(int i=1;i<4;i++)stopped[i]=game.State.chefs[i].position;
   yield return Delay(.30f);
   for(int i=1;i<4;i++)Check(Vector3.Distance(stopped[i],game.State.chefs[i].position)<.05f&&game.HarnessInputAge(i)>.4f,"stale-input-stops-slot-"+i);
   ulong target=IdInSlot(1);var server=game.State;server.orders.Clear();Food recipe=chapter==KitchenChapter.Burgers?Food.Burger:Food.Salad;
   server.orders.Add(new OrderState{dish=recipe,reward=chapter==KitchenChapter.Burgers?100:80,remaining=95,total=95});server.nextOrder=999;server.remaining=180;server.experience=25;
   server.chefs[1].position=new Vector3(3,0,-1.6f);server.chefs[1].facing=Vector3.back;
   server.chefs[1].held=chapter==KitchenChapter.Burgers?new Item(recipe){bun=1,patty=1}:new Item(recipe){tomato=1,lettuce=1};
   SetPhase("serve",target);yield return Await(()=>server.delivered==1&&server.score>0&&server.dirtyPlates==1,"remote serving updates host");
   yield return Await(()=>EveryClientMarked("served"),"delivery broadcast reaches every client");
   Check(true,"remote-serving-score-orders-and-dishes");
   SetPhase("cards");
   yield return Await(()=>EveryClientMarked("cards"),"all clients see three cards and reject local choices");
   float frozen=server.remaining;yield return Delay(.3f);
   Check(server.level==2&&server.experience==0&&game.simulation.ChoosingSkill&&server.remaining==frozen,"level-up-freezes-host-clock");
   Check(game.ChooseTeamSkill(server.skillChoices[0]),"host-selects-offered-skill");
   SetPhase("skill-selected");
   yield return Await(()=>EveryClientMarked("skill"),"all clients receive host skill selection");
   game.simulation.BeginKitchenEvent(KitchenEvent.Blackout);game.simulation.BeginBossOrder();server.boss.completed=2;server.experience=0;
   server.orders.Clear();var ordinaryDuringBoss=new OrderState{dish=recipe,reward=80,remaining=5,total=95};server.orders.Add(ordinaryDuringBoss);server.nextOrder=1;
   server.chefs[1].position=new Vector3(3,0,-1.6f);server.chefs[1].facing=Vector3.back;server.chefs[1].held=new Item(server.boss.dish);
   float bossClock=server.boss.remaining;SetPhase("boss-hold");yield return Delay(.4f);
   yield return Await(()=>EveryClientMarked("boss-hold"),"clients receive paused normal-order clocks");
   Check(ordinaryDuringBoss.remaining==5&&server.nextOrder==1&&server.boss.remaining<bossClock,"boss-freezes-normal-deadlines-and-arrivals");
   SetPhase("boss",IdInSlot(1));yield return Await(()=>server.equipmentChoice&&server.bossesDefeated==1,"remote serving completes boss");
   yield return Await(()=>EveryClientMarked("boss"),"all clients see boss reward and event");
   Check(server.boss==null&&server.delivered==2,"remote-boss-quantity-completion");
   Check(game.ChooseTeamEquipment(1),"host-selects-boss-equipment");SetPhase("equipment");
   yield return Await(()=>EveryClientMarked("equipment"),"equipment broadcast reaches everyone");
   server.orders.Clear();server.orders.Add(new OrderState{dish=recipe,required=2,completed=1,difficultyTier=2,reward=150,remaining=130,total=150});server.experience=0;var groupOrder=server.orders[0];
   server.chefs[1].position=new Vector3(3,0,-1.6f);server.chefs[1].facing=Vector3.back;server.chefs[1].held=new Item(recipe);
   SetPhase("group-order",IdInSlot(1));yield return Await(()=>server.delivered==3&&!server.orders.Contains(groupOrder)&&groupOrder.completed==2,"remote serving completes group quantity");
   yield return Await(()=>EveryClientMarked("group"),"all clients receive group completion");Check(true,"remote-group-order-completion");
   Food dropped=chapter==KitchenChapter.Burgers?Food.Cheese:Food.Tomato;server.loose.Clear();server.chefs[1].held=new Item(dropped);server.chefs[1].position=new Vector3(-3,0,1.4f);
   SetPhase("leave",target);yield return Await(()=>game.HarnessConnections==3&&!server.chefs[1].active,"client disconnect removes chef");
   Check(server.loose.Exists(l=>l.item.food==dropped&&Vector3.Distance(l.position,new Vector3(-3,0,1.4f))<.1f),"disconnect-preserves-held-item");
   yield return Await(()=>Marked("leaver-disconnected"),"client shutdown completes");SetPhase("rejoin",target);
   yield return Await(SlotsReady,"client reconnects");AssignSlots();
   Check(IdInSlot(1)!=target,"reconnect-reuses-vacant-slot");
   yield return Await(()=>Marked("rejoined"),"reconnected client receives current state");Check(true,"late-join-receives-current-score-and-items");
   server=game.State;server.skillRanks[(int)KitchenSkill.Knife]=3;server.skillRanks[(int)KitchenSkill.Heat]=3;server.skillRanks[(int)KitchenSkill.Safety]=1;server.skillRanks[(int)KitchenSkill.FleetFeet]=3;server.skillRanks[(int)KitchenSkill.Dash]=1;
   server.experience=0;server.orders.Clear();server.orders.Add(new OrderState{dish=recipe,reward=100,remaining=95,total=95});server.nextOrder=999;
   server.chefs[1].position=new Vector3(3,0,-.8f);server.chefs[1].facing=Vector3.back;server.chefs[1].held=new Item(recipe){flameKissed=true};
   SetPhase("evolved");yield return Await(()=>EveryClientMarked("evolved"),"all clients receive evolution ranks and flame ingredient");
   int beforeDash=server.score;SetPhase("dash-serve",IdInSlot(1));
   yield return Await(()=>server.delivered==4,"remote dash and Q serves evolved food");
   yield return Await(()=>EveryClientMarked("dash-served"),"evolved serving result reaches all clients");
   Check(server.score-beforeDash>=200&&server.chefs[1].held.Empty,"remote-evolution-serving-and-flame-bonus");
   var dying=game.State;dying.health=1;dying.orders.Clear();dying.orders.Add(new OrderState{remaining=.01f});
   SetPhase("death");
   yield return Await(()=>EveryClientMarked("dead"),"all clients receive death and manual restart screen");
   Check(dying.ended&&dying.health==0,"host-lethal-order-penalty");
   yield return Delay(4.5f);
   Check(game.State==dying&&dying.ended&&dying.run==1,"no-automatic-restart-after-four-seconds");
   Check(game.RestartTeamRun(),"host-manual-restart");
   yield return Await(()=>EveryClientMarked("reset"),"all clients receive clean new run");
   Check(game.HarnessConnections==4&&game.State.chefs.FindAll(c=>c.active).Count==4,"death-reset-preserves-connections");
   SetPhase("shutdown");yield return Delay(.15f);game.Disconnect();
   yield return Await(EveryClientFinished,"clients return to menu and finish");Check(true,"host-shutdown-returns-all-clients-to-menu");
  }
  IEnumerator OverflowScenario()
  {
   yield return Await(()=>!string.IsNullOrEmpty(game.HarnessDisconnectReason),"capacity rejection arrives",12);
   Check(!game.HarnessConnected&&game.HarnessDisconnectReason.IndexOf("full",StringComparison.OrdinalIgnoreCase)>=0,"overflow-rejected-for-capacity",game.HarnessDisconnectReason);
  }
  IEnumerator ClientScenario()
  {
   yield return Await(()=>game.HarnessConnected&&game.HarnessSnapshots>0,"client connects and receives snapshots");
   yield return Await(()=>ClientSlot()>0&&game.State.chefs.FindAll(c=>c.active).Count==4,"host assigns client slot and complete roster");
   Check(game.State.started&&!game.State.kiosk&&game.State.chapter==chapter&&game.State.chefs.FindAll(c=>c.active).Count==4&&game.State.orders.Count>0,"initial-state-and-chapter");
   float initialTime=game.State.remaining;int initialSnapshots=game.HarnessSnapshots;
   yield return Await(()=>game.HarnessSnapshots>initialSnapshots+1&&game.State.remaining<initialTime,"game clock progresses over snapshots");
   Check(true,"snapshot-clock-progress");Signal("initial-"+role);
   int[] offeredCards=null;bool attemptedChoice=false;
   string previous="";float changed=0;bool leaving=false,rejoined=false;
   while(true)
   {
    var control=Control();if(control.phase!=previous)
    {
     previous=control.phase;phase=previous;changed=Time.realtimeSinceStartup;input=default;SuppressInputTransmission=false;Event("phase "+phase);
     if(phase=="forgery"&&game.HarnessClientId.ToString()==control.targetId){game.HarnessForgeState();Signal("forgery-sent");}
     if(phase=="pickup")input.interact=true;
     if(phase=="move"||phase=="stale")input.move=Vector2.right;
     if((phase=="serve"||phase=="boss"||phase=="group-order")&&game.HarnessClientId.ToString()==control.targetId)input.interact=true;
     if(phase=="dash-serve"&&game.HarnessClientId.ToString()==control.targetId)input=new ChefInput{move=Vector2.down,dash=true,toss=true};
     if(phase=="leave"&&game.HarnessClientId.ToString()==control.targetId)
     {
      leaving=true;game.Disconnect();yield return Await(()=>!game.HarnessListening&&!game.HarnessShuttingDown,"leaving client shutdown");Signal("leaver-disconnected");
     }
     if(phase=="rejoin"&&leaving)
     {
      int snapshots=game.HarnessSnapshots;game.HarnessBegin(false,chapter);
      yield return Await(()=>game.HarnessConnected&&game.HarnessSnapshots>snapshots,"rejoin receives snapshot");
      yield return Await(()=>ClientSlot()==1,"vacant slot assigned after rejoin");
      Check(game.State.chapter==chapter&&game.State.delivered==3&&game.State.equipmentRanks[1]==1&&game.State.score>0&&game.State.loose.Count==1&&game.State.level==2&&Array.Exists(game.State.skillRanks,r=>r>0),"rejoin-state-recovery");Signal("rejoined");rejoined=true;
     }
    }
    int slot=game.HarnessConnected?ClientSlot():-1;
    if(phase=="pickup"&&slot>0&&game.State.chefs[slot].held.food==SupplyFor(slot))Signal("picked-"+role);
    if(phase=="stale"&&Time.realtimeSinceStartup-changed>.25f)SuppressInputTransmission=true;
    if(phase=="serve"&&game.State.delivered==1&&game.State.score>0&&game.State.dirtyPlates==1)Signal("served-"+role);
    if(phase=="cards"&&game.State.level==2&&game.State.skillChoices.Count==3&&!attemptedChoice)
    {
     offeredCards=game.State.skillChoices.ToArray();attemptedChoice=true;
     Check(new System.Collections.Generic.HashSet<int>(offeredCards).Count==3&&!game.ChooseTeamSkill(offeredCards[0])&&game.State.skillChoices.Count==3,"client-cannot-select-team-skill");
     Signal("cards-"+role);
    }
    if(phase=="skill-selected"&&!Marked("skill-"+role)&&game.State.skillChoices.Count==0&&Array.Exists(game.State.skillRanks,r=>r>0))
    {
     Check(offeredCards!=null&&Array.Exists(offeredCards,id=>game.State.skillRanks[id]==1),"selected-skill-was-offered");
     Signal("skill-"+role);
    }
    if(phase=="boss-hold"&&game.simulation.NormalOrdersOnHold&&game.State.orders.Exists(o=>o.remaining==5)&&game.State.nextOrder==1)Signal("boss-hold-"+role);
    if(phase=="boss"&&game.State.equipmentChoice&&game.State.bossesDefeated==1&&!Marked("boss-"+role))
    {Check(game.State.boss==null&&game.State.kitchenEvent==KitchenEvent.Blackout&&!game.ChooseTeamEquipment(1),"boss-event-and-host-only-equipment");Signal("boss-"+role);}
    if(phase=="equipment"&&game.State.equipmentRanks[1]==1&&!game.State.equipmentChoice)Signal("equipment-"+role);
    if(phase=="group-order"&&game.State.delivered==3&&!game.State.orders.Exists(o=>o.difficultyTier==2&&o.required==2))Signal("group-"+role);
    if(phase=="evolved"&&game.simulation.EvolutionMask==7&&game.State.chefs[1].held.flameKissed&&!Marked("evolved-"+role))
    {Check(true,"evolution-and-flame-snapshot");Signal("evolved-"+role);}
    if(phase=="dash-serve"&&game.State.delivered==4&&game.State.chefs[1].held.Empty)Signal("dash-served-"+role);
    if(phase=="death")
    {
     if(game.State.ended&&game.State.health==0&&game.State.restartIn==0&&!Marked("dead-"+role))
     {Check(!game.RestartTeamRun(),"client-cannot-restart-team");Signal("dead-"+role);}
     if(game.State.run==2&&!game.State.ended&&game.State.health==100&&game.State.score==0&&game.State.delivered==0&&game.State.level==1&&game.State.experience==0&&game.simulation.EvolutionMask==0&&game.State.boss==null&&game.State.kitchenEvent==KitchenEvent.None&&Array.TrueForAll(game.State.equipmentRanks,r=>r==0)&&Array.TrueForAll(game.State.skillRanks,r=>r==0)&&game.State.loose.Count==0&&game.State.chefs.FindAll(c=>c.active).Count==4)
      Signal("reset-"+role);
    }
    if(phase=="shutdown")
    {
     yield return Await(()=>!game.HarnessConnected&&!game.State.started,"host shutdown returns client to menu",12);
     Check(!leaving||rejoined,"reconnect-completed-before-host-shutdown");Check(game.HarnessSnapshots>=5,"multiple-real-network-snapshots",game.HarnessSnapshots.ToString());Check(true,"host-disconnect-menu");yield break;
    }
    yield return null;
   }
  }
 }
}

