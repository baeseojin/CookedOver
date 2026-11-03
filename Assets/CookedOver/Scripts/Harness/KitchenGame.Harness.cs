using System;
using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenGame
 {
  internal int HarnessRejectedStates { get; set; }
  internal float HarnessInputAge(int slot)=>Time.unscaledTime-lastInput[slot];
  internal int HarnessSnapshots { get; private set; }
  internal int HarnessConnections => network==null?0:network.ConnectedClientsIds.Count;
  internal ulong HarnessClientId => network==null?ulong.MaxValue:network.LocalClientId;
  internal bool HarnessConnected => network!=null&&network.IsConnectedClient;
  internal bool HarnessListening => network!=null&&network.IsListening;
  internal bool HarnessShuttingDown => network!=null&&network.ShutdownInProgress;
  internal string HarnessDisconnectReason => network==null?"":network.DisconnectReason;
  internal int HarnessSlot(ulong id)=>slots.TryGetValue(id,out int slot)?slot:-1;
  internal ulong[] HarnessClientIds
  {
   get { if(network==null)return Array.Empty<ulong>();var ids=new ulong[network.ConnectedClientsIds.Count];for(int i=0;i<ids.Length;i++)ids[i]=network.ConnectedClientsIds[i];return ids; }
  }
  internal void HarnessBegin(bool host,KitchenChapter chapter)
  {
   if(!KitchenHarnessDriver.Requested)throw new InvalidOperationException("Harness controls require --kitchen-harness.");
   kioskSelected=false;selectedChapter=chapter;address="127.0.0.1";
   if(host)StartHost();else Join();
  }
  internal void HarnessForgeState()
  {
   if(!KitchenHarnessDriver.Requested||!HarnessConnected||network.IsServer)return;
   Send("kitchen.state",Unity.Netcode.NetworkManager.ServerClientId,JsonUtility.ToJson(new KitchenState{score=999999}));
  }
 }
}

