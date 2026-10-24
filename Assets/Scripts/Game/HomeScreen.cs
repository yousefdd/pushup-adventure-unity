using System.Collections.Generic;
using DiceQuest.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiceQuest.Game
{
    /// <summary>Camp: pick a hero, then play.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        RectTransform _root;
        UText _soundPill, _heroName, _perk, _stats, _best;
        Fighter _hero;
        Die _die;
        RawImage _far, _near;
        readonly List<Image> _frames = new List<Image>();
        float _t, _dieT;

        public void Build()
        {
            _root = (RectTransform)transform;

            var top = Ui.Rect(_root, "Top").Band(true, 12, 48);
            _soundPill = Widgets.Pill(top, "Sound", "", Theme.SurfaceDeep, ToggleSound, 120);
            ((RectTransform)_soundPill.transform.parent).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(120, 40));
            var lang = Widgets.Pill(top, "Language", Loc.Arabic ? "English" : "العربية", Theme.SurfaceDeep,
                () => GameApp.I.ToggleLanguage(), 110);
            ((RectTransform)lang.transform.parent).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(110, 40));

            var title = Ui.Label(_root, "Title", Loc.Tr("Dice Quest"), 50, Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)title.transform).Band(true, 68, 62);
            var tag = Ui.Label(_root, "Tagline", Loc.Tr("Roll the die, walk the board, beat the boss."), 16, Theme.TextMuted,
                TextAnchor.MiddleCenter, false, true);
            ((RectTransform)tag.transform).Band(true, 128, 26);

            // The chosen hero stands in the first world, a die tumbling beside them.
            var stage = Ui.Panel(_root, "Stage", Theme.SurfaceDeep, 18);
            var sr = stage.rectTransform;
            sr.anchorMin = new Vector2(0, 1);
            sr.anchorMax = new Vector2(1, 1);
            sr.pivot = new Vector2(0.5f, 1);
            sr.offsetMin = new Vector2(16, -420);
            sr.offsetMax = new Vector2(-16, -164);
            stage.gameObject.AddComponent<RectMask2D>();
            var w = WorldData.Chapters[0];
            _far = Backdrop(sr, w.Far);
            _near = Backdrop(sr, w.Near);
            var shade = Ui.Img(sr, "Shade", new Color(0, 0, 0, 0.15f));
            shade.rectTransform.Fill();
            _hero = new Fighter(sr, "Hero", 150);
            _hero.Root.anchoredPosition = new Vector2(190, 14);
            _die = new Die(sr, 84);
            _die.Root.anchorMin = _die.Root.anchorMax = Vector2.zero;
            _die.Root.anchoredPosition = new Vector2(380, 80);

            _heroName = Ui.Label(_root, "Name", "", 30, Color.white, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_heroName.transform).Band(true, 426, 40);
            _perk = Ui.Label(_root, "Perk", "", 16, Theme.Text, TextAnchor.MiddleCenter, false, true);
            ((RectTransform)_perk.transform).Band(true, 466, 26);
            _stats = Ui.Label(_root, "Stats", "", 17, Theme.GoldTop, TextAnchor.MiddleCenter, true);
            ((RectTransform)_stats.transform).Band(true, 494, 26);

            var head = Ui.Label(_root, "Choose", Loc.Tr("Choose your hero"), 20, Theme.TextMuted, TextAnchor.MiddleCenter, true);
            ((RectTransform)head.transform).Band(true, 530, 28);
            var row = Ui.Rect(_root, "Heroes").Band(true, 562, 124);
            row.offsetMin = new Vector2(16, row.offsetMin.y);
            row.offsetMax = new Vector2(-16, row.offsetMax.y);
            var g = row.Row(8);
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = true;
            foreach (var h in Heroes.All)
            {
                var id = h.Id;
                var frame = Ui.Panel(row, id, Theme.Outline, 14);
                _frames.Add(frame);
                var face = Ui.Panel(frame.rectTransform, "Face", Theme.Surface, 12);
                face.rectTransform.Fill(3, 3, 3, 3);
                var pic = Widgets.Sprite(face.rectTransform, "Pic", h.Sprite, 86);
                pic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(86, 86));
                var name = Ui.Label(face.rectTransform, "Name", Loc.Tr(h.Name), 15, Color.white, TextAnchor.MiddleCenter, true, false, true);
                ((RectTransform)name.transform).Band(false, 4, 24);
                Tap.On(frame, () => { Prefs.Hero = id; Prefs.Save(); Theme.Play("pick", 0.6f); Refresh(); });
            }

            _best = Ui.Label(_root, "Best", "", 15, Theme.RewardText, TextAnchor.MiddleCenter);
            ((RectTransform)_best.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 132), new Vector2(400, 24));
            var play = Ui.Button(_root, "Play", Theme.Accent, Theme.AccentTop, Play);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(320, 86));
            play.SetText(Loc.Tr("Play"));
            play.Title.Text.fontSize = 36;

            Refresh();
        }

        RawImage Backdrop(RectTransform parent, string art)
        {
            var r = Ui.Rect(parent, art).gameObject.AddComponent<RawImage>();
            r.rectTransform.Fill();
            r.raycastTarget = false;
            var s = Theme.Art(art);
            r.texture = s != null ? s.texture : null;
            return r;
        }

        void ToggleSound()
        {
            Theme.Muted = !Theme.Muted;
            Prefs.Save();
            Refresh();
        }

        void Play()
        {
            Theme.Play("fight");
            GameApp.I.StartRun(Prefs.Hero);
        }

        void Refresh()
        {
            _soundPill.Set(Loc.Tr(Theme.Muted ? "Sound off" : "Sound on"));
            var h = Heroes.ById(Prefs.Hero);
            _hero.SetArt(h.Sprite, 150, false);
            _heroName.Set(Loc.Tr(h.Name));
            _perk.Set(Loc.Tr(h.Perk));
            _stats.Set(Loc.Tr("HP") + " " + h.Hp + "   " + Loc.Tr("ATK") + " " + h.Atk + "   " + Loc.Tr("DEF") + " " + h.Def);
            for (var i = 0; i < Heroes.All.Length; i++)
                _frames[i].color = Heroes.All[i].Id == h.Id ? Theme.GoldTop : Theme.Outline;
            _best.Set(Prefs.BestWorld > 0 ? Loc.Tr("Best: world {n}", "n", Prefs.BestWorld) : "");
        }

        void Update()
        {
            _t += Time.deltaTime;
            Scroll(_far, _t * 0.004f);
            Scroll(_near, _t * 0.012f);
            _hero.Breathe(Mathf.Sin(_t * 3.9f) * 0.5f + 0.5f);
            // The die tumbles every couple of seconds.
            _dieT += Time.deltaTime;
            var spin = _dieT % 2.4f;
            if (spin < 0.6f)
            {
                _die.Root.localRotation = Quaternion.Euler(0, 0, spin / 0.6f * 360);
                _die.Root.anchoredPosition = new Vector2(380, 80 + Mathf.Sin(spin / 0.6f * Mathf.PI) * 50);
                if (Mathf.FloorToInt(_dieT * 12) % 2 == 0) _die.Set(Random.Range(1, 7));
            }
            else
            {
                _die.Root.localRotation = Quaternion.identity;
                _die.Root.anchoredPosition = new Vector2(380, 80);
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Play();
        }

        static void Scroll(RawImage img, float x)
        {
            if (img.texture == null) return;
            var r = img.rectTransform.rect;
            var tileW = r.height * img.texture.width / img.texture.height;
            img.uvRect = new Rect(x, 0, r.width / Mathf.Max(1, tileW), 1);
        }
    }
}
