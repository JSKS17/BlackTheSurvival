using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        CraftingCatalog craftingCatalog;
        readonly Dictionary<string, CraftDescriptionLayout> craftingLayouts = new Dictionary<string, CraftDescriptionLayout>();
        int craftingFontRevision;

        sealed class CraftDescriptionLayout
        {
            public GUIContent content;
            public string scrollKey;
            public float width, height, plainHeight, scrollWidth, scrollHeight;
            public int revision = -1, size;
        }

        CraftingCatalog CraftCatalog => craftingCatalog ?? (craftingCatalog = new CraftingCatalog(GameDatabase.Gear));

        void DrawCraftDescription(Rect rect, CraftingItem item, int size, Color color)
        {
            CraftDescriptionLayout layout;
            if (!craftingLayouts.TryGetValue(item.gear.id, out layout))
            {
                layout = new CraftDescriptionLayout { content = new GUIContent(item.description), scrollKey = "craft/" + item.gear.id };
                craftingLayouts.Add(item.gear.id, layout);
            }
            wrapped.fontSize = size;
            wrapped.normal.textColor = color;
            if (layout.revision != craftingFontRevision || layout.width != rect.width || layout.height != rect.height || layout.size != size)
            {
                layout.width = rect.width; layout.height = rect.height; layout.size = size;
                layout.plainHeight = wrapped.CalcHeight(layout.content, rect.width);
                layout.scrollWidth = Math.Max(30, rect.width - 15);
                layout.scrollHeight = layout.plainHeight > rect.height ? wrapped.CalcHeight(layout.content, layout.scrollWidth) + 5 : 0;
                layout.revision = craftingFontRevision;
            }
            if (layout.plainHeight <= rect.height) { GUI.Label(rect, layout.content, wrapped); return; }
            Vector2 position;
            summaryScroll.TryGetValue(layout.scrollKey, out position);
            position = GUI.BeginScrollView(rect, position, new Rect(0, 0, layout.scrollWidth, layout.scrollHeight), false, true);
            GUI.Label(new Rect(0, 0, layout.scrollWidth, layout.scrollHeight), layout.content, wrapped);
            GUI.EndScrollView();
            summaryScroll[layout.scrollKey] = position;
        }
    }
}
