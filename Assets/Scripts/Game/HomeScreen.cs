using System.Collections.Generic;
using PushupAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PushupAdventure.Game
{
    /// <summary>Camp: pick a world and a hero, then start the run.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        RectTransform _root, _preview;
        UText _chapterTitle, _blurb, _best, _special, _lockNote, _soundPill;
        Image _boss, _lockShade;
        RawImage _far, _near;
        CandyButton _start;
        readonly List<Image> _heroFrames = new List<Image>();
        float _t;

        ChapterDef Chapter => WorldData.Chapters[Prefs.Chapter - 1];

        public void Build()
        {
            _root = (RectTransform)transform;

            // Top bar: sound, title, language.
            var top = Ui.Rect(_root, "Top").Band(true, 12, 48);
            _soundPill = Widgets.Pill(top, "Sound", "", Theme.SurfaceDeep, ToggleSound, 120);
            ((RectTransform)_soundPill.transform.parent).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(120, 40));
            var lang = Widgets.Pill(top, "Language", Loc.Arabic ? "English" : "العربية", Theme.SurfaceDeep,
                () => GameApp.I.ToggleLanguage(), 110);
            ((RectTransform)lang.transform.parent).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(110, 40));

            var title = Ui.Label(_root, "Title", Loc.Tr("Push Up Adventure"), 40, Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)title.transform).Band(true, 66, 52);
            var tagline = Ui.Label(_root, "Tagline", Loc.Tr("Each pushup is a day on the road, or a blow in a fight."), 16,
                Theme.TextMuted, TextAnchor.MiddleCenter, false, true);
            ((RectTransform)tagline.transform).Band(true, 118, 26).offsetMin = new Vector2(20, 0);
            ((RectTransform)tagline.transform).offsetMax = new Vector2(-20, -118);

            BuildChapterCard();
            BuildHeroes();

            _start = Ui.Button(_root, "Start", Theme.Accent, Theme.AccentTop, StartRun);
            ((RectTransform)_start.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(320, 80));
            var hint = Ui.Label(_root, "Hint", Loc.Tr("Bosses wind up heavy blows: three quick pushups stagger them."), 14,
                Theme.TextMuted, TextAnchor.MiddleCenter, false, true);
            var hr = (RectTransform)hint.transform;
            hr.anchorMin = new Vector2(0, 0);
            hr.anchorMax = new Vector2(1, 0);
            hr.pivot = new Vector2(0.5f, 0);
            hr.offsetMin = new Vector2(24, 4);
            hr.offsetMax = new Vector2(-24, 36);

            Refresh();
        }

        void BuildChapterCard()
        {
            var card = Ui.Panel(_root, "World", Theme.Surface, 18);
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0, 1);
            cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.offsetMin = new Vector2(16, -500);
            cr.offsetMax = new Vector2(-16, -152);

            // The world's own scenery with its boss standing on the road.
            var view = Ui.Panel(cr, "View", Theme.SurfaceDeep, 14);
            _preview = view.rectTransform;
            _preview.anchorMin = new Vector2(0, 1);
            _preview.anchorMax = new Vector2(1, 1);
            _preview.pivot = new Vector2(0.5f, 1);
            _preview.offsetMin = new Vector2(8, -216);
            _preview.offsetMax = new Vector2(-8, -8);
            view.gameObject.AddComponent<RectMask2D>();
            _far = Ui.Rect(_preview, "Far").gameObject.AddComponent<RawImage>();
            _far.rectTransform.Fill();
            _far.raycastTarget = false;
            _near = Ui.Rect(_preview, "Near").gameObject.AddComponent<RawImage>();
            _near.rectTransform.Fill();
            _near.raycastTarget = false;
            _boss = Widgets.Sprite(_preview, "Boss", "foe_capling", 130);
            _boss.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(130, 130));
            _lockShade = Ui.Img(_preview, "Locked", new Color(0, 0, 0, 0.6f));
            _lockShade.rectTransform.Fill();
            _lockNote = Ui.Label(_lockShade.rectTransform, "Note", "", 22, Color.white, TextAnchor.MiddleCenter, true, true, true);
            ((RectTransform)_lockNote.transform).Fill(24, 0, 24, 0);

            var prev = Widgets.Pill(_preview, "Prev", "<", Theme.SurfaceDeep, () => Step(-1), 44, 44);
            ((RectTransform)prev.transform.parent).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(44, 44));
            prev.Text.fontSize = 26;
            var next = Widgets.Pill(_preview, "Next", ">", Theme.SurfaceDeep, () => Step(1), 44, 44);
            ((RectTransform)next.transform.parent).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(44, 44));
            next.Text.fontSize = 26;

            _chapterTitle = Ui.Label(cr, "Name", "", 24, Theme.GoldTop, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)_chapterTitle.transform).Band(true, 222, 34);
            _blurb = Ui.Label(cr, "Blurb", "", 15, Theme.Text, TextAnchor.UpperCenter, false, true);
            var br = (RectTransform)_blurb.transform;
            br.anchorMin = new Vector2(0, 0);
            br.anchorMax = new Vector2(1, 1);
            br.offsetMin = new Vector2(18, 30);
            br.offsetMax = new Vector2(-18, -258);
            _best = Ui.Label(cr, "Best", "", 14, Theme.RewardText, TextAnchor.MiddleCenter, false);
            ((RectTransform)_best.transform).Band(false, 6, 24);
        }

        void BuildHeroes()
        {
            var head = Ui.Label(_root, "Heroes", Loc.Tr("Choose your hero"), 22, Theme.Text, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)head.transform).Band(true, 508, 32);
            var row = Ui.Rect(_root, "HeroRow").Band(true, 544, 128);
            row.offsetMin = new Vector2(16, row.offsetMin.y);
            row.offsetMax = new Vector2(-16, row.offsetMax.y);
            var g = row.Row(8);
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = true;
            foreach (var c in Content.Classes)
            {
                var id = c.Id;
                var frame = Ui.Panel(row, c.Id, Theme.Outline, 14);
                _heroFrames.Add(frame);
                var face = Ui.Panel(frame.rectTransform, "Face", Theme.Surface, 12);
                face.rectTransform.Fill(3, 3, 3, 3);
                var pic = Widgets.Sprite(face.rectTransform, "Hero", c.Sprite, 88);
                pic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(88, 88));
                var name = Ui.Label(face.rectTransform, "Name", Loc.Tr(c.Name), 16, Color.white, TextAnchor.MiddleCenter, true, false, true);
                ((RectTransform)name.transform).Band(false, 4, 26);
                Tap.On(frame, () => { Prefs.HeroClass = id; Prefs.Save(); Theme.Play("pick", 0.6f); Refresh(); });
            }
            _special = Ui.Label(_root, "Special", "", 15, Theme.Text, TextAnchor.UpperCenter, false, true);
            var sr = (RectTransform)_special.transform;
            sr.anchorMin = new Vector2(0, 0);
            sr.anchorMax = new Vector2(1, 1);
            sr.offsetMin = new Vector2(24, 170);
            sr.offsetMax = new Vector2(-24, -680);
        }

        void Step(int d)
        {
            var n = Mathf.Clamp(Prefs.Chapter + d, 1, WorldData.Chapters.Length);
            if (n == Prefs.Chapter) return;
            Prefs.Chapter = n;
            Prefs.Save();
            Theme.Play("whoosh", 0.5f);
            Refresh();
        }

        void ToggleSound()
        {
            Theme.Muted = !Theme.Muted;
            Prefs.Save();
            Refresh();
        }

        void StartRun()
        {
            if (!Prefs.Open(Chapter)) return;
            Theme.Play("fight");
            GameApp.I.StartRun(Chapter, Prefs.HeroClass);
        }

        void Refresh()
        {
            var ch = Chapter;
            var open = Prefs.Open(ch);
            _soundPill.Set(Loc.Tr(Theme.Muted ? "Sound off" : "Sound on"));

            _far.texture = Theme.Art(ch.Far)?.texture;
            _near.texture = Theme.Art(ch.Near)?.texture;
            _boss.sprite = Theme.Art(ch.Boss.Img);
            _boss.rectTransform.localRotation = ch.Boss.Front ? Quaternion.Euler(0, 24, 0) : Quaternion.identity;
            _boss.color = open ? Color.white : Theme.Mystery;
            _lockShade.gameObject.SetActive(!open);
            if (!open)
            {
                var prev = WorldData.Chapters[Mathf.Max(0, ch.Number - 2)];
                _lockNote.Set(Loc.Tr("Locked") + "\n" + Loc.Tr("Clear {world} to open", "world", Loc.Tr(prev.Name)));
            }

            _chapterTitle.Set(Loc.Chapter(ch.Number, ch.Roman) + " · " + Loc.Tr(ch.Name));
            _blurb.Set(Loc.Tr(ch.Blurb));
            var best = Prefs.Best(ch);
            _best.Set(best > 0 ? Loc.Tr("Longest survived: {days}", "days", Loc.Days(best)) : "");

            var cls = Content.ClassById(Prefs.HeroClass);
            for (var i = 0; i < Content.Classes.Length; i++)
                _heroFrames[i].color = Content.Classes[i].Id == cls.Id ? Theme.GoldTop : Theme.Outline;
            _special.Set(Loc.Tr("Special") + ": *" + Loc.Tr(cls.Special) + "*\n" + Loc.Tr(cls.SpecialText));
            _special.SetKeywordColor(Theme.SpecialTop);

            _start.SetText(Loc.Tr(open ? "Start" : "Locked"), open ? Loc.Tr("30 days · {tier}", "tier", Loc.Tr("Normal")) : null);
            if (open) _start.Paint(Theme.Accent, Theme.AccentTop);
            else _start.Paint(Theme.Surface, Theme.TextMuted);
            _start.Interactable = open;
        }

        void Update()
        {
            // The scenery drifts behind the boss; the boss breathes.
            _t += Time.deltaTime;
            SetCover(_far, _t * 0.004f);
            SetCover(_near, _t * 0.012f);
            var b = Mathf.Sin(_t * 2.4f) * 0.5f + 0.5f;
            _boss.rectTransform.localScale = new Vector3(1 - b * 0.015f, 1 + b * 0.035f, 1);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartRun();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Step(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) Step(1);
        }

        /// <summary>The painting fills the box by height, repeating sideways.</summary>
        static void SetCover(RawImage img, float scroll)
        {
            if (img.texture == null) return;
            var r = img.rectTransform.rect;
            var tileW = r.height * img.texture.width / img.texture.height;
            img.uvRect = new Rect(scroll, 0, r.width / Mathf.Max(1, tileW), 1);
        }
    }
}
