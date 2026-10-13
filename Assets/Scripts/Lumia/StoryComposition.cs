using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed class StoryActorLayer
    {
        public readonly string sprite;
        public readonly float centerX, baseline, opacity;
        public readonly int pixelScale;
        public readonly bool flip;
        public StoryActorLayer(string sprite, float centerX, float baseline, int pixelScale = 4, bool flip = false, float opacity = 1)
        { this.sprite = sprite; this.centerX = centerX; this.baseline = baseline; this.pixelScale = pixelScale; this.flip = flip; this.opacity = opacity; }
    }
    public sealed class StoryCompositionDef
    {
        public readonly string id, background, fieldEffect;
        public readonly StoryActorLayer[] actors;
        public StoryCompositionDef(string id, string background, string fieldEffect, params StoryActorLayer[] actors)
        { this.id = id; this.background = background; this.fieldEffect = fieldEffect; this.actors = actors; }
    }
    /// <summary>Cut art, actors and presentation are independent of story text and saved cut indices.</summary>
    public static class StoryComposition
    {
        public const int LogicalSpriteSize = 80;
        static StoryActorLayer A(string sprite, float x, float y = .96f, int scale = 4, bool flip = false, float opacity = 1) => new StoryActorLayer(sprite, x, y, scale, flip, opacity);
        public static readonly StoryCompositionDef[] All =
        {
            new StoryCompositionDef("prologue_1", "lab_night", "none", A("nia_frustrated", .43f)),
            new StoryCompositionDef("prologue_2", "vf_island", "vf", A("nia_overload", .43f, .96f, 5)),
            new StoryCompositionDef("prologue_3", "lab_night", "glitch", A("hana_work", .40f)),
            new StoryCompositionDef("prologue_4", "vf_island", "portal", A("hana_determined", .33f)),
            new StoryCompositionDef("normal_1", "island_restored", "none", A("nia_defeated", .63f, .96f, 3), A("hana_relieved", .33f)),
            new StoryCompositionDef("normal_2", "vf_arena", "portal", A("hana_startled", .22f), A("nia_taunt", .52f, .78f)),
            new StoryCompositionDef("normal_3", "vf_arena", "vf", A("nia_cocky", .30f, .80f, 3), A("nia_cocky", .48f, .86f, 4), A("nia_cocky", .66f, .79f, 3, true), A("hana_startled", .15f)),
            new StoryCompositionDef("normal_4", "vf_island", "portal", A("hana_determined", .33f)),
            new StoryCompositionDef("hard_1", "island_restored", "dream", A("hana_relieved", .38f)),
            new StoryCompositionDef("hard_2", "bedroom_night", "none", A("nia_wake", .34f, .94f)),
            new StoryCompositionDef("hard_3", "vf_island", "vf", A("nia_cast", .42f, .95f, 5)),
            new StoryCompositionDef("hard_4", "endless_portal", "portal", A("nia_challenge", .35f, .92f, 4))
        };
        public static StoryCompositionDef For(string cutId) => All.FirstOrDefault(frame => frame.id == cutId);
        public static string[] Backgrounds => All.Select(frame => frame.background).Distinct().ToArray();
        public static string[] Characters => All.SelectMany(frame => frame.actors).Select(actor => actor.sprite).Distinct().ToArray();
    }
    [Serializable] public sealed class StoryCharacterGeometry
    {
        public string id;
        public int width, height, left, top, right, bottom;
    }
    [Serializable] public sealed class StoryCharacterGeometryCatalog
    {
        public StoryCharacterGeometry[] characters = new StoryCharacterGeometry[0];
    }
}
