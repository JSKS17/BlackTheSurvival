"""Rebuild the 55 newly playable launch characters from official 1.0 source names.

The dictionaries below contain fan-game balance only. Skill names, slots and
cooldown snapshots are extracted from the cached official Korean article.
"""
import json, re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
official = json.loads((root/'docs/research/classic_1280.json').read_text(encoding='utf-8'))
database = (root/'Assets/Scripts/Lumia/GameDatabase.cs').read_text(encoding='utf-8-sig')
ids = {name: cid for cid,name in re.findall(r'Encounter\("([^"]+)","([^"]+)"', database)}
core = {'nadine','sua','aya','isol','emma','yuki','jackie','hyunwoo','hyejin'}

# d=damage, b=block, h=heal, t=draw, g=energy, p=poison, v=vulnerable,
# w=weak, s=strength, e=evasion, n=hits, z=exhaust, f=free skill slots.
balance = {
 'nathapon':['d=8,v=1','d=3,n=3,w=1','d=8,v=2','w=3,t=2,b=12,z=true'],
 'nicky':['d=10,f=q','d=6,b=12','d=14,w=1','d=30,v=2,z=true'],
 'daniel':['d=10,t=1','v=3,p=3','d=8,e=25','d=6,n=5,h=4,z=true'],
 'tia':['d=4,w=1','b=4,p=1','d=12,e=25','d=5,n=5,w=2,h=5,z=true'],
 'laura':['d=4','d=11,v=2','d=3,e=15','d=25,e=25,t=1,z=true'],
 'lenox':['d=9,b=2,f=q','d=12,w=2','d=10,v=1','d=5,n=4,p=5,z=true'],
 'leon':['d=10,w=1','b=10,s=1','e=30,h=5','d=26,w=2,z=true'],
 'rozzi':['d=9,e=10,f=q','d=8,b=5,f=w','d=6,n=2,e=25','d=16,p=3,f=r'],
 'luke':['d=11,v=1','s=2,d=5','d=9,e=20','d=29,w=2,f=q,z=true'],
 'dailin':['d=4,n=3','b=12,h=4,e=15','d=9,w=2','d=6,n=5,w=2,z=true'],
 'rio':['s=1,z=true','d=10,v=1,f=qwe','d=10,e=30','d=5,n=4,f=r'],
 'martina':['d=9,v=1,f=q','w=2,p=2','d=10,e=20,t=1','d=18,s=2,t=1,z=true'],
 'mai':['d=8,b=4','d=10,b=16','d=10,b=8,e=25','b=24,h=18,z=true'],
 'markus':['d=11,b=3','d=13,v=2','d=12,w=1','d=20,v=2,f=r'],
 'magnus':['d=12,w=2','d=3,n=4,f=w','d=13,w=1','d=30,v=2,z=true'],
 'vanya':['d=10,b=5,f=q','b=12,e=15','d=8,e=30','d=24,w=3,b=10,z=true'],
 'barbara':['d=8,p=3,f=q','d=5,n=2,f=r','d=12,b=8','s=3,t=2,f=qwe,z=true'],
 'bernice':['d=11,w=1','p=5,w=1','e=20,v=1','d=7,n=4,w=2,z=true'],
 'bianca':['d=11,h=4,w=1','h=12,b=8,f=qer','d=12,h=5,e=15','d=6,n=4,h=12,z=true'],
 'celine':['d=10,p=2','d=3,f=q','d=10,e=20,w=1','d=5,n=2,v=1'],
 'sho':['d=10,w=1','b=15,h=8','d=11,w=1,f=e','d=5,n=5,p=4,z=true'],
 'shoichi':['d=10,f=q','d=12,e=15,f=w','d=12,v=2,f=e','d=5,n=6,t=1,z=true'],
 'sissela':['d=10,p=2','b=18,e=20','d=11,b=6,f=e','d=32,h=5,z=true'],
 'silvia':['d=9,h=4','d=12,w=2','d=11,e=25','d=11,e=30'],
 'adela':['d=7','d=6,n=2,w=1','d=12,e=20','d=30,b=10,z=true'],
 'adriana':['d=3,n=3,p=2','p=5,v=2','d=12,e=20','d=7,n=3,p=4,z=true'],
 'adina':['d=9,w=1','d=11,b=6','d=5,n=2,h=4','t=1,b=4,z=true'],
 'isaac':['d=10,v=1','s=2,b=5,f=qwe','d=12,e=20','d=28,h=8,v=2,z=true'],
 'alex':['d=10,s=1','d=12,v=2','d=10,e=30','d=3,n=10,w=2,z=true'],
 'jan':['d=11,f=e','d=13,w=2','b=7,e=35,f=e','d=25,w=2,v=2,z=true'],
 'estelle':['d=9,w=1','d=12,b=8','b=22,w=2','h=24,b=22,z=true'],
 'aiden':['d=9','d=12,b=5','d=10,e=20','d=26,w=2,t=1,z=true'],
 'echion':['d=5,n=2,w=1','b=14','d=9,e=20,f=e','d=14,p=2,f=e'],
 'elena':['d=5,n=2,w=1','d=11,b=8,e=15,f=qw','e=25,b=4','d=28,w=3,z=true'],
 'johann':['d=9,h=6','h=12,b=12,f=w','b=8,e=25','h=24,b=20,s=1,z=true'],
 'william':['s=2,b=4','d=5,n=2,v=1','e=20,d=3','d=27,t=1,z=true'],
 'irem':['d=10','d=10,w=2','e=30,b=7','s=1,b=4,z=true'],
 'eva':['d=11,f=q','d=12,v=2','e=35,b=8','d=4,n=4,p=2,z=true'],
 'ian':['d=9,f=q','d=10,w=2','d=8,e=20,f=e','d=27,h=6,s=2,z=true'],
 'eleven':['d=10','b=15,w=2','d=12,e=20','d=25,h=10,b=10,z=true'],
 'zahir':['d=11','d=3,f=qe','d=12,w=2,e=10','d=6,n=5,v=2,z=true'],
 'jenny':['d=10,w=1,f=q','d=12,v=2','e=35,t=1','d=28,w=3,f=e,z=true'],
 'camilo':['d=10,h=3','d=13,w=1','d=3,e=15','d=5,n=5,h=10,e=15,z=true'],
 'karla':['d=4','d=3,v=1,f=e','d=12,e=30','d=28,w=3,z=true'],
 'cathy':['d=10,p=3,f=q','d=12,p=3','d=11,w=2,e=15','d=30,h=12,p=5,z=true'],
 'chloe':['d=9','d=6,n=2,b=4','d=12,e=20,f=q','b=20,d=24,s=2,f=we,z=true'],
 'chiara':['d=10,v=1','d=12,b=12','d=12,w=2','d=26,h=10,p=4,z=true'],
 'tazia':['d=4','d=13,v=2','d=12,e=20','d=30,w=2,z=true'],
 'theodore':['d=10,h=5','s=2,b=10','d=12,w=2','d=30,h=18,s=1,z=true'],
 'felix':['d=11','d=12,v=1','d=11,h=4,f=qwe','d=28,b=10,z=true'],
 'priya':['d=4,h=2','b=12,e=25','d=12,h=6,w=1','d=28,h=15,w=2,z=true'],
 'fiora':['d=11,f=q','d=5,n=2','d=12,e=25,f=qw','d=30,w=2'],
 'piolo':['d=5,n=2','d=9,b=14','d=13,w=2','d=11'],
 'hart':['d=10','s=2,e=15','d=11,e=25','w=3,h=18,b=16,z=true'],
 'haze':['d=5','d=4,n=3,f=w','d=3,n=4,e=15','d=5,n=5,z=true'],
}

