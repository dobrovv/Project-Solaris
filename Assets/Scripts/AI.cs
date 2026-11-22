using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public abstract class AI
{
    public Faction faction;
    public Graph graph;

    protected struct Move
    {
        public Graph.VertexT source;
        public Graph.VertexT target;
        public float score;
    }
    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    protected bool IsMine(Graph.VertexT v)
    {
        return v.keep.Faction == faction;
    }

    protected bool IsEnemy(Graph.VertexT v)
    {
        return v.keep.Faction != faction && v.keep.Faction != Factions.Neutral;
    }

    protected int EnergyWithOutgoing(Graph.VertexT v)
    {
        int sum = v.keep.EnergyCount;


        foreach ( var e in graph.GetEdgesFromVertex(v))
        {
            var units = e.road.getUnitsOnRoad();
            foreach (var unit in units)
            {
                if (unit.Origin == v.keep && unit.Faction == v.keep.Faction)
                    sum += unit.EnergyCount;
            }
        }

        return sum;
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

    public bool IsKeepAtBorderWithEnemy(Graph.VertexT keepV)
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
        keeps.RemoveAll(v => !IsKeepAtBorderWithEnemy(v));
        return keeps;
    }


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
        aiThrottleCounter = (aiThrottleCounter + 1) % 12; // Throttle AI actions to every 12th update

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
            var neighbors = graph.GetAdjacentVertices(v);
            if (neighbors.Count == 0)
                return;
            int randomNeighbor = Random.Range(0, neighbors.Count);
            GameManager.Instance.CreateUnit(v.keep, graph.GetEdgeBetweenVertices(v, neighbors[randomNeighbor]).road);
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
    int aiThrottleCounter = 0;
    // ------------------------------------------------------------------
    // Tuning knobs - for normal ai, same as hard
    // ------------------------------------------------------------------
    const int ActionsPerTick = 1;        // how many units Hard may launch per AI tick
    const int Garrison = 1;              // energy a keep always leaves at home
    const float ReinforceWeight = 0.5f;  // share of an enemy's neighbouring energy counted as help for the defender
    const float EnemyValue = 30f;        // base value of capturing an enemy keep
    const float NeutralValue = 10f;      // base value of capturing a neutral keep

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
        aiThrottleCounter = (aiThrottleCounter + 1) % 4; // Throttle AI actions to every 4th update

        if (aiThrottleCounter != 0)
            return;

        var mine = GetKeepsForFaction(faction);

        if (mine.Count == 0)
            return;

        var moves = new List<Move>();
        CollectAttacks(mine, moves);
        CollectReinforcement(mine, moves);

        moves.Sort((a, b) => b.score.CompareTo(a.score));

        var used = new HashSet<Graph.VertexT>();
        int actions = 0;

        foreach (var move in moves)
        {
            if (actions >= ActionsPerTick)
                break;

            // one unit per keep per tick, and never empty a keep completely
            if (used.Contains(move.source) || move.source.keep.EnergyCount - Garrison <= 0)
                continue;

            GameManager.Instance.CreateUnit(move.source.keep, graph.GetEdgeBetweenVertices(move.source, move.target).road);
            used.Add(move.source);
            actions++;
        }

    }

    void CollectAttacks(List<Graph.VertexT> mine, List<Move> moves)
    {
        foreach (var v in mine)
        {
            if (v.keep.EnergyCount <= 1)
                continue;
            foreach (var n in graph.GetAdjacentVertices(v))
            {
                if (n.keep.Faction == faction)
                    continue;
                float advantage = OffenceAgainst(n) - DefenceOf(n);
                if (advantage <= 0)
                    continue;
                float score = advantage + (IsEnemy(n) ? EnemyValue : NeutralValue);
                moves.Add(new Move { source = v, target = n, score = score });
            }
        }
    }

    void CollectReinforcement(List<Graph.VertexT> mine, List<Move> moves)
    {
        var front = new List<Graph.VertexT>();
        
        foreach (var v in mine)
            if (IsKeepAtBorder(v))
                front.Add(v);

        //if (front.Count == 0)
        //    return;
        if (front.Count == 0)
        {
            front.Add(mine[Random.Range(0, mine.Count)]); // fallback to a random keep if no front is found
            front.Add(mine[Random.Range(0, mine.Count)]); // fallback to a random keep if no front is found
            front.Add(mine[Random.Range(0, mine.Count)]); // fallback to a random keep if no front is found
        }

        // distance (in keeps) from the front, walking through our own territory only
        var dist = new Dictionary<Graph.VertexT, int>();
        var queue = new Queue<Graph.VertexT>();

        foreach (var f in front)
        {
            dist[f] = 0;
            queue.Enqueue(f);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var nb in graph.GetAdjacentVertices(current))
            {
                if (!IsMine(nb) || dist.ContainsKey(nb))
                    continue;
                dist[nb] = dist[current] + 1;
                queue.Enqueue(nb);
            }
        }

        foreach (var v in mine)
        {
            int d ;
            if (!dist.TryGetValue(v, out d) || d == 0)
                continue;

            if (v.keep.EnergyCount <= 1)
                continue;

            // step toward the front, preferring closest path to front
            Graph.VertexT best = null;
            int bestnd = int.MaxValue;
            foreach (var nb in graph.GetAdjacentVertices(v))
            {
                int nd;
                if (!IsMine(nb) || !dist.TryGetValue(nb, out nd) || nd >= d)
                    continue;
                if (best == null || nd < bestnd) { 
                    best = nb;
                    bestnd = nd;
                }
            }
            if (best == null)
                continue;
            moves.Add(new Move { source = v, target = best, score = v.keep.EnergyCount });
        }

    }

    // how much a keep can resist: its own energy plus help from allied neighbours (neutrals get no help)
    float DefenceOf(Graph.VertexT n)
    {
        float defence = n.keep.EnergyCount;

        if (!IsMine(n))
        {
            foreach (var nb in graph.GetAdjacentVertices(n))
                if (nb.keep.Faction == n.keep.Faction && nb.keep.Faction != Factions.Neutral)
                    defence += nb.keep.EnergyCount * ReinforceWeight;
        }

        return defence;
    }

    // how much energy we can throw at n from all our keeps touching it
    float OffenceAgainst(Graph.VertexT n)
    {
        float offence = 0f;

        foreach (var nb in graph.GetAdjacentVertices(n))
            if (IsMine(nb))
                offence += Mathf.Max(0, EnergyWithOutgoing(nb) - Garrison);

        return offence;
    }

}

