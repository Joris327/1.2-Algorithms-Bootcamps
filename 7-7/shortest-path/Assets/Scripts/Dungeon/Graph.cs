using System.Collections.Generic;
using UnityEngine;

public class Graph<T>
{
    private Dictionary<T, List<T>> adjacencyList;

    public Graph()
    {
        adjacencyList = new Dictionary<T, List<T>>();
    }
    
    public void Clear() 
    { 
        adjacencyList.Clear(); 
    }
    
    public void RemoveNode(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            adjacencyList.Remove(node);
        }
        
        foreach (var key in adjacencyList.Keys)
        {
            adjacencyList[key].Remove(node);
        }
    }
    
    public List<T> GetNodes()
    {
        return new List<T>(adjacencyList.Keys);
    }
    
    public void AddNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<T>();
        }
    }

    public void RemoveEdge(T fromNode, T toNode)
    {
        if (adjacencyList.ContainsKey(fromNode))
        {
            adjacencyList[fromNode].Remove(toNode);
        }
        if (adjacencyList.ContainsKey(toNode))
        {
            adjacencyList[toNode].Remove(fromNode);
        }
    }

    public void AddEdge(T fromNode, T toNode) { 
        if (!adjacencyList.ContainsKey(fromNode))
        {
            AddNode(fromNode);
        }
        if (!adjacencyList.ContainsKey(toNode)) { 
            AddNode(toNode);
        } 
        
        adjacencyList[fromNode].Add(toNode); 
        adjacencyList[toNode].Add(fromNode); 
    } 
    
    public List<T> GetNeighbors(T node) 
    { 
        return adjacencyList[node]; 
    }

    public int GetNodeCount()
    {
        return adjacencyList.Count;
    }
    
    public void PrintGraph()
    {
        foreach (var node in adjacencyList)
        {
            Debug.Log($"{node.Key}: {string.Join(", ", node.Value)}");
        }
    }
    
    // Breadth-First Search (BFS)
    public void BFS(T startNode)
    {
        if (!adjacencyList.ContainsKey(startNode))
        {
            Debug.Log("Start node does not exist in the graph.");
            return;
        }

        Queue<T> queue = new Queue<T>();
        HashSet<T> visited = new HashSet<T>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            T currentNode = queue.Dequeue();
            Debug.Log(currentNode);

            foreach (T neighbor in adjacencyList[currentNode])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    // Depth-First Search (DFS)
    public void DFS(T startNode)
    {
        if (!adjacencyList.ContainsKey(startNode))
        {
            Debug.Log("Start node does not exist in the graph.");
            return;
        }

        Stack<T> stack = new Stack<T>();
        HashSet<T> visited = new HashSet<T>();

        stack.Push(startNode);
        visited.Add(startNode);

        while (stack.Count > 0)
        {
            T currentNode = stack.Pop();
            Debug.Log(currentNode);

            foreach (T neighbor in adjacencyList[currentNode])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    stack.Push(neighbor);
                }
            }
        }
    }
}