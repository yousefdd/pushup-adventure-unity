using System;
using System.Collections.Generic;
using System.Linq;

namespace PushupAdventure.Core
{
    public enum RunMode { Walk, Fight, Pick, Spin, Card, Busy, Over }

    public enum OverlayKind { Pick, Choice, Wheel, ChestClosed, Chest, Clear, Defeat }

    public enum LogKind { Story, Fight, Win }

    public enum FloatKind { Hit, Crit, Heal, Hurt, Reflect }

    public enum FxKind { Walk, FoeEnter, Banner, Lunge, Slash, Float, Shake, Special, Windup, Interrupt, FoeDown, Dodge, Ability, LevelUp }

    /// <summary>A one-shot effect for the screen to animate.</summary>
    public struct RunFx
    {
        public FxKind Kind;
        public string Text;
        public bool ByHero, Crit, OnFoe, Boss;
        public FloatKind Float;
        public float Strength, Seconds;
        public FoeAbility Ability;
    }

    public sealed class LogEntry
    {
        public readonly string Head, Body, Tag;
        public readonly LogKind Kind;

        /// <summary>Body is plain text; a name wrapped in *stars* is a keyword.</summary>
        public LogEntry(string head, string body, string tag = null, LogKind kind = LogKind.Story)
        {
            Head = head; Body = body; Tag = tag; Kind = kind;
        }
    }

    public sealed class PickOption
    {
        public readonly string Title, Text, Skill;
        public PickOption(string title, string text, string skill = null) { Title = title; Text = text; Skill = skill; }
    }

    public sealed class Overlay
    {
        public OverlayKind Kind;
        public string Title, Subtitle, Result;
        public List<PickOption> Options = new List<PickOption>();
        public int WheelIndex;
        public bool Spinning;
        public Loot Loot;
        public int Survived, Best;
    }

    /// <summary>One run down a chapter's road, driven by pushups: each pushup is a
    /// day on the road, a strike in a fight, a pick in a choice, or a spin of
    /// the wheel. Time passes through Tick; the screen reads the state and
    /// drains Fx.</summary>
    public sealed class RunController
    {
        public const double WalkTime = 0.6, WheelSpin = 2.5, BossIntro = 0.9, FightIntro = 0.6;
        public const double PickCycle = 1.8, PickAuto = 12, CardAuto = 2.6, SpinAuto = 5;

        public readonly Rng Rng;
        public readonly Hero Hero;
        public readonly ChapterDef Chapter;
        public readonly List<LogEntry> Log = new List<LogEntry>();
        public readonly List<RunFx> Fx = new List<RunFx>();

        public int Day, Round, Reps, BestDays;
        public RunMode Mode = RunMode.Walk;
        public Battle Battle;
        public Overlay Overlay;
        public double PickTime, SpinWait;
        public double? CardLeft;
        public int SpecialCharge;
        public bool Won;

        double _waitLeft;
        Action _then, _onCardClosed;
        Action<int> _onPicked;
        readonly int[] _storyBag;

        public RunController(ChapterDef chapter, string heroClass, int seed, int bestDays = 0)
        {
            Rng = new Rng(seed);
            Chapter = chapter;
            Hero = Hero.Start(chapter.StartLevel, heroClass);
            BestDays = bestDays;
            _storyBag = Enumerable.Range(0, Content.Stories.Length).OrderBy(_ => Rng.Next()).ToArray();
            AddLog(new LogEntry(Loc.Tr("Day {n}", "n", 0),
                Loc.Chapter(Chapter.Number, Chapter.Roman) + ": *" + Loc.Tr(Chapter.Name) + "*. " + Loc.Tr(Chapter.Blurb)));
        }

        public bool Finished => Mode == RunMode.Over;
        public FightState Fight => Battle?.St;
        public double FoeClock => Battle?.Threat ?? 0;

        /// <summary>The card that glows now; a pushup takes it.</summary>
        public int LitIndex => Overlay == null || Overlay.Options.Count == 0
            ? 0
            : (int)(PickTime / PickCycle) % Overlay.Options.Count;

        public double PickAutoLeft => Math.Max(0, PickAuto - PickTime);

        public IEnumerable<string> Upcoming => Content.Road.Skip(Day).Take(5);

