using G = Lumia.BossGoal;
using F = Lumia.BossEffect;

namespace Lumia
{
    public static partial class BossProfiles
    {
        static partial void Classic()
        {
            Add("nathapon", "셔터 사이의 도주", "첫 구도는 이동으로 벗어나고, 후반에는 움직임을 기다리는 셔터에 새 경로를 남기지 않아야 합니다.",
                P(1,"구도 고정","렌즈 안에 다음 구도를 잡습니다.","이동 기술이나 공격 압박으로 구도를 깨뜨리세요.","nathapon_q",G.Movement,F.ResourcePrepare,1,F.Shield,8,resource:"picturesque",label:"구도",cap:3),
                P(2,"움직임을 기다리는 셔터","새 움직임이 남을 순간에 셔터를 닫으려 합니다.","이동 카드를 쓰지 않고 2장 이상 사용해 연속 사진의 구도를 멈추세요.","nathapon_r",G.NoMovement,F.Status,1,F.DelayedStrike,12,status:"blind"));
            Add("nicky", "컷 없는 스턴트", "큰 충격을 분노로 돌리는 가드에는 작은 행동을 나누고, 마지막 어퍼컷은 방어로 받아내야 합니다.",
                P(1,"가드의 역이용","큰 충격을 받아 분노로 돌릴 준비를 합니다.","카드를 2장 이상 사용하되 한 카드의 직접 피해를 24 미만으로 유지하세요.","nicky_w",G.NoHeavyHit,F.Counter,5,F.ResourcePrepare,1,resource:"rage",label:"분노",cap:4),
                P(2,"분노의 클라이맥스","분노를 어퍼컷에 싣습니다.","방어를 모아 마지막 펀치의 궤적을 흘리세요.","nicky_r",G.Block,F.Strike,14,F.ResourcePrepare,2,resource:"rage",label:"분노",cap:4));
            Add("daniel", "그림자 속 걸작", "강한 한 번의 타격에서 영감을 읽으므로 가벼운 행동으로 실루엣을 흐립니다. 후반에는 다른 카드 키로 걸작의 완성을 끊습니다.",
                P(1,"과장 없는 실루엣","강한 타격에서 걸작의 영감을 읽으려 합니다.","카드를 2장 이상 사용하고, 한 카드의 직접 피해를 24 미만으로 낮춰 실루엣을 흐리세요.","daniel_w",G.NoHeavyHit,F.ResourcePrepare,1,F.Evasion,10,resource:"inspiration",label:"영감",cap:4),
                P(2,"걸작의 마지막 선","그림자에 마지막 선을 긋습니다.","서로 다른 카드 키로 완성될 자세를 바꾸세요.","daniel_r",G.MixedKeys,F.DelayedStrike,16,F.Status,1,status:"blind"));
            Add("tia", "섞이는 세 가지 색", "같은 행동을 반복하면 물감의 조합에 갇히고, 무지개가 완성되면 띠아가 활기를 되찾습니다.",
                P(1,"겹친 물감","발밑의 두 색을 섞습니다.","다른 카드 키를 사용해 물감의 조합을 흔드세요.","tia_q",G.MixedKeys,F.Status,1,F.ResourcePrepare,1,resource:"paint",label:"물감",cap:3,status:"root"),
                P(2,"무지개 마무리","그림 전체에 마지막 색을 덧칠합니다.","기술을 연속으로 사용해 무지개를 완성할 틈을 끊으세요.","tia_r",G.Skill,F.Strike,12,F.Heal,6));
            Add("laura", "예고장에 적힌 망설임", "작은 틈으로 예고장을 피하고, 황혼의 도약이 끝나기 전에 결단해야 합니다.",
                P(1,"예고된 표적","예고장의 수신인을 정합니다.","저렴한 행동을 이어 괴도의 계산을 어긋나게 하세요.","laura_w",G.CheapCards,F.Evasion,10,F.ResourcePrepare,1,resource:"victim",label:"예고장 표식 상태",cap:1),
                P(2,"황혼의 절도","황혼의 착지로 행동 여유를 훔칩니다.","압박하거나 방어해 괴도의 마지막 거래를 막으세요.","laura_r",G.Damage,F.Drain,1,F.Heal,8));
            Add("lenox", "끊어야 하는 낚싯줄", "태클을 준비하는 레녹스의 박자를 끊고 푸른뱀의 긴 출혈을 차단해야 합니다.",
                P(1,"팽팽한 줄","당겨 올릴 태클을 준비합니다.","기본 공격을 나눠 낚싯줄의 긴장을 풀어 버리세요.","lenox_w",G.Basic,F.ResourcePrepare,1,F.Shield,8,resource:"tackle",label:"태클",cap:3),
                P(2,"푸른뱀의 고리","긴 채찍이 몸을 감쌉니다.","먼저 압박하거나 방어를 갖춰 푸른뱀을 끊으세요.","lenox_r",G.Damage,F.Status,1,F.DelayedStrike,12,status:"pull"));
            Add("leon", "물결 아래의 기세", "방어로 물웅덩이 준비를 늦추고, 큰 파도가 회복으로 이어지기 전에 압박해야 합니다.",
                P(1,"물보라의 발판","물웅덩이 위에 발판을 만듭니다.","방어를 준비해 물 위의 기세를 받아내세요.","leon_e",G.Block,F.ResourcePrepare,1,F.Shield,8,resource:"pool",label:"물웅덩이",cap:3),
                P(2,"돌아오는 파도","파도를 타며 호흡을 되찾습니다.","공격 압박이나 충분한 방어로 파도의 흐름을 끊으세요.","leon_r",G.Damage,F.Drain,1,F.Heal,8));
            Add("rozzi", "폭탄 뒤의 두 발", "사격 유형을 바꿔 부착 폭탄을 끊고, 후속 더블샷은 방어로 대응합니다.",
                P(1,"숨긴 셈텍스","사격 틈에 셈텍스를 붙입니다.","다른 카드 키를 섞어 폭탄을 붙일 타이밍을 흐리세요.","rozzi_r",G.MixedKeys,F.DelayedStrike,10,F.ResourcePrepare,1,resource:"semtex",label:"부착 폭탄",cap:1),
                P(2,"교차 더블샷","두 발의 교차 사격을 준비합니다.","방어를 모아 연속 사격을 받아내세요.","rozzi_w",G.Block,F.Strike,14,F.Prepare,1));
            Add("luke", "청소 완료까지 추격", "세제 표식의 추적을 끊고, 회복하며 추격하는 루크에게 반격할 틈을 남겨야 합니다.",
                P(1,"세제의 궤적","세제로 이동 흔적을 드러냅니다.","이동 기술이나 공격 압박으로 표적화를 끊으세요.","luke_q",G.Movement,F.ResourcePrepare,1,F.Status,1,resource:"clean_target",label:"세제 표식 상태",cap:1,status:"slow"),
                P(2,"마지막 서비스","추격을 마치며 여유를 되찾습니다.","기본 공격을 이어 추격의 호흡을 흔드세요.","luke_r",G.Basic,F.Drain,1,F.Heal,8));
            Add("dailin", "잔이 비기 전의 취권", "취기를 준비할 틈을 압박하고, 취권의 마지막 연타는 방어로 흘려야 합니다.",
                P(1,"비워지는 술잔","술잔을 비우며 취기를 올립니다.","공격하거나 방어를 갖춰 다음 잔을 방해하세요.","dailin_w",G.Damage,F.ResourcePrepare,2,F.Evasion,10,resource:"bac",label:"취기",cap:4),
                P(2,"취권의 호흡","술기운에 휩싸인 연타가 다가옵니다.","방어를 모아 연타와 침묵의 타이밍을 넘기세요.","dailin_r",G.Block,F.Strike,14,F.Status,1,status:"silence"));
            Add("rio", "두 활의 거리", "활의 흐름을 움직임으로 흔들고 마지막 화살의 긴 사선을 막아야 합니다.",
                P(1,"비상의 흐름","활을 바꾸며 거리를 벌립니다.","이동 기술이나 공격 압박으로 활의 흐름을 끊으세요.","rio_e",G.Movement,F.Evasion,10,F.ResourcePrepare,1,resource:"flow",label:"흐름",cap:3),
                P(2,"마지막 화살","정확한 마지막 화살을 준비합니다.","방어를 충분히 쌓아 장거리 사격을 흘리세요.","rio_r",G.Block,F.DelayedStrike,16,F.Prepare,1));
            Add("martina", "숨길 수 없는 방송", "카메라를 설치할 틈을 줄이고 녹화가 방송으로 이어지기 전에 기술로 끊어야 합니다.",
                P(1,"숨겨진 카메라","새 카메라의 각도를 맞춥니다.","기본 공격을 이어 카메라 설치를 방해하세요.","martina_w",G.Basic,F.ResourcePrepare,1,F.Shield,8,resource:"camera",label:"숨겨진 카메라",cap:2),
                P(2,"생방송 전환","취재 기록을 방송으로 연결합니다.","기술을 이어 써 방송 전환의 호흡을 끊으세요.","martina_r",G.Skill,F.ResourcePrepare,1,F.Heal,6,resource:"record",label:"녹화",cap:3));
            Add("mai", "몸에 맞는 장막", "큰 충격을 되돌리는 숄에는 가벼운 행동을 나눕니다. 후반에는 디버프 기술을 기다리는 수선에 맞춰 해로운 기술을 아껴야 합니다.",
                P(1,"장막을 흘리는 손길","큰 충격을 되돌려주는 숄을 펼칩니다.","카드를 2장 이상 사용하고, 한 카드의 직접 피해를 24 미만으로 유지해 장막을 흘리세요.","mai_w",G.NoHeavyHit,F.Shield,12,F.Counter,4),
                P(2,"흐트러짐 없는 안감","디버프 기술에 맞춰 안감을 수선할 준비를 합니다.","디버프 기술 없이 카드를 2장 이상 사용해 수선의 손길을 늦추세요.","mai_r",G.NoDebuffSkill,F.Heal,10,F.Cleanse,1));
            Add("markus", "흔들리는 대지의 교범", "행동을 섞어 균열을 막고, 전사의 돌격이 방어를 깨기 전에 준비해야 합니다.",
                P(1,"균열의 표식","전투 교범에 지각 균열을 새깁니다.","다른 카드 키로 균열을 만들 자세를 흔드세요.","markus_w",G.MixedKeys,F.Status,1,F.ResourcePrepare,1,resource:"rift",label:"지각 균열 상태",cap:1,status:"armor_break"),
                P(2,"전사의 충돌","균열을 따라 전사가 돌진합니다.","방어를 모아 충돌 지점을 흘리세요.","markus_e",G.Block,F.Strike,14,F.Status,1,status:"armor_break"));
            Add("magnus", "멈추지 않는 근성", "회전할 근성을 준비할 틈을 끊고 바이크가 도착하기 전에 충돌에 대비합니다.",
                P(1,"돌아가는 근성","근성으로 회전 공격을 버팁니다.","기술을 연속으로 사용해 회전 자세를 흔드세요.","magnus_w",G.Skill,F.Counter,4,F.ResourcePrepare,1,resource:"tough",label:"근성",cap:4),
                P(2,"폭주 도로","바이크의 충돌 경로가 좁아집니다.","방어를 모아 바이크를 받아내거나 먼저 압박하세요.","magnus_r",G.Block,F.DelayedStrike,16,F.Status,1,status:"slow"));
            Add("vanya", "돌아오는 꿈의 나비", "돌아오는 나비를 다른 행동으로 흘리고, 꿈에 잠기기 전 기본 공격으로 리듬을 깨야 합니다.",
                P(1,"왕복 나비","돌아올 나비의 길을 그립니다.","다른 카드 키를 조합해 왕복 경로를 흔드세요.","vanya_q",G.MixedKeys,F.DelayedStrike,10,F.Shield,6),
                P(2,"몽롱한 가루","꿈결 가루가 움직임을 늦춥니다.","기본 공격을 이어 꿈의 리듬을 깨뜨리세요.","vanya_r",G.Basic,F.Status,1,F.DelayedStrike,12,status:"slow"));
            Add("barbara", "실험은 오버클럭으로", "센트리를 설치할 틈을 압박하고 과충전 회로는 기본 공격으로 끊어야 합니다.",
                P(1,"센트리 기지","센트리 회로를 연결합니다.","공격하거나 방어를 준비해 설치 작업을 방해하세요.","barbara_q",G.Damage,F.ResourcePrepare,1,F.Shield,6,resource:"sentry",label:"센트리건",cap:2),
                P(2,"오버클럭 회로","레일건 회로를 과충전합니다.","기본 공격으로 회로의 빈틈을 끊으세요.","barbara_r",G.Basic,F.ResourcePrepare,2,F.Prepare,1,resource:"railgun",label:"레일건",cap:3));
            Add("bernice", "매가 지켜보는 덫", "덫을 움직임으로 피하고 매의 추적이 마지막 사격으로 이어지지 않게 압박합니다.",
                P(1,"발밑의 사냥 덫","퇴로에 덫을 설치합니다.","이동 기술이나 공격 압박으로 덫 설치를 끊으세요.","bernice_w",G.Movement,F.Status,1,F.DelayedStrike,8,status:"root"),
                P(2,"매의 사냥터","매가 마지막 탄환의 표적을 찾습니다.","먼저 압박하거나 방어를 갖춰 추적을 끊으세요.","bernice_r",G.Damage,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"falcon",label:"매 표식 상태",cap:1));
            Add("bianca", "관 밖의 붉은 영역", "혈액을 모을 시간을 줄이고, 후반에는 디버프 기술이나 집중 피해로 진조의 영역이 회복할 흐름을 끊습니다.",
                P(1,"혈액의 저금","혈액을 관 속에 모읍니다.","기술을 이어 써 혈액을 저장할 시간을 줄이세요.","bianca_q",G.Skill,F.ResourcePrepare,1,F.Heal,6,resource:"blood",label:"혈액",cap:4),
                P(2,"흐트러진 혈액 순환","혈액의 영역에서 회복의 흐름을 잇습니다.","디버프 기술을 1회 사용하거나 피해를 집중해 혈액 순환을 흐트러뜨리세요.","bianca_r",G.DebuffSkill,F.Drain,1,F.Heal,10));
            Add("celine", "빈 껍질도 폭탄이다", "저렴한 행동으로 설치를 방해하고 기본 공격으로 융합의 연결을 끊습니다.",
                P(1,"플라즈마 배치","폭탄 두 개를 연결합니다.","저렴한 행동을 나눠 설치 준비를 흔드세요.","celine_q",G.CheapCards,F.ResourcePrepare,2,F.DelayedStrike,8,resource:"bomb",label:"플라즈마 폭탄",cap:4),
                P(2,"자력 융합망","빈 껍질까지 융합합니다.","기본 공격을 이어 융합의 연결 고리를 끊으세요.","celine_r",G.Basic,F.ResourcePrepare,2,F.DelayedStrike,12,resource:"fusion",label:"융합 폭탄",cap:4));
            Add("sho", "빈 냄비의 뜨거운 맛", "식사를 준비할 틈을 압박하고 마지막 화력은 방어로 받아내야 합니다.",
                P(1,"든든한 한 그릇","식사하며 다음 웍을 준비합니다.","공격하거나 방어를 갖춰 식사 시간을 끊으세요.","sho_w",G.Damage,F.Heal,8,F.ResourcePrepare,1,resource:"meal",label:"든든한 식사",cap:3),
                P(2,"마지막 센 불","소스와 불길로 발걸음을 묶습니다.","방어를 모아 뜨거운 맛을 흘리세요.","sho_r",G.Block,F.DelayedStrike,12,F.Status,1,status:"slow"));
            Add("shoichi", "단검으로 쓰는 계약", "다양한 행동으로 바닥 단검의 계산을 어긋나게 하고 무자비한 연계는 짧게 끊습니다.",
                P(1,"바닥의 계약금","바닥 단검으로 다음 이동을 계산합니다.","다른 카드 키를 섞어 회수 경로를 흔드세요.","shoichi_e",G.MixedKeys,F.ResourcePrepare,2,F.Evasion,10,resource:"dagger",label:"바닥 단검",cap:4),
                P(2,"무자비한 정산","남은 단검으로 계약을 마감합니다.","저렴한 행동을 나눠 연속 이동의 틈을 끊으세요.","shoichi_r",G.CheapCards,F.Strike,12,F.Prepare,1));
            Add("sissela", "윌슨의 바깥과 안", "윌슨을 전개할 틈을 줄이고 해방의 거품이 터지기 전에 방어를 준비합니다.",
                P(1,"윌슨의 외출","윌슨을 하나 쪽으로 보냅니다.","기본 공격으로 윌슨의 전개를 방해하세요.","sissela_q",G.Basic,F.ResourcePrepare,1,F.Shield,8,resource:"wilson",label:"윌슨 외출 상태",cap:1),
                P(2,"해방의 거품","윌슨의 거품이 퇴로를 당깁니다.","방어를 모아 거품의 폭발을 흘리세요.","sissela_r",G.Block,F.DelayedStrike,16,F.Status,1,status:"pull"));
            Add("silvia", "피니시라인 너머", "짧은 행동으로 연료 준비를 끊고 바이크의 추격 경로는 움직임으로 흩뜨립니다.",
                P(1,"피트의 연료","바이크의 연료를 준비합니다.","저렴한 행동을 이어 시동 준비를 끊으세요.","silvia_q",G.CheapCards,F.ResourcePrepare,2,F.Evasion,10,resource:"fuel",label:"연료",cap:4),
                P(2,"바이크 추격","바이크가 피니시라인을 돌파합니다.","이동 기술이나 공격 압박으로 추격선을 흔드세요.","silvia_r",G.Movement,F.DelayedStrike,14,F.Evasion,15));
            Add("adela", "체크메이트의 세 기물", "폰이 기다리는 이동로를 피하며 제자리에서 첫 수를 넘기고, 기본 공격으로 마지막 나이트 포크를 끊습니다.",
                P(1,"멈춰야 하는 첫 수","이동할 길목에 승급할 폰을 놓습니다.","이동 카드 없이 2장 이상 사용해 폰이 기다리는 길목을 피하세요.","adela_q",G.NoMovement,F.ResourcePrepare,2,F.Shield,6,resource:"pawn",label:"폰",cap:4),
                P(2,"나이트의 포크","나이트가 마지막 공격선을 만듭니다.","기본 공격을 이어 체크메이트의 포크를 끊으세요.","adela_r",G.Basic,F.ResourcePrepare,1,F.DelayedStrike,12,resource:"knight",label:"나이트",cap:1));
            Add("adriana", "기름 위의 불빛", "기름을 뿌리는 동작은 기동으로 끊고 화염 지대는 충분한 방어로 넘겨야 합니다.",
                P(1,"기름의 경로","퇴로를 기름으로 잇습니다.","이동 기술이나 공격 압박으로 기름 경로를 끊으세요.","adriana_w",G.Movement,F.ResourcePrepare,1,F.DelayedStrike,8,resource:"oil",label:"기름",cap:3),
                P(2,"불길의 경계","화염이 남은 길을 좁힙니다.","방어를 모아 번지는 화염을 흘리세요.","adriana_r",G.Block,F.DelayedStrike,14,F.Status,1,status:"slow"));
            Add("adina", "합을 기다리는 별자리", "서로 다른 행동으로 별읽기를 흔들고, 합이 완성되기 전 기술을 이어 써야 합니다.",
                P(1,"빈 별자리","별자리의 합을 기다립니다.","다른 카드 키로 별읽기의 순서를 바꾸세요.","adina_r",G.MixedKeys,F.ResourcePrepare,1,F.Shield,6,resource:"conjunction",label:"별자리 합 상태",cap:1),
                P(2,"합의 궤도","다음 별에 힘을 집중합니다.","기술을 이어 사용해 완성될 합을 끊으세요.","adina_q",G.Skill,F.ResourcePrepare,1,F.DelayedStrike,12,resource:"conjunction",label:"별자리 합 상태",cap:1));
            Add("isaac", "세 번째 착취의 손", "기본 공격으로 착취의 준비를 흔들고 강탈은 방어로 받아내야 합니다.",
                P(1,"붙잡는 손","세 번째 착취를 준비합니다.","기본 공격을 나눠 접근할 타이밍을 끊으세요.","isaac_q",G.Basic,F.ResourcePrepare,1,F.Drain,1,resource:"exploit",label:"착취",cap:3),
                P(2,"강탈의 끝","공격과 회복을 한 번에 거둡니다.","방어를 모아 강탈의 손을 흘리세요.","isaac_r",G.Block,F.Strike,12,F.Heal,6));
            Add("alex", "잠입과 폭격의 교대", "다른 카드 키로 잠입의 계산을 깨고 이동이나 압박으로 폭격 좌표를 흔듭니다.",
                P(1,"잠입의 계산","코일 충전을 숨기며 경로를 바꿉니다.","다른 카드 키를 섞어 전환 경로를 흔드세요.","alex_e",G.MixedKeys,F.ResourcePrepare,1,F.Evasion,10,resource:"gauss",label:"코일 충전",cap:3),
                P(2,"정밀 폭격 좌표","폭격의 좌표를 고정합니다.","이동 기술이나 공격 압박으로 좌표 고정을 끊으세요.","alex_r",G.Movement,F.DelayedStrike,14,F.Status,1,status:"slow"));
            Add("jan", "로프 안의 열혈", "짧은 행동으로 링의 준비를 끊고 기본 공격으로 연계 발차기의 박자를 흐립니다.",
                P(1,"로프의 반동","링 로프에 반격을 겁니다.","저렴한 행동으로 링을 펼칠 호흡을 끊으세요.","jan_r",G.CheapCards,F.ResourcePrepare,1,F.Counter,4,resource:"ring",label:"링 로프",cap:1),
                P(2,"열혈의 회전","모은 열혈을 발차기에 싣습니다.","기본 공격을 이어 회전의 박자를 흐트러뜨리세요.","jan_w",G.Basic,F.ResourcePrepare,2,F.Strike,10,resource:"unyielding",label:"열혈",cap:3));
            Add("estelle", "방패 뒤의 구조", "압박으로 방패 진입을 끊고 헬기 지원은 행동을 섞어 저지해야 합니다.",
                P(1,"진입 방패","방패를 세우고 진입합니다.","공격하거나 방어를 갖춰 진입 대형을 무너뜨리세요.","estelle_e",G.Damage,F.Shield,12,F.ResourcePrepare,1,resource:"hazard",label:"방패방어 상태",cap:1),
                P(2,"헬기 구조선","헬기 지원이 도착합니다.","다른 카드 키로 구조 신호의 박자를 흔드세요.","estelle_r",G.MixedKeys,F.Heal,8,F.DelayedStrike,12));
            Add("aiden", "네 번의 암페어", "짧은 타격으로 충전을 방해하고 과전하의 두 번째 낙뢰에는 방어를 준비합니다.",
                P(1,"전하 축적","검끝의 암페어가 올라갑니다.","기본 공격을 이어 충전 동작을 끊으세요.","aiden_q",G.Basic,F.ResourcePrepare,2,F.Strike,6,resource:"ampere",label:"암페어",cap:4),
                P(2,"과전하 낙뢰","과전하가 다음 낙뢰를 부릅니다.","방어를 모아 두 번째 낙뢰를 흘리세요.","aiden_r",G.Block,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"overcharge",label:"과전하",cap:2));
            Add("echion", "탈피 직전의 VF", "기술로 VF 보호를 흔들고 폭주의 연속 입력은 행동을 섞어 끊어야 합니다.",
                P(1,"뒤집힌 비늘","VF를 비늘 아래 모읍니다.","기술을 이어 사용해 VF를 모을 여유를 줄이세요.","echion_w",G.Skill,F.ResourcePrepare,2,F.Shield,6,resource:"vf",label:"VF",cap:4),
                P(2,"독사의 폭주","VF 폭주가 행동의 여유를 압박합니다.","다른 카드 키로 폭주의 연속 패턴을 끊으세요.","echion_r",G.MixedKeys,F.ResourcePrepare,1,F.Drain,1,resource:"overflow",label:"VF 폭주",cap:2));
            Add("elena", "얼음 위의 스텝", "빙결 지대의 냉기는 이동으로 피하고, 후반에는 디버프 기술이나 집중 피해로 피겨의 박자를 어긋나게 만듭니다.",
                P(1,"냉기의 스텝","빙판 위에 냉기를 겹칩니다.","이동 기술이나 공격 압박으로 빙판의 흐름을 끊으세요.","elena_w",G.Movement,F.ResourcePrepare,1,F.Status,1,resource:"chill",label:"냉기",cap:3,status:"slow"),
                P(2,"어긋나는 피겨","냉기를 이어 마지막 파열의 박자를 맞춥니다.","디버프 기술을 1회 사용하거나 피해를 누적해 피겨의 박자를 어긋나게 하세요.","elena_r",G.DebuffSkill,F.Status,1,F.DelayedStrike,10,status:"slow"));
            Add("johann", "향로가 지키는 성역", "기본 공격으로 향로를 펼칠 틈을 줄이고, 디버프 기술이나 집중 피해로 성역의 회복 순환을 방해합니다.",
                P(1,"향로의 불씨","향로에 새로운 불씨를 넣습니다.","기본 공격을 이어 향로의 준비를 끊으세요.","johann_w",G.Basic,F.ResourcePrepare,1,F.Heal,6,resource:"censer",label:"신성의 향로",cap:1),
                P(2,"향로의 끊긴 기도","향로와 성역을 이어 회복의 순환을 만듭니다.","디버프 기술을 1회 사용하거나 피해를 집중해 성역의 기도를 방해하세요.","johann_r",G.DebuffSkill,F.Heal,10,F.Shield,10));
            Add("william", "돌아오는 공의 궤적", "기본 공격으로 바닥 공의 회수를 방해하고 완성된 패스트볼은 방어로 받아냅니다.",
                P(1,"바닥의 두 공","돌아올 공을 마운드에 놓습니다.","기본 공격을 나눠 회수할 박자를 흔드세요.","william_w",G.Basic,F.ResourcePrepare,2,F.Strike,6,resource:"ball",label:"바닥 공",cap:4),
                P(2,"완성된 캐치볼","마지막 캐치볼을 준비합니다.","방어를 모아 완성된 공의 궤적을 흘리세요.","william_r",G.Block,F.ResourcePrepare,2,F.DelayedStrike,12,resource:"catch",label:"캐치볼",cap:3));
            Add("irem", "생선과 고양이 방울", "생선 표적을 기동으로 피하고 고양이의 사냥 자세는 다른 행동으로 흔듭니다.",
                P(1,"생선의 표식","생선으로 사냥할 대상을 고릅니다.","이동 기술이나 공격 압박으로 표적을 벗어나세요.","irem_q",G.Movement,F.ResourcePrepare,1,F.Evasion,10,resource:"fish",label:"생선 표식 상태",cap:1),
                P(2,"고양이의 사냥","고양이 상태로 사냥을 준비합니다.","다른 카드 키를 섞어 고양이의 예측을 흔드세요.","irem_q",G.MixedKeys,F.ResourcePrepare,1,F.Evasion,15,resource:"cat",label:"고양이 상태",cap:1));
            Add("eva", "자수정 속 불협화음", "생명력을 모을 틈을 압박하고 서로 다른 행동으로 VF 방출의 호흡을 끊어야 합니다.",
                P(1,"모이는 생명력","자수정에 생명력을 모읍니다.","공격하거나 방어를 갖춰 충전을 방해하세요.","eva_e",G.Damage,F.ResourcePrepare,2,F.Shield,6,resource:"vital",label:"생명력",cap:4),
                P(2,"VF 불협화음","세 번째 방출에 힘을 집중합니다.","다른 카드 키로 방출의 호흡을 끊으세요.","eva_r",G.MixedKeys,F.DelayedStrike,16,F.ResourcePrepare,1,resource:"amethyst",label:"자수정의 물결 상태",cap:1));
            Add("ian", "붙잡힌 육신의 해방", "다른 행동으로 난동을 흩뜨리고 빙의한 육신의 압박에는 방어를 준비합니다.",
                P(1,"억누르는 난동","육신에 난동을 모읍니다.","다른 카드 키로 난동의 방향을 흔드세요.","ian_w",G.MixedKeys,F.ResourcePrepare,1,F.Evasion,10,resource:"thrash",label:"난동",cap:3),
                P(2,"유령의 육신","빙의한 육신이 상처를 회복합니다.","방어를 모아 빙의의 연속 타격을 흘리세요.","ian_r",G.Block,F.ResourcePrepare,2,F.Heal,6,resource:"possessed",label:"빙의",cap:3));
            Add("eleven", "방송을 멈추지 않는 포크", "미니 버거를 모으는 시간을 줄이고 회오리의 회복을 압박으로 끊습니다.",
                P(1,"미니 버거 준비","다음 한입을 위해 버거를 모읍니다.","기본 공격을 이어 버거를 챙길 틈을 줄이세요.","eleven_q",G.Basic,F.ResourcePrepare,2,F.Heal,6,resource:"slider",label:"미니 버거",cap:4),
                P(2,"칼로리 회오리","회오리와 회복으로 방송을 잇습니다.","압박하거나 방어를 갖춰 회오리의 흐름을 끊으세요.","eleven_r",G.Damage,F.Counter,5,F.Heal,8));
            Add("zahir", "사신의 눈과 네 차크람", "서로 다른 행동으로 차크람의 조준을 흔들고 하늘의 타격은 기술로 끊습니다.",
                P(1,"돌아오는 차크람","차크람의 다음 궤도를 읽습니다.","다른 카드 키로 궤도 계산을 어긋나게 하세요.","zahir_w",G.MixedKeys,F.ResourcePrepare,2,F.Evasion,10,resource:"chakram",label:"차크람",cap:4),
                P(2,"하늘의 사신","사신의 눈으로 마지막 타격을 고릅니다.","기술을 이어 사용해 하늘의 궤도를 끊으세요.","zahir_r",G.Skill,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"eye",label:"사신의 눈 상태",cap:1));
            Add("jenny", "죽음 뒤에도 계속되는 무대", "카펫을 펼치는 동작을 끊고 다른 배역으로 바뀌는 마지막 무대를 교란해야 합니다.",
                P(1,"펼쳐진 카펫","다음 배역의 카펫을 준비합니다.","기술을 이어 사용해 무대를 설치할 틈을 줄이세요.","jenny_w",G.Skill,F.ResourcePrepare,1,F.Shield,8,resource:"carpet",label:"레드 카펫",cap:1),
                P(2,"여왕의 커튼콜","무대 뒤에 마지막 폭발을 숨깁니다.","다른 카드 키로 배역 전환의 박자를 흔드세요.","jenny_r",G.MixedKeys,F.DelayedStrike,12,F.Heal,8));
            Add("camilo", "두 스텝 뒤의 귀환", "첫 춤은 행동을 섞어 끊고 두엔데의 귀환은 기동으로 흘립니다.",
                P(1,"맞물린 파소","회전할 파소를 맞춥니다.","다른 카드 키로 춤의 첫 박자를 바꾸세요.","camilo_q",G.MixedKeys,F.ResourcePrepare,1,F.Heal,6,resource:"paso",label:"파소",cap:2),
                P(2,"귀환의 두엔데","두 번째 스텝에서 되돌아옵니다.","이동 기술이나 공격 압박으로 귀환 지점을 흔드세요.","camilo_r",G.Movement,F.ResourcePrepare,1,F.DelayedStrike,12,resource:"step",label:"스텝",cap:2));
            Add("karla", "회수해야 할 작살", "짧은 행동으로 연결 작살을 끊고 큰 사슬은 압박이나 방어로 대응합니다.",
                P(1,"연결 작살","작살 두 개의 연결을 당깁니다.","저렴한 행동을 나눠 연결 준비를 끊으세요.","karla_q",G.CheapCards,F.ResourcePrepare,2,F.Shield,6,resource:"harpoon",label:"연결 작살",cap:4),
                P(2,"구속의 닻","큰 작살의 닻이 내려옵니다.","압박하거나 방어를 갖춰 닻의 연결을 끊으세요.","karla_r",G.Damage,F.Status,1,F.DelayedStrike,12,status:"pull"));
            Add("cathy", "세 번째 상처의 진단", "디버프 기술이나 집중 피해로 절개의 준비를 교란하고, 수술 구역의 회복은 다양한 행동으로 끊어야 합니다.",
                P(1,"수술 전의 상처","상처를 검사하며 절개를 준비합니다.","디버프 기술을 1회 사용하거나 피해를 누적해 절개의 준비를 교란하세요.","cathy_q",G.DebuffSkill,F.ResourcePrepare,1,F.Status,1,resource:"wounded",label:"상처",cap:3,status:"heal_reduction"),
                P(2,"응급 수술 구역","수술 구역과 후속 출혈을 준비합니다.","다른 카드 키를 섞어 처치의 박자를 끊으세요.","cathy_r",G.MixedKeys,F.DelayedStrike,12,F.Heal,8));
            Add("chloe", "니나와 함께 잡은 실", "기본 공격으로 니나 전개를 방해하고, 후반에는 큰 한 방을 나눠 생명 공유의 반격을 흘립니다.",
                P(1,"니나의 전개","니나에게 첫 공격 신호를 보냅니다.","기본 공격을 이어 인형을 전개할 틈을 줄이세요.","chloe_q",G.Basic,F.ResourcePrepare,1,F.Shield,6,resource:"nina",label:"니나 전개 상태",cap:1),
                P(2,"끊지 않는 생명의 실","큰 충격을 생명 공유의 반격으로 되돌리려 합니다.","카드를 2장 이상 사용하고, 한 카드의 직접 피해를 32 미만으로 나눠 생명의 실을 흘리세요.","chloe_r",G.NoHeavyHit,F.Counter,5,F.Heal,6));
            Add("chiara", "낙인을 거두는 심판", "기술로 낙인의 준비를 흔들고 폭주의 회복은 압박으로 끊어야 합니다.",
                P(1,"뒤틀린 기도","낙인을 기도 안에 모읍니다.","기술을 이어 사용해 기도를 마칠 틈을 줄이세요.","chiara_w",G.Skill,F.ResourcePrepare,1,F.Shield,8,resource:"stigma",label:"낙인",cap:4),
                P(2,"낙인의 심판","낙인을 거두며 생명력을 흡수합니다.","압박하거나 방어를 갖춰 폭주의 순환을 끊으세요.","chiara_r",G.Damage,F.Drain,1,F.Heal,10));
            Add("tazia", "깨뜨려 완성하는 검", "파편을 모을 틈을 줄이고 파라디소의 유리 검은 다른 행동으로 흔들어야 합니다.",
                P(1,"유리 장벽","장벽 아래 파편을 모읍니다.","기본 공격을 이어 파편 수집을 방해하세요.","tazia_w",G.Basic,F.ResourcePrepare,2,F.Shield,8,resource:"shard",label:"유리 파편",cap:4),
                P(2,"파라디소 파괴","완성한 유리 검을 깨뜨립니다.","다른 카드 키로 마지막 파괴의 자세를 끊으세요.","tazia_r",G.MixedKeys,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"sword",label:"유리 검",cap:1));
            Add("theodore", "스크린 너머의 고정포", "증폭 스크린의 사선을 기동으로 피하고 고정된 충전포에는 방어를 준비합니다.",
                P(1,"스크린 각도","증폭 스크린을 사선에 세웁니다.","이동 기술이나 공격 압박으로 스크린 각도를 흐리세요.","theodore_w",G.Movement,F.ResourcePrepare,1,F.Shield,6,resource:"screen",label:"증폭 스크린",cap:1),
                P(2,"고정된 에너지 포","목표를 고정하고 충전합니다.","방어를 충분히 쌓아 고정포의 사선을 흘리세요.","theodore_q",G.Block,F.ResourcePrepare,1,F.DelayedStrike,14,resource:"lock",label:"목표 고정 상태",cap:1));
            Add("felix", "세 번째 창술의 선택", "행동을 섞어 첫 연계를 끊고 기본 공격으로 뇌룡격의 호흡을 흔들어야 합니다.",
                P(1,"이어지는 창술","세 번째 창술의 마무리를 고릅니다.","다른 카드 키로 창술의 첫 연결을 끊으세요.","felix_q",G.MixedKeys,F.ResourcePrepare,1,F.Counter,3,resource:"weaving",label:"연계 창술",cap:3),
                P(2,"뇌룡의 충전","완성될 창술에 뇌룡격을 싣습니다.","기본 공격을 이어 충전 호흡을 흔드세요.","felix_r",G.Basic,F.ResourcePrepare,2,F.DelayedStrike,12,resource:"weaving",label:"연계 창술",cap:3));
            Add("priya", "꽃이 남긴 메아리", "꽃을 심는 동작을 끊고 열매와 보호가 돌아오기 전 대지를 압박해야 합니다.",
                P(1,"첫 개화","메아리를 받을 꽃을 심습니다.","기본 공격을 이어 꽃이 자리 잡을 틈을 줄이세요.","priya_q",G.Basic,F.ResourcePrepare,1,F.Heal,6,resource:"flower",label:"사라스바티 꽃",cap:3),
                P(2,"열매의 메아리","꽃 열매가 보호와 회복을 돌려줍니다.","압박하거나 방어를 갖춰 메아리의 순환을 끊으세요.","priya_r",G.Damage,F.Heal,10,F.Shield,10));
            Add("fiora", "세 번의 정확한 뚜셰", "다른 행동으로 찌르기의 간격을 바꾸고 완성된 플레슈에는 방어를 준비합니다.",
                P(1,"찌르기의 간격","발끝에 뚜셰의 간격을 잽니다.","다른 카드 키로 예측한 자세를 바꾸세요.","fiora_q",G.MixedKeys,F.ResourcePrepare,1,F.Evasion,10,resource:"touche",label:"뚜셰",cap:3),
                P(2,"마지막 플레슈","뚜셰를 마지막 찌르기에 모읍니다.","방어를 모아 정확한 플레슈를 흘리세요.","fiora_r",G.Block,F.ResourcePrepare,2,F.Strike,12,resource:"touche",label:"뚜셰",cap:3));
            Add("piolo", "끝까지 채우는 집중", "짧은 행동으로 튕겨내기의 호흡을 끊고 응징자는 기술을 이어 써 중단합니다.",
                P(1,"튕겨내는 호흡","쌍절곤으로 반격과 집중을 준비합니다.","저렴한 행동을 나눠 긴 호흡을 끊으세요.","piolo_w",G.CheapCards,F.Counter,4,F.ResourcePrepare,1,resource:"focus",label:"집중",cap:3),
                P(2,"응징자의 집중","모은 집중을 응징자에 싣습니다.","기술을 이어 사용해 마지막 집중을 흔드세요.","piolo_r",G.Skill,F.ResourcePrepare,2,F.Strike,12,resource:"focus",label:"집중",cap:3));
            Add("hart", "앙코르 없는 공연", "다른 카드 키로 오버드라이브를 끊고, 후반에는 디버프 기술을 아껴 공연이 이어갈 박자를 바꿉니다.",
                P(1,"오버드라이브 잔향","기타의 잔향에 힘을 싣습니다.","다른 카드 키로 공연의 첫 박자를 바꾸세요.","hart_w",G.MixedKeys,F.ResourcePrepare,1,F.DelayedStrike,8,resource:"overdrive",label:"오버드라이브 상태",cap:1),
                P(2,"흐트러지지 않는 앙코르","디버프 기술에 맞춰 공연의 회복을 이어갈 준비를 합니다.","디버프 기술을 쓰지 않고 카드를 2장 이상 사용해 공연의 박자를 바꾸세요.","hart_r",G.NoDebuffSkill,F.Heal,8,F.Status,1,status:"attack_down"));
            Add("haze", "다음 무기를 꺼내기 전에", "무기 교체의 사선을 흔들고 로켓 탄환의 마지막 폭발은 방어로 대비합니다.",
                P(1,"기관단총 교체","새 무기를 꺼내며 사선을 바꿉니다.","이동 기술이나 공격 압박으로 교체 동작을 끊으세요.","haze_e",G.Movement,F.Prepare,1,F.Evasion,10),
                P(2,"로켓 탄창","남은 로켓을 마지막 폭발에 모읍니다.","방어를 충분히 쌓아 로켓의 폭발을 흘리세요.","haze_r",G.Block,F.ResourcePrepare,2,F.DelayedStrike,14,resource:"ammo",label:"탄환",cap:4));
        }
    }
}
