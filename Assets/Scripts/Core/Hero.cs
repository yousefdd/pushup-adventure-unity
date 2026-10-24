using System;
using System.Collections.Generic;

namespace PushupAdventure.Core
{
    /// <summary>A seeded source of numbers in [0, 1), so a run can be replayed.</summary>
    public class Rng
    {
        readonly Random _r;
        public Rng(int seed) { _r = new Random(seed); }
        public virtual double Next() => _r.NextDouble();
    }

    public sealed class Hero
    {
        public int Lv, Xp, Hp, Max, Atk, Def;
        public readonly string Cls;

        /// <summary>Skill key → how many times it was picked (stacks).</summary>
        public readonly Dictionary<string, int> Sk = new Dictionary<string, int>();

        public Hero(int lv, int max, int atk, int def, string cls)
        {
            Lv = lv; Max = max; Hp = max; Atk = atk; Def = def; Cls = cls;
        }

        /// <summary>A fresh hero at [level] with the [cls] perk applied.</summary>
        public static Hero Start(int level, string cls)
        {
            var c = Content.ClassById(cls);
            return new Hero(level,
                (int)Math.Round((Rules.BaseHp + (level - 1) * 15) * c.Power),
                (int)Math.Round((Rules.BaseAtk + (level - 1) * 3) * c.Power),
                Rules.BaseDef, c.Id);
        }

        public HeroClass Class => Content.ClassById(Cls);

        /// <summary>Strikes needed to charge the special.</summary>
        public int SpecialCost => 8;

        public int Stacks(string key)
        {
            int n;
            return Sk.TryGetValue(key, out n) ? n : 0;
        }
    }
}
