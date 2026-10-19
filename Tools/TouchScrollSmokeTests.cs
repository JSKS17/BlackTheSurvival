using System;
using System.Collections.Generic;
using Lumia;

// A standalone behavioral runner; it has no Unity or scene dependency.
public static class TouchScrollSmokeTests
{
    static int assertions, scenarios;
    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Near(float actual, float expected, string message)
    { Check(Math.Abs(actual - expected) < .001f, message + " (actual=" + actual + ", expected=" + expected + ")"); }
    static TouchScrollArea Area(string key = "list", float maxX = 0, float maxY = 1000, int layer = 0, int depth = 0, int order = 0)
    {
        return new TouchScrollArea { key = key, bounds = new TouchScrollBounds(0, 0, 600, 400), enabled = true,
            maxX = maxX, maxY = maxY, layer = layer, depth = depth, order = order, pixelsPerUnitX = 1, pixelsPerUnitY = 1 };
    }
    static List<TouchScrollArea> Areas(params TouchScrollArea[] areas) { return new List<TouchScrollArea>(areas); }
    static void Scenario(Action action) { action(); scenarios++; }

    public static int Main()
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        try
        {
            Scenario(ShortTapAndThreshold);
            Scenario(VerticalDirectionAndReversal);
            Scenario(HorizontalAxisLock);
            Scenario(BoundariesAndImmediateReversal);
            Scenario(SinglePointerOwnership);
            Scenario(CancelAndFreshGesture);
            Scenario(NestedDirectionalOwnership);
            Scenario(ModalAndDisabledScopes);
            Scenario(TacticalModuleOverlayScopes);
            Scenario(ScreenPixelsAndGuiUnits);
            Scenario(UnsupportedAxisDoesNotClick);
            Scenario(FastReleaseAndFinalPosition);
            Scenario(NoOverflowAndOutsideStarts);
            Scenario(AncestorClippingAndHalfOpenBounds);
            Scenario(CandidateSnapshotsSurviveRegistryReuse);
            Scenario(UnsupportedInitialJitterCanFindSupportedAxis);
            Scenario(ContentShrinkReclampsTheGestureAnchor);
            Console.WriteLine("TOUCH_SCROLL_RESULT {\"result\":\"PASS\",\"assertions\":" + assertions + ",\"scenarios\":" + scenarios + "}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL: " + error.Message);
            Console.WriteLine("TOUCH_SCROLL_RESULT {\"result\":\"FAIL\",\"assertions\":" + assertions + ",\"scenarios\":" + scenarios + "}");
            return 1;
        }
    }

    static void TacticalModuleOverlayScopes()
    {
        var gesture = new TouchScrollGesture();
        var inventory = Area("inventory-foods", layer: 2);
        var candidates = Area("tactical-module-cards", layer: 10);
        candidates.bounds = new TouchScrollBounds(101, 225, 520, 338);
        var details = Area("tactical-module-preview", layer: 10);
        details.bounds = new TouchScrollBounds(660, 338, 504, 211);
        var registry = Areas(inventory, candidates, details);
        Check(gesture.Begin(1, 300, 350, registry, 10) && gesture.Move(1, 300, 280), "module picker accepts a swipe at its top modal layer");
        Check(gesture.OwnerKey == "tactical-module-cards", "module candidate list owns the swipe rather than the inventory behind it");
        Near(gesture.ScrollY, 70, "module candidate list scrolls by the actual finger distance");
        gesture.Cancel();
        Check(gesture.Begin(2, 800, 450, registry, 10) && gesture.Move(2, 800, 410), "module full-effect preview has an independent scroll target");
        Check(gesture.OwnerKey == "tactical-module-preview", "module preview does not scroll the candidate list or underlying food bag");
        gesture.Cancel();
        Check(!gesture.Begin(3, 800, 450, registry, 2), "closed module areas cannot capture touches assigned to the inventory layer");
    }

    static void ShortTapAndThreshold()
    {
        var gesture = new TouchScrollGesture(); var list = Area(); list.scrollY = 70;
        Check(gesture.Begin(4, 100, 100, Areas(list), 0), "press inside a scrolling body is a candidate");
        Check(gesture.IsTracking && !gesture.IsDragging && !gesture.HasScroll, "candidate press does not claim a tap");
        for (int i = 1; i <= 8; ++i)
            Check(!gesture.Move(4, 100, 100 - i) && !gesture.IsDragging, "small pointer jitter remains a tap: " + i);
        Check(!gesture.End(4, 100, 93), "a short release is available to its original button");
        Check(!gesture.IsTracking && !gesture.HasScroll, "tap creates no scroll result");
        Check(gesture.Begin(4, 100, 100, Areas(list), 0), "next touch starts normally");
        Check(gesture.Move(4, 100, 90), "cumulative travel crosses the physical threshold");
        Check(gesture.IsDragging && gesture.HasScroll && gesture.Axis == TouchScrollAxis.Vertical, "crossing threshold claims the vertical body");
        Near(gesture.ScrollY, 80, "first claimed move includes all travel from the press");
    }

    static void VerticalDirectionAndReversal()
    {
        var gesture = new TouchScrollGesture(); var list = Area(); list.scrollY = 150;
        Check(gesture.Begin(1, 100, 200, Areas(list), 0), "vertical gesture begins");
        Check(gesture.Move(1, 102, 160), "finger moving up starts vertical scrolling");
        Near(gesture.ScrollY, 190, "finger up moves down through the document");
        Near(gesture.ScrollX, 0, "minor horizontal jitter does not move content sideways");
        Check(gesture.Move(1, 99, 180), "finger can reverse during its drag");
        Near(gesture.ScrollY, 170, "finger down reverses document movement");
        Check(gesture.Move(1, 99, 500), "captured pointer can leave the initial viewport");
        Near(gesture.ScrollY, 0, "leaving the viewport keeps the same owner and clamps at the top");
        Check(gesture.OwnerKey == "list" && gesture.Axis == TouchScrollAxis.Vertical, "owner and axis remain locked outside bounds");
    }

    static void HorizontalAxisLock()
    {
        var gesture = new TouchScrollGesture(); var list = Area("map", 500, 500); list.scrollX = 100; list.scrollY = 75;
        Check(gesture.Begin(2, 200, 200, Areas(list), 0) && gesture.Move(2, 170, 198), "dominant horizontal movement selects horizontal axis");
        Check(gesture.Axis == TouchScrollAxis.Horizontal, "map drag locks to horizontal axis");
        Near(gesture.ScrollX, 130, "finger left moves farther right through the map"); Near(gesture.ScrollY, 75, "other scroll axis is preserved");
        gesture.Move(2, 160, 50);
        Near(gesture.ScrollX, 140, "later move keeps horizontal direction despite large vertical drift");
        Near(gesture.ScrollY, 75, "axis lock prevents a diagonal pan");
        Check(gesture.End(2, 160, 50), "a horizontal drag release is consumed instead of selecting a card/node");
    }

    static void BoundariesAndImmediateReversal()
    {
        var gesture = new TouchScrollGesture(); var list = Area("bounded", 0, 100); list.scrollY = 50;
        Check(gesture.Begin(3, 100, 200, Areas(list), 0) && gesture.Move(3, 100, 50), "bounded drag reaches bottom");
        Near(gesture.ScrollY, 100, "bottom clamps exactly");
        gesture.Move(3, 100, -500); Near(gesture.ScrollY, 100, "continued outward travel never overscrolls");
        gesture.Move(3, 100, -499); Near(gesture.ScrollY, 99, "reversing one pixel responds immediately without an overshoot dead zone");
        gesture.Move(3, 100, 900); Near(gesture.ScrollY, 0, "top clamps exactly");
        gesture.Move(3, 100, 899); Near(gesture.ScrollY, 1, "top-edge reversal responds immediately");
    }

    static void SinglePointerOwnership()
    {
        var gesture = new TouchScrollGesture(); var list = Area();
        Check(gesture.Begin(11, 100, 200, Areas(list), 0), "first pointer starts tracking");
        Check(!gesture.Begin(12, 100, 100, Areas(list), 0), "a second pointer cannot replace the first candidate");
        Check(!gesture.Move(12, 100, 50) && !gesture.IsDragging, "second pointer movement cannot begin scrolling");
        Check(!gesture.End(12, 100, 50) && gesture.IsTracking, "second pointer release cannot end the first touch");
        Check(gesture.Move(11, 100, 170), "original pointer still owns the gesture");
        Near(gesture.ScrollY, 30, "original press anchor survived second-finger noise");
        gesture.Move(12, 100, 1); Near(gesture.ScrollY, 30, "second pointer cannot alter an active drag");
        Check(gesture.End(11, 100, 170) && !gesture.IsTracking, "only the owner ends its drag");
    }

    static void CancelAndFreshGesture()
    {
        var gesture = new TouchScrollGesture(); var list = Area(); list.scrollY = 100;
        Check(gesture.Begin(1, 100, 200, Areas(list), 0) && gesture.Move(1, 100, 160), "gesture begins before a touch cancel");
        gesture.Cancel();
        Check(!gesture.IsTracking && !gesture.IsDragging, "touch cancel releases candidate and drag ownership");
        Check(!gesture.Move(1, 100, 100) && !gesture.End(1, 100, 100), "cancelled pointer cannot resume or click on release");
        gesture.Reset(); Check(!gesture.HasScroll && gesture.OwnerKey == null && gesture.Axis == TouchScrollAxis.None, "frame reset clears stale scroll routing");
        Check(gesture.Begin(2, 150, 150, Areas(list), 0) && !gesture.End(2, 150, 150), "new touch after cancellation is a normal tap");
    }

    static void NestedDirectionalOwnership()
    {
        var outer = Area("outer", 800, 800, depth: 0, order: 20);
        var inner = Area("inner", 0, 300, depth: 1, order: 1); inner.bounds = new TouchScrollBounds(100, 80, 250, 180);
        var gesture = new TouchScrollGesture();
        Check(gesture.Begin(1, 150, 150, Areas(outer, inner), 0) && gesture.Move(1, 150, 120), "nested vertical candidate begins");
        Check(gesture.OwnerKey == "inner", "deepest scroll body wins even when parent has later order");
        gesture.Reset();
        Check(gesture.Begin(1, 150, 150, Areas(outer, inner), 0) && gesture.Move(1, 120, 148), "nested horizontal movement begins");
        Check(gesture.OwnerKey == "outer" && gesture.Axis == TouchScrollAxis.Horizontal, "horizontal parent handles movement unsupported by the vertical child");
        gesture.Reset();
        var overlay = Area("last drawn", 0, 100, depth: 1, order: 50); overlay.bounds = inner.bounds;
        Check(gesture.Begin(1, 150, 150, Areas(outer, overlay, inner), 0) && gesture.Move(1, 150, 120), "overlapping same-depth scopes begin");
        Check(gesture.OwnerKey == "last drawn", "last drawn scope wins within the same layer and depth");
    }

    static void ModalAndDisabledScopes()
    {
        var background = Area("background", 0, 800, layer: 0, depth: 4, order: 99);
        var modal = Area("modal", 0, 400, layer: 6, depth: 0, order: 1);
        var disabled = Area("disabled", 0, 900, layer: 6, depth: 9, order: 100); disabled.enabled = false;
        var gesture = new TouchScrollGesture();
        Check(gesture.Begin(7, 150, 150, Areas(background, disabled, modal), 6) && gesture.Move(7, 150, 120), "foreground modal can scroll");
        Check(gesture.OwnerKey == "modal", "disabled and covered background scopes cannot steal a modal drag");
        gesture.Reset();
        Check(!gesture.Begin(8, 150, 150, Areas(background, disabled), 6), "modal without a scrolling body never falls through to the background");
    }

    static void ScreenPixelsAndGuiUnits()
    {
        var gesture = new TouchScrollGesture(); var list = Area(); list.pixelsPerUnitX = list.pixelsPerUnitY = .5f;
        Check(gesture.Begin(4, 100, 200, Areas(list), 0), "half-scale screen gesture begins");
        Check(!gesture.Move(4, 100, 192), "tap threshold remains screen pixels, not scaled GUI units");
        Check(gesture.Move(4, 100, 190), "physical threshold crosses at ten screen pixels");
        Near(gesture.ScrollY, 20, "ten physical pixels equal twenty GUI units at half scale");
        gesture.Move(4, 100, 180); Near(gesture.ScrollY, 40, "subsequent screen deltas use the same GUI scale");
        gesture.Reset(); list.pixelsPerUnitX = .5f; list.pixelsPerUnitY = 2;
        Check(gesture.Begin(4, 100, 200, Areas(list), 0) && gesture.Move(4, 108, 188), "nonuniform screen transform can begin a gesture");
        Check(gesture.Axis == TouchScrollAxis.Vertical, "axis dominance is evaluated in physical pixels before GUI scaling");
        Near(gesture.ScrollY, 6, "vertical delta divides by the actual vertical screen scale");
    }

    static void UnsupportedAxisDoesNotClick()
    {
        var gesture = new TouchScrollGesture();
        Check(gesture.Begin(5, 200, 100, Areas(Area()), 0), "vertical list observes a possible tap");
        gesture.Move(5, 100, 101);
        Check(gesture.IsDragging && !gesture.HasScroll && gesture.Axis == TouchScrollAxis.None, "unsupported horizontal swipe still cancels a button click");
        Check(gesture.End(5, 100, 101), "long unsupported-axis swipe release is consumed");
    }

    static void FastReleaseAndFinalPosition()
    {
        var gesture = new TouchScrollGesture(); var list = Area(); list.scrollY = 20;
        Check(gesture.Begin(6, 100, 200, Areas(list), 0), "fast touch begins");
        Check(gesture.End(6, 100, 150), "down/up-only quick swipe is recognized without a sampled move");
        Check(!gesture.IsTracking && gesture.HasScroll && gesture.OwnerKey == "list", "release keeps its final scroll routing available for the GUI event");
        Near(gesture.ScrollY, 70, "release incorporates the final unsampled movement");
        gesture.Reset(); Check(!gesture.HasScroll, "explicit reset clears the finalized result");
        Check(gesture.Begin(6, 100, 200, Areas(list), 0) && gesture.Move(6, 100, 180), "ordinary drag begins before final release movement");
        Check(gesture.End(6, 100, 170), "ordinary drag consumes the release");
        Near(gesture.ScrollY, 50, "last release delta is applied exactly once");
    }

    static void NoOverflowAndOutsideStarts()
    {
        var gesture = new TouchScrollGesture();
        Check(!gesture.Begin(1, 100, 100, Areas(Area("fits", 0, 0)), 0), "fitting content preserves ordinary controls without creating a scroll gesture");
        Check(!gesture.Begin(1, -1, 100, Areas(Area()), 0), "touch outside every viewport cannot begin scrolling");
        Check(!gesture.Begin(1, 601, 100, Areas(Area()), 0), "touch beyond the far edge cannot begin scrolling");
        Check(!gesture.Move(1, 100, 100) && !gesture.IsTracking, "moving into a viewport after an outside press never captures it");
    }

    static void AncestorClippingAndHalfOpenBounds()
    {
        var parent = new TouchScrollBounds(10, 20, 100, 80);
        var child = new TouchScrollBounds(80, 70, 100, 80);
        var clipped = TouchScrollBounds.Intersect(parent, child);
        Near(clipped.x, 80, "nested viewport keeps the visible left edge"); Near(clipped.y, 70, "nested viewport keeps the visible top edge");
        Near(clipped.width, 30, "nested viewport clips at its ancestor's right edge"); Near(clipped.height, 30, "nested viewport clips at its ancestor's bottom edge");
        Check(clipped.Contains(80, 70) && clipped.Contains(109.9f, 99.9f), "visible child interior remains touchable");
        Check(!clipped.Contains(110, 90) && !clipped.Contains(90, 100), "far edges use the same half-open hit test as GUI Rect");
        Check(!clipped.Contains(130, 90), "offscreen child content cannot capture a gesture");
        var empty = TouchScrollBounds.Intersect(parent, new TouchScrollBounds(200, 200, 20, 20));
        Check(empty.width == 0 && empty.height == 0 && !empty.Contains(200, 200), "fully clipped nested viewport has no touchable body");
        var gesture = new TouchScrollGesture(); var inner = Area("clipped child"); inner.bounds = clipped;
        Check(!gesture.Begin(1, 130, 90, Areas(inner), 0), "gesture candidate honors the ancestor-clipped bounds");
    }

    static void CandidateSnapshotsSurviveRegistryReuse()
    {
        var gesture = new TouchScrollGesture(); var original = Area("pressed body"); original.scrollY = 30;
        var registry = Areas(original);
        Check(gesture.Begin(2, 100, 200, registry, 0), "gesture captures the visible body at its press");
        // Repaint reuses its area objects while virtualized rows can change order.
        // A press must keep its own immutable identity, scale and initial position.
        original.key = "another row"; original.scrollY = 500; original.maxY = 9000;
        original.pixelsPerUnitY = 2; original.bounds = new TouchScrollBounds(500, 500, 5, 5);
        registry.Clear();
        Check(gesture.Move(2, 100, 180) && gesture.OwnerKey == "pressed body", "registry pool reuse cannot redirect a pending gesture to another scope");
        Near(gesture.ScrollY, 50, "pending gesture retains its press-time scroll and GUI scale");
        original.key = "third row"; original.pixelsPerUnitY = 10;
        gesture.Move(2, 100, 170);
        Check(gesture.OwnerKey == "pressed body", "active owner identity remains stable after later registry writes");
        Near(gesture.ScrollY, 60, "active drag never reads a recycled area's scale");
    }

    static void UnsupportedInitialJitterCanFindSupportedAxis()
    {
        var gesture = new TouchScrollGesture(); var list = Area("vertical body");
        Check(gesture.Begin(1, 100, 200, Areas(list), 0), "vertical body observes the initial contact");
        gesture.Move(1, 110, 198);
        Check(gesture.IsDragging, "an initial sideways wobble already suppresses a click");
        gesture.Move(1, 110, 100);
        Check(gesture.HasScroll && gesture.OwnerKey == "vertical body" && gesture.Axis == TouchScrollAxis.Vertical,
            "unsupported initial jitter cannot permanently prevent the supported vertical swipe");
        Check(gesture.ScrollY > 0, "supported movement after initial wobble actually scrolls the body");
        Check(gesture.End(1, 110, 100), "late direction recognition still suppresses a release click");
    }

    static void ContentShrinkReclampsTheGestureAnchor()
    {
        var gesture = new TouchScrollGesture(); var list = Area("changing body"); list.scrollY = 500;
        Check(gesture.Begin(1, 100, 200, Areas(list), 0) && gesture.Move(1, 100, 180), "long content begins an active drag");
        Near(gesture.ScrollY, 520, "original long-content position is captured");
        gesture.ConstrainBounds(0, 100);
        Near(gesture.ScrollY, 100, "content shrink clamps the authoritative core position, not only its rendered output");
        gesture.Move(1, 100, 181);
        Near(gesture.ScrollY, 99, "one-pixel reversal after content shrink moves immediately");
        gesture.ConstrainBounds(0, 0); gesture.Move(1, 100, 160);
        Near(gesture.ScrollY, 0, "content that now fits cannot keep an out-of-bounds offset");
        Check(gesture.End(1, 100, 160), "shrinking content never restores the suppressed click");
    }
}
