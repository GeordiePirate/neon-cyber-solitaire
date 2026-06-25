using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private float lifeTime = 0.8f;

    [Header("References")]
    [SerializeField] private TextMeshPro textMesh;

    private Vector3 randomDrift;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(string message, Color neonColor)
    {
        if (textMesh != null)
        {
            textMesh.text = message;
            textMesh.color = neonColor;
            textMesh.fontSize = message.Contains("\n") ? 3.5f : 5f;
        }

        // Random horizontal drift for organic feel
        randomDrift = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Move upward with slight drift
        transform.Translate((Vector3.up + randomDrift) * moveSpeed * Time.deltaTime);

        // Fade out
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * Time.deltaTime);
            textMesh.color = c;
        }

        // Scale up slightly as it rises
        float t = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
        transform.localScale = Vector3.one * t;
    }
}
