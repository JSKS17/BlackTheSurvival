using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public enum TutorialStep { Welcome, Attack, Guard, EndTurn, EnemyTurn, FirstBlock, SecondBlock, SpendCost, Detonate, FreeCard, Field, Complete }

    /// <summary>A disposable practice engine. Never receives a player's run or research profile.</summary>
    public sealed class TutorialSession
    {
        public GameEngine Engine { get; private set; }
        public TutorialStep Step { get; private set; }
        public bool FieldSeen { get; private set; }
        public static readonly string[] Dialogue =
        {
            "하나, 게임 할래? 여긴 연습용 아케이드야. 내가 옆에서 알려 줄게. 일단 한 판 해 보자!",
            "아래 ‘기본 공격’을 눌러 봐. 숫자는 필요한 코스트야. 오른쪽 위 ?를 누르면 쓰기 전에 정보도 볼 수 있어.",
            "좋아. 이번엔 ‘경계’! 방어도는 피해를 먼저 받아 줘. 상대의 예정 피해랑 같이 보면 계산하기 쉽겠지?",
            "할 일 끝났으면 턴 종료! 남은 코스트는 다음 턴에 쌓이지 않아. 상대가 뭘 하는지도 잘 봐.",
            "지금은 상대 차례야. 방어도가 먼저 줄어드는 거 보이지? 다음 내 턴이 오면 코스트가 다시 차.",
            "이제 내 기술로 콤보 연습! ‘아케이드 드롭’을 써 봐. 필드에 아케이드 블록이 남을 거야.",
            "블록 1! 한 번 더 ‘아케이드 드롭’. 내 Q는 블록을 최대 3까지 쌓아 둬. 모았다가 터뜨리는 맛이지.",
            "블록은 2가 됐어. 이번엔 ‘경계’를 써 봐. 남은 코스트로 어디까지 할 수 있는지 계산해 볼까?",
            "마무리는 ‘싱크 포인트’! 블록 2를 써서 추가 피해를 주고, 다음 Q의 코스트도 1 줄여 줘.",
            "코스트 0인데 Q가 켜졌네? 할인돼서 0이 된 카드는 쓸 수 있어. 조건으로 받은 무료 사용권도 마찬가지야. 눌러 봐!",
            "콤보 성공! ‘필드 확인’을 열어 봐. 내 상태와 회피율, 설치물은 여기서 확인해. 놓친 효과는 전투 기록도 보고!",
            "이 정도면 준비 끝! 보스의 예고와 금지 조건도 꼭 읽어. 하나는 카드 골라. 다음 스테이지에서 기다릴게. 히힛."
        };
        public string Line => Dialogue[(int)Step];
        public string ExpectedCard => Step == TutorialStep.Attack ? "basic_attack"
            : Step == TutorialStep.Guard || Step == TutorialStep.SpendCost ? "basic_guard"
            : Step == TutorialStep.FirstBlock || Step == TutorialStep.SecondBlock || Step == TutorialStep.FreeCard ? "nia_q"
            : Step == TutorialStep.Detonate ? "nia_w" : null;

        public TutorialSession()
        {
            Engine = new GameEngine(84125);
            var s = Engine.State;
            s.stage = RunStage.Combat; s.mainRune = s.supportRune = null; s.passives.Clear(); s.gear.Clear();
            s.log.Clear(); s.hp = s.maxHp = 80;
            s.combat = new CombatState { enemyName = "연습용 닭", enemyId = "tutorial_target", animal = "chicken", enemyLevel = 1,
                enemyHp = 999, enemyMaxHp = 999, enemyEnergy = 1, enemyAvailableEnergy = 1, turn = 1, energy = GameEngine.StartingMaxEnergy,
                intentDamage = GameDatabase.Card("basic_attack").damage,
                hand = new List<string> { "basic_attack", "basic_guard" },
                enemyPlan = new List<string> { "basic_attack" }, enemyPlanCosts = new List<int> { 1 }, enemyPlanFreeCast = new List<bool> { false } };
            s.combat.intentDamage = Engine.PreviewCardUse("basic_attack", true).normalDamage;
        }
        public bool Start()
        { if (Step != TutorialStep.Welcome) return false; Step = TutorialStep.Attack; return true; }
        public bool CanPlay(int index)
        {
            var hand = Engine.State.combat.hand;
            return index >= 0 && index < hand.Count && hand[index] == ExpectedCard && Engine.CanPlayCard(index);
        }
        public bool Play(int index)
        {
            if (!CanPlay(index) || !Engine.PlayCard(index)) return false;
            switch (Step)
            {
                case TutorialStep.Attack: Step = TutorialStep.Guard; break;
                case TutorialStep.Guard: Step = TutorialStep.EndTurn; break;
                case TutorialStep.FirstBlock: Step = TutorialStep.SecondBlock; break;
                case TutorialStep.SecondBlock: Step = TutorialStep.SpendCost; break;
                case TutorialStep.SpendCost: Step = TutorialStep.Detonate; break;
                case TutorialStep.Detonate: Step = TutorialStep.FreeCard; break;
                case TutorialStep.FreeCard: Step = TutorialStep.Field; break;
            }
            return true;
        }
        public bool EndTurn()
        {
            if (Step != TutorialStep.EndTurn || !Engine.BeginEndTurn()) return false;
            Step = TutorialStep.EnemyTurn; return true;
        }
        public void ObserveTurn()
        {
            var c = Engine.State.combat;
            if (Step != TutorialStep.EnemyTurn || c.enemyTurn || c.turn < 2) return;
            // Deliberately fixed lesson hand; all actual card resolutions use normal game rules.
            c.hand = new List<string> { "nia_q", "nia_q", "basic_guard", "nia_w", "nia_q" };
            c.drawPile.Clear(); c.discardPile.Clear(); c.energy = Engine.MaxEnergy;
            Step = TutorialStep.FirstBlock;
        }
        public bool OpenField()
        { if (Step != TutorialStep.Field) return false; FieldSeen = true; return true; }
        public bool FinishField()
        { if (Step != TutorialStep.Field || !FieldSeen) return false; Step = TutorialStep.Complete; return true; }
    }
}
