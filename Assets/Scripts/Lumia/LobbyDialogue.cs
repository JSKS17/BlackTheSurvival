using System;
using System.Collections.Generic;

namespace Lumia
{
    // Presentation-only dialogue: reading or advancing never touches a run,
    // a save, Unity's random generator, or the engine's deterministic random stream.
    public static class LobbyDialogue
    {
        static readonly string[] Text =
        {
            "하나, 시작 최대 코스트는 5야. 센 카드부터 누르기 전에 이번 턴 계산부터 해 볼까?",
            "레벨이 올라도 최대 코스트는 그대로야. 코스트를 늘려 주는 조우도 눈여겨봐!",
            "실험체랑 보스 보상 카드는 코스트 상관없이 최대 2장! 많이보다 잘 맞는 걸 고르자.",
            "튜토리얼은 다 아는 내용이라고? 그 말 하고 첫 방에서 눕는 사람, 나도 봤는데. 히힛.",
            "보상 카드 잘못 눌렀어? 다시 누르면 선택 취소야. 확정하기 전까진 마음 바꿔도 돼.",
            "카드 보관은 전투마다 2번, 한 번에 1장이야. 대신 다음 턴 새로 뽑는 카드가 1장 줄어!",
            "남은 코스트가 0이어도 끝은 아니지! 조건을 채운 무료 연계 사용권이 있다면 대상 카드를 써 봐.",
            "이번 판 포기하겠습니다… 생각해 보니 진행하겠습니다. 하나 손패 보니까 또 하고 싶어졌어.",
            "내 Q는 아케이드 블록을 최대 3까지 남겨. W로 모아서 터뜨리면 다음 Q도 1코스트 할인!",
            "무기를 끼면 그 무기군의 D 카드가 덱에 들어와. 새 무기 샀으면 덱도 확인해 봐!",
            "알렉스 패시브가 있으면 무기마다 다른 무기군의 D도 하나 더 받아. 무기 바꾸는 맛 있지?",
            "상자 열 때 번쩍하는 연출, 나만 따라 해? 아무것도 안 나와도 자세는 전설 등급인데.",
            "치명타는 일반 공격만 터져. 기본 피해 150%에 장비 보너스를 더해. 확률 상한은 노말·하드 30%, 무한 100%야.",
            "패시브 자리는 3개야. 새 능력도 좋지만, 지금 덱이 쓰던 능력부터 보고 바꾸자.",
            "가지고 있는 패시브 주인의 기술은 카드 후보에 조금 더 잘 떠. 덱의 주연을 정해 볼까?",
            "키오스크에 딱 맞는 오브젝트가 있네. 음 매혹적… 잠깐, 내 지갑은 안 매혹적인데?",
            "시작 카드 후보에는 5코스트 이상이 안 떠. 첫 손패부터 못 쓰는 필살기만 잡히면 곤란하잖아.",
            "시작 카드 리롤 전에 마음에 드는 건 골라 둬. 선택한 카드는 남고 나머지만 다시 뽑아!",
            "모닥불 휴식은 완전 회복, 만년 스프, 카드 최대 2장 제거! 쉬는 것도 빌드의 일부라니까.",
            "태양도 좋지만 오늘은 모닥불 만세! 손 시리지, 배고프지, 체력 없지. 여기 완전 본진이야.",
            "모닥불에서 제작·요리를 고르면 회복은 없어. 둘을 합쳐 3번이니까 체력부터 확인해!",
            "제작 태그를 여러 개 누르면 그 옵션이 모두 있는 장비만 보여. 욕심내다 검색 결과 0 만들진 말고.",
            "VF혈액샘플은 두 번째 보스를 이겨야 키오스크에서 팔아. 그전엔 다른 장비 계획을 짜 보자!",
            "식스, 세븐… 크레딧 세는 중이야. 계산까지 밈으로 하면 키오스크에서 혼나겠지?",
            "키오스크는 만년 스프랑 조리 완료 음식을 안 팔아. 재료 챙겼으면 모닥불 위치도 봐 둬.",
            "늑대랑 곰은 오브젝트를 남기기도 해. 닭·들개·멧돼지한테선 오브젝트가 안 나오고!",
            "야생동물은 절반 확률로 고기를 줘. 기본 공격 카드도 3%로 나오니까 보상은 끝까지 확인!",
            "히든 보스 찾았냐고? 크레딧은 없는데 사고 싶은 건 다 있는 내 장바구니. 공략 좀 알려 줘.",
            "내 상태랑 회피율은 필드 창에서 같이 봐. 손패만 보고 달리면 켜 둔 효과도 놓친다?",
            "디버프가 헷갈리면 카드 상세 설명 맨 아래! 무슨 효과인지 알고 걸어야 콤보도 맞지.",
            "장비는 부위마다 기본 2개야. 무한 모드는 3막을 마칠 때 원하는 부위의 장착 한도를 1 늘릴 수 있어.",
            "게임 오버 화면이 실력 성적표는 아니야. 방금 패턴 하나 배웠으면 다음 판은 달라지지.",
            "1·2막은 세 경로 보스가 달라. 3막은 전부 나! 미리보기 보고 와도 봐주진 않을 건데. 히힛.",
            "보스 체력이 절반 이하면 다음 내 턴에 2페이즈! 나는 4분의 1 이하에서 한 번 더 바뀌어.",
            "금지형 보스 기믹은 금지 행동 없이 카드를 직접 2장 이상! 그냥 턴만 넘기면 성공이 아니야.",
            "위, 위, 아래, 아래… 아니, 하나! 여긴 커맨드 말고 카드를 누르는 게임이잖아.",
            "다시 만나기로 약속하면 다음 조우에 후속 이야기가 80%로 떠. 못 만난 약속도 사라지진 않아.",
            "보고서로 연 레어 조우는 다음 새 원정부터야. 지금 가던 길이 갑자기 바뀌는 건 아니고!",
            "실험체 과제 3개를 채우고 관측 데이터 12로 보고서를 완성해 봐. 새 이야기가 기다려.",
            "스피드런도 좋지만 설명 스킵 기록은 안 재. 보스가 뭘 금지했는지는 읽고 달리자, 응?",
            "해금한 실험체 후보는 12%로 레어 조우가 돼. 성공한 선택지엔 크레딧 30도 추가야!",
            "카드 강화는 고른 한 장에만 적용돼. 같은 카드라도 다른 방향으로 키울 수 있다는 거지!",
            "무한에서 보스가 주는 최대 코스트 증가는 추가 6까지! 조우로 올린 코스트는 따로 적용돼.",
            "협동 게임에서 혼자 다 하려는 사람 꼭 있더라. 하나는 계산해. 응원은 내가 맡을게.",
            "노말 탈출에 성공하면 하드, 하드 탈출에 성공하면 무한이 열려! 다음 스테이지도 같이 갈 거지?",
            "하드는 적 체력 25%, 직접 피해 10%가 늘어. 방어 카드를 장식으로 두면 나도 못 구해 준다?",
            "무한은 3막을 돌 때마다 적이 강해져. 같은 얼굴이라고 첫 바퀴처럼 덤비면 위험해!",
            "한 판만 더? 그 말은 보통 엔딩보다 길더라. 물 한 모금 마시고, 다음 판은 진짜 멋지게 가자."
        };

