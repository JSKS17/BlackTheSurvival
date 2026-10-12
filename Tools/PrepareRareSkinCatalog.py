"""Write the pure runtime skin-name catalog from the checked-in release manifest."""
import json
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[1]
data = json.loads((ROOT / 'docs/art-rare-skins/skin-manifest.json').read_text(encoding='utf-8-sig'))
skins = data['skins']
assert len(skins) == 91 and len({s['id'] for s in skins}) == 91
quote = lambda text: json.dumps(text, ensure_ascii=False)
lines = ['using System.Collections.Generic;', '', 'namespace Lumia', '{',
         '    // Display metadata only. Original fan scenes are not official skin lore.',
         '    public static class RareSkinCatalog', '    {',
         '        static readonly Dictionary<string, string> Names = new Dictionary<string, string>', '        {']
for skin in skins:
    lines.append('            { ' + quote(skin['id']) + ', ' + quote(skin['skinNameKR']) + ' },')
lines += ['        };', '',
          '        public static IEnumerable<string> SubjectIds => Names.Keys;',
          '        public static int Count => Names.Count;',
          '        public static bool HasSubject(string id) => id != null && Names.ContainsKey(id);',
          '        public static string NameForSubject(string id) => HasSubject(id) ? Names[id] : "";',
          '        public static bool IsCustomOutfit(string id) => id == "lucia" || id == "ceres" || id == "craver";',
          '        public static string DesignNote(string id) => IsCustomOutfit(id) ? "팬 창작 사복" : "스킨에서 착안한 팬 창작 조우";',
          '    }', '}', '']
target = ROOT / 'Assets/Scripts/Lumia/RareSkinCatalog.cs'
target.write_text('\n'.join(lines), encoding='utf8')
target.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid5(uuid.NAMESPACE_URL, 'BlackTheSurvival/RareSkinCatalog.cs').hex + '\n', encoding='utf8')
print('Runtime rare skin names:', len(skins))
