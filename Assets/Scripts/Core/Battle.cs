using System;
using System.Collections.Generic;

namespace PushupAdventure.Core
{
    public struct Blow
    {
        public int Dmg, Heal;
        public bool Crit, Miss;
    }

    /// <summary>A fight in progress: the foe's HP, its attack clock and status.</summary>
    public sealed class FightState
    {
        public readonly Foe Foe;
        public int Hp;
        public bool First = true;

        /// <summary>Hits left that Stone Skin halves.</summary>
        public int Shield;

        public FightState(Foe foe) { Foe = foe; Hp = foe.Hp; }

        /// <summary>Frenzy: below half HP the foe attacks a third faster.</summary>
        public bool Frenzied => Foe.Ability == FoeAbility.Frenzy && Hp * 2 < Foe.Hp;

        public double Pace => (Foe.Boss ? Rules.PaceBoss : Foe.Elite ? Rules.PaceElite : Rules.PaceNormal) *
                              (Frenzied ? 0.75 : 1);

        /// <summary>A blow of [d]: Evasive may dodge it, Stone Skin halves it.</summary>
        public Blow Take(int d, bool crit, Rng rng, Func<int, int> onHit = null)
        {
            if (Foe.Ability == FoeAbility.Evasive && rng.Next() < 0.15) return new Blow { Miss = true };
            if (Shield > 0)
            {
                Shield--;
                d = Math.Max(1, d / 2);
            }
            Hp = Math.Max(0, Hp - d);
            return new Blow { Dmg = d, Crit = crit, Heal = onHit == null ? 0 : onHit(d) };
        }
    }

    public abstract class BattleEvent { }

    public sealed class StrikeEvent : BattleEvent { public List<Blow> Blows; }

    public sealed class SpecialEvent : BattleEvent
    {
        public string Name;
        public List<Blow> Blows;
        public int Healed, SelfHit;
        public bool Stun;
    }

    public sealed class FoeHitEvent : BattleEvent
    {
        public int Dmg, Reflect, Drained;
        public bool Heavy;
    }

    /// <summary>The foe used its ability. Amount: HP healed, hits shielded,
    /// charge drained or damage dealt; Tick marks a poison tick.</summary>
    public sealed class FoeAbilityEvent : BattleEvent
    {
        public FoeAbility Ability;
        public int Amount;
        public bool Tick;
    }

    public sealed class WindupEvent : BattleEvent { }

    public sealed class InterruptEvent : BattleEvent { }

    /// <summary>One fight in time: the hero's pushups, the special meter, the
    /// foe's clock, its heavy blows and its ability. The screen and the tests
    /// both drive it.</summary>
    public sealed class Battle
    {
        /// <summary>Seconds a heavy blow takes to land, and the pushups that stop it.</summary>
        public const double WindupTime = 2.4;
        public const int WindupBreak = 3;

        public readonly Hero Hero;
        public readonly FightState St;
        readonly Rng _rng;

        public double Clock;
        public int Attacks;
        public bool Windup;
        public double WindupLeft;
        public int WindupHits;

        /// <summary>Special meter, in strikes.</summary>
        public int Charge;

        /// <summary>Venom on the hero: seconds left, and time to the next tick.</summary>
        public double PoisonLeft, PoisonTick;
        bool _frenzyShown;

        public Battle(Hero hero, Foe foe, Rng rng, int charge = 0)
        {
            Hero = hero;
            St = new FightState(foe);
            _rng = rng;
            Charge = charge;
        }

        public Foe Foe => St.Foe;
        public bool Over => St.Hp <= 0 || Hero.Hp <= 0;
        public bool Won => St.Hp <= 0 && Hero.Hp > 0;
        public bool SpecialReady => Charge >= Hero.SpecialCost;

        /// <summary>0..1 progress of the foe's next attack (or of the heavy blow).</summary>
        public double Threat => Windup ? 1 - WindupLeft / WindupTime : Math.Min(1, Clock / St.Pace);

        /// <summary>One pushup.</summary>
        public List<BattleEvent> Rep()
        {
            var output = new List<BattleEvent>();
            if (Over) return output;
            if (SpecialReady)
            {
                Charge = 0;
                var s = HeroSpecial();
                output.Add(s);
                if (s.Stun)
                {
                    Clock = 0;
                    if (Windup)
                    {
                        Windup = false;
                        output.Add(new InterruptEvent());
                    }
                }
            }
            else
            {
                Charge++;
                output.Add(new StrikeEvent { Blows = HeroStrike() });
            }
            if (Windup)
            {
                WindupHits++;
                if (WindupHits >= WindupBreak)
                {
                    Windup = false;
                    Clock = 0;
                    output.Add(new InterruptEvent());
                }
            }
            return output;
        }

        /// <summary>Time passes; the foe attacks on its own clock.</summary>
        public List<BattleEvent> Advance(double dt)
        {
            var output = new List<BattleEvent>();
            if (Over || dt <= 0) return output;
            if (PoisonLeft > 0)
            {
                PoisonLeft -= dt;
                PoisonTick -= dt;
                if (PoisonTick <= 0)
                {
                    PoisonTick += 1;
                    var d = Math.Max(1, (int)Math.Round(Hero.Max * 0.03));
                    Hero.Hp = Math.Max(0, Hero.Hp - d);
                    output.Add(new FoeAbilityEvent { Ability = FoeAbility.Venom, Amount = d, Tick = true });
                    if (Over) return output;
                }
            }
            if (!_frenzyShown && St.Frenzied)
            {
                _frenzyShown = true;
                output.Add(new FoeAbilityEvent { Ability = FoeAbility.Frenzy });
            }
            if (Windup)
            {
                WindupLeft -= dt;
                if (WindupLeft <= 0)
                {
                    Windup = false;
                    Clock = 0;
                    output.Add(FoeHit(2.2, true));
                }
                return output;
            }
            Clock += dt;
            if (Clock >= St.Pace)
            {
                Clock -= St.Pace;
                Attacks++;
                if (Foe.WindsUp && Attacks % 3 == 0)
                {
                    Windup = true;
                    WindupLeft = WindupTime;
                    WindupHits = 0;
                    output.Add(new WindupEvent());
                }
                else if (Abilities.IsActive(Foe.Ability) && Attacks % Abilities.Every == 0)
                {
                    output.AddRange(UseAbility());
                }
                else
                {
                    output.Add(FoeHit(1, false));
                }
            }
            return output;
        }