        public static readonly IReadOnlyList<string> Lines = Array.AsReadOnly(Text);
        public static int Count => Lines.Count;
        public static string At(int index) => Lines[Normalize(index)];
        public static int NextIndex(int index) => (Normalize(index) + 1) % Count;

        static int Normalize(int index)
        {
            int wrapped = index % Count;
            return wrapped < 0 ? wrapped + Count : wrapped;
        }
    }

    /// <summary>A presentation-only shuffle bag; every line appears once before reshuffling.</summary>
    public sealed class LobbyDialogueShuffle
    {
        readonly Random random;
        readonly List<int> bag = new List<int>();
        int previous = -1;
        public LobbyDialogueShuffle(int seed) { random = new Random(seed); }
        public int Next()
        {
            if (bag.Count == 0)
            {
                for (int i = 0; i < LobbyDialogue.Count; i++) bag.Add(i);
                for (int i = bag.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1), swap = bag[i]; bag[i] = bag[j]; bag[j] = swap;
                }
                if (bag.Count > 1 && bag[bag.Count - 1] == previous)
                { int swap = bag[0]; bag[0] = previous; bag[bag.Count - 1] = swap; }
            }
            previous = bag[bag.Count - 1]; bag.RemoveAt(bag.Count - 1); return previous;
        }
    }
}
