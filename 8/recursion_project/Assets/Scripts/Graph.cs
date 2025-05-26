using System.Collections.Generic;
using System.Linq;
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
        return new List<T>(adjacencyList[node]); 
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
    public void BFS(T v)
    {
        HashSet<T> discovered = new HashSet<T>();
        Queue<T> Q = new Queue<T>();
        
        Q.Enqueue(v);
        discovered.Add(v);

        while (Q.Count > 0)
        {
            v = Q.Dequeue();
            Debug.Log(v);
            foreach (T w in GetNeighbors(v))
            {
                if (!discovered.Contains(w))
                {
                    Q.Enqueue(w);
                    discovered.Add(w);
                }
            }
        }
    }

    // Depth-First Search (DFS)
    public void DFS(T v)
    {
        HashSet<T> discovered = new HashSet<T>();
        
        Stack<T> S = new Stack<T>();
        S.Push(v);
        while (S.Count > 0)
        {
            v = S.Pop();
            if (!discovered.Contains(v))
            {
                discovered.Add(v);
                Debug.Log(v);
                foreach (T w in GetNeighbors(v))
                {
                    S.Push(w);
                }
            }
        }
    }
    
    // Depth-First Search (DFS) Recursive
    public void DFSRecursive(T v)
    {
        HashSet<T> discovered = new HashSet<T>();
        DFSRecursion(discovered, v);
    }

    private void DFSRecursion(HashSet<T> discovered, T v)
    {
        /* */
    }
}