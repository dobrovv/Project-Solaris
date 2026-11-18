using TMPro;
using UnityEngine;

public class StarComp : MonoBehaviour
{
    // ───────────────────────── Visual tuning ─────────────────────────
    private const float APPEAR_DURATION = 0.7f;   // seconds for the spawn pop-in
    private const float APPEAR_MAX_DELAY = 0.5f;   // random stagger so stars don't all pop at once
    private const float FILL_SMOOTHING = 10f;    // higher = progress ring catches up faster
    private const float COLOR_SMOOTHING = 7f;     // higher = faction colour changes faster
    private const float FILL_TINT = 0.14f;  // how much faction colour tints the dark disk (0 = pure black)
    private const float GLOW_INTENSITY = 0.2f;   // resting halo intensity (same as before)
    private const float GLOW_BREATH = 0.25f;  // halo breathes +-25%
    private const float GLOW_BREATH_SPEED = 1.4f;   // radians per second
    private const float WARN_PULSE_SPEED = 6f;     // overfilled warning pulse
    private const float SELECT_ROT_SPEED = 14f;    // degrees per second
    private const float OUTLINE_ROT_SPEED = 9f;     // degrees per second (counter-rotates)
    private const float BASE_FONT_SIZE = 3f;

    // Base scales of each quad (same values as before)
    private const float CIRCLE_SCALE = 2f;
    private const float PROGRESS_SCALE = 2f;
    private const float ICON_SCALE = 1f;
    private const float SELECTION_SCALE = 2.2f;
    private const float OUTLINE_SCALE = 2.1f;
    private const float GLOW_SCALE = 2.5f;

    // Shader property IDs (cheaper than strings every frame)
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int DiameterId = Shader.PropertyToID("_Diameter");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
    private static readonly int PercentageId = Shader.PropertyToID("_Percentage");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int DotSizeId = Shader.PropertyToID("_DotSize");
    private static readonly int DotCountId = Shader.PropertyToID("_DotCount");
    private static readonly int FaloffId = Shader.PropertyToID("_Faloff");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    // ───────────────────────── Game state ─────────────────────────
    private float diameter = 0.5f;

    private float energyCountFloat = 0f;
    private int energyCount = 0;

    private int maxEnergyCount = 100;

    private StarType starType;
    private Faction faction;
    private Graph.VertexT vertex;
    private Vector3 position;

    public const float POP_INC_INTERVAL = 0.1f; // How often the popCount increases (in seconds)
    public float energyIncCountdown = POP_INC_INTERVAL; // Countdown timer for increasing popCount

    // ───────────────────────── Visual state ─────────────────────────
    private GameObject quadCircle, quadProgress, quadSelection, quadOutline, quadIcon, quadGlow;
    private Material matCircle, matProgress, matSelection, matOutline, matIcon, matGlow;
    private TMPro.TextMeshPro energyText;

    private bool visualsReady;
    private Color displayColor;            // smoothly follows faction.color
    private float displayFill;             // smoothly follows energy / max
    private float appearTimer, appearT, lastAppear = -1f;
    private float glowPhase;
    private float flash;                   // 0..1, brief halo flash on events
    private float punch;                   // 0..1, brief text pop on events
    private float selectBlend;             // 0..1, selection ring pop in/out
    private bool wasOverfilled;
    private int shownEnergy;

    public void InitStar(Graph.VertexT v, StarType type, Faction thisFaction, int initialEnergyCount)
    {
        energyCountFloat = initialEnergyCount;
        energyCount = (int)energyCountFloat;

        faction = thisFaction;
        vertex = v;
        position = new Vector3(v.position.x, v.position.y, 1f);
        starType = type;
        diameter = StarTypes.GetStarTypeRadius(type); // Set diameter based on star type, times 2 due to later scaling
        maxEnergyCount = StarTypes.GetStarTypeMaxEnergyRate(type);

        transform.position = position;
    }

    void Start()
    {
        displayColor = faction.color;
        displayFill = 0f;                                  // ring fills up during the spawn animation
        appearTimer = -Random.Range(0f, APPEAR_MAX_DELAY); // negative = waiting before pop-in
        glowPhase = Random.value * Mathf.PI * 2f;          // every star breathes out of sync

        CreateCircle();
        CreateProgress();
        CreateIcon();
        CreateText();
        CreateSelection();
        CreateOutline();
        CreateGlow();

        ApplyFactionColor();
        visualsReady = true;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Update the energy increase countdown timer
        if (energyIncCountdown <= 0)
        {
            energyIncCountdown = POP_INC_INTERVAL;
            OnEnergyIncCountdown();
        }

        // 2. Decrease the countdown timer based on the elapsed time and game time scale
        energyIncCountdown -= dt * GameManager.Instance.gameTimeScale;

        // 3. Purely visual animation
        if (visualsReady)
            AnimateVisuals(dt);
    }

