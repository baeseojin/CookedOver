using System;
using UnityEngine;

namespace CookedOver
{
    // Presentation only: all cooking and multiplayer actions stay in KitchenGame.
    public sealed partial class KitchenGame
    {
        static readonly Color Ink = new Color(.13f,.23f,.25f);
        static readonly Color Paper = new Color(1,.97f,.88f);
        static readonly Color Orange = new Color(1,.48f,.20f);
        static readonly Color Mint = new Color(.24f,.67f,.52f);
        static readonly Color Red = new Color(.94f,.29f,.24f);
        static readonly Color[] PlayerColors = { new Color(1,.60f,.23f), new Color(.29f,.69f,.90f), new Color(.72f,.49f,.82f), new Color(.40f,.73f,.45f) };
        Texture2D uiRound, uiCircle, uiGradient;
        Font uiFont;
        GUIStyle uiText, uiField, uiHit;
        readonly System.Collections.Generic.Dictionary<int, GUIStyle> uiStyles = new System.Collections.Generic.Dictionary<int, GUIStyle>();
        int menuNetworkTab;
        float uiScale;
        Vector2 uiOrigin;
        int displayedScore = -1;
        float scorePop;
        Camera uiCamera;
        bool cameraStyled;

        void InitUI()
        {
            if(uiRound != null) return;
            uiRound = RoundedTexture(64,18); uiCircle = RoundedTexture(64,32);
            uiGradient = new Texture2D(1,128,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave, wrapMode=TextureWrapMode.Clamp };
            for(int y=0;y<128;y++) uiGradient.SetPixel(0,y,Color.Lerp(new Color(.08f,.22f,.24f),new Color(.20f,.39f,.37f),y/127f));
            uiGradient.Apply();
            uiFont=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial Rounded MT Bold","Arial"},24);
            uiText=new GUIStyle { font=uiFont, alignment=TextAnchor.MiddleLeft, wordWrap=false, clipping=TextClipping.Clip };
            uiField=new GUIStyle(GUI.skin.textField) { font=uiFont,fontSize=22,padding=new RectOffset(16,12,10,8),alignment=TextAnchor.MiddleLeft };
            uiField.normal.textColor=Ink;uiField.focused.textColor=Ink;uiField.normal.background=Texture2D.whiteTexture;uiField.focused.background=Texture2D.whiteTexture;
            uiHit=new GUIStyle();
        }
        static Texture2D RoundedTexture(int size,int radius)
        {
            var t=new Texture2D(size,size,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear };
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(radius-x-.5f, x+.5f-(size-radius));
                float dy=Mathf.Max(radius-y-.5f, y+.5f-(size-radius));
                float distance=Mathf.Sqrt(Mathf.Max(dx,0)*Mathf.Max(dx,0)+Mathf.Max(dy,0)*Mathf.Max(dy,0));
                t.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(radius-distance)));
            }
            t.Apply();return t;
        }
        GUIStyle TextStyle(int size,bool bold,TextAnchor align)
        {
            int key=size*100+(bold?50:0)+(int)align;
            if(!uiStyles.TryGetValue(key,out var style))
            {style=new GUIStyle(uiText){fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=align};uiStyles[key]=style;}
            return style;
        }
        void Label(Rect r,string text,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {var style=TextStyle(size,bold,align);style.normal.textColor=color;GUI.Label(r,text,style);}
        void Flat(Rect r,Color color) { GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white; }
        void Round(Rect r,Color color,float radius=16)
        {GUI.color=Color.white;GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,radius);}
        void Disc(Rect r,Color color) {GUI.color=color;GUI.DrawTexture(r,uiCircle);GUI.color=Color.white;}
        void Card(Rect r,Color color,float radius=18)
        {Round(new Rect(r.x+2,r.y+6,r.width,r.height),new Color(.025f,.09f,.10f,.22f),radius);Round(r,color,radius);}
        bool Button(Rect r,string text,Color fill,bool enabled=true,int textSize=24)
        {
            bool hover=enabled&&r.Contains(Event.current.mousePosition);
            float lift=hover?-3:0;
            Round(new Rect(r.x,r.y+6,r.width,r.height),Color.Lerp(fill,Ink,.35f),16);
            Round(new Rect(r.x,r.y+lift,r.width,r.height),enabled?(hover?Color.Lerp(fill,Color.white,.12f):fill):new Color(.62f,.67f,.65f),16);
            Label(new Rect(r.x,r.y+lift,r.width,r.height),text,textSize,fill==Paper?Ink:Color.white,true,TextAnchor.MiddleCenter);
            return enabled&&GUI.Button(r,GUIContent.none,uiHit);
        }
        void Line(Vector2 a,Vector2 b,float width,Color color)
        {
            var old=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,angle),Vector3.one);Flat(new Rect(0,-width/2,Vector2.Distance(a,b),width),color);GUI.matrix=old;
        }
        void Star(Vector2 center,float radius,Color color)
        {
            // A five-point silhouette drawn without external texture dependencies.
            for(int i=0;i<5;i++)
            {
                float a=(-90+i*72)*Mathf.Deg2Rad,b=(-90+(i+2)*72)*Mathf.Deg2Rad;
                Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,radius*.33f,color);
            }
            Disc(new Rect(center.x-radius*.33f,center.y-radius*.33f,radius*.66f,radius*.66f),color);
        }
        void FoodIcon(Rect r,Food f,bool chopped=false)
        {
            if(DrawKioskFoodIcon(r,f))return;
            if(DrawBurgerFoodIcon(r,f))return;
            float x=r.x,y=r.y,w=r.width,h=r.height;
            if(f==Food.Tomato||f==Food.TomatoSoup)
            {
                if(f==Food.TomatoSoup){Bowl(r,new Color(.93f,.30f,.17f));return;}
                Disc(new Rect(x+w*.12f,y+h*.18f,w*.76f,h*.73f),new Color(.73f,.16f,.12f));
                Disc(new Rect(x+w*.10f,y+h*.12f,w*.76f,h*.73f),new Color(.97f,.30f,.23f));
                Disc(new Rect(x+w*.26f,y+h*.24f,w*.16f,h*.12f),new Color(1,.64f,.44f));
                Line(new Vector2(x+w*.48f,y+h*.24f),new Vector2(x+w*.49f,y+h*.03f),w*.09f,new Color(.22f,.47f,.27f));
                Line(new Vector2(x+w*.49f,y+h*.22f),new Vector2(x+w*.30f,y+h*.09f),w*.08f,new Color(.22f,.47f,.27f));
                Line(new Vector2(x+w*.49f,y+h*.22f),new Vector2(x+w*.69f,y+h*.10f),w*.08f,new Color(.22f,.47f,.27f));
            }
            else if(f==Food.Onion||f==Food.OnionSoup)
            {
                if(f==Food.OnionSoup){Bowl(r,new Color(.83f,.62f,.36f));return;}
                Disc(new Rect(x+w*.14f,y+h*.18f,w*.72f,h*.72f),new Color(.66f,.42f,.70f));
                Disc(new Rect(x+w*.23f,y+h*.20f,w*.54f,h*.65f),new Color(.90f,.75f,.91f));
                Disc(new Rect(x+w*.38f,y+h*.22f,w*.22f,h*.6f),new Color(1,.91f,.91f));
                Line(new Vector2(x+w*.48f,y+h*.23f),new Vector2(x+w*.40f,y+h*.03f),w*.08f,new Color(.46f,.61f,.32f));
            }
            else if(f==Food.Lettuce||f==Food.Salad)
            {
                if(f==Food.Salad)
                {Disc(new Rect(x,y+h*.1f,w,h*.8f),new Color(.74f,.81f,.76f));Disc(new Rect(x+w*.05f,y+h*.1f,w*.90f,h*.70f),Color.white);}
                Disc(new Rect(x+w*.18f,y+h*.19f,w*.59f,h*.62f),new Color(.27f,.58f,.30f));
                Disc(new Rect(x+w*.12f,y+h*.15f,w*.45f,h*.45f),new Color(.50f,.76f,.31f));
                Disc(new Rect(x+w*.46f,y+h*.13f,w*.40f,h*.46f),new Color(.61f,.82f,.39f));
                Disc(new Rect(x+w*.31f,y+h*.38f,w*.47f,h*.41f),new Color(.46f,.72f,.28f));
                if(f==Food.Salad){Disc(new Rect(x+w*.28f,y+h*.30f,w*.19f,h*.18f),Red);Disc(new Rect(x+w*.60f,y+h*.48f,w*.17f,h*.17f),Red);}
            }
            else if(f==Food.Plate||f==Food.DirtyPlate)
            {Disc(new Rect(x,y+h*.12f,w,h*.76f),new Color(.72f,.80f,.77f));Disc(new Rect(x+w*.08f,y+h*.15f,w*.84f,h*.6f),Paper);if(f==Food.DirtyPlate)Disc(new Rect(x+w*.33f,y+h*.33f,w*.34f,h*.25f),new Color(.62f,.43f,.25f));}
            else if(f==Food.Extinguisher)
            {Round(new Rect(x+w*.28f,y+h*.23f,w*.44f,h*.67f),Red,8);Flat(new Rect(x+w*.34f,y+h*.10f,w*.30f,h*.16f),Ink);Line(new Vector2(x+w*.58f,y+h*.15f),new Vector2(x+w*.85f,y+h*.47f),w*.08f,Ink);}
            else if(f==Food.Burnt) {Bowl(r,new Color(.20f,.23f,.21f));}
            if(chopped){Round(new Rect(x+w*.67f,y+h*.65f,w*.30f,h*.30f),Mint,6);Label(new Rect(x+w*.67f,y+h*.65f,w*.30f,h*.30f),"✓",Mathf.Max(10,(int)(w*.22f)),Color.white,true,TextAnchor.MiddleCenter);}
        }
        void Bowl(Rect r,Color soup)
        {
            Disc(new Rect(r.x+r.width*.08f,r.y+r.height*.27f,r.width*.84f,r.height*.65f),new Color(.72f,.80f,.77f));
            Disc(new Rect(r.x+r.width*.08f,r.y+r.height*.16f,r.width*.84f,r.height*.54f),Color.white);
            Disc(new Rect(r.x+r.width*.15f,r.y+r.height*.20f,r.width*.70f,r.height*.39f),soup);
            Disc(new Rect(r.x+r.width*.33f,r.y+r.height*.31f,r.width*.12f,r.height*.07f),new Color(.32f,.58f,.28f));
            Line(new Vector2(r.x+r.width*.38f,r.y+r.height*.10f),new Vector2(r.x+r.width*.42f,r.y-r.height*.05f),r.width*.035f,new Color(1,1,1,.75f));
            Line(new Vector2(r.x+r.width*.61f,r.y+r.height*.10f),new Vector2(r.x+r.width*.65f,r.y-r.height*.08f),r.width*.035f,new Color(1,1,1,.75f));
        }
        void Hat(Rect r)
        {
            Disc(new Rect(r.x+r.width*.05f,r.y+r.height*.09f,r.width*.53f,r.height*.61f),Color.white);
            Disc(new Rect(r.x+r.width*.29f,r.y,r.width*.54f,r.height*.65f),Color.white);
            Disc(new Rect(r.x+r.width*.55f,r.y+r.height*.13f,r.width*.43f,r.height*.55f),Color.white);
            Round(new Rect(r.x+r.width*.23f,r.y+r.height*.48f,r.width*.57f,r.height*.39f),Paper,8);
            Flat(new Rect(r.x+r.width*.28f,r.y+r.height*.75f,r.width*.47f,r.height*.035f),new Color(.78f,.83f,.75f));
        }
        public static string Recipe(Food food) => KioskRecipeName(food) ?? (food==Food.Burger?"기본 버거":food==Food.Cheeseburger?"치즈버거":food==Food.DeluxeBurger?"디럭스 버거":food==Food.Salad?"가든 샐러드":food==Food.TomatoSoup?"토마토 수프":"양파 수프");
        static string FoodName(Item item)
        {
            string kioskName=KioskRecipeName(item.food);if(kioskName!=null)return kioskName;
            if(item.food==Food.Plate&&(item.bun>0||item.patty>0||item.cheese>0))return "버거 조립 중";
            switch(item.food)
            {
                case Food.Bun:return "버거 빵";case Food.RawPatty:return "생패티";case Food.CookedPatty:return "구운 패티";case Food.Cheese:return "치즈";
                case Food.None:return "빈손";case Food.Tomato:return item.chopped?"손질한 토마토":"토마토";
                case Food.Onion:return item.chopped?"손질한 양파":"양파";case Food.Lettuce:return item.chopped?"손질한 양상추":"양상추";
                case Food.Plate:return item.tomato>0?"접시 + 토마토":item.lettuce>0?"접시 + 양상추":"깨끗한 접시";
                case Food.DirtyPlate:return "더러운 접시";case Food.Extinguisher:return "소화기";case Food.Burnt:return "탄 음식";
                default:return Recipe(item.food);
            }
        }
        void OnGUI()
        {
            if(KitchenHarnessDriver.Requested)return;
            InitUI();var old=GUI.matrix;
            uiScale=Mathf.Min(Screen.width/1440f,Screen.height/900f);uiOrigin=new Vector2((Screen.width-1440*uiScale)/2,(Screen.height-900*uiScale)/2);
            GUI.matrix=Matrix4x4.TRS(uiOrigin,Quaternion.identity,Vector3.one*uiScale);
            if(!State.started) DrawMenu();else {DrawHUD();DrawEncounterHUD();if(State.ended||State.paused) DrawResults();else if(State.equipmentChoice) DrawEquipmentReward();else if(simulation.ChoosingSkill) DrawLevelUp();else if(showSkills&&!State.kiosk) DrawSkillCollection();else DrawSkillFeedback();}
            GUI.matrix=old;
        }
        void DrawMenu()
        {
            bool burger=selectedChapter==KitchenChapter.Burgers;
            GUI.color=Color.white;GUI.DrawTexture(new Rect(-1000,-1000,3440,2900),uiGradient);
            for(int i=0;i<7;i++)Disc(new Rect(830+(i%3)*220,40+(i/3)*260,130,130),new Color(1,1,1,.025f));
            Round(new Rect(70,48,196,36),new Color(1,1,1,.10f),10);Label(new Rect(85,48,170,36),"CO-OP KITCHEN",15,Paper,true);
            Hat(new Rect(68,116,130,105)); DrawChapterSelector();
            Label(new Rect(69,233,750,94),"LITTLE LADLE",68,Paper,true);
            Label(new Rect(74,328,640,48),"작은 주방, 커다란 한 끼.",30,new Color(.97f,.75f,.40f),true);
            Label(new Rect(76,386,640,33),burger?"패티를 굽고, 나만의 버거를 완성하세요!":"함께 썰고, 끓이고, 서빙하세요!",22,new Color(.80f,.88f,.80f));
            if(Button(new Rect(76,456,296,74),"혼자 요리하기",Orange))StartSolo(false);
            if(Button(new Rect(390,456,296,74),"둘이서 요리하기",Mint))StartSolo(true);
            DrawKioskSelector();
            Card(new Rect(76,574,610,192),new Color(.09f,.25f,.26f));
            Label(new Rect(98,590,145,30),"친구와 함께",22,Paper,true);
            if(Button(new Rect(370,590,134,34),"같은 네트워크",menuNetworkTab==0?Mint:new Color(.20f,.36f,.36f),true,15))menuNetworkTab=0;
            if(Button(new Rect(516,590,146,34),"온라인 코드",menuNetworkTab==1?Mint:new Color(.20f,.36f,.36f),true,15))menuNetworkTab=1;
            Label(new Rect(100,638,550,25),menuNetworkTab==0?"방을 만들거나 방장의 IP 주소로 참가하세요.":"방을 만들고 친구에게 참가 코드를 알려주세요.",16,new Color(.76f,.86f,.81f));
            if(Button(new Rect(100,685,154,52),"방 만들기",Orange,!connecting,20)){if(menuNetworkTab==0)StartHost();else ConnectRelay(true);}
            Round(new Rect(267,685,220,52),Paper,10);
            if(menuNetworkTab==0)address=GUI.TextField(new Rect(271,688,212,46),address,100,uiField);else joinCode=GUI.TextField(new Rect(271,688,212,46),joinCode,12,uiField);
            if(Button(new Rect(501,685,161,52),connecting?"연결 중…":"참가하기",Mint,!connecting,20)){if(menuNetworkTab==0)Join();else ConnectRelay(false);}
            Card(new Rect(794,141,556,609),Paper,28);
            if(kioskSelected)DrawKioskMenuPanel();else{
            Round(new Rect(827,170,182,37),new Color(.95f,.87f,.66f),10);Label(new Rect(827,170,182,37),burger?"CHAPTER 02":"CHAPTER 01",17,Ink,true,TextAnchor.MiddleCenter);
            Label(new Rect(828,225,465,52),burger?"버거 하우스 오픈!":"첫 번째 저녁 영업",35,Ink,true);
            Label(new Rect(830,286,440,30),"로그라이크  ·  1–4인  ·  스킬과 보스",20,new Color(.36f,.48f,.44f));
            FoodIcon(new Rect(841,354,118,109),burger?Food.Burger:Food.Salad);FoodIcon(new Rect(1010,354,118,109),burger?Food.Cheeseburger:Food.TomatoSoup);FoodIcon(new Rect(1173,354,118,109),burger?Food.DeluxeBurger:Food.OnionSoup);
            Label(new Rect(829,480,458,32),burger?"기본 = 빵 + 구운 패티  /  치즈버거 = +치즈":"샐러드  =  손질한 토마토 + 양상추",18,Ink,true);
            Label(new Rect(829,522,458,32),burger?"디럭스 = 치즈버거 + 손질한 토마토·양상추":"수프  =  같은 재료 3개 → 조리 → 접시",18,Ink,true);
            if(burger)Label(new Rect(829,555,470,24),"패티 6초 굽기 · 접시 위에 조립 · 14초 뒤 타요!",15,new Color(.39f,.48f,.42f));
            Round(new Rect(828,584,486,122),new Color(.91f,.93f,.85f),16);
            Label(new Rect(850,597,420,28),"오늘의 목표",18,Ink,true);
            for(int i=0;i<3;i++){Star(new Vector2(865+i*150,659),15,new Color(.98f,.65f,.20f));Label(new Rect(889+i*150,643,90,32),new[]{"200","500","900"}[i],23,Ink,true);}
            }
            Label(new Rect(78,806,1250,28),"WASD 이동    E 집기 / 놓기    F 길게 작업    SPACE 대시    Q 던지기    ·    게임패드 지원",18,new Color(.76f,.85f,.79f));
            if(!string.IsNullOrEmpty(failure)){Round(new Rect(76,845,1274,40),new Color(.67f,.20f,.18f,.94f),10);Label(new Rect(90,845,1240,40),failure,16,Color.white);}
        }
        void DrawHUD()
        {
            var s=State;
            // Keep the kitchen unobstructed: tickets above, scores and chef inventory below.
            Card(new Rect(26,20,249,69),new Color(.11f,.29f,.28f));Hat(new Rect(40,28,59,50));
            Label(new Rect(105,28,161,28),"LITTLE LADLE",18,Paper,true);
            Label(new Rect(106,56,157,21),State.chapter==KitchenChapter.Burgers?"02  버거 하우스":"01  첫 저녁 영업",13,new Color(.78f,.87f,.79f));
            bool hurry=s.kiosk&&s.remaining<30;float pulse=hurry?.92f+.08f*Mathf.Sin(Time.unscaledTime*8):1;
            Card(new Rect(1195,20,219,79),hurry?Red:Paper);
            if(s.kiosk){Disc(new Rect(1214,39,39,39),hurry?Color.white:Ink);Disc(new Rect(1218,43,31,31),hurry?Red:Paper);
            Line(new Vector2(1233,58),new Vector2(1233,48),3,hurry?Color.white:Ink);Line(new Vector2(1233,58),new Vector2(1242,62),3,hurry?Color.white:Ink);}
            else Hat(new Rect(1211,36,44,41));
            Label(new Rect(1260,29,147,57),!s.kiosk?"생존 중":((int)s.remaining/60).ToString("00")+":"+((int)s.remaining%60).ToString("00"),(int)(38*pulse),hurry?Color.white:Ink,true,TextAnchor.MiddleCenter);
            string mode=connection.StartsWith("ONLINE CODE:")?"참가 코드  "+relayCode:connection.StartsWith("HOST")?"친구를 기다리는 주방 · 최대 4인":s.chefs.FindAll(c=>c.active).Count+"명의 셰프가 요리 중";
            Label(new Rect(310,27,850,36),mode,18,new Color(.87f,.94f,.84f),true,TextAnchor.MiddleCenter);
            Label(new Rect(310,84,450,28),s.kiosk?"주문 들어갑니다!":"도전 "+s.run+" · 웨이브 "+s.wave+" · 위협 "+(simulation.ThreatTier+1),18,Paper,true);
            if(s.kiosk)DrawKioskTickets();else for(int i=0;i<s.orders.Count;i++)DrawTicket(s.orders[i],i);
            if(displayedScore!=s.score){if(displayedScore>=0)scorePop=Time.unscaledTime;displayedScore=s.score;}
            Card(new Rect(26,754,302,121),Paper);
            Disc(new Rect(43,772,47,47),new Color(1,.74f,.26f));Label(new Rect(43,772,47,47),"$",30,Ink,true,TextAnchor.MiddleCenter);
            float pop=1+Mathf.Max(0,1-(Time.unscaledTime-scorePop)*3)*.12f;
            Label(new Rect(101,762,197,63),s.score.ToString("0000"),(int)(41*pop),Ink,true);
            for(int i=0;i<3;i++)Star(new Vector2(65+i*36,851),12,s.score>=new[]{200,500,900}[i]?new Color(1,.66f,.18f):new Color(.78f,.80f,.72f));
            Label(new Rect(176,834,133,31),"연속 팁 ×"+Mathf.Min(5,s.combo+1),16,Mint,true);
            if (!s.kiosk)
            {
                Card(new Rect(26,650,302,88),new Color(.11f,.29f,.28f));
                Label(new Rect(42,658,270,29),"팀 체력  " + s.health + " / 100",22,s.health<=30?Red:Paper,true);
                Round(new Rect(42,701,270,16),Ink,6);
                if(s.health>0)Round(new Rect(42,701,270*s.health/100f,16),s.health<=30?Red:Mint,6);
            }
            DrawExperience();
            int activeIndex=0;
            for(int i=0;i<s.chefs.Count;i++)
            {
                var chef=s.chefs[i];if(!chef.active)continue;float x=350+activeIndex*238;activeIndex++;
                Card(new Rect(x,802,226,73),Paper);Round(new Rect(x,802,49,73),PlayerColors[i],14);
                Label(new Rect(x,802,49,73),"P"+(i+1),20,Ink,true,TextAnchor.MiddleCenter);
                if(!chef.held.Empty)FoodIcon(new Rect(x+55,814,43,43),chef.held.food,chef.held.chopped);
                Label(new Rect(x+(chef.held.Empty?63:103),811,chef.held.Empty?150:117,52),(chef.held.flameKissed?"불맛 · ":"")+FoodName(chef.held),15,Ink,true);
            }
            Label(new Rect(354,766,1005,28),"E 집기·놓기     F 길게 손질·설거지·소화     SPACE 대시     Q 던지기",17,Paper,true);
            Card(new Rect(1205,672,209,65),new Color(.11f,.29f,.28f));FoodIcon(new Rect(1219,684,43,39),Food.Plate);
            Label(new Rect(1270,680,133,26),"깨끗한 접시  "+s.cleanPlates,15,Paper,true);Label(new Rect(1270,707,133,20),"설거지 대기  "+s.dirtyPlates,13,new Color(.76f,.85f,.77f));
            DrawWorldHints();
            DrawContext();
            if(s.noticeTime>0 && !s.ended)
            {
                float slide=Mathf.Min(1,s.noticeTime*4);float y=690+(1-slide)*18;
                Card(new Rect(407,y,650,48),new Color(.11f,.29f,.28f,.96f));Label(new Rect(424,y,616,48),TranslateNotice(s.notice),19,Paper,true,TextAnchor.MiddleCenter);
            }
        }
        void DrawTicket(OrderState o,int index)
        {
            float x=27+index*260;float fraction=Mathf.Clamp01(o.remaining/o.total);
            bool onHold=simulation.NormalOrdersOnHold;bool urgent=!onHold&&o.remaining<20;float shake=urgent?Mathf.Sin(Time.unscaledTime*6+index)*2:0;
            var r=new Rect(x,126+shake,245,137);Card(r,Paper,14);
            Round(new Rect(x+14,r.y-9,onHold?114:64,24),onHold?Mint:urgent?Red:Orange,6);Label(new Rect(x+14,r.y-9,onHold?114:64,24),onHold?"보스전 · 시간 정지":"#"+(index+1).ToString("00"),onHold?12:14,Color.white,true,TextAnchor.MiddleCenter);
            FoodIcon(new Rect(x+13,r.y+28,66,61),o.dish);
            Label(new Rect(x+88,r.y+23,148,32),Recipe(o.dish),20,Ink,true);
            Label(new Rect(x+89,r.y+56,143,24),o.required>1?"단체 주문  "+o.completed+" / "+o.required:KitchenSimulation.IsBurger(o.dish)?"패티 굽기 → 조립":o.dish==Food.Salad?"썰어서 담기":"썰기 → 끓이기",13,new Color(.39f,.48f,.42f));
            if(KitchenSimulation.IsBurger(o.dish)) DrawBurgerIngredients(o.dish,x,r.y);
            else {
            Food f=o.dish==Food.OnionSoup?Food.Onion:Food.Tomato;
            FoodIcon(new Rect(x+17,r.y+82,28,28),f,true);
            FoodIcon(new Rect(x+52,r.y+82,28,28),o.dish==Food.Salad?Food.Lettuce:f,true);
            if(o.dish!=Food.Salad)FoodIcon(new Rect(x+87,r.y+82,28,28),f,true);
            }
            Label(new Rect(x+129,r.y+83,99,26),(o.required>1?"개당 +":"+")+o.reward+"점",14,Ink,true,TextAnchor.MiddleRight);
            Round(new Rect(x+12,r.y+120,221,8),new Color(.84f,.85f,.76f),4);
            if(fraction>0)Round(new Rect(x+12,r.y+120,221*fraction,8),onHold?Mint:urgent?Red:fraction<.5f?Orange:Mint,4);
        }
        void DrawWorldHints()
        {
            if(uiCamera==null)return;
            foreach(var station in State.stations)
            {
                if(station.kind==StationKind.KioskSupplies||station.kind==StationKind.DrinkSupplies)
                {
                    Vector3 screen=uiCamera.WorldToScreenPoint(station.position+Vector3.up*2.5f);
                    if(screen.z>0)
                    {
                        float sx=(screen.x-uiOrigin.x)/uiScale,sy=(Screen.height-screen.y-uiOrigin.y)/uiScale;
                        Card(new Rect(sx-116,sy-26,232,58),Mint,10);
                        Label(new Rect(sx-110,sy-24,220,27),station.kind==StationKind.DrinkSupplies?"음료대":"냉동 재료대",18,Color.white,true,TextAnchor.MiddleCenter);
                        Label(new Rect(sx-110,sy+3,220,25),KioskRecipeName(KitchenSimulation.SuppliesFor(station.kind)[station.portions%2])+"  ·  F 변경 / E 집기",13,Color.white,false,TextAnchor.MiddleCenter);
                    }
                    continue;
                }
                string hint="";Color color=Paper;
                if(State.kiosk)hint=KioskStationHint(station);
                if(station.fire>0){hint="불이 났어요!";color=Red;}
                else if(station.kind==StationKind.Grill&&!station.item.Empty)
                {hint=station.item.food==Food.Burnt?"탄 패티":station.item.food==Food.CookedPatty?"다 익었어요!":"굽는 중 "+Mathf.Min(100,Mathf.FloorToInt(station.heat/(KitchenSimulation.GrillCookSeconds/simulation.CookingSpeed)*100))+"%";color=station.item.food==Food.CookedPatty?Mint:Paper;}
                else if(station.kind==StationKind.Stove&&station.portions>0)
                {hint=station.soup==Food.Burnt?"탄 냄비":station.heat>=KitchenSimulation.CookSeconds/simulation.CookingSpeed?"수프 완성!":station.portions+" / 3";color=station.heat>=KitchenSimulation.CookSeconds/simulation.CookingSpeed?Mint:Paper;}
                else if(station.kind==StationKind.Serve)hint="서빙";
                else if(station.kind==StationKind.Counter&&State.chapter==KitchenChapter.Burgers)hint="조립대";
                if(simulation.StationOffline(State.stations.IndexOf(station))){hint="정전 중";color=Red;}
                if(string.IsNullOrEmpty(hint))continue;
                Vector3 p=uiCamera.WorldToScreenPoint(station.position+Vector3.up*(station.kind==StationKind.Counter?2.45f:2.0f));if(p.z<0)continue;
                float x=(p.x-uiOrigin.x)/uiScale,y=(Screen.height-p.y-uiOrigin.y)/uiScale;
                float width=hint.Length>5?112:94;
                Card(new Rect(x-width/2,y-14,width,29),color,9);
                Label(new Rect(x-width/2,y-14,width,29),hint,14,color==Paper?Ink:Color.white,true,TextAnchor.MiddleCenter);
            }
        }
        void DrawContext()
        {
            if(State.chefs.Count==0)return;var chef=State.chefs[0];if(chef.target<0||chef.target>=State.stations.Count)return;
            var station=State.stations[chef.target];string action="E  집기 / 내려놓기";
            if(station.kind==StationKind.KioskSupplies||station.kind==StationKind.DrinkSupplies)action="F 길게: 품목 변경 · E: 가져오기 / 같은 종류 반납";
            else if(station.kind==StationKind.Fryer)action="5초 튀기기 · 사이드는 빈 접시로 / 새우패티는 빈손으로 가져가기";
            else if(station.kind==StationKind.Shrimp)action="E  생새우패티 가져오기";
            else if(station.kind==StationKind.Sauce)action="E  불고기소스 가져오기";
            else if(station.fire>0)action="F 길게  소화기로 진압";
            else if(station.kind==StationKind.Board)action=station.item.Raw&&!station.item.chopped?"F 길게  재료 손질":"E  재료 놓기 / 가져가기";
            else if(station.kind==StationKind.Sink)action="F 길게  설거지";
            else if(station.kind==StationKind.Grill)action=station.item.food==Food.CookedPatty?"E  구운 패티 가져가기":station.item.Empty?"E  생패티를 그릴에 올리기":(KitchenSimulation.GrillCookSeconds/simulation.CookingSpeed).ToString("0.0")+"초 조리 · "+(KitchenSimulation.GrillBurnSeconds+simulation.BurnGrace).ToString("0")+"초 뒤 타요!";
            else if(station.kind==StationKind.Buns)action="E  버거 빵 가져오기";
            else if(station.kind==StationKind.Meat)action="E  생패티 가져오기";
            else if(station.kind==StationKind.Cheese)action="E  치즈 가져오기";
            else if(station.kind==StationKind.Counter&&State.chapter==KitchenChapter.Burgers)action="E  접시에 빵·패티·토핑 조립";
            else if(station.kind==StationKind.Stove)action=station.heat>=KitchenSimulation.CookSeconds/simulation.CookingSpeed?"E  접시에 수프 담기":"E  손질한 재료 3개 넣기";
            else if(station.kind==StationKind.Serve)action="E  완성한 요리 서빙";
            else if(station.kind==StationKind.Trash)action="E  들고 있는 물건 버리기";
            Card(new Rect(505,623,431,45),Paper);Label(new Rect(520,623,401,45),action,18,Ink,true,TextAnchor.MiddleCenter);
        }
        string TranslateNotice(string value)
        {
            if(value.StartsWith("Burger service open"))return "패티 6초 굽기 → 접시에 빵·패티·토핑 조립 → 서빙!";
            if(value.StartsWith("Patty ready"))return "패티가 다 익었어요! 타기 전에 가져가세요.";
            if(value.StartsWith("Grill a raw"))return "패티 "+(KitchenSimulation.GrillCookSeconds/simulation.CookingSpeed).ToString("0.0")+"초 조리 · "+(KitchenSimulation.GrillBurnSeconds+simulation.BurnGrace).ToString("0")+"초 뒤 타요!";
            if(value.StartsWith("Service is open"))return "영업 시작! 맛있는 한 끼를 부탁해요.";
            if(value.StartsWith("Order served"))return "서빙 완료!  "+value.Substring(value.IndexOf('+'))+"점";
            if(value.StartsWith("Order expired"))return "주문 시간이 끝났어요.  −30점";
            if(value.StartsWith("FIRE!"))return "불이 났어요! 소화기를 들고 F를 길게 누르세요.";
            if(value.StartsWith("Fire out"))return State.chapter==KitchenChapter.Burgers?"진압 완료! 빈손으로 탄 패티를 꺼내 버리세요.":"진압 완료! 빈손으로 탄 냄비를 비워주세요.";
            if(value.StartsWith("Clean plate"))return "깨끗한 접시가 준비됐어요!";
            if(value.StartsWith("Chef joined"))return "새로운 셰프가 주방에 합류했어요!";
            if(value.StartsWith("Pot cleaned"))return "냄비가 깨끗해졌어요. 다시 요리해요!";
            if(value.StartsWith("No matching"))return "이 요리를 기다리는 주문이 없어요.";
            if(value.StartsWith("Serve a plated"))return "접시에 완성한 요리를 담아 가져오세요.";
            if(value.StartsWith("3 matching"))return "같은 재료 3개 · 9초 조리 · 접시로 가져가기";
            if(value.StartsWith("Hold work"))return "빈손으로 F를 길게 눌러 설거지하세요.";
            if(value.StartsWith("Extinguish"))return "먼저 소화기로 불을 꺼주세요!";
            if(value.StartsWith("Discarded"))return "정리 끝! 다음 요리를 준비해요.";
            return value;
        }
        void DrawResults()
        {
            Flat(new Rect(-1000,-1000,3440,2900),new Color(.04f,.13f,.14f,.82f));
            Card(new Rect(395,174,650,556),Paper,30);Hat(new Rect(654,112,133,116));
            Label(new Rect(420,241,600,56),State.ended?(State.kiosk?"오늘 영업이 끝났습니다!":"주방이 무너졌습니다!"):"잠깐, 쉬어 갈까요?",37,Ink,true,TextAnchor.MiddleCenter);
            if(State.ended)
            {
                for(int i=0;i<3;i++)Star(new Vector2(626+i*94,347),32,simulation.Stars>i?new Color(1,.66f,.18f):new Color(.79f,.81f,.72f));
                Label(new Rect(430,394,580,66),State.score.ToString("0000")+" 점",49,Ink,true,TextAnchor.MiddleCenter);
                Label(new Rect(430,475,580,37),"도전 "+State.run+" · 서빙 "+State.delivered+" · 놓친 주문 "+State.missed,22,new Color(.38f,.48f,.42f),false,TextAnchor.MiddleCenter);
            }
            else Label(new Rect(435,357,570,72),"준비되면 다시 주방으로 돌아가요.",24,Ink,false,TextAnchor.MiddleCenter);
            if(State.ended)Label(new Rect(430,516,580,25),Authority?"재시작 버튼을 누르면 새 도전이 시작됩니다.":"방장이 재시작 버튼을 누르기를 기다리고 있습니다.",16,Ink,false,TextAnchor.MiddleCenter);
            if(Authority&&Button(new Rect(452,554,536,61),State.ended?"재시작":"계속 요리하기",Orange))
            {
                if(State.ended)RestartTeamRun();else State.paused=false;
            }
            if(Button(new Rect(452,638,536,52),"메뉴로 돌아가기",Mint)){Disconnect();simulation.Reset(selectedChapter);}
        }
        void LateUpdate()
        {
            if(uiCamera==null)uiCamera=Camera.main;
            if(uiCamera!=null&&!cameraStyled)
            {
                uiCamera.orthographicSize=7.0f;
                uiCamera.transform.position=new Vector3(0,14,-10.8f);uiCamera.transform.LookAt(new Vector3(0,0,1.6f));
                uiCamera.backgroundColor=new Color(.16f,.31f,.30f);cameraStyled=true;
            }
        }
        void DisposeUI()
        {
            if(uiRound!=null)Destroy(uiRound);if(uiCircle!=null)Destroy(uiCircle);if(uiGradient!=null)Destroy(uiGradient);if(uiFont!=null)Destroy(uiFont);
        }
    }
}






