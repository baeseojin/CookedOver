using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace CookedOver.Editor
{
 public static class KitchenHarnessEditor
 {
  public static string BinaryPath=>Path.GetFullPath("Builds/Harness/CookedOverHarness.exe");
  [MenuItem("CookedOver/Harness/Verify Rules and Contracts")]
  static void MenuRules(){string path=Path.GetFullPath("HarnessResults/editor-rules.json");var report=VerifyRules(path);Debug.Log("Harness "+report.status+": "+path);}
  public static KitchenHarnessReport VerifyRules(string output)
  {
   var report=new KitchenHarnessReport{role="rules",chapter="all",runId=Path.GetFileName(Path.GetDirectoryName(output)),startedUtc=DateTime.UtcNow.ToString("o"),processId=System.Diagnostics.Process.GetCurrentProcess().Id};
   Run(report,"classic-burger-kiosk-regression",()=>KioskIntegrationVerification.Run());
   Run(report,"network-snapshot-contract",VerifySnapshotContract);
   Run(report,"survival-death-and-reset",VerifySurvival);
   Run(report,"skill-progression-and-selection",KitchenSkillVerification.Progression);
   foreach(KitchenSkill skill in Enum.GetValues(typeof(KitchenSkill))) { var current=skill; Run(report,"skill-effect-"+skill,()=>KitchenSkillVerification.Effect(current)); }
   Run(report,"skill-five-synergies",KitchenSkillVerification.Synergies);
   Run(report,"skill-immediate-pick-bonuses",KitchenSkillVerification.PickBonuses);
   Run(report,"kitchen-events",KitchenEncounterVerification.Events);
   Run(report,"boss-and-equipment-rewards",KitchenEncounterVerification.BossAndRewards);
   Run(report,"four-plate-conservation",KitchenModeVerification.Plates);
   Run(report,"roguelike-and-timed-kiosk-modes",KitchenModeVerification.Modes);
   Run(report,"evolution-automation",KitchenEvolutionVerification.Automation);
   Run(report,"evolution-flame",KitchenEvolutionVerification.Flame);
   Run(report,"evolution-dash-delivery",KitchenEvolutionVerification.DashDelivery);
   Run(report,"balanced-card-offers",KitchenBalanceVerification.Offers);
   Run(report,"scaled-group-orders-and-bosses",KitchenBalanceVerification.Orders);
   Run(report,"boss-protects-normal-orders",KitchenEncounterVerification.BossProtectsNormalOrders);
   report.status=report.checks.TrueForAll(c=>c.status=="passed")?"passed":"failed";report.finishedUtc=DateTime.UtcNow.ToString("o");
   Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));File.WriteAllText(output,JsonUtility.ToJson(report,true));return report;
  }
  static void Run(KitchenHarnessReport report,string name,Action action)
  {var watch=System.Diagnostics.Stopwatch.StartNew();try{action();report.checks.Add(new KitchenHarnessCheck{name=name,status="passed",elapsedSeconds=(float)watch.Elapsed.TotalSeconds});}catch(Exception e){report.checks.Add(new KitchenHarnessCheck{name=name,status="failed",detail=e.ToString(),elapsedSeconds=(float)watch.Elapsed.TotalSeconds});}}
  static void VerifySnapshotContract()
  {
   foreach(KitchenChapter chapter in Enum.GetValues(typeof(KitchenChapter)))
   {
    var sim=new KitchenSimulation();sim.Reset(chapter);sim.Start();var state=sim.State;state.score=1234;state.health=25;state.run=3;state.wave=2;state.restartIn=2;state.level=4;state.experience=17;state.skillRanks[0]=2;state.skillRanks[1]=1;state.skillChoices.AddRange(new[]{2,5,8});
    state.kitchenEvent=KitchenEvent.Blackout;state.eventTime=12;state.eventWave=2;state.bossWave=2;state.blackoutStation=10;state.oilPosition=new Vector3(0,0,1.6f);state.bossActive=true;state.boss=new OrderState{dish=Food.Burger,required=3,completed=2,remaining=37,total=120};state.equipmentChoice=true;state.equipmentRanks[1]=2;state.bossesDefeated=1;state.autoWashTime=1.2f;
    state.orders[0].difficultyTier=3;state.orders[0].required=2;state.orders[0].completed=1;state.boss.difficultyTier=2;
    state.chefs[1].active=true;state.chefs[1].held=new Item(Food.DeluxeBurger){bun=1,patty=1,cheese=1,tomato=1,lettuce=1,flameKissed=true};
    state.loose.Add(new LooseItem{item=new Item(Food.CookedPatty){flameKissed=true},position=new Vector3(2,0,1),velocity=Vector3.right});
    var clone=KitchenSimulation.FromSnapshot(JsonUtility.ToJson(state));
    if(clone.orders[0].difficultyTier!=3||clone.orders[0].required!=2||clone.orders[0].completed!=1||clone.boss.difficultyTier!=2||!clone.chefs[1].held.flameKissed||!clone.loose[0].item.flameKissed||clone.kitchenEvent!=KitchenEvent.Blackout||clone.eventTime!=12||clone.eventWave!=2||clone.bossWave!=2||clone.blackoutStation!=10||clone.oilPosition!=state.oilPosition||!clone.bossActive||clone.boss==null||clone.boss.completed!=2||clone.boss.remaining!=37||!clone.equipmentChoice||clone.equipmentRanks[1]!=2||clone.bossesDefeated!=1||clone.autoWashTime!=1.2f||clone.level!=4||clone.experience!=17||clone.skillRanks.Length!=10||clone.skillRanks[0]!=2||clone.skillRanks[1]!=1||clone.skillChoices.Count!=3||clone.skillChoices[2]!=8||clone.health!=25||clone.run!=3||clone.wave!=2||clone.restartIn!=2||clone.chapter!=chapter||clone.score!=1234||clone.chefs.Count!=4||clone.stations.Count!=state.stations.Count||clone.orders.Count!=2||clone.chefs[1].held.bun!=1||clone.chefs[1].held.cheese!=1||clone.loose.Count!=1||clone.loose[0].velocity!=Vector3.right)throw new Exception("Snapshot field lost for "+chapter);
    state.bossActive=false;state.boss=null;
    var empty=KitchenSimulation.FromSnapshot(JsonUtility.ToJson(state));
    if(empty.boss!=null||empty.bossActive)throw new Exception("Inactive boss reappeared after snapshot");
   }
  }
  static void VerifySurvival()
  {
   foreach(KitchenChapter chapter in Enum.GetValues(typeof(KitchenChapter)))
   {
    var sim=new KitchenSimulation();sim.Reset(chapter);sim.Start();var state=sim.State;
    Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(chapter+": "+message);};
    var chef=state.chefs[0];chef.target=state.stations.FindIndex(s=>s.kind==StationKind.Serve);
    chef.held=new Item(Food.RawPatty);sim.Interact(chef);
    check(state.health==75&&state.score==-30&&chef.held.Empty,"raw submission penalty and negative score");
    state.orders.Clear();chef.held=new Item(Food.Salad);sim.Interact(chef);
    check(state.health==50&&state.score==-60&&state.dirtyPlates==1,"unmatched dish consumed with plate conserved");
    state.orders.Add(new OrderState{dish=Food.Salad,reward=80});chef.held=new Item(Food.Salad);sim.Interact(chef);
    check(state.health==50&&state.delivered==1&&state.score>0,"matching recipe still rewards");
    state.orders.Clear();state.orders.Add(new OrderState{remaining=.01f});sim.Tick(.05f,null);
    check(state.health==20&&state.missed==1,"expired order damages team");
    state.chefs[2].active=true;state.chefs[2].held=new Item(Food.Cheese);
    state.loose.Add(new LooseItem{item=new Item(Food.Tomato)});state.stations[0].fire=1;
    state.orders.Clear();state.orders.Add(new OrderState{remaining=.01f});sim.Tick(.05f,null);
    check(state.health==0&&state.ended&&state.restartIn==0,"lethal expiry waits for restart");
    int score=state.score;chef.held=new Item(Food.RawPatty);sim.Interact(chef);
    check(state.score==score,"no interactions after death");
    float deadClock=state.remaining;
    for(int i=0;i<200;i++)sim.Tick(.05f,null);
    check(sim.State==state&&state.ended&&state.run==1&&state.health==0&&state.remaining==deadClock,"death waits indefinitely without automatic restart");
    sim.RestartRun();
    check(sim.State!=state&&sim.State.run==2&&sim.State.chapter==chapter&&sim.State.health==100&&sim.State.score==0&&sim.State.delivered==0&&sim.State.missed==0&&sim.State.wave==1&&sim.State.started&&!sim.State.ended,"new run restores defaults");
    check(sim.State.chefs[2].active&&sim.State.chefs[2].held.Empty&&sim.State.chefs[2].position==KitchenSimulation.Spawn(2)&&sim.State.loose.Count==0&&sim.State.stations.TrueForAll(s=>s.fire==0)&&sim.State.cleanPlates==4,"reset kitchen and preserve roster");
    sim.State.remaining=.01f;sim.Tick(.05f,null);check(sim.State.wave==2&&!sim.State.ended,"timer advances survival wave");
   }
  }
  [MenuItem("CookedOver/Harness/Build Integration Player")]
  static void MenuBuild(){Debug.Log(BuildPlayer());}
  public static string BuildPlayer(bool clean=false)
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play mode and finish compilation before building the harness.");
   Directory.CreateDirectory(Path.GetDirectoryName(BinaryPath));var target=UnityEditor.Build.NamedBuildTarget.Standalone;var stripping=PlayerSettings.GetManagedStrippingLevel(target);
   try
   {
    PlayerSettings.SetManagedStrippingLevel(target,ManagedStrippingLevel.Disabled);
    var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{KitchenStageBuilder.ScenePath},locationPathName=BinaryPath,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|(clean?BuildOptions.CleanBuildCache:BuildOptions.None)});
    if(build.summary.result!=BuildResult.Succeeded)throw new Exception("Harness build failed: "+build.summary.result+" errors="+build.summary.totalErrors);
   }
   finally{PlayerSettings.SetManagedStrippingLevel(target,stripping);}
   return BinaryPath;
  }
  public static void BatchRules()
  {try{var report=VerifyRules(OutputArgument());EditorApplication.Exit(report.status=="passed"?0:1);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  public static void BatchBuild()
  {try{string binary=BuildPlayer();File.WriteAllText(OutputArgument(),binary);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  static string OutputArgument()
  {var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--harness-output");if(index<0||index+1>=args.Length)throw new ArgumentException("--harness-output required.");return Path.GetFullPath(args[index+1]);}
 }
}



