using TMPro;
using UnityEngine;

public class StarComp : MonoBehaviour
{
    private float diameter = 0.5f;
    
    private float energyCountFloat = 0f;
    private int   energyCount = 0;
    
    private int   maxEnergyCount = 100;

    private StarType starType;
    private Faction faction;
    private Graph.VertexT vertex;
    private Vector3 position;

    public const float POP_INC_INTERVAL = 0.1f; // How often the popCount increases (in seconds)
    public float energyIncCountdown = POP_INC_INTERVAL; // Countdown timer for increasing popCount

    private GameObject quadCircle, quadProgress, quadSelection, quadOutline, quadIcon, quadGlow;

    private TMPro.TextMeshPro energyText;

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

    void Awake()
    {

    }

    void Start()
    {
        CreateCircle();
        CreateProgress();
        CreateIcon();
        CreateText();
        CreateSelection();
        CreateOutline();
        CreateGlow();
       
        UpdateCircle();
        UpdateProgress();
        UpdateText();
        UpdateIcon();
        UpdateSelection();
        UpdateGlow();
    }

    private void Update()
    {
        // 1. Update the energy increase countdown timer
        if (energyIncCountdown <= 0)
        {
            energyIncCountdown = POP_INC_INTERVAL;
            OnEnergyIncCountdown();
        }
        
        // 2. Decrease the countdown timer based on the elapsed time and game time scale
        energyIncCountdown -= Time.deltaTime * GameManager.Instance.gameTimeScale;


        // 1. Rotate the selection quad if this star is selected, otherwise hide it
        if (GameManager.Instance.selectedStar == this)
        {
            quadSelection.SetActive(true);
            quadSelection.transform.Rotate(Vector3.back * 10 * Time.deltaTime);
        }
        else
        {
            quadSelection.SetActive(false);
        }

    }

    public void OnEnergyIncCountdown()
    {
        if (faction == Factions.Neutral)
            return; // Neutral stars do not produce energy

        if (energyCountFloat < maxEnergyCount) { 
            float energyIncrease = StarTypes.GetStarTypeProductionRate(StarType) * POP_INC_INTERVAL;
            SetEnergy(energyCountFloat + energyIncrease); // Increase energy by 1 each interval
        }
        else
        {
            float excess = energyCountFloat - maxEnergyCount;

            // Smoothly reduce the excess energy.
            float dampedExcess = excess * Mathf.Exp(
                -StarTypes.GetStarTypeDampingRate(starType) * POP_INC_INTERVAL
            ) -1 * POP_INC_INTERVAL;

            SetEnergy(Mathf.Max(maxEnergyCount, maxEnergyCount + dampedExcess));
        }
    }

