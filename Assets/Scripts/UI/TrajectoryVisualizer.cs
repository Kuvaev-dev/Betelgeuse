using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Стрічка траєкторії. Під час польоту минуле незмінне (append-only), тож лінія
/// ніколи не rebuild/мерехтить; лише tip стежить за ракетою щокадру.
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxPoints = 12000;
    public float baseLineWidth = 3.2f;
    public float minPointDistance = 1.25f;
    public float groundY = 0.5f;

    public Color goodColor = new(0.35f, 0.95f, 0.65f, 1f);
    public Color badColor = new(1f, 0.4f, 0.42f, 1f);
    public Color normalColor = new(0.45f, 0.9f, 1f, 1f);

    readonly List<Vector3> pts = new();
    Vector3 lastCommitted;
    bool hasCommitted;
    bool finished;
    bool visible = true;
    float smoothWidth;
    Vector3 tipSmoothed;
    bool hasTip;

    // Повторно використаний буфер — без alloc щокадру
    Vector3[] uploadBuf = new Vector3[64];

    public int PointCount => pts.Count;
    public IReadOnlyList<Vector3> Points => pts;
    public bool IsVisible => visible;

    void Awake()
    {
        EnsureLine();
        if (rocketPhysics == null)
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();
        smoothWidth = baseLineWidth;
    }

    void Start()
    {
        EnsureLine();
        ApplyVisibility();
    }

    void EnsureLine()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        lineRenderer.sortingOrder = 100;
        lineRenderer.widthMultiplier = 1f;
        lineRenderer.generateLightingData = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.positionCount = 0;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default")
                            ?? Shader.Find("Unlit/Color")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                var mat = new Material(shader) { color = Color.white, renderQueue = 3000 };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                lineRenderer.sharedMaterial = mat;
            }
        }

        ApplyColor(normalColor);
        ApplyWidth(baseLineWidth);
    }

    void ApplyWidth(float w)
    {
        if (lineRenderer == null) return;
        float ww = Mathf.Clamp(w, 1.2f, 12f);
        lineRenderer.startWidth = ww;
        lineRenderer.endWidth = ww * 0.7f;
    }

    public void SetVisible(bool on)
    {
        visible = on;
        ApplyVisibility();
    }

    public void ToggleVisible() => SetVisible(!visible);

    void ApplyVisibility()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = visible && pts.Count >= 2;
    }

    void FixedUpdate()
    {
        if (!InFlight()) return;
        SampleCommit(rocketPhysics.state.position);
    }

    void LateUpdate()
    {
        if (rocketPhysics == null)
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();
        if (rocketPhysics != null && rocketPhysics.batchDrivenTicks) return;
        if (!visible || lineRenderer == null) return;

        if (InFlight())
        {
            // Tip стежить за ракетою щокадру (плавно), історія фіксована
            Vector3 tip = rocketPhysics.state.position;
            if (tip.y < groundY + 0.12f) tip.y = groundY + 0.12f;

            if (!hasTip)
            {
                tipSmoothed = tip;
                hasTip = true;
            }
            else
            {
                // Швидке, але стабільне стеження tip (без overshoot)
                float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
                tipSmoothed = Vector3.Lerp(tipSmoothed, tip, k);
            }

            SampleCommit(tip); // може додати committed-вузол
            PushLive(tipSmoothed);
        }
        else if (pts.Count >= 2 && lineRenderer.positionCount != pts.Count)
        {
            PushCommittedOnly();
        }

        // Повільна адаптація ширини — ніколи snap (snap давав мерехтіння)
        if (pts.Count > 1 && Camera.main != null)
        {
            Vector3 mid = pts[pts.Count / 2];
            float d = Vector3.Distance(Camera.main.transform.position, mid);
            float want = baseLineWidth + d * 0.0055f;
            smoothWidth = Mathf.Lerp(smoothWidth, want, 1f - Mathf.Exp(-3f * Time.deltaTime));
            ApplyWidth(smoothWidth);
        }
    }

    bool InFlight()
    {
        return !finished
               && rocketPhysics != null
               && rocketPhysics.simulationArmed
               && !rocketPhysics.batchDrivenTicks
               && !rocketPhysics.state.simulationFinished
               && !rocketPhysics.state.isLanded;
    }

    public void SampleFlight(bool force = false)
    {
        if (!InFlight() && !force) return;
        if (rocketPhysics == null) return;
        SampleCommit(rocketPhysics.state.position, force);
    }

    /// <summary>Додати стабільний вузол історії, коли ракета змістилась достатньо далеко.</summary>
    void SampleCommit(Vector3 p, bool force = false)
    {
        if (finished && !force) return;
        if (p.y < groundY) p.y = groundY + 0.12f;

        float minDist = minPointDistance;
        if (p.y < 200f) minDist = 1.0f;
        if (p.y < 60f) minDist = 0.65f;
        if (p.y < 15f) minDist = 0.35f;

        // Близько до ємності: лише проріджувати NEW samples — ніколи не різати початок шляху
        if (!force && pts.Count > maxPoints * 3 / 4)
        {
            float fill = pts.Count / (float)maxPoints;
            minDist *= Mathf.Lerp(1.5f, 4f, Mathf.InverseLerp(0.75f, 1f, fill));
        }

        if (!force && hasCommitted && (p - lastCommitted).sqrMagnitude < minDist * minDist)
            return;

        // Жорсткий ліміт: тримати повну історію; далі не commit (tip і далі стежить у PushLive)
        if (!force && pts.Count >= maxPoints)
            return;

        // Легке one-step згладжування лише NEW-вузла (ніколи не переписувати минулі)
        if (hasCommitted && !force)
            p = Vector3.Lerp(lastCommitted, p, 0.82f);

        pts.Add(p);
        lastCommitted = p;
        hasCommitted = true;
    }

    /// <summary>Історія + live tip (tip не commit, доки SampleCommit).</summary>
    void PushLive(Vector3 tip)
    {
        EnsureLine();
        int nHist = pts.Count;
        if (nHist == 0)
        {
            // Bootstrap з двома точками, щоб LineRenderer міг малювати
            EnsureBuf(2);
            uploadBuf[0] = tip;
            uploadBuf[1] = tip + Vector3.up * 0.25f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(uploadBuf);
            lineRenderer.enabled = visible;
            return;
        }

        // tip візуально замінює останній committed, якщо дуже близько; інакше append як ephemeral end
        bool tipIsNew = (tip - pts[nHist - 1]).sqrMagnitude > 0.0004f;
        int n = tipIsNew ? nHist + 1 : nHist;
        EnsureBuf(n);
        for (int i = 0; i < nHist; i++)
            uploadBuf[i] = pts[i];
        if (tipIsNew)
            uploadBuf[nHist] = tip;
        else
            uploadBuf[nHist - 1] = tip; // зсунути останній вузол до tip без зміни count

        // Лише збільшувати positionCount; зменшення дає мерехтіння
        if (lineRenderer.positionCount < n)
            lineRenderer.positionCount = n;
        else if (lineRenderer.positionCount > n + 2)
            lineRenderer.positionCount = n; // рідкісне зменшення, коли tip зливається

        // Вивантажувати лише використаний prefix
        if (lineRenderer.positionCount != n)
            lineRenderer.positionCount = n;
        lineRenderer.SetPositions(Slice(n));
        lineRenderer.enabled = visible && n >= 2;
    }

    void PushCommittedOnly()
    {
        EnsureLine();
        int n = pts.Count;
        if (n < 2)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
            return;
        }
        EnsureBuf(n);
        for (int i = 0; i < n; i++)
            uploadBuf[i] = pts[i];
        lineRenderer.positionCount = n;
        lineRenderer.SetPositions(Slice(n));
        lineRenderer.enabled = visible;
    }

    void EnsureBuf(int n)
    {
        if (uploadBuf.Length < n)
            uploadBuf = new Vector3[Mathf.NextPowerOfTwo(n)];
    }

    Vector3[] Slice(int n)
    {
        // SetPositions потребує масив точної довжини на деяких версіях Unity
        if (uploadBuf.Length == n) return uploadBuf;
        var exact = new Vector3[n];
        System.Array.Copy(uploadBuf, exact, n);
        return exact;
    }

    public void OnSimulationFinished(bool successful)
    {
        finished = true;
        if (rocketPhysics != null)
        {
            Vector3 touch = rocketPhysics.state.position;
            touch.y = groundY + 0.2f;
            if (hasCommitted && lastCommitted.y > groundY + 2f)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt((lastCommitted.y - groundY) / 10f), 3, 8);
                Vector3 from = lastCommitted;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    t = t * t * (3f - 2f * t);
                    pts.Add(Vector3.Lerp(from, touch, t));
                }
                lastCommitted = touch;
            }
            else
                SampleCommit(touch, force: true);
        }
        hasTip = false;
        PushCommittedOnly();
        ApplyColor(successful ? goodColor : badColor);
    }

    public void Clear()
    {
        pts.Clear();
        hasCommitted = false;
        finished = false;
        hasTip = false;
        EnsureLine();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
            ApplyColor(normalColor);
        }
        smoothWidth = baseLineWidth;
    }

    public bool TryGetOverview(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 100f;
        if (pts.Count == 0 && rocketPhysics == null) return false;

        Vector3 min = Vector3.zero, max = Vector3.zero;
        bool any = false;
        void Enc(Vector3 p)
        {
            if (!any) { min = max = p; any = true; }
            else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        }
        Enc(Vector3.zero);
        if (rocketPhysics != null)
        {
            Enc(rocketPhysics.state.position);
            if (rocketPhysics.parameters != null)
                Enc(rocketPhysics.parameters.startPosition);
        }
        int step = Mathf.Max(1, pts.Count / 200);
        for (int i = 0; i < pts.Count; i += step) Enc(pts[i]);
        if (!any) return false;
        center = (min + max) * 0.5f;
        radius = Mathf.Min(1500f, Mathf.Max(80f, (max - min).magnitude * 0.5f, max.y * 0.45f + 50f));
        return true;
    }

    void ApplyColor(Color c)
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.95f);
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(c, 0f),
                new GradientColorKey(Color.Lerp(c, Color.white, 0.1f), 0.5f),
                new GradientColorKey(c, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.95f, 1f)
            });
        lineRenderer.colorGradient = g;
        if (lineRenderer.sharedMaterial != null)
        {
            var mat = lineRenderer.sharedMaterial;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            mat.color = c;
        }
    }
}
