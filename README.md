# Push Up Adventure (Unity, IL2CPP)

A pushup adventure game for Android. Every pushup moves you one day down the
road, or lands one blow in a fight. Fifteen worlds, ninety creatures, four
heroes, skills, a lucky wheel, chests and choices. English and Arabic.

The game is built with Unity 2022.3 LTS. The APK uses the **IL2CPP** scripting
backend: the C# code is converted to C++ and compiled to native ARM64 machine
code. GitHub Actions builds it.

## Get the APK

1. Add the three Unity licence secrets (see below). This is a one-time step.
2. Push to `main`, or open **Actions → Android APK (IL2CPP) → Run workflow**.
3. When the run is green, download the **PushUpAdventure-apk** artifact from
   the run page. Unzip it and install `PushUpAdventure.apk` on an Android phone
   (Android 7.0 or newer, 64-bit).

The APK is signed with Unity's debug key. That is fine for installing it on
your own phones. Publishing on Google Play needs your own upload key (see
"Releasing" below).

## Unity licence

The build runs the Unity editor inside GitHub Actions, so it needs a Unity
licence. A free Personal licence works. Add these three secrets in the
repository's **Settings → Secrets and variables → Actions → New repository
secret**:

| Secret | What to put in it |
| --- | --- |
| `UNITY_EMAIL` | The email address of your Unity account |
| `UNITY_PASSWORD` | Your Unity account password (letters and digits only work best) |
| `UNITY_LICENSE` | The whole text of your `Unity_lic.ulf` licence file |

To get the licence file: install Unity Hub, sign in, open **Preferences →
Licenses → Add → Get a free personal license**. The file is then at:

- Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
- macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
- Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

Open it in a text editor and paste all of it into `UNITY_LICENSE`.

Your email and password go only into GitHub's encrypted secrets. Never commit
them, and never paste them into an issue or a chat.

## What the workflow does

1. **Check Unity licence secrets**: stops with a clear message if one is missing.
2. **Edit mode tests**: the game rules (`Assets/Tests/EditMode`), in Unity.
3. **Build APK (IL2CPP, ARM64)**: `PushupAdventure.EditorTools.BuildScript`
   creates the scene, sets IL2CPP, ARM64, Android 7.0+ and portrait, and
   builds `build/Android/PushUpAdventure.apk`.

The version number comes from git tags (game-ci's `Semantic` versioning): tag
a commit `v1.2` and the next builds are `1.2.x`.

## Project layout

| Path | What it holds |
| --- | --- |
| `Assets/Scripts/Core` | The rules in plain C#, with no UnityEngine: battle, run, worlds, skills, loot, translations, Arabic shaping |
| `Assets/Scripts/Game` | The Unity side: the interface, built in code, plus animation and sound |
| `Assets/Editor/BuildScript.cs` | The IL2CPP Android build |
| `Assets/Tests/EditMode` | Tests for the rules |
| `Assets/Resources` | Art, fonts and sounds |
| `tools/` | Scripts that bring the data and assets over from the Flutter version |

There is no scene to edit. `GameApp` starts itself when any scene loads and
builds the whole interface in code.

## Open it in Unity

1. Install Unity **2022.3.62f2** with **Android Build Support** (including the
   Android SDK, NDK and OpenJDK) from Unity Hub.
2. **Open** this folder in Unity Hub.
3. Press Play. Space, Enter or a tap is one pushup.
4. To build on your computer, use the menu **Push Up Adventure → Build Android
   APK (IL2CPP)**. The APK goes to `Builds/Android/`.

## Arabic

Unity's built-in text cannot join Arabic letters or run them right to left.
`ArabicShaper` (in `Assets/Scripts/Core/Loc.cs`) turns each line into joined
letter forms in visual order, and `UText` wraps Arabic lines itself. The
Tajawal and Lalezar fonts include those letter forms.

## Updating from the Flutter version

The worlds, creatures and Arabic text come from the Flutter project:

```bash
python3 tools/port_from_flutter.py /path/to/flutter/project
python3 tools/import_assets.py /path/to/flutter/project
```

## Releasing on Google Play

Play wants an Android App Bundle signed with your own upload key. Create a
keystore, add it as the secrets `ANDROID_KEYSTORE_BASE64`,
`ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` and `ANDROID_KEYALIAS_PASS`,
and set `androidExportType: androidAppBundle` plus the keystore inputs on the
`game-ci/unity-builder` step (see the game-ci documentation).

## Licences

The fonts are under the SIL Open Font License; the licence texts sit next to
them in `Assets/Resources/Fonts`. The art and sounds are the game's own.

---

# مغامرة الضغط (Unity مع IL2CPP)

لعبة مغامرة بتمارين الضغط على أندرويد: كل ضغطة تنقلك يومًا على الطريق أو
تضرب ضربة في القتال. فيها 15 عالمًا و90 مخلوقًا و4 أبطال، بالعربية
والإنجليزية.

اللعبة مبنية بـ Unity 2022.3، والـ APK يستعمل **IL2CPP**: يتحول كود C# إلى
C++ ثم يُترجَم إلى كود آلة أصلي لمعالجات ARM64. البناء يتم على GitHub
Actions.

## كيف تحصل على الـ APK

1. أضف أسرار ترخيص Unity الثلاثة (مرة واحدة فقط).
2. ادفع إلى فرع `main`، أو افتح **Actions → Android APK (IL2CPP) → Run workflow**.
3. عندما ينجح التشغيل، نزّل الملف **PushUpAdventure-apk** من صفحة التشغيل، وفك
   الضغط، وثبّت `PushUpAdventure.apk` على هاتف أندرويد (7.0 أو أحدث).

## ترخيص Unity

في المستودع: **Settings → Secrets and variables → Actions → New repository
secret**، وأضف:

- `UNITY_EMAIL`: بريد حساب Unity.
- `UNITY_PASSWORD`: كلمة مرور حساب Unity (يُفضَّل أن تكون حروفًا وأرقامًا فقط).
- `UNITY_LICENSE`: محتوى ملف `Unity_lic.ulf` كاملًا.

للحصول على الملف: ثبّت Unity Hub وسجّل الدخول، ثم **Preferences → Licenses →
Add → Get a free personal license**. على ويندوز تجده في
`C:\ProgramData\Unity\Unity_lic.ulf`. افتحه بمحرر نصوص والصق محتواه كله في
السر `UNITY_LICENSE`.

ضع بريدك وكلمة مرورك في أسرار GitHub المشفرة فقط، ولا تضعها في الكود أو في
أي محادثة.
