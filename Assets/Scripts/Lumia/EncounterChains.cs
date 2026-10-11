using System.Collections.Generic;

namespace Lumia
{
    // Follow-up scenes are deliberately separate from the normal subject roster.
    // Each promise is offered by one existing choice; its reward can be claimed once.
    public static class EncounterChains
    {
        public static readonly List<EventDef> Events = new List<EventDef>();

        public static EventDef Event(string id) => Events.Find(x => x.id == id);

        public static void Configure(List<EventDef> normalEvents)
        {
            Events.Clear();
            Followup("chain_sho_kitchen", "쇼우", "돌려받은 빈 도시락",
                "약속한 자리에 쇼우의 큰 냄비가 놓여 있다. 지난번 하나가 건넨 고기로 만든 도시락이 깨끗이 비워진 것을 보고 셰프가 호탕하게 웃는다. 이번에는 배를 채울지, 웍을 다루는 손을 배울지 하나에게 맡긴다.",
                O("여정용 트러플 파스타를 부탁한다", "식어도 맛있게 먹을 수 있도록 셰프가 포장을 두 번 확인한다.", "food", objectId:"truffle_pasta"),
                O("약속했던 웍 수업을 받는다", "쇼우가 식사 시간의 박자에 맞춰 웍을 들게 한다.", "card", card:"sho_w"),
                O("다음 손님을 위해 배식을 돕는다", "빈 그릇을 함께 정리하고, 쇼우가 챙겨 준 품삯을 받는다.", "credits", 45));
            Start(normalEvents, "event_sho", 2, "고기를 맡기고 다음 도시락을 예약한다",
                "쇼우가 맡긴 고기로 웰던 스테이크를 구워 주고, 다음 만남에 쓸 도시락통에 하나의 이름을 적는다.",
                "chain_sho_kitchen", food:"meat");

            Followup("chain_lenox_line", "레녹스", "다시 팽팽해진 낚싯줄",
                "하나가 도착하자 레녹스가 지난번 둘이 손질한 줄을 건넨다. 그때의 매듭은 끊어지지 않았다. 이번에는 수면 아래 큰 그림자가 걸렸다. 레녹스는 서두르지 말라며 하나의 손에 힘이 들어가는 순간을 기다린다.",
                O("둘이 잡은 생선으로 식사를 챙긴다", "레녹스가 이번 조과를 피쉬 앤 칩스로 손질해 포장한다.", "food", objectId:"fish_chips"),
                O("줄의 궤적으로 거리를 벌리는 법을 배운다", "약속했던 실습을 마치고 레녹스의 휩쓸기를 기술로 익힌다.", "card", card:"lenox_e"),
                O("끊어질 뻔한 매듭을 다시 묶는다", "줄을 정리하는 손끝으로 기존 기술의 낭비도 줄인다.", "upgrade_card"));
            Start(normalEvents, "event_lenox", 2, "낚시 미끼를 보태고 다음 입질을 약속한다",
                "레녹스가 오늘 잡은 연어를 나누고, 함께 손질한 낚싯줄을 다음 만남까지 챙겨 두겠다고 한다.",
                "chain_lenox_line", credits:15);

            Followup("chain_emma_finale", "엠마", "관객 한 사람을 위한 피날레",
                "엠마가 손수건을 펼치자 지난 공연에서 하나가 고른 카드가 나타난다. 빈 관객석 한가운데 이름표가 붙어 있다. 엠마는 약속한 조수가 왔다며, 탈출구를 꺼내는 마술에는 하나의 선택도 필요하다고 속삭인다.",
                O("마술의 마지막 순간을 함께 완성한다", "엠마가 Change★의 전환 타이밍을 하나에게 맡긴다.", "card", card:"emma_r"),
                O("카드를 다루는 기본 손놀림을 익힌다", "화려한 속임수보다 먼저 필요한 한 번의 정확한 움직임을 배운다.", "card", card:"basic_attack"),
                O("불필요한 패를 손수건 아래 넣는다", "엠마의 손수건 아래에서 불필요한 패를 정리한다.", "remove_card"));
            Start(normalEvents, "event_emma", 2, "무대 소품을 보태고 다음 공연의 조수가 된다",
                "엠마의 응원을 받으며 잠시 쉬고, 다음 공연에는 관객석 대신 무대에 서기로 약속한다.",
                "chain_emma_finale", credits:20);

            Followup("chain_nia_continue", "니아", "둘이 누르는 CONTINUE",
                "지난번 하나와 니아가 맞춘 입력이 아케이드 화면에 저장되어 있다. 니아는 혼자서는 누를 수 없는 두 개의 버튼을 가리킨다. 하나가 손을 얹자 닫혔던 설정 창이 열린다. 이번 보너스의 사용처는 플레이어가 고른다.",
                O("다음 턴의 입력 제한을 한 칸 늘린다", "니아가 저장된 협동 기록으로 컨트롤러의 입력 여유를 확장한다.", "max_energy", 1),
                O("위기에서 다시 일어나는 법을 배운다", "니아가 다음 시도를 위한 1UP의 사용법을 보여 준다.", "card", card:"nia_e"),
                O("자주 쓰는 기술의 입력을 다듬는다", "새 슬롯 대신 익숙한 기술 하나의 입력을 더 정확하게 만든다.", "upgrade_card"));
            Start(normalEvents, "event_nia", 1, "컨티뉴 코인을 넣고 협동 입력을 저장한다",
                "니아와 싱크 포인트의 타이밍을 연습한 뒤, 다음 만남에 사용할 협동 플레이 기록을 저장한다.",
                "chain_nia_continue", credits:25);

            Followup("chain_alex_exchange", "알렉스", "암호문 뒤의 귀환 경로",
                "알렉스가 약속한 암호의 뒷부분을 건넨다. 지난번 하나가 제공한 연구소 주파수는 정확했다. 자기 신원 대신 탈출 경로가 적힌 지도를 펼치고, 이번 작전에 필요한 보상을 선택하라고 짧게 말한다.",
                O("원거리에서 신호를 끊는 법을 배운다", "알렉스가 코드 변경과 함께 필요한 기술만 정확히 전한다.", "card", card:"alex_q"),
                O("이미 익힌 작전 동작을 검토한다", "알렉스가 하나의 기술에서 눈에 띄는 움직임을 교정한다.", "upgrade_card"),
                O("정보 제공의 정산금을 받는다", "확인된 정보의 대가만 봉투에 넣어 건넨다.", "credits", 50));
            Start(normalEvents, "event_alex", 0, "해독 비용을 내고 다음 접선 신호를 받는다",
                "펄스 스팅을 익힌 하나에게 알렉스가 다음 접선에서 사용할 짧은 암호를 남긴다.",
                "chain_alex_exchange", credits:20);

            Followup("chain_cathy_checkup", "캐시", "봉합 뒤의 경과 관찰",
                "캐시가 지난번 처치 기록을 펼치고 같은 상처를 다시 살핀다. 하나가 탈출 이야기부터 꺼내자 경과 관찰을 끝낸 뒤에 하자고 답한다. 붕대 아래가 안정된 것을 확인하고서야 다음 치료 계획을 설명한다.",
                O("손상된 부위를 다시 치료받는다", "캐시가 지난 진단과 달라진 부분만 찾아 꼼꼼히 처치한다.", "heal", 28),
                O("반복해서 다치지 않을 관리법을 배운다", "치료 이후의 몸 관리까지 배우고 여정을 버틸 여유가 늘어난다.", "max_health", 4),
                O("정확한 수술 동작을 관찰한다", "캐시가 안전한 도구로 앰퓨테이션의 순서를 보여 준다.", "card", card:"cathy_w"));
            Start(normalEvents, "event_cathy", 1, "치료 물품을 보태고 경과 관찰을 예약한다",
                "캐시의 처치를 받은 뒤, 다음 만남에서 같은 상처의 경과를 확인하기로 한다.",
                "chain_cathy_checkup", credits:15);

            Followup("chain_fiora_rematch", "피오라", "같은 간격, 달라진 발끝",
                "피오라가 지난 수업과 같은 자리에 선다. 하나가 먼저 발을 고치자 검끝이 잠깐 내려간다. 약속한 연습을 잊지 않았다는 사실만은 인정한 듯하다. 이번에는 기본 찌르기를 굳힐지, 빈틈을 따라갈지 선택하게 한다.",
                O("흔들리지 않는 기본 찌르기를 반복한다", "피오라가 고개를 끄덕일 때까지 한 번의 정확한 찌르기를 완성한다.", "card", card:"basic_attack"),
                O("간격을 좁히는 동작까지 배운다", "피오라가 지난 자세에서 이어지는 마르셰 & 롱빼를 가르친다.", "card", card:"fiora_e"),
                O("마지막 대련까지 자세를 유지한다", "지친 뒤에도 자세를 무너뜨리지 않는 훈련으로 기초 체력을 단련한다.", "max_health", 3));
            Start(normalEvents, "event_fiora", 1, "자세를 교정받고 다음 대련을 약속한다",
                "피오라와 기존 기술 하나를 교정한 뒤, 다음 만남에서 같은 간격으로 다시 서 보기로 한다.",
                "chain_fiora_rematch");

            Followup("chain_theodore_signal", "테오도르", "잡음 사이의 같은 신호",
                "무전기에서 지난번 하나가 정한 호출 신호가 들린다. 테오도르는 약속한 자리에서 사선을 비워 두었다. 하나의 연구 기록을 반영한 지원 장치를 보여 주며, 다음 이동에 필요한 것을 먼저 묻는다.",
                O("지원 사격의 장치를 직접 다뤄 본다", "테오도르가 안전한 사선 뒤에서 에너지 포의 운용법을 알려 준다.", "card", card:"theodore_q"),
                O("엄폐 지점에서 상처를 돌본다", "테오도르가 주변을 살피는 동안 하나가 안전하게 치료를 마친다.", "heal", 24),
                O("확인한 안전 경로의 보수를 받는다", "두 사람이 함께 검증한 정찰 정보의 보수를 나눈다.", "credits", 45));
            Start(normalEvents, "event_theodore", 1, "지원 장치의 교정 비용을 내고 호출 신호를 정한다",
                "연구 기록으로 기존 기술을 다듬고, 다음 지원에서 사용할 둘만의 호출 신호를 정한다.",
                "chain_theodore_signal", credits:20);
        }

        static void Start(List<EventDef> normal, string eventId, int optionIndex, string label, string story, string followupId,
            int credits = 0, string food = null, string material = null)
        {
            var source = normal.Find(x => x.id == eventId);
            if (source == null || source.options == null || optionIndex >= source.options.Length) return;
            var option = source.options[optionIndex];
            option.label = label;
            option.description = story;
            option.followupId = followupId;
            option.creditCost = credits;
            option.requiredFoodId = food;
            option.requiredObjectId = material;
        }

        static void Followup(string id, string owner, string title, string story, params EventOption[] options)
        {
            Events.Add(new EventDef { id = id, owner = owner, title = title, story = story, options = options, chainOnly = true });
        }

        static EventOption O(string label, string story, string effect, int amount = 0, string card = null, string objectId = null)
        {
            return new EventOption { label = label, description = story, effect = effect, amount = amount, cardId = card, objectId = objectId };
        }
    }
}
