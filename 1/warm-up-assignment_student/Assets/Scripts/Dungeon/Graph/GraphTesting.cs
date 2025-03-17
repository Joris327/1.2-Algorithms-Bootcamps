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
        graph.AddNode("G");
        graph.AddNode("H");
        graph.AddNode("I");
        graph.AddNode("J");
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");
        graph.AddEdge("B", "E");
        graph.AddEdge("C", "F");
        graph.AddEdge("C", "G");
        graph.AddEdge("D", "H");
        graph.AddEdge("D", "I");
        graph.AddEdge("E", "J");
        
        //Debug.Log("Graph Structure:");
        //graph.PrintGraph();
        graph.BFS("A");
        graph.DFS("A");
    }
}