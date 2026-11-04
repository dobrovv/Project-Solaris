using System.Collections.Generic;
using UnityEngine;

public abstract class AI
{
    public Faction faction;
    public Graph graph;

    abstract protected void DoUpdate();
    public static AI Build(string difficulty, Faction faction, Graph graph)
    {
        if (difficulty == "Easy")
            return new AIEasy(faction, graph);
        else if (difficulty == "Normal")
            return new AINormal(faction, graph);
        else if (difficulty == "Hard")
            return new AIHard(faction, graph);
        else
            throw new System.ArgumentException("Invalid AI difficulty: " + difficulty);
    }

    public List<Graph.VertexT> GetKeepsForFaction(Faction faction)
    {
        var list = graph.GetVertices();
        list.RemoveAll(v => v.keep.Faction != faction);
        return list;
    }

    // Returns true if keep neighbours keeps of different factions including neutrals
    public bool IsKeepAtBorder(Graph.VertexT keepV)
    {
        bool isKeepAtBorder = false;
        List<Graph.VertexT> neighbors = graph.GetAdjacentVertices(keepV);

        foreach (Graph.VertexT neighbor in neighbors)
        {
            if (neighbor.keep.Faction != faction)
            {
                isKeepAtBorder = true;
                break;
            }
        }

        return isKeepAtBorder;
    }

    public bool IsKeepAtBorderWithNonNeutrals(Graph.VertexT keepV)
    {
        bool isKeepAtBorder = false;
        List<Graph.VertexT> neighbors = graph.GetAdjacentVertices(keepV);

        foreach (Graph.VertexT neighbor in neighbors)
        {
            if (neighbor.keep.Faction != faction && neighbor.keep.Faction != Factions.Neutral)
            {
                isKeepAtBorder = true;
                break;
            }
        }

        return isKeepAtBorder;
    }

    // Return a list of keeps bordering another faction including neutral factions
    public List<Graph.VertexT> GetAllKeepsAtBorder()
    {
        var keeps = GetKeepsForFaction(faction);
        keeps.RemoveAll(v => !IsKeepAtBorder(v));
        return keeps;
    }

    // Return a list of keeps bordering at least one non neutral keep
    public List<Graph.VertexT> GetAllKeepsAtBorderStrict()
    {
        var keeps = GetKeepsForFaction(faction);
        keeps.RemoveAll(v => !IsKeepAtBorderWithNonNeutrals(v));
        return keeps;
    }

    public List<Graph.VertexT> FindPathToClosestKeepAtBorder(Graph.VertexT start)
    {
        // Standard FIFO Queue for BFS traversal
        Queue<Graph.VertexT> queue = new Queue<Graph.VertexT>();

        Dictionary<Graph.VertexT, Graph.VertexT> predecessors = new Dictionary<Graph.VertexT, Graph.VertexT>();

        List<Graph.VertexT> borderland = GetAllKeepsAtBorder();

        // Initialize BFS
        queue.Enqueue(start);
        predecessors[start] = default!; // Start node has no parent

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (borderland.Contains(current))
            {
                return ReconstructPath(predecessors, current);
            }

            foreach (Graph.VertexT neighbor in graph.GetAdjacentVertices(current))
            {
                // If the neighbor hasn't been discovered yet
                if (!predecessors.ContainsKey(neighbor))
                {
                    predecessors[neighbor] = current; // Record where we came from
                    queue.Enqueue(neighbor);
                }
            }

        }

        return new List<Graph.VertexT>(); // Return empty list if no path exists

    }

    /// <summary>
    /// Backtracks from the target node back to the start node to construct the path.
    /// </summary>
    private List<Graph.VertexT> ReconstructPath(Dictionary<Graph.VertexT, Graph.VertexT> predecessors, Graph.VertexT target)
    {
        List<Graph.VertexT> path = new List<Graph.VertexT>();
        Graph.VertexT current = target;

        // Trace backward until we hit a key pointing to default/null (the start node)
        while (current != null && predecessors.ContainsKey(current))
        {
            path.Add(current);
            current = predecessors[current];
        }

        path.Reverse(); // Reverse the list to get Start -> Target order
        return path;
    }

    public int getPotentialAttackersEnergyCount(Graph.VertexT targetKeep)
    {
        int potentialAttackers = 0;
        List<Graph.VertexT> neighbors = graph.GetAdjacentVertices(targetKeep);
        foreach (Graph.VertexT neighbor in neighbors)
        {
            if (neighbor.keep.Faction != faction && neighbor.keep.Faction != Factions.Neutral)
            {
                potentialAttackers += neighbor.keep.EnergyCount;
            }
        }
        return potentialAttackers;
    }

}



public class AIEasy : AI
{
    int aiThrottleCounter = 0;

    public AIEasy(Faction faction, Graph graph)
    {
        this.faction = faction;
        this.graph = graph;

        GameManager.Instance.OnAITick.AddListener(() =>
        {
            DoUpdate();
        });

    }

