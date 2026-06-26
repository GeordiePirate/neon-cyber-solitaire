using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Auto-generates all missing GameObjects, sprites, and prefabs at runtime
/// so the Neon Cyber Solitaire game works without any imported assets.
/// Runs automatically when you hit Play ▶ — attach nothing to anything.
/// </summary>
public static class _Bootstrap
{
    /// <summary>Pre-rendered card face sprites loaded from Resources/card_atlas.</summary>
    public static System.Collections.Generic.Dictionary<string, Sprite> CardSprites { get; private set; }
    public static Sprite CardBackSprite { get; private set; }
    static bool _hasRun = false;

    /// <summary>
    /// Returns an additive-blend sprite material that works in URP.
    /// "Sprites/Additive" does NOT ship with URP, so Shader.Find returns null there
    /// and new Material(null) throws — which previously aborted the whole bootstrap.
    /// We fall back to a Sprites/Default material with additive blend settings.
    /// </summary>
    static Material MakeAdditiveSpriteMaterial()
    {
        Shader sh = Shader.Find("Sprites/Additive")
                    ?? Shader.Find("Legacy Shaders/Particles/Additive")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Transparent");
        var mat = new Material(sh);
        // Force additive blending (One One) so glow layers accumulate light
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite"))   mat.SetFloat("_ZWrite", 0f);
        return mat;
    }

    /// <summary>Safe sprite material — never returns null.</summary>
    static Material MakeSpriteMaterial()
    {
        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
        return new Material(sh);
    }

    /// <summary>
    /// Loads the pre-rendered card sprite atlas from Resources and slices into per-card sprites.
    /// Falls back to procedural textures if the atlas isn't found.
    /// </summary>
    static void LoadCardSprites()
    {
        CardSprites = new System.Collections.Generic.Dictionary<string, Sprite>();
        var atlas = Resources.Load<Texture2D>("card_atlas");
        if (atlas != null)
        {
            // Adjusted for 2048x1024 atlas
            int cols = 13; // A-10,J,Q,K
            int rows = 4;  // suits

            float actualCardWidth = Mathf.Floor(atlas.width / (float)cols * 100f) / 100f;
            float actualCardHeight = Mathf.Floor(atlas.height / (float)rows * 100f) / 100f;

            Debug.Log($"[Bootstrap] Atlas size: {atlas.width}x{atlas.height}. Using {actualCardWidth}x{actualCardHeight} per card.");

            for (int suit = 0; suit < rows; suit++)
            {
                for (int rank = 0; rank < cols; rank++)
                {
                    string rankStr = (rank + 1) switch
                    {
                        1 => "A",
                        11 => "J",
                        12 => "Q",
                        13 => "K",
                        _ => (rank + 1).ToString()
                    };
                    string suitStr = suit switch { 0 => "S", 1 => "H", 2 => "D", 3 => "C", _ => "S" };
                    string key = rankStr + suitStr;

                    Rect rect = new Rect(
                        rank * actualCardWidth,
                        suit * actualCardHeight,
                        actualCardWidth,
                        actualCardHeight
                    );

                    var sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100f);
                    CardSprites[key] = sprite;
                }
            }
            Debug.Log($"[Bootstrap] Successfully loaded {CardSprites.Count} card sprites from atlas");
        }
        else
        {
            Debug.LogWarning("[Bootstrap] card_atlas.png not found in Resources! Falling back to procedural.");
        }

        // Card back
        CardBackSprite = Resources.Load<Sprite>("card_back");
        if (CardBackSprite == null)
            Debug.LogWarning("[Bootstrap] card_back.png not found in Resources!");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (_hasRun) return;
        _hasRun = true;

