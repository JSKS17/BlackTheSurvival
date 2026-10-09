using System;
using System.Collections.Generic;

namespace Lumia
{
    public enum ZoneKind { Wildlife, Subject, Kiosk, Campfire, Encounter, Boss }
    public enum GearSlot { Weapon, Clothes, Head, Arm, Legs }
    public enum RunStage { Preparation, Map, Combat, Rewards, Kiosk, Campfire, Encounter, PassiveChoice, Won, Lost }
    [Serializable] public class CardDef
    {
        public string id, name, owner, key, description, category = "skill";
        public float cooldown;
        public int cost = 1, damage, block, heal, draw, energy, poison, vulnerable, weak, strength;
        public int evasion, duration = 1, hits = 1;
        public bool exhaust;
        public string[] freeCastTargets = new string[0];
        public int freeCastCount;
        public bool freeCastOnHit;
        public bool freeCastLastSkill;
        public string effect, effectColor;
        public int effectVariant;
        public SkillMechanicProfile mechanics;
        public CardStatusRule[] statuses = new CardStatusRule[0];
        public bool movement;
    }
    [Serializable] public class PassiveDef
    {
        public string id, name, owner, description, trigger;
        public int amount;
        public TraitMechanicProfile mechanics;
    }
    [Serializable] public class RuneDef
    {
        public string id, name, tree, description, trigger;
        public bool main;
        public int amount;
        public TraitMechanicProfile mechanics;
    }
    [Serializable] public class GearDef
    {
        public string id, name, objectId, rarity = "전설", weaponClass, cardId, description, effect;
        public GearSlot slot;
        public int attack, block, health, evasion, amount;
        public int controlResistance, damageDeferral;
        public int critChance;
        public string[] optionTags = new string[0];
        public TraitMechanicProfile mechanics;
    }
    [Serializable] public class FoodDef
    {
        public string id, name, description, upgradeTo;
        public int price, heal;
        public bool fullHeal;
    }
    [Serializable] public class ObjectDef
    {
        public string id, name, description;
        public int price;
    }
    [Serializable] public class EventOption
    {
        public string label, description, effect, cardId, objectId, passiveId;
        public int amount;
    }
    [Serializable] public class EventDef
    {
        public string id, owner, title, story;
        public EventOption[] options;
    }
    [Serializable] public class CharacterDef
    {
        public string id, name, passiveId;
        public string[] cards;
        public string[] weaponClasses = new string[0];
    }
}
