using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class AIUpdateEvent : UnityEvent { }

public class GameManager : MonoBehaviour
{

    // Global static reference to the active GameManager
    public static GameManager Instance { get; private set; }

    public StarComp selectedStar;
    public Graph graph;
    public Factions factions;

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
        factions = new Factions();
        Faction ai1 = factions.CreateFaction();
        Faction ai2 = factions.CreateFaction();
        Faction ai3 = factions.CreateFaction();
        //CreateMap02(ai1);
        
        if (MainMenuOptions.GameMode == "Skirmish") { 
            GenerateMap();
        } else
        {
            MapData mapData = MapLoader.LoadMap("map_0"+MainMenuOptions.SelectedMap);
            AI.Build("Normal", ai1, graph);
            AI.Build("Normal", ai2, graph);
            AI.Build("Normal", ai3, graph);
        }




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

        // Detects if the Escape key was pressed this frame
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("MainMenuScene");
            //Application.Quit();
        }
    }

    void GenerateMap()
    {
        int seed = Random.Range(int.MinValue, int.MaxValue);
        Debug.Log("Seed: " + seed);

        // chosee generator randomly
        string generator = Random.Range(0, 2) == 0 ? "A" : "RNG";

        if (MainMenuOptions.MapSize == "Tiny")
        {
            graph = MapGenerator.Generate(8, new Vector4(-3, -3, 3, 3), seed);
        }
        else if (MainMenuOptions.MapSize == "Small")
            graph = MapGenerator.Generate(15, new Vector4(-5, -5, 5, 5), seed, generator);
        else if (MainMenuOptions.MapSize == "Medium")
            graph = MapGenerator.Generate(21, new Vector4(-9, -5, 9, 5), seed, generator);
        else if (MainMenuOptions.MapSize == "Large")
            graph = MapGenerator.Generate(27, new Vector4(-10, -5, 10, 5), seed, generator);

        List<StarComp> keeps = new List<StarComp>();

        // 1. init keeps and based on the graph
        foreach (Graph.VertexT v in graph.GetVertices())
        {
            float selector = Random.Range(0f, 1f);

            StarType type = selector <= 0.6 ? StarType.Normal : selector <= 0.9 ? StarType.WhiteDwarf : StarType.GasGiant;

            CreateStar(v, type, Factions.Neutral, StarTypes.GetStarTypeMaxEnergyRate(type)/3);
            keeps.Add(v.keep);
        }

        // 2. Init roads based on the graph
        foreach (Graph.EdgeT edge in graph.GetEdges())
        {
            CreateLane(edge);
        }

        // Reinitialize start as a player 
        graph.GetVertexByIndex(0).keep.InitStar(graph.GetVertexByIndex(0), StarType.Normal, Factions.Player, 10);

        List<Graph.VertexT> occupiedVertices = new List<Graph.VertexT>();
        
        
        occupiedVertices.Add(graph.GetVertexByIndex(0));

        for (int i=2; i < MainMenuOptions.OpponentsCount+2; i++) { 
            var vAiPlanet = graph.GetFurthestVertexByPath(occupiedVertices);

            vAiPlanet.keep.InitStar(vAiPlanet, StarType.Normal, factions.GetFaction(i), 10);
            
            occupiedVertices.Add(vAiPlanet);
            
            AI.Build(MainMenuOptions.AIDifficulty, factions.GetFaction(i), graph);
        }

        // center camera at the first keep (the player's keep)
        //Camera.main.transform.position = new Vector3(keeps[0].position.x, 50, keeps[0].position.z);
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

    public StarComp CreateStar(Graph.VertexT v, StarType type, Faction faction, int initialEnergyCount = 10)
    {
        GameObject keepObj = new GameObject("Star " + v.Index.ToString());
        StarComp star = keepObj.AddComponent<StarComp>();
        star.InitStar(v, type, faction, initialEnergyCount);
        v.keep = star;
        return star;
    }

    public LaneComp CreateLane(Graph.EdgeT edge)
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
