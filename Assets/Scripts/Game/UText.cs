using System.Collections.Generic;
using System.Text;
using PushupAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PushupAdventure.Game
{
    /// <summary>A label that reads right in both languages. Unity's legacy text
    /// cannot join Arabic letters or run right to left, so Arabic is shaped
    /// and wrapped here, line by line, and handed over in visual order.
    /// Words in *stars* are keywords and take the keyword colour.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class UText : MonoBehaviour
    {
        Text _t;
        string _logical = "";
        bool _display, _wrap;
        TextAnchor _anchor;
        Color _keyword = Theme.Keyword;
        float _width = -1;
        bool _arabicWas, _rewrap;

        public Text Text => _t;
        public string Value => _logical;

        public void Init(bool display, int size, Color color, TextAnchor anchor, bool wrap)
        {
            _t = GetComponent<Text>();
            _display = display;
            _wrap = wrap;
            _anchor = anchor;
            _t.fontSize = size;
            _t.color = color;
            _t.raycastTarget = false;
            _t.supportRichText = true;
            _t.verticalOverflow = VerticalWrapMode.Overflow;
            _t.lineSpacing = display ? 0.95f : 1.05f;
            Refresh();
        }

        public void SetKeywordColor(Color c)
        {
            _keyword = c;
            Refresh();
        }

        public void Set(string logical)
        {
            logical = logical ?? "";
            if (logical == _logical && _arabicWas == Loc.Arabic) return;
            _logical = logical;
            Refresh();
        }

        public void SetColor(Color c) => _t.color = c;

        void OnRectTransformDimensionsChange()
        {
            if (_t == null || !_wrap) return;
            var w = ((RectTransform)transform).rect.width;
            if (Mathf.Abs(w - _width) > 0.5f) _rewrap = true;
        }

        // Called from inside a layout pass: changing the text there would be
        // lost, so the new width is applied once the pass is over.
        void LateUpdate()
        {
            if (!_rewrap) return;
            _rewrap = false;
            Refresh();
        }

        public void Refresh()
        {
            if (_t == null) return;
            _arabicWas = Loc.Arabic;
            var marked = Mark(_logical);
            // Arabic words get the Arabic font and shaping in either language
            // (the language button shows "العربية" on the English screen).
            var rtl = ArabicShaper.HasArabic(marked);
            _t.font = Theme.Font(_display, Loc.Arabic || rtl);
            _t.alignment = rtl ? Mirror(_anchor) : _anchor;
            _width = ((RectTransform)transform).rect.width;
            if (!rtl)
            {
                _t.horizontalOverflow = _wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                _t.text = Tags(marked, false);
                return;
            }
            _t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var lines = _wrap && _width > 1 ? Wrap(marked, _width) : new List<string>(marked.Split('\n'));
            var o = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0) o.Append('\n');
                o.Append(ArabicShaper.Visual(ArabicShaper.Shape(lines[i])));
            }
            _t.text = Tags(o.ToString(), true);
        }

        static TextAnchor Mirror(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return a;
            }
        }

        /// <summary>*word* → marks around the word; rich-text brackets are escaped.</summary>
        static string Mark(string s)
        {
            if (s.IndexOf('*') < 0) return s;
            var o = new StringBuilder(s.Length);
            var open = false;
            foreach (var c in s)
            {
                if (c == '*')
                {
                    o.Append(open ? ArabicShaper.KeyClose : ArabicShaper.KeyOpen);
                    open = !open;
                }
                else o.Append(c);
            }
            if (open) o.Append(ArabicShaper.KeyClose);
            return o.ToString();
        }

        /// <summary>Marks → colour tags. In visual (right-to-left) order the
        /// close mark comes first.</summary>
        string Tags(string s, bool visual)
        {
            if (s.IndexOf(ArabicShaper.KeyOpen) < 0 && s.IndexOf(ArabicShaper.KeyClose) < 0) return s;
            var open = "<color=#" + ColorUtility.ToHtmlStringRGB(_keyword) + ">";
            const string close = "</color>";
            return s.Replace(visual ? ArabicShaper.KeyClose : ArabicShaper.KeyOpen, '\u0001')
                .Replace(visual ? ArabicShaper.KeyOpen : ArabicShaper.KeyClose, '\u0002')
                .Replace("\u0001", open).Replace("\u0002", close);
        }

        /// <summary>Greedy word wrap on the shaped widths, in reading order. A
        /// keyword split across lines is closed and reopened.</summary>
        List<string> Wrap(string s, float width)
        {
            var lines = new List<string>();
            var font = _t.font;
            var size = _t.fontSize;
            var style = _t.fontStyle;
            var space = Advance(font, " ", size, style);
            var inKey = false;
            foreach (var para in s.Split('\n'))
            {
                var line = new StringBuilder();
                var lineW = 0f;
                foreach (var word in para.Split(' '))
                {
                    var w = Advance(font, ArabicShaper.Shape(word), size, style);
                    if (line.Length > 0 && lineW + space + w > width * 0.98f)
                    {
                        if (inKey) line.Append(ArabicShaper.KeyClose);
                        lines.Add(line.ToString());
                        line.Clear();
                        if (inKey) line.Append(ArabicShaper.KeyOpen);
                        lineW = 0;
                    }
                    if (lineW > 0) line.Append(' ');
                    lineW += (lineW > 0 ? space : 0) + w;
                    line.Append(word);
                    foreach (var c in word)
                    {
                        if (c == ArabicShaper.KeyOpen) inKey = true;
                        else if (c == ArabicShaper.KeyClose) inKey = false;
                    }
                }
                if (inKey) line.Append(ArabicShaper.KeyClose);
                lines.Add(line.ToString());
                if (inKey) inKey = false;
            }
            return lines;
        }

        static float Advance(Font font, string s, int size, FontStyle style)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            font.RequestCharactersInTexture(s, size, style);
            var w = 0f;
            foreach (var c in s)
            {
                CharacterInfo ci;
                if (font.GetCharacterInfo(c, out ci, size, style)) w += ci.advance;
            }
            return w;
        }
    }
}