        List<Blow> HeroStrike()
        {
            var h = Hero;
            var hits = 1 + (h.Stacks("twin") > 0 && _rng.Next() < 0.25 * h.Stacks("twin") ? 1 : 0);
            var blows = new List<Blow>();
            for (var i = 0; i < hits && St.Hp > 0; i++)
            {
                var d = h.Atk;
                var c = h.Class;
                var crit = _rng.Next() < c.CritChance + 0.15 * h.Stacks("eye");
                if (crit) d = (int)Math.Round(d * c.CritMult);
                // Spark boosts the opening blow, never past 60% of the foe.
                if (St.First && h.Stacks("spark") > 0)
                    d = Math.Max(d, Math.Min(d * (1 + 2 * h.Stacks("spark")), (int)Math.Round(St.Foe.Hp * 0.6)));
                St.First = false;
                blows.Add(St.Take(d, crit, _rng,
                    x => h.Stacks("leech") > 0 ? Rules.HealBy(h, (int)Math.Round(x * 0.15 * h.Stacks("leech"))) : 0));
            }
            return blows;
        }

        SpecialEvent HeroSpecial()
        {
            var h = Hero;
            var c = h.Class;
            Func<double, bool, Blow> hit = (mult, canCrit) =>
            {
                var d = (int)Math.Round(h.Atk * mult);
                var crit = canCrit && _rng.Next() < c.CritChance + 0.15 * h.Stacks("eye");
                if (crit) d = (int)Math.Round(d * c.CritMult);
                return St.Take(d, crit, _rng);
            };
            var e = new SpecialEvent { Name = c.Special, Blows = new List<Blow>() };
            switch (c.Id)
            {
                case "knight":
                    e.Blows.Add(hit(2.5, false));
                    e.Stun = true;
                    break;
                case "ranger":
                    for (var i = 0; i < 5 && St.Hp > 0; i++) e.Blows.Add(hit(0.7, true));
                    break;
                case "monk":
                    e.Blows.Add(hit(3.0, false));
                    e.Healed = Rules.Heal(h, 0.15);
                    break;
                default:
                    var cost = (int)Math.Round(h.Hp * 0.05);
                    h.Hp = Math.Max(1, h.Hp - cost);
                    e.SelfHit = cost;
                    for (var i = 0; i < 3 && St.Hp > 0; i++) e.Blows.Add(hit(1.4, true));
                    break;
            }
            return e;
        }

        List<BattleEvent> UseAbility()
        {
            var a = Foe.Ability;
            var o = new List<BattleEvent>();
            switch (a)
            {
                case FoeAbility.Regen:
                    var before = St.Hp;
                    St.Hp = Math.Min(Foe.Hp, St.Hp + (int)Math.Round(Foe.Hp * 0.10));
                    o.Add(new FoeAbilityEvent { Ability = a, Amount = St.Hp - before });
                    break;
                case FoeAbility.StoneSkin:
                    St.Shield = 3;
                    o.Add(new FoeAbilityEvent { Ability = a, Amount = 3 });
                    break;
                case FoeAbility.Venom:
                    PoisonLeft = 4;
                    PoisonTick = 1;
                    o.Add(new FoeAbilityEvent { Ability = a });
                    o.Add(FoeHit(1, false));
                    break;
                case FoeAbility.Breath:
                    var d = Math.Max(1, (int)Math.Round(Foe.Atk * 1.6));
                    Hero.Hp = Math.Max(0, Hero.Hp - d);
                    o.Add(new FoeAbilityEvent { Ability = a, Amount = d });
                    break;
                case FoeAbility.Hex:
                    var lost = Charge - Charge / 2;
                    Charge /= 2;
                    o.Add(new FoeAbilityEvent { Ability = a, Amount = lost });
                    o.Add(FoeHit(1, false));
                    break;
                default:
                    o.Add(FoeHit(1, false));
                    break;
            }
            return o;
        }

        FoeHitEvent FoeHit(double mult, bool heavy)
        {
            var taken = Math.Max(1, (int)Math.Round(Foe.Atk * mult) - Hero.Def);
            Hero.Hp = Math.Max(0, Hero.Hp - taken);
            var reflect = 0;
            if (Hero.Stacks("thorns") > 0)
            {
                reflect = (int)Math.Round(taken * 0.3 * Hero.Stacks("thorns"));
                St.Hp = Math.Max(0, St.Hp - reflect);
            }
            var drained = 0;
            if (Foe.Ability == FoeAbility.Drain && St.Hp > 0)
            {
                var before = St.Hp;
                St.Hp = Math.Min(Foe.Hp, St.Hp + taken / 2);
                drained = St.Hp - before;
            }
            return new FoeHitEvent { Dmg = taken, Reflect = reflect, Heavy = heavy, Drained = drained };
        }
    }
}
