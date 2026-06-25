using System.Collections;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance;

    [Header("Net-Scan Settings")]
    [SerializeField] private float scanDuration = 3.0f;
    [SerializeField] private int maxUsesPerGame = 1;

    private int usesRemaining;

    public delegate void NetScanAction(float duration);
    public static event NetScanAction OnNetScanTriggered;

    public bool CanUse => usesRemaining > 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResetUses();
    }

    public void ResetUses()
    {
        usesRemaining = maxUsesPerGame;
    }

    public void TriggerNetScan()
    {
        if (usesRemaining <= 0)
        {
            Debug.Log("Net-Scan already depleted for this run.");
            return;
        }

        usesRemaining--;
        Debug.Log($"Initiating Net-Scan overlay... ({usesRemaining} use(s) remaining)");
        OnNetScanTriggered?.Invoke(scanDuration);
    }

    public int GetUsesRemaining() => usesRemaining;
    public int GetMaxUses() => maxUsesPerGame;
}
