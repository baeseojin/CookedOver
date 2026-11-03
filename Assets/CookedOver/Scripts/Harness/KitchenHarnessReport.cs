using System;
using System.Collections.Generic;
namespace CookedOver
{
 [Serializable] public sealed class KitchenHarnessCheck {public string name,status,detail;public float elapsedSeconds;}
 [Serializable] public sealed class KitchenHarnessReport
 {
  public int schemaVersion=1;public string runId,role,chapter,status="running",startedUtc,finishedUtc,error;
  public int processId,snapshots,activeChefs;public List<KitchenHarnessCheck> checks=new List<KitchenHarnessCheck>();
 }
 [Serializable] public sealed class KitchenHarnessControl {public string phase,targetId;}
}
