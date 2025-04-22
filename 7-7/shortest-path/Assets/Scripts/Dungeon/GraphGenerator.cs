using System;
using NaughtyAttributes;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class GraphGenerator : MonoBehaviour
{
    [SerializeField]
    private RectInt dungeonBounds;

    private Graph<Vector3> graph = new Graph<Vector3>();
    
    public GameObject floor;
    
    private void Start()
    {
        GenerateGraph();
    }

    [Button]
    void GenerateGraph()
    {
        graph.Clear();
        
        // Connect neighbors
        for (int x = dungeonBounds.xMin; x < dungeonBounds.xMax; x++)
        {
            for (int y = dungeonBounds.yMin; y < dungeonBounds.yMax; y++)
            {
                Vector3 currentPos = new Vector3(x, 0, y);

                // Cardinal directions (up, down, left, right)
                TryConnectNeighbor(x + 1, y, currentPos);    // Right
                TryConnectNeighbor(x - 1, y, currentPos);    // Left
                TryConnectNeighbor(x, y + 1, currentPos);    // Up
                TryConnectNeighbor(x, y - 1, currentPos);    // Down

                // Diagonal directions
                TryConnectNeighbor(x + 1, y + 1, currentPos); // Top-right
                TryConnectNeighbor(x - 1, y + 1, currentPos); // Top-left
                TryConnectNeighbor(x + 1, y - 1, currentPos); // Bottom-right
                TryConnectNeighbor(x - 1, y - 1, currentPos); // Bottom-left
            }
        }

        floor.transform.position = new Vector3( dungeonBounds.center.x - .5f, -.5f, dungeonBounds.center.y - .5f);
        floor.transform.localScale = new Vector3(dungeonBounds.width, 1, dungeonBounds.height);
    }
    
    
    private void TryConnectNeighbor(int nx, int ny, Vector3 currentPos)
    {
        if (nx >= dungeonBounds.xMin && nx < dungeonBounds.xMax &&
            ny >= dungeonBounds.yMin && ny < dungeonBounds.yMax)
        {
            Vector3 neighborPos = new Vector3(nx, 0, ny);
            graph.AddEdge(currentPos, neighborPos);
        }
    }
    
    private void Update()
    {
        foreach (var node in graph.GetNodes())
        {
            DebugExtension.DebugWireSphere(node, Color.cyan, .2f);
            foreach (var neighbor in graph.GetNeighbors(node))
            {
                Debug.DrawLine(node, neighbor, Color.cyan);
            }
        }
    }

    public Graph<Vector3> GetGraph()
    {
        return graph;
    }
    
}
