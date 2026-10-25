using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        const float TouchSwipeThreshold = 9;
        readonly TouchScrollGesture touchScrollGesture = new TouchScrollGesture();
        readonly List<TouchScrollArea> touchScrollAreas = new List<TouchScrollArea>();
        readonly List<TouchScrollArea> touchScrollAreaPool = new List<TouchScrollArea>();
        readonly Stack<TouchScrollScope> touchScrollScopes = new Stack<TouchScrollScope>();
        readonly Dictionary<TouchScrollAreaKey, string> touchScrollKeys = new Dictionary<TouchScrollAreaKey, string>();
        Vector2 touchGuiOrigin, touchNativePoint;
        int touchInputFrame = -1, touchNativeCount, touchFinger, touchNativeFinger, touchButtonControl, touchSuppressUntil = -1, touchEndedFrame = -1;
        int touchContextLayer, touchContextStage, touchContextWidth, touchContextHeight, touchContextTurn;
        long touchContextFlags;
        string touchContextCard, touchContextTrait, touchContextGear, touchContextBoss, touchContextCraft, touchContextMap;
        GameEngine touchContextEngine;
        bool touchContextFullscreen, touchWaitForRelease, touchReleaseButton, touchNativeExists, touchNativeTracking;
        TouchPhase touchNativePhase;
        EventType touchRawGuiEvent;

        struct TouchScrollScope
        {
            public string identity;
            public TouchScrollBounds visibleBounds;
            public Vector2 contentOrigin;
        }
        struct TouchScrollAreaKey : IEquatable<TouchScrollAreaKey>
        {
            public string semantic, parent;
            public int layer;
            public Rect viewport;
            public bool Equals(TouchScrollAreaKey other) => semantic == other.semantic && parent == other.parent && layer == other.layer && viewport == other.viewport;
            public override bool Equals(object other) => other is TouchScrollAreaKey && Equals((TouchScrollAreaKey)other);
            public override int GetHashCode() { unchecked { return (((semantic?.GetHashCode() ?? 0) * 397 ^ (parent?.GetHashCode() ?? 0)) * 397 ^ layer) * 397 ^ viewport.GetHashCode(); } }
        }

        int TouchTopLayer()
        {
            if (StoryVisible) return 0;
            if (TutorialActive) return CombatInformationOpen || !string.IsNullOrEmpty(inspectCard) ? 6 : fieldInfo ? 5 : 0;
            return TacticalModuleChoiceOpen ? 10 : AdvancedCardChoiceOpen ? 9 : tutorialOffer ? 8 : modeSelecting || runMenu || endlessConfirm ? 7
                : CombatInformationOpen || !string.IsNullOrEmpty(inspectCard) || !string.IsNullOrEmpty(inspectTrait) || !string.IsNullOrEmpty(inspectGear) ? 6
                : fieldInfo || enemyLoadout || !string.IsNullOrEmpty(inspectBoss) ? 5 : help ? 4 : settings ? 3 : inventory ? 2
                : catalog || research || !string.IsNullOrEmpty(craftPending) || !string.IsNullOrEmpty(mapIntel) ? 1 : 0;
        }
        long TouchContextFlags()
        {
            return (lobby ? 1L : 0) | (settings ? 1L << 1 : 0) | (catalog ? 1L << 2 : 0)
                | (inventory ? 1L << 3 : 0) | (help ? 1L << 4 : 0) | (fieldInfo ? 1L << 5 : 0)
                | (fieldEnemy ? 1L << 6 : 0) | (enemyLoadout ? 1L << 7 : 0) | (research ? 1L << 8 : 0)
                | (modeSelecting ? 1L << 9 : 0) | (runMenu ? 1L << 10 : 0) | (endlessConfirm ? 1L << 11 : 0)
                | (tutorialOffer ? 1L << 12 : 0) | (TutorialActive ? 1L << 13 : 0) | (StoryVisible ? 1L << 14 : 0)
                | (CombatInformationOpen ? 1L << 15 : 0) | (AdvancedCardChoiceOpen ? 1L << 16 : 0) | (BrowserAccountFrozen ? 1L << 17 : 0)
                | (TacticalModuleChoiceOpen ? 1L << 18 : 0);
        }
        void RememberTouchContext()
        {
            touchContextEngine = Engine; touchContextStage = Engine == null ? -1 : (int)Engine.State.stage;
            touchContextLayer = TouchTopLayer(); touchContextFlags = TouchContextFlags();
            touchContextTurn = Engine?.State.combat?.turn ?? -1;
            touchContextCard = inspectCard; touchContextTrait = inspectTrait; touchContextGear = inspectGear;
            touchContextBoss = inspectBoss; touchContextCraft = craftPending; touchContextMap = mapIntel;
            touchContextWidth = Screen.width; touchContextHeight = Screen.height; touchContextFullscreen = Screen.fullScreen;
        }
        bool TouchContextChanged() => BrowserAccountFrozen || !ReferenceEquals(Engine, touchContextEngine)
            || (Engine == null ? -1 : (int)Engine.State.stage) != touchContextStage || TouchTopLayer() != touchContextLayer
            || (Engine?.State.combat?.turn ?? -1) != touchContextTurn
            || TouchContextFlags() != touchContextFlags || inspectCard != touchContextCard || inspectTrait != touchContextTrait
            || inspectGear != touchContextGear || inspectBoss != touchContextBoss || craftPending != touchContextCraft || mapIntel != touchContextMap
            || Screen.width != touchContextWidth || Screen.height != touchContextHeight || Screen.fullScreen != touchContextFullscreen;

        bool BeginNativeTouchScroll(int finger, Vector2 point, bool native = false)
        {
            if (BrowserAccountFrozen || touchWaitForRelease) return false;
            if (!touchScrollGesture.Begin(finger, point.x, point.y, touchScrollAreas, TouchTopLayer(), TouchSwipeThreshold)) return false;
            touchFinger = finger; touchNativeFinger = finger; touchNativeTracking = native;
            touchSuppressUntil = -1; touchReleaseButton = false; touchEndedFrame = -1;
            RememberTouchContext(); return true;
        }
        void MoveNativeTouchScroll(Vector2 point, bool end)
        {
            bool wasDragging = touchScrollGesture.IsDragging;
            bool dragged = end ? touchScrollGesture.End(touchFinger, point.x, point.y) : touchScrollGesture.Move(touchFinger, point.x, point.y);
            if (dragged)
            {
                if (!wasDragging) touchReleaseButton = true;
                touchSuppressUntil = Time.frameCount + 1;
            }
            if (end) touchEndedFrame = Time.frameCount;
        }
        void CancelNativeTouchScroll(bool suppressRelease)
        {
            if (suppressRelease)
            { touchReleaseButton = true; touchSuppressUntil = Time.frameCount + 1; }
            touchScrollGesture.Cancel(); touchNativeTracking = false; touchEndedFrame = Time.frameCount;
        }

        void UpdateTouchScrollInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (touchInputFrame == Time.frameCount) return;
            touchInputFrame = Time.frameCount;
            touchNativeCount = Input.touchCount; touchNativeExists = touchNativeCount == 1;
            if ((touchScrollGesture.IsTracking || touchScrollGesture.IsDragging) && TouchContextChanged()) CancelNativeTouchScroll(true);
            if (touchScrollGesture.IsDragging && !touchScrollGesture.IsTracking && Time.frameCount > touchEndedFrame)
            { touchScrollGesture.Reset(); touchNativeTracking = false; }
            if (touchNativeCount > 1)
            {
                if (touchScrollGesture.IsTracking || touchScrollGesture.IsDragging || touchButtonControl != 0) CancelNativeTouchScroll(true);
                touchWaitForRelease = true; touchSuppressUntil = Time.frameCount + 1; return;
            }
            if (touchWaitForRelease)
            {
                if (touchNativeCount == 0) touchWaitForRelease = false;
                else { touchSuppressUntil = Time.frameCount + 1; return; }
            }
            if (!touchNativeExists)
            {
                if (touchButtonControl != 0 && touchEndedFrame >= 0 && Time.frameCount > touchEndedFrame + 2) touchReleaseButton = true;
                return;
            }
            Touch touch = Input.GetTouch(0); touchNativePhase = touch.phase;
            touchNativePoint = new Vector2(touch.position.x, Screen.height - touch.position.y);
            // Browser GUI events can arrive before legacy Input's touch snapshot.
            // Adopt that snapshot without changing the gesture's stable pointer ID.
            if (touchScrollGesture.IsTracking && !touchNativeTracking)
            { touchNativeTracking = true; touchNativeFinger = touch.fingerId; }
            if (touch.phase == TouchPhase.Canceled)
            { if (touchScrollGesture.IsTracking || touchButtonControl != 0) CancelNativeTouchScroll(true); return; }
            if (touch.phase == TouchPhase.Began)
            {
                if (!touchScrollGesture.IsTracking) BeginNativeTouchScroll(touch.fingerId, touchNativePoint, true);
                return;
            }
            if (touchScrollGesture.IsTracking)
            {
                if (touch.fingerId != touchNativeFinger) { CancelNativeTouchScroll(true); return; }
            }