    override protected void DoUpdate()
    {
        aiThrottleCounter = (aiThrottleCounter + 1) % 15; // Throttle AI actions to every 15th update

        if (aiThrottleCounter != 0)
            return;


        //Debug.Log("AI Update for faction: " + faction.color.ToString());

        // AI logic to decide on actions
        // For example, find a keep to attack or reinforce

        var keeps = GetKeepsForFaction(faction);

        if (keeps.Count == 0)
            return;

        int randomIndex = Random.Range(0, keeps.Count);
        Graph.VertexT v = keeps[randomIndex];

        bool isKeepAtBorder = IsKeepAtBorder(v);

        if (!isKeepAtBorder)
        {
            //int randomNeighbor = Random.Range(0, neighbors.Count);
            //GameManager.Instance.createUnit(v.keep, graph.GetEdgeBetween(v, neighbors[randomNeighbor]).road);
        }
        else
        {
            foreach (Graph.VertexT neighbor in graph.GetAdjacentVertices(v))
            {
                if (neighbor.keep.Faction != faction && v.keep.EnergyCount > neighbor.keep.EnergyCount + 1)
                {
                    GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbor).road);
                    break;
                }
            }
        }



    }
}

public class AINormal : AI
{

    public AINormal(Faction faction, Graph graph)
    {
        this.faction = faction;
        this.graph = graph;

        GameManager.Instance.OnAITick.AddListener(() =>
        {
            DoUpdate();
        });

    }

    override protected void DoUpdate()
    {
        //Debug.Log("AI Update for faction: " + faction.color.ToString());

        // AI logic to decide on actions
        // For example, find a keep to attack or reinforce
        var keeps = GetKeepsForFaction(faction);

        if (keeps.Count == 0)
            return;

        int randomIndex = Random.Range(0, keeps.Count);
        Graph.VertexT v = keeps[randomIndex];

        bool isKeepAtBorder = IsKeepAtBorder(v);

        var neighbours = graph.GetAdjacentVertices(v);

        if (!isKeepAtBorder)
        {
            int randomNeighbor = Random.Range(0, neighbours.Count);
            GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbours[randomNeighbor]).road);
        }
        else
        {
            foreach (Graph.VertexT neighbor in neighbours)
            {
                if (neighbor.keep.Faction != faction && v.keep.EnergyCount > neighbor.keep.EnergyCount + 1)
                {
                    GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbor).road);
                    break;
                }
            }
        }


    }
}

public class AIHard : AI
{

    public AIHard(Faction faction, Graph graph)
    {
        this.faction = faction;
        this.graph = graph;

        GameManager.Instance.OnAITick.AddListener(() =>
        {
            DoUpdate();
        });

    }

    override protected void DoUpdate()
    {
        //Debug.Log("AI Update for faction: " + faction.color.ToString());

        // AI logic to decide on actions
        // For example, find a keep to attack or reinforce
        var keeps = GetKeepsForFaction(faction);

        if (keeps.Count == 0)
            return;

        int randomIndex = Random.Range(0, keeps.Count);
        Graph.VertexT v = keeps[randomIndex];

        bool isKeepAtBorder = IsKeepAtBorder(v);

        var neighbours = graph.GetAdjacentVertices(v);

        if (!isKeepAtBorder)
        {
            List<Graph.VertexT> pathToClosetBorderland = FindPathToClosestKeepAtBorder(v);

            if (pathToClosetBorderland.Count > 1)
            {
                var nextKeep = pathToClosetBorderland[1];
                //Debug.Log("AIHard: Moving unit from keep " + v.keep.name + " to closest borderland keep " + nextKeep.keep.name);
                var road = graph.GetEdgeBetweenVertices(v, nextKeep).road;

                    GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, nextKeep).road);
                    //v.keep.autoPointerRoad = road;
                    return;

            }
            else
            {

                int randomNeighbor = Random.Range(0, neighbours.Count);
                GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbours[randomNeighbor]).road);
                return;
            }
        }
        else // keep is at border with another faction or neutrals
        {

            foreach (Graph.VertexT neighbor in neighbours)
            {
                if (neighbor.keep.Faction != faction && v.keep.EnergyCount > getPotentialAttackersEnergyCount(neighbor) + neighbor.keep.EnergyCount + 1)
                {
                    GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbor).road);
                    return;
                }
            }


            bool isKeepAtBorderWithNonNeutrals = IsKeepAtBorderWithNonNeutrals(v);
            if (!isKeepAtBorderWithNonNeutrals)
            {
                // Send units to a neighboring keep to speed up map capture
                foreach (Graph.VertexT neighbor in neighbours)
                {
                    if (neighbor.keep.Faction == faction && v.keep.EnergyCount <= neighbor.keep.EnergyCount)
                    {
                        GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbor).road);
                        return;
                    }
                }
            }
            else
            {
                //v.keep.autoPointerRoad = null;
            }

        }


    }
}