        /// <summary>The button's label and its small line: what the next pushup does.</summary>
        public (string label, string sub) ActionLabel
        {
            get
            {
                switch (Mode)
                {
                    case RunMode.Walk: return (Loc.Tr("Next Day"), Loc.Tr("1 pushup"));
                    case RunMode.Fight:
                        if (Battle != null && Battle.SpecialReady) return (Loc.Tr("SPECIAL!"), Loc.Tr(Hero.Class.Special));
                        if (Battle != null && Battle.Windup)
                            return (Loc.Tr("STRIKE!"), Loc.Tr("stop the blow: {n} more", "n", Battle.WindupBreak - Battle.WindupHits));
                        return (Loc.Tr("STRIKE!"), Loc.Tr("1 pushup = 1 hit"));
                    case RunMode.Pick:
                        return (Loc.Tr("Pick"), Loc.Tr(Overlay != null && Overlay.Kind == OverlayKind.Choice
                            ? "a pushup takes the glowing one" : "a pushup takes the glowing card"));
                    case RunMode.Spin: return (Loc.Tr("Spin!"), Loc.Tr("1 pushup"));
                    case RunMode.Card:
                        if (CardLeft == null) return (Loc.Tr("Back"), Loc.Tr("a pushup"));
                        if (Overlay != null && Overlay.Kind == OverlayKind.ChestClosed) return (Loc.Tr("Pick up"), Loc.Tr("a pushup"));
                        return (Loc.Tr("Continue"), Loc.Tr("a pushup, or wait a moment"));
                    case RunMode.Busy:
                        if (Overlay != null && Overlay.Kind == OverlayKind.Wheel) return (Loc.Tr("Spinning"), "• • •");
                        if (Battle != null) return (Loc.Tr("FIGHT!"), "• • •");
                        return (Loc.Tr("Moving"), "• • •");
                    default: return (Loc.Tr("Back to camp"), Loc.Tr("a pushup"));
                }
            }
        }

        // ------------------------------------------------------------ input

        /// <summary>One pushup (or a tap).</summary>
        public void Rep()
        {
            switch (Mode)
            {
                case RunMode.Walk: Reps++; NextDay(); break;
                case RunMode.Fight: Reps++; Strike(); break;
                case RunMode.Pick: Reps++; Choose(LitIndex); break;
                case RunMode.Spin: Reps++; StartSpin(); break;
                case RunMode.Card: Reps++; CloseCard(); break;
            }
        }

        public void Choose(int i)
        {
            if (Mode != RunMode.Pick) return;
            var f = _onPicked;
            Overlay = null;
            _onPicked = null;
            f?.Invoke(i);
        }

        public void CloseCard()
        {
            if (Mode != RunMode.Card) return;
            var f = _onCardClosed;
            Overlay = null;
            _onCardClosed = null;
            CardLeft = null;
            f?.Invoke();
        }

        public void Tick(double dt)
        {
            if (dt <= 0 || Finished) return;
            switch (Mode)
            {
                case RunMode.Busy:
                    _waitLeft -= dt;
                    if (_waitLeft <= 0 && _then != null)
                    {
                        var f = _then;
                        _then = null;
                        f();
                    }
                    break;
                case RunMode.Fight:
                    Events(Battle.Advance(dt));
                    break;
                case RunMode.Pick:
                    PickTime += dt;
                    if (PickTime >= PickAuto) Choose(LitIndex);
                    break;
                case RunMode.Spin:
                    SpinWait += dt;
                    if (SpinWait >= SpinAuto) StartSpin();
                    break;
                case RunMode.Card:
                    if (CardLeft != null)
                    {
                        CardLeft -= dt;
                        if (CardLeft <= 0) CloseCard();
                    }
                    break;
            }
        }

        // ------------------------------------------------------------- days

        string Head => Loc.Tr("Day {n}", "n", Day);

        void Wait(double secs, Action then)
        {
            Mode = RunMode.Busy;
            _waitLeft = secs;
            _then = then;
        }

        void NextDay()
        {
            Day++;
            Fx.Add(new RunFx { Kind = FxKind.Walk });
            Wait(WalkTime, ResolveDay);
        }

