using UnityEngine;

public class EnergyComp : MonoBehaviour
{
    private Faction faction;
    private float unitRadius = 0.075f;
    private int popCount;
    public float speed = 3.3f; // Speed of the unit's movement

    private StarComp originKeep;
    private LaneComp road;

    private Vector3 targetPos;

    private GameObject quadCircle, quadGlow;
    private LineRenderer lineRenderer;


    public StarComp Origin {  get; private set; }

    public Faction Faction
    {
        get { return faction; }
    }

    public int EnergyCount
    {
        get { return popCount; }
    }

    public Vector3 Position
    {
        get { return transform.position; }
    }

    public LaneComp Road
    {
        get { return road; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateCircle(); // Create the visual representation of the unit as a circle
        CreateGlow(); // Create the glow effect for the unit
        CreateLine(); // Create the trailing line behind the unit

        transform.position = GetStarPosition(); // Set the initial position to the origin keep's position
        targetPos = GetEndPosition(); // Set the target position to the other keep's position

        // 1. Rotate the unit to face towards the other keep
        Vector3 direction = road.getOtherKeep(originKeep).Position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    public void InitUnit(StarComp originKeep, LaneComp road, float percentageToSend)
    {
        this.originKeep = originKeep;
        this.road = road;
        this.faction = originKeep.Faction;

        percentageToSend = Mathf.Clamp01(percentageToSend); // Ensure percentage is between 0 and 1
        this.popCount = (int)(originKeep.EnergyCountFloat * percentageToSend); // Initialize popCount from the origin keep's popCount
        originKeep.SetEnergy(originKeep.EnergyCountFloat - popCount); // Decrease the origin keep's popCount by the unit's popCount

        road.addUnitToUnitsOnRoad(this);
    }

    private Vector3 GetStarPosition()
    {
        StarComp targetKeep = road.getOtherKeep(originKeep);
        Vector3 direction = (targetKeep.Position - originKeep.Position).normalized;
        return originKeep.Position + direction * (originKeep.Diameter/2*2); // Adjust the start position based on star radius, todo: times two due to scaling of orignal star diameter by two

    }

    private Vector3 GetEndPosition()
    {
        StarComp targetKeep = road.getOtherKeep(originKeep);
        Vector3 direction = (targetKeep.Position - originKeep.Position).normalized;
        return targetKeep.Position - direction * (targetKeep.Diameter/2*2); // Adjust the position based on the unit's radius and the keep's radius, todo: times two due to scaling of orignal star diameter by two

    }



    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime * GameManager.Instance.gameTimeScale);
        

        if ((targetPos - transform.position).magnitude < 0.01f)
        {
            DoDestinationReaced();
        }

        DoCollisionDetectionWithUnitsOnRoad();
    }

    private void DoCollisionDetectionWithUnitsOnRoad()
    {
        foreach (EnergyComp otherUnit in road.getUnitsOnRoad())
        {
            if (otherUnit == this) continue; // Skip self
            if (otherUnit.originKeep == originKeep) continue; // skip units coming from the same keep
            float distance = Vector3.Distance(transform.position, otherUnit.transform.position);

            if (distance < unitRadius * 2)
            {
                OnUnitCollision(this, otherUnit);
            }
        }
    }

    private void DoDestinationReaced()
    {
        StarComp targetKeep = road.getOtherKeep(originKeep);

        targetKeep.OnEnergyArrived(this);

        deleteUnit(this);// Destroy the unit after reaching the destination
    }

    private void OnUnitCollision(EnergyComp unitA, EnergyComp unitB)
    {
        Debug.Log($"Collision detected between units from {unitA.originKeep.name} and {unitB.originKeep.name}");
        if (unitA.faction == unitB.faction)
        {
            // Same faction, do nothing or implement merging logic if needed
            return;
        }
        else
        {
            if (unitA.popCount > unitB.popCount)
            {
                unitA.popCount -= unitB.popCount;
                unitB.popCount = 0;

                deleteUnit(unitB);
                GrowingCircle.CreateEnergyPulse(unitB.Position, unitB.Faction.color, 0.7f, 0.3f);
                GrowingDisk.CreateEnergyDisk(unitB.Position, unitB.Faction.color, 0.7f, 0.3f);
                //createUnitExplosion(unitA);
                //GameManager.Instance.PlayUnitCaptureSound();
            }
            else if (unitA.popCount < unitB.popCount)
            {
                unitB.popCount -= unitA.popCount;
                unitA.popCount = 0;

                deleteUnit(unitA);
                GrowingCircle.CreateEnergyPulse(unitA.Position, unitA.Faction.color, 0.7f, 0.3f);
                GrowingDisk.CreateEnergyDisk(unitA.Position, unitA.Faction.color, 0.7f, 0.3f);

                //createUnitExplosion(unitB);
                //GameManager.Instance.PlayUnitCaptureSound();
            }
            else // Equal popCounts
            {
                unitA.popCount = 0;
                unitB.popCount = 0;

                deleteUnit(unitA);
                deleteUnit(unitB);

                GrowingCircle.CreateEnergyPulse(unitA.Position, unitA.Faction.color, 0.7f, 0.3f);
                GrowingDisk.CreateEnergyDisk(unitA.Position, unitA.Faction.color, 0.7f, 0.3f);
                //createUnitExplosion(unitA);
                //GameManager.Instance.PlayUnitCaptureSound();
            }
        }
    }

    public void deleteUnit(EnergyComp unit)
    {
        unit.road.removeUnitFromUnitsOnRoad(unit);
        Destroy(unit.gameObject);
    }

    void CreateCircle()
    {
        // 1. Create a Quad primitive
        quadCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadCircle.name = "UnitCircle";
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
        quadCircle.transform.localScale = new Vector3(1f, 1f, 1f);
        quadCircle.transform.localPosition = new Vector3(0, 0, 0);

        quadRenderer.material.SetFloat("_Diameter", unitRadius*2);
        quadRenderer.material.SetFloat("_Thickness", 0.04f);
        quadRenderer.material.SetColor("_Fill", faction.color);
        quadRenderer.material.SetColor("_Color", faction.color*1.1f);
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

        quadRenderer.material.SetFloat("_Radius", 0.15f);
        //quadRenderer.material.SetFloat("_Faloff", 0.6f);
        //quadRenderer.material.SetFloat("_Intensity", 0.3f);
        quadRenderer.material.SetFloat("_Faloff", 2.16f);
        quadRenderer.material.SetFloat("_Intensity", 1.6f);
        quadRenderer.material.SetColor("_Color", faction.color);
    }

    void CreateLine()
    {
        lineRenderer = this.gameObject.AddComponent<LineRenderer>();
        // set z index to be under the stars
        gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, 101);
        lineRenderer.textureMode = LineTextureMode.Tile; // Optional: Set texture mode to Tile for repeating textures

        Material solidRoadMat = Resources.Load("Shaders/Shader Graphs_SolidLine", typeof(Material)) as Material;

        if (solidRoadMat == null)
            Debug.LogError("Material not found in Resources folder!");

        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;

        lineRenderer.material = solidRoadMat;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, new Vector3(-0.4f,0));
        lineRenderer.material.SetFloat("_Height", 0.1f);
        lineRenderer.material.SetColor("_Color", faction.color);
        lineRenderer.endColor = new Color(faction.color.r, faction.color.g, faction.color.b, 0f); // Set the end color to be transparent
    }
}