    // ───────────────────────── Animation ─────────────────────────
    private void AnimateVisuals(float dt)
    {
        float time = Time.time;
        bool over = isOverfilled();

        // Spawn pop-in (eased with a little overshoot)
        if (appearT < 1f)
        {
            appearTimer += dt;
            appearT = Mathf.Clamp01(appearTimer / APPEAR_DURATION);
        }
        float appear = EaseOutBack(appearT);

        // One-shot effects fade out
        flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);
        punch = Mathf.MoveTowards(punch, 0f, dt * 4f);

        UpdateColorBlend();
        UpdateFillBlend(dt);

        // Static parts only need rescaling while the pop-in is running
        if (!Mathf.Approximately(appear, lastAppear))
        {
            lastAppear = appear;
            SetScale(quadCircle, CIRCLE_SCALE * appear);
            SetScale(quadProgress, PROGRESS_SCALE * appear);
        }

        // Halo: gentle breathing, brighter on events and when selected, faster when overfilled
        glowPhase += dt * GLOW_BREATH_SPEED * (over ? 3f : 1f);
        float breath = Mathf.Sin(glowPhase);
        matGlow.SetFloat(IntensityId, GLOW_INTENSITY * (1f + GLOW_BREATH * breath) + flash * 0.35f + selectBlend * 0.1f);
        SetScale(quadGlow, GLOW_SCALE * appear * (1f + 0.03f * breath + flash * 0.12f));

        // Icon ring: very subtle breathing
        SetScale(quadIcon, ICON_SCALE * appear * (1f + 0.02f * breath));

        // Energy number pops when something happens
        float textScale = appear * (1f + 0.3f * punch * punch);
        energyText.transform.localScale = new Vector3(textScale, textScale, 1f);

        // Overfilled warning: ring and number pulse between orange and a warm yellow
        if (over)
        {
            float w = 0.5f + 0.5f * Mathf.Sin(time * WARN_PULSE_SPEED);
            Color warn = Color.Lerp(Color.orange, new Color(1f, 0.9f, 0.55f), w);
            matProgress.SetColor(ColorId, warn);
            energyText.color = warn;
        }
        else if (wasOverfilled)
        {
            matProgress.SetColor(ColorId, displayColor);
            energyText.color = Color.white;
        }
        wasOverfilled = over;

        // Selection ring: pops in/out and keeps rotating while visible
        bool selected = GameManager.Instance.selectedStar == this;
        selectBlend = Mathf.MoveTowards(selectBlend, selected ? 1f : 0f, dt * 6f);
        bool showSelection = selectBlend > 0.001f;
        if (quadSelection.activeSelf != showSelection)
            quadSelection.SetActive(showSelection);
        if (showSelection)
        {
            quadSelection.transform.Rotate(Vector3.back * SELECT_ROT_SPEED * dt);
            SetScale(quadSelection, SELECTION_SCALE * appear * EaseOutBack(selectBlend) * (1f + 0.015f * Mathf.Sin(time * 3f)));
        }