        void ResolveDay()
        {
            var node = Content.Road[Day - 1];
            switch (node)
            {
                case "story":
                    var st = Content.Stories[_storyBag[Day % _storyBag.Length]];
                    st.Effect.Apply(Hero);
                    AddLog(new LogEntry(Head, Loc.Tr(st.Text), Loc.Tr(st.Effect.Tag)));
                    EndDay();
                    break;
                case "skill":
                    AddLog(new LogEntry(Head, Loc.Tr("You sit down to think. Pip wobbles with an idea.")));
                    ChooseSkill(EndDay);
                    break;
                case "wheel":
                    AddLog(new LogEntry(Head, Loc.Tr("You find an old *lucky wheel* nailed to a fence post.")));
                    Overlay = new Overlay
                    {
                        Kind = OverlayKind.Wheel, Title = Loc.Tr("Lucky wheel"),
                        WheelIndex = (int)Math.Floor(Rng.Next() * Content.Wheel.Length),
                    };
                    SpinWait = 0;
                    Mode = RunMode.Spin;
                    break;
                case "chest":
                    AddLog(new LogEntry(Head, Loc.Tr("A mossy *chest* sits by the roadside.")));
                    Card(new Overlay { Kind = OverlayKind.ChestClosed, Title = Loc.Tr("Treasure!") }, () =>
                    {
                        var l = Rules.RollLoot(Rng, Hero.Class.LootLuck);
                        Rules.Equip(Hero, l);
                        AddLog(new LogEntry(Head, Loc.Tr("You prise open the chest and find *{item}*.", "item", Loc.Tr(l.Name)),
                            "+" + l.V + " " + StatName(l.Stat)));
                        Card(new Overlay { Kind = OverlayKind.Chest, Title = Loc.Tr("Treasure!"), Loot = l }, EndDay);
                    });
                    break;
                case "choice":
                    var c = Content.Choices[Day % Content.Choices.Length];
                    AddLog(new LogEntry(Head, Loc.Tr(c.Text)));
                    var o = new Overlay { Kind = OverlayKind.Choice, Title = Loc.Tr("Make a choice"), Subtitle = Loc.Tr(c.Text) };
                    o.Options.Add(new PickOption(Loc.Tr(c.A.Label), Loc.Tr(c.A.Effect.Tag)));
                    o.Options.Add(new PickOption(Loc.Tr(c.B.Label), Loc.Tr(c.B.Effect.Tag)));
                    Pick(o, i =>
                    {
                        var opt = i == 0 ? c.A : c.B;
                        opt.Effect.Apply(Hero);
                        AddLog(new LogEntry(Head, Loc.Tr("You chose *{choice}*.", "choice", Loc.Tr(opt.Label)), Loc.Tr(opt.Effect.Tag)));
                        EndDay();
                    });
                    break;
                default:
                    StartFight(Rules.FoeFor(node == "boss" ? "boss" : node.Substring(4), Chapter));
                    break;
            }
        }

        void EndDay()
        {
            if (Day >= Content.Road.Length) return;
            Mode = RunMode.Walk;
        }

        void Pick(Overlay o, Action<int> then)
        {
            Overlay = o;
            PickTime = 0;
            _onPicked = then;
            Mode = RunMode.Pick;
        }

        void Card(Overlay o, Action then, bool auto = true)
        {
            Overlay = o;
            CardLeft = auto ? CardAuto : (double?)null;
            _onCardClosed = then;
            Mode = RunMode.Card;
        }

        void ChooseSkill(Action then)
        {
            var keys = Rules.SkillOffer(Rng);
            var o = new Overlay { Kind = OverlayKind.Pick, Title = Loc.Tr("Choose a skill") };
            foreach (var k in keys)
                o.Options.Add(new PickOption(Loc.Tr(Content.Skills[k].Name), Loc.Tr(Content.Skills[k].Text), k));
            Pick(o, i =>
            {
                var s = Content.Skills[keys[i]];
                Rules.ApplySkill(Hero, s.Key);
                AddLog(new LogEntry(Head, Loc.Tr("You learned *{skill}*.", "skill", Loc.Tr(s.Name)), Loc.Tr(s.Text)));
                then();
            });
        }

