"""Generate the reviewed, source-keyed status table from the saved 12.5.0 reference tooltips."""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parent.parent
references=json.loads((root/'docs/art-chibi/reference-skills.json').read_text(encoding='utf-8-sig'))
records={x['id']:x for x in references if x['slot']!='T'}
patterns={
 'slow':r'\bslow(?:ed|ing|s)?\b|reduces? (?:the )?movement speed',
 'root':r'\broot(?:ed|ing|s)?\b', 'stun':r'\bstun(?:ned|ning|s)?\b',
 'silence':r'\bsilenc(?:e|ed|ing|es)\b', 'blind':r'\bblind(?:ed|ing|s)?\b',
 'fear':r'\bfear(?:ed|ing|s)?\b', 'taunt':r'\btaunt(?:ed|ing|s)?\b',
 'airborne':r'\bairbo[r]?n[e]?\b', 'knockback':r'knock.{0,25}back|push(?:es|ing)? (?:or pulling |enemies |them |targets )*back',
 'pull':r'(?:dragging|pulling) enemies|pulls? (?:nearby )?enemies|pulling them',
 'suppression':r'\bsup(?:p)?ress(?:es|ing|ed)\s+(?:the |an |enemy|enemies)',
 'polymorph':r'\bpolymorph', 'heal_reduction':r'healing reduction|reducing the target.s healing',
 'armor_break':r'(?:reducing|decreasing|reduces?|decreases?) (?:their |the target.s |enemy )?defen[cs]e',
 'sleep':r'fall asleep', 'berserk':r'driving them insane',
}
statuses={};movement=[]
for cid,r in records.items():
 t=re.sub(r'<[^>]+>','',r['tooltip']).lower()
 found=[kind for kind,p in patterns.items() if re.search(p,t)]
 if found:statuses[cid]=found
 if re.search(r'\b(?:dashes?|teleports?|leaps?|jumps?|vaults?|surfs?|rushes?)\b|charges? (?:forward|toward|towards|in|to|quickly)|flies forward|become their shadow|dive in',t):movement.append(cid)
# Reviewed exceptions: effects on the caster or merely named resources are not debuffs on enemies.
for cid in ['hyejin_r','sua_r','mai_r','nia_q','garnet_w','craver_r','emma_r']:
 statuses.pop(cid,None)
