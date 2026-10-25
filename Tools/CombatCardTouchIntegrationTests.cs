using System;
using System.Collections.Generic;
using UnityEngine;

// Compile the production LumiaTouchScroll partial directly. The doubles below
// supply only the Unity host boundary (input snapshots, transforms and control
// capture); gesture ownership, suppression and nested-area registration remain
// the production implementation. This is not a physical-device browser test.
namespace UnityEngine
{
    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }
    public enum EventType { MouseDown, MouseUp, MouseDrag, Repaint, Layout, ScrollWheel, TouchDown, TouchMove, TouchUp, Used }
    public enum PointerType { Mouse, Touch }
    public struct Touch { public int fingerId; public Vector2 position; public TouchPhase phase; }
    public struct Vector2
    {
        public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero => new Vector2(0,0);
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
    }
    public struct Rect
    {
        public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public Vector2 position=>new Vector2(x,y); public float xMax=>x+width; public float yMax=>y+height;
        public bool Contains(Vector2 p)=>p.x>=x&&p.y>=y&&p.x<xMax&&p.y<yMax;
        public static bool operator ==(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;
        public static bool operator !=(Rect a,Rect b)=>!(a==b);
        public override bool Equals(object o)=>o is Rect&&(Rect)o==this;
        public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode()^width.GetHashCode()^height.GetHashCode();
    }
    public struct Matrix4x4 { public float scale,ox,oy; public static Matrix4x4 identity=>new Matrix4x4{scale=1}; }
    public static class Mathf
    {
        public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(v,max));
        public static float Round(float value)=>(float)Math.Round(value,MidpointRounding.ToEven);
    }
    public static class Screen { public static int width=1280,height=720;public static bool fullScreen; }
    public static class Time { public static int frameCount; }
    public static class Input { public static Touch[] touches=new Touch[0];public static int touchCount=>touches.Length;public static Touch GetTouch(int n)=>touches[n]; }
    public sealed class Event
    {
        public static Event current; public EventType rawType,type;public PointerType pointerType;
        public Vector2 screenPoint;
        public Vector2 mousePosition=>GUI.FromScreen(screenPoint);
        public bool isMouse=>type==EventType.MouseDown||type==EventType.MouseDrag||type==EventType.MouseUp;
        public void Use(){type=EventType.Used;}
    }
    public sealed class RectOffset { public int left,top; }
    public sealed class GUIStyle { public float fixedWidth=12,fixedHeight=12;public RectOffset margin=new RectOffset(); }
    public sealed class GUISkin { public GUIStyle verticalScrollbar=new GUIStyle(),horizontalScrollbar=new GUIStyle(); }
    public static class GUIUtility
    {
        public static int hotControl;
        public static Vector2 GUIToScreenPoint(Vector2 p)=>GUI.ToScreen(p);
    }
    public static class GUI
    {
        public static Matrix4x4 matrix=Matrix4x4.identity;
        public static bool enabled=true;
        public static GUISkin skin=new GUISkin();
        static readonly Stack<Vector2> origins=new Stack<Vector2>();
        static Vector2 origin;
        // Unity's GUIToScreenPoint host boundary adds nested GUIClip origins
        // outside GUI.matrix. This differs from the painted content transform;
        // the measured 632x355 browser trace establishes that distinction.
        public static Vector2 ToScreen(Vector2 p)=>new Vector2(matrix.ox+origin.x+p.x*matrix.scale,matrix.oy+origin.y+p.y*matrix.scale);
        public static Vector2 ToPaintedScreen(Vector2 p)=>new Vector2(matrix.ox+(p.x+origin.x)*matrix.scale,matrix.oy+(p.y+origin.y)*matrix.scale);
        public static Vector2 FromScreen(Vector2 p)=>new Vector2((p.x-matrix.ox)/matrix.scale-origin.x,(p.y-matrix.oy)/matrix.scale-origin.y);
        public static void Reset(){origins.Clear();origin=Vector2.zero;matrix=Matrix4x4.identity;enabled=true;GUIUtility.hotControl=0;}
        public static Vector2 BeginScrollView(Rect viewport,Vector2 position,Rect view,bool h,bool v)
        {
            origins.Push(origin);origin+=viewport.position+new Vector2(Mathf.Round(-position.x-view.x),Mathf.Round(-position.y-view.y));return position;
        }
        public static void EndScrollView(bool handle){origin=origins.Pop();}
        public static void BeginGroup(Rect rect){origins.Push(origin);origin+=rect.position;}
        public static void EndGroup(){origin=origins.Pop();}
    }
}

