using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(DungeonGenerator))]
public class DoorGenerator : MonoBehaviour
{
    DungeonGenerator dungeonGenerator;
    
    List<RectInt> roomsList = new();
    readonly List<RectInt> doorsList = new();
    
    System.Diagnostics.Stopwatch watch = new();
    float visualDelay = 0;
    int wallThickness = 0;
    int doorSize = 0;
    System.Random random = new();
    int seed = 0;
    bool printStatistics = true;
    Graph<RectInt> nodeGraph;
    
    List<double> generationTimesList = new();
    List<int> doorCountsList = new();
    
    [SerializeField] float debugDoorHeight = 5;
    
    public void StartGenerator(List<RectInt> pRoomsList, float pVisualDelay, int pWallThickness, int pSeed, bool pPrintStatistics,  Graph<RectInt> pNodeGraph)
    {
        ClearGenerator();
        
        DebugDrawingBatcher.BatchCall(DrawDoors);
        
        if (!TryGetComponent(out dungeonGenerator)) Debug.Log(name + "could not find DungeonGenerator on itself.", this);
        
        roomsList = pRoomsList;
        visualDelay = pVisualDelay;
        wallThickness = pWallThickness;
        doorSize = wallThickness * 2;
        seed = pSeed;
        random = new(seed);
        printStatistics = pPrintStatistics;
        nodeGraph = pNodeGraph;
        
        StartCoroutine(GenerateDoors());
    }
    
    public void ClearGenerator()
    {
        StopCoroutine(GenerateDoors());
        
        roomsList.Clear();
        doorsList.Clear();
        generationTimesList.Clear();
        doorCountsList.Clear();
        nodeGraph = new();
    }
    
    IEnumerator GenerateDoors()
    {
        watch = System.Diagnostics.Stopwatch.StartNew();
        
        foreach (RectInt room in roomsList)
        {
            foreach (RectInt connectedRoom in nodeGraph.GetEdges(room))
            {
                if (visualDelay > 0)
                {
                    DrawDoors();
                    yield return new WaitForSeconds(visualDelay);
                }
        
                RectInt overLap = AlgorithmsUtils.Intersect(room, connectedRoom);
                
                if (overLap.width >= (wallThickness * 4) + doorSize)
                {
                    int xPos = random.Next(
                        Math.Max(room.xMin, connectedRoom.xMin) + (wallThickness * 2), 
                        Math.Min(room.xMax, connectedRoom.xMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    int yPos;
                    if (room.y < connectedRoom.y) yPos = room.yMax - doorSize;
                    else yPos = room.yMin;
                        
                    doorsList.Add(new(xPos, yPos, doorSize, doorSize));
                    continue;
                }
                
                if (overLap.height >= (wallThickness * 4) + doorSize)
                {
                    int yPos = random.Next(
                        Math.Max(room.yMin, connectedRoom.yMin) + (wallThickness * 2),
                        Math.Min(room.yMax, connectedRoom.yMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    int xPos;
                    if (room.x < connectedRoom.x) xPos = room.xMax - doorSize;
                    else xPos = room.xMin;
                    
                    doorsList.Add(new(xPos, yPos, doorSize, doorSize));
                    continue;
                }
            }
        }
        
        // for (int i = 0; i < roomsList.Count; i++)
        // {
        //     RectInt room1 = roomsList[i];
            
        //     if (visualDelay > 0)
        //     {
        //         DrawDoors();
        //         yield return new WaitForSeconds(visualDelay);
        //     }
            
        //     for (int j = i+1; j < roomsList.Count; j++)
        //     {
        //         RectInt room2 = roomsList[j];
                
        //         if (room1 == room2)
        //         {
        //             Debug.Log("overlap");
        //             continue;
        //         }
        //         if (!AlgorithmsUtils.Intersects(room1, room2)) continue;
        //         //if (nodeGraph.GetEdges(room1).Contains(room2)) continue;
                
        //         RectInt overLap = AlgorithmsUtils.Intersect(room1, room2);
                
        //         if (overLap.width >= (wallThickness * 4) + doorSize)
        //         {
        //             int xPos = random.Next(
        //                 Math.Max(room1.xMin, room2.xMin) + (wallThickness * 2), 
        //                 Math.Min(room1.xMax, room2.xMax) - (wallThickness * 2) - doorSize + 1
        //             );
                    
        //             int yPos;
        //             if (room1.y < room2.y) yPos = room1.yMax - doorSize;
        //             else yPos = room1.yMin;
                        
        //             doorsList.Add(new(xPos, yPos, doorSize, doorSize));
        //             continue;
        //         }
                
        //         if (overLap.height >= (wallThickness * 4) + doorSize)
        //         {
        //             int yPos = random.Next(
        //                 Math.Max(room1.yMin, room2.yMin) + (wallThickness * 2),
        //                 Math.Min(room1.yMax, room2.yMax) - (wallThickness * 2) - doorSize + 1
        //             );
                    
        //             int xPos;
        //             if (room1.x < room2.x) xPos = room1.xMax - doorSize;
        //             else xPos = room1.xMin;
                    
        //             doorsList.Add(new(xPos, yPos, doorSize, doorSize));
        //             continue;
        //         }
        //     }
        // }
        
        watch.Stop();
        
        if (!printStatistics)
        {
            generationTimesList.Add(watch.Elapsed.TotalMilliseconds);
            doorCountsList.Add(doorsList.Count);
        }
        else if (generationTimesList.Count > 0)
        {
            generationTimesList.Add(watch.Elapsed.TotalMilliseconds);
            doorCountsList.Add(doorsList.Count);
            
            Debug.Log("-");
            Debug.Log("Average doors Generation Time: " + Math.Round(generationTimesList.Average(), 3));
            Debug.Log("Average door count: " + doorCountsList.Average());
        }
        else
        {
            Debug.Log("-");
            Debug.Log("Doors Generation Time: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3));
            Debug.Log("Door count: " + doorsList.Count);
        }
        
        dungeonGenerator.doneGeneratingDoors = true;
    }
    
    void DrawDoors()
    {
        foreach (RectInt door in doorsList)
        {
            AlgorithmsUtils.DebugRectInt(door, Color.blue);
        }
        
        foreach (var node in nodeGraph.GetGraph())
        {
            foreach (RectInt edge in node.Value)
            {
                Vector3 nodeCenter = new(node.Key.center.x, 0, node.Key.center.y);
                Vector3 keyCenter = new (edge.center.x, 0, edge.center.y);
                
                Debug.DrawLine(nodeCenter, keyCenter, Color.red);
                AlgorithmsUtils.DebugRectInt(new(new((int)node.Key.center.x, (int)node.Key.center.y), new(1,1)), Color.white);
            }
        }
    }
}
