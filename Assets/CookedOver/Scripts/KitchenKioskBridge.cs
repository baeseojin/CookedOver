using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace CookedOver
{
 // Only the authoritative kitchen receives orders. No service key or customer data is used.
 public sealed class KitchenKioskBridge : MonoBehaviour
 {
  const string Url="https://qtckzactriwoiiscrkqd.supabase.co";
  const string PublicKey="sb_publishable_Um04ERgyhHb5YtXYspqH5A_Tjvf_ZmF";
  [Serializable] class Credentials {public string email,password;}
  [Serializable] class Refresh {public string refresh_token;}
  [Serializable] class User {public string id;}
  [Serializable] class Session {public string access_token,refresh_token;public int expires_in;public User user;}
  [Serializable] class Run {public string p_run;}
  [Serializable] class Serve {public string p_run,p_task;public int p_expected;}
  [Serializable] class Poll {public bool busy;public KioskTask[] tasks;}
  [Serializable] class Served {public bool cancelled;public int served;}
  [Serializable] class Pending {public string task;public int expected;}
  [Serializable] class Outbox {public List<Pending> entries=new List<Pending>();}
  KitchenGame game;string run,access,refresh,userId,body,error,path;long code;float expires;
  string manualEmail,manualPassword;
  Outbox outbox=new Outbox();UnityWebRequest activeRequest;bool accepting;
  public string Status {get;private set;}="키오스크 연결 대기";
  public void Begin(KitchenGame owner,string email=null,string password=null)
  {
   StopReceiving();manualEmail=email;manualPassword=password;game=owner;run=Guid.NewGuid().ToString();
   game.simulation.KioskServing=QueueServe;StartCoroutine(Receive());
  }
  public void StopReceiving()
  {
   if(!string.IsNullOrEmpty(access)&&!string.IsNullOrEmpty(run))
   {
    var release=new UnityWebRequest(Url+"/rest/v1/rpc/kitchen_game_release","POST");
    release.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Run{p_run=run})));
    release.downloadHandler=new DownloadHandlerBuffer();release.timeout=5;release.SetRequestHeader("Content-Type","application/json");
    release.SetRequestHeader("apikey",PublicKey);release.SetRequestHeader("Authorization","Bearer "+access);
    release.SendWebRequest().completed+=_=>release.Dispose();
   }
   accepting=false;StopAllCoroutines();
   if(activeRequest!=null){activeRequest.Abort();activeRequest.Dispose();activeRequest=null;}
   if(game!=null)game.simulation.KioskServing=null;
   access=null;refresh=null;userId=null;outbox=new Outbox();path=null;manualEmail=null;manualPassword=null;
  }
  static string Env(string name)
  {
   string value=Environment.GetEnvironmentVariable(name);
   #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
   if(string.IsNullOrEmpty(value))value=Environment.GetEnvironmentVariable(name,EnvironmentVariableTarget.User);
   #endif
   return value;
  }
  IEnumerator Authenticate()
  {
   error=null;
   if(!string.IsNullOrEmpty(access)&&Time.realtimeSinceStartup<expires)yield break;
   accepting=false;
   if(!string.IsNullOrEmpty(refresh))
   {
    yield return Send("/auth/v1/token?grant_type=refresh_token",new Refresh{refresh_token=refresh},false);
    if(error==null){SetSession(Parse<Session>());yield break;}
    access=null;refresh=null;
   }
   string email=string.IsNullOrWhiteSpace(manualEmail)?Env("KIOSK_EMAIL"):manualEmail;
   string password=string.IsNullOrEmpty(manualPassword)?Env("KIOSK_PASSWORD"):manualPassword;
   if(string.IsNullOrWhiteSpace(email)||string.IsNullOrEmpty(password)){error="메뉴에서 키오스크 계정 이메일과 비밀번호를 입력한 뒤 시작해 주세요.";yield break;}
   Status="키오스크 계정 연결 중";
   yield return Send("/auth/v1/token?grant_type=password",new Credentials{email=email,password=password},false);
   if(error==null)SetSession(Parse<Session>());
  }
  void SetSession(Session session)
  {
   if(session==null||string.IsNullOrEmpty(session.access_token)||session.user==null||!Guid.TryParse(session.user.id,out _)){error="로그인 응답을 확인할 수 없습니다.";return;}
   access=session.access_token;refresh=session.refresh_token;expires=Time.realtimeSinceStartup+Mathf.Max(5,session.expires_in-60);
   if(userId==session.user.id)return;
   userId=session.user.id;path=Path.Combine(Application.persistentDataPath,"kiosk-outbox-"+userId+".json");
   try {outbox=File.Exists(path)?JsonUtility.FromJson<Outbox>(File.ReadAllText(path)):new Outbox();if(outbox==null||outbox.entries==null)throw new IOException();}
   catch(Exception){access=null;error="서빙 대기 기록을 읽지 못했습니다. 저장 파일을 확인해 주세요.";}
  }
  IEnumerator Receive()
  {
   while(game!=null&&game.Authority&&game.State.kiosk&&game.State.started)
   {
    yield return Authenticate();
    if(error!=null){Status=error;yield return new WaitForSecondsRealtime(8);continue;}
    yield return Send("/rest/v1/rpc/kitchen_game_poll",new Run{p_run=run},true);
    Poll snapshot=error==null?Parse<Poll>():null;
    accepting=false;
    if(snapshot==null){Status=error??"주문 응답을 읽지 못했습니다.";yield return new WaitForSecondsRealtime(5);continue;}
    if(snapshot.busy){Status="다른 게임이 이 키오스크 주문을 처리 중입니다. 종료 후 최대 90초 기다려 주세요.";yield return new WaitForSecondsRealtime(5);continue;}
    while(outbox.entries.Count>0)
    {
     var pending=outbox.entries[0];
     yield return Send("/rest/v1/rpc/kitchen_game_serve",new Serve{p_run=run,p_task=pending.task,p_expected=pending.expected},true);
     var ack=error==null?Parse<Served>():null;
     if(ack==null)break;
     outbox.entries.RemoveAt(0);
     if(!SaveOutbox()){outbox.entries.Insert(0,pending);error="서빙 기록 저장에 실패했습니다.";break;}
    }
    if(error!=null){Status=error+" · 서빙 기록을 보관하고 재시도합니다.";yield return new WaitForSecondsRealtime(5);continue;}
    // Refresh after acknowledgement so a just-served item cannot appear again.
    yield return Send("/rest/v1/rpc/kitchen_game_poll",new Run{p_run=run},true);
    snapshot=error==null?Parse<Poll>():null;
    if(snapshot==null||snapshot.busy){Status=error??"주문 연결을 재확인합니다.";yield return new WaitForSecondsRealtime(5);continue;}
    var pendingIds=new HashSet<string>();foreach(var p in outbox.entries)pendingIds.Add(p.task);
    game.simulation.ApplyKioskOrders(snapshot.tasks,pendingIds);accepting=true;
    Status="키오스크 연결됨 · 대기 항목 "+game.State.orders.Count+"개 · 2초마다 확인";
    foreach(var t in snapshot.tasks??Array.Empty<KioskTask>())if(t.recipe=="Unsupported")Status="지원하지 않는 새 메뉴가 있습니다: "+t.label+" · 레시피 연결 필요";
    yield return new WaitForSecondsRealtime(2);
   }
   accepting=false;
  }
  bool QueueServe(OrderState order)
  {
   if(!accepting||string.IsNullOrEmpty(path)||outbox.entries.Count>=100)return false;
   if(outbox.entries.Exists(p=>p.task==order.externalTaskId))return false;
   var pending=new Pending{task=order.externalTaskId,expected=order.completed};outbox.entries.Add(pending);
   if(SaveOutbox())return true;
   outbox.entries.Remove(pending);return false;
  }
  bool SaveOutbox()
  {
   try {Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(path+".tmp",JsonUtility.ToJson(outbox));if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);return true;}
   catch(Exception){return false;}
  }
  T Parse<T>() where T:class
  {try{return JsonUtility.FromJson<T>(body);}catch(Exception){error="서버 응답 형식을 확인할 수 없습니다.";return null;}}
  IEnumerator Send(string endpoint,object payload,bool authenticated)
  {
   error=null;body=null;
   using(var request=new UnityWebRequest(Url+endpoint,"POST"))
   {
    activeRequest=request;request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
    request.downloadHandler=new DownloadHandlerBuffer();request.timeout=15;
    request.SetRequestHeader("Content-Type","application/json");request.SetRequestHeader("apikey",PublicKey);
    if(authenticated)request.SetRequestHeader("Authorization","Bearer "+access);
    yield return request.SendWebRequest();code=request.responseCode;
    if(request.result!=UnityWebRequest.Result.Success)
    {
     error=code==401?"키오스크 로그인을 갱신합니다.":code==403?"등록된 활성 키오스크 계정인지 확인해 주세요.":"주문 연결에 실패했습니다 ("+code+").";
     if(code==401)access=null;
    }
    else body=request.downloadHandler.text;
    activeRequest=null;
   }
  }
  void OnDestroy(){StopReceiving();}
 }
}
