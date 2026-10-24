// The game's data: creatures, worlds, classes, skills, events. Pure C# with no
// UnityEngine, so the rules compile and test anywhere (see Assets/Tests).
using System;

namespace PushupAdventure.Core
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

    /// <summary>Every creature type fights with one trick of its own. Active
    /// ones fire on every fourth attack; passive ones always hold.</summary>
    public enum FoeAbility { None, Regen, StoneSkin, Frenzy, Drain, Venom, Breath, Hex, Evasive }

    public static class Abilities
    {
        /// <summary>Attacks between a foe's ability turns.</summary>
        public const int Every = 4;

        public static bool IsActive(FoeAbility a) =>
            a == FoeAbility.Regen || a == FoeAbility.StoneSkin || a == FoeAbility.Venom ||
            a == FoeAbility.Breath || a == FoeAbility.Hex;

        public static string Label(FoeAbility a)
        {
            switch (a)
            {
                case FoeAbility.Regen: return "Regrowth";
                case FoeAbility.StoneSkin: return "Stone Skin";
                case FoeAbility.Frenzy: return "Frenzy";
                case FoeAbility.Drain: return "Life Drain";
                case FoeAbility.Venom: return "Venom";
                case FoeAbility.Breath: return "Fire Breath";
                case FoeAbility.Hex: return "Hex";
                case FoeAbility.Evasive: return "Evasive";
                default: return "";
            }
        }
    }

    public sealed class FoeKind
    {
        public readonly string Name, Img, Quote;

        /// <summary>Toughness: HP and attack scale with it.</summary>
        public readonly float M;
        public readonly FoeAbility Ability;

        /// <summary>Drawn facing the viewer; turned a little toward the hero.</summary>
        public readonly bool Front;

        public FoeKind(string name, string img, float m, string quote, FoeAbility ability, bool front)
        {
            Name = name; Img = img; M = m; Quote = quote; Ability = ability; Front = front;
        }
    }

    public sealed class ChapterDef
    {
        public readonly int Number, StartLevel, Ground;
        public readonly string Id, Roman, Name, Blurb;
        public readonly FoeKind[] Foes;
        public readonly FoeKind Boss;

        public ChapterDef(int number, string id, string roman, string name, string blurb, int startLevel, int ground,
            FoeKind[] foes, FoeKind boss)
        {
            Number = number; Id = id; Roman = roman; Name = name; Blurb = blurb; StartLevel = startLevel;
            Ground = ground; Foes = foes; Boss = boss;
        }

        public string Far => "bg_" + Id + "_far";
        public string Near => "bg_" + Id + "_near";
    }

    public sealed class HeroClass
    {
        public readonly string Id, Name, Special, SpecialText;
        public readonly double CritChance, CritMult, Power, Xp, LootLuck;

        public HeroClass(string id, string name, string special, string specialText, double critChance = 0,
            double critMult = 2, double power = 1, double xp = 1, double lootLuck = 0)
        {
            Id = id; Name = name; Special = special; SpecialText = specialText; CritChance = critChance;
            CritMult = critMult; Power = power; Xp = xp; LootLuck = lootLuck;
        }

        public string Sprite => "hero_" + Id;
    }

    public sealed class Skill
    {
        public readonly string Key, Name, Text;
        public readonly Rarity Rarity;

        public Skill(string key, string name, string text, Rarity rarity = Rarity.Common)
        {
            Key = key; Name = name; Text = text; Rarity = rarity;
        }
    }

    public sealed class Effect
    {
        public readonly string Tag;
        public readonly Action<Hero> Apply;

        public Effect(string tag, Action<Hero> apply) { Tag = tag; Apply = apply; }
    }

    public sealed class Story
    {
        public readonly string Text;
        public readonly Effect Effect;

        public Story(string text, Effect effect) { Text = text; Effect = effect; }
    }

    public sealed class WheelSlice
    {
        public readonly string Label;
        public readonly Action<Hero> Apply;
        public readonly int Xp;
        public readonly bool Skill;

        public WheelSlice(string label, Action<Hero> apply = null, int xp = 0, bool skill = false)
        {
            Label = label; Apply = apply; Xp = xp; Skill = skill;
        }
    }

    public sealed class ChoiceOption
    {
        public readonly string Label;
        public readonly Effect Effect;

        public ChoiceOption(string label, Effect effect) { Label = label; Effect = effect; }
    }

    public sealed class Choice
    {
        public readonly string Text;
        public readonly ChoiceOption A, B;

        public Choice(string text, ChoiceOption a, ChoiceOption b) { Text = text; A = a; B = b; }
    }

    public sealed class Loot
    {
        public readonly Rarity Rarity;
        public readonly string Img, Name, Stat;
        public readonly int V;

        public Loot(Rarity rarity, string img, string name, string stat, int v)
        {
            Rarity = rarity; Img = img; Name = name; Stat = stat; V = v;
        }
    }
}
