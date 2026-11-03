using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenHarnessDriver:MonoBehaviour
 {
  public static readonly bool Requested=Array.IndexOf(Environment.GetCommandLineArgs(),"--kitchen-harness")>=0;
  public static ushort ListenPort {get;private set;}=18080;
  public static bool SuppressInputTransmission {get;private set;}
  static KitchenHarnessDriver instance;
  int controlRevision;string lastControlPath;
  KitchenHarnessControl lastControl=new KitchenHarnessControl{phase="boot"};
  KitchenGame game;KitchenChapter chapter;string role,folder,phase="boot";KitchenHarnessReport report;
  ChefInput input;float started,timeout=100;bool finished;readonly ConcurrentQueue<string> errors=new ConcurrentQueue<string>();
  public static ChefInput ReadInput(bool second)
  {
   if(instance==null||second)return default;var result=instance.input;
   instance.input.interact=instance.input.dash=instance.input.toss=false;return result;
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Requested&&instance==null)new GameObject("Kitchen integration harness").AddComponent<KitchenHarnessDriver>();}
  static string Argument(string name,string fallback=null)
  {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
  void Start()
  {
   instance=this;started=Time.realtimeSinceStartup;
   try
   {
    folder=Argument("--harness-dir");role=Argument("--harness-role");
    if(string.IsNullOrEmpty(folder)||!Path.IsPathRooted(folder))throw new ArgumentException("--harness-dir must be absolute.");
    if(role!="host"&&role!="overflow"&&role!="client-1"&&role!="client-2"&&role!="client-3")throw new ArgumentException("Unknown harness role.");
    Directory.CreateDirectory(folder);
    if(!ushort.TryParse(Argument("--harness-port"),out var port)||port<1024)throw new ArgumentException("Invalid harness port.");ListenPort=port;
    if(!Enum.TryParse(Argument("--harness-chapter"),out chapter)||!Enum.IsDefined(typeof(KitchenChapter),chapter))throw new ArgumentException("Invalid chapter.");
    float.TryParse(Argument("--harness-timeout","100"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out timeout);if(timeout<20||timeout>300)throw new ArgumentException("Invalid timeout.");
    report=new KitchenHarnessReport{runId=Argument("--harness-run-id","local"),role=role,chapter=chapter.ToString(),startedUtc=DateTime.UtcNow.ToString("o"),processId=Process.GetCurrentProcess().Id};
    Application.targetFrameRate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;
    Application.logMessageReceivedThreaded+=OnLog;
    game=FindFirstObjectByType<KitchenGame>();if(game==null)throw new Exception("KitchenGame missing from build scene.");
    var geometry=game.transform.Find("Kitchen Geometry");if(geometry!=null)geometry.gameObject.SetActive(false);
    game.HarnessBegin(role=="host",chapter);
    StartCoroutine(Guard(role=="host"?HostScenario():role=="overflow"?OverflowScenario():ClientScenario()));
   }
   catch(Exception e){Finish(false,e.ToString());}
  }
  void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Enqueue(message+"\n"+stack);}
  IEnumerator Guard(IEnumerator scenario)
  {
   var stack=new Stack<IEnumerator>();stack.Push(scenario);
   while(stack.Count>0&&!finished)
   {
    object next=null;Exception failure=null;
    try
    {
     if(errors.TryDequeue(out string error))throw new Exception("Runtime error: "+error);
     if(Time.realtimeSinceStartup-started>timeout)throw new TimeoutException("Global timeout in phase "+phase);
     var current=stack.Peek();if(!current.MoveNext()){(current as IDisposable)?.Dispose();stack.Pop();continue;}
     next=current.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}
    }
    catch(Exception e){failure=e;}
    if(failure!=null){Finish(false,failure.ToString());yield break;}
    yield return next;
   }
   if(!finished)Finish(true,null);
  }
  IEnumerator Await(Func<bool> condition,string label,float seconds=15)
  {
   float deadline=Time.realtimeSinceStartup+seconds;
   while(!condition()){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException(label+"; phase="+phase);yield return null;}
  }
  IEnumerator Delay(float seconds){float deadline=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<deadline)yield return null;}
  void Check(bool condition,string label,string detail="")
  {
   report.checks.Add(new KitchenHarnessCheck{name=label,status=condition?"passed":"failed",detail=detail,elapsedSeconds=Time.realtimeSinceStartup-started});
   Event(label+(condition?" PASS":" FAIL"));if(!condition)throw new Exception(label+": "+detail);
  }
  string FileAt(string name)=>Path.Combine(folder,name);
  bool Marked(string name)=>File.Exists(FileAt(name+".signal"));
  void Signal(string name){string path=FileAt(name+".signal");if(!File.Exists(path))AtomicWrite(path,DateTime.UtcNow.ToString("o"));}
  void Event(string message)
  {
   var row=new KitchenHarnessCheck{name=message,status=phase,detail=DateTime.UtcNow.ToString("o"),elapsedSeconds=Time.realtimeSinceStartup-started};
   File.AppendAllText(FileAt(role+".events.jsonl"),JsonUtility.ToJson(row)+"\n");
   UnityEngine.Debug.Log("HARNESS "+role+" "+message);
  }
  static string ReadShared(string path)
  {using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(stream))return reader.ReadToEnd();}
  internal static void AtomicWrite(string path,string text)
  {string temp=path+".tmp";File.WriteAllText(temp,text);File.Move(temp,path);}
  void SetPhase(string value,ulong target=ulong.MaxValue)
  {phase=value;AtomicWrite(FileAt("control-"+(++controlRevision).ToString("D4")+".json"),JsonUtility.ToJson(new KitchenHarnessControl{phase=value,targetId=target.ToString()}));Event("phase "+value);}
  KitchenHarnessControl Control()
  {var records=Directory.GetFiles(folder,"control-*.json");Array.Sort(records,StringComparer.Ordinal);try{if(records.Length>0&&records[records.Length-1]!=lastControlPath){lastControl=JsonUtility.FromJson<KitchenHarnessControl>(ReadShared(records[records.Length-1]));lastControlPath=records[records.Length-1];}}catch(IOException){}return lastControl;}
  void AssignSlots()
  {foreach(ulong id in game.HarnessClientIds)if(id!=0&&!File.Exists(FileAt("slot-"+id+".txt")))AtomicWrite(FileAt("slot-"+id+".txt"),game.HarnessSlot(id).ToString());}
  int ClientSlot()
  {string path=FileAt("slot-"+game.HarnessClientId+".txt");return File.Exists(path)&&int.TryParse(ReadShared(path),out int slot)?slot:-1;}
  bool EveryClientMarked(string name)=>Marked(name+"-client-1")&&Marked(name+"-client-2")&&Marked(name+"-client-3");
  bool EveryClientFinished()
  {for(int i=1;i<=3;i++){string path=FileAt("client-"+i+".json");if(!File.Exists(path))return false;var result=JsonUtility.FromJson<KitchenHarnessReport>(ReadShared(path));if(result.status!="passed")throw new Exception("client-"+i+" failed: "+result.error);}return true;}
  void Finish(bool passed,string error)
  {
   if(finished)return;finished=true;Application.logMessageReceivedThreaded-=OnLog;
   try
   {
    if(report==null)report=new KitchenHarnessReport{role=role??"invalid",chapter=chapter.ToString(),startedUtc=DateTime.UtcNow.ToString("o")};
    report.status=passed?"passed":"failed";report.finishedUtc=DateTime.UtcNow.ToString("o");report.error=error;
    if(game!=null){report.snapshots=game.HarnessSnapshots;report.activeChefs=game.State.chefs.FindAll(c=>c.active).Count;AtomicWrite(FileAt(role+".state.json"),JsonUtility.ToJson(game.State,true));}
    AtomicWrite(FileAt(role+".json"),JsonUtility.ToJson(report,true));
    UnityEngine.Debug.Log("KITCHEN_HARNESS "+report.status+" "+role);
   }
   catch(Exception e){UnityEngine.Debug.LogError("Could not write harness report: "+e);passed=false;}
   if(game!=null)game.Disconnect();Application.Quit(passed?0:1);
  }
  void OnDestroy(){Application.logMessageReceivedThreaded-=OnLog;if(instance==this)instance=null;}
 }
}



