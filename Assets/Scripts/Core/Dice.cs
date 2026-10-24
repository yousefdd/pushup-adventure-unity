using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceQuest.Core
{
    public enum TileKind { Start, Enemy, Elite, Buff, Curse, Shop, Treasure, Luck }

    /// <summary>A hero to pick at camp: starting numbers and one perk.</summary>
    public sealed class HeroDef
    {
        public readonly string Id, Name, Perk;
        public readonly int Hp, Atk, Def;
        public readonly double Crit, CritMult, Interval;

        public HeroDef(string id, string name, string perk, int hp, int atk, int def, double crit, double critMult, double interval)
        {
            Id = id; Name = name; Perk = perk; Hp = hp; Atk = atk; Def = def;
            Crit = crit; CritMult = critMult; Interval = interval;
        }

        public string Sprite => "hero_" + Id;
    }

    public static class Heroes
    {
        public static readonly HeroDef[] All =
        {
            new HeroDef("knight", "Knight", "Tough: starts with the most HP and armour", 130, 10, 4, 0.05, 2.0, 1.0),
            new HeroDef("ranger", "Ranger", "Quick: strikes fast and often critically", 110, 10, 2, 0.22, 2.0, 0.8),
            new HeroDef("monk", "Monk", "Calm: heals 8% of max HP after every fight", 110, 10, 3, 0.08, 2.0, 0.95),
            new HeroDef("berserker", "Berserker", "Fierce: the hardest hitter, crits deal 2.5×", 115, 13, 1, 0.12, 2.5, 1.05),
        };

        public static HeroDef ById(string id) => All.FirstOrDefault(h => h.Id == id) ?? All[0];
    }

    public sealed class Hero
    {
        public readonly HeroDef Def;
        public int Hp, Max, Atk, Armor, Lv = 1, Xp, Gold;
        public double Crit;

        public Hero(HeroDef d)
        {
            Def = d; Hp = Max = d.Hp; Atk = d.Atk; Armor = d.Def; Crit = d.Crit;
        }

        public int XpToNext => 30 + 10 * Lv;

        public int Heal(double share) => HealBy((int)Math.Round(Max * share));

        public int HealBy(int n)
        {
            var before = Hp;
            Hp = Math.Min(Max, Hp + Math.Max(0, n));
            return Hp - before;
        }

        /// <summary>Adds XP; returns the levels gained (+12 max HP, +2 ATK each).</summary>
        public int GainXp(int xp)
        {
            Xp += xp;
            var ups = 0;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Lv++;
                ups++;
                Max += 12;
                Hp += 12;
                Atk += 2;
            }
            return ups;
        }
    }

    public sealed class Foe
    {
        public readonly FoeKind Kind;
        public readonly int Hp, Atk, Def, Gold, Xp;
        public readonly bool Elite, Boss;
        public readonly double Interval;

        public Foe(FoeKind kind, int hp, int atk, int def, int gold, int xp, bool elite, bool boss, double interval)
        {
            Kind = kind; Hp = hp; Atk = atk; Def = def; Gold = gold; Xp = xp; Elite = elite; Boss = boss; Interval = interval;
        }
    }

    /// <summary>One blow in an automatic fight, at time T seconds.</summary>
    public struct Beat
    {
        public double T;
        public bool ByHero, Crit;
        public int Dmg, HeroHp, FoeHp;
    }

    public sealed class Fight
    {
        public Foe Foe;
        public int HeroHpBefore;
        public readonly List<Beat> Beats = new List<Beat>();
        public bool Won;
        public int Gold, Xp, LevelUps, Healed;

        public double Duration => Beats.Count == 0 ? 0 : Beats[Beats.Count - 1].T;
    }

    public sealed class ShopItem
    {
        public readonly string Key, Name, Text, Icon;
        public readonly int Price;
        public bool Sold;

        public ShopItem(string key, string name, string text, string icon, int price)
        {
            Key = key; Name = name; Text = text; Icon = icon; Price = price;
        }
    }

    public enum OutcomeKind { Fight, Buff, Curse, Treasure, Luck, Shop, Camp, StageClear, GameWon, Defeat }

    /// <summary>What a landing did, for the screen to show. Title and Text are
    /// already translated; Icon is a picture name.</summary>
    public sealed class Outcome
    {
        public OutcomeKind Kind;
        public string Title, Text, Icon;
        public bool Good = true;
        public Fight Fight;
        public List<ShopItem> Shop;
        public int Die;
    }

    /// <summary>One roll: the die, the tiles the hero passes, and whether the
    /// boss stops the walk at camp.</summary>
    public sealed class Move
    {
        public int Value, From;
        public readonly List<int> Path = new List<int>();
        public bool Boss;
        public int CampGold, CampHeal;
    }

    /// <summary>A run: the hero walks a ring of 24 tiles, rolling a die. Every
    /// tile is an event. After two laps the world's boss waits at camp; beat
    /// it and the next world opens, with stronger creatures.</summary>
    public sealed class DiceRun
    {
        public const int Size = 24, LapsPerStage = 2;

        public readonly Hero Hero;
        public readonly Rng Rng;
        public TileKind[] Board;
        public int Stage, Lap = 1, Pos, Rolls, Kills;
        public bool Over, Won;

        public DiceRun(string heroId, int seed)
        {
            Rng = new Rng(seed);
            Hero = new Hero(Heroes.ById(heroId));
            Board = MakeBoard(Rng);
        }

        public ChapterDef Chapter => WorldData.Chapters[Stage];

        // Balance, tuned with the run simulation in the tests.
        public static double WorldGrowth = 0.30, LapGrowth = 0.18, BossHp = 3.0, BossAtk = 1.2, BossHeal = 0.3;

        /// <summary>Creatures grow stronger each world and each lap; the boss
        /// is measured against the world, not the lap.</summary>
        double Power(bool boss) => (1 + WorldGrowth * Stage) * (boss ? 1 : 1 + LapGrowth * (Math.Min(Lap, LapsPerStage) - 1));

        // ------------------------------------------------------------ board

        /// <summary>The 23 tiles after camp.</summary>
        static readonly Dictionary<TileKind, int> Mix = new Dictionary<TileKind, int>
        {
            { TileKind.Enemy, 9 }, { TileKind.Elite, 2 }, { TileKind.Buff, 4 }, { TileKind.Curse, 3 },
            { TileKind.Shop, 2 }, { TileKind.Treasure, 2 }, { TileKind.Luck, 1 },
        };

        public static TileKind[] MakeBoard(Rng rng)
        {
            var pool = Mix.SelectMany(x => Enumerable.Repeat(x.Key, x.Value)).ToList();
            for (var attempt = 0; ; attempt++)
            {
                var order = pool.OrderBy(_ => rng.Next()).ToList();
                order.Insert(0, TileKind.Start);
                var b = order.ToArray();
                // Fair starts: no elite in the first three steps, shops apart.
                var ok = attempt > 200 || (Enumerable.Range(1, 3).All(i => b[i] != TileKind.Elite) &&
                         Enumerable.Range(0, Size).All(i => b[i] != TileKind.Shop || b[(i + 1) % Size] != TileKind.Shop));
                if (ok) return b;
            }
        }

        // ------------------------------------------------------------ moves

        /// <summary>Rolls the die and walks. Passing camp pays a little gold and
        /// heals a little; after the last lap the boss stops the walk there.</summary>
        public Move Roll(int forced = 0)
        {
            var m = new Move { From = Pos, Value = forced > 0 ? forced : Rng.Range(1, 6) };
            Rolls++;
            for (var i = 0; i < m.Value; i++)
            {
                Pos = (Pos + 1) % Size;
                m.Path.Add(Pos);
                if (Pos != 0) continue;
                Lap++;
                if (Lap > LapsPerStage)
                {
                    // The boss waits at camp; a short rest first.
                    m.Boss = true;
                    m.CampHeal += Hero.Heal(BossHeal);
                    break;
                }
                m.CampGold += 5 + 2 * Stage;
                Hero.Gold += 5 + 2 * Stage;
                m.CampHeal += Hero.Heal(0.10);
            }
            return m;
        }

        /// <summary>What the tile the hero stands on does.</summary>
        public Outcome Land(Move m)
        {
            if (m.Boss) return FightOutcome(MakeFoe(Chapter.Boss, false, true));
            switch (Board[Pos])
            {
                case TileKind.Enemy: return FightOutcome(MakeFoe(FoeAt(Pos), false, false));
                case TileKind.Elite: return FightOutcome(MakeFoe(FoeAt(Pos), true, false));
                case TileKind.Buff: return Buff();
                case TileKind.Curse: return Curse();
                case TileKind.Shop: return Shop();
                case TileKind.Treasure: return Treasure();
                case TileKind.Luck: return Luck();
                default:
                    var healed = Hero.Heal(0.25);
                    return new Outcome
                    {
                        Kind = OutcomeKind.Camp, Title = Loc.Tr("Camp"), Icon = "pet_pip",
                        Text = Loc.Tr("You rest by the fire. Heal +{n}.", "n", healed),
                    };
            }
        }

        /// <summary>The creature waiting on an enemy or elite tile this lap
        /// (the board shows it). Later laps bring the world's tougher ones.</summary>
        public FoeKind FoeAt(int pos)
        {
            var foes = Chapter.Foes;
            var elite = Board[pos] == TileKind.Elite;
            var lo = elite ? 3 : Math.Min(Lap - 1, 2);
            var hi = elite ? foes.Length - 1 : Math.Min(foes.Length - 1, 2 + Lap);
            return foes[lo + (pos * 7 + Lap * 3) % (hi - lo + 1)];
        }

        public Foe MakeFoe(FoeKind k, bool elite, bool boss)
        {
            var p = Power(boss);
            var m = 0.7 + 0.5 * k.M;
            var hp = 34 * Math.Pow(p, 1.25) * m;
            var atk = 8.5 * p * m;
            if (elite) { hp *= 1.8; atk *= 1.25; }
            if (boss) { hp *= BossHp; atk *= BossAtk; }
            var gold = boss ? 45 + 12 * Stage : elite ? 18 + 5 * Stage : 7 + 2 * Stage;
            var xp = boss ? 60 : elite ? 25 : 10;
            var interval = boss ? 0.9 : elite ? 1.0 : 1.1;
            return new Foe(k, (int)Math.Round(hp), (int)Math.Round(atk), Stage / 2, gold, xp, elite, boss, interval);
        }

        // ----------------------------------------------------------- fights

        /// <summary>The whole fight, worked out at once: blows on two clocks
        /// until one side falls. The screen plays the beats back.</summary>
        public Fight Simulate(Foe foe)
        {
            var h = Hero;
            var f = new Fight { Foe = foe, HeroHpBefore = h.Hp };
            var foeHp = foe.Hp;
            double th = 0.5, tf = 0.8;
            while (h.Hp > 0 && foeHp > 0 && f.Beats.Count < 400)
            {
                var b = new Beat();
                if (th <= tf)
                {
                    var crit = Rng.Next() < h.Crit;
                    var d = Math.Max(1, (int)Math.Round(h.Atk * (0.9 + 0.2 * Rng.Next())) - foe.Def);
                    if (crit) d = (int)Math.Round(d * h.Def.CritMult);
                    foeHp = Math.Max(0, foeHp - d);
                    b = new Beat { T = th, ByHero = true, Crit = crit, Dmg = d };
                    th += h.Def.Interval;
                }
                else
                {
                    var d = Math.Max(1, (int)Math.Round(foe.Atk * (0.9 + 0.2 * Rng.Next())) - h.Armor);
                    h.Hp = Math.Max(0, h.Hp - d);
                    b = new Beat { T = tf, ByHero = false, Dmg = d };
                    tf += foe.Interval;
                }
                b.HeroHp = h.Hp;
                b.FoeHp = foeHp;
                f.Beats.Add(b);
            }
            // A fight that never ends (both too tough) goes to the hero.
            f.Won = h.Hp > 0;
            if (!f.Won) return f;
            Kills++;
            f.Gold = foe.Gold;
            h.Gold += foe.Gold;
            f.Xp = foe.Xp;
            f.LevelUps = h.GainXp(foe.Xp);
            if (h.Def.Id == "monk") f.Healed = h.Heal(0.08);
            return f;
        }

        Outcome FightOutcome(Foe foe)
        {
            var f = Simulate(foe);
            var o = new Outcome { Kind = OutcomeKind.Fight, Fight = f, Icon = foe.Kind.Img, Good = f.Won };
            var name = Loc.Tr(foe.Kind.Name);
            if (!f.Won)
            {
                Over = true;
                o.Title = Loc.Tr("Defeat");
                o.Text = Loc.Tr("The {foe} was too strong.", "foe", name);
                return o;
            }
            o.Title = Loc.Tr(foe.Boss ? "Boss defeated!" : foe.Elite ? "Elite defeated!" : "Victory!");
            o.Text = Loc.Tr("{foe} is beaten. +{gold} gold.", "foe", name, "gold", f.Gold) +
                     (f.LevelUps > 0 ? " " + Loc.Tr("Level up! Level {n}.", "n", Hero.Lv) : "");
            if (foe.Boss) NextStage(o);
            return o;
        }

        void NextStage(Outcome o)
        {
            Stage++;
            if (Stage >= WorldData.Chapters.Length)
            {
                Stage = WorldData.Chapters.Length - 1;
                Won = true;
                Over = true;
                o.Kind = OutcomeKind.GameWon;
                o.Title = Loc.Tr("Legend!");
                o.Text = Loc.Tr("Every world is free. Your hero is a legend.");
                return;
            }
            Lap = 1;
            Pos = 0;
            Board = MakeBoard(Rng);
            var healed = Hero.Heal(0.3);
            o.Kind = OutcomeKind.StageClear;
            o.Text += " " + Loc.Tr("On to {world}. Heal +{n}.", "world", Loc.Tr(Chapter.Name), "n", healed);
        }

        // ----------------------------------------------------- other tiles

        Outcome Buff()
        {
            var o = new Outcome { Kind = OutcomeKind.Buff, Title = Loc.Tr("Blessing"), Icon = "gear_star" };
            switch (Rng.Range(0, 4))
            {
                case 0: Hero.Atk += 2; o.Text = Loc.Tr("A shrine sharpens your blade. ATK +2."); break;
                case 1: Hero.Armor += 1; o.Text = Loc.Tr("Old runes harden your skin. DEF +1."); break;
                case 2: Hero.Max += 15; Hero.Hp += 15; o.Text = Loc.Tr("A spring fills you with life. Max HP +15."); break;
                case 3: o.Text = Loc.Tr("A healer patches you up. Heal +{n}.", "n", Hero.Heal(0.3)); break;
                default:
                    var g = 12 + 3 * Stage;
                    Hero.Gold += g;
                    o.Text = Loc.Tr("You find a lost purse. +{n} gold.", "n", g);
                    break;
            }
            return o;
        }

        Outcome Curse()
        {
            var o = new Outcome { Kind = OutcomeKind.Curse, Title = Loc.Tr("Curse"), Icon = "foe_hex_imp", Good = false };
            switch (Rng.Range(0, 2))
            {
                case 0:
                    var d = Math.Min(Hero.Hp - 1, (int)Math.Round(Hero.Max * 0.12));
                    Hero.Hp -= Math.Max(0, d);
                    o.Text = Loc.Tr("A hidden trap snaps shut. HP −{n}.", "n", Math.Max(0, d));
                    break;
                case 1:
                    var g = (int)Math.Round(Hero.Gold * 0.3);
                    Hero.Gold -= g;
                    o.Text = Loc.Tr("A thief cuts your purse. Gold −{n}.", "n", g);
                    break;
                default:
                    Hero.Atk = Math.Max(4, Hero.Atk - 1);
                    o.Text = Loc.Tr("Rust creeps over your blade. ATK −1.");
                    break;
            }
            return o;
        }

        Outcome Treasure()
        {
            var o = new Outcome { Kind = OutcomeKind.Treasure, Title = Loc.Tr("Treasure!") };
            var g = 5 + 2 * Stage;
            Hero.Gold += g;
            switch (Rng.Range(0, 2))
            {
                case 0:
                    var a = Rng.Range(2, 3) + Stage / 3;
                    Hero.Atk += a;
                    o.Icon = "gear_sword";
                    o.Text = Loc.Tr("Inside: a fine sword. ATK +{n}, gold +{g}.", "n", a, "g", g);
                    break;
                case 1:
                    var h = 15 + 5 * Rng.Range(0, 2) + 3 * Stage;
                    Hero.Max += h;
                    Hero.Hp += h;
                    o.Icon = "gear_plate";
                    o.Text = Loc.Tr("Inside: sturdy armour. Max HP +{n}, gold +{g}.", "n", h, "g", g);
                    break;
                default:
                    var d = 1 + Stage / 4;
                    Hero.Armor += d;
                    o.Icon = "gear_helm";
                    o.Text = Loc.Tr("Inside: a strong helm. DEF +{n}, gold +{g}.", "n", d, "g", g);
                    break;
            }
            return o;
        }

        Outcome Luck()
        {
            var die = Rng.Range(1, 6);
            var o = new Outcome { Kind = OutcomeKind.Luck, Title = Loc.Tr("Lucky roll"), Die = die, Good = die >= 3 };
            switch (die)
            {
                case 6: Hero.Atk += 3; o.Text = Loc.Tr("Six! The spirits bless you. ATK +3."); break;
                case 5: Hero.Gold += 25 + 5 * Stage; o.Text = Loc.Tr("Five! A pile of coins. +{n} gold.", "n", 25 + 5 * Stage); break;
                case 4: o.Text = Loc.Tr("Four! A warm breeze. Heal +{n}.", "n", Hero.Heal(0.25)); break;
                case 3: o.Text = Loc.Tr("Three. Nothing happens."); break;
                case 2:
                    var g = Math.Min(Hero.Gold, 15);
                    Hero.Gold -= g;
                    o.Text = Loc.Tr("Two. You drop some coins. Gold −{n}.", "n", g);
                    break;
                default:
                    var d = Math.Min(Hero.Hp - 1, (int)Math.Round(Hero.Max * 0.1));
                    Hero.Hp -= Math.Max(0, d);
                    o.Text = Loc.Tr("One! You stub your toe. HP −{n}.", "n", Math.Max(0, d));
                    break;
            }
            return o;
        }

        // ------------------------------------------------------------- shop

        public static readonly string[] ShopKeys = { "potion", "elixir", "blade", "shield", "armor", "charm" };

        Outcome Shop()
        {
            var keys = ShopKeys.OrderBy(_ => Rng.Next()).Take(3).ToList();
            return new Outcome
            {
                Kind = OutcomeKind.Shop, Title = Loc.Tr("Shop"), Icon = "gear_ruby",
                Text = Loc.Tr("A travelling merchant waves you over."),
                Shop = keys.Select(MakeItem).ToList(),
            };
        }

        ShopItem MakeItem(string key)
        {
            var s = Stage;
            switch (key)
            {
                case "potion": return new ShopItem(key, Loc.Tr("Potion"), Loc.Tr("Heal 40%"), "gear_sapphire", 12 + 4 * s);
                case "elixir": return new ShopItem(key, Loc.Tr("Elixir"), Loc.Tr("Heal to full"), "gear_ruby", 24 + 6 * s);
                case "blade": return new ShopItem(key, Loc.Tr("Sharp blade"), Loc.Tr("ATK +{n}", "n", 3 + s / 2), "gear_axe", 25 + 8 * s);
                case "shield": return new ShopItem(key, Loc.Tr("Iron shield"), Loc.Tr("DEF +{n}", "n", 2 + s / 4), "gear_helm", 25 + 8 * s);
                case "armor": return new ShopItem(key, Loc.Tr("Chain mail"), Loc.Tr("Max HP +{n}", "n", 20 + 5 * s), "gear_plate", 25 + 8 * s);
                default: return new ShopItem(key, Loc.Tr("Lucky charm"), Loc.Tr("Crit chance +6%"), "gear_star", 30 + 8 * s);
            }
        }

        /// <summary>Buys an item if there is gold for it.</summary>
        public bool Buy(ShopItem i)
        {
            if (i.Sold || Hero.Gold < i.Price) return false;
            Hero.Gold -= i.Price;
            i.Sold = true;
            var s = Stage;
            switch (i.Key)
            {
                case "potion": Hero.Heal(0.4); break;
                case "elixir": Hero.Heal(1); break;
                case "blade": Hero.Atk += 3 + s / 2; break;
                case "shield": Hero.Armor += 2 + s / 4; break;
                case "armor": Hero.Max += 20 + 5 * s; Hero.Hp += 20 + 5 * s; break;
                default: Hero.Crit = Math.Min(0.75, Hero.Crit + 0.06); break;
            }
            return true;
        }
    }
}
