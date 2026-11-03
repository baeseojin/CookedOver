using System;
using UnityEngine;
namespace CookedOver.Editor
{
    public static class KitchenBalanceVerification
    {
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
        static KitchenSimulation New(){var sim=new KitchenSimulation();sim.Reset(KitchenChapter.Burgers);sim.Start();return sim;}
        public static void Offers()
        {
            for(int run=0;run<30;run++){
                var sim=New();if(run%2==0)for(int i=0;i<10;i++)sim.State.skillRanks[i]=i%3;
                sim.GrantExperience(50);var offered=sim.State.skillChoices;
                Check(offered.Count==3&&new System.Collections.Generic.HashSet<int>(offered).Count==3,"three distinct offers");
                for(int i=0;i<3;i++)Check(KitchenSimulation.SkillCategory(offered[i])==i,"one offer per category");
                Check(offered.Exists(id=>sim.State.skillRanks[id]==0),"at least one unowned skill while available");
            }
            var last=New();for(int i=0;i<10;i++)last.State.skillRanks[i]=3;last.State.skillRanks[9]=0;last.GrantExperience(50);Check(last.State.skillChoices.Contains(9),"last unowned skill is offered");
            last=New();for(int i=0;i<10;i++)last.State.skillRanks[i]=4;last.GrantExperience(50);Check(last.State.skillChoices.Count==3,"fully owned pool still offers upgrades");
        }
        public static void Orders()
        {
            var sim=New();Check(sim.ThreatTier==0&&sim.State.orders[0].dish==Food.Burger&&sim.State.orders[0].required==1,"intro difficulty preserved");
            var existing=sim.State.orders[0];float originalTime=existing.total;sim.State.level=17;sim.State.wave=5;
            Check(sim.ThreatTier==4&&existing.required==1&&existing.total==originalTime,"level growth never rewrites existing orders");
            sim.State.orders.Clear();sim.State.sequence=0;sim.State.nextOrder=.01f;sim.Tick(.05f,null);var order=sim.State.orders[0];
            Check(order.required==3&&order.difficultyTier==4&&order.total==185&&order.reward>=180&&order.dish!=Food.Burger,"high tier creates complex group order with longer deadline");
            var chef=sim.State.chefs[0];int plates=sim.State.stations.FindIndex(s=>s.kind==StationKind.Plates),serve=sim.State.stations.FindIndex(s=>s.kind==StationKind.Serve);
            for(int i=0;i<3;i++){
                chef.target=plates;sim.Interact(chef);Check(chef.held.food==Food.Plate,"group delivery uses a real plate");chef.held.food=order.dish;
                sim.State.experience=0;chef.target=serve;sim.Interact(chef);Check(order.completed==i+1&&sim.State.delivered==i+1,"each portion counts once");
                Check(sim.State.orders.Contains(order)==(i<2),"group order remains until final portion");
            }
            Check(sim.State.cleanPlates==1&&sim.State.dirtyPlates==3,"group delivery preserves four plates");
            sim.BeginBossOrder();Check(sim.State.boss.required==6&&sim.State.boss.total==180&&sim.State.boss.dish==Food.DeluxeBurger&&sim.State.boss.difficultyTier==4,"grown boss quantities and time are capped");
            sim.State.wave=100;sim.State.level=100;Check(sim.ThreatTier==5&&sim.ArrivalInterval>=12,"difficulty and arrival pressure have bounds");
            sim.RestartRun();Check(sim.ThreatTier==0&&sim.State.orders[0].required==1,"new run restores intro difficulty");
            sim.EnableKioskMode();sim.State.level=100;sim.State.wave=100;Check(sim.ThreatTier==0,"kiosk excluded from scaling");
        }
    }
}
