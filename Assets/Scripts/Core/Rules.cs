using System;
using System.Collections.Generic;
using System.Linq;

namespace PushupAdventure.Core
{
    public sealed class Foe
    {
        public readonly string Name, Img, Quote;
        public readonly int Hp, Atk, Xp;
        public readonly bool Elite, Boss, WindsUp, Front;
        public readonly FoeAbility Ability;

        public Foe(string name, string img, int hp, int atk, bool elite, bool boss, string quote, bool windsUp,
            FoeAbility ability, bool front, int xp)
        {
            Name = name; Img = img; Hp = hp; Atk = atk; Elite = elite; Boss = boss; Quote = quote;
            WindsUp = windsUp; Ability = ability; Front = front; Xp = xp;
        }
    }

    /// <summary>Numbers and the small rules: growth, healing, skills, loot, XP, foes.</summary>
    public static class Rules
    {
        public const int BaseHp = 120, BaseAtk = 14, BaseDef = 2;
        public const int FoeHpBase = 85, FoeAtk = 21;
        public const double BossHpScale = 4.0, BossAtkScale = 1.5;

        /// <summary>Strikes a world's first foe takes from a hero of that world's
        /// level, and the same for its boss: nothing dies in one hit.</summary>
        public const double FoeHits = 6.0, BossHits = 55.0;
        public const int XpPerLevel = 60;

        /// <summary>Seconds between a foe's own attacks.</summary>
        public const double PaceNormal = 2.6, PaceElite = 2.3, PaceBoss = 1.8;

        public static double AtkRef(ChapterDef ch) => BaseAtk + 3 * (ch.StartLevel + 1);

        public static void Grow(Hero h, string stat, double pct)
        {
            if (stat == "hp")
            {
                var add = (int)Math.Round(h.Max * pct);
                h.Max += add;
                h.Hp += add;
            }
            else
            {
                h.Atk += Math.Max(1, (int)Math.Round(h.Atk * pct));
            }
        }

        public static int Heal(Hero h, double pct) => HealBy(h, (int)Math.Round(h.Max * pct));

        public static int HealBy(Hero h, int n)
        {
            var before = h.Hp;
            h.Hp = Math.Min(h.Max, h.Hp + n);
            return h.Hp - before;
        }

        public static void ApplySkill(Hero h, string key)
        {
            h.Sk[key] = h.Stacks(key) + 1;
            if (key == "edge") Grow(h, "atk", 0.20);
            if (key == "hide") Grow(h, "hp", 0.30);
            if (key == "guard") h.Def += 4;
        }

        /// <summary>Three different skills, uniformly shuffled.</summary>
        public static List<string> SkillOffer(Rng rng)
        {
            var keys = Content.Skills.Keys.ToList();
            for (var i = keys.Count - 1; i > 0; i--)
            {
                var j = (int)Math.Floor(rng.Next() * (i + 1));
                var t = keys[i];
                keys[i] = keys[j];
                keys[j] = t;
            }
            return keys.Take(3).ToList();
        }

        public static Loot RollLoot(Rng rng, double luck = 0)
        {
            // Luck pulls the roll toward the rare end.
            var r = 1 - (1 - rng.Next()) * (1 - luck);
            var tier = r < 0.5 ? 0 : r < 0.8 ? 2 : r < 0.95 ? 4 : 6;
            return Content.LootTable[tier + (rng.Next() < 0.5 ? 0 : 1)];
        }

        public static void Equip(Hero h, Loot item)
        {
            switch (item.Stat)
            {
                case "hp": h.Max += item.V; h.Hp += item.V; break;
                case "atk": h.Atk += item.V; break;
                default: h.Def += item.V; break;
            }
        }

        /// <summary>Adds XP; returns how many levels were gained.</summary>
        public static int GainXp(Hero h, int xp)
        {
            h.Xp += (int)Math.Round(xp * h.Class.Xp);
            var ups = 0;
            while (h.Xp >= XpPerLevel)
            {
                h.Xp -= XpPerLevel;
                h.Lv++;
                ups++;
                h.Max += 15;
                h.Hp += 15;
                h.Atk += 3;
            }
            return ups;
        }

        /// <summary>The foe for a road tag: "boss", "2" or "2+" (an elite).</summary>
        public static Foe FoeFor(string tag, ChapterDef ch)
        {
            if (tag == "boss")
            {
                var b = ch.Boss;
                return new Foe(b.Name, b.Img, (int)Math.Round(BossHits * AtkRef(ch)),
                    (int)Math.Round(FoeAtk * b.M * BossAtkScale), false, true, b.Quote, true, b.Ability, b.Front,
                    (int)Math.Round(FoeHpBase * b.M * BossHpScale / 3));
            }
            var plus = tag.EndsWith("+");
            var f = ch.Foes[int.Parse(tag.Replace("+", ""))];
            var m = f.M * (plus ? 1.35 : 1);
            var step = m / ch.Foes[0].M;
            return new Foe(f.Name + (plus ? " ★" : ""), f.Img, (int)Math.Round(FoeHits * AtkRef(ch) * step),
                (int)Math.Round(FoeAtk * m), plus, false, f.Quote, false, f.Ability, f.Front,
                (int)Math.Round(FoeHpBase * m / 3));
        }
    }
}
