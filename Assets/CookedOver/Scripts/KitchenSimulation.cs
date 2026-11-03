using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookedOver
{
    public enum KitchenChapter { Classic, Burgers }
    public enum Food { None, Tomato, Onion, Lettuce, Plate, DirtyPlate, Extinguisher, TomatoSoup, OnionSoup, Salad, Burnt, Bun, RawPatty, CookedPatty, Cheese, Burger, Cheeseburger, DeluxeBurger, BulgogiBurger, ShrimpBurger, DoubleBurger, RawShrimp, CookedShrimp, BulgogiSauce, RawFries, RawCheeseSticks, Fries, CheeseSticks, Cola, Sprite }
    public enum StationKind { Counter, Tomato, Onion, Lettuce, Board, Stove, Plates, Sink, Serve, Trash, Extinguisher, Buns, Meat, Cheese, Grill, Shrimp, Sauce, Fryer, KioskSupplies, DrinkSupplies }
    [Serializable] public class Item
    {
        public Food food; public bool chopped, flameKissed; public int tomato, lettuce, bun, patty, cheese, shrimp, sauce;
        public Item(Food f = Food.None) { food = f; }
        public bool Empty => food == Food.None;
        public bool Raw => food == Food.Tomato || food == Food.Onion || food == Food.Lettuce;
        public bool Dish => food == Food.TomatoSoup || food == Food.OnionSoup || food == Food.Salad || food == Food.Burger || food == Food.Cheeseburger || food == Food.DeluxeBurger || KitchenSimulation.IsKioskDish(food);
        public string Label => food == Food.None ? "" : food == Food.Plate ? "Plate" + (tomato > 0 ? " + tomato" : "") + (lettuce > 0 ? " + lettuce" : "") : (chopped ? "Cut " : "") + food.ToString();
    }
    [Serializable] public class ChefState
    {
        public int id; public Vector3 position, facing = Vector3.forward; public Item held = new Item();
        public float dashCooldown, dashTime, work; public int target = -1; public bool active;
    }
    [Serializable] public class StationState
    {
        public string name; public StationKind kind; public Vector3 position; public Item item = new Item();
        public float progress, heat, fire; public Food soup; public int portions;
    }
    [Serializable] public class OrderState { public string externalTaskId,externalOrderId,externalName; public int required=1,completed,difficultyTier; public bool pendingAck; public Food dish; public float remaining = 90, total = 90; public int reward; }
    [Serializable] public class LooseItem { public Item item; public Vector3 position, velocity; public float age; }
    [Serializable] public class KitchenState
    {
        public KitchenChapter chapter;
        public int health = 100, run = 1, wave = 1; public float restartIn;
        public KitchenEvent kitchenEvent; public float eventTime,autoWashTime; public int eventWave,bossWave,blackoutStation=-1,bossesDefeated;
        public Vector3 oilPosition; public OrderState boss; public bool bossActive,equipmentChoice; public int[] equipmentRanks=new int[3];
        public int level = 1, experience; public int[] skillRanks = new int[KitchenSkills.Count];
        public List<int> skillChoices = new List<int>();
        public List<ChefState> chefs = new List<ChefState>(); public List<StationState> stations = new List<StationState>();
        public List<OrderState> orders = new List<OrderState>(); public List<LooseItem> loose = new List<LooseItem>();
        public float remaining = 240, nextOrder = 14; public int score, delivered, missed, combo, cleanPlates = 4, dirtyPlates, sequence;
        public bool kiosk; public bool started, ended, paused; public string notice = "Welcome to LITTLE LADLE"; public float noticeTime;
    }
    public struct ChefInput { public Vector2 move; public bool interact, work, dash, toss; }

    // Host owns every gameplay mutation. Rendering and transport consume this serializable state.
    public sealed partial class KitchenSimulation
    {
        public const float RoundSeconds = 240, ChopSeconds = 2.2f, CookSeconds = 9, BurnSeconds = 18, WashSeconds = 3;
        public const int MaxHealth = 100, WrongDishDamage = 25, MissedOrderDamage = 30;
        // restartIn is retained in snapshots for compatibility; manual restart keeps it at zero.
        public KitchenState State;
        public KitchenSimulation() { Reset(); }
        public void Reset(KitchenChapter chapter = KitchenChapter.Classic)
        {
            State = new KitchenState { chapter = chapter };
            if (chapter == KitchenChapter.Classic)
            {
            Add("TOMATO", StationKind.Tomato, -5, 3); Add("ONION", StationKind.Onion, -3, 3); Add("LETTUCE", StationKind.Lettuce, -1, 3);
            Add("CHOP", StationKind.Board, 1, 3); Add("CHOP", StationKind.Board, 3, 3); Add("BIN", StationKind.Trash, 5, 3);
            Add("PLATES", StationKind.Plates, -5, -3); Add("WASH", StationKind.Sink, -3, -3); Add("SERVE", StationKind.Serve, 3, -3);
            Add("FIRE KIT", StationKind.Extinguisher, 5, -3);
            Add("STOVE 1", StationKind.Stove, -1, -3); Add("STOVE 2", StationKind.Stove, 1, -3);
            Add("PASS", StationKind.Counter, -1, 0); Add("PASS", StationKind.Counter, 1, 0);
            }
            else BuildBurgerLayout();
            for (int i = 0; i < 4; i++) State.chefs.Add(new ChefState { id = i, position = Spawn(i) });
            State.chefs[0].active = true;
        }
        public void RestartRun()
        {
            var previous = State;
            Reset(previous.chapter);
            State.run = previous.run + 1;
            for (int i = 0; i < State.chefs.Count; i++) State.chefs[i].active = previous.chefs[i].active;
            if (previous.kiosk) EnableKioskMode();
            Start();
            Notice("새 도전 " + State.run + " · 체력과 주방이 초기화됐습니다.", 5);
        }
        void Penalize(int damage, string reason)
        {
            if (State.kiosk || State.ended) return;
            damage = FailureDamage(damage);
            State.score -= 30; State.health = Mathf.Max(0, State.health - damage); State.combo = 0;
            Notice(reason + " · 체력 -" + damage + " / 점수 -30", 4);
            if (State.health == 0) { State.ended = true; State.restartIn = 0; Notice("주방이 무너졌습니다! 재시작 버튼을 눌러주세요.", 100); }
        }
        public static Vector3 Spawn(int i) => new Vector3(i % 2 == 0 ? -3 : 3, 0, i < 2 ? -0.8f : 1.2f);
        void Add(string n, StationKind k, float x, float z) => State.stations.Add(new StationState { name = n, kind = k, position = new Vector3(x, 0, z) });
        public void Start() { if (State.started) return; State.started = true; if(!State.kiosk){AddOrder(); AddOrder();} Notice(State.chapter == KitchenChapter.Burgers ? "Burger service open!" : "Service is open!", 5); }
        public void Notice(string text, float seconds = 2.5f) { State.notice = text; State.noticeTime = seconds; }
        void AddOrder()
        {
            if(State.chapter == KitchenChapter.Burgers) { AddBurgerOrder(); return; }
            var dishes=ThreatTier==0?new[]{Food.Salad,Food.TomatoSoup,Food.OnionSoup}:new[]{Food.TomatoSoup,Food.OnionSoup,Food.Salad,Food.TomatoSoup};
            Food dish=dishes[State.sequence++%dishes.Length];
            var order=new OrderState{dish=dish,reward=dish==Food.Salad?80:120,remaining=95,total=95};ConfigureOrder(order);State.orders.Add(order);
        }
        public int Target(ChefState c)
        {
            int best = -1; float distance = 1.65f;
            for (int i = 0; i < State.stations.Count; i++)
            {
                Vector3 d = State.stations[i].position - c.position;
                if (d.magnitude < distance && Vector3.Dot(c.facing, d.normalized) > 0.15f) { best = i; distance = d.magnitude; }
            }
            return best;
        }
        bool Walkable(Vector3 p)
        {
            if (Mathf.Abs(p.x) > 6 || Mathf.Abs(p.z) > 3.85f) return false;
            foreach (var s in State.stations) if (Mathf.Abs(p.x - s.position.x) < 1.04f && Mathf.Abs(p.z - s.position.z) < 0.88f) return false;
            return true;
        }
        public void Tick(float dt, ChefInput[] inputs)
        {
            if (!State.started || State.paused || ChoosingReward) return;
            if (State.ended) return;
            dt=Mathf.Clamp(dt,0,.05f);State.remaining=Mathf.Max(0,State.remaining-dt);State.noticeTime-=dt;
            if(State.remaining<=0){
                if(State.kiosk){State.ended=true;Notice("영업 종료! 미완료 키오스크 주문은 그대로 유지됩니다.",100);return;}
                State.wave++;State.remaining=RoundSeconds;Notice("생존 웨이브 "+State.wave,4);
            }
            bool normalOrdersOnHold=NormalOrdersOnHold;
            TickEncounters(dt);normalOrdersOnHold|=NormalOrdersOnHold;if(State.ended)return;
            for (int i = 0; i < State.chefs.Count; i++)
            {
                var c = State.chefs[i]; if (!c.active) continue;
                ChefInput input = inputs != null && i < inputs.Length ? inputs[i] : default;
                Vector2 move = Vector2.ClampMagnitude(input.move, 1); c.dashCooldown -= dt; c.dashTime -= dt;
                if (input.dash && c.dashCooldown <= 0 && move.sqrMagnitude > 0) { c.dashTime = .18f; c.dashCooldown = DashCooldown; }
                Vector3 delta = new Vector3(move.x, 0, move.y) * (c.dashTime > 0 ? 9 * (MoveSpeed / 3.7f) : MoveSpeed) * FloorSpeed(c.position) * dt;
                if (move.sqrMagnitude > .01f) c.facing = new Vector3(move.x, 0, move.y).normalized;
                Vector3 pos = c.position; if (Walkable(pos + new Vector3(delta.x, 0, 0))) pos.x += delta.x;
                if (Walkable(pos + new Vector3(0, 0, delta.z))) pos.z += delta.z;
                bool overlap = false; foreach (var other in State.chefs) if (other.active && other != c && Vector3.Distance(pos, other.position) < .58f) overlap = true;
                if (!overlap) c.position = pos;
                c.target = Target(c); if (input.interact) Interact(c); if (State.ended || ChoosingReward) return; if (input.toss) Toss(c);
                if (input.work && c.target >= 0) Work(c, State.stations[c.target], dt);
            }
            TickAutomation(dt);
            foreach (var s in State.stations)
            {
                if(StationOffline(State.stations.IndexOf(s)))continue;
                if(s.kind == StationKind.Fryer) { TickFryer(s,dt); continue; }
                if(s.kind == StationKind.Grill) { TickGrill(s, dt); continue; }
                if (s.kind != StationKind.Stove) continue;
                if (s.fire > 0) { s.fire = Mathf.Min(1, s.fire + dt * .08f * FireGrowth); continue; }
                if (s.portions == 3 && s.soup != Food.Burnt)
                {
                    s.heat += dt;
                    if (s.heat >= BurnSeconds + BurnGrace) { s.soup = Food.Burnt; s.fire = 1; State.combo = 0; Notice("FIRE! Hold work with the extinguisher."); }
                }
            }
            for (int i = State.loose.Count - 1; i >= 0; i--)
            {
                var l = State.loose[i]; l.age += dt;
                if (l.velocity.sqrMagnitude > .05f)
                {
                    Vector3 p = l.position + l.velocity * dt;
                    if (Walkable(p)) l.position = p; else l.velocity = Vector3.zero;
                    l.velocity = Vector3.MoveTowards(l.velocity, Vector3.zero, dt * 10);
                }
            }
            if(State.kiosk||normalOrdersOnHold||NormalOrdersOnHold)return;
            for(int i=State.orders.Count-1;i>=0;i--){
                State.orders[i].remaining-=dt*OrderClockSpeed;
                if(State.orders[i].remaining<=0){State.orders.RemoveAt(i);State.missed++;Penalize(MissedOrderDamage,"주문 시간 초과");if(State.ended)return;}
            }
            State.nextOrder -= dt;
            if (State.nextOrder <= 0) { if (State.orders.Count < 4) AddOrder(); State.nextOrder = ArrivalInterval*(State.kitchenEvent==KitchenEvent.Rush?.4f:1); }
            if (State.orders.Count == 0) AddOrder();
        }
        public void Interact(ChefState c)
        {
            if (State.ended || State.paused || ChoosingReward) return;
            if (c.held.Empty)
            {
                for (int i = 0; i < State.loose.Count; i++) if (Vector3.Distance(c.position, State.loose[i].position) < 1.1f)
                { c.held = State.loose[i].item; State.loose.RemoveAt(i); return; }
            }
            if (c.target < 0) return;
            var s = State.stations[c.target]; var h = c.held;
            if(StationOffline(c.target)){Notice("설비 정전 중! 다른 조리 설비를 이용하세요.");return;}
            if (s.fire > 0) { Notice("Extinguish the fire first"); return; }
            if(InteractKioskStation(c,s))return;
            if(s.kind == StationKind.Buns || s.kind == StationKind.Meat || s.kind == StationKind.Cheese)
            { if(h.Empty) c.held = new Item(s.kind == StationKind.Buns ? Food.Bun : s.kind == StationKind.Meat ? Food.RawPatty : Food.Cheese); return; }
            if(s.kind == StationKind.Grill) { InteractGrill(c,s); return; }
            if (s.kind == StationKind.Tomato || s.kind == StationKind.Onion || s.kind == StationKind.Lettuce)
            { if (h.Empty) c.held = new Item(s.kind == StationKind.Tomato ? Food.Tomato : s.kind == StationKind.Onion ? Food.Onion : Food.Lettuce); return; }
            if (s.kind == StationKind.Extinguisher)
            { if (h.Empty) c.held = new Item(Food.Extinguisher); else if (h.food == Food.Extinguisher) c.held = new Item(); return; }
            if (s.kind == StationKind.Plates)
            { if (h.Empty && State.cleanPlates > 0) { c.held = new Item(Food.Plate); State.cleanPlates--; } else if (h.food == Food.Plate && h.tomato == 0 && h.lettuce == 0 && h.bun == 0 && h.patty == 0 && h.cheese == 0 && h.shrimp == 0 && h.sauce == 0) { State.cleanPlates++; c.held = new Item(); } return; }
            if (s.kind == StationKind.Sink)
            { if (h.food == Food.DirtyPlate) { State.dirtyPlates++; c.held = new Item(); } else if (h.Empty && State.dirtyPlates > 0) Notice("Hold work to wash dishes"); return; }
            if (s.kind == StationKind.Trash)
            {
                if (h.food == Food.DirtyPlate) State.dirtyPlates++;
                else if (h.food == Food.Plate || h.Dish && UsesPlate(h.food)) State.cleanPlates++;
                c.held = new Item(); Notice("Discarded"); return;
            }
            if (s.kind == StationKind.Serve)
            {
                if (h.Empty && State.dirtyPlates > 0) { State.dirtyPlates--; c.held = new Item(Food.DirtyPlate); return; }
                if (!h.Dish)
                {
                    if (!h.Empty && !State.kiosk) RejectDish(c); else Notice("Serve a plated recipe here");
                    return;
                }
                if(TryServeKiosk(c))return;
                if(TryServeBoss(c))return;
                var order = State.orders.Find(o => o.dish == h.food);
                if (order == null) { RejectDish(c); return; }
                bool flameKissed=h.flameKissed;
                int tip = Mathf.CeilToInt(order.remaining / order.total * 30) + Mathf.Min(State.combo, 4) * 10;
                State.combo++; State.delivered++; order.completed++;if(order.completed>=order.required)State.orders.Remove(order); c.held = new Item(); State.dirtyPlates++;
                RewardDelivery(order, order.reward + tip, flameKissed); return;
            }
            if (s.kind == StationKind.Stove)
            {
                if (h.Raw && h.chopped && h.food != Food.Lettuce && s.portions < 3 && (s.portions == 0 || s.soup == h.food))
                { s.soup = h.food; s.portions++; c.held = new Item(); return; }
                if (h.food == Food.Plate && h.tomato == 0 && h.lettuce == 0 && s.portions == 3 && s.heat >= CookSeconds / CookingSpeed && s.soup != Food.Burnt)
                { c.held = new Item(s.soup == Food.Tomato ? Food.TomatoSoup : Food.OnionSoup){flameKissed=FlameWindow(s.heat,BurnSeconds+BurnGrace)}; ClearPot(s); return; }
                if (s.soup == Food.Burnt && h.Empty) { ClearPot(s); Notice("Pot cleaned"); return; }
                Notice("3 matching chopped vegetables; cook 9s; collect with a plate"); return;
            }
            // A plate can assemble ingredients in either direction, on any preparation counter.
            if (State.chapter == KitchenChapter.Burgers ? TryBurger(h, s.item) : TryPlate(h, s.item)) { s.item = new Item(); s.progress = 0; return; }
            if (State.chapter == KitchenChapter.Burgers ? TryBurger(s.item, h) : TryPlate(s.item, h)) { c.held = new Item(); return; }
            if (h.Empty && !s.item.Empty) { c.held = s.item; s.item = new Item(); s.progress = 0; }
            else if (!h.Empty && s.item.Empty) { s.item = h; c.held = new Item(); s.progress = 0; }
        }
        void RejectDish(ChefState chef)
        {
            if (chef.held.food == Food.Plate || chef.held.Dish && UsesPlate(chef.held.food)) State.dirtyPlates++;
            chef.held = new Item();
            Penalize(WrongDishDamage, "잘못된 음식 제출");
        }
        static bool TryPlate(Item plate, Item ingredient)
        {
            if (plate.food != Food.Plate || !ingredient.chopped) return false;
            if (ingredient.food == Food.Tomato && plate.tomato == 0) plate.tomato++;
            else if (ingredient.food == Food.Lettuce && plate.lettuce == 0) plate.lettuce++; else return false;
            if (plate.tomato == 1 && plate.lettuce == 1) plate.food = Food.Salad;
            return true;
        }
        static void ClearPot(StationState s) { s.portions = 0; s.heat = 0; s.soup = Food.None; s.fire = 0; }
        public void Work(ChefState c, StationState s, float dt)
        {
            if (State.ended || State.paused || ChoosingReward) return;
            if((s.kind==StationKind.KioskSupplies||s.kind==StationKind.DrinkSupplies) && c.held.Empty){s.progress+=dt;if(s.progress>=.35f){s.progress=0;var choices=SuppliesFor(s.kind);s.portions=(s.portions+1)%choices.Length;Notice("F: 품목 변경 / E: 가져오기 · "+choices[s.portions]);}return;}
            if (s.fire > 0 && c.held.food == Food.Extinguisher)
            { s.fire = Mathf.Max(0, s.fire - dt * .8f * ExtinguishingSpeed); if (s.fire <= 0) Notice("Fire out! Interact empty-handed to clean pot"); return; }
            if (!c.held.Empty) return;
            if (s.kind == StationKind.Board && s.item.Raw && !s.item.chopped)
            { s.progress += dt * ChopSpeed; if (s.progress >= ChopSeconds) { s.item.chopped = true; s.progress = 0; } }
            if (s.kind == StationKind.Sink && State.dirtyPlates > 0)
            { s.progress += dt * WashingSpeed; if (s.progress >= WashSeconds) { State.dirtyPlates--; State.cleanPlates++; s.progress = 0; Notice("Clean plate ready"); } }
        }
        public void Toss(ChefState c)
        {
            if (State.ended || State.paused || ChoosingReward || c.held.Empty) return;
            if(TryDashServe(c))return;
            if(State.loose.Count>=24)return;
            Vector3 p = c.position + c.facing * .55f;
            if (!Walkable(p)) return;
            State.loose.Add(new LooseItem { item = c.held, position = p, velocity = c.facing * 8 }); c.held = new Item();
        }
        public int Stars => State.score >= 900 ? 3 : State.score >= 500 ? 2 : State.score >= 200 ? 1 : 0;
    }
}


