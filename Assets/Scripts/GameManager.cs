using UnityEngine;

public class GameManager : MonoBehaviour
{

    // Global static reference to the active GameManager
    public static GameManager Instance { get; private set; }

    public StarComp selectedStar;
    public Graph graph;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        graph = new Graph(50);
        Graph.VertexT v1 = graph.AddVertex(new Vector2(0, 0));
        Graph.VertexT v2 = graph.AddVertex(new Vector2(2, 2));
        Graph.VertexT v3 = graph.AddVertex(new Vector2(-1f, -2));
        Graph.EdgeT e12 =   graph.AddEdge(v1, v2);
        Graph.EdgeT e13 = graph.AddEdge(v1, v3);

        CreateStar(v1, StarType.Normal, Factions.Player);
        CreateStar(v2, StarType.GasGiant, Factions.Neutral);
        CreateStar(v3, StarType.WhiteDwarf, Factions.Player);
        CreateLane(e12);
        CreateLane(e13);
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
