using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop-in extensions for your Graph class (no changes to Graph.cs needed).
/// Usage: Graph.VertexT v = graph.GetFurthestVertex(myVertices);
/// </summary>
public static class GraphExtensions
{
    /// <summary>
    /// Returns the vertex that is furthest from the given vertices, measured in straight-line
    /// distance: for every candidate we take its distance to the NEAREST vertex in 'from',
    /// then pick the candidate where that distance is largest.
    /// Vertices in 'from' are never returned. Returns null if there is no candidate.
    /// </summary>
    public static Graph.VertexT GetFurthestVertex(this Graph graph, ICollection<Graph.VertexT> from)
    {
        if (from == null || from.Count == 0) return null;

        Graph.VertexT best = null;
        float bestDistance = -1f;

        foreach (Graph.VertexT candidate in graph.GetVertices())
        {
            if (from.Contains(candidate)) continue;

            float nearest = float.MaxValue;
            foreach (Graph.VertexT source in from)
                nearest = Mathf.Min(nearest, Vector2.Distance(candidate.position, source.position));

            if (nearest > bestDistance)
            {
                bestDistance = nearest;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// Same idea, but distance is measured along the graph's edges (shortest path, using edge weights)
    /// instead of straight-line. Vertices that can't be reached from 'from' are ignored.
    /// Swap 'e.weight' for 1f below to count jumps (hops) instead of distance.
    /// Returns null if nothing is reachable.
    /// </summary>
    public static Graph.VertexT GetFurthestVertexByPath(this Graph graph, ICollection<Graph.VertexT> from)
    {
        if (from == null || from.Count == 0) return null;

        int n = graph.NumVertices;
        float[] dist = new float[n];
        bool[] done = new bool[n];
        for (int i = 0; i < n; i++) dist[i] = float.PositiveInfinity;

        // Multi-source Dijkstra: every vertex in 'from' starts at distance 0.
        foreach (Graph.VertexT source in from)
            dist[source.Index] = 0f;

        for (int step = 0; step < n; step++)
        {
            // Pick the closest unfinished vertex (O(V^2) overall, fine for a few hundred stars).
            int u = -1;
            for (int i = 0; i < n; i++)
                if (!done[i] && (u == -1 || dist[i] < dist[u])) u = i;

            if (u == -1 || float.IsPositiveInfinity(dist[u])) break; // rest is unreachable
            done[u] = true;

            Graph.VertexT current = graph.GetVertexByIndex(u);
            foreach (Graph.EdgeT e in graph.GetEdgesFromVertex(current))
            {
                Graph.VertexT other = (e.A == current) ? e.B : e.A;
                float candidate = dist[u] + e.weight;
                if (candidate < dist[other.Index])
                    dist[other.Index] = candidate;
            }
        }

        // Largest finite distance wins; sources sit at 0 so they are never picked.
        Graph.VertexT best = null;
        float bestDistance = 0f;
        for (int i = 0; i < n; i++)
        {
            if (float.IsPositiveInfinity(dist[i]) || dist[i] <= bestDistance) continue;
            bestDistance = dist[i];
            best = graph.GetVertexByIndex(i);
        }

        return best;
    }
}