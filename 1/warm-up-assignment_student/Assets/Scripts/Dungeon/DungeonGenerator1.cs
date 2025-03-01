using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator1 : MonoBehaviour
{
    List<RectRoom> splitRooms = new();
    Queue<RectRoom> toDoQueue = new();
    
    [SerializeField] int seed = 0;
    int startSeed;
    [SerializeField, Min(0)] int amountToGenerate = 1;
    [SerializeField, Min(0)] float visualDelay = 0.5f;
    
    [Header("World")]
    [SerializeField, Min(0)] int worldWidth = 100;
    [SerializeField, Min(0)] int worldHeight = 100;
    
    [Header("Rooms")]
    [SerializeField] int minRoomSize = 10;
    [SerializeField] int maxRoomSize = 20;
    [SerializeField, Min(0)] int roomsLimit = 1000;
    [SerializeField, Range(0, 100)] int chanceToSplit = 90;
    
    /// <summary>
    /// How many times to guarantee rooms split before applying a chance. Prevents dungeon-sized rooms.
    /// </summary>
    [Tooltip("How many times to guarantee rooms split before applying a chance. Prevents dungeon-sized rooms.")]
    [SerializeField, Min(0)] int guaranteedSplits = 4;
    
    [Header("Walls")]
    [SerializeField, Min(0)] int wallThickness = 1;
    [SerializeField, Min(0)] float debugWallHeight = 5;
    
    System.Diagnostics.Stopwatch watch = new();
    System.Random random = new();
    
    List<int> roomsGenerated;
    List<int> roomsRemoved;
    List<int> roomsFinalCounts;
    List<double> generationTimes;
    int generationCount = 0;

    void Awake()
    {
        startSeed = seed;
        
        roomsGenerated = new(amountToGenerate);
        roomsRemoved = new(amountToGenerate);
        roomsFinalCounts = new(amountToGenerate);
        generationTimes = new(amountToGenerate);
        
        //GenerateDungeon();
        StartCoroutine(GenerateDungeonVisually());
        
        //if (generateAmount > 0) GenerateBatch();
        
        
        
        //Debug.Log("-----");
        //Debug.Log("Total generation time in seconds: " + Math.Round(generationTimes.Sum() / 1000, 3));
        //Debug.Log("Average generation time in milliseconds: " + generationTimes.Average(), this);
        //Debug.Log("Average rooms generated: " + roomsGenerated.Average(), this);
        //Debug.Log("Average rooms removed: " + roomsRemoved.Average(), this);
        //Debug.Log("Average rooms final: " + roomsFinalCounts.Average(), this);
        //Debug.Log("Amount of dungeons generated: " + generationCount, this);
    }
    
    void GenerateBatch()
    {
        roomsGenerated = new(amountToGenerate);
        roomsRemoved = new(amountToGenerate);
        roomsFinalCounts = new(amountToGenerate);
        generationTimes = new(amountToGenerate);
        
        for (int i = 0; i < amountToGenerate; i++)
        {
            if (visualDelay > 0) break;
            //GenerateDungeon(true);
            StartCoroutine(GenerateDungeonVisually());
        }
        
        Debug.Log("-----");
        Debug.Log("Total generation time in seconds: " + Math.Round(generationTimes.Sum() / 1000, 3));
        Debug.Log("Average generation time in milliseconds: " + generationTimes.Average(), this);
        //Debug.Log("Average rooms generated: " + roomsGenerated.Average(), this);
        //Debug.Log("Average rooms removed: " + roomsRemoved.Average(), this);
        Debug.Log("Average rooms final: " + roomsFinalCounts.Average(), this);
        Debug.Log("Amount of dungeons generated: " + generationTimes.Count, this);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            //GenerateDungeon();
            StartCoroutine(GenerateDungeonVisually());
            //if (generateAmount > 0) GenerateBatch();
        }
        
        if (splitRooms != null && splitRooms.Count > 0)
        {
            foreach(RectRoom room in splitRooms)
            {
                AlgorithmsUtils.DebugRectRoom(room, Color.yellow, 0, false, debugWallHeight);
            }
        } 
        //else Debug.LogWarning("No Rooms to show");
        
        if (toDoQueue != null && toDoQueue.Count > 0)
        {
            foreach(RectRoom room in toDoQueue)
            {
                AlgorithmsUtils.DebugRectRoom(room, Color.yellow, 0, false, debugWallHeight);
            }
        }
        //else Debug.LogWarning("No Rooms to show");
        
        DebugExtension.DebugLocalCube(transform, new Vector3(worldWidth, 0, worldHeight), new Vector3(worldWidth/2f, 0, worldHeight/2f)); //shows world border
    }
    
    IEnumerator GenerateDungeonVisually(bool storeStats = false)
    {
        SetupGenerator();
        
        if ( !CanBeSplit(toDoQueue.Peek()))
        {
            Debug.LogWarning("Unsplittable");
            splitRooms = toDoQueue.ToList();
            //yield return;
            StopCoroutine(GenerateDungeonVisually());
        }
        
        int removedRooms = 0;
        int roomsMade = 0;
        
        while (toDoQueue.Count > 0)
        {
            if (visualDelay > 0) yield return new WaitForSeconds(visualDelay);
            
            if (splitRooms.Count > guaranteedSplits && random.Next(0, 100) > chanceToSplit && !MustBeSplit(toDoQueue.Peek()))
            {
                splitRooms.Add(toDoQueue.Dequeue());
            }
            
            SplitRoom(toDoQueue.Dequeue());
            removedRooms++;
            roomsMade += 2;
            
            if (toDoQueue.Count > roomsLimit)
            {
                Debug.LogWarning("Reached room limit");
                splitRooms.AddRange(toDoQueue.ToList());
                break;
            }
        }
        
        splitRooms.TrimExcess();
        
        watch.Stop();
        
        // if (storeStats)
        // {
        //     roomsGenerated.Add(roomsMade);
        //     roomsRemoved.Add(removedRooms);
        //     roomsFinalCounts.Add(splitRooms.Count);
        //     generationTimes.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 3));
        // }
        // else
        // {
        //     Debug.Log("---");
        //     Debug.Log("Generation time in milliseconds: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3), this);
        //     Debug.Log("Rooms generated: " + roomsMade, this);
        //     Debug.Log("Rooms removed: " + removedRooms, this);
        //     Debug.Log("Rooms final: " + splitRooms.Count, this);
        // }
        
        generationCount++;
        
        if (amountToGenerate == 1)
        {
            Debug.Log("---");
            Debug.Log("Generation time in milliseconds: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3), this);
            Debug.Log("Rooms generated: " + roomsMade, this);
            Debug.Log("Rooms removed: " + removedRooms, this);
            Debug.Log("Rooms final: " + splitRooms.Count, this);
        }
        else if (generationCount < amountToGenerate)
        {
            roomsGenerated.Add(roomsMade);
            roomsRemoved.Add(removedRooms);
            roomsFinalCounts.Add(splitRooms.Count);
            generationTimes.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 3));
            
            StartCoroutine(GenerateDungeonVisually(true));
        }
        else
        {
            Debug.Log("-----");
            Debug.Log("Total generation time in seconds: " + Math.Round(generationTimes.Sum() / 1000, 3));
            Debug.Log("Average generation time in milliseconds: " + generationTimes.Average(), this);
            Debug.Log("Average rooms generated: " + roomsGenerated.Average(), this);
            Debug.Log("Average rooms removed: " + roomsRemoved.Average(), this);
            Debug.Log("Average rooms final: " + roomsFinalCounts.Average(), this);
            Debug.Log("Amount of dungeons generated: " + generationTimes.Count, this);
        }
    }
    
    void GenerateDungeon(bool storeStats = false)
    {
        SetupGenerator();
        
        if ( !CanBeSplit(toDoQueue.Peek()))
        {
            Debug.LogWarning("Unsplittable");
            splitRooms = toDoQueue.ToList();
            return;
        }
        
        int removedRooms = 0;
        int roomsMade = 0;
        
        while (toDoQueue.Count > 0)
        {
            if (splitRooms.Count > guaranteedSplits && random.Next(0, 100) > chanceToSplit && !MustBeSplit(toDoQueue.Peek()))
            {
                splitRooms.Add(toDoQueue.Dequeue());
            }
            
            SplitRoom(toDoQueue.Dequeue());
            removedRooms++;
            roomsMade += 2;
            
            if (toDoQueue.Count > roomsLimit)
            {
                Debug.LogWarning("Reached room limit");
                splitRooms.AddRange(toDoQueue.ToList());
                break;
            }
        }
        
        splitRooms.TrimExcess();
        
        watch.Stop();
        
        if (storeStats)
        {
            roomsGenerated.Add(roomsMade);
            roomsRemoved.Add(removedRooms);
            roomsFinalCounts.Add(splitRooms.Count);
            generationTimes.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 3));
        }
        else
        {
            Debug.Log("---");
            Debug.Log("Generation time in milliseconds: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3), this);
            Debug.Log("Rooms generated: " + roomsMade, this);
            Debug.Log("Rooms removed: " + removedRooms, this);
            Debug.Log("Rooms final: " + splitRooms.Count, this);
        }
    }
    
    void SetupGenerator()
    {
        splitRooms = new((Mathf.Max(worldHeight, worldWidth) / minRoomSize) * 2);
        toDoQueue.Clear();
        
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        
        watch = System.Diagnostics.Stopwatch.StartNew();
        
        random = new(seed);
        
        toDoQueue.Enqueue(new(0, 0, worldWidth, worldHeight));
    }
    
    void SplitRoom(RectRoom room)
    {
        if ( !CanBeSplit(room)) return;
        
        bool splitVertically = false; 
        if (room.IsWiderThanHigh()) splitVertically = true;
        
        RectRoom newRoom1;
        RectRoom newRoom2 = new(room.x, room.y, room.width, room.height);
        
        if (splitVertically)
        {
            int splitPos = random.Next(minRoomSize, room.width - minRoomSize);
            
            newRoom1 = new(room.x + splitPos - wallThickness, room.y, room.width - splitPos + wallThickness, room.height);
            newRoom2.width = splitPos + wallThickness;
        }
        else
        {
            int splitPos = random.Next(minRoomSize, room.height - minRoomSize);
            
            newRoom1 = new(room.x, room.y + splitPos - wallThickness, room.width, room.height - splitPos + wallThickness);
            newRoom2.height = splitPos + wallThickness;
        }
        
        if (CanBeSplit(newRoom2)) toDoQueue.Enqueue(newRoom2);
        else splitRooms.Add(newRoom2);
        
        if (CanBeSplit(newRoom1)) toDoQueue.Enqueue(newRoom1);
        else splitRooms.Add(newRoom1);
    }
    
    bool IsEven(int value) => value % 2 == 0;
    
    bool CanBeSplit(RectRoom room) => room.width > (minRoomSize + wallThickness) * 2 || room.height > (minRoomSize + wallThickness) * 2;
    
    bool MustBeSplit(RectRoom room) => room.width > (maxRoomSize - wallThickness) || room.height > (maxRoomSize - wallThickness);
}
