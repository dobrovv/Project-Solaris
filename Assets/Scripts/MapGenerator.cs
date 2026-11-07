using System.Collections.Generic;
using UnityEngine;
using static Graph;

public class MapGenerator
{

    private const float KEEP_OVERLAP_RADIUS = 1.8f; // Keep radius is 0.5f, so this is a bit more than that to avoid overlapping keeps
    private const float REMOVE_INTERSECTING_EDGES_RADIUS = 0.9f; // Keep radius is 0.5f, so this is a bit more than that to avoid intersecting edges with keeps


    public static Graph Generate(int desiredNumVertices, Vector4 corners, int seed, string generator = "RNG")
    {
        if (generator == "A")
            return GeneratorA(desiredNumVertices, corners, seed);
        else
            return GeneratorRNG(desiredNumVertices, corners, seed);
    }

    public static Vector2 GetRandomPositionWithoutOverlap(Graph graph, float overlapRadius, Vector4 corners, int seed, out bool couldFindNewPosition)
    {

        Vector2 position = new Vector2(Random.Range(corners.x, corners.z), Random.Range(corners.y, corners.w));

        for (int i = 0; i < graph.NumVertices; i++)
        {
            Vector2 existingPosition = graph.GetVertexPositionByIndex(i);

            if (Vector2.Distance(position, existingPosition) < overlapRadius)
            {
                for (int triesCount = 0; triesCount < 100; triesCount++)
                {
                    couldFindNewPosition = true;

                    position = new Vector2(Random.Range(corners.x, corners.z), Random.Range(corners.y, corners.w));
                    for (int j = 0; j < graph.NumVertices; j++)
                    {
                        existingPosition = graph.GetVertexPositionByIndex(j);
                        if (Vector2.Distance(position, existingPosition) < overlapRadius)
                        {
                            couldFindNewPosition = false;
                            break;
                        }
                    }

                    if (couldFindNewPosition)
                    {
                        return position;
                    }

                }

                couldFindNewPosition = false;
                return position;
            }
        }

        couldFindNewPosition = true;
        return position;

    }

    static public List<VertexT> getClosestVerticesByDistance(Graph graph, VertexT which, int closestCount)
    {
        List<VertexT> result = new List<VertexT>(closestCount);
        List<VertexT> vertices = graph.GetVertices();
        vertices.Sort((a, b) => Vector2.Distance(which.position, a.position).CompareTo(Vector2.Distance(which.position, b.position)));

        // Clamp the closestCount 
        if (closestCount > vertices.Count - 1)
        {
            closestCount = vertices.Count - 1;
        }

        // Since the closest vertex is the first element itself on index zero, we skeep it
        for (int i = 1; i < closestCount + 1; i++)
        {
            result.Add(vertices[i]);
        }

        return result;
    }

    static List<EdgeT> getIntersectingEdges(Graph graph)
    {
        List<EdgeT> result = new List<EdgeT>();

        List<EdgeT> edges = graph.GetEdges();

        for (int ie1 = 0; ie1 < edges.Count; ie1++)
        {
            EdgeT e1 = edges[ie1];

            for (int ie2 = ie1 + 1; ie2 < edges.Count; ie2++)
            {
                EdgeT e2 = edges[ie2];

                if (doEdgesIntersect(e1, e2))
                {
                    // Return the longests edge for this algorithm
                    if (e1.weight > e2.weight)
                        result.Add(e1);
                    else
                        result.Add(e2);
                }
            }
        }

        return result;
    }

    static bool doEdgesIntersect(EdgeT e1, EdgeT e2)
    {
        Vector2 interserctionPoint;
        return Intersection2D.TryGetLineIntersection(e1.A.position, e1.B.position, e2.A.position, e2.B.position, out interserctionPoint);
    }

    public static Graph GeneratorA(int desiredNumVertices, Vector4 corners, int seed)
    {
        Graph graph = new Graph(desiredNumVertices);

        Random.InitState(seed);

        // Add vertices with random positions
        for (int i = 0; i < desiredNumVertices; i++)
        {
            bool couldFindNewPosition;
            Vector2 position = GetRandomPositionWithoutOverlap(graph, KEEP_OVERLAP_RADIUS, corners, seed, out couldFindNewPosition);

            if (!couldFindNewPosition)
            {
                Debug.LogWarning("Could not find a non-overlapping position for vertex " + i);
                continue; // Skip adding this vertex if we couldn't find a valid position
            }

            graph.AddVertex(position);
        }

        // Generate edges between every 3 closest vertices
        for (int i = 0; i < graph.NumVertices; i++)
        {
            VertexT v = graph.GetVertexByIndex(i);
            List<VertexT> closest = getClosestVerticesByDistance(graph, v, Random.Range(1, 4));

            for (int j = 0; j < closest.Count; j++)
            {
                if (v == closest[j])
                    Debug.LogError("Same vertex returned");

                graph.AddEdge(v, closest[j]);
            }
        }

        // Remove edges that are intersecting with other edges (graph looks less messy)
        foreach (EdgeT edge in getIntersectingEdges(graph))
        {
            graph.RemoveEdge(edge);
        }


        // Remove edges that are intersecting with other vertices (graph looks less messy)
        foreach (EdgeT edge in graph.GetEdges())
        {
            foreach (VertexT vertex in graph.GetVertices())
            {
                if (vertex != edge.A && vertex != edge.B)
                {
                    if (Intersection2D.HasLineIntersectCircle(edge.A.position, edge.B.position, vertex.position, REMOVE_INTERSECTING_EDGES_RADIUS))
                    {
                        graph.RemoveEdge(edge);
                        //break; // No need to check other vertices for this edge
                    }
                }
            }
        }


        // Generator may return disconected component islands
        // Here connect them based on the two closest vertices in each component
        List<List<VertexT>> components;

        for (; ; )
        {
            components = graph.GetConnectedComponents();

            // Repeat untill graph is completly connected
            if (components.Count < 2)
                break;

            List<VertexT> comp1 = components[0];
            List<VertexT> comp2 = components[1];

            VertexT vc1 = comp1[0];
            VertexT vc2 = comp2[0];
            float minDistance = float.MaxValue;

            //
            for (int i = 0; i < comp1.Count; i++)
            {
                for (int j = 0; j < comp2.Count; j++)
                {
                    float currentDistance = Vector2.Distance(comp1[i].position, comp2[j].position);
                    if (currentDistance < minDistance)
                    {
                        vc1 = comp1[i];
                        vc2 = comp2[j];
                        minDistance = currentDistance;
                    }
                }
            }

            graph.AddEdge(vc1, vc2);

        }

        return graph;
    }


