using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    /// <summary>Integer-grid spell animation. Each card has a stable shape seed and palette.</summary>
    public sealed class CombatEffects
    {
        sealed class Burst
        {
            public CombatAction action;
            public CardDef card;
            public PassiveDef passive;
            public RuneDef rune;
            public GearDef gear;
            public float start, duration;
            public Color color;
            public int seed;
        }
        readonly List<Burst> bursts = new List<Burst>();
        public bool Busy { get { return bursts.Exists(b => Time.unscaledTime - b.start < b.duration * .9f); } }
        public void Clear() { bursts.Clear(); }

        public float Add(CombatAction action, bool reduced)
        {
            var card = GameDatabase.Card(action.cardId);
            var passive=GameDatabase.Passive(action.traitId);var rune=GameDatabase.Rune(action.traitId);
            var gear=(action.traitId??"").StartsWith("gear:")?GameDatabase.Equipment(action.traitId.Substring(5)):action.kind=="deferred_damage"?GameDatabase.Equipment(action.cardId):null;
            Color color = new Color(.57f, .87f, .74f);
            if (card != null) ColorUtility.TryParseHtmlString("#" + card.effectColor, out color);
            if (passive!=null) color=new Color(.84f,.66f,.96f);
            if (rune!=null) color=new Color(.94f,.78f,.47f);
            if (gear!=null) color=new Color(.96f,.72f,.43f);
            float duration = reduced ? .2f : card != null && card.hits > 1 ? .95f : .72f;
            bursts.Add(new Burst { action = action, card = card, passive=passive, rune=rune, gear=gear, start = Time.unscaledTime, duration = duration, color = color, seed = card == null ? action.id : card.effectVariant });
            return duration;
        }

        public Vector2 Offset(bool enemy, bool reduced)
        {
            if (reduced) return Vector2.zero;
            var b = bursts.FindLast(x => Time.unscaledTime - x.start < x.duration && x.action.enemy == enemy);
            if (b == null) return Vector2.zero;
            float p = (Time.unscaledTime - b.start) / b.duration;
            return new Vector2(Snap(Mathf.Sin(p * Mathf.PI) * (enemy ? -14 : 14)), Snap(-Mathf.Sin(p * Mathf.PI) * 6));
        }

        public void Draw(GUIStyle font, bool reduced)
        {
            if (Event.current.type != EventType.Repaint) return;
            bursts.RemoveAll(b => Time.unscaledTime - b.start >= b.duration);
            foreach (var b in bursts)
            {
                float p = Mathf.Clamp01((Time.unscaledTime - b.start) / b.duration);
                Vector2 source = b.action.enemy ? new Vector2(1015, 282) : new Vector2(232, 282);
                Vector2 target = b.action.enemy ? new Vector2(232, 282) : new Vector2(1015, 282);
                string effect = b.card == null ? "poison" : b.card.effect;
                if(b.passive!=null || b.rune!=null || b.gear!=null) effect=b.action.heal>0?"heal":b.action.block>0?"shield":b.rune?.id=="lightning"?"lightning":b.action.kind=="burn"?"flame":"arcane";
                bool status=(b.action.kind??"").StartsWith("status_");
                if(status) effect="arcane";
                Color tint = b.color; tint.a = Mathf.Clamp01((1 - p) * 2.5f);
                bool attack = status || b.action.damage > 0 || b.action.avoided > 0 || b.card != null && (b.card.damage > 0 || b.card.poison > 0 || b.card.weak > 0 || b.card.vulnerable > 0);
                if (!reduced) Spell(effect, source, target, p, tint, b.seed, b.card == null ? 1 : b.card.hits, attack);
                else Ring(b.action.damage > 0 ? target : source, 28 + p * 20, tint, 4, b.seed % 3);
                if (b.action.block > 0) Shield(source, p, new Color(.57f, .87f, .74f, tint.a), b.seed);
                if (b.action.heal > 0) Heal(source, p, new Color(.65f, .91f, .57f, tint.a), b.seed);
                if (b.action.damage > 0) Number(font, target + new Vector2(0, -84 - p * 46), "−" + b.action.damage, new Color(1f, .58f, .51f, tint.a));
                if (b.card != null && b.card.damage > 0 && b.action.damage == 0 && b.action.avoided == 0) Number(font, target + new Vector2(0, -84 - p * 46), "막힘", tint, 16);
                if (b.action.avoided > 0) Number(font, target + new Vector2(0, -40 - p * 30), "회피!" + (b.action.avoided > 1 ? " ×" + b.action.avoided : ""), new Color(.56f, .84f, .96f, tint.a));
                if (b.action.heal > 0) Number(font, source + new Vector2(0, -112 - p * 30), "+" + b.action.heal, new Color(.65f, .91f, .57f, tint.a));
                if (b.action.block > 0) Number(font, source + new Vector2(0, 45 - p * 20), "방어 +" + b.action.block, tint);
                Rect banner = new Rect(425, 366, 430, 40);
                Pixel(new Rect(banner.x, banner.y, banner.width, banner.height), new Color(.025f, .055f, .08f, .94f));
                Pixel(new Rect(banner.x, banner.y, 4, banner.height), tint);
                if (b.passive != null || b.rune!=null || b.gear!=null || b.card != null)
                {
                    Color oldColor = GUI.color; GUI.color = new Color(1, 1, 1, tint.a);
                    GUI.DrawTexture(new Rect(banner.x + 10, banner.y + 4, 32, 32), b.passive!=null?PixelArt.PassiveIcon(b.passive.id):b.rune!=null?PixelArt.SystemIcon(b.rune.id):b.gear!=null?PixelArt.SystemIcon(b.gear.id):PixelArt.SkillIcon(b.card.id), ScaleMode.ScaleToFit, true);
                    GUI.color = oldColor;
                }
                string identity=b.action.kind=="deferred_damage"?"유예 피해 · 아오자이":b.passive!=null?"패시브 · "+b.passive.name:b.rune!=null?"룬 · "+b.rune.name:b.gear!=null?"장비 · "+b.gear.name:b.card==null?"중독":b.card.name;
                string delayed=b.passive==null && b.rune==null && b.gear==null && !string.IsNullOrEmpty(b.action.kind)?" · "+EffectLabel(b.action.kind):"";
                bool icon=b.passive!=null || b.rune!=null || b.gear!=null || b.card!=null;
                bool displayEnemy=b.action.kind=="deferred_damage"?!b.action.enemy:b.action.enemy;
                Number(font, banner.center + new Vector2(icon ? 20 : 0, 0), (displayEnemy ? "상대  /  " : "하나  /  ") + identity + delayed, tint, identity.Length+delayed.Length>18?11:14, icon ? 368 : 420);
            }
        }

        static string EffectLabel(string kind)
        {
            if((kind??"").StartsWith("status_")) return StatusMechanics.Name(kind.Substring(7));
            switch(kind)
            {
                case "delayed_damage":return "설치물 발동";
                case "summon":return "소환물 공격";
                case "counter":return "반격";
                case "bleed":return "출혈";
                case "burn":return "화상";
                case "hot":return "지속 회복";
                case "guard":return "지속 보호";
                case "revive":return "치명상 방지";
                default:return "효과 발동";
            }
        }

        static void Spell(string effect, Vector2 source, Vector2 target, float p, Color c, int seed, int hits, bool attack)
        {
            Vector2 center = attack ? target : source;
            float travel = Mathf.Clamp01(p / .5f);
            Vector2 point = Vector2.Lerp(source, target, travel);
            float impact = Mathf.Clamp01((p - .35f) / .65f);
            switch (effect)
            {
                case "projectile":
                    for (int i = 0; i < Math.Min(5, hits); i++)
                    {
                        float q = Mathf.Clamp01((p - i * .08f) / .45f);
                        Vector2 a = Vector2.Lerp(source, target, q) + new Vector2(0, (i - hits / 2) * 14);
                        Line(a, a + new Vector2(source.x < target.x ? -66 : 66, 0), c, 5);
                        Diamond(a, 10, Color.white);
                    }
                    if (p > .35f) Impact(target, impact, c, seed);
                    break;
                case "slash": case "multi":
                    int count = effect == "multi" ? Math.Min(6, Math.Max(2, hits)) : 2;
                    for (int i = 0; i < count; i++)
                    {
                        float q = Mathf.Clamp01((p - i * .07f) * 1.8f);
                        float angle = (seed % 90 + i * 63) * Mathf.Deg2Rad;
                        Vector2 a = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Line(center - a * 85 * q, center + a * 85 * q, c, i == 0 ? 8 : 4);
                        Line(center - a * 72 * q + new Vector2(4, 4), center + a * 72 * q + new Vector2(4, 4), Color.white, 3);
                    }
                    Impact(center, p, c, seed);
                    break;
                case "explosion":
                    Diamond(point, 12 + seed % 8, c);
                    if (p > .3f)
                    {
                        Ring(center, 12 + impact * 100, c, 8, seed % 4);
                        Ring(center, 8 + impact * 63, new Color(1, .88f, .62f, c.a), 6, 0);
                        Particles(center, impact, c, seed, 28, 120);
                        if (impact < .3f) Cross(center, 34 * (1 - impact), Color.white);
                    }
                    break;
                case "lightning":
                    for (int i = 0; i < 12; i++)
                    {
                        float x = Mathf.Lerp(source.x, target.x, i / 12f);
                        float nx = Mathf.Lerp(source.x, target.x, (i + 1) / 12f);
                        float y = 282 + Mathf.Sin(i * 2.6f + seed % 13 + (int)(p * 12)) * 35;
                        float ny = 282 + Mathf.Sin((i + 1) * 2.6f + seed % 13 + (int)(p * 12)) * 35;
                        Line(new Vector2(x, y), new Vector2(nx, ny), c, 6);
                    }
                    Impact(center, p, Color.white, seed);
                    break;
                case "flame":
                    for (int i = 0; i < 25; i++)
                    {
                        float q = (p + i * .037f) % 1;
                        Vector2 a = Vector2.Lerp(source, target, q) + new Vector2(0, Mathf.Sin(i + q * 12) * (10 + q * 45));
                        Pixel(new Rect(a.x, a.y - 12 * q, 8 + i % 3 * 4, 12 + i % 4 * 4), i % 2 == 0 ? c : new Color(1, .75f, .35f, c.a));
                    }
                    Particles(center, p, c, seed, 16, 60);
                    break;
                case "ice":
                    for (int i = 0; i < 7; i++)
                    {
                        float angle = (i * 360f / 7 + seed % 45) * Mathf.Deg2Rad;
                        Vector2 a = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (20 + p * 70);
                        Diamond(a, 15 + i % 3 * 5, c);
                        Line(a + new Vector2(0, -20), a + new Vector2(0, 20), Color.white, 3);
                    }
                    Ring(center, 30 + p * 40, c, 4, 1);
                    break;
                case "arcade":
                    for (int i = 0; i < 20; i++)
                    {
                        float q = (p + i * .024f) % 1;
                        Vector2 a = Vector2.Lerp(source, target, q) + new Vector2(0, ((seed + i * 37) % 9 - 4) * 14);
                        Rect r = new Rect(a.x, a.y, 12 + i % 3 * 4, 12 + i % 3 * 4);
                        Outline(r, i % 3 == 0 ? Color.white : c, 3);
                        if (i % 4 == 0) Cross(a, 6, c);
                    }
                    if (p > .35f) { Diamond(center, impact * 75, c); Particles(center, impact, c, seed, 12, 100); }
                    break;
                case "shield": Shield(source, p, c, seed); break;
                case "heal": Heal(source, p, c, seed); break;
                case "dash":
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 a = source + new Vector2((source.x < target.x ? 1 : -1) * i * 22, i % 2 * 10 - 15);
                        Line(a, a + new Vector2(32, -28), c, 4);
                    }
                    Ring(source, 35 + p * 60, c, 4, 1);
                    if (attack) Impact(target, p, c, seed);
                    break;
                case "trap":
                    Outline(new Rect(center.x - 64, 340, 128, 30), c, 5);
                    for (int i = 0; i < 6; i++) Line(new Vector2(center.x - 55 + i * 22, 340), new Vector2(center.x - 44 + i * 22, 310 - p * 45), c, 4);
                    Particles(center, p, c, seed, 14, 70);
                    break;
                case "poison":
                    for (int i = 0; i < 14; i++)
                    {
                        Vector2 a = center + new Vector2((i * 37 + seed) % 120 - 60, 60 - ((p + i * .08f) % 1) * 145);
                        Outline(new Rect(a.x, a.y, 12, 12), c, 4);
                    }
                    break;
                default:
                    Ring(center, 45 + p * 20, c, 4, seed % 4);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (i * 72 + seed % 60 + p * 90) * Mathf.Deg2Rad;
                        float b = a + 144 * Mathf.Deg2Rad;
                        Line(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 65, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 65, c, 3);
                    }
                    Particles(center, p, c, seed, 16, 110);
                    break;
            }
        }

        static void Shield(Vector2 center, float p, Color c, int seed)
        {
            float x = center.x + (center.x < 640 ? 50 : -50), y = center.y;
            float w = 28 + p * 12;
            Line(new Vector2(x - w, y - 55), new Vector2(x + w, y - 55), c, 5);
            Line(new Vector2(x - w, y - 55), new Vector2(x - w, y + 15), c, 5);
            Line(new Vector2(x + w, y - 55), new Vector2(x + w, y + 15), c, 5);
            Line(new Vector2(x - w, y + 15), new Vector2(x, y + 55), c, 5);
            Line(new Vector2(x + w, y + 15), new Vector2(x, y + 55), c, 5);
            Cross(new Vector2(x, y - 10), 12, c);
            Particles(center, p, c, seed, 8, 70);
        }
        static void Heal(Vector2 center, float p, Color c, int seed)
        {
            for (int i = 0; i < 9; i++)
            {
                Vector2 a = center + new Vector2((i * 41 + seed) % 110 - 55, 70 - ((p + i * .08f) % 1) * 145);
                Cross(a, i % 2 == 0 ? 8 : 5, c);
            }
            Ring(center + new Vector2(0, 65), 40 + p * 30, c, 4, 1);
        }
        static void Impact(Vector2 center, float p, Color c, int seed)
        {
            Ring(center, 10 + p * 78, c, 4, seed % 4);
            Particles(center, p, c, seed, 14, 95);
        }
        static void Particles(Vector2 center, float p, Color c, int seed, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i * 137.5f + seed % 360) * Mathf.Deg2Rad;
                Vector2 at = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (12 + p * radius * (.55f + i % 5 * .1f));
                Pixel(new Rect(at.x, at.y, 4 + i % 3 * 2, 4 + i % 3 * 2), i % 5 == 0 ? Color.white : c);
            }
        }
        static void Number(GUIStyle font, Vector2 center, string text, Color color, int size = 22, int width = 250)
        {
            var style = new GUIStyle(font) { fontSize = size, alignment = TextAnchor.MiddleCenter };
            Rect r = new Rect(center.x - width / 2, center.y - 16, width, 32);
            style.normal.textColor = new Color(.02f, .04f, .06f, color.a);
            GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), text, style);
            style.normal.textColor = color; GUI.Label(r, text, style);
        }
        static void Diamond(Vector2 c, float radius, Color color)
        {
            Vector2 top = c + new Vector2(0, -radius), right = c + new Vector2(radius, 0), bottom = c + new Vector2(0, radius), left = c + new Vector2(-radius, 0);
            Line(top, right, color, 4); Line(right, bottom, color, 4); Line(bottom, left, color, 4); Line(left, top, color, 4);
        }
        static void Cross(Vector2 c, float size, Color color)
        {
            Pixel(new Rect(c.x - size, c.y - 3, size * 2, 6), color); Pixel(new Rect(c.x - 3, c.y - size, 6, size * 2), color);
        }
        static void Ring(Vector2 c, float radius, Color color, int thickness, int shape)
        {
            int sides = shape == 0 ? 20 : shape == 1 ? 8 : shape == 2 ? 12 : 6;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Line(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color, thickness);
            }
        }
        static void Outline(Rect r, Color color, int width)
        {
            Pixel(new Rect(r.x, r.y, r.width, width), color); Pixel(new Rect(r.x, r.yMax - width, r.width, width), color);
            Pixel(new Rect(r.x, r.y, width, r.height), color); Pixel(new Rect(r.xMax - width, r.y, width, r.height), color);
        }
        static float Snap(float n) => Mathf.Round(n / 3) * 3;
        static void Pixel(Rect r, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(Snap(r.x), Snap(r.y), Math.Max(3, Snap(r.width)), Math.Max(3, Snap(r.height))), Texture2D.whiteTexture);
            GUI.color = old;
        }
        static void Line(Vector2 a, Vector2 b, Color color, int width)
        {
            int steps = Math.Max(1, (int)(Vector2.Distance(a, b) / 4));
            for (int i = 0; i <= steps; i++) { Vector2 p = Vector2.Lerp(a, b, (float)i / steps); Pixel(new Rect(p.x - width / 2, p.y - width / 2, width, width), color); }
        }

        public static AudioClip SoundFor(CardDef card)
        {
            int seed = card == null ? 7 : card.effectVariant;
            int length = card != null && card.hits > 1 ? 8500 : 6000;
            var clip = AudioClip.Create("VF " + (card == null ? "status" : card.id), length, 1, 22050, false);
            var data = new float[length];
            float start = 180 + seed % 650;
            bool rising = card != null && (card.effect == "heal" || card.effect == "shield" || card.effect == "arcade");
            uint noise = (uint)Math.Max(1, seed);
            for (int i = 0; i < length; i++)
            {
                float p = (float)i / length, frequency = start * (rising ? .6f + p : 1.3f - p);
                float wave = Mathf.Sin(i * frequency / 22050f * Mathf.PI * 2) > 0 ? 1 : -1;
                noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                if (card != null && (card.effect == "explosion" || card.effect == "flame" || card.effect == "projectile")) wave = wave * .35f + ((noise & 255) / 127f - 1) * .65f;
                float pulse = card != null && card.hits > 1 ? Mathf.Abs(Mathf.Sin(p * card.hits * Mathf.PI)) : 1;
                data[i] = wave * .11f * (1 - p) * pulse;
            }
            clip.SetData(data, 0); return clip;
        }
    }
}
