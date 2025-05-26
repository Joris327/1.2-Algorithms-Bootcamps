using UnityEngine;

public class GraphTester : MonoBehaviour
{
    //TODO: Declare and initialize a graph variable of type string here

    void Start()
    {
        Debug.Log("TODO: Check & Read GraphTester.Start()");

        Graph<string> graph = new();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        
        graph.AddEdge("B", "D");
        graph.AddEdge("B", "E");
        
        graph.AddEdge("C", "F");
        graph.AddEdge("C", "G");
        
        graph.AddEdge("D", "H");
        graph.AddEdge("D", "I");
        
        graph.AddEdge("E", "J");
        
        graph.DFSRecursive("A");
    }
}