namespace Lumia
{
    public sealed class TestCombatState { public int turn=1; }
    public sealed class TestRunState { public int stage=1;public TestCombatState combat=new TestCombatState(); }
    public sealed class GameEngine { public TestRunState State=new TestRunState(); }
    public sealed partial class LumiaGame
    {
        bool StoryVisible,TutorialActive,CombatInformationOpen,TacticalModuleChoiceOpen,AdvancedCardChoiceOpen;
        bool tutorialOffer,modeSelecting,runMenu,endlessConfirm,fieldInfo,enemyLoadout,help,settings,inventory,catalog,research,lobby,fieldEnemy,BrowserAccountFrozen;
        string inspectCard="",inspectTrait="",inspectGear="",inspectBoss="",craftPending="",mapIntel="";
        int currentLayer,topLayer;
        GameEngine Engine=new GameEngine();
        Vector2 hand,body,deep;
        public Rect HandViewport=new Rect(24,458,1004,262),HandView=new Rect(0,0,1600,245);
        public Rect BodyViewport=new Rect(11,108,166,106),BodyView=new Rect(0,0,151,400);
        public Rect DeepViewport=new Rect(30,50,160,100),DeepView=new Rect(0,0,148,400);
        public bool DoubleNested;
        public Vector2 ApiBodyOrigin,PaintedBodyOrigin,PaintedDeepOrigin;
        public int cardUses;
        public float BodyY=>body.y; public float HandX=>hand.x;public float DeepY=>deep.y;
        public TouchScrollArea AreaAtDepth(int depth)=>touchScrollAreas.FindLast(area=>area.depth==depth);
        public void SetPositions(Vector2 handPosition,Vector2 bodyPosition){hand=handPosition;body=bodyPosition;}
        public bool Tracking=>touchScrollGesture.IsTracking;public bool Dragging=>touchScrollGesture.IsDragging;
        public bool Suppressed=>Event.current.type==EventType.Used;
        public void UpdateInput()=>UpdateTouchScrollInput();
        public void Freeze(){BrowserAccountFrozen=true;}
        public void ChangeTurn(){Engine.State.combat.turn++;}
        public void OpenModal(){inspectCard="test";}
        public void LoseFocus(){OnApplicationFocus(false);}
        public void CancelForResize(){CancelNativeTouchScroll(true);}
        public void Frame(EventType type,float x,float y,bool playable=true,float scale=1,bool eventTouch=false,float offsetX=0,float offsetY=0)
        {
            Event.current=new Event {rawType=type,type=type,screenPoint=new Vector2(x,y),pointerType=Input.touchCount>0||eventTouch?PointerType.Touch:PointerType.Mouse};
            BeginTouchScrollGUI();
            GUI.matrix=new Matrix4x4 {scale=scale,ox=offsetX,oy=offsetY};currentLayer=topLayer=0;GUI.enabled=true;
            hand=BeginTouchScrollView("combat-hand",HandViewport,hand,HandView);
            ApiBodyOrigin=GUIUtility.GUIToScreenPoint(BodyViewport.position)-touchGuiOrigin;
            PaintedBodyOrigin=GUI.ToPaintedScreen(BodyViewport.position);
            body=BeginTouchScrollView("body/test",BodyViewport,body,BodyView,false,true);
            if(DoubleNested)
            {
                PaintedDeepOrigin=GUI.ToPaintedScreen(DeepViewport.position);
                deep=BeginTouchScrollView("deep/test",DeepViewport,deep,DeepView,false,true);
                EndTouchScrollView();
            }
            EndTouchScrollView();
            if(playable) CardButton(new Rect(0,0,188,245));
            EndTouchScrollView();EndTouchScrollGUI();
        }
        void CardButton(Rect r)
        {
            int previous=GUIUtility.hotControl;EventType raw=Event.current.rawType;
            // Unity GUI.DoControl handles synthesized mouse events. Raw Touch
            // events remain available to the production custom scroll wrapper.
            // Reference: UnityCsReference Modules/IMGUI/GUI.cs, DoControl.
            if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)&&GUIUtility.hotControl==0)
            {GUIUtility.hotControl=101;Event.current.Use();}
            else if(Event.current.type==EventType.MouseUp&&GUIUtility.hotControl==101)
            {GUIUtility.hotControl=0;if(r.Contains(Event.current.mousePosition))cardUses++;Event.current.Use();}
            ObserveTouchScrollButton(previous,raw);
        }
    }
}

