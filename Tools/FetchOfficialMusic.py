"""Acquire the three public vocal OSTs; preserve other acquired recordings and scores.

The public event player files contain vocals. MR download buttons on those pages
require a platform login; this tool neither guesses MR URLs nor bypasses that
login. GenerateInstrumentalMusic.py provides the separate post-lobby scores.
"""
import hashlib
import json
from pathlib import Path
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'Assets/Resources/Lumia/Audio/Music'
SOURCES = [
    ('music_red_heart', 'Red Heart', 'Haesoo',
     'https://event.playeternalreturn.com/S10/OST/RedHeart?hl=ko-KR',
     'https://cdn.playeternalreturn.com/event/season10/redheart/UwBxA/ERS10_Red_Heart.mp3'),
    ('music_golden_willow', 'Golden Willow', 'Byeol Eun',
     'https://event.playeternalreturn.com/S9/GoldenWillow?hl=ko-KR',
     'https://cdn.playeternalreturn.com/event/season9/willow/ERS9_Golden_Williow_KR.mp3'),
    ('music_summertime', 'Summertime', 'Eternal Return OST',
     'https://event.playeternalreturn.com/S8/SummertimeMelody?hl=ko-KR',
     'https://cdn.playeternalreturn.com/event/season8/melody/summertime.mp3'),
]

def meta(path, music=False):
    path = Path(str(path)+'.meta')
    if path.exists():
        return
    path.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'''\nAudioImporter:
  externalObjects: {}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 2
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.75
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {}
  forceToMono: 0
  normalize: 0
  ambisonic: 0
  loadInBackground: 1
  userData:
  assetBundleName:
  assetBundleVariant:
''', encoding='utf-8')

def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    records = []
    for identity, title, performer, page, url in SOURCES:
        target = OUTPUT / (identity+'.mp3')
        if not target.exists():
            request = urllib.request.Request(url, headers={'User-Agent': 'BlackTheSurvival-FanGame-Audio'})
            with urllib.request.urlopen(request, timeout=90) as response:
                data = response.read()
            assert len(data)>100000 and (data.startswith(b'ID3') or data[0]==255), 'Not an MP3: '+url
            target.write_bytes(data)
        meta(target, True)
        data = target.read_bytes()
        records.append(dict(id=identity,title=title,performer=performer,
                            path='Lumia/Audio/Music/'+identity,
                            file=target.relative_to(ROOT).as_posix(),
                            sourcePage=page,sourceUrl=url,sourceName=url.rsplit('/',1)[1],
                            origin='official_original_music', kind='original_music', containsVocals=True,
                            alteration='None; official MP3 bytes preserved. Unity imports streaming Vorbis.',
                            bytes=len(data),sha256=hashlib.sha256(data).hexdigest()))
        print(identity, len(data), 'bytes')
    manifest = ROOT/'docs/audio/music-sources.json'
    manifest.parent.mkdir(parents=True,exist_ok=True)
    existing = json.loads(manifest.read_text(encoding='utf-8')) if manifest.exists() else []
    refreshed_ids = {entry['id'] for entry in records}
    preserved = [entry for entry in existing if entry['id'] not in refreshed_ids]
    manifest.write_text(json.dumps(records + preserved,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

if __name__ == '__main__':
    main()
