using System;
using System.Collections.Generic;
using System.Linq;

namespace PushupAdventure.Core
{
    /// <summary>Classes, skills, stories, the wheel, choices, loot and the road.</summary>
    public static class Content
    {
        public static readonly HeroClass[] Classes =
        {
            new HeroClass("knight", "Knight", "Shield Bash", "2.5× hit, stuns: the foe's clock and any heavy blow are reset",
                power: 1.05),
            new HeroClass("ranger", "Ranger", "Arrow Rain", "Five arrows at 0.7× each, every one can crit",
                critChance: 0.06),
            new HeroClass("monk", "Monk", "Palm of Dawn", "3× hit and heal 15% of max HP", xp: 1.03, lootLuck: 0.25),
            new HeroClass("berserker", "Berserker", "Blood Frenzy", "Three 1.4× hits; costs 5% of current HP",
                critChance: 0.03, critMult: 2.5),
        };

        public static HeroClass ClassById(string id) => Classes.FirstOrDefault(c => c.Id == id) ?? Classes[0];

        public static readonly Dictionary<string, Skill> Skills = new Dictionary<string, Skill>
        {
            { "edge", new Skill("edge", "Ember Edge", "+20% attack") },
            { "hide", new Skill("hide", "Iron Hide", "+30% max HP, healed now") },
            { "guard", new Skill("guard", "Stone Guard", "+4 defence") },
            { "eye", new Skill("eye", "Keen Eye", "+15% chance to deal double damage", Rarity.Rare) },
            { "twin", new Skill("twin", "Twin Strike", "+25% chance to strike twice", Rarity.Rare) },
            { "leech", new Skill("leech", "Leech Fang", "Heal 15% of the damage you deal", Rarity.Rare) },
            { "thorns", new Skill("thorns", "Thornmail", "Attackers take 30% of the damage they deal", Rarity.Rare) },
            { "spark", new Skill("spark", "First Spark", "Your first hit each fight deals 3×", Rarity.Epic) },
            { "wind", new Skill("wind", "Second Wind", "Heal 20% after every win", Rarity.Epic) },
        };

        public static readonly Story[] Stories =
        {
            new Story("A meadow sprite hums as you pass. You feel lighter on your feet.",
                new Effect("Max HP +6%", h => Rules.Grow(h, "hp", 0.06))),
            new Story("You share your bread with a lost traveller. They press a whetstone into your hand.",
                new Effect("ATK +4", h => h.Atk += 4)),
            new Story("Pip sniffs out a patch of glowberries.", new Effect("Heal 20%", h => Rules.Heal(h, 0.2))),
            new Story("An old shrine hums softly. Its stones settle into your boots.", new Effect("DEF +2", h => h.Def += 2)),
            new Story("You trip over a root. Pip jiggles, trying not to laugh.",
                new Effect("HP −8", h => h.Hp = Math.Max(1, h.Hp - 8))),
            new Story("A farmer shows you how to swing from the hips.", new Effect("ATK +5%", h => Rules.Grow(h, "atk", 0.05))),
        };

        /// <summary>Lucky wheel, clockwise.</summary>
        public static readonly WheelSlice[] Wheel =
        {
            new WheelSlice("Max HP +10%", h => Rules.Grow(h, "hp", 0.10)),
            new WheelSlice("Learn a skill", skill: true),
            new WheelSlice("EXP +30", xp: 30),
            new WheelSlice("Heal 30%", h => Rules.Heal(h, 0.3)),
            new WheelSlice("ATK +10%", h => Rules.Grow(h, "atk", 0.10)),
            new WheelSlice("EXP +60", xp: 60),
        };

        public static readonly Choice[] Choices =
        {
            new Choice("You reach a campfire at dusk. Rest, or drill until dark?",
                new ChoiceOption("Rest", new Effect("Heal 30%", h => Rules.Heal(h, 0.3))),
                new ChoiceOption("Train", new Effect("ATK +12%", h => Rules.Grow(h, "atk", 0.12)))),
            new Choice("A hedge witch offers a charm, or a bowl of hot stew.",
                new ChoiceOption("Stew", new Effect("Heal 40%", h => Rules.Heal(h, 0.4))),
                new ChoiceOption("Charm", new Effect("DEF +3", h => h.Def += 3))),
        };

        public static readonly Loot[] LootTable =
        {
            new Loot(Rarity.Common, "gear_sandals", "Ember Boots", "hp", 15),
            new Loot(Rarity.Common, "gear_fang", "Sun Charm", "atk", 3),
            new Loot(Rarity.Uncommon, "gear_axe", "Bloodrune Axe", "atk", 5),
            new Loot(Rarity.Uncommon, "gear_hood", "Gilded Helm", "def", 2),
            new Loot(Rarity.Rare, "gear_plate", "Dragon Aegis", "hp", 40),
            new Loot(Rarity.Rare, "gear_sword", "Amethyst Blade", "atk", 8),
            new Loot(Rarity.Epic, "gear_greaves", "Shadow Boots", "def", 5),
            new Loot(Rarity.Epic, "gear_star", "Star Amulet", "atk", 12),
        };

        /// <summary>Every chapter's road: 30 days, the boss on the last one.</summary>
        public static readonly string[] Road =
        {
            "story", "foe:0", "skill", "story", "foe:1", "wheel", "skill", "foe:2", "choice", "chest",
            "foe:3", "skill", "story", "foe:4", "wheel", "choice", "skill", "foe:0+", "chest", "foe:1+",
            "skill", "story", "foe:2+", "wheel", "choice", "foe:3+", "skill", "foe:4+", "choice", "boss",
        };
    }
}