public class AIHard : AI
{
    // ------------------------------------------------------------------
    // Tuning knobs - change these to make Hard easier or harder
    // ------------------------------------------------------------------
    const int ActionsPerTick = 1;        // how many units Hard may launch per AI tick
    const int Garrison = 1;              // energy a keep always leaves at home
    const float AttackMargin = 1f;       // required offence-minus-defence before attacking
    const float ReinforceWeight = 0.5f;  // share of an enemy's neighbouring energy counted as help for the defender
    const float EnemyValue = 30f;        // base value of capturing an enemy keep
    const float NeutralValue = 10f;       // base value of capturing a neutral keep
    const float DefenceBase = 2f;       // base priority of rescuing a threatened keep
    const int NeutralHoldSlack = 11;      // border keeps keep their energy while a neutral is this close to being winnable



    public AIHard(Faction faction, Graph graph)
    {
        this.faction = faction;
        this.graph = graph;

        GameManager.Instance.OnAITick.AddListener(() =>
        {
            DoUpdate();
        });
    }

    // ------------------------------------------------------------------
    // Main loop: collect every useful move, rank them, play the best ones
    // ------------------------------------------------------------------
    override protected void DoUpdate()
    {
        var mine = GetKeepsForFaction(faction);

        if (mine.Count == 0)
            return;

        var moves = new List<Move>();
        CollectAttacks(mine, moves);
        CollectDefense(mine, moves);
        CollectReinforcements(mine, moves);

        moves.Sort((a, b) => b.score.CompareTo(a.score));

        var used = new HashSet<Graph.VertexT>();
        int actions = 0;

        foreach (var move in moves)
        {
            if (actions >= ActionsPerTick)
                break;

            // one unit per keep per tick, and never empty a keep completely
            if (used.Contains(move.source) || Spare(move.source) <= 0)
                continue;

            GameManager.Instance.CreateUnit(move.source.keep, graph.GetEdgeBetweenVertices(move.source, move.target).road);
            used.Add(move.source);
            actions++;
        }
    }

    // ------------------------------------------------------------------
    // 1) Attacks: capture neutral / enemy keeps that we can overpower.
    //    All our keeps next to the target count toward the attack, and the
    //    defender gets credit for help from its own neighbours.
    // ------------------------------------------------------------------
    void CollectAttacks(List<Graph.VertexT> mine, List<Move> moves)
    {
        foreach (var v in mine)
        {
            if (Spare(v) <= 0)
                continue;

            foreach (var n in graph.GetAdjacentVertices(v))
            {
                if (IsMine(n))
                    continue;

                float advantage = OffenceAgainst(n) - DefenceOf(n);
                if (advantage < AttackMargin)
                    continue;

                // hubs (many connections) are worth more because they open more routes
                float value = (IsEnemy(n) ? EnemyValue : NeutralValue) + graph.GetAdjacentVertices(n).Count * 0.5f;

                // don't strip a keep that other enemies are about to hit
                int remaining = v.keep.EnergyCount - 1;
                float exposed = Mathf.Max(0, EnemyEnergyAround(v, n) - remaining);

                float score = value + advantage * 0.5f - n.keep.EnergyCount * 0.3f - exposed * 0.4f;
                moves.Add(new Move { source = v, target = n, score = score });
            }
        }
    }

