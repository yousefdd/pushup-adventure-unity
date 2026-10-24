# Dice Quest (Unity, IL2CPP)

A board roguelike for Android. Tap to roll the die; your hero hops around a
ring of 24 tiles and every tile is an event: a creature to fight (the fight
plays itself), an elite, a blessing, a curse, a chest, a shop or a lucky
roll. After two laps the world's boss waits at camp. Beat it and the next of
fifteen worlds opens, with stronger creatures. Four heroes, English and Arabic.

The pushup adventure that used to live here is kept on the `pushup-game`
branch.

The game is built with Unity 2022.3 LTS. The APK uses the **IL2CPP** scripting
backend: the C# code is converted to C++ and compiled to native ARM64 machine
code. GitHub Actions builds it.

## Get the APK

1. Add the three Unity licence secrets (see below). This is a one-time step.
2. Push to `main`, or open **Actions → Android APK (IL2CPP) → Run workflow**.
3. When the run is green, download the **DiceQuest-apk** artifact from
   the run page. Unzip it and install `DiceQuest.apk` on an Android phone
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
3. **Build APK (IL2CPP, ARM64)**: `DiceQuest.EditorTools.BuildScript`
   creates the scene, sets IL2CPP, ARM64, Android 7.0+ and portrait, and
   builds `build/Android/DiceQuest.apk`.

The version number comes from git tags (game-ci's `Semantic` versioning): tag
a commit `v1.2` and the next builds are `1.2.x`.

## Project layout

| Path | What it holds |
| --- | --- |
| `Assets/Scripts/Core` | The rules in plain C#, with no UnityEngine: board, dice, fights, shop, worlds, translations, Arabic shaping |
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
3. Press Play. Tap (or press Space) to roll.
4. To build on your computer, use the menu **Dice Quest → Build Android
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

# رحلة النرد (Unity مع IL2CPP)

لعبة لوحة ونرد لأندرويد: المس الشاشة لرمي النرد، فيتقدم بطلك على 24 مربعًا،
وكل مربع حدث: وحش تقاتله (القتال تلقائي)، أو عدو نخبة، أو بركة، أو لعنة، أو
صندوق كنز، أو متجر، أو رمية حظ. بعد جولتين ينتظرك زعيم العالم عند المعسكر؛
اهزمه لينفتح العالم التالي من 15 عالمًا. أربعة أبطال، بالعربية والإنجليزية.

لعبة الضغط السابقة محفوظة في الفرع `pushup-game`.

يتحول كود C# عبر **IL2CPP** إلى C++ ثم إلى كود آلة ARM64، والبناء على GitHub Actions.
