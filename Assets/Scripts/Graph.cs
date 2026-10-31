
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public class VertexT
    {
        private int index;

        public int Index { get { return index; } }

        public StarComp keep;
        public Vector2 position;

        public VertexT (Vector2 position, int index)
        {
            this.position = position; 
            this.index = index;
            this.keep = null;
        }
    }

    public class EdgeT
    {
        public LaneComp road;
        public float weight;

        private VertexT a;
        private VertexT b;

        public VertexT A { get { return a; } }
        public VertexT B { get { return b; } }

        public EdgeT(float weight, VertexT a, VertexT b)
        {
            this.weight = weight; road = null;
            this.a = a;
            this.b = b;
        }
    }

    public int NumVertices { get { return vertices.Count; } }
    EdgeT[,] Matrix;

    List<VertexT> vertices;

    public Graph(int maxNumVertices)
    {
        
        this.vertices = new List<VertexT>();
        this.Matrix = new EdgeT[maxNumVertices, maxNumVertices];
        
        for (int row = 0; row < maxNumVertices; row++)
        {
            for (int col = 0; col < maxNumVertices; col++)
            {
                Matrix[row, col] = null;
            }
        }
    }

    public Graph.VertexT AddVertex(Vector2 position)
    {
        int index = vertices.Count;
        vertices.Add(new VertexT(position, index));

        return vertices[index];
    }

    public VertexT GetVertexByIndex(int iv)
    {
        if (iv >= NumVertices || iv < 0)
            throw new System.ArgumentOutOfRangeException("Vertex is out of bounds");

        return vertices[iv];
    }

    public Vector2 GetVertexPositionByIndex(int iv)
    {
        return vertices[iv].position;
    }

    public EdgeT AddEdge(VertexT v1, VertexT v2)
    {
        int iv1 = vertices.IndexOf(v1); 
        int iv2 = vertices.IndexOf(v2);

        if (iv1 >= NumVertices || iv2 >= NumVertices || iv1 < 0 || iv2 < 0)
            throw new System.ArgumentOutOfRangeException("Vertices are out of bounds");

        Matrix[iv1, iv2] = new EdgeT(Vector2.Distance(vertices[iv1].position, vertices[iv2].position), vertices[iv1], vertices[iv2]);
        Matrix[iv2, iv1] = Matrix[iv1, iv2];
        
        return Matrix[iv1, iv2];
    }

    public void RemoveEdge(EdgeT e)
    {
        int iv1 = vertices.IndexOf(e.A);
        int iv2 = vertices.IndexOf(e.B);

        if (iv1 >= NumVertices || iv2 >= NumVertices || iv1 < 0 || iv2 < 0)
            throw new System.ArgumentOutOfRangeException("Vertices are out of bounds");

        Matrix[iv1, iv2] = null;
        Matrix[iv2, iv1] = null;
    }


    public List<VertexT> GetVertices()
    {
        List<VertexT> newVertices = new List<VertexT>();
        for (int i = 0; i < NumVertices; i++)
        {
            newVertices.Add(vertices[i]);
        }

        return newVertices;
    }

    public IEnumerable<Vector2> GetVerticesPosition()
    {
        List<Vector2> positions = new List<Vector2>();
        foreach (var v in vertices)
            positions.Add(v.position);
        return positions;
    }

    public List<VertexT> GetAdjacentVertices(VertexT v)
    {
        int iv = vertices.IndexOf(v);
        
        if (iv >= NumVertices || iv < 0)
            throw new System.ArgumentOutOfRangeException("Vertex is out of bounds");
        
        List<VertexT> adjacentVertices = new List<VertexT>();
        for (int i = 0; i < NumVertices; i++)
        {
            if (Matrix[iv,i] != null && Matrix[iv, i].weight > 0)
            {
                adjacentVertices.Add(vertices[i]);
            }
        }
        return adjacentVertices;
    }

    public List<EdgeT> GetEdgesFromVertex(VertexT v)
    {
        int iv = vertices.IndexOf(v);
        
        if (iv >= NumVertices || iv < 0)
            throw new System.ArgumentOutOfRangeException("Vertex is out of bounds");
        
        List<EdgeT> edges = new List<EdgeT>();
        for (int i = 0; i < NumVertices; i++)
        {
            if (iv == i) continue; // Skip self-loop check
            if (Matrix[iv,i] != null && Matrix[iv, i].weight > 0)
            {
                edges.Add(Matrix[iv, i]);
            }
        }
        return edges;
    }



    public List<EdgeT> GetEdges()
    {
        List<EdgeT> edges = new List<EdgeT>();
        
        for (int i = 0; i < NumVertices; i++)
        {
            for (int j = i + 1; j < NumVertices; j++)
            {
                if (Matrix[i, j] != null && Matrix[i, j].weight != 0)
                {
                    edges.Add(Matrix[i, j]);
                }
            }
        }
        return edges;
    }

    public float GetEdgeWeight(int v1, int v2)
    {
        return this.Matrix[v1, v2].weight;
    }

    public EdgeT GetEdgeBetween(VertexT v1, VertexT v2)
    {
        int iv1 = vertices.IndexOf(v1);
        int iv2 = vertices.IndexOf(v2);
        if (iv1 >= NumVertices || iv2 >= NumVertices || iv1 < 0 || iv2 < 0)
            throw new System.ArgumentOutOfRangeException("Vertices are out of bounds");
        return Matrix[iv1, iv2];
    }

    // Returns a list of diconected component islands of the graph
    public List<List<VertexT>> GetConnectedComponents()
    {
        bool[] visited = new bool[NumVertices];
        
        List<List<VertexT>> allComponents = new List<List<VertexT>>();

        for (int i = 0; i < NumVertices; i++)
        {
            if (!visited[i])
            {
                List<VertexT> component = new List<VertexT>();
                Queue<int> queue = new Queue<int>();

                visited[i] = true;
                queue.Enqueue(i);

                while (queue.Count > 0)
                {
                    int curr = queue.Dequeue();
                    component.Add(GetVertexByIndex(curr));

                    foreach (VertexT neighbor in GetAdjacentVertices(GetVertexByIndex(curr)))
                    {
                        if (!visited[neighbor.Index])
                        {
                            visited[neighbor.Index] = true;
                            queue.Enqueue(neighbor.Index);
                        }
                    }
                }

                allComponents.Add(component);
            }
        }

        return allComponents;
    }

}

