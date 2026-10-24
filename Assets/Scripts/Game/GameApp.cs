using DiceQuest.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceQuest.Game
{
    /// <summary>Starts the game in any scene (the build's scene is empty): a
    /// camera, one canvas, the event system, and the home or run screen.</summary>
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp I { get; private set; }

        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _safe, _column;
        MonoBehaviour _screen;
        Rect _lastSafe;
        Vector2Int _lastSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("Dice Quest");
            DontDestroyOnLoad(go);
            go.AddComponent<GameApp>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Prefs.Load();

            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.SetParent(transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Bg;
            cam.orthographic = true;
            cam.cullingMask = 0;
            cam.gameObject.AddComponent<AudioListener>();

            var cgo = new GameObject("Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;
            _scaler = cgo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(Theme.Width, 960);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.referencePixelsPerUnit = 100;
            cgo.AddComponent<GraphicRaycaster>();

            var bg = Ui.Img(cgo.transform, "Background", Theme.Bg);
            bg.rectTransform.Fill();
            _safe = Ui.Rect(cgo.transform, "SafeArea");
            _column = Ui.Rect(_safe, "Column");

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            Fit();
            ShowHome();
        }

        /// <summary>Phones: the full width is 540 units. Wider screens (a
        /// tablet on its side, the editor): a 540-unit column in the middle.</summary>
        void Fit()
        {
            _lastSafe = Screen.safeArea;
            _lastSize = new Vector2Int(Screen.width, Screen.height);
            var aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            _scaler.matchWidthOrHeight = aspect > Theme.Width / 960f ? 1 : 0;

            var s = Screen.safeArea;
            _safe.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            _safe.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;

            _column.anchorMin = new Vector2(0.5f, 0);
            _column.anchorMax = new Vector2(0.5f, 1);
            _column.pivot = new Vector2(0.5f, 0.5f);
            _column.offsetMin = new Vector2(-Theme.Width / 2, 0);
            _column.offsetMax = new Vector2(Theme.Width / 2, 0);
        }

        void Update()
        {
            if (Screen.safeArea != _lastSafe || Screen.width != _lastSize.x || Screen.height != _lastSize.y) Fit();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_screen is BoardScreen) ShowHome();
                else Application.Quit();
            }
        }

        void Swap<T>(System.Action<T> init) where T : MonoBehaviour
        {
            if (_screen != null) Destroy(_screen.gameObject);
            var rt = Ui.Rect(_column, typeof(T).Name).Fill();
            var s = rt.gameObject.AddComponent<T>();
            _screen = s;
            init(s);
        }

        public void ShowHome() => Swap<HomeScreen>(h => h.Build());

        public void StartRun(string heroId) => Swap<BoardScreen>(r => r.Build(heroId));

        /// <summary>Switches language and rebuilds the screen that is showing.</summary>
        public void ToggleLanguage()
        {
            Loc.Arabic = !Loc.Arabic;
            Prefs.Save();
            if (_screen is HomeScreen) ShowHome();
        }
    }

    /// <summary>What the game remembers between sessions.</summary>
    public static class Prefs
    {
        public static string Hero = "knight";

        public static void Load()
        {
            Loc.Arabic = PlayerPrefs.GetInt("arabic", Application.systemLanguage == SystemLanguage.Arabic ? 1 : 0) == 1;
            Theme.Muted = PlayerPrefs.GetInt("muted", 0) == 1;
            Hero = PlayerPrefs.GetString("hero", "knight");
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("arabic", Loc.Arabic ? 1 : 0);
            PlayerPrefs.SetInt("muted", Theme.Muted ? 1 : 0);
            PlayerPrefs.SetString("hero", Hero);
            PlayerPrefs.Save();
        }

        /// <summary>The furthest world reached (1-based), over all runs.</summary>
        public static int BestWorld
        {
            get => PlayerPrefs.GetInt("best_world", 0);
            set
            {
                if (value <= BestWorld) return;
                PlayerPrefs.SetInt("best_world", value);
                PlayerPrefs.Save();
            }
        }
    }
}
