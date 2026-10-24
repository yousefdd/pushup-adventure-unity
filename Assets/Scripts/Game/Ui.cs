using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PushupAdventure.Game
{
    /// <summary>Builds the interface in code: rectangles, rounded panels,
    /// labels and candy buttons. There are no prefabs or scene files to edit.</summary>
    public static class Ui
    {
        // -------------------------------------------------------- shapes

        static readonly Dictionary<int, Sprite> Rounds = new Dictionary<int, Sprite>();
        static Sprite _triangle, _ring, _glow;

        /// <summary>A white rounded rectangle, nine-sliced, corner radius r units.</summary>
        public static Sprite Round(int r)
        {
            Sprite s;
            if (Rounds.TryGetValue(r, out s)) return s;
            var n = r * 2 + 4;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            var c = (n - 1) / 2f;
            var inner = c - r;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var dx = Mathf.Max(0, Mathf.Abs(x - c) - inner);
                var dy = Mathf.Max(0, Mathf.Abs(y - c) - inner);
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.Clamp01(r - d + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            s = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(r + 1, r + 1, r + 1, r + 1));
            Rounds[r] = s;
            return s;
        }

        public static Sprite Circle => Round(64);

        /// <summary>A white triangle pointing down.</summary>
        public static Sprite Triangle
        {
            get
            {
                if (_triangle != null) return _triangle;
                const int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var half = y / 2f; // width grows toward the top
                    var a = Mathf.Clamp01(half - Mathf.Abs(x - n / 2f) + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
                t.SetPixels32(px);
                t.Apply(false, true);
                return _triangle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
            }
        }

        /// <summary>A soft round glow, bright in the middle.</summary>
        public static Sprite Glow
        {
            get
            {
                if (_glow != null) return _glow;
                const int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    var a = Mathf.Clamp01(1 - d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
                t.SetPixels32(px);
                t.Apply(false, true);
                return _glow = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
            }
        }

        /// <summary>A thin white ring.</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring != null) return _ring;
                const int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - 0.5f, n / 2f - 0.5f));
                    var a = Mathf.Clamp01(1 - Mathf.Abs(d - 56) / 6f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
                t.SetPixels32(px);
                t.Apply(false, true);
                return _ring = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
            }
        }

        // ------------------------------------------------------- objects

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchor to a box of the parent (0..1) with an inset in units.</summary>
        public static RectTransform Fill(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Fixed size, placed by an anchor point of the parent and a pivot.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>A band across the parent at the top (or bottom) with a fixed height.</summary>
        public static RectTransform Band(this RectTransform rt, bool top, float offset, float height)
        {
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.anchoredPosition = new Vector2(0, top ? -offset : offset);
            rt.sizeDelta = new Vector2(0, height);
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rt = Rect(parent, name);
            var i = rt.gameObject.AddComponent<Image>();
            i.color = color;
            i.sprite = sprite;
            i.raycastTarget = false;
            if (sliced) i.type = Image.Type.Sliced;
            else if (sprite != null) i.preserveAspect = true;
            return i;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius) =>
            Img(parent, name, color, Round(radius), true);

        public static UText Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, bool display = false, bool wrap = false, bool stroke = false)
        {
            var rt = Rect(parent, name);
            rt.gameObject.AddComponent<Text>();
            if (stroke)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.08f, 0.09f, 0.12f, 0.95f);
                o.effectDistance = new Vector2(2, -2);
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.5f);
                sh.effectDistance = new Vector2(0, -3);
            }
            var u = rt.gameObject.AddComponent<UText>();
            u.Init(display, size, color, anchor, wrap);
            u.Set(text);
            return u;
        }

        /// <summary>A candy button: a dark rim, a coloured face with a lighter
        /// top, a label and a small line under it.</summary>
        public static CandyButton Button(Transform parent, string name, Color face, Color top, Action onTap)
        {
            var rt = Rect(parent, name);
            var b = rt.gameObject.AddComponent<CandyButton>();
            b.Build(face, top, onTap);
            return b;
        }

        public static LayoutElement Size(this Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            // Not ??: a missing component is a Unity "fake null" in the editor.
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) le.preferredHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static HorizontalLayoutGroup Row(this RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup Column(this RectTransform rt, float spacing, TextAnchor align = TextAnchor.UpperCenter)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static void Clear(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        // ------------------------------------------------------- easing

        public static float OutCubic(float t) => 1 - Mathf.Pow(1 - Mathf.Clamp01(t), 3);
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        /// <summary>0 → 1 → 0 over t in 0..1: out fast, back slower.</summary>
        public static float Dash(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.35f ? OutCubic(t / 0.35f) : 1 - OutCubic((t - 0.35f) / 0.65f);
        }
    }

    /// <summary>The big button. It sinks while held and runs its action on release.</summary>
    public sealed class CandyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Image Rim, Face, Shine;
        public UText Title, Sub;
        public RectTransform Body;
        Action _onTap;
        bool _down;
        public bool Interactable = true;

        public void Build(Color face, Color top, Action onTap)
        {
            _onTap = onTap;
            var rt = (RectTransform)transform;
            var hit = gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Rim = Ui.Panel(rt, "Rim", Color.black, 18);
            Rim.rectTransform.Fill();
            Body = Ui.Rect(rt, "Body").Fill(0, 5, 0, 0);
            Face = Ui.Panel(Body, "Face", face, 18);
            Face.rectTransform.Fill(3, 3, 3, 3);
            Shine = Ui.Panel(Body, "Shine", top, 14);
            Shine.rectTransform.anchorMin = new Vector2(0, 0.5f);
            Shine.rectTransform.anchorMax = new Vector2(1, 1);
            Shine.rectTransform.offsetMin = new Vector2(7, 0);
            Shine.rectTransform.offsetMax = new Vector2(-7, -6);
            Shine.color = new Color(top.r, top.g, top.b, 0.55f);
            Title = Ui.Label(Body, "Title", "", 30, Color.white, TextAnchor.MiddleCenter, true, false, true);
            Title.transform.SetAsLastSibling();
            ((RectTransform)Title.transform).Fill(8, 0, 8, 0);
            Sub = Ui.Label(Body, "Sub", "", 14, new Color(1, 1, 1, 0.92f), TextAnchor.MiddleCenter, false, false, true);
            Paint(face, top);
        }

        /// <summary>Title only, or title with a small line under it.</summary>
        public void SetText(string title, string sub = null)
        {
            Title.Set(title);
            Sub.Set(sub ?? "");
            var two = !string.IsNullOrEmpty(sub);
            var tr = (RectTransform)Title.transform;
            var sr = (RectTransform)Sub.transform;
            if (two)
            {
                tr.anchorMin = new Vector2(0, 0.38f);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(8, 0);
                tr.offsetMax = new Vector2(-8, -2);
                sr.anchorMin = new Vector2(0, 0);
                sr.anchorMax = new Vector2(1, 0.42f);
                sr.offsetMin = new Vector2(8, 6);
                sr.offsetMax = new Vector2(-8, 0);
            }
            else
            {
                tr.Fill(8, 0, 8, 0);
            }
            Sub.gameObject.SetActive(two);
        }

        public void Paint(Color face, Color top)
        {
            Face.color = face;
            Shine.color = new Color(top.r, top.g, top.b, 0.55f);
            Rim.color = Color.Lerp(face, Color.black, 0.55f);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable) return;
            _down = true;
            Body.offsetMin = new Vector2(0, 1);
            Body.offsetMax = new Vector2(0, -4);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _down = false;
            Body.offsetMin = new Vector2(0, 5);
            Body.offsetMax = Vector2.zero;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable || _down) return;
            _onTap?.Invoke();
        }

        public void Tap() => _onTap?.Invoke();
    }
}
