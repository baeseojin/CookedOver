using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace CookedOver
{
    // Opt-in standalone integration probe. Never runs during ordinary play.
    public sealed class KitchenNetworkSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kitchen-network-smoke")>=0)
                new GameObject("Network smoke probe").AddComponent<KitchenNetworkSmoke>();
        }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var game=FindFirstObjectByType<KitchenGame>();
            game.Join();
            yield return new WaitForSecondsRealtime(12);
            var manager=NetworkManager.Singleton;
            bool connected=manager!=null&&manager.IsConnectedClient;
            int active=game.State.chefs.FindAll(c=>c.active).Count;
            bool pass=connected&&game.State.started&&active>=2&&game.State.remaining<239&&game.State.score==777;
            Debug.Log("KITCHEN_NETWORK_SMOKE "+(pass?"PASS":"FAIL")+" connected="+connected+" chefs="+active+" remaining="+game.State.remaining+" score="+game.State.score);
            Application.Quit(pass?0:1);
        }
    }
}
