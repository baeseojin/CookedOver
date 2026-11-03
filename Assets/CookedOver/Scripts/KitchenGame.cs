using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace CookedOver
{
    [Serializable] public class InputPacket { public float x, y; public bool interact, work, dash, toss; }
    public sealed partial class KitchenGame : MonoBehaviour
    {
        public KitchenSimulation simulation = new KitchenSimulation();
        public KitchenState State => simulation.State;
        public KitchenView view;
        KitchenChapter selectedChapter = KitchenChapter.Burgers;
        NetworkManager network; UnityTransport transport;
        readonly Dictionary<ulong, int> slots = new Dictionary<ulong, int>();
        readonly ChefInput[] inputs = new ChefInput[4]; readonly float[] lastInput = new float[4];
        float nextSend, nextSnapshot; bool localTwo, connecting, suppressDisconnect; string address = "127.0.0.1", connection = "Offline", failure = "";
        GUIStyle title, heading, body, small, button; Texture2D white;
        string relayCode = "", joinCode = ""; int connectionAttempt;
        AudioSource sound; AudioClip successTone, alertTone; int lastDelivered, lastMissed; bool lastFire;
        public bool Authority => network == null || !network.IsListening || network.IsServer;
        public void Awake()
        {
            Application.runInBackground = true;
            if (view == null) view = GetComponent<KitchenView>();
            if (view == null) view = gameObject.AddComponent<KitchenView>();
            view.game = this;
            sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false;
            successTone = Tone(660, .16f); alertTone = Tone(180, .28f);
        }
        static AudioClip Tone(float frequency, float duration)
        {
            int count=(int)(44100*duration);var samples=new float[count];
            for(int i=0;i<count;i++) samples[i]=Mathf.Sin(2*Mathf.PI*frequency*i/44100f)*.13f*(1-i/(float)count);
            var clip=AudioClip.Create("Kitchen feedback",count,1,44100,false);clip.SetData(samples,0);return clip;
        }
        public void StartSolo(bool two)
        {
            Disconnect(); simulation.Reset(selectedChapter); localTwo = two; State.chefs[1].active = two; PrepareKioskKitchen(); simulation.Start(); StartKioskBridge(); connection = two ? "LOCAL CO-OP" : "SOLO";
        }
        public void StartHost()
        {
            Disconnect(); simulation.Reset(selectedChapter); localTwo = false; EnsureNetwork(); transport.SetConnectionData("127.0.0.1", KitchenHarnessDriver.Requested?KitchenHarnessDriver.ListenPort:(ushort)7777, KitchenHarnessDriver.Requested?"127.0.0.1":"0.0.0.0");
            if (!network.StartHost()) { failure = "Could not host on port 7777"; return; }
            slots[network.LocalClientId] = 0; PrepareKioskKitchen(); simulation.Start(); StartKioskBridge(); connection = "HOST :7777";
        }
        public void Join()
        {
            Disconnect(); simulation.Reset(selectedChapter); EnsureNetwork(); transport.SetConnectionData(address.Trim(), KitchenHarnessDriver.Requested?KitchenHarnessDriver.ListenPort:(ushort)7777); connecting = true;
            if (!network.StartClient()) { connecting = false; failure = "Could not start client"; }
            else connection = "CONNECTING...";
        }
        public async void ConnectRelay(bool host)
        {
            if(connecting)return;
            Disconnect();simulation.Reset(selectedChapter);localTwo=false;connecting=true;failure="";
            int attempt=connectionAttempt;
            try
            {
                if(string.IsNullOrEmpty(Application.cloudProjectId)) throw new InvalidOperationException("Link this project in Project Settings > Services and enable Relay first.");
                EnsureNetwork();
                if(UnityServices.State!=ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync(new InitializationOptions().SetProfile("chef"+Guid.NewGuid().ToString("N").Substring(0,12)));
                if(!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if(attempt!=connectionAttempt || this==null)return;
                if(host)
                {
                    var allocation=await RelayService.Instance.CreateAllocationAsync(3);
                    string code=await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    if(attempt!=connectionAttempt || this==null)return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation,"dtls"));
                    if(!network.StartHost())throw new InvalidOperationException("Relay host could not start.");
                    relayCode=code;slots[network.LocalClientId]=0;PrepareKioskKitchen();simulation.Start();StartKioskBridge();connection="ONLINE CODE: "+relayCode;
                }
                else
                {
                    var allocation=await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpperInvariant());
                    if(attempt!=connectionAttempt || this==null)return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation,"dtls"));
                    if(!network.StartClient())throw new InvalidOperationException("Relay client could not start.");
                    connection="ONLINE / "+joinCode;
                }
                connecting=false;
            }
            catch(Exception e){if(this!=null&&attempt==connectionAttempt){connecting=false;failure=e.Message;connection="Offline";}}
        }
        void EnsureNetwork()
        {
            suppressDisconnect=false;
            if (network != null) return;
            // NGO requires its manager to be a scene root, not a child of the kitchen.
            var root = new GameObject("Kitchen Network"); root.SetActive(false);
            transport = root.AddComponent<UnityTransport>(); network = root.AddComponent<NetworkManager>();
            network.NetworkConfig = new NetworkConfig { NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = false };
            network.ConnectionApprovalCallback = (request, response) =>
            {
                response.Approved = network.ConnectedClientsIds.Count < 4; response.CreatePlayerObject = false;
                response.Reason = response.Approved ? "" : "Kitchen is full (4 chefs)";
            };
            network.OnClientConnectedCallback += Connected; network.OnClientDisconnectCallback += Disconnected;
            network.OnServerStarted += RegisterMessages;
            network.OnClientStarted += RegisterMessages;
            root.SetActive(true);
        }
        void RegisterMessages()
        {
            network.CustomMessagingManager.RegisterNamedMessageHandler("kitchen.input", ReceiveInput);
            network.CustomMessagingManager.RegisterNamedMessageHandler("kitchen.state", ReceiveState);
        }
        void Connected(ulong id)
        {
            if (network.IsServer)
            {
                if (id == network.LocalClientId) { slots[id] = 0; return; }
                for (int i = 1; i < 4; i++) if (!State.chefs[i].active) { slots[id] = i; State.chefs[i].active = true; State.chefs[i].position = KitchenSimulation.Spawn(i); break; }
                simulation.Notice("Chef joined the kitchen");
            }
            else { connecting = false; if(!connection.StartsWith("ONLINE")) connection = "CLIENT / " + address; }
        }
        void Disconnected(ulong id)
        {
            if(suppressDisconnect)return;
            if (network.IsServer && slots.TryGetValue(id, out int slot))
            {
                var c = State.chefs[slot];
                if (!c.held.Empty) State.loose.Add(new LooseItem { item = c.held, position = c.position });
                c.held = new Item(); c.active = false; inputs[slot] = default; slots.Remove(id);
            }
            else if (!network.IsServer) { connecting = false; simulation.Reset(selectedChapter); failure = "Disconnected: " + network.DisconnectReason; connection = "Offline"; }
        }
        void ReceiveInput(ulong sender, FastBufferReader reader)
        {
            if (!network.IsServer || !slots.TryGetValue(sender, out int slot) || slot == 0) return;
            try
            {
                reader.ReadValueSafe(out string json); var p = JsonUtility.FromJson<InputPacket>(json);
                if (p == null || float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y)) return;
                var previous = inputs[slot];
                inputs[slot] = new ChefInput { move = Vector2.ClampMagnitude(new Vector2(p.x, p.y), 1), work = p.work, interact = p.interact || previous.interact, dash = p.dash || previous.dash, toss = p.toss || previous.toss };
                lastInput[slot] = Time.unscaledTime;
            }
            catch (Exception e) { Debug.LogWarning("Ignored invalid input: " + e.Message); }
        }
        void ReceiveState(ulong sender, FastBufferReader reader)
        {
            if(network.IsServer){if(KitchenHarnessDriver.Requested)HarnessRejectedStates++;return;}
            if(sender != NetworkManager.ServerClientId)return;
            reader.ReadValueSafe(out string json); var state = KitchenSimulation.FromSnapshot(json);
            if (state != null) { simulation.State = state; selectedChapter = state.chapter; HarnessSnapshots++; }
        }
        void Send(string channel, ulong to, string json)
        {
            using (var writer = new FastBufferWriter(System.Text.Encoding.UTF8.GetByteCount(json) * 2 + 32, Allocator.Temp))
            { writer.WriteValueSafe(json); network.CustomMessagingManager.SendNamedMessage(channel, to, writer, NetworkDelivery.ReliableFragmentedSequenced); }
        }
        public void Disconnect()
        {
            if(kioskBridge!=null)kioskBridge.StopReceiving();
            connecting = false;
            suppressDisconnect=true;
            connectionAttempt++; relayCode="";
            if (network != null && network.IsListening) network.Shutdown(); slots.Clear(); Array.Clear(inputs, 0, inputs.Length);
        }
        void OnDestroy() { DisposeUI(); Disconnect(); if(network!=null) Destroy(network.gameObject); }
        ChefInput ReadInput(bool second)
        {
            if(KitchenHarnessDriver.Requested)return KitchenHarnessDriver.ReadInput(second);
            var k = Keyboard.current; var input = new ChefInput();
            if (k != null)
            {
                input.move = second ? new Vector2((k.rightArrowKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed ? 1 : 0), (k.upArrowKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed ? 1 : 0)) : new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                input.interact = second ? k.enterKey.wasPressedThisFrame : k.eKey.wasPressedThisFrame;
                input.work = second ? k.rightShiftKey.isPressed : k.fKey.isPressed;
                input.dash = second ? k.rightCtrlKey.wasPressedThisFrame : k.spaceKey.wasPressedThisFrame;
                input.toss = second ? k.backslashKey.wasPressedThisFrame : k.qKey.wasPressedThisFrame;
            }
            int padIndex = second ? 1 : 0;
            if (Gamepad.all.Count > padIndex)
            {
                var pad = Gamepad.all[padIndex]; if (pad.leftStick.ReadValue().sqrMagnitude > .05f) input.move = pad.leftStick.ReadValue();
                input.interact |= pad.buttonSouth.wasPressedThisFrame; input.work |= pad.buttonWest.isPressed; input.dash |= pad.buttonEast.wasPressedThisFrame; input.toss |= pad.buttonNorth.wasPressedThisFrame;
            }
            return input;
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && State.started && !State.ended && !simulation.ChoosingReward && (network == null || !network.IsListening)) State.paused = !State.paused;
            if (Authority)
            {
                inputs[0] = ReadInput(false); if (localTwo) inputs[1] = ReadInput(true);
                for (int i = 1; i < 4; i++) if (!localTwo && Time.unscaledTime - lastInput[i] > .4f) inputs[i] = default;
                simulation.Tick(Time.deltaTime, inputs);
                for (int i = 0; i < 4; i++) { var p = inputs[i]; p.interact = p.dash = p.toss = false; inputs[i] = p; }
                if (network != null && network.IsServer && Time.unscaledTime >= nextSnapshot)
                { nextSnapshot = Time.unscaledTime + .075f; string json = JsonUtility.ToJson(State); foreach (ulong id in network.ConnectedClientsIds) if (id != network.LocalClientId) Send("kitchen.state", id, json); }
            }
            else if (network.IsConnectedClient && !KitchenHarnessDriver.SuppressInputTransmission)
            {
                var p = ReadInput(false);
                if (Time.unscaledTime >= nextSend || p.interact || p.dash || p.toss)
                { nextSend = Time.unscaledTime + .03f; Send("kitchen.input", NetworkManager.ServerClientId, JsonUtility.ToJson(new InputPacket { x = p.move.x, y = p.move.y, work = p.work, interact = p.interact, dash = p.dash, toss = p.toss })); }
            }
            if(!KitchenHarnessDriver.Requested) view.Render(State);
            bool fire=State.stations.Exists(s=>s.fire>0);
            if(State.delivered>lastDelivered && sound!=null) sound.PlayOneShot(successTone);
            if((State.missed>lastMissed || fire&&!lastFire) && sound!=null) sound.PlayOneShot(alertTone);
            lastDelivered=State.delivered;lastMissed=State.missed;lastFire=fire;
        }
    }
}