        void StartSpin()
        {
            var o = Overlay;
            if (o == null || o.Kind != OverlayKind.Wheel) return;
            o.Spinning = true;
            Wait(WheelSpin, () =>
            {
                var s = Content.Wheel[o.WheelIndex];
                if (s.Skill)
                {
                    Overlay = null;
                    ChooseSkill(EndDay);
                    return;
                }
                if (s.Xp > 0) LevelUp(Rules.GainXp(Hero, s.Xp));
                else s.Apply(Hero);
                o.Result = Loc.Tr(s.Label) + "!";
                AddLog(new LogEntry(Head, Loc.Tr("The wheel clicks to a stop."), Loc.Tr(s.Label)));
                Card(o, EndDay);
            });
        }

        // ----------------------------------------------------------- fights

        void StartFight(Foe f)
        {
            AddLog(new LogEntry(Head,
                f.Boss
                    ? Loc.Tr("The ground shakes. *{foe}* blocks the way! “{quote}”", "foe", Loc.TrName(f.Name), "quote", Loc.Tr(f.Quote))
                    : Loc.Tr("A *{foe}* blocks the road! “{quote}”", "foe", Loc.TrName(f.Name), "quote", Loc.Tr(f.Quote)),
                null, LogKind.Fight));
            Battle = new Battle(Hero, f, Rng, SpecialCharge);
            Round = 0;
            Fx.Add(new RunFx { Kind = FxKind.FoeEnter, Boss = f.Boss });
            Fx.Add(new RunFx { Kind = FxKind.Banner, Text = Loc.Tr(f.Boss ? "BOSS!" : "FIGHT!"), Seconds = (float)(f.Boss ? BossIntro : FightIntro) });
            Wait(f.Boss ? BossIntro : FightIntro, () => Mode = RunMode.Fight);
        }

        void Strike()
        {
            Round++;
            Events(Battle.Rep());
        }

        void Events(List<BattleEvent> events)
        {
            var b = Battle;
            if (b == null) return;
            foreach (var e in events)
            {
                if (e is StrikeEvent se)
                {
                    Fx.Add(new RunFx { Kind = FxKind.Lunge, ByHero = true });
                    foreach (var x in se.Blows) BlowFx(x);
                }
                else if (e is SpecialEvent sp)
                {
                    Fx.Add(new RunFx { Kind = FxKind.Special, Text = Loc.Tr(sp.Name) });
                    Fx.Add(new RunFx { Kind = FxKind.Shake, Strength = 1 });
                    foreach (var x in sp.Blows) BlowFx(x);
                    if (sp.Healed > 0) Fx.Add(Float("+" + sp.Healed, FloatKind.Heal, false));
                    if (sp.SelfHit > 0) Fx.Add(Float("−" + sp.SelfHit, FloatKind.Hurt, false));
                }
                else if (e is FoeHitEvent fh)
                {
                    Fx.Add(new RunFx { Kind = FxKind.Lunge, ByHero = false });
                    Fx.Add(Float("−" + fh.Dmg, FloatKind.Hurt, false));
                    if (fh.Heavy) Fx.Add(new RunFx { Kind = FxKind.Shake, Strength = 1.2f });
                    if (fh.Reflect > 0) Fx.Add(Float("−" + fh.Reflect, FloatKind.Reflect, true));
                    if (fh.Drained > 0) Fx.Add(Float("+" + fh.Drained, FloatKind.Heal, true));
                }
                else if (e is FoeAbilityEvent fa)
                {
                    if (fa.Tick)
                    {
                        Fx.Add(Float("−" + fa.Amount, FloatKind.Hurt, false));
                        continue;
                    }
                    Fx.Add(new RunFx { Kind = FxKind.Banner, Text = Loc.Tr(Abilities.Label(fa.Ability)), Seconds = 0.8f });
                    Fx.Add(new RunFx { Kind = FxKind.Ability, Ability = fa.Ability });
                    if (fa.Ability == FoeAbility.Regen) Fx.Add(Float("+" + fa.Amount, FloatKind.Heal, true));
                    if (fa.Ability == FoeAbility.Breath) Fx.Add(Float("−" + fa.Amount, FloatKind.Crit, false));
                }
                else if (e is WindupEvent)
                {
                    Fx.Add(new RunFx { Kind = FxKind.Windup });
                }
                else if (e is InterruptEvent)
                {
                    Fx.Add(new RunFx { Kind = FxKind.Interrupt });
                    Fx.Add(new RunFx { Kind = FxKind.Banner, Text = Loc.Tr("STAGGERED!"), Seconds = 0.7f });
                }
            }
            if (b.Over) EndFight(b.Won);
        }