# Passive mechanics retain the source's identity while using supported deck-game
# triggers. Movement passives use the battle_start_evasion trigger.
passives = {
 'nathapon':('attack_bonus',3), 'nicky':('battle_start_strength',2),
 'daniel':('battle_start_evasion',12), 'tia':('skill_bonus',3),
 'laura':('skill_bonus',2), 'lenox':('battle_start_block',12),
 'leon':('battle_start_evasion',10), 'rozzi':('attack_bonus',3),
 'luke':('kill_heal',9), 'dailin':('attack_bonus',3),
 'rio':('skill_bonus',3), 'martina':('battle_start_strength',2),
 'mai':('attack_bonus',3), 'markus':('skill_bonus',3),
 'magnus':('turn_block',4), 'vanya':('turn_block',4),
 'barbara':('skill_bonus',3), 'bernice':('attack_bonus',4),
 'bianca':('turn_heal',3), 'celine':('attack_bonus',3),
 'sho':('turn_heal',3), 'shoichi':('skill_bonus',3),
 'sissela':('turn_heal',3), 'silvia':('battle_start_strength',2),
 'adela':('skill_bonus',3), 'adriana':('skill_bonus',3),
 'adina':('battle_start_evasion',10), 'isaac':('turn_heal',3),
 'alex':('battle_start_evasion',12), 'jan':('skill_bonus',3),
 'estelle':('turn_heal',3), 'aiden':('attack_bonus',3),
 'echion':('skill_bonus',3), 'elena':('skill_bonus',3),
 'johann':('turn_block',4), 'william':('attack_bonus',3),
 'irem':('turn_block',3), 'eva':('skill_bonus',3),
 'ian':('turn_heal',3), 'eleven':('turn_heal',4),
 'zahir':('battle_start_evasion',12), 'jenny':('battle_start_block',15),
 'camilo':('turn_block',4), 'karla':('skill_bonus',3),
 'cathy':('skill_bonus',3), 'chloe':('attack_bonus',3),
 'chiara':('skill_bonus',3), 'tazia':('skill_bonus',3),
 'theodore':('battle_start_block',14), 'felix':('attack_bonus',3),
 'priya':('turn_block',4), 'fiora':('skill_bonus',3),
 'piolo':('attack_bonus',3), 'hart':('attack_bonus',3),
 'haze':('attack_bonus',3),
}