    // Generates the Relative Neighborhood Graph for randomly distributed 'desiredNumVertices'
    public static Graph GeneratorRNG(int desiredNumVertices, Vector4 corners, int seed)
    {
        Graph graph = new Graph(desiredNumVertices);

        Random.InitState(seed);
        // Add vertices with random positions
        for (int i = 0; i < desiredNumVertices; i++)
        {
            bool couldFindNewPosition;
            Vector2 position = GetRandomPositionWithoutOverlap(graph, KEEP_OVERLAP_RADIUS, corners, seed, out couldFindNewPosition);

            if (!couldFindNewPosition)
            {
                Debug.LogWarning("Could not find a non-overlapping position for vertex " + i);
                continue; // Skip adding this vertex if we couldn't find a valid position
            }

            graph.AddVertex(position);
        }

        int n = graph.NumVertices;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                Graph.VertexT p = graph.GetVertexByIndex(i);
                Graph.VertexT q = graph.GetVertexByIndex(j);
                float d_pq_sq = (p.position - q.position).sqrMagnitude;
                bool isNeighbor = true;

                for (int k = 0; k < n; k++)
                {
                    if (k == i || k == j) continue;

                    Graph.VertexT r = graph.GetVertexByIndex(k);
                    double d_pr_sq = (p.position - r.position).sqrMagnitude;
                    double d_qr_sq = (q.position - r.position).sqrMagnitude;

                    // If r is strictly closer to both p and q than they are to each other
                    if (d_pr_sq < d_pq_sq && d_qr_sq < d_pq_sq)
                    {
                        isNeighbor = false;
                        break; // No need to check further, this edge is invalid
                    }
                }

                // If no point r violated the condition, add the edge
                if (isNeighbor)
                {
                    graph.AddEdge(p, q);
                }
            }
        }

        return graph;
    }
}

public class Intersection2D
{

    // Checks if line segment AB intersects with line segment CD.
    // Optionally returns the intersection point.
    public static bool TryGetLineIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersection)
    {
        intersection = new Vector2(0, 0);

        // Direction vectors of the segments
        float rX = b.x - a.x;
        float rY = b.y - a.y;
        float sX = d.x - c.x;
        float sY = d.y - c.y;

        // Vector cross product denominator (r x s)
        float denominator = rX * sY - rY * sX;

        // If denominator is 0, lines are parallel or collinear
        if (System.Math.Abs(denominator) < 1e-9)
        {
            return false;
        }

        // Numerators for the equations: t = (q - p) x s / (r x s) and u = (q - p) x r / (r x s)
        float qpX = c.x - a.x;
        float qpY = c.y - a.y;

        float tNumerator = qpX * sY - qpY * sX;
        float uNumerator = qpX * rY - qpY * rX;

        float t = tNumerator / denominator;
        float u = uNumerator / denominator;

        // For finite line segments, both t and u must be between 0 and 1
        // Too exclude endpoints I decreased the exremities a little - Vlad
        if (t >= 0.01 && t <= 0.99 && u >= 0.01 && u <= 0.99)
        {
            // Calculate the exact intersection point
            intersection = new Vector2(a.x + t * rX, a.y + t * rY);
            return true;
        }

        // Intersect as infinite lines, but not as finite segments
        return false;
    }

    // Check if line segment AB intersects circle with center C and radius R
    public static bool HasLineIntersectCircle(Vector2 lineStart, Vector2 lineEnd, Vector2 circleCenter, float radius)
    {
        Vector2 closestPoint = GetClosestPointOnLineSegment(lineStart, lineEnd, circleCenter);
        float distanceSquared = (closestPoint - circleCenter).sqrMagnitude;
        return distanceSquared <= (radius * radius);
    }

    // Helper method to find the closest point on a line segment
    public static Vector2 GetClosestPointOnLineSegment(Vector2 point1, Vector2 point2, Vector2 targetPoint)
    {
        Vector2 lineDirection = point2 - point1;
        float lineLengthSquared = lineDirection.sqrMagnitude;

        // If the line segment is a single point
        if (lineLengthSquared == 0f) return point1;

        // Find projection of target point onto the line, clamped between 0 and 1
        float t = Vector2.Dot(targetPoint - point1, lineDirection) / lineLengthSquared;
        t = Mathf.Clamp01(t);

        return point1 + t * lineDirection;
    }

}

