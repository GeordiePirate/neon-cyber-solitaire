using System.Collections;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Scoring Values")]
    [SerializeField] private int foundationPoints = 10;
    [SerializeField] private int tableauPoints = 5;
    [SerializeField] private int revealPoints = 15;

    [Header("Streak Settings")]
    [SerializeField] private float streakWindow = 3.0f;
    [SerializeField] private float maxMultiplier = 4.0f;

    private int currentScore = 0;
    private float currentMultiplier = 1.0f;
    private float streakTimer = 0f;
    private bool isStreakActive = false;
    private int highScore = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateUIVisuals();
    }

    private void Update()
    {
        if (isStreakActive)
        {
            streakTimer -= Time.deltaTime;
            if (streakTimer <= 0)
            {
                ResetStreak();
            }
        }
    }

    public void AddPoints(int basePoints)
    {
        int finalPoints = Mathf.RoundToInt(basePoints * currentMultiplier);
        currentScore += finalPoints;
        BuildStreak();
        UpdateUIVisuals();

        // Spawn floating text popup
        if (floatingTextPrefab != null)
        {
            string message = $"+{finalPoints}";
            if (currentMultiplier > 1.0f)
                message = $"+{finalPoints} (x{currentMultiplier:F1})";

            string comboMessage = currentMultiplier switch
            {
                >= 4.0f => "GLITCH STREAK!",
                >= 3.0f => "MAX COMBO!",
                >= 2.0f => "CYBER STREAK!",
                >= 1.5f => "COMBO!",
                _ => ""
            };

            if (!string.IsNullOrEmpty(comboMessage))
                message = comboMessage + "\n" + message;

            SpawnFloatingText(message, GetComboColor());
        }
    }

    public void AddRevealPoints()
    {
        AddPoints(revealPoints);
    }

    private void BuildStreak()
    {
        streakTimer = streakWindow;
        isStreakActive = true;
        if (currentMultiplier < maxMultiplier)
            currentMultiplier += 0.5f;
    }

    private void ResetStreak()
    {
        isStreakActive = false;
        currentMultiplier = 1.0f;
        UpdateUIVisuals();
    }

    private void UpdateUIVisuals()
    {
        if (scoreText != null)
            scoreText.text = $"{currentScore:D6}";

        if (multiplierText != null)
        {
            if (currentMultiplier > 1.0f)
            {
                multiplierText.text = $"x{currentMultiplier:F1}";
                multiplierText.enabled = true;
            }
            else
            {
                multiplierText.enabled = false;
            }
        }
    }

    private Color GetComboColor()
    {
        return currentMultiplier switch
        {
            >= 4.0f => new Color(1f, 0.2f, 0.6f), // Hot pink
            >= 3.0f => new Color(1f, 0.5f, 0f),    // Orange
            >= 2.0f => new Color(0.2f, 1f, 0.8f),  // Cyan
            >= 1.5f => new Color(0.6f, 0.2f, 1f),  // Purple
            _ => Color.white
        };
    }

    private void SpawnFloatingText(string message, Color color)
    {
        if (floatingTextPrefab == null) return;
        var go = Instantiate(floatingTextPrefab, Vector3.zero, Quaternion.identity);
        var ft = go.GetComponent<FloatingText>();
        if (ft != null)
            ft.Setup(message, color);
    }

    public void SaveHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    public int GetHighScore() => highScore;
    public int GetCurrentScore() => currentScore;
    public float GetCurrentMultiplier() => currentMultiplier;
}
