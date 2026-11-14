using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class MapData
{
    public string mapName;
    public StarData[] stars;
    public ConnectionData[] connections;
}

[Serializable]
public class StarData
{
    public string name;
    public float x;
    public float y;
    public int population;
    public string type;
    public string faction;
}

[Serializable]
public class ConnectionData
{
    public int from;
    public int to;
}


public static class ExtensionMethods
{
    // Allows us to natively 'await' Unity's AsyncOperation types
    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOp.completed += obj => tcs.SetResult(null);
        return ((Task)tcs.Task).GetAwaiter();
    }
}

public class MapLoader
{
    public static MapData LoadMap(string fileName)
    {

        TextAsset jsonFile = Resources.Load<TextAsset>("Maps/"+ fileName);

        if (jsonFile != null)
        {
            MapData mapData = JsonUtility.FromJson<MapData>(jsonFile.text);
            CreateMap(mapData);
            return mapData;
        }
        else
        {
            Debug.LogError("Failed to load map:" + fileName);
            return null;
        }
    }

    private static void CreateMap(MapData map)
    {
        Debug.Log("Loading map: " + map.mapName);

        // Create stars
        foreach (StarData star in map.stars)
        {
            Vector2 position = new Vector2(star.x, star.y);

            StarType type = (StarType)System.Enum.Parse(
                typeof(StarType), star.type
            );

            Faction faction = ParseFactionString(star.faction);

            Debug.Log($"{star.name}: {star.population}, {type}, {faction}");

            // Use your existing star creation method here.
            // Example:
            var v = GameManager.Instance.graph.AddVertex(new Vector2(star.x, star.y));
            GameManager.Instance.CreateStar(v, type, faction, star.population);
        }

        // Create connections
        foreach (ConnectionData connection in map.connections)
        {
            Debug.Log($"Connect {connection.from} to {connection.to}");

            Graph graph = GameManager.Instance.graph;

            // Use your graph's edge creation method here.
            var e = graph.AddEdge(graph.GetVertexByIndex(connection.from), graph.GetVertexByIndex(connection.to));
            GameManager.Instance.CreateLane(e);
        }
    }

    static Faction ParseFactionString(string faction)
    {
        if (string.IsNullOrEmpty(faction)) { 
            Debug.LogError("faction is null");
            return null;
        }

        if (faction == "Neutral")
            return Factions.Neutral;

        if (faction == "Player")
            return Factions.Player;

        if (faction.StartsWith("AI_"))
        {
            string numberPart = faction.Substring(3); // Skips "AI_"
            if (int.TryParse(numberPart, out int x))
            {
                return GameManager.Instance.factions.GetFaction(x + 1);// x now holds the integer value, e.g., 123
            } else
            {
                Debug.LogError("faction id is invalid: "+numberPart);
                return null;
            }
        }
        Debug.LogError("faction string is invalid: " + faction);
        return null;
    }
}