statuses['nia_w']=['pull','slow']
statuses['darko_r']=['suppression','slow']  # Source tooltip misspells suppresses.
statuses['darko_w']=['slow','attack_down']
statuses['elena_r']=['freeze']
statuses['jackie_w']=['slow']
statuses['yuki_q']=['slow','stun']
statuses['emma_e']=['polymorph','slow']
statuses['vanya_r']=['slow','sleep']
statuses['garnet_w']=['slow','root']
statuses['sua_w']=['blind']
statuses['sua_q']=['slow','stun']
statuses['sua_e']=['slow','airborne']
statuses['weapon_bow']=['slow']
statuses['weapon_hammer']=['armor_break']
statuses['weapon_dagger']=['slow']
statuses['nathapon_r']=['stasis']
statuses['bernice_q']=['slow']
statuses['priya_r']=['dance']
statuses['sho_r']=['slow','heal_reduction']
statuses.pop('martina_w',None)
statuses['martina_q']=['slow','root']
movement += ['weapon_dagger','weapon_axe','weapon_sword','weapon_rapier','tactical_blink','tactical_plasma']
if 'stun' in statuses.get('xuelin_w',[]):statuses['xuelin_w'].remove('stun')
# Switches, enhanced versions and original resource gates are evaluated before the card consumes its resources.
gates={
 ('yuki_q','stun'):('cuff',1),('sua_q','stun'):('bookmark',1),('sua_e','airborne'):('bookmark',1),
 ('nia_w','slow'):('blocks',1),('felix_q','airborne'):('weaving',2),('felix_w','root'):('weaving',2),
 ('adina_q','stun'):('sign',1),('adina_w','stun'):('sign',1),('adina_e','stun'):('sign',1),
 ('martina_q','slow'):('camera',1),('martina_q','root'):('camera',1),
 ('emma_q','slow'):('dove',1),('adela_q','stun'):('pawn',3),('nicky_e','stun'):('guard_ready',1),
 ('magnus_e','stun'):('wall_pressure',1),('martina_w','root'):('record',3),('martina_e','stun'):('record',3),('martina_r','stun'):('record',3),
 ('mai_q','slow'):('pin',1),('coraline_q','slow'):('mirror',1),('fenrir_q','slow'):('claw',2),
 ('garnet_w','root'):('pain',3),('mirka_q','stun'):('impulse',4),('hisui_w','airborne'):('iaido',2),
 ('lucia_q','slow'):('glorious',1),('lucia_r','stun'):('crystal',1),('henry_e','root'):('chronograph',1),
 ('xuelin_w','stun'):('field_strikes',3),('istvan_e','airborne'):('variable',2),
 ('debi_marlene_e','airborne'):('stance',0),('rio_r','slow'):('bow',1),('rio_r','stun'):('bow',0),
 ('rio_r','knockback'):('bow',0),('ceres_q','stun'):('rush',1),
}
timings={
 ('jackie_w','slow'):'next_basic',('nadine_w','slow'):'trap',('nadine_r','slow'):'wolf',
 ('isol_q','root'):'semtex',('hyejin_w','pull'):'charm',('nathapon_w','root'):'last_frame',
 ('estelle_q','stun'):'next_basic',('estelle_r','airborne'):'helitack',('estelle_r','slow'):'helitack',
 ('alonso_q','stun'):'next_basic',('alonso_e','root'):'attraction',('arda_w','stun'):'cube',
 ('yumin_r','airborne'):'crane',('abigail_r','slow'):'dimension',('mirka_r','airborne'):'downburst',
 ('fenrir_r','slow'):'fatal_bite',('vanya_r','sleep'):'sleep_fuse',('henry_r','stun'):'chrono_explosion',
 ('garnet_w','root'):'charged_pain',('garnet_w','slow'):'pain_release',('lenore_r','berserk'):'finale',
 ('lenore_r','slow'):'recital',('rozzi_r','slow'):'semtex_fuse',
 ('nathapon_e','knockback'):'next_basic',('bernice_w','root'):'foothold',
 ('chiara_e','root'):'mania',('fiora_r','stun'):'fleche',('priya_r','dance'):'echo',
}
lines=['using System.Collections.Generic;','using System.Linq;','','namespace Lumia','{','    // Generated from saved reference tooltips; conditional and delayed effects were manually reviewed.','    public static class StatusIdentity','    {','        public static void Configure(List<CardDef> cards)','        {','            var table=new Dictionary<string,List<CardStatusRule>>();']
for cid,kinds in statuses.items():
 for kind in kinds:
  args=f'"{cid}","{kind}"'
  if (cid,kind) in gates:
   key,amount=gates[cid,kind];args+=f', need:"{key}", count:{amount}'
   if amount==0 or cid.startswith('adina_'):args+=', exact:true'
  if (cid,kind) in timings:args+=f', timing:"{timings[cid,kind]}"'
  lines.append(f'            Add(table,{args});')
  if cid=='martina_q' and kind=='root':
   lines.append('            var broadcastRoot=table["martina_q"].Last();broadcastRoot.conditionKey2="record";broadcastRoot.conditionAmount2=3;')
# Emma R retains its previous-skill branch rather than applying every variant at once.
for kind,prev,key in [('root','emma_q','dove'),('pull','emma_w','hat'),('polymorph','emma_e','rabbit'),('slow','emma_e','rabbit')]:
 lines.append(f'            Add(table,"emma_r","{kind}",need:"{key}",previous:"{prev}");')
for cid in ['elena_q','elena_w']:
 lines.append(f'            Add(table,"{cid}","freeze",need:"chill",count:2);')
for cid in ['tia_q','tia_e']:
 for kind,brush,paint in [('silence',0,2),('silence',1,1),('root',2,1),('root',0,3)]:
  lines.append(f'            Add(table,"{cid}","{kind}",need:"brush",count:{brush},exact:true);')
  lines.append(f'            var {cid}_{brush}_{paint}=table["{cid}"].Last();{cid}_{brush}_{paint}.conditionKey2="paint";{cid}_{brush}_{paint}.conditionAmount2={paint};{cid}_{brush}_{paint}.conditionExact2=true;')
