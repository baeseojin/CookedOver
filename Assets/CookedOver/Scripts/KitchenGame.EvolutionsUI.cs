using UnityEngine;
namespace CookedOver
{
    public sealed partial class KitchenGame
    {
        bool showEvolutions;
        string EvolutionCardHint(KitchenSkill skill)
        {
            var preview=simulation.PreviewSkill(skill);
            for(int i=0;i<3;i++)if((KitchenEvolutions.Primary[i]==skill||KitchenEvolutions.Secondary[i]==skill)&&preview.Evolved((KitchenEvolution)i)&&!simulation.Evolved((KitchenEvolution)i))return "진화: "+KitchenEvolutions.Requirement(i);
            for(int i=0;i<3;i++)if((KitchenEvolutions.Primary[i]==skill||KitchenEvolutions.Secondary[i]==skill)&&!simulation.Evolved((KitchenEvolution)i))return "진화: "+KitchenEvolutions.Requirement(i);
            return KitchenSkills.SynergyEffects[KitchenSkills.Pair(skill)];
        }
        void DrawEvolutionCollection()
        {
            Label(new Rect(80,125,1280,32),"주력 R3 + 짝 R1이면 자동 진화 · 팀 전체 적용 · 재시작 시 초기화",20,SkillGold);
            for(int i=0;i<3;i++) {
                float y=193+i*204;bool active=simulation.Evolved((KitchenEvolution)i);
                Card(new Rect(80,y,1280,180),active?Paper:new Color(.15f,.25f,.29f),20);
                SkillEmblem(new Rect(105,y+31,90,90),(int)KitchenEvolutions.Primary[i],new Color(.15f,.39f,.43f));
                Label(new Rect(220,y+15,1040,37),KitchenEvolutions.Names[i]+(active?"  · 진화 완료":"  · 미해금"),29,active?Ink:Paper,true);
                int main=simulation.Rank(KitchenEvolutions.Primary[i]),pair=simulation.Rank(KitchenEvolutions.Secondary[i]);
                Label(new Rect(220,y+61,1040,30),KitchenEvolutions.Requirement(i)+"   [현재 "+main+"/3 · "+pair+"/1]",20,active?Mint:SkillGold,true);
                SkillParagraph(new Rect(220,y+107,1100,56),KitchenEvolutions.Effects[i],20,active?Ink:Paper);
            }
            Label(new Rect(80,825,1280,32),"자동화: 재료를 조리대에 놓기  ·  불맛: 늦게 꺼낸 재료로 완성  ·  번개 서빙: SPACE 대시 중 Q",17,Paper);
        }
    }
}
