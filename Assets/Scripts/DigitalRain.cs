using UnityEngine;
using TMPro;

/// <summary>
/// Renders a Matrix-style digital rain effect in the background.
/// Creates falling columns of katakana/binary characters with neon glow.
/// Enhanced with more columns, wider spread, and better color.
/// </summary>
public class DigitalRain : MonoBehaviour
{
    [Header("Rain Settings")]
    public int columnCount = 30;
    public float fallSpeed = 1.0f;
    public float spawnRate = 0.8f;
    public Color rainColor = new Color(0f, 0.8f, 0.4f, 0.35f);
    public Color headColor = new Color(0.6f, 1f, 0.8f, 0.7f);

    private struct RainColumn
    {
        public float x;
        public float y;
        public float length;
        public float speed;
        public float offset;
    }

    private RainColumn[] columns;
    private float[] progress;
    private float time;
    private TextMeshPro[,] chars;

    // Binary + hex glyphs — LiberationSans (Unity's default SDF font) has no katakana,
    // so katakana rendered as □ boxes. Binary/hex matches the concept-art "binary rain" anyway.
    private readonly string charset = "0101010101ABCDEF0123456789";

    void Start()
    {
        columns = new RainColumn[columnCount];
        progress = new float[columnCount];
        int maxLen = 14;

        chars = new TextMeshPro[columnCount, maxLen];

        for (int i = 0; i < columnCount; i++)
        {
            columns[i] = new RainColumn
            {
                x = Random.Range(-5f, 5f),
                y = Random.Range(-2.5f, 3f),
                length = Random.Range(4, maxLen + 1),
                speed = Random.Range(0.5f, 1.5f) * fallSpeed,
                offset = Random.Range(0f, 10f)
            };
            progress[i] = -columns[i].length;

            for (int j = 0; j < maxLen; j++)
            {
                var go = new GameObject($"Char_{i}_{j}", typeof(TextMeshPro));
                go.transform.SetParent(transform, false);
                var tmp = go.GetComponent<TextMeshPro>();
                tmp.text = charset[Random.Range(0, charset.Length)].ToString();
                tmp.fontSize = 0.3f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.clear;
                tmp.GetComponent<Renderer>().sortingOrder = -6;
                chars[i, j] = tmp;
            }
        }
    }

    void Update()
    {
        time += Time.deltaTime;

        for (int i = 0; i < columnCount; i++)
        {
            progress[i] += Time.deltaTime * columns[i].speed * spawnRate;

            for (int j = 0; j < columns[i].length; j++)
            {
                float charY = columns[i].y - progress[i] + j * 0.3f;
                float wrapY = Mathf.Repeat(charY + 3f, 6f) - 3f;

                var tmp = chars[i, j];
                if (wrapY < -2.5f || wrapY > 2.5f)
                {
                    tmp.color = Color.clear;
                    continue;
                }

                tmp.transform.localPosition = new Vector3(columns[i].x, wrapY, 0.5f);

                // Random character change
                if (Random.value < 0.01f)
                    tmp.text = charset[Random.Range(0, charset.Length)].ToString();

                // Color: head is bright, tail fades
                float t = (float)j / columns[i].length;
                if (t < 0.1f)
                {
                    // Head character — brighter
                    tmp.color = headColor;
                }
                else if (t < 0.3f)
                {
                    tmp.color = Color.Lerp(headColor, rainColor, (t - 0.1f) / 0.2f);
                }
                else
                {
                    float fade = 1f - ((t - 0.3f) / 0.7f);
                    tmp.color = new Color(rainColor.r, rainColor.g, rainColor.b, rainColor.a * fade);
                }
            }

            // Reset when fully scrolled through
            if (progress[i] > columns[i].length + 4f)
            {
                progress[i] = -columns[i].length;
                columns[i].x = Random.Range(-5f, 5f);
                columns[i].length = Random.Range(4, 15);
                columns[i].speed = Random.Range(0.5f, 1.5f) * fallSpeed;
            }
        }
    }
}
