#if LUMIA_TEST_RUNNER
using System;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void LayeredStoryCompositionsAndProgress()
    {
        Check(StoryComposition.All.Length==12 && StoryComposition.Backgrounds.Length==6 && StoryComposition.Characters.Length==12,
            "twelve cuts use six actor-free backgrounds and twelve independently authored expression/pose variants");
        foreach(var scene in StoryScenes.All)
        foreach(var frame in scene.frames)
        {
            var layers=StoryComposition.For(frame.image);
            Check(layers!=null && layers.id==frame.image && !string.IsNullOrEmpty(layers.background) && layers.actors.Length>0,
                "every existing saved cut key resolves to its complete independent composition");
            Check(layers.actors.All(actor=>actor.sprite.StartsWith("nia_",StringComparison.Ordinal)||actor.sprite.StartsWith("hana_",StringComparison.Ordinal)),
                "cutscene actors select dedicated variants rather than unchanged gameplay portraits");
            Check(layers.actors.All(actor=>actor.centerX>0 && actor.centerX<1 && actor.baseline>.5f && actor.baseline<=1 && actor.pixelScale>=3 && actor.pixelScale<=5 && actor.opacity>0 && actor.opacity<=1),
                "actor anchors and integral display scale stay in the cutscene canvas");
        }
        Check(StoryComposition.For("normal_3").actors.Count(actor=>actor.sprite=="nia_cocky")==3,
            "the weakest-Nia joke is composed from three independent sprite layers");
        Check(StoryComposition.For("hard_2").background=="bedroom_night" && StoryComposition.For("hard_2").actors.Single().sprite=="nia_wake",
            "the dream reveal has a separate wake expression in the bedroom");
        var engine=new GameEngine(84226);engine.State.story=StoryScenes.NewRun(engine.State);
        StoryScenes.Advance(engine.State);StoryScenes.Advance(engine.State);
        string before=StoryGameplaySnapshot(engine.State);int rng=engine.State.rngState;
        var restored=new GameEngine((RunState)Clone(engine.State));
        var sceneAfter=StoryScenes.Pending(restored.State);
        Check(sceneAfter.id=="prologue" && restored.State.story.frameIndex==2 && StoryComposition.For(sceneAfter.frames[2].image).actors[0].sprite=="hana_work",
            "a v0.1.25 midway cut save resumes the same line using its new layered scene");
        foreach(var cut in StoryComposition.All)StoryComposition.For(cut.id);
        Check(StoryGameplaySnapshot(engine.State)==before && engine.State.rngState==rng,
            "scene composition lookup does not consume gameplay randomness or change progression");
    }
}
#endif