    public void SetEnergy(float energy)
    {
        energyCountFloat = energy;
        energyCount = (int)energyCountFloat;
        UpdateProgress();
        UpdateText();
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
                faction = unit.Faction;
                
                FloatingText.Create("CAPURED",
                    new Vector3(transform.position.x, transform.position.y + diameter + 0.2f, transform.position.z),
                    new Color(252f/255f, 211f/255f, 77f/255f)
                );

                GrowingCircle.CreateStarPulse(position, new Color(252f / 255f, 211f / 255f, 77f / 255f), 0.5f, StarTypes.GetStarTypeRadius(starType)-0.1f);

                UpdateCircle();
                UpdateIcon();
                UpdateGlow();
                UpdateSelection();
                
                GameManager.Instance.graph.GetEdgesFromVertex(vertex).ForEach(edge => edge.Road.SetSolidLine());

            }
            else if (unit.EnergyCount <= energyCountFloat)
            {
                SetEnergy(energyCountFloat - unit.EnergyCount);

                FloatingText.Create("-" + unit.EnergyCount,
                    new Vector3(transform.position.x, transform.position.y + diameter + 0.2f, transform.position.z),
                    unit.Faction.color
                );
            }
        }
    }

    private void CreateCircle()
    {
        // 1. Create a Quad primitive
        quadCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadCircle.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_Disk";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadCircle.transform.SetParent(this.transform);
        quadCircle.transform.localScale = new Vector3(2f, 2f, 1f);
        quadCircle.transform.localPosition = new Vector3(0, 0, 10); // set the z-index to a background layer

        quadRenderer.material.SetFloat("_Diameter", diameter);
        quadRenderer.material.SetFloat("_Thickness", 0.04f);
        //quadRenderer.material.SetColor("_Fill", faction.color/2);
        quadRenderer.material.SetColor("_Fill", Color.black);

    }

    private void CreateProgress()
    {
        // 1. Create a Quad primitive
        quadProgress = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadProgress.name = "ProgressQuad";
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadProgress.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_CircularProgressBar";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadProgress.transform.SetParent(this.transform);
        quadProgress.transform.localPosition = new Vector3(0, 0, 0);
        quadProgress.transform.localScale = new Vector3(2f, 2f, 1f);

        quadRenderer.material.SetFloat("_Diameter", diameter + 0.1f);
        quadRenderer.material.SetFloat("_Thickness", 0.06f);
    }


    private void CreateIcon()
    {
        // 1. Create a Quad primitive
        quadIcon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadIcon.name = "IconQuad";
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadIcon.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_Circle";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadIcon.transform.SetParent(this.transform);
        quadIcon.transform.localScale = new Vector3(1f, 1f, 1f);
        quadIcon.transform.localPosition = new Vector3(0, 0, 1);

        quadRenderer.material.SetFloat("_Diameter", 0.58f);
        quadRenderer.material.SetFloat("_Thickness", 0.014f);
    }

    private void CreateSelection()
    {
        // 1. Create a Quad primitive
        quadSelection = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadSelection.name = "SelectionQuad";
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadSelection.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_CircularDottedOutline";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadSelection.transform.SetParent(this.transform);
        quadSelection.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        quadSelection.transform.localPosition = new Vector3(0, 0, 0);

        quadRenderer.material.SetFloat("_Radius", diameter/2 + 0.1f);
        quadRenderer.material.SetFloat("_Thickness", 0.01f);
        quadRenderer.material.SetFloat("_DotSize", 0.055f);
        quadRenderer.material.SetFloat("_DotCount", 18f);
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    private void CreateOutline()
    {
        // 1. Create a Quad primitive
        quadOutline = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadOutline.name = "OutlineQuad";
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadOutline.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_CircularDottedOutline";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadOutline.transform.SetParent(this.transform);
        quadOutline.transform.localScale = new Vector3(2.1f, 2.1f, 1f);
        quadOutline.transform.localPosition = new Vector3(0, 0, 0);

        quadRenderer.material.SetFloat("_Radius", diameter / 2 + 0.1f);
        quadRenderer.material.SetFloat("_Thickness", 0.01f);
        quadRenderer.material.SetFloat("_DotSize", 0.03f);
        quadRenderer.material.SetFloat("_DotCount", 30f);
        quadRenderer.material.SetColor("_Color", faction.color);
        quadOutline.SetActive(false);

    }

    private void CreateGlow()
    {
        // 1. Create a Quad primitive
        quadGlow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGlow.name = "GlowQuad";
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadGlow.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_StarHalo";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadGlow.transform.SetParent(this.transform);
        quadGlow.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        quadGlow.transform.localPosition = new Vector3(0, 0, 100); // set the z-index to a background layer

        quadRenderer.material.SetFloat("_Radius", 0.5f);
        quadRenderer.material.SetFloat("_Faloff", 0.8f);
        quadRenderer.material.SetFloat("_Intensity", 0.2f);
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    private void CreateText()
    {
        // Create a new GameObject for the text
        GameObject textObject = new GameObject("EnergyText");
        textObject.transform.SetParent(this.transform);
        textObject.transform.localPosition = Vector3.zero; // Center the text
        // Add a TextMeshPro component to the GameObject
        energyText = textObject.AddComponent<TMPro.TextMeshPro>();
        energyText.text = energyCount.ToString();
        energyText.fontSize = 3f;
        energyText.alignment = TMPro.TextAlignmentOptions.Center;
        energyText.color = Color.white;
        energyText.font = Resources.Load<TMP_FontAsset>("Fonts/JetBrainsMono-Regular SDF");
        // Adjust the scale of the text to fit within the circle
        float scaleFactor = 1f; // Adjust this value as needed
        textObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
    }

    public void SetOutline(bool setOutline)
    {
        quadOutline.SetActive(setOutline);
        Renderer quadRenderer = quadOutline.GetComponent<Renderer>();
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    // Call this if you change parameters at runtime
    public void UpdateCircle()
    {
        Renderer quadRenderer = quadCircle.GetComponent<Renderer>();
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    private void UpdateProgress()
    {
        Renderer quadRenderer = quadProgress.GetComponent<Renderer>();
        
        quadRenderer.material.SetColor("_Color", !isOverfilled() ? faction.color : Color.orange);
        quadRenderer.material.SetFloat("_Percentage", Mathf.Clamp01(energyCountFloat / maxEnergyCount));
    }

    private void UpdateSelection()
    {
        Renderer quadRenderer = quadSelection.GetComponent<Renderer>();
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    private void UpdateText()
    {
        energyText.text = energyCount.ToString();
        energyText.color = !isOverfilled() ? Color.white : Color.orange;
    }

    private void UpdateIcon()
    {
        Renderer quadRenderer = quadIcon.GetComponent<Renderer>();
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    private void UpdateGlow()
    {
        Renderer quadRenderer = quadGlow.GetComponent<Renderer>();
        quadRenderer.material.SetColor("_Color", faction.color);
    }

}