using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    /// <summary>Resolution independent, bitmap-font interface. All gameplay lives in GameEngine.</summary>
    public sealed class LumiaGame : MonoBehaviour
    {
        public static LumiaGame Instance;
        public GameEngine Engine { get; private set; }
        const float W = 1280, H = 720;
        static readonly Color Ink = C("0b1421"), Panel = C("142233"), Line = C("33465b"), Text = C("dce8e1"), Muted = C("acbdc8"), Mint = C("91dfbd"), Gold = C("efc979"), Pink = C("dd88ae");
        Font font;
        GUIStyle label, centered, wrapped, button, field;
        bool initialized, lobby = true, settings, catalog, inventory, help, fieldInfo, fieldEnemy, inspectEnemyCard, summaryMode = true, enemyLoadout;
        int prepStep, inventoryTab, catalogPage;
        string catalogQuery = "", craftPending = "", inspectCard = "", inspectTrait = "", inspectGear = "";
        readonly Dictionary<string, Vector2> summaryScroll = new Dictionary<string, Vector2>();
        int currentLayer, topLayer;
        GearSlot craftSlot;
        Vector2 scroll, inventoryScroll, handScroll, mapScroll, detailScroll, fieldScroll, encounterStoryScroll;
        int displayedTurn = -1;
        int mapAct = -1, mapRow = -99;
        readonly CombatEffects effects = new CombatEffects();
        readonly Dictionary<string, AudioClip> skillSounds = new Dictionary<string, AudioClip>();
        float nextEnemyActionAt, nextPlayerActionAt;
        float volume = .4f;
        bool sound = true, reduceMotion;
        AudioSource audioSource;
        AudioClip click;
        string savePath;
        string toast = "";
        float toastUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<LumiaGame>() == null)
                new GameObject(GameIdentity.ProductName).AddComponent<LumiaGame>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            bool verify = Debug.isDebugBuild && (Array.IndexOf(Environment.GetCommandLineArgs(), "-lumia-verify") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-lumia-art-verify") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-lumia-portrait-verify") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-identity-verify") >= 0);
            savePath = Path.Combine(Application.persistentDataPath, verify ? "lumia-verification.json" : GameIdentity.SaveFileName);
            GameIdentity.MigrateLegacyData(savePath, verify);
            font = Resources.Load<Font>("Lumia/Galmuri11");
            if (!font) font = Font.CreateDynamicFontFromOSFont(new[] { "Gulim", "Malgun Gothic", "Arial" }, 16);
            volume = PlayerPrefs.GetFloat("lumia.volume", .4f);
            sound = PlayerPrefs.GetInt("lumia.sound", 1) != 0;
            reduceMotion = PlayerPrefs.GetInt("lumia.reduceMotion", 0) != 0;
            audioSource = gameObject.AddComponent<AudioSource>();
            click = AudioClip.Create("pixel select", 1600, 1, 22050, false);
            float[] wave = new float[1600];
            for (int i = 0; i < wave.Length; i++) wave[i] = (i % 42 < 21 ? .12f : -.12f) * (1f - (float)i / wave.Length);
            click.SetData(wave, 0);
            PixelArt.Ensure();
            if (font && font.material && font.material.mainTexture) font.material.mainTexture.filterMode = FilterMode.Point;
            Font.textureRebuilt += PixelFont;
        }

        void InitStyles()
        {
            if (initialized) return;
            initialized = true;
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, padding = new RectOffset(0, 0, 0, 0), richText = false };
            label.normal.textColor = Text;
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            wrapped = new GUIStyle(label) { wordWrap = true };
            button = new GUIStyle(centered); button.normal.background = null; button.hover.background = null; button.active.background = null;
            field = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 16, padding = new RectOffset(12, 12, 8, 8) };
            field.normal.textColor = Text; field.focused.textColor = Mint;
            field.normal.background = Flat(Panel); field.focused.background = Flat(C("203b40")); field.hover.background = field.normal.background;
            PixelControl(GUI.skin.horizontalSlider, Line, 0, 5);
            PixelControl(GUI.skin.horizontalSliderThumb, Mint, 15, 20);
            PixelControl(GUI.skin.verticalScrollbar, Ink, 12, 0);
            PixelControl(GUI.skin.verticalScrollbarThumb, Mint, 12, 0);
            PixelControl(GUI.skin.horizontalScrollbar, Ink, 0, 12);
            PixelControl(GUI.skin.horizontalScrollbarThumb, Mint, 0, 12);
            PixelControl(GUI.skin.verticalScrollbarUpButton, Line, 12, 12);
            PixelControl(GUI.skin.verticalScrollbarDownButton, Line, 12, 12);
            PixelControl(GUI.skin.horizontalScrollbarLeftButton, Line, 12, 12);
            PixelControl(GUI.skin.horizontalScrollbarRightButton, Line, 12, 12);
        }

        void Update()
        {
            if (Engine == null || lobby) return;
            ConsumeCombatActions();
            var c = Engine.State.combat;
            if (Engine.State.stage == RunStage.Combat && c != null && c.enemyTurn && !effects.Busy && Time.unscaledTime >= nextEnemyActionAt
                && !settings && !inventory && !catalog && !help && !fieldInfo && !enemyLoadout && string.IsNullOrEmpty(inspectGear) && string.IsNullOrEmpty(inspectCard) && string.IsNullOrEmpty(inspectTrait))
            {
                if (Engine.AdvanceEnemyAction()) Save();
                ConsumeCombatActions();
                nextEnemyActionAt = Time.unscaledTime + (reduceMotion ? .08f : .18f);
            }
        }

        void ConsumeCombatActions()
        {
            if (Engine == null || Engine.CombatActions.Count == 0) return;
            foreach (var action in Engine.CombatActions)
            {
                effects.Add(action, reduceMotion);
                if (sound && audioSource)
                {
                    AudioClip clip;
                    if (!skillSounds.TryGetValue(action.cardId, out clip))
                    {
                        clip = CombatEffects.SoundFor(GameDatabase.Card(action.cardId)); skillSounds[action.cardId] = clip;
                    }
                    audioSource.PlayOneShot(clip, volume);
                }
            }
            Engine.CombatActions.Clear();
        }

        Texture2D Flat(Color color) { var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point }; t.SetPixel(0, 0, color); t.Apply(); return t; }
        void PixelControl(GUIStyle style, Color color, int width, int height)
        {
            var t = Flat(color); style.normal.background = style.hover.background = style.active.background = style.focused.background = t;
            style.border = new RectOffset(); style.padding = new RectOffset();
            style.fixedWidth = width; style.fixedHeight = height;
        }

        void OnGUI()
        {
            InitStyles();
            float scale = Mathf.Min(Screen.width / W, Screen.height / H);
            float ox = (Screen.width - W * scale) / 2, oy = (Screen.height - H * scale) / 2;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            currentLayer = 0;
            topLayer = !string.IsNullOrEmpty(inspectCard) || !string.IsNullOrEmpty(inspectTrait) || !string.IsNullOrEmpty(inspectGear) ? 6 : fieldInfo || enemyLoadout ? 5 : help ? 4 : settings ? 3 : inventory ? 2 : catalog || !string.IsNullOrEmpty(craftPending) ? 1 : 0;
            GUI.enabled = topLayer == 0;
            Fill(new Rect(-ox / scale, -oy / scale, Screen.width / scale, Screen.height / scale), Ink);
            if (lobby || Engine == null) DrawLobby();
            else
            {
                switch (Engine.State.stage)
                {
                    case RunStage.Preparation: DrawPreparation(); break;
                    case RunStage.Map: DrawMap(); break;
                    case RunStage.Combat: DrawCombat(); break;
                    case RunStage.Rewards: DrawRewards(); break;
                    case RunStage.Kiosk: DrawKiosk(); break;
                    case RunStage.Campfire: DrawCamp(); break;
                    case RunStage.Encounter: DrawEncounter(); break;
                    case RunStage.PassiveChoice: DrawPassiveChoice(); break;
                    case RunStage.Won: case RunStage.Lost: DrawEnding(); break;
                }
                currentLayer = 0;
                if (Btn(new Rect(1174, 18, 84, 32), "메뉴", false, Muted)) { lobby = true; Save(); }
            }
            if (!lobby && Engine != null && (Engine.State.stage == RunStage.Combat || Engine.State.stage == RunStage.Rewards || Engine.State.stage == RunStage.Lost)) effects.Draw(label, reduceMotion);
            if (catalog) { currentLayer = 1; GUI.enabled = topLayer == 1; DrawCatalog(); }
            if (inventory && Engine != null) { currentLayer = 2; GUI.enabled = topLayer == 2; DrawInventory(); }
            if (settings) { currentLayer = 3; GUI.enabled = topLayer == 3; DrawSettings(); }
            if (help) { currentLayer = 4; GUI.enabled = topLayer == 4; DrawHelp(); }
            if (fieldInfo) { currentLayer = 5; GUI.enabled = topLayer == 5; DrawFieldDetail(); }
            if (enemyLoadout) { currentLayer = 5; GUI.enabled = topLayer == 5; DrawEnemyLoadout(); }
            if (!string.IsNullOrEmpty(inspectCard)) { currentLayer = 6; GUI.enabled = true; DrawCardDetail(); }
            if (!string.IsNullOrEmpty(inspectTrait)) { currentLayer = 6; GUI.enabled = true; DrawTraitDetail(); }
            if (!string.IsNullOrEmpty(inspectGear)) { currentLayer = 6; GUI.enabled = true; DrawGearDetail(); }
            GUI.enabled = true;
            if (Time.unscaledTime < toastUntil)
            {
                Box(new Rect(280, 650, 720, 44), Ink, Gold);
                Txt(new Rect(292, 658, 696, 28), toast, 14, Gold, true);
            }
        }

        void DrawLobby()
        {
            Tex(new Rect(0, 0, W, H), PixelArt.Background(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, 540, H), new Color(.025f, .06f, .09f, .88f));
            Fill(new Rect(0, 0, W, 5), Mint);
            Txt(new Rect(62, 50, 460, 22), "AGLAIA  /  EXPERIMENT # 00", 14, Mint);
            Txt(new Rect(58, 107, 460, 70), "Black", 64, Text);
            Txt(new Rect(63, 180, 460, 42), "The Survival", 33, Mint);
            Fill(new Rect(64, 236, 62, 3), Mint);
            Txt(new Rect(64, 259, 412, 58), "그녀의 게임 속으로 떨어진 연구원.\n한 장의 카드로, 실험의 끝을 바꾼다.", 16, Muted);
            if (MenuButton(347, "01", "새로운 실험", "룬을 선택하고 루미아로 진입")) NewRun();
            bool saved = File.Exists(savePath);
            if (MenuButton(415, "02", "이어하기", saved ? "저장된 실험 기록 불러오기" : "저장된 기록이 없습니다", saved)) Load();
            if (MenuButton(483, "03", "카드 도감", GameDatabase.Cards.Count + "장의 스킬 기록")) { catalog = true; catalogPage = 0; }
            if (MenuButton(551, "04", "환경 설정", "음량 · 화면 · 접근성")) settings = true;
            Box(new Rect(901, 547, 311, 84), new Color(.025f, .06f, .09f, .90f), Line);
            Txt(new Rect(922, 559, 272, 22), "니아  /  GAME MASTER", 14, Pink);
            Txt(new Rect(922, 590, 272, 22), "“이번 판은, 탈출할 수 있을까?”", 14, Text);
            Fill(new Rect(0, 670, W, 50), new Color(.025f, .06f, .09f, .94f));
            Txt(new Rect(25, 680, 1230, 30), "이 게임은 님블뉴런이 개발한 IP ‘이터널 리턴’을 기반으로 한 비공식 팬 게임입니다. 모든 저작권은 님블뉴런에 귀속됩니다.", 14, Muted, true);
        }

        bool MenuButton(float y, string n, string title, string sub, bool enabled = true)
        {
            Rect r = new Rect(62, y, 418, 58);
            bool hover = r.Contains(Event.current.mousePosition) && enabled;
            Box(r, hover ? C("254239") : C("14222d"), hover ? Mint : Line);
            Txt(new Rect(78, y + 17, 40, 22), n, 14, enabled ? Mint : Muted);
            Txt(new Rect(125, y + 8, 290, 26), title, 20, enabled ? Text : Muted);
            Txt(new Rect(125, y + 35, 310, 20), sub, 11, Muted);
            Txt(new Rect(449, y + 17, 24, 26), ">", 20, enabled ? Mint : Muted);
            return Hit(r, enabled);
        }

        void NewRun()
        {
            Engine = new GameEngine(Environment.TickCount & int.MaxValue);
            lobby = false; prepStep = 0; scroll = handScroll = mapScroll = detailScroll = Vector2.zero; displayedTurn = -1;
            mapAct = -1; mapRow = -99; effects.Clear(); nextEnemyActionAt = nextPlayerActionAt = 0;
            var main = GameDatabase.Runes.FirstOrDefault(x => x.main);
            if (main != null)
            {
                var support = GameDatabase.Runes.FirstOrDefault(x => !x.main && x.tree == main.tree);
                if (support != null) Engine.SetRunes(main.id, support.id);
            }
            Save();
        }

        void Header(string title, string subtitle)
        {
            Fill(new Rect(0, 0, W, 80), Ink);
            Fill(new Rect(0, 78, W, 2), Line);
            Txt(new Rect(26, 17, 530, 29), title, 22, Text);
            Txt(new Rect(27, 48, 630, 24), subtitle, 11, Muted);
            if (Engine == null) return;
            var s = Engine.State;
            Txt(new Rect(697, 15, 90, 23), "LV. " + s.level.ToString("00"), 16, Mint);
            Txt(new Rect(799, 15, 174, 23), "HP " + s.hp + " / " + s.maxHp, 16, Pink);
            Tex(new Rect(985,12,27,27),PixelArt.SystemIcon("credits"),ScaleMode.ScaleToFit);
            Txt(new Rect(1021, 15, 134, 23), s.credits + " CR", 16, Gold);
            Bar(new Rect(799, 46, 170, 7), s.hp, s.maxHp, Pink);
            if (s.stage != RunStage.Preparation && Btn(new Rect(985, 44, 170, 24), "덱 / 장비 / 가방", false, Muted)) { inventory = true; inventoryScroll = Vector2.zero; }
        }

        void DrawPreparation()
        {
            Header("실험 준비", "하나의 관측 기록  /  VF 세계 진입 전 최종 점검");
            for (int i = 0; i < 3; i++)
            {
                string[] labels = { "01  룬 설정", "02  시작 패시브", "03  시작 카드" };
                Box(new Rect(32 + i * 409, 102, 398, 43), i == prepStep ? C("254239") : Panel, i == prepStep ? Mint : Line);
                Txt(new Rect(42 + i * 409, 112, 378, 25), labels[i], 16, i == prepStep ? Mint : Muted, true);
            }
            if (prepStep == 0) DrawRuneSelection(false);
            else if (prepStep == 1)
            {
                Txt(new Rect(36, 170, 1160, 30), "첫 번째 능력  /  실험체의 패시브를 하나 선택하세요.", 20, Text);
                var offers = Engine.State.passiveOffers.ToArray();
                for (int i = 0; i < offers.Length; i++)
                {
                    var p = GameDatabase.Passive(offers[i]);
                    if (p == null) continue;
                    Rect r = new Rect(36 + i * 410, 220, 388, 300);
                    Box(r, Panel, Engine.State.chosenPassive == p.id ? Mint : Line);
                    Tex(new Rect(r.x + 98, r.y + 42, 72, 72), PixelArt.PassiveIcon(p.id), ScaleMode.ScaleToFit);
                    Tex(new Rect(r.x + 190, r.y + 22, 104, 110), PixelArt.Portrait(p.owner), ScaleMode.ScaleToFit);
                    Txt(new Rect(r.x + 20, r.y + 145, 348, 27), p.owner + " · " + p.name, 20, Mint, true);
                    FitText(new Rect(r.x + 25, r.y + 182, 338, 57), TraitSummary(p.mechanics, p.description), 14, Text);
                    TraitDetailButton(new Rect(r.x + 25, r.y + 235, 338, 18), p.id);
                    Para(new Rect(r.x + 25, r.y + 254, 338, 36), PassiveAffinity(p), 13, Mint);
                    if (Hit(r)) Act(() => Engine.SelectStartingPassive(p.id));
                }
                if (Btn(new Rect(39, 547, 240, 43), "다시 뽑기  ·  " + Engine.State.passiveRerolls, false, Gold, Engine.State.passiveRerolls > 0)) Act(Engine.RerollPassives);
                if (Btn(new Rect(999, 626, 241, 48), "카드 선택으로  >", true, Mint, !string.IsNullOrEmpty(Engine.State.chosenPassive))) prepStep = 2;
            }
            else
            {
                Txt(new Rect(36, 165, 1100, 32), "기본 공격 5장 · 방어 3장 + 스킬 카드 3장", 20, Text);
                Txt(new Rect(36, 203, 1100, 23), "선택 " + Engine.State.draftSelected.Count + " / 3  ·  시작 선택지는 코스트 0~4입니다. 리롤하면 선택하지 않은 카드만 바뀝니다.", 14, Mint);
                var offers = Engine.State.draftOffers.ToArray();
                for (int i = 0; i < offers.Length; i++)
                {
                    Rect r = new Rect(36 + i * 205, 247, 188, 276);
                    DrawCard(r, offers[i], Engine.State.draftSelected.Contains(offers[i]));
                    if (Hit(r)) Act(() => Engine.ToggleDraft(offers[i]));
                }
                if (Btn(new Rect(36, 546, 242, 43), "다시 뽑기  ·  " + Engine.State.draftRerolls, false, Gold, Engine.State.draftRerolls > 0)) Act(Engine.RerollDraft);
                if (Btn(new Rect(999, 626, 241, 48), "루미아로 진입  >", true, Mint, Engine.State.draftSelected.Count == 3)) Act(Engine.BeginJourney);
            }
            if (prepStep > 0 && Btn(new Rect(36, 626, 162, 48), "<  이전", false, Muted)) prepStep--;
        }

        void DrawRuneSelection(bool eventChange)
        {
            Txt(new Rect(36, 170, 1150, 28), "메인 룬 1개 + 같은 계열의 보조 룬 1개", 20, Text);
            Txt(new Rect(36, 209, 1150, 25), "룬은 특정 조우 이벤트에서만 변경할 수 있습니다.", 14, Muted);
            var mains = GameDatabase.Runes.Where(x => x.main).ToArray();
            for (int i = 0; i < mains.Length; i++)
            {
                var r = mains[i];
                int columns = Math.Min(mains.Length, 4);
                Rect box = new Rect(36 + (i % columns) * (1206f / columns), 257 + (i / columns) * 101, 1206f / columns - 14, 91);
                Box(box, Panel, r.id == Engine.State.mainRune ? Mint : Line);
                Tex(new Rect(box.x+12,box.y+13,40,40),PixelArt.SystemIcon(r.id),ScaleMode.ScaleToFit);
                Txt(new Rect(box.x + 64, box.y + 10, box.width - 78, 25), r.name + " / " + r.tree, 15, Mint);
                FitText(new Rect(box.x + 64, box.y + 36, box.width - 78, 32), TraitSummary(r.mechanics,r.description), 11, Text);
                TraitDetailButton(new Rect(box.x + 14,box.y + 70,box.width - 28,16),r.id);
                if (Hit(box))
                {
                    var support = GameDatabase.Runes.FirstOrDefault(x => !x.main && x.tree == r.tree);
                    if (support != null) Act(() => Engine.SetRunes(r.id, support.id));
                }
            }
            var selected = GameDatabase.Rune(Engine.State.mainRune);
            if (selected != null)
            {
                var supports = GameDatabase.Runes.Where(x => !x.main && x.tree == selected.tree).ToArray();
                for (int i = 0; i < supports.Length; i++)
                {
                    Rect box = new Rect(36 + i * (1206f / Math.Max(1, supports.Length)), 507, 1206f / Math.Max(1, supports.Length) - 14, 81);
                    var r = supports[i];
                    Box(box, Panel, r.id == Engine.State.supportRune ? Gold : Line);
                    Tex(new Rect(box.x+12,box.y+12,40,40),PixelArt.SystemIcon(r.id),ScaleMode.ScaleToFit);
                    Txt(new Rect(box.x + 64, box.y + 10, box.width - 78, 23), r.name, 16, Gold);
                    FitText(new Rect(box.x + 64, box.y + 35, box.width - 78, 27), TraitSummary(r.mechanics,r.description), 11, Text);
                    TraitDetailButton(new Rect(box.x + 14,box.y + 62,box.width - 28,16),r.id);
                    if (Hit(box)) Act(() => Engine.SetRunes(selected.id, r.id));
                }
            }
            if (Btn(new Rect(999, 626, 241, 48), eventChange ? "룬 변경 완료  >" : "패시브 선택으로  >", true, Mint, !string.IsNullOrEmpty(Engine.State.supportRune)))
            {
                if (eventChange) Act(Engine.FinishRuneChange); else prepStep = 1;
            }
        }

        void DrawMap()
        {
            var s = Engine.State;
            Header("루미아 탐색 지도", "ACT " + s.act + " / 03  ·  연결된 구역을 선택하세요.  ·  노드 방문 시 자동 저장");
            Tex(new Rect(0, 80, 905, 640), PixelArt.MapBackground(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 80, 905, 640), new Color(.035f, .07f, .10f, .74f));
            string[] actNames = { "숲의 잔향", "금지구역의 신호", "게임 월드의 끝" };
            Txt(new Rect(35, 110, 800, 35), "0" + s.act + "  /  " + actNames[s.act - 1], 25, Text);
            Txt(new Rect(35, 153, 800, 23), "막당 " + s.mapRows + "구역  ·  현재 " + Math.Max(0, s.row + 1) + " / " + s.mapRows + "  ·  지도를 가로로 스크롤할 수 있습니다.", 14, Muted);
            var nodes = s.map.Where(x => x.act == s.act).ToArray();
            var reachable = Engine.AvailableNodes();
            if (mapAct != s.act || mapRow != s.row)
            {
                mapScroll = new Vector2(Math.Max(0, (s.row - 2) * 119), 0); mapAct = s.act; mapRow = s.row;
            }
            if (Btn(new Rect(35, 186, 96, 29), "< 이전", false, Muted)) mapScroll.x = Math.Max(0, mapScroll.x - 238);
            if (Btn(new Rect(770, 186, 96, 29), "다음 >", false, Muted)) mapScroll.x = Math.Min(Math.Max(0, s.mapRows * 119 + 40 - 843), mapScroll.x + 238);
            int selectedLane = -1;
            mapScroll = GUI.BeginScrollView(new Rect(27, 225, 843, 371), mapScroll, new Rect(0, 0, Math.Max(843, s.mapRows * 119 + 40), 348));
            foreach (var n in nodes)
                foreach (var next in nodes.Where(x => x.row == n.row + 1 && Math.Abs(x.lane - n.lane) <= 1))
                    PixelLine(NodeRect(n).center, NodeRect(next).center, n.visited && next.visited ? Mint : C("2a3b4b"), 2);
            foreach (var n in nodes)
            {
                Rect r = NodeRect(n);
                bool available = reachable.Any(x => x.id == n.id);
                Color tone = n.kind == ZoneKind.Boss ? Pink : n.kind == ZoneKind.Kiosk ? Gold : n.kind == ZoneKind.Campfire ? C("dfac7d") : Mint;
                Box(r, n.visited ? C("2b4540") : available ? C("20394a") : C("142231"), available ? tone : n.visited ? Mint : Line);
                Tex(new Rect(r.x + 12, r.y + 9, 36, 36), PixelArt.Icon(n.kind.ToString()), ScaleMode.ScaleToFit, available || n.visited ? Color.white : Muted);
                Txt(new Rect(r.x - 22, r.y + 65, 106, 22), ZoneName(n.kind), 11, available ? tone : Muted, true);
                if (n.nearKiosk && n.kind == ZoneKind.Subject) Txt(new Rect(r.x - 14, r.y - 22, 92, 20), "키오스크 주변", 9, Gold, true);
                if (Hit(r, available)) selectedLane = n.lane;
            }
            for (int i = 0; i < s.mapRows; i++) Txt(new Rect(8 + i * 119, 8, 94, 24), (i + 1).ToString("00"), 11, Muted, true);
            GUI.EndScrollView();
            if (selectedLane >= 0) { Act(() => Engine.EnterNode(selectedLane)); return; }
            Sidebar(921, 103);
            Box(new Rect(27, 613, 843, 69), Panel, Line);
            Txt(new Rect(43, 625, 811, 23), "야생동물  /  실험체  /  키오스크  /  모닥불  /  조우  /  보스", 11, Text);
            Txt(new Rect(43, 650, 811, 20), "늑대·곰은 희귀 오브젝트를 드롭합니다. 키오스크 주변 실험체도 오브젝트를 보유할 수 있습니다.", 11, Muted);
        }
        Rect NodeRect(MapNode n) { return new Rect(25 + n.row * 119, 53 + n.lane * 102, 60, 60); }

        void Sidebar(float x, float y)
        {
            var s = Engine.State;
            Box(new Rect(x, y, 331, 573), Panel, Line);
            Tex(new Rect(x + 20, y + 21, 80, 104), PixelArt.Portrait("hana"), ScaleMode.ScaleToFit);
            Txt(new Rect(x + 119, y + 28, 190, 28), "하나", 25, Text);
            Txt(new Rect(x + 119, y + 66, 190, 23), "AGLAIA RESEARCHER", 11, Mint);
            Txt(new Rect(x + 20, y + 139, 291, 28), "경험치  " + s.xp + " / " + Engine.NextLevelXp, 11, Muted);
            Txt(new Rect(x + 20, y + 173, 291, 23), "최대 코스트  " + Engine.MaxEnergy, 16, Gold);
            Txt(new Rect(x + 20, y + 219, 291, 22), "RUNE / 룬", 11, Muted);
            var main = GameDatabase.Rune(s.mainRune); var support = GameDatabase.Rune(s.supportRune);
            if(main!=null) Tex(new Rect(x+20,y+244,25,25),PixelArt.SystemIcon(main.id),ScaleMode.ScaleToFit);
            if(support!=null) Tex(new Rect(x+20,y+278,25,25),PixelArt.SystemIcon(support.id),ScaleMode.ScaleToFit);
            Txt(new Rect(x + 54, y + 245, 257, 25), main == null ? "—" : main.name, 16, Mint);
            Txt(new Rect(x + 54, y + 278, 257, 23), support == null ? "—" : support.name, 14, Text);
            Txt(new Rect(x + 20, y + 322, 291, 22), "PASSIVE / " + s.passives.Count + " OF 3", 11, Muted);
            for (int i = 0; i < s.passives.Count; i++)
            {
                var p = GameDatabase.Passive(s.passives[i]);
                if (p != null) Txt(new Rect(x + 20, y + 351 + i * 35, 291, 25), p.owner + " · " + p.name, 14, Text);
            }
            Txt(new Rect(x + 20, y + 477, 291, 26), "덱 " + s.deck.Count + "장  /  장비 " + s.gear.Count + "개", 14, Muted);
            if (Btn(new Rect(x + 20, y + 520, 291, 34), "실험 가이드  ?", false, Muted)) help = true;
        }

        void DrawCombat()
        {
            var s = Engine.State; var c = s.combat;
            if (c == null) return;
            Header("전투  /  " + c.enemyName, "TURN " + c.turn + (c.enemyTurn ? "  ·  상대가 기술을 사용하고 있습니다." : "  ·  무료 연계 카드는 코스트를 모두 써도 사용할 수 있습니다."));
            Tex(new Rect(0, 80, W, 359), PixelArt.BattleBackground(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 80, W, 359), new Color(.02f, .04f, .07f, .2f));
            DrawFieldStrip(new Rect(22, 101, 396, 61), false);
            DrawFieldStrip(new Rect(862, 101, 396, 61), true);
            Box(new Rect(440, 104, 399, 99), Ink, Pink);
            Txt(new Rect(452, 116, 375, 24), (c.enemyTurn ? "상대 기술 " + c.enemyActionIndex + " / " + c.enemyPlan.Count : "상대의 다음 행동") + "  /  최대 코스트 " + GameEngine.EnergyForLevel(c.enemyLevel), 14, Pink, true);
            string intent = Engine.EnemyIntent;
            if (c.enemyTurn)
            {
                var pending = c.enemyPlan.Skip(c.enemyActionIndex).Select(id => GameDatabase.Card(id)?.name ?? "").ToArray();
                intent = pending.Length == 0 ? "마지막 기술을 마무리하고 있습니다." : string.Join(" · ", pending) + "  / 남은 예정 피해 " + c.intentDamage + " · 방어 " + c.intentBlock;
            }
            if (c.enemyPlan.Count > 0)
            {
                var planned = c.enemyPlan.Skip(c.enemyTurn ? c.enemyActionIndex : 0).Take(6).ToArray();
                for (int i = 0; i < planned.Length; i++)
                {
                    Rect icon = new Rect(455 + i * 31, 146, 25, 25);
                    Tex(icon, PixelArt.SkillIcon(planned[i]), ScaleMode.ScaleToFit);
                    if (Hit(icon)) { inspectCard = planned[i]; inspectEnemyCard=true; detailScroll = Vector2.zero; }
                }
                Para(new Rect(454, 176, 371, 25), "예정 피해 " + c.intentDamage + " · 방어 " + c.intentBlock + "  / 적중 시 연계·턴 종료 포함", 10, Text);
            }
            else Para(new Rect(454, 148, 371, 40), intent, 11, Text);
            float bob = reduceMotion ? 0 : Mathf.Floor(Mathf.Sin(Time.unscaledTime * 2) * 2) * 2;
            Vector2 po = effects.Offset(false, reduceMotion), eo = effects.Offset(true, reduceMotion);
            Tex(new Rect(129 + po.x, 169 + bob + po.y, 214, 215), PixelArt.Portrait("hana"), ScaleMode.ScaleToFit);
            Tex(new Rect(913 + eo.x, 169 - bob + eo.y, 218, 216), PixelArt.Portrait(c.enemyId), ScaleMode.ScaleToFit);
            Txt(new Rect(142, 388, 188, 22), "하나  ·  방어 " + c.block, 14, Mint, true);
            Txt(new Rect(873, 388, 292, 22), c.enemyName + "  LV." + c.enemyLevel + "  방어 " + c.enemyBlock, 14, Pink, true);
            Bar(new Rect(873, 419, 292, 9), c.enemyHp, c.enemyMaxHp, Pink);
            Txt(new Rect(886, 353, 266, 24), c.enemyHp + " / " + c.enemyMaxHp, 14, Text, true);
            Txt(new Rect(343, 220, 516, 27), "ENERGY", 11, Gold, true);
            Txt(new Rect(343, 247, 516, 49), c.energy + " / " + Engine.MaxEnergy, 33, Gold, true);
            if (string.IsNullOrEmpty(c.animal) && Btn(new Rect(481, 322, 252, 33), "상대 패시브 · 장비 · D/F", false, Pink)) { enemyLoadout=true; inventoryScroll=Vector2.zero; }
            Fill(new Rect(0, 442, W, 278), Ink);
            Fill(new Rect(0, 441, W, 2), Line);
            Txt(new Rect(24, 454, 800, 22), "손패 " + c.hand.Count + "  /  뽑을 카드 " + c.drawPile.Count + "  /  버린 카드 " + c.discardPile.Count + "  /  소멸 " + c.exhaustPile.Count, 11, Muted);
            if (displayedTurn != c.turn) { handScroll = Vector2.zero; displayedTurn = c.turn; }
            var hand = c.hand.ToArray();
            handScroll = GUI.BeginScrollView(new Rect(24, 485, 1004, 229), handScroll, new Rect(0, 0, Math.Max(1004, hand.Length * 178), 212));
            bool cardUsed = false;
            for (int i = 0; i < hand.Length; i++)
            {
                Rect r = new Rect(i * 178, 0, 168, 212);
                bool playable = Engine.CanPlayCard(i) && Time.unscaledTime >= nextPlayerActionAt;
                DrawCard(r, hand[i], false, playable, true);
                if (Hit(r, playable)) { int index = i; Act(() => Engine.PlayCard(index)); nextPlayerActionAt = Time.unscaledTime + .18f; cardUsed = true; break; }
            }
            GUI.EndScrollView();
            if (cardUsed) return;
            if (Btn(new Rect(1070, 515, 186, 71), c.enemyTurn ? "상대 행동 중" : "턴 종료  >", true, Gold, !c.enemyTurn && Time.unscaledTime >= nextPlayerActionAt)) { Act(Engine.BeginEndTurn); return; }
            if (Btn(new Rect(1070, 601, 186, 38), "회복 아이템", false, Mint, !c.enemyTurn)) { inventory = true; inventoryTab = 2; inventoryScroll = Vector2.zero; }
            if (s.log.Count > 0) Para(new Rect(1070, 653, 186, 43), s.log[s.log.Count - 1], 11, Muted);
        }

        void DrawCard(Rect r, string id, bool selected = false, bool enabled = true, bool combat = false)
        {
            var d = GameDatabase.Card(id); if (d == null) return;
            Color tone = d.damage > 0 ? Pink : d.block > 0 ? Mint : Gold;
            Box(r, enabled ? Panel : C("111d28"), selected ? Gold : enabled ? tone : Line);
            Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, 32), C("203343"));
            int cost = combat ? Engine.EffectiveCardCost(id) : d.cost;
            bool free = combat && cost < d.cost;
            bool taxed=combat && cost>d.cost;
            bool sealedByStatus=combat && !StatusMechanics.CanUse(Engine.State.combat.playerStatuses,d);
            Color costTone=free?Mint:taxed?Pink:Gold;
            Box(new Rect(r.x + 7, r.y + 7, 26, 25), Ink, costTone);
            Txt(new Rect(r.x + 8, r.y + 6, 25, 25), cost.ToString(), 16, costTone, true);
            Txt(new Rect(r.x + 38, r.y + 11, r.width - 45, 20), sealedByStatus?"상태이상 · 사용 불가":free ? (cost == 0 ? "무료 연계 " : "코스트 할인 ") + d.key : taxed?"상태 코스트 +"+(cost-d.cost)+" / "+d.key:d.owner + " " + d.key, d.owner.Length > 7 || sealedByStatus ? 9 : 11, sealedByStatus?Pink:enabled ? Text : Muted);
            float artHeight = r.height >= 350 ? 84 : r.height >= 250 ? 56 : 36;
            var owner = GameDatabase.Characters.Find(x => x.id == d.owner || x.name == d.owner);
            float iconX = owner == null ? r.x + (r.width - artHeight) / 2 : r.x + 14;
            Rect iconRect = new Rect(iconX, r.y + 41, artHeight, artHeight);
            Box(iconRect, Ink, enabled ? tone : Line);
            Tex(new Rect(iconRect.x + 2, iconRect.y + 2, iconRect.width - 4, iconRect.height - 4), PixelArt.SkillIcon(d.id), ScaleMode.ScaleToFit, enabled ? Color.white : Muted);
            if (owner != null)
                Tex(new Rect(r.xMax - 14 - artHeight * .8f, r.y + 39, artHeight * .8f, artHeight + 4), PixelArt.Portrait(owner.id), ScaleMode.ScaleToFit, enabled ? Color.white : Muted);
            float nameY = r.y + 43 + artHeight;
            float nameHeight=r.height>=250?31:26;
            Para(new Rect(r.x + 9, nameY, r.width - 18, nameHeight), d.name + (selected ? " ✓" : ""), r.width < 130 || d.name.Length > 13 ? 11 : 13, tone);
            Rect body = new Rect(r.x + 11, nameY + nameHeight + 3, r.width - 22, Math.Max(22, r.yMax - nameY - nameHeight - 31));
            SummaryText(body, CardSummary(d, combat), 11, enabled ? Text : Muted,"card/"+id+"/"+r.width+"/"+r.height);
            if (inspectCard != id)
            {
                Rect info = new Rect(r.x + 7, r.yMax - 25, r.width - 14, 19);
                Fill(info, C("203343")); Txt(info, selected ? "선택됨 · 상세 설명" : d.freeCastCount > 0 ? "무료 연계 · 상세 설명 +" : "상세 설명  +", 9, selected ? Gold : Muted, true);
                if (Hit(info)) { inspectCard = id; inspectEnemyCard=false; summaryMode=true; detailScroll = Vector2.zero; }
            }
        }

        string CardSummary(CardDef d, bool combat)
        {
            bool upgraded = combat && Engine.State.upgrades.Contains(d.id);
            return DescriptionSummary.Card(d, GameDatabase.Cards, combat ? Engine.CardDamage(d.id) : d.damage, d.block > 0 ? d.block + (upgraded ? 3 : 0) : 0, d.heal > 0 ? d.heal + (upgraded ? 2 : 0) : 0);
        }

        void DrawRewards()
        {
            var s = Engine.State; var r = s.rewards; if (r == null) return;
            Header("실험 기록 확보", "전투 승리  /  보상은 한 번만 지급됩니다.");
            Txt(new Rect(40, 108, 1198, 40), "VICTORY", 33, Mint);
            string obj = string.IsNullOrEmpty(r.objectId) ? "" : "  +  " + GameDatabase.Object(r.objectId).name;
            Txt(new Rect(40, 165, 1198, 28), "+ " + r.xp + " 경험치    + " + r.credits + " 크레딧" + obj, 20, Gold);
            Txt(new Rect(40, 222, 1198, 25), "다시 누르면 선택이 취소됩니다. 남은 획득 코스트: " + Engine.RewardRemainingBudget + " / " + r.cardBudget, 16, Text);
            var choices = r.choices.ToArray();
            for (int i = 0; i < choices.Length; i++)
            {
                float cw = Math.Min(208, 1190f / Math.Max(1, choices.Length) - 14);
                Rect cr = new Rect(40 + i * (cw + 14), 271, cw, 279);
                bool taken = r.taken.Contains(choices[i]); var d = GameDatabase.Card(choices[i]);
                bool selectable = taken || d != null && Engine.RewardCardPrice(choices[i]) <= Engine.RewardRemainingBudget;
                DrawCard(cr, choices[i], taken, selectable);
                if (d != null && Engine.RewardCardPrice(choices[i]) != d.cost) Txt(new Rect(cr.x + 7, cr.y + 36, cr.width - 14, 15), "획득 코스트 " + Engine.RewardCardPrice(choices[i]), 9, Gold, true);
                if (Hit(cr, selectable)) { Act(() => Engine.ClaimCard(choices[i])); return; }
            }
            if (Btn(new Rect(999, 626, 241, 48), r.taken.Count == 0 ? "카드 없이 진행  >" : "선택 확정  >", true, Mint)) Act(Engine.FinishRewards);
            Txt(new Rect(40, 633, 820, 40), "확정하면 선택한 " + r.taken.Count + "장이 덱에 들어갑니다. 확정 전에는 선택을 자유롭게 변경할 수 있습니다.", 11, Muted);
            if (r.boss && !string.IsNullOrEmpty(s.pendingPassive))
            {
                var p = GameDatabase.Passive(s.pendingPassive);
                if (p != null) Txt(new Rect(40, 573, 1160, 27), "보스 패시브  /  " + p.owner + " · " + p.name + "  —  " + p.description, 14, Gold);
            }
        }

        void DrawKiosk()
        {
            Header("키오스크", "AGLAIA SUPPLY  /  희귀 오브젝트와 회복 재료를 구매하세요.");
            Txt(new Rect(36, 104, 1160, 28), "OBJECTS / 제작 재료", 20, Gold);
            if(Engine.ShopPrice(200)<200) Txt(new Rect(873,109,365,25),"룬·패시브 할인 10% 적용 중",12,Mint);
            for (int i = 0; i < GameDatabase.Objects.Count; i++)
            {
                var d = GameDatabase.Objects[i]; Rect r = new Rect(36 + i * 243, 149, 228, 169);
                Box(r, Panel, Line); Tex(new Rect(r.x + 81, r.y + 13, 64, 64), PixelArt.Icon(d.id), ScaleMode.ScaleToFit);
                Txt(new Rect(r.x + 10, r.y + 84, r.width - 20, 24), d.name, 16, Text, true);
                int price=Engine.ObjectPrice(d.id);
                if (Btn(new Rect(r.x + 15, r.y + 122, r.width - 30, 34), price + " CR  ·  구매", false, Gold, Engine.State.credits >= price)) Act(() => Engine.BuyObject(d.id));
            }
            Txt(new Rect(36, 349, 1160, 28), "FOOD / 회복 아이템 · 모닥불 요리 재료", 20, Mint);
            var foods = GameDatabase.Foods.Where(x => x.price > 0).ToArray();
            scroll = GUI.BeginScrollView(new Rect(35, 397, 1206, 200), scroll, new Rect(0, 0, 1180, Mathf.Ceil(foods.Length / 4f) * 81));
            for (int i = 0; i < foods.Length; i++)
            {
                var d = foods[i]; Rect r = new Rect(i % 4 * 295, i / 4 * 81, 281, 71);
                Box(r, Panel, Line); Tex(new Rect(r.x + 10, r.y + 14, 43, 43), PixelArt.Icon(d.id), ScaleMode.ScaleToFit);
                Txt(new Rect(r.x + 67, r.y + 10, 194, 22), d.name, 14, Text);
                int price=Engine.FoodPrice(d.id);
                if (Btn(new Rect(r.x + 67, r.y + 36, 193, 25), price + " CR  /  " + (d.fullHeal ? "완전 회복" : "회복 " + d.heal), false, Mint, Engine.State.credits >= price)) Act(() => Engine.BuyFood(d.id));
            }
            GUI.EndScrollView();
            if (Btn(new Rect(999, 626, 241, 48), "상점 나가기  >", true, Mint)) Act(Engine.LeaveKiosk);
            Txt(new Rect(37, 633, 910, 30), "모닥불에서 오브젝트를 장비로, 요리 재료를 완성 요리로 바꿀 수 있습니다.", 11, Muted);
        }

        void DrawCamp()
        {
            var s = Engine.State;
            Header("모닥불", "작은 불빛 아래, 잠시 실험을 멈춥니다. 입장 시 체력을 모두 회복합니다.");
            if (s.campChoice == 0)
            {
                Tex(new Rect(36, 164, 376, 379), PixelArt.Icon("Campfire"), ScaleMode.ScaleToFit);
                Box(new Rect(453, 155, 783, 195), Panel, Mint);
                Txt(new Rect(478, 179, 722, 28), "01  휴식과 만년 스프", 25, Mint);
                Para(new Rect(478, 228, 722, 49), "최대 체력까지 회복하고, 한 번 사용하면 모든 체력을 회복하는 만년 스프를 1개 받습니다.", 16, Text);
                if (Btn(new Rect(478, 291, 722, 39), "휴식하기", true, Mint)) Act(() => Engine.ChooseCamp(false));
                Box(new Rect(453, 385, 783, 195), Panel, Gold);
                Txt(new Rect(478, 408, 722, 28), "02  제작과 요리", 25, Gold);
                Para(new Rect(478, 456, 722, 50), "최대 체력까지 회복하고, 보유 오브젝트로 장비를 제작하거나 재료로 요리합니다. 둘을 합쳐 최대 3회.", 16, Text);
                if (Btn(new Rect(478, 519, 722, 39), "작업 시작", true, Gold)) Act(() => Engine.ChooseCamp(true));
                return;
            }
            Txt(new Rect(36, 102, 1100, 30), "남은 작업 횟수  " + s.campActions + " / 3", 20, Gold);
            for (int i = 0; i < 5; i++) if (Btn(new Rect(36 + i * 175, 152, 161, 38), SlotName((GearSlot)i), craftSlot == (GearSlot)i, Mint)) { craftSlot = (GearSlot)i; scroll = Vector2.zero; }
            if (Btn(new Rect(933, 152, 305, 38), "가방에서 요리 / 회복", false, Gold)) { inventory = true; inventoryTab = 2; inventoryScroll = Vector2.zero; }
            var gear = GameDatabase.Gear.Where(x => x.slot == craftSlot).ToArray();
            scroll = GUI.BeginScrollView(new Rect(36, 211, 1202, 366), scroll, new Rect(0, 0, 1174, Mathf.Ceil(gear.Length / 3f) * 151));
            for (int i = 0; i < gear.Length; i++)
            {
                var g = gear[i]; Rect r = new Rect(i % 3 * 393, i / 3 * 151, 378, 136);
                Box(r, Panel, g.rarity == "초월" ? Pink : Gold);
                Tex(new Rect(r.x+15,r.y+10,34,34),PixelArt.SystemIcon(g.id),ScaleMode.ScaleToFit);
                Txt(new Rect(r.x + 59, r.y + 12, 300, 26), g.name + " · " + g.rarity, 16, Gold);
                FitText(new Rect(r.x + 15, r.y + 45, 344, 43), DescriptionSummary.Gear(g), 11, Text);
                if(Hit(new Rect(r.x+15,r.y+10,34,34))) {inspectGear=g.id;summaryMode=true;detailScroll=Vector2.zero;}
                var o = GameDatabase.Object(g.objectId);
                Tex(new Rect(r.x+16,r.y+92,28,28),PixelArt.SystemIcon(g.objectId),ScaleMode.ScaleToFit);
                if (Btn(new Rect(r.x + 52, r.y + 93, 307, 29), (o == null ? g.objectId : o.name) + " 1개  ·  제작", false, Mint, s.campActions > 0 && s.objects.Contains(g.objectId)))
                {
                    if (s.gear.Count(id => GameDatabase.Equipment(id).slot == g.slot) >= 2) craftPending = g.id;
                    else Act(() => Engine.Craft(g.id));
                }
            }
            GUI.EndScrollView();
            if (Btn(new Rect(999, 626, 241, 48), "불을 떠나기  >", true, Mint)) Act(Engine.LeaveCamp);
            Txt(new Rect(36, 625, 925, 45), "각 슬롯은 최대 2개. 무기 제작 시 무기군 스킬이 덱에 추가됩니다. 슬롯이 가득 차면 교체할 장비를 선택합니다.", 11, Muted);
            if (!string.IsNullOrEmpty(craftPending)) DrawCraftReplacement();
        }

        void DrawCraftReplacement()
        {
            currentLayer = 1;
            GUI.enabled = topLayer == 1;
            Modal(new Rect(250, 170, 780, 385)); var pending = GameDatabase.Equipment(craftPending);
            Txt(new Rect(279, 194, 720, 32), pending.name + "  /  교체할 장비 선택", 20, Gold);
            int y = 249;
            var gear = Engine.State.gear.ToArray();
            for (int i = 0; i < gear.Length; i++)
            {
                var g = GameDatabase.Equipment(gear[i]); if (g.slot != pending.slot) continue;
                int replace = i;
                if (Btn(new Rect(280, y, 720, 74), g.name + "\n" + g.description, false, Text)) { Act(() => Engine.Craft(craftPending, replace)); craftPending = ""; return; }
                y += 90;
            }
            if (Btn(new Rect(280, 493, 720, 35), "취소", false, Muted)) craftPending = "";
        }

        void DrawEncounter()
        {
            var s = Engine.State;
            Header("실험체 조우", "우연한 만남도, 실험의 일부일까요?");
            if (s.runeChangePending) { DrawRuneSelection(true); return; }
            if (string.IsNullOrEmpty(s.chosenEventId))
            {
                Txt(new Rect(36, 113, 1160, 34), "세 개의 신호 중 하나를 따라가세요.", 25, Text);
                var offers = s.encounterOffers.ToArray();
                for (int i = 0; i < offers.Length; i++)
                {
                    var e = GameDatabase.Event(offers[i]); Rect r = new Rect(36 + i * 410, 183, 387, 402);
                    Box(r, Panel, Line); Tex(new Rect(r.x + 94, r.y + 22, 200, 187), PixelArt.Portrait(e.owner), ScaleMode.ScaleToFit);
                    Txt(new Rect(r.x + 20, r.y + 224, 347, 24), e.owner, 14, Mint, true);
                    Txt(new Rect(r.x + 20, r.y + 264, 347, 32), e.title, 20, Text, true);
                    FitText(new Rect(r.x + 25, r.y + 313, 337, 65), e.story, 14, Muted);
                    if (Hit(r)) { encounterStoryScroll=Vector2.zero;Act(() => Engine.SelectEncounter(e.id)); return; }
                }
            }
            else
            {
                var e = GameDatabase.Event(s.chosenEventId);
                Tex(new Rect(40, 146, 324, 392), PixelArt.Portrait(e.owner), ScaleMode.ScaleToFit);
                Txt(new Rect(403, 124, 830, 32), e.owner + "  /  " + e.title, 25, Mint);
                wrapped.fontSize=18;float storyHeight=Math.Max(104,wrapped.CalcHeight(new GUIContent(e.story),801)+10);
                encounterStoryScroll=GUI.BeginScrollView(new Rect(403,178,825,112),encounterStoryScroll,new Rect(0,0,801,storyHeight));
                Para(new Rect(0,0,801,storyHeight),e.story,18,Text);GUI.EndScrollView();
                for (int i = 0; i < e.options.Length; i++)
                {
                    var option = e.options[i]; Rect r = new Rect(403, 310 + i * 116, 825, 107);
                    var reward=EventPresentation.RewardCard(option);
                    bool canChoose=Engine.CanChooseEventOption(i);
                    Box(r, Panel, Line);
                    Txt(new Rect(r.x + 19, r.y + 9, r.width - 116, 25), (canChoose?"":"[선택 불가] ")+(i + 1) + "  " + option.label, 16, canChoose?Gold:Muted);
                    string summary=EventPresentation.RewardSummary(option);
                    string narrative=option.description.EndsWith(summary,StringComparison.Ordinal)
                        ? option.description.Substring(0,option.description.Length-summary.Length).Trim() : option.description;
                    FitText(new Rect(r.x + 19, r.y + 36, r.width - 116, 34), narrative, 14, Text);
                    if(reward!=null)
                    {
                        string price=option.effect=="trade_card"?"크레딧 "+option.amount+" 지불 · ":option.effect=="risky_card"?"체력 "+option.amount+" 소모 · ":"";
                        Para(new Rect(r.x+19,r.y+76,r.width-116,28),price+"획득 / "+EventPresentation.RewardTitle(option),11,Mint);
                        Rect cardIcon=new Rect(r.xMax-76,r.y+10,58,58);
                        Tex(cardIcon,PixelArt.SkillIcon(reward.id),ScaleMode.ScaleToFit);
                        Txt(new Rect(r.xMax-90,r.y+76,84,20),"카드 보기 +",10,Mint,true);
                        if(Hit(new Rect(r.xMax-90,r.y+5,84,94))) {inspectCard=reward.id;inspectEnemyCard=false;detailScroll=Vector2.zero;return;}
                    }
                    else FitText(new Rect(r.x+19,r.y+76,r.width-38,28),summary,12,Mint);
                    if (Hit(r,canChoose)) { int index = i; Act(() => Engine.ChooseEventOption(index)); return; }
                }
            }
        }

        void DrawPassiveChoice()
        {
            var s = Engine.State; var p = GameDatabase.Passive(s.pendingPassive);
            Header("새로운 패시브", "보스의 기록을 흡수합니다. 최대 3개의 패시브를 보유할 수 있습니다.");
            if (p == null) { if (Btn(new Rect(430, 340, 420, 48), "계속", true, Mint)) Act(() => Engine.ReplacePassive(-1)); return; }
            Box(new Rect(40, 137, 1194, 177), Panel, Gold);
            Tex(new Rect(62, 154, 120, 132), PixelArt.Portrait(p.owner), ScaleMode.ScaleToFit);
            Tex(new Rect(203, 182, 72, 72), PixelArt.PassiveIcon(p.id), ScaleMode.ScaleToFit);
            Txt(new Rect(300, 161, 897, 32), p.owner + " · " + p.name, 25, Gold);
            FitText(new Rect(300, 210, 897, 40), TraitSummary(p.mechanics,p.description), 18, Text);
            TraitDetailButton(new Rect(300,254,897,18),p.id);
            Para(new Rect(300, 275, 897, 28), PassiveAffinity(p), 14, Mint);
            Txt(new Rect(40, 345, 1160, 26), s.passives.Count < 3 ? "새 패시브를 추가할 수 있습니다." : "교체할 패시브를 선택하거나 새 능력을 포기하세요.", 16, Muted);
            for (int i = 0; i < s.passives.Count; i++)
            {
                var old = GameDatabase.Passive(s.passives[i]); int index = i;
                Rect r = new Rect(40 + i * 405, 402, 387, 133); Box(r, Panel, Line);
                Tex(new Rect(r.x + 15, r.y + 17, 48, 48), PixelArt.PassiveIcon(old.id), ScaleMode.ScaleToFit);
                Para(new Rect(r.x + 78, r.y + 17, 292, 38), old.owner + " · " + old.name, 16, Mint);
                FitText(new Rect(r.x + 15, r.y + 60, 354, 31), TraitSummary(old.mechanics,old.description), 12, Text);
                TraitDetailButton(new Rect(r.x+15,r.y+92,354,17),old.id);
                Txt(new Rect(r.x + 15, r.y + 113, 354, 17), "주인 스킬의 무작위 등장 빈도가 증가합니다.", 11, Mint);
                if (Hit(r)) { Act(() => Engine.ReplacePassive(index)); return; }
            }
            if (Btn(new Rect(873, 626, 363, 48), s.passives.Count < 3 ? "새 패시브 획득  >" : "새 패시브 포기  >", true, Mint)) Act(() => Engine.ReplacePassive(-1));
        }

        void DrawEnding()
        {
            bool won = Engine.State.stage == RunStage.Won;
            Tex(new Rect(0, 0, W, H), PixelArt.Background(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, W, H), new Color(.025f, .05f, .075f, .84f));
            Txt(new Rect(128, 131, 1024, 73), won ? "EXPERIMENT COMPLETE" : "SIGNAL LOST", 44, won ? Mint : Pink, true);
            Txt(new Rect(190, 231, 900, 40), won ? "실험의 경계 밖으로" : "다시, 루미아에서", 33, Text, true);
            Para(new Rect(276, 326, 728, 124), won ? "니아의 게임 월드가 작은 픽셀 조각으로 흩어졌다.\n하나는 꺼진 모니터 앞에서 눈을 떴다.\n\n“기록 종료. 다음 실험은… 없었으면 좋겠네.”" : "실험 기록이 끊겼다. 그러나 니아의 화면에는\n아직 ‘다시 시작’ 버튼이 깜빡이고 있다.\n\n다른 카드, 다른 길. 다음 판은 달라질 수 있다.", 20, Text);
            Txt(new Rect(260, 497, 760, 27), "최종 레벨 " + Engine.State.level + "  /  덱 " + Engine.State.deck.Count + "장  /  크레딧 " + Engine.State.credits, 16, Gold, true);
            if (Btn(new Rect(424, 579, 432, 54), "로비로 돌아가기  >", true, Mint)) { lobby = true; Save(); }
        }

        void DrawInventory()
        {
            Modal(new Rect(55, 81, 1170, 590));
            Txt(new Rect(78, 98, 650, 32), "하나의 실험 기록", 22, Mint);
            if (Btn(new Rect(1127, 100, 69, 30), "닫기", false, Muted)) { inventory = false; return; }
            string[] tabs = { "보유 덱", "장비", "가방 · 요리", "패시브", "룬" };
            for (int i = 0; i < tabs.Length; i++) if (Btn(new Rect(79 + i * 223, 150, 209, 35), tabs[i], i == inventoryTab, Mint)) { inventoryTab = i; inventoryScroll = Vector2.zero; }
            var s = Engine.State;
            if (inventoryTab == 0)
            {
                var cards = s.deck.GroupBy(x => x).ToArray();
                inventoryScroll = GUI.BeginScrollView(new Rect(80, 207, 1120, 424), inventoryScroll, new Rect(0, 0, 1090, Mathf.Ceil(cards.Length / 6f) * 244));
                for (int i = 0; i < cards.Length; i++)
                {
                    Rect r = new Rect(i % 6 * 182, i / 6 * 244, 166, 226); DrawCard(r, cards[i].Key);
                    Txt(new Rect(r.x + 7, r.yMax + 1, r.width - 14, 16), "보유 " + cards[i].Count() + "장", 11, Gold, true);
                    if (Hit(r)) { inspectCard = cards[i].Key; inspectEnemyCard=false; }
                }
                GUI.EndScrollView();
            }
            else if (inventoryTab == 1)
            {
                for (int slot = 0; slot < 5; slot++)
                {
                    Txt(new Rect(81, 217 + slot * 80, 170, 25), SlotName((GearSlot)slot), 16, Gold);
                    var held = s.gear.Where(id => GameDatabase.Equipment(id).slot == (GearSlot)slot).ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        Rect r = new Rect(261 + i * 466, 208 + slot * 80, 448, 67); Box(r, Panel, Line);
                        var g = i < held.Length ? GameDatabase.Equipment(held[i]) : null;
                        if(g!=null) Tex(new Rect(r.x+12,r.y+9,48,48),PixelArt.SystemIcon(g.id),ScaleMode.ScaleToFit);
                        Txt(new Rect(r.x + 73, r.y + 8, r.width - 86, 24), g == null ? "빈 슬롯" : g.name + " · " + g.rarity, 14, g == null ? Muted : Text);
                        if (g != null)
                        {
                            FitText(new Rect(r.x + 73, r.y + 36, r.width - 86, 22), DescriptionSummary.Gear(g), 11, Muted);
                            if(Hit(new Rect(r.x+10,r.y+7,52,52))) {inspectGear=g.id;summaryMode=true;detailScroll=Vector2.zero;}
                        }
                    }
                }
            }
            else if (inventoryTab == 2)
            {
                var objects=s.objects.GroupBy(id=>id).ToArray();
                if(objects.Length==0) Txt(new Rect(83,207,1077,26),"오브젝트 / 보유한 재료 없음",14,Gold);
                for(int i=0;i<objects.Length;i++)
                {
                    var obj=GameDatabase.Object(objects[i].Key);float x=83+i*221;
                    Tex(new Rect(x,207,29,29),PixelArt.SystemIcon(objects[i].Key),ScaleMode.ScaleToFit);
                    Txt(new Rect(x+36,212,179,24),(obj?.name ?? objects[i].Key)+" ×"+objects[i].Count(),12,Gold);
                }
                var foods = s.foods.ToArray();
                inventoryScroll = GUI.BeginScrollView(new Rect(80, 256, 1120, 376), inventoryScroll, new Rect(0, 0, 1090, Math.Max(376, foods.Length * 82)));
                if (foods.Length == 0) Txt(new Rect(10, 36, 1000, 40), "가방이 비어 있습니다. 키오스크에서 음식과 요리 재료를 구입할 수 있습니다.", 16, Muted);
                for (int i = 0; i < foods.Length; i++)
                {
                    var f = GameDatabase.Food(foods[i]); if (f == null) continue;
                    int index = i; Rect r = new Rect(0, i * 82, 1083, 72); Box(r, Panel, Line);
                    Tex(new Rect(15, r.y + 12, 48, 48), PixelArt.Icon(f.id), ScaleMode.ScaleToFit);
                    Txt(new Rect(82, r.y + 10, 547, 25), f.name + (f.fullHeal ? "  /  완전 회복" : "  /  회복 " + f.heal), 16, Mint);
                    Txt(new Rect(82, r.y + 41, 547, 24), f.description, 11, Muted);
                    bool usable = Engine.State.combat == null || !Engine.State.combat.enemyTurn;
                    if (Btn(new Rect(692, r.y + 20, 176, 33), "먹기", false, Mint, usable)) { Act(() => Engine.UseFood(index)); break; }
                    bool canCook = s.stage == RunStage.Campfire && s.campChoice == 2 && s.campActions > 0 && !string.IsNullOrEmpty(f.upgradeTo);
                    if (Btn(new Rect(885, r.y + 20, 176, 33), canCook ? "요리하기" : "요리 불가", false, Gold, canCook)) { Act(() => Engine.Cook(index)); break; }
                }
                GUI.EndScrollView();
            }
            else if (inventoryTab == 3)
            {
                for (int i = 0; i < s.passives.Count; i++)
                {
                    var p = GameDatabase.Passive(s.passives[i]); Rect r = new Rect(80, 215 + i * 127, 1115, 110); Box(r, Panel, Line);
                    Tex(new Rect(r.x + 17, r.y + 23, 64, 64), PixelArt.PassiveIcon(p.id), ScaleMode.ScaleToFit);
                    Tex(new Rect(r.x + 94, r.y + 8, 73, 94), PixelArt.Portrait(p.owner), ScaleMode.ScaleToFit);
                    Txt(new Rect(r.x + 187, r.y + 16, 906, 27), p.owner + " · " + p.name, 20, Mint);
                    FitText(new Rect(r.x + 187, r.y + 48, 906, 29), TraitSummary(p.mechanics,p.description), 15, Text);
                    TraitDetailButton(new Rect(r.x+960,r.y+16,131,22),p.id);
                    Txt(new Rect(r.x + 187, r.y + 86, 906, 20), PassiveAffinity(p), 13, Mint);
                }
            }
            else
            {
                var runes=new[]{GameDatabase.Rune(s.mainRune),GameDatabase.Rune(s.supportRune)};
                Txt(new Rect(82,211,1095,26),"룬은 특정 조우 이벤트에서만 변경할 수 있습니다.",14,Muted);
                for(int i=0;i<runes.Length;i++)
                {
                    var r=runes[i];if(r==null) continue;
                    Rect box=new Rect(80,259+i*162,1115,145);Box(box,Panel,i==0?Mint:Gold);
                    Tex(new Rect(box.x+20,box.y+20,80,80),PixelArt.SystemIcon(r.id),ScaleMode.ScaleToFit);
                    Txt(new Rect(box.x+118,box.y+16,977,28),(r.main?"메인":"보조")+" / "+r.name+" · "+r.tree,20,i==0?Mint:Gold);
                    FitText(new Rect(box.x+118,box.y+56,977,56),TraitSummary(r.mechanics,r.description),16,Text);
                    TraitDetailButton(new Rect(box.x+20,box.y+115,1075,20),r.id);
                }
            }
        }

        static string PassiveAffinity(PassiveDef passive)
        {
            return passive.owner + "의 스킬이 무작위 카드 선택지에 더 자주 등장합니다.";
        }

        static string TraitSummary(TraitMechanicProfile profile,string fallback) => DescriptionSummary.Trait(profile,fallback);

        void FitText(Rect r,string text,int size,Color color)
        {
            SummaryText(r,DescriptionSummary.Normalize(text),size,color,"preview/"+text+"/"+r.width+"/"+r.height);
        }

        void SummaryText(Rect r,string text,int size,Color color,string key)
        {
            text=text??"";
            wrapped.fontSize=size;
            float plainHeight=wrapped.CalcHeight(new GUIContent(text),r.width);
            if(plainHeight<=r.height) { Para(r,text,size,color); return; }
            float width=Math.Max(30,r.width-15);
            wrapped.fontSize=size;
            float height=wrapped.CalcHeight(new GUIContent(text),width)+5;
            Vector2 position;summaryScroll.TryGetValue(key,out position);
            position=GUI.BeginScrollView(r,position,new Rect(0,0,width,height),false,true);
            Para(new Rect(0,0,width,height),text,size,color);
            GUI.EndScrollView();summaryScroll[key]=position;
        }

        void DescriptionTabs(Rect r)
        {
            float half=(r.width-8)/2;
            if(Btn(new Rect(r.x,r.y,half,r.height),"요약",summaryMode,Mint)) {summaryMode=true;detailScroll=Vector2.zero;}
            if(Btn(new Rect(r.x+half+8,r.y,half,r.height),"전체 설명",!summaryMode,Gold)) {summaryMode=false;detailScroll=Vector2.zero;}
        }

        void TraitDetailButton(Rect r,string id)
        {
            Fill(r,C("203343")); Txt(r,"효과 · 발동 조건 자세히 +",10,Muted,true);
            if(Hit(r)) { inspectTrait=id;summaryMode=true;detailScroll=Vector2.zero; }
        }

        void DrawTraitDetail()
        {
            var passive=GameDatabase.Passive(inspectTrait); var rune=GameDatabase.Rune(inspectTrait);
            if(passive==null && rune==null) { inspectTrait="";return; }
            Modal(new Rect(210,82,860,562));
            string name=passive!=null?passive.name:rune.name;
            string description=passive!=null?passive.description:rune.description;
            string owner=passive!=null?passive.owner:rune.tree;
            if(passive!=null)
            {
                Tex(new Rect(244,154,165,184),PixelArt.Portrait(passive.owner),ScaleMode.ScaleToFit);
                Tex(new Rect(284,363,84,84),PixelArt.PassiveIcon(passive.id),ScaleMode.ScaleToFit);
            }
            else
            {
                Box(new Rect(251,226,157,157),Panel,Gold);
                Tex(new Rect(275,239,109,109),PixelArt.SystemIcon(rune.id),ScaleMode.ScaleToFit);
                Txt(new Rect(267,353,125,24),rune.main?"메인 룬":"보조 룬",15,Gold,true);
            }
            Txt(new Rect(445,110,590,40),name,name.Length>16?20:25,Mint);
            Txt(new Rect(445,164,590,25),owner+" / "+(passive!=null?"패시브":"룬"),16,Gold);
            DescriptionTabs(new Rect(445,200,590,32));
            string text=summaryMode ? (passive!=null?DescriptionSummary.Passive(passive):DescriptionSummary.Rune(rune)) : DescriptionSummary.Normalize(description)+"\n\n"+(passive!=null?"패시브의 전투 효과는 주인의 카드가 없어도 발동합니다.\n"+PassiveAffinity(passive):"룬의 효과는 중첩·발동 횟수와 지속 턴 제한을 따릅니다.");
            wrapped.fontSize=15;float height=Math.Max(273,wrapped.CalcHeight(new GUIContent(text),568)+12);
            detailScroll=GUI.BeginScrollView(new Rect(445,244,590,279),detailScroll,new Rect(0,0,568,height));
            Para(new Rect(0,0,568,height),text,15,Text);GUI.EndScrollView();
            if(Btn(new Rect(237,580,804,40),"효과 설명 닫기",true,Mint)) inspectTrait="";
        }

        void DrawCatalog()
        {
            Modal(new Rect(25, 27, 1230, 666));
            Txt(new Rect(49, 44, 1000, 32), "카드 도감  /  AGLAIA ARCHIVE", 22, Mint);
            if (Btn(new Rect(1158, 45, 70, 31), "닫기", false, Muted)) { catalog = false; return; }
            string query = GUI.TextField(new Rect(49, 95, 880, 38), catalogQuery, field);
            if (query != catalogQuery) { catalogQuery = query; catalogPage = 0; }
            Txt(new Rect(949, 104, 270, 25), "카드명 · 실험체 검색", 11, Muted);
            var cards = GameDatabase.Cards.Where(x => string.IsNullOrEmpty(catalogQuery) || (x.name + x.owner + x.key).IndexOf(catalogQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            int pages = Math.Max(1, (cards.Length + 11) / 12); catalogPage = Math.Min(catalogPage, pages - 1);
            for (int i = 0; i < 12; i++)
            {
                int index = catalogPage * 12 + i; if (index >= cards.Length) break;
                Rect r = new Rect(49 + i % 6 * 198, 153 + i / 6 * 229, 181, 215); DrawCard(r, cards[index].id);
                if (Hit(r)) { inspectCard = cards[index].id; inspectEnemyCard=false; }
            }
            if (Btn(new Rect(49, 626, 175, 39), "<  이전", false, Muted, catalogPage > 0)) catalogPage--;
            Txt(new Rect(415, 635, 450, 25), (catalogPage + 1) + " / " + pages + "   ·   " + cards.Length + " RECORDS", 14, Text, true);
            if (Btn(new Rect(1050, 626, 175, 39), "다음  >", false, Mint, catalogPage < pages - 1)) catalogPage++;
        }

        void DrawCardDetail()
        {
            var d = GameDatabase.Card(inspectCard); if (d == null) { inspectCard = ""; return; }
            Modal(new Rect(210, 82, 860, 562));
            bool inBattle = Engine != null && Engine.State.stage == RunStage.Combat;
            bool combat = inBattle && !inspectEnemyCard;
            int currentCost=inBattle ? (inspectEnemyCard ? Engine.EnemyEffectiveCardCost(d.id) : Engine.EffectiveCardCost(d.id)) : d.cost;
            DrawCard(new Rect(237, 113, 215, 400), d.id, false, true, combat);
            Txt(new Rect(480, 111, 560, 39), d.name, d.name.Length > 17 ? 20 : 25, Mint);
            Txt(new Rect(480, 163, 560, 28), d.owner + "  /  " + d.key + "  /  코스트 " + d.cost + (currentCost != d.cost ? " → 현재 " + currentCost : ""), 16, Gold);
            DescriptionTabs(new Rect(480,201,559,32));
            if(inBattle && inspectEnemyCard) Txt(new Rect(480,240,560,16),"상대의 기본 효과 · 패시브와 장비 보정은 행동 예고에 반영됩니다.",10,Muted);
            if(combat && Engine.CardTotalDamage(d.id)>0) Txt(new Rect(480,240,560,16),"현재 연계 포함 예상 공격 피해 " + Engine.CardTotalDamage(d.id) + " · 상대 방어·회피에 따라 달라집니다.",10,Mint);
            bool upgraded = combat && Engine.State.upgrades.Contains(d.id);
            string description = combat ? CardPresentation.Describe(d, GameDatabase.Cards, Engine.CardDamage(d.id), d.block > 0 ? d.block + (upgraded ? 3 : 0) : 0, d.heal > 0 ? d.heal + (upgraded ? 2 : 0) : 0) : d.description;
            string rules = CardPresentation.Rules(d);
            string fullText = summaryMode ? CardSummary(d,combat) : DescriptionSummary.Normalize(description + (string.IsNullOrEmpty(rules) ? "" : "\n\n" + rules));
            if(combat && !StatusMechanics.CanUse(Engine.State.combat.playerStatuses,d))
                fullText="현재 상태이상으로 이 카드를 사용할 수 없습니다. 나의 필드에서 발동한 상태와 남은 턴을 확인하세요.\n\n"+fullText;
            wrapped.fontSize = 16;
            float height = Math.Max(263, wrapped.CalcHeight(new GUIContent(fullText), 536) + 12);
            detailScroll = GUI.BeginScrollView(new Rect(480, 263, 559, 263), detailScroll, new Rect(0, 0, 536, height));
            Para(new Rect(0, 0, 536, height), fullText, 16, Text);
            GUI.EndScrollView();
            Txt(new Rect(480, 534, 560, 25), "원본 쿨다운 " + d.cooldown + "초를 턴제 코스트로 변환했습니다.", 11, Muted);
            if (Btn(new Rect(237, 580, 804, 40), "기록 닫기", true, Mint)) inspectCard = "";
        }

        void DrawGearDetail()
        {
            var g=GameDatabase.Equipment(inspectGear);if(g==null){inspectGear="";return;}
            Modal(new Rect(210,82,860,562));
            Tex(new Rect(245,211,164,164),PixelArt.SystemIcon(g.id),ScaleMode.ScaleToFit);
            Txt(new Rect(445,110,590,40),g.name,25,Gold);
            Txt(new Rect(445,164,590,25),SlotName(g.slot)+" / "+g.rarity+(g.slot==GearSlot.Weapon?" / "+g.weaponClass:""),16,Mint);
            DescriptionTabs(new Rect(445,200,590,32));
            string text=summaryMode?DescriptionSummary.Gear(g):DescriptionSummary.Normalize(g.description);
            if(!summaryMode)
            {
                var material=GameDatabase.Object(g.objectId);
                text+="\n\n모닥불에서 "+(material?.name??g.objectId)+" 1개를 소모하여 제작합니다.";
                if(g.slot==GearSlot.Weapon) text+="\n무기를 교체하면 장비로 추가된 D 카드도 교체됩니다. 보상이나 이벤트로 얻은 카드는 유지됩니다.\n알렉스 패시브가 있으면 무기마다 다른 무기군의 D 1장을 추가로 얻습니다.";
                text+="\n각 장비 슬롯에는 최대 2개를 장착할 수 있습니다.";
            }
            wrapped.fontSize=15;float height=Math.Max(273,wrapped.CalcHeight(new GUIContent(text),568)+12);
            detailScroll=GUI.BeginScrollView(new Rect(445,244,590,279),detailScroll,new Rect(0,0,568,height));
            Para(new Rect(0,0,568,height),text,15,Text);GUI.EndScrollView();
            if(Btn(new Rect(237,580,804,40),"장비 설명 닫기",true,Mint))inspectGear="";
        }

        void DrawEnemyLoadout()
        {
            var c=Engine?.State.combat;if(c==null){enemyLoadout=false;return;}
            Modal(new Rect(180,75,920,574));
            Tex(new Rect(210,101,125,147),PixelArt.Portrait(c.enemyId),ScaleMode.ScaleToFit);
            Txt(new Rect(360,103,700,38),c.enemyName+" · LV."+c.enemyLevel,25,Pink);
            Txt(new Rect(360,151,700,26),"무기군 "+c.enemyWeaponClass+" / 최대 코스트 "+GameEngine.EnergyForLevel(c.enemyLevel),16,Gold);
            string[] skills={"basic_attack",c.enemyWeaponCardId,c.enemyTacticalCardId};
            for(int i=0;i<skills.Length;i++)
            {
                var d=GameDatabase.Card(skills[i]);if(d==null)continue;
                Rect r=new Rect(360+i*230,189,220,54);Box(r,Panel,Line);
                Tex(new Rect(r.x+6,r.y+6,42,42),PixelArt.SkillIcon(d.id),ScaleMode.ScaleToFit);
                Para(new Rect(r.x+55,r.y+8,157,40),d.key+" / "+d.name+"\n코스트 "+d.cost,12,Text);
                if(Hit(r)){inspectCard=d.id;inspectEnemyCard=true;summaryMode=true;detailScroll=Vector2.zero;}
            }
            var p=GameDatabase.Passive(c.enemyPassiveId);
            float total=125+(c.enemyGear?.Count??0)*86;
            inventoryScroll=GUI.BeginScrollView(new Rect(210,265,860,300),inventoryScroll,new Rect(0,0,838,Math.Max(292,total)));
            if(p!=null)
            {
                Box(new Rect(0,0,838,111),Panel,Line);
                Tex(new Rect(12,18,61,61),PixelArt.PassiveIcon(p.id),ScaleMode.ScaleToFit);
                Txt(new Rect(88,10,724,26),"패시브 / "+p.name,17,Mint);
                SummaryText(new Rect(88,43,724,55),DescriptionSummary.Passive(p),13,Text,"enemy/"+p.id);
                if(Hit(new Rect(12,18,61,61))){inspectTrait=p.id;summaryMode=true;detailScroll=Vector2.zero;}
            }
            if(c.enemyGear==null || c.enemyGear.Count==0) Txt(new Rect(16,139,800,33),"장착한 장비가 없습니다. 후반 실험체와 보스는 장비를 갖춥니다.",14,Muted);
            else for(int i=0;i<c.enemyGear.Count;i++)
            {
                var g=GameDatabase.Equipment(c.enemyGear[i]);if(g==null)continue;
                float y=125+i*86;Box(new Rect(0,y,838,78),Panel,Line);
                Tex(new Rect(12,y+15,48,48),PixelArt.SystemIcon(g.id),ScaleMode.ScaleToFit);
                Txt(new Rect(78,y+9,739,23),g.name+" · "+g.rarity+" / "+SlotName(g.slot),15,Gold);
                SummaryText(new Rect(78,y+35,739,33),DescriptionSummary.Gear(g),12,Text,"enemy/gear/"+g.id);
                if(Hit(new Rect(12,y+15,48,48))){inspectGear=g.id;summaryMode=true;detailScroll=Vector2.zero;}
            }
            GUI.EndScrollView();
            if(Btn(new Rect(210,591,860,37),"상대 정보 닫기",true,Mint))enemyLoadout=false;
        }

        void DrawFieldStrip(Rect r, bool enemy)
        {
            var tokens = FieldTokens(enemy);
            Box(r, Ink, tokens.Count == 0 ? Line : enemy ? Pink : Mint);
            Txt(new Rect(r.x + 10, r.y + 6, r.width - 20, 18), (enemy ? "상대" : "나") + "의 필드 · 회피 " + (enemy?Engine.EnemyEvasion:Engine.Evasion) + "% · 눌러서 확인", 11, Muted);
            if (tokens.Count > 0)
            {
                string summary = string.Join(" · ", tokens.Take(3).Select(TokenShort));
                Para(new Rect(r.x + 10, r.y + 28, r.width - 20, 27), summary, 11, enemy ? Pink : Mint);
            }
            else Txt(new Rect(r.x+10,r.y+29,r.width-20,24),"상태이상과 남아 있는 효과가 없습니다.",11,Muted);
            if (Hit(r)) { fieldInfo=true; fieldEnemy=enemy; fieldScroll=Vector2.zero; }
        }

        List<SkillMechanicToken> FieldTokens(bool enemy)
        {
            var tokens=Engine.SkillStateSnapshot(enemy);
            var c=Engine.State.combat;
            if(c==null) return tokens;
            int poison=enemy?c.enemyPoison:c.poison,weak=enemy?c.enemyWeak:c.weak,vulnerable=enemy?c.enemyVulnerable:c.vulnerable;
            if(poison>0) tokens.Add(new SkillMechanicToken{label="중독",kind="legacy_poison",amount=poison});
            if(weak>0) tokens.Add(new SkillMechanicToken{label="약화",kind="legacy_weak",remaining=weak});
            if(vulnerable>0) tokens.Add(new SkillMechanicToken{label="취약",kind="legacy_vulnerable",remaining=vulnerable});
            var grants=enemy ? c.enemyFreeCasts : c.freeCasts;
            foreach(var grant in grants.Where(x=>x.uses>0))
            {
                var card=GameDatabase.Card(grant.cardId);
                tokens.Add(new SkillMechanicToken { owner=card?.owner,key=grant.cardId,sourceCard=grant.cardId,targetCard=grant.cardId,label=card?.name ?? grant.cardId,kind="free_cast",amount=grant.uses });
            }
            return tokens;
        }

        static string TokenShort(SkillMechanicToken token)
        {
            if(token.kind=="legacy_poison") return "중독 " + token.amount;
            if(token.kind=="legacy_weak" || token.kind=="legacy_vulnerable") return token.label+" · "+token.remaining+"턴";
            if(token.kind=="deferred_damage") return "유예 피해 "+token.amount+" · 남은 "+token.remaining+"턴";
            if((token.kind??"").StartsWith("status_")) return token.label+" · "+token.remaining+"턴";
            if (token.kind == "free_cast") return token.label + " 무료 " + token.amount + "회";
            if (token.kind == "revive" && token.persistent) return token.label + " 사용될 때까지";
            if (token.kind == "resource") return token.label + " " + token.amount + "/" + token.cap;
            if (token.kind == "discount") return token.label + " −" + token.amount;
            if (token.kind == "delayed_damage") return token.label + " " + token.delay + "회 후";
            if (token.kind == "energy_buff") return token.label + " 코스트 +" + token.amount + " · " + token.remaining + "턴";
            if (token.kind == "damage_buff") return token.label + " 피해 +" + token.amount + " · " + token.remaining + "턴";
            if (token.kind == "evasion_buff") return token.label + " 회피 +" + token.amount + "% · " + token.remaining + "턴";
            return token.label + (token.kind == "empower_basic" ? " +" + token.amount : " " + token.remaining + "턴");
        }

        void DrawFieldDetail()
        {
            if (Engine == null || Engine.State.combat == null) { fieldInfo=false; return; }
            Modal(new Rect(206, 85, 868, 550));
            Txt(new Rect(233, 109, 813, 37), (fieldEnemy ? "상대" : "나") + "의 필드 · 상태이상 · 회피율", 25, fieldEnemy ? Pink : Mint);
            var tokens=FieldTokens(fieldEnemy);
            Txt(new Rect(233, 156, 813, 26), "현재 회피율 " + (fieldEnemy?Engine.EnemyEvasion:Engine.Evasion) + "%  /  아이콘을 눌러 효과의 출처를 확인할 수 있습니다.", 14, Muted);
            fieldScroll=GUI.BeginScrollView(new Rect(233, 195, 813, 344), fieldScroll, new Rect(0, 0, 788, Math.Max(330,tokens.Count*72)));
            if (tokens.Count == 0) Txt(new Rect(15, 35, 756, 40), "현재 설치물이나 남아 있는 연계 효과가 없습니다.", 16, Muted, true);
            for (int i=0;i<tokens.Count;i++)
            {
                var t=tokens[i]; float y=i*72;
                Box(new Rect(0,y,788,64),Panel,Line);
                string traitId=(t.owner??"").Replace("trait:","");
                var passive=GameDatabase.Passive(traitId);var rune=GameDatabase.Rune(traitId);
                var gear=traitId.StartsWith("gear:")?GameDatabase.Equipment(traitId.Substring(5)):null;
                string source=t.sourceCard;
                if (string.IsNullOrEmpty(source)) source=GameDatabase.Cards.FirstOrDefault(x=>x.owner==t.owner && x.mechanics!=null && x.mechanics.rules.Any(a=>a.key==t.key && (a.op=="gain" || a.op=="set")))?.id;
                if(passive!=null || rune!=null || gear!=null)
                {
                    Rect icon=new Rect(10,y+10,44,44);
                    if(passive!=null) Tex(icon,PixelArt.PassiveIcon(passive.id),ScaleMode.ScaleToFit);
                    else {Box(icon,Ink,Gold);Tex(icon,PixelArt.SystemIcon(gear!=null?gear.id:rune.id),ScaleMode.ScaleToFit);}
                    if(Hit(icon)) {if(gear!=null)inspectGear=gear.id;else inspectTrait=traitId;summaryMode=true;detailScroll=Vector2.zero;}
                }
                else if (!string.IsNullOrEmpty(source))
                {
                    Rect icon=new Rect(10,y+10,44,44); Tex(icon,PixelArt.SkillIcon(source),ScaleMode.ScaleToFit);
                    if (Hit(icon)) { inspectCard=source; inspectEnemyCard=fieldEnemy; detailScroll=Vector2.zero; }
                }
                Txt(new Rect(67,y+9,706,23),TokenShort(t),15,fieldEnemy?Pink:Mint);
                string detail=t.kind=="resource" ? (passive!=null || rune!=null ? "공통 행동에 반응하는 " + (passive!=null?passive.name:rune.name) + "의 누적 상태입니다." : t.owner + "의 연계 기술로 소비하거나 강화합니다.") : t.kind=="discount" ? "대상 카드를 한 번 사용하면 사라집니다. 코스트가 0이면 에너지 없이 사용할 수 있습니다." : t.kind=="counter" ? "적의 공격이 적중하면 턴당 한 번 " + t.amount + "의 피해로 반격합니다." : t.kind=="revive" ? "치명상을 한 번 막고 체력을 " + t.amount + " 회복합니다." : t.kind=="empower_basic" ? "다음 기본 공격의 첫 적중 피해를 " + t.amount + " 늘립니다." : t.kind=="hot" ? "자신의 턴 종료마다 체력을 " + t.amount + " 회복합니다." : t.kind=="guard" ? "자신의 턴 종료마다 방어도를 " + t.amount + " 얻습니다." : t.kind=="energy_buff" ? "최대 코스트가 " + t.amount + " 증가합니다. 이미 얻은 에너지는 즉시 회수하지 않습니다." : t.kind=="damage_buff" ? "각 공격 카드의 첫 적중 피해가 " + t.amount + " 증가합니다." : t.kind=="evasion_buff" ? "회피율이 " + t.amount + "% 증가합니다." : t.kind=="exposure" ? "받는 공격 피해가 " + t.amount + "% 증가합니다." : t.kind=="heal_reduction" ? "받는 회복량이 " + t.amount + "% 감소합니다." : t.kind=="delayed_damage" ? "자신의 턴 종료 " + t.delay + "회 후 " + t.amount + "의 피해를 줍니다." : "자신의 턴 종료마다 " + t.amount + "의 피해를 줍니다.";
                if(t.kind=="free_cast") detail="이번 턴에 대상 카드 한 장을 코스트 없이 사용할 수 있습니다. 턴이 끝나면 사라집니다.";
                if(t.kind=="resource" && gear!=null) detail=gear.name+"의 장비 효과에 사용하는 누적 수치입니다. 장비 아이콘에서 발동 조건을 확인하세요.";
                if(t.kind=="deferred_damage") detail="아오자이로 미룬 체력 피해입니다. 남은 피해 "+t.amount+"을 "+t.remaining+"턴에 나누어 자신의 턴 시작마다 받습니다.";
                if(t.kind=="legacy_poison") detail="자신의 턴 시작에 피해 "+t.amount+"을 받고 중독 수치가 1 감소합니다.";
                if(t.kind=="legacy_weak") detail="공격 피해가 25% 감소합니다. 자신의 턴 종료에 남은 턴이 감소합니다.";
                if(t.kind=="legacy_vulnerable") detail="받는 공격 피해가 50% 증가합니다. 자신의 턴 종료에 남은 턴이 감소합니다.";
                if((t.kind??"").StartsWith("status_")) detail=StatusMechanics.Explain(t.kind.Substring(7));
                if(t.kind=="resource" && t.persistent) detail+=" 다음 전투에도 유지됩니다.";
                Para(new Rect(67,y+35,706,24),detail,11,Text);
            }
            GUI.EndScrollView();
            if(Btn(new Rect(233,565,813,42),"필드 닫기",true,Mint)) fieldInfo=false;
        }

        void DrawSettings()
        {
            Modal(new Rect(279, 123, 722, 469));
            Txt(new Rect(311, 150, 625, 38), "환경 설정", 25, Mint);
            Txt(new Rect(311, 219, 260, 28), "효과음 음량", 20, Text);
            volume = GUI.HorizontalSlider(new Rect(574, 225, 345, 25), volume, 0, 1);
            Txt(new Rect(311, 280, 280, 28), "효과음", 20, Text);
            if (Btn(new Rect(705, 278, 241, 36), sound ? "켜짐" : "꺼짐", sound, Mint)) sound = !sound;
            Txt(new Rect(311, 343, 370, 28), "움직임 최소화", 20, Text);
            if (Btn(new Rect(705, 342, 241, 36), reduceMotion ? "켜짐" : "꺼짐", reduceMotion, Mint)) reduceMotion = !reduceMotion;
            Txt(new Rect(311, 407, 340, 27), "화면 모드", 20, Text);
            if (Btn(new Rect(705, 405, 241, 36), Screen.fullScreen ? "전체 화면" : "창 모드", false, Gold)) Screen.fullScreen = !Screen.fullScreen;
            if (Btn(new Rect(311, 501, 635, 49), "설정 저장 / 닫기", true, Mint))
            {
                PlayerPrefs.SetFloat("lumia.volume", volume); PlayerPrefs.SetInt("lumia.sound", sound ? 1 : 0); PlayerPrefs.SetInt("lumia.reduceMotion", reduceMotion ? 1 : 0); PlayerPrefs.Save(); settings = false;
            }
        }

        void DrawHelp()
        {
            Modal(new Rect(150, 100, 980, 530));
            Txt(new Rect(185, 124, 900, 39), "실험 가이드", 25, Mint);
            Para(new Rect(185, 181, 900, 356), "01  새 지도는 막당 12구역, 총 3막입니다. 가로로 스크롤하여 경로를 살펴보세요. 기존 저장 지도의 길이는 그대로 이어집니다.\n\n02  매 턴 손패 5장과 코스트를 받습니다. 카드 코스트는 0~7이며, 최대 코스트는 레벨에 따라 5~8입니다. 방어도는 다음 내 턴에 사라집니다.\n\n03  스택·표식·설치물은 필드 창에서 확인하세요. 다음 카드 할인과 이번 턴 무료 사용권은 대상 기술에 적용됩니다. 0코스트가 되면 에너지 없이 사용할 수 있습니다.\n\n04  승리 보상 카드는 다시 누르면 선택이 취소됩니다. 선택 확정을 눌러야 덱에 들어갑니다.\n\n05  키오스크에서 재료·음식을 구입하세요. 모닥불은 완전 회복 후 만년 스프 또는 제작·요리 3회를 제공합니다. 장비는 슬롯마다 2개, 패시브는 3개까지 보유합니다.", 16, Text);
            if (Btn(new Rect(185, 550, 900, 45), "기록 확인", true, Mint)) help = false;
        }

        bool Act(Func<bool> action)
        {
            bool success = action();
            if (success) { ConsumeCombatActions(); Save(); }
            else Notify(Engine.State.message);
            return success;
        }
        void Notify(string message) { if (string.IsNullOrEmpty(message)) return; toast = message; toastUntil = Time.unscaledTime + 4; }
        public void Save()
        {
            if (Engine == null) return;
            try
            {
                string json = JsonUtility.ToJson(Engine.State, true);
                File.WriteAllText(savePath + ".tmp", json);
                if (File.Exists(savePath)) File.Copy(savePath, savePath + ".bak", true);
                File.Copy(savePath + ".tmp", savePath, true);
                File.Delete(savePath + ".tmp");
            }
            catch (Exception e) { Notify("저장 실패: " + e.Message); Debug.LogWarning(e); }
        }
        void Load()
        {
            try
            {
                var state = JsonUtility.FromJson<RunState>(File.ReadAllText(savePath));
                if (state == null || state.level < 1 || state.level > 20 || state.deck == null || state.map == null || !Enum.IsDefined(typeof(RunStage), state.stage)) throw new InvalidDataException("저장 기록 형식이 올바르지 않습니다.");
                Engine = new GameEngine(state); lobby = false;
                prepStep = state.stage == RunStage.Preparation ? string.IsNullOrEmpty(state.chosenPassive) ? 0 : 2 : 0;
            }
            catch (Exception e) { Notify("기록을 불러올 수 없습니다: " + e.Message); }
        }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }
        void PixelFont(Font rebuilt) { if (rebuilt == font && rebuilt.material && rebuilt.material.mainTexture) rebuilt.material.mainTexture.filterMode = FilterMode.Point; }
        void OnDestroy() { Font.textureRebuilt -= PixelFont; foreach (var clip in skillSounds.Values) if (clip) Destroy(clip); }

        public void VerificationView(string view)
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-lumia-verify") < 0) return;
            lobby = catalog = inventory = settings = help = fieldInfo = enemyLoadout = false;
            inspectCard = inspectTrait = inspectGear = craftPending = ""; inspectEnemyCard=false; summaryMode=true; scroll = inventoryScroll = encounterStoryScroll = detailScroll = Vector2.zero;summaryScroll.Clear();
            if (view == "lobby") { lobby = true; return; }
            bool showFull=view.EndsWith("_full",StringComparison.Ordinal);
            if(showFull)view=view.Substring(0,view.Length-5);
            NewRun();
            if (view == "starting_passives") { prepStep = 1; return; }
            Engine.SelectStartingPassive(Engine.State.passiveOffers[0]);
            if(view=="preparation_pinned")
            {
                Engine.ToggleDraft(Engine.State.draftOffers[0]);Engine.RerollDraft();prepStep=2;return;
            }
            foreach (string id in Engine.State.draftOffers.Take(3).ToArray()) Engine.ToggleDraft(id);
            if (view == "preparation") { prepStep = 2; return; }
            Engine.BeginJourney();
            if(view=="enemy_loadout" || view=="enemy_field")
            {
                Engine.State.act=3;Engine.State.row=Engine.State.mapRows-2;Engine.State.lane=1;Engine.State.level=20;
                Engine.EnterNode(Engine.AvailableNodes().First().lane);
                effects.Clear();Engine.CombatActions.Clear();
                if(view=="enemy_loadout") enemyLoadout=true;
                else {fieldInfo=true;fieldEnemy=true;}
                return;
            }
            if(view=="alex_weapon_deck")
            {
                Engine.State.gear=new List<string>{"fragarach","judgement"};Engine.State.passives=new List<string>{"alex_p"};
                Engine=new GameEngine(Engine.State);inventory=true;inventoryTab=0;return;
            }
            if(view=="gear_detail") {inspectGear="death_book";summaryMode=!showFull;return;}
            if (view == "combat" || view == "player_status" || view == "combo" || view=="combo_field" || view=="enemy_detail" || view.StartsWith("fx_") || view.StartsWith("mechanic_") || view.StartsWith("trait_") || view.StartsWith("status_") || view=="rune_buff" || view=="rune_healing")
            {
                if(view.StartsWith("trait_")) Engine.State.passives=new List<string>{"isaac_p"};
                if(view=="trait_revive") Engine.State.passives=new List<string>{"jenny_p"};
                if(view=="rune_buff") { Engine.State.level=20; Engine.State.mainRune="amplification_drone";Engine.State.supportRune="coupon";Engine.State.passives.Clear(); }
                if(view.StartsWith("mechanic_")) { Engine.State.passives.Clear();Engine.State.mainRune="diamond";Engine.State.supportRune="tempering"; }
                if(view.StartsWith("status_") || view=="rune_healing") { Engine.State.passives.Clear();Engine.State.mainRune=view=="rune_healing"?"healing_drone":"diamond";Engine.State.supportRune="coupon"; }
                Engine.EnterNode(Engine.AvailableNodes().First().lane);
                var c = Engine.State.combat;
                c.enemyHp = c.enemyMaxHp = 600;
                if(view=="player_status")
                {
                    c.evasion=18;c.evasionTurns=2;c.poison=2;c.weak=2;c.vulnerable=1;
                    StatusMechanics.Add(c.playerStatuses,"blind","isaac_e");fieldInfo=true;fieldEnemy=false;
                    Engine.CombatActions.Clear();effects.Clear();
                }
                else if(view=="rune_healing")
                {
                    Engine.State.maxHp=90;Engine.State.hp=31;c.block=0;c.evasion=0;
                    c.enemyName="아야";c.enemyId="aya";c.animal="";c.enemyLevel=1;
                    c.enemyDeck=new List<string>{"basic_attack"};c.enemyPlan=new List<string>{"basic_attack"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};
                    Engine.BeginEndTurn();Engine.AdvanceEnemyAction();Engine.AdvanceEnemyAction();ConsumeCombatActions();effects.Clear();
                    fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero;
                }
                else if(view.StartsWith("status_"))
                {
                    c.hand=new List<string>{"aya_e","nia_q","basic_attack","basic_guard"};c.energy=5;
                    StatusMechanics.Add(c.playerStatuses,"slow","nia_w");
                    if(view=="status_field" || view=="status_lock") {StatusMechanics.Add(c.playerStatuses,"silence","daniel_r");StatusMechanics.Add(c.playerStatuses,"root","isol_q");}
                    if(view=="status_field") {fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero;}
                    if(view=="status_detail") {inspectCard="nia_w";detailScroll=Vector2.zero;}
                    ConsumeCombatActions();effects.Clear();
                }
                else if(view.StartsWith("mechanic_"))
                {
                    Engine.State.deck=new List<string>{"nia_q","nia_q","nia_q","nia_w","nia_q"};
                    c.hand=Engine.State.deck.ToList();c.drawPile.Clear();c.discardPile.Clear();c.exhaustPile.Clear();c.energy=5;
                    Engine.PlayCard(0);Engine.PlayCard(0);Engine.PlayCard(0);
                    if(view!="mechanic_blocks") Engine.PlayCard(0);
                    ConsumeCombatActions();effects.Clear();
                    if(view=="mechanic_field") { fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero; }
                    if(view=="mechanic_detail") { inspectCard="nia_w";detailScroll=Vector2.zero; }
                }
                else if(view.StartsWith("trait_"))
                {
                    if(view=="trait_revive") {ConsumeCombatActions();effects.Clear();fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero;return;}
                    c.hand=new List<string>{"basic_attack","basic_attack","basic_attack","basic_guard"};Engine.State.hp=40;
                    Engine.PlayCard(0);Engine.PlayCard(0);
                    if(view=="trait_third") Engine.PlayCard(0);
                    ConsumeCombatActions();
                    if(view!="trait_third") effects.Clear();
                    if(view=="trait_detail") {inspectTrait="isaac_p";detailScroll=Vector2.zero;}
                }
                else if(view=="rune_buff")
                {
                    Engine.State.deck=new List<string>{"nia_r","nia_q","basic_attack","basic_guard"};c.hand=new List<string>{"nia_r","nia_q","basic_attack","basic_guard"};c.drawPile.Clear();
                    Engine.PlayCard(0);ConsumeCombatActions();effects.Clear();fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero;
                }
                else if (view == "combo" || view=="combo_field")
                {
                    Engine.State.deck = new List<string> { "yuki_w", "yuki_e", "basic_attack", "basic_attack" };
                    c.hand = new List<string> { "yuki_w", "basic_attack", "basic_attack" };
                    c.drawPile = new List<string> { "yuki_e" }; c.discardPile.Clear(); c.exhaustPile.Clear();
                    Engine.PlayCard(0); Engine.PlayCard(c.hand.IndexOf("basic_attack")); Engine.PlayCard(c.hand.IndexOf("basic_attack"));
                    ConsumeCombatActions(); effects.Clear();
                    if(view=="combo_field") {fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero;}
                }
                else if(view=="enemy_detail")
                {
                    c.enemyId="aya";c.enemyName="아야";c.animal="";
                    c.enemySkills.discounts.Add(new SkillCostDiscount{sourceCard="aya_e",targetCard="aya_w",amount=2});
                    inspectCard="aya_w";inspectEnemyCard=true;detailScroll=Vector2.zero;
                }
                else if (view == "fx_player")
                {
                    c.hand.Insert(0, "nia_q"); Engine.PlayCard(0); ConsumeCombatActions();
                }
                else if (view == "fx_enemy" || view == "fx_recover")
                {
                    string id = view == "fx_enemy" ? "aya_w" : "charlotte_w";
                    c.enemyName = view == "fx_enemy" ? "아야" : "샬럿"; c.enemyId = view == "fx_enemy" ? "aya" : "charlotte";
                    c.animal = ""; c.enemyLevel = 20; c.enemyHp = view == "fx_enemy" ? 600 : 120;
                    c.enemyDeck = new List<string> { id }; c.enemyPlan = new List<string> { id }; c.enemyPlanCosts = new List<int> { GameDatabase.Card(id).cost };
                    Engine.BeginEndTurn(); Engine.AdvanceEnemyAction(); ConsumeCombatActions();
                }
            }
            else if (view == "rewards" || view == "rewards_cancelled")
            {
                Engine.State.stage = RunStage.Rewards;
                Engine.State.rewards = new RewardState { xp = 36, credits = 75, cardBudget = 8, choices = new List<string> { "aya_q", "aya_w", "aya_e", "aya_r" } };
                Engine.ClaimCard("aya_r");
                if (view == "rewards_cancelled") Engine.ClaimCard("aya_r");
            }
            else if (view == "detail") { inspectCard = "sua_r"; detailScroll = Vector2.zero; }
            else if (view == "long_map")
            {
                Engine.State.act = 2; Engine.State.row = 8; Engine.State.lane = 1;
                foreach (var n in Engine.State.map.Where(n => n.act == 2 && n.row <= 8 && n.lane == 1)) n.visited = true;
            }
            else if (view == "kiosk" || view=="kiosk_coupon")
            {
                Engine.State.stage = RunStage.Kiosk; Engine.State.credits = 570;
                if(view=="kiosk_coupon") {Engine.State.mainRune="amplification_drone";Engine.State.supportRune="coupon";Engine.State.passives.Clear();}
            }
            else if (view == "campfire")
            {
                Engine.State.stage = RunStage.Campfire; Engine.State.campChoice = 0; Engine.State.campActions = 3;
                Engine.ChooseCamp(true); Engine.State.objects.AddRange(GameDatabase.Objects.Select(x => x.id));
                Engine.State.foods.Add(GameDatabase.Foods.First(x => !string.IsNullOrEmpty(x.upgradeTo)).id);
            }
            else if (view == "encounter" || view.StartsWith("encounter_"))
            {
                Engine.State.stage = RunStage.Encounter;
                Engine.State.encounterOffers = GameDatabase.Events.Take(3).Select(x => x.id).ToList();
                if(view!="encounter")
                {
                    string owner=GameDatabase.Characters.First(x=>x.id==(view=="encounter_long"?"debi_marlene":view=="encounter_trade"?"blair":view=="encounter_risky"?"craver":"nia")).name;
                    var encounter=GameDatabase.Events.First(x=>x.owner==owner);
                    Engine.State.encounterOffers.Add(encounter.id);Engine.SelectEncounter(encounter.id);
                    if(view=="encounter_trade") Engine.State.credits=0;
                    if(view=="encounter_risky") Engine.State.hp=8;
                    if(view=="encounter_detail") {inspectCard=EventPresentation.RewardCard(encounter.options.First(x=>EventPresentation.RewardCard(x)!=null)).id;detailScroll=Vector2.zero;}
                }
            }
            else if (view == "catalog") catalog = true;
            else if (view == "inventory") { inventory = true; inventoryTab = 2; Engine.State.foods.Add("soup"); Engine.State.objects.AddRange(GameDatabase.Objects.Take(3).Select(x => x.id)); }
            else if(view=="gear_inventory") {inventory=true;inventoryTab=1;Engine.State.gear=GameDatabase.Gear.GroupBy(x=>x.slot).SelectMany(x=>x.Take(2)).Select(x=>x.id).ToList();}
            else if (view == "passive_inventory")
            {
                inventory = true; inventoryTab = 3;
                Engine.State.passives = GameDatabase.Characters.Take(3).Select(x => x.passiveId).ToList();
            }
            else if(view=="rune_inventory") {inventory=true;inventoryTab=4;Engine.State.mainRune="amplification_drone";Engine.State.supportRune="coupon";}
            else if(view=="rune_detail") {inspectTrait="amplification_drone";detailScroll=Vector2.zero;}
            else if(view=="rune_selection") {Engine.State.stage=RunStage.Preparation;prepStep=0;}
            else if (view == "passive_replacement")
            {
                Engine.State.passives = GameDatabase.Characters.Take(3).Select(x => x.passiveId).ToList();
                Engine.State.pendingPassive = GameDatabase.Passives.OrderByDescending(x => x.description.Length).First(x => !Engine.State.passives.Contains(x.id)).id;
                Engine.State.stage = RunStage.PassiveChoice;
            }
            summaryMode=!showFull;
        }

        static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
        void Fill(Rect r, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Box(Rect r, Color bg, Color border)
        {
            Fill(r, border); Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), bg);
            Fill(new Rect(r.x + 4, r.y + 4, 5, 3), border); Fill(new Rect(r.xMax - 9, r.yMax - 7, 5, 3), border);
        }
        void Txt(Rect r, string value, int size, Color color, bool center = false)
        {
            var style = center ? centered : label; style.fontSize = size; style.normal.textColor = color;
            GUI.Label(r, value ?? "", style);
        }
        void Para(Rect r, string value, int size, Color color) { wrapped.fontSize = size; wrapped.normal.textColor = color; GUI.Label(r, value ?? "", wrapped); }
        bool Btn(Rect r, string value, bool primary = false, Color? tone = null, bool enabled = true)
        {
            Color accent = tone ?? Mint; bool hover = r.Contains(Event.current.mousePosition) && enabled;
            Box(r, enabled ? primary ? C("27433d") : hover ? C("263b49") : Panel : C("121c28"), enabled ? accent : Line);
            button.fontSize = value.Contains("\n") ? 14 : 16; button.normal.textColor = enabled ? accent : Muted;
            GUI.Label(new Rect(r.x + 8, r.y + 2, r.width - 16, r.height - 4), value, button);
            return Hit(r, enabled);
        }
        bool Hit(Rect r, bool enabled = true)
        {
            if (currentLayer != topLayer || !GUI.enabled || !enabled || !GUI.Button(r, GUIContent.none, GUIStyle.none)) return false;
            if (sound && audioSource && click) audioSource.PlayOneShot(click, volume); return true;
        }
        void Tex(Rect r, Texture texture, ScaleMode mode, Color? tint = null)
        {
            if (!texture) return; Color old = GUI.color; GUI.color = tint ?? Color.white; GUI.DrawTexture(r, texture, mode); GUI.color = old;
        }
        void Bar(Rect r, int current, int max, Color color) { Fill(r, Line); Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01((float)current / Math.Max(1, max)), r.height), color); }
        void Modal(Rect r) { Fill(new Rect(0, 0, W, H), new Color(0, .02f, .04f, .88f)); Box(r, Ink, Mint); }
        void PixelLine(Vector2 from, Vector2 to, Color color, int thickness)
        {
            int dx = (int)Mathf.Abs(to.x - from.x), dy = (int)Mathf.Abs(to.y - from.y);
            int steps = Math.Max(dx, dy) / 4;
            for (int i = 0; i <= steps; i++) { Vector2 p = Vector2.Lerp(from, to, (float)i / Math.Max(1, steps)); Fill(new Rect(Mathf.Round(p.x / 2) * 2, Mathf.Round(p.y / 2) * 2, 4, thickness), color); }
        }
        static string ZoneName(ZoneKind k) { string[] names = { "야생동물", "실험체", "키오스크", "모닥불", "실험체 조우", "보스" }; return names[(int)k]; }
        static string SlotName(GearSlot k) { string[] names = { "무기", "옷", "머리", "팔 / 장식", "다리" }; return names[(int)k]; }
    }
}
