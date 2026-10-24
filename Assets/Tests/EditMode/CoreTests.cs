using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PushupAdventure.Core;

namespace PushupAdventure.Tests
{
    public class CoreTests
    {
        static Battle FightWith(FoeAbility a, double rngValue = 0.5)
        {
            foreach (var ch in WorldData.Chapters)
            {
                for (var i = 0; i < ch.Foes.Length; i++)
                {
                    if (ch.Foes[i].Ability != a) continue;
                    return new Battle(Hero.Start(ch.StartLevel, "knight"), Rules.FoeFor(i.ToString(), ch), new FixedRng(rngValue));
                }
            }
            Assert.Fail("no foe with " + a);
            return null;
        }

        /// <summary>Plays the foe's clock up to its first ability turn.</summary>
        static List<BattleEvent> ToAbility(Battle b)
        {
            var o = new List<BattleEvent>();
            while (b.Attacks < Abilities.Every && !b.Over)
            {
                o.AddRange(b.Advance(0.05));
                if (b.Windup) b.WindupLeft = 0;
            }
            return o;
        }

        [Test]
        public void FifteenWorldsNinetyFoesAndEveryCreatureHasAnAbility()
        {
            Assert.AreEqual(15, WorldData.Chapters.Length);
            var all = WorldData.Chapters.SelectMany(c => c.Foes.Concat(new[] { c.Boss })).ToList();
            Assert.AreEqual(90, all.Count);
            Assert.IsFalse(all.Any(f => f.Ability == FoeAbility.None));
            Assert.AreEqual(8, all.Select(f => f.Ability).Distinct().Count());
        }

        [Test]
        public void NoFoeFallsToOneBlow()
        {
            foreach (var ch in WorldData.Chapters)
            {
                var h = Hero.Start(ch.StartLevel + 4, "berserker");
                for (var i = 0; i < 5; i++)
                    Assert.Greater(Rules.FoeFor(i.ToString(), ch).Hp, h.Atk * 3, ch.Name + " foe " + i);
                Assert.Greater(Rules.FoeFor("boss", ch).Hp, h.Atk * 25, ch.Name + " boss");
            }
        }

        [Test]
        public void EachAbilityDoesItsJob()
        {
            var regen = FightWith(FoeAbility.Regen);
            regen.St.Hp = regen.Foe.Hp / 2;
            ToAbility(regen);
            Assert.Greater(regen.St.Hp, regen.Foe.Hp / 2);

            var stone = FightWith(FoeAbility.StoneSkin);
            ToAbility(stone);
            Assert.AreEqual(3, stone.St.Shield);
            var strike = (StrikeEvent)stone.Rep()[0];
            Assert.Less(strike.Blows[0].Dmg, stone.Hero.Atk);

            var hex = FightWith(FoeAbility.Hex);
            hex.Charge = 6;
            ToAbility(hex);
            Assert.AreEqual(3, hex.Charge);

            var venom = FightWith(FoeAbility.Venom);
            ToAbility(venom);
            var hp = venom.Hero.Hp;
            var ticks = new List<BattleEvent>();
            for (var i = 0; i < 20; i++) ticks.AddRange(venom.Advance(0.1));
            Assert.IsTrue(ticks.OfType<FoeAbilityEvent>().Any(e => e.Tick));
            Assert.Less(venom.Hero.Hp, hp);

            var drain = FightWith(FoeAbility.Drain);
            drain.St.Hp = drain.Foe.Hp / 2;
            Assert.Greater(ToAbility(drain).OfType<FoeHitEvent>().First().Drained, 0);

            var frenzy = FightWith(FoeAbility.Frenzy);
            var calm = frenzy.St.Pace;
            frenzy.St.Hp = frenzy.Foe.Hp / 3;
            Assert.Less(frenzy.St.Pace, calm);

            var dodge = FightWith(FoeAbility.Evasive, 0.01);
            Assert.IsTrue(((StrikeEvent)dodge.Rep()[0]).Blows[0].Miss);
        }

        [Test]
        public void ThreeQuickPushupsStaggerAHeavyBlow()
        {
            var ch = WorldData.Chapters[0];
            var b = new Battle(Hero.Start(ch.StartLevel, "ranger"), Rules.FoeFor("boss", ch), new FixedRng(0.9));
            var events = new List<BattleEvent>();
            while (!b.Windup) events.AddRange(b.Advance(0.05));
            Assert.IsInstanceOf<WindupEvent>(events.Last());
            b.Rep();
            b.Rep();
            Assert.IsTrue(b.Rep().OfType<InterruptEvent>().Any());
            Assert.IsFalse(b.Windup);
        }

        [Test]
        public void AWholeRunEndsInVictoryOrDefeatAndVisitsEveryMode()
        {
            foreach (var cls in Content.Classes.Select(c => c.Id))
            {
                var run = new RunController(WorldData.Chapters[0], cls, 7);
                var seen = new HashSet<RunMode>();
                for (var i = 0; i < 20000 && !run.Finished; i++)
                {
                    seen.Add(run.Mode);
                    if (run.Mode == RunMode.Busy) run.Tick(0.2);
                    else
                    {
                        run.Rep();
                        run.Tick(0.4);
                    }
                }
                Assert.IsTrue(run.Finished, cls);
                CollectionAssert.IsSupersetOf(seen, new[] { RunMode.Walk, RunMode.Fight, RunMode.Pick, RunMode.Card });
                Assert.Greater(run.Log.Count, 20);
            }
        }

