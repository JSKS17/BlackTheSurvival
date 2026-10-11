using G = Lumia.BossGoal;
using F = Lumia.BossEffect;

namespace Lumia
{
    public static partial class BossProfiles
    {
        static partial void Recent()
        {
            Add("charlotte", "기적이 닿기 전에", "빛의 치유와 보호를 끊지 못하면 전투가 길어집니다. 체력 절반 아래에서는 약화를 걷어내며 보호를 강화합니다.",
                P(1,"치유의 빛","빛이 상처와 빈틈을 메웁니다.","피해를 집중하거나 방어로 치유의 흐름을 끊으세요.","charlotte_w",G.Damage,F.Heal,6,F.Shield,8),
                P(2,"꺼지지 않는 희망","기적의 빛으로 불리한 효과를 걷어냅니다.","다른 카드 키를 섞어 보호를 다시 만들 틈을 빼앗으세요.","charlotte_r",G.MixedKeys,F.Shield,16,F.Cleanse,1));
            Add("debi_marlene", "두 검의 교대 무대", "서로를 엄호하는 두 검이 잉크를 준비하며 공방을 교대합니다. 후반에는 붉은 잉크의 회복으로 압박을 이어갑니다.",
                P(1,"엄호하는 푸른 검","푸른 잉크를 남기며 다음 검을 엄호합니다.","다른 카드 키를 섞어 두 검의 교대 리듬을 흔드세요.","debi_marlene_e",G.MixedKeys,F.ResourcePrepare,1,F.Shield,8,resource:"ink",label:"블루&레드 잉크",cap:2),
                P(2,"쫓아오는 붉은 검","잉크를 채우고 두 검이 함께 밀고 들어옵니다.","기본 공격을 이어 교대 사이의 짧은 틈을 노리세요.","debi_marlene_r",G.Basic,F.ResourcePrepare,2,F.Heal,8,resource:"ink",label:"블루&레드 잉크",cap:2));
            Add("arda", "유적의 마지막 해독자", "입방체와 고대의 힘을 준비하며 해독할 틈을 요구합니다. 유물이 깨어난 뒤에는 보호를 얻고 원래 기술의 각성을 연결합니다.",
                P(1,"입방체 해독","고대의 힘이 입방체에 모입니다.","기본 공격을 이어 유물의 공명을 흐트러뜨리세요.","arda_w",G.Basic,F.ResourcePrepare,1,F.DelayedStrike,8,resource:"antiquity",label:"고대의 정수",cap:3),
                P(2,"잠든 힘의 각성","유물의 잠들어 있던 힘을 깨웁니다.","기술을 조합해 각성 의식을 중단하세요.","arda_r",G.Skill,F.ResourcePrepare,1,F.Shield,10,resource:"awakened",label:"잠들어있는 힘 각성 상태",cap:1));
            Add("abigail", "좌표를 지우는 사냥", "표식을 남긴 좌표로 차원을 베어 접근합니다. 이동으로 좌표를 흔들고, 마지막 차원 베기는 다양한 행동으로 저지할 수 있습니다.",
                P(1,"지워지지 않는 좌표","다음 베기의 차원 좌표를 고정합니다.","이동 효과 카드를 사용해 좌표에서 벗어나세요.","abigail_w",G.Movement,F.ResourcePrepare,1,F.Strike,6,resource:"coordinates",label:"좌표 표식 상태",cap:1),
                P(2,"차원 절단","차원 좌표를 따라 마지막 베기가 예약됩니다.","서로 다른 카드 키로 차원 절단의 계산을 흐트러뜨리세요.","abigail_r",G.MixedKeys,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"coordinates",label:"좌표 표식 상태",cap:1));
            Add("alonso", "벗어날 수 없는 자장", "큰 충격을 기다리는 방패에는 가벼운 행동을 나누고, 후반에는 자기장의 끌어당김과 회복을 다양한 행동으로 끊습니다.",
                P(1,"파동을 숨긴 방패","큰 충격을 반격할 자기장을 방패 뒤에 모읍니다.","카드를 2장 이상 사용하고, 한 카드의 직접 피해를 24 미만으로 유지해 자력을 흘리세요.","alonso_w",G.NoHeavyHit,F.Counter,4,F.Shield,10),
                P(2,"집결하는 자기장","확장된 자기장이 퇴로를 닫습니다.","다른 카드 키를 섞어 자기장의 중심을 벗어나세요.","alonso_r",G.MixedKeys,F.Heal,8,F.Status,1,status:"pull"));
            Add("leni", "장난감의 반격", "당근 바주카로 회복할 틈을 벌고 스프링 함정으로 다음 충격을 예약합니다. 정면에서 버티는 대응과 움직이는 대응을 번갈아 요구합니다.",
                P(1,"당근 응급처치","당근과 장난감 망치로 재정비합니다.","방어를 모아 장난감의 반격을 받아내세요.","leni_q",G.Block,F.Heal,6,F.Counter,3),
                P(2,"스프링 탈출극","스프링 함정 뒤로 몸을 숨깁니다.","이동 효과 카드를 사용해 함정이 터질 자리를 피하세요.","leni_r",G.Movement,F.DelayedStrike,16,F.Evasion,15));
            Add("tsubame", "네 번째 인장의 칼끝", "수리검과 안개의 인장을 준비하며 생사각인의 마무리를 노립니다. 인장이 쌓이기 전에 공격의 흐름을 바꿔야 합니다.",
                P(1,"안개의 인장","수리검 인장을 남기고 안개로 숨습니다.","서로 다른 카드 키를 사용해 은신의 박자를 읽으세요.","tsubame_q",G.MixedKeys,F.ResourcePrepare,1,F.Evasion,10,resource:"seal",label:"생사 각인",cap:4),
                P(2,"생사의 칼끝","남긴 인장으로 마지막 칼끝을 겨눕니다.","기본 공격을 이어 오의를 준비할 틈을 빼앗으세요.","tsubame_r",G.Basic,F.ResourcePrepare,2,F.Strike,10,resource:"seal",label:"생사 각인",cap:4));
            Add("kenneth", "꺼지지 않는 업화", "공격을 받아 분노를 준비하고 업화의 여진을 남깁니다. 후반에는 모은 분노와 회복으로 계속 전진합니다.",
                P(1,"분노의 불씨","업화 뒤에 분노의 불씨를 남깁니다.","방어를 모아 불씨가 번지는 것을 막으세요.","kenneth_w",G.Block,F.ResourcePrepare,1,F.DelayedStrike,8,resource:"rage",label:"불타는 도끼",cap:4),
                P(2,"꺼지지 않는 분쇄","분노를 채우며 화염 속에서 버팁니다.","집중 공격이나 충분한 방어로 재정비를 끊으세요.","kenneth_r",G.Damage,F.ResourcePrepare,2,F.Heal,8,resource:"rage",label:"불타는 도끼",cap:4));
            Add("katja", "한 발을 위한 사선", "새 이동 경로를 읽으며 사격 거리를 벌립니다. 제자리의 행동으로 정찰을 흘린 뒤 후반의 정밀 탄환은 방어로 견딥니다.",
                P(1,"정지선의 사격 거리","이동 경로를 읽고 사격 거리를 벌리려 합니다.","이동 카드 없이 2장 이상 사용해 정찰 사선에 새 경로를 남기지 마세요.","katja_e",G.NoMovement,F.ResourcePrepare,1,F.Evasion,10,resource:"distance",label:"사격 거리",cap:2),
                P(2,"정밀한 한 발","스캔을 고정하고 정밀 탄환을 예약합니다.","방어를 모아 한 발의 위력을 받아내세요.","katja_r",G.Block,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"scan",label:"드론 스캔 상태",cap:1));
            Add("darko", "강제집행 예고장", "부채를 준비하고 다음 턴의 행동 비용을 압박합니다. 후반에는 강제집행을 위한 방패까지 갖추므로 행동을 나눠 대응해야 합니다.",
                P(1,"고리대금 청구","부채 장부에 다음 행동을 기록합니다.","저렴한 카드를 나누어 사용해 수금 준비를 방해하세요.","darko_q",G.CheapCards,F.ResourcePrepare,1,F.Drain,1,resource:"debt",label:"채무 표식",cap:3),
                P(2,"집행 대기","부채를 쌓으며 집행할 자세를 잡습니다.","다른 카드 키를 섞어 집행의 근거를 무너뜨리세요.","darko_r",G.MixedKeys,F.ResourcePrepare,2,F.Shield,10,resource:"debt",label:"채무 표식",cap:3));
            Add("lenore", "끝나지 않는 광시곡", "짧은 음을 쌓은 뒤 다음 악장의 비용과 지연 타격을 준비합니다. 같은 리듬을 반복하면 마지막 연주를 멈추기 어렵습니다.",
                P(1,"떨리는 짧은 음","트릴을 쌓으며 짧은 음을 찌릅니다.","다른 카드 키를 섞어 연주의 박자를 바꾸세요.","lenore_q",G.MixedKeys,F.ResourcePrepare,1,F.Strike,6,resource:"trill",label:"트릴 연주",cap:3),
                P(2,"고뇌의 마지막 악장","광시곡의 여운과 다음 연주를 준비합니다.","방어를 충분히 쌓아 마지막 악장을 견디세요.","lenore_r",G.Block,F.DelayedStrike,14,F.Prepare,1));
            Add("garnet", "고통을 끌어안는 처형", "억누른 고통으로 반격할 틈을 만들고 처형의 포옹을 준비합니다. 공격하기 전에 방어와 압박의 순서를 고르는 전투입니다.",
                P(1,"억누른 고통","고통을 모아 첫 공격을 받아칠 준비를 합니다.","방어를 모아 고통의 반격을 흘리세요.","garnet_w",G.Block,F.ResourcePrepare,1,F.Counter,4,resource:"pain",label:"고통",cap:3),
                P(2,"처형의 포옹","포옹 속에 처형의 충격을 예약합니다.","집중 공격이나 충분한 방어로 처형 준비를 끊으세요.","garnet_r",G.Damage,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"embrace",label:"철처녀 표식 상태",cap:1));
            Add("yumin", "바람 사이의 빈틈", "풍검의 바람으로 보호를 두르고 흘러가는 검무 뒤에 여진을 남깁니다. 움직임과 다양한 기술을 섞어 바람의 간격을 읽어야 합니다.",
                P(1,"경운의 호흡","바람을 모아 검의 간격을 벌립니다.","이동 효과 카드를 사용해 바람의 중심을 바꾸세요.","yumin_e",G.Movement,F.ResourcePrepare,1,F.Shield,8,resource:"wind",label:"바람 지대",cap:2),
                P(2,"풍류의 잔상","검무의 여진을 남기고 바람 속에 숨습니다.","서로 다른 카드 키로 검무의 빈틈을 찾으세요.","yumin_r",G.MixedKeys,F.DelayedStrike,14,F.Evasion,15));
            Add("hisui", "두 검의 간격", "거합의 호흡을 준비한 뒤 장검의 간격으로 전환합니다. 기본 공격으로 호흡을 끊고 다양한 행동으로 되돌아오는 검을 막습니다.",
                P(1,"거합의 호흡","검집 안에 거합의 호흡을 모읍니다.","기본 공격을 이어 검을 뽑기 전의 호흡을 끊으세요.","hisui_q",G.Basic,F.ResourcePrepare,1,F.Shield,6,resource:"iaido",label:"거합 검기",cap:3),
                P(2,"돌아오는 제비","장검을 펴고 돌아오는 검격을 준비합니다.","서로 다른 카드 키로 장검의 간격을 흔드세요.","hisui_r",G.MixedKeys,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"long_sword",label:"모노호시자오",cap:2));
            Add("justyna", "별빛 포격 명령", "아스트라의 포격 준비를 방어로 감싸고 마지막 포격을 예약합니다. 기술을 섞어 조준을 끊거나 넓은 방어로 포격을 견뎌야 합니다.",
                P(1,"집중 포격 준비","아스트라를 모으며 사격 자세를 고정합니다.","서로 다른 카드 키로 집중 포격의 조준을 흔드세요.","justyna_w",G.MixedKeys,F.ResourcePrepare,1,F.Shield,6,resource:"astra",label:"아스트라 에너지",cap:4),
                P(2,"정의의 폭격","마지막 포격 명령에 폭발을 예약합니다.","방어를 충분히 쌓아 아스트라 버스트를 받아내세요.","justyna_r",G.Block,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"justice",label:"정의의 표적 상태",cap:1));
            Add("istvan", "관측 밖의 변수", "서로 얽힌 변수를 준비하며 관측을 고정합니다. 후반에는 작은 행동으로 계산을 바꾸지 않으면 붕괴의 여진이 다가옵니다.",
                P(1,"얽힌 관측","변수를 보호하며 관측을 이어갑니다.","다른 카드 키를 섞어 하나의 관측으로 읽히지 않게 하세요.","istvan_w",G.MixedKeys,F.ResourcePrepare,1,F.Shield,8,resource:"variable",label:"연산 변수",cap:3),
                P(2,"파동 붕괴","변수를 채워 붕괴의 순간을 예약합니다.","저렴한 행동을 나누어 사용해 계산을 계속 바꾸세요.","istvan_r",G.CheapCards,F.ResourcePrepare,2,F.DelayedStrike,12,resource:"variable",label:"연산 변수",cap:3));
            Add("xuelin", "검진의 주인", "날아간 검으로 첫 공격을 받아치고 검진의 검격을 준비합니다. 검의 바깥과 안쪽에서 서로 다른 대응이 필요합니다.",
                P(1,"돌아오는 비검","비검을 내보내 첫 공격을 기다립니다.","기본 공격을 이어 검을 회수할 호흡을 끊으세요.","xuelin_q",G.Basic,F.ResourcePrepare,1,F.Counter,3,resource:"sword_out",label:"어검 외출 상태",cap:1),
                P(2,"만검의 중심","검진에 검격을 모아 마지막 칼날을 예약합니다.","서로 다른 카드 키로 검진의 중심을 바꾸세요.","xuelin_r",G.MixedKeys,F.ResourcePrepare,2,F.DelayedStrike,12,resource:"field_strikes",label:"검진 낙검",cap:3));
            Add("henry", "되감기는 탈출 시각", "시간 장치에는 짧은 행동으로 대응하고, 후반에는 새 이동 경로를 남기지 않아 시간 재구축의 기록을 비웁니다.",
                P(1,"장치의 초침","시간 제어장치에 다음 초침을 기록합니다.","저렴한 행동을 이어 시간 장치의 기록을 흐트러뜨리세요.","henry_w",G.CheapCards,F.ResourcePrepare,1,F.DelayedStrike,10,resource:"chronograph",label:"시간 제어 장치",cap:1),
                P(2,"기록되지 않는 경로","새로운 이동 경로를 시간 영역에 기록하려 합니다.","이동 카드를 쓰지 않고 2장 이상 사용해 재구축할 경로를 남기지 마세요.","henry_r",G.NoMovement,F.Drain,1,F.DelayedStrike,14));
            Add("blair", "전환되는 추적 방식", "무장 전환으로 회복할 틈을 벌고 VP를 준비한 반격으로 추적을 이어갑니다. 한 가지 행동만 반복하면 전환된 칼날에 붙잡힙니다.",
                P(1,"전환 베기","무장을 전환하며 자세를 재정비합니다.","다른 카드 키를 사용해 전환의 박자를 흔드세요.","blair_e",G.MixedKeys,F.ResourcePrepare,1,F.Heal,6,resource:"shift",label:"시프트 상태",cap:1),
                P(2,"끝없는 추적","VP를 채우고 반격할 칼날을 겨눕니다.","방어를 모아 추적의 첫 반격을 받아내세요.","blair_r",G.Block,F.ResourcePrepare,2,F.Counter,5,resource:"vp",label:"VF 추적력",cap:3));
            Add("mirka", "충격을 돌려주는 망치", "임펄스를 모으는 장벽으로 첫 공격을 받아치고 충격을 되돌려줍니다. 작은 행동으로 장벽을 흔들고 후반에는 강하게 압박해야 합니다.",
                P(1,"리펄스 장벽","장벽에 임펄스를 모으며 반격을 기다립니다.","저렴한 행동을 나누어 장벽의 흐름을 흔드세요.","mirka_w",G.CheapCards,F.ResourcePrepare,1,F.Counter,4,resource:"impulse",label:"리펄스 게이지",cap:4),
                P(2,"망치의 잔향","임펄스를 채워 망치의 여진을 예약합니다.","피해를 집중하거나 충분한 방어로 충격 축적을 끊으세요.","mirka_q",G.Damage,F.ResourcePrepare,2,F.DelayedStrike,14,resource:"impulse",label:"리펄스 게이지",cap:4));
            Add("fenrir", "끝까지 쫓는 본능", "손톱의 준비와 회복으로 사냥을 이어가고 마지막에는 행동할 틈을 압박합니다. 추적을 견디는 방어와 다양한 행동이 서로 다른 돌파구입니다.",
                P(1,"찢는 손톱","손톱을 세우며 사냥할 체력을 회복합니다.","방어를 모아 첫 사냥의 손톱을 흘리세요.","fenrir_q",G.Block,F.ResourcePrepare,1,F.Heal,6,resource:"claw",label:"손톱",cap:3),
                P(2,"숨통을 겨누는 추격","회복하며 다음 행동의 틈을 빼앗습니다.","서로 다른 카드 키로 추적을 예측하기 어렵게 만드세요.","fenrir_r",G.MixedKeys,F.Drain,1,F.Heal,8));
            Add("coraline", "진실과 거짓의 반사", "거울의 면을 준비해 자신을 보호하고 이세계의 잔영으로 공격을 흘립니다. 같은 입력을 반복하지 않는 것이 두 거울을 읽는 열쇠입니다.",
                P(1,"진실의 거울면","거울의 면을 비추며 자신을 보호합니다.","다른 카드 키로 거울이 읽는 행동을 바꾸세요.","coraline_w",G.MixedKeys,F.ResourcePrepare,1,F.Shield,8,resource:"mirror",label:"백·흑 거울",cap:2),
                P(2,"이세계의 잔영","거울 잔영을 남기고 모습을 흩뜨립니다.","서로 다른 카드 키를 더 섞어 잔영과 본체를 구분하세요.","coraline_r",G.MixedKeys,F.ResourcePrepare,1,F.Evasion,15,resource:"shard",label:"거울 조각 표식 상태",cap:1));
            Add("bihyung", "신명나는 마지막 내기", "도깨비불을 모아 내기의 반격을 준비하고 놀이판의 지연 타격을 펼칩니다. 짧은 공격으로 내기를 흔들고 후반에는 행동을 다양하게 섞어야 합니다.",
                P(1,"내기 한 판","도깨비불을 모으며 내기의 결과를 기다립니다.","기본 공격을 이어 내기의 흐름을 바꾸세요.","bihyung_e",G.Basic,F.ResourcePrepare,1,F.Counter,3,resource:"divine",label:"신력",cap:3),
                P(2,"신명나는 놀이판","놀이판의 여운을 남기고 자신을 보호합니다.","서로 다른 카드 키로 놀이판의 규칙을 흔드세요.","bihyung_r",G.MixedKeys,F.DelayedStrike,14,F.Shield,10));
            Add("craver", "마지막 탄환의 승부", "탄창 단계를 준비하며 회피하고 마지막 탄환과 백플립을 연결합니다. 짧은 행동으로 재장전을 끊고 후반에는 입력을 다양하게 바꿔야 합니다.",
                P(1,"재장전의 빈틈","탄창을 넘기며 사선을 바꿉니다.","저렴한 행동을 이어 재장전할 시간을 빼앗으세요.","craver_q",G.CheapCards,F.ResourcePrepare,1,F.Evasion,10,resource:"magazine",label:"탄창 단계",cap:2),
                P(2,"백플립의 마지막 발","마지막 탄환을 준비하고 뒤로 빠지며 쏩니다.","다른 카드 키를 섞어 마지막 탄환의 사선을 흐트러뜨리세요.","craver_w",G.MixedKeys,F.ResourcePrepare,2,F.Strike,12,resource:"magazine",label:"탄창 단계",cap:2));
            Add("lucia", "예법을 갖춘 일제사격", "수정을 준비하며 자신을 보호하고 총사의 예법으로 사격 자세를 바꿉니다. 기본 공격으로 준비를 끊은 뒤 다양한 행동으로 마지막 사선을 피해야 합니다.",
                P(1,"화려한 준비","수정을 준비하며 사격 자세를 보호합니다.","기본 공격을 이어 일제사격을 준비할 틈을 빼앗으세요.","lucia_w",G.Basic,F.ResourcePrepare,1,F.Shield,6,resource:"crystal",label:"수정",cap:1),
                P(2,"총사의 마지막 예법","찬란한 자세로 전환하며 다음 탄환을 예약합니다.","다른 카드 키를 섞어 예법으로 읽힌 사선을 벗어나세요.","lucia_r",G.MixedKeys,F.ResourcePrepare,1,F.DelayedStrike,12,resource:"glorious",label:"찬란한 낭만 상태",cap:1));
            Add("ceres", "무너지지 않는 경호선", "경호의 보호로 첫 공격을 받아치며 맹세를 유지합니다. 체력 절반 아래에서는 회복과 방어를 함께 준비하므로 다양한 행동으로 보호를 끊어야 합니다.",
                P(1,"경호의 약속","경호선 뒤에서 첫 공격을 기다립니다.","피해를 집중하거나 방어를 모아 경호 자세를 흔드세요.","ceres_e",G.Damage,F.Shield,12,F.Counter,3),
                P(2,"빛에게 바치는 맹세","맹세의 빛으로 보호와 체력을 되찾습니다.","서로 다른 카드 키로 맹세를 다시 세울 틈을 빼앗으세요.","ceres_r",G.MixedKeys,F.Shield,16,F.Heal,6));
        }
    }
}
