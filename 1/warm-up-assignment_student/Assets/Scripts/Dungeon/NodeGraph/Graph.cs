using System.Collections.Generic;
using UnityEngine;

public class Graph<T>
{
    readonly Dictionary<T, List<T>> graph = new();
    
    public void AddNode(T node)
    {
        if (node == null)
        {
            Debug.LogError("Node was NULL");
            return;
        }
        
        if (graph.ContainsKey(node)) return;
        
        graph.Add(node, new());
    }
    
    public void RemoveNode(T node)
    {
        foreach (var connectedNode in graph[node])
        {
            graph[connectedNode].Remove(node);
        }
        
        graph.Remove(node);
    }
    
    public void AddEdge(T nodeA, T nodeB)
    {
        if (nodeA == null || nodeB == null)
        {
            Debug.LogError("Node A or B was NULL");
            return;
        }
        
        if (!graph.ContainsKey(nodeA)) return;
        if (!graph.ContainsKey(nodeB)) return;
        
        if (!graph[nodeA].Contains(nodeB)) graph[nodeA].Add(nodeB);
        if (!graph[nodeB].Contains(nodeA)) graph[nodeB].Add(nodeA);
    }
    
    public List<T> GetEdges(T node)
    {
        if (node == null)
        {
            Debug.LogError("Node was NULL");
            return null;
        }
        
        return new(graph[node]);
    }
    
    public void PrintGraph()
    {
        if (graph.Count == 0) Debug.Log("NodeGraph is empty.");
        
        foreach (var node in graph)
        {
            Debug.Log("Key: " + node.Key);
            
            foreach (var edge in node.Value)
            {
                Debug.Log("Value: " + edge);
            }
        }
    }
    
    public Dictionary<T, List<T>> GetGraph()
    {
        return graph;
    }
}