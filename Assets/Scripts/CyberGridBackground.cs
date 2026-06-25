using UnityEngine;

/// <summary>
/// Thin, clean neon cyber grid — subtle background lines with a traveling wave pulse.
/// Two layers: a large main grid and a smaller secondary grid for depth.
/// </summary>
public class CyberGridBackground : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1.5f;
    public int gridWidth = 12;
    public int gridHeight = 8;
    public Color gridColor = new Color(0f, 0.25f, 0.5f, 0.10f);
    public Color pulseColor = new Color(0f, 0.6f, 1f, 0.25f);

    [Header("Animation")]
    public float pulseSpeed = 1.0f;

    private LineRenderer lineRenderer;
    private LineRenderer secondaryGrid;
    private float time;

    void Start()
    {
        // Primary grid
        var go = new GameObject("GridLineRenderer");
        go.transform.SetParent(transform, false);
        lineRenderer = go.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = 0.012f;
        lineRenderer.endWidth = 0.012f;
        lineRenderer.loop = false;
        lineRenderer.sortingOrder = -10;
        lineRenderer.useWorldSpace = false;

        // Secondary fine grid — offset, smaller, dimmer for depth
        var go2 = new GameObject("GridLineRenderer_Fine");
        go2.transform.SetParent(transform, false);
        secondaryGrid = go2.AddComponent<LineRenderer>();
        secondaryGrid.material = new Material(Shader.Find("Sprites/Default"));
        secondaryGrid.startWidth = 0.006f;
        secondaryGrid.endWidth = 0.006f;
        secondaryGrid.loop = false;
        secondaryGrid.sortingOrder = -9;
        secondaryGrid.useWorldSpace = false;
    }

    void Update()
    {
        time += Time.deltaTime;
        DrawGrid();
    }

    void DrawGrid()
    {
        float halfW = gridWidth * cellSize * 0.5f;
        float halfH = gridHeight * cellSize * 0.5f;

        int hCount = gridHeight + 1;
        int vCount = gridWidth + 1;
        int totalSegments = hCount + vCount;
        int positionsCount = totalSegments * 2;

        lineRenderer.positionCount = positionsCount;
        Vector3[] positions = new Vector3[positionsCount];
        int idx = 0;

        // Horizontal lines
        for (int y = 0; y <= gridHeight; y++)
        {
            float yPos = -halfH + y * cellSize;
            positions[idx++] = new Vector3(-halfW, yPos, 0.5f);
            positions[idx++] = new Vector3(halfW, yPos, 0.5f);
        }

        // Vertical lines
        for (int x = 0; x <= gridWidth; x++)
        {
            float xPos = -halfW + x * cellSize;
            positions[idx++] = new Vector3(xPos, -halfH, 0.5f);
            positions[idx++] = new Vector3(xPos, halfH, 0.5f);
        }

        lineRenderer.SetPositions(positions);

        // Gentle pulse
        float pulse = Mathf.Sin(time * pulseSpeed) * 0.3f + 0.7f;
        Color c = Color.Lerp(gridColor, pulseColor, pulse * 0.5f);
        c.a = Mathf.Lerp(0.08f, 0.2f, pulse);
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;

        // Secondary grid — smaller, offset, dimmer
        float fineCell = cellSize * 0.33f;
        float fineHalfW = halfW * 2f;
        float fineHalfH = halfH * 2f;
        int fineHCount = Mathf.RoundToInt(fineHalfH * 2f / fineCell) + 1;
        int fineVCount = Mathf.RoundToInt(fineHalfW * 2f / fineCell) + 1;
        // loops below run y=0..fineHCount and x=0..fineVCount inclusive => (count+1) lines each
        int fineSegments = (fineHCount + 1) + (fineVCount + 1);
        int finePosCount = fineSegments * 2;

        secondaryGrid.positionCount = finePosCount;
        Vector3[] finePositions = new Vector3[finePosCount];
        int fidx = 0;

        // Horizontal fine lines
        for (int y = 0; y <= fineHCount; y++)
        {
            float yPos = -fineHalfH + y * fineCell;
            finePositions[fidx++] = new Vector3(-fineHalfW, yPos, 0.5f);
            finePositions[fidx++] = new Vector3(fineHalfW, yPos, 0.5f);
        }

        // Vertical fine lines
        for (int x = 0; x <= fineVCount; x++)
        {
            float xPos = -fineHalfW + x * fineCell;
            finePositions[fidx++] = new Vector3(xPos, -fineHalfH, 0.5f);
            finePositions[fidx++] = new Vector3(xPos, fineHalfH, 0.5f);
        }

        secondaryGrid.SetPositions(finePositions);

        // Even more subtle for secondary grid
        Color fineC = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a * 0.4f);
        secondaryGrid.startColor = fineC;
        secondaryGrid.endColor = fineC;
    }
}
