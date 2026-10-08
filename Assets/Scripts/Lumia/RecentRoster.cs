using System.Collections.Generic;

namespace Lumia
{
    // The 26 subjects introduced after Haze use their real ability names and
    // launch-patch cooldown snapshots. See docs/ROSTER_RECENT.md for sources.
    // All numeric combat effects below are turn-based fan-game adaptations.
    public static class RecentRoster
    {
        public static void Populate(List<CardDef> cards, List<PassiveDef> passives, List<CharacterDef> characters)
        {
            S(cards,"debi_marlene","데비&마를렌","q","하드 슬래시 / 크레센트 슬래시",4,damage:10,strength:1);
            S(cards,"debi_marlene","데비&마를렌","w","휠 댄스 / 크레센트 댄스",10,damage:6,hits:3,block:9);
            S(cards,"debi_marlene","데비&마를렌","e","엄호해줘, 마를렌! / 지금이야, 데비!",18,damage:14,evasion:30,duration:2,weak:1);
            S(cards,"debi_marlene","데비&마를렌","r","트윈즈 러시",90,damage:10,hits:5,weak:2,exhaust:true);
            P(passives,characters,"debi_marlene","데비&마를렌","블루&레드","서로 다른 색을 교차시키는 추가 피해를 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"arda","아르다","q","샤마쉬의 두루마리",7,damage:12);
            S(cards,"arda","아르다","w","바빌론의 입방체",14,damage:7,hits:2,weak:2);
            S(cards,"arda","아르다","e","님루드의 비석",16,damage:15,weak:1,block:5);
            S(cards,"arda","아르다","r","잠들어있는 힘",30,draw:2,strength:3);
            P(passives,characters,"arda","아르다","유물 탐구","유물이 축적하는 고대의 정수를 회복력으로 전환하여 턴 시작 시 체력을 3 회복합니다.","turn_heal",3);

            S(cards,"abigail","아비게일","q","바이너리 스핀",8.5f,damage:6,hits:2,evasion:10,duration:1);
            S(cards,"abigail","아비게일","w","호라이즌 클리브",10,damage:12,block:12,targets:new[]{"abigail_e"});
            S(cards,"abigail","아비게일","e","워프 슬래시",12,damage:13,evasion:20,duration:1);
            S(cards,"abigail","아비게일","r","디멘션 스트라이크",80,damage:40,evasion:50,duration:1,weak:2,targets:new[]{"abigail_e"},exhaust:true);
            P(passives,characters,"abigail","아비게일","티어링 블레이드","강화 공격과 방어력 감소 효과를 반영하여 기본 공격 피해를 4 증가시킵니다.","attack_bonus",4);

            S(cards,"alonso","알론소","q","마그네틱 펀치",8,damage:11,vulnerable:1,evasion:15,duration:1);
            S(cards,"alonso","알론소","w","바운싱 실드",16,damage:14,block:22);
            S(cards,"alonso","알론소","e","어트랙션 슬램!",17,damage:16,weak:2,block:8);
            S(cards,"alonso","알론소","r","게더링 필드",80,damage:7,hits:4,heal:18,weak:2,exhaust:true);
            P(passives,characters,"alonso","알론소","플라즈마 베리어","자기장으로 유지하는 회복력을 반영하여 턴 시작 시 체력을 3 회복합니다.","turn_heal",3);

            S(cards,"leni","레니","q","당근! 바주카",12,damage:12,heal:8);
            S(cards,"leni","레니","w","뿅!망치",16,damage:15,weak:2,evasion:20,duration:1);
            S(cards,"leni","레니","e","에어 호른! 건",9,damage:9,block:11,weak:1);
            S(cards,"leni","레니","r","스프링! 트랩",30,damage:27,weak:3,evasion:30,duration:1);
            P(passives,characters,"leni","레니","곰돌이! 공격","곰돌이의 지원 공격을 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"tsubame","츠바메","q","수리검 전개",6,damage:4,hits:3,weak:1);
            S(cards,"tsubame","츠바메","w","제비걸음",11,evasion:45,duration:2,draw:2);
            S(cards,"tsubame","츠바메","e","안개의 술",24,evasion:65,duration:2,draw:1);
            S(cards,"tsubame","츠바메","r","오의 - 생사 각인",0.5f,damage:28,heal:12,targets:new[]{"tsubame_w"},cost:3);
            P(passives,characters,"tsubame","츠바메","히구루마류 암살술","나무토막을 이용한 바꿔치기를 반영하여 회피율을 12% 증가시킵니다.","evade_bonus",12);

            S(cards,"kenneth","케네스","q","분노의 일격",9,damage:13,heal:4,weak:1);
            S(cards,"kenneth","케네스","w","업화",14,block:16,poison:4);
            S(cards,"kenneth","케네스","e","맹공격",14,damage:12,evasion:25,duration:2,strength:2,targets:new[]{"kenneth_e"});
            S(cards,"kenneth","케네스","r","화염 분쇄",80,damage:10,hits:4,strength:3,weak:2,exhaust:true);
            P(passives,characters,"kenneth","케네스","억압된 분노","불타는 도끼가 되돌려 주는 생명력을 반영하여 턴 시작 시 체력을 3 회복합니다.","turn_heal",3);

            S(cards,"katja","카티야","q","조준 사격",7,damage:14);
            S(cards,"katja","카티야","w","목표물 포착",30,draw:3,vulnerable:3);
            S(cards,"katja","카티야","e","접근 금지",13,damage:13,weak:2,evasion:30,duration:2);
            S(cards,"katja","카티야","r","정밀 조준",80,damage:18,hits:3,exhaust:true);
            P(passives,characters,"katja","카티야","잿빛 사신","탄환을 재장전하며 강화하는 사격을 반영하여 기본 공격 피해를 4 증가시킵니다.","attack_bonus",4);

            S(cards,"charlotte","샬럿","q","빛무리",8,damage:14,weak:2);
            S(cards,"charlotte","샬럿","w","치유의 빛",10,heal:16);
            S(cards,"charlotte","샬럿","e","희망의 궤적",8,block:14,evasion:45,duration:1);
            S(cards,"charlotte","샬럿","r","기적 실현",130,block:60,evasion:80,duration:1,exhaust:true);
            P(passives,characters,"charlotte","샬럿","고결한 마음","위기에 처한 아군을 보살피는 회복력을 반영하여 턴 시작 시 체력을 3 회복합니다.","turn_heal",3);

            S(cards,"darko","다르코","q","고리대금",3,damage:9,vulnerable:1,evasion:10,duration:1);
            S(cards,"darko","다르코","w","수금",10,block:16,weak:1,strength:2);
            S(cards,"darko","다르코","e","추심",14,damage:17,weak:2);
            S(cards,"darko","다르코","r","강제집행",90,damage:48,weak:3,exhaust:true);
            P(passives,characters,"darko","다르코","압류","적의 방어력을 훔치는 공격을 반영하여 기본 공격 피해를 3 증가시킵니다.","attack_bonus",3);

            S(cards,"lenore","르노어","q","스타카토",7,damage:3,hits:5);
            S(cards,"lenore","르노어","w","피네",15,damage:12,block:14,weak:1,evasion:20,duration:1);
            S(cards,"lenore","르노어","e","달 세뇨",17,damage:18,weak:2,vulnerable:1);
            S(cards,"lenore","르노어","r","고뇌의 광시곡",80,damage:33,poison:7,weak:3,exhaust:true);
            P(passives,characters,"lenore","르노어","고통의 선율","비명과 강화된 연주가 만드는 추가 피해를 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"garnet","가넷","q","짓뭉개기&꿰뚫기",6,damage:6,hits:2,weak:1);
            S(cards,"garnet","가넷","w","억누른 고통",13,damage:13,block:16,heal:12,targets:new[]{"garnet_q","garnet_e"},onHit:false);
            S(cards,"garnet","가넷","e","그릇된 집착",9,damage:12,weak:1,evasion:15,duration:1);
            S(cards,"garnet","가넷","r","처형식",80,damage:44,weak:2,vulnerable:3,exhaust:true);
            P(passives,characters,"garnet","가넷","익숙한 아픔","고통에 익숙해진 내구력을 반영하여 턴 시작 시 방어도를 4 얻습니다.","turn_block",4);

            S(cards,"yumin","유민","q","선풍",7,damage:4,hits:3,targets:new[]{"yumin_e"});
            S(cards,"yumin","유민","w","풍인",9,damage:12,weak:2,targets:new[]{"yumin_e"});
            S(cards,"yumin","유민","e","경운",11,damage:10,block:9,evasion:30,duration:2);
            S(cards,"yumin","유민","r","풍류운산",80,damage:20,hits:2,weak:3,targets:new[]{"yumin_e"},exhaust:true);
            P(passives,characters,"yumin","유민","도력","바람 지대에서 얻는 보호막을 반영하여 턴 시작 시 방어도를 4 얻습니다.","turn_block",4);

            S(cards,"hisui","히스이","q","쾌연격",10,damage:7,hits:2,weak:1);
            S(cards,"hisui","히스이","w","거합일섬",0,damage:6,hits:3,block:10,heal:8,cost:3);
            S(cards,"hisui","히스이","e","월륜참",15,damage:16,evasion:35,duration:1);
            S(cards,"hisui","히스이","r","모노호시자오 / 츠바메가에시",70,damage:17,hits:3,weak:2,exhaust:true);
            P(passives,characters,"hisui","히스이","검의 기억","검의 기억을 소모할 때 얻는 움직임을 반영하여 회피율을 10% 증가시킵니다.","evade_bonus",10);

            S(cards,"justyna","유스티나","q","연속 포격&섬멸 포격",0.8f,damage:6,hits:2,weak:1,targets:new[]{"justyna_w"},cost:2);
            S(cards,"justyna","유스티나","w","집중 포격",10,damage:6,hits:2,vulnerable:2);
            S(cards,"justyna","유스티나","e","부스트 대쉬",2,damage:7,evasion:25,duration:1);
            S(cards,"justyna","유스티나","r","아스트라 버스트",80,damage:6,hits:8,evasion:50,duration:1,exhaust:true);
            P(passives,characters,"justyna","유스티나","아스트라 에너지","아스트라 에너지가 재충전되는 효과를 반영하여 턴 시작 시 사용 가능한 코스트를 1 추가로 얻습니다.","turn_energy",1);

            S(cards,"istvan","이슈트반","q","관측",4.5f,damage:11);
            S(cards,"istvan","이슈트반","w","양자 얽힘",8,damage:9,block:10,heal:4);
            S(cards,"istvan","이슈트반","e","경로 적분",12,damage:14,vulnerable:2,evasion:20,duration:1);
            S(cards,"istvan","이슈트반","r","파동 함수 붕괴",80,damage:13,hits:4,weak:2,exhaust:true);
            P(passives,characters,"istvan","이슈트반","연산","다른 가능성을 계산한 뒤 얻는 기동력을 반영하여 회피율을 10% 증가시킵니다.","evade_bonus",10);

            S(cards,"xuelin","슈린","q","비검잔위",12,damage:5,hits:3,poison:3,targets:new[]{"xuelin_e"});
            S(cards,"xuelin","슈린","w","무진검결",20,damage:17,block:24,weak:2);
            S(cards,"xuelin","슈린","e","비섬보",14,damage:7,hits:2,evasion:30,duration:1,weak:1);
            S(cards,"xuelin","슈린","r","만검귀종",80,damage:8,hits:5,poison:5,exhaust:true);
            P(passives,characters,"xuelin","슈린","결심응진","회수한 어검이 되돌려 주는 생명력을 반영하여 턴 시작 시 체력을 3 회복합니다.","turn_heal",3);

            S(cards,"henry","헨리","q","시계바늘",9,damage:6,hits:2);
            S(cards,"henry","헨리","w","시간 제어 장치",14,damage:3,hits:4,poison:3,weak:2,targets:new[]{"henry_w"});
            S(cards,"henry","헨리","e","시간 도약",13,damage:11,weak:1,evasion:40,duration:2,targets:new[]{"henry_q","henry_w"},onHit:false);
            S(cards,"henry","헨리","r","시간 재구축 영역",80,damage:40,block:20,weak:3,exhaust:true);
            P(passives,characters,"henry","헨리","시차 균열","타이머가 폭발하며 만드는 추가 피해를 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"blair","블레어","q","이중 참격 / 이중 휩쓸기",10,damage:7,hits:2,heal:5);
            S(cards,"blair","블레어","w","말살 / 칼날폭풍",20,damage:5,hits:4,vulnerable:2,block:16);
            S(cards,"blair","블레어","e","절단 베기 / 반격 베기",9,damage:11,weak:1,block:10);
            S(cards,"blair","블레어","r","XMS-5 활성화 / 끝없는 추적",90,damage:40,heal:12,strength:3,exhaust:true);
            P(passives,characters,"blair","블레어","블레이드 시프트","무기 형태를 바꾼 뒤 강화되는 공격을 반영하여 기본 공격 피해를 4 증가시킵니다.","attack_bonus",4);

            S(cards,"mirka","미르카","q","크래시 해머",10,damage:13,weak:1);
            S(cards,"mirka","미르카","w","리펄스 배리어",9,block:17,evasion:15,duration:1);
            S(cards,"mirka","미르카","e","백스텝 러시 / 파워 히트",13,damage:8,hits:2,block:13,weak:1);
            S(cards,"mirka","미르카","r","다운버스트",80,damage:45,evasion:50,duration:1,weak:2,exhaust:true);
            P(passives,characters,"mirka","미르카","리펄스 게이지","축적한 게이지가 강화하는 망치 공격을 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"fenrir","펜리르","q","찢는 손톱",5,damage:10,heal:5,weak:1,targets:new[]{"fenrir_e"});
            S(cards,"fenrir","펜리르","w","먹잇감 추적 / 본능적 후퇴",14,damage:10,evasion:40,duration:1,targets:new[]{"fenrir_e"});
            S(cards,"fenrir","펜리르","e","사냥 개시",14,damage:11,evasion:25,duration:2);
            S(cards,"fenrir","펜리르","r","숨통 끊기",80,damage:41,block:19,weak:2,exhaust:true);
            P(passives,characters,"fenrir","펜리르","최후의 발악","죽음 앞에서도 버티는 생명력을 반영하여 최대 체력을 14 증가시킵니다.","max_health",14);

            S(cards,"coraline","코렐라인","q","단죄의 섬광",8,damage:13,weak:1,targets:new[]{"coraline_w"});
            S(cards,"coraline","코렐라인","w","진실의 거울 / 거짓의 거울",6,strength:2,draw:1);
            S(cards,"coraline","코렐라인","e","죄의 굴레",14,damage:14,block:9,vulnerable:2,weak:1);
            S(cards,"coraline","코렐라인","r","이세계의 잔영",32,damage:18,evasion:45,duration:2,targets:new[]{"coraline_q","coraline_w","coraline_e"});
            P(passives,characters,"coraline","코렐라인","인과의 이면","거울 조각 표식을 깨뜨리는 피해를 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"bihyung","비형","q","뚝딱!",8,damage:6,hits:2,heal:7);
            S(cards,"bihyung","비형","w","천벌 받아라!",11,damage:13,block:14,evasion:20,duration:1,weak:1);
            S(cards,"bihyung","비형","e","내기 한판!",14,damage:16,weak:2,evasion:15,duration:1);
            S(cards,"bihyung","비형","r","신명나게 놀아보자!",80,damage:46,block:25,weak:2,exhaust:true);
            P(passives,characters,"bihyung","비형","도깨비 불","신력이 만드는 도깨비 불의 추가 피해를 반영하여 스킬 피해를 3 증가시킵니다.","skill_bonus",3);

            S(cards,"craver","크레이버","q","더블 탭 / 포커스 샷",9,damage:6,hits:2,poison:2);
            S(cards,"craver","크레이버","w","스윕 킥 / 백플립",11,damage:6,hits:2,block:8,weak:1,evasion:30,duration:1,targets:new[]{"craver_w"});
            S(cards,"craver","크레이버","e","컴뱃 롤 / 퀵 스텝",13,damage:12,evasion:35,duration:1,targets:new[]{"craver_e"});
            S(cards,"craver","크레이버","r","쇼다운!",80,damage:37,heal:18,targets:new[]{"craver_q","craver_w","craver_e"},exhaust:true,onHit:false);
            P(passives,characters,"craver","크레이버","데스페라도","탄환에 담긴 추가 피해를 반영하여 기본 공격 피해를 4 증가시킵니다.","attack_bonus",4);

            S(cards,"lucia","루치아","q","화려한 낭만 / 찬란한 낭만",5,damage:10,weak:1);
            S(cards,"lucia","루치아","w","일제 사격!",8,damage:4,hits:4);
            S(cards,"lucia","루치아","e","무엄하시네요!",13,evasion:35,duration:2,draw:2);
            S(cards,"lucia","루치아","r","총사의 예법",70,damage:43,weak:2,targets:new[]{"lucia_e"},exhaust:true);
            P(passives,characters,"lucia","루치아","영애의 소양","수정을 깨뜨리며 강화하는 사격을 반영하여 기본 공격 피해를 4 증가시킵니다.","attack_bonus",4);

            S(cards,"ceres","세레스","q","쇄도 / 관철",12,damage:13,block:13,weak:1,evasion:15,duration:1);
            S(cards,"ceres","세레스","w","파쇄의 진",13,damage:18,weak:2);
            S(cards,"ceres","세레스","e","경호",12,block:17,heal:8,evasion:20,duration:1);
            S(cards,"ceres","세레스","r","빛에게 바치는 맹세",85,block:60,exhaust:true);
            P(passives,characters,"ceres","세레스","낭만을 지키는 검","흑요석 파편이 남기는 추가 피해를 반영하여 기본 공격 피해를 4 증가시킵니다.","attack_bonus",4);
        }

        static void S(List<CardDef> cards, string ownerId, string owner, string key, string name, float cooldown,
            int damage=0, int block=0, int heal=0, int draw=0, int energy=0, int poison=0,
            int vulnerable=0, int weak=0, int strength=0, int evasion=0, int duration=1,
            int hits=1, bool exhaust=false, string[] targets=null, int cost=-1, bool onHit=true)
        {
            cards.Add(new CardDef {
                id=ownerId+"_"+key, name=name, owner=owner, key=key.ToUpperInvariant(), cooldown=cooldown,
                cost=cost<0 ? GameDatabase.CostForCooldown(cooldown) : cost,
                damage=damage, block=block, heal=heal, draw=draw, energy=energy, poison=poison,
                vulnerable=vulnerable, weak=weak, strength=strength, evasion=evasion,
                duration=duration, hits=hits, exhaust=exhaust,
                freeCastTargets=targets??new string[0], freeCastCount=targets==null ? 0 : 1,
                freeCastOnHit=targets!=null && onHit,
                description="원본 스킬의 특징을 턴제 전투 효과로 전환합니다."
            });
        }

        static void P(List<PassiveDef> passives, List<CharacterDef> characters, string id, string owner,
            string name, string description, string trigger, int amount)
        {
            passives.Add(new PassiveDef { id=id+"_p", name=name, owner=owner, description=description, trigger=trigger, amount=amount });
            var character=characters.Find(x=>x.id==id);
            if(character==null)
            {
                character=new CharacterDef { id=id, name=owner };
                characters.Add(character);
            }
            character.passiveId=id+"_p";
            character.cards=new[]{id+"_q",id+"_w",id+"_e",id+"_r"};
        }
    }
}
