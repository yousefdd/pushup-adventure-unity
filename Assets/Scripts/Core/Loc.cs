using System;
using System.Text;

namespace PushupAdventure.Core
{
    /// <summary>English is the key; Arabic comes from LocAr (generated from the
    /// Flutter app). {name} slots are filled after the lookup.</summary>
    public static partial class Loc
    {
        public static bool Arabic;

        /// <summary>Tr("Day {n}", "n", 3): name/value pairs fill the slots.</summary>
        public static string Tr(string en, params object[] args)
        {
            string s;
            if (!Arabic || !Ar.TryGetValue(en, out s)) s = en;
            for (var i = 0; i + 1 < args.Length; i += 2)
                s = s.Replace("{" + args[i] + "}", Convert.ToString(args[i + 1]));
            return s;
        }

        public static bool Has(string en) => Ar.ContainsKey(en);

        /// <summary>A foe's name: elites carry a ★ after the name in both languages.</summary>
        public static string TrName(string name) =>
            name.EndsWith(" ★") ? Tr(name.Substring(0, name.Length - 2)) + " ★" : Tr(name);

        static readonly string[] OrdinalsAr =
        {
            "الأول", "الثاني", "الثالث", "الرابع", "الخامس", "السادس", "السابع", "الثامن", "التاسع", "العاشر",
            "الحادي عشر", "الثاني عشر", "الثالث عشر", "الرابع عشر", "الخامس عشر",
        };

        /// <summary>"Chapter IV" → "الفصل الرابع".</summary>
        public static string Chapter(int number, string roman) =>
            Arabic && number >= 1 && number <= OrdinalsAr.Length ? "الفصل " + OrdinalsAr[number - 1] : roman;

        /// <summary>"1 day", "5 days"; in Arabic the noun follows the number's rules.</summary>
        public static string Days(int n)
        {
            if (!Arabic) return n == 1 ? "1 day" : n + " days";
            if (n == 1) return "يوم واحد";
            if (n == 2) return "يومان";
            var r = n % 100;
            if (r >= 3 && r <= 10) return n + " أيام";
            if (r >= 11) return n + " يومًا";
            return n + " يوم";
        }
    }

    /// <summary>Arabic for a text engine without shaping (Unity's legacy UI):
    /// letters become their joined presentation forms, then each line is put
    /// in visual order (right to left, with Latin and numbers kept as they are).</summary>
    public static class ArabicShaper
    {
        // isolated, final, initial, medial (0 = no such form: the letter joins only to the right)
        static readonly int[,] Forms =
        {
            { 0x0621, 0xFE80, 0, 0, 0 }, { 0x0622, 0xFE81, 0xFE82, 0, 0 }, { 0x0623, 0xFE83, 0xFE84, 0, 0 },
            { 0x0624, 0xFE85, 0xFE86, 0, 0 }, { 0x0625, 0xFE87, 0xFE88, 0, 0 }, { 0x0626, 0xFE89, 0xFE8A, 0xFE8B, 0xFE8C },
            { 0x0627, 0xFE8D, 0xFE8E, 0, 0 }, { 0x0628, 0xFE8F, 0xFE90, 0xFE91, 0xFE92 }, { 0x0629, 0xFE93, 0xFE94, 0, 0 },
            { 0x062A, 0xFE95, 0xFE96, 0xFE97, 0xFE98 }, { 0x062B, 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C },
            { 0x062C, 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0 }, { 0x062D, 0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4 },
            { 0x062E, 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8 }, { 0x062F, 0xFEA9, 0xFEAA, 0, 0 }, { 0x0630, 0xFEAB, 0xFEAC, 0, 0 },
            { 0x0631, 0xFEAD, 0xFEAE, 0, 0 }, { 0x0632, 0xFEAF, 0xFEB0, 0, 0 }, { 0x0633, 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4 },
            { 0x0634, 0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8 }, { 0x0635, 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC },
            { 0x0636, 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0 }, { 0x0637, 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4 },
            { 0x0638, 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8 }, { 0x0639, 0xFEC9, 0xFECA, 0xFECB, 0xFECC },
            { 0x063A, 0xFECD, 0xFECE, 0xFECF, 0xFED0 }, { 0x0641, 0xFED1, 0xFED2, 0xFED3, 0xFED4 },
            { 0x0642, 0xFED5, 0xFED6, 0xFED7, 0xFED8 }, { 0x0643, 0xFED9, 0xFEDA, 0xFEDB, 0xFEDC },
            { 0x0644, 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0 }, { 0x0645, 0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4 },
            { 0x0646, 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8 }, { 0x0647, 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC },
            { 0x0648, 0xFEED, 0xFEEE, 0, 0 }, { 0x0649, 0xFEEF, 0xFEF0, 0, 0 }, { 0x064A, 0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4 },
        };

