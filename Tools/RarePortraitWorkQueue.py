"""Print exact per-asset prompts/reference paths for built-in image generation."""
import argparse
import json
from pathlib import Path
from PrepareRarePortraits import ROOT, entries, reference_inputs, prepare

parser = argparse.ArgumentParser()
parser.add_argument('ids', nargs='+')
parser.add_argument('--prepare', action='store_true')
args = parser.parse_args()
if args.prepare:
    import contextlib
    import io
    with contextlib.redirect_stdout(io.StringIO()):
        prepare(set(args.ids))
selected, _ = entries()
result = []
priority = ('\nSelected-skin priority: the selected skin image and its written appearance description '
            'override the default face note for hair style/color, eye color and accessories. '
            'Preserve distinctive expression and eye shape, but never add default glasses, hats '
            'or costume details absent from the selected skin.\n')
for asset_id in args.ids:
    prompt_file = ROOT / 'docs/art-rare-skins/prompts' / (asset_id + '.txt')
    prompt = prompt_file.read_text(encoding='utf8')
    if not (ROOT / 'docs/art-rare-skins/generation' / (asset_id + '.json')).exists() and priority.strip() not in prompt:
        prompt += priority
        prompt_file.write_text(prompt, encoding='utf8')
    result.append(dict(id=asset_id, prompt=prompt,
                       refs=[str(ROOT / ref['path']) for ref in reference_inputs(selected[asset_id])]))
print(json.dumps(result, ensure_ascii=False))
