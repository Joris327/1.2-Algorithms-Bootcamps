using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(DungeonGenerator))]
public class DoorGenerator : MonoBehaviour
{
    //input
    DungeonGenerator dungeonGenerator;
    List<RectRoom> roomsList = new();
    Graph<RectRoom> nodeGraph;
    System.Random random = new();
    int seed = 0;
    int wallThickness = 0;
    int doorSize = 0;
    float visualDelay = 0;
    
    //statictics
    bool printStatistics = true;
    System.Diagnostics.Stopwatch watch = new();
    List<double> generationTimesList = new();
    List<int> doorCountsList = new();
    int doorsCount = 0;

    public void StartGenerator(List<RectRoom> pRoomsList, float pVisualDelay, int pWallThickness, int pSeed, bool pPrintStatistics,  Graph<RectRoom> pNodeGraph)
    {
        ClearGenerator();
        
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
        generationTimesList.Clear();
        doorCountsList.Clear();
        nodeGraph = null;
        doorsCount = 0;
    }
    
    IEnumerator GenerateDoors()
    {
        watch = System.Diagnostics.Stopwatch.StartNew();
        
        foreach (RectRoom room in roomsList)
        {
            List<RectRoom> connectionsList = new(nodeGraph.GetNeighbors(room));
            
            //foreach (RectRoom connectedRoom in nodeGraph.GetNeighbors(room))
            for (int i = 0; i < connectionsList.Count; i++)
            {
                RectRoom connectedRoom = connectionsList[i];
                
                if (room.doors.Any(connectedRoom.doors.Contains)) continue; //this line takes about 50% of the time of this entire foreach loop
                
                if (visualDelay > 0) yield return new WaitForSeconds(visualDelay);
                
                RectInt overLap = AlgorithmsUtils.Intersect(room.roomData, connectedRoom.roomData);
                RectDoor newDoor;
                int xPos;
                int yPos;
                
                if (overLap.width >= (wallThickness * 4) + doorSize)
                {
                    xPos = random.Next(
                        Math.Max(room.roomData.xMin, connectedRoom.roomData.xMin) + (wallThickness * 2), 
                        Math.Min(room.roomData.xMax, connectedRoom.roomData.xMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    if (room.roomData.y < connectedRoom.roomData.y) yPos = room.roomData.yMax - doorSize;
                    else yPos = room.roomData.yMin;
                }
                else if (overLap.height >= (wallThickness * 4) + doorSize)
                {
                    yPos = random.Next(
                        Math.Max(room.roomData.yMin, connectedRoom.roomData.yMin) + (wallThickness * 2),
                        Math.Min(room.roomData.yMax, connectedRoom.roomData.yMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    if (room.roomData.x < connectedRoom.roomData.x) xPos = room.roomData.xMax - doorSize;
                    else xPos = room.roomData.xMin;
                }
                else
                {
                    //nodeGraph.RemoveEdge(room, connectedRoom);
                    continue;
                }

                newDoor = new(new(xPos, yPos, doorSize, doorSize));
                
                room.doors.Add(newDoor);
                connectedRoom.doors.Add(newDoor);
                
                doorsCount++;
            }
        }
        
        watch.Stop();
        
        EnsureConnectivity();
        
        if (!printStatistics)
        {
            generationTimesList.Add(watch.Elapsed.TotalMilliseconds);
            doorCountsList.Add(doorsCount);
        }
        else if (generationTimesList.Count > 0)
        {
            generationTimesList.Add(watch.Elapsed.TotalMilliseconds);
            doorCountsList.Add(doorsCount);
            
            Debug.Log("-");
            Debug.Log("Average doors Generation Time: " + Math.Round(generationTimesList.Average(), 3));
            Debug.Log("Average door count: " + doorCountsList.Average());
        }
        else
        {
            Debug.Log("-");
            Debug.Log("Doors Generation Time: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3));
            Debug.Log("Door count: " + doorsCount);
        }
        
        dungeonGenerator.doneGeneratingDoors = true;
    }
    
    void EnsureConnectivity()
    {
        System.Diagnostics.Stopwatch searchWatch = System.Diagnostics.Stopwatch.StartNew();
        int connected = -1;//nodeGraph.BFS(roomsList[nodeGraph.GetNodeCount()/2]);
        searchWatch.Stop();
        
        Debug.Log("-");
        Debug.Log("nodes: " + nodeGraph.GetNodeCount() + ", connected: " + connected);
        Debug.Log("Search Time: " + Math.Round(searchWatch.Elapsed.TotalMilliseconds, 3));
    }
}