        const char Tatweel = 'ـ';

        static int Row(char c)
        {
            for (var i = 0; i < Forms.GetLength(0); i++)
                if (Forms[i, 0] == c) return i;
            return -1;
        }

        public static bool IsArabicLetter(char c) => Row(c) >= 0 || c == Tatweel;

        /// <summary>Harakat and other marks: dropped (the legacy UI cannot place them).</summary>
        static bool IsMark(char c) => (c >= 'ً' && c <= 'ٟ') || c == 'ٰ';

        static bool JoinsLeft(char c)
        {
            if (c == Tatweel) return true;
            var r = Row(c);
            return r >= 0 && Forms[r, 3] != 0;
        }

        public static bool HasArabic(string s)
        {
            foreach (var c in s)
                if ((c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﻿')) return true;
            return false;
        }

        /// <summary>Keyword start and end marks: invisible to joining, so a
        /// highlighted word still joins its neighbours.</summary>
        public const char KeyOpen = '\uE000', KeyClose = '\uE001';

        static bool Transparent(char c) => c == KeyOpen || c == KeyClose;

        static char Before(string t, int i)
        {
            for (var j = i - 1; j >= 0; j--)
                if (!Transparent(t[j])) return t[j];
            return ' ';
        }

        static char After(string t, int i)
        {
            for (var j = i + 1; j < t.Length; j++)
                if (!Transparent(t[j])) return t[j];
            return ' ';
        }

        /// <summary>Joined presentation forms, still in logical (reading) order.</summary>
        public static string Shape(string s)
        {
            var src = new StringBuilder();
            foreach (var c in s)
                if (!IsMark(c)) src.Append(c);
            var t = src.ToString();
            var o = new StringBuilder(t.Length);
            for (var i = 0; i < t.Length; i++)
            {
                var c = t[i];
                var r = Row(c);
                if (r < 0)
                {
                    o.Append(c);
                    continue;
                }
                var prevJoins = JoinsLeft(Before(t, i));
                // Lam + alef: one ligature.
                if (c == 'ل' && i + 1 < t.Length && "آأإا".IndexOf(t[i + 1]) >= 0)
                {
                    var lig = t[i + 1] == 'آ' ? 0xFEF5 : t[i + 1] == 'أ' ? 0xFEF7 : t[i + 1] == 'إ' ? 0xFEF9 : 0xFEFB;
                    o.Append((char)(prevJoins ? lig + 1 : lig));
                    i++;
                    continue;
                }
                var nextJoins = JoinsLeft(c) && Row(After(t, i)) >= 0;
                int form;
                if (prevJoins && nextJoins) form = Forms[r, 4];
                else if (prevJoins) form = Forms[r, 2];
                else if (nextJoins) form = Forms[r, 3];
                else form = Forms[r, 1];
                o.Append((char)(form != 0 ? form : Forms[r, 1]));
            }
            return o.ToString();
        }

        static bool IsLtr(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

        static char Mirror(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '«': return '»';
                case '»': return '«';
                case '“': return '”';
                case '”': return '“';
                default: return c;
            }
        }

        static bool RunChar(char c) => IsLtr(c) || "%+-−.,:/×".IndexOf(c) >= 0;

        /// <summary>One shaped line in visual order: right to left, but runs of
        /// Latin letters and numbers (with signs and % around them) stay as written.</summary>
        public static string Visual(string line)
        {
            // Split into segments in reading order: left-to-right runs (holding at
            // least one letter or digit) and everything else.
            var segs = new System.Collections.Generic.List<(string text, bool ltr)>();
            var j = 0;
            while (j < line.Length)
            {
                var k = j;
                if (RunChar(line[j]))
                {
                    while (k < line.Length && RunChar(line[k])) k++;
                    var run = line.Substring(j, k - j);
                    var hasLetter = false;
                    foreach (var c in run) hasLetter |= IsLtr(c);
                    segs.Add((run, hasLetter));
                }
                else
                {
                    while (k < line.Length && !RunChar(line[k])) k++;
                    segs.Add((line.Substring(j, k - j), false));
                }
                j = k;
            }
            var o = new StringBuilder(line.Length);
            for (var n = segs.Count - 1; n >= 0; n--)
            {
                if (segs[n].ltr)
                {
                    o.Append(segs[n].text);
                    continue;
                }
                for (var m = segs[n].text.Length - 1; m >= 0; m--) o.Append(Mirror(segs[n].text[m]));
            }
            return o.ToString();
        }

        /// <summary>Shape + visual order for a single line.</summary>
        public static string Line(string logical) => HasArabic(logical) ? Visual(Shape(logical)) : logical;
    }
}
