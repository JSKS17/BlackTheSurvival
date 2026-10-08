using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    /// <summary>All art is a bitmap, rendered with nearest-neighbour sampling.</summary>
    public static class PixelArt
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        public const int PortraitPixelHeight = 160;
        const int LegacyPortraitPixelHeight = 80;
        private static Texture2D lobby, battle, map, atlas, foods, subjects;
        private static readonly Color32 Ink = C("121b2b"), Teal = C("53d6cd"), Gold = C("ecc17d"), Pink = C("e47baa");

        public static void Ensure()
        {
            if (lobby != null) return;
            lobby = Load("Lobby") ?? Lab();
            battle = Load("Battle") ?? Forest();
            atlas = Load("CharacterAtlas");
            foods = Load("FoodAtlas");
            subjects = Load("SubjectAtlas");
            map = Island();
        }

        public static Texture2D Background() { Ensure(); return lobby; }
        public static Texture2D BattleBackground() { Ensure(); return battle; }
        public static Texture2D MapBackground() { Ensure(); return map; }

        public static Texture2D Portrait(string id)
        {
            Ensure();
            var key = (id ?? "hana").Trim().ToLowerInvariant();
            switch (key)
            {
                case "하나": case "dr. 하나": key = "hana"; break;
                case "니아": case "niah": key = "nia"; break;
                case "닭": key = "chicken"; break;
                case "들개": case "wilddog": case "wild dog": key = "dog"; break;
                case "멧돼지": case "wildboar": key = "boar"; break;
                case "늑대": key = "wolf"; break;
                case "곰": key = "bear"; break;
                case "재키": key = "jackie"; break;
                case "아야": key = "aya"; break;
                case "현우": key = "hyunwoo"; break;
                case "유키": key = "yuki"; break;
                case "혜진": key = "hyejin"; break;
                case "수아": key = "sua"; break;
                case "아이솔": key = "isol"; break;
                case "나딘": key = "nadine"; break;
                case "엠마": key = "emma"; break;
            }
            var character = GameDatabase.Characters.Find(x => x.id == key || x.name == id);
            if (character != null) key = character.id;
            Texture2D result;
            if (Cache.TryGetValue("portrait/" + key, out result)) return result;
            var identity = Load("PortraitsIdentity/" + key);
            var specific = identity ?? Load("PortraitsCoarse/" + key) ?? Load("Portraits/" + key);
            if (specific != null) result = specific;
            else if (atlas != null && (key == "hana" || key == "nia" || key == "chicken" || key == "dog" || key == "boar" || key == "wolf" || key == "bear" || key == "drone"))
            {
                // Coordinates are normalized from the generated source, top-left origin.
                // Extracting atlas regions preserves original pixels and transparent alpha.
                switch (key)
                {
                    case "hana": result = Slice(atlas, .015f, .035f, .23f, .50f); break;
                    case "nia": result = Slice(atlas, .253f, .035f, .263f, .50f); break;
                    case "chicken": result = Slice(atlas, .528f, .155f, .203f, .38f); break;
                    case "dog": result = Slice(atlas, .746f, .12f, .25f, .41f); break;
                    case "boar": result = Slice(atlas, .01f, .62f, .254f, .31f, true); break;
                    case "wolf": result = Slice(atlas, .25f, .59f, .245f, .33f, true); break;
                    case "bear": result = Slice(atlas, .497f, .58f, .265f, .34f, true); break;
                    default: result = Slice(atlas, .776f, .58f, .215f, .31f); break;
                }
            }
            else if (subjects != null && Array.IndexOf(new[] { "jackie", "aya", "hyunwoo", "yuki", "hyejin", "sua", "isol", "nadine", "emma" }, key) >= 0)
            {
                switch (key)
                {
                    case "jackie": result = Slice(subjects, 0f, 0f, .3333f, .336f); break;
                    case "aya": result = Slice(subjects, .34f, 0f, .29f, .336f); break;
                    case "hyunwoo": result = Slice(subjects, .68f, 0f, .31f, .336f); break;
                    case "yuki": result = Slice(subjects, 0f, .34f, .33f, .32f); break;
                    case "hyejin": result = Slice(subjects, .34f, .34f, .32f, .32f); break;
                    case "sua": result = Slice(subjects, .659f, .34f, .341f, .32f); break;
                    case "isol": result = Slice(subjects, 0f, .66f, .33f, .34f); break;
                    case "nadine": result = Slice(subjects, .34f, .66f, .326f, .34f); break;
                    default: result = Slice(subjects, .669f, .66f, .331f, .34f); break;
                }
            }
            else result = Subject(key);
            // Authored faces keep their complete logical grid; reducing them to 80 pixels
            // erased eyelids, eyebrows, facial hair and expression in the old portraits.
            if(identity==null && (key=="hana" || character!=null)) result=CoarsePortrait(result);
            Cache["portrait/" + key] = result;
            return result;
        }

        static Texture2D CoarsePortrait(Texture2D source)
        {
            if(source==null || source.height<=LegacyPortraitPixelHeight) return source;
            int width=Mathf.Max(1,Mathf.RoundToInt(source.width*LegacyPortraitPixelHeight/(float)source.height));
            var target=RenderTexture.GetTemporary(width,LegacyPortraitPixelHeight,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            try
            {
                source.filterMode=FilterMode.Point;
                Graphics.Blit(source,target);
                RenderTexture.active=target;
                var pixels=new Texture2D(width,LegacyPortraitPixelHeight,TextureFormat.RGBA32,false);
                pixels.name=source.name+"_coarse80";
                pixels.ReadPixels(new Rect(0,0,width,LegacyPortraitPixelHeight),0,0,false);
                pixels.Apply(false,false);
                pixels.filterMode=FilterMode.Point;pixels.wrapMode=TextureWrapMode.Clamp;
                return pixels;
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
        }

        public static Texture2D Icon(string kind)
        {
            Ensure();
            var key = (kind ?? "skill").Trim().ToLowerInvariant();
            Texture2D icon;
            if (Cache.TryGetValue("icon/" + key, out icon)) return icon;
            icon = Load("SystemIcons/" + key) ?? Load("Icons/" + key);
            if (icon == null && foods != null)
            {
                string[] ids = { "potato", "meat", "salmon", "sweet_potato", "truffle", "fries", "steak", "unused_omelet", "chicken_food", "fish_chips", "salmon_steak", "honey_potato", "truffle_pasta", "soup", "pizza", "fish_cutlet" };
                int index = Array.IndexOf(ids, key);
                if (index >= 0) icon = Slice(foods, (index % 4) * .25f, (index / 4) * .25f, .25f, .25f);
            }
            if (icon == null) icon = DrawIcon(key);
            Cache["icon/" + key] = icon;
            return icon;
        }

        /// <summary>Every card uses its own authored pixel icon, including basic, weapon and tactical cards.</summary>
        public static Texture2D SkillIcon(string cardId)
        {
            string key = (cardId ?? "basic_attack").Trim().ToLowerInvariant();
            Texture2D result;
            if (Cache.TryGetValue("skill/" + key, out result)) return result;
            result = Load("SkillIcons/" + key);
            // This keeps development previews usable. The art verification tool requires
            // a dedicated PNG for every card and never accepts this placeholder.
            if (result == null)
            {
                var card = GameDatabase.Card(key);
                result = Icon(card != null && card.block > 0 ? "guard" : "attack");
            }
            Cache["skill/" + key] = result;
            return result;
        }

        public static Texture2D PassiveIcon(string passiveId)
        {
            string key = (passiveId ?? "").Trim().ToLowerInvariant();
            Texture2D result;
            if (Cache.TryGetValue("passive/" + key, out result)) return result;
            result = Load("PassiveIcons/" + key) ?? Icon("rune");
            Cache["passive/" + key] = result;
            return result;
        }

        public static bool HasDedicatedPortrait(string characterId) { return Load("PortraitsIdentity/" + characterId) != null || Load("Portraits/" + characterId) != null; }
        public static bool HasIdentityPortrait(string characterId) { return Load("PortraitsIdentity/" + characterId) != null; }
        public static bool HasSkillIcon(string cardId) { return Load("SkillIcons/" + cardId) != null; }
        public static bool HasPassiveIcon(string passiveId) { return Load("PassiveIcons/" + passiveId) != null; }
        public static bool HasSystemIcon(string id) { return Load("SystemIcons/" + id) != null; }
        public static Texture2D SystemIcon(string id)
        {
            string key=(id??"rune").Trim().ToLowerInvariant();Texture2D texture;
            if(Cache.TryGetValue("system/"+key,out texture)) return texture;
            texture=Load("SystemIcons/"+key) ?? Icon(key);Cache["system/"+key]=texture;
            return texture;
        }

        private static Texture2D Load(string path)
        {
            var texture = Resources.Load<Texture2D>("Lumia/" + path);
            if (texture != null) { texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; texture.anisoLevel = 0; }
            return texture;
        }

        private static Texture2D Slice(Texture2D source, float nx, float ny, float nw, float nh, bool isolate = false)
        {
            int x = Mathf.RoundToInt(nx * source.width), yTop = Mathf.RoundToInt(ny * source.height);
            int w = Mathf.Min(Mathf.RoundToInt(nw * source.width), source.width - x);
            int h = Mathf.Min(Mathf.RoundToInt(nh * source.height), source.height - yTop);
            try
            {
                var sourcePixels = source.GetPixels(x, source.height - yTop - h, w, h);
                // Some animal silhouettes share a column boundary. Keep their own connected
                // alpha island so a neighbour's tail cannot appear in the cropped sprite.
                if (isolate) KeepLargestAlphaIsland(sourcePixels, w, h);
                int left = w, right = -1, bottom = h, top = -1;
                for (int yy = 0; yy < h; yy++) for (int xx = 0; xx < w; xx++)
                    if (sourcePixels[yy * w + xx].a > .08f) { left = Mathf.Min(left, xx); right = Mathf.Max(right, xx); bottom = Mathf.Min(bottom, yy); top = Mathf.Max(top, yy); }
                if (right < left) return source;
                int tw = right - left + 5, th = top - bottom + 5;
                var pixels = new Color[tw * th];
                for (int yy = bottom; yy <= top; yy++) for (int xx = left; xx <= right; xx++) pixels[(yy - bottom + 2) * tw + xx - left + 2] = sourcePixels[yy * w + xx];
                var texture = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels); texture.Apply(); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
            catch (UnityException) { return Subject("hana"); }
        }

        private static void KeepLargestAlphaIsland(Color[] pixels, int w, int h)
        {
            var visited = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            List<int> largest = null;
            for (int start = 0; start < pixels.Length; start++)
            {
                if (visited[start] || pixels[start].a <= .08f) continue;
                int head = 0, tail = 1;
                queue[0] = start; visited[start] = true;
                while (head < tail)
                {
                    int pos = queue[head++], px = pos % w, py = pos / w;
                    for (int d = 0; d < 4; d++)
                    {
                        int ax = px + (d == 0 ? -1 : d == 1 ? 1 : 0), ay = py + (d == 2 ? -1 : d == 3 ? 1 : 0);
                        if (ax < 0 || ax >= w || ay < 0 || ay >= h) continue;
                        int next = ay * w + ax;
                        if (visited[next] || pixels[next].a <= .08f) continue;
                        visited[next] = true; queue[tail++] = next;
                    }
                }
                if (largest == null || tail > largest.Count) { largest = new List<int>(tail); for (int i = 0; i < tail; i++) largest.Add(queue[i]); }
            }
            if (largest == null) return;
            var keep = new bool[pixels.Length];
            foreach (int index in largest) keep[index] = true;
            for (int index = 0; index < pixels.Length; index++) if (!keep[index]) pixels[index] = Color.clear;
        }

        private static Texture2D DrawIcon(string key)
        {
            var p = new Canvas(32, 32);
            Color32 pale = C("ebedce"), red = C("e66873"), blue = C("8eade3"), brown = C("895c4d");
            if (key.Contains("camp") || key == "모닥불" || key == "fire")
            {
                p.Line(6, 6, 25, 4, brown, 3); p.Line(6, 4, 25, 6, Gold, 2);
                p.Poly(C("d95a41"), 9, 8, 5, 15, 10, 20, 13, 27, 18, 21, 22, 26, 25, 16, 22, 8);
                p.Poly(Gold, 13, 8, 11, 15, 16, 23, 18, 17, 22, 13, 19, 8);
                p.Poly(pale, 15, 8, 14, 12, 18, 17, 20, 11, 18, 8);
            }
            else if (key == "wildlife" || key == "야생동물" || key == "paw")
            {
                p.Poly(Gold, 7, 7, 8, 12, 13, 17, 18, 17, 24, 12, 25, 7, 20, 5, 16, 7, 12, 5);
                p.Ellipse(5, 18, 5, 8, Gold); p.Ellipse(12, 23, 5, 7, Gold); p.Ellipse(19, 23, 5, 7, Gold); p.Ellipse(26, 18, 5, 8, Gold);
            }
            else if (key == "kiosk" || key == "키오스크" || key == "shop")
            {
                p.Rect(8, 3, 17, 3, Ink); p.Rect(10, 6, 13, 25, Gold); p.Rect(12, 17, 9, 11, Ink); p.Rect(13, 20, 7, 6, Teal);
                p.Rect(12, 10, 8, 3, Ink); p.Rect(14, 5, 5, 4, brown); p.Rect(15, 14, 4, 1, Pink);
            }
            else if (key == "encounter" || key == "실험체 조우" || key == "event")
            {
                p.Poly(Pink, 4, 12, 4, 27, 28, 27, 28, 12, 17, 12, 10, 5, 10, 12);
                p.Rect(15, 18, 3, 5, Ink); p.Rect(15, 14, 3, 2, Ink);
            }
            else if (key == "boss" || key == "보스")
            {
                p.Poly(red, 5, 8, 3, 23, 11, 17, 16, 28, 21, 17, 29, 23, 26, 8); p.Rect(6, 5, 20, 3, Gold);
                p.Diamond(16, 12, 3, pale);
            }
            else if (key == "subject" || key == "실험체" || key == "combat" || key == "attack" || key == "basic_attack")
            {
                p.Line(6, 6, 24, 25, pale, 4); p.Line(6, 25, 24, 6, blue, 4);
                p.Line(4, 11, 10, 4, Gold, 2); p.Line(20, 4, 27, 11, Gold, 2); p.Rect(4, 4, 4, 4, brown); p.Rect(23, 4, 4, 4, brown);
            }
            else if (key.Contains("blood") || key == "vf" || key.Contains("혈액"))
            {
                p.Rect(12, 5, 9, 22, blue); p.Rect(13, 8, 7, 14, red); p.Rect(11, 27, 11, 3, pale); p.Rect(14, 23, 2, 3, pale); p.Rect(10, 3, 13, 3, Ink);
            }
            else if (key.Contains("force") || key.Contains("포스"))
            {
                p.Diamond(16, 16, 13, blue); p.Diamond(16, 16, 9, Ink); p.Diamond(16, 16, 6, Teal); p.Diamond(15, 18, 2, pale);
            }
            else if (key.Contains("mithril") || key.Contains("미스릴"))
            {
                p.Poly(blue, 4, 8, 10, 23, 25, 23, 28, 8); p.Poly(pale, 10, 23, 14, 28, 27, 28, 25, 23); p.Line(6, 10, 26, 10, Teal, 2);
            }
            else if (key.Contains("tree") || key.Contains("생명"))
            {
                p.Rect(14, 4, 4, 11, brown); p.Poly(Teal, 16, 29, 4, 15, 10, 15, 6, 10, 26, 10, 22, 15, 28, 15); p.Rect(14, 18, 4, 4, pale);
            }
            else if (key.Contains("meteor") || key.Contains("운석"))
            {
                p.Line(15, 15, 26, 29, Gold, 2); p.Line(22, 12, 29, 23, red, 2); p.Line(10, 20, 21, 30, red, 2);
                p.Poly(brown, 3, 7, 4, 17, 12, 22, 21, 18, 23, 9, 16, 3, 8, 3); p.Poly(Gold, 7, 17, 13, 19, 18, 15, 12, 11, 7, 12); p.Rect(7, 6, 4, 3, Ink);
            }
            else if (key.Contains("coin") || key.Contains("credit") || key.Contains("크레딧"))
            {
                p.Ellipse(16, 16, 24, 24, brown); p.Ellipse(15, 18, 22, 22, Gold); p.Ellipse(15, 18, 16, 16, brown); p.Line(15, 12, 15, 24, Gold, 3); p.Rect(12, 21, 7, 3, Gold);
            }
            else if (key.Contains("soup") || key.Contains("스프") || key.Contains("stew") || key.Contains("soup"))
            {
                p.Poly(C("676d9b"), 4, 8, 2, 20, 28, 20, 26, 8, 22, 4, 8, 4); p.Ellipse(15, 20, 27, 9, Gold); p.Ellipse(15, 21, 20, 6, C("b3694d"));
                p.Rect(9, 20, 4, 3, Teal); p.Rect(17, 21, 4, 3, pale); p.Rect(20, 18, 3, 3, red); p.Line(9, 26, 11, 29, pale, 1); p.Line(18, 27, 20, 30, pale, 1);
            }
            else if (key == "watermelon" || key == "수박")
            {
                // 12.0's replacement for omelet rice: a red seeded wedge with layered rind.
                p.Poly(Ink, 3, 5, 16, 29, 29, 5);
                p.Poly(C("376c43"), 4, 6, 16, 27, 28, 6);
                p.Poly(C("80b766"), 5, 8, 16, 27, 27, 8);
                p.Poly(C("dbe6a5"), 7, 11, 16, 26, 25, 11);
                p.Poly(C("df5d6d"), 8, 13, 16, 25, 24, 13);
                p.Line(11, 17, 15, 24, C("f69488"), 2);
                p.Rect(12, 15, 2, 2, C("422a34"));
                p.Rect(18, 15, 2, 2, C("422a34"));
                p.Rect(15, 20, 2, 2, C("422a34"));
                p.Line(7, 8, 25, 8, C("214d35"), 1);
            }
            else if (key.Contains("bread") || key.Contains("garlic") || key.Contains("마늘") || key.Contains("빵"))
            {
                p.Poly(brown, 5, 6, 2, 13, 8, 23, 23, 27, 29, 23, 27, 16, 13, 5); p.Poly(Gold, 6, 9, 5, 14, 10, 21, 23, 24, 25, 21, 23, 17, 12, 9);
                p.Line(9, 11, 12, 18, pale, 2); p.Line(15, 14, 18, 22, pale, 2); p.Rect(18, 17, 2, 2, Teal);
            }
            else if (key.Contains("pizza") || key.Contains("피자"))
            {
                p.Poly(brown, 3, 7, 4, 13, 15, 29, 28, 9, 28, 5); p.Poly(Gold, 5, 10, 15, 25, 25, 9); p.Ellipse(14, 19, 5, 5, red); p.Ellipse(19, 12, 5, 5, red); p.Rect(10, 13, 2, 3, Teal);
            }
            else if (key.Contains("steak") || key.Contains("meat") || key.Contains("고기") || key.Contains("스테이크") || key.Contains("cutlet"))
            {
                p.Ellipse(16, 12, 28, 18, blue); p.Poly(brown, 4, 12, 5, 20, 14, 25, 25, 22, 28, 15, 22, 9, 11, 8); p.Poly(red, 6, 14, 8, 20, 15, 23, 23, 21, 25, 15, 20, 12, 12, 11);
                p.Line(11, 12, 15, 20, brown, 2); p.Line(16, 12, 20, 20, brown, 2); p.Ellipse(24, 8, 4, 4, Teal);
            }
            else if (key.Contains("heal") || key == "health" || key == "hp")
            {
                p.Poly(red, 16, 4, 3, 17, 3, 24, 8, 28, 13, 27, 16, 23, 19, 27, 24, 28, 29, 24, 29, 17); p.Rect(7, 22, 4, 2, pale);
            }
            else if (key.Contains("block") || key.Contains("defend") || key.Contains("shield"))
            {
                p.Poly(blue, 16, 3, 5, 11, 3, 25, 16, 29, 29, 25, 27, 11); p.Poly(Ink, 16, 7, 9, 13, 7, 22, 16, 25, 25, 22, 23, 13); p.Rect(14, 12, 4, 11, Teal);
            }
            else if (key.Contains("energy") || key.Contains("rune"))
            {
                p.Poly(Gold, 7, 3, 11, 14, 5, 14, 23, 30, 19, 20, 27, 20, 14, 8, 18, 8); p.Line(12, 15, 23, 26, pale, 1);
            }
            else if (key.Contains("draw") || key.Contains("deck") || key.Contains("card"))
            {
                p.Rect(4, 7, 17, 23, blue); p.Rect(9, 3, 20, 25, Gold); p.Rect(11, 5, 16, 21, Ink); p.Diamond(19, 16, 5, Pink);
            }
            else if (key.Contains("weapon") || key.Contains("sword"))
            {
                p.Line(8, 7, 25, 25, blue, 5); p.Line(9, 9, 25, 26, pale, 2); p.Line(5, 14, 16, 3, Gold, 3); p.Line(4, 3, 10, 9, brown, 4);
            }
            else
            {
                // Skill motifs vary by a stable hash, so every catalog card has distinct pixel art.
                uint seed = Hash(key); Color32 accent = Palette(seed);
                p.Diamond(16, 16, 13, Ink); p.Diamond(16, 16, 11, accent); p.Diamond(16, 16, 8, Ink);
                for (int i = 0; i < 7; i++) { seed = Next(seed); int x = 5 + (int)(seed % 22); seed = Next(seed); int y = 5 + (int)(seed % 22); p.Line(16, 16, x, y, accent, 2); }
                p.Diamond(16, 16, 4, pale); p.Rect(14, 15, 4, 3, accent);
            }
            return p.Texture("Icon " + key);
        }

        private static Texture2D Subject(string id)
        {
            var p = new Canvas(64, 96); uint hash = Hash(id);
            var accent = Palette(hash); var hair = C("687592"); var skin = C("efcbaa");
            if (id.Contains("jackie")) { hair = C("aa364a"); accent = C("8b333d"); }
            else if (id.Contains("yuki")) { hair = C("18283d"); accent = C("c2ced6"); }
            else if (id.Contains("aya")) { hair = C("473144"); accent = C("4968ab"); }
            else if (id.Contains("hyejin")) { hair = C("342333"); accent = C("a65792"); }
            else if (id.Contains("rozzi")) { hair = C("242634"); accent = C("bb514c"); }
            else if (id.Contains("isol")) { hair = C("e0d0a4"); accent = C("62734a"); }
            else if (id.Contains("sissela")) { hair = C("d4dfea"); accent = C("829eb7"); }
            p.Ellipse(32, 6, 42, 8, C("17202a"));
            p.Rect(18, 6, 10, 28, Ink); p.Rect(36, 6, 10, 28, Ink); p.Rect(16, 5, 14, 6, accent); p.Rect(34, 5, 14, 6, accent);
            p.Poly(Ink, 15, 27, 12, 50, 19, 61, 45, 61, 52, 50, 49, 27); p.Poly(accent, 18, 29, 17, 48, 22, 58, 42, 58, 48, 48, 45, 29);
            p.Rect(20, 29, 24, 3, Gold); p.Rect(27, 49, 8, 9, C("d3dce2"));
            p.Line(14, 51, 7, 29, Ink, 10); p.Line(15, 51, 8, 30, accent, 6); p.Rect(5, 24, 7, 8, skin);
            p.Line(47, 51, 53, 35, Ink, 10); p.Line(47, 51, 53, 35, accent, 6); p.Rect(50, 30, 7, 8, skin);
            p.Poly(hair, 17, 61, 16, 78, 21, 89, 42, 89, 48, 78, 47, 61); p.Rect(21, 62, 23, 19, skin);
            p.Poly(hair, 17, 78, 22, 89, 42, 89, 47, 78, 40, 80, 34, 85, 29, 78, 23, 82);
            p.Rect(24, 73, 3, 4, Ink); p.Rect(37, 73, 3, 4, Ink); p.Rect(26, 76, 1, 1, C("ffffff")); p.Rect(39, 76, 1, 1, C("ffffff")); p.Rect(30, 65, 5, 1, C("bc7e6e"));
            if (id.Contains("yuki")) { p.Line(55, 23, 60, 74, C("a9c7d7"), 3); p.Line(49, 35, 62, 35, Gold, 3); }
            else if (id.Contains("jackie")) { p.Line(50, 33, 60, 78, C("cd6572"), 7); p.Line(50, 32, 60, 77, C("e5c7ad"), 2); }
            else { p.Rect(52, 35, 11, 6, Ink); p.Rect(58, 39, 6, 2, C("809baf")); }
            return p.Texture("Subject " + id);
        }

        private static Texture2D Lab()
        {
            var p = new Canvas(320, 180); p.Rect(0, 0, 320, 180, C("0a1221"));
            for (int i = 0; i < 5; i++) { p.Rect(160 + i * 30, 48, 25, 95, C("213950")); p.Rect(162 + i * 30, 52, 21, 84, C("184b58")); p.Rect(164 + i * 30, 55, 3, 76, C("286c77")); }
            for (int y = 8; y < 40; y += 8) p.Line(0, y, 320, y, C("253449"));
            for (int x = 0; x < 320; x += 22) p.Line(x, 0, 160 + (x - 160) / 2, 40, C("253449"));
            p.Rect(205, 32, 99, 9, Ink); p.Rect(213, 41, 74, 14, C("34506a")); p.Rect(228, 55, 52, 31, C("194b59")); p.Rect(232, 58, 44, 24, Teal);
            return p.Texture("Pixel laboratory");
        }

        private static Texture2D Forest()
        {
            var p = new Canvas(320, 180); p.Rect(0, 0, 320, 180, C("182642")); p.Rect(0, 0, 320, 50, C("142c2e"));
            for (int i = 0; i < 22; i++) { int x = (i * 37) % 320, h = 28 + i % 6 * 9; p.Rect(x + 5, 48, 5, h, C("11232d")); p.Poly(C("163836"), x - 9, 55, x + 7, 55 + h, x + 24, 55); }
            p.Rect(0, 20, 320, 24, C("263b3b")); for (int i = 0; i < 50; i++) p.Rect(i * 17 % 320, i * 11 % 38, 3, 1, C("3e5550"));
            return p.Texture("Pixel forest");
        }

        private static Texture2D Island()
        {
            var p = new Canvas(480, 270); p.Rect(0, 0, 480, 270, C("111f30"));
            for (int y = 6; y < 270; y += 10) for (int x = 4 + y % 17; x < 480; x += 23) p.Rect(x, y, 6, 1, C("1d3442"));
            p.Poly(C("29404a"), 45, 80, 80, 140, 62, 173, 107, 220, 166, 231, 216, 253, 280, 225, 343, 233, 408, 190, 432, 143, 402, 93, 358, 54, 293, 43, 246, 24, 192, 45, 139, 29, 94, 57);
            p.Poly(C("375552"), 53, 83, 88, 139, 72, 171, 110, 212, 170, 220, 216, 244, 279, 217, 339, 224, 399, 184, 421, 143, 392, 98, 353, 63, 288, 51, 244, 33, 192, 54, 141, 37, 99, 64);
            uint seed = 719u;
            for (int i = 0; i < 100; i++)
            {
                seed = Next(seed); int x = 90 + (int)(seed % 290); seed = Next(seed); int y = 64 + (int)(seed % 150);
                if (x < 121 || x > 352) { p.Poly(C("1d3c40"), x - 4, y, x, y + 10, x + 4, y); continue; }
                p.Rect(x, y, 8, 6, C("24343f")); p.Rect(x, y + 6, 8, 8, C("58716b")); p.Rect(x + 1, y + 7, 6, 6, C("405451")); p.Rect(x + 2, y + 8, 2, 2, C("91b8a2"));
            }
            p.Line(118, 69, 366, 198, C("668277"), 4); p.Line(119, 69, 366, 198, C("354a47"), 2);
            p.Line(125, 196, 349, 74, C("668277"), 4); p.Line(125, 196, 349, 74, C("354a47"), 2);
            p.Rect(216, 117, 50, 31, C("182b35")); p.Rect(220, 124, 43, 24, C("62787f")); p.Rect(225, 128, 33, 14, C("314e5b")); p.Rect(239, 132, 5, 6, Teal);
            // Subtle dark frame keeps map routes and labels legible above the bitmap.
            p.Rect(0, 0, 480, 8, C("0e1825")); p.Rect(0, 262, 480, 8, C("0e1825"));
            return p.Texture("Lumia pixel island");
        }

        private static Color32 C(string hex) { return new Color32(Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16), 255); }
        private static uint Hash(string s) { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return h; }
        private static uint Next(uint s) { return s * 1664525u + 1013904223u; }
        private static Color32 Palette(uint h) { Color32[] colors = { Teal, Gold, Pink, C("92a7dc"), C("89ba82"), C("cf6d62") }; return colors[h % colors.Length]; }

        private sealed class Canvas
        {
            private readonly int w, h; private readonly Color32[] pixels;
            public Canvas(int width, int height) { w = width; h = height; pixels = new Color32[w * h]; }
            public void Pixel(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) pixels[y * w + x] = c; }
            public void Rect(int x, int y, int width, int height, Color32 c) { for (int j = y; j < y + height; j++) for (int i = x; i < x + width; i++) Pixel(i, j, c); }
            public void Line(int x0, int y0, int x1, int y1, Color32 c, int thick = 1)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
                while (true) { Rect(x0 - thick / 2, y0 - thick / 2, thick, thick, c); if (x0 == x1 && y0 == y1) break; int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; } }
            }
            public void Ellipse(int cx, int cy, int width, int height, Color32 c)
            {
                for (int y = -height / 2; y <= height / 2; y++) for (int x = -width / 2; x <= width / 2; x++) if (x * x * height * height + y * y * width * width <= width * width * height * height / 4) Pixel(cx + x, cy + y, c);
            }
            public void Diamond(int cx, int cy, int size, Color32 c) { for (int y = -size; y <= size; y++) Rect(cx - (size - Math.Abs(y)), cy + y, 2 * (size - Math.Abs(y)) + 1, 1, c); }
            public void Poly(Color32 c, params int[] v)
            {
                int count = v.Length / 2, min = h, max = 0;
                for (int i = 0; i < count; i++) { min = Math.Min(min, v[2 * i + 1]); max = Math.Max(max, v[2 * i + 1]); }
                var nodes = new List<int>();
                for (int y = min; y <= max; y++)
                {
                    nodes.Clear(); int j = count - 1;
                    for (int i = 0; i < count; i++) { int yi = v[2 * i + 1], yj = v[2 * j + 1]; if ((yi < y && yj >= y) || (yj < y && yi >= y)) nodes.Add(v[2 * i] + (y - yi) * (v[2 * j] - v[2 * i]) / (yj - yi)); j = i; }
                    nodes.Sort(); for (int i = 0; i + 1 < nodes.Count; i += 2) Rect(nodes[i], y, nodes[i + 1] - nodes[i] + 1, 1, c);
                }
            }
            public Texture2D Texture(string name)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0 };
                t.SetPixels32(pixels); t.Apply(); return t;
            }
        }
    }
}

#if UNITY_EDITOR
namespace Lumia.Editor
{
    /// <summary>Point-filtered and lossless import is part of the pixel art presentation.</summary>
    public sealed class LumiaPixelArtImporter : UnityEditor.AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Lumia/", StringComparison.Ordinal)) return;
            var importer = (UnityEditor.TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.isReadable = assetPath.Contains("Atlas");
        }
    }
}
#endif