public static class CombatCardTouchIntegrationTests
{
    static int assertions,scenarios;
    static readonly List<string> failures=new List<string>();
    static void Check(bool condition,string message){assertions++;if(!condition)throw new Exception(message);}
    static void Near(float actual,float expected,string message)=>Check(Math.Abs(actual-expected)<.01f,message+" actual="+actual+" expected="+expected);
    static Lumia.LumiaGame Fresh(float scale=1)
    {
        GUI.Reset();Input.touches=new Touch[0];Screen.width=1280;Screen.height=720;Time.frameCount=0;
        var game=new Lumia.LumiaGame();game.Frame(EventType.Repaint,0,0,true,scale);return game;
    }
    static void Touch(TouchPhase phase,float x,float y,int count=1)
    {
        Time.frameCount++;Input.touches=new Touch[count];
        for(int i=0;i<count;i++)Input.touches[i]=new Touch {fingerId=i+1,position=new Vector2(x,Screen.height-y),phase=phase};
    }
    static void Scenario(Action test)
    {try{test();}catch(Exception error){failures.Add(test.Method.Name);Console.WriteLine("CASE_FAIL "+test.Method.Name+": "+error.Message);}scenarios++;}
    public static int Main()
    {
        try
        {
            Scenario(EventOnlyTouchSurvivesRepaint);Scenario(RawTouchEventsScroll);Scenario(DelayedNativePollAdoptsEventGesture);Scenario(NativePollAfterPlainMouseDown);
            Scenario(NestedVerticalSwipe);Scenario(HorizontalHandSwipe);Scenario(ShortTapUsesExactlyOnce);
            Scenario(UnplayableCardStillScrolls);Scenario(NativePollingFallback);Scenario(ScaledCardSwipe);
            Scenario(MultiTouchCancels);Scenario(ContextChangeCancels);Scenario(FastReleaseSuppressesCard);
            Scenario(NativeScrollbarCaptureIsPreserved);Scenario(FocusLossCancels);Scenario(EventOnlyTapUsesExactlyOnce);Scenario(EventOnlyAfterCanceledNative);Scenario(DesktopClickAfterEarlierResizeCancel);
            Scenario(MeasuredMobileNestedBounds);Scenario(MeasuredMobileNestedSwipe);Scenario(ScrolledParentFractionalRounding);Scenario(DoubleNestedMobileBoundsAndSwipe);Scenario(NonzeroViewRectOrigins);
            string failedJson=failures.Count==0?"[]":"[\""+string.Join("\",\"",failures.ToArray())+"\"]";
            Console.WriteLine("COMBAT_CARD_TOUCH_RESULT {\"result\":\""+(failures.Count==0?"PASS":"FAIL")+"\",\"assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"failedScenarios\":"+failedJson+"}");return failures.Count==0?0:1;
        }
        catch(Exception error){Console.Error.WriteLine(error.Message);Console.WriteLine("COMBAT_CARD_TOUCH_RESULT {\"result\":\"FAIL\",\"assertions\":"+assertions+",\"scenarios\":"+scenarios+"}");return 1;}
    }
    static void Down(Lumia.LumiaGame g,float x=100,float y=620,bool playable=true,float scale=1)
    {Touch(TouchPhase.Began,x,y);g.UpdateInput();g.Frame(EventType.MouseDown,x,y,playable,scale);}
    static void Move(Lumia.LumiaGame g,float x,float y,bool playable=true,float scale=1)
    {Touch(TouchPhase.Moved,x,y);g.UpdateInput();g.Frame(EventType.MouseDrag,x,y,playable,scale);g.Frame(EventType.Repaint,x,y,playable,scale);}
    static void Up(Lumia.LumiaGame g,float x,float y,bool playable=true,float scale=1)
    {Touch(TouchPhase.Ended,x,y);g.UpdateInput();g.Frame(EventType.MouseUp,x,y,playable,scale);g.Frame(EventType.Repaint,x,y,playable,scale);}
    static void NestedVerticalSwipe()
    {var g=Fresh();Down(g);Check(g.Tracking,"card press tracks a native finger");Move(g,100,570);Near(g.BodyY,50,"vertical swipe scrolls nested description");Near(g.HandX,0,"vertical description swipe leaves hand stable");Check(GUIUtility.hotControl==0,"drag releases card capture");Up(g,100,570);Check(g.cardUses==0,"swipe release never uses card");}
    static void HorizontalHandSwipe()
    {var g=Fresh();Down(g);Move(g,40,620);Near(g.HandX,60,"horizontal swipe scrolls outer hand");Near(g.BodyY,0,"horizontal hand movement leaves body stable");Up(g,40,620);Check(g.cardUses==0,"hand swipe never uses card");}
    static void ShortTapUsesExactlyOnce()
    {var g=Fresh();Down(g);Up(g,102,617);Check(g.cardUses==1,"short native tap uses card exactly once");Near(g.BodyY,0,"tap does not scroll body");Check(GUIUtility.hotControl==0,"tap releases capture");}
    static void UnplayableCardStillScrolls()
    {var g=Fresh();Down(g,100,620,false);Move(g,100,580,false);Near(g.BodyY,40,"unplayable card explanation remains scrollable");Up(g,100,580,false);Check(g.cardUses==0,"unplayable card never activates");}
    static void NativePollingFallback()
    {var g=Fresh();Down(g);Touch(TouchPhase.Moved,100,580);g.UpdateInput();g.Frame(EventType.Repaint,0,0);Near(g.BodyY,40,"repaint fallback processes native movement with no queued drag");Touch(TouchPhase.Ended,100,570);g.UpdateInput();g.Frame(EventType.Repaint,0,0);Near(g.BodyY,50,"repaint fallback includes final touch position");Check(g.cardUses==0,"polling fallback does not use card");}
    static void ScaledCardSwipe()
    {var g=Fresh(.5f);Down(g,50,310,true,.5f);Move(g,50,285,true,.5f);Near(g.BodyY,50,"physical screen distance scales to card content units");Up(g,50,285,true,.5f);Check(g.cardUses==0,"scaled swipe does not use card");}
    static void MultiTouchCancels()
    {var g=Fresh();Down(g);Touch(TouchPhase.Moved,100,580,2);g.UpdateInput();g.Frame(EventType.MouseDrag,100,580);Check(!g.Tracking&&!g.Dragging,"second finger cancels active gesture");Up(g,100,580);Check(g.cardUses==0,"multitouch release does not activate card");}
    static void ContextChangeCancels()
    {var g=Fresh();Down(g);g.ChangeTurn();Move(g,100,580);Check(!g.Tracking&&!g.Dragging,"turn transition cancels previous card gesture");Up(g,100,580);Check(g.cardUses==0,"old turn release never uses card");}
    static void FastReleaseSuppressesCard()
    {var g=Fresh();Down(g);Up(g,100,580);Near(g.BodyY,40,"final release can acquire and move nested body");Check(g.cardUses==0,"fast swipe release does not become card tap");}
    static void NativeScrollbarCaptureIsPreserved()
    {var g=Fresh();Touch(TouchPhase.Began,100,620);g.UpdateInput();GUIUtility.hotControl=999;g.Frame(EventType.Layout,100,620);Check(!g.Tracking,"foreign native control ends pending custom gesture");Check(GUIUtility.hotControl==999,"foreign scrollbar control remains captured");}
    static void EventOnlyTouchSurvivesRepaint()
    {
        var g=Fresh();Time.frameCount++;g.Frame(EventType.MouseDown,100,620,eventTouch:true);
        Check(g.Tracking,"event-only native touch begins");g.Frame(EventType.Repaint,100,620,eventTouch:true);
        Check(g.Tracking&&!g.Dragging,"no Input.touch snapshot must not end an event-only finger on repaint");
        Time.frameCount++;g.Frame(EventType.MouseDrag,100,580,eventTouch:true);g.Frame(EventType.Repaint,100,580,eventTouch:true);
        Near(g.BodyY,40,"event-only drag scrolls actual registered body");
        Time.frameCount++;g.Frame(EventType.MouseUp,100,580,eventTouch:true);g.Frame(EventType.Repaint,100,580,eventTouch:true);
        Check(g.cardUses==0,"event-only swipe does not activate overlapping card");
    }
    static void RawTouchEventsScroll()
    {
        var g=Fresh();Touch(TouchPhase.Began,100,620);g.UpdateInput();g.Frame(EventType.TouchDown,100,620);
        Check(g.Tracking&&GUIUtility.hotControl==0,"raw TouchDown tracks scrolling without synthesizing a card capture");
        Touch(TouchPhase.Moved,100,580);g.UpdateInput();g.Frame(EventType.TouchMove,100,580);g.Frame(EventType.Repaint,100,580);
        Near(g.BodyY,40,"raw touch move scrolls card description");
        Touch(TouchPhase.Ended,100,580);g.UpdateInput();g.Frame(EventType.TouchUp,100,580);Check(g.cardUses==0,"raw touch swipe does not use card");
    }
    static void DelayedNativePollAdoptsEventGesture()
    {
        var g=Fresh();Time.frameCount++;g.Frame(EventType.MouseDown,100,620,eventTouch:true);
        Touch(TouchPhase.Began,100,620);Input.touches[0]=new Touch {fingerId=42,position=new Vector2(100,Screen.height-620),phase=TouchPhase.Began};
        g.UpdateInput();g.Frame(EventType.Repaint,100,620);
        Check(g.Tracking,"late native snapshot adopts event-started finger");
        Touch(TouchPhase.Moved,100,580);Input.touches[0]=new Touch {fingerId=42,position=new Vector2(100,Screen.height-580),phase=TouchPhase.Moved};
        g.UpdateInput();g.Frame(EventType.MouseDrag,100,580);g.Frame(EventType.Repaint,100,580);
        Near(g.BodyY,40,"adopted native finger retains gesture pointer ownership");
        Touch(TouchPhase.Ended,100,580);Input.touches[0]=new Touch {fingerId=42,position=new Vector2(100,Screen.height-580),phase=TouchPhase.Ended};
        g.UpdateInput();g.Frame(EventType.MouseUp,100,580);Check(g.cardUses==0,"late-native swipe does not use card");
    }
    static void FocusLossCancels()
    {var g=Fresh();Down(g);g.LoseFocus();Check(!g.Tracking&&!g.Dragging,"focus loss cancels native touch gesture");Up(g,100,620);Check(g.cardUses==0,"focus loss release never uses card");}
    static void NativePollAfterPlainMouseDown()
    {
        var g=Fresh();Time.frameCount++;g.Frame(EventType.MouseDown,100,620);
        Check(!g.Tracking&&GUIUtility.hotControl==101,"plain GUI press can precede native Input.Began");
        Touch(TouchPhase.Began,100,620);g.UpdateInput();g.Frame(EventType.Layout,100,620);
        Check(g.Tracking,"late native Began recognizes this card's previous GUI capture");
        Move(g,100,580);Near(g.BodyY,40,"late-polled native finger scrolls the captured card body");
        Up(g,100,580);Check(g.cardUses==0,"late-polled swipe does not activate card");
    }
    static void EventOnlyTapUsesExactlyOnce()
    {
        var g=Fresh();Time.frameCount++;g.Frame(EventType.MouseDown,100,620,eventTouch:true);g.Frame(EventType.Repaint,100,620,eventTouch:true);
        Time.frameCount++;g.Frame(EventType.MouseUp,102,617,eventTouch:true);Check(g.cardUses==1,"event-only touch tap uses card exactly once");Near(g.BodyY,0,"event-only short tap leaves text position");
    }
    static void EventOnlyAfterCanceledNative()
    {
        var g=Fresh();Down(g);Touch(TouchPhase.Canceled,100,620);g.UpdateInput();g.Frame(EventType.Repaint,100,620);
        Time.frameCount+=3;Input.touches=new Touch[0];g.Frame(EventType.Repaint,100,620);
        Time.frameCount++;g.Frame(EventType.MouseDown,100,620,eventTouch:true);
        Check(g.Tracking,"stale Canceled native phase does not reject a new event-only finger");
        Time.frameCount++;g.Frame(EventType.MouseDrag,100,580,eventTouch:true);Near(g.BodyY,40,"event-only swipe works after native cancel");
    }
    static void DesktopClickAfterEarlierResizeCancel()
    {
        var g=Fresh();Time.frameCount=1;g.CancelForResize();
        Time.frameCount=2;g.Frame(EventType.Repaint,100,620);
        Time.frameCount=10;g.UpdateInput();g.Frame(EventType.MouseDown,100,620);
        Check(!g.Tracking,"ordinary desktop press stays outside native touch gesture tracking");
        Check(GUIUtility.hotControl==101,"ordinary desktop press captures its card button");
        Time.frameCount=11;g.UpdateInput();g.Frame(EventType.MouseUp,100,620);
        Check(g.cardUses==1,"old resize-cancellation marker must not release a later ordinary desktop press before MouseUp");
        Check(GUIUtility.hotControl==0,"ordinary desktop release clears its capture");
    }
    const float MeasuredScale=355f/720,MeasuredOffset=(632-1280*MeasuredScale)/2;
    static Lumia.LumiaGame Measured()
    {
        GUI.Reset();Input.touches=new Touch[0];Screen.width=632;Screen.height=355;Time.frameCount=0;
        var g=new Lumia.LumiaGame {HandView=new Rect(0,0,1004,245),BodyViewport=new Rect(11,108,166,109),BodyView=new Rect(0,0,151,234.17f)};
        g.Frame(EventType.Repaint,0,0,scale:MeasuredScale,offsetX:MeasuredOffset);return g;
    }
    static void MeasuredFrame(Lumia.LumiaGame g,EventType type,float x,float y,bool playable=true)
    {g.Frame(type,x,y,playable,MeasuredScale,offsetX:MeasuredOffset);}
    static void MeasuredMobileNestedBounds()
    {
        var g=Measured();var root=g.AreaAtDepth(1);var body=g.AreaAtDepth(2);
        Near(root.bounds.x,12.27779f,"measured phone hand left");Near(root.bounds.y,225.8194f,"measured phone hand top");
        Near(g.ApiBodyOrigin.x,29.86807f,"host models actual unscaled GUIClip-origin API X");Near(g.ApiBodyOrigin.y,511.25f,"host models actual unscaled GUIClip-origin API Y");
        Near(g.PaintedBodyOrigin.x,17.70139f,"painted body screen X from measured browser");Near(g.PaintedBodyOrigin.y,279.0694f,"painted body screen Y from measured browser");
        Near(body.bounds.x,g.PaintedBodyOrigin.x,"registered nested viewport left follows actual paint");Near(body.bounds.y,g.PaintedBodyOrigin.y,"registered nested viewport top follows actual paint");
        Check(body.bounds.Contains(57,323),"actual native finger (57,323) lands inside the registered body");Check(body.bounds.height>53,"nested body remains visibly touchable after parent and screen clipping");
    }
    static void MeasuredMobileNestedSwipe()
    {
        var g=Measured();Touch(TouchPhase.Began,57,323);g.UpdateInput();MeasuredFrame(g,EventType.MouseDown,57,323);
        Check(g.Tracking,"actual measured mobile touch starts on inner body even with no hand overflow");
        Touch(TouchPhase.Moved,57,293);g.UpdateInput();MeasuredFrame(g,EventType.MouseDrag,57,293);MeasuredFrame(g,EventType.Repaint,57,293);
        Near(g.BodyY,30/MeasuredScale,"measured mobile finger moves nested description by scaled distance");Near(g.HandX,0,"measured vertical swipe does not move outer hand");
        Touch(TouchPhase.Ended,57,293);g.UpdateInput();MeasuredFrame(g,EventType.MouseUp,57,293);
        Check(g.cardUses==0,"measured swipe cannot use the card under its description");
    }
    static void ScrolledParentFractionalRounding()
    {
        foreach(float position in new[]{198.4f,198.5f,198.6f})
        {
            var g=Measured();g.HandView=new Rect(0,0,1600,245);g.BodyViewport=new Rect(209,108,166,109);g.SetPositions(new Vector2(position,0),Vector2.zero);
            MeasuredFrame(g,EventType.Repaint,0,0);var area=g.AreaAtDepth(2);
            Near(area.bounds.x,MeasuredOffset+(24+Mathf.Round(-position)+209)*MeasuredScale,"fractional parent position uses Unity-rounded content origin");
            Near(area.bounds.x,g.PaintedBodyOrigin.x,"fractional parent's registered child matches painted child");
            float x=area.bounds.x+20,y=area.bounds.y+40;
            Touch(TouchPhase.Began,x,y);g.UpdateInput();MeasuredFrame(g,EventType.MouseDown,x,y,false);
            Touch(TouchPhase.Moved,x,y-20);g.UpdateInput();MeasuredFrame(g,EventType.MouseDrag,x,y-20,false);MeasuredFrame(g,EventType.Repaint,x,y-20,false);
            Near(g.BodyY,20/MeasuredScale,"nested text scroll works after horizontal hand scrolling and rounding");Near(g.HandX,position,"vertical swipe preserves fractional hand scroll position");
        }
    }
    static void DoubleNestedMobileBoundsAndSwipe()
    {
        var g=Measured();g.HandViewport=new Rect(20,100,600,400);g.HandView=new Rect(0,0,1200,800);
        g.BodyViewport=new Rect(100,50,300,220);g.BodyView=new Rect(0,0,500,700);g.DoubleNested=true;
        g.SetPositions(new Vector2(50.4f,20.6f),new Vector2(10.6f,30.4f));MeasuredFrame(g,EventType.Repaint,0,0,false);
        var area=g.AreaAtDepth(3);Near(area.bounds.x,MeasuredOffset+89*MeasuredScale,"second nested viewport composes both parent rounded origins");Near(area.bounds.y,149*MeasuredScale,"second nested viewport composes both parent Y origins");
        Near(area.bounds.x,g.PaintedDeepOrigin.x,"deep registered X matches paint");Near(area.bounds.y,g.PaintedDeepOrigin.y,"deep registered Y matches paint");
        float x=area.bounds.x+10,y=area.bounds.y+35;
        Touch(TouchPhase.Began,x,y);g.UpdateInput();MeasuredFrame(g,EventType.MouseDown,x,y,false);
        Touch(TouchPhase.Moved,x,y-25);g.UpdateInput();MeasuredFrame(g,EventType.MouseDrag,x,y-25,false);MeasuredFrame(g,EventType.Repaint,x,y-25,false);
        Near(g.DeepY,25/MeasuredScale,"deepest supported text viewport owns vertical movement");Near(g.BodyY,30.4f,"deep swipe preserves enclosing text position");Near(g.HandX,50.4f,"deep swipe preserves outer hand position");
    }
    static void NonzeroViewRectOrigins()
    {
        var g=Measured();g.HandView=new Rect(7.3f,11.2f,1600,800);g.BodyViewport=new Rect(70,75,166,109);g.BodyView=new Rect(3.2f,8.4f,151,400);
        g.DeepViewport=new Rect(12,20,100,70);g.DeepView=new Rect(0,0,88,300);g.DoubleNested=true;
        g.SetPositions(new Vector2(20.4f,10.6f),new Vector2(2.4f,6.3f));MeasuredFrame(g,EventType.Repaint,0,0,false);
        var area=g.AreaAtDepth(3);Near(area.bounds.x,MeasuredOffset+72*MeasuredScale,"nonzero viewRect origin participates before native rounding");Near(area.bounds.y,516*MeasuredScale,"nested nonzero viewRect Y origins remain in screen units");
        Near(area.bounds.x,g.PaintedDeepOrigin.x,"nonzero-origin nested X matches paint");Near(area.bounds.y,g.PaintedDeepOrigin.y,"nonzero-origin nested Y matches paint");Check(area.bounds.height>30,"nonzero-origin nested viewport keeps its visible swipe area");
    }
}
