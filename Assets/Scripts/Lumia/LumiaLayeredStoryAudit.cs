using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool LayeredStoryVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(),"-bts-layered-story-verify")>=0;
        public bool LayeredStoryStylesReady => initialized;
        public void GuardLayeredStoryVerification()
        {
            if(!LayeredStoryVerificationEnabled || Path.GetFileName(savePath)!="lumia-verification.json")
                throw new InvalidOperationException("Layered story verification requires an isolated development save.");
        }
        public void PrepareLayeredStoryVerification()
        {
            GuardLayeredStoryVerification();Engine=new GameEngine(84226);InitializeRunStory();StoryScenes.Skip(Engine.State);
            researchStore.Profile.normalCleared=researchStore.Profile.hardCleared=true;
            lobby=true;Save();
        }
        public int VerifyLayeredStoryLayout()
        {
            GuardLayeredStoryVerification();
            int text=VerifyStoryLayout();
            foreach(var cut in StoryComposition.All)PrepareStoryComposition(cut.id);
            if(storyCharacterGeometry==null || storyCharacterGeometry.characters.Length!=12)throw new InvalidOperationException("Twelve transparent sprite geometry records are required.");
            foreach(string id in StoryComposition.Characters)
            {
                var geometry=storyCharacterGeometry.characters.Single(item=>item.id==id);
                var source=StoryImage("Characters/"+id);var sprite=StoryCharacter(id);
                if(!source || source.width!=geometry.width || source.height!=geometry.height || geometry.left<0 || geometry.top<0 || geometry.right>geometry.width || geometry.bottom>geometry.height || geometry.left>=geometry.right || geometry.top>=geometry.bottom)
                    throw new InvalidOperationException("Invalid character crop metadata: "+id);
                if(sprite.texture.width!=80 || sprite.texture.height!=80 || sprite.texture.filterMode!=FilterMode.Point || sprite.width<=0 || sprite.height<=0)
                    throw new InvalidOperationException("Character must display on the same coarse logical pixel grid: "+id);
                sprite.texture.Release();
                if(!StoryCharacter(id).texture.IsCreated())throw new InvalidOperationException("Lost render target was not restored: "+id);
            }
            Debug.Log("BTS LAYER COMPOSITION PASS / backgrounds=6 / sprites=12 / cuts=12 / text="+text+" / separateActors=PASS / logicalPixels=80 / preloadedWholeCut=PASS");
            return text;
        }
        public void VerifyLayeredStoryPersistence()
        {
            GuardLayeredStoryVerification();
            Engine=new GameEngine(84227);InitializeRunStory();StoryScenes.Advance(Engine.State);StoryScenes.Advance(Engine.State);WriteRunSave();
            bool recovered;Engine=new GameEngine(ReadRunSave(out recovered));RestoreRunStory();
            if(StoryScenes.Pending(Engine.State)?.id!=StoryScenes.Prologue || Engine.State.story.frameIndex!=2)
                throw new InvalidOperationException("Layered scenes must preserve the saved v0.1.25 cut index.");
            string before=JsonUtility.ToJson(Engine.State),profile=JsonUtility.ToJson(researchStore.Profile),disk=File.ReadAllText(savePath);
            foreach(var scene in StoryScenes.All)for(int i=0;i<scene.frames.Length;i++)ShowStoryVerification(scene.id,i);
            if(before!=JsonUtility.ToJson(Engine.State) || profile!=JsonUtility.ToJson(researchStore.Profile) || disk!=File.ReadAllText(savePath))
                throw new InvalidOperationException("Viewing layered replays changed run, research or disk state.");
            CloseStoryReplay();lobby=true;
        }
    }
}
