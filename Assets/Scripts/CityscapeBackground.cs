using UnityEngine;

/// <summary>
/// Procedurally generates a layered neon-cyberpunk city skyline silhouette with
/// lit windows, plus a faint circuit-network panel — the two elements that make
/// the scene read as a "cyber city" (matching the concept art).
///
/// Self-contained: generates its own textures, builds child SpriteRenderers, and
/// applies a gentle time-based drift for parallax life. The project's camera is
/// static, so we drift the layers on a sine curve rather than off camera motion.
/// </summary>
public class CityscapeBackground : MonoBehaviour
{
    [Header("Drift (camera is static, so we animate the layers themselves)")]
    public float farDrift = 0.04f;    // distant skyline — barely moves
    public float nearDrift = 0.10f;   // closer skyline — moves a touch more
    public float driftSpeed = 0.15f;

    private Transform farLayer;
    private Transform nearLayer;
    private Transform circuitLayer;
    private Vector3 farStart, nearStart;

    // Neon palette (matches the game's card colours)
    static readonly Color Cyan    = new Color(0f, 0.88f, 1f);
    static readonly Color Magenta = new Color(1f, 0.08f, 0.58f);

    void Start()
    {
        // ── Far skyline: dim, tall, behind everything ──────────────
        var farTex = GenerateSkyline(512, 160, heightSeed: 7, density: 0.55f,
            buildingColor: new Color(0.02f, 0.03f, 0.07f, 1f), windowDim: 0.35f);
        farLayer = MakeLayer("Skyline_Far", farTex, 512, 160,
            sortingOrder: -16, scale: 14f, yPos: 2.6f, tint: new Color(0.6f, 0.7f, 1f, 0.7f));
        farStart = farLayer.localPosition;

        // ── Near skyline: brighter, shorter, in front of far ───────
        var nearTex = GenerateSkyline(512, 130, heightSeed: 19, density: 0.7f,
            buildingColor: new Color(0.01f, 0.01f, 0.04f, 1f), windowDim: 0.8f);
        nearLayer = MakeLayer("Skyline_Near", nearTex, 512, 130,
            sortingOrder: -15, scale: 13f, yPos: 2.0f, tint: Color.white);
        nearStart = nearLayer.localPosition;

        // ── Circuit-network panel: faint glowing nodes/traces, upper area ──
        var circuitTex = GenerateCircuit(256, 256);
        circuitLayer = MakeLayer("CircuitNetwork", circuitTex, 256, 256,
            sortingOrder: -14, scale: 6f, yPos: 3.0f, tint: new Color(1f, 0.3f, 0.7f, 0.5f));
        circuitLayer.localPosition += new Vector3(3.2f, 0f, 0f); // upper-right like the art
    }

    void Update()
    {
        float t = Time.time * driftSpeed;
        if (farLayer != null)
            farLayer.localPosition = farStart + new Vector3(Mathf.Sin(t) * farDrift, 0f, 0f);
        if (nearLayer != null)
            nearLayer.localPosition = nearStart + new Vector3(Mathf.Sin(t * 1.3f) * nearDrift, 0f, 0f);
        if (circuitLayer != null)
        {
            // gentle pulse on the circuit network
            float pulse = 0.4f + (Mathf.Sin(Time.time * 0.8f) * 0.5f + 0.5f) * 0.3f;
            var sr = circuitLayer.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.3f, 0.7f, pulse);
        }
    }

    Transform MakeLayer(string name, Texture2D tex, int w, int h, int sortingOrder,
        float scale, float yPos, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
        sr.sortingOrder = sortingOrder;
        sr.color = tint;
        go.transform.localScale = new Vector3(scale, scale, 1f) / (w / 100f);
        go.transform.localPosition = new Vector3(0f, yPos, 0f);
        return go.transform;
    }

    /// <summary>
    /// Generates a city skyline silhouette: a row of buildings of varying heights
    /// with lit neon windows (alternating cyan/magenta). Transparent above rooftops.
    /// </summary>
    Texture2D GenerateSkyline(int w, int h, int heightSeed, float density,
        Color buildingColor, float windowDim)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var rng = new System.Random(heightSeed);

        // Pre-compute building edges + heights
        int x = 0;
        var clear = new Color(0, 0, 0, 0);
        // init transparent
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        while (x < w)
        {
            int bw = rng.Next(18, 46);                  // building width
            int bh = (int)(h * Mathf.Lerp(0.35f, 1.0f, (float)rng.NextDouble())); // height
            bool lit = rng.NextDouble() < density;
            Color winColor = (rng.NextDouble() < 0.5) ? Cyan : Magenta;

            for (int bx = 0; bx < bw && x + bx < w; bx++)
            {
                int gx = x + bx;
                for (int by = 0; by < bh; by++)
                {
                    Color c = buildingColor;
                    // rooftop edge highlight (thin neon rim)
                    if (by >= bh - 2)
                        c = Color.Lerp(buildingColor, winColor, 0.6f);
                    // windows: grid of small lit cells
                    else if (lit && bx > 2 && bx % 6 < 3 && by > 4 && by % 8 < 4)
                    {
                        // randomly some windows dark
                        if (rng.NextDouble() < 0.7)
                            c = Color.Lerp(buildingColor, winColor, windowDim);
                    }
                    pixels[by * w + gx] = c;
                }
            }
            x += bw + rng.Next(2, 8); // gap between buildings
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    /// <summary>
    /// Generates a faint circuit-board network: nodes connected by horizontal/vertical
    /// traces, glowing magenta — mirrors the upper-right panel in the concept art.
    /// </summary>
    Texture2D GenerateCircuit(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var rng = new System.Random(101);
        var pixels = new Color[w * h];
        var clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Color trace = new Color(1f, 0.25f, 0.65f, 0.85f);
        Color node  = new Color(1f, 0.5f, 0.85f, 1f);

        // Scatter nodes on a loose grid, connect with L-shaped traces
        int step = 36;
        var nodes = new System.Collections.Generic.List<Vector2Int>();
        for (int gy = step; gy < h - step; gy += step)
            for (int gx = step; gx < w - step; gx += step)
                if (rng.NextDouble() < 0.7)
                    nodes.Add(new Vector2Int(gx + rng.Next(-8, 8), gy + rng.Next(-8, 8)));

        // Draw traces between nearby nodes (L-shaped)
        foreach (var a in nodes)
        {
            foreach (var b in nodes)
            {
                if (a == b) continue;
                int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                if (dist > step + 12) continue;          // only near neighbours
                if (rng.NextDouble() < 0.5) continue;     // sparse
                // horizontal then vertical
                for (int xx = Mathf.Min(a.x, b.x); xx <= Mathf.Max(a.x, b.x); xx++)
                    PlotSafe(pixels, w, h, xx, a.y, trace);
                for (int yy = Mathf.Min(a.y, b.y); yy <= Mathf.Max(a.y, b.y); yy++)
                    PlotSafe(pixels, w, h, b.x, yy, trace);
            }
        }
        // Draw node dots (3x3)
        foreach (var n in nodes)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (dx * dx + dy * dy <= 4)
                        PlotSafe(pixels, w, h, n.x + dx, n.y + dy, node);

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    static void PlotSafe(Color[] px, int w, int h, int x, int y, Color c)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return;
        px[y * w + x] = c;
    }
}
