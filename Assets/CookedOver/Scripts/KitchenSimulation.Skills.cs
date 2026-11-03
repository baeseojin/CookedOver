using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookedOver
{
    // IDs are part of the network snapshot contract. Append new skills; never reorder.
    public enum KitchenSkill { FleetFeet, Dash, Knife, Heat, Safety, Wash, Patience, Armor, Recovery, Tips }

    public static class KitchenSkills
    {
        public const int Count = 10;
        public static readonly string[] Names = { "경쾌한 발걸음", "질주 본능", "달인의 칼날", "화력 집중", "내열 코팅", "거품 마스터", "단골의 여유", "강철 멘탈", "따뜻한 한 끼", "팁의 마술사" };
        public static readonly string[] Effects = {
            "이동 속도 +35%", "대시 재사용 속도 +100%", "손질 속도 +100%", "수프·패티 조리 속도 +100%",
            "음식이 타기까지 여유 +8초", "설거지 속도 +100%", "주문 제한시간 소모 속도 완화", "실패 피해 완화",
            "서빙마다 체력 8 회복", "서빙 획득 점수 +50%"
        };
        public static readonly string[] Details = {
            "팀 전체의 이동 속도가 등급마다 크게 빨라집니다.", "대시 쿨다운을 1.2 ÷ (1 + 1.0 × 등급)초로 줄입니다.",
            "선택 즉시 조리대의 손질 중인 재료를 완성합니다.", "선택 즉시 조리 중인 수프·패티를 완성합니다. 탄 음식은 제외합니다.",
            "선택 즉시 모든 불을 끕니다. 탄 음식은 정리해야 합니다.", "선택 즉시 설거지 대기 중인 접시를 모두 씻습니다.",
            "선택 즉시 대기 주문 시간을 15초 회복합니다 (원래 제한까지).", "피해를 1 ÷ (1 + 0.75 × 등급)로 줄이고 선택 즉시 체력 15를 회복합니다.",
            "선택 즉시 체력 25 회복. 체력 최대 100.", "선택 즉시 100점을 얻고 이후 모든 서빙 점수를 강화합니다."
        };
        public static readonly KitchenSkill[] Partners = { KitchenSkill.Dash, KitchenSkill.FleetFeet, KitchenSkill.Heat, KitchenSkill.Knife, KitchenSkill.Armor, KitchenSkill.Recovery, KitchenSkill.Tips, KitchenSkill.Safety, KitchenSkill.Wash, KitchenSkill.Patience };
        public static readonly string[] SynergyNames = { "번개 배달", "라이브 키친", "불굴의 주방", "힐링 서비스", "별 다섯 단골" };
        public static readonly string[] SynergyEffects = { "서빙 경험치 +5", "손질·조리 속도 추가 +25%", "불 확산 절반 · 소화 속도 2배", "서빙 체력 회복 추가 +3", "절반 시간 안에 서빙하면 점수 추가 +20%" };
        public static int Pair(KitchenSkill skill)
        {
            switch (skill) {
                case KitchenSkill.FleetFeet: case KitchenSkill.Dash: return 0;
                case KitchenSkill.Knife: case KitchenSkill.Heat: return 1;
                case KitchenSkill.Safety: case KitchenSkill.Armor: return 2;
                case KitchenSkill.Wash: case KitchenSkill.Recovery: return 3;
                default: return 4;
            }
        }
        public static int Rank(KitchenState state, KitchenSkill skill) => state.skillRanks[(int)skill];
        public static bool Synergy(KitchenState state, KitchenSkill skill) => Rank(state, skill) > 0 && Rank(state, Partners[(int)skill]) > 0;
        public static int RequiredExperience(int level) => 50 + (level - 1) * 20;
    }

    public sealed partial class KitchenSimulation
    {
        public int Rank(KitchenSkill skill) => State.kiosk ? 0 : KitchenSkills.Rank(State, skill);
        public bool Synergy(KitchenSkill skill) => !State.kiosk && KitchenSkills.Synergy(State, skill);
        public bool ChoosingSkill => !State.kiosk && State.skillChoices.Count > 0;
        public float MoveSpeed => 3.7f * (1 + .35f * Rank(KitchenSkill.FleetFeet)) * EquipmentMovement;
        public float DashCooldown => 1.2f / (1 + Rank(KitchenSkill.Dash));
        public float ChopSpeed => (1 + Rank(KitchenSkill.Knife)) * (Synergy(KitchenSkill.Knife) ? 1.25f : 1);
        public float CookingSpeed => (1 + Rank(KitchenSkill.Heat)) * (Synergy(KitchenSkill.Heat) ? 1.25f : 1) * EquipmentCooking;
        public float WashingSpeed => 1 + Rank(KitchenSkill.Wash);
        public float OrderClockSpeed => 1 / (1 + .4f * Rank(KitchenSkill.Patience));
        public float BurnGrace => 8 * Rank(KitchenSkill.Safety);
        public float FireGrowth => Synergy(KitchenSkill.Safety) ? .5f : 1;
        public float ExtinguishingSpeed => Synergy(KitchenSkill.Safety) ? 2 : 1;

        public int FailureDamage(int damage) => Mathf.CeilToInt(damage / (1 + .75f * Rank(KitchenSkill.Armor)));
        public int DeliveryHealing => 8 * Rank(KitchenSkill.Recovery) + (Synergy(KitchenSkill.Recovery) ? 3 : 0);
        public float RewardMultiplier => 1 + .5f * Rank(KitchenSkill.Tips);
        public int DeliveryExperience => 25 + (Synergy(KitchenSkill.FleetFeet) ? 5 : 0);
        // Lightweight read-only preview; never mutate the live state while rendering cards.
        KitchenSimulation(KitchenState state) { State = state; }
        public KitchenSimulation PreviewSkill(KitchenSkill skill)
        {
            var state = new KitchenState { chapter = State.chapter, kiosk = State.kiosk, skillRanks = (int[])State.skillRanks.Clone(), equipmentRanks=(int[])State.equipmentRanks.Clone() };
            state.skillRanks[(int)skill]++;
            return new KitchenSimulation(state);
        }
        public string SkillReadout(KitchenSkill skill)
        {
            switch (skill) {
                case KitchenSkill.FleetFeet: return "이동 속도 " + MoveSpeed.ToString("0.00");
                case KitchenSkill.Dash: return "대시 쿨다운 " + DashCooldown.ToString("0.00") + "초";
                case KitchenSkill.Knife: return "손질 " + (ChopSeconds / ChopSpeed).ToString("0.00") + "초";
                case KitchenSkill.Heat: return (State.chapter == KitchenChapter.Burgers ? "패티 " + (GrillCookSeconds / CookingSpeed).ToString("0.00") : "수프 " + (CookSeconds / CookingSpeed).ToString("0.00")) + "초";
                case KitchenSkill.Safety: return "탄화 " + ((State.chapter == KitchenChapter.Burgers ? GrillBurnSeconds : BurnSeconds) + BurnGrace).ToString("0.0") + "초";
                case KitchenSkill.Wash: return "설거지 " + (WashSeconds / WashingSpeed).ToString("0.00") + "초";
                case KitchenSkill.Patience: return "주문 유효시간 ×" + (1 / OrderClockSpeed).ToString("0.00");
                case KitchenSkill.Armor: return "오제출/만료 피해 " + FailureDamage(WrongDishDamage) + "/" + FailureDamage(MissedOrderDamage);
                case KitchenSkill.Recovery: return "서빙 회복 +" + DeliveryHealing + " (최대 100)";
                default: return "보상 ×" + RewardMultiplier.ToString("0.00") + (Synergy(KitchenSkill.Tips) ? " · 빠른 서빙 +20%" : "");
            }
        }
        void RewardDelivery(OrderState order, int baseReward, bool flameKissed=false)
        {
            float bonus = RewardMultiplier;
            if (Synergy(KitchenSkill.Tips) && order.remaining >= order.total * .5f) bonus *= 1.2f;
            bool flameBonus=flameKissed&&Evolved(KitchenEvolution.Flame);
            if(flameBonus)bonus*=2;
            int reward = Mathf.CeilToInt(baseReward * bonus - .0001f);
            State.score += reward;
            State.health = Mathf.Min(MaxHealth, State.health + DeliveryHealing);
            GrantExperience(DeliveryExperience);
            Notice((flameBonus?"불맛 서빙 ×2! +":"서빙 완료! +") + reward + "점 · 경험치 +" + DeliveryExperience);
        }
        // Used only by host gameplay. Clients receive the resulting snapshot.
        public void GrantExperience(int amount)
        {
            if (State.kiosk || !State.started || State.ended || amount <= 0) return;
            State.experience += amount;
            OfferLevelUp();
        }
        void OfferLevelUp()
        {
            if (ChoosingSkill || State.experience < KitchenSkills.RequiredExperience(State.level)) return;
            State.experience -= KitchenSkills.RequiredExperience(State.level);
            State.level++;
            DrawSkillChoices();
        }
        public static string PickBonusDescription(KitchenSkill skill)
        {
            switch (skill) {
                case KitchenSkill.Dash: return "즉시 팀의 대시 쿨다운 초기화";
                case KitchenSkill.Knife: return "즉시 조리대 재료 손질 완료";
                case KitchenSkill.Heat: return "즉시 조리 중인 수프·패티 완성";
                case KitchenSkill.Safety: return "즉시 모든 화재 진압";
                case KitchenSkill.Wash: return "즉시 대기 중인 접시 모두 세척";
                case KitchenSkill.Patience: return "즉시 주문 시간 +15초 (원래 제한까지)";
                case KitchenSkill.Armor: return "즉시 체력 +15 (최대 100)";
                case KitchenSkill.Recovery: return "즉시 체력 +25 (최대 100)";
                case KitchenSkill.Tips: return "즉시 점수 +100";
                default: return "즉시 팀 전체 이동 속도 증가";
            }
        }
        void ApplyPickBonus(KitchenSkill skill)
        {
            switch (skill) {
                case KitchenSkill.Dash: foreach(var chef in State.chefs) chef.dashCooldown=0; break;
                case KitchenSkill.Knife:
                    foreach(var station in State.stations) if(station.kind==StationKind.Board && station.item.Raw && !station.item.chopped) {station.item.chopped=true;station.progress=0;} break;
                case KitchenSkill.Heat:
                    foreach(var station in State.stations) {
                        if(station.fire>0||StationOffline(State.stations.IndexOf(station)))continue;
                        if(station.kind==StationKind.Stove && station.portions==3 && station.soup!=Food.Burnt) station.heat=Mathf.Max(station.heat,CookSeconds/CookingSpeed);
                        if(station.kind==StationKind.Grill && station.item.food==Food.RawPatty) {station.heat=Mathf.Max(station.heat,GrillCookSeconds/CookingSpeed);TickGrill(station,0);}
                    } break;
                case KitchenSkill.Safety: foreach(var station in State.stations)station.fire=0; break;
                case KitchenSkill.Wash: WashWaitingPlates(); break;
                case KitchenSkill.Patience: foreach(var order in State.orders)order.remaining=Mathf.Min(order.total,order.remaining+15); break;
                case KitchenSkill.Armor: State.health=Mathf.Min(MaxHealth,State.health+15); break;
                case KitchenSkill.Recovery: State.health=Mathf.Min(MaxHealth,State.health+25); break;
                case KitchenSkill.Tips: State.score+=100; break;
            }
        }
        public bool ChooseSkill(int skill)
        {
            if (State.kiosk || State.ended || State.equipmentChoice || !State.started || !State.skillChoices.Contains(skill)) return false;
            int before=EvolutionMask;
            State.skillRanks[skill]++;
            ApplyPickBonus((KitchenSkill)skill);
            State.skillChoices.Clear();
            Notice(KitchenSkills.Names[skill] + " 적용 · " + SkillReadout((KitchenSkill)skill), 6);
            int unlocked=EvolutionMask&~before;
            if(unlocked!=0)for(int i=0;i<3;i++)if((unlocked&(1<<i))!=0)Notice("기술 진화! "+KitchenEvolutions.Names[i],6);
            OfferLevelUp(); // Carry overflow XP into consecutive selections, never discard it.
            return true;
        }
    }
}