        [Test]
        public void TheWheelWaitsForAPushupOrSpinsItself()
        {
            var a = new RunController(WorldData.Chapters[0], "knight", 3);
            for (var i = 0; i < 2000 && a.Mode != RunMode.Spin && !a.Finished; i++)
            {
                if (a.Mode == RunMode.Busy) a.Tick(0.3);
                else { a.Rep(); a.Tick(0.05); }
            }
            Assert.AreEqual(RunMode.Spin, a.Mode);
            a.Tick(RunController.SpinAuto - 1);
            Assert.IsFalse(a.Overlay.Spinning);
            a.Rep();
            Assert.IsTrue(a.Overlay.Spinning);
            Assert.AreEqual(RunMode.Busy, a.Mode);
        }

        [Test]
        public void DayCountsReadNaturally()
        {
            Loc.Arabic = false;
            Assert.AreEqual("1 day", Loc.Days(1));
            Assert.AreEqual("39 days", Loc.Days(39));
            Loc.Arabic = true;
            Assert.AreEqual("يوم واحد", Loc.Days(1));
            Assert.AreEqual("يومان", Loc.Days(2));
            Assert.AreEqual("7 أيام", Loc.Days(7));
            Assert.AreEqual("30 يومًا", Loc.Days(30));
            Assert.AreEqual("الفصل الثاني عشر", Loc.Chapter(12, "Chapter XII"));
            Loc.Arabic = false;
            Assert.AreEqual("Chapter XII", Loc.Chapter(12, "Chapter XII"));
        }

        [Test]
        public void ArabicLettersJoinAndReadRightToLeft()
        {
            // باب: initial beh, final alef, isolated beh (alef never joins left).
            Assert.AreEqual("ﺑﺎﺏ", ArabicShaper.Shape("باب"));
            // لا is one ligature.
            Assert.AreEqual("ﻻ", ArabicShaper.Shape("لا"));
            // Marks are dropped; numbers and Latin keep their order in the visual line.
            Assert.AreEqual(ArabicShaper.Shape("كَ"), ArabicShaper.Shape("ك"));
            // A keyword mark between letters does not break the join.
            Assert.AreEqual("\uFE91" + ArabicShaper.KeyOpen + "\uFE8E\uFE8F",
                ArabicShaper.Shape("ب" + ArabicShaper.KeyOpen + "اب"));
            var line = ArabicShaper.Line("اليوم 12");
            Assert.IsTrue(line.StartsWith("12 "), line);
            Assert.IsTrue(ArabicShaper.Line("ATK +12%").Equals("ATK +12%"));
        }

        [Test]
        public void EveryStringTheGameShowsHasArabic()
        {
            var missing = new List<string>();
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            var trCall = new Regex("Loc\\.Tr\\(\"((?:[^\"\\\\]|\\\\.)*)\"");
            foreach (var f in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (f.EndsWith("LocAr.cs")) continue;
                foreach (Match m in trCall.Matches(File.ReadAllText(f)))
                {
                    var key = Regex.Unescape(m.Groups[1].Value);
                    if (!Loc.Has(key)) missing.Add(Path.GetFileName(f) + ": " + key);
                }
            }
            var content = new List<string>();
            content.AddRange(Content.Skills.Values.SelectMany(s => new[] { s.Name, s.Text }));
            content.AddRange(Content.Stories.SelectMany(s => new[] { s.Text, s.Effect.Tag }));
            content.AddRange(Content.Wheel.Select(w => w.Label));
            content.AddRange(Content.Choices.SelectMany(c => new[] { c.Text, c.A.Label, c.B.Label, c.A.Effect.Tag, c.B.Effect.Tag }));
            content.AddRange(Content.LootTable.Select(l => l.Name));
            content.AddRange(Content.Classes.SelectMany(c => new[] { c.Name, c.Special }));
            content.AddRange(WorldData.Chapters.SelectMany(c => new[] { c.Name, c.Blurb }
                .Concat(c.Foes.Concat(new[] { c.Boss }).SelectMany(f => new[] { f.Name, f.Quote }))));
            content.AddRange(System.Enum.GetValues(typeof(FoeAbility)).Cast<FoeAbility>()
                .Where(a => a != FoeAbility.None).Select(Abilities.Label));
            missing.AddRange(content.Where(s => !Loc.Has(s)).Select(s => "content: " + s));
            Assert.IsEmpty(missing, string.Join("\n", missing.Distinct()));
        }
    }

    /// <summary>Always the same number: makes a roll's outcome certain in a test.</summary>
    sealed class FixedRng : Rng
    {
        readonly double _v;
        public FixedRng(double v) : base(0) { _v = v; }
        public override double Next() => _v;
    }
}
