using System;
using UnityEngine;

namespace Lumia
{
    public sealed partial class CombatEffects
    {
        static void Spell(CombatVisualProfile profile, CardDef card, Vector2 source, Vector2 target, float p, Color c, Color accent, int seed, bool attack)
        {
            float contact = Mathf.Clamp(profile.impactAt, .28f, .72f);
            float travel = Mathf.Clamp01((p - .08f) / Mathf.Max(.1f, contact - .08f));
            float impact = Mathf.Clamp01((p - contact) / Mathf.Max(.12f, 1 - contact));
            bool hit = p >= contact;
            float direction = target.x > source.x ? 1 : -1;
            // Gun skills can resolve their damage through mechanic pulses instead of CardDef.damage.
            bool gun = profile.motif == "bullet" || profile.motif == "barrage";
            bool offense = attack || gun && HasInstantDamage(card);
            Vector2 center = offense ? target : source;
            Vector2 point = Vector2.Lerp(source, center, travel);
            int count = Mathf.Clamp(profile.projectiles, 1, 6);
            if (p < .2f)
            {
                float charge = Mathf.Clamp01(p / .2f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = (seed % 90 + i * 120) * Mathf.Deg2Rad;
                    Vector2 at = source + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (36 - charge * 15);
                    Pixel(new Rect(at.x, at.y, 4, 4), WithAlpha(accent, c.a * charge));
                }
            }
            switch (profile.motif)
            {
                case "slash":
                    if (p > contact - .16f)
                    {
                        float q = Mathf.Clamp01((p - contact + .16f) / .31f);
                        for (int i = 0; i < Math.Min(3, count); i++)
                        {
                            float angle = (seed % 45 - 65 + i * 67) * Mathf.Deg2Rad;
                            Vector2 axis = new Vector2(Mathf.Cos(angle) * direction, Mathf.Sin(angle));
                            Vector2 normal = new Vector2(-axis.y, axis.x);
                            for (int n = 0; n < 7; n++)
                            {
                                float a = n / 7f, b = (n + 1) / 7f;
                                Vector2 first = center + axis * Mathf.Lerp(-72, 72, a) + normal * Mathf.Sin(a * Mathf.PI) * 18;
                                Vector2 last = center + axis * Mathf.Lerp(-72, 72, b) + normal * Mathf.Sin(b * Mathf.PI) * 18;
                                if (a > q) break;
                                Line(first, last, c, 7 - n / 2);
                                if (n < 5) Line(first + normal * 5, last + normal * 5, accent, 3);
                            }
                        }
                        if (hit) Sparks(center, impact, c, accent, seed, 9, 66);
                    }
                    break;
                case "thrust":
                    if (p > .13f && !hit)
                    {
                        Vector2 behind = point - new Vector2(direction * 67, 0);
                        Line(behind, point, c, 6);
                        Line(behind + new Vector2(0, 7), point, accent, 3);
                        Triangle(point, direction, 15, c);
                    }
                    if (hit) { Line(center - new Vector2(direction * 58 * (1 - impact), 0), center + new Vector2(direction * 26, 0), accent, 4); Sparks(center, impact, c, accent, seed, 7, 72); }
                    break;
                case "barrage": case "bullet":
                    if (!offense)
                    {
                        // Reload and next-basic preparations stay around the actor and never shoot the foe.
                        Vector2 magazine = source + new Vector2(direction * 43, 20 - Mathf.Sin(p * Mathf.PI) * 13);
                        Outline(new Rect(magazine.x - 8, magazine.y - 15, 16, 30), c, 3);
                        for (int i = 0; i < 3; i++) Pixel(new Rect(magazine.x - 3, magazine.y - 9 + i * 8, 6, 4), accent);
                        Line(magazine + new Vector2(-direction * 13, -23), magazine + new Vector2(-direction * 13, 10), c, 3);
                        Triangle(magazine + new Vector2(-direction * 13, -23), -1, 7, accent);
                        break;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        float q = Mathf.Clamp01((p - .08f - i * .035f) / Mathf.Max(.16f, contact - .08f));
                        Vector2 bullet = Vector2.Lerp(source, center, q) + new Vector2(0, (i - (count - 1) / 2f) * 16);
                        if (q < 1 && p > .08f + i * .035f)
                        {
                            Line(bullet - new Vector2(direction * 47, 0), bullet, c, profile.motif == "barrage" ? 4 : 6);
                            Pixel(new Rect(bullet.x - 5, bullet.y - 3, 11, 6), accent);
                            if (i == 0) Muzzle(source, direction, p, accent);
                        }
                    }
                    if (hit)
                    {
                        GunImpact(source, center, p - contact, direction, profile.motif == "barrage" ? count : Math.Min(2, count), c, accent, seed);
                        Sparks(center, impact, c, accent, seed, profile.motif == "barrage" ? 13 : 7, 67);
                    }
                    break;
                case "sniper":
                    if (!hit)
                    {
                        float aim = Mathf.Clamp01(p / contact);
                        Line(source, center, WithAlpha(c, c.a * .52f), 2);
                        Corners(new Rect(center.x - 38 + aim * 7, center.y - 38 + aim * 7, 76 - aim * 14, 76 - aim * 14), c, 11);
                        Cross(center, 10, accent);
                    }
                    else
                    {
                        if (impact < .28f) { Line(source, center, c, 6); Line(source, center, accent, 3); }
                        Sparks(center, impact, c, accent, seed, 12, 100);
                    }
                    break;
                case "arrow":
                    for (int i = 0; i < Math.Min(4, count); i++)
                    {
                        float q = Mathf.Clamp01((p - i * .045f - .06f) / Mathf.Max(.18f, contact - .06f));
                        Vector2 at = Vector2.Lerp(source, center, q) + new Vector2(0, (i - (Math.Min(4, count) - 1) / 2f) * 23 - Mathf.Sin(q * Mathf.PI) * 17);
                        if (q < 1 && p > i * .045f + .06f)
                        {
                            Vector2 tail = at - new Vector2(direction * 47, 0);
                            Line(tail, at, accent, 3); Triangle(at, direction, 11, c);
                            Line(tail, tail - new Vector2(direction * 12, 9), c, 4); Line(tail, tail - new Vector2(direction * 12, -9), c, 4);
                        }
                    }
                    if (hit) { Feather(center, direction, impact, c, accent); Sparks(center, impact, c, accent, seed, 7, 50); }
                    break;
                case "bomb":
                    if (!hit)
                    {
                        Vector2 toss = point + new Vector2(0, -Mathf.Sin(travel * Mathf.PI) * 80);
                        Octagon(toss, 13, c, 5); Pixel(new Rect(toss.x - 6, toss.y - 20, 9, 7), accent);
                        Line(toss + new Vector2(2, -20), toss + new Vector2(8, -29), c, 3);
                        Star(toss + new Vector2(8, -31), 5, accent, 4, p * 90);
                    }
                    else
                    {
                        float radius = 20 + impact * 86;
                        Star(center, radius * (1 - impact * .45f), c, 8, seed % 20); Star(center, radius * .52f, accent, 6, 20);
                        Sparks(center, impact, c, accent, seed, 17, 113);
                        for (int i = 0; i < 4; i++) Octagon(center + new Vector2((i - 1.5f) * 25, 20 - impact * 40), 9 + impact * 15, WithAlpha(c, c.a * .5f), 3);
                    }
                    break;
                case "flame":
                    Flames(point, p, c, accent, seed, 11, hit ? 66 : 34);
                    if (p > .1f && !hit)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float q = Mathf.Clamp01(travel - i * .06f);
                            Vector2 at = Vector2.Lerp(source, center, q) + new Vector2(0, Mathf.Sin(i + p * 8) * 13);
                            Flame(at, 8 + i * 2, c, accent);
                        }
                    }
                    if (hit) Flames(center, impact, c, accent, seed + 7, 10, 70);
                    break;
                case "lightning":
                    if (p > .14f && p < contact + .3f)
                    {
                        Vector2 previous = source;
                        int tick = (int)(p * 18);
                        for (int i = 1; i <= 12; i++)
                        {
                            float q = i / 12f;
                            Vector2 next = Vector2.Lerp(source, center, q) + new Vector2(0, i == 12 ? 0 : ((seed + i * 17 + tick * 7) % 5 - 2) * 12);
                            Line(previous, next, c, 6); Line(previous, next, accent, 3);
                            if (i % 4 == 0) Line(next, next + new Vector2(direction * 22, -24), c, 3);
                            previous = next;
                        }
                    }
                    if (hit) { Star(center, 34 * (1 - impact), accent, 6, 0); Sparks(center, impact, c, accent, seed, 9, 75); }
                    break;
                case "ice":
                    if (!hit)
                    {
                        for (int i = 0; i < Math.Min(4, count + 1); i++) Crystal(point + new Vector2(-direction * i * 19, (i - 1.5f) * 16), 10 + i * 2, c, accent, direction);
                    }
                    else
                    {
                        for (int i = 0; i < 6; i++) Crystal(center + Radial(i * 60 + seed % 30) * (15 + impact * 57), 18 * (1 - impact * .5f), c, accent, i % 2 == 0 ? 1 : -1);
                        Snowflake(center, 31 + impact * 23, accent);
                    }
                    break;
                case "arcade":
                    if (!hit)
                    {
                        for (int i = 0; i < Math.Min(4, count + 1); i++)
                        {
                            float q = Mathf.Clamp01(travel - i * .07f);
                            Vector2 at = Vector2.Lerp(source, center, q) + new Vector2(0, (i - 1.5f) * 23);
                            Blocks(at, 10, (seed + i) % 4, i % 2 == 0 ? c : accent, p * direction);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 at = center + new Vector2((i % 4 - 1.5f) * (15 + impact * 22), (i / 4 - 1) * (15 + impact * 22));
                            Outline(new Rect(at.x, at.y, 10, 10), i % 3 == 0 ? accent : c, 3);
                        }
                        Star(center, 22 * (1 - impact), accent, 4, 0);
                    }
                    break;
                case "orb":
                    if (!hit)
                    {
                        Octagon(point, 16 + Mathf.Sin(p * 12) * 3, c, 5); Octagon(point, 8, accent, 3);
                        for (int i = 0; i < 3; i++) Pixel(new Rect(point.x - direction * (24 + i * 13), point.y - 3 + i % 2 * 6, 6, 6), WithAlpha(c, c.a * (.7f - i * .15f)));
                    }
                    else { Octagon(center, 18 + impact * 48, c, 5); for (int i = 0; i < 5; i++) Diamond(center + Radial(i * 72 + p * 45) * (20 + impact * 45), 8, accent); }
                    break;
                case "sigil":
                    Sigil(center, p, c, accent, seed);
                    if (hit) { Line(center + new Vector2(0, -95), center + new Vector2(0, 60), accent, 5); Sparks(center, impact, c, accent, seed, 9, 70); }
                    break;
                case "trap":
                    {
                        Vector2 floor = new Vector2(center.x, 342);
                        float close = hit ? 1 - impact : travel;
                        Outline(new Rect(floor.x - 52, floor.y - 8, 104, 25), c, 4);
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 a = floor + new Vector2(-44 + i * 22, 0);
                            Line(a, a + new Vector2(9, -18 - close * 24), c, 4); Line(a + new Vector2(9, -18 - close * 24), a + new Vector2(18, 0), accent, 3);
                        }
                        if (hit) Sparks(center + new Vector2(0, 30), impact, c, accent, seed, 8, 60);
                    }
                    break;
                case "flower":
                    {
                        Vector2 stem = center + new Vector2(0, 47);
                        Line(stem + new Vector2(0, 28), stem - new Vector2(0, 31), WithAlpha(new Color(.35f, .68f, .45f), c.a), 5);
                        Line(stem, stem + new Vector2(-20, -13), c, 5); Line(stem + new Vector2(0, 10), stem + new Vector2(18, -3), c, 5);
                        Flower(center + new Vector2(0, 5), 20 + (hit ? impact * 14 : travel * 7), c, accent, seed % 60 + p * 35);
                        if (hit) for (int i = 0; i < 8; i++) Petal(center + Radial(i * 45 + seed) * (24 + impact * 65), 7, i * 45, c, accent);
                    }
                    break;
                case "water":
                    for (int i = 0; i < 3; i++) Wave(point + new Vector2(-direction * i * 34, 30 + i * 14), 30 + i * 7, direction, c, accent);
                    if (hit) { for (int i = 0; i < 8; i++) Drop(center + Radial(i * 45) * (16 + impact * 67) + new Vector2(0, impact * 13), 6, c, accent); Wave(center + new Vector2(0, 48), 55 + impact * 30, direction, c, accent); }
                    break;
                case "wind":
                    for (int i = 0; i < 3; i++) Crescent(point + new Vector2(-direction * i * 19, (i - 1) * 15), 30 + i * 7, direction, c, accent);
                    if (hit) for (int i = 0; i < 3; i++) Arc(center, 24 + impact * 55 + i * 10, p * 180 + i * 100, 190, c, 3);
                    break;
                case "earth":
                    for (int i = 0; i < 5; i++)
                    {
                        float q = hit ? impact : travel;
                        Vector2 at = center + new Vector2((i - 2) * 24, 58 - q * (15 + i % 3 * 12));
                        Crystal(at, (10 + i % 3 * 6) * Mathf.Clamp01(p * 6), c, accent, i % 2 == 0 ? 1 : -1);
                        Pixel(new Rect(at.x - 10, 360, 20, 4), c);
                    }
                    if (hit) Sparks(center + new Vector2(0, 35), impact, c, accent, seed, 9, 78);
                    break;
                case "blood":
                    if (!hit) { Line(point - new Vector2(direction * 42, 14), point, c, 5); Drop(point, 9, c, accent); }
                    if (hit) { Drops(center, impact, c, seed, 12, 76); Line(center - new Vector2(33, -36), center + new Vector2(33, -36), accent, 4); }
                    break;
                case "poison":
                    if (!hit) { Bubbles(point, p, c, seed, 6, 26); Line(point - new Vector2(direction * 30, 0), point, c, 3); }
                    if (hit) { Bubbles(center, impact, c, seed, 11, 64); Pixel(new Rect(center.x - 45 - impact * 15, 353, 90 + impact * 30, 7), WithAlpha(c, c.a * .7f)); }
                    break;
                // Remaining subjects use purpose-built geometry below.
                default: OtherSpell(profile, card, source, center, point, p, impact, travel, hit, direction, count, c, accent, seed, attack); break;
            }
        }

        static void OtherSpell(CombatVisualProfile profile, CardDef card, Vector2 source, Vector2 center, Vector2 point,
            float p, float impact, float travel, bool hit, float direction, int count, Color c, Color accent, int seed, bool attack)
        {
            switch (profile.motif)
            {
                case "music":
                    for (int i = 0; i < 3; i++) Line(point + new Vector2(-28, -20 + i * 15), point + new Vector2(42, -20 + i * 15), WithAlpha(c, c.a * .45f), 2);
                    for (int i = 0; i < 4; i++) Note((hit ? center : point) + new Vector2((i - 1.5f) * 26, Mathf.Sin(p * 8 + i) * 20 - (hit ? impact * 38 : 0)), i % 2 == 0 ? c : accent, i % 2 == 0);
                    if (hit) Arc(center, 42 + impact * 37, 210, 120, c, 4);
                    break;
                case "star":
                    if (!hit) { Star(point, 18 + Mathf.Sin(p * 10) * 3, c, 5, p * 95); Star(point, 8, accent, 4, -p * 80); }
                    else for (int i = 0; i < 6; i++) Star(center + Radial(i * 60 + seed) * (12 + impact * 70), 8 + i % 2 * 5, i % 2 == 0 ? c : accent, i % 2 == 0 ? 5 : 4, i * 17);
                    break;
                case "puppet":
                    {
                        float sway = Mathf.Sin(p * 8) * 12;
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 knot = center + new Vector2((i - 2) * 22 + sway, i % 2 * 18 + 10);
                            Line(new Vector2(center.x + (i - 2) * 31, 183), knot, i % 2 == 0 ? c : accent, 2);
                            Diamond(knot, 5, c);
                        }
                        Line(center + new Vector2(-49, -88), center + new Vector2(49, -88), c, 5);
                        if (hit) { Line(center + new Vector2(-50, 20), center + new Vector2(50, -23), accent, 4); Sparks(center, impact, c, accent, seed, 7, 60); }
                    }
                    break;
                case "shield": Shield(source, p, c, seed, true); if (hit && attack) Sparks(center, impact, c, accent, seed, 7, 55); break;
                case "heal": Heal(source, p, c, seed); break;
                case "chain":
                    {
                        int links = Math.Max(1, (int)(Vector2.Distance(source, point) / 23));
                        for (int i = 0; i < Math.Min(35, links); i++)
                        {
                            Vector2 at = Vector2.Lerp(source, point, i / (float)links) + new Vector2(0, Mathf.Sin(i * .7f + p * 7) * 12);
                            Diamond(at, i % 2 == 0 ? 9 : 6, i % 2 == 0 ? c : accent);
                        }
                        if (hit) { Diamond(center, 30 + impact * 22, c); Line(center - new Vector2(27, 27), center + new Vector2(27, 27), accent, 4); }
                    }
                    break;
                case "prism":
                    {
                        bool screen = card != null && (card.id == "theodore_w" || card.id == "coraline_w");
                        Vector2 prism = screen ? source + new Vector2(direction * 52, 0) : attack ? point : source + new Vector2(direction * 55, 0);
                        if (screen)
                        {
                            Outline(new Rect(prism.x - 20, prism.y - 52, 40, 105), c, 4);
                            Line(prism + new Vector2(-20, -52), prism + new Vector2(20, 53), accent, 3);
                            for (int i = 0; i < 4; i++) Line(prism + new Vector2(-15, -35 + i * 23), prism + new Vector2(15, -18 + i * 23), WithAlpha(accent, c.a * .65f), 3);
                        }
                        else { TriangleUp(prism, 30, c, accent); Line(prism + new Vector2(-25, 25), prism + new Vector2(25, -10), accent, 3); }
                        if (hit && attack && !screen)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                Vector2 at = center + new Vector2(0, (i - 1) * 21);
                                Line(source + new Vector2(direction * 40, 0), at, i == 1 ? accent : c, 3);
                                Star(at, 12 * (1 - impact) + 4, i == 1 ? c : accent, 4, 0);
                            }
                        }
                    }
                    break;
                case "camera":
                    {
                        bool panorama = card != null && card.key == "E";
                        float zoom = 1 - travel * .22f;
                        float width = panorama ? 145 : card != null && card.key == "R" ? 150 : 124;
                        Corners(new Rect(center.x - width * .5f * zoom, center.y - 47 * zoom, width * zoom, 94 * zoom), c, 18);
                        Cross(center, 7, accent);
                        if (panorama) Line(center + new Vector2(-width * .5f + travel * width, -40), center + new Vector2(-width * .5f + travel * width, 40), accent, 3);
                        if (hit)
                        {
                            Octagon(center, 34 * (1 - impact) + 9, accent, 4);
                            for (int i = 0; i < 6; i++) Line(center + Radial(i * 60) * (8 + impact * 17), center + Radial(i * 60 + 35) * 35, c, 3);
                        }
                    }
                    break;
                case "chess":
                    {
                        Vector2 at = hit ? center : point + new Vector2(0, -Mathf.Sin(travel * Mathf.PI) * 44);
                        int variant = card == null ? seed % 4 : card.key == "W" ? 1 : card.key == "E" ? 2 : card.key == "R" ? 3 : 0;
                        ChessPiece(at, variant, c, accent);
                        if (hit)
                        {
                            int tiles = variant == 3 ? 20 : 12;
                            for (int i = 0; i < tiles; i++) if ((i + i / 4) % 2 == 0)
                                Pixel(new Rect(center.x - 54 + i % 4 * 27, 317 + i / 4 * 12, 27, 12), WithAlpha(c, c.a * .5f));
                            Sparks(center, impact, c, accent, seed, 7, 65);
                        }
                    }
                    break;
                case "time":
                    {
                        Vector2 at = hit ? center : point;
                        Octagon(at, 37, c, 4);
                        for (int i = 0; i < 8; i++) { Vector2 a = Radial(i * 45); Line(at + a * 30, at + a * 35, accent, 3); }
                        Line(at, at + Radial(-90 + p * 280) * 24, accent, 4); Line(at, at + Radial(p * 110) * 17, c, 4);
                        if (hit) { Outline(new Rect(at.x - 15, at.y - 24, 30, 48), accent, 3); Line(at + new Vector2(-14, -20), at + new Vector2(14, 20), c, 3); Line(at + new Vector2(14, -20), at + new Vector2(-14, 20), c, 3); }
                    }
                    break;
                case "paper":
                    for (int i = 0; i < Math.Min(5, count + 1); i++)
                    {
                        float q = hit ? impact : travel;
                        Vector2 at = (hit ? center : point) + new Vector2((i - 2) * (12 + q * 17), Mathf.Sin(p * 7 + i) * 25 - (hit ? impact * 22 : 0));
                        Paper(at, i % 2 == 0 ? c : accent, i % 2 == 0 ? accent : c, i);
                    }
                    break;
                case "blade_orbit":
                    {
                        Vector2 around = attack && hit ? center : source;
                        for (int i = 0; i < Math.Min(6, count + 2); i++)
                        {
                            float angle = i * 360f / Math.Min(6, count + 2) + p * 170 * direction;
                            Vector2 at = around + Radial(angle) * (42 + impact * 27), axis = Radial(angle + 90);
                            Line(at - axis * 16, at + axis * 16, c, 5); Line(at - axis * 10 + Radial(angle) * 4, at + axis * 15, accent, 3); Diamond(at + axis * 18, 5, accent);
                        }
                        if (hit && attack) Sparks(center, impact, c, accent, seed, 8, 65);
                    }
                    break;
                default: Sigil(center, p, c, accent, seed); break;
            }
        }

        static bool HasInstantDamage(CardDef card)
        {
            if (card == null || card.mechanics == null || card.mechanics.rules == null) return false;
            foreach (var rule in card.mechanics.rules)
                if (rule != null && rule.amount > 0 && (rule.op == "bonus_damage" || rule.op == "flat_damage" || rule.op == "damage")) return true;
            return false;
        }

        static void GunImpact(Vector2 source, Vector2 target, float elapsed, float direction, int shots, Color c, Color accent, int seed)
        {
            for (int i = 0; i < Math.Min(6, shots); i++)
            {
                float age = elapsed - i * .025f;
                if (age < 0 || age >= .28f) continue;
                float energy = 1 - age / .28f;
                float spread = shots == 1 ? 0 : (i - (shots - 1) * .5f) * 23;
                Vector2 at = target + new Vector2(direction * (((seed + i * 13) % 3) - 1) * 8, spread);
                Color trail = WithAlpha(c, c.a * energy * .86f), core = WithAlpha(accent, accent.a * energy);
                // The short outgoing trace persists after travel, making each gunshot readable at impact.
                Vector2 tail = at - new Vector2(direction * (115 + energy * 40), 0);
                Line(tail, at + new Vector2(direction * 13, 0), trail, 5);
                Line(tail + new Vector2(direction * 29, 0), at + new Vector2(direction * 10, 0), core, 3);
                float radius = 22 + energy * 27;
                for (int ray = 0; ray < 6; ray++)
                {
                    Vector2 axis = Radial(ray * 60 + i * 19 + 30);
                    Line(at + axis * 10, at + axis * radius, ray % 2 == 0 ? trail : core, ray % 2 == 0 ? 4 : 3);
                }
                Cross(at, 7 + energy * 10, core);
                Vector2 muzzle = source + new Vector2(direction * 49, spread * .3f);
                Line(muzzle, muzzle + new Vector2(direction * (15 + energy * 15), -11 * energy), core, 3);
                Line(muzzle, muzzle + new Vector2(direction * (15 + energy * 15), 11 * energy), trail, 4);
            }
        }

        static void Muzzle(Vector2 source, float direction, float p, Color c)
        {
            if (p > .32f) return;
            Vector2 at = source + new Vector2(direction * 43, 0);
            Line(at, at + new Vector2(direction * 21, -13), c, 4); Line(at, at + new Vector2(direction * 27, 0), c, 4); Line(at, at + new Vector2(direction * 21, 13), c, 4);
        }
        static void Feather(Vector2 at, float direction, float p, Color c, Color accent)
        {
            Vector2 tail = at - new Vector2(direction * (44 - p * 10), 0);
            Line(tail, at + new Vector2(direction * 8, 0), accent, 3);
            Line(tail, tail - new Vector2(direction * 12, 9), c, 4); Line(tail, tail - new Vector2(direction * 12, -9), c, 4);
        }
        static void Triangle(Vector2 at, float direction, float radius, Color c)
        {
            Line(at, at - new Vector2(direction * radius, radius * .6f), c, 4); Line(at, at - new Vector2(direction * radius, -radius * .6f), c, 4);
        }
        static void TriangleUp(Vector2 at, float radius, Color c, Color accent)
        {
            Vector2 top = at + new Vector2(0, -radius), left = at + new Vector2(-radius, radius * .8f), right = at + new Vector2(radius, radius * .8f);
            Line(top, left, c, 4); Line(left, right, c, 4); Line(right, top, accent, 4); Line(top, at, accent, 3); Line(at, left, accent, 3); Line(at, right, accent, 3);
        }
        static void Crystal(Vector2 at, float radius, Color c, Color accent, float direction)
        {
            Vector2 top = at + new Vector2(direction * radius * .25f, -radius), bottom = at + new Vector2(-direction * radius * .25f, radius);
            Vector2 left = at + new Vector2(-radius * .55f, 0), right = at + new Vector2(radius * .55f, 0);
            Line(top, right, c, 4); Line(right, bottom, c, 4); Line(bottom, left, c, 4); Line(left, top, c, 4); Line(top, bottom, accent, 3);
        }
        static void Snowflake(Vector2 at, float radius, Color c)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 axis = Radial(i * 60), side = Radial(i * 60 + 90), tip = at + axis * radius;
                Line(at, tip, c, 3); Line(at + axis * radius * .62f, tip - axis * 9 + side * 8, c, 3); Line(at + axis * radius * .62f, tip - axis * 9 - side * 8, c, 3);
            }
        }
        static void Blocks(Vector2 at, float size, int variant, Color c, float wiggle)
        {
            at.y += Mathf.Sin(wiggle * 9) * 4;
            for (int i = 0; i < 4; i++)
            {
                float x = variant == 0 ? i % 2 : variant == 1 ? i : variant == 2 ? i % 3 : i % 2 + i / 2;
                float y = variant == 0 ? i / 2 : variant == 1 ? 0 : variant == 2 ? i / 3 : i / 2;
                Rect r = new Rect(at.x + (x - 1) * size, at.y + (y - .5f) * size, size, size);
                Outline(r, c, 3); Pixel(new Rect(r.x + 3, r.y + 3, 3, 3), WithAlpha(Color.white, c.a));
            }
        }
        static void Sigil(Vector2 at, float p, Color c, Color accent, int seed)
        {
            float radius = 32 + Mathf.Clamp01(p * 3) * 19;
            Octagon(at, radius, c, 3);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60 + seed % 30 + p * 30;
                Vector2 a = at + Radial(angle) * radius, b = at + Radial(angle + 120) * radius;
                Line(a, b, c, 3); Pixel(new Rect(a.x - 3, a.y - 3, 6, 6), accent);
            }
            Diamond(at, 12, accent);
        }
        static void Flower(Vector2 at, float radius, Color c, Color accent, float angle)
        {
            for (int i = 0; i < 5; i++) Petal(at + Radial(i * 72 + angle) * radius * .65f, radius * .55f, i * 72 + angle, c, accent);
            Octagon(at, 7, accent, 4);
        }
        static void Petal(Vector2 at, float radius, float angle, Color c, Color accent)
        {
            Vector2 axis = Radial(angle), side = Radial(angle + 90);
            Line(at - axis * radius, at + side * radius * .55f, c, 4); Line(at + side * radius * .55f, at + axis * radius, c, 4);
            Line(at + axis * radius, at - side * radius * .55f, c, 4); Line(at - side * radius * .55f, at - axis * radius, c, 4); Line(at, at + axis * radius * .6f, accent, 3);
        }
        static void Wave(Vector2 at, float radius, float direction, Color c, Color accent)
        {
            Vector2 previous = at + new Vector2(-direction * radius, 8);
            for (int i = 1; i <= 8; i++)
            {
                float q = i / 8f;
                Vector2 next = at + new Vector2(direction * Mathf.Lerp(-radius, radius, q), 8 - Mathf.Sin(q * Mathf.PI) * radius * .64f);
                Line(previous, next, c, 5);
                if (i > 4) Line(previous + new Vector2(0, 7), next + new Vector2(0, 7), accent, 3);
                previous = next;
            }
            Line(previous, previous + new Vector2(-direction * radius * .35f, -radius * .3f), accent, 4);
        }
        static void Crescent(Vector2 at, float radius, float direction, Color c, Color accent)
        {
            Arc(at, radius, direction > 0 ? -65 : 115, 130, c, 5); Arc(at - new Vector2(direction * 8, 0), radius - 8, direction > 0 ? -53 : 127, 106, accent, 3);
        }
        static void Flame(Vector2 at, float radius, Color c, Color accent)
        {
            Vector2 top = at + new Vector2(radius * .25f, -radius * 1.5f), left = at + new Vector2(-radius * .65f, 0), bottom = at + new Vector2(0, radius * .4f), right = at + new Vector2(radius * .6f, -radius * .2f);
            Line(top, left, c, 5); Line(left, bottom, c, 5); Line(bottom, right, c, 5); Line(right, top, c, 5); Line(bottom, at + new Vector2(0, -radius * .65f), accent, 4);
        }
        static void Flames(Vector2 at, float p, Color c, Color accent, int seed, int count, float radius)
        {
            for (int i = 0; i < Math.Min(12, count); i++)
            {
                float q = (p + i * .071f) % 1;
                Vector2 a = at + new Vector2(((seed + i * 37) % 13 - 6) / 6f * radius * .65f, 24 - q * radius * 1.5f);
                Flame(a, 6 + i % 3 * 4, i % 2 == 0 ? c : accent, i % 2 == 0 ? accent : c);
            }
        }
        static void Drop(Vector2 at, float radius, Color c, Color accent)
        {
            Line(at + new Vector2(0, -radius), at + new Vector2(-radius * .65f, radius * .45f), c, 4); Line(at + new Vector2(-radius * .65f, radius * .45f), at + new Vector2(0, radius), c, 4);
            Line(at + new Vector2(0, radius), at + new Vector2(radius * .65f, radius * .45f), c, 4); Line(at + new Vector2(radius * .65f, radius * .45f), at + new Vector2(0, -radius), c, 4);
            Pixel(new Rect(at.x - 3, at.y, 3, 3), accent);
        }
        static void Drops(Vector2 at, float p, Color c, int seed, int count, float radius)
        {
            for (int i = 0; i < Math.Min(12, count); i++) Drop(at + Radial(i * 137.5f + seed % 360) * (9 + p * radius) + new Vector2(0, p * p * 30), 5 + i % 3 * 2, c, WithAlpha(Color.white, c.a * .6f));
        }
        static void Bubbles(Vector2 at, float p, Color c, int seed, int count, float radius)
        {
            for (int i = 0; i < Math.Min(12, count); i++)
            {
                float q = (p + i * .077f) % 1;
                Vector2 a = at + new Vector2(((seed + i * 23) % 11 - 5) / 5f * radius * .7f, 31 - q * radius * 1.6f);
                Octagon(a, 5 + i % 3 * 3, c, 3); Pixel(new Rect(a.x - 2, a.y - 3, 3, 3), WithAlpha(Color.white, c.a * .5f));
            }
        }
        static void Note(Vector2 at, Color c, bool doubleNote)
        {
            Pixel(new Rect(at.x - 6, at.y + 6, 12, 8), c); Line(at + new Vector2(5, 9), at + new Vector2(5, -20), c, 3); Line(at + new Vector2(5, -20), at + new Vector2(19, -25), c, 4);
            if (doubleNote) { Line(at + new Vector2(19, -25), at + new Vector2(19, 3), c, 3); Pixel(new Rect(at.x + 9, at.y + 1, 11, 8), c); }
            else Line(at + new Vector2(19, -25), at + new Vector2(18, -13), c, 3);
        }
        static void Paper(Vector2 at, Color c, Color accent, int variant)
        {
            Outline(new Rect(at.x - 10, at.y - 18, 20, 36), c, 3);
            Line(at + new Vector2(-5, -9), at + new Vector2(5, -9), accent, 3); Line(at + new Vector2(0, -9), at + new Vector2(0, 10), accent, 3);
            Line(at + new Vector2(-5, 1 + variant % 2 * 4), at + new Vector2(5, 1 + variant % 2 * 4), accent, 3);
        }
        static void ChessPiece(Vector2 at, int variant, Color c, Color accent)
        {
            Pixel(new Rect(at.x - 22, at.y + 24, 44, 6), c); Pixel(new Rect(at.x - 18, at.y + 16, 36, 6), accent);
            Line(at + new Vector2(-15, 14), at + new Vector2(-8, -12), c, 5); Line(at + new Vector2(15, 14), at + new Vector2(8, -12), c, 5);
            if (variant == 0) Octagon(at + new Vector2(0, -22), 10, c, 4);
            else if (variant == 1)
            {
                Line(at + new Vector2(-8, -11), at + new Vector2(-15, -30), c, 5); Line(at + new Vector2(-15, -30), at + new Vector2(3, -37), c, 5);
                Line(at + new Vector2(3, -37), at + new Vector2(19, -20), c, 5); Line(at + new Vector2(19, -20), at + new Vector2(5, -15), c, 5); Pixel(new Rect(at.x + 2, at.y - 29, 3, 3), accent);
            }
            else if (variant == 2)
            {
                Outline(new Rect(at.x - 19, at.y - 34, 38, 21), c, 4); for (int i = 0; i < 3; i++) Pixel(new Rect(at.x - 18 + i * 13, at.y - 39, 9, 10), c);
            }
            else { Diamond(at + new Vector2(0, -21), 11, c); Cross(at + new Vector2(0, -40), 9, accent); }
        }
        static void Shield(Vector2 at, float p, Color c, int seed, bool particles)
        {
            float direction = at.x < 640 ? 1 : -1;
            Vector2 center = at + new Vector2(direction * 45, 0);
            float w = 26 + Mathf.Clamp01(p * 3) * 10;
            Line(center + new Vector2(-w, -49), center + new Vector2(w, -49), c, 5); Line(center + new Vector2(-w, -49), center + new Vector2(-w, 13), c, 5); Line(center + new Vector2(w, -49), center + new Vector2(w, 13), c, 5);
            Line(center + new Vector2(-w, 13), center + new Vector2(0, 48), c, 5); Line(center + new Vector2(w, 13), center + new Vector2(0, 48), c, 5); Cross(center + new Vector2(0, -8), 12, c);
            if (particles) for (int i = 0; i < 4; i++) Diamond(center + new Vector2((i - 1.5f) * 24, 50 - ((p + i * .15f) % 1) * 95), 4, c);
        }
        static void Heal(Vector2 at, float p, Color c, int seed)
        {
            for (int i = 0; i < 6; i++)
            {
                float q = (p + i * .13f) % 1;
                Vector2 a = at + new Vector2((i * 31 + seed) % 90 - 45, 49 - q * 108);
                Cross(a, i % 2 == 0 ? 8 : 5, c);
            }
            Arc(at + new Vector2(0, 52), 40, 10, 160, c, 3); Arc(at + new Vector2(0, 52), 40, 190, 160, c, 3);
        }
        static void Sparks(Vector2 at, float p, Color c, Color accent, int seed, int count, float radius)
        {
            for (int i = 0; i < Math.Min(18, count); i++)
            {
                Vector2 direction = Radial(i * 137.5f + seed % 360);
                Vector2 a = at + direction * (8 + p * radius * (.6f + i % 4 * .12f));
                Line(a, a + direction * (5 + (1 - p) * 12), i % 3 == 0 ? accent : c, i % 4 == 0 ? 5 : 3);
            }
        }
        static void Particles(Vector2 at, float p, Color c, int seed, int count, float radius)
        {
            for (int i = 0; i < Math.Min(16, count); i++)
            {
                Vector2 a = at + Radial(i * 137.5f + seed % 360) * (12 + p * radius * (.6f + i % 4 * .12f));
                Pixel(new Rect(a.x, a.y, 3 + i % 3 * 3, 3 + i % 3 * 3), c);
            }
        }
        static void Diamond(Vector2 at, float radius, Color c)
        {
            Vector2 top = at + new Vector2(0, -radius), right = at + new Vector2(radius, 0), bottom = at + new Vector2(0, radius), left = at + new Vector2(-radius, 0);
            Line(top, right, c, 3); Line(right, bottom, c, 3); Line(bottom, left, c, 3); Line(left, top, c, 3);
        }
        static void Cross(Vector2 at, float size, Color c)
        {
            Pixel(new Rect(at.x - size, at.y - 3, size * 2, 6), c); Pixel(new Rect(at.x - 3, at.y - size, 6, size * 2), c);
        }
        static Vector2 Radial(float angle) { return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)); }
        static void Star(Vector2 at, float radius, Color c, int points, float angle)
        {
            points = Math.Min(8, Math.Max(4, points));
            for (int i = 0; i < points * 2; i++)
            {
                float a = angle - 90 + i * 180f / points, b = angle - 90 + (i + 1) * 180f / points;
                Line(at + Radial(a) * radius * (i % 2 == 0 ? 1 : .38f), at + Radial(b) * radius * (i % 2 == 0 ? .38f : 1), c, 3);
            }
        }
        static void Octagon(Vector2 at, float radius, Color c, int width)
        {
            for (int i = 0; i < 8; i++) Line(at + Radial(22.5f + i * 45) * radius, at + Radial(22.5f + (i + 1) * 45) * radius, c, width);
        }
        static void Arc(Vector2 at, float radius, float start, float degrees, Color c, int width)
        {
            int segments = Math.Max(4, (int)(Mathf.Abs(degrees) / 16));
            for (int i = 0; i < segments; i++) Line(at + Radial(start + degrees * i / segments) * radius, at + Radial(start + degrees * (i + 1) / segments) * radius, c, width);
        }
        static void Corners(Rect r, Color c, float length)
        {
            Line(new Vector2(r.x, r.y), new Vector2(r.x + length, r.y), c, 3); Line(new Vector2(r.x, r.y), new Vector2(r.x, r.y + length), c, 3);
            Line(new Vector2(r.xMax, r.y), new Vector2(r.xMax - length, r.y), c, 3); Line(new Vector2(r.xMax, r.y), new Vector2(r.xMax, r.y + length), c, 3);
            Line(new Vector2(r.x, r.yMax), new Vector2(r.x + length, r.yMax), c, 3); Line(new Vector2(r.x, r.yMax), new Vector2(r.x, r.yMax - length), c, 3);
            Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - length, r.yMax), c, 3); Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMax - length), c, 3);
        }
        static void Outline(Rect r, Color c, int width)
        {
            Pixel(new Rect(r.x, r.y, r.width, width), c); Pixel(new Rect(r.x, r.yMax - width, r.width, width), c);
            Pixel(new Rect(r.x, r.y, width, r.height), c); Pixel(new Rect(r.xMax - width, r.y, width, r.height), c);
        }
        static float Snap(float n) { return Mathf.Round(n / 3) * 3; }
        static void Pixel(Rect r, Color c)
        {
            if (c.a <= .01f) return;
            float x = Snap(r.x), y = Snap(r.y), width = Math.Max(3, Snap(r.width)), height = Math.Max(3, Snap(r.height));
            // Geometry cannot spill into the hand, header, field strip or navigation.
            float left = Mathf.Max(18, x), top = Mathf.Max(166, y), right = Mathf.Min(1262, x + width), bottom = Mathf.Min(438, y + height);
            if (right <= left || bottom <= top) return;
            Color old = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(left, top, right - left, bottom - top), Texture2D.whiteTexture);
            GUI.color = old;
        }
        static void Line(Vector2 a, Vector2 b, Color c, int width)
        {
            if (c.a <= .01f) return;
            int steps = Math.Min(180, Math.Max(1, (int)(Vector2.Distance(a, b) / 4)));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                Pixel(new Rect(p.x - width / 2f, p.y - width / 2f, width, width), c);
            }
        }
    }
}
