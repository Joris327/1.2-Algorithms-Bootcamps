using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(DungeonGenerator))]
public class DoorGenerator : MonoBehaviour
{
    DungeonGenerator dungeonGenerator;
    
    List<RectRoom> roomsList = new();
    readonly List<RectRoom> doorsList = new();
    
    System.Diagnostics.Stopwatch watch = new();
    float visualDelay = 0;
    int wallThickness = 0;
    int doorSize = 0;
    System.Random random = new();
    int seed = 0;
    bool printStatistics = true;
    
    List<double> generationTimesList = new();
    List<int> doorCountsList = new();
    
    [SerializeField] float debugDoorHeight = 5;
    
    public void StartGenerator(List<RectRoom> pRoomsList, float pVisualDelay, int pWallThickness, int pSeed, bool pPrintStatistics)
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
        
        StartCoroutine(GenerateDoors());
    }
    
    public void ClearGenerator()
    {
        StopCoroutine(GenerateDoors());
        StopCoroutine(DrawDoorsContinuesly());
        
        roomsList.Clear();
        doorsList.Clear();
        generationTimesList.Clear();
        doorCountsList.Clear();
    }
    
    IEnumerator GenerateDoors()
    {
        if (printStatistics) StartCoroutine(DrawDoorsContinuesly());
        
        watch = System.Diagnostics.Stopwatch.StartNew();
        
        foreach(RectRoom room1 in roomsList)
        {
            if (visualDelay > 0)
            {
                DrawDoors(visualDelay);
                yield return new WaitForSeconds(visualDelay);
            }
            
            foreach(RectRoom room2 in roomsList)
            {
                if (room1 == room2) continue;
                if (!AlgorithmsUtils.Intersects(room1, room2)) continue;
                if (room1.connections.Contains(room2)) continue;
                
                RectRoom overLap = AlgorithmsUtils.Intersect(room1, room2);
                
                if (overLap.width >= (wallThickness * 4) + doorSize)
                {
                    int xPos = random.Next(
                        Math.Max(room1.xMin, room2.xMin) + (wallThickness * 2), 
                        Math.Min(room1.xMax, room2.xMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    int yPos;
                    if (room1.y < room2.y) yPos = room1.yMax - doorSize;
                    else yPos = room1.yMin;
                        
                    doorsList.Add(new(xPos, yPos, doorSize, doorSize));
                    room1.connections.Add(room2);
                    room2.connections.Add(room1);
                    continue;
                }
                else if (overLap.height >= (wallThickness * 4) + doorSize)
                {
                    int yPos = random.Next(
                        Math.Max(room1.yMin, room2.yMin) + (wallThickness * 2),
                        Math.Min(room1.yMax, room2.yMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    int xPos;
                    if (room1.x < room2.x) xPos = room1.xMax - doorSize;
                    else xPos = room1.xMin;
                    
                    doorsList.Add(new(xPos, yPos, doorSize, doorSize));
                    room1.connections.Add(room2);
                    room2.connections.Add(room1);
                    continue;
                }
            }
        }
        
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
    
    IEnumerator DrawDoorsContinuesly(float duration = 0)
    {
        while (true)
        {
            DrawDoors(duration);
            yield return null;
        }
    }
    
    void DrawDoors(float duration = 0)
    {
        foreach (RectRoom door in doorsList)
        {
            AlgorithmsUtils.DebugRectRoom(door, Color.blue, duration, false, debugDoorHeight);
        }
    }
}
