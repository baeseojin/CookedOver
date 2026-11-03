using System;
using UnityEngine;

namespace CookedOver.Editor
{
    public static class KitchenSkillVerification
    {
        static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
        static KitchenSimulation New(KitchenChapter chapter = KitchenChapter.Classic) { var sim = new KitchenSimulation(); sim.Reset(chapter); sim.Start(); return sim; }
        static void Equip(KitchenSimulation sim, KitchenSkill skill)
        {
            sim.State.skillChoices.Clear(); sim.State.skillChoices.Add((int)skill);
            Check(sim.ChooseSkill((int)skill), "offered skill selection succeeds");
        }
        static void Serve(KitchenSimulation sim, float remaining = 100)
        {
            var chef = sim.State.chefs[0]; sim.State.orders.Clear();
            sim.State.orders.Add(new OrderState { dish = Food.Salad, remaining = remaining, total = 100, reward = 100 });
            chef.target = sim.State.stations.FindIndex(s => s.kind == StationKind.Serve); chef.held = new Item(Food.Salad); sim.Interact(chef);
        }
        public static void Progression()
        {
            var sim = New(); var state = sim.State;
            var preview = sim.PreviewSkill(KitchenSkill.Knife);
            Check(Mathf.Abs(KitchenSimulation.ChopSeconds/preview.ChopSpeed-1.1f)<.001f && sim.ChopSpeed==1 && state.skillRanks[2]==0 && state.skillChoices.Count==0, "preview matches actual formula without changing live state");
            var invalidChef = state.chefs[0]; invalidChef.target = state.stations.FindIndex(s => s.kind == StationKind.Serve); invalidChef.held = new Item(Food.RawPatty); sim.Interact(invalidChef);
            Check(state.experience == 0 && state.level == 1, "invalid submission grants no XP");
            sim = New(); state = sim.State;
            Check(state.level == 1 && state.experience == 0, "level-one start");
            Serve(sim); Check(state.experience == 25 && !sim.ChoosingSkill, "first actual delivery awards XP");
            Serve(sim); Check(state.level == 2 && state.experience == 0 && state.skillChoices.Count == 3 && new System.Collections.Generic.HashSet<int>(state.skillChoices).Count == 3, "second delivery offers three unique cards");
            state.orders.Add(new OrderState{dish=Food.OnionSoup,remaining=80}); var cooking = state.stations.Find(s => s.kind == StationKind.Stove); cooking.soup = Food.Tomato; cooking.portions = 3;
            float clock = state.remaining, heat = cooking.heat, orderClock = state.orders[0].remaining; var position = state.chefs[0].position;
            sim.Tick(.05f, new[] { new ChefInput { move = Vector2.right, interact = true } });
            Check(state.remaining == clock && state.chefs[0].position == position && cooking.heat == heat && state.orders[0].remaining == orderClock, "selection freezes simulation");
            int delivered = state.delivered; Serve(sim); Check(state.delivered == delivered, "selection blocks direct serving");
            Check(!sim.ChooseSkill(-1) && !sim.ChooseSkill(99), "invalid choices rejected");
            int chosen = state.skillChoices[0]; Check(sim.ChooseSkill(chosen) && state.skillRanks[chosen] == 1 && !sim.ChoosingSkill && !sim.ChooseSkill(chosen), "one selection one rank");
            sim.GrantExperience(200); int selections = 0;
            while (sim.ChoosingSkill) { Check(selections++ < 10, "queued level-ups terminate"); sim.ChooseSkill(state.skillChoices[0]); }
            Check(selections == 2 && state.level == 4 && state.experience == 40, "overflow XP retained across consecutive choices");
            sim.GrantExperience(100); Check(sim.ChoosingSkill, "new level queued before reset"); sim.RestartRun();
            Check(sim.State.level == 1 && sim.State.experience == 0 && sim.State.skillChoices.Count == 0 && Array.TrueForAll(sim.State.skillRanks, r => r == 0), "death reset clears progression");
            sim = New(); sim.EnableKioskMode(); sim.GrantExperience(1000); Check(sim.State.level == 1 && !sim.ChoosingSkill, "kiosk remains isolated");
            // Repeat upgrades have no cap: every offer remains three meaningful distinct skills.
            sim = New(); for (int i = 0; i < 10; i++) sim.State.skillRanks[i] = 3;
            sim.GrantExperience(50); Check(sim.State.skillChoices.Count == 3, "all-owned skill pool still offers three upgrades");
            chosen = sim.State.skillChoices[0]; sim.ChooseSkill(chosen); Check(sim.State.skillRanks[chosen] == 4, "stacked rank increases");
        }
        public static void Effect(KitchenSkill skill)
        {
            var sim = New(skill == KitchenSkill.Heat || skill == KitchenSkill.Safety ? KitchenChapter.Burgers : KitchenChapter.Classic);
            Equip(sim, skill); var state = sim.State; var chef = state.chefs[0];
            switch (skill) {
                case KitchenSkill.FleetFeet:
                    chef.position = new Vector3(0, 0, 1.5f); sim.Tick(.05f, new[] { new ChefInput { move = Vector2.right } });
                    Check(Mathf.Abs(chef.position.x - 4.995f * .05f)<.001f, "fleet feet changes actual movement"); break;
                case KitchenSkill.Dash:
                    chef.position = new Vector3(0, 0, 1.5f); sim.Tick(.05f, new[] { new ChefInput { move = Vector2.right, dash = true } });
                    Check(Mathf.Abs(chef.dashCooldown-.6f)<.001f && chef.dashTime > 0, "dash reduces real cooldown"); break;
                case KitchenSkill.Knife:
                    var board = state.stations.Find(s => s.kind == StationKind.Board); board.item = new Item(Food.Tomato);
                    sim.Work(chef, board, 1.05f); Check(!board.item.chopped,"knife still needs work"); sim.Work(chef,board,.1f); Check(board.item.chopped, "knife accelerates chopping"); break;
                case KitchenSkill.Heat:
                    var grill = state.stations.Find(s => s.kind == StationKind.Grill); grill.item = new Item(Food.RawPatty);
                    for (int i = 0; i < 58; i++) sim.Tick(.05f, null);
                    Check(grill.item.food==Food.RawPatty,"patty not ready before three seconds");for(int i=0;i<4;i++)sim.Tick(.05f,null);
                    Check(grill.item.food == Food.CookedPatty && grill.heat < 3.2f, "heat cooks patty early");
                    var soup = New(); Equip(soup, skill); var stove = soup.State.stations.Find(s => s.kind == StationKind.Stove);
                    stove.soup = Food.Tomato; stove.portions = 3; stove.heat = 4.6f; var c = soup.State.chefs[0]; c.held = new Item(Food.Plate); c.target = soup.State.stations.IndexOf(stove); soup.Interact(c);
                    Check(c.held.food == Food.TomatoSoup, "heat cooks soup early"); break;
                case KitchenSkill.Safety:
                    var safeGrill = state.stations.Find(s => s.kind == StationKind.Grill); safeGrill.item = new Item(Food.CookedPatty); safeGrill.heat = 21.9f;
                    sim.Tick(.05f, null); Check(safeGrill.fire == 0 && safeGrill.item.food == Food.CookedPatty, "coating delays patty burn");
                    var safeSoup = New(); Equip(safeSoup, skill); var safeStove = safeSoup.State.stations.Find(s => s.kind == StationKind.Stove); safeStove.portions = 3; safeStove.soup = Food.Tomato; safeStove.heat = 25.9f;
                    safeSoup.Tick(.05f, null); Check(safeStove.fire == 0, "coating delays soup burn"); break;
                case KitchenSkill.Wash:
                    state.dirtyPlates = 1; sim.Work(chef, state.stations.Find(s => s.kind == StationKind.Sink), 1.55f);
                    Check(state.dirtyPlates == 0 && state.cleanPlates == 5, "wash speed changes actual plate recovery"); break;
                case KitchenSkill.Patience:
                    state.orders[0].remaining = 80; sim.Tick(.05f, null); Check(state.orders[0].remaining > 79.95f, "patience slows order decay"); break;
                case KitchenSkill.Armor:
                    chef.target = state.stations.FindIndex(s => s.kind == StationKind.Serve); chef.held = new Item(Food.RawPatty); sim.Interact(chef);
                    Check(state.health == 85 && state.score == -30, "armor reduces damage, not score penalty"); break;
                case KitchenSkill.Recovery:
                    state.health = 50; Serve(sim); Check(state.health == 58, "recovery heals on valid delivery");
                    state.experience = 0; state.health = 99; Serve(sim); Check(state.health == 100, "healing caps at 100"); break;
                case KitchenSkill.Tips:
                    Serve(sim); Check(state.score == 295, "tips increases actual award"); break;
            }
        }
        public static void PickBonuses()
        {
            var sim=New();sim.State.health=40;Equip(sim,KitchenSkill.Armor);Check(sim.State.health==55,"armor pick heals 15");
            sim=New();sim.State.health=40;Equip(sim,KitchenSkill.Recovery);Check(sim.State.health==65&&sim.State.cleanPlates==4,"recovery pick heals without creating plates");
            sim=New();sim.State.cleanPlates=2;sim.State.dirtyPlates=2;Equip(sim,KitchenSkill.Wash);Check(sim.State.cleanPlates==4&&sim.State.dirtyPlates==0,"wash pick converts existing dirty plates");
            sim=New();sim.State.chefs[0].dashCooldown=1;sim.State.chefs[2].dashCooldown=1;Equip(sim,KitchenSkill.Dash);Check(sim.State.chefs.TrueForAll(c=>c.dashCooldown==0),"dash pick resets every chef cooldown");
            sim=New();var board=sim.State.stations.Find(s=>s.kind==StationKind.Board);board.item=new Item(Food.Tomato);Equip(sim,KitchenSkill.Knife);Check(board.item.chopped,"knife pick finishes current board");
            sim=New(KitchenChapter.Burgers);var grill=sim.State.stations.Find(s=>s.kind==StationKind.Grill);grill.item=new Item(Food.RawPatty);Equip(sim,KitchenSkill.Heat);Check(grill.item.food==Food.CookedPatty,"heat pick finishes current patty");
            sim=New();var stove=sim.State.stations.Find(s=>s.kind==StationKind.Stove);stove.portions=3;stove.soup=Food.Tomato;Equip(sim,KitchenSkill.Heat);Check(stove.heat>=KitchenSimulation.CookSeconds/sim.CookingSpeed,"heat pick finishes current soup");
            stove.fire=1;stove.soup=Food.Burnt;Equip(sim,KitchenSkill.Safety);Check(stove.fire==0&&stove.soup==Food.Burnt,"safety extinguishes without restoring burnt food");
            sim=New();sim.State.orders[0].remaining=20;Equip(sim,KitchenSkill.Patience);Check(sim.State.orders[0].remaining==35&&sim.State.orders[1].remaining==sim.State.orders[1].total,"patience adds 15 seconds and caps at original total");
            sim=New();Equip(sim,KitchenSkill.Tips);Check(sim.State.score==100,"tips pick grants immediate score");
            int score=sim.State.score;Check(!sim.ChooseSkill((int)KitchenSkill.Tips)&&sim.State.score==score,"duplicate selection cannot repeat bonus");
            Equip(sim,KitchenSkill.Tips);Check(sim.State.score==200&&sim.Rank(KitchenSkill.Tips)==2,"repeat upgrade grants bonus once and stacks");
            sim.RestartRun();Check(sim.State.score==0&&sim.State.cleanPlates==4&&sim.State.health==100,"pick bonuses clear on new run");
        }
        public static void Synergies()
        {
            var sim = New(); Equip(sim, KitchenSkill.FleetFeet); Serve(sim); Check(sim.State.experience == 25, "one partner alone is not a synergy");
            sim = New(); Equip(sim, KitchenSkill.FleetFeet); Equip(sim, KitchenSkill.Dash); Serve(sim); Check(sim.State.experience == 30, "courier grants bonus XP");
            sim = New(); Equip(sim, KitchenSkill.Knife); Equip(sim, KitchenSkill.Heat);
            var board = sim.State.stations.Find(s => s.kind == StationKind.Board); board.item = new Item(Food.Tomato); sim.Work(sim.State.chefs[0], board, 1.5f);
            Check(board.item.chopped && sim.CookingSpeed > 1.2f, "live kitchen boosts work and cooking");
            sim = New(); Equip(sim, KitchenSkill.Safety); Equip(sim, KitchenSkill.Armor);
            var stove = sim.State.stations.Find(s => s.kind == StationKind.Stove); stove.fire = .5f; sim.Tick(.05f, null); Check(Mathf.Abs(stove.fire - .502f) < .0001f, "fire synergy halves growth");
            var chef = sim.State.chefs[0]; chef.held = new Item(Food.Extinguisher); sim.Work(chef, stove, .1f); Check(Mathf.Abs(stove.fire - .342f) < .0001f, "fire synergy doubles extinguishing");
            sim = New(); Equip(sim, KitchenSkill.Wash); Equip(sim, KitchenSkill.Recovery); sim.State.health = 50; Serve(sim); Check(sim.State.health == 61, "healing service grants extra recovery");
            sim = New(); Equip(sim, KitchenSkill.Patience); Equip(sim, KitchenSkill.Tips); Serve(sim); Check(sim.State.score == 334, "fast service synergy multiplies tips");
            sim = New(); Equip(sim, KitchenSkill.Patience); Equip(sim, KitchenSkill.Tips); Serve(sim, 40); Check(sim.State.score == 268, "late delivery has no fast-service bonus");
        }
    }
}
