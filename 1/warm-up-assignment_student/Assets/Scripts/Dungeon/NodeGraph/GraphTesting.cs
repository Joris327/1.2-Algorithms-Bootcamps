using UnityEngine;

public class GraphTester : MonoBehaviour
{
    void Start()
    {
        Graph<string> graph = new();
        
        graph.AddNode("A");
        graph.AddNode("B");
        graph.AddNode("C");
        graph.AddNode("D");
        graph.AddNode("E");
        graph.AddNode("F");
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");
        graph.AddEdge("C", "D");
        graph.AddEdge("D", "E");
        graph.AddEdge("E", "F");
        
        Debug.Log("Graph Structure:");
        graph.PrintGraph();
    }
}