using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Graph<T>
{
    Dictionary<T, List<T>> adjacencyList = new();
    
    public Graph(){}
    
    public Graph(Graph<T> otherGraph)
    {
        adjacencyList = new(otherGraph.adjacencyList);
    }
    
    public void Clear() 
    { 
        adjacencyList.Clear(); 
    }
    
    public bool ContainsKey(T key)
    {
        return adjacencyList.ContainsKey(key);
    }
    
    public void RemoveNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            Debug.LogWarning("Graph: could not remove node as key was not present in dictionary.");
            return;
        }
        
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
        
        adjacencyList[nodeA].Remove(nodeB);
        adjacencyList[nodeB].Remove(nodeA);
    }
    
    public List<T> Edges(T node)
    {
        if (node == null)
        {
            Debug.LogError("Node was NULL");
            return null;
        }
        
        //return new(graph[node]);
        return adjacencyList[node];
    }
    
    public int EdgeCount(T node)
    {
        return adjacencyList[node].Count;
    }
    
    public T First()
    {
        return adjacencyList.First().Key;
    }
    
    public int KeyCount()
    {
        return adjacencyList.Count;
    }
    
    public T[] Keys() => adjacencyList.Keys.ToArray();
    
    public T ElementAt(int index) => adjacencyList.ElementAt(index).Key;
    
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
    
    public void ConvertToSpanningTree()
    {
        T startNode = adjacencyList.Keys.First();
        
        Stack<T> toDo = new();
        toDo.Push(startNode);
        
        Dictionary<T, List<T>> discovered = new()
        {
            { startNode, new() }
        };
        
        while (toDo.Count > 0)
        {
            T node = toDo.Pop();
            
            foreach (T connectedNode in adjacencyList[node])
            {
                if (discovered.ContainsKey(connectedNode)) continue;
                
                toDo.Push(connectedNode);
                discovered.Add(connectedNode, new());
                discovered[node].Add(connectedNode);
                discovered[connectedNode].Add(node);
            }
        }
        
        adjacencyList = discovered;
    }
    
    // Breadth-First Search (BFS)
    public int BFS(T startNode)
    {
        Queue<T> toDo = new();
        toDo.Enqueue(startNode);
        
        List<T> discovered = new()
        {
            startNode
        };
        
        while (toDo.Count > 0)
        {
            T node = toDo.Dequeue();
            
            foreach (T connectedNode in adjacencyList[node])
            {
                if (discovered.Contains(connectedNode)) continue;
                
                toDo.Enqueue(connectedNode);
                discovered.Add(connectedNode);
                //discovered.AddEdge(node, connectedNode);
            }
        }
        
        return discovered.Count;
    }

    // Depth-First Search (DFS)
    public int DFS(T startNode)
    {
        Stack<T> toDo = new();
        toDo.Push(startNode);
        
        List<T> discovered = new()
        {
            startNode
        };
        
        while (toDo.Count > 0)
        {
            T node = toDo.Pop();
            
            foreach (T connectedNode in adjacencyList[node])
            {
                if (discovered.Contains(connectedNode)) continue;
                
                toDo.Push(connectedNode);
                discovered.Add(connectedNode);
                //discovered.AddEdge(node, connectedNode);
            }
        }
        
        return discovered.Count;
    }
}