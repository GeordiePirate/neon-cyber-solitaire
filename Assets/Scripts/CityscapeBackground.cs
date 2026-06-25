using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Layered, animated cyberpunk city background built from real generated art.
/// Loads PNG layers from Assets/Sprites/Background at runtime (Editor Play mode)
/// and animates them: parallax drift on the skyline, slow-crossing ships, and a
/// pulsing tint. Camera is static in this project, so all motion is time-based.
///
/// Layers (back -> front):
///   sky.png      (opaque gradient, backmost, slowest drift)
///   skyline.png  (keyed neon buildings, faster drift)
///   ships        (spawned from ship.png, cross the sky)
/// Digital rain + circuit grid remain separate procedural components.
/// </summary>
public class CityscapeBackground : MonoBehaviour
{
    [Header("Parallax drift (static camera -> time-based sine)")]
    public float skyDrift = 0.05f;
    public float skylineDrift = 0.18f;
    public float driftSpeed = 0.12f;

    [Header("Ships")]
    public float shipInterval = 7f;   // seconds between ship spawns
    public float shipSpeed = 0.7f;     // world units / sec

    private Transform skyT, skylineT;
    private Vector3 skyStart, skylineStart;
    private Sprite shipSprite;
    private float shipTimer;
    private readonly List<ShipMover> ships = new List<ShipMover>();

    // Where the art lives (relative to project root). Editor Play mode reads these directly.
    static string BgDir => Path.Combine(Application.dataPath, "Sprites", "Background");

    void Start()
    {
        // ── Backmost sky (opaque gradient) ─────────────────────────
        var skySprite = LoadSprite("sky.png");
        if (skySprite != null)
        {
            skyT = MakeLayer("Sky", skySprite, sortingOrder: -20, worldWidth: 16f, yPos: 1.5f,
                tint: Color.white);
            skyStart = skyT.localPosition;
        }

        // ── Skyline (keyed neon buildings, more detailed art) ──────
        var skylineSprite = LoadSprite("skyline.png");
        if (skylineSprite != null)
        {
            // Pivot at bottom-center so it anchors to the horizon, not floating
            skylineT = MakeLayer("Skyline", skylineSprite, sortingOrder: -17, worldWidth: 16f, yPos: -3.4f,
                tint: Color.white, pivotY: 0f);
            skylineStart = skylineT.localPosition;
        }

        // ── Circuit network panel (procedural, upper-right) ────────
        BuildCircuitPanel();

        // ── Ships ──────────────────────────────────────────────────
        shipSprite = LoadSprite("ship.png");
        shipTimer = 1.5f; // first ship soon after start
    }

    void Update()
    {
        float t = Time.time * driftSpeed;
        if (skyT != null)
            skyT.localPosition = skyStart + new Vector3(Mathf.Sin(t) * skyDrift, 0f, 0f);
        if (skylineT != null)
            skylineT.localPosition = skylineStart + new Vector3(Mathf.Sin(t * 1.3f) * skylineDrift, 0f, 0f);

        // Spawn ships periodically
        if (shipSprite != null)
        {
            shipTimer -= Time.deltaTime;
            if (shipTimer <= 0f)
            {
                shipTimer = shipInterval + Random.Range(-2f, 3f);
                SpawnShip();
            }
        }

        // Move + cull ships
        for (int i = ships.Count - 1; i >= 0; i--)
        {
            if (ships[i] == null || ships[i].Done) { if (ships[i] != null) Destroy(ships[i].gameObject); ships.RemoveAt(i); }
        }
    }

    void SpawnShip()
    {
        bool leftToRight = Random.value < 0.5f;
        float y = Random.Range(2.0f, 4.2f);
        var go = new GameObject("Ship");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = shipSprite;
        sr.sortingOrder = -16;
        sr.color = new Color(1f, 1f, 1f, 0.85f);
        float scale = Random.Range(0.4f, 0.7f);
        // Ship art faces RIGHT by default. Face the direction of travel:
        // moving right -> keep positive; moving left -> flip negative.
        go.transform.localScale = new Vector3(leftToRight ? scale : -scale, scale, 1f);
        float startX = leftToRight ? -10f : 10f;
        go.transform.localPosition = new Vector3(startX, y, 0.5f);
        var mover = go.AddComponent<ShipMover>();
        mover.speed = (leftToRight ? 1f : -1f) * shipSpeed * Random.Range(0.8f, 1.3f);
        mover.limit = 10.5f;
        ships.Add(mover);
    }