        void BlowFx(Blow x)
        {
            if (x.Miss)
            {
                Fx.Add(new RunFx { Kind = FxKind.Dodge });
                Fx.Add(Float(Loc.Tr("MISS"), FloatKind.Reflect, true));
                return;
            }
            Fx.Add(new RunFx { Kind = FxKind.Slash, Crit = x.Crit });
            Fx.Add(Float((x.Crit ? Loc.Tr("CRIT") + " " : "") + "−" + x.Dmg, x.Crit ? FloatKind.Crit : FloatKind.Hit, true));
            if (x.Crit) Fx.Add(new RunFx { Kind = FxKind.Shake, Strength = 0.6f });
            if (x.Heal > 0) Fx.Add(Float("+" + x.Heal, FloatKind.Heal, false));
        }

        static RunFx Float(string text, FloatKind kind, bool onFoe) =>
            new RunFx { Kind = FxKind.Float, Text = text, Float = kind, OnFoe = onFoe };

        void EndFight(bool won)
        {
            SpecialCharge = Battle.Charge;
            var st = Battle.St;
            Battle = null;
            if (!won)
            {
                Wait(0.5, () => Defeat(st.Foe));
                return;
            }
            Fx.Add(new RunFx { Kind = FxKind.FoeDown, Boss = st.Foe.Boss });
            var windHeal = Hero.Stacks("wind") > 0 ? Rules.Heal(Hero, 0.2) : 0;
            var xp = st.Foe.Xp;
            LevelUp(Rules.GainXp(Hero, xp));
            var secs = st.Foe.Boss ? 1.3 : 0.8;
            Fx.Add(new RunFx
            {
                Kind = FxKind.Banner, Seconds = (float)secs,
                Text = Loc.Tr(st.Foe.Boss ? "CHAPTER CLEAR!" : st.Foe.Elite ? "Big win!" : "Victory!"),
            });
            Wait(secs, () =>
            {
                var tag = Loc.Tr("EXP") + " +" + xp + (windHeal > 0 ? " · " + Loc.Tr("Heal +{n}", "n", windHeal) : "");
                AddLog(new LogEntry(Loc.Tr("Victory"), Loc.Tr("The *{foe}* is defeated.", "foe", Loc.TrName(st.Foe.Name)), tag, LogKind.Win));
                if (st.Foe.Boss)
                {
                    Won = true;
                    BestDays = Math.Max(BestDays, Day);
                    Card(new Overlay
                    {
                        Kind = OverlayKind.Clear, Title = Loc.Tr("Victory"),
                        Subtitle = Loc.Tr("{boss} is down. Hero level {n}.", "boss", Loc.Tr(Chapter.Boss.Name), "n", Hero.Lv),
                        Survived = Day, Best = BestDays,
                    }, () => Mode = RunMode.Over, false);
                }
                else
                {
                    EndDay();
                }
            });
        }

        void Defeat(Foe f)
        {
            BestDays = Math.Max(BestDays, Day - 1);
            Card(new Overlay
            {
                Kind = OverlayKind.Defeat, Title = Loc.Tr("Defeat"),
                Subtitle = Loc.Tr("The {foe} won on day {n}. Better gear hits harder.", "foe", Loc.TrName(f.Name), "n", Day),
                Survived = Day - 1, Best = BestDays,
            }, () => Mode = RunMode.Over, false);
        }

        void LevelUp(int ups)
        {
            if (ups > 0) Fx.Add(new RunFx { Kind = FxKind.LevelUp, Text = Loc.Tr("Level {n}!", "n", Hero.Lv) });
        }

        void AddLog(LogEntry e) => Log.Insert(0, e);

        public static string StatName(string stat) =>
            stat == "hp" ? Loc.Tr("max HP") : stat == "atk" ? Loc.Tr("ATK") : Loc.Tr("DEF");
    }
}