notes = {
 'tia_w':'팔레트의 색 변경 자체 쿨다운 0.02초를 사용합니다. 색별 준비 시간은 통합했습니다.',
 'mai_w':'충전형 스킬의 1레벨 충전 시간 28초를 사용합니다.',
 'bernice_w':'충전형 스킬의 1레벨 충전 시간 18초를 사용합니다.',
 'celine_q':'충전형 스킬의 1레벨 충전 시간 7초를 사용합니다.',
 'celine_w':'공식 자료에 독립 쿨다운이 미표기된 기폭 명령입니다. 카드 쿨다운 기준을 0초로 둡니다.',
 'celine_r':'1단계 융합의 기준 1.25초입니다. 단계별 강화는 연타 피해로 변환했습니다.',
 'adina_r':'공식 자료에 독립 쿨다운이 미표기된 천체 교환입니다. 0초 기준으로 설정하고 소멸 카드로 제한합니다.',
 'alex_q':'원거리 형태 코일건을 대표 Q로 사용합니다.',
 'alex_w':'원거리 형태 타겟 마커를 대표 W로 사용합니다.',
 'alex_e':'원거리 형태 펄스 스팅을 대표 E로 사용합니다.',
 'echion_r':'VF폭주를 함께 표현하는 독사의 진노 - 바이퍼의 3.5초를 사용합니다. VF 게이지 조건은 턴 효과로 변환했습니다.',
 'irem_r':'이렘 형태 고양이로 펑!의 1.5초를 사용합니다. 실제 형태 전환은 힘과 방어도 부여로 변환했습니다.',
 'ian_q':'이안의 일반 형태 피하세요!를 대표 Q로 사용합니다.',
 'ian_w':'이안의 일반 형태 미안해요..를 대표 W로 사용합니다.',
 'ian_e':'이안의 일반 형태 비키세요!를 대표 E로 사용합니다.',
 'william_e':'원본의 쿨다운이 0초인 회수 이동입니다.',
 'eva_r':'원본 쿨다운이 0초이며 VF 자원을 사용하는 채널링 스킬입니다. 자원 조건을 전투당 소멸로 변환했습니다.',
 'karla_q':'공격 속도에 반비례하는 0.75초 기준을 사용합니다.',
 'felix_q':'연계 창술의 1레벨 공유 쿨다운 10초를 사용합니다.',
 'felix_w':'연계 창술의 1레벨 공유 쿨다운 10초를 사용합니다.',
 'felix_e':'연계 창술의 1레벨 공유 쿨다운 10초를 사용합니다.',
}

fields={'d':'damage','b':'block','h':'heal','t':'draw','g':'energy','p':'poison','v':'vulnerable','w':'weak','s':'strength','e':'evasion','n':'hits','z':'exhaust','f':'free'}
stat_names={
 'attack_bonus':lambda n:f'기본 공격 카드의 피해량이 {n} 증가합니다.',
 'skill_bonus':lambda n:f'스킬 카드의 피해량이 {n} 증가합니다.',
 'turn_block':lambda n:f'매 턴 시작 시 방어도를 {n} 얻습니다.',
 'turn_heal':lambda n:f'매 턴 시작 시 체력을 {n} 회복합니다.',
 'battle_start_block':lambda n:f'전투 시작 시 방어도를 {n} 얻습니다.',
 'battle_start_strength':lambda n:f'전투 시작 시 힘이 {n} 증가합니다.',
 'battle_start_evasion':lambda n:f'전투 시작 시 회피율이 {n}% 증가합니다.',
 'kill_heal':lambda n:f'전투에서 승리하면 체력을 {n} 회복합니다.',
}

