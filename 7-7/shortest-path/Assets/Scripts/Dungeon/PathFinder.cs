using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum Algorithms
{
    BFS,
    Dijkstra,
    AStar
}

public class PathFinder : MonoBehaviour
{
    
    public GraphGenerator graphGenerator;
    
    private Vector3 startNode;
    private Vector3 endNode;
    
    public List<Vector3> path = new List<Vector3>();
    HashSet<Vector3> discovered = new HashSet<Vector3>();
    
    private Graph<Vector3> graph;
    
    public Algorithms algorithm = Algorithms.BFS;
    
    void Start()
    {
        graphGenerator = GetComponent<GraphGenerator>();
        graph = graphGenerator.GetGraph();
    }

    private Vector3 GetClosestNodeToPosition(Vector3 position)
    {
        //Vector3 closestNode = Vector3.zero;
        //float closestDistance = Mathf.Infinity;
        
        Vector3 closestNode = new(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
        
        return closestNode;
    }
    
    public List<Vector3> CalculatePath(Vector3 from, Vector3 to)
    {
        Vector3 playerPosition = from;
        
        startNode = GetClosestNodeToPosition(playerPosition);
        endNode = GetClosestNodeToPosition(to);

        List<Vector3> shortestPath = new List<Vector3>();
        
        switch (algorithm)
        {
            case Algorithms.BFS:
                shortestPath = BFS(startNode, endNode);
                break;
            case Algorithms.Dijkstra:
                shortestPath =  Dijkstra(startNode, endNode);
                break;
            case Algorithms.AStar:
                shortestPath =  AStar(startNode, endNode);
                break;
        }
        
        path = shortestPath; //Used for drawing the path
        
        return shortestPath;
    }
    
    List<Vector3> BFS(Vector3 start, Vector3 end) 
    {
        //return new List<Vector3>(); // No path found
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()
        discovered.Clear();
        
        Dictionary<Vector3, Vector3> pairs = new();
        
        Queue<Vector3> queue = new();
        queue.Enqueue(start);
        discovered.Add(start);
        
        while (queue.Count > 0)
        {
            Vector3 node = queue.Dequeue();
            
            if (node == end)
            {
                return ReconstructPath(pairs, start, end);
            }
            
            foreach (Vector3 edge in graph.GetNeighbors(node))
            {
                if (!discovered.Contains(edge))
                {
                    queue.Enqueue(edge);
                    discovered.Add(edge);
                    
                    pairs.Add(edge, node);
                }
            }
        }

        return new List<Vector3>(); // No path found
    }
    
    
    public List<Vector3> Dijkstra(Vector3 start, Vector3 end)
    {
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()
        discovered.Clear(); 
        
        List<KeyValuePair<Vector3, float>> queue = new(); //node, priority
        Dictionary<Vector3, float> costs = new(); //node, cost
        Dictionary<Vector3, Vector3> path = new(); //edge, node
        
        queue.Add(new KeyValuePair<Vector3, float>(start, 0));
        costs.Add(start, 0);
        discovered.Add(start);
        
        while (queue.Count > 0)
        {
            Vector3 node = queue[^1].Key;
            queue.RemoveAt(queue.Count-1);
            discovered.Add(node);
            
            if (node == end)
            {
                return ReconstructPath(path, start, end);
            }
            
            foreach (Vector3 edge in graph.GetNeighbors(node))
            {
                float newCost = costs[node] + Cost(node, edge);
                if (!costs.Keys.Contains(edge))
                {
                    costs.Add(edge, newCost);
                    path.Add(edge, node);
                    queue.Add(new KeyValuePair<Vector3, float>(edge, newCost));
                    //discovered.Add(edge);
                }
                else if (newCost < costs[edge])
                {
                    costs[edge] = newCost;
                    path[edge] = node;
                    queue.Add(new KeyValuePair<Vector3, float>(edge, newCost));
                }
            }
            
            queue = queue.OrderByDescending(node => node.Value).ToList();
        }
        
        /* */
        return new List<Vector3>(); // No path found
    }
    
    List<Vector3> AStar( Vector3 start, Vector3 end)
    {
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()
        discovered.Clear();
        
        List<KeyValuePair<Vector3, float>> queue = new(); //node, priority
        Dictionary<Vector3, float> costs = new(); //node, cost
        Dictionary<Vector3, Vector3> path = new(); //edge, node
        
        queue.Add(new KeyValuePair<Vector3, float>(start, 0));
        costs.Add(start, 0);
        discovered.Add(start);
        
        while (queue.Count > 0)
        {
            Vector3 node = queue[^1].Key;
            queue.RemoveAt(queue.Count-1);
            discovered.Add(node);
            
            if (node == end)
            {
                return ReconstructPath(path, start, end);
            }
            
            foreach (Vector3 edge in graph.GetNeighbors(node))
            {
                float newCost = costs[node] + Cost(node, edge);
                if (!costs.Keys.Contains(edge))
                {
                    costs.Add(edge, newCost);
                    path.Add(edge, node);
                    queue.Add(new KeyValuePair<Vector3, float>(edge, newCost + Heuristic(edge, end)));
                    //discovered.Add(edge);
                }
                else if (newCost < costs[edge])
                {
                    costs[edge] = newCost;
                    path[edge] = node;
                    queue.Add(new KeyValuePair<Vector3, float>(edge, newCost + Heuristic(edge, end)));
                }
            }
            
            queue = queue.OrderByDescending(node => node.Value).ToList();
        }
        
        /* */
        return new List<Vector3>(); // No path found
    }
    
    public float Cost(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }
    
    public float Heuristic(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to);
    }
    
    List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> parentMap, Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 currentNode = end;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = parentMap[currentNode];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startNode, .3f);
    
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endNode, .3f);
    
        if (discovered != null) {
            foreach (var node in discovered)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(node, .3f);
            }
        }
        
        if (path != null) {
            foreach (var node in path)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(node, .3f);
            }
        }
        
        
    }
}
