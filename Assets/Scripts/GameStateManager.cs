using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages game state: Playing, Won, Paused.
/// Triggers win screen with stats when all foundations complete.
/// Wired into BoardManager through its win-check event.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public enum State { Playing, Won, Paused }
    public State CurrentState { get; private set; } = State.Playing;

    [Header("Win Screen")]
    [SerializeField] private GameObject winScreenPrefab; // optional prefab; auto-generates if null
    [SerializeField] private float winDelay = 1.0f; // delay before win screen appears

    [Header("Win Celebration")]
    [SerializeField] private int winParticleCount = 200;
    [SerializeField] private Color[] winColors = {
        new Color(0f, 0.88f, 1f),    // Cyan
        new Color(1f, 0.08f, 0.58f), // Magenta
        new Color(0.6f, 0.2f, 1f),   // Purple
        new Color(0.2f, 1f, 0.2f)    // Green
    };

    private GameObject winScreen;
    private Canvas winCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Wire into BoardManager
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.OnCardMovedToFoundation += CheckWinCondition;
        }
    }

    private void OnDestroy()
    {
        if (BoardManager.Instance != null)
            BoardManager.Instance.OnCardMovedToFoundation -= CheckWinCondition;
    }

    private void CheckWinCondition(CardData card)
    {
        if (CurrentState != State.Playing) return;
        if (BoardManager.Instance.CheckWin())
        {
            StartCoroutine(ShowWinScreenDelayed());
        }
    }

    private IEnumerator ShowWinScreenDelayed()
    {
        CurrentState = State.Won;
        yield return new WaitForSeconds(winDelay);

        // Save high score
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SaveHighScore();

        BuildWinScreen();
    }

    private void BuildWinScreen()
    {
        winScreen = new GameObject("WinScreen");
        winScreen.transform.position = Vector3.zero;

        // Canvas for UI
        winCanvas = winScreen.AddComponent<Canvas>();
        winCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        winScreen.AddComponent<UnityEngine.UI.CanvasScaler>();
        winScreen.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Dark overlay
        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(winScreen.transform, false);
        var overlayImg = overlayGo.AddComponent<UnityEngine.UI.Image>();
        overlayImg.color = new Color(0, 0, 0, 0.75f);
        var overlayRt = overlayImg.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        // ── Win title ────────────────────────────────────────
        var title = CreateWinText("GLITCH\nCOMPLETE", new Vector2(0, 120), 64,
            new Color(0f, 0.88f, 1f), TextAlignmentOptions.Center);

        // ── Score ────────────────────────────────────────────
        int score = ScoreManager.Instance?.GetCurrentScore() ?? 0;
        int highScore = ScoreManager.Instance?.GetHighScore() ?? 0;
        var scoreText = CreateWinText($"SCORE: {score:D6}", new Vector2(0, 30), 36,
            Color.white, TextAlignmentOptions.Center);
        var bestText = CreateWinText($"BEST: {highScore:D6}", new Vector2(0, -10), 22,
            new Color(0.4f, 0.4f, 0.5f), TextAlignmentOptions.Center);

        // ── Stats ────────────────────────────────────────────
        var statsText = CreateWinText(
            score > 0 && score >= highScore ? "★ NEW HIGH SCORE ★" : "",
            new Vector2(0, -50), 18,
            new Color(1f, 0.08f, 0.58f), TextAlignmentOptions.Center);

        // ── Play Again button ────────────────────────────────
        var btnGo = new GameObject("PlayAgainBtn", typeof(UnityEngine.UI.Button));
        btnGo.transform.SetParent(winScreen.transform, false);

        var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.1f, 0.1f, 0.3f, 0.8f);

        var btnRt = btnImg.rectTransform;
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(260, 60);
        btnRt.anchoredPosition = new Vector2(0, -120);

        var btnText = new GameObject("Text", typeof(TextMeshProUGUI));
        btnText.transform.SetParent(btnGo.transform, false);
        var btnTmp = btnText.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "▶  PLAY AGAIN";
        btnTmp.fontSize = 24;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = new Color(0f, 0.88f, 1f);
        btnTmp.fontStyle = FontStyles.Bold;
        var btnTextRt = btnTmp.rectTransform;
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.sizeDelta = Vector2.zero;

        // Wire restart
        btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(RestartGame);

        // ── Win celebration particles ────────────────────────
        var winPsGo = new GameObject("WinParticles");
        winPsGo.transform.SetParent(winScreen.transform, false);
        var winPs = winPsGo.AddComponent<ParticleSystem>();
        var wm = winPs.main;
        wm.loop = true;
        wm.startLifetime = 2.0f;
        wm.startSpeed = 3f;
        wm.startSize = 0.08f;
        wm.startColor = winColors[Random.Range(0, winColors.Length)];
        wm.maxParticles = winParticleCount;
        wm.simulationSpace = ParticleSystemSimulationSpace.World;

        var we = winPs.emission;
        we.rateOverTime = 60f;

        var ws = winPs.shape;
        ws.shapeType = ParticleSystemShapeType.Box;
        ws.scale = new Vector3(12f, 8f, 1f);

        var wr = winPs.GetComponent<ParticleSystemRenderer>();
        wr.material = _Bootstrap.GetSpriteMaterial();
        wr.sortingOrder = 110;

        // Cycle through win colors
        StartCoroutine(CycleWinParticleColors(winPs));

        Debug.Log("[GameState] Win screen displayed.");
    }

    private TextMeshProUGUI CreateWinText(string text, Vector2 position, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject($"WinText_{text.GetHashCode()}", typeof(TextMeshProUGUI));
        go.transform.SetParent(winScreen.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(600, 80);
        return tmp;
    }

    private IEnumerator CycleWinParticleColors(ParticleSystem ps)
    {
        int idx = 0;
        while (CurrentState == State.Won && ps != null)
        {
            var main = ps.main;
            main.startColor = winColors[idx % winColors.Length];
            idx++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void RestartGame()
    {
        CurrentState = State.Playing;
        Destroy(winScreen);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>Called by _Bootstrap to inject shared material reference.</summary>
    public static Material GetSpriteMat()
    {
        return _Bootstrap.GetSpriteMaterial();
    }
}
