using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DiceQuest.Core;
using NUnit.Framework;

namespace DiceQuest.Tests
{
    public class CoreTests
    {
        /// <summary>Plays a whole run the simple way: roll, and in a shop heal
        /// when low, else buy what makes the hero stronger.</summary>
        public static DiceRun Play(string hero, int seed, int maxRolls = 2000)
        {
            var run = new DiceRun(hero, seed);
            while (!run.Over && run.Rolls < maxRolls)
            {
                var o = run.Land(run.Roll());
                if (o.Kind != OutcomeKind.Shop) continue;
                var h = run.Hero;
                foreach (var i in o.Shop.OrderBy(i => IsHeal(i) ? (h.Hp * 2 < h.Max ? 0 : 2) : 1))
                {
                    if (IsHeal(i) && h.Hp * 10 > h.Max * 7) continue;
                    run.Buy(i);
                }
            }
            return run;
        }

        static bool IsHeal(ShopItem i) => i.Key == "potion" || i.Key == "elixir";

        [Test]
        public void TheBoardHasEveryKindOfTileAndCampFirst()
        {
            var b = DiceRun.MakeBoard(new Rng(1));
            Assert.AreEqual(DiceRun.Size, b.Length);
            Assert.AreEqual(TileKind.Start, b[0]);
            Assert.AreEqual(9, b.Count(t => t == TileKind.Enemy));
            Assert.AreEqual(2, b.Count(t => t == TileKind.Shop));
            Assert.IsFalse(b.Skip(1).Take(3).Contains(TileKind.Elite));
        }

        [Test]
        public void ARollWalksThatManyTilesAndCampPaysOnTheWay()
        {
            var run = new DiceRun("knight", 3);
            var m = run.Roll(4);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, m.Path);
            Assert.AreEqual(4, run.Pos);
            run.Pos = 22;
            var gold = run.Hero.Gold;
            m = run.Roll(3);
            CollectionAssert.AreEqual(new[] { 23, 0, 1 }, m.Path);
            Assert.AreEqual(2, run.Lap);
            Assert.Greater(run.Hero.Gold, gold);
            Assert.IsFalse(m.Boss);
        }

        [Test]
        public void AfterTheLastLapTheBossStopsTheWalkAtCamp()
        {
            var run = new DiceRun("knight", 3) { Lap = DiceRun.LapsPerStage, Pos = 22 };
            var m = run.Roll(5);
            Assert.IsTrue(m.Boss);
            Assert.AreEqual(0, run.Pos);
            CollectionAssert.AreEqual(new[] { 23, 0 }, m.Path);
        }

        [Test]
        public void AFightIsPlayedOutBlowByBlow()
        {
            var run = new DiceRun("ranger", 9);
            var foe = run.MakeFoe(run.Chapter.Foes[0], false, false);
            var f = run.Simulate(foe);
            Assert.IsTrue(f.Won);
            Assert.AreEqual(0, f.Beats.Last().FoeHp);
            Assert.IsTrue(f.Beats.Zip(f.Beats.Skip(1), (a, b) => a.T <= b.T).All(x => x));
            Assert.Greater(f.Beats.Count(b => b.ByHero), 2, "no foe falls to one or two blows");
            Assert.Greater(run.Hero.Gold, 0);
        }

        [Test]
        public void BeatingABossOpensTheNextWorld()
        {
            var run = new DiceRun("berserker", 5) { Lap = DiceRun.LapsPerStage + 1 };
            run.Hero.Atk = 999;
            run.Hero.Max = run.Hero.Hp = 9999;
            var o = run.Land(new Move { Boss = true });
            Assert.AreEqual(OutcomeKind.StageClear, o.Kind);
            Assert.AreEqual(1, run.Stage);
            Assert.AreEqual(1, run.Lap);
            Assert.AreEqual(WorldData.Chapters[1], run.Chapter);
        }