    // ------------------------------------------------------------------
    // 2) Defence: a keep with more enemy energy next to it than its own
    //    energy gets help from a safer neighbouring keep.
    // ------------------------------------------------------------------
    void CollectDefense(List<Graph.VertexT> mine, List<Move> moves)
    {
        foreach (var k in mine)
        {
            int deficit = EnemyEnergyAround(k) - k.keep.EnergyCount;
            if (deficit <= 0)
                continue;

            foreach (var donor in graph.GetAdjacentVertices(k))
            {
                if (!IsMine(donor) || Spare(donor) <= 0)
                    continue;

                int donorDeficit = EnemyEnergyAround(donor) - donor.keep.EnergyCount;
                if (donorDeficit >= deficit)
                    continue; // donor is in worse shape than the keep it would help

                // Note this scoring pushes attack margin for the Attcking moves up (to around DefenceBase + 8)
                // Since there is almost always a keep undefended by a neighbour, and the AI will prefer to reinforce it instead of attacking.
                float score = DefenceBase + Mathf.Min(deficit, 8) - Mathf.Max(0, donorDeficit) * 0.5f; 
                moves.Add(new Move { source = donor, target = k, score = score });
            }
        }
    }

    // ------------------------------------------------------------------
    // 3) Reinforcement: interior keeps push energy toward the front line.
    //    The front is every keep touching a real enemy; if no enemy is
    //    reachable yet, it is every keep touching neutrals.
    // ------------------------------------------------------------------
    void CollectReinforcements(List<Graph.VertexT> mine, List<Move> moves)
    {
        var front = new List<Graph.VertexT>();

        foreach (var v in mine)
            if (IsKeepAtBorderWithEnemy(v))
                front.Add(v);

        if (front.Count == 0)
        {
            foreach (var v in mine)
                if (IsKeepAtBorder(v))
                    front.Add(v);
        }

        //if (front.Count == 0)
        //    return;
        if (front.Count == 0) { 
            front.Add(mine[Random.Range(0, mine.Count)]); // fallback to a random keep if no front is found
            front.Add(mine[Random.Range(0, mine.Count)]); // fallback to a random keep if no front is found
            front.Add(mine[Random.Range(0, mine.Count)]); // fallback to a random keep if no front is found
        }

        // distance (in keeps) from the front, walking through our own territory only
        var dist = new Dictionary<Graph.VertexT, int>();
        var queue = new Queue<Graph.VertexT>();

        foreach (var f in front)
        {
            dist[f] = 0;
            queue.Enqueue(f);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var nb in graph.GetAdjacentVertices(current))
            {
                if (!IsMine(nb) || dist.ContainsKey(nb))
                    continue;

                dist[nb] = dist[current] + 1;
                queue.Enqueue(nb);
            }
        }

        foreach (var v in mine)
        {
            int d;
            if (!dist.TryGetValue(v, out d) || d == 0)
                continue;

            if (Spare(v) <= 0 || ShouldHold(v))
                continue;

            // step toward the front, preferring the neighbour with the least energy so it spreads out
            Graph.VertexT best = null;

            foreach (var nb in graph.GetAdjacentVertices(v))
            {
                int nd;
                if (!IsMine(nb) || !dist.TryGetValue(nb, out nd) || nd >= d)
                    continue;

                if (best == null || nb.keep.EnergyCount < best.keep.EnergyCount)
                    best = nb;
            }

            if (best == null)
                continue;

            moves.Add(new Move { source = v, target = best, score = 1f + Spare(v) * 0.3f });
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------


    // energy a keep can send without leaving itself empty
    int Spare(Graph.VertexT v)
    {
        return Mathf.Max(0, v.keep.EnergyCount - Garrison);
    }

    // total energy of non-neutral enemy keeps next to v (optionally ignoring one of them)
    int EnemyEnergyAround(Graph.VertexT v, Graph.VertexT exclude = null)
    {
        int sum = 0;

        foreach (var n in graph.GetAdjacentVertices(v))
            if (n != exclude && IsEnemy(n))
                sum += n.keep.EnergyCount;

        return sum;
    }

    // how much a keep can resist: its own energy plus help from allied neighbours (neutrals get no help)
    float DefenceOf(Graph.VertexT n)
    {
        float defence = n.keep.EnergyCount;

        if (!IsMine(n))
        {
            foreach (var nb in graph.GetAdjacentVertices(n))
                if (nb.keep.Faction == n.keep.Faction && nb.keep.Faction != Factions.Neutral)
                    defence += nb.keep.EnergyCount * ReinforceWeight;
        }

        return defence;
    }

    // how much energy we can throw at n from all our keeps touching it
    float OffenceAgainst(Graph.VertexT n)
    {
        float offence = 0f;

        foreach (var nb in graph.GetAdjacentVertices(n))
            if (IsMine(nb))
                //offence += Spare(nb);
                offence += Mathf.Max(0, EnergyWithOutgoing(nb) - Garrison);

        return offence;
    }

    // a border keep waits (instead of sending energy away) while a neutral neighbour is nearly winnable
    bool ShouldHold(Graph.VertexT v)
    {
        foreach (var n in graph.GetAdjacentVertices(v))
            if (n.keep.Faction == Factions.Neutral && n.keep.EnergyCount <= v.keep.EnergyCount + NeutralHoldSlack)
                return true;

        return false;
    }
}