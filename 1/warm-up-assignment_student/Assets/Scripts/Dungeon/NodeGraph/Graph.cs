using System.Collections.Generic;
using UnityEngine;

public class Graph<T>
{
    readonly Dictionary<T, List<T>> adjacencyList = new();
    
    public void Clear() 
    { 
        adjacencyList.Clear(); 
    }
    
    public void RemoveNode(T node)
    {
        // if (adjacencyList.ContainsKey(node))
        // {
        //     adjacencyList.Remove(node);
        // }

        // foreach (var key in adjacencyList.Keys)
        // {
        //     adjacencyList[key].Remove(node);
        // }
        foreach (var connectedNode in adjacencyList[node])
        {
            adjacencyList[connectedNode].Remove(node);
        }
        
        adjacencyList.Remove(node);
    }
    
    public List<T> GetNodes() => new(adjacencyList.Keys);
    
    public void AddNode(T node)
    {
        if (node == null)
        {
            Debug.LogError("Node was NULL");
            return;
        }
        
        if (adjacencyList.ContainsKey(node)) return;
        
        adjacencyList.Add(node, new());
    }
    
    public void AddEdge(T nodeA, T nodeB)
    {
        if (nodeA == null)
        {
            Debug.LogError("The given node (nodeA) was NULL");
            return;
        }
        if (nodeB == null)
        {
            Debug.LogError("The given node (nodeB) was NULL");
            return;
        }
        
        if (!adjacencyList.ContainsKey(nodeA)) AddNode(nodeA);
        if (!adjacencyList.ContainsKey(nodeB)) AddNode(nodeB);
        
        if (!adjacencyList[nodeA].Contains(nodeB)) adjacencyList[nodeA].Add(nodeB);
        if (!adjacencyList[nodeB].Contains(nodeA)) adjacencyList[nodeB].Add(nodeA);
    }
    
    public void RemoveEdge(T nodeA, T nodeB)
    {
        if (nodeA == null)
        {
            Debug.LogError("The given node (nodeA) was NULL");
            return;
        }
        if (nodeB == null)
        {
            Debug.LogError("The given node (nodeB) was NULL");
            return;
        }
        
        if (adjacencyList.ContainsKey(nodeA)) adjacencyList[nodeA].Remove(nodeB);
        if (adjacencyList.ContainsKey(nodeB)) adjacencyList[nodeB].Remove(nodeA);
    }
    
    public List<T> GetNeighbors(T node)
    {
        if (node == null)
        {
            Debug.LogError("Node was NULL");
            return null;
        }
        
        //return new(graph[node]);
        return adjacencyList[node];
    }
    
    public int GetNodeCount()
    {
        return adjacencyList.Count;
    }
    
    public void PrintGraph()
    {
        if (adjacencyList.Count == 0) Debug.Log("NodeGraph is empty.");
        
        foreach (var node in adjacencyList)
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
        return adjacencyList;
    }
    
    // Breadth-First Search (BFS)
    public void BFS(T startNode)
    {
        /* */
    }

    // Depth-First Search (DFS)
    public void DFS(T startNode)
    {
        /* */
    }
}