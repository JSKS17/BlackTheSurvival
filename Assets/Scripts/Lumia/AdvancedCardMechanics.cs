using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Source identities stay outside CardDef so old decks and upgraded references remain valid.
    // This class is pure: preparation always copies, and context calculations never spend anything.
    public static class AdvancedCardMechanics
    {
        // Per-cast display metadata is local to this temporary definition, never written to a deck/save.
        sealed class PreparedDefinition : CardDef
        {
            public int xCoefficient;
            public bool suppressAttack, emptyX, controlProtection;
            public CardDef targetSource;
        }
        static readonly HashSet<string> ChargeCards = new HashSet<string>(StringComparer.Ordinal) {
            "eleven_q", "eleven_w", "eleven_e", "aiden_w", "felix_r"
        };
        static readonly HashSet<string> XCards = new HashSet<string>(StringComparer.Ordinal) { "eva_r", "sho_r" };
        static string Id(CardDef card) => card == null ? "" : CardInstances.BaseId(card.id) ?? "";
        public static bool CanCharge(CardDef card) => ChargeCards.Contains(Id(card));
        public static bool IsX(CardDef card) => XCards.Contains(Id(card));
        public static bool HasTargetChoice(CardDef card) => Id(card) == "sissela_e" || Id(card) == "sua_w";
        public static bool GrantsControlProtection(CardDef card) => (card as PreparedDefinition)?.controlProtection == true;
        public static bool HasForesight(CardDef card) => Id(card) == "adina_r";
        public static int SacrificeLimit(CardDef card) => Id(card) == "tazia_r" || Id(card) == "echion_w" ? 3 : 0;
        public static string Kind(CardDef card) => HasTargetChoice(card) ? "target" : HasForesight(card) ? "foresight"
            : SacrificeLimit(card) > 0 ? "sacrifice" : CanCharge(card) ? "charge" : IsX(card) ? "x" : "";
        public static int ChargeGuard(CardDef card) => Id(card) == "eleven_w" ? 3 : 0;
        public static bool SuppressesAttack(CardDef card) => (card as PreparedDefinition)?.suppressAttack == true;
        public static bool IsEmptyX(CardDef card) => (card as PreparedDefinition)?.emptyX == true;
        public static int XCoefficient(CardDef card) => card is PreparedDefinition ? ((PreparedDefinition)card).xCoefficient : card?.damage ?? 0;
        public static int ChargeBonusDamage(CardDef card)
        {
            switch (Id(card)) {
                case "eleven_q": case "eleven_e": case "aiden_w": return 3;
                case "felix_r": return 7;
                default: return 0;
            }
        }

        // Runs once after identity, status and D/F balancing, before generated descriptions.
        public static void Configure(List<CardDef> cards)
        {
            foreach (var card in cards ?? new List<CardDef>()) {
                switch (Id(card)) {
                    case "sissela_e": card.block = 0; break;
                    case "sua_w": card.damage = 8; card.block = 10; break;
                    case "adina_r": card.cost = 1; card.draw = 0; card.energy = 0; break;
                    case "eleven_e": card.movement = true; break;
                    case "aiden_w": card.damage = 8; card.statuses = (card.statuses ?? new CardStatusRule[0]).Where(rule => rule.key != "root").ToArray(); break;
                    case "felix_r": card.statuses = (card.statuses ?? new CardStatusRule[0]).Where(rule => rule.key != "stun").ToArray(); break;
                    case "eva_r": card.damage = 8; card.hits = 1; card.cost = 1; break;
                    case "sho_r": card.damage = 9; card.hits = 1; card.cost = 1; break;
                }
            }
        }

        public static CardDef PreparedCard(CardDef source, string mode = "", int charge = 0, int sacrificed = 0, int x = 0)
        {
            if (source == null) return null;
            // Re-preparing a target display must start from its unchosen, upgraded definition.
            // This also makes switching self/enemy previews idempotent without leaking both branches.
            source=(source as PreparedDefinition)?.targetSource ?? source;
            var cloned = StrategyMechanics.Copy(source);
            var card = new PreparedDefinition { xCoefficient = XCoefficient(source), targetSource=HasTargetChoice(source)?source:null };
            foreach (var field in typeof(CardDef).GetFields()) field.SetValue(card, field.GetValue(cloned));
            card.statuses = (source.statuses ?? new CardStatusRule[0]).Where(rule => rule != null).Select(rule => {
                var copy = new CardStatusRule();
                foreach (var field in typeof(CardStatusRule).GetFields()) field.SetValue(copy, field.GetValue(rule));
                return copy;
            }).ToArray();
            if (HasTargetChoice(card) && mode == "self") {
                ClearEffects(card);
                card.suppressAttack = true;
                card.block = Id(card) == "sua_w" ? Math.Max(0, source.block) : 12 + Math.Max(0, source.block);
                card.controlProtection = Id(card) == "sua_w";
                return card;
            }
            if (Id(card) == "sua_w") card.block = 0;
            if (CanCharge(card) && charge > 0) {
                card.damage += ChargeBonusDamage(card); // Exactly one charge stage, never charge * bonus.
                if (Id(card) == "eleven_q" || Id(card) == "eleven_e")
                    foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
                        if (rule.op == "gain" && rule.key == "slider") rule.amount += 1;
                if (Id(card) == "eleven_w") {
                    foreach (var status in card.statuses) if (status.key == "taunt") status.duration = Math.Min(3, status.duration + 1);
                    foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
                        if (rule.op == "counter" && rule.key == "fork") rule.duration = Math.Min(3, rule.duration + 1);
                }
                // These controls are a full-charge reward in the original skills.
                if (Id(card) == "aiden_w") AddStatus(card, "root");
                if (Id(card) == "felix_r") AddStatus(card, "stun");
            }
            else if (CanCharge(card)) {
                if (Id(card) == "aiden_w") card.statuses = card.statuses.Where(rule => rule.key != "root").ToArray();
                if (Id(card) == "felix_r") card.statuses = card.statuses.Where(rule => rule.key != "stun").ToArray();
            }
            if (IsX(card)) {
                int paid = Math.Max(0, x);
                card.cost = paid;
                if (paid == 0) { ClearEffects(card); card.suppressAttack = true; card.emptyX = true; }
                else card.damage = (int)Math.Min(int.MaxValue / 8L, (long)Math.Max(0, card.damage) * paid);
                card.hits = 1; // One aggregated hit: strength, equipment and resource bonuses apply once.
            }
            return card;
        }

        static void AddStatus(CardDef card, string key)
        {
            if (!card.statuses.Any(rule => rule.key == key))
                card.statuses = card.statuses.Concat(new[] { new CardStatusRule { op = "status", key = key, label = StatusMechanics.Name(key), duration = 1, onHit = true } }).ToArray();
        }
        static void ClearEffects(CardDef card)
        {
            card.damage = card.block = card.heal = card.draw = card.energy = card.poison = card.weak = card.vulnerable = card.strength = card.evasion = 0;
            card.statuses = new CardStatusRule[0];
            card.mechanics = new SkillMechanicProfile();
            card.freeCastCount = 0; card.freeCastTargets = new string[0]; card.freeCastLastSkill = false;
        }

        // Snapshot BEFORE paying/spending/applying this card. Missing HP uses a ratio, not max-HP inflation.
        // Charge and X are already in PreparedCard; the two arguments deliberately add nothing here.
        public static SkillInstantBonus ContextualBonus(CardDef card, int hp, int maxHp, int block, int charge = 0, int sacrificed = 0, int x = 0)
        {
            var bonus = new SkillInstantBonus();
            int missing = Math.Max(0, maxHp - Math.Max(0, Math.Min(maxHp, hp)));
            int sacrifices = Math.Max(0, Math.Min(SacrificeLimit(card), sacrificed));
            switch (Id(card)) {
                case "sissela_r": bonus.damage = Ratio(missing, maxHp, 12); break;
                case "chiara_w": bonus.damage = Math.Min(10, Math.Max(0, block) / 3); bonus.block = Ratio(missing, maxHp, 6); break;
                case "lenox_q": bonus.damage = Math.Min(8, Math.Max(0, maxHp) / 25); break;
                case "tazia_r": bonus.damage = sacrifices * 4; break;
                case "echion_w": bonus.block = sacrifices * 4; break;
            }
            return bonus;
        }
        static int Ratio(int value, int maximum, int cap) => maximum <= 0 ? 0 : (int)Math.Min(cap, (long)value * cap / maximum);

        public static string Summary(CardDef card)
        {
            switch (Id(card)) {
                case "sissela_e": return "대상을 선택합니다. 자신은 방어, 적은 피해·기절을 받습니다.";
                case "sua_w": return "대상을 선택합니다. 자신은 방어·군중 제어 면역, 적은 피해·실명을 받습니다.";
                case "adina_r": return "뽑을 카드 3장을 보고 손패 1장과 교환합니다.";
                case "eleven_q": case "eleven_e": return "1턴 충전하면 피해와 미니 버거 생성량이 증가합니다.";
                case "eleven_w": return "1턴 충전하면 도발과 반격이 1턴 길어집니다.";
                case "aiden_w": return "1턴 충전하면 피해가 증가하고 속박을 부여합니다.";
                case "felix_r": return "1턴 충전하면 피해가 증가하고 기절을 부여합니다.";
                case "tazia_r": return "다른 손패를 최대 3장 소멸시켜 피해를 높입니다.";
                case "echion_w": return "다른 손패를 최대 3장 소멸시켜 방어도를 높입니다.";
                case "eva_r": case "sho_r": return "남은 코스트를 모두 써서 1당 피해 " + XCoefficient(card) + "을 줍니다.";
                case "sissela_r": return "잃은 체력 비율이 높을수록 추가 피해를 줍니다.";
                case "chiara_w": return "기존 방어도로 공격하고 잃은 체력에 비례해 방어합니다.";
                case "lenox_q": return "자신의 최대 체력에 비례한 추가 피해를 줍니다.";
                default: return "";
            }
        }
        public static string Description(CardDef card)
        {
            switch (Id(card)) {
                case "sissela_e": return "사용할 때 대상을 선택합니다. " + TargetChoiceDescription(card,"self") + " " + TargetChoiceDescription(card,"enemy");
                case "sua_w": return "사용할 때 자신 또는 적을 선택합니다. " + TargetChoiceDescription(card, "self") + " " + TargetChoiceDescription(card, "enemy")
                    + " 보호는 새로 받는 군중 제어만 무시하며 기존 상태를 해제하지 않습니다. 이 카드로 얻은 보호 방어도를 먼저 소모하며 다른 카드로 방어도를 얻어도 보호량은 늘지 않습니다. 방어도가 파괴되어도 면역은 즉시 끝납니다. 중독·출혈·화상·피해 증감 등 군중 제어가 아닌 효과는 막지 않습니다.";
                case "adina_r": return "뽑을 카드의 위에서 최대 3장을 미리 봅니다. 그중 1장과 이 카드를 제외한 손패 1장의 위치를 맞바꿉니다. 손패의 장수와 두 카드의 강화는 유지되며, 교환 없이 사용할 수도 있습니다. 교환으로 카드를 사용하거나 버린 것으로 처리하지 않습니다.";
                case "eleven_q": case "eleven_e": return ChargeDescription(card) + " 충전 후 공격이 적중하면 미니 버거 생성량이 1 증가합니다. 미니 버거의 최대치는 4로 유지됩니다.";
                case "eleven_w": return ChargeDescription(card) + " 충전을 시작할 때 방어도 3을 얻습니다. 충전 후 사용하면 도발과 집중 도발 반격의 지속 시간이 각각 1턴 증가합니다.";
                case "aiden_w": return ChargeDescription(card) + " 충전 후 공격이 적중하면 1턴 동안 속박을 부여합니다.";
                case "felix_r": return ChargeDescription(card) + " 충전 후 공격이 적중하면 1턴 동안 기절을 부여합니다.";
                case "tazia_r": return "사용할 때 이 카드를 제외한 손패를 최대 3장 선택해 이번 전투에서 소멸시킵니다. 소멸시킨 카드 1장마다 직접 피해가 4 증가합니다. 아무 카드도 소멸시키지 않고 사용할 수도 있습니다. 영구 덱에서는 제거하지 않습니다.";
                case "echion_w": return "사용할 때 이 카드를 제외한 손패를 최대 3장 선택해 VF 연료로 전환하고 이번 전투에서 소멸시킵니다. 전환한 카드 1장마다 방어도를 4 추가로 얻습니다. 카드 전환 없이도 사용할 수 있으며 영구 덱에서는 제거하지 않습니다.";
                case "eva_r": case "sho_r": return "X 코스트: 사용 직전 남은 코스트를 모두 소모하고, 소모한 코스트 1당 기본 피해 " + XCoefficient(card) + "을 줍니다. 직접 공격은 한 번이며 힘·장비·고유 자원에 따른 추가 효과는 한 번만 적용합니다. 무료 사용권과 코스트 감소로 X를 늘리지 않습니다. X가 0이면 피해와 모든 카드 효과가 발생하지 않습니다.";
                case "sissela_r": return "사용 직전 자신의 잃은 체력 비율에 12를 곱한 만큼 직접 피해가 증가합니다. 소수점은 버리며 최대 12입니다. 최대 체력이 커져도 같은 체력 비율에서는 추가 피해가 같습니다.";
                case "chiara_w": return "사용 직전 보유한 방어도 3마다 직접 피해가 1 증가하며 최대 10입니다. 기존 방어도는 소모하지 않습니다. 자신의 잃은 체력 비율에 6을 곱한 만큼 방어도를 추가로 얻으며 소수점은 버립니다. 이번 카드로 새로 얻는 방어도는 이 카드의 피해를 늘리지 않습니다.";
                case "lenox_q": return "자신의 최대 체력 25마다 직접 피해가 1 증가하며 최대 8입니다. 소수점은 버립니다.";
                default: return "";
            }
        }
        public static string TargetChoiceDescription(CardDef card, string mode)
        {
            if (!HasTargetChoice(card) || mode != "self" && mode != "enemy") return "";
            var prepared = PreparedCard(card, mode);
            if (mode == "self")
                return "자신: 방어도 " + prepared.block + "을 얻습니다. " + (Id(card) == "sua_w"
                    ? "이 카드의 보호 방어도가 남아 있는 동안 군중 제어를 무시합니다. 보호량이 0이 되거나 다음 자기 턴에 방어도가 초기화되면 면역이 끝납니다. 적에게 피해나 실명을 주지 않습니다."
                    : "적에게 피해나 기절을 주지 않습니다.");
            string statuses = string.Join("·", (prepared.statuses ?? new CardStatusRule[0]).Select(rule => StatusMechanics.Name(rule.key) + " " + rule.duration + "턴"));
            return "적: 기본 피해 " + prepared.damage + "을 주며" + (statuses.Length > 0 ? " 적중하면 " + statuses + "을 부여합니다." : " 공격합니다.")
                + " 자신에게 방어도를 주지 않습니다.";
        }
        static string ChargeDescription(CardDef card)
        {
            int damage = ChargeBonusDamage(card);
            return CardPresentation.WithParticle(SkillReferences.Name(card),"를","을")+" 즉시 사용하거나 1턴 충전할 수 있습니다. 충전할 때 코스트를 미리 지불하고 해당 카드 한 장을 보관합니다. 다음 자기 턴부터 보관한 카드 한 장을 한 번 무료로 사용할 수 있으며 충전은 중첩되지 않습니다. 완료된 카드는 사용할 때까지 보관되고, 보관한 장수만큼 다음 턴에 새로 뽑는 카드가 줄어듭니다."
                + (damage > 0 ? " 충전 후 직접 피해가 " + damage + " 증가합니다." : "");
        }
    }
}
