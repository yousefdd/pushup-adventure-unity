using System.Collections.Generic;
using DiceQuest.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiceQuest.Game
{
    /// <summary>The run: a ring of 24 tiles around the middle of the screen.
    /// Tap to roll; the hero hops tile by tile and the tile's event plays in
    /// the middle: a fight, a blessing, a curse, a chest, a shop or a lucky
    /// roll. The rules live in DiceRun; this draws and animates them.</summary>
    public sealed class BoardScreen : MonoBehaviour
    {
        enum Phase { Idle, Rolling, Moving, Fighting, Card, Over }

        const float W = Theme.Width, TopH = 52, StatsH = 58, BottomH = 140;
        const float HopTime = 0.24f, RollTime = 0.8f;

        DiceRun _run;
        Phase _phase;
        Move _move;
        Outcome _outcome;
        float _phaseT, _time;
        int _hop;
        bool _persisted, _boardBuilt;

        // Layout
        RectTransform _root, _boardBox, _board, _tiles, _inner, _idle, _battle, _card, _fx;
        RawImage _far, _near;
        float _cell;
        UText _title, _lapText, _hintText, _hint, _atk, _def, _lv, _gold;
        Bar _hp, _xp;
        Die _die;
        Fighter _token;
        CandyButton _action;
        Image _muteSlash;
        int _boardStage = -1, _boardLap = -1;
        readonly List<Image> _tileFrames = new List<Image>();

        // Battle
        Fighter _bHero, _bFoe;
        Bar _bHeroHp, _bFoeHp;
        UText _bFoeName;
        int _beat, _shownHeroHp = -1;
        float _heroLunge = -1, _foeLunge = -1, _heroFlash = -1, _foeFlash = -1, _foeFall = -1, _speed = 1;
        readonly List<Floating> _floats = new List<Floating>();

        public void Build(string heroId)
        {
            _run = new DiceRun(heroId, System.Environment.TickCount);
            _root = (RectTransform)transform;
            BuildTop();
            BuildStats();
            BuildBottom();
        }

        // =============================================================== build

        void BuildTop()
        {
            var top = Ui.Rect(_root, "Top").Band(true, 0, TopH);
            var close = Ui.Panel(top, "Close", Theme.SurfaceDeep, 20);
            close.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(40, 40));
            foreach (var a in new[] { 45f, -45f })
            {
                var bar = Ui.Img(close.rectTransform, "X", Color.white);
                bar.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 4));
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, a);
            }
            Tap.On(close, () => { Persist(); GameApp.I.ShowHome(); });

            var mute = Ui.Panel(top, "Sound", Theme.SurfaceDeep, 20);
            mute.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(40, 40));
            var box = Ui.Img(mute.rectTransform, "Box", Color.white);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7, 0), new Vector2(7, 9));
            var cone = Ui.Img(mute.rectTransform, "Cone", Color.white, Ui.Triangle);
            cone.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
            cone.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            _muteSlash = Ui.Img(mute.rectTransform, "Off", Theme.Hp);
            _muteSlash.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 4));
            _muteSlash.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            Tap.On(mute, () => { Theme.Muted = !Theme.Muted; Prefs.Save(); });

            _title = Ui.Label(top, "World", "", 22, Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_title.transform).Fill(60, 0, 60, 0);
        }

        void BuildStats()
        {
            var band = Ui.Img(_root, "Stats", Theme.Band).rectTransform.Band(true, TopH, StatsH);
            var lip = Ui.Img(band, "Lip", Theme.BandTop);
            lip.rectTransform.Band(true, 0, 4);
            // HP across the left half; level, attack, defence and gold to the right.
            _hp = new Bar(band, "Hp", Theme.Hp, 24, true);
            _hp.Root.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -2), new Vector2(190, 24));
            var heart = Ui.Label(band, "HpName", Loc.Tr("HP"), 12, Theme.TextMuted, TextAnchor.MiddleLeft);
            ((RectTransform)heart.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -4), new Vector2(60, 14));
            _xp = new Bar(band, "Xp", Theme.Xp, 6, false);
            _xp.Root.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 4), new Vector2(190, 6));
            var row = Ui.Rect(band, "Chips");
            row.anchorMin = new Vector2(0, 0);
            row.anchorMax = new Vector2(1, 1);
            row.offsetMin = new Vector2(212, 8);
            row.offsetMax = new Vector2(-10, -10);
            var g = row.Row(6);
            g.childForceExpandWidth = g.childForceExpandHeight = true;
            _lv = Chip(row, Loc.Tr("Lv."), Theme.GoldTop);
            _atk = Chip(row, Loc.Tr("ATK"), Theme.DangerTop);
            _def = Chip(row, Loc.Tr("DEF"), Theme.CardBorder);
            _gold = Chip(row, Loc.Tr("Gold"), Theme.Gold);
        }

        UText Chip(RectTransform row, string name, Color color)
        {
            var chip = Ui.Panel(row, name, Theme.BandChip, 10);
            var label = Ui.Label(chip.rectTransform, "Name", name, 11, Theme.TextMuted, TextAnchor.MiddleCenter);
            ((RectTransform)label.transform).Band(true, 1, 14);
            var v = Ui.Label(chip.rectTransform, "Value", "", 19, color, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)v.transform).Fill(2, 2, 2, 14);
            return v;
        }

        void BuildBoard()
        {
            _boardBox = Ui.Rect(_root, "BoardArea");
            _boardBox.anchorMin = Vector2.zero;
            _boardBox.anchorMax = Vector2.one;
            _boardBox.offsetMin = new Vector2(0, BottomH);
            _boardBox.offsetMax = new Vector2(0, -(TopH + StatsH + 6));

            // The board's size follows the space: six tiles across, eight down.
            var areaH = Mathf.Max(400, _root.rect.height - BottomH - TopH - StatsH - 6);
            _cell = Mathf.Floor(Mathf.Min((W - 20) / 6f, areaH / 8f));
            var frame = Ui.Panel(_boardBox, "Board", Theme.Outline, 16);
            _board = frame.rectTransform;
            _board.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(_cell * 6 + 8, _cell * 8 + 8));
            var mask = Ui.Rect(_board, "Scenery").Fill(4, 4, 4, 4);
            mask.gameObject.AddComponent<RectMask2D>();
            _far = Backdrop(mask);
            _near = Backdrop(mask);
            var dim = Ui.Img(mask, "Dim", new Color(0.05f, 0.05f, 0.1f, 0.35f));
            dim.rectTransform.Fill();

            _tiles = Ui.Rect(_board, "Tiles").Fill(4, 4, 4, 4);
            _inner = Ui.Rect(_board, "Middle");
            _inner.anchorMin = _inner.anchorMax = new Vector2(0, 1);
            _inner.pivot = new Vector2(0, 1);
            _inner.anchoredPosition = new Vector2(4 + _cell + 6, -(4 + _cell + 6));
            _inner.sizeDelta = new Vector2(_cell * 4 - 12, _cell * 6 - 12);

            BuildIdle();
            BuildBattle();
            _card = Ui.Rect(_inner, "Card").Fill();

            _token = new Fighter(_board, "Hero", _cell * 1.05f);
            _token.Root.anchorMin = _token.Root.anchorMax = new Vector2(0, 1);
            _token.SetArt(_run.Hero.Def.Sprite, _cell * 1.05f, false);
            _token.Root.anchoredPosition = TileFoot(_run.Pos);

            _fx = Ui.Rect(_board, "Fx").Fill();
            RebuildTiles();
        }

        RawImage Backdrop(RectTransform parent)
        {
            var r = Ui.Rect(parent, "Layer").gameObject.AddComponent<RawImage>();
            r.rectTransform.Fill();
            r.raycastTarget = false;
            return r;
        }

        void BuildIdle()
        {
            _idle = Ui.Rect(_inner, "Idle").Fill();
            var hit = Ui.Img(_idle, "Tap", new Color(0, 0, 0, 0));
            hit.rectTransform.Fill();
            Tap.On(hit, Roll);
            _lapText = Ui.Label(_idle, "Lap", "", 22, Color.white, TextAnchor.MiddleCenter, true, true, true);
            ((RectTransform)_lapText.transform).Band(true, 10, 64);
            _die = new Die(_idle, 116);
            _die.Root.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(116, 116));
            _hintText = Ui.Label(_idle, "Hint", Loc.Tr("Tap to roll"), 18, Theme.TextMuted, TextAnchor.MiddleCenter, false, true, true);
            ((RectTransform)_hintText.transform).Band(false, 40, 30);
        }

        void BuildBattle()
        {
            _battle = Ui.Panel(_inner, "Battle", new Color(0.06f, 0.06f, 0.1f, 0.78f), 14).rectTransform;
            _battle.Fill();
            var hit = _battle.GetComponent<Image>();
            Tap.On(hit, () => _speed = 3);
            _bFoeName = Ui.Label(_battle, "FoeName", "", 22, Color.white, TextAnchor.MiddleCenter, true, true, true);
            ((RectTransform)_bFoeName.transform).Band(true, 10, 60);
            var s = Mathf.Min(_inner.sizeDelta.x * 0.46f, 140);
            _bHero = new Fighter(_battle, "Hero", s);
            _bHero.Root.anchorMin = _bHero.Root.anchorMax = new Vector2(0.27f, 0);
            _bHero.Root.anchoredPosition = new Vector2(0, 110);
            _bFoe = new Fighter(_battle, "Foe", s);
            _bFoe.Root.anchorMin = _bFoe.Root.anchorMax = new Vector2(0.73f, 0);
            _bFoe.Root.anchoredPosition = new Vector2(0, 110);
            _bHeroHp = new Bar(_battle, "HeroHp", Theme.Hp, 20, true);
            _bHeroHp.Root.Place(new Vector2(0.27f, 0), new Vector2(0.5f, 0), new Vector2(0, 76), new Vector2(s * 0.9f, 20));
            _bFoeHp = new Bar(_battle, "FoeHp", Theme.Hp, 20, true);
            _bFoeHp.Root.Place(new Vector2(0.73f, 0), new Vector2(0.5f, 0), new Vector2(0, 76), new Vector2(s * 0.9f, 20));
            var hint = Ui.Label(_battle, "Faster", "»»", 22, Theme.TextMuted, TextAnchor.MiddleCenter, true);
            ((RectTransform)hint.transform).Band(false, 20, 30);
        }

        void BuildBottom()
        {
            var bottom = Ui.Rect(_root, "Bottom").Band(false, 0, BottomH);
            _action = Ui.Button(bottom, "Action", Theme.Accent, Theme.AccentTop, OnAction);
            ((RectTransform)_action.transform).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(W * 0.62f, 84));
            _hint = Ui.Label(bottom, "Hint", "", 14, Theme.TextMuted, TextAnchor.MiddleCenter, false, true);
            ((RectTransform)_hint.transform).Band(false, 8, 28);
        }

        // ============================================================== board

        /// <summary>Ring position → column and row (row 0 at the top). Camp is
        /// the bottom-left corner; the path runs right, up, left and down.</summary>
        static Vector2Int Cell(int i)
        {
            if (i <= 5) return new Vector2Int(i, 7);
            if (i <= 12) return new Vector2Int(5, 12 - i);
            if (i <= 17) return new Vector2Int(17 - i, 0);
            return new Vector2Int(0, i - 17);
        }

        Vector2 TileCenter(int i)
        {
            var c = Cell(i);
            return new Vector2(4 + (c.x + 0.5f) * _cell, -(4 + (c.y + 0.5f) * _cell));
        }

        Vector2 TileFoot(int i) => TileCenter(i) + new Vector2(0, -_cell * 0.42f);

        void RebuildTiles()
        {
            _boardStage = _run.Stage;
            _boardLap = _run.Lap;
            var ch = _run.Chapter;
            _far.texture = Theme.Art(ch.Far)?.texture;
            _near.texture = Theme.Art(ch.Near)?.texture;
            Ui.Clear(_tiles);
            _tileFrames.Clear();
            for (var i = 0; i < DiceRun.Size; i++)
            {
                var kind = _run.Board[i];
                var bossHere = i == 0 && _run.Lap >= DiceRun.LapsPerStage;
                Color rim;
                switch (kind)
                {
                    case TileKind.Enemy: rim = Theme.Danger; break;
                    case TileKind.Elite: rim = Theme.Special; break;
                    case TileKind.Buff: rim = Theme.Accent; break;
                    case TileKind.Curse: rim = Theme.Defeat; break;
                    case TileKind.Shop: rim = Theme.Gold; break;
                    case TileKind.Treasure: rim = Theme.Orange; break;
                    case TileKind.Luck: rim = Theme.CardBorder; break;
                    default: rim = bossHere ? Theme.Hp : Theme.GoldTop; break;
                }
                var frame = Ui.Panel(_tiles, i.ToString(), rim, 12);
                frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0, 1);
                frame.rectTransform.anchoredPosition = TileCenter(i) - new Vector2(4, -4);
                frame.rectTransform.sizeDelta = new Vector2(_cell - 6, _cell - 6);
                _tileFrames.Add(frame);
                var face = Ui.Panel(frame.rectTransform, "Face", new Color(Theme.Surface.r, Theme.Surface.g, Theme.Surface.b, 0.92f), 10);
                face.rectTransform.Fill(4, 4, 4, 4);
                TileIcon(face.rectTransform, i, kind, bossHere);
            }
        }

        void TileIcon(RectTransform parent, int i, TileKind kind, bool bossHere)
        {
            var s = _cell - 18;
            string art = null;
            switch (kind)
            {
                case TileKind.Start: art = bossHere ? _run.Chapter.Boss.Img : "pet_pip"; break;
                case TileKind.Enemy:
                case TileKind.Elite: art = _run.FoeAt(i).Img; break;
                case TileKind.Buff: art = "gear_star"; break;
                case TileKind.Curse: art = "foe_hex_grimoire"; break;
                case TileKind.Shop: art = "gear_ruby"; break;
            }
            if (art != null)
            {
                var img = Widgets.Sprite(parent, "Icon", art, s);
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
            }
            else if (kind == TileKind.Treasure)
            {
                var c = Widgets.Chest(parent, s * 0.8f);
                c.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 0.8f, s * 0.64f));
            }
            else if (kind == TileKind.Luck)
            {
                var d = new Die(parent, s * 0.7f);
                d.Root.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 0.7f, s * 0.7f));
                d.Set(6);
            }
            if (kind == TileKind.Elite || bossHere)
            {
                var tag = Ui.Panel(parent, "Tag", bossHere ? Theme.Hp : Theme.Special, 6);
                tag.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(_cell * 0.8f, 16));
                var t = Ui.Label(tag.rectTransform, "Text", Loc.Tr(bossHere ? "BOSS" : "Elite"), 11, Color.white, TextAnchor.MiddleCenter, true);
                ((RectTransform)t.transform).Fill();
            }
        }

        // ============================================================= phases

        void SetPhase(Phase p)
        {
            _phase = p;
            _phaseT = 0;
            _idle.gameObject.SetActive(p == Phase.Idle || p == Phase.Rolling || p == Phase.Moving);
            _battle.gameObject.SetActive(p == Phase.Fighting);
            _card.gameObject.SetActive(p == Phase.Card || p == Phase.Over);
            if (p == Phase.Idle)
            {
                if (_boardStage != _run.Stage || _boardLap != _run.Lap) RebuildTiles();
                var bossNext = _run.Lap >= DiceRun.LapsPerStage;
                _lapText.Set(Loc.Tr("Lap {n}/{m}", "n", _run.Lap, "m", DiceRun.LapsPerStage) +
                             (bossNext ? "\n" + Loc.Tr("Boss next!") : ""));
                _lapText.SetColor(bossNext ? Theme.DangerTop : Color.white);
                _hintText.gameObject.SetActive(true);
            }
        }

        void OnAction()
        {
            switch (_phase)
            {
                case Phase.Idle: Roll(); break;
                case Phase.Fighting: _speed = 3; break;
                case Phase.Card: CloseCard(); break;
                case Phase.Over:
                    Persist();
                    GameApp.I.ShowHome();
                    break;
            }
        }

        void Roll()
        {
            if (_phase != Phase.Idle) return;
            _move = _run.Roll();
            _hop = 0;
            _hintText.gameObject.SetActive(false);
            Theme.Play("whoosh", 0.7f);
            SetPhase(Phase.Rolling);
        }

        void Update()
        {
            if (_run == null) return;
            if (!_boardBuilt)
            {
                // Built on the first frame, once the canvas knows its size.
                Canvas.ForceUpdateCanvases();
                BuildBoard();
                _boardBuilt = true;
                SetPhase(Phase.Idle);
                return;
            }
            var dt = Mathf.Min(Time.deltaTime, 0.1f);
            _time += dt;
            _phaseT += dt;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) OnAction();

            switch (_phase)
            {
                case Phase.Idle:
                    _die.Root.localScale = Vector3.one * (1 + 0.04f * Mathf.Sin(_time * 5));
                    _die.Root.localRotation = Quaternion.identity;
                    break;
                case Phase.Rolling: UpdateRolling(); break;
                case Phase.Moving: UpdateMoving(); break;
                case Phase.Fighting: UpdateFighting(dt); break;
            }
            UpdateToken();
            UpdateFloats(dt);
            DrawStats();
            DrawAction();
            _muteSlash.gameObject.SetActive(Theme.Muted);
            // The scenery drifts slowly behind the board.
            Slide(_far, _time * 3);
            Slide(_near, _time * 8);
        }

        void UpdateRolling()
        {
            if (_phaseT < RollTime)
            {
                // Tumbling: a new face every few frames, spinning and hopping.
                if (Mathf.FloorToInt(_phaseT * 14) != Mathf.FloorToInt((_phaseT - Time.deltaTime) * 14))
                {
                    _die.Set(Random.Range(1, 7));
                    Theme.Play("tick", 0.3f);
                }
                _die.Root.localRotation = Quaternion.Euler(0, 0, _phaseT / RollTime * 720);
                _die.Root.localScale = Vector3.one * (1 + 0.25f * Mathf.Sin(_phaseT / RollTime * Mathf.PI));
                return;
            }
            _die.Set(_move.Value);
            _die.Root.localRotation = Quaternion.identity;
            _die.Root.localScale = Vector3.one * Mathf.Lerp(1.3f, 1, Mathf.Clamp01((_phaseT - RollTime) / 0.2f));
            if (_phaseT > RollTime + 0.35f) SetPhase(Phase.Moving);
        }

        void UpdateMoving()
        {
            if (_hop >= _move.Path.Count)
            {
                Arrive();
                return;
            }
            var from = _hop == 0 ? _move.From : _move.Path[_hop - 1];
            var to = _move.Path[_hop];
            var k = Mathf.Clamp01(_phaseT / HopTime);
            _token.Root.anchoredPosition = Vector2.Lerp(TileFoot(from), TileFoot(to), k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * _cell * 0.35f);
            if (k < 1) return;
            _phaseT = 0;
            _hop++;
            Pulse(to);
            Theme.Play("pick", 0.35f);
            if (to == 0 && !(_move.Boss && _hop == _move.Path.Count) && _move.CampGold > 0)
                Float(TileCenter(0), "+" + _move.CampGold + " " + Loc.Tr("Gold"), Theme.GoldTop);
        }

        void Arrive()
        {
            _outcome = _run.Land(_move);
            if (_move.Boss)
            {
                Float(TileCenter(0), Loc.Tr("BOSS"), Theme.DangerTop, 1.6f);
                Theme.Play("fight");
            }
            if (_outcome.Fight != null) StartFight(_outcome.Fight);
            else ShowCard(_outcome);
        }

        void Pulse(int i)
        {
            if (i < 0 || i >= _tileFrames.Count) return;
            _tileFrames[i].transform.localScale = Vector3.one * 1.15f;
        }

        void UpdateToken()
        {
            var b = Mathf.Sin(_time * 4) * 0.5f + 0.5f;
            _token.Breathe(b);
            if (_phase == Phase.Idle) _token.Root.anchoredPosition = TileFoot(_run.Pos);
            foreach (var f in _tileFrames) f.transform.localScale = Vector3.Lerp(f.transform.localScale, Vector3.one, Time.deltaTime * 8);
        }

        // ============================================================ fights

        void StartFight(Fight f)
        {
            var foe = f.Foe;
            _bFoeName.Set(Loc.Tr(foe.Kind.Name) + (foe.Boss ? " · " + Loc.Tr("BOSS") : foe.Elite ? " · " + Loc.Tr("Elite") : ""));
            _bFoeName.SetColor(foe.Boss ? Theme.DangerTop : foe.Elite ? Theme.SpecialTop : Color.white);
            var s = Mathf.Min(_inner.sizeDelta.x * 0.46f, 140);
            _bHero.SetArt(_run.Hero.Def.Sprite, s, false);
            _bFoe.SetArt(foe.Kind.Img, foe.Boss ? s * 1.2f : s, foe.Kind.Front);
            _bFoe.Pic.color = Color.white;
            _bFoe.Root.localRotation = Quaternion.identity;
            _beat = 0;
            _speed = 1;
            _foeFall = -1;
            _shownHeroHp = f.HeroHpBefore;
            _bHeroHp.Set(f.HeroHpBefore / (float)_run.Hero.Max, f.HeroHpBefore.ToString());
            _bFoeHp.Set(1, foe.Hp.ToString());
            SetPhase(Phase.Fighting);
            if (!_move.Boss) Theme.Play("fight", 0.6f);
        }

        void UpdateFighting(float dt)
        {
            var f = _outcome.Fight;
            var t = _phaseT * 1.3f;
            // Fast forward: the fight's clock runs faster once tapped.
            if (_speed > 1) _phaseT += dt * (_speed - 1);
            while (_beat < f.Beats.Count && f.Beats[_beat].T + 0.4 <= t) PlayBeat(f.Beats[_beat++]);

            Step(ref _heroLunge, dt, 0.3f);
            Step(ref _foeLunge, dt, 0.3f);
            Step(ref _heroFlash, dt, 0.25f);
            Step(ref _foeFlash, dt, 0.25f);
            var gap = _inner.sizeDelta.x * 0.22f;
            _bHero.Root.anchoredPosition = new Vector2(Dash(_heroLunge) * gap, 110);
            _bFoe.Root.anchoredPosition = new Vector2(-Dash(_foeLunge) * gap, 110);
            _bHero.SetFlash(_heroFlash >= 0 && _heroFlash < 0.6f ? 0.85f : 0);
            _bFoe.SetFlash(_foeFlash >= 0 && _foeFlash < 0.6f ? 0.85f : 0);
            var br = Mathf.Sin(_time * 4) * 0.5f + 0.5f;
            _bHero.Breathe(br);
            _bFoe.Breathe(1 - br);
            _bHeroHp.Set(_shownHeroHp / (float)_run.Hero.Max, _shownHeroHp.ToString());

            if (_beat < f.Beats.Count) return;
            // All blows struck: the loser falls, then the result card.
            if (_foeFall < 0)
            {
                _foeFall = 0;
                Theme.Play(f.Won ? "victory" : "defeat");
            }
            _foeFall += dt;
            var fall = Mathf.Clamp01(_foeFall / 0.5f);
            var loser = f.Won ? _bFoe : _bHero;
            loser.Root.localRotation = Quaternion.Euler(0, 0, (f.Won ? -1 : 1) * fall * 80);
            loser.Pic.color = new Color(1, 1, 1, 1 - fall * 0.7f);
            if (_foeFall > 1.0f)
            {
                _bHero.Root.localRotation = Quaternion.identity;
                _bHero.Pic.color = Color.white;
                ShowCard(_outcome);
            }
        }

        void PlayBeat(Beat b)
        {
            if (b.ByHero)
            {
                _heroLunge = 0;
                _foeFlash = 0;
                Float(BattlePoint(false), (b.Crit ? "!" : "") + "−" + b.Dmg, b.Crit ? Theme.GoldTop : Color.white, b.Crit ? 1.3f : 1);
                _bFoeHp.Set(b.FoeHp / (float)_outcome.Fight.Foe.Hp, b.FoeHp.ToString());
                Theme.Play(b.Crit ? "crit" : "hit", 0.7f);
            }
            else
            {
                _foeLunge = 0;
                _heroFlash = 0;
                Float(BattlePoint(true), "−" + b.Dmg, Theme.Hp);
                Theme.Play("hurt", 0.6f);
            }
            _shownHeroHp = b.HeroHp;
        }

        /// <summary>Above a fighter in the battle panel, in board coordinates.</summary>
        Vector2 BattlePoint(bool hero)
        {
            var size = _inner.sizeDelta;
            var fighter = Mathf.Min(size.x * 0.46f, 140);
            return _inner.anchoredPosition + new Vector2(size.x * (hero ? 0.27f : 0.73f), -size.y + 110 + fighter);
        }

        static void Step(ref float t, float dt, float seconds)
        {
            if (t < 0) return;
            t += dt / seconds;
            if (t >= 1) t = -1;
        }

        static float Dash(float v)
        {
            if (v <= 0 || v >= 1) return 0;
            return v < 0.35f ? Ui.OutCubic(v / 0.35f) : 1 - Ui.OutCubic((v - 0.35f) / 0.65f);
        }

        // ============================================================= cards

        void ShowCard(Outcome o)
        {
            Ui.Clear(_card);
            var panel = Ui.Panel(_card, "Panel", Theme.Card, 16);
            panel.rectTransform.Fill();
            var rim = panel.gameObject.AddComponent<Outline>();
            rim.effectColor = o.Good ? Theme.CardBorder : Theme.Hp;
            rim.effectDistance = new Vector2(3, -3);
            var col = panel.rectTransform.Column(8, TextAnchor.UpperCenter);
            col.padding = new RectOffset(14, 14, 16, 14);
            col.childForceExpandWidth = true;

            var title = Ui.Label(panel.rectTransform, "Title", o.Title, 28, o.Good ? Theme.CardHead : Theme.Danger, TextAnchor.MiddleCenter, true);
            title.Size(h: 38);
            var iconBox = Ui.Rect(panel.rectTransform, "Icon");
            iconBox.Size(h: o.Kind == OutcomeKind.Shop ? 0 : 110);
            if (o.Kind == OutcomeKind.Luck)
            {
                var d = new Die(iconBox, 90);
                d.Root.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
                d.Set(o.Die);
            }
            else if (o.Kind == OutcomeKind.Treasure && string.IsNullOrEmpty(o.Icon))
            {
                Widgets.Chest(iconBox, 100).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 80));
            }
            else if (!string.IsNullOrEmpty(o.Icon) && o.Kind != OutcomeKind.Shop)
            {
                var glow = Ui.Img(iconBox, "Glow", new Color(1, 1, 1, 0.5f), Ui.Glow);
                glow.color = o.Good ? new Color(Theme.GoldTop.r, Theme.GoldTop.g, Theme.GoldTop.b, 0.55f) : new Color(Theme.Hp.r, Theme.Hp.g, Theme.Hp.b, 0.45f);
                glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
                var pic = Widgets.Sprite(iconBox, "Pic", o.Icon, 104);
                pic.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 104));
                if (o.Kind == OutcomeKind.Fight && !o.Good) pic.color = new Color(1, 0.6f, 0.6f);
            }
            var text = Ui.Label(panel.rectTransform, "Text", o.Text ?? "", 17, Theme.TextOnCard, TextAnchor.UpperCenter, false, true);
            text.Size(flexH: o.Kind == OutcomeKind.Shop ? 0 : 1);
            if (o.Kind == OutcomeKind.Shop) BuildShop(panel.rectTransform, o);

            Theme.Play(o.Kind == OutcomeKind.Curse || !o.Good ? "hurt" : o.Kind == OutcomeKind.Shop ? "pick" : "levelup", 0.7f);
            SetPhase(Phase.Card);
        }

        void BuildShop(RectTransform panel, Outcome o)
        {
            foreach (var item in o.Shop)
            {
                var it = item;
                var row = Ui.Panel(panel, it.Key, Theme.Paper, 12);
                row.Size(h: 74);
                var rr = row.rectTransform;
                var pic = Widgets.Sprite(rr, "Pic", it.Icon, 56);
                pic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(56, 56));
                var name = Ui.Label(rr, "Name", it.Name, 17, Theme.CardHead, TextAnchor.MiddleLeft, true);
                var nr = (RectTransform)name.transform;
                nr.anchorMin = new Vector2(0, 0.5f);
                nr.anchorMax = new Vector2(1, 1);
                nr.offsetMin = new Vector2(70, 0);
                nr.offsetMax = new Vector2(-104, -6);
                var desc = Ui.Label(rr, "Text", it.Text, 14, Theme.TextOnCardMuted, TextAnchor.MiddleLeft);
                var dr = (RectTransform)desc.transform;
                dr.anchorMin = new Vector2(0, 0);
                dr.anchorMax = new Vector2(1, 0.5f);
                dr.offsetMin = new Vector2(70, 6);
                dr.offsetMax = new Vector2(-104, 0);
                CandyButton buy = null;
                buy = Ui.Button(rr, "Buy", Theme.Gold, Theme.GoldTop, () =>
                {
                    if (!_run.Buy(it))
                    {
                        Theme.Play("hurt", 0.4f);
                        return;
                    }
                    Theme.Play("levelup", 0.6f);
                    PaintBuy(buy, it);
                });
                ((RectTransform)buy.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(90, 58));
                buy.Title.Text.fontSize = 22;
                PaintBuy(buy, it);
            }
        }

        void PaintBuy(CandyButton b, ShopItem it)
        {
            if (it.Sold)
            {
                b.SetText(Loc.Tr("Sold"));
                b.Paint(Theme.Surface, Theme.TextMuted);
                b.Interactable = false;
                return;
            }
            b.SetText(it.Price.ToString(), Loc.Tr("Buy"));
            var afford = _run.Hero.Gold >= it.Price;
            if (afford) b.Paint(Theme.Gold, Theme.GoldTop);
            else b.Paint(Theme.Surface, Theme.TextMuted);
        }

        void CloseCard()
        {
            if (_run.Over)
            {
                ShowEnd();
                return;
            }
            Theme.Play("pick", 0.5f);
            SetPhase(Phase.Idle);
        }

        void ShowEnd()
        {
            Persist();
            Ui.Clear(_card);
            var won = _run.Won;
            var panel = Ui.Panel(_card, "Panel", Theme.Surface, 16);
            panel.rectTransform.Fill();
            var col = panel.rectTransform.Column(10, TextAnchor.MiddleCenter);
            col.padding = new RectOffset(12, 12, 16, 16);
            var ribbon = Widgets.Ribbon(panel.rectTransform, Loc.Tr(won ? "Legend!" : "Defeat"), won ? Theme.Gold : Theme.DefeatTop, _inner.sizeDelta.x - 40);
            ribbon.Size(h: 64);
            var pic = Widgets.Sprite(panel.rectTransform, "Hero", _run.Hero.Def.Sprite, 110);
            pic.Size(h: 110);
            if (!won) pic.color = new Color(0.6f, 0.6f, 0.7f);
            var worlds = Ui.Label(panel.rectTransform, "Worlds", Loc.Tr("Worlds cleared: {n}", "n", won ? WorldData.Chapters.Length : _run.Stage),
                22, Theme.GoldTop, TextAnchor.MiddleCenter, true, true, true);
            worlds.Size(h: 34);
            var stats = Ui.Label(panel.rectTransform, "Stats", Loc.Tr("Rolls: {n} · Foes beaten: {k}", "n", _run.Rolls, "k", _run.Kills),
                16, Theme.Text, TextAnchor.MiddleCenter, false, true);
            stats.Size(h: 48);
            SetPhase(Phase.Over);
        }

        /// <summary>Remembers the furthest world reached, once per run.</summary>
        void Persist()
        {
            if (_persisted || _run == null) return;
            if (!_run.Over && _run.Stage == 0) return;
            _persisted = _run.Over;
            Prefs.BestWorld = _run.Won ? WorldData.Chapters.Length : _run.Stage + 1;
        }

        void OnDestroy() => Persist();

        // ============================================================ drawing

        void DrawStats()
        {
            var h = _run.Hero;
            var ch = _run.Chapter;
            _title.Set(Loc.Tr("World {n}", "n", _run.Stage + 1) + " · " + Loc.Tr(ch.Name));
            var hp = _phase == Phase.Fighting ? _shownHeroHp : h.Hp;
            _hp.Set(hp / (float)h.Max, hp + "/" + h.Max);
            _xp.Set(h.Xp / (float)h.XpToNext);
            _lv.Set(h.Lv.ToString());
            _atk.Set(h.Atk.ToString());
            _def.Set(h.Armor.ToString());
            _gold.Set(h.Gold.ToString());
        }

        void DrawAction()
        {
            string label, sub = null;
            Color face = Theme.Surface, top = Theme.TextMuted;
            switch (_phase)
            {
                case Phase.Idle:
                    label = Loc.Tr("Roll");
                    face = Theme.Accent;
                    top = Theme.AccentTop;
                    break;
                case Phase.Rolling: label = Loc.Tr("Rolling…"); break;
                case Phase.Moving: label = Loc.Tr("Moving…"); break;
                case Phase.Fighting: label = Loc.Tr("Fighting…"); sub = "»»"; break;
                case Phase.Card:
                    var shop = _outcome != null && _outcome.Kind == OutcomeKind.Shop;
                    label = Loc.Tr(shop ? "Leave" : "Continue");
                    face = Theme.Orange;
                    top = Theme.OrangeTop;
                    break;
                default:
                    label = Loc.Tr("Back to camp");
                    face = Theme.Orange;
                    top = Theme.OrangeTop;
                    break;
            }
            _action.SetText(label, sub);
            _action.Paint(face, top);
            _action.transform.localScale = Vector3.one * (_phase == Phase.Idle ? 1 + 0.025f * Mathf.Sin(_time * 6) : 1);
            _hint.Set(_phase == Phase.Idle ? Loc.Tr("Tap to roll") : "");
        }

        /// <summary>The painting fills the board by height and repeats sideways,
        /// shifted px units.</summary>
        static void Slide(RawImage img, float px)
        {
            if (img.texture == null) return;
            var r = img.rectTransform.rect;
            var tileW = Mathf.Max(1, r.height * img.texture.width / img.texture.height);
            img.uvRect = new Rect(px / tileW, 0, r.width / tileW, 1);
        }

        // ============================================================ floats

        void Float(Vector2 at, string text, Color c, float scale = 1)
        {
            var label = Ui.Label(_fx, "Float", text, 26, c, TextAnchor.MiddleCenter, true, false, true);
            var rt = (RectTransform)label.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(240, 40);
            rt.anchoredPosition = at;
            _floats.Add(new Floating { Text = label, From = at, Scale = scale });
        }

        void UpdateFloats(float dt)
        {
            for (var i = _floats.Count - 1; i >= 0; i--)
            {
                var f = _floats[i];
                f.T += dt / 0.9f;
                if (f.T >= 1)
                {
                    Destroy(f.Text.gameObject);
                    _floats.RemoveAt(i);
                    continue;
                }
                var rt = (RectTransform)f.Text.transform;
                rt.anchoredPosition = f.From + new Vector2(0, Ui.OutCubic(f.T) * 46);
                var pop = f.T < 0.15f ? Mathf.Lerp(0.6f, 1.3f, f.T / 0.15f) : Mathf.Lerp(1.3f, 1, (f.T - 0.15f) / 0.85f);
                rt.localScale = Vector3.one * pop * f.Scale;
                var c = f.Text.Text.color;
                c.a = f.T < 0.7f ? 1 : (1 - f.T) / 0.3f;
                f.Text.Text.color = c;
                _floats[i] = f;
            }
        }

        struct Floating
        {
            public UText Text;
            public Vector2 From;
            public float T, Scale;
        }
    }
}
