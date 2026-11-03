using UnityEngine;
namespace CookedOver
{
    public sealed partial class KitchenSimulation
    {
        // Difficulty follows run progress, not movement/cooking multipliers. Existing orders never change retroactively.
        public int ThreatTier=>State.kiosk?0:Mathf.Clamp(Mathf.Max(State.wave-1,(State.level-1)/4),0,5);
        public float ArrivalInterval=>Mathf.Max(12,22-2*ThreatTier);
        void ConfigureOrder(OrderState order)
        {
            order.difficultyTier=ThreatTier;order.required=1+ThreatTier/2;
            order.total+=35*(order.required-1);order.remaining=order.total;order.reward+=10*ThreatTier;
        }
        public static int SkillCategory(int id)
        {
            if(id==(int)KitchenSkill.FleetFeet||id==(int)KitchenSkill.Dash)return 2;
            if(id==(int)KitchenSkill.Knife||id==(int)KitchenSkill.Heat||id==(int)KitchenSkill.Wash)return 0;
            return 1;
        }
        public static string SkillCategoryName(int id)=>SkillCategory(id)==0?"생산":SkillCategory(id)==1?"생존 · 보상":"이동";
        void DrawSkillChoices()
        {
            for(int category=0;category<3;category++) {
                int total=0;
                for(int id=0;id<KitchenSkills.Count;id++)if(SkillCategory(id)==category)total+=State.skillRanks[id]==0?3:State.skillRanks[id]<3?2:1;
                int roll=Random.Range(0,total),choice=0;
                for(int id=0;id<KitchenSkills.Count;id++)if(SkillCategory(id)==category){roll-=State.skillRanks[id]==0?3:State.skillRanks[id]<3?2:1;if(roll<0){choice=id;break;}}
                State.skillChoices.Add(choice);
            }
            // Keep at least one new option while any skill remains unowned, without duplicating categories.
            if(State.skillChoices.TrueForAll(id=>State.skillRanks[id]>0))
                for(int id=0;id<KitchenSkills.Count;id++)if(State.skillRanks[id]==0){State.skillChoices[SkillCategory(id)]=id;break;}
        }
    }
}
