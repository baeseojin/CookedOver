using System;
using UnityEngine;
namespace CookedOver
{
 public sealed partial class KitchenGame
 {
  bool kioskSelected;KitchenKioskBridge kioskBridge;int kioskPage;string kioskEmail="",kioskPassword="";
  void PrepareKioskKitchen(){if(kioskSelected){selectedChapter=KitchenChapter.Burgers;simulation.EnableKioskMode();}}
  void StartKioskBridge(){if(!State.kiosk)return;if(kioskBridge==null)kioskBridge=gameObject.AddComponent<KitchenKioskBridge>();kioskBridge.Begin(this,kioskEmail,kioskPassword);kioskPassword="";}
  void DrawKioskMenuPanel()
  {
   Label(new Rect(828,172,486,46),"키오스크 주문 주방",32,Ink,true);
   Label(new Rect(828,224,486,26),"WPF 키오스크와 같은 기기 계정으로 연결합니다.",17,Ink);
   Label(new Rect(828,268,486,26),"키오스크 계정 이메일",17,Ink,true);
   kioskEmail=GUI.TextField(new Rect(828,302,486,48),kioskEmail,160,uiField);
   Label(new Rect(828,365,486,26),"비밀번호",17,Ink,true);
   kioskPassword=GUI.PasswordField(new Rect(828,399,486,48),kioskPassword,'●',256,uiField);
   Label(new Rect(828,458,486,24),"환경변수가 설정되어 있으면 입력 없이 연결됩니다.",15,Ink);
   Label(new Rect(828,503,486,28),"불고기 · 치즈 · 새우 · 더블버거",22,Ink,true);
   Label(new Rect(828,545,486,26),"감자튀김 · 치즈스틱 · 콜라 · 사이다",20,Ink);
   Label(new Rect(828,591,486,26),"왼쪽에서 혼자 / 둘이서 요리하기를 누르세요.",18,Mint,true);
   Label(new Rect(828,632,486,24),"세트 구성과 수량이 실제 주문 그대로 들어옵니다.",16,Ink);
   Label(new Rect(828,669,486,24),"4분 영업 · 스킬 없이 조리 · 미완료 주문 유지",17,Ink,true);
  }
  void DrawKioskSelector()
  {
   if(Button(new Rect(76,538,610,29),kioskSelected?"키오스크 주문 연동 켜짐 · 결제된 주문으로 요리":"키오스크 주문 연동 켜기",kioskSelected?Mint:new Color(.19f,.34f,.34f),!connecting,16))
   {kioskSelected=!kioskSelected;if(kioskSelected)SelectChapter(KitchenChapter.Burgers);}
  }
  public static string KioskRecipeName(Food food)
  {
   switch(food)
   {
    case Food.BulgogiBurger:return "불고기버거";case Food.ShrimpBurger:return "새우버거";case Food.DoubleBurger:return "더블버거";
    case Food.RawShrimp:return "생새우패티";case Food.CookedShrimp:return "튀긴 새우패티";case Food.BulgogiSauce:return "불고기소스";
    case Food.RawFries:return "냉동 감자";case Food.RawCheeseSticks:return "냉동 치즈스틱";
    case Food.Fries:return "감자튀김";case Food.CheeseSticks:return "치즈스틱";case Food.Cola:return "콜라";case Food.Sprite:return "사이다";
    default:return null;
   }
  }
  public static Food[] KioskIngredients(Food food)
  {
   switch(food)
   {
    case Food.BulgogiBurger:return new[]{Food.Bun,Food.CookedPatty,Food.BulgogiSauce,Food.Lettuce};
    case Food.ShrimpBurger:return new[]{Food.Bun,Food.CookedShrimp,Food.Lettuce};
    case Food.DoubleBurger:return new[]{Food.Bun,Food.CookedPatty,Food.CookedPatty,Food.Cheese};
    case Food.Fries:return new[]{Food.RawFries,Food.Plate};case Food.CheeseSticks:return new[]{Food.RawCheeseSticks,Food.Plate};
    case Food.Cola:return new[]{Food.Cola};case Food.Sprite:return new[]{Food.Sprite};
    default:return BurgerIngredients(food);
   }
  }
  static string KioskInstructions(Food f)
  {
   if(f==Food.Fries||f==Food.CheeseSticks)return "5초 튀기기 → 빈 접시에 담기";
   if(f==Food.Cola||f==Food.Sprite)return "음료대에서 선택 → 서빙";
   if(f==Food.ShrimpBurger)return "새우패티 튀기기 + 양상추 손질";
   if(f==Food.BulgogiBurger)return "패티 굽기 + 소스 + 손질한 양상추";
   if(f==Food.DoubleBurger)return "구운 패티 2장 + 빵 + 치즈";
   return f==Food.None?"지원할 조리법 연결이 필요합니다":"패티 굽기 → 빵·치즈 조립";
  }
  void DrawKioskTickets()
  {
   int pages=Mathf.Max(1,(State.orders.Count+3)/4);kioskPage=Mathf.Clamp(kioskPage,0,pages-1);
   string status=Authority?(kioskBridge!=null?kioskBridge.Status:"키오스크 연결 준비 중"):"방장이 키오스크 주문을 수신합니다";
   Label(new Rect(310,60,850,26),status,15,Paper,false,TextAnchor.MiddleCenter);
   if(Button(new Rect(1070,90,60,30),"◀",Mint,kioskPage>0,17))kioskPage--;
   Label(new Rect(1138,89,90,30),(kioskPage+1)+" / "+pages,16,Paper,true,TextAnchor.MiddleCenter);
   if(Button(new Rect(1235,90,60,30),"▶",Mint,kioskPage<pages-1,17))kioskPage++;
   if(State.orders.Count==0)Label(new Rect(310,145,800,60),"키오스크에서 결제한 주문을 기다립니다",27,Paper,true,TextAnchor.MiddleCenter);
   for(int i=kioskPage*4;i<Mathf.Min(State.orders.Count,kioskPage*4+4);i++)
   {
    var o=State.orders[i];float x=27+(i%4)*350;var r=new Rect(x,130,332,144);Card(r,Paper,14);
    Label(new Rect(x+12,134,305,22),"주문 #"+o.externalOrderId+(o.pendingAck?" · 반영 중":" · "+o.completed+" / "+o.required),15,Mint,true);
    FoodIcon(new Rect(x+12,165,64,65),o.dish);
    Label(new Rect(x+85,163,235,31),o.externalName+" ×"+(o.required-o.completed),21,Ink,true);
    Label(new Rect(x+85,199,235,24),KioskInstructions(o.dish),12,Ink);
    var ingredients=KioskIngredients(o.dish);
    for(int n=0;n<ingredients.Length;n++)FoodIcon(new Rect(x+90+n*33,232,28,27),ingredients[n],ingredients[n]==Food.Lettuce);
   }
  }
  static string KioskStationHint(StationState s)
  {
   switch(s.kind)
   {
    case StationKind.Shrimp:return "새우패티";case StationKind.Sauce:return "불고기소스";
    case StationKind.KioskSupplies:case StationKind.DrinkSupplies:return KioskRecipeName(KitchenSimulation.SuppliesFor(s.kind)[s.portions%2])+" · F 변경";
    case StationKind.Fryer:return s.item.Empty?"튀김기":s.heat>=5?"완성 · 가져가기":"튀기는 중";
    default:return "";
   }
  }
  bool DrawKioskFoodIcon(Rect r,Food f)
  {
   if(f==Food.Cola||f==Food.Sprite)
   {Round(new Rect(r.x+r.width*.22f,r.y+r.height*.2f,r.width*.56f,r.height*.70f),f==Food.Cola?Red:Mint,5);Line(new Vector2(r.center.x,r.y+r.height*.5f),new Vector2(r.center.x+r.width*.12f,r.y),r.width*.055f,Paper);Label(r,f==Food.Cola?"C":"S",(int)(r.width*.32f),Paper,true,TextAnchor.MiddleCenter);return true;}
   if(f==Food.Fries||f==Food.RawFries||f==Food.CheeseSticks||f==Food.RawCheeseSticks)
   {for(int i=0;i<4;i++)Round(new Rect(r.x+r.width*(.12f+i*.19f),r.y+r.height*(.16f+(i%2)*.08f),r.width*.13f,r.height*.67f),f==Food.RawFries||f==Food.RawCheeseSticks?Paper:Orange,4);return true;}
   if(f==Food.RawShrimp||f==Food.CookedShrimp)
   {Disc(new Rect(r.x+r.width*.08f,r.y+r.height*.25f,r.width*.84f,r.height*.55f),f==Food.RawShrimp?new Color(1,.64f,.60f):new Color(.91f,.60f,.20f));return true;}
   if(f==Food.BulgogiSauce)
   {Round(new Rect(r.x+r.width*.25f,r.y+r.height*.18f,r.width*.50f,r.height*.7f),new Color(.39f,.20f,.07f),7);return true;}
   return false;
  }
 }
}
