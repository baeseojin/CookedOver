using UnityEngine;
namespace CookedOver
{
    public enum KitchenEvolution { Automation, Flame, Courier }
    public static class KitchenEvolutions
    {
        public static readonly string[] Names={"자동화 주방","불맛 주방","번개 서빙"};
        public static readonly KitchenSkill[] Primary={KitchenSkill.Knife,KitchenSkill.Heat,KitchenSkill.FleetFeet};
        public static readonly KitchenSkill[] Secondary={KitchenSkill.Heat,KitchenSkill.Safety,KitchenSkill.Dash};
        public static readonly string[] Effects={"셰프 주변 2.2m 안의 조리대가 자동 손질 (작업 속도의 65%)","타기 직전 3초 안에 꺼낸 수프·패티로 만든 요리의 서빙 점수 2배","대시 중 서빙대를 향해 Q: 3.25m 안의 일치하는 주문에 즉시 서빙"};
        public static string Requirement(int id)=>KitchenSkills.Names[(int)Primary[id]]+" R3 + "+KitchenSkills.Names[(int)Secondary[id]]+" R1";
    }
    public sealed partial class KitchenSimulation
    {
        public bool Evolved(KitchenEvolution evolution)
        {
            int id=(int)evolution;return !State.kiosk&&Rank(KitchenEvolutions.Primary[id])>=3&&Rank(KitchenEvolutions.Secondary[id])>=1;
        }
        public int EvolutionMask {get {int mask=0;for(int i=0;i<3;i++)if(Evolved((KitchenEvolution)i))mask|=1<<i;return mask;}}
        void TickAutomation(float dt)
        {
            if(!Evolved(KitchenEvolution.Automation))return;
            for(int i=0;i<State.stations.Count;i++) {
                var station=State.stations[i];
                if(station.kind!=StationKind.Board||!station.item.Raw||station.item.chopped||station.fire>0||StationOffline(i))continue;
                if(!State.chefs.Exists(c=>c.active&&Vector3.Distance(c.position,station.position)<=2.2f))continue;
                station.progress+=dt*ChopSpeed*.65f;
                if(station.progress>=ChopSeconds){station.item.chopped=true;station.progress=0;}
            }
        }
        bool FlameWindow(float heat,float burnAt)=>Evolved(KitchenEvolution.Flame)&&heat>=burnAt-3&&heat<burnAt;
        bool TryDashServe(ChefState chef)
        {
            if(!Evolved(KitchenEvolution.Courier)||chef.dashTime<=0||!chef.held.Dish)return false;
            if((State.boss==null||State.boss.dish!=chef.held.food)&&!State.orders.Exists(o=>o.dish==chef.held.food))return false;
            int index=State.stations.FindIndex(s=>s.kind==StationKind.Serve);if(index<0)return false;
            Vector3 delta=State.stations[index].position-chef.position;float distance=delta.magnitude;
            if(distance>3.25f||Vector3.Dot(chef.facing,delta.normalized)<.65f)return false;
            for(float step=.15f;step<distance-1.1f;step+=.15f)if(!Walkable(chef.position+delta.normalized*step))return false;
            int previous=chef.target,delivered=State.delivered;chef.target=index;Interact(chef);chef.target=previous;
            if(State.delivered==delivered)return false;
            Notice("번개 서빙 성공! 대시 투척으로 주문 완료",4);return true;
        }
    }
}