        // Outline (toggled from outside via SetOutline): counter-rotates and pulses slightly
        if (quadOutline.activeSelf)
        {
            quadOutline.transform.Rotate(Vector3.forward * OUTLINE_ROT_SPEED * dt);
            SetScale(quadOutline, OUTLINE_SCALE * appear * (1f + 0.02f * Mathf.Sin(time * 4f)));
        }
    }

    // Faction colour glides to its new value (e.g. after a capture) instead of snapping
    private void UpdateColorBlend()
    {
        Color target = faction.color;
        if (displayColor == target) return;

        displayColor = Color.Lerp(displayColor, target, 1f - Mathf.Exp(-COLOR_SMOOTHING * Time.deltaTime));
        float dist = Mathf.Abs(displayColor.r - target.r) + Mathf.Abs(displayColor.g - target.g) + Mathf.Abs(displayColor.b - target.b);
        if (dist < 0.01f) displayColor = target;

        ApplyFactionColor();
    }

    // Progress ring glides toward the real value instead of jumping in steps
    private void UpdateFillBlend(float dt)
    {
        float target = Mathf.Clamp01(energyCountFloat / maxEnergyCount);
        if (Mathf.Approximately(displayFill, target)) return;

        displayFill = Mathf.Lerp(displayFill, target, 1f - Mathf.Exp(-FILL_SMOOTHING * dt));
        if (Mathf.Abs(displayFill - target) < 0.0005f) displayFill = target;

        matProgress.SetFloat(PercentageId, displayFill);
    }

    private void ApplyFactionColor()
    {
        matCircle.SetColor(ColorId, displayColor);
        matCircle.SetColor(FillId, DiskFill(displayColor));   // dark disk with a hint of faction colour
        if (!isOverfilled()) matProgress.SetColor(ColorId, displayColor);
        matIcon.SetColor(ColorId, displayColor);
        matGlow.SetColor(ColorId, displayColor);
        matSelection.SetColor(ColorId, displayColor);
        matOutline.SetColor(ColorId, displayColor);
    }

    private static Color DiskFill(Color c)
    {
        return new Color(c.r * FILL_TINT, c.g * FILL_TINT, c.b * FILL_TINT, 1f);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private static void SetScale(GameObject go, float s)
    {
        go.transform.localScale = new Vector3(s, s, 1f);
    }

    private void Kick(float flashAmount, float punchAmount)
    {
        flash = Mathf.Max(flash, flashAmount);
        punch = Mathf.Max(punch, punchAmount);
    }

    // ───────────────────────── Game logic ─────────────────────────
    public void OnEnergyIncCountdown()
    {
        if (faction == Factions.Neutral)
            return; // Neutral stars do not produce energy

        if (energyCountFloat < maxEnergyCount)
        {
            float energyIncrease = StarTypes.GetStarTypeProductionRate(StarType) * POP_INC_INTERVAL;
            SetEnergy(energyCountFloat + energyIncrease); // Increase energy by 1 each interval
        }
        else
        {
            float excess = energyCountFloat - maxEnergyCount;

            // Smoothly reduce the excess energy.
            float dampedExcess = excess * Mathf.Exp(
                -StarTypes.GetStarTypeDampingRate(starType) * POP_INC_INTERVAL
            ) - 1 * POP_INC_INTERVAL;

            SetEnergy(Mathf.Max(maxEnergyCount, maxEnergyCount + dampedExcess));
        }
    }

    public void SetEnergy(float energy)
    {
        energyCountFloat = energy;
        energyCount = (int)energyCountFloat;
        UpdateText();   // the progress ring follows energyCountFloat on its own in Update
    }

    public StarType StarType { get { return starType; } }
    public Graph.VertexT Vertex { get { return vertex; } }

    public Vector3 Position
    {
        get { return position; }
    }

    public Faction Faction
    {
        get { return faction; }
    }

    public int EnergyCount
    {
        get { return energyCount; }
    }

    public float EnergyCountFloat
    {
        get { return energyCountFloat; }
    }

    public float Diameter
    {
        get { return diameter; }
    }

    public bool isOverfilled()
    {
        return energyCount > maxEnergyCount;
    }

    public void OnEnergyArrived(EnergyComp unit)
    {
        if (unit.Faction == this.Faction)
        {
            SetEnergy(energyCountFloat + unit.EnergyCount);
            Kick(0.5f, 0.8f);

            FloatingText.Create("+" + unit.EnergyCount,
                new Vector3(transform.position.x, transform.position.y + diameter + 0.2f, transform.position.z),
                faction.color
            );
        }
        else
        {
            if (unit.EnergyCount > energyCountFloat)
            {
                SetEnergy(unit.EnergyCount - energyCountFloat);
                faction = unit.Faction;   // colours glide to the new faction automatically
                Kick(1f, 1f);

                FloatingText.Create("CAPTURED",
                    new Vector3(transform.position.x, transform.position.y + diameter + 0.2f, transform.position.z),
                    new Color(252f / 255f, 211f / 255f, 77f / 255f)
                );

                GrowingCircle.CreateStarPulse(position, new Color(252f / 255f, 211f / 255f, 77f / 255f), 0.5f, StarTypes.GetStarTypeRadius(starType) - 0.1f);

                GameManager.Instance.graph.GetEdgesFromVertex(vertex).ForEach(edge => edge.Road.SetSolidLine());
                
                if (GameManager.Instance.selectedStar == this)
                {
                    GameManager.Instance.SetSelectedStar(null);
                }
            }
            else if (unit.EnergyCount <= energyCountFloat)
            {
                SetEnergy(energyCountFloat - unit.EnergyCount);
                Kick(0.7f, 1f);

                FloatingText.Create("-" + unit.EnergyCount,
                    new Vector3(transform.position.x, transform.position.y + diameter + 0.2f, transform.position.z),
                    unit.Faction.color
                );
            }
        }
    }

    // ───────────────────────── Visual construction ─────────────────────────
    // Creates a quad with its own material instance (kept so we can animate it and clean it up).
    private GameObject CreateQuad(string objectName, string materialPath, float scale, float z, out Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = objectName;

        Renderer quadRenderer = go.GetComponent<Renderer>();
        Material shared = Resources.Load<Material>(materialPath);
        if (shared != null)
        {
            material = new Material(shared);
            quadRenderer.sharedMaterial = material;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
            material = quadRenderer.material;
        }

        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        return go;
    }

    private void CreateCircle()
    {
        quadCircle = CreateQuad("CircleQuad", "Shaders/Shader Graphs_Disk", CIRCLE_SCALE, 10f, out matCircle); // z = background layer
        matCircle.SetFloat(DiameterId, diameter);
        matCircle.SetFloat(ThicknessId, 0.04f);
    }

    private void CreateProgress()
    {
        quadProgress = CreateQuad("ProgressQuad", "Shaders/Shader Graphs_CircularProgressBar", PROGRESS_SCALE, 0f, out matProgress);
        matProgress.SetFloat(DiameterId, diameter + 0.1f);
        matProgress.SetFloat(ThicknessId, 0.06f);
        matProgress.SetFloat(PercentageId, 0f);
    }

    private void CreateIcon()
    {
        quadIcon = CreateQuad("IconQuad", "Shaders/Shader Graphs_Circle", ICON_SCALE, 1f, out matIcon);
        matIcon.SetFloat(DiameterId, 0.58f);
        matIcon.SetFloat(ThicknessId, 0.014f);
    }

    private void CreateSelection()
    {
        quadSelection = CreateQuad("SelectionQuad", "Shaders/Shader Graphs_CircularDottedOutline", SELECTION_SCALE, 0f, out matSelection);
        matSelection.SetFloat(RadiusId, diameter / 2 + 0.1f);
        matSelection.SetFloat(ThicknessId, 0.01f);
        matSelection.SetFloat(DotSizeId, 0.055f);
        matSelection.SetFloat(DotCountId, 18f);
        quadSelection.SetActive(false);
    }

    private void CreateOutline()
    {
        quadOutline = CreateQuad("OutlineQuad", "Shaders/Shader Graphs_CircularDottedOutline", OUTLINE_SCALE, 0f, out matOutline);
        matOutline.SetFloat(RadiusId, diameter / 2 + 0.1f);
        matOutline.SetFloat(ThicknessId, 0.01f);
        matOutline.SetFloat(DotSizeId, 0.03f);
        matOutline.SetFloat(DotCountId, 30f);
        quadOutline.SetActive(false);
    }

    private void CreateGlow()
    {
        quadGlow = CreateQuad("GlowQuad", "Shaders/Shader Graphs_StarHalo", GLOW_SCALE, 100f, out matGlow); // z = background layer
        matGlow.SetFloat(RadiusId, 0.5f);
        matGlow.SetFloat(FaloffId, 0.8f);
        matGlow.SetFloat(IntensityId, GLOW_INTENSITY);
    }

    private void CreateText()
    {
        GameObject textObject = new GameObject("EnergyText");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = Vector3.zero; // Center the text

        energyText = textObject.AddComponent<TMPro.TextMeshPro>();
        energyText.font = Resources.Load<TMP_FontAsset>("Fonts/JetBrainsMono-Regular SDF");
        energyText.fontSize = BASE_FONT_SIZE * Mathf.Clamp(diameter / 0.5f, 0.85f, 1.3f); // bigger stars get slightly bigger numbers
        energyText.alignment = TMPro.TextAlignmentOptions.Center;
        energyText.color = Color.white;

        shownEnergy = energyCount;
        energyText.SetText("{0}", energyCount);
    }

    // ───────────────────────── Public visual hooks ─────────────────────────
    public void SetOutline(bool setOutline)
    {
        quadOutline.SetActive(setOutline);
        matOutline.SetColor(ColorId, displayColor);
    }

    // Snaps all faction-coloured parts to the current faction colour (no glide)
    public void UpdateCircle()
    {
        if (!visualsReady) return;
        displayColor = faction.color;
        ApplyFactionColor();
    }

    // Only touches the text mesh when the displayed number actually changes (no per-tick allocations)
    private void UpdateText()
    {
        if (energyText == null || energyCount == shownEnergy) return;
        shownEnergy = energyCount;
        energyText.SetText("{0}", energyCount);
    }

    private void OnDestroy()
    {
        // Material instances created in CreateQuad are ours to clean up
        if (matCircle != null) Destroy(matCircle);
        if (matProgress != null) Destroy(matProgress);
        if (matIcon != null) Destroy(matIcon);
        if (matSelection != null) Destroy(matSelection);
        if (matOutline != null) Destroy(matOutline);
        if (matGlow != null) Destroy(matGlow);
    }
}