        try
        {
            var log = new System.Diagnostics.Stopwatch();
            log.Start();

            SetupCamera();
            LoadCardSprites();
            var gm = SetupGameManager();
            SetupCanvas(gm);
            var cardPrefab = SetupCardPrefab();
            var layout = SetupTableau(cardPrefab);
            SetupFoundations();
            SetupEventSystem();
            SetupPostProcessing();
            SetupBackgroundEffects();
            var rain = SetupDigitalRain();
            SetupParticles();
            SetupScanlines();

            // Wait 1 frame then build (gives Unity time to call Awake/Start on the managers)
            GameObject runner = new GameObject("_BootstrapRunner");
            runner.AddComponent<BootstrapRunner>().Init(layout);

            log.Stop();
            Debug.Log($"[Bootstrap] Neon Cyber Solitaire ready! ({log.ElapsedMilliseconds}ms)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bootstrap] ERROR: {e.Message}\n{e.StackTrace}");
        }
    }

    // ═══════════════════════════════════════════════════
    //  1. CAMERA
    // ═══════════════════════════════════════════════════

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.6f;  // Fits full 7-col board + foundations with margin
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.003f, 0.001f, 0.008f);  // Near-pure deeper black
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.useOcclusionCulling = false;
        // Enable post-processing in 2D mode via URP extension
        var urpCam = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpCam != null)
        {
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
        }
        cam.transform.position = new Vector3(-0.2f, -0.8f, -10);  // Centered on board (board sits below origin)
    }

    // ═══════════════════════════════════════════════════
    //  2. GAME MANAGER
    // ═══════════════════════════════════════════════════

    static GameObject SetupGameManager()
    {
        var gm = new GameObject("GameManager");
        gm.AddComponent<BoardManager>();
        gm.AddComponent<ScoreManager>();
        gm.AddComponent<AbilityManager>();
        return gm;
    }

    // ═══════════════════════════════════════════════════
    //  3. CANVAS + UI
    // ═══════════════════════════════════════════════════

    static void SetupCanvas(GameObject gm)
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Score text (top-left) — neon cyan
        var scoreTmp = CreateUIText(canvasGo, "ScoreText", "000000",
            new Vector2(220, -40), 48, TextAlignmentOptions.Left,
            new Color(0f, 0.88f, 1f));

        // Multiplier text (top-right) — neon pink
        var multiTmp = CreateUIText(canvasGo, "MultiplierText", "",
            new Vector2(-220, -40), 36, TextAlignmentOptions.Right,
            new Color(1f, 0.08f, 0.58f));

        // High score
        CreateUIText(canvasGo, "HighScoreText", "BEST: 0",
            new Vector2(220, -80), 22, TextAlignmentOptions.Left,
            new Color(0.4f, 0.4f, 0.5f));

        // Draw hint
        CreateUIText(canvasGo, "DrawHint", "[ CLICK STOCK TO DRAW ]",
            new Vector2(0, 300), 20, TextAlignmentOptions.Center,
            new Color(0.3f, 0.8f, 1f, 0.6f));

        // Wire ScoreManager to UI
        var sm = gm.GetComponent<ScoreManager>();
        if (sm != null) sm.SetUI(scoreTmp, multiTmp);
    }

    static TextMeshProUGUI CreateUIText(GameObject parent, string name, string text,
        Vector2 anchoredPos, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(400, 50);

        return tmp;
    }

    // ═══════════════════════════════════════════════════
    //  4. CARD PREFAB — Holographic Neon Cyberpunk Card
    // ═══════════════════════════════════════════════════

    static GameObject SetupCardPrefab()
    {
        var prefab = new GameObject("Card_Prefab");

        // Higher resolution textures (160x224) for crisp visuals
        var backTex  = GenerateTexture(160, 224, (x, y) =>
        {
            bool border = IsBorder(x, y, 160, 224, 8);
            if (border) return new Color(0.05f, 0.25f, 0.6f, 0.8f);

            // Circuit lines — more detailed at higher res
            bool hLine = y % 40 < 3 || y % 56 < 2;
            bool vLine = x % 40 < 3 || x % 56 < 2;
            bool dot = (x % 28 < 4 && y % 28 < 4) || (x % 28 > 24 && y % 28 > 24);
            bool diamond = Mathf.Abs(x - 80) + Mathf.Abs(y - 112) < 40;
            bool diamondInner = Mathf.Abs(x - 80) + Mathf.Abs(y - 112) < 24;
            bool corner = (x < 30 && y < 30) || (x > 130 && y < 30) || (x < 30 && y > 194) || (x > 130 && y > 194);
            bool cross = (Mathf.Abs(x - 80) < 3 || Mathf.Abs(y - 112) < 3) && (Mathf.Abs(x - 80) + Mathf.Abs(y - 112)) > 20;

            if (diamondInner) return new Color(0f, 0.5f, 1f, 0.6f);
            if (diamond) return new Color(0f, 0.3f, 0.7f, 0.4f);
            if (corner) return new Color(0.2f, 0.05f, 0.4f, 0.4f);
            if (cross) return new Color(0f, 0.6f, 1f, 0.6f);
            if (dot) return new Color(0f, 0.7f, 1f, 0.5f);
            if (hLine || vLine) return new Color(0f, 0.3f, 0.6f, 0.3f);

            return new Color(0.04f, 0.01f, 0.08f);
        });

        // Card face — DARK HOLOGRAPHIC PANEL: filled dark body with circuit traces,
        // so cards read as solid objects with a cyberpunk feel.
        var faceTex  = GenerateTexture(160, 224, (x, y) =>
        {
            if (IsBorder(x, y, 160, 224, 8))
                return new Color(0.06f, 0.12f, 0.25f, 0.90f);   // darker rim

            // Vertical gradient: deeper at bottom, brighter toward top
            float t = (float)y / 224f;
            float dark = Mathf.Lerp(0.35f, 0.70f, t);  // dark at bottom, slightly lighter top
            float r = Mathf.Lerp(0.03f, 0.08f, t);
            float g = Mathf.Lerp(0.02f, 0.06f, t);
            float b = Mathf.Lerp(0.08f, 0.14f, t);

            // Faint circuit-board traces
            bool circuitH = (x % 18 < 1) && (y % 36 > 8) && (y % 36 < 28);
            bool circuitV = (y % 18 < 1) && (x % 36 > 8) && (x % 36 < 28);
            bool node = ((x + 9) % 36 < 3 && (y + 9) % 36 < 3);

            float alpha = 0.72f;  // SOLID — cards read as real objects
            if (node)       { r = 0.0f; g = 0.30f; b = 0.60f; alpha = 0.40f; }
            else if (circuitH || circuitV) { r = 0.0f; g = 0.15f; b = 0.30f; alpha = 0.25f; }

            return new Color(r * dark, g * dark, b * dark, alpha);
        });

        // Thick neon border (16px wide, white pixels — read as neon glow ring)
        var borderTex = GenerateTexture(160, 224, (x, y) =>
            IsBorder(x, y, 160, 224, 16) ? Color.white : Color.clear);

        var backSprite  = Sprite.Create(backTex,  new Rect(0, 0, 160, 224), new Vector2(0.5f, 0.5f), 100f);
        var faceSprite  = Sprite.Create(faceTex,  new Rect(0, 0, 160, 224), new Vector2(0.5f, 0.5f), 100f);
        var borderSprite = Sprite.Create(borderTex, new Rect(0, 0, 160, 224), new Vector2(0.5f, 0.5f), 100f);

        // Background (card face) — sortingOrder 1
        var bg = prefab.AddComponent<SpriteRenderer>();
        bg.sprite = faceSprite;
        bg.sortingOrder = 1;

        // Glow layer 1 — INNER GLOW (tight, bright)
        var glow1Go = new GameObject("GlowInner", typeof(SpriteRenderer));
        glow1Go.transform.SetParent(prefab.transform, false);
        var glow1 = glow1Go.GetComponent<SpriteRenderer>();
        glow1.sprite = borderSprite;
        glow1.sortingOrder = 0;
        glow1.color = new Color(0f, 0.5f, 1f, 0.5f);
        glow1.material = MakeAdditiveSpriteMaterial();
        glow1Go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        // Glow layer 2 — OUTER HALO (medium, soft)
        var glow2Go = new GameObject("GlowOuter", typeof(SpriteRenderer));
        glow2Go.transform.SetParent(prefab.transform, false);
        var glow2 = glow2Go.GetComponent<SpriteRenderer>();
        glow2.sprite = borderSprite;
        glow2.sortingOrder = -1;
        glow2.color = new Color(0f, 0.4f, 1f, 0.25f);
        glow2.material = MakeAdditiveSpriteMaterial();
        glow2Go.transform.localScale = new Vector3(2.8f, 2.8f, 1f);

        // Glow layer 3 — AMBIENT GLOW (wide, faint — makes the bloom pop)
        var glow3Go = new GameObject("GlowAmbient", typeof(SpriteRenderer));
        glow3Go.transform.SetParent(prefab.transform, false);
        var glow3 = glow3Go.GetComponent<SpriteRenderer>();
        glow3.sprite = borderSprite;
        glow3.sortingOrder = -2;
        glow3.color = new Color(0f, 0.3f, 0.8f, 0.1f);
        glow3.material = MakeAdditiveSpriteMaterial();
        glow3Go.transform.localScale = new Vector3(5.0f, 5.0f, 1f);

        // Border (visible neon outline)
        var borderGo = new GameObject("Border", typeof(SpriteRenderer));
        borderGo.transform.SetParent(prefab.transform, false);
        var border = borderGo.GetComponent<SpriteRenderer>();
        border.sprite = borderSprite;
        border.sortingOrder = 2;

        // ── Face symbols — BIG holographic text ───────────

        // Corner rank top-left
        var rankTL = new GameObject("RankTL", typeof(TextMeshPro));
        rankTL.transform.SetParent(prefab.transform, false);
        var rankTLTmp = rankTL.GetComponent<TextMeshPro>();
        rankTLTmp.text = "";
        rankTLTmp.fontSize = 14f;          // Bigger corner rank
        rankTLTmp.alignment = TextAlignmentOptions.TopLeft;
        rankTLTmp.color = Color.clear;
        rankTLTmp.fontStyle = FontStyles.Bold;
        rankTLTmp.GetComponent<Renderer>().sortingOrder = 3;
        rankTL.transform.localPosition = new Vector3(-0.62f, 0.82f, -0.01f);
        var rtTL = rankTLTmp.rectTransform;
        rtTL.sizeDelta = new Vector2(0.7f, 0.5f);

        // Corner suit top-left
        var suitTL = new GameObject("SuitTL", typeof(TextMeshPro));
        suitTL.transform.SetParent(prefab.transform, false);
        var suitTLTmp = suitTL.GetComponent<TextMeshPro>();
        suitTLTmp.text = "";
        suitTLTmp.fontSize = 10f;          // Bigger corner suit
        suitTLTmp.alignment = TextAlignmentOptions.TopLeft;
        suitTLTmp.color = Color.clear;
        suitTLTmp.fontStyle = FontStyles.Bold;
        suitTLTmp.GetComponent<Renderer>().sortingOrder = 3;
        suitTL.transform.localPosition = new Vector3(-0.62f, 0.55f, -0.01f);
        var stTL = suitTLTmp.rectTransform;
        stTL.sizeDelta = new Vector2(0.6f, 0.4f);

        // MASSIVE center suit symbol — the holographic centerpiece
        var centerSuit = new GameObject("CenterSuit", typeof(TextMeshPro));
        centerSuit.transform.SetParent(prefab.transform, false);
        var centerSuitTmp = centerSuit.GetComponent<TextMeshPro>();
        centerSuitTmp.text = "";
        centerSuitTmp.fontSize = 26f;         // HUGE center symbol
        centerSuitTmp.alignment = TextAlignmentOptions.Center;
        centerSuitTmp.color = Color.clear;
        centerSuitTmp.fontStyle = FontStyles.Bold;
        centerSuitTmp.GetComponent<Renderer>().sortingOrder = 3;
        centerSuit.transform.localPosition = new Vector3(0, 0.05f, -0.01f);
        var csRect = centerSuitTmp.rectTransform;
        csRect.sizeDelta = new Vector2(1.2f, 1.0f);

        // Corner rank bottom-right (inverted)
        var rankBR = new GameObject("RankBR", typeof(TextMeshPro));
        rankBR.transform.SetParent(prefab.transform, false);
        var rankBRTmp = rankBR.GetComponent<TextMeshPro>();
        rankBRTmp.text = "";
        rankBRTmp.fontSize = 11f;
        rankBRTmp.alignment = TextAlignmentOptions.BottomRight;
        rankBRTmp.color = Color.clear;
        rankBRTmp.fontStyle = FontStyles.Bold;
        rankBRTmp.GetComponent<Renderer>().sortingOrder = 3;
        rankBR.transform.localPosition = new Vector3(0.62f, -0.82f, -0.01f);
        var rtBR = rankBRTmp.rectTransform;
        rtBR.sizeDelta = new Vector2(0.7f, 0.5f);

        // Corner suit bottom-right (inverted)
        var suitBR = new GameObject("SuitBR", typeof(TextMeshPro));
        suitBR.transform.SetParent(prefab.transform, false);
        var suitBRTmp = suitBR.GetComponent<TextMeshPro>();
        suitBRTmp.text = "";
        suitBRTmp.fontSize = 8f;
        suitBRTmp.alignment = TextAlignmentOptions.BottomRight;
        suitBRTmp.color = Color.clear;
        suitBRTmp.fontStyle = FontStyles.Bold;
        suitBRTmp.GetComponent<Renderer>().sortingOrder = 3;
        suitBR.transform.localPosition = new Vector3(0.62f, -0.65f, -0.01f);
        var stBR = suitBRTmp.rectTransform;
        stBR.sizeDelta = new Vector2(0.6f, 0.4f);

        // Glitch particles placeholder
        new GameObject("GlitchParticles") { transform = { parent = prefab.transform } };

        // Collider
        var col = prefab.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.12f);

        // Visual controller
        var visual = prefab.AddComponent<CardVisualController>();
        visual.SetRenderers(border, bg, glow1, glow2, glow3, rankTLTmp, suitTLTmp, centerSuitTmp, rankBRTmp, suitBRTmp);

        // Input handler
        prefab.AddComponent<CardInputHandler>();

        return prefab;
    }

    static Texture2D GenerateTexture(int w, int h, System.Func<int, int, Color> pixelFn)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, pixelFn(x, y));
        tex.Apply();
        return tex;
    }

    static bool IsBorder(int x, int y, int w, int h, int t) =>
        x < t || x >= w - t || y < t || y >= h - t;

    // ═══════════════════════════════════════════════════
    //  5. TABLEAU LAYOUT
    // ═══════════════════════════════════════════════════

    static TableauLayoutManager SetupTableau(GameObject cardPrefab)
    {
        var layoutGo = new GameObject("TableauLayout");
        var layout = layoutGo.AddComponent<TableauLayoutManager>();
        layout.SetCardPrefab(cardPrefab);
        
        // Stock pile visual
        var stockTex = GenerateTexture(140, 180, (x, y) =>
        {
            if (IsBorder(x, y, 140, 180, 8))
                return new Color(0f, 0.3f, 0.6f, 0.6f);
            return new Color(0.03f, 0.01f, 0.06f, 0.5f);
        });
        var stockSprite = Sprite.Create(stockTex, new Rect(0, 0, 140, 180), new Vector2(0.5f, 0.5f), 100f);
        
        var stockGo = new GameObject("StockPile");
        var stockSr = stockGo.AddComponent<SpriteRenderer>();
        stockSr.sprite = stockSprite;
        stockSr.color = new Color(0f, 0.5f, 1f, 0.5f);
        stockSr.sortingOrder = -1;
        stockGo.transform.position = layout.GetStockPosition();
        
        // Stock label
        var stockLabelGo = new GameObject("StockLabel", typeof(TextMeshPro));
        stockLabelGo.transform.SetParent(stockGo.transform, false);
        var stockLabel = stockLabelGo.GetComponent<TextMeshPro>();
        stockLabel.text = "DRAW";
        stockLabel.fontSize = 2f;
        stockLabel.alignment = TextAlignmentOptions.Center;
        stockLabel.color = new Color(0f, 0.6f, 1f, 0.4f);
        stockLabel.GetComponent<Renderer>().sortingOrder = 0;
        var stockRect = stockLabel.rectTransform;
        stockRect.sizeDelta = new Vector2(1f, 0.4f);
        
        // Waste pile area visual
        var wasteGo = new GameObject("WastePile");
        var wasteSr = wasteGo.AddComponent<SpriteRenderer>();
        wasteSr.sprite = stockSprite;
        wasteSr.color = new Color(0.3f, 0.1f, 0.5f, 0.3f);
        wasteSr.sortingOrder = -1;
        wasteGo.transform.position = layout.GetWastePosition(0);
        
        return layout;
    }

    // ═══════════════════════════════════════════════════
    //  6. FOUNDATION ZONES
    // ═══════════════════════════════════════════════════

    static void SetupFoundations()
    {
        Color[] zoneColors = {
            new Color(1f, 0.08f, 0.58f, 0.4f),  // Hearts - Neon Pink
            new Color(1f, 0.3f, 0.3f, 0.4f),     // Diamonds - Neon Red
            new Color(0f, 0.5f, 1f, 0.4f),       // Clubs - Electric Blue
            new Color(0f, 0.88f, 1f, 0.4f)       // Spades - Cyan
        };

        var zoneTex = GenerateTexture(140, 180, (x, y) =>
        {
            if (IsBorder(x, y, 140, 180, 4))
                return new Color(0.2f, 0.2f, 0.3f, 0.5f);
            // Subtle inner glow
            float cx = (x - 70f) / 70f;
            float cy = (y - 90f) / 90f;
            float dist = Mathf.Sqrt(cx * cx + cy * cy);
            if (dist < 0.7f) return new Color(0.05f, 0.05f, 0.12f, 0.3f);
            return new Color(0.1f, 0.1f, 0.15f, 0.15f);
        });
        var zoneSprite = Sprite.Create(zoneTex, new Rect(0, 0, 140, 180), new Vector2(0.5f, 0.5f), 100f);

        string[] names = { "Hearts", "Diamonds", "Clubs", "Spades" };
        string[] symbols = { "♥", "♦", "♣", "♠" };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"Foundation_{names[i]}");
            go.transform.position = new Vector3(0.5f + i * 1.8f, 2.0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = zoneSprite;
            sr.color = zoneColors[i];
            sr.sortingOrder = -1;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 1.8f);

            go.AddComponent<FoundationDropZone>().Init(i);
            
            // Suit label on foundation zone
            var suitGo = new GameObject("SuitLabel", typeof(TextMeshPro));
            suitGo.transform.SetParent(go.transform, false);
            var suitLabel = suitGo.GetComponent<TextMeshPro>();
            suitLabel.text = symbols[i];
            suitLabel.fontSize = 3.5f;
            suitLabel.alignment = TextAlignmentOptions.Center;
            suitLabel.color = new Color(zoneColors[i].r, zoneColors[i].g, zoneColors[i].b, 0.5f);
            suitLabel.GetComponent<Renderer>().sortingOrder = 0;
            var suitRect = suitLabel.rectTransform;
            suitRect.sizeDelta = new Vector2(1f, 0.5f);
        }
    }

    // ═══════════════════════════════════════════════════
    //  7. EVENT SYSTEM
    // ═══════════════════════════════════════════════════

    static void SetupEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ═══════════════════════════════════════════════════
    //  8. POST-PROCESSING — HEAVY BLOOM + TONEMAPPING
    // ═══════════════════════════════════════════════════

    static void SetupPostProcessing()
    {
        var volumeGo = new GameObject("PostProcessVolume");
        var volume = volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;

        var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();

        // Bloom — CRANKED for dramatic neon glow
        var bloom = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.Bloom>();
        bloom.active = true;
        bloom.threshold.value = 0.25f;      // Lower threshold = more bloom on dimmer things
        bloom.intensity.value = 5.0f;        // High intensity for dramatic glow
        bloom.scatter.value = 0.7f;          // Wide scatter for bigger halos
        bloom.tint.value = new Color(0.9f, 0.85f, 1f, 1f);  // Slight blue-purple tint
        bloom.clamp.value = 65472f;
        bloom.dirtTexture.value = null;
        bloom.dirtIntensity.value = 0f;
        profile.components.Add(bloom);

        // Tonemapping (ACES for punchy neon — better than Neutral)
        var tone = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.Tonemapping>();
        tone.active = true;
        tone.mode.value = UnityEngine.Rendering.Universal.TonemappingMode.Neutral;
        profile.components.Add(tone);

        // Lift/Gamma/Gain — slight contrast bump
        var liftGammaGain = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.LiftGammaGain>();
        liftGammaGain.active = true;
        // Slight gamma boost + gain for more pop
        liftGammaGain.lift.value = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        liftGammaGain.gamma.value = new Vector4(0.8f, 0.85f, 1.0f, 0.0f);
        liftGammaGain.gain.value = new Vector4(1.1f, 1.05f, 1.15f, 0.0f);
        profile.components.Add(liftGammaGain);

        volume.profile = profile;
    }

    // ═══════════════════════════════════════════════════
    //  9. BACKGROUND EFFECTS — Deep cyberpunk environment
    // ═══════════════════════════════════════════════════

    static void SetupBackgroundEffects()
    {
        // NOTE: The dark gradient sky is now provided by the real sky.png layer
        // inside CityscapeBackground (sortingOrder -20). The old procedural
        // BackgroundGradient was removed so it doesn't render in front of the skyline.

        // Animated cyber grid — thin, subtle cyan lines
        var gridGo = new GameObject("CyberGrid");
        gridGo.AddComponent<CyberGridBackground>();

        // City skyline silhouette + circuit network panel (the "cyber city" layers)
        var cityGo = new GameObject("Cityscape");
        cityGo.AddComponent<CityscapeBackground>();

        // Ambient particles
        var particlesGo = new GameObject("AmbientParticles");
        var ps = particlesGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.05f;
        main.startColor = new Color(0f, 0.5f, 1f, 0.3f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(10f, 6f, 1f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = MakeSpriteMaterial();
        renderer.sortingOrder = -5;

        // Additional glowing dust motes — floating data particles
        var dustGo = new GameObject("DataDust");
        var dustPs = dustGo.AddComponent<ParticleSystem>();
        var dustMain = dustPs.main;
        dustMain.loop = true;
        dustMain.startLifetime = 8f;
        dustMain.startSpeed = 0.15f;
        dustMain.startSize = 0.03f;
        dustMain.startColor = new Color(0.6f, 0.2f, 1f, 0.2f); // Purple
        dustMain.maxParticles = 40;
        dustMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var dustEmission = dustPs.emission;
        dustEmission.rateOverTime = 4f;

        var dustShape = dustPs.shape;
        dustShape.shapeType = ParticleSystemShapeType.Rectangle;
        dustShape.scale = new Vector3(12f, 7f, 1f);

        var dustRenderer = dustPs.GetComponent<ParticleSystemRenderer>();
        dustRenderer.material = MakeSpriteMaterial();
        dustRenderer.sortingOrder = -7;
    }

    // ═══════════════════════════════════════════════════
    //  10. DIGITAL RAIN — Matrix-style katakana effect
    // ═══════════════════════════════════════════════════

    static GameObject SetupDigitalRain()
    {
        var rainGo = new GameObject("DigitalRain");
        rainGo.AddComponent<DigitalRain>();
        rainGo.transform.position = new Vector3(0, 0, 1f);
        return rainGo;
    }

    // ═══════════════════════════════════════════════════
    //  11. DATA STREAM PARTICLES
    // ═══════════════════════════════════════════════════

    static void SetupParticles()
    {
        // Floating data stream particles — rising motes
        var dataGo = new GameObject("DataStreams");
        var ps = dataGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 6f;
        main.startSpeed = 0.3f;
        main.startSize = 0.04f;
        main.startColor = new Color(0f, 1f, 0.6f, 0.15f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;
        main.startRotation = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(8f, 5f, 1f);

        var velOverLifetime = ps.velocityOverLifetime;
        velOverLifetime.enabled = true;
        velOverLifetime.xMultiplier = 0.2f;
        velOverLifetime.yMultiplier = -0.1f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0f, 1f, 0.6f), 0f),
                new GradientColorKey(new Color(0f, 0.5f, 1f), 0.5f),
                new GradientColorKey(new Color(1f, 0.08f, 0.58f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = MakeSpriteMaterial();
        renderer.sortingOrder = -4;
    }

    // ═══════════════════════════════════════════════════
    //  12. SCAN LINES OVERLAY — CRT monitor effect
    // ═══════════════════════════════════════════════════

    static void SetupScanlines()
    {
        // Create scan line texture procedurally
        int texW = 4, texH = 12;
        var scanTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        for (int x = 0; x < texW; x++)
        {
            for (int y = 0; y < texH; y++)
            {
                // Every other line is slightly darker — more prominent scanlines
                float alpha = (y % 2 == 0) ? 0.12f : 0f;
                scanTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        scanTex.Apply();
        scanTex.wrapMode = TextureWrapMode.Repeat;

        var mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = scanTex;
        mat.mainTextureOffset = new Vector2(0, 0);

        var scanGo = new GameObject("ScanLines");
        var sr = scanGo.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(scanTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), 100f);
        sr.material = mat;
        sr.color = Color.white;
        sr.sortingOrder = 100;
        scanGo.transform.localScale = new Vector3(10f, 6f, 1f);

        // Animated scan line scroll
        var scroller = scanGo.AddComponent<ScanlineScroller>();
        scroller.scrollSpeed = 0.5f;
    }
}

/// <summary>
/// Waits one frame for Awake/Start to initialize managers, then builds the tableau.
/// </summary>
public class BootstrapRunner : MonoBehaviour
{
    private TableauLayoutManager layout;

    public void Init(TableauLayoutManager layoutRef)
    {
        layout = layoutRef;
    }

    private void Start()
    {
        // BoardManager.Start() already fired (we're AfterSceneLoad),
        // so manually trigger building
        if (BoardManager.Instance != null)
        {
            layout.BuildTableau();
            Debug.Log("[BootstrapRunner] Tableau built!");
        }
        else
        {
            Debug.LogError("[BootstrapRunner] BoardManager not found!");
        }
        Destroy(gameObject);
    }
}

/// <summary>
/// Scrolls the material texture offset for CRT scan line animation.
/// </summary>
public class ScanlineScroller : MonoBehaviour
{
    public float scrollSpeed = 0.5f;

    private Material mat;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) mat = sr.material;
        if (mat == null) enabled = false;
    }

    void Update()
    {
        if (mat != null)
        {
            float offset = Time.time * scrollSpeed % 1f;
            mat.mainTextureOffset = new Vector2(0, offset);
        }
    }
}