def source_skill(char,key):
    options=[s for s in char['skills'] if s['key']==key]
    assert options,(char['name'],key)
    if char['name']=='에키온' and key=='R': return options[1]
    return options[0]

def cooldown(skill,cid,key):
    if cid=='felix' and key in 'QWE':return 10
    if cid=='mai' and key=='W':return 28
    if cid=='bernice' and key=='W':return 18
    if cid=='celine' and key=='Q':return 7
    for value in skill['cooldown']:
        # Exact cooldown lines begin with a number. Ignore reduction prose.
        m=re.match(r'(?:감소:\s*)?(\d+(?:\.\d+)?)',value)
        if m and not value[m.end():].lstrip().startswith('%'):
            return float(m.group(1))
    if (cid,key) in [('celine','W'),('adina','R')]:return 0
    raise ValueError((cid,key,skill))

def label(skill,cid,key):
    if cid=='nicky' and key=='E':return '강력한 펀치 / 분노의 펀치!'
    if cid=='echion' and key=='R':return 'VF폭주 / 독사의 진노'
    if cid=='eva' and key=='W':return '위상의 소용돌이'
    if cid=='eva' and key=='R':return 'VF 방출'
    name=skill['name'].split('(')[0].strip()
    return name.removeprefix('사용 효과: ').strip()

out=['using System;','using System.Collections.Generic;','','namespace Lumia','{',
 '    // Official names and cooldown snapshots: docs/ROSTER_CLASSIC.md.',
 '    // Every damage/status number below is authored for this turn-based fan game.',
 '    public static class ClassicRoster','    {',
 '        public static void Populate(List<CardDef> cards, List<PassiveDef> passives, List<CharacterDef> characters)',
 '        {']
table=[]
added=[]
for char in official:
    cid=ids[char['name']]
    if cid in core:continue
    assert cid in balance,(cid,char['name'])
    added.append(cid)
    out += [f'            // {char["name"]}',f'            Subject(cards, passives, characters, "{cid}", "{char["name"]}",',]
    passive=source_skill(char,'P')
    pname=label(passive,cid,'P')
    trigger,amount=passives[cid]
    out += [f'                "{pname}", "{trigger}", {amount}, "{stat_names[trigger](amount)}",']
    row=[char['name'],cid,pname]
    for j,key in enumerate('QWER'):
        skill=source_skill(char,key)
        cd=cooldown(skill,cid,key)
        cname=label(skill,cid,key)
        cdtext=f'{cd:g}'
        args=[]
        for piece in balance[cid][j].split(','):
            k,v=piece.split('=')
            args.append(fields[k]+':'+('"'+v+'"' if k=='f' else v))
        out += [f'                S("{cname}", "{key}", {cdtext}f, '+', '.join(args)+('));' if j==3 else '),')]
        row.append(cname+' ('+cdtext+'초)')
    table.append('| '+' | '.join(row)+' |')
    out+=['']
assert len(added)==55,len(added)
out += ['        }','',
 '        static CardDef S(string name, string key, float cooldown, int damage=0, int block=0, int heal=0, int draw=0, int energy=0, int poison=0, int vulnerable=0, int weak=0, int strength=0, int evasion=0, int duration=2, int hits=1, bool exhaust=false, string free=null)',
 '        {',
 '            return new CardDef { name=name, key=key, cooldown=cooldown, cost=1, damage=damage, block=block, heal=heal, draw=draw, energy=energy, poison=poison, vulnerable=vulnerable, weak=weak, strength=strength, evasion=evasion, duration=duration, hits=hits, exhaust=exhaust, category="skill", description="", freeCastTargets=string.IsNullOrEmpty(free) ? Array.Empty<string>() : Slots(free), freeCastCount=string.IsNullOrEmpty(free) ? 0 : 1, freeCastOnHit=!string.IsNullOrEmpty(free) && damage>0 };',
 '        }','',
 '        static string[] Slots(string slots)',
 '        {',
 '            var result = new string[slots.Length];',
 '            for (int i=0; i<slots.Length; i++) result[i]=slots[i].ToString();',
 '            return result;',
 '        }','',
 '        static void Subject(List<CardDef> cards, List<PassiveDef> passives, List<CharacterDef> characters, string id, string owner, string passiveName, string trigger, int amount, string description, params CardDef[] skills)',
 '        {',
 '            var subject = characters.Find(x => x.id==id);',
 '            if (subject==null) { subject=new CharacterDef { id=id, name=owner }; characters.Add(subject); }',
 '            if (subject.cards!=null && subject.cards.Length>0) return;',
 '            subject.passiveId=id+"_p";',
 '            subject.cards=new string[skills.Length];',
 '            for (int i=0; i<skills.Length; i++)',
 '            {',
 '                var card=skills[i]; card.id=id+"_"+card.key.ToLowerInvariant(); card.owner=owner;',
 '                for (int t=0; t<card.freeCastTargets.Length; t++) card.freeCastTargets[t]=id+"_"+card.freeCastTargets[t];',
 '                subject.cards[i]=card.id; cards.Add(card);',
 '            }',
 '            passives.Add(new PassiveDef { id=subject.passiveId, name=passiveName, owner=owner, trigger=trigger, amount=amount, description=description });',
 '        }','    }','}']
