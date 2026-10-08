using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in development screenshots verify dedicated art without changing a run.</summary>
    public sealed class LumiaArtVerification : MonoBehaviour
    {
        sealed class ArtItem
        {
            public string id, name, subtitle;
            public Texture2D texture;
            public bool present;
        }
        readonly List<ArtItem> sprites = new List<ArtItem>();
        readonly List<ArtItem> icons = new List<ArtItem>();
        readonly List<ArtItem> systems = new List<ArtItem>();
        GUIStyle text, title;
        bool showingSprites;
        bool showingSystems;
        int page;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartVerification()
        {
            if (Debug.isDebugBuild && (Array.IndexOf(Environment.GetCommandLineArgs(), "-lumia-art-verify") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-lumia-portrait-verify") >= 0))
                new GameObject("Lumia art verification").AddComponent<LumiaArtVerification>();
        }

        IEnumerator Start()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ArtVerification"));
            Directory.CreateDirectory(output);
            int missing = 0;
            bool portraitOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-lumia-portrait-verify")>=0;
            sprites.Add(new ArtItem { id = "hana", name = "하나", texture = PixelArt.Portrait("hana"), present = PixelArt.HasIdentityPortrait("hana") });
            foreach (var character in GameDatabase.Characters)
                sprites.Add(new ArtItem { id = character.id, name = character.name, texture = PixelArt.Portrait(character.id), present = PixelArt.HasIdentityPortrait(character.id) });
            foreach (var card in portraitOnly?new List<CardDef>():GameDatabase.Cards)
                icons.Add(new ArtItem { id = card.id, name = card.name, subtitle = card.owner + " " + card.key, texture = PixelArt.SkillIcon(card.id), present = PixelArt.HasSkillIcon(card.id) });
            foreach (var passive in portraitOnly?new List<PassiveDef>():GameDatabase.Passives)
                icons.Add(new ArtItem { id = passive.id, name = passive.name, subtitle = passive.owner + " T", texture = PixelArt.PassiveIcon(passive.id), present = PixelArt.HasPassiveIcon(passive.id) });
            if(!portraitOnly)
            {
                foreach(var rune in GameDatabase.Runes) AddSystem(rune.id,rune.name,(rune.main?"메인":"보조")+" 룬");
                foreach(var item in GameDatabase.Objects) AddSystem(item.id,item.name,"오브젝트");
                foreach(var gear in GameDatabase.Gear) AddSystem(gear.id,gear.name,gear.rarity+" 장비");
                AddSystem("credits","크레딧","게임 재화");AddSystem("campfire","모닥불","휴식과 제작");
            }
            foreach (var item in sprites) if (!item.present) { missing++; Debug.LogError("LUMIA MISSING PORTRAIT " + item.id); }
            foreach (var item in icons) if (!item.present) { missing++; Debug.LogError("LUMIA MISSING SKILL ICON " + item.id); }
            foreach(var item in systems) if(!item.present){missing++;Debug.LogError("LUMIA MISSING SYSTEM ICON "+item.id);}
            string portraitOutput=Path.Combine(output,"IdentityPortraits");Directory.CreateDirectory(portraitOutput);
            foreach(var item in sprites)
            {
                if(item.texture.height>PixelArt.PortraitPixelHeight) {missing++;Debug.LogError("LUMIA PORTRAIT GRID "+item.id);}
                ExportTexture(item.texture,Path.Combine(portraitOutput,item.id+".png"));
            }
            File.WriteAllText(Path.Combine(output, "coverage.txt"), "Dedicated sprites: " + sprites.Count + "\nDedicated icons: " + icons.Count + "\nSystem icons: "+systems.Count+"\nPortrait pixel height: "+PixelArt.PortraitPixelHeight+"\nMissing assets: " + missing + "\n");
            for (int kind = 0; kind < 3; kind++)
            {
                showingSprites = kind == 0;
                showingSystems=kind==2;
                int pages = (int)Math.Ceiling((showingSprites ? sprites.Count / 12d : showingSystems?systems.Count/40d:icons.Count / 40d));
                for (page = 0; page < pages; page++)
                {
                    yield return new WaitForSecondsRealtime(.35f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, (showingSprites ? "sprites_" : showingSystems?"systems_":"icons_") + (page + 1).ToString("00") + ".png"));
                    yield return new WaitForSecondsRealtime(.35f);
                }
            }
            Debug.Log("LUMIA ART COVERAGE sprites=" + sprites.Count + " icons=" + icons.Count + " systems="+systems.Count+" missing=" + missing);
            Application.Quit(missing == 0 ? 0 : 1);
        }

        void AddSystem(string id,string name,string kind) => systems.Add(new ArtItem{id=id,name=name,subtitle=kind,texture=PixelArt.SystemIcon(id),present=PixelArt.HasSystemIcon(id)});
        static void ExportTexture(Texture2D source,string path)
        {
            var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Texture2D copy=null;
            try {Graphics.Blit(source,target);RenderTexture.active=target;copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);copy.Apply();File.WriteAllBytes(path,copy.EncodeToPNG());}
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(copy!=null)UnityEngine.Object.Destroy(copy);}
        }

        void OnGUI()
        {
            GUI.depth = -100;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Math.Min(Screen.width / 1280f, Screen.height / 720f) * 1280) / 2, (Screen.height - Math.Min(Screen.width / 1280f, Screen.height / 720f) * 720) / 2, 0), Quaternion.identity, Vector3.one * Math.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.color = new Color(.04f, .08f, .12f); GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (text == null)
            {
                Font font = Resources.Load<Font>("Lumia/Galmuri11");
                text = new GUIStyle { font = font, fontSize = 11, alignment = TextAnchor.UpperCenter, wordWrap = true };
                text.normal.textColor = new Color(.88f, .94f, .91f);
                title = new GUIStyle(text) { fontSize = 22 };
            }
            var items = showingSprites ? sprites : showingSystems?systems:icons;
            int size = showingSprites ? 12 : 40;
            GUI.Label(new Rect(30, 22, 1220, 37), (showingSprites ? "눈매와 표정이 살아 있는 픽셀 실험체" : showingSystems?"룬 · 크레딧 · 모닥불 · 오브젝트 · 장비":"실험체 · 무기 · 전술 · 패시브 아이콘") + "  /  " + (page + 1) + " / " + Math.Ceiling(items.Count / (double)size), title);
            for (int index = page * size; index < Math.Min(items.Count, (page + 1) * size); index++)
            {
                var item = items[index]; int local = index % size;
                float x = showingSprites ? 34 + local % 6 * 204 : 32 + local % 10 * 123;
                float y = showingSprites ? 83 + local / 6 * 288 : 82 + local / 10 * 153;
                Rect cell = new Rect(x, y, showingSprites ? 188 : 112, showingSprites ? 265 : 139);
                GUI.color = item.present ? new Color(.07f, .15f, .2f) : new Color(.4f, .06f, .06f); GUI.DrawTexture(cell, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTexture(showingSprites ? new Rect(x + 22, y + 8, 144, 204) : new Rect(x + 24, y + 9, 64, 64), item.texture, ScaleMode.ScaleToFit, true);
                GUI.Label(showingSprites ? new Rect(x + 6, y + 218, 176, 23) : new Rect(x + 3, y + 81, 106, 31), item.name, text);
                GUI.Label(showingSprites ? new Rect(x + 6, y + 244, 176, 18) : new Rect(x + 3, y + 115, 106, 22), showingSprites ? item.id : item.subtitle, text);
            }
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
