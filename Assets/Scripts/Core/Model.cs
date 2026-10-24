// The worlds and their creatures. Pure C# with no UnityEngine, so the rules
// compile and test anywhere (see Assets/Tests).
using System;

namespace DiceQuest.Core
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
}