        [Test]
        public void TheShopSellsOnlyWhatYouCanPay()
        {
            var run = new DiceRun("knight", 2);
            var item = new ShopItem("blade", "Blade", "", "", 30);
            run.Hero.Gold = 20;
            Assert.IsFalse(run.Buy(item));
            run.Hero.Gold = 40;
            var atk = run.Hero.Atk;
            Assert.IsTrue(run.Buy(item));
            Assert.AreEqual(10, run.Hero.Gold);
            Assert.Greater(run.Hero.Atk, atk);
            Assert.IsFalse(run.Buy(item), "sold once");
        }

        [Test]
        public void EveryTileDoesSomethingAndNothingBreaks()
        {
            var kinds = new HashSet<OutcomeKind>();
            for (var seed = 0; seed < 40; seed++)
            {
                var run = new DiceRun(Heroes.All[seed % 4].Id, seed);
                while (!run.Over && run.Rolls < 400)
                {
                    var o = run.Land(run.Roll());
                    kinds.Add(o.Kind);
                    Assert.IsFalse(string.IsNullOrEmpty(o.Title));
                    Assert.IsFalse(string.IsNullOrEmpty(o.Text), o.Kind.ToString());
                    Assert.GreaterOrEqual(run.Hero.Hp, run.Over && !run.Won ? 0 : 1);
                    Assert.GreaterOrEqual(run.Hero.Gold, 0);
                }
            }
            CollectionAssert.IsSupersetOf(kinds, new[]
            {
                OutcomeKind.Fight, OutcomeKind.Buff, OutcomeKind.Curse, OutcomeKind.Treasure,
                OutcomeKind.Luck, OutcomeKind.Shop, OutcomeKind.StageClear,
            });
        }

        /// <summary>Balance: most runs clear the first world, few beat them all,
        /// and every hero can get far.</summary>
        [Test]
        public void ARunIsWinnableButNotFree()
        {
            foreach (var h in Heroes.All)
            {
                var stages = Enumerable.Range(0, 200).Select(s => Play(h.Id, 1000 + s)).Select(r => r.Won ? 15 : r.Stage).ToList();
                var clearFirst = stages.Count(s => s >= 1) / 200.0;
                var all = stages.Count(s => s >= 15) / 200.0;
                var avg = stages.Average();
                Assert.That(clearFirst, Is.InRange(0.6, 0.98), h.Id + " clears world 1");
                Assert.That(all, Is.LessThan(0.1), h.Id + " beats everything");
                Assert.That(avg, Is.InRange(2.0, 9.0), h.Id + " average worlds");
            }
        }

        [Test]
        public void ChapterNamesReadNaturally()
        {
            Loc.Arabic = true;
            Assert.AreEqual("الفصل الثاني عشر", Loc.Chapter(12, "Chapter XII"));
            Loc.Arabic = false;
            Assert.AreEqual("Chapter XII", Loc.Chapter(12, "Chapter XII"));
        }

        [Test]
        public void ArabicLettersJoinAndReadRightToLeft()
        {
            Assert.AreEqual("\uFE91\uFE8E\uFE8F", ArabicShaper.Shape("باب"));
            Assert.AreEqual("\uFEFB", ArabicShaper.Shape("لا"));
            Assert.AreEqual("\uFE91" + ArabicShaper.KeyOpen + "\uFE8E\uFE8F",
                ArabicShaper.Shape("ب" + ArabicShaper.KeyOpen + "اب"));
            Assert.IsTrue(ArabicShaper.Line("اليوم 12").StartsWith("12 "));
            Assert.AreEqual("ATK +12%", ArabicShaper.Line("ATK +12%"));
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
            var content = Heroes.All.SelectMany(h => new[] { h.Name, h.Perk })
                .Concat(WorldData.Chapters.SelectMany(c => new[] { c.Name, c.Blurb }
                    .Concat(c.Foes.Concat(new[] { c.Boss }).Select(f => f.Name))));
            missing.AddRange(content.Where(s => !Loc.Has(s)).Select(s => "content: " + s));
            Assert.IsEmpty(missing, string.Join("\n", missing.Distinct()));
        }
    }
}
