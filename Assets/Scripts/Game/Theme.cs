using System.Collections.Generic;
using PushupAdventure.Core;
using UnityEngine;

namespace PushupAdventure.Game
{
    /// <summary>The look of the game: colour roles and sizes (the same values as
    /// the Flutter version's tokens), fonts, pictures and sounds.</summary>
    public static class Theme
    {
        static Color Hex(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        public static Color Rgb(int rgb) => Hex((uint)rgb);

        public static readonly Color Bg = Hex(0x1F1F29);
        public static readonly Color Surface = Hex(0x2A2A35);
        public static readonly Color SurfaceDeep = Hex(0x16161D);
        public static readonly Color Outline = Hex(0x20262E);
        public static readonly Color Text = Color.white;
        public static readonly Color TextMuted = Hex(0xA6A9BD);
        public static readonly Color TextOnCard = Hex(0x26303F);
        public static readonly Color TextOnCardMuted = Hex(0x3B4250);
        public static readonly Color Keyword = Hex(0xB4460C);
        public static readonly Color RewardBg = Hex(0x1D2A3A);
        public static readonly Color RewardText = Hex(0x8FF07A);
        public static readonly Color Accent = Hex(0x2E8B2A);
        public static readonly Color AccentTop = Hex(0x7BE05A);
        public static readonly Color Danger = Hex(0xC7402E);
        public static readonly Color DangerTop = Hex(0xFFA24A);
        public static readonly Color Gold = Hex(0xF0A52A);
        public static readonly Color GoldTop = Hex(0xFFD65A);
        public static readonly Color OnGold = Hex(0x20262E);
        public static readonly Color Hp = Hex(0xD93A52);
        public static readonly Color Xp = Hex(0xFFD65A);
        public static readonly Color Band = Hex(0x3B2A22);
        public static readonly Color BandTop = Hex(0x7A5236);
        public static readonly Color BandChip = Hex(0x1E1511);
        public static readonly Color Road = Hex(0xE2D2A4);
        public static readonly Color Paper = Hex(0xFBF1DA);
        public static readonly Color FriendWord = Hex(0x2F6FD6);
        public static readonly Color FoeWord = Hex(0xD9452B);
        public static readonly Color Ability = Hex(0xB58CFF);
        public static readonly Color Defeat = Hex(0x5B3F9E);
        public static readonly Color DefeatTop = Hex(0x8A6AD8);
        public static readonly Color Orange = Hex(0xE07A1F);
        public static readonly Color OrangeTop = Hex(0xFFB54A);
        public static readonly Color Special = Hex(0xA05AF0);
        public static readonly Color SpecialTop = Hex(0xE0A0FF);
        public static readonly Color CardBorder = Hex(0x7FD3EC);
        public static readonly Color CardHead = Hex(0x2A6F8F);
        public static readonly Color Card = Hex(0xFBF6EA);
        public static readonly Color Mystery = Hex(0x0E0E16);

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return Hex(0x5FD08C);
                case Rarity.Rare: return Hex(0x6BA8FF);
                case Rarity.Epic: return Hex(0xA98CFF);
                case Rarity.Legendary: return Hex(0xFFD24A);
                case Rarity.Mythic: return Hex(0xFF5A6A);
                default: return Hex(0x8B82A8);
            }
        }

        public static Color AbilityColor(FoeAbility a)
        {
            switch (a)
            {
                case FoeAbility.Regen: return Hex(0x5FD08C);
                case FoeAbility.StoneSkin: return Hex(0xB9B0A0);
                case FoeAbility.Frenzy: return Hex(0xFF6A3D);
                case FoeAbility.Drain: return Hex(0xD93A52);
                case FoeAbility.Venom: return Hex(0x9BE04A);
                case FoeAbility.Breath: return Hex(0xFFA24A);
                case FoeAbility.Hex: return Hex(0xB58CFF);
                default: return Hex(0x7FD3EC);
            }
        }

        // Sizes, in canvas units (the canvas is 540 wide).
        public const float Width = 540;
        public const float Scene = 340;
        public const float SceneryShare = 0.36f;
        public const float Fighter = 96;
        public const float FighterBoss = 132;
        public const float Pet = 48;
        public const float Button = 72;
        public const float StatBand = 60;

        // ------------------------------------------------------------ fonts

        static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        static Font LoadFont(string name)
        {
            Font f;
            if (!Fonts.TryGetValue(name, out f) || f == null)
            {
                f = Resources.Load<Font>("Fonts/" + name);
                if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Fonts[name] = f;
            }
            return f;
        }

        /// <summary>Display type for titles and numbers, body type for reading;
        /// Arabic gets fonts drawn for it.</summary>
        public static Font Font(bool display) =>
            Loc.Arabic
                ? LoadFont(display ? "Lalezar-Regular" : "Tajawal-ExtraBold")
                : LoadFont(display ? "LilitaOne-Regular" : "nunito_extrabold");

        // --------------------------------------------------------- pictures

        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        static Shader _flash;
        static bool _flashLoaded;

        public static Sprite Art(string name)
        {
            Sprite s;
            if (Sprites.TryGetValue(name, out s) && s != null) return s;
            s = Resources.Load<Sprite>("Art/" + name);
            if (s == null)
            {
                // Imported as a plain texture: make the sprite here.
                var t = Resources.Load<Texture2D>("Art/" + name);
                if (t != null) s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0), 100);
            }
            Sprites[name] = s;
            return s;
        }

        /// <summary>A material that can blend a picture toward white (the hit
        /// flash), or null if the shader is missing.</summary>
        public static Material FlashMaterial()
        {
            if (!_flashLoaded)
            {
                _flashLoaded = true;
                _flash = Resources.Load<Shader>("Shaders/UIFlash");
            }
            return _flash != null && _flash.isSupported ? new Material(_flash) : null;
        }

        // ----------------------------------------------------------- sounds

        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        static AudioSource _audio;
        public static bool Muted;

        public static void Play(string name, float volume = 1)
        {
            if (Muted) return;
            AudioClip c;
            if (!Clips.TryGetValue(name, out c))
            {
                c = Resources.Load<AudioClip>("Sfx/" + name);
                Clips[name] = c;
            }
            if (c == null) return;
            if (_audio == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                _audio = go.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
            }
            _audio.PlayOneShot(c, volume);
        }
    }
}
