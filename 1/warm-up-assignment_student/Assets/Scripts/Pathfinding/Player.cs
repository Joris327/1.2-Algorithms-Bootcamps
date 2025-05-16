using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

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

        // Add visual debugging here
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

    void SetDestination(Vector3 target)
    {
        if (agent.enabled) agent.destination = target;
    }

    public void SetGraph(Graph<Vector3> newGraphMap)
    {
        graph = newGraphMap;
    }
    
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

    async Task FindPath()
    {
        Vector3 start = FindClosestNode(transform.position);
        Vector3 end = FindClosestNode(clickPosition);
        
        List<KeyValuePair<Vector3, float>> queue = new(); //node, priority
	    Dictionary<Vector3, float> costs = new(); //node, cost
        Dictionary<Vector3, Vector3> path = new(); //edge, node
        discovered = new();
        
        queue.Add(new KeyValuePair<Vector3, float>(start,0));
	    costs.Add(start, 0);
        
        while (queue.Count > 0)
        {
            Vector3 node = queue[^1].Key;
            queue.RemoveAt(queue.Count-1);
            discovered.Add(node);
            
            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(new((int)node.x, (int)node.z, 1, 1), Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }
            
            if (node == end)
            {
                ReconstructPath(path, end, start);
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
                        case SearchMode.Dijkstra: queue.Add(new KeyValuePair<Vector3, float>(edge, newCost                              )); break;
                        case SearchMode.AStar   : queue.Add(new KeyValuePair<Vector3, float>(edge, newCost + (Vector3.Distance(edge, end) * heuristicWeight))); break;
                    }
                }
            }
            
            queue = queue.OrderByDescending(node => node.Value).ToList();
        }
        
        Debug.LogWarning("No path Found");
        return;
    }
    
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
    }
}
