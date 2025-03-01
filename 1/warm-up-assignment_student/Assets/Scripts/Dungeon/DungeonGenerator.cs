using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    List<RectRoom> splitRooms = new();
    Queue<RectRoom> toDoQueue = new();
    
    [Tooltip("The seed used to generate the dungeon. If 0, will generate new random seed for each dungeon. Else will use the same seed for every dungeon generated.")]
    [SerializeField] int seed = 0;
    int startSeed = 0;
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
    
    //statistics
    List<int> totalGeneratedRoomsList;
    List<int> totalRoomsRemovedList;
    List<int> totalRoomsFinalDungeonList;
    List<double> DungeonGenerationTimesList;
    int DungeonsGeneratedCount = 0;
    
    #region Awake/Start/Update
    
    void Awake()
    {
        startSeed = seed;
        
        StartGenerator();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            StartGenerator();
        }
    }
    
    #endregion
    #region Generator
    
    [Button("Generate")]
    void StartGenerator()
    {
        totalGeneratedRoomsList = new(amountToGenerate);
        totalRoomsRemovedList = new(amountToGenerate);
        totalRoomsFinalDungeonList = new(amountToGenerate);
        DungeonGenerationTimesList = new(amountToGenerate);
        
        DungeonsGeneratedCount = 0;
        
        StartCoroutine(GenerateDungeon());
    }
    
    IEnumerator GenerateDungeon()
    {
        if (amountToGenerate < 1) StopCoroutine(GenerateDungeon());
        
        splitRooms = new((Mathf.Max(worldHeight, worldWidth) / minRoomSize) * 2);
        toDoQueue.Clear();
        
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        
        watch = System.Diagnostics.Stopwatch.StartNew();
        
        random = new(seed);
        
        toDoQueue.Enqueue(new(0, 0, worldWidth, worldHeight));
        
        if ( !CanBeSplit(toDoQueue.Peek()))
        {
            Debug.LogWarning("Unsplittable");
            splitRooms = toDoQueue.ToList();
            
            StopCoroutine(GenerateDungeon());
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
        
        DungeonsGeneratedCount++;
        
        if (amountToGenerate == 1)
        {
            Debug.Log("---");
            Debug.Log("Generation time in milliseconds: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3), this);
            Debug.Log("Rooms generated: " + roomsMade, this);
            Debug.Log("Rooms removed: " + removedRooms, this);
            Debug.Log("Rooms final: " + splitRooms.Count, this);
        }
        else if (DungeonsGeneratedCount < amountToGenerate)
        {
            totalGeneratedRoomsList.Add(roomsMade);
            totalRoomsRemovedList.Add(removedRooms);
            totalRoomsFinalDungeonList.Add(splitRooms.Count);
            DungeonGenerationTimesList.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 3));
            
            StartCoroutine(GenerateDungeon());
        }
        else
        {
            totalGeneratedRoomsList.Add(roomsMade);
            totalRoomsRemovedList.Add(removedRooms);
            totalRoomsFinalDungeonList.Add(splitRooms.Count);
            DungeonGenerationTimesList.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 3));
            
            Debug.Log("-----");
            Debug.Log("Total generation time in seconds: " + Math.Round(DungeonGenerationTimesList.Sum() / 1000, 3));
            Debug.Log("Average generation time in milliseconds: " + DungeonGenerationTimesList.Average(), this);
            Debug.Log("Average rooms generated: " + totalGeneratedRoomsList.Average(), this);
            Debug.Log("Average rooms removed: " + totalRoomsRemovedList.Average(), this);
            Debug.Log("Average rooms final: " + totalRoomsFinalDungeonList.Average(), this);
            Debug.Log("Amount of dungeons generated: " + DungeonsGeneratedCount, this);
        }
        
        StartCoroutine(ShowDungeon());
    }
    
    #endregion
    #region Room Splitting
    
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
    
    #endregion
    #region Helper Methods
    
    IEnumerator ShowDungeon()
    {
        while (true)
        {
            if (splitRooms != null && splitRooms.Count > 0)
            {
                foreach(RectRoom room in splitRooms)
                {
                    AlgorithmsUtils.DebugRectRoom(room, Color.yellow, 0, false, debugWallHeight);
                }
            }
            
            if (toDoQueue != null && toDoQueue.Count > 0)
            {
                foreach(RectRoom room in toDoQueue)
                {
                    AlgorithmsUtils.DebugRectRoom(room, Color.yellow, 0, false, debugWallHeight);
                }
            }
            
            DebugExtension.DebugLocalCube(transform, new Vector3(worldWidth, 0, worldHeight), new Vector3(worldWidth/2f, 0, worldHeight/2f)); //shows world border
            
            yield return null;
        }
    }
    
    [Button("Clear Dungeon")]
    void ClearDungeon()
    {
        StopCoroutine(ShowDungeon());
        
        totalGeneratedRoomsList = null;
        totalRoomsRemovedList = null;
        totalRoomsFinalDungeonList = null;
        DungeonGenerationTimesList = null;
        
        seed = startSeed;
        
        splitRooms.Clear();
        
        DungeonsGeneratedCount = 0;
    }
    
    bool IsEven(int value) => value % 2 == 0;
    
    bool CanBeSplit(RectRoom room) => room.width > (minRoomSize + wallThickness) * 2 || room.height > (minRoomSize + wallThickness) * 2;
    
    bool MustBeSplit(RectRoom room) => room.width > (maxRoomSize - wallThickness) || room.height > (maxRoomSize - wallThickness);
    
    #endregion
}
