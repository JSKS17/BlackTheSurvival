using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    /// <summary>Short, mirrored pixel choreography. Rendering never changes combat resolution.</summary>
    public sealed partial class CombatEffects
    {
        sealed class Burst
        {
            public CombatAction action;
            public CardDef card;
            public PassiveDef passive;
            public RuneDef rune;
            public GearDef gear;
            public CombatVisualProfile profile;
            public UltimateAnimationDef ultimate;
            public bool direct, reduced;
            public float start, duration, textUntil;
            public int damageLane = -1, healLane = -1, blockLane = -1, statusLane = -1;
            public Color color, accent;
            public int seed;
        }

        const int MaxBursts = 18;
        public const float ReadableTextSeconds = 1.1f, ReducedTextSeconds = .85f;
        readonly List<Burst> bursts = new List<Burst>(MaxBursts);
        readonly Dictionary<int, GUIStyle> numberStyles = new Dictionary<int, GUIStyle>();
        GUIStyle sourceFont;
        float verificationProgress = -1;
        float pausedAt = -1;
        float Clock { get { return pausedAt >= 0 ? pausedAt : Time.unscaledTime; } }
        public bool Busy
        {
            get
            {
                for (int i = bursts.Count - 1; i >= 0; i--)
                    if (Age(bursts[i]) < bursts[i].duration * (bursts[i].ultimate != null ? 1 : .94f)) return true;
                return false;
            }
        }
        public bool Casting
        {
            get
            {
                for (int i = bursts.Count - 1; i >= 0; i--)
                    if (bursts[i].direct && Age(bursts[i]) < (bursts[i].ultimate != null ? bursts[i].duration : Mathf.Min(.32f, bursts[i].duration * .42f))) return true;
                return false;
            }
        }

        public bool UltimateBusy
        {
            get { foreach (var b in bursts) if (b.ultimate != null && VisualAlive(b)) return true; return false; }
        }
        public string UltimateSubject
        {
            get { for (int i = bursts.Count - 1; i >= 0; i--) if (bursts[i].ultimate != null && VisualAlive(bursts[i])) return bursts[i].ultimate.id; return null; }
        }
        public bool UltimateEnemy
        {
            get { for (int i = bursts.Count - 1; i >= 0; i--) if (bursts[i].ultimate != null && VisualAlive(bursts[i])) return bursts[i].action.enemy; return false; }
        }
        public bool ReadableFeedbackAlive
        {
            get { foreach (var b in bursts) if (Age(b) >= Impact(b) * b.duration && Age(b) < b.textUntil) return true; return false; }
        }
        public int UltimateFrame(bool reduced)
        {
            for (int i = bursts.Count - 1; i >= 0; i--)
                if (bursts[i].ultimate != null && VisualAlive(bursts[i]))
                {
                    return UltimateAnimations.FrameIndex(bursts[i].ultimate, Age(bursts[i]), bursts[i].reduced);
                }
            return -1;
        }
        public void Clear() { bursts.Clear(); verificationProgress = pausedAt = -1; }
        public void Pause(bool paused)
        {
            if (paused && pausedAt < 0) pausedAt = Time.unscaledTime;
            else if (!paused && pausedAt >= 0)
            {
                float delay = Time.unscaledTime - pausedAt;
                foreach (var burst in bursts) burst.start += delay;
                pausedAt = -1;
            }
        }
        public void FreezeForVerification(float normalized) { verificationProgress = Mathf.Clamp01(normalized); }
        public void ResumeVerification()
        {
            if (verificationProgress >= 0)
                foreach (var b in bursts) b.start = Clock - verificationProgress * b.duration;
            verificationProgress = -1;
        }
        float Age(Burst b) { return verificationProgress >= 0 ? verificationProgress * b.duration : Clock - b.start; }
        float Progress(Burst b) { return Mathf.Clamp01(Age(b) / b.duration); }
        bool VisualAlive(Burst b) { return Age(b) < b.duration; }
        bool Alive(Burst b) { return Age(b) < Math.Max(b.duration, b.textUntil); }
        static float Impact(Burst b) { return b.reduced ? 0 : b.direct ? Mathf.Clamp(b.profile.impactAt, .28f, .72f) : .12f; }

        public float Add(CombatAction action, bool reduced)
        {
            if (action == null) return 0;
            var card = GameDatabase.Card(action.cardId) ?? WildlifeMechanics.Card(action.cardId);
            var passive = GameDatabase.Passive(action.traitId);
            var rune = GameDatabase.Rune(action.traitId);
            var gear = (action.traitId ?? "").StartsWith("gear:", StringComparison.Ordinal)
                ? GameDatabase.Equipment(action.traitId.Substring(5))
                : action.kind == "deferred_damage" ? GameDatabase.Equipment(action.cardId) : null;
            bool direct = card != null && (string.IsNullOrEmpty(action.kind) || action.kind == "wildlife_skill") && string.IsNullOrEmpty(action.traitId);
            var profile = WildlifeVisualProfile(action.cardId) ?? CombatVisualProfiles.For(card);
            var ultimate = direct ? UltimateAnimations.For(card) : null;
            Color color = Html(profile.primary, new Color(.57f, .87f, .74f));
            Color accent = Html(profile.accent, Color.white);
            if (passive != null) color = new Color(.84f, .66f, .96f);
            if (rune != null) color = new Color(.94f, .78f, .47f);
            if (gear != null) color = new Color(.96f, .72f, .43f);
            float duration = reduced ? .24f : direct ? Mathf.Clamp(profile.duration, .62f, 1.08f) : .52f;
            if (ultimate != null)
                duration = reduced ? UltimateAnimations.ReducedSeconds : ultimate.Duration;
            if (bursts.Count >= MaxBursts)
            {
                int remove = bursts.FindIndex(b => !b.direct);
                bursts.RemoveAt(remove < 0 ? 0 : remove);
            }
            var burst = new Burst { action = action, card = card, passive = passive, rune = rune, gear = gear,
                profile = profile, ultimate = ultimate, direct = direct, reduced = reduced, start = Clock, duration = duration,
                color = color, accent = accent, seed = profile.seed ^ (action.id * 31 & 255) };
            burst.textUntil = Impact(burst) * duration + (reduced ? ReducedTextSeconds : ReadableTextSeconds);
            bursts.Add(burst);
            if (action.damage > 0 || action.avoided > 0 || direct && card.damage > 0) burst.damageLane = ReserveFeedbackLane(!action.enemy);
            if (action.heal > 0) burst.healLane = ReserveFeedbackLane(action.enemy);
            if (action.block > 0) burst.blockLane = ReserveFeedbackLane(action.enemy);
            if (!direct && action.damage == 0 && action.avoided == 0 && action.heal == 0 && action.block == 0)
                burst.statusLane = ReserveFeedbackLane((action.kind ?? "").StartsWith("status_", StringComparison.Ordinal) ? !action.enemy : action.enemy);
            return duration;
        }

        int ReserveFeedbackLane(bool enemy)
        {
            int occupied = 0;
            foreach (var b in bursts)
            {
                if (Age(b) >= b.textUntil) continue;
                if (b.action.enemy != enemy && b.damageLane >= 0) occupied |= 1 << b.damageLane;
                if (b.action.enemy == enemy)
                {
                    if (b.healLane >= 0) occupied |= 1 << b.healLane;
                    if (b.blockLane >= 0) occupied |= 1 << b.blockLane;
                }
                bool statusEnemy = (b.action.kind ?? "").StartsWith("status_", StringComparison.Ordinal) ? !b.action.enemy : b.action.enemy;
                if (statusEnemy == enemy && b.statusLane >= 0) occupied |= 1 << b.statusLane;
            }
            for (int lane = 0; lane < 7; lane++) if ((occupied & 1 << lane) == 0) return lane;
            // Bounded seven-line readout per actor: on unusually dense procs replace the oldest line,
            // rather than stacking two unreadable strings or pushing labels into the hand/HUD.
            foreach (var b in bursts)
            {
                if (Age(b) >= b.textUntil) continue;
                if (b.action.enemy != enemy && b.damageLane >= 0) { int lane = b.damageLane; b.damageLane = -1; return lane; }
                if (b.action.enemy == enemy && b.healLane >= 0) { int lane = b.healLane; b.healLane = -1; return lane; }
                if (b.action.enemy == enemy && b.blockLane >= 0) { int lane = b.blockLane; b.blockLane = -1; return lane; }
                bool statusEnemy = (b.action.kind ?? "").StartsWith("status_", StringComparison.Ordinal) ? !b.action.enemy : b.action.enemy;
                if (statusEnemy == enemy && b.statusLane >= 0) { int lane = b.statusLane; b.statusLane = -1; return lane; }
            }
            return 0;
        }

        static Vector2 FeedbackPosition(bool enemy, int lane, float lift)
        {
            return new Vector2(enemy ? 1015 : 232, 180 + lane * 30 - lift);
        }

        internal bool FeedbackLanesDistinct()
        {
            foreach (bool enemy in new[] { false, true })
            {
                int used = 0;
                foreach (var b in bursts)
                {
                    if (Age(b) >= b.textUntil) continue;
                    int[] lanes = { b.action.enemy != enemy ? b.damageLane : -1,
                        b.action.enemy == enemy ? b.healLane : -1, b.action.enemy == enemy ? b.blockLane : -1,
                        ((b.action.kind ?? "").StartsWith("status_", StringComparison.Ordinal) ? !b.action.enemy : b.action.enemy) == enemy ? b.statusLane : -1 };
                    foreach (int lane in lanes)
                    {
                        if (lane < 0) continue;
                        if (lane > 6 || (used & 1 << lane) != 0) return false;
                        used |= 1 << lane;
                    }
                }
            }
            return true;
        }

        static Color Html(string value, Color fallback)
        {
            Color parsed;
            return !string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value[0] == '#' ? value : "#" + value, out parsed) ? parsed : fallback;
        }

        Burst ActorCast(bool enemy)
        {
            for (int i = bursts.Count - 1; i >= 0; i--)
                if (bursts[i].direct && bursts[i].action.enemy == enemy && VisualAlive(bursts[i])) return bursts[i];
            return null;
        }

        // Preserved for callers that only require translation. DrawActor includes this offset itself.
        public Vector2 Offset(bool enemy, bool reduced)
        {
            if (reduced) return Vector2.zero;
            var b = ActorCast(enemy);
            return b == null ? Vector2.zero : ActorMotion(b, enemy, Progress(b));
        }

        static Vector2 ActorMotion(Burst b, bool enemy, float p)
        {
            float direction = enemy ? -1 : 1;
            float contact = Mathf.Clamp(b.profile.impactAt, .28f, .72f);
            float approach = Mathf.Clamp01(p / contact);
            float recover = Mathf.Clamp01((p - contact) / Mathf.Max(.12f, 1 - contact));
            float forward = p <= contact ? 1 - Mathf.Pow(1 - approach, 3) : 1 - recover * recover;
            float impulse = Mathf.Sin(Mathf.Clamp01(p / .7f) * Mathf.PI);
            switch (b.profile.motion)
            {
                case "dash": return new Vector2(Snap(direction * (forward * 93 - Mathf.Sin(approach * Mathf.PI) * 7)), Snap(-Mathf.Sin(p * Mathf.PI) * 7));
                case "retreat": return new Vector2(Snap(-direction * impulse * 63), Snap(-impulse * 8));
                case "leap": return new Vector2(Snap(direction * forward * 71), Snap(-Mathf.Sin(Mathf.Clamp01(p / .82f) * Mathf.PI) * 43));
                case "blink": return new Vector2(Snap(direction * (p < .18f ? 0 : p < .61f ? 88 : 88 * (1 - Mathf.Clamp01((p - .61f) / .39f)))), 0);
                case "orbit": return new Vector2(Snap(direction * Mathf.Sin(p * Mathf.PI * 2) * 35), Snap(-Mathf.Sin(p * Mathf.PI) * 16));
                case "strike": return new Vector2(Snap(direction * forward * 31), Snap(-Mathf.Sin(p * Mathf.PI) * 4));
                case "cast": return new Vector2(0, Snap(-Mathf.Sin(p * Mathf.PI) * 9));
                default: return Vector2.zero;
            }
        }

        public void DrawActor(Rect rect, Texture2D texture, bool enemy, bool reduced)
        {
            if (!texture) return;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            try
            {
                var b = ActorCast(enemy);
                if (b != null && b.ultimate != null) reduced = b.reduced;
                if (reduced) { GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true); return; }
                Vector2 offset = b == null ? Vector2.zero : ActorMotion(b, enemy, Progress(b));
                float rotation = 0, squash = 0, fade = 1;
                float p = b == null ? 1 : Progress(b);
                float direction = enemy ? -1 : 1;
                if (b != null)
                {
                    float kick = Mathf.Sin(Mathf.Clamp01(p / .7f) * Mathf.PI);
                    if (b.profile.motion == "dash" || b.profile.motion == "strike") rotation = direction * kick * 4;
                    if (b.profile.motion == "retreat") rotation = -direction * kick * 4;
                    if (b.profile.motion == "leap") rotation = direction * Mathf.Sin(p * Mathf.PI * 2) * 5;
                    squash = b.profile.motion == "leap" ? Mathf.Sin(p * Mathf.PI * 2) * .035f : kick * .018f;
                    if (b.profile.motion == "blink") fade = p > .12f && p < .21f ? .18f : p > .58f && p < .65f ? .45f : 1;
                    if (b.profile.motion == "dash" || b.profile.motion == "retreat" || b.profile.motion == "leap" || b.profile.motion == "orbit")
                    {
                        for (int i = 3; i >= 1; i--)
                        {
                            float previous = Mathf.Max(0, p - i * .035f);
                            Vector2 ghost = ActorMotion(b, enemy, previous);
                            float alpha = Mathf.Sin(p * Mathf.PI) * (.18f - i * .035f);
                            GUI.color = new Color(b.color.r, b.color.g, b.color.b, oldColor.a * alpha);
                            GUI.DrawTexture(new Rect(rect.x + ghost.x, rect.y + ghost.y, rect.width, rect.height), texture, ScaleMode.ScaleToFit, true);
                        }
                    }
                    if (b.profile.motion == "blink" && p > .12f && p < .69f)
                    {
                        GUI.color = new Color(b.accent.r, b.accent.g, b.accent.b, oldColor.a * (1 - p) * .24f);
                        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
                    }
                }
                // Target recoil is independent of the actor's own ongoing cast.
                for (int i = bursts.Count - 1; i >= 0; i--)
                {
                    var hit = bursts[i];
                    if (!VisualAlive(hit) || hit.action.enemy == enemy || hit.action.damage <= 0) continue;
                    float phase = Progress(hit), impactAt = hit.direct ? hit.profile.impactAt : .16f;
                    float hp = (phase - impactAt) / .24f;
                    if (hp >= 0 && hp < 1)
                    {
                        float recoil = Mathf.Sin(hp * Mathf.PI) * (hit.action.critical ? 12 : 8);
                        offset.x += Snap(-direction * recoil);
                        rotation -= direction * recoil * .35f;
                        squash -= Mathf.Sin(hp * Mathf.PI) * .028f;
                    }
                    break;
                }
                Rect drawn = new Rect(rect.x + offset.x - rect.width * squash * .5f, rect.y + offset.y + rect.height * squash,
                    rect.width * (1 + squash), rect.height * (1 - squash));
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * fade);
                if (Mathf.Abs(rotation) > .1f) GUIUtility.RotateAroundPivot(rotation, new Vector2(drawn.center.x, drawn.yMax - 12));
                GUI.DrawTexture(drawn, texture, ScaleMode.ScaleToFit, true);
            }
            finally { GUI.color = oldColor; GUI.matrix = oldMatrix; }
        }

        public void Draw(GUIStyle font, bool reduced)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (verificationProgress < 0)
                for (int i = bursts.Count - 1; i >= 0; i--) if (!Alive(bursts[i])) bursts.RemoveAt(i);
            Burst primary = null, fallback = null;
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                if (!Alive(bursts[i])) continue;
                if (fallback == null) fallback = bursts[i];
                if (primary == null && bursts[i].direct && VisualAlive(bursts[i])) primary = bursts[i];
            }
            // Accessibility changes take effect on the next cast, keeping this atlas timeline internally consistent.
            bool primaryReduced = primary != null && primary.ultimate != null ? primary.reduced : reduced;
            bool ultimateVisible = primary != null && primary.ultimate != null;
            if (ultimateVisible)
                UltimateAnimations.Draw(primary.ultimate, primary.card, primary.action.enemy, Age(primary), primaryReduced, font);
            foreach (var b in bursts)
            {
                if (!Alive(b)) continue;
                bool motionReduced = b.ultimate != null ? b.reduced : reduced;
                float p = Progress(b);
                Vector2 source = b.action.enemy ? new Vector2(1015, 282) : new Vector2(232, 282);
                Vector2 target = b.action.enemy ? new Vector2(232, 282) : new Vector2(1015, 282);
                bool status = (b.action.kind ?? "").StartsWith("status_", StringComparison.Ordinal);
                bool stoppedWildlife = b.action.kind == "wildlife_interrupt" || b.action.kind == "wildlife_despair";
                bool attack = !stoppedWildlife && (status || b.action.damage > 0 || b.action.avoided > 0 || b.card != null && (b.card.damage > 0 || b.card.poison > 0 || b.card.weak > 0 || b.card.vulnerable > 0));
                float impactAt = Impact(b);
                float hitProgress = Mathf.Clamp01((p - impactAt) / Mathf.Max(.1f, 1 - impactAt));
                float alpha = Mathf.Min(1, (1 - p) * 3.5f);
                Color color = WithAlpha(b.color, alpha), accent = WithAlpha(b.accent, alpha);
                if (VisualAlive(b) && b.direct && !motionReduced && b.ultimate == null)
                    Spell(b.profile, b.card, source, target, p, color, accent, b.seed, attack);
                else if (VisualAlive(b) && !b.direct && !motionReduced)
                    Reaction(b, attack ? target : source, p, color, accent);
                else if (VisualAlive(b) && motionReduced && p < .85f)
                {
                    // Reduced motion is spatially static: no travel, zoom, flash, orbit or recoil.
                    Vector2 at = attack ? target : source;
                    if (b.action.block > 0) Shield(at, .3f, color, b.seed, false);
                    else if (b.action.heal > 0) Cross(at, 14, color);
                    else Outline(new Rect(at.x - 26, at.y - 26, 52, 52), color, 3);
                }
                if (p >= impactAt)
                {
                    if (VisualAlive(b) && b.action.block > 0 && !motionReduced) Shield(source, hitProgress, WithAlpha(new Color(.57f, .87f, .74f), alpha), b.seed, true);
                    if (VisualAlive(b) && b.action.heal > 0 && !motionReduced) Heal(source, hitProgress, WithAlpha(new Color(.65f, .91f, .57f), alpha), b.seed);
                    // Floating labels outlive their brief particles, without extending card/enemy action locks.
                    alpha = Mathf.Clamp01((b.textUntil - Age(b)) / .28f);
                    color = WithAlpha(b.color, alpha);
                    float lift = motionReduced ? 0 : Mathf.Clamp01((Age(b) - impactAt * b.duration) / .8f) * 2;
                    Vector2 damageAt = FeedbackPosition(!b.action.enemy, b.damageLane, lift);
                    Vector2 healAt = FeedbackPosition(b.action.enemy, b.healLane, lift);
                    Vector2 blockAt = FeedbackPosition(b.action.enemy, b.blockLane, lift);
                    string label = b.direct ? "" : ReactionIdentity(b) + " ";
                    if (b.action.damage > 0 && b.damageLane >= 0) Number(font, damageAt, label + (b.action.critical ? "치명타! −" : "−") + b.action.damage,
                        WithAlpha(b.action.critical ? new Color(1f, .82f, .39f) : new Color(1f, .58f, .51f), alpha), b.action.critical ? 24 : b.direct ? 21 : 14, b.direct ? 260 : 320);
                    if (VisualAlive(b) && b.action.critical && !motionReduced) Star(target, 25 * (1 - hitProgress), accent, 4, 0);
                    if (b.direct && b.damageLane >= 0 && b.card.damage > 0 && b.action.damage == 0 && b.action.avoided == 0) Number(font, damageAt, "직접 피해 0", color, 16);
                    if (b.action.avoided > 0 && b.damageLane >= 0) Number(font, damageAt, "회피!" + (b.action.avoided > 1 ? " ×" + b.action.avoided : ""), WithAlpha(new Color(.56f, .84f, .96f), alpha), 17);
                    if (b.action.heal > 0 && b.healLane >= 0) Number(font, healAt, label + "+" + b.action.heal, WithAlpha(new Color(.65f, .91f, .57f), alpha), b.direct ? 20 : 14, 320);
                    if (b.action.block > 0 && b.blockLane >= 0) Number(font, blockAt, label + "방어 +" + b.action.block, color, b.direct ? 17 : 14, 320);
                    if (b.statusLane >= 0)
                        Number(font, FeedbackPosition(status ? !b.action.enemy : b.action.enemy, b.statusLane, lift), label.Trim(), color, 14, 320);
                }
            }
            if (primary != null)
                CombatCardCue.Draw(primary.action, primary.card, primary.profile, Progress(primary), primary.duration, font, primaryReduced);
            var banner = primary ?? fallback;
            if (banner != null && !ultimateVisible && banner.ultimate == null && VisualAlive(banner)) DrawBanner(banner, font);
        }

        void DrawBanner(Burst b, GUIStyle font)
        {
            float alpha = Mathf.Min(1, (1 - Progress(b)) * 3.5f);
            Color tint = WithAlpha(b.color, alpha);
            Rect r = new Rect(425, 398, 430, 35);
            Pixel(r, new Color(.025f, .055f, .08f, .92f * alpha));
            Pixel(new Rect(r.x, r.y, 4, r.height), tint);
            Texture2D icon = WildlifeMechanics.IsAnimalSkill(b.action.cardId) ? PixelArt.Portrait(b.action.cardId == "wildlife_boar_charge" ? "boar" : "bear") : b.passive != null ? PixelArt.PassiveIcon(b.passive.id) : b.rune != null ? PixelArt.SystemIcon(b.rune.id) : b.gear != null ? PixelArt.SystemIcon(b.gear.id) : b.card != null ? PixelArt.SkillIcon(b.card.id) : null;
            if (icon)
            {
                Color old = GUI.color;
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.DrawTexture(new Rect(r.x + 10, r.y + 3, 29, 29), icon, ScaleMode.ScaleToFit, true);
                GUI.color = old;
            }
            bool displayEnemy = b.action.kind == "deferred_damage" ? !b.action.enemy : b.action.enemy;
            string identity = b.direct ? b.card.name : ReactionIdentity(b);
            Number(font, r.center + new Vector2(icon ? 20 : 0, 0), (displayEnemy ? "상대  /  " : "하나  /  ") + identity,
                new Color(.91f, .96f, .98f, alpha), identity.Length > 22 ? 11 : 14, icon ? 368 : 420);
        }

        static string ReactionIdentity(Burst b)
        {
            if (b.action.kind == "wildlife_interrupt") return "동물 기술 중단";
            if (b.action.kind == "wildlife_despair") return "돌진 실패 · 좌절";
            if (b.action.kind == "deferred_damage") return "장비 · 아오자이";
            if (b.passive != null) return "패시브 · " + b.passive.name;
            if (b.rune != null) return "룬 · " + b.rune.name;
            if (b.gear != null) return "장비 · " + b.gear.name;
            if (b.action.cardId == "status_poison") return "중독";
            return EffectLabel(b.action.kind);
        }

        static string EffectLabel(string kind)
        {
            if ((kind ?? "").StartsWith("status_", StringComparison.Ordinal)) return StatusMechanics.Name(kind.Substring(7));
            switch (kind)
            {
                case "delayed_damage": return "설치물 발동";
                case "summon": return "소환물 공격";
                case "counter": return "반격";
                case "bleed": return "출혈";
                case "burn": return "화상";
                case "hot": return "지속 회복";
                case "guard": return "지속 보호";
                case "revive": return "치명상 방지";
                case "boss_interrupt": return "보스 기믹 저지";
                default: return "효과 발동";
            }
        }

        static void Reaction(Burst b, Vector2 at, float p, Color c, Color accent)
        {
            if (b.action.heal > 0) { Cross(at + new Vector2(0, -18 - p * 20), 12, c); return; }
            if (b.action.block > 0) { Shield(at, p, c, b.seed, false); return; }
            string kind = b.action.kind ?? "";
            if (kind == "bleed" || kind == "status_bleed") Drops(at, p, c, b.seed, 5, 40);
            else if (kind == "burn" || kind == "status_burn") Flames(at, p, c, accent, b.seed, 6, 35);
            else if (kind == "summon" || kind == "delayed_damage") { Star(at, 16 + p * 18, c, 4, b.seed % 90); Particles(at, p, accent, b.seed, 5, 42); }
            else if (b.action.cardId == "status_poison" || kind == "poison") Bubbles(at, p, c, b.seed, 5, 40);
            else { Cross(at, 13 + p * 13, c); Particles(at, p, accent, b.seed, 5, 36); }
        }

        void Number(GUIStyle font, Vector2 center, string text, Color color, int size = 20, int width = 250)
        {
            if (sourceFont != font) { sourceFont = font; numberStyles.Clear(); }
            GUIStyle style;
            if (!numberStyles.TryGetValue(size, out style))
            {
                style = new GUIStyle(font) { fontSize = size, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
                numberStyles.Add(size, style);
            }
            Rect r = new Rect(Snap(center.x - width / 2f), Snap(center.y - 16), width, 32);
            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                style.normal.textColor = new Color(.015f, .025f, .04f, color.a);
                // Eight surrounding copies form an actual outline, readable on both pale effects and dark scenery.
                int outline = size >= 17 ? 2 : 1;
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                        if (x != 0 || y != 0) GUI.Label(new Rect(r.x + x * outline, r.y + y * outline, r.width, r.height), text, style);
                style.normal.textColor = color;
                GUI.Label(r, text, style);
            }
            finally { GUI.color = oldColor; }
        }

        static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

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
