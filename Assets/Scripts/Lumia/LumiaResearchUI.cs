using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool research, researchReadReport;
        string researchQuery = "", researchSubject = "nia";
        Vector2 researchListScroll, researchEndingScroll;
        bool researchLongReportVerification, researchLongReportBottomVerification;
        string researchVerifiedReportLayout;
        string researchFilteredQuery;
        CharacterDef[] researchFilteredCharacters;
        ResearchProfile researchTaskProfile;
        readonly Dictionary<string, ResearchTaskDisplay> researchTaskDisplays = new Dictionary<string, ResearchTaskDisplay>(StringComparer.Ordinal);
        readonly Dictionary<string, ResearchSubjectDef> researchDefinitions = new Dictionary<string, ResearchSubjectDef>(StringComparer.Ordinal);

        sealed class ResearchTaskDisplay
        {
            public ResearchSubjectProgress progress;
            public bool observed, uniqueCompleted;
            public string[] usedSkills;
            public ResearchTaskView[] tasks;
        }

        CharacterDef[] ResearchDisplayCharacters()
        {
            string query = (researchQuery ?? "").Trim();
            if (researchFilteredCharacters != null && string.Equals(query, researchFilteredQuery, StringComparison.OrdinalIgnoreCase)) return researchFilteredCharacters;
            researchFilteredQuery = query;
            researchFilteredCharacters = GameDatabase.Characters.Where(character => query.Length == 0 || character.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            return researchFilteredCharacters;
        }

        ResearchTaskView[] ResearchDisplayTasks(ResearchProfile profile, string subjectId)
        {
            if (!ReferenceEquals(profile, researchTaskProfile)) { researchTaskProfile = profile; researchTaskDisplays.Clear(); }
            ResearchSubjectProgress progress = null;
            if (profile?.subjects != null)
                for (int i = 0; i < profile.subjects.Count; i++)
                    if (profile.subjects[i]?.subjectId == subjectId) { progress = profile.subjects[i]; break; }
            var skills = progress?.usedSkills;
            int count = skills?.Count ?? 0;
            ResearchTaskDisplay display;
            bool reusable = researchTaskDisplays.TryGetValue(subjectId, out display)
                && ReferenceEquals(display.progress, progress) && display.observed == (progress?.observed == true)
                && display.uniqueCompleted == (progress?.uniqueCompleted == true) && display.usedSkills.Length == count;
            for (int i = 0; reusable && i < count; i++) reusable = display.usedSkills[i] == skills[i];
            if (reusable) return display.tasks;
            display = new ResearchTaskDisplay { progress = progress, observed = progress?.observed == true,
                uniqueCompleted = progress?.uniqueCompleted == true, usedSkills = skills == null ? new string[0] : skills.ToArray(),
                tasks = ResearchProgression.Tasks(profile, subjectId) };
            researchTaskDisplays[subjectId] = display;
            return display.tasks;
        }

        ResearchSubjectDef ResearchDisplayDefinition(string subjectId)
        {
            ResearchSubjectDef definition;
            if (!researchDefinitions.TryGetValue(subjectId, out definition))
            { definition = ResearchProgression.ForSubject(subjectId); researchDefinitions[subjectId] = definition; }
            return definition;
        }

        void OpenResearch()
        {
            RetryPendingResearch();
            research = true; researchReadReport = false; researchEndingScroll = Vector2.zero;
            if (GameDatabase.Character(researchSubject) == null) researchSubject = "nia";
        }

        void DrawResearch()
        {
            var profile = researchStore.Profile;
            Modal(new Rect(40, 39, 1200, 623));
            Txt(new Rect(64, 58, 960, 33), "하나의 연구 기록", 25, Mint);
            if (Btn(new Rect(1130, 59, 85, 32), "닫기", false, Muted)) { research = false; return; }
            string notice = !string.IsNullOrEmpty(researchStore.LoadError) ? "연구 기록 확인 필요: " + researchStore.LoadError
                : pendingResearchRuns > 0 ? "미정산 원정 " + pendingResearchRuns + "건을 안전하게 보관 중입니다. 연구 기록을 다시 열거나 다음 실행 때 정산을 재시도합니다."
                : "원정이 끝나면 기록이 축적됩니다. 완성한 보고서의 레어 조우는 다음 원정부터 등장합니다.";
            FitText(new Rect(65, 100, 1149, 23), notice, 13, researchStore.IsReadOnly ? Pink : Muted);
            ResearchMetric(new Rect(64, 134, 274, 51), "관측 데이터", profile.data.ToString(), Gold);
            ResearchMetric(new Rect(352, 134, 274, 51), "종료한 원정", profile.totalRuns.ToString(), Mint);
            ResearchMetric(new Rect(640, 134, 274, 51), "성공한 탈출", profile.totalWins.ToString(), Mint);
            ResearchMetric(new Rect(928, 134, 286, 51), "완성한 보고서", profile.completedReports.Count + " / " + GameDatabase.Characters.Count, Pink);

            researchQuery = GUI.TextField(new Rect(64, 202, 274, 35), researchQuery, field);
            if (string.IsNullOrEmpty(researchQuery)) Txt(new Rect(78, 211, 244, 22), "실험체 이름 검색", 13, Muted);
            var characters = ResearchDisplayCharacters();
            researchListScroll = GUI.BeginScrollView(new Rect(64, 247, 274, 373), researchListScroll, new Rect(0, 0, 250, Math.Max(373, characters.Length * 43)));
            var visible = CraftingCatalog.VisibleItems(characters.Length, researchListScroll.y, 373, 1, 43);
            for (int i = visible.first; i < visible.end; i++)
            {
                var character = characters[i]; var tasks = ResearchDisplayTasks(profile, character.id);
                Rect item = new Rect(0, i * 43, 250, 38);
                bool selected = researchSubject == character.id, complete = profile.completedReports.Contains(character.id);
                Box(item, selected ? C("254239") : Panel, selected ? Mint : Line);
                Tex(new Rect(5, item.y + 3, 31, 31), PixelArt.Portrait(character.name), ScaleMode.ScaleToFit);
                Txt(new Rect(43, item.y + 8, 145, 24), character.name, 15, selected ? Mint : Text);
                Txt(new Rect(185, item.y + 10, 58, 20), complete ? "완성" : tasks.Count(task => task.completed) + " / 3", 12, complete ? Gold : Muted, true);
                if (Hit(item)) { researchSubject = character.id; researchReadReport = false; }
            }
            GUI.EndScrollView();
            if (characters.Length == 0) Txt(new Rect(76, 265, 245, 28), "검색 결과가 없습니다.", 14, Muted);
            Txt(new Rect(64, 628, 274, 20), "전체 " + GameDatabase.Characters.Count + "명 · 진척은 원정 종료 후 반영", 11, Muted);
            DrawResearchSubject();
        }

        void ResearchMetric(Rect rect, string title, string value, Color color)
        {
            Box(rect, Panel, Line);
            Txt(new Rect(rect.x + 13, rect.y + 8, rect.width - 26, 18), title, 12, Muted);
            Txt(new Rect(rect.x + 13, rect.y + 25, rect.width - 26, 24), value, 21, color);
        }

        void DrawResearchSubject()
        {
            var profile = researchStore.Profile; var character = GameDatabase.Character(researchSubject) ?? GameDatabase.Characters[0];
            var definition = ResearchDisplayDefinition(character.id); var tasks = ResearchDisplayTasks(profile, character.id);
            bool complete = profile.completedReports.Contains(character.id), ready = tasks.All(task => task.completed);
            Tex(new Rect(361, 201, 86, 88), PixelArt.RarePortrait(character.id), ScaleMode.ScaleToFit);
            Txt(new Rect(465, 207, 726, 31), definition.title, 22, Mint);
            Txt(new Rect(466, 246, 726, 24), complete ? "연구 보고서 완성 · 레어 조우 해금" : "연구 과제 " + tasks.Count(task => task.completed) + " / 3 달성", 14, complete ? Gold : Muted);
            FitText(new Rect(466, 271, 726, 19), "레어 조우 모습 · " + RareSkinCatalog.NameForSubject(character.id), 12, Gold);
            if (researchReadReport && complete)
            {
                Box(new Rect(361, 300, 853, 322), Panel, Gold);
                Txt(new Rect(380, 317, 812, 27), "연구 보고서", 19, Gold);
                Para(new Rect(380, 359, 812, 130), definition.reportText, 17, Text);
                Fill(new Rect(380, 501, 812, 2), Line);
                Rect unlockRect = new Rect(380, 519, 812, 46);
                FitText(unlockRect, definition.unlockText, 15, Mint);
                VerifyResearchReportScroll(character.id, unlockRect, definition.unlockText);
                if (Btn(new Rect(380, 578, 812, 28), "연구 과제로 돌아가기", false, Muted)) researchReadReport = false;
                return;
            }
            for (int i = 0; i < tasks.Length; i++)
            {
                var task = tasks[i]; Rect rect = new Rect(361, 300 + i * 82, 853, 73);
                Box(rect, Panel, task.completed ? Mint : Line);
                Txt(new Rect(rect.x + 14, rect.y + 9, 654, 24), task.title, 17, task.completed ? Mint : Text);
                Txt(new Rect(rect.x + 679, rect.y + 10, 156, 22), task.completed ? "달성" : task.progress + " / " + task.target, 14, task.completed ? Mint : Gold, true);
                Para(new Rect(rect.x + 14, rect.y + 33, 817, 34), task.description, 13, Muted);
            }
            Box(new Rect(361, 551, 853, 71), Panel, complete || ready ? Gold : Line);
            string status = complete ? "보고서를 완성했습니다." : ready ? "세 연구 과제를 달성했습니다." : "세 연구 과제를 달성하면 보고서를 완성할 수 있습니다.";
            Txt(new Rect(377, 561, 556, 23), status, 14, complete || ready ? Gold : Text);
            Txt(new Rect(377, 589, 556, 20), complete ? "레어 후보: 1막 12% · 2막 18% · 3막 이후 25% · 막당 1회" : "완성 비용: 관측 데이터 " + ResearchProgression.ReportCost + (ready && profile.data < ResearchProgression.ReportCost ? " · 데이터가 부족합니다." : " · 보고서와 레어 조우를 해금합니다."), 11, Muted);
            if (complete)
            {
                if (Btn(new Rect(944, 565, 253, 42), "연구 보고서 읽기", true, Gold)) researchReadReport = true;
            }
            else if (Btn(new Rect(944, 565, 253, 42), "보고서 완성 · " + ResearchProgression.ReportCost, true, Gold, ready && profile.data >= ResearchProgression.ReportCost && !researchStore.IsReadOnly))
            {
                CompleteResearchReport(character.id);
            }
        }

        bool CompleteResearchReport(string subjectId)
        {
            RetryPendingResearch();
            string error;
            if (researchStore.TryCompleteReport(subjectId, out error)) { researchReadReport = true; AudioCue("encounter.choose"); return true; }
            Notify(error); return false;
        }

        void VerifyResearchReportScroll(string subjectId, Rect viewport, string text)
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-research-verify") < 0
                || Event.current.type != EventType.Repaint || researchVerifiedReportLayout == subjectId) return;
            string normalized = CachedNormalize(text);
            var key = new SummaryLayoutKey { text = text ?? "", size = 15, width = viewport.width, height = viewport.height, normalize = true };
            SummaryLayout layout;
            if (!summaryLayouts.TryGetValue(key, out layout) || layout.plain.content.text != normalized
                || Math.Abs(layout.plain.height - CachedTextHeight(normalized, 15, viewport.width)) > .1f)
                throw new InvalidOperationException("Research report must render its full normalized unlock text using the native cached layout: " + subjectId);
            bool scrolling = layout.plain.height > viewport.height;
            Vector2 position = Vector2.zero;
            float maxScrollY = 0;
            if (scrolling)
            {
                float expectedHeight = CachedTextHeight(normalized, 15, 797) + 5;
                if (layout.scrolling == null || layout.scrolling.content.text != normalized || layout.scrollWidth != 797
                    || Math.Abs(layout.scrollHeight - expectedHeight) > .1f || layout.scrollHeight <= viewport.height
                    || !summaryScroll.TryGetValue(layout.scrollKey, out position))
                    throw new InvalidOperationException("Research report must retain the complete unlock text in its real vertical scroll view: " + subjectId);
                maxScrollY = Math.Max(0, expectedHeight - viewport.height);
            }
            else if (layout.scrolling != null)
                throw new InvalidOperationException("A fitting research unlock must retain the plain text layout: " + subjectId);
            if (researchLongReportVerification && (subjectId != "yuki" || !scrolling))
                throw new InvalidOperationException("The long research report fixture must visibly exercise Yuki's unlock scrollbar.");
            if (researchLongReportVerification && (researchLongReportBottomVerification
                    ? position.y <= 0 || Math.Abs(position.y - maxScrollY) > .1f : Math.Abs(position.y) > .1f))
                throw new InvalidOperationException("The long research report must reach its requested real scroll position.");
            researchVerifiedReportLayout = subjectId;
            Debug.Log("BTS RESEARCH REPORT SCROLL PASS / subject=" + subjectId + " / scrolling=" + scrolling
                + " / viewport=46 / contentWidth=" + (scrolling ? layout.scrollWidth : viewport.width)
                + " / fullText=PASS / cachedNativeHeight=PASS / position=" + (researchLongReportVerification ? researchLongReportBottomVerification ? "bottom" : "top" : "plain")
                + " / scrollY=" + position.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                + " / maxScrollY=" + maxScrollY.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        void DrawResearchEnding()
        {
            var state = Engine.State; bool won = state.stage == RunStage.Won;
            var report = researchStore.GetSettlement(state);
            Tex(new Rect(0, 0, W, H), PixelArt.Background(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, W, H), new Color(.025f, .05f, .075f, .91f));
            Txt(new Rect(50, 34, 1180, 49), ModeEndingTitle(state), 36, won || state.endlessRetired ? Mint : Pink, true);
            Txt(new Rect(50, 92, 1180, 32), ModeEndingSubtitle(state), 23, Text, true);
            Txt(new Rect(50, 136, 1180, 24), ModeEndingSummary(state), 15, Muted, true);
            Box(new Rect(48, 183, 554, 402), Panel, Gold);
            Txt(new Rect(68, 200, 514, 30), "이번 원정의 관측 데이터", 22, Gold);
            if (report == null || report.legacy)
            {
                Para(new Rect(68, 251, 514, 124), "연구 기록 도입 전에 종료한 원정입니다.\n새 원정부터 관측 데이터와 연구 진척을 얻습니다.", 18, Muted);
            }
            else
            {
                int y = 249;
                foreach (var line in report.lines)
                {
                    Txt(new Rect(69, y, 392, 24), line.title + (line.count > 0 ? "  " + line.count + " × " + line.unit : ""), 14, Text);
                    Txt(new Rect(468, y, 110, 24), "+ " + line.amount, 16, line.amount > 0 ? Gold : Muted, true);
                    y += 28;
                }
                Fill(new Rect(68, 509, 514, 2), Line);
                Txt(new Rect(69, 527, 350, 30), "총 획득", 19, Gold);
                Txt(new Rect(428, 521, 150, 42), "+ " + report.earned, 28, Gold, true);
            }
            Box(new Rect(622, 183, 610, 402), Panel, Mint);
            Txt(new Rect(642, 200, 568, 30), "이번 원정의 연구 성과", 22, Mint);
            string notes = ResearchEndingNotes(report, state);
            float notesHeight = Math.Max(280, CachedTextHeight(notes, 15, 540) + 12);
            researchEndingScroll = GUI.BeginScrollView(new Rect(643, 249, 567, 310), researchEndingScroll, new Rect(0, 0, 540, notesHeight));
            Para(new Rect(0, 0, 540, notesHeight), notes, 15, Text); GUI.EndScrollView();
            bool pending = !string.IsNullOrEmpty(researchSaveError) || researchStore.IsReadOnly;
            Txt(new Rect(49, 598, 1182, 25), pending ? "연구 정산이 보류되었습니다. 기록을 저장한 뒤 다시 시도해주세요." : "보유 관측 데이터 " + researchStore.Profile.data + (report?.legacy == true ? " · 새 원정부터 연구 기록이 축적됩니다." : " · 연구 진척과 첫 관측 기록이 보존되었습니다."), 14, pending ? Pink : Muted, true);
            if (pending && Btn(new Rect(935, 133, 297, 31), "연구 정산 다시 시도", false, Pink)) Save();
            if (Btn(new Rect(48, 641, 328, 48), "로비로 돌아가기", false, Muted)) { lobby = true; Save(); }
            DrawCombatInformationButton(new Rect(390, 641, 212, 48));
            if (Btn(new Rect(622, 641, 610, 48), "연구 기록 확인", true, Mint)) { lobby = true; Save(); OpenResearch(); }
        }

        static string ResearchEndingNotes(ResearchSettlement report, RunState state)
        {
            if (report == null || report.legacy) return "니아가 실험 기록의 보존 기능을 복구했습니다.\n\n다음 원정에서는 실패하더라도 완료한 구역과 전투, 새 관측과 실험체 연구 성과가 연구소에 남습니다.";
            var lines = new List<string>();
            lines.Add("완료 구역 " + report.nodes + "곳 · 전투 승리 " + report.victories + "회 · 보스 처치 " + report.bosses + "회");
            lines.Add("보스 특수 행동 저지 " + report.interruptions + "회");
            lines.Add("\n관측한 실험체 " + report.observedSubjects.Count + "명 · 첫 관측 " + report.newSubjects + "명");
            if (report.observedSubjects.Count > 0) lines.Add(string.Join(" · ", report.observedSubjects.Select(id => GameDatabase.Character(id)?.name ?? id)));
            lines.Add("\n새로 달성한 연구 과제 " + report.completedTasks.Count + "개");
            if (report.completedTasks.Count > 0) lines.Add(string.Join("\n", report.completedTasks.Select(ResearchTaskLabel)));
            lines.Add("\n성립한 교차 연계 " + report.synergyKeys.Count + "종 · 첫 연계 " + report.newSynergies + "종");
            if (report.synergyKeys.Count > 0) lines.Add(string.Join("\n", report.synergyKeys.Select(ResearchSynergyLabel)));
            if (state.deck.Count > 0)
            {
                var skills = state.deck.Select(GameDatabase.Card).Where(card => card != null && card.category == "skill").GroupBy(card => card.owner).OrderByDescending(group => group.Count()).Take(3);
                lines.Add("\n최종 덱의 주요 실험체\n" + string.Join(" · ", skills.Select(group => group.Key + " " + group.Count() + "장")));
            }
            return string.Join("\n", lines);
        }

        static string ResearchSynergyLabel(string key)
        {
            string[] parts = (key ?? "").Split('|');
            string[] keys = { "vf", "bomb", "oil", "wound", "displace", "mobility", "bloom", "support" };
            string[] names = { "VF 강화", "폭탄", "기름과 화염", "상처", "위치 제어", "기동", "개화", "지원" };
            int index = Array.IndexOf(keys, parts[0]); string family = index < 0 ? parts[0] : names[index];
            return parts.Length < 3 ? family : family + " · " + (GameDatabase.Character(parts[1])?.name ?? parts[1]) + " → " + (GameDatabase.Character(parts[2])?.name ?? parts[2]);
        }

        static string ResearchTaskLabel(string key)
        {
            string subject = (key ?? "").Split(':')[0];
            var task = ResearchProgression.Tasks(null, subject).FirstOrDefault(value => value.id == key);
            return (GameDatabase.Character(subject)?.name ?? subject) + " · " + (task?.title ?? key);
        }

        string ResearchVerificationProfilePath
        {
            get
            {
                if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-research-verify") < 0 && !RareV2VerificationEnabled
                    || Path.GetFileName(savePath) != "lumia-verification.json")
                    throw new InvalidOperationException("Research verification must use isolated run and profile slots.");
                return Path.Combine(Application.persistentDataPath, "bts-research-verification.json");
            }
        }

        void ResetResearchVerificationProfile(ResearchProfile profile)
        {
            string path = ResearchVerificationProfilePath;
            foreach (string pending in PendingResearchFiles()) File.Delete(pending);
            pendingResearchRuns = 0;
            File.WriteAllText(path, JsonUtility.ToJson(profile, true));
            researchStore = new ResearchStore(path, value => JsonUtility.ToJson(value, true), json => JsonUtility.FromJson<ResearchProfile>(json));
            researchSaveError = "";
        }

        ResearchProfile ResearchVerificationProfile(int data = 750, bool ready = false, bool completed = false)
        {
            var profile = new ResearchProfile { data = data, totalData = data, totalRuns = 4, totalWins = 1 };
            profile.subjects.Add(new ResearchSubjectProgress { subjectId = "nia", observed = true, uniqueCompleted = ready,
                usedSkills = ready ? GameDatabase.Character("nia").cards.ToList() : new List<string> { "nia_q", "nia_w" } });
            if (completed) profile.completedReports.Add("nia");
            return profile;
        }

        GameEngine NewResearchVerificationRun(int seed)
        {
            var engine = new GameEngine(seed);
            if (!engine.SetResearchUnlocks(researchStore.Profile.completedReports)) throw new InvalidOperationException("New run could not snapshot completed research reports.");
            var main = GameDatabase.Runes.First(rune => rune.main);
            engine.SetRunes(main.id, GameDatabase.Runes.First(rune => !rune.main && rune.tree == main.tree).id);
            engine.SelectStartingPassive(engine.State.passiveOffers[0]);
            foreach (string id in engine.State.draftOffers.Take(3).ToArray()) engine.ToggleDraft(id);
            if (!engine.BeginJourney()) throw new InvalidOperationException("Native research verification could not begin a run.");
            return engine;
        }

        void PrepareResearchNiaVictory(int seed)
        {
            Engine = NewResearchVerificationRun(seed); lobby = false;
            var state = Engine.State; var node = Engine.AvailableNodes().First(); node.kind = ZoneKind.Subject; node.assignedOpponent = "nia";
            state.passives.Clear(); state.gear.Clear(); state.mainRune = state.supportRune = "";
            if (!Engine.EnterNode(node.lane)) throw new InvalidOperationException("Research verification could not enter Nia combat.");
            var combat = state.combat;
            combat.enemyHp = combat.enemyMaxHp = 2000; combat.enemyBlock = combat.enemyEvasion = 0;
            combat.enemyGear.Clear(); combat.enemyPassiveId = ""; combat.enemyTraits = new TraitActorState(); combat.enemySkills = new SkillActorState(); combat.enemyStatuses = new StatusActorState();
            combat.hand = new List<string> { "nia_q", "nia_q", "nia_q", "nia_w", "nia_e", "nia_r", "basic_attack" };
            combat.energy = 200;
            foreach (string id in new[] { "nia_q", "nia_q", "nia_q", "nia_w", "nia_e", "nia_r" })
                if (!Engine.PlayCard(combat.hand.IndexOf(id))) throw new InvalidOperationException("Native research skill failed: " + id);
            if (!state.research.observedSubjects.Contains("nia") || !state.research.completedUniqueSubjects.Contains("nia") || GameDatabase.Character("nia").cards.Any(id => !state.research.usedSkillIds.Contains(id)))
                throw new InvalidOperationException("Actual Nia block conversion and four used skills did not reach the research journal.");
            combat.enemyHp = 1; combat.enemyBlock = 0;
            if (!Engine.PlayCard(combat.hand.IndexOf("basic_attack")) || state.stage != RunStage.Rewards || !Engine.FinishRewards() || !node.visited)
                throw new InvalidOperationException("Native research victory and completed node were not recorded.");
            effects.Clear(); Engine.CombatActions.Clear();
        }

        void PrepareResearchUnlockedEncounter(int seed)
        {
            Engine = NewResearchVerificationRun(seed); lobby = false; research = false;
            var node = Engine.AvailableNodes().First(); node.kind = ZoneKind.Encounter;
            var encounter = ResearchEncounters.EventForSubject("nia");
            if (encounter == null || !Engine.State.research.unlockedReports.Contains("nia") || !Engine.EnterNode(node.lane))
                throw new InvalidOperationException("A new run must make the completed Nia report's additional encounter eligible.");
            Engine.State.encounterOffers = new List<string> { encounter.id };
            if (!Engine.SelectEncounter(encounter.id) || Engine.CurrentEncounter?.id != encounter.id)
                throw new InvalidOperationException("Research encounter must be selectable in the native encounter screen.");
            effects.Clear(); Engine.CombatActions.Clear();
        }

        public void VerificationResearchFlows()
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-research-verify") < 0) return;
            ResetResearchVerificationProfile(new ResearchProfile { data = ResearchProgression.ReportCost, totalData = ResearchProgression.ReportCost });
            PrepareResearchNiaVictory(79101);
            Engine.State.stage = RunStage.Lost; Engine.State.hp = 0;
            var expected = researchStore.GetSettlement(Engine.State); string runId = Engine.State.research.runId;
            Save(); int data = researchStore.Profile.data;
            if (data != ResearchProgression.ReportCost + expected.earned || data < ResearchProgression.ReportCost || researchStore.Profile.totalRuns != 1
                || !Engine.State.research.settled || !ResearchProgression.Tasks(researchStore.Profile, "nia").All(task => task.completed))
                throw new InvalidOperationException("Lost run must persist actual observation data and all three Nia research tasks.");
            string profileBefore = JsonUtility.ToJson(researchStore.Profile), runBefore = JsonUtility.ToJson(Engine.State); int rng = Engine.State.rngState;
            foreach (var character in GameDatabase.Characters)
            {
                ResearchProgression.Tasks(researchStore.Profile, character.id); ResearchProgression.ForSubject(character.id); ResearchProgression.ReportText(researchStore.Profile, character.id);
            }
            researchStore.GetSettlement(Engine.State);
            if (profileBefore != JsonUtility.ToJson(researchStore.Profile) || runBefore != JsonUtility.ToJson(Engine.State) || Engine.State.rngState != rng)
                throw new InvalidOperationException("Reading research UI data must not mutate the profile, run, or game RNG.");
            Save(); Save(); Load(); Save();
            if (researchStore.Profile.data != data || researchStore.Profile.totalRuns != 1 || Engine.State.research.runId != runId)
                throw new InvalidOperationException("Repeated end screen saves and terminal reload must never grant duplicate research data.");
            Engine = NewResearchVerificationRun(79102); string snapshot = JsonUtility.ToJson(Engine.State.research); int cost = ResearchProgression.ReportCost;
            if (!CompleteResearchReport("nia") || researchStore.Profile.data != data - cost || !researchStore.Profile.completedReports.Contains("nia")
                || snapshot != JsonUtility.ToJson(Engine.State.research))
                throw new InvalidOperationException("Report completion must spend its configured data cost without changing an ongoing run's unlock snapshot.");
            if (CompleteResearchReport("nia") || researchStore.Profile.data != data - cost)
                throw new InvalidOperationException("A completed report must not charge its data a second time.");
            PrepareResearchUnlockedEncounter(79103);
            string encounterId = Engine.CurrentEncounter.id;
            Engine.State.credits = 1000;
            int rareCredits = Engine.State.credits;
            if (!Engine.ChooseEventOption(0) || !Engine.State.research.completedResearchEvents.Contains(encounterId)
                || Engine.State.credits != rareCredits - GameDatabase.Event(encounterId).options[0].amount || !Engine.State.deck.Any(id=>Engine.CardUpgradeLevel(id)==2))
                throw new InvalidOperationException("Choosing an unlocked research encounter option must persist its completion.");
            ResetResearchVerificationProfile(new ResearchProfile()); Engine = NewResearchVerificationRun(79104);
            for (int act = 1; act <= 3; act++) CompleteBossPathsBattle(act, 1);
            Save();
            if (Engine.State.stage != RunStage.Won || researchStore.Profile.totalWins != 1 || researchStore.Profile.totalRuns != 1
                || researchStore.GetSettlement(Engine.State).escapeBonus != 12 || researchStore.GetSettlement(Engine.State).bosses != 3)
                throw new InvalidOperationException("Actual three-boss escape must award the escape and boss research data once.");
            int wonData = researchStore.Profile.data; Load(); Save();
            if (researchStore.Profile.data != wonData || researchStore.Profile.totalRuns != 1 || researchStore.Profile.totalWins != 1)
                throw new InvalidOperationException("Reloading an escaped run must not duplicate observation data or wins.");
            VerificationResearchLegacyJson();
            VerificationResearchPending();
            research = false; lobby = true; researchReadReport = false; researchSaveError = ""; toastUntil = 0;
            Debug.Log("BTS RESEARCH FLOW PASS / subjects=91 / actualTasks=3 / loss=PASS / escape=PASS / duplicateReload=PASS / reportCost=" + ResearchProgression.ReportCost + " / snapshot=PASS / encounter=PASS / legacy=PASS / isolatedSave=PASS / readOnly=PASS / pending=PASS");
        }

        // Unity can serialize a null plain class as a default object. Removing the
        // top-level member reproduces a real v0.1.18 save independently of that behavior.
        static string WithoutResearchField(string json)
        {
            int start = json.IndexOf("\"research\"", StringComparison.Ordinal);
            if (start < 0) return json;
            int value = json.IndexOf(':', start) + 1;
            while (value < json.Length && char.IsWhiteSpace(json[value])) value++;
            int end = value, depth = 0; bool quoted = false, escaped = false;
            for (; end < json.Length; end++)
            {
                char character = json[end];
                if (quoted)
                {
                    if (escaped) escaped = false;
                    else if (character == '\\') escaped = true;
                    else if (character == '"') quoted = false;
                    continue;
                }
                if (character == '"') { quoted = true; continue; }
                if (character == '{' || character == '[') depth++;
                else if (character == '}' || character == ']')
                {
                    if (depth == 0) break;
                    if (--depth == 0) { end++; break; }
                }
                else if (depth == 0 && character == ',') break;
            }
            while (end < json.Length && char.IsWhiteSpace(json[end])) end++;
            if (end < json.Length && json[end] == ',') end++;
            else
            {
                int previous = start - 1;
                while (previous >= 0 && char.IsWhiteSpace(json[previous])) previous--;
                if (previous >= 0 && json[previous] == ',') start = previous;
            }
            return json.Remove(start, end - start);
        }

        void VerificationResearchLegacyJson()
        {
            int terminalCases = 0, activeCases = 0;
            foreach (bool missingField in new[] { true, false })
            {
                foreach (var stage in new[] { RunStage.Lost, RunStage.Won })
                {
                    ResetResearchVerificationProfile(new ResearchProfile()); Engine = NewResearchVerificationRun(79105 + terminalCases);
                    Engine.State.stage = stage; Engine.State.research = null;
                    string json = JsonUtility.ToJson(Engine.State, true);
                    if (missingField) json = WithoutResearchField(json);
                    Engine = new GameEngine(JsonUtility.FromJson<RunState>(json)); Save();
                    if (!Engine.State.research.legacyEnded || !Engine.State.research.settled || researchStore.Profile.totalRuns != 0 || researchStore.Profile.data != 0)
                        throw new InvalidOperationException("Previously ended legacy runs must not receive retrospective research awards: " + stage + " / missing=" + missingField);
                    terminalCases++;
                }
                ResetResearchVerificationProfile(new ResearchProfile()); Engine = NewResearchVerificationRun(79110 + activeCases);
                Engine.State.row = 0; Engine.State.lane = 1; Engine.State.map.First(node => node.act == 1 && node.row == 0 && node.lane == 1).visited = true;
                Engine.State.research = null; int rng = Engine.State.rngState;
                string activeJson = JsonUtility.ToJson(Engine.State, true);
                if (missingField) activeJson = WithoutResearchField(activeJson);
                Engine = new GameEngine(JsonUtility.FromJson<RunState>(activeJson));
                if (!Engine.State.research.begun || Engine.State.research.legacyEnded || Engine.State.research.completedNodeIds.Count != 0 || Engine.State.rngState != rng)
                    throw new InvalidOperationException("An active legacy run must start a fresh journal without retrospective nodes or gameplay RNG changes.");
                var next = Engine.AvailableNodes().First(); next.kind = ZoneKind.Subject; next.assignedOpponent = "nia";
                if (!Engine.EnterNode(next.lane)) throw new InvalidOperationException("Migrated active legacy research fixture could not enter combat.");
                var combat = Engine.State.combat; combat.enemyEvasion = combat.enemyBlock = 0; combat.enemyHp = combat.enemyMaxHp = 2000;
                combat.hand = new List<string> { "nia_q" }; combat.energy = 200;
                if (!Engine.PlayCard(0) || !Engine.State.research.usedSkillIds.Contains("nia_q") || !Engine.State.research.observedSubjects.Contains("nia"))
                    throw new InvalidOperationException("Actual post-migration skill use and observation must enter an active legacy run's journal.");
                Engine.State.stage = RunStage.Lost; Engine.State.hp = 0; Save();
                if (researchStore.Profile.totalRuns != 1 || researchStore.Profile.data <= 0 || researchStore.GetSettlement(Engine.State).nodes != 0)
                    throw new InvalidOperationException("An active migrated legacy run must award only its new observations at the real terminal stage.");
                effects.Clear(); Engine.CombatActions.Clear(); activeCases++;
            }
            Debug.Log("BTS RESEARCH LEGACY JSON PASS / missingField=PASS / nullReference=PASS / terminalCases=" + terminalCases + " / activeCases=" + activeCases);
        }

        void VerificationResearchPending()
        {
            string profilePath = ResearchVerificationProfilePath;
            for (int restart = 0; restart < 2; restart++)
            {
                ResetResearchVerificationProfile(new ResearchProfile()); PrepareResearchNiaVictory(79301 + restart);
                Engine.State.stage = RunStage.Lost; Engine.State.hp = 0;
                int expected = researchStore.GetSettlement(Engine.State).earned;
                string runId = Engine.State.research.runId, pendingPath = PendingResearchPath(Engine.State);
                string terminal = JsonUtility.ToJson(Engine.State, true);
                using (var profileLock = new FileStream(profilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    Save();
                    if (Engine.State.research.settled || string.IsNullOrEmpty(researchSaveError))
                        throw new InvalidOperationException("A locked profile must leave its terminal run eligible for retry.");
                    if (restart == 1)
                    {
                        Engine = null;
                        researchStore = new ResearchStore(profilePath, value => JsonUtility.ToJson(value, true), json => JsonUtility.FromJson<ResearchProfile>(json));
                        RetryPendingResearch();
                    }
                    NewRun();
                    if (Engine == null || Engine.State.stage != RunStage.Preparation || Engine.State.research.runId == runId
                        || !File.Exists(pendingPath) || ParseRunSave(File.ReadAllText(pendingPath)).research.runId != runId
                        || researchStore.Profile.data != 0 || pendingResearchRuns != 1)
                        throw new InvalidOperationException("New run must durably queue a failed terminal settlement, including after restart.");
                }
                string nextRun = JsonUtility.ToJson(Engine.State); int rng = Engine.State.rngState;
                OpenResearch();
                if (File.Exists(pendingPath) || pendingResearchRuns != 0 || researchStore.Profile.data != expected || researchStore.Profile.totalRuns != 1
                    || nextRun != JsonUtility.ToJson(Engine.State) || Engine.State.rngState != rng)
                    throw new InvalidOperationException("Opening research must replay its queued run exactly once without changing the new run.");
                // Simulate a crash after the permanent profile was replaced but before
                // the queue file could be deleted. The ledger must deduplicate it.
                WriteAtomicRunFile(pendingPath, terminal, false);
                RetryPendingResearch(); RetryPendingResearch();
                if (File.Exists(pendingPath) || researchStore.Profile.data != expected || researchStore.Profile.totalRuns != 1)
                    throw new InvalidOperationException("Replaying an already-paid queue entry must delete it without duplicate data.");
            }
            ResetResearchVerificationProfile(new ResearchProfile()); PrepareResearchNiaVictory(79303);
            Engine.State.stage = RunStage.Lost; Engine.State.hp = 0;
            string blockedId = Engine.State.research.runId, blockedPath = PendingResearchPath(Engine.State);
            WriteAtomicRunFile(blockedPath, JsonUtility.ToJson(Engine.State, true), false);
            using (var profileLock = new FileStream(profilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (var queueLock = new FileStream(blockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Save(); string savedBefore = File.ReadAllText(savePath); NewRun();
                if (Engine.State.research.runId != blockedId || Engine.State.stage != RunStage.Lost || savedBefore != File.ReadAllText(savePath)
                    || !toast.Contains("새 원정을 시작하지 않았습니다"))
                    throw new InvalidOperationException("A failed queue write must block NewRun and preserve the terminal run and its saved file.");
            }
            RetryPendingResearch();
            if (File.Exists(blockedPath) || researchStore.Profile.totalRuns != 1)
                throw new InvalidOperationException("An unblocked queued settlement must remain recoverable after a denied NewRun.");
            ResetResearchVerificationProfile(new ResearchProfile()); Engine = NewResearchVerificationRun(79304); Save(); Save();
            string restoredId = Engine.State.research.runId; int restoredRng = Engine.State.rngState;
            File.WriteAllText(savePath, "damaged verification run"); Engine = null; Load();
            if (Engine == null || Engine.State.research.runId != restoredId || Engine.State.rngState != restoredRng
                || ParseRunSave(File.ReadAllText(savePath)).research.runId != restoredId
                || ParseRunSave(File.ReadAllText(savePath + ".bak")).research.runId != restoredId)
                throw new InvalidOperationException("Load must recover a valid backup and atomically repair its primary without damaging the backup.");
        }

        public void VerificationResearchLayout()
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-research-verify") < 0) return;
            int measurements = 0; float maxTask = 0, maxReport = 0;
            var taskStyle = new GUIStyle(wrapped) { fontSize = 13 }; var reportStyle = new GUIStyle(wrapped) { fontSize = 17 };
            foreach (var character in GameDatabase.Characters)
            {
                foreach (var task in ResearchProgression.Tasks(researchStore.Profile, character.id))
                {
                    font.RequestCharactersInTexture(task.description, 13, FontStyle.Normal);
                    float height = taskStyle.CalcHeight(new GUIContent(task.description), 817); maxTask = Math.Max(maxTask, height); measurements++;
                    if (height > 34.1f) throw new InvalidOperationException("Research task text does not fit: " + task.id + " / " + height);
                }
                string report = ResearchProgression.ForSubject(character.id).reportText;
                font.RequestCharactersInTexture(report, 17, FontStyle.Normal);
                float reportHeight = reportStyle.CalcHeight(new GUIContent(report), 812); maxReport = Math.Max(maxReport, reportHeight); measurements++;
                if (reportHeight > 130.1f) throw new InvalidOperationException("Research report text does not fit: " + character.id + " / " + reportHeight);
            }
            Debug.Log("BTS RESEARCH LAYOUT PASS / measurements=" + measurements + " / taskMax=" + maxTask + " / reportMax=" + maxReport + " / nativeFont=PASS");
            int rareMeasurements = 0, previewMeasurements = 0, scrollingUnlocks = 0; float maxUnlock = 0, maxSkin = 0;
            var unlockStyle = new GUIStyle(wrapped) { fontSize = 15 }; var skinStyle = new GUIStyle(wrapped) { fontSize = 12 };
            var previewStyle = new GUIStyle(wrapped) { fontSize = 14 };
            foreach (var character in GameDatabase.Characters)
            {
                string unlock = ResearchProgression.ForSubject(character.id).unlockText, skin = "레어 조우 모습 · " + RareSkinCatalog.NameForSubject(character.id);
                string normalizedUnlock = CachedNormalize(unlock);
                font.RequestCharactersInTexture(normalizedUnlock, 15, FontStyle.Normal); font.RequestCharactersInTexture(skin, 12, FontStyle.Normal);
                float unlockHeight = unlockStyle.CalcHeight(new GUIContent(normalizedUnlock), 812), skinHeight = skinStyle.CalcHeight(new GUIContent(skin), 726);
                maxUnlock = Math.Max(maxUnlock, unlockHeight); maxSkin = Math.Max(maxSkin, skinHeight); rareMeasurements += 2;
                if (skinHeight > 19.1f) throw new InvalidOperationException("Rare report skin name does not fit: " + character.id + " / " + skinHeight);
                if (string.IsNullOrWhiteSpace(normalizedUnlock) || Math.Abs(CachedTextHeight(normalizedUnlock, 15, 812) - unlockHeight) > .1f)
                    throw new InvalidOperationException("Research unlock native height and complete normalized content must match the display cache: " + character.id);
                if (unlockHeight > 46)
                {
                    float contentHeight = CachedTextHeight(normalizedUnlock, 15, 797) + 5;
                    float measuredHeight = unlockStyle.CalcHeight(new GUIContent(normalizedUnlock), 797) + 5;
                    if (float.IsNaN(contentHeight) || float.IsInfinity(contentHeight) || contentHeight <= 46
                        || contentHeight < unlockHeight || Math.Abs(contentHeight - measuredHeight) > .1f)
                        throw new InvalidOperationException("Research unlock scroll content must preserve the full native text height: " + character.id);
                    scrollingUnlocks++;
                }
                string preview = RareSkinStories.Preview(ResearchEncounters.EventForSubject(character.id).story);
                font.RequestCharactersInTexture(preview, 14, FontStyle.Normal);
                float previewHeight = previewStyle.CalcHeight(new GUIContent(preview), 337); previewMeasurements++;
                if (previewHeight > 65.1f) throw new InvalidOperationException("Rare encounter preview needs scrolling: " + character.id + " / " + previewHeight);
            }
            Debug.Log("BTS RARE LAYOUT PASS / measurements=" + rareMeasurements + " / unlockMax=" + maxUnlock + " / skinMax=" + maxSkin + " / nativeFont=PASS");
            if (scrollingUnlocks == 0) throw new InvalidOperationException("The native research audit must exercise the authored long unlock texts.");
            Debug.Log("BTS RARE UNLOCK SCROLL LAYOUT PASS / scrollingUnlocks=" + scrollingUnlocks
                + " / viewport=46 / contentWidth=797 / fullText=PASS / cachedNativeHeight=PASS");
            Debug.Log("BTS RARE PREVIEW PASS / scenes=" + previewMeasurements + " / nativeFont=PASS");
        }

        public void VerificationRareContent()
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-research-verify") < 0) return;
            int portraits = 0, skins = 0, choices = 0;
            foreach (var character in GameDatabase.Characters)
            {
                if (string.IsNullOrWhiteSpace(RareSkinCatalog.NameForSubject(character.id))) throw new InvalidOperationException("Rare skin has no name: " + character.id);
                skins++;
                if (!PixelArt.HasRarePortrait(character.id)) throw new InvalidOperationException("Rare skin resource is missing: " + character.id);
                var sprite = PixelArt.RarePortrait(character.id);
                if (sprite == null || sprite.height != PixelArt.PortraitPixelHeight || sprite.filterMode != FilterMode.Point
                    || ReferenceEquals(sprite, PixelArt.Portrait(character.id)) || !ReferenceEquals(sprite, PixelArt.RarePortrait(character.id)))
                    throw new InvalidOperationException("Rare sprite size, point sampling, isolation, or cache failed: " + character.id);
                portraits++;
                var encounter = ResearchEncounters.EventForSubject(character.id);
                if (encounter == null || encounter.options.Length != 3) throw new InvalidOperationException("Rare scene is missing: " + character.id);
                foreach (var option in encounter.options)
                {
                    if (option.bonusCredits != 0 || string.IsNullOrEmpty(EventPresentation.RewardSummary(option)))
                        throw new InvalidOperationException("Rare scene reward or description is missing: " + encounter.id);
                    choices++;
                }
            }
            const string missing = "__rare_portrait_missing_verification__";
            if (PixelArt.HasRarePortrait(missing) || !ReferenceEquals(PixelArt.RarePortrait(missing), PixelArt.Portrait(missing)))
                throw new InvalidOperationException("Missing rare sprite must fall back to the ordinary character portrait.");
            Debug.Log("BTS RARE CONTENT PASS / portraits=" + portraits + " / skins=" + skins + " / choices=" + choices + " / logicalHeight=" + PixelArt.PortraitPixelHeight + " / fallback=PASS");
        }

        void VerificationResearchView(string view)
        {
            researchQuery = ""; researchSubject = "nia"; researchListScroll = researchEndingScroll = Vector2.zero; researchReadReport = false; toastUntil = 0;
            researchLongReportVerification = view == "research_report_long" || view == "research_report_long_bottom";
            researchLongReportBottomVerification = view == "research_report_long_bottom"; researchVerifiedReportLayout = null;
            var profile = view == "research_empty" || view == "research_lobby" ? new ResearchProfile()
                : ResearchVerificationProfile(view == "research_insufficient" ? ResearchProgression.ReportCost - 1 : ResearchProgression.ReportCost + 250, view != "research_progress", view == "research_completed" || view == "research_report" || view == "research_encounter");
            if (researchLongReportVerification)
            {
                profile.completedReports.Add("yuki");
                profile.subjects.Add(new ResearchSubjectProgress { subjectId = "yuki", observed = true, uniqueCompleted = true,
                    usedSkills = GameDatabase.Character("yuki").cards.ToList() });
            }
            ResetResearchVerificationProfile(profile);
            lobby = true; research = view != "research_lobby";
            if (view == "research_report") researchReadReport = true;
            if (researchLongReportVerification) { researchSubject = "yuki"; researchReadReport = true; }
            if (researchLongReportBottomVerification)
                summaryScroll["preview/" + ResearchProgression.ForSubject("yuki").unlockText + "/812/46"] = new Vector2(0, 1000);
            if (view == "research_ending_lost")
            {
                ResetResearchVerificationProfile(new ResearchProfile()); PrepareResearchNiaVictory(79201); Engine.State.stage = RunStage.Lost; Engine.State.hp = 0; Save(); research = false;
            }
            if (view == "research_ending_won")
            {
                ResetResearchVerificationProfile(new ResearchProfile()); Engine = NewResearchVerificationRun(79202); lobby = false;
                for (int act = 1; act <= 3; act++) CompleteBossPathsBattle(act, 1);
                Save(); research = false;
            }
            if (view == "research_encounter") PrepareResearchUnlockedEncounter(79203);
            toastUntil = 0;
        }
    }
}
