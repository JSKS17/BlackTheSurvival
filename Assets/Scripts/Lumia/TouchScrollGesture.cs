using System;
using System.Collections.Generic;

namespace Lumia
{
    public enum TouchScrollAxis { None, Horizontal, Vertical }

    public struct TouchScrollBounds
    {
        public float x, y, width, height;
        public TouchScrollBounds(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = Math.Max(0, width); this.height = Math.Max(0, height); }
        public bool Contains(float px, float py) => width > 0 && height > 0 && px >= x && py >= y && px < x + width && py < y + height;
        public static TouchScrollBounds Intersect(TouchScrollBounds a, TouchScrollBounds b)
        {
            float left = Math.Max(a.x, b.x), top = Math.Max(a.y, b.y);
            return new TouchScrollBounds(left, top, Math.Min(a.x + a.width, b.x + b.width) - left,
                Math.Min(a.y + a.height, b.y + b.height) - top);
        }
    }

    public sealed class TouchScrollArea
    {
        public string key;
        public TouchScrollBounds bounds;
        public int layer, depth, order;
        public bool enabled;
        public float maxX, maxY, scrollX, scrollY, pixelsPerUnitX = 1, pixelsPerUnitY = 1;
    }

    // Pure gesture state: screen pixels decide tap versus swipe; only movement is
    // converted into content units. It never changes a button or a game state.
    public sealed class TouchScrollGesture
    {
        readonly List<TouchScrollArea> candidates = new List<TouchScrollArea>();
        int pointerId;
        float startX, startY, lastX, lastY, threshold;
        TouchScrollArea owner;
        public bool IsTracking { get; private set; }
        public bool IsDragging { get; private set; }
        public bool HasScroll => owner != null && Axis != TouchScrollAxis.None;
        public string OwnerKey => owner?.key;
        public TouchScrollAxis Axis { get; private set; }
        public float ScrollX { get; private set; }
        public float ScrollY { get; private set; }

        public bool Begin(int id, float x, float y, IList<TouchScrollArea> areas, int activeLayer, float thresholdPixels = 9)
        {
            if (IsTracking) return false;
            Reset();
            if (!Finite(x) || !Finite(y) || areas == null || !Finite(thresholdPixels)) return false;
            foreach (var area in areas)
                if (area != null && area.enabled && area.layer == activeLayer && area.bounds.Contains(x, y)
                    && (area.maxX > 0 || area.maxY > 0) && area.pixelsPerUnitX > 0 && area.pixelsPerUnitY > 0)
                    candidates.Add(new TouchScrollArea { key = area.key, bounds = area.bounds, layer = area.layer,
                        depth = area.depth, order = area.order, enabled = area.enabled, maxX = area.maxX, maxY = area.maxY,
                        scrollX = area.scrollX, scrollY = area.scrollY, pixelsPerUnitX = area.pixelsPerUnitX, pixelsPerUnitY = area.pixelsPerUnitY });
            if (candidates.Count == 0) return false;
            pointerId = id; startX = lastX = x; startY = lastY = y;
            threshold = Math.Max(1, thresholdPixels); IsTracking = true;
            return true;
        }

        public bool Move(int id, float x, float y)
        {
            if (!IsTracking || id != pointerId || !Finite(x) || !Finite(y)) return false;
            float dx = x - lastX, dy = y - lastY;
            if (!IsDragging)
            {
                float totalX = x - startX, totalY = y - startY;
                if (totalX * totalX + totalY * totalY < threshold * threshold)
                { lastX = x; lastY = y; return false; }
                IsDragging = true;
            }
            if (owner == null)
            {
                float totalX = x - startX, totalY = y - startY;
                bool horizontal = Math.Abs(totalX) >= Math.Abs(totalY);
                owner = Math.Abs(horizontal ? totalX : totalY) >= threshold ? Choose(horizontal) : null;
                if (owner == null && Math.Abs(horizontal ? totalY : totalX) >= threshold)
                { horizontal = !horizontal; owner = Choose(horizontal); }
                if (owner != null)
                {
                    Axis = horizontal ? TouchScrollAxis.Horizontal : TouchScrollAxis.Vertical;
                    ScrollX = Clamp(owner.scrollX, owner.maxX); ScrollY = Clamp(owner.scrollY, owner.maxY);
                }
                // Unsupported movement cancels its tap, but a subsequent clear
                // movement on a supported axis may still acquire that scroller.
                dx = totalX; dy = totalY;
            }
            if (HasScroll)
            {
                if (Axis == TouchScrollAxis.Horizontal) ScrollX = Clamp(ScrollX - dx / owner.pixelsPerUnitX, owner.maxX);
                else ScrollY = Clamp(ScrollY - dy / owner.pixelsPerUnitY, owner.maxY);
            }
            lastX = x; lastY = y;
            return IsDragging;
        }

        TouchScrollArea Choose(bool horizontal)
        {
            TouchScrollArea chosen = null;
            foreach (var area in candidates)
            {
                if ((horizontal ? area.maxX : area.maxY) <= 0) continue;
                if (chosen == null || area.depth > chosen.depth || area.depth == chosen.depth && area.order > chosen.order)
                    chosen = area;
            }
            return chosen;
        }

        public bool End(int id, float x, float y)
        {
            if (!IsTracking || id != pointerId) return false;
            Move(id, x, y); IsTracking = false;
            return IsDragging;
        }
        public void ConstrainBounds(float maxX, float maxY)
        {
            if (owner == null || !Finite(maxX) || !Finite(maxY)) return;
            owner.maxX = Math.Max(0, maxX); owner.maxY = Math.Max(0, maxY);
            ScrollX = Clamp(ScrollX, owner.maxX); ScrollY = Clamp(ScrollY, owner.maxY);
        }
        public void Cancel() { Reset(); }
        public void Reset()
        {
            candidates.Clear(); owner = null; IsTracking = IsDragging = false;
            Axis = TouchScrollAxis.None; ScrollX = ScrollY = 0;
        }
        static float Clamp(float value, float max) => Math.Max(0, Math.Min(Math.Max(0, max), value));
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
