using System.Collections.Generic;

namespace PushupAdventure.Core
{
    /// <summary>Arabic for the few lines only the Unity version shows. The rest
    /// comes from LocAr, generated from the Flutter app.</summary>
    public static partial class Loc
    {
        static readonly Dictionary<string, string> Extra = new Dictionary<string, string>
        {
            { "Push Up Adventure", "مغامرة الضغط" },
            { "Choose your hero", "اختر بطلك" },
            { "Each pushup is a day on the road, or a blow in a fight.", "كل ضغطة يوم على الطريق، أو ضربة في القتال." },
            { "Leave", "مغادرة" },
            { "Upcoming", "القادم" },
        };

        static Loc()
        {
            foreach (var kv in Extra) Ar[kv.Key] = kv.Value;
        }
    }
}