    Transform MakeLayer(string name, Sprite sprite, int sortingOrder, float worldWidth, float yPos, Color tint, float pivotY = 0.5f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        // Rebuild the sprite with the requested pivot (default center, 0 = bottom)
        var tex = sprite.texture;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, pivotY), 100f);
        sr.sortingOrder = sortingOrder;
        sr.color = tint;
        // Scale so the sprite spans `worldWidth` units horizontally
        float spriteWorldW = sr.sprite.bounds.size.x; // in units at scale 1
        float s = worldWidth / spriteWorldW;
        go.transform.localScale = new Vector3(s, s, 1f);
        go.transform.localPosition = new Vector3(0f, yPos, 0f);
        return go.transform;
    }

    Sprite LoadSprite(string fileName)
    {
        try
        {
            string path = Path.Combine(BgDir, fileName);
            if (!File.Exists(path)) { Debug.LogWarning($"[Cityscape] missing {path}"); return null; }
            byte[] data = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(data);                 // resizes automatically
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Cityscape] failed to load {fileName}: {e.Message}");
            return null;
        }
    }

    // ── Procedural circuit-network panel (upper-right, pulsing) ────
    void BuildCircuitPanel()
    {
        var tex = GenerateCircuit(256, 256);
        var go = new GameObject("CircuitNetwork");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f);
        sr.sortingOrder = -14;
        sr.color = new Color(1f, 0.3f, 0.7f, 0.5f);
        go.transform.localScale = new Vector3(6f, 6f, 1f) / (256f / 100f);
        go.transform.localPosition = new Vector3(3.4f, 3.2f, 0f);
        go.AddComponent<CircuitPulse>().renderer2D = sr;
    }

    Texture2D GenerateCircuit(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var rng = new System.Random(101);
        var pixels = new Color[w * h];
        var clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Color trace = new Color(1f, 0.25f, 0.65f, 0.85f);
        Color node = new Color(1f, 0.5f, 0.85f, 1f);

        int step = 36;
        var nodes = new List<Vector2Int>();
        for (int gy = step; gy < h - step; gy += step)
            for (int gx = step; gx < w - step; gx += step)
                if (rng.NextDouble() < 0.7)
                    nodes.Add(new Vector2Int(gx + rng.Next(-8, 8), gy + rng.Next(-8, 8)));

        foreach (var a in nodes)
            foreach (var b in nodes)
            {
                if (a.Equals(b)) continue;
                int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                if (dist > step + 12) continue;
                if (rng.NextDouble() < 0.5) continue;
                for (int xx = Mathf.Min(a.x, b.x); xx <= Mathf.Max(a.x, b.x); xx++)
                    Plot(pixels, w, h, xx, a.y, trace);
                for (int yy = Mathf.Min(a.y, b.y); yy <= Mathf.Max(a.y, b.y); yy++)
                    Plot(pixels, w, h, b.x, yy, trace);
            }

        foreach (var n in nodes)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (dx * dx + dy * dy <= 4)
                        Plot(pixels, w, h, n.x + dx, n.y + dy, node);

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    static void Plot(Color[] px, int w, int h, int x, int y, Color c)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return;
        px[y * w + x] = c;
    }
}

/// <summary>Moves a ship horizontally; flags Done when off-screen.</summary>
public class ShipMover : MonoBehaviour
{
    public float speed;
    public float limit = 10.5f;
    public bool Done { get; private set; }

    void Update()
    {
        transform.localPosition += new Vector3(speed * Time.deltaTime, 0f, 0f);
        if (Mathf.Abs(transform.localPosition.x) > limit) Done = true;
    }
}

/// <summary>Pulses a circuit panel's alpha for a living-network feel.</summary>
public class CircuitPulse : MonoBehaviour
{
    public SpriteRenderer renderer2D;
    void Update()
    {
        if (renderer2D == null) return;
        float a = 0.35f + (Mathf.Sin(Time.time * 0.8f) * 0.5f + 0.5f) * 0.3f;
        var c = renderer2D.color;
        renderer2D.color = new Color(c.r, c.g, c.b, a);
    }
}
