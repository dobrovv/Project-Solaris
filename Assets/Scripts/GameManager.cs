using UnityEngine;
using UnityEngine.Events;

public class AIUpdateEvent : UnityEvent { }

public class GameManager : MonoBehaviour
{

    // Global static reference to the active GameManager
    public static GameManager Instance { get; private set; }

    public StarComp selectedStar;
    public Graph graph;

    public float gameTimeScale = 1f;// Default time scale is 1 (normal speed)


    public AIUpdateEvent OnAITick = new AIUpdateEvent();
    [SerializeField] private float AITickInterval = 0.05f;
    private float aiTimer;

    private void Awake()
    {
        // Enforce the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Clear duplicates if another exists
            return;
        }

        Instance = this;
    }

    private void EnableInputController()
    {
        // Enable the appropriate input controller based on the platform
        if (Application.isMobilePlatform) {
            Camera.main.gameObject.GetComponent<CameraControllerPC>().enabled = false;
        }
        else
        {
            //Camera.main.gameObject.GetComponent<CameraControllerMobile>().enabled = false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnableInputController();

        graph = new Graph(50);
        Factions factions = new Factions();
        Faction ai1 = factions.CreateFaction(Factions.AI_1_COLOR);
        CreateMap(ai1);

        AI.Build("Hard", ai1, graph);

        FloatingText.Create("Welcome to the game!", new Vector3(-5, -1, 0), Color.yellow);
    }

    void Update()
    {
        aiTimer += Time.deltaTime * GameManager.Instance.gameTimeScale;

        // Catch-up loop handles frame spikes cleanly
        while (aiTimer >= AITickInterval)
        {
            aiTimer -= AITickInterval;
            OnAITick?.Invoke();
        }
    }

    void CreateMap(Faction ai)
    {
        //// ==========================================
        //// Player starting position
        //// ==========================================

        //Graph.VertexT player = graph.AddVertex(new Vector2(-5, 0));


        //// ==========================================
        //// Left region
        //// ==========================================

        //Graph.VertexT v1 = graph.AddVertex(new Vector2(-3, 2));
        //Graph.VertexT v2 = graph.AddVertex(new Vector2(-3, -2));
        //Graph.VertexT v3 = graph.AddVertex(new Vector2(-1, 0));


        //// ==========================================
        //// Central region
        //// ==========================================

        //Graph.VertexT v4 = graph.AddVertex(new Vector2(1, 2));
        //Graph.VertexT v5 = graph.AddVertex(new Vector2(1, -2));
        //Graph.VertexT v6 = graph.AddVertex(new Vector2(3, 0));


        //// ==========================================
        //// Right region
        //// ==========================================

        //Graph.VertexT v7 = graph.AddVertex(new Vector2(5, 2));
        //Graph.VertexT v8 = graph.AddVertex(new Vector2(5, -2));

        //// ==========================================
        //// Upper and lower branches
        //// ==========================================

        //Graph.VertexT v9 = graph.AddVertex(new Vector2(0, 4));
        //Graph.VertexT v10 = graph.AddVertex(new Vector2(0, -4));
        //Graph.VertexT v11 = graph.AddVertex(new Vector2(4, 4));


        //// ==========================================
        //// Create stars
        //// ==========================================

        //CreateStar(player, StarType.Normal, Factions.Player, 20);

        //CreateStar(v1, StarType.Normal, Factions.Neutral, 10);
        //CreateStar(v2, StarType.WhiteDwarf, Factions.Neutral, 8);
        //CreateStar(v3, StarType.GasGiant, Factions.Neutral, 15);

        //CreateStar(v4, StarType.Normal, Factions.Neutral, 12);
        //CreateStar(v5, StarType.WhiteDwarf, Factions.Neutral, 8);
        //CreateStar(v6, StarType.GasGiant, Factions.Neutral, 20);

        //CreateStar(v7, StarType.Normal, Factions.Neutral, 12);
        //CreateStar(v8, StarType.WhiteDwarf, Factions.Neutral, 8);

        //CreateStar(v9, StarType.GasGiant, Factions.Neutral, 15);
        //CreateStar(v10, StarType.Normal, Factions.Neutral, 10);
        //CreateStar(v11, StarType.GasGiant, ai, 18);

        //// ==========================================
        //// Connections
        //// ==========================================

        //// Player connections
        //CreateLane(graph.AddEdge(player, v1));
        //CreateLane(graph.AddEdge(player, v2));

        //// Left region
        //CreateLane(graph.AddEdge(v1, v2));
        //CreateLane(graph.AddEdge(v1, v3));
        //CreateLane(graph.AddEdge(v2, v3));

        //// Left to center
        //CreateLane(graph.AddEdge(v3, v4));
        //CreateLane(graph.AddEdge(v3, v5));

        //// Central region
        //CreateLane(graph.AddEdge(v4, v5));
        //CreateLane(graph.AddEdge(v4, v6));
        //CreateLane(graph.AddEdge(v5, v6));

        //// Right region
        //CreateLane(graph.AddEdge(v6, v7));
        //CreateLane(graph.AddEdge(v6, v8));
        //CreateLane(graph.AddEdge(v7, v8));

        //// Upper branch
        //CreateLane(graph.AddEdge(v1, v9));
        //CreateLane(graph.AddEdge(v9, v4));
        //CreateLane(graph.AddEdge(v9, v11));
        //CreateLane(graph.AddEdge(v11, v7));

        //// Lower branch
        //CreateLane(graph.AddEdge(v2, v10));
        //CreateLane(graph.AddEdge(v10, v5));

        graph = GameManager.Instance.graph;

        // Star positions
        // Layout is based on the provided image.
        var alpha = graph.AddVertex(new Vector2(-8f, 0f));
        var vega = graph.AddVertex(new Vector2(-6f, 3f));
        var nova = graph.AddVertex(new Vector2(-5.5f, -3.2f));

        var rigel = graph.AddVertex(new Vector2(-2.8f, 1.8f));
        var crown = graph.AddVertex(new Vector2(0f, 4.3f));
        var atlas = graph.AddVertex(new Vector2(2.8f, 1.8f));

        var core = graph.AddVertex(new Vector2(0f, 0f));

        var aegis = graph.AddVertex(new Vector2(-2.8f, -1.8f));
        var bastion = graph.AddVertex(new Vector2(2.8f, -1.8f));
        var abyss = graph.AddVertex(new Vector2(0f, -4.3f));

        var lyra = graph.AddVertex(new Vector2(6f, 3f));
        var orion = graph.AddVertex(new Vector2(8f, 0f));
        var titan = graph.AddVertex(new Vector2(5.5f, -3.2f));

        // Create stars
        CreateStar(alpha, StarType.Normal, Factions.Player, 45);

        CreateStar(vega, StarType.WhiteDwarf, Factions.Neutral, 15);
        CreateStar(nova, StarType.GasGiant, Factions.Neutral, 60);

        CreateStar(rigel, StarType.Normal, Factions.Neutral, 20);
        CreateStar(crown, StarType.GasGiant, Factions.Neutral, 60);
        CreateStar(atlas, StarType.Normal, Factions.Neutral, 20);

        CreateStar(core, StarType.Normal, Factions.Neutral, 20);

        CreateStar(aegis, StarType.Normal, Factions.Neutral, 44);
        CreateStar(bastion, StarType.Normal, Factions.Neutral, 44);
        CreateStar(abyss, StarType.WhiteDwarf, Factions.Neutral, 15);

        CreateStar(lyra, StarType.WhiteDwarf, Factions.Neutral, 15);
        CreateStar(orion, StarType.Normal, ai, 45);
        CreateStar(titan, StarType.GasGiant, Factions.Neutral, 60);

        // Create connections
        CreateConnection(alpha, vega);
        CreateConnection(alpha, nova);

        CreateConnection(vega, rigel);
        CreateConnection(nova, aegis);

        CreateConnection(rigel, crown);
        CreateConnection(rigel, core);

        CreateConnection(crown, core);
        CreateConnection(crown, atlas);

        CreateConnection(atlas, core);
        CreateConnection(atlas, lyra);

        CreateConnection(core, aegis);
        CreateConnection(core, bastion);
        CreateConnection(core, abyss);

        CreateConnection(aegis, abyss);
        CreateConnection(bastion, abyss);

        CreateConnection(bastion, titan);
        CreateConnection(titan, orion);

        CreateConnection(lyra, orion);

    }

    private void CreateConnection(Graph.VertexT a, Graph.VertexT b)
    {
        var edge = graph.AddEdge(a, b);
        CreateLane(edge);
    }

    public void SetSelectedStar(StarComp starOrNull)
    {
        if (selectedStar != null)
        {
            foreach (var edge in graph.GetEdgesFromVertex(selectedStar.Vertex))
            {
                edge.road.SetSolidLine();
                edge.road.getOtherKeep(selectedStar).SetOutline(false);
            }
        }

        selectedStar = starOrNull;

        if (starOrNull == null)
            return;

        foreach ( var edge in graph.GetEdgesFromVertex(selectedStar.Vertex))
        {
            edge.road.SetDottedLine();
            edge.road.getOtherKeep(selectedStar).SetOutline(true);

        }
    }

    private StarComp CreateStar(Graph.VertexT v, StarType type, Faction faction, int initialEnergyCount = 10)
    {
        GameObject keepObj = new GameObject("Star " + v.Index.ToString());
        StarComp star = keepObj.AddComponent<StarComp>();
        star.InitStar(v, type, faction, initialEnergyCount);
        v.keep = star;
        return star;
    }

    private LaneComp CreateLane(Graph.EdgeT edge)
    {
        GameObject roadObj = new GameObject("Lane " + edge.A.Index.ToString() + "-" + edge.B.Index.ToString());
        LaneComp road = roadObj.AddComponent<LaneComp>();
        road.InitLane(edge);
        edge.road = road;
        return road;
    }

    public EnergyComp CreateUnit(StarComp keepOrigin, LaneComp road, float percentageToSend = 1f)
    {
        GameObject unitObj = new GameObject("Dynamic Unit #");
        EnergyComp unit = unitObj.AddComponent<EnergyComp>();
        unit.InitUnit(keepOrigin, road, percentageToSend);
        
        return unit;
    }
}
