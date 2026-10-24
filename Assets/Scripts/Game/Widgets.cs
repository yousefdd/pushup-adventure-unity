using System;
using DiceQuest.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceQuest.Game
{
    /// <summary>A rounded bar with a fill and an optional number on it.</summary>
    public sealed class Bar
    {
        public readonly RectTransform Root;
        readonly RectTransform _fill;
        readonly Image _fillImg;
        readonly UText _label;
        float _shown = -1;

        public Bar(Transform parent, string name, Color color, float height, bool label)
        {
            var bg = Ui.Panel(parent, name, Theme.Outline, Mathf.Max(2, (int)(height / 2)));
            Root = bg.rectTransform;
            var inner = Ui.Panel(Root, "Track", Theme.SurfaceDeep, Mathf.Max(2, (int)(height / 2) - 2));
            inner.rectTransform.Fill(2, 2, 2, 2);
            _fillImg = Ui.Panel(inner.rectTransform, "Fill", color, Mathf.Max(2, (int)(height / 2) - 2));
            _fill = _fillImg.rectTransform;
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(1, 1);
            _fill.offsetMin = _fill.offsetMax = Vector2.zero;
            if (label)
            {
                _label = Ui.Label(Root, "Value", "", Mathf.RoundToInt(height * 0.85f), Color.white, TextAnchor.MiddleCenter, true, false, true);
                ((RectTransform)_label.transform).Fill(0, -4, 0, -4);
            }
        }

        /// <summary>Value 0..1; the fill eases toward it.</summary>
        public void Set(float v, string text = null)
        {
            v = Mathf.Clamp01(v);
            _shown = _shown < 0 ? v : Mathf.MoveTowards(_shown, v, Time.deltaTime * 1.6f);
            _fill.anchorMax = new Vector2(_shown, 1);
            _fillImg.enabled = _shown > 0.01f;
            if (_label != null && text != null) _label.Set(text);
        }

        public void SetColor(Color c) => _fillImg.color = c;
    }

    /// <summary>A small round button with a glyph or a word.</summary>
    public sealed class Tap : MonoBehaviour, IPointerClickHandler
    {
        public Action OnTap;
        public void OnPointerClick(PointerEventData e) => OnTap?.Invoke();

        public static Tap On(Component c, Action onTap)
        {
            var g = c.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
            var t = c.gameObject.AddComponent<Tap>();
            t.OnTap = onTap;
            return t;
        }
    }

    public static class Widgets
    {
        /// <summary>A pill with a word, tappable.</summary>
        public static UText Pill(Transform parent, string name, string text, Color bg, Action onTap, float w, float h = 40)
        {
            var p = Ui.Panel(parent, name, bg, (int)(h / 2));
            p.rectTransform.sizeDelta = new Vector2(w, h);
            var rim = p.gameObject.AddComponent<Outline>();
            rim.effectColor = Theme.Outline;
            rim.effectDistance = new Vector2(2, -2);
            var l = Ui.Label(p.rectTransform, "Text", text, 17, Color.white, TextAnchor.MiddleCenter, false, false, true);
            ((RectTransform)l.transform).Fill(8, 0, 8, 0);
            if (onTap != null) Tap.On(p, onTap);
            return l;
        }

        /// <summary>A gold ribbon with the title on it.</summary>
        public static RectTransform Ribbon(Transform parent, string text, Color face, float width = 360)
        {
            var root = Ui.Rect(parent, "Ribbon");
            root.sizeDelta = new Vector2(width, 64);
            var tailL = Ui.Panel(root, "TailL", Color.Lerp(face, Color.black, 0.35f), 8);
            tailL.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(-14, -8), new Vector2(60, 44));
            var tailR = Ui.Panel(root, "TailR", Color.Lerp(face, Color.black, 0.35f), 8);
            tailR.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(14, -8), new Vector2(60, 44));
            var rim = Ui.Panel(root, "Rim", Theme.Outline, 14);
            rim.rectTransform.Fill(0, 2, 0, 2);
            var body = Ui.Panel(root, "Body", face, 12);
            body.rectTransform.Fill(3, 5, 3, 5);
            var shine = Ui.Panel(body.rectTransform, "Shine", new Color(1, 1, 1, 0.25f), 10);
            shine.rectTransform.anchorMin = new Vector2(0, 0.55f);
            shine.rectTransform.anchorMax = Vector2.one;
            shine.rectTransform.offsetMin = new Vector2(6, 0);
            shine.rectTransform.offsetMax = new Vector2(-6, -4);
            var l = Ui.Label(root, "Title", text, 32, Color.white, TextAnchor.MiddleCenter, true, false, true);
            ((RectTransform)l.transform).Fill(10, 4, 10, 4);
            return root;
        }

        /// <summary>A picture of a creature, hero or item, sitting on the bottom of its box.</summary>
        public static Image Sprite(Transform parent, string name, string art, float size)
        {
            var i = Ui.Img(parent, name, Color.white, Theme.Art(art));
            i.preserveAspect = true;
            i.rectTransform.sizeDelta = new Vector2(size, size);
            return i;
        }

        /// <summary>A treasure chest made of shapes.</summary>
        public static RectTransform Chest(Transform parent, float size)
        {
            var root = Ui.Rect(parent, "Chest");
            root.sizeDelta = new Vector2(size, size * 0.8f);
            var wood = new Color(0.55f, 0.32f, 0.16f);
            var dark = new Color(0.33f, 0.18f, 0.09f);
            var body = Ui.Panel(root, "Body", dark, 8);
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = new Vector2(1, 0.62f);
            body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
            var face = Ui.Panel(body.rectTransform, "Face", wood, 6);
            face.rectTransform.Fill(4, 4, 4, 4);
            var lid = Ui.Panel(root, "Lid", dark, 16);
            lid.rectTransform.anchorMin = new Vector2(0, 0.55f);
            lid.rectTransform.anchorMax = Vector2.one;
            lid.rectTransform.offsetMin = lid.rectTransform.offsetMax = Vector2.zero;
            var lidFace = Ui.Panel(lid.rectTransform, "Face", Color.Lerp(wood, Color.white, 0.1f), 13);
            lidFace.rectTransform.Fill(4, 4, 4, 4);
            foreach (var x in new[] { 0.18f, 0.82f })
            {
                var band = Ui.Img(root, "Band", Theme.Gold);
                band.rectTransform.Place(new Vector2(x, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(size * 0.1f, size * 0.8f));
            }
            var lockRim = Ui.Panel(root, "Lock", Theme.Outline, 6);
            lockRim.rectTransform.Place(new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.2f, size * 0.24f));
            var lockFace = Ui.Panel(lockRim.rectTransform, "Face", Theme.GoldTop, 4);
            lockFace.rectTransform.Fill(3, 3, 3, 3);
            return root;
        }

        public static string RarityName(Rarity r) => Loc.Tr(r.ToString());
    }

    /// <summary>A die: a rounded white square with pips.</summary>
    public sealed class Die
    {
        public readonly RectTransform Root;
        readonly Image[] _pips = new Image[9];
        readonly Image _face;

        // Which of the 3×3 pip spots each face lights (0 = top left).
        static readonly int[][] Faces =
        {
            new[] { 4 }, new[] { 2, 6 }, new[] { 2, 4, 6 }, new[] { 0, 2, 6, 8 },
            new[] { 0, 2, 4, 6, 8 }, new[] { 0, 2, 3, 5, 6, 8 },
        };

        public Die(Transform parent, float size)
        {
            var rim = Ui.Panel(parent, "Die", Theme.Outline, (int)(size * 0.18f));
            Root = rim.rectTransform;
            Root.sizeDelta = new Vector2(size, size);
            var shade = Ui.Panel(Root, "Shade", new Color(0.82f, 0.84f, 0.9f), (int)(size * 0.16f));
            shade.rectTransform.Fill(size * 0.04f, size * 0.04f, size * 0.04f, size * 0.04f);
            _face = Ui.Panel(Root, "Face", Color.white, (int)(size * 0.15f));
            _face.rectTransform.Fill(size * 0.05f, size * 0.09f, size * 0.05f, size * 0.04f);
            var pip = size * 0.17f;
            for (var i = 0; i < 9; i++)
            {
                var x = (i % 3 - 1) * size * 0.26f;
                var y = (1 - i / 3) * size * 0.26f;
                _pips[i] = Ui.Img(_face.rectTransform, "Pip", Theme.Outline, Ui.Circle);
                _pips[i].rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(pip, pip));
            }
            Set(1);
        }

        public void Set(int value)
        {
            var lit = Faces[Mathf.Clamp(value, 1, 6) - 1];
            for (var i = 0; i < 9; i++) _pips[i].enabled = System.Array.IndexOf(lit, i) >= 0;
            // Ones are red, like a real die.
            if (value == 1) _pips[4].color = Theme.Danger;
            else _pips[4].color = Theme.Outline;
        }
    }
}
