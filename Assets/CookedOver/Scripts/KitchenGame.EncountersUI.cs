using UnityEngine;
namespace CookedOver
{
    public sealed partial class KitchenGame
    {
        public bool ChooseTeamEquipment(int id)
        {
            if(!Authority)return false;
            bool result=simulation.ChooseEquipment(id);
            if(result){System.Array.Clear(inputs,0,inputs.Length);showSkills=false;}
            return result;
        }
        static readonly string[] EquipmentNames={"터보 조리 설비","자동 식기세척기","미끄럼 방지 바닥"};
        static readonly string[] EquipmentEffects={"조리 속도 추가 +25% / 등급","4 ÷ 등급 초마다 접시 1개 자동 세척","기름 감속 면역 · 이동 속도 +15% / 등급"};
        static readonly string[] EquipmentDetails={"기술 카드의 조리 가속과 곱해서 적용됩니다.\n현재 조리 중인 음식에도 바로 적용됩니다.","설거지 대기 중인 접시를 자동으로 씻습니다.\n선택 즉시 대기 중인 접시를 모두 씻습니다.","기름 구역을 그대로 통과할 수 있습니다.\n선택 즉시 팀 체력을 20 회복합니다."};
        void DrawEncounterHUD()
        {
            if(State.kiosk)return;
            if(State.eventTime>0) {
                string name=State.kitchenEvent==KitchenEvent.Oil?(State.equipmentRanks[2]>0?"기름 · 감속 면역":"기름 유출 · 이동 -55%"):State.kitchenEvent==KitchenEvent.Blackout?"부분 정전":"주문 폭주";
                Round(new Rect(797,82,382,32),new Color(.65f,.25f,.17f),8);
                Label(new Rect(808,82,360,32),name+"  ·  "+Mathf.CeilToInt(State.eventTime)+"초",17,Paper,true,TextAnchor.MiddleCenter);
            } else Label(new Rect(806,84,370,28),State.bossWave!=State.wave?"대식가 등장까지 "+Mathf.CeilToInt(Mathf.Max(0,State.remaining-150))+"초":"다음 웨이브에 새 도전",15,Paper,false,TextAnchor.MiddleCenter);
            if(State.boss!=null) {
                var boss=State.boss;Card(new Rect(1195,274,219,258),new Color(.25f,.16f,.22f));
                Label(new Rect(1210,285,189,29),"BOSS · 대식가",22,SkillGold,true,TextAnchor.MiddleCenter);
                FoodIcon(new Rect(1260,327,90,75),boss.dish);
                Label(new Rect(1206,408,197,28),Recipe(boss.dish),20,Paper,true,TextAnchor.MiddleCenter);
                Label(new Rect(1206,443,197,32),boss.completed+" / "+boss.required+"개",25,SkillGold,true,TextAnchor.MiddleCenter);
                Label(new Rect(1206,484,197,26),Mathf.CeilToInt(boss.remaining)+"초 · 완료 시 설비 보상",14,boss.remaining<25?Red:Paper,true,TextAnchor.MiddleCenter);
            }
            if(State.boss!=null)Label(new Rect(1206,511,197,20),"일반 주문 시간·유입 정지",12,SkillGold,true,TextAnchor.MiddleCenter);
            int gear=State.equipmentRanks[0]+State.equipmentRanks[1]+State.equipmentRanks[2];
            if(gear>0) {
                Card(new Rect(1195,549,219,100),new Color(.11f,.29f,.28f));
                for(int i=0;i<3;i++)Label(new Rect(1207,556+i*28,195,26),EquipmentNames[i]+"  R"+State.equipmentRanks[i],14,Paper,true);
            }
        }
        void DrawEquipmentReward()
        {
            Flat(new Rect(-1000,-1000,3440,2900),new Color(.025f,.06f,.10f,.95f));
            Label(new Rect(160,100,1120,38),"B O S S   C L E A R",23,SkillGold,true,TextAnchor.MiddleCenter);
            Label(new Rect(160,157,1120,67),"대식가 만족! 주방 설비를 강화하세요",36,Paper,true,TextAnchor.MiddleCenter);
            Label(new Rect(160,236,1120,32),Authority?"팀 전체에 적용 · 같은 설비를 다시 선택하면 강화됩니다":"방장이 설비 보상을 선택하고 있습니다",18,Paper,false,TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++) {
                float x=162+i*384;Card(new Rect(x,312,348,346),Paper,22);
                SkillEmblem(new Rect(x+25,337,80,80),i==0?3:i==1?5:0,new Color(.15f,.39f,.43f));
                Label(new Rect(x+25,437,298,35),EquipmentNames[i],26,Ink,true);
                Label(new Rect(x+25,480,298,27),"설비 등급 "+State.equipmentRanks[i]+" → "+(State.equipmentRanks[i]+1),17,Ink,true);
                SkillParagraph(new Rect(x+25,520,298,55),EquipmentEffects[i],19,new Color(.13f,.48f,.40f));
                SkillParagraph(new Rect(x+25,587,298,62),EquipmentDetails[i],15,Ink);
                if(Authority&&Button(new Rect(x,686,348,56),(i+1)+"  설비 선택",Mint,true,22)){ChooseTeamEquipment(i);return;}
            }
            Label(new Rect(160,790,1120,30),"선택하는 동안 주문과 조리가 멈춥니다. 다음 보스에서도 설비 보상을 얻습니다.",17,Paper,false,TextAnchor.MiddleCenter);
            if(Authority&&Event.current.type==EventType.KeyDown){int id=(int)Event.current.keyCode-(int)KeyCode.Alpha1;if(id>=0&&id<3){ChooseTeamEquipment(id);Event.current.Use();}}
        }
    }
}
