using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CookedOver
{
    public sealed partial class KitchenView
    {
        Transform oilSurface,rushSignal;
        readonly List<Renderer> powerMeters=new List<Renderer>();
        readonly List<Transform> outageMarks=new List<Transform>();
        readonly List<Light> cookerLights=new List<Light>();
        void BuildEncounterVisuals(KitchenState state)
        {
            oilSurface=Root("Oil spill floor",generated);
            var pool=Part(oilSurface,"Glossy oil puddle",PrimitiveType.Cylinder,Vector3.zero,new Vector3(2.8f,.003f,2.8f),new Color(.26f,.22f,.035f,.96f));
            var surface=pool.GetComponent<Renderer>();surface.shadowCastingMode=ShadowCastingMode.Off;surface.receiveShadows=false;surface.sharedMaterial.SetFloat("_Smoothness",.92f);
            var edge=Ring("Oil hazard boundary",Vector3.zero,1.4f,.022f,new Color(1,.64f,.13f));edge.SetParent(oilSurface,false);
            for(int i=0;i<4;i++)Part(oilSurface,"Oil reflection",PrimitiveType.Sphere,new Vector3(-.6f+i*.35f,.009f,.25f-i*.11f),new Vector3(.24f,.008f,.65f),new Color(.84f,.86f,.65f,.30f));
            oilSurface.gameObject.SetActive(false);
            powerMeters.Clear();outageMarks.Clear();cookerLights.Clear();
            foreach(var station in state.stations) {
                bool cooker=station.kind==StationKind.Stove||station.kind==StationKind.Grill||station.kind==StationKind.Fryer;
                if(!cooker){powerMeters.Add(null);outageMarks.Add(null);cookerLights.Add(null);continue;}
                var meter=Box("Cooker power light",station.position+new Vector3(.52f,1.24f,-.56f),new Vector3(.30f,.07f,.065f),new Color(.34f,.86f,.55f),.02f);powerMeters.Add(meter.GetComponent<Renderer>());
                var mark=Root("Outage warning",generated,station.position+Vector3.up*1.54f);
                for(int side=-1;side<=1;side+=2){var bar=Part(mark,"Power off cross",PrimitiveType.Cube,Vector3.zero,new Vector3(1.12f,.04f,.10f),new Color(1,.31f,.12f),.02f);bar.localRotation=Quaternion.Euler(0,side*45,0);}
                mark.gameObject.SetActive(false);outageMarks.Add(mark);
                var lightRoot=Root("Cooker warm glow",generated,station.position+Vector3.up*1.65f);var lamp=lightRoot.gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=1.8f;lamp.intensity=.45f;lamp.color=new Color(1,.52f,.18f);lamp.shadows=LightShadows.None;cookerLights.Add(lamp);
            }
            var serve=state.stations.Find(s=>s.kind==StationKind.Serve);rushSignal=Root("Rush service signal",generated,serve!=null?serve.position+Vector3.up*1.9f:Vector3.zero);
            for(int i=0;i<3;i++)Part(rushSignal,"Rush beacon",PrimitiveType.Sphere,new Vector3((i-1)*.28f,0,0),Vector3.one*.17f,new Color(1,.25f,.08f));
            rushSignal.gameObject.SetActive(false);
        }
        void RenderEncounterVisuals(KitchenState state,float time)
        {
            bool oil=state.started&&!state.kiosk&&state.kitchenEvent==KitchenEvent.Oil&&state.eventTime>0;
            oilSurface.gameObject.SetActive(oil);if(oil)oilSurface.localPosition=state.oilPosition+Vector3.up*.075f;
            for(int i=0;i<powerMeters.Count;i++) {
                if(powerMeters[i]==null)continue;
                bool offline=state.started&&!state.kiosk&&state.kitchenEvent==KitchenEvent.Blackout&&state.eventTime>0&&state.blackoutStation==i;
                bool blink=Mathf.FloorToInt(time*5)%2==0;
                powerMeters[i].sharedMaterial=Mat(offline?(blink?new Color(1,.15f,.04f):Dark):new Color(.34f,.86f,.55f));
                outageMarks[i].gameObject.SetActive(offline);
                cookerLights[i].enabled=state.started&&!offline;
            }
            bool rush=state.started&&!state.kiosk&&state.kitchenEvent==KitchenEvent.Rush&&state.eventTime>0;
            rushSignal.gameObject.SetActive(rush);if(rush)rushSignal.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*10)*12);
        }
    }
}
