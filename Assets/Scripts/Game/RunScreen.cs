using System.Collections.Generic;
using System.Linq;
using PushupAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PushupAdventure.Game
{
    /// <summary>A run down one world's road. Top: the scene (scenery, road,
    /// hero, pet and foe). Then the hero's stats, learned skills and the story
    /// log. Bottom: the one big button, which is a pushup. The rules live in
    /// RunController; this draws its state and animates its effects.</summary>
    public sealed class RunScreen : MonoBehaviour
    {
        const float SceneH = Theme.Scene, W = Theme.Width;
        const float Feet = SceneH * 0.2f;
        const float ActionH = 128;

        RunController _run;
        ChapterDef _ch;
        bool _persisted;

        // Layout
        RectTransform _root, _scene, _shake, _fxLayer, _logList, _skillsRow, _modal, _modalBody, _dayIcons, _actions;
        RawImage _far, _near, _road;
        Fighter _hero, _pet, _foe, _dead;
        RectTransform _foeInfo, _windup, _heroBars, _foeBars, _chest;
        Image _foeGlow, _shield, _specialFlash;
        Image[] _windupPips;
        UText _foeName, _abilityTag, _dayLabel, _roundLabel, _hitsLabel, _hitsTitle, _hint, _lv, _hpText, _atk, _def;
        RectTransform _dayStrip, _hitsBox;
        Bar _heroHp, _special, _foeHp, _clock, _xp;
        UText _specialReady;
        CandyButton _action;
        CandyButton[] _choices;
        Image _muteSlash;

        // Effects
        readonly List<Floating> _floats = new List<Floating>();
        readonly List<BurstFx> _bursts = new List<BurstFx>();
        UText _banner, _specialBanner;
        float _bannerT = -1, _bannerFor, _specialT = -1;
        float _heroLunge = -1, _foeLunge = -1, _hurt = -1, _flash = -1, _enter = -1, _dodge = -1, _death = -1;
        float _walk = -1, _shakeT = -1, _shakeStrength, _flashT = -1, _chestT = -1, _time;
        float _scroll;
        int _hits, _logCount, _skillsSum = -1, _dayShown = -1, _floatSlot;
        Foe _shownFoe;

        // Modals
        Overlay _modalFor;
        OverlayKind _modalKind;
        RectTransform _wheel;
        float _wheelFrom, _wheelTo, _wheelT = -1;
        int _wheelTick;
        bool _wheelSpun;
        UText _wheelResult, _pickAuto;
        readonly List<Image> _pickCards = new List<Image>();

        public void Build(ChapterDef chapter, string heroClass)
        {
            _ch = chapter;
            _run = new RunController(chapter, heroClass, System.Environment.TickCount, Prefs.Best(chapter));
            _root = (RectTransform)transform;
            BuildScene();
            BuildStats();
            BuildLog();
            BuildActions();
            BuildModal();
            Theme.Play("whoosh", 0.6f);
        }

        // ================================================================ build

        void BuildScene()
        {
            var sceneBox = Ui.Rect(_root, "Scene").Band(true, 0, SceneH);
            sceneBox.gameObject.AddComponent<RectMask2D>();
            _shake = Ui.Rect(sceneBox, "Shake").Fill(-12, -12, -12, -12);
            _scene = Ui.Rect(_shake, "Stage").Fill(12, 12, 12, 12);

            var strip = SceneH * Theme.SceneryShare;
            var sand = Color.Lerp(Theme.Rgb(_ch.Ground), Theme.Road, 0.78f);
            _road = Ui.Rect(_scene, "Road").gameObject.AddComponent<RawImage>();
            _road.rectTransform.Fill(0, 0, 0, strip - 2);
            _road.texture = RoadTexture();
            _road.color = sand;
            _road.raycastTarget = false;
            _far = Backdrop("Far", _ch.Far, strip);
            _near = Backdrop("Near", _ch.Near, strip);
            var edge = Ui.Img(_scene, "Edge", new Color(0, 0, 0, 0.12f));
            edge.rectTransform.Band(true, strip - 2, 6);

            // Top row: leave, the days ahead (or the round), sound.
            var close = Ui.Panel(_scene, "Close", new Color(Theme.SurfaceDeep.r, Theme.SurfaceDeep.g, Theme.SurfaceDeep.b, 0.85f), 22);
            close.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(44, 44));
            foreach (var a in new[] { 45f, -45f })
            {
                var bar = Ui.Img(close.rectTransform, "X", Color.white);
                bar.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 4));
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, a);
            }
            Tap.On(close, () => { Persist(); GameApp.I.ShowHome(); });

            var mute = Ui.Panel(_scene, "Sound", new Color(Theme.SurfaceDeep.r, Theme.SurfaceDeep.g, Theme.SurfaceDeep.b, 0.85f), 20);
            mute.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -10), new Vector2(40, 40));
            var box = Ui.Img(mute.rectTransform, "Box", Color.white);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7, 0), new Vector2(7, 9));
            var cone = Ui.Img(mute.rectTransform, "Cone", Color.white, Ui.Triangle);
            cone.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(18, 18));
            cone.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            _muteSlash = Ui.Img(mute.rectTransform, "Off", Theme.Hp);
            _muteSlash.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 4));
            _muteSlash.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            Tap.On(mute, () => { Theme.Muted = !Theme.Muted; Prefs.Save(); });

            _dayStrip = Ui.Panel(_scene, "Days", new Color(Theme.SurfaceDeep.r, Theme.SurfaceDeep.g, Theme.SurfaceDeep.b, 0.85f), 22).rectTransform;
            _dayStrip.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(330, 48));
            _dayLabel = Ui.Label(_dayStrip, "Day", "", 20, Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_dayLabel.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(92, 40));
            _dayIcons = Ui.Rect(_dayStrip, "Next");
            _dayIcons.anchorMin = new Vector2(0, 0);
            _dayIcons.anchorMax = new Vector2(1, 1);
            _dayIcons.offsetMin = new Vector2(100, 4);
            _dayIcons.offsetMax = new Vector2(-10, -4);
            _dayIcons.Row(6, TextAnchor.MiddleLeft);
            _roundLabel = Ui.Label(_scene, "Round", "", 30, Color.white, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_roundLabel.transform).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(300, 48));

            _hitsBox = Ui.Rect(_scene, "Hits");
            _hitsBox.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -60), new Vector2(110, 70));
            _hitsTitle = Ui.Label(_hitsBox, "Title", Loc.Tr("Hits"), 16, Theme.GoldTop, TextAnchor.UpperRight, true, false, true);
            ((RectTransform)_hitsTitle.transform).Band(true, 0, 20);
            _hitsLabel = Ui.Label(_hitsBox, "Count", "", 44, Color.white, TextAnchor.UpperRight, true, false, true);
            ((RectTransform)_hitsLabel.transform).Band(true, 18, 50);

            // The fighters: pet and hero on the left, the foe on the right.
            _foeGlow = Ui.Img(_scene, "Glow", new Color(1, 0.2f, 0.2f, 0), Ui.Glow);
            _foeGlow.preserveAspect = false;
            _pet = new Fighter(_scene, "Pet", Theme.Pet);
            _pet.SetArt("pet_pip", Theme.Pet, false);
            _pet.Root.anchoredPosition = new Vector2(W * 0.06f + Theme.Pet / 2, Feet);
            _hero = new Fighter(_scene, "Hero", Theme.Fighter);
            _hero.SetArt(_run.Hero.Class.Sprite, Theme.Fighter, false);
            _dead = new Fighter(_scene, "Fallen", Theme.Fighter);
            _dead.Body.pivot = new Vector2(1, 0);
            _dead.Root.gameObject.SetActive(false);
            _foe = new Fighter(_scene, "Foe", Theme.Fighter);
            _foe.Root.gameObject.SetActive(false);
            _shield = Ui.Img(_foe.Root, "Shield", new Color(1, 1, 1, 0.5f), Ui.Ring);
            _shield.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));

            _chest = Widgets.Chest(_scene, 90);
            _chest.anchorMin = _chest.anchorMax = Vector2.zero;
            _chest.pivot = new Vector2(0.5f, 0);
            _chest.anchoredPosition = new Vector2(W * 0.86f - 48, Feet);
            _chest.gameObject.SetActive(false);

            // Name, ability and the heavy-blow warning over the foe.
            _foeInfo = Ui.Rect(_scene, "FoeInfo");
            _foeInfo.anchorMin = _foeInfo.anchorMax = Vector2.zero;
            _foeInfo.pivot = new Vector2(0.5f, 0);
            _foeInfo.sizeDelta = new Vector2(200, 54);
            _foeName = Ui.Label(_foeInfo, "Name", "", 17, Color.white, TextAnchor.MiddleCenter, false, false, true);
            ((RectTransform)_foeName.transform).Band(true, 0, 24);
            var tag = Ui.Panel(_foeInfo, "Ability", Theme.SurfaceDeep, 10);
            tag.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(120, 22));
            _abilityTag = Ui.Label(tag.rectTransform, "Text", "", 13, Theme.Ability, TextAnchor.MiddleCenter);
            ((RectTransform)_abilityTag.transform).Fill(4, 0, 4, 0);
            _windup = Ui.Rect(_foeInfo, "Windup");
            _windup.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(120, 56));
            var badge = Ui.Panel(_windup, "Badge", Theme.Hp, 12);
            badge.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(116, 34));
            var heavy = Ui.Label(badge.rectTransform, "Text", Loc.Tr("! HEAVY"), 22, Color.white, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)heavy.transform).Fill();
            _windupPips = new Image[Battle.WindupBreak];
            for (var i = 0; i < _windupPips.Length; i++)
            {
                _windupPips[i] = Ui.Img(_windup, "Pip", Theme.SurfaceDeep);
                _windupPips[i].rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                    new Vector2((i - (_windupPips.Length - 1) / 2f) * 20, 2), new Vector2(14, 14));
                var o = _windupPips[i].gameObject.AddComponent<Outline>();
                o.effectColor = Theme.Outline;
            }

            // Bars stand on the ground under each fighter.
            _heroBars = Ui.Rect(_scene, "HeroBars");
            _heroBars.anchorMin = _heroBars.anchorMax = Vector2.zero;
            _heroBars.pivot = new Vector2(0.5f, 1);
            _heroBars.sizeDelta = new Vector2(Theme.Fighter * 0.95f, 34);
            _heroHp = new Bar(_heroBars, "Hp", Theme.Hp, 18, true);
            _heroHp.Root.Band(true, 0, 18);
            _special = new Bar(_heroBars, "Special", Theme.Special, 10, false);
            _special.Root.Band(true, 22, 10);
            _specialReady = Ui.Label(_special.Root, "Ready", Loc.Tr("READY"), 12, Color.white, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_specialReady.transform).Fill(0, -4, 0, -4);

            _foeBars = Ui.Rect(_scene, "FoeBars");
            _foeBars.anchorMin = _foeBars.anchorMax = Vector2.zero;
            _foeBars.pivot = new Vector2(0.5f, 1);
            _foeBars.sizeDelta = new Vector2(Theme.Fighter * 0.95f, 32);
            _foeHp = new Bar(_foeBars, "Hp", Theme.Hp, 18, true);
            _foeHp.Root.Band(true, 0, 18);
            _clock = new Bar(_foeBars, "Clock", Theme.GoldTop, 8, false);
            _clock.Root.Band(true, 22, 8);

            _fxLayer = Ui.Rect(_scene, "Fx").Fill();
            _specialFlash = Ui.Img(_scene, "SpecialFlash", new Color(Theme.SpecialTop.r, Theme.SpecialTop.g, Theme.SpecialTop.b, 0));
            _specialFlash.rectTransform.Fill();
            _banner = Ui.Label(_scene, "Banner", "", 52, Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_banner.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(520, 80));
            _banner.gameObject.SetActive(false);
            _specialBanner = Ui.Label(_scene, "Special", "", 40, Theme.SpecialTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_specialBanner.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(520, 60));
            _specialBanner.gameObject.SetActive(false);
        }

        RawImage Backdrop(string name, string art, float strip)
        {
            var r = Ui.Rect(_scene, name).gameObject.AddComponent<RawImage>();
            r.rectTransform.Band(true, 0, strip);
            var s = Theme.Art(art);
            r.texture = s != null ? s.texture : null;
            r.raycastTarget = false;
            if (r.texture != null) r.texture.wrapMode = TextureWrapMode.Repeat;
            return r;
        }

        /// <summary>A strip of sandy road: pale ruts and pebbles on white, tinted
        /// by the world's ground colour. It repeats sideways.</summary>
        static Texture2D RoadTexture()
        {
            const int w = 256, h = 128;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h];
            var rnd = new System.Random(5);
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = 1f - 0.04f * Mathf.PerlinNoise(x * 0.05f, y * 0.11f);
                // Two worn ruts along the road.
                var rut = Mathf.Abs(y - h * 0.62f) < 5 || Mathf.Abs(y - h * 0.28f) < 4;
                if (rut) v -= 0.06f;
                px[y * w + x] = new Color(v, v, v);
            }
            for (var i = 0; i < 70; i++)
            {
                int cx = rnd.Next(w), cy = rnd.Next(h), r = 1 + rnd.Next(3);
                var shade = 0.78f + (float)rnd.NextDouble() * 0.12f;
                for (var dy = -r; dy <= r; dy++)
                for (var dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    var x = (cx + dx + w) % w;
                    var y = Mathf.Clamp(cy + dy, 0, h - 1);
                    px[y * w + x] = new Color(shade, shade, shade);
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        void BuildStats()
        {
            var band = Ui.Img(_root, "Stats", Theme.Band).rectTransform.Band(true, SceneH, Theme.StatBand);
            var lip = Ui.Img(band, "Lip", Theme.BandTop);
            lip.rectTransform.Band(true, 0, 5);
            var row = Ui.Rect(band, "Row").Fill(10, 8, 10, 12);
            var g = row.Row(8);
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = true;
            _lv = Chip(row, Loc.Tr("Lv."), Theme.GoldTop, out var lvBox);
            _xp = new Bar(lvBox, "Xp", Theme.Xp, 6, false);
            _xp.Root.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(90, 6));
            _hpText = Chip(row, Loc.Tr("HP"), Theme.Hp, out _);
            _atk = Chip(row, Loc.Tr("ATK"), Theme.DangerTop, out _);
            _def = Chip(row, Loc.Tr("DEF"), Theme.CardBorder, out _);

            _skillsRow = Ui.Img(_root, "Skills", Theme.SurfaceDeep).rectTransform.Band(true, SceneH + Theme.StatBand, 32);
            var sg = _skillsRow.Row(6, TextAnchor.MiddleCenter);
            sg.padding = new RectOffset(10, 10, 4, 4);
            _skillsRow.gameObject.SetActive(false);
        }

        UText Chip(RectTransform row, string name, Color color, out RectTransform box)
        {
            var chip = Ui.Panel(row, name, Theme.BandChip, 10);
            box = chip.rectTransform;
            var label = Ui.Label(box, "Name", name, 12, Theme.TextMuted, TextAnchor.MiddleCenter);
            ((RectTransform)label.transform).Band(true, 2, 16);
            var v = Ui.Label(box, "Value", "", 20, color, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)v.transform).Fill(2, 6, 2, 16);
            return v;
        }

        void BuildLog()
        {
            var area = Ui.Rect(_root, "Log");
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(0, ActionH);
            area.offsetMax = new Vector2(0, -(SceneH + Theme.StatBand));
            area.gameObject.AddComponent<RectMask2D>();
            _logList = Ui.Rect(area, "Cards");
            _logList.anchorMin = new Vector2(0, 1);
            _logList.anchorMax = new Vector2(1, 1);
            _logList.pivot = new Vector2(0.5f, 1);
            _logList.offsetMin = new Vector2(12, -2000);
            _logList.offsetMax = new Vector2(-12, -10);
            var g = _logList.Column(8);
            g.childControlHeight = true;
        }

        void BuildActions()
        {
            _actions = Ui.Rect(_root, "Actions").Band(false, 0, ActionH);
            _action = Ui.Button(_actions, "Action", Theme.Accent, Theme.AccentTop, Rep);
            ((RectTransform)_action.transform).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(W * 0.6f, 82));
            _choices = new CandyButton[2];
            for (var i = 0; i < 2; i++)
            {
                var k = i;
                _choices[i] = Ui.Button(_actions, "Choice" + i, Theme.Surface, Theme.TextMuted, () => _run.Choose(k));
                ((RectTransform)_choices[i].transform).Place(new Vector2(i == 0 ? 0.25f : 0.75f, 1), new Vector2(0.5f, 1),
                    new Vector2(i == 0 ? 6 : -6, -12), new Vector2(W / 2 - 24, 82));
                _choices[i].Title.Text.fontSize = 24;
                _choices[i].gameObject.SetActive(false);
            }
            _hint = Ui.Label(_actions, "Hint", "", 14, Theme.TextMuted, TextAnchor.MiddleCenter, false, true);
            var hr = (RectTransform)_hint.transform;
            hr.anchorMin = new Vector2(0, 0);
            hr.anchorMax = new Vector2(1, 0);
            hr.pivot = new Vector2(0.5f, 0);
            hr.offsetMin = new Vector2(16, 6);
            hr.offsetMax = new Vector2(-16, 32);
        }

        void BuildModal()
        {
            _modal = Ui.Img(_root, "Modal", new Color(0.05f, 0.05f, 0.09f, 0.82f)).rectTransform;
            _modal.anchorMin = Vector2.zero;
            _modal.anchorMax = Vector2.one;
            _modal.offsetMin = new Vector2(0, ActionH);
            _modal.offsetMax = Vector2.zero;
            _modal.GetComponent<Image>().raycastTarget = true;
            _modal.gameObject.SetActive(false);
        }

        // =============================================================== input

        void Rep()
        {
            if (_run.Finished) return;
            var before = _run.Mode;
            _run.Rep();
            if (before == RunMode.Pick || before == RunMode.Card) Theme.Play("pick", 0.7f);
            if (before == RunMode.Spin) Theme.Play("whoosh", 0.7f);
        }

        // ============================================================== update

        void Update()
        {
            if (_run == null) return;
            var dt = Mathf.Min(Time.deltaTime, 0.1f);
            _time += dt;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.UpArrow))
                Rep();
            _run.Tick(dt);
            if (_run.Finished)
            {
                Persist();
                GameApp.I.ShowHome();
                return;
            }
            if (_run.Overlay != null && (_run.Overlay.Kind == OverlayKind.Clear || _run.Overlay.Kind == OverlayKind.Defeat))
                Persist();

            DrainFx();
            AdvanceTimers(dt);
            DrawScene();
            DrawStats();
            DrawLog();
            DrawActions();
            DrawModal(dt);
        }

        void AdvanceTimers(float dt)
        {
            Step(ref _heroLunge, dt, 0.36f);
            Step(ref _foeLunge, dt, 0.36f);
            Step(ref _hurt, dt, 0.3f);
            Step(ref _flash, dt, 0.28f);
            Step(ref _enter, dt, 0.55f);
            Step(ref _dodge, dt, 0.35f);
            Step(ref _death, dt, 1.1f);
            Step(ref _walk, dt, (float)RunController.WalkTime);
            Step(ref _shakeT, dt, 0.4f);
            Step(ref _flashT, dt, 0.45f);
            Step(ref _chestT, dt, 0.4f);
            if (_specialT >= 0)
            {
                _specialT += dt;
                if (_specialT > 1.0f) { _specialT = -1; _specialBanner.gameObject.SetActive(false); }
            }
            if (_bannerT >= 0)
            {
                _bannerT += dt;
                if (_bannerT > _bannerFor) { _bannerT = -1; _banner.gameObject.SetActive(false); }
            }
            for (var i = _floats.Count - 1; i >= 0; i--)
            {
                var f = _floats[i];
                f.T += dt / 0.85f;
                if (f.T >= 1) { Destroy(f.Text.gameObject); _floats.RemoveAt(i); continue; }
                var v = f.T;
                var pop = v < 0.18f ? 0.6f + 0.9f * Ui.OutCubic(v / 0.18f) : 1.5f - 0.5f * Mathf.Clamp01((v - 0.18f) / 0.82f / 0.4f);
                var rt = (RectTransform)f.Text.transform;
                rt.anchoredPosition = f.From + new Vector2(0, Ui.OutCubic(v) * 40);
                rt.localScale = Vector3.one * pop * f.Scale;
                var c = f.Text.Text.color;
                c.a = v < 0.7f ? 1 : Mathf.Clamp01((1 - v) / 0.3f);
                f.Text.Text.color = c;
                _floats[i] = f;
            }
            for (var i = _bursts.Count - 1; i >= 0; i--)
            {
                var b = _bursts[i];
                b.T += dt / b.Seconds;
                if (b.T >= 1) { Destroy(b.Img.gameObject); _bursts.RemoveAt(i); continue; }
                b.Img.rectTransform.localScale = Vector3.one * Mathf.Lerp(b.From, b.To, Ui.OutCubic(b.T));
                var c = b.Color;
                c.a *= 1 - b.T;
                b.Img.color = c;
                _bursts[i] = b;
            }
        }

        static void Step(ref float t, float dt, float seconds)
        {
            if (t < 0) return;
            t += dt / seconds;
            if (t >= 1) t = -1;
        }

        // ----------------------------------------------------------- effects

        void DrainFx()
        {
            foreach (var fx in _run.Fx) Play(fx);
            _run.Fx.Clear();
        }

        void Play(RunFx fx)
        {
            switch (fx.Kind)
            {
                case FxKind.Walk:
                    _walk = 0;
                    Theme.Play("whoosh", 0.35f);
                    break;
                case FxKind.FoeEnter:
                    _shownFoe = _run.Battle?.Foe;
                    if (_shownFoe != null)
                    {
                        var size = _shownFoe.Boss ? Theme.FighterBoss : Theme.Fighter;
                        _foe.SetArt(_shownFoe.Img, size, _shownFoe.Front);
                        _foeName.Set(Loc.TrName(_shownFoe.Name));
                        _abilityTag.Set(Loc.Tr(Abilities.Label(_shownFoe.Ability)));
                        _abilityTag.SetColor(Theme.AbilityColor(_shownFoe.Ability));
                    }
                    _enter = 0;
                    _hits = 0;
                    Theme.Play("fight", fx.Boss ? 1 : 0.7f);
                    if (fx.Boss) Shake(1.2f);
                    break;
                case FxKind.Banner:
                    Banner(fx.Text, fx.Seconds);
                    break;
                case FxKind.Lunge:
                    if (fx.ByHero)
                    {
                        _heroLunge = 0;
                    }
                    else
                    {
                        _foeLunge = 0;
                        _hurt = 0;
                        Theme.Play("hurt", 0.8f);
                    }
                    break;
                case FxKind.Slash:
                    _flash = 0;
                    _hits++;
                    Slash(fx.Crit);
                    Theme.Play(fx.Crit ? "crit" : "hit", fx.Crit ? 1 : 0.8f);
                    break;
                case FxKind.Float:
                    Float(fx.Text, fx.Float, fx.OnFoe);
                    break;
                case FxKind.Shake:
                    Shake(fx.Strength);
                    break;
                case FxKind.Special:
                    _specialT = 0;
                    _flashT = 0;
                    _specialBanner.Set(fx.Text);
                    _specialBanner.gameObject.SetActive(true);
                    _heroLunge = 0;
                    Theme.Play("special");
                    break;
                case FxKind.Windup:
                    Theme.Play("tick");
                    Burst(FoeCenter(), Theme.Hp, 0.5f, 1.4f, 0.5f);
                    break;
                case FxKind.Interrupt:
                    _flash = 0;
                    Burst(FoeCenter(), Theme.GoldTop, 0.4f, 1.8f, 0.5f);
                    Theme.Play("crit");
                    break;
                case FxKind.FoeDown:
                    if (_shownFoe != null)
                    {
                        var size = _shownFoe.Boss ? Theme.FighterBoss : Theme.Fighter;
                        _dead.SetArt(_shownFoe.Img, size, _shownFoe.Front);
                        _dead.Root.anchoredPosition = FoeFoot(_shownFoe);
                        _dead.Root.gameObject.SetActive(true);
                        _death = 0;
                    }
                    Theme.Play("victory", 0.8f);
                    break;
                case FxKind.Dodge:
                    _dodge = 0;
                    Theme.Play("whoosh", 0.6f);
                    break;
                case FxKind.Ability:
                    Burst(FoeCenter(), Theme.AbilityColor(fx.Ability), 0.4f, 1.7f, 0.6f);
                    Theme.Play("ability");
                    break;
                case FxKind.LevelUp:
                    Float(fx.Text, FloatKind.Crit, false, 1.3f);
                    Burst(HeroCenter(), Theme.GoldTop, 0.4f, 1.6f, 0.7f);
                    Theme.Play("levelup");
                    break;
            }
        }

        void Banner(string text, float seconds)
        {
            _banner.Set(text);
            _banner.gameObject.SetActive(true);
            _bannerT = 0;
            _bannerFor = Mathf.Max(0.6f, seconds + 0.25f);
        }

        void Shake(float strength)
        {
            _shakeT = 0;
            _shakeStrength = strength;
        }

        void Float(string text, FloatKind kind, bool onFoe, float scale = 1)
        {
            Color c;
            switch (kind)
            {
                case FloatKind.Crit: c = Theme.GoldTop; break;
                case FloatKind.Heal:
                case FloatKind.Reflect: c = Theme.RewardText; break;
                case FloatKind.Hurt: c = Theme.Hp; break;
                default: c = Color.white; break;
            }
            var label = Ui.Label(_fxLayer, "Float", text, 30, c, TextAnchor.MiddleCenter, true, false, true);
            var rt = (RectTransform)label.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(240, 44);
            var head = onFoe ? FoeCenter() + new Vector2(0, 50) : HeroCenter() + new Vector2(0, 50);
            _floatSlot = (_floatSlot + 1) % 3;
            var from = head + new Vector2((_floatSlot - 1) * 22, (_floatSlot % 2) * 14);
            rt.anchoredPosition = from;
            _floats.Add(new Floating { Text = label, From = from, Scale = scale });
        }

        void Burst(Vector2 at, Color color, float from, float to, float seconds)
        {
            var img = Ui.Img(_fxLayer, "Burst", color, Ui.Ring);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
            img.rectTransform.anchoredPosition = at;
            img.rectTransform.sizeDelta = new Vector2(130, 130);
            _bursts.Add(new BurstFx { Img = img, From = from, To = to, Seconds = seconds, Color = color });
        }

        void Slash(bool crit)
        {
            var img = Ui.Img(_fxLayer, "Slash", crit ? Theme.GoldTop : Color.white, Ui.Round(3), true);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
            img.rectTransform.anchoredPosition = FoeCenter();
            img.rectTransform.sizeDelta = new Vector2(crit ? 130 : 100, crit ? 10 : 6);
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(25f, 55f) * (Random.value < 0.5f ? 1 : -1));
            _bursts.Add(new BurstFx { Img = img, From = 0.3f, To = 1.2f, Seconds = 0.22f, Color = img.color });
        }

        // ------------------------------------------------------------- scene

        Vector2 HeroFoot => new Vector2(W * 0.18f + Theme.Fighter / 2, Feet);

        Vector2 FoeFoot(Foe f) =>
            new Vector2(W * 0.86f - (f != null && f.Boss ? Theme.FighterBoss : Theme.Fighter) / 2, Feet);

        Vector2 HeroCenter() => _hero.Root.anchoredPosition + new Vector2(0, Theme.Fighter * 0.55f);

        Vector2 FoeCenter()
        {
            var f = _shownFoe;
            var size = f != null && f.Boss ? Theme.FighterBoss : Theme.Fighter;
            return (_foe.Root.gameObject.activeSelf ? _foe.Root.anchoredPosition : FoeFoot(f)) + new Vector2(0, size * 0.5f);
        }

        float Gap
        {
            get
            {
                var foeSize = _shownFoe != null && _shownFoe.Boss ? Theme.FighterBoss : Theme.Fighter;
                return Mathf.Max(0, W * 0.68f - Theme.Fighter - foeSize - 8);
            }
        }

        static float DashCurve(float v)
        {
            if (v <= 0 || v >= 1) return 0;
            if (v < 0.3f) return Ui.OutCubic(v / 0.3f);
            if (v < 0.55f) return 1;
            var k = (v - 0.55f) / 0.45f;
            return 1 - (k < 0.5f ? 4 * k * k * k : 1 - Mathf.Pow(-2 * k + 2, 3) / 2);
        }

        static float Knock(float v) => v < 0 ? 0 : Ui.OutCubic(1 - v);

        void DrawScene()
        {
            var b = _run.Battle;
            var fight = _run.Fight;
            var breath = Mathf.Sin(_time * Mathf.PI * 2 / 1.6f) * 0.5f + 0.5f;

            // Scenery and road slide a little each day and each round.
            var target = _run.Day + _run.Round / 8f;
            _scroll = Mathf.Lerp(_scroll, target, 1 - Mathf.Exp(-Time.deltaTime * 5));
            Slide(_far, _scroll * 40);
            Slide(_near, _scroll * 140);
            Slide(_road, _scroll * 140, 256);

            var sh = _shakeT >= 0 ? (1 - _shakeT) * _shakeStrength * 8 : 0;
            _shake.anchoredPosition = new Vector2(Mathf.Sin(_shakeT * 40) * sh, Mathf.Cos(_shakeT * 33) * sh);

            // Walk: a waddle with a hop on each step.
            var walkStep = _walk >= 0 ? Mathf.Sin(_walk * Mathf.PI * 2) : 0;
            var petStep = _walk >= 0 ? Mathf.Sin(Mathf.Clamp01((_walk - 0.25f) / 0.75f) * Mathf.PI * 2) : 0;

            _pet.Root.anchoredPosition = new Vector2(W * 0.06f + Theme.Pet / 2, Feet + Mathf.Abs(petStep) * 8);
            _pet.Body.localRotation = Quaternion.Euler(0, 0, -petStep * 5);
            _pet.Breathe(breath);

            var dash = DashCurve(_heroLunge);
            var hurt = Knock(_hurt);
            _hero.Root.anchoredPosition = HeroFoot + new Vector2(dash * Gap - hurt * 16, Mathf.Abs(walkStep) * 8);
            _hero.Body.localRotation = Quaternion.Euler(0, 0, -(dash * 0.2f - hurt * 0.12f) * Mathf.Rad2Deg - walkStep * 5);
            _hero.Breathe(breath);
            _hero.SetFlash(_hurt >= 0 && _hurt < 0.6f ? 0.9f : 0);

            // The foe.
            var showFoe = fight != null;
            _foe.Root.gameObject.SetActive(showFoe);
            _foeInfo.gameObject.SetActive(showFoe);
            _foeBars.gameObject.SetActive(showFoe);
            _heroBars.gameObject.SetActive(showFoe);
            _hitsBox.gameObject.SetActive(showFoe && _hits > 0);
            _roundLabel.gameObject.SetActive(showFoe);
            _dayStrip.gameObject.SetActive(!showFoe);
            if (showFoe)
            {
                var f = fight.Foe;
                if (f != _shownFoe)
                {
                    _shownFoe = f;
                    _foe.SetArt(f.Img, f.Boss ? Theme.FighterBoss : Theme.Fighter, f.Front);
                }
                var size = f.Boss ? Theme.FighterBoss : Theme.Fighter;
                var enter = _enter >= 0 ? (1 - Ui.OutCubic(_enter)) * SceneH : 0;
                var dodge = _dodge >= 0 ? Mathf.Sin(_dodge * Mathf.PI) : 0;
                var fdash = DashCurve(_foeLunge);
                var hit = Knock(_flash);
                _foe.Root.anchoredPosition = FoeFoot(f) + new Vector2(enter - fdash * Gap + dodge * 32 + hit * 16, dodge * 24);
                _foe.Body.localRotation = Quaternion.Euler(0, 0, -(-fdash * 0.2f + hit * 0.12f) * Mathf.Rad2Deg);
                _foe.Breathe(breath, 1 - hit * 0.1f, 1 + hit * 0.06f, b != null && b.Windup ? 1 + 0.04f * breath : 1);
                _foe.SetFlash(_flash >= 0 && _flash < 0.6f ? 0.9f : 0);
                var frenzied = fight.Frenzied;
                var tint = frenzied ? 0.15f + 0.15f * breath : 0;
                _foe.Pic.color = new Color(1, 1 - tint, 1 - tint);

                var windup = b != null && b.Windup;
                var glowA = windup ? 0.45f + 0.4f * breath : frenzied ? 0.2f + 0.3f * breath : 0;
                _foeGlow.color = new Color(Theme.Hp.r, Theme.Hp.g, Theme.Hp.b, glowA);
                _foeGlow.rectTransform.anchorMin = _foeGlow.rectTransform.anchorMax = Vector2.zero;
                _foeGlow.rectTransform.anchoredPosition = _foe.Root.anchoredPosition + new Vector2(0, size * 0.5f);
                _foeGlow.rectTransform.sizeDelta = Vector2.one * size * (1.4f + 0.15f * breath);
                _shield.gameObject.SetActive(fight.Shield > 0);
                _shield.rectTransform.anchoredPosition = new Vector2(0, size * 0.5f);
                _shield.rectTransform.sizeDelta = Vector2.one * size * 1.05f;
                _shield.color = new Color(Theme.TextMuted.r, Theme.TextMuted.g, Theme.TextMuted.b, 0.5f + 0.3f * breath);

                _foeInfo.anchoredPosition = _foe.Root.anchoredPosition + new Vector2(0, size + 2);
                _windup.gameObject.SetActive(windup);
                if (windup)
                {
                    _windup.localScale = Vector3.one * (1 + 0.1f * breath);
                    for (var i = 0; i < _windupPips.Length; i++)
                        _windupPips[i].color = i < b.WindupHits ? Theme.GoldTop : Theme.SurfaceDeep;
                }

                _foeBars.anchoredPosition = new Vector2(_foe.Root.anchoredPosition.x, Feet - 6);
                _foeHp.Set(fight.Hp / (float)f.Hp, fight.Hp.ToString());
                _clock.Set((float)_run.FoeClock);
                _clock.SetColor(windup ? Theme.Hp : Theme.GoldTop);

                _heroBars.anchoredPosition = new Vector2(_hero.Root.anchoredPosition.x, Feet - 6);
                _heroHp.Set(_run.Hero.Hp / (float)_run.Hero.Max, _run.Hero.Hp.ToString());
                if (b != null)
                {
                    var ready = b.SpecialReady;
                    _special.Set(b.Charge / (float)_run.Hero.SpecialCost);
                    _special.SetColor(ready ? Theme.SpecialTop : Theme.Special);
                    _specialReady.gameObject.SetActive(ready);
                }
                _roundLabel.Set(Loc.Tr("Round {n}", "n", _run.Round + 1));
                _hitsLabel.Set(_hits.ToString());
            }
            else
            {
                _foeGlow.color = Color.clear;
                DrawDays();
            }

            // The beaten foe tips over backwards and fades.
            if (_death >= 0)
            {
                var v = _death;
                var fall = Mathf.Pow(Mathf.Clamp01(v / 0.4f), 3);
                var fade = v < 0.6f ? 1 : 1 - (v - 0.6f) / 0.4f;
                var size = _shownFoe != null && _shownFoe.Boss ? Theme.FighterBoss : Theme.Fighter;
                _dead.Root.anchoredPosition = FoeFoot(_shownFoe) + new Vector2(fall * size * 0.25f, 0);
                _dead.Body.localRotation = Quaternion.Euler(0, 0, -fall * 180 / 2.2f);
                _dead.Pic.color = new Color(1, 1, 1, Mathf.Clamp01(fade));
                _dead.SetFlash(v < 0.12f ? 0.9f : 0);
            }
            else if (_dead.Root.gameObject.activeSelf)
            {
                _dead.Root.gameObject.SetActive(false);
            }

            // A closed chest waits on the road, wiggling.
            var chest = _run.Overlay != null && _run.Overlay.Kind == OverlayKind.ChestClosed;
            if (chest && !_chest.gameObject.activeSelf) _chestT = 0;
            _chest.gameObject.SetActive(chest);
            if (chest)
            {
                var pop = _chestT >= 0 ? Mathf.Lerp(0.3f, 1, Ui.OutBack(_chestT)) : 1;
                _chest.localScale = Vector3.one * pop;
                _chest.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_time * 9) * 4 * Mathf.Max(0, Mathf.Sin(_time * 1.7f)));
            }

            _muteSlash.gameObject.SetActive(Theme.Muted);

            // Banners pop in, hold, and fade.
            if (_bannerT >= 0)
            {
                var rt = (RectTransform)_banner.transform;
                rt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1, Ui.OutBack(_bannerT / 0.25f));
                var a = Mathf.Clamp01((_bannerFor - _bannerT) / 0.2f);
                var c = _banner.Text.color;
                c.a = a;
                _banner.Text.color = c;
            }
            if (_specialT >= 0)
            {
                var rt = (RectTransform)_specialBanner.transform;
                rt.localScale = Vector3.one * Mathf.Lerp(0.5f, 1, Ui.OutBack(_specialT / 0.25f));
                var c = _specialBanner.Text.color;
                c.a = Mathf.Clamp01((1 - _specialT) / 0.25f);
                _specialBanner.Text.color = c;
            }
            var fc = _specialFlash.color;
            fc.a = _flashT >= 0 ? 0.35f * (1 - _flashT) : 0;
            _specialFlash.color = fc;
        }

        static void Slide(RawImage img, float px, float tile = 0)
        {
            if (img.texture == null) return;
            var r = img.rectTransform.rect;
            var tileW = tile > 0 ? tile : r.height * img.texture.width / (float)img.texture.height;
            var vTiles = tile > 0 ? r.height / (tile * img.texture.height / img.texture.width) : 1;
            img.uvRect = new Rect(px / tileW, 0, r.width / Mathf.Max(1, tileW), vTiles);
        }

        /// <summary>"Day 7" and the next days' events as small pictures.</summary>
        void DrawDays()
        {
            if (_dayShown == _run.Day) return;
            _dayShown = _run.Day;
            _dayLabel.Set(Loc.Tr("Day {n}", "n", Mathf.Max(1, _run.Day)));
            Ui.Clear(_dayIcons);
            var first = true;
            foreach (var node in _run.Upcoming)
            {
                var s = first ? 34f : 26f;
                var slot = Ui.Panel(_dayIcons, node, first ? Theme.GoldTop : Theme.Outline, (int)(s / 2));
                slot.Size(s, s);
                var face = Ui.Panel(slot.rectTransform, "Face", Theme.Surface, (int)(s / 2) - 2);
                face.rectTransform.Fill(2, 2, 2, 2);
                NodeIcon(face.rectTransform, node, s - 6);
                first = false;
            }
        }

        void NodeIcon(RectTransform parent, string node, float s)
        {
            string art = null;
            if (node == "boss") art = _ch.Boss.Img;
            else if (node.StartsWith("foe:"))
            {
                int i;
                if (int.TryParse(node.Substring(4).TrimEnd('+'), out i) && i < _ch.Foes.Length) art = _ch.Foes[i].Img;
            }
            else if (node == "skill") art = "gear_star";
            else if (node == "story") art = "pet_pip";
            if (art != null)
            {
                var img = Widgets.Sprite(parent, "Icon", art, s);
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
                return;
            }
            if (node == "chest")
            {
                var c = Widgets.Chest(parent, s * 0.8f);
                c.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 0.8f, s * 0.64f));
                return;
            }
            if (node == "wheel")
            {
                var r = Ui.Img(parent, "Wheel", Theme.GoldTop, Ui.Ring);
                r.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
                var hub = Ui.Img(parent, "Hub", Theme.Gold, Ui.Circle);
                hub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 0.35f, s * 0.35f));
                return;
            }
            var q = Ui.Label(parent, "Q", "?", (int)(s * 0.8f), Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)q.transform).Fill();
        }

        // ------------------------------------------------------------- stats

        void DrawStats()
        {
            var h = _run.Hero;
            _lv.Set(h.Lv.ToString());
            _xp.Set(h.Xp / (float)Rules.XpPerLevel);
            _hpText.Set(h.Hp + "/" + h.Max);
            _atk.Set(h.Atk.ToString());
            _def.Set(h.Def.ToString());

            var sum = h.Sk.Values.Sum();
            if (sum == _skillsSum) return;
            _skillsSum = sum;
            _skillsRow.gameObject.SetActive(sum > 0);
            var logArea = (RectTransform)_logList.parent;
            logArea.offsetMax = new Vector2(0, -(SceneH + Theme.StatBand + (sum > 0 ? 32 : 0)));
            Ui.Clear(_skillsRow);
            foreach (var kv in h.Sk)
            {
                Skill s;
                if (!Content.Skills.TryGetValue(kv.Key, out s)) continue;
                var chip = Ui.Panel(_skillsRow, kv.Key, Theme.Surface, 11);
                var text = Loc.Tr(s.Name) + (kv.Value > 1 ? " ×" + kv.Value : "");
                var l = Ui.Label(chip.rectTransform, "Name", text, 13, Theme.GoldTop, TextAnchor.MiddleCenter);
                ((RectTransform)l.transform).Fill(8, 0, 8, 0);
                chip.Size(l.Text.preferredWidth + 18, 24);
            }
        }

        // --------------------------------------------------------------- log

        void DrawLog()
        {
            var log = _run.Log;
            if (log.Count == _logCount) return;
            for (var i = log.Count - _logCount - 1; i >= 0; i--) AddCard(log[i]);
            _logCount = log.Count;
            while (_logList.childCount > 30) DestroyImmediate(_logList.GetChild(_logList.childCount - 1).gameObject);
        }

        void AddCard(LogEntry e)
        {
            var card = Ui.Panel(_logList, "Entry", Theme.Paper, 12);
            card.transform.SetAsFirstSibling();
            var rim = card.gameObject.AddComponent<Outline>();
            rim.effectColor = Theme.Outline;
            rim.effectDistance = new Vector2(2, -2);
            var col = card.rectTransform.Column(4);
            col.padding = new RectOffset(14, 14, 10, 12);

            var head = Ui.Rect(card.rectTransform, "Head");
            head.Row(8, Loc.Arabic ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            Color headColor;
            switch (e.Kind)
            {
                case LogKind.Fight: headColor = Theme.FoeWord; break;
                case LogKind.Win: headColor = Theme.Accent; break;
                default: headColor = Theme.CardHead; break;
            }
            var title = Ui.Label(head, "Title", e.Head, 18, headColor, TextAnchor.MiddleLeft, true);
            title.Size(flexW: 1);
            if (!string.IsNullOrEmpty(e.Tag))
            {
                var pill = Ui.Panel(head, "Tag", Theme.RewardBg, 10);
                var t = Ui.Label(pill.rectTransform, "Text", e.Tag, 13, Theme.RewardText, TextAnchor.MiddleCenter);
                ((RectTransform)t.transform).Fill(8, 0, 8, 0);
                pill.Size(Mathf.Min(260, t.Text.preferredWidth + 18), 24);
                // Right to left: the tag goes first, the title sits on the right.
                if (Loc.Arabic) pill.transform.SetAsFirstSibling();
            }
            head.Size(h: 26);

            var body = Ui.Label(card.rectTransform, "Body", e.Body, 16, Theme.TextOnCard, TextAnchor.UpperLeft, false, true);
            body.SetKeywordColor(e.Kind == LogKind.Fight ? Theme.FoeWord : e.Kind == LogKind.Win ? Theme.Accent : Theme.FriendWord);
        }

        // ----------------------------------------------------------- actions

        void DrawActions()
        {
            var o = _run.Overlay;
            var choice = o != null && o.Kind == OverlayKind.Choice && _run.Mode == RunMode.Pick;
            _action.gameObject.SetActive(!choice);
            for (var i = 0; i < _choices.Length; i++) _choices[i].gameObject.SetActive(choice && i < o.Options.Count);
            if (choice)
            {
                var lit = _run.LitIndex;
                for (var i = 0; i < o.Options.Count && i < _choices.Length; i++)
                {
                    var on = i == lit;
                    _choices[i].SetText(o.Options[i].Title, o.Options[i].Text);
                    if (on) _choices[i].Paint(Theme.Gold, Theme.GoldTop);
                    else _choices[i].Paint(Theme.Surface, Theme.TextMuted);
                    _choices[i].transform.localScale = Vector3.one * (on ? 1.04f : 0.96f);
                }
                _hint.Set(Loc.Tr("A pushup takes the glowing one · auto in {n}s", "n", Mathf.CeilToInt((float)_run.PickAutoLeft)));
                return;
            }
            var (label, sub) = _run.ActionLabel;
            _action.SetText(label, sub);
            Color face, top;
            if (o != null && (o.Kind == OverlayKind.ChestClosed || o.Kind == OverlayKind.Clear || o.Kind == OverlayKind.Defeat))
            {
                face = Theme.Orange; top = Theme.OrangeTop;
            }
            else
            {
                switch (_run.Mode)
                {
                    case RunMode.Walk: face = Theme.Accent; top = Theme.AccentTop; break;
                    case RunMode.Fight:
                        if (_run.Battle != null && _run.Battle.SpecialReady) { face = Theme.Special; top = Theme.SpecialTop; }
                        else { face = Theme.Danger; top = Theme.DangerTop; }
                        break;
                    case RunMode.Pick:
                    case RunMode.Spin: face = Theme.Gold; top = Theme.GoldTop; break;
                    case RunMode.Card: face = Theme.Orange; top = Theme.OrangeTop; break;
                    default: face = Theme.Surface; top = Theme.TextMuted; break;
                }
            }
            _action.Paint(face, top);
            _action.Interactable = _run.Mode != RunMode.Busy;
            var pulse = _run.Mode == RunMode.Busy ? 1 : 1 + 0.02f * Mathf.Sin(_time * 6);
            _action.transform.localScale = Vector3.one * pulse;
            _hint.Set(Loc.Tr("Tap or press Space for each pushup"));
        }

        // ------------------------------------------------------------ modals

        static bool IsModal(Overlay o) =>
            o != null && o.Kind != OverlayKind.Choice && o.Kind != OverlayKind.ChestClosed;

        void DrawModal(float dt)
        {
            var o = _run.Overlay;
            if (!IsModal(o))
            {
                if (_modal.gameObject.activeSelf)
                {
                    _modal.gameObject.SetActive(false);
                    Ui.Clear(_modal);
                }
                _modalFor = null;
                return;
            }
            if (o != _modalFor || o.Kind != _modalKind)
            {
                _modalFor = o;
                _modalKind = o.Kind;
                Ui.Clear(_modal);
                _modal.gameObject.SetActive(true);
                _modalBody = Ui.Rect(_modal, "Body");
                _modalBody.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W - 32, 560));
                _pickCards.Clear();
                _wheel = null;
                switch (o.Kind)
                {
                    case OverlayKind.Pick: BuildPick(o); break;
                    case OverlayKind.Wheel: BuildWheel(o); break;
                    case OverlayKind.Chest: BuildChest(o); break;
                    default: BuildEnd(o); break;
                }
                _modalBody.localScale = Vector3.one * 0.85f;
            }
            _modalBody.localScale = Vector3.Lerp(_modalBody.localScale, Vector3.one, 1 - Mathf.Exp(-dt * 14));

            if (o.Kind == OverlayKind.Pick)
            {
                var lit = _run.LitIndex;
                for (var i = 0; i < _pickCards.Count; i++)
                {
                    var on = i == lit;
                    _pickCards[i].color = on ? Theme.GoldTop : Theme.CardBorder;
                    _pickCards[i].transform.localScale = Vector3.one * (on ? 1.03f + 0.01f * Mathf.Sin(_time * 8) : 0.97f);
                }
                _pickAuto.Set(Loc.Tr("No pushup? Picking the glowing one in {n}s", "n", Mathf.CeilToInt((float)_run.PickAutoLeft)));
            }
            if (o.Kind == OverlayKind.Wheel && _wheel != null)
            {
                if (o.Spinning && !_wheelSpun)
                {
                    _wheelSpun = true;
                    _wheelT = 0;
                    _wheelFrom = _wheel.localEulerAngles.z;
                    _wheelTo = 360 * 5 + (o.WheelIndex + 0.5f) * 360f / Content.Wheel.Length;
                    _wheelTick = 0;
                }
                if (_wheelT >= 0)
                {
                    _wheelT += dt / (float)RunController.WheelSpin;
                    var a = Mathf.Lerp(_wheelFrom, _wheelTo, Ui.OutCubic(Mathf.Min(1, _wheelT)));
                    _wheel.localRotation = Quaternion.Euler(0, 0, a);
                    var slice = Mathf.FloorToInt(a / (360f / Content.Wheel.Length));
                    if (slice != _wheelTick) { _wheelTick = slice; Theme.Play("tick", 0.4f); }
                    if (_wheelT >= 1) _wheelT = -1;
                }
                else if (!o.Spinning)
                {
                    _wheel.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_time * 2) * 6);
                }
                _wheelResult.Set(string.IsNullOrEmpty(o.Result) ? (o.Spinning ? Loc.Tr("Spinning…") : Loc.Tr("A pushup spins it!")) : o.Result);
                if (!string.IsNullOrEmpty(o.Result) && _wheelResult.Text.color != Theme.GoldTop)
                {
                    _wheelResult.SetColor(Theme.GoldTop);
                    Theme.Play("levelup", 0.7f);
                }
            }
        }

        void BuildPick(Overlay o)
        {
            var col = _modalBody.Column(10, TextAnchor.MiddleCenter);
            col.childForceExpandWidth = false;
            var heroBox = Ui.Rect(_modalBody, "Hero");
            heroBox.Size(160, 120);
            var glow = Ui.Img(heroBox, "Glow", new Color(Theme.GoldTop.r, Theme.GoldTop.g, Theme.GoldTop.b, 0.6f), Ui.Glow);
            glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 200));
            var pic = Widgets.Sprite(heroBox, "Pic", _run.Hero.Class.Sprite, 120);
            pic.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(120, 120));
            var ribbon = Widgets.Ribbon(_modalBody, o.Title, Theme.Gold);
            ribbon.Size(360, 64);
            for (var i = 0; i < o.Options.Count; i++)
            {
                var k = i;
                var opt = o.Options[i];
                var border = Ui.Panel(_modalBody, "Option" + i, Theme.CardBorder, 14);
                border.Size(W - 64, 84);
                var face = Ui.Panel(border.rectTransform, "Face", Theme.Card, 12);
                face.rectTransform.Fill(4, 4, 4, 4);
                var name = Ui.Label(face.rectTransform, "Name", opt.Title, 22, Theme.CardHead, TextAnchor.UpperLeft, true);
                ((RectTransform)name.transform).Fill(14, 36, 14, 6);
                var text = Ui.Label(face.rectTransform, "Text", opt.Text, 15, Theme.TextOnCard, TextAnchor.UpperLeft, false, true);
                ((RectTransform)text.transform).Fill(14, 4, 14, 38);
                Tap.On(border, () => { Theme.Play("pick", 0.7f); _run.Choose(k); });
                _pickCards.Add(border);
            }
            _pickAuto = Ui.Label(_modalBody, "Auto", "", 14, Theme.TextMuted, TextAnchor.MiddleCenter);
            _pickAuto.Size(W - 64, 24);
        }

        void BuildWheel(Overlay o)
        {
            const float size = 320;
            var stand = Ui.Rect(_modalBody, "Stand");
            stand.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(size, size));
            var rim = Ui.Img(stand, "Rim", Theme.Outline, Ui.Circle);
            rim.rectTransform.Fill(-10, -10, -10, -10);
            var rim2 = Ui.Img(stand, "Rim2", Theme.Gold, Ui.Circle);
            rim2.rectTransform.Fill(-6, -6, -6, -6);
            _wheel = Ui.Rect(stand, "Wheel").Fill();
            var colors = new[] { Theme.Danger, Theme.Gold, Theme.Accent, Theme.Special, Theme.CardHead, Theme.Orange };
            var n = Content.Wheel.Length;
            for (var i = 0; i < n; i++)
            {
                // Slice i runs clockwise from the top, i/n of the way round.
                var wedge = Ui.Img(_wheel, "Slice" + i, colors[i % colors.Length], Ui.Circle);
                wedge.preserveAspect = false;
                wedge.type = Image.Type.Filled;
                wedge.fillMethod = Image.FillMethod.Radial360;
                wedge.fillOrigin = (int)Image.Origin360.Top;
                wedge.fillClockwise = true;
                wedge.fillAmount = 1f / n;
                wedge.rectTransform.Fill();
                wedge.rectTransform.localRotation = Quaternion.Euler(0, 0, -i * 360f / n);
                var mid = (i + 0.5f) * 360f / n;
                var label = Ui.Label(_wheel, "Label" + i, Loc.Tr(Content.Wheel[i].Label), 16, Color.white, TextAnchor.MiddleCenter, true, true, true);
                var lr = (RectTransform)label.transform;
                var rad = mid * Mathf.Deg2Rad;
                lr.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * size * 0.31f, new Vector2(96, 48));
                lr.localRotation = Quaternion.Euler(0, 0, -mid);
            }
            var hub = Ui.Img(stand, "Hub", Theme.Outline, Ui.Circle);
            hub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54, 54));
            var hubFace = Ui.Img(hub.rectTransform, "Face", Theme.GoldTop, Ui.Circle);
            hubFace.rectTransform.Fill(5, 5, 5, 5);
            var pointer = Ui.Img(stand, "Pointer", Theme.Hp, Ui.Triangle);
            pointer.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(40, 40));
            var pOut = pointer.gameObject.AddComponent<Outline>();
            pOut.effectColor = Theme.Outline;
            _wheelResult = Ui.Label(_modalBody, "Result", "", 28, Color.white, TextAnchor.MiddleCenter, true, true, true);
            ((RectTransform)_wheelResult.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 40 - size / 2 - 24), new Vector2(W - 64, 80));
            var title = Widgets.Ribbon(_modalBody, o.Title, Theme.Gold);
            title.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), new Vector2(0, 40 + size / 2 + 22), new Vector2(320, 64));
            _wheelT = -1;
            _wheelSpun = false;
        }

        void BuildChest(Overlay o)
        {
            var l = o.Loot;
            var c = Theme.RarityColor(l.Rarity);
            var col = _modalBody.Column(8, TextAnchor.MiddleCenter);
            col.childForceExpandWidth = false;
            var ribbon = Widgets.Ribbon(_modalBody, o.Title, Theme.Gold);
            ribbon.Size(340, 64);
            var box = Ui.Rect(_modalBody, "Loot");
            box.Size(220, 200);
            var glow = Ui.Img(box, "Glow", new Color(c.r, c.g, c.b, 0.7f), Ui.Glow);
            glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 260));
            var tile = Ui.Panel(box, "Tile", c, 16);
            tile.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, 132));
            var face = Ui.Panel(tile.rectTransform, "Face", Theme.Surface, 13);
            face.rectTransform.Fill(4, 4, 4, 4);
            var pic = Widgets.Sprite(face.rectTransform, "Item", l.Img, 108);
            pic.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(108, 108));
            Burst(new Vector2(W / 2, SceneH / 2), c, 0.5f, 1.8f, 0.8f);
            var rarity = Ui.Label(_modalBody, "Rarity", Widgets.RarityName(l.Rarity), 18, c, TextAnchor.MiddleCenter, true, false, true);
            rarity.Size(W - 64, 26);
            var name = Ui.Label(_modalBody, "Name", Loc.Tr(l.Name), 30, Color.white, TextAnchor.MiddleCenter, true, false, true);
            name.Size(W - 64, 40);
            var stat = Ui.Label(_modalBody, "Stat", "+" + l.V + " " + RunController.StatName(l.Stat) + " · " + Loc.Tr("equipped"), 17,
                Theme.RewardText, TextAnchor.MiddleCenter);
            stat.Size(W - 64, 26);
            Theme.Play("levelup", 0.8f);
        }

        void BuildEnd(Overlay o)
        {
            var won = o.Kind == OverlayKind.Clear;
            var col = _modalBody.Column(10, TextAnchor.MiddleCenter);
            col.childForceExpandWidth = false;
            if (won)
            {
                var pet = Widgets.Sprite(_modalBody, "Pet", "pet_pip", 96);
                pet.Size(96, 96);
            }
            var ribbon = Widgets.Ribbon(_modalBody, o.Title, won ? Theme.Gold : Theme.DefeatTop);
            ribbon.Size(360, 64);
            var chapter = Ui.Label(_modalBody, "Chapter", Loc.Chapter(_ch.Number, _ch.Roman) + " · " + Loc.Tr(_ch.Name), 18,
                Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            chapter.Size(W - 64, 28);

            var panel = Ui.Panel(_modalBody, "Days", Theme.Surface, 16);
            panel.Size(320, 120);
            var label = Ui.Label(panel.rectTransform, "Label", Loc.Tr("Days survived"), 16, Theme.TextMuted, TextAnchor.MiddleCenter);
            ((RectTransform)label.transform).Band(true, 8, 24);
            var days = Ui.Label(panel.rectTransform, "Value", o.Survived.ToString(), 54, Color.white, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)days.transform).Fill(0, 22, 0, 30);
            var best = Ui.Label(panel.rectTransform, "Best", Loc.Tr("Longest journey: {n}d", "n", o.Best), 14, Theme.RewardText, TextAnchor.MiddleCenter);
            ((RectTransform)best.transform).Band(false, 6, 22);

            if (!string.IsNullOrEmpty(o.Subtitle))
            {
                var note = Ui.Label(_modalBody, "Note", o.Subtitle, 16, Theme.Text, TextAnchor.MiddleCenter, false, true);
                note.Size(W - 80, 48);
            }
            var reps = Ui.Label(_modalBody, "Reps", Loc.Tr("{n} pushups this run", "n", _run.Reps), 16, Theme.RewardText, TextAnchor.MiddleCenter);
            reps.Size(W - 64, 26);
            Theme.Play(won ? "victory" : "defeat");
        }

        // ------------------------------------------------------------ saving

        /// <summary>Records the best journey and a cleared world, once.</summary>
        void Persist()
        {
            if (_persisted || _run == null) return;
            var o = _run.Overlay;
            var ended = _run.Finished || (o != null && (o.Kind == OverlayKind.Clear || o.Kind == OverlayKind.Defeat));
            Prefs.SetBest(_ch, Mathf.Max(_run.BestDays, ended ? 0 : _run.Day - 1));
            if (!ended) return;
            _persisted = true;
            if (_run.Won)
            {
                Prefs.Cleared = _ch.Number;
                if (_ch.Number < WorldData.Chapters.Length) Prefs.Chapter = _ch.Number + 1;
                Prefs.Save();
            }
        }

        void OnDestroy() => Persist();

        // ------------------------------------------------------------- types

        struct Floating
        {
            public UText Text;
            public Vector2 From;
            public float T, Scale;
        }

        struct BurstFx
        {
            public Image Img;
            public float T, From, To, Seconds;
            public Color Color;
        }
    }

    /// <summary>A figure on the road: a root that moves, a body that leans and
    /// breathes (pivot at the feet), the picture, and a white flash over it.</summary>
    public sealed class Fighter
    {
        public readonly RectTransform Root, Body;
        public readonly Image Pic;
        readonly Material _mat;
        string _art;
        float _flash = -1;

        public Fighter(Transform parent, string name, float size)
        {
            Root = Ui.Rect(parent, name);
            Root.anchorMin = Root.anchorMax = Vector2.zero;
            Root.pivot = new Vector2(0.5f, 0);
            Root.sizeDelta = new Vector2(size, size);
            Body = Ui.Rect(Root, "Body").Fill();
            Body.pivot = new Vector2(0.5f, 0);
            Pic = Ui.Img(Body, "Picture", Color.white);
            Pic.rectTransform.Fill();
            Pic.preserveAspect = true;
            _mat = Theme.FlashMaterial();
            if (_mat != null) Pic.material = _mat;
        }

        /// <summary>Front-facing creatures turn a little toward the hero.</summary>
        public void SetArt(string art, float size, bool front)
        {
            Root.sizeDelta = new Vector2(size, size);
            Pic.rectTransform.localRotation = front ? Quaternion.Euler(0, 24, 0) : Quaternion.identity;
            if (art == _art) return;
            _art = art;
            Pic.sprite = Theme.Art(art);
        }

        /// <summary>0..1 toward white. Without the shader, a bright tint stands in.</summary>
        public void SetFlash(float a)
        {
            if (Mathf.Approximately(a, _flash)) return;
            _flash = a;
            if (_mat != null)
            {
                _mat.SetFloat("_Flash", a);
                Pic.SetMaterialDirty();
            }
        }

        /// <summary>A slow squash and stretch, so nobody stands frozen.</summary>
        public void Breathe(float b, float sx = 1, float sy = 1, float scale = 1) =>
            Body.localScale = new Vector3((1 - b * 0.015f) * sx * scale, (1 + b * 0.035f) * sy * scale, 1);
    }
}
