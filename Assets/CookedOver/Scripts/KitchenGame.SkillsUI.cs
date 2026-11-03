using UnityEngine;

namespace CookedOver
{
    public sealed partial class KitchenGame
    {
        bool showSkills;
        int feedbackEvolution, feedbackEvolutionId=-1; int feedbackRun, feedbackRanks; KitchenSkill feedbackSkill; float feedbackUntil;
        void DrawSkillFeedback()
        {
            if(State.kiosk)return;
            int total=0;for(int i=0;i<KitchenSkills.Count;i++)total+=State.skillRanks[i];
            if(feedbackRun!=State.run){feedbackRun=State.run;feedbackRanks=0;feedbackUntil=0;feedbackEvolution=0;feedbackEvolutionId=-1;System.Array.Clear(feedbackSeen,0,feedbackSeen.Length);}
            if(total>feedbackRanks) {
                feedbackEvolutionId=-1;
                for(int i=0;i<KitchenSkills.Count;i++) if(State.skillRanks[i]>feedbackSeen[i])feedbackSkill=(KitchenSkill)i;
                feedbackUntil=Time.unscaledTime+2.4f;
                if(sound!=null)sound.PlayOneShot(successTone,.8f);
            }
            int unlocked=simulation.EvolutionMask&~feedbackEvolution;
            if(unlocked!=0){for(int i=0;i<3;i++)if((unlocked&(1<<i))!=0)feedbackEvolutionId=i;feedbackUntil=Time.unscaledTime+4.2f;}
            feedbackEvolution=simulation.EvolutionMask;
            feedbackRanks=total;for(int i=0;i<KitchenSkills.Count;i++)feedbackSeen[i]=State.skillRanks[i];
            if(Time.unscaledTime>=feedbackUntil||State.ended||State.paused||simulation.ChoosingSkill)return;
            float alpha=Mathf.Clamp01((feedbackUntil-Time.unscaledTime)*2);
            Color old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            Card(new Rect(410,286,620,112),new Color(.12f,.29f,.31f));
            Label(new Rect(432,295,576,34),feedbackEvolutionId>=0?"기술 진화! "+KitchenEvolutions.Names[feedbackEvolutionId]:KitchenSkills.Names[(int)feedbackSkill]+"  R"+State.skillRanks[(int)feedbackSkill]+" 강화!",25,SkillGold,true,TextAnchor.MiddleCenter);
            Label(new Rect(432,333,576,26),feedbackEvolutionId>=0?KitchenEvolutions.Requirement(feedbackEvolutionId):simulation.SkillReadout(feedbackSkill),20,Paper,true,TextAnchor.MiddleCenter);
            Label(new Rect(432,367,576,22),feedbackEvolutionId>=0?"보유 기술 목록의 진화 빌드에서 사용법을 확인하세요":KitchenSimulation.PickBonusDescription(feedbackSkill),15,Paper,false,TextAnchor.MiddleCenter);GUI.color=old;
        }
        readonly int[] feedbackSeen=new int[KitchenSkills.Count];
        public bool RestartTeamRun()
        {
            if (!Authority || !State.started || !State.ended) return false;
            simulation.RestartRun(); System.Array.Clear(inputs, 0, inputs.Length); showSkills = false; return true;
        }
        public bool ChooseTeamSkill(int skill)
        {
            if (!Authority) return false;
            bool selected = simulation.ChooseSkill(skill);
            if (selected) { System.Array.Clear(inputs, 0, inputs.Length); showSkills = false; }
            return selected;
        }
        static readonly Color SkillGold = new Color(1, .76f, .32f);
        void SkillParagraph(Rect rect, string value, int size, Color color)
        {
            var style = new GUIStyle(TextStyle(size, false, TextAnchor.UpperLeft)) { wordWrap = true };
            style.normal.textColor = color; GUI.Label(rect, value, style);
        }
        void DrawExperience()
        {
            if (State.kiosk) return;
            Card(new Rect(26, 536, 302, 96), new Color(.11f, .22f, .30f));
            Label(new Rect(42, 542, 270, 34), "LV. " + State.level + "   주방의 성장", 22, SkillGold, true);
            int required = KitchenSkills.RequiredExperience(State.level);
            Label(new Rect(42, 578, 270, 20), "EXP  " + State.experience + " / " + required + "  · 서빙 +" + simulation.DeliveryExperience, 14, Paper);
            Round(new Rect(42, 607, 270, 9), Ink, 4);
            float fraction = Mathf.Clamp01((float)State.experience / required);
            if (fraction > 0) Round(new Rect(42, 607, 270 * fraction, 9), SkillGold, 4);
            if (!simulation.ChoosingReward && !State.ended && !State.paused && Button(new Rect(26, 482, 302, 42), showSkills ? "기술 목록 닫기" : "보유 기술 · 시너지 보기", new Color(.19f, .34f, .39f), true, 17)) showSkills = !showSkills;
        }
        void SkillEmblem(Rect rect, int id, Color color)
        {
            Disc(rect, color);
            var inset = new Rect(rect.x + 15, rect.y + 15, rect.width - 30, rect.height - 30);
            switch ((KitchenSkill)id) {
                case KitchenSkill.Knife: FoodIcon(inset, Food.Tomato, true); break;
                case KitchenSkill.Heat: FoodIcon(inset, Food.CookedPatty); break;
                case KitchenSkill.Safety: FoodIcon(inset, Food.Extinguisher); break;
                case KitchenSkill.Wash: FoodIcon(inset, Food.Plate); break;
                case KitchenSkill.Recovery: Label(rect, "+", 50, Paper, true, TextAnchor.MiddleCenter); break;
                case KitchenSkill.Tips: Label(rect, "$", 43, Paper, true, TextAnchor.MiddleCenter); break;
                case KitchenSkill.Armor: Hat(inset); break;
                case KitchenSkill.Patience: Label(rect, "TIME", 19, Paper, true, TextAnchor.MiddleCenter); break;
                default:
                    for (int i = 0; i < 3; i++) Line(new Vector2(rect.x + 23 + i * 12, rect.y + 24), new Vector2(rect.x + 12 + i * 12, rect.y + 55), 5, Paper);
                    break;
            }
        }
        void DrawLevelUp()
        {
            Flat(new Rect(-1000, -1000, 3440, 2900), new Color(.025f, .06f, .10f, .94f));
            Label(new Rect(160, 83, 1120, 35), "L E V E L   U P", 19, SkillGold, true, TextAnchor.MiddleCenter);
            Label(new Rect(160, 123, 1120, 67), "레벨 " + State.level + " · 새로운 기술을 선택하세요", 38, Paper, true, TextAnchor.MiddleCenter);
            Label(new Rect(160, 197, 1120, 32), Authority ? "팀 공용 · 생산 / 생존·보상 / 이동 중 선택 · 같은 기술은 등급 강화" : "방장이 팀의 기술을 선택하고 있습니다", 19, new Color(.66f, .78f, .81f), false, TextAnchor.MiddleCenter);
            // Capture the offered IDs: choosing can replace this list for queued level-ups.
            int[] offered = State.skillChoices.ToArray();
            for (int i = 0; i < offered.Length; i++) {
                int id = offered[i]; var skill = (KitchenSkill)id; var partner = KitchenSkills.Partners[id];
                var preview=simulation.PreviewSkill(skill);bool evolves=(preview.EvolutionMask&~simulation.EvolutionMask)!=0;
                float x = 162 + i * 384;
                bool combo = KitchenSkills.Rank(State, partner) > 0;
                Color accent = combo ? SkillGold : new Color(.38f, .73f, .72f);
                bool hover = Authority && new Rect(x, 270, 348, 474).Contains(Event.current.mousePosition);
                Card(new Rect(x - 3, 267, 354, 480), hover ? SkillGold : new Color(.20f, .37f, .41f), 23);
                Card(new Rect(x, 270, 348, 474), Paper, 21);
                Label(new Rect(x + 25, 288, 296, 26), evolves?"진화 해금! / "+(KitchenSkills.Rank(State,skill)+1)+"등급":KitchenSkills.Rank(State, skill) == 0 ? "새 기술  /  1등급" : "기술 강화  /  " + (KitchenSkills.Rank(State, skill) + 1) + "등급", 16, Ink, true);
                Label(new Rect(x+121,350,200,42),KitchenSimulation.SkillCategoryName(id),19,Ink,true);
                SkillEmblem(new Rect(x + 25, 333, 80, 80), id, new Color(.15f, .39f, .43f));
                Label(new Rect(x + 25, 429, 300, 36), KitchenSkills.Names[id], 27, Ink, true);
                SkillParagraph(new Rect(x + 25, 477, 298, 53), KitchenSkills.Effects[id], 21, new Color(.13f, .48f, .40f));
                SkillParagraph(new Rect(x + 25, 539, 298, 74), simulation.SkillReadout(skill) + "\n→ " + preview.SkillReadout(skill) + "\n" + KitchenSimulation.PickBonusDescription(skill), 14, Ink);
                Round(new Rect(x + 20, 625, 308, 99), combo ? new Color(1, .86f, .57f) : new Color(.87f, .90f, .86f), 10);
                Label(new Rect(x + 31, 630, 286, 26), (combo ? "시너지 발동! " : "조합 · ") + KitchenSkills.SynergyNames[KitchenSkills.Pair(skill)], 17, Ink, true);
                Label(new Rect(x + 31, 660, 286, 23), "짝 기술: " + KitchenSkills.Names[(int)partner], 15, Ink);
                SkillParagraph(new Rect(x + 31, 687, 286, 34), EvolutionCardHint(skill), 13, Ink);
                if (Authority && (Button(new Rect(x, 766, 348, 53), (i + 1) + "   " + (combo ? "시너지 선택" : "기술 선택"), combo ? new Color(.81f, .51f, .16f) : new Color(.20f, .57f, .53f), true, 21))) {
                    ChooseTeamSkill(id); showSkills = false; return;
                }
            }
            Label(new Rect(160, 840, 1120, 30), "주문 · 조리 · 이동이 멈춰 있습니다. 선택 후 즉시 재개됩니다.", 17, new Color(.66f, .78f, .81f), false, TextAnchor.MiddleCenter);
            if (Authority && Event.current.type == EventType.KeyDown) {
                int index = (int)Event.current.keyCode - (int)KeyCode.Alpha1;
                if (index >= 0 && index < offered.Length) { ChooseTeamSkill(offered[index]); showSkills = false; Event.current.Use(); }
            }
        }
        void DrawSkillCollection()
        {
            Flat(new Rect(-1000, -1000, 3440, 2900), new Color(.025f, .06f, .10f, .94f));
            Label(new Rect(80, 60, 1050, 60), "주방의 기술  ·  LV. " + State.level, 36, Paper, true);
            if (Button(new Rect(1190, 64, 170, 48), "닫기", Mint, true, 21)) showSkills = false;
            if(Button(new Rect(970,64,190,48),showEvolutions?"기술 목록":"진화 빌드",Orange,true,20))showEvolutions=!showEvolutions;
            if(showEvolutions){DrawEvolutionCollection();return;}
            Label(new Rect(80, 125, 1280, 30), "짝 기술을 각각 1등급 이상 보유하면 자동 시너지 발동 · 이 목록을 보는 동안에도 영업은 계속됩니다", 18, SkillGold);
            for (int i = 0; i < KitchenSkills.Count; i++) {
                float x = 80 + (i % 2) * 650, y = 185 + (i / 2) * 95;
                int rank = KitchenSkills.Rank(State, (KitchenSkill)i);
                Card(new Rect(x, y, 630, 82), rank > 0 ? Paper : new Color(.15f, .25f, .29f), 12);
                Label(new Rect(x + 18, y + 7, 270, 30), KitchenSkills.Names[i] + "  " + (rank > 0 ? "R" + rank : "미보유"), 21, rank > 0 ? Ink : Paper, true);
                Label(new Rect(x + 18, y + 43, 596, 28), simulation.SkillReadout((KitchenSkill)i), 17, rank > 0 ? Ink : new Color(.6f, .73f, .74f));
            }
            for (int i = 0; i < 5; i++) {
                var skill = new[] { KitchenSkill.FleetFeet, KitchenSkill.Knife, KitchenSkill.Safety, KitchenSkill.Wash, KitchenSkill.Patience }[i];
                bool active = KitchenSkills.Synergy(State, skill);
                Label(new Rect(80, 685 + i * 30, 1280, 27), (active ? "● 활성  " : "○ 잠김  ") + KitchenSkills.SynergyNames[i] + "   " + KitchenSkills.Names[(int)skill] + " + " + KitchenSkills.Names[(int)KitchenSkills.Partners[(int)skill]] + "   →   " + KitchenSkills.SynergyEffects[i], 17, active ? SkillGold : new Color(.61f, .72f, .75f));
            }
        }
    }
}