#endif
        }

        void BeginTouchScrollGUI()
        {
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            touchGuiOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
            Vector2 eventPoint = GUIUtility.GUIToScreenPoint(Event.current.mousePosition) - touchGuiOrigin;
            GUI.matrix = matrix;
            touchRawGuiEvent = Event.current.rawType;
            UpdateTouchScrollInput();
#if ENABLE_LEGACY_INPUT_MANAGER
            bool rawTouch = touchRawGuiEvent == EventType.TouchDown || touchRawGuiEvent == EventType.TouchMove || touchRawGuiEvent == EventType.TouchUp;
            bool nativePointer = (Event.current.isMouse || rawTouch) && (touchNativeExists || touchScrollGesture.IsTracking || Event.current.pointerType == PointerType.Touch);
            if (nativePointer && !touchWaitForRelease && (!touchNativeExists || touchNativePhase != TouchPhase.Canceled))
            {
                if ((touchRawGuiEvent == EventType.MouseDown || touchRawGuiEvent == EventType.TouchDown) && !touchScrollGesture.IsTracking)
                    BeginNativeTouchScroll(touchNativeExists ? Input.GetTouch(0).fingerId : touchFinger, eventPoint, touchNativeExists);
                else if (touchScrollGesture.IsTracking && (touchRawGuiEvent == EventType.MouseDrag || touchRawGuiEvent == EventType.MouseUp
                    || touchRawGuiEvent == EventType.TouchMove || touchRawGuiEvent == EventType.TouchUp))
                    MoveNativeTouchScroll(eventPoint, touchRawGuiEvent == EventType.MouseUp || touchRawGuiEvent == EventType.TouchUp);
            }
            // Process queued native GUI events in their original order first.
            // Polling the latest touch before an older queued Drag would reverse
            // its delta at a clamped edge. Repaint is the safe native fallback.
            // An event-only Touch pointer has no polled native position or release.
            // Keep it alive until GUI Up instead of ending it at a stale (0, 0).
            if (touchRawGuiEvent == EventType.Repaint && touchScrollGesture.IsTracking && touchNativeTracking && !touchWaitForRelease)
            {
                if (!touchNativeExists) MoveNativeTouchScroll(touchNativePoint, true);
                else if (touchNativePhase == TouchPhase.Ended) MoveNativeTouchScroll(touchNativePoint, true);
                else if (touchNativePhase == TouchPhase.Moved || touchNativePhase == TouchPhase.Stationary)
                    MoveNativeTouchScroll(touchNativePoint, false);
            }
#endif
            if (touchReleaseButton)
            {
                // Only release the button which this native finger actually pressed.
                if (touchButtonControl != 0 && GUIUtility.hotControl == touchButtonControl) GUIUtility.hotControl = 0;
                touchButtonControl = 0; touchReleaseButton = false;
            }
            bool touchEvent = touchRawGuiEvent == EventType.TouchDown || touchRawGuiEvent == EventType.TouchMove || touchRawGuiEvent == EventType.TouchUp;
            if ((touchScrollGesture.IsDragging || Time.frameCount <= touchSuppressUntil || touchWaitForRelease) && (Event.current.isMouse || touchEvent))
                Event.current.Use();
            if (touchRawGuiEvent == EventType.Repaint)
            {
                touchScrollAreas.Clear();
                if (touchScrollKeys.Count > 2048 && !touchScrollGesture.IsTracking && !touchScrollGesture.IsDragging) touchScrollKeys.Clear();
            }
        }
        void EndTouchScrollGUI()
        {
            // A scrollbar/slider or another native control owns this touch. Leave
            // its hotControl and normal Up event to Unity rather than stealing it.
            if (touchScrollGesture.IsTracking && !touchScrollGesture.IsDragging && GUIUtility.hotControl != 0 && GUIUtility.hotControl != touchButtonControl)
                CancelNativeTouchScroll(false);
            if (touchRawGuiEvent == EventType.Repaint && touchScrollGesture.HasScroll)
            {
                bool visible = false;
                foreach (TouchScrollArea area in touchScrollAreas)
                    if (area.key == touchScrollGesture.OwnerKey && area.enabled) { visible = true; break; }
                if (!visible) CancelNativeTouchScroll(true);
            }
        }
        void ObserveTouchScrollButton(int previousControl, EventType eventType)
        {
            // Remember our own button even when GUI Down precedes Input.Began.
            // Otherwise the next Layout mistakes that card for a foreign control
            // and cancels its pending swipe before it crosses the threshold.
            if ((eventType == EventType.MouseDown || eventType == EventType.TouchDown)
                && previousControl == 0 && GUIUtility.hotControl != 0)
            { touchButtonControl = GUIUtility.hotControl; touchEndedFrame = -1; }
            if ((eventType == EventType.MouseUp || eventType == EventType.TouchUp) && GUIUtility.hotControl == 0) touchButtonControl = 0;
        }

        Vector2 BeginTouchScrollView(string key, Rect viewport, Vector2 position, Rect viewRect, bool alwaysShowHorizontal = false, bool alwaysShowVertical = false)
        {
            Vector2 p0 = GUIUtility.GUIToScreenPoint(viewport.position) - touchGuiOrigin;
            Vector2 p1 = GUIUtility.GUIToScreenPoint(new Vector2(viewport.xMax, viewport.yMax)) - touchGuiOrigin;
            float scaleX = Math.Max(.0001f, (p1.x - p0.x) / Math.Max(.0001f, viewport.width));
            float scaleY = Math.Max(.0001f, (p1.y - p0.y) / Math.Max(.0001f, viewport.height));
            if (touchScrollScopes.Count > 0)
            {
                // Unity applies GUI.matrix to the local point but adds enclosing
                // clip origins without scaling. Derive nested screen positions
                // from the parent's rendered content origin instead.
                Vector2 origin = touchScrollScopes.Peek().contentOrigin;
                p0 = origin + new Vector2(viewport.x * scaleX, viewport.y * scaleY);
                p1 = p0 + new Vector2(viewport.width * scaleX, viewport.height * scaleY);
            }
            var screenRect = new Rect(p0.x, p0.y, Math.Max(0, p1.x - p0.x), Math.Max(0, p1.y - p0.y));
            bool horizontal = alwaysShowHorizontal || viewRect.width > viewport.width;
            bool vertical = alwaysShowVertical || viewRect.height > viewport.height;
            float scrollbarWidth = (GUI.skin.verticalScrollbar.fixedWidth > 0 ? GUI.skin.verticalScrollbar.fixedWidth : 12) + GUI.skin.verticalScrollbar.margin.left;
            float scrollbarHeight = (GUI.skin.horizontalScrollbar.fixedHeight > 0 ? GUI.skin.horizontalScrollbar.fixedHeight : 12) + GUI.skin.horizontalScrollbar.margin.top;
            if (horizontal && viewRect.height > viewport.height - scrollbarHeight) vertical = true;
            if (vertical && viewRect.width > viewport.width - scrollbarWidth) horizontal = true;
            float width = Math.Max(0, viewport.width - (vertical ? scrollbarWidth : 0));
            float height = Math.Max(0, viewport.height - (horizontal ? scrollbarHeight : 0));
            float maxX = Math.Max(0, viewRect.width - width), maxY = Math.Max(0, viewRect.height - height);
            var visible = new TouchScrollBounds(p0.x, p0.y, width * scaleX, height * scaleY);
            if (touchScrollScopes.Count > 0) visible = TouchScrollBounds.Intersect(visible, touchScrollScopes.Peek().visibleBounds);
            visible = TouchScrollBounds.Intersect(visible, new TouchScrollBounds(0, 0, Screen.width, Screen.height));
            var identity = new TouchScrollAreaKey { semantic = key, parent = touchScrollScopes.Count > 0 ? touchScrollScopes.Peek().identity : null, layer = currentLayer, viewport = screenRect };
            string areaKey;
            if (!touchScrollKeys.TryGetValue(identity, out areaKey))
            { areaKey = "touch-scroll-" + touchScrollKeys.Count; touchScrollKeys.Add(identity, areaKey); }
            bool owns = touchScrollGesture.HasScroll && touchScrollGesture.OwnerKey == areaKey && !TouchContextChanged();
            if (owns) touchScrollGesture.ConstrainBounds(maxX, maxY);
            Vector2 desired = owns ? new Vector2(Mathf.Clamp(touchScrollGesture.ScrollX, 0, maxX), Mathf.Clamp(touchScrollGesture.ScrollY, 0, maxY)) : position;
            Vector2 native = GUI.BeginScrollView(viewport, desired, viewRect, alwaysShowHorizontal, alwaysShowVertical);
            if (!owns) desired = native;
            // Native ScrollViewState.apply can restore the preceding wheel position.
            // Correct the actual content origin as well as the returned value, so
            // that state cannot overwrite a finger's authoritative drag position.
            Vector2 correction = native - desired;
            GUI.BeginGroup(new Rect(correction.x, correction.y, Math.Max(viewRect.xMax, viewport.width), Math.Max(viewRect.yMax, viewport.height)));
            Vector2 contentOrigin = p0 + new Vector2(
                (Mathf.Round(-native.x - viewRect.x) + correction.x) * scaleX,
                (Mathf.Round(-native.y - viewRect.y) + correction.y) * scaleY);
            touchScrollScopes.Push(new TouchScrollScope { identity = areaKey, visibleBounds = visible, contentOrigin = contentOrigin });
            if (touchRawGuiEvent == EventType.Repaint)
            {
                int index = touchScrollAreas.Count;
                if (touchScrollAreaPool.Count <= index) touchScrollAreaPool.Add(new TouchScrollArea());
                TouchScrollArea area = touchScrollAreaPool[index];
                area.key = areaKey; area.bounds = visible; area.layer = currentLayer; area.depth = touchScrollScopes.Count;
                area.order = index; area.enabled = GUI.enabled && currentLayer == topLayer;
                area.maxX = maxX; area.maxY = maxY;
                area.scrollX = desired.x; area.scrollY = desired.y; area.pixelsPerUnitX = scaleX; area.pixelsPerUnitY = scaleY;
                touchScrollAreas.Add(area);
            }
            return desired;
        }
        void EndTouchScrollView()
        {
            GUI.EndGroup();
            if (touchScrollScopes.Count > 0) touchScrollScopes.Pop();
            bool managed = touchScrollGesture.IsTracking || touchScrollGesture.IsDragging || touchWaitForRelease;
            GUI.EndScrollView(!managed || Event.current.type == EventType.ScrollWheel);
        }
        void OnApplicationFocus(bool focused) { if (!focused) CancelNativeTouchScroll(true); }
    }
}
