using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// does pathfinding
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(FollowPathController))]
public class Player : MonoBehaviour
{
    [SerializeField] SearchMode searchMode;
    [SerializeField] bool showGraph = true;
    [SerializeField, Min(0)] float visualDelay = 0;
    [SerializeField, Min(0)] float heuristicWeight = 1;
    
    enum SearchMode { Dijkstra, AStar }
    
    NavMeshAgent agent;
    Vector3 clickPosition = new();
    Graph<Vector3> graph = null;
    FollowPathController controller;
    HashSet<Vector3> discovered = new();
    List<Vector3> path = new();
    
    void Awake()
    {
        if (!TryGetComponent(out agent)) Debug.LogError("Player: could not find NavMeshAgent component.", this);
        if (!TryGetComponent(out controller)) Debug.LogError("Player: could not find FollowPathController component.", this);
    }

    async void Update()
    {
        // Get the mouse click position in world space 
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo))
            {
                Vector3 clickWorldPosition = hitInfo.point;
                //Debug.Log(clickWorldPosition); 

                clickPosition = clickWorldPosition;
                
                if (graph == null)
                {
                    agent.enabled = true;
                    SetDestination(clickPosition);
                }
                else
                {
                    await FindPath();
                    controller.GoToDestination(path);
                }
            }
        }

        // Add debug visuals
        DebugExtension.DebugCircle(clickPosition, Color.blue);
        Debug.DrawLine(Camera.main.transform.position, clickPosition, Color.yellow);
        
        if (graph != null && showGraph)
        {
            Vector3[] keys = graph.GetNodes().ToArray();
            foreach (Vector3 pos in keys)
            {
                foreach (Vector3 other in graph.Edges(pos))
                {
                    Debug.DrawLine(pos, other, Color.grey);
                }
            }
            
            foreach (Vector3 pos in discovered)
            {
                DebugExtension.DebugPoint(pos, 0.5f, 0, false);
            }
            
            if (path != null)
            {
                foreach (Vector3 node in path)
                {
                    DebugExtension.DebugPoint(node, Color.red, 0.5f, 0, false);
                }
            }
        }
    }
    
    /// <summary>
    /// set destination for the unity AI agent
    /// </summary>
    void SetDestination(Vector3 target)
    {
        if (agent.enabled) agent.destination = target;
    }
    
    /// <summary>
    /// gives the node graph of the dungeon to the player so it can do pathfinding.
    /// </summary>
    public void SetGraph(Graph<Vector3> newGraphMap)
    {
        graph = new Graph<Vector3>(newGraphMap);
    }
    
    /// <summary>
    /// find the node that is closest to the place that was clicked on screen.
    /// </summary>
    Vector3 FindClosestNode(Vector3 input)
    {
        Vector3 closestNode = new();
        float shortestDistance = float.MaxValue;
        
        foreach (Vector3 node in graph.GetNodes())
        {
            if ((input - node).magnitude < shortestDistance)
            {
                shortestDistance = (node - input).magnitude;
                closestNode = node;
            }
        }
        
        return closestNode;
    }
    
    /// <summary>
    /// search the nodegraph for the end node 
    /// </summary>
    async Task FindPath()
    {
        Debug.Log("---");
        System.Diagnostics.Stopwatch pathFindWatch = System.Diagnostics.Stopwatch.StartNew();
        Vector3 start = FindClosestNode(transform.position);
        Vector3 end = FindClosestNode(clickPosition);
        
        List<KeyValuePair<Vector3, float>> toDoList = new(); //node, priority
	    Dictionary<Vector3, float> costs = new(); //node, cost
        Dictionary<Vector3, Vector3> path = new(); //edge, node
        discovered = new();
        
        toDoList.Add(new KeyValuePair<Vector3, float>(start,0));
	    costs.Add(start, 0);
        
        while (toDoList.Count > 0)
        {
            Vector3 node = toDoList[^1].Key;
            toDoList.RemoveAt(toDoList.Count-1);
            discovered.Add(node);
            
            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(new((int)node.x, (int)node.z, 1, 1), Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }
            
            if (node == end)
            {
                ReconstructPath(path, end, start);
                pathFindWatch.Stop();
                Debug.Log("Nodes Searched: " + discovered.Count);
                Debug.Log("Path find time: " + Math.Round(pathFindWatch.Elapsed.TotalMilliseconds, 3));
                return;
            }
            
            foreach (Vector3 edge in graph.Edges(node))
            {
                float newCost = costs[node] + Vector3.Distance(edge, node);
                
                if (!costs.Keys.Contains(edge) || newCost < costs[edge])
                {
                    costs[edge] = newCost;
                    path[edge] = node;
                    
                    switch (searchMode)
                    {
                        case SearchMode.Dijkstra: toDoList.Add(new KeyValuePair<Vector3, float>(edge, newCost                              )); break;
                        case SearchMode.AStar   : toDoList.Add(new KeyValuePair<Vector3, float>(edge, newCost + (Vector3.Distance(edge, end) * heuristicWeight))); break;
                    }
                }
            }
            
            toDoList = toDoList.OrderByDescending(node => node.Value).ToList();
        }
        
        Debug.LogWarning("No path Found");
        return;
    }
    
    /// <summary>
    /// traces the found path back to the origin for the player to follow
    /// </summary>
    void ReconstructPath(Dictionary<Vector3, Vector3> newPath, Vector3 end, Vector3 start)
    {
        List<Vector3> returnList = new();
        returnList.Add(end);
        
        while (returnList.Last() != start)
        {
            returnList.Add(newPath[returnList.Last()]);
        }
        
        path = returnList;
        path.Reverse();
        Debug.Log("Path Length: " + path.Count);
    }
}