lines+=['            var movement=new HashSet<string>(new[] { '+','.join('"'+x+'"' for x in movement)+' });',
 '            foreach(var card in cards)', '            {',
 '                if(card.category!="basic"){card.weak=0;card.vulnerable=0;}',
 '                List<CardStatusRule> rules;card.statuses=table.TryGetValue(card.id,out rules)?rules.ToArray():new CardStatusRule[0];',
 '                card.movement=movement.Contains(card.id);',
 '            }','        }',
 '        static void Add(Dictionary<string,List<CardStatusRule>> table,string card,string kind,string need=null,int count=1,bool exact=false,string timing="cast",string previous=null)',
 '        {','            List<CardStatusRule> rules;if(!table.TryGetValue(card,out rules)){rules=new List<CardStatusRule>();table.Add(card,rules);}',
 '            rules.Add(new CardStatusRule {key=kind,onHit=timing=="cast",duration=1,conditionKey=need,conditionAmount=count,conditionExact=exact,timing=timing,conditionPrevious=previous});',
 '        }','    }','}']
(root/'Assets/Scripts/Lumia/StatusIdentity.cs').write_text('\n'.join(lines)+'\n',encoding='utf-8')
reviewed_statuses={}
for cid,kind in re.findall(r'Add\(table,"([^"]+)","([^"]+)"','\n'.join(lines)):
 if kind not in reviewed_statuses.setdefault(cid,[]):reviewed_statuses[cid].append(kind)
doc=['# 원본 상태이상과 턴제 변환','', '원본 기록: `docs/art-chibi/reference-skills.json`의 12.5.0 툴팁. 원본에 없는 기술 카드의 일괄 약화·취약을 제거하고 아래 실제 상태를 부여한다. 조건 및 설치물 발동 시점은 카드 설명에서 확인할 수 있다.','',
 '| 카드 | 적용 상태 |', '|---|---|']
names={'slow':'둔화','root':'속박','stun':'기절','silence':'침묵','blind':'실명','fear':'공포','taunt':'도발','airborne':'에어본','knockback':'넉백','pull':'끌어당김','suppression':'제압','polymorph':'변이','heal_reduction':'치유 감소','armor_break':'방어력 감소','sleep':'수면','berserk':'광란','attack_down':'공격력 감소','freeze':'빙결','dance':'춤','stasis':'정지'}
for cid,kinds in reviewed_statuses.items():doc.append('| '+cid+' | '+', '.join(names[k] for k in kinds)+' |')
doc+=['','- 하드 CC는 다음 대상 턴의 코스트를 1~2 줄이며 가장 강한 효과만 적용한다. 최소 1 코스트는 유지한다. 원작의 행동 불능 시간을 턴제로 옮긴 밸런스 조정이다.',
 '- 침묵은 실험체 기술, 속박은 실제 이동 기술, 무장 해제는 기본 공격을 제한한다. 둔화·끌어당김은 이동 카드의 비용을 1 올린다.',
 '- 실명은 명중률 -25%, 공포·매혹·공격력 감소는 공격 피해 -20%, 방어력 감소는 받는 공격 피해 +10%, 치유 감소는 회복량 -20%다. 같은 상태는 중첩하지 않는다.',
 '- 기간은 대상의 턴 종료에 감소한다. 설치형 CC는 설치물의 실제 피해가 발생할 때 적용된다. 강화 기본 공격의 CC는 다음 기본 공격이 적중하면 적용되고, 빗나가도 준비 효과는 소비된다.',
 '- 치유 드론은 피해로 체력이 최대의 정확히 1/3 이하가 되었을 때 발동하며, 3턴 재발동 대기와 기존 2턴×2 회복을 유지한다.']
(root/'docs/STATUS_IDENTITY.md').write_text('\n'.join(doc)+'\n',encoding='utf-8')
print('Generated statuses for',len(reviewed_statuses),'cards; movement cards:',len(movement))
