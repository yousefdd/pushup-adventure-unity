using System;

namespace DiceQuest.Core
{
    /// <summary>A seeded source of numbers in [0, 1), so a run can be replayed.</summary>
    public class Rng
    {
        readonly Random _r;
        public Rng(int seed) { _r = new Random(seed); }
        public virtual double Next() => _r.NextDouble();

        /// <summary>A whole number from min to max, both included.</summary>
        public int Range(int min, int max) => min + (int)Math.Floor(Next() * (max - min + 1));
    }
}
