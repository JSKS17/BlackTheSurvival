using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Pure, source-specific data. Baseline overrides are applied after the shared balance scaling.
    public sealed class SkillMechanicProfile
    {
        public string summary;
        public SkillRule[] rules = new SkillRule[0];
        public float damageMultiplier = 1, blockMultiplier = 1, healMultiplier = 1;
        public int damage = -1, hits = -1, block = -1, heal = -1, draw = -1, energy = -1, poison = -1;
        public int weak = -1, vulnerable = -1, strength = -1, evasion = -1, duration = -1, exhaust = -1, freeCastCount = -1;
        public string[] freeCastTargets;
        public bool? freeCastOnHit, freeCastLastSkill;
    }
    public class SkillRule
    {
        public string op, key, label, targetCard, scaleKey, conditionKey, conditionKey2, conditionPrevious;
        public int amount, cap = 3, duration = 2, delay = 1, conditionAmount = 1, conditionAmount2 = 1;
        public bool onHit, conditionExact, conditionExact2, isState;
        internal SkillRule ShallowCopy() => (SkillRule)MemberwiseClone();
    }
    [Serializable] public sealed class SkillResource
    {
        public string owner, key, label;
        public int amount, cap;
        public int fraction;
        public bool persistent, isState;
    }
    [Serializable] public sealed class SkillTimedEffect
    {
        public string owner, key, label, kind, sourceCard;
        public int amount, remaining, delay, uses;
        public bool persistent;
        public bool synergyBoosted;
        public bool tacticalEnhanced;
        public int synergyBoostAmount;
    }
    [Serializable] public sealed class SkillCostDiscount
    {
        public string sourceCard, targetCard;
        public int amount;
    }
    [Serializable] public sealed class SkillOwnerHistory
    {
        public string owner, cardId;
    }
    [Serializable] public sealed class SkillActorState
    {
        public List<SkillResource> resources = new List<SkillResource>();
        public List<SkillTimedEffect> effects = new List<SkillTimedEffect>();
        public List<SkillCostDiscount> discounts = new List<SkillCostDiscount>();
        public List<SkillOwnerHistory> history = new List<SkillOwnerHistory>();
        public List<string> discountSources = new List<string>();
        public SynergyState synergy = new SynergyState();
        public CompanionState companion = new CompanionState();
    }
    public sealed class SkillMechanicToken
    {
        public string owner, key, label, kind, sourceCard, targetCard;
        public int amount, cap, remaining, delay;
        public bool persistent;
    }
    public struct SkillInstantBonus { public int damage, block, heal; }
    public sealed class SkillPulse { public string sourceCard, kind, traitId, key; public int amount, externalAmount; }

    // A reviewed semantic link, never an automatic match on a resource key. The
    // passive may use a different key or encode a physical resource as a state.
    public sealed class SharedPassiveResourceDefinition
    {
        public readonly string passiveId, subjectId, passiveKey, skillKey, label;
        public readonly int cap;
        public readonly bool isState, passiveStateAsResource, legacyOnly;
        public bool persistent => false; // Reviewed shared identities all reset per battle; permanent growth stays private.
        public string owner => GameDatabase.Character(subjectId)?.name ?? subjectId;
        internal SharedPassiveResourceDefinition(string subjectId, string skillKey, int cap, string label,
            string passiveKey = null, bool isState = false, bool passiveStateAsResource = false, bool legacyOnly = false)
        {
            this.subjectId=subjectId; passiveId=subjectId+"_p"; this.skillKey=skillKey; this.passiveKey=passiveKey ?? skillKey;
            this.cap=cap; this.label=label; this.isState=isState; this.passiveStateAsResource=passiveStateAsResource; this.legacyOnly=legacyOnly;
        }
    }

    public static class SkillMechanics
    {
        static readonly SharedPassiveResourceDefinition[] sharedPassiveResources = {
            new SharedPassiveResourceDefinition("yuki","cuff",3,"단추"),
            new SharedPassiveResourceDefinition("nathapon","picturesque",3,"구도"),
            new SharedPassiveResourceDefinition("nicky","rage",4,"분노"),
            new SharedPassiveResourceDefinition("leon","pool",3,"물웅덩이"),
            new SharedPassiveResourceDefinition("dailin","bac",4,"취기"),
            new SharedPassiveResourceDefinition("markus","rattled",1,"흔들림 상태",isState:true),
            new SharedPassiveResourceDefinition("magnus","tough",4,"근성"),
            new SharedPassiveResourceDefinition("vanya","dream",4,"나비의 꿈"),
            new SharedPassiveResourceDefinition("bianca","blood",4,"혈액"),
            new SharedPassiveResourceDefinition("shoichi","dagger",4,"바닥 단검"),
            new SharedPassiveResourceDefinition("jan","unyielding",3,"열혈"),
            new SharedPassiveResourceDefinition("aiden","ampere",4,"암페어"),
            new SharedPassiveResourceDefinition("elena","chill",3,"냉기"),
            new SharedPassiveResourceDefinition("elena","frozen",1,"빙결 상태",isState:true),
            new SharedPassiveResourceDefinition("william","ball",4,"바닥 공"),
            new SharedPassiveResourceDefinition("eleven","slider",4,"미니 버거"),
            new SharedPassiveResourceDefinition("zahir","eye",1,"사신의 눈 상태",isState:true),
            new SharedPassiveResourceDefinition("karla","readiness",3,"장전"),
            new SharedPassiveResourceDefinition("chiara","stigma",4,"낙인"),
            new SharedPassiveResourceDefinition("tazia","shard",4,"유리 파편"),
            new SharedPassiveResourceDefinition("tazia","collection",4,"파편 수집"),
            new SharedPassiveResourceDefinition("tazia","sword",1,"유리 검",legacyOnly:true),
            new SharedPassiveResourceDefinition("felix","weaving",3,"연계 창술"),
            new SharedPassiveResourceDefinition("priya","flower",3,"사라스바티 꽃"),
            new SharedPassiveResourceDefinition("fiora","touche",3,"뚜셰"),
            new SharedPassiveResourceDefinition("debi_marlene","ink",2,"블루&레드 잉크"),
            new SharedPassiveResourceDefinition("arda","antiquity",3,"고대의 정수"),
            new SharedPassiveResourceDefinition("tsubame","seal",4,"생사 각인"),
            new SharedPassiveResourceDefinition("kenneth","rage",4,"불타는 도끼"),
            new SharedPassiveResourceDefinition("justyna","astra",4,"아스트라 에너지"),
            new SharedPassiveResourceDefinition("justyna","justice",1,"정의의 표적 상태",isState:true),
            new SharedPassiveResourceDefinition("istvan","variable",3,"연산 변수"),
            new SharedPassiveResourceDefinition("blair","form",1,"블레이드 형태 상태",isState:true),
            new SharedPassiveResourceDefinition("mirka","impulse",4,"리펄스 게이지"),
            new SharedPassiveResourceDefinition("coraline","shard",1,"거울 조각 표식 상태",isState:true),
            new SharedPassiveResourceDefinition("bihyung","divine",3,"신력"),
            new SharedPassiveResourceDefinition("lucia","crystal",1,"수정"),
            new SharedPassiveResourceDefinition("jackie","bleeding",3,"출혈 표식",passiveKey:"blood_marks"),
            new SharedPassiveResourceDefinition("cathy","wounded",3,"상처",passiveKey:"wound_stacks"),
            new SharedPassiveResourceDefinition("blair","shift",1,"시프트 상태",passiveKey:"shifting",isState:true),
            new SharedPassiveResourceDefinition("yumin","wind",2,"바람 지대",passiveKey:"wind_zone",passiveStateAsResource:true)
        };
        static readonly Dictionary<string,SharedPassiveResourceDefinition[]> sharedByPassive = sharedPassiveResources
            .GroupBy(x=>x.passiveId).ToDictionary(x=>x.Key,x=>x.ToArray(),StringComparer.Ordinal);
        static readonly SharedPassiveResourceDefinition[] noSharedResources = new SharedPassiveResourceDefinition[0];
        public static IEnumerable<SharedPassiveResourceDefinition> SharedPassiveResourceDefinitions => sharedPassiveResources;
        public static SharedPassiveResourceDefinition[] SharedResourcesFor(string passiveId)
        {
            SharedPassiveResourceDefinition[] definitions;
            return passiveId!=null && sharedByPassive.TryGetValue(passiveId,out definitions) ? definitions : noSharedResources;
        }
        // Explicit semantic classification: forms, preparations, and attached marks are binary
        // states. Physical objects (chess pieces, crystals, logs, charms) remain numerical resources.
        private static readonly Dictionary<string, string> stateNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {"aya|fear","공포"}, {"nicky|guard_ready","가드 준비"}, {"daniel|subject","영감의 대상"},
            {"laura|victim","예고장 표식"}, {"lenox|viper","푸른뱀 표식"}, {"luke|clean_target","세제 표식"},
            {"rio|bow","장궁 자세"}, {"mai|veil","숄 장막"}, {"markus|rattled","흔들림"}, {"markus|rift","지각 균열"},
            {"magnus|wall_pressure","벽 압박"}, {"vanya|sleep","몽롱함"}, {"bernice|trap","덫 속박"}, {"bernice|falcon","매 표식"},
            {"sho|sauce","소스범벅"}, {"shoichi|risk","협상 표식"}, {"sissela|wilson","윌슨 외출"}, {"silvia|bike","바이크 탑승"},
            {"adina|conjunction","별자리 합"}, {"isaac|reinforce","경화 준비"}, {"isaac|arrest","검거"},
            {"alex|recognition","타겟 마커"}, {"alex|infiltrate","잠입"}, {"jan|weave","위빙"}, {"estelle|hazard","방패방어"},
            {"aiden|rush","볼트 표식"}, {"aiden|overcharged","과전하 완료"}, {"echion|bite","송곳니 표식"}, {"elena|field","빙결 지대"}, {"elena|frozen","빙결"},
            {"william|mound","마운드"}, {"irem|cat","고양이"}, {"irem|fish","생선 표식"}, {"eva|amethyst","자수정의 물결"},
            {"zahir|eye","사신의 눈"}, {"jenny|role","블랙 티 배역"}, {"cathy|severe","중상"}, {"chloe|nina","니나 전개"},
            {"theodore|lock","목표 고정"}, {"hart|overdrive","오버드라이브"}, {"debi_marlene|stance","레드 자세"},
            {"arda|awakened","잠들어있는 힘 각성"}, {"abigail|coordinates","좌표 표식"}, {"abigail|blade_ready","티어링 블레이드 준비"},
            {"katja|scan","드론 스캔"}, {"garnet|embrace","철처녀 표식"}, {"justyna|justice","정의의 표적"},
            {"xuelin|sword_out","어검 외출"}, {"blair|form","블레이드 형태"}, {"blair|shift","시프트"},
            {"blair|basic_ready","블레이드 시프트 기본 공격 준비"}, {"blair|shifting","시프팅"},
            {"lucia|glorious","찬란한 낭만"}, {"ceres|rush","관철 준비"}, {"sua|book","마음의 양식"},
            {"yumin|wind_zone","바람 지대"}, {"coraline|shard","거울 조각 표식"},
            {"camilo|basic_step","기본 공격 후 교대 준비"}, {"camilo|skill_step","기술 후 교대 준비"}
        };
        private static readonly HashSet<string> targetStates = new HashSet<string>(StringComparer.Ordinal)
        {
            "aya|fear", "daniel|subject", "laura|victim", "lenox|viper", "luke|clean_target", "markus|rattled",
            "magnus|wall_pressure", "vanya|sleep", "bernice|trap", "bernice|falcon", "sho|sauce", "shoichi|risk",
            "isaac|arrest", "alex|recognition", "aiden|rush", "echion|bite", "elena|frozen", "irem|fish",
            "cathy|severe", "theodore|lock", "abigail|coordinates", "garnet|embrace", "justyna|justice", "coraline|shard"
        };
        private static readonly HashSet<string> formStates = new HashSet<string>(StringComparer.Ordinal)
        {
            "rio|bow", "silvia|bike", "estelle|hazard", "irem|cat", "jenny|role", "debi_marlene|stance", "blair|form", "arda|awakened", "alex|infiltrate"
        };
        private static string StateOwner(string owner)
        {
            string id = (owner ?? "").Replace("trait:", "");
            if (id.EndsWith("_p", StringComparison.Ordinal)) id = id.Substring(0, id.Length - 2);
            var character = GameDatabase.Characters.FirstOrDefault(x => x.id == id || x.name == id);
            return character == null ? id : character.id;
        }
        public static bool IsState(string owner, string key) { return !string.IsNullOrEmpty(key) && stateNames.ContainsKey(StateOwner(owner) + "|" + key); }
        public static bool IsState(CardDef card, string key) { return card != null && IsState(card.owner, key); }
        public static string StateName(string owner, string key, string fallback = null)
        {
            string label;
            if (!stateNames.TryGetValue(StateOwner(owner) + "|" + key, out label)) label = fallback ?? key ?? "활성";
            return label.EndsWith(" 상태", StringComparison.Ordinal) ? label : label + " 상태";
        }
        public static string StateName(CardDef card, string key) { return StateName(card?.owner, key, card == null ? key : ResourceName(card, key)); }
        public static string StateEnterText(CardDef card, string key)
        {
            string name=StateName(card,key), identity=StateOwner(card?.owner)+"|"+key;
            if(targetStates.Contains(identity))return "적에게 "+CardPresentation.WithParticle(name,"를","을")+" 부여합니다";
            if(formStates.Contains(identity))return name+"로 진입합니다";
            return CardPresentation.WithParticle(name,"를","을")+" 활성화합니다";
        }
        public static string StateExitText(CardDef card, string key) { return CardPresentation.WithParticle(StateName(card,key),"를","을")+" 해제합니다"; }
        public static string StateCondition(CardDef card, string key, int amount, bool exact)
        {
            if (IsState(card, key)) return (targetStates.Contains(StateOwner(card?.owner)+"|"+key) ? "적이 " : "") + StateName(card, key) + (amount == 0 && exact ? "가 아니면" : "이면");
            return CardPresentation.WithParticle(ResourceName(card, key), "가", "이") + " " + amount + (exact ? "이면" : " 이상이면");
        }
        public static void ConfigureStates(string owner, IEnumerable<SkillRule> rules)
        {
            foreach (var rule in rules ?? Enumerable.Empty<SkillRule>())
            {
                if (!IsState(owner, rule.key)) continue;
                if (rule.op == "consume" || ((rule.op == "gain" || rule.op == "set") && rule.cap == 1) || rule.op.StartsWith("state_", StringComparison.Ordinal))
                {
                    rule.isState = true; rule.cap = 1; rule.label = StateName(owner, rule.key, rule.label);
                    if (rule.op == "consume" || (rule.op == "set" && rule.amount == 0)) rule.op = "state_off";
                    else if (rule.op == "gain" || rule.op == "set") rule.op = "state_on";
                }
            }
        }
        public static void Configure(List<CardDef> cards)
        {
            var profiles = new Dictionary<string, SkillMechanicProfile>(StringComparer.Ordinal);
            SkillIdentityCore.Populate(profiles);
            SkillIdentityClassic.Populate(profiles);
            SkillIdentityRecent.Populate(profiles);
            foreach (var card in cards)
            {
                SkillMechanicProfile profile;
                if (!profiles.TryGetValue(card.id, out profile)) continue;
                foreach(var rule in profile.rules ?? new SkillRule[0])
                {
                    int limit=rule.op=="revive"?40:rule.op=="delayed_damage" || rule.op=="bonus_damage" || rule.op=="bonus_block"?24:rule.op=="bonus_heal"?20:rule.op=="empower_basic"?16:rule.op=="counter"?8:rule.op=="discount"?7:new[] {"bleed","burn","summon","hot","guard"}.Contains(rule.op)?10:8;
                    rule.amount=Clamp(rule.amount,0,limit);rule.cap=Clamp(rule.cap,1,8);rule.duration=Clamp(rule.duration,1,3);rule.delay=Clamp(rule.delay,1,3);
                }
                profile.summary = DescriptionSummary.Normalize(profile.summary);
                ConfigureStates(card.owner, profile.rules);
                card.mechanics = profile;
                card.damage = Override(card.damage, profile.damage, profile.damageMultiplier, 48);
                card.block = Override(card.block, profile.block, profile.blockMultiplier, 40);
                card.heal = Override(card.heal, profile.heal, profile.healMultiplier, 35);
                if (profile.hits >= 0) card.hits = Clamp(profile.hits, 1, 5);
                if (profile.draw >= 0) card.draw = Clamp(profile.draw, 0, 3);
                if (profile.energy >= 0) card.energy = Clamp(profile.energy, 0, 2);
                if (profile.poison >= 0) card.poison = Clamp(profile.poison, 0, 6);
                if (profile.weak >= 0) card.weak = Clamp(profile.weak, 0, 3);
                if (profile.vulnerable >= 0) card.vulnerable = Clamp(profile.vulnerable, 0, 3);
                if (profile.strength >= 0) card.strength = Clamp(profile.strength, 0, 4);
                if (profile.evasion >= 0) card.evasion = Clamp(profile.evasion, 0, 60);
                if (profile.duration >= 0) card.duration = Clamp(profile.duration, 1, 3);
                if (profile.exhaust >= 0) card.exhaust = profile.exhaust > 0;
                if (profile.freeCastCount >= 0) card.freeCastCount = Clamp(profile.freeCastCount, 0, 1);
                if (profile.freeCastTargets != null) card.freeCastTargets = profile.freeCastTargets;
                if (profile.freeCastOnHit.HasValue) card.freeCastOnHit = profile.freeCastOnHit.Value;
                if (profile.freeCastLastSkill.HasValue) card.freeCastLastSkill = profile.freeCastLastSkill.Value;
                if (!card.exhaust && card.cost == 0 && (card.draw > 0 || card.energy > 0)) card.cost = 1;
            }
            StatusIdentity.Configure(cards);
        }
        private static int Override(int original, int value, float multiplier, int cap)
        {
            return value >= 0 ? Clamp(value, 0, cap) : Clamp((int)Math.Round(original * Math.Max(0, Math.Min(1.5f, multiplier))), 0, cap);
        }
        public static int Clamp(int amount, int min, int max) { return Math.Max(min, Math.Min(max, amount)); }
        public static SkillActorState Ensure(SkillActorState actor)
        {
            if (actor == null) actor = new SkillActorState();
            if (actor.resources == null) actor.resources = new List<SkillResource>();
            if (actor.effects == null) actor.effects = new List<SkillTimedEffect>();
            if (actor.discounts == null) actor.discounts = new List<SkillCostDiscount>();
            if (actor.history == null) actor.history = new List<SkillOwnerHistory>();
            if (actor.discountSources == null) actor.discountSources = new List<string>();
            CompanionMechanics.Ensure(actor);
            CrossSubjectSynergies.Ensure(actor);
            // Keep the owner/key/value layout used by existing saves, but restore binary semantics.
            foreach (var resource in actor.resources.Where(x => x.cap == 1 && IsState(x.owner, x.key)))
            {
                resource.isState = true; resource.amount = resource.amount > 0 ? 1 : 0; resource.fraction = 0;
                resource.label = StateName(resource.owner, resource.key, resource.label);
            }
            return actor;
        }
        public static SkillActorState Clone(SkillActorState actor)
        {
            actor = actor ?? new SkillActorState();
            return Ensure(new SkillActorState {
                resources = (actor.resources ?? new List<SkillResource>()).Select(x => new SkillResource { owner=x.owner, key=x.key, label=x.label, amount=x.amount, cap=x.cap, fraction=x.fraction,persistent=x.persistent,isState=x.isState }).ToList(),
                effects = (actor.effects ?? new List<SkillTimedEffect>()).Select(x => new SkillTimedEffect { owner=x.owner, key=x.key, label=x.label, kind=x.kind, sourceCard=x.sourceCard, amount=x.amount, remaining=x.remaining, delay=x.delay, uses=x.uses,persistent=x.persistent,synergyBoosted=x.synergyBoosted,synergyBoostAmount=x.synergyBoostAmount,tacticalEnhanced=x.tacticalEnhanced }).ToList(),
                discounts = (actor.discounts ?? new List<SkillCostDiscount>()).Select(x => new SkillCostDiscount { sourceCard=x.sourceCard, targetCard=x.targetCard, amount=x.amount }).ToList(),
                history = (actor.history ?? new List<SkillOwnerHistory>()).Select(x => new SkillOwnerHistory { owner=x.owner, cardId=x.cardId }).ToList(),
                discountSources = (actor.discountSources ?? new List<string>()).ToList(),
                synergy = CrossSubjectSynergies.Clone(actor.synergy),
                companion = CompanionMechanics.Clone(actor.companion)
            });
        }
        public static int Resource(SkillActorState actor, string owner, string key)
        {
            if (actor == null || actor.resources == null || string.IsNullOrEmpty(key)) return 0;
            var resource = actor.resources.FirstOrDefault(x => x.owner == owner && x.key == key);
            return resource == null ? 0 : Clamp(resource.amount, 0, Clamp(resource.cap, 1, 12));
        }
        public static string TaziaResourceOwner => GameDatabase.Card("tazia_q")?.owner ?? "타지아";
        // The inspector shows E's collection before forging spends it. Read the
        // passed copy's actual gain rules so upgraded copies keep their own values.
        public static int TaziaCollectionAfterE(SkillActorState before, CardDef card)
        {
            int collection=Resource(before,card?.owner,"collection");
            if (card == null || CardInstances.BaseId(card.id) != "tazia_e") return collection;
            foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
                if (rule.op == "gain" && rule.key == "collection" && Condition(rule,before,card,true))
                    collection=Clamp(collection+Math.Max(0,rule.amount),0,Clamp(rule.cap,1,8));
            return collection;
        }
        public static void ShareTaziaResources(SkillActorState skills, SkillActorState legacy) => SharePassiveResources(skills,legacy,"tazia_p");
        // Merge only reviewed identities. Consuming the old entries makes save/load
        // idempotent; unrelated passive meters, counters and effects stay private.
        public static void SharePassiveResources(SkillActorState skills, SkillActorState legacy, string passiveId = null)
        {
            if (skills==null || legacy==null || ReferenceEquals(skills,legacy)) return;
            if (skills.resources==null) skills.resources=new List<SkillResource>();
            if (legacy.resources==null) legacy.resources=new List<SkillResource>();
            foreach (var definition in passiveId==null ? sharedPassiveResources : SharedResourcesFor(passiveId))
            {
                string owner=definition.owner;
                int total=0, count=0; SkillResource only=null; bool old=false;
                foreach (var resource in skills.resources)
                    if (SharedResourceMatch(resource,definition,owner)) { total=Math.Min(definition.cap,total+Clamp(resource.amount,0,definition.cap)); count++; only=resource; }
                foreach (var resource in legacy.resources)
                    if (SharedResourceMatch(resource,definition,owner)) { total=Math.Min(definition.cap,total+Clamp(resource.amount,0,definition.cap)); count++; old=true; }
                if (count==0) continue;
                if (!old && count==1 && only.owner==owner && only.key==definition.skillKey && only.cap==definition.cap
                    && only.amount>=0 && only.amount<=definition.cap && only.fraction==0 && only.isState==definition.isState && !only.persistent) continue;
                skills.resources.RemoveAll(resource=>SharedResourceMatch(resource,definition,owner));
                legacy.resources.RemoveAll(resource=>SharedResourceMatch(resource,definition,owner));
                var merged=SharedResourceCopy(definition,total);
                skills.resources.Add(merged);
            }
        }
        static bool SharedResourceMatch(SkillResource resource, SharedPassiveResourceDefinition definition, string owner) => resource!=null
            && ((resource.owner==owner || resource.owner==definition.subjectId) && resource.key==definition.skillKey
                || resource.owner=="trait:"+definition.passiveId && resource.key==definition.passiveKey);
        static SkillResource SharedResourceCopy(SharedPassiveResourceDefinition definition, int amount) => new SkillResource {
            owner=definition.owner,key=definition.skillKey,label=definition.isState ? StateName(definition.owner,definition.skillKey,definition.label) : definition.label,
            amount=definition.isState ? amount>0 ? 1 : 0 : Clamp(amount,0,definition.cap),cap=definition.cap,isState=definition.isState
        };
        public static void PullSharedPassiveResources(SkillActorState view, SkillActorState shared, string passiveId)
        {
            if (view==null || shared==null || ReferenceEquals(view,shared)) return;
            SharePassiveResources(shared,view,passiveId);
            foreach (var definition in SharedResourcesFor(passiveId))
            {
                if (definition.legacyOnly) continue;
                var current=shared.resources.FirstOrDefault(resource=>resource.owner==definition.owner && resource.key==definition.skillKey);
                if (current==null) continue;
                var copy=SharedResourceCopy(definition,current.amount);
                copy.owner="trait:"+passiveId; copy.key=definition.passiveKey;
                view.resources.Add(copy);
            }
        }
        public static void CommitSharedPassiveResources(SkillActorState view, SkillActorState shared, string passiveId)
        {
            if (view==null || shared==null || ReferenceEquals(view,shared)) return;
            foreach (var definition in SharedResourcesFor(passiveId))
            {
                if (definition.legacyOnly) continue;
                var source=view.resources.FirstOrDefault(resource=>resource.owner=="trait:"+passiveId && resource.key==definition.passiveKey);
                if (source==null) continue;
                var current=shared.resources.FirstOrDefault(resource=>resource.owner==definition.owner && resource.key==definition.skillKey);
                if (current==null) { current=SharedResourceCopy(definition,source.amount); shared.resources.Add(current); }
                else { current.amount=definition.isState ? source.amount>0 ? 1 : 0 : Clamp(source.amount,0,definition.cap); current.cap=definition.cap; current.isState=definition.isState; current.fraction=0; current.persistent=false; }
                view.resources.RemoveAll(resource=>resource.owner=="trait:"+passiveId && resource.key==definition.passiveKey);
            }
        }
        // Resource producers use the common physical cap. Scale caps and explicit
        // set amounts retain their original balance, as do every unrelated rule.
        public static SkillRule SharedPassiveRule(string passiveId, SkillRule rule)
        {
            if (rule==null) return null;
            var definition=SharedResourcesFor(passiveId).FirstOrDefault(x=>!x.legacyOnly && x.passiveKey==rule.key);
            if (definition==null) return rule;
            bool producer=rule.op=="gain" || rule.op=="set" || rule.op=="state_on" || rule.op=="state_off" || rule.op=="state_toggle";
            if (!producer || rule.cap==definition.cap && !definition.passiveStateAsResource) return rule;
            var copy=rule.ShallowCopy();
            copy.cap=definition.cap;
            if (definition.passiveStateAsResource)
            {
                copy.isState=false; copy.label=definition.label;
                if (copy.op=="state_on") copy.op="set";
                else if (copy.op=="state_off") copy.op="consume";
            }
            return copy;
        }
        private static bool Condition(SkillRule rule, SkillActorState actor, CardDef card, bool landed)
        {
            if (rule.onHit && !landed) return false;
            int count = Resource(actor, card.owner, rule.conditionKey);
            if (!string.IsNullOrEmpty(rule.conditionKey) && (rule.conditionExact ? count != rule.conditionAmount : count < rule.conditionAmount)) return false;
            count = Resource(actor, card.owner, rule.conditionKey2);
            if (!string.IsNullOrEmpty(rule.conditionKey2) && (rule.conditionExact2 ? count != rule.conditionAmount2 : count < rule.conditionAmount2)) return false;
            if (!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                var previous = actor.history.FirstOrDefault(x => x.owner == card.owner);
                var last = previous == null ? null : GameDatabase.Card(previous.cardId);
                if (last == null || (last.id != rule.conditionPrevious && last.key != rule.conditionPrevious)) return false;
            }
            return true;
        }
        private static int Amount(SkillRule rule, SkillActorState before, CardDef card, int limit)
        {
            int amount = Math.Max(0, rule.amount);
            if (!string.IsNullOrEmpty(rule.scaleKey)) amount *= Math.Min(Clamp(rule.cap, 1, 8), Resource(before, card.owner, rule.scaleKey));
            return Clamp(amount, 0, limit);
        }
        public static SkillInstantBonus Bonuses(SkillActorState actor, CardDef card, bool landed = true)
        {
            var bonus = new SkillInstantBonus();
            if (actor == null || card == null) return bonus;
            if (card.mechanics != null)
                foreach (var rule in card.mechanics.rules ?? new SkillRule[0])
                {
                    if (!Condition(rule, actor, card, landed)) continue;
                    if (rule.op == "bonus_damage") bonus.damage += Amount(rule, actor, card, 24);
                    if (rule.op == "bonus_block") bonus.block += Amount(rule, actor, card, 24);
                    if (rule.op == "bonus_heal") bonus.heal += Amount(rule, actor, card, 20);
                }
            if (card.category == "basic" && card.key == "ATK") bonus.damage += actor.effects.Where(x => x.kind == "empower_basic").Sum(x => x.amount);
            var tactical=TacticalSkills.Bonuses(actor,card,landed);
            bonus.damage+=tactical.damage;bonus.block+=tactical.block;bonus.heal+=tactical.heal;
            bonus.damage = Clamp(bonus.damage, 0, 24); bonus.block = Clamp(bonus.block, 0, 24); bonus.heal = Clamp(bonus.heal, 0, 20);
            var cross=CrossSubjectSynergies.Bonuses(actor,card,landed);
            bonus.damage+=cross.damage;bonus.block+=cross.block;bonus.heal+=cross.heal;
            return bonus;
        }
        // Shield destruction is evaluated from the state before this card is used,
        // then applied only after a direct hit succeeds and before it absorbs damage.
        public static bool BreaksBlock(SkillActorState actor, CardDef card, bool landed = true)
        {
            return card != null && actor != null && (card.mechanics?.rules ?? new SkillRule[0])
                .Any(rule => rule.op == "break_block" && Condition(rule, actor, card, landed));
        }
        public static int Discount(SkillActorState actor, string cardId)
        {
            return actor == null || actor.discounts == null ? 0 : actor.discounts.Where(x => x.targetCard == cardId).Select(x => Clamp(x.amount, 0, 7)).DefaultIfEmpty(0).Max();
        }
        public static void ConsumeDiscount(SkillActorState actor, string cardId)
        {
            if (actor != null) actor.discounts.RemoveAll(x => x.targetCard == cardId);
        }
        public static void AfterCard(SkillActorState actor, SkillActorState before, CardDef card, bool landed, bool recordHistory = true, int paidCost = -1)
        {
            actor = Ensure(actor);
            if (card.category == "basic" && card.key == "ATK") actor.effects.RemoveAll(x => x.kind == "empower_basic");
            if (card.mechanics != null)
                foreach (var rule in card.mechanics.rules ?? new SkillRule[0])
                {
                    // Only Tazia E completes its newly collected glass in this cast.
                    // Every ordinary rule below still uses the pre-card snapshot.
                    if (rule.op == "forge_sword")
                    {
                        if (CardInstances.BaseId(card.id) != "tazia_e" || !Condition(rule, actor, card, landed)) continue;
                        var sword = actor.resources.FirstOrDefault(x => x.owner == card.owner && x.key == rule.key);
                        if (sword == null) { sword = new SkillResource { owner=card.owner, key=rule.key }; actor.resources.Add(sword); }
                        sword.label=rule.label; sword.cap=1; sword.amount=Clamp(rule.amount, 0, sword.cap);
                        foreach (var collection in actor.resources.Where(x => x.owner == card.owner && x.key == rule.conditionKey)) collection.amount=0;
                        continue;
                    }
                    if (!Condition(rule, before, card, landed)) continue;
                    string key = string.IsNullOrEmpty(rule.key) ? rule.op : rule.key;
                    if (rule.op == "gain" || rule.op == "set" || rule.op == "consume" || rule.op == "state_on" || rule.op == "state_off" || rule.op == "state_toggle")
                    {
                        var resource = actor.resources.FirstOrDefault(x => x.owner == card.owner && x.key == key);
                        if (resource == null) { resource = new SkillResource { owner=card.owner, key=key, label=rule.label ?? key, cap=Clamp(rule.cap, 1, 8) }; actor.resources.Add(resource); }
                        if (rule.op != "consume") resource.cap = rule.isState ? 1 : Clamp(rule.cap, 1, 8);
                        if (!string.IsNullOrEmpty(rule.label)) resource.label = rule.label;
                        resource.isState = rule.isState;
                        if (rule.isState) resource.label = StateName(card.owner, key, resource.label);
                        if (rule.op == "state_on") resource.amount = 1;
                        else if (rule.op == "state_off") resource.amount = 0;
                        else if (rule.op == "state_toggle") resource.amount = Resource(before, card.owner, key) > 0 ? 0 : 1;
                        else if (rule.op == "gain") resource.amount = Clamp(resource.amount + Math.Max(0, rule.amount), 0, resource.cap);
                        else if (rule.op == "set") resource.amount = Clamp(rule.amount, 0, resource.cap);
                        else resource.amount = Math.Max(0, resource.amount - (rule.amount <= 0 ? resource.amount : rule.amount));
                    }
                    else if (rule.op == "discount")
                    {
                        string source = card.id + "|" + rule.targetCard;
                        if (GameDatabase.Card(rule.targetCard) == null || actor.discountSources.Contains(source)) continue;
                        actor.discountSources.Add(source);
                        var discount = actor.discounts.FirstOrDefault(x => x.targetCard == rule.targetCard);
                        if (discount == null) { discount = new SkillCostDiscount { sourceCard=card.id, targetCard=rule.targetCard }; actor.discounts.Add(discount); }
                        discount.amount = Math.Max(discount.amount, Amount(rule, before, card, 7));
                    }
                    else if (rule.op == "clear_effect") actor.effects.RemoveAll(x => x.owner == card.owner && x.key == key);
                    else if (new[] { "bleed", "burn", "delayed_damage", "summon", "hot", "guard", "counter", "empower_basic", "revive" }.Contains(rule.op))
                    {
                        int limit = rule.op == "counter" ? 8 : rule.op == "empower_basic" ? 16 : rule.op == "revive" ? 40 : rule.op == "delayed_damage" ? (TacticalSkills.IsTactical(card)?40:24) : 10;
                        int amount = Amount(rule, before, card, limit);
                        if (amount == 0) continue;
                        var effect = actor.effects.FirstOrDefault(x => x.owner == card.owner && x.kind == rule.op && x.key == key);
                        if (effect == null)
                        {
                            if (actor.effects.Count >= 12) continue;
                            effect = new SkillTimedEffect { owner=card.owner, key=key, kind=rule.op }; actor.effects.Add(effect);
                        }
                        effect.label = rule.label ?? key; effect.sourceCard = card.id;
                        effect.amount = Math.Max(effect.amount, amount);
                        effect.remaining = Math.Max(effect.remaining, Clamp(rule.duration, 1, 3));
                        effect.delay = rule.op == "delayed_damage" ? Clamp(rule.delay, 1, 3) : 0;
                    }
                }
            if (recordHistory) CrossSubjectSynergies.AfterCard(actor,before,card,landed,paidCost<0?card.cost:paidCost);
            TacticalSkills.AfterCard(actor,before,card,landed);
            if (recordHistory && card.category == "skill")
            {
                var history = actor.history.FirstOrDefault(x => x.owner == card.owner);
                if (history == null) { history = new SkillOwnerHistory { owner=card.owner }; actor.history.Add(history); }
                history.cardId = card.id;
            }
        }
        public static void StartTurn(SkillActorState actor)
        {
            CrossSubjectSynergies.StartTurn(actor);
            actor.discountSources.Clear();
            foreach (var effect in actor.effects)
            {
                effect.uses = 0;
                if ((effect.kind == "counter" || effect.kind == "revive") && !effect.persistent) effect.remaining--;
            }
            actor.effects.RemoveAll(x => x.remaining <= 0);
        }
        public static List<SkillPulse> Tick(SkillActorState actor)
        {
            var pulses = new List<SkillPulse>(); int damageBudget = 40, healBudget = 25;
            foreach (var effect in actor.effects.ToArray())
            {
                if (effect.kind == "tactical_buff" || effect.kind == "tactical_pending") continue;
                if (effect.kind == "counter" || effect.kind == "revive" || effect.kind == "empower_basic") continue;
                if (effect.kind == "delayed_damage" && --effect.delay > 0) continue;
                int external = effect.owner?.StartsWith("synergy:")==true ? Math.Max(0,effect.amount)
                    : effect.synergyBoosted ? Clamp(effect.synergyBoostAmount,0,effect.amount) : 0;
                var pulse=new SkillPulse { sourceCard=effect.sourceCard, key=effect.key,kind=effect.kind, amount=effect.amount,externalAmount=external,traitId=effect.owner?.StartsWith("trait:")==true?effect.owner.Substring(6):null };
                LimitScheduledPulse(pulse,ref damageBudget,ref healBudget);
                if (pulse.amount > 0) pulses.Add(pulse);
                if (effect.kind == "delayed_damage" || --effect.remaining <= 0) actor.effects.Remove(effect);
            }
            return pulses;
        }
        // Apply only the original skill/trait budget. Cooperative enhancements
        // remain complete when actor and trait pulses are combined a second time.
        public static void LimitScheduledPulse(SkillPulse pulse,ref int damageBudget,ref int supportBudget)
        {
            int external=Clamp(pulse.externalAmount,0,Math.Max(0,pulse.amount));
            int native=Math.Max(0,pulse.amount-external);
            if(pulse.kind=="hot" || pulse.kind=="guard")
            {
                native=Math.Min(native,Math.Max(0,supportBudget));supportBudget-=native;
            }
            else
            {
                native=Math.Min(native,Math.Max(0,damageBudget));damageBudget-=native;
            }
            pulse.amount=native+external;
        }
        public static bool CounterEveryHit(string sourceCard, string key) => sourceCard == "weapon_tonfa" && key == "spin";
        public static List<SkillPulse> Counters(SkillActorState defender, int landedHits = 1)
        {
            var result = new List<SkillPulse>();
            if (landedHits <= 0) return result;
            foreach (var effect in defender.effects.Where(x => x.kind == "counter" && x.remaining > 0 && (x.uses == 0 || CounterEveryHit(x.sourceCard, x.key))))
            {
                int count = CounterEveryHit(effect.sourceCard, effect.key) ? landedHits : 1;
                // Tonfa ignores the old save's uses value without growing it forever.
                if (!CounterEveryHit(effect.sourceCard, effect.key)) effect.uses++;
                for (int i = 0; i < count; i++)
                    result.Add(new SkillPulse { sourceCard=effect.sourceCard,key=effect.key, kind="counter", amount=Clamp(effect.amount, 0, 8),traitId=effect.owner?.StartsWith("trait:")==true?effect.owner.Substring(6):null });
            }
            return result;
        }
        public static SkillPulse Revive(SkillActorState actor)
        {
            var effect = actor.effects.FirstOrDefault(x => x.kind == "revive" && x.remaining > 0);
            if (effect == null) return null;
            actor.effects.Remove(effect);
            return new SkillPulse { sourceCard=effect.sourceCard, kind="revive", amount=Clamp(effect.amount, 1, 40),traitId=effect.owner?.StartsWith("trait:")==true?effect.owner.Substring(6):null };
        }
        public static List<SkillMechanicToken> Snapshot(SkillActorState actor)
        {
            var result = new List<SkillMechanicToken>();
            if (actor == null) return result;
            result.AddRange(actor.resources.Where(x => x.amount > 0).Select(x => new SkillMechanicToken { owner=x.owner, key=x.key, label=x.isState || (x.cap == 1 && IsState(x.owner,x.key)) ? StateName(x.owner,x.key,x.label) : x.label, kind=x.isState || (x.cap == 1 && IsState(x.owner,x.key)) ? "state" : "resource", amount=x.amount, cap=x.cap,persistent=x.persistent }));
            result.AddRange(actor.effects.Select(x => new SkillMechanicToken { owner=x.owner, key=x.key, label=x.label, kind=x.kind, sourceCard=x.sourceCard, amount=x.amount, remaining=x.remaining, delay=x.delay,persistent=x.persistent }));
            result.AddRange(actor.discounts.Select(x => new SkillMechanicToken { label=SkillReferences.Name(x.targetCard) + " 할인", kind="discount", sourceCard=x.sourceCard, targetCard=x.targetCard, amount=x.amount }));
            result.AddRange(CrossSubjectSynergies.Snapshot(actor));
            return result;
        }
        public static string Summary(SkillActorState actor)
        {
            return string.Join(" · ", Snapshot(actor).Select(x => x.label + (x.kind == "state" ? " · 활성" : x.kind == "resource" ? " " + x.amount + "/" + x.cap : x.kind == "discount" ? " -" + x.amount : " " + x.amount + "(" + (x.delay > 0 ? x.delay : x.remaining) + "턴)")));
        }
        public static string ResourceName(CardDef card, string key)
        {
            var rules = GameDatabase.Cards.Where(x => x.owner == card.owner && x.mechanics != null)
                .SelectMany(x => x.mechanics.rules ?? new SkillRule[0]).Where(r => r.key == key && !string.IsNullOrEmpty(r.label));
            var resource = rules.FirstOrDefault(r => r.op == "gain" || r.op == "set" || r.op.StartsWith("state_", StringComparison.Ordinal) || r.op == "damage_resource") ?? rules.FirstOrDefault();
            if (resource != null) return resource.label;
            return key;
        }
        public static List<string> Describe(CardDef card)
        {
            return DescriptionSummary.GroupEffects(DescriptionEffects(card));
        }
        public static List<ConditionEffect> DescriptionEffects(CardDef card)
        {
            var result = new List<ConditionEffect>();
            if (card == null || card.mechanics == null) return result;
            if (card.id == "garnet_r") result.Add(new ConditionEffect {
                key="garnet_execution|recast|hit",
                condition="이번 턴에 얻은 무료 사용권으로 재사용하고, 적에게 철처녀 표식이 남아 있으며, 사용 전 적의 체력이 최대 체력의 " + FreeCastMechanics.GarnetExecutionPercent + "% 이하이고 공격이 적중하면",
                effect="방어도를 무시하고 적의 남은 체력을 모두 없애 처형합니다",
                note="처형 조건은 재사용 피해를 주기 전 체력으로 판정합니다. "+CardPresentation.WithParticle(SkillReferences.Names(new[]{"garnet_q","garnet_e"}),"가","이")+" 철처녀 표식을 소모했다면 처형되지 않습니다. 보스에게도 같은 체력 비율을 적용하며, 치명상 방지 효과는 기존 규칙대로 적용됩니다." });
            foreach (var rule in card.mechanics.rules ?? new SkillRule[0])
            {
                string label = new[] { "gain", "set", "consume" }.Contains(rule.op) ? ResourceName(card, rule.key ?? rule.op) : string.IsNullOrWhiteSpace(rule.label) ? ResourceName(card, rule.key ?? rule.op) : rule.label;
                string amount = !string.IsNullOrEmpty(rule.scaleKey) ? ResourceName(card, rule.scaleKey) + " × " + rule.amount : rule.amount.ToString();
                if(!string.IsNullOrEmpty(rule.scaleKey))
                {
                    int maximum=rule.op=="bonus_damage" || rule.op=="bonus_block" || rule.op=="delayed_damage"?24:rule.op=="bonus_heal"?20:rule.op=="counter"?8:rule.op=="discount"?7:rule.op=="revive"?40:rule.op=="empower_basic"?16:10;
                    amount+="(최대 "+Math.Min(maximum,rule.amount*rule.cap)+")";
                }
                string text, note = "";
                switch (rule.op)
                {
                    case "gain": text = CardPresentation.WithParticle(label,"를","을") + " " + rule.amount + " 증가시킵니다(최대 " + Clamp(rule.cap, 1, 8) + ")"; break;
                    case "set": text = CardPresentation.WithParticle(label,"를","을") + " " + rule.amount + "로 만듭니다"; break;
                    case "consume": text = CardPresentation.WithParticle(label,"를","을") + " " + (rule.amount <= 0 ? "모두" : rule.amount.ToString()) + " 소모합니다"; break;
                    case "forge_sword": text = "파편 수집을 모두 소모해 유리 검을 " + rule.amount + "로 만듭니다(최대 " + rule.cap + ")"; break;
                    case "state_on": text = StateEnterText(card,rule.key); break;
                    case "state_off": text = StateExitText(card,rule.key); break;
                    case "state_toggle":
                        text = CardPresentation.WithParticle(StateName(card, rule.key),"를","을") + " 전환합니다";
                        note = StateName(card, rule.key) + "가 아니면 " + StateName(card, rule.key) + "로 진입합니다. " + StateName(card, rule.key) + "인 경우에는 " + CardPresentation.WithParticle(StateName(card, rule.key),"를","을") + " 해제합니다.";
                        break;
                    case "bonus_damage": text = "첫 적중에 추가 피해를 " + amount + "만큼 줍니다"; break;
                    case "bonus_block": text = "방어도를 " + amount + "만큼 추가로 얻습니다"; break;
                    case "bonus_heal": text = "체력을 " + amount + "만큼 추가로 회복합니다"; break;
                    case "discount": text = "다음 " + SkillReferences.Name(rule.targetCard,card) + "의 코스트를 " + amount + " 줄이는 할인을 얻습니다(1회, 중첩되지 않음)"; break;
                    case "bleed": case "burn": case "summon": text = "자신의 턴 종료마다 " + amount + "의 피해를 " + Clamp(rule.duration, 1, 3) + "회 주는 " + label + " 효과를 남깁니다"+(rule.op=="summon"?"":"(방어도 무시)"); break;
                    case "hot": text = "자신의 턴 종료마다 체력을 " + amount + "씩 " + Clamp(rule.duration, 1, 3) + "회 회복하는 " + label + " 효과를 얻습니다"; break;
                    case "guard": text = "자신의 턴 종료마다 방어도를 " + amount + "씩 " + Clamp(rule.duration, 1, 3) + "회 얻는 " + label + " 효과를 얻습니다"; break;
                    case "delayed_damage": text = "자신의 턴 종료 " + Clamp(rule.delay, 1, 3) + "회 후 " + amount + "의 피해를 주는 " + CardPresentation.WithParticle(label,"를","을") + " 설치합니다"; break;
                    case "counter": text = Clamp(rule.duration, 1, 3) + "턴 동안 적의 공격에 맞으면 " + amount + "의 피해로 반격하는 효과를 얻습니다" + (CounterEveryHit(card.id, rule.key) ? "(각 직접 적중마다, 횟수 제한 없음)" : "(턴당 1회)"); break;
                    case "break_block": text = "피해를 주기 전에 적의 방어도를 모두 없앱니다"; note = "첫 추격으로 준비한 검거가 적중할 때만 발동합니다. 회피되거나 빗나가면 방어도는 유지되며, 방어도를 없앤 수치는 피해로 계산하지 않습니다."; break;
                    case "empower_basic": text = "다음 기본 공격의 첫 적중 피해를 " + amount + " 늘리는 강화 효과를 얻습니다(1회)"; break;
                    case "revive": text = Clamp(rule.duration, 1, 3) + "턴 동안 치명적인 피해를 1회 막고 체력을 " + amount + " 회복하는 효과를 얻습니다"; break;
                    case "clear_effect": text = label + "의 남은 지속 효과를 회수합니다"; break;
                    default: continue;
                }
                var conditions = new List<string>();
                if (!string.IsNullOrEmpty(rule.conditionKey)) conditions.Add((rule.op == "forge_sword" ? "이번 "+SkillReferences.Name(card)+"의 파편 수집을 마친 직후 " : "") + StateCondition(card, rule.conditionKey, rule.conditionAmount, rule.conditionExact));
                if (!string.IsNullOrEmpty(rule.conditionKey2)) conditions.Add(StateCondition(card, rule.conditionKey2, rule.conditionAmount2, rule.conditionExact2));
                if (!string.IsNullOrEmpty(rule.conditionPrevious)) conditions.Add(card.owner+"의 직전 기술이 " + SkillReferences.Name(rule.conditionPrevious,card) + "이면");
                if (rule.onHit) conditions.Add("공격이 적중하면");
                result.Add(new ConditionEffect { key=DescriptionSummary.ConditionKey(rule), condition=DescriptionSummary.JoinConditions(conditions), effect=text, note=note });
            }
            return result;
        }
        public static string Rules(CardDef card)
        {
            if (card?.mechanics == null) return "";
            if(TacticalSkills.IsDelayed(card))return "준비한 전술 효과는 다음 자기 턴 시작에 한 번 적용됩니다. 시전한 턴의 종료에는 발동하지 않습니다. 효과와 예약한 상태이상은 저장·이어하기에도 유지됩니다.";
            var rules=card.mechanics.rules ?? new SkillRule[0]; var text=new List<string>();
            bool external=CrossSubjectSynergies.FullDescription(card).Any();
            if (rules.Any(r=>r.op=="gain" || r.op=="set" || r.op=="consume" || r.op.StartsWith("state_",StringComparison.Ordinal) || !string.IsNullOrEmpty(r.scaleKey) || !string.IsNullOrEmpty(r.conditionKey) || !string.IsNullOrEmpty(r.conditionKey2)))
            {
                var shared=SharedResourcesFor(StateOwner(card.owner)+"_p").Where(x=>!x.legacyOnly).ToArray();
                text.Add("이 카드의 고유 자원과 상태는 "+card.owner+"의 카드끼리 공유하며 전투가 끝나면 초기화됩니다. 다른 실험체의 고유 자원과 상태로 대신할 수 없습니다."
                    + (shared.Length>0 ? " 패시브의 "+string.Join("·",shared.Select(x=>x.label))+"도 공유하며, 패시브와 기술의 획득·소모는 각각 적용됩니다." : "")
                    + (card.owner==TaziaResourceOwner ? " 유리 검은 "+CardPresentation.WithParticle(SkillReferences.Name("tazia_q"),"가","이")+" 소모하며, 빗나가도 소모됩니다." : ""));
            }
            if (rules.Any(r=>r.op=="discount")) text.Add("할인은 다음 대상 카드 1회에 적용되며, 같은 카드의 할인 부여는 자신의 한 턴에 1회로 제한됩니다.");
            else if (rules.Any(r=>new[] {"bleed","burn","delayed_damage","summon","hot","guard"}.Contains(r.op))) text.Add("지속 효과는 시전자의 턴 종료에 적용되며, 같은 설치물을 다시 놓으면 수치와 남은 횟수의 큰 값을 유지합니다."+(external?"":" 실험체 한 명의 기술·패시브·룬을 합친 턴 종료 효과는 피해 40, 회복과 방어 합계 25까지 적용됩니다."));
            else if (rules.Any(r=>r.op=="counter" || r.op=="revive")) text.Add("반격과 치명상 방지의 남은 턴은 자신의 다음 턴 시작부터 감소합니다.");
            if(rules.Count(r=>r.op=="bonus_damage")>1 || rules.Any(r=>r.op=="bonus_damage" && !string.IsNullOrEmpty(r.scaleKey))) text.Add(external?"추가 피해는 다중 적중에서도 첫 적중 1회에 적용됩니다.":"카드 1회의 추가 피해는 최대 24이며 다중 적중에서는 첫 적중 1회에 적용됩니다.");
            return string.Join("\n",text.Take(2));
        }
        public static string Summary(CardDef card)
        {
            return card == null ? "" : DescriptionSummary.Card(card, GameDatabase.Cards);
        }
    }
}