(root/'Assets/Scripts/Lumia/ClassicRoster.cs').write_text('\n'.join(out)+'\n',encoding='utf-8')

doc='''# 초기 실험체 전투 카드 확장

[님블뉴런 공식 1.0.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR)를 기준으로, 조우 이벤트에만 있던 초기 실험체 55명을 전투 데이터에 연결했습니다. 기존 카드가 있던 9명과 합치면 출시 당시 64명 전원이 포함됩니다. 니아 및 출시 이후 실험체는 별도 자료에서 확장합니다.

기준은 현행 실시간 게임의 최신 밸런스가 아닌 공식 1.0 쿨다운 스냅샷입니다. 이름과 Q/W/E/R 슬롯, 패시브 명칭은 공식 자료를 사용하며 피해, 회복, 방어도, 상태, 에너지 및 수집 보상 값은 이 팬 게임의 독자적인 턴제 밸런스입니다. 다양한 형태를 가진 실험체는 대표 형태의 네 슬롯에 부가 효과를 통합했습니다.

## 데이터 검증 기준

- 실험체마다 자신의 Q/W/E/R 카드 4장과 자신의 패시브 1개가 존재합니다.
- 같은 이름의 이벤트 실험체를 기존 ID로 연결하며 별도 실험체를 중복 생성하지 않습니다.
- 1레벨 쿨다운을 사용하며 충전형은 1레벨 충전 시간을 사용합니다.
- 독립 쿨다운이 없는 자원/명령형 스킬은 공식 자료에 없는 시간을 만들어 넣지 않고 0초 기준 및 별도 팬 게임 제한을 사용합니다.
- 이동 및 은신의 일부 효과는 회피율로, CC는 약화/취약으로, 지속 피해는 출혈·화상·독의 공통 수치로 바꿉니다.
- 쿨다운 감소를 가진 카드의 추가 사용은 아래 원본 대상에 연결한 턴제 변환입니다. 표식·진화·범위·VF 게이지 등 원본의 실시간 조건은 카드의 부가 효과로 통합하고 무료 사용은 턴 내 제한을 받습니다. 기본 공격을 통한 감소는 별도 공격 시점 조건이므로 무관한 스킬의 쿨다운 감소로 추가하지 않습니다.

## 공식 이름 및 쿨다운 기준

| 실험체 | ID | 패시브 | Q | W | E | R |
|---|---|---|---|---|---|---|
'''+ '\n'.join(table)+'''

## 특수 쿨다운/형태 선택

'''+ '\n'.join('- `'+key+'`: '+value for key,value in notes.items())+'''

## 쿨다운 감소 카드의 대상

'''
for cid,slots in balance.items():
    for i,slot in enumerate(slots):
        match=re.search(r'(?:^|,)f=([qwer]+)',slot)
        if match:
            doc += '- `'+cid+'_'+'qwer'[i]+'` → '+', '.join('`'+cid+'_'+v+'`' for v in match.group(1))+'.\n'
doc += '\n추출 원본은 `docs/research/classic_1280.html`, 구조화 결과는 `docs/research/classic_1280.json`에 보관합니다. 스킬 문장 전체를 게임 UI에 복제하지 않고 중앙 설명 생성기가 실제 카드 효과를 문장으로 설명합니다.\n'
(root/'docs/ROSTER_CLASSIC.md').write_text(doc,encoding='utf-8')
print(f'Added {len(added)} subjects, {len(added)*4} skills, {len(added)} passives.')
