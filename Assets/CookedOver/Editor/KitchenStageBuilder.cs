using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookedOver.Editor
{
    public static class KitchenStageBuilder
    {
        public const string ScenePath = "Assets/Scenes/KitchenStage.unity";
        public static void BuildFromCommandLine()
        {
            try { BuildStandalone(); EditorApplication.Exit(0); }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static string BuildStandalone()
        {
            Directory.CreateDirectory("Builds/Windows");
            var target=UnityEditor.Build.NamedBuildTarget.Standalone;
            var stripping=PlayerSettings.GetManagedStrippingLevel(target);
            try
            {
                // Development builds retain code, avoiding reflection-based linker discovery in optional IDE plugins.
                PlayerSettings.SetManagedStrippingLevel(target,ManagedStrippingLevel.Disabled);
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath}, locationPathName="Builds/Windows/LittleLadle.exe", target=BuildTarget.StandaloneWindows64, options=BuildOptions.Development });
                if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Standalone build failed: "+result.summary.result);
            }
            finally { PlayerSettings.SetManagedStrippingLevel(target,stripping); }
            return "Builds/Windows/LittleLadle.exe";
        }
        [MenuItem("CookedOver/Create Kitchen Stage")]
        public static void Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop play mode first.");
            // Save the currently open scene before opening the newly generated stage.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "KitchenStage";
            var root = new GameObject("LITTLE LADLE - Kitchen Stage");
            var game = root.AddComponent<KitchenGame>(); var view = root.AddComponent<KitchenView>(); game.view = view; view.game = game;
            view.Build();
            Directory.CreateDirectory("Assets/CookedOver/Materials");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var m=renderer.sharedMaterial;
                if(m==null || AssetDatabase.Contains(m) || !m.name.StartsWith("Kitchen ")) continue;
                string path="Assets/CookedOver/Materials/"+m.name+".mat";
                var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(existing==null) AssetDatabase.CreateAsset(m,path);
                else renderer.sharedMaterial=existing;
            }
            var cameraObject=new GameObject("Main Camera"); cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
            camera.transform.position=new Vector3(0,14,-12); camera.transform.LookAt(new Vector3(0,0,.4f));
            camera.orthographic=true; camera.orthographicSize=7.8f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.055f,.14f,.16f);
            var sun=new GameObject("Warm Kitchen Light").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.7f; sun.transform.rotation=Quaternion.Euler(48,-28,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.72f,.74f);
            KitchenArtBaker.SaveArt(root);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="Little Ladle"; PlayerSettings.companyName="CookedOver";
            PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900;
            AssetDatabase.SaveAssets(); Selection.activeGameObject=root;
            Debug.Log("CookedOver: KitchenStage created and added to build settings.");
        }
        [MenuItem("CookedOver/Run Gameplay Verification")]
        public static void Verify()
        {
            var sim=new KitchenSimulation(); sim.Start(); var state=sim.State; var chef=state.chefs[0];
            Action<bool,string> check=(condition,message)=>{if(!condition)throw new Exception("Kitchen verification failed: "+message);};
            var board=state.stations.Find(s=>s.kind==StationKind.Board);
            var stove=state.stations.Find(s=>s.kind==StationKind.Stove);
            var serve=state.stations.Find(s=>s.kind==StationKind.Serve);
            Action<StationState> target=s=>{chef.target=state.stations.IndexOf(s);};
            Action<Food> chop=f=>
            {
                board=state.stations.Find(s=>s.kind==StationKind.Board && s.item.Empty);
                check(board!=null,"a free chopping board exists");
                chef.held=new Item(f);target(board);sim.Interact(chef);
                check(chef.held.Empty,"place vegetable");sim.Work(chef,board,KitchenSimulation.ChopSeconds+.01f);
                check(board.item.chopped,"chop complete");sim.Interact(chef);check(chef.held.chopped,"pick chopped vegetable");
            };
            for(int i=0;i<3;i++){chop(Food.Tomato);target(stove);sim.Interact(chef);}
            check(stove.portions==3,"pot accepts three tomatoes");
            for(int i=0;i<190;i++)sim.Tick(.05f,new ChefInput[4]);
            chef.held=new Item(Food.Plate);target(stove);sim.Interact(chef);check(chef.held.food==Food.TomatoSoup,"plate cooked soup");
            target(serve);sim.Interact(chef);check(state.delivered==1&&state.score>=120&&state.dirtyPlates==1,"serve soup and return dirty dish");
            chef.held=new Item();chop(Food.Tomato);target(board);sim.Interact(chef);chef.held=new Item(Food.Plate);sim.Interact(chef);
            check(chef.held.tomato==1,"plate tomato");target(board);sim.Interact(chef);var saladBoard=board;chop(Food.Lettuce);target(saladBoard);sim.Interact(chef);
            check(saladBoard.item.food==Food.Salad,"assemble salad on counter");sim.Interact(chef);target(serve);state.experience=0;sim.Interact(chef);check(state.delivered==2,"serve salad");
            var sink=state.stations.Find(s=>s.kind==StationKind.Sink);chef.held=new Item();int clean=state.cleanPlates;sim.Work(chef,sink,KitchenSimulation.WashSeconds+.01f);check(state.cleanPlates==clean+1,"wash dish");
            stove.soup=Food.Onion;stove.portions=3;stove.heat=17.99f;sim.Tick(.05f,new ChefInput[4]);check(stove.fire>0&&stove.soup==Food.Burnt,"food burns and catches fire");
            chef.held=new Item(Food.Extinguisher);sim.Work(chef,stove,2);check(stove.fire==0,"extinguish fire");chef.held=new Item();target(stove);sim.Interact(chef);check(stove.portions==0,"clear burnt pot");
            chef.held=new Item(Food.Onion);chef.position=new Vector3(-3,0,0);chef.facing=Vector3.forward;sim.Toss(chef);check(chef.held.Empty&&state.loose.Count==1,"throw item");sim.Interact(chef);check(chef.held.food==Food.Onion&&state.loose.Count==0,"recover thrown item");
            chef.held=new Item(Food.Plate);var bin=state.stations.Find(s=>s.kind==StationKind.Trash);target(bin);clean=state.cleanPlates;sim.Interact(chef);check(state.cleanPlates==clean+1,"discard plate preserves plate supply");
            state.orders.Clear();state.orders.Add(new OrderState{dish=Food.OnionSoup,remaining=.01f});int missed=state.missed;sim.Tick(.05f,new ChefInput[4]);check(state.missed==missed+1,"expired order penalty");
            float time=state.remaining;state.paused=true;sim.Tick(.05f,new ChefInput[4]);check(state.remaining==time,"pause freezes time");state.paused=false;
            state.remaining=.01f;sim.Tick(.05f,new ChefInput[4]);check(!state.ended&&state.wave==2&&state.remaining==KitchenSimulation.RoundSeconds,"survival advances wave");
            Debug.Log("COOKEDOVER VERIFICATION PASS: chop, cook, plate, soup+salad delivery, dirty dishes, washing, burn, extinguish, pot cleanup, throw/pickup, plate conservation, timeout, pause, results.");
        }
    }
}

