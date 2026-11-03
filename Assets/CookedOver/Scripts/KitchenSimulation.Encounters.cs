using UnityEngine;
namespace CookedOver
{
    public enum KitchenEvent { None, Oil, Blackout, Rush }
    public sealed partial class KitchenSimulation
    {
        public static KitchenState FromSnapshot(string json)
        {
            var state=JsonUtility.FromJson<KitchenState>(json);
            // JsonUtility recreates inline null classes as defaults. Presence must be explicit.
            if(state!=null&&!state.bossActive)state.boss=null;
            return state;
        }
        public bool NormalOrdersOnHold=>!State.kiosk&&State.boss!=null;
        public bool ChoosingReward => ChoosingSkill || State.equipmentChoice;
        public bool StationOffline(int index) => !State.kiosk && State.kitchenEvent==KitchenEvent.Blackout && State.eventTime>0 && State.blackoutStation==index;
        public float FloorSpeed(Vector3 position) => !State.kiosk && State.equipmentRanks[2]==0 && State.kitchenEvent==KitchenEvent.Oil && Vector3.Distance(position,State.oilPosition)<1.4f ? .45f : 1;
        public float EquipmentCooking => State.kiosk ? 1 : 1+.25f*State.equipmentRanks[0];
        public float EquipmentMovement => State.kiosk ? 1 : 1+.15f*State.equipmentRanks[2];
        public void BeginKitchenEvent(KitchenEvent kind)
        {
            if(State.kiosk||!State.started||State.ended||kind==KitchenEvent.None)return;
            State.kitchenEvent=kind;State.eventTime=25;State.eventWave=State.wave;
            State.blackoutStation=State.stations.FindIndex(s=>s.kind==StationKind.Grill||s.kind==StationKind.Stove);
            State.oilPosition=new Vector3(0,0,1.6f);
            if(kind==KitchenEvent.Rush)State.nextOrder=Mathf.Min(State.nextOrder,2);
            Notice(kind==KitchenEvent.Oil?"기름 유출! 표시 구역을 우회하세요.":kind==KitchenEvent.Blackout?"부분 정전! 꺼진 조리 설비는 잠시 사용할 수 없습니다.":"주문 폭주! 25초 동안 주문이 빠르게 들어옵니다.",5);
        }
        public void BeginBossOrder()
        {
            if(State.kiosk||!State.started||State.ended||State.boss!=null)return;
            State.bossWave=State.wave;State.bossActive=true;
            int tier=ThreatTier,quantity=3+Mathf.Min(3,tier);float deadline=120+20*(quantity-3);
            Food recipe=State.chapter==KitchenChapter.Burgers?(tier==0?Food.Burger:tier==1?Food.Cheeseburger:Food.DeluxeBurger):(tier%2==0?Food.TomatoSoup:Food.OnionSoup);
            State.boss=new OrderState{dish=recipe,required=quantity,total=deadline,remaining=deadline,reward=100+15*tier,difficultyTier=tier};
            Notice("대식가 등장! 일반 주문의 시간과 유입은 잠시 멈춥니다.",6);
        }
        void TickEncounters(float dt)
        {
            if(State.kiosk)return;
            if(State.eventTime>0) {State.eventTime=Mathf.Max(0,State.eventTime-dt);if(State.eventTime==0){State.kitchenEvent=KitchenEvent.None;State.blackoutStation=-1;}}
            if(State.eventWave!=State.wave && State.remaining<=RoundSeconds-25)BeginKitchenEvent((KitchenEvent)(1+(State.wave-1)%3));
            if(State.bossWave!=State.wave && State.boss==null && State.remaining<=RoundSeconds-90)BeginBossOrder();
            if(State.boss!=null){State.boss.remaining-=dt*OrderClockSpeed;
                if(State.boss.remaining<=0){State.boss=null;State.bossActive=false;State.missed++;Penalize(40,"대식가 주문 실패");}}
            if(State.equipmentRanks[1]>0) {
                State.autoWashTime+=dt;
                float period=4f/State.equipmentRanks[1];
                if(State.autoWashTime>=period){State.autoWashTime=0;if(State.dirtyPlates>0){State.dirtyPlates--;State.cleanPlates++;}}
            }
        }
        bool TryServeBoss(ChefState chef)
        {
            var boss=State.boss;if(State.kiosk||boss==null||chef.held.food!=boss.dish)return false;
            bool flameKissed=chef.held.flameKissed;
            boss.completed++;State.delivered++;State.combo++;chef.held=new Item();State.dirtyPlates++;
            bool complete=boss.completed>=boss.required;
            if(complete){State.boss=null;State.bossActive=false;State.bossesDefeated++;State.score+=300+50*boss.difficultyTier;State.equipmentChoice=true;}
            RewardDelivery(boss,boss.reward,flameKissed);
            if(complete)Notice("대식가 주문 완료! +"+(300+50*boss.difficultyTier)+"점 · 설비 보상을 선택하세요.",6);
            else Notice("대식가 서빙 "+boss.completed+" / "+boss.required,4);
            return true;
        }
        public void WashWaitingPlates()
        {
            State.cleanPlates+=State.dirtyPlates;State.dirtyPlates=0;
        }
        public bool ChooseEquipment(int id)
        {
            if(State.kiosk||!State.started||State.ended||!State.equipmentChoice||id<0||id>2)return false;
            State.equipmentRanks[id]++;State.equipmentChoice=false;
            if(id==1)WashWaitingPlates();
            if(id==2)State.health=Mathf.Min(MaxHealth,State.health+20);
            Notice("설비 보상 적용! 주방이 강해졌습니다.",5);return true;
        }
    }
}
