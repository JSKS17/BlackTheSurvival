using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Lumia
{
    [DefaultExecutionOrder(10000)]
    public sealed class LumiaPresentationVerification : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string version, result = "RUNNING", failure;
            public int screenshots, textMeasurements;
            public bool practiceIsolated, storyResumed, previewReadOnly, historyResumed, personalSavesUnchanged;
            public List<string> views = new List<string>();
        }
        readonly Result result = new Result();
        string output; bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-presentation-verify") >= 0)
                new GameObject("BTS presentation verification").AddComponent<LumiaPresentationVerification>();
        }
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PresentationVerification")); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(result,true));
            var checks = Verify();
            while (!failed)
            {
                object next = null; bool more = false;
                try { more = checks.MoveNext(); if (more) next = checks.Current; }
                catch (Exception ex) { failed = true; result.result="FAIL"; result.failure=ex.ToString(); File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(result,true)); Debug.LogException(ex); }
                if (!more || failed) break;
                yield return next;
            }
            if (failed) Application.Quit(1);
        }
        IEnumerator Verify()
        {
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            var original = HashPersonal();
            yield return null; yield return new WaitForSecondsRealtime(.25f);
            var game = LumiaGame.Instance; game.GuardPresentationVerification(); result.version=Application.version;
            game.PreparePresentationVerification();
            result.textMeasurements=game.VerifyPresentationText();
            game.ShowPresentationVerification("lobby"); yield return Capture("lobby_random_credits");
            game.ShowPresentationVerification("offer"); yield return Capture("tutorial_offer");
            game.ShowPresentationVerification("tutorial"); yield return Capture("tutorial_welcome");
            game.StepPresentationTutorial(); yield return Capture("tutorial_attack");
            game.StepPresentationTutorial(); yield return Capture("tutorial_guard");
            game.StepPresentationTutorial(); yield return Capture("tutorial_end_turn");
            game.StepPresentationTutorial(); yield return Capture("tutorial_enemy_turn");
            game.CompletePresentationEnemyTurn(); yield return Capture("tutorial_first_block");
            game.StepPresentationTutorial(); yield return Capture("tutorial_second_block");
            game.StepPresentationTutorial(); yield return Capture("tutorial_spend_cost");
            game.StepPresentationTutorial(); yield return Capture("tutorial_detonate");
            game.StepPresentationTutorial(); yield return Capture("tutorial_zero_cost");
            game.ShowPresentationVerification("preview"); yield return Capture("preview_zero_cost");
            game.ClosePresentationInformation(); game.StepPresentationTutorial(); yield return Capture("tutorial_field_instruction");
            game.ShowPresentationVerification("field"); yield return Capture("tutorial_field");
            game.ShowPresentationVerification("history"); yield return Capture("combat_history_both_sides");
            game.CompletePresentationTutorial(); result.practiceIsolated=true;
            game.VerifyPresentationPersistence(); result.storyResumed=result.historyResumed=result.previewReadOnly=true;
            game.ShowPresentationVerification("boss_preview"); yield return Capture("preview_boss_movement_restriction");
            game.ShowPresentationVerification("status_preview"); yield return Capture("preview_unavailable_silence");
            game.ShowPresentationVerification("condition_preview"); yield return Capture("preview_conditional_recast");
            foreach(var scene in StoryScenes.All)
            for(int index=0;index<scene.frames.Length;index++)
            { game.ShowStoryVerification(scene.id,index); yield return Capture(scene.id+"_"+(index+1)); }
            game.ShowStoryVerification("menu",0); yield return Capture("story_replay_menu");
            result.textMeasurements+=game.VerifyStoryLayout();
            var after=HashPersonal(); Require(original.All(pair=>after[pair.Key]==pair.Value),"Personal save files changed during presentation verification.");
            result.personalSavesUnchanged=true; result.result="PASS";
            File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(result,true));
            Debug.Log("BTS PRESENTATION PASS / screenshots="+result.screenshots+" / text="+result.textMeasurements+" / practiceIsolation=PASS / storyResume=PASS / previewReadOnly=PASS / historyResume=PASS / personalSaves=PASS");
            Application.Quit(0);
        }
        IEnumerator Capture(string view)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,view+".png"));
            yield return new WaitForSecondsRealtime(.12f);
            result.screenshots++; result.views.Add(view);
        }
        static Dictionary<string,string> HashPersonal()
        {
            var result=new Dictionary<string,string>();
            foreach(string name in new[]{GameIdentity.SaveFileName,GameIdentity.SaveFileName+".bak","bts-research-v1.json","bts-research-v1.json.bak"})
            { string path=Path.Combine(Application.persistentDataPath,name); using(var sha=SHA256.Create())result[name]=File.Exists(path)?BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))):"MISSING"; }
            return result;
        }
        public static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }

    public sealed partial class LumiaGame
    {
        bool PresentationVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(),"-bts-presentation-verify")>=0;
        string presentationOriginalState, presentationDisk, presentationProfile;
        void PresentationCheck(bool condition,string message) { LumiaPresentationVerification.Require(condition,message); }
        public void GuardPresentationVerification()
        {
            PresentationCheck(PresentationVerificationEnabled && Path.GetFileName(savePath)=="lumia-verification.json","Presentation verification requires the isolated save path.");
        }
        public void PreparePresentationVerification()
        {
            GuardPresentationVerification(); Engine=new GameEngine(84126);
            InitializeRunStory(); StoryScenes.Skip(Engine.State); Save(); lobby=true;
            presentationOriginalState=JsonUtility.ToJson(Engine.State); presentationDisk=File.ReadAllText(savePath); presentationProfile=JsonUtility.ToJson(researchStore.Profile);
        }
        public int VerifyPresentationText()
        {
            InitStyles(); int count=0;
            var speech=new GUIStyle(wrapped){fontSize=19,wordWrap=true};
            foreach(string line in TutorialSession.Dialogue)
            { font.RequestCharactersInTexture(line,19,FontStyle.Normal); PresentationCheck(speech.CalcHeight(new GUIContent(line),1069)<=77,"Tutorial speech does not fit: "+line); count++; }
            speech.fontSize=11;
            foreach(string line in new[]{"개발: 블장","문의: azxar2782@gmail.com"})
            {font.RequestCharactersInTexture(line,11,FontStyle.Normal); PresentationCheck(speech.CalcHeight(new GUIContent(line),275)<=16,"Lobby credit does not fit.");count++;}
            speech.fontSize=13;
            foreach(string line in LobbyDialogue.Lines)
            { font.RequestCharactersInTexture(line,13,FontStyle.Normal); PresentationCheck(speech.CalcHeight(new GUIContent(line),388)<=58,"Lobby dialogue does not fit.");count++; }
            return count;
        }
        public void ShowPresentationVerification(string view)
        {
            GuardPresentationVerification();
            if(view=="lobby"){lobby=true;return;}
            if(view=="offer"){tutorialOffer=true;return;}
            if(view=="tutorial"){tutorialOffer=false;StartTutorial();return;}
            if(view=="preview") { combatInformationCard="nia_q";informationEnemy=false;informationHandIndex=Engine.State.combat.hand.IndexOf("nia_q");cachedCombatPreview=null;return; }
            if(view=="field"){tutorial.OpenField();fieldInfo=true;fieldEnemy=false;return;}
            if(view=="history"){fieldInfo=false;combatHistoryOpen=true;historyAllBattles=true;informationFilter=informationSide=0;return;}
            CloseStoryReplay();CloseCombatInformation();fieldInfo=false;lobby=false;
            Engine=new TutorialSession().Engine;var c=Engine.State.combat;
            c.hand=new List<string>{"nia_q","nia_w","basic_attack"};c.energy=5;
            if(view=="condition_preview"){c.hand=new List<string>{"cathy_q"};combatInformationCard="cathy_q";}
            else if(view=="status_preview")
            {c.playerStatuses.effects.Add(new CombatStatus{kind="silence",remaining=1});combatInformationCard="nia_q";}
            else
            {
                // Use the actual boss entry routine from a normal fixture instead of inventing challenge values.
                Engine=NewBossVerificationRun(84128);c=PrepareBossVarietyBattle(BossGoal.NoMovement);
                c.hand=new List<string>{GameDatabase.Cards.First(card=>card.movement&&card.cost<5).id};c.energy=Engine.MaxEnergy;
                combatInformationCard=c.hand[0];
            }
            informationEnemy=false;informationHandIndex=0;cachedCombatPreview=null;
        }
        public void StepPresentationTutorial()
        {
            GuardPresentationVerification();
            if(tutorial.Step==TutorialStep.Welcome)tutorial.Start();
            else if(tutorial.Step==TutorialStep.EndTurn)tutorial.EndTurn();
            else PresentationCheck(tutorial.Play(Engine.State.combat.hand.IndexOf(tutorial.ExpectedCard)),"Tutorial expected card failed: "+tutorial.Step);
            ConsumeCombatActions();effects.Clear();
        }
        public void CompletePresentationEnemyTurn()
        { while(Engine.State.combat.enemyTurn)Engine.AdvanceEnemyAction();ConsumeCombatActions();effects.Clear();tutorial.ObserveTurn(); }
        public void ClosePresentationInformation(){CloseCombatInformation();}
        public void CompletePresentationTutorial()
        {
            CloseCombatInformation();fieldInfo=false;tutorial.FinishField();Save();
            PresentationCheck(File.ReadAllText(savePath)==presentationDisk && JsonUtility.ToJson(researchStore.Profile)==presentationProfile,"Practice wrote to the original save or research profile.");
            FinishTutorial();PresentationCheck(JsonUtility.ToJson(Engine.State)==presentationOriginalState,"Practice did not restore the original run exactly.");
        }
        public void VerifyPresentationPersistence()
        {
            Engine=new GameEngine(84127);InitializeRunStory();StoryScenes.Advance(Engine.State);WriteRunSave();
            bool recovered;Engine=new GameEngine(ReadRunSave(out recovered));RestoreRunStory();
            PresentationCheck(StoryScenes.Pending(Engine.State)?.id==StoryScenes.Prologue && Engine.State.story.frameIndex==1,"Prologue did not resume its saved cut.");
            CloseStoryReplay();StoryScenes.Skip(Engine.State);Engine=new TutorialSession().Engine;
            Engine.PlayCard(0);int entries=Engine.State.combatHistory.Count;string before=JsonUtility.ToJson(Engine.State);int rng=Engine.State.rngState;
            Engine.PreviewCardUse(0);PresentationCheck(before==JsonUtility.ToJson(Engine.State) && Engine.State.rngState==rng,"Preview changed live game state.");
            WriteRunSave();Engine=new GameEngine(ReadRunSave(out recovered));
            PresentationCheck(entries>0 && Engine.State.combatHistory.Count==entries,"Combat history did not survive native JSON save and load.");
        }
    }
}
