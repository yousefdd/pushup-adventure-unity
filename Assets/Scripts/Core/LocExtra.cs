using System.Collections.Generic;

namespace DiceQuest.Core
{
    /// <summary>Arabic for the dice game's own lines. World and creature names
    /// come from LocAr, generated from the Flutter app.</summary>
    public static partial class Loc
    {
        static readonly Dictionary<string, string> Extra = new Dictionary<string, string>
        {
            // Camp and menus
            { "Dice Quest", "رحلة النرد" },
            { "Roll the die, walk the board, beat the boss.", "ارمِ النرد، تقدّم على اللوحة، واهزم الزعيم." },
            { "Choose your hero", "اختر بطلك" },
            { "Play", "العب" },
            { "Best: world {n}", "الأفضل: العالم {n}" },
            { "Tough: starts with the most HP and armour", "صلب: يبدأ بأكبر صحة ودرع" },
            { "Quick: strikes fast and often critically", "سريع: يضرب بسرعة وكثيرًا ما تكون ضربته حرجة" },
            { "Calm: heals 8% of max HP after every fight", "هادئ: يستعيد 8% من صحته بعد كل قتال" },
            { "Fierce: the hardest hitter, crits deal 2.5×", "شرس: أقوى ضربة، والضربة الحرجة ×2.5" },

            // The board
            { "World {n}", "العالم {n}" },
            { "Lap {n}/{m}", "الجولة {n}/{m}" },
            { "Boss next!", "الزعيم قادم!" },
            { "Roll", "ارمِ النرد" },
            { "Tap to roll", "المس لرمي النرد" },
            { "Rolling…", "النرد يدور…" },
            { "Moving…", "يتقدّم…" },
            { "Fighting…", "قتال…" },
            { "Continue", "متابعة" },
            { "Leave", "مغادرة" },
            { "Buy", "شراء" },
            { "Sold", "بيع" },
            { "Gold", "ذهب" },
            { "Back to camp", "العودة إلى المعسكر" },
            { "Worlds cleared: {n}", "العوالم المحرَّرة: {n}" },
            { "Rolls: {n} · Foes beaten: {k}", "الرميات: {n} · الأعداء المهزومون: {k}" },
            { "Camp: +{g} gold, heal +{h}", "المعسكر: +{g} ذهب، صحة +{h}" },
            { "The boss of {world} blocks the camp!", "زعيم {world} يسدّ طريق المعسكر!" },
            { "Elite", "نخبة" },
            { "BOSS", "زعيم" },

            // Tiles
            { "Camp", "المعسكر" },
            { "You rest by the fire. Heal +{n}.", "تستريح عند النار. صحة +{n}." },
            { "The {foe} was too strong.", "{foe} كان أقوى منك." },
            { "{foe} is beaten. +{gold} gold.", "هُزم {foe}. +{gold} ذهب." },
            { "Level up! Level {n}.", "ارتفع المستوى! المستوى {n}." },
            { "Boss defeated!", "هُزم الزعيم!" },
            { "Elite defeated!", "هُزم عدو النخبة!" },
            { "Legend!", "أسطورة!" },
            { "Every world is free. Your hero is a legend.", "تحرّرت كل العوالم. بطلك صار أسطورة." },
            { "On to {world}. Heal +{n}.", "إلى {world}. صحة +{n}." },
            { "Blessing", "بركة" },
            { "A shrine sharpens your blade. ATK +2.", "مزار يشحذ سيفك. الهجوم +2." },
            { "Old runes harden your skin. DEF +1.", "رموز قديمة تقوّي جلدك. الدفاع +1." },
            { "A spring fills you with life. Max HP +15.", "نبع يملؤك بالحياة. أقصى صحة +15." },
            { "A healer patches you up. Heal +{n}.", "معالج يضمّد جراحك. صحة +{n}." },
            { "You find a lost purse. +{n} gold.", "تجد كيسًا ضائعًا. +{n} ذهب." },
            { "Curse", "لعنة" },
            { "A hidden trap snaps shut. HP −{n}.", "فخ مخفي ينغلق عليك. الصحة −{n}." },
            { "A thief cuts your purse. Gold −{n}.", "لص يسرق كيسك. الذهب −{n}." },
            { "Rust creeps over your blade. ATK −1.", "الصدأ يأكل سيفك. الهجوم −1." },
            { "Inside: a fine sword. ATK +{n}, gold +{g}.", "في الداخل: سيف ممتاز. الهجوم +{n}، الذهب +{g}." },
            { "Inside: sturdy armour. Max HP +{n}, gold +{g}.", "في الداخل: درع متين. أقصى صحة +{n}، الذهب +{g}." },
            { "Inside: a strong helm. DEF +{n}, gold +{g}.", "في الداخل: خوذة قوية. الدفاع +{n}، الذهب +{g}." },
            { "Lucky roll", "رمية الحظ" },
            { "Six! The spirits bless you. ATK +3.", "ستة! الأرواح تباركك. الهجوم +3." },
            { "Five! A pile of coins. +{n} gold.", "خمسة! كومة من العملات. +{n} ذهب." },
            { "Four! A warm breeze. Heal +{n}.", "أربعة! نسمة دافئة. صحة +{n}." },
            { "Three. Nothing happens.", "ثلاثة. لا شيء يحدث." },
            { "Two. You drop some coins. Gold −{n}.", "اثنان. تسقط منك بعض العملات. الذهب −{n}." },
            { "One! You stub your toe. HP −{n}.", "واحد! تتعثر بحجر. الصحة −{n}." },

            // Shop
            { "Shop", "المتجر" },
            { "A travelling merchant waves you over.", "تاجر متجوّل يلوّح لك." },
            { "Potion", "جرعة" },
            { "Heal 40%", "صحة 40%" },
            { "Elixir", "إكسير" },
            { "Heal to full", "صحة كاملة" },
            { "Sharp blade", "نصل حاد" },
            { "ATK +{n}", "الهجوم +{n}" },
            { "Iron shield", "درع حديدي" },
            { "DEF +{n}", "الدفاع +{n}" },
            { "Chain mail", "زرد" },
            { "Max HP +{n}", "أقصى صحة +{n}" },
            { "Lucky charm", "تميمة الحظ" },
            { "Crit chance +6%", "فرصة الضربة الحرجة +6%" },
        };

        static Loc()
        {
            foreach (var kv in Extra) Ar[kv.Key] = kv.Value;
        }
    }
}
