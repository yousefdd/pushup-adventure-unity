using UnityEngine;
using UnityEngine.UI;

namespace DiceQuest.Game
{
    /// <summary>A figure on the board or in a fight: a root that moves, a body that
    /// leans and breathes (pivot at the feet), and the picture, which can flash white.</summary>
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
