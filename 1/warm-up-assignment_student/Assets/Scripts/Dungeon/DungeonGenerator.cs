using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    List<RectRoom> rooms = new();
    
    [SerializeField] int seed = 0;
    int startSeed;
    [SerializeField] int generateAmount = 0;
    
    [Header("World")]
    [SerializeField, Min(0)] int worldWidth = 100;
    [SerializeField, Min(0)] int worldHeight = 100;
    
    [Header("Rooms")]
    [SerializeField] int minRoomSize = 5;
    [SerializeField] int maxRoomSize = 15;
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

    void Awake()
    {
        startSeed = seed;
        
        GenerateDungeon();
        
        if (generateAmount > 0) GenerateBatch();
    }
    
    void GenerateBatch()
    {
        roomsGenerated = new(generateAmount);
        roomsRemoved = new(generateAmount);
        roomsFinalCounts = new(generateAmount);
        generationTimes = new(generateAmount);
        
        for (int i = 0; i < generateAmount; i++)
        {
            GenerateDungeon(true);
        }
        
        Debug.Log("---");
        Debug.Log("Total generation time in seconds: " + Math.Round(generationTimes.Sum() / 1000, 3));
        Debug.Log("Average generation time in milliseconds: " + generationTimes.Average(), this);
        Debug.Log("Average rooms generated: " + roomsGenerated.Average(), this);
        Debug.Log("Average rooms removed: " + roomsRemoved.Average(), this);
        Debug.Log("Average rooms final: " + roomsFinalCounts.Average(), this);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G)) GenerateDungeon();
        
        if (rooms != null && rooms.Count > 0)
        {
            foreach(RectRoom room in rooms)
            {
                AlgorithmsUtils.DebugRectRoom(room, Color.yellow, 0, false, debugWallHeight);
            }
        }
        else Debug.LogWarning("No Rooms to show");
        
        DebugExtension.DebugLocalCube(transform, new Vector3(worldWidth, 0, worldHeight), new Vector3(worldWidth/2f, 0, worldHeight/2f)); //shows world border
    }
    
    void GenerateDungeon(bool storeStats = false)
    {
        rooms.Clear();
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        
        watch = System.Diagnostics.Stopwatch.StartNew();
        
        random = new(seed);

        RectRoom baseRoom = new(0, 0, worldWidth, worldHeight);
        rooms.Add(baseRoom);
        
        if ( !CanBeSplit(baseRoom))
        {
            Debug.LogWarning("Unsplittable");
            return;
        }
        
        for (int i = 0; i < rooms.Count; i++)
        {
            RectRoom currentRoom = rooms.ElementAt(i);
            if (i > guaranteedSplits && !MustBeSplit(currentRoom) && random.Next(0, 100) > chanceToSplit)
            {
                //do nothing
            }
            else
            {
                SplitRoom(currentRoom);
            }
            
            if (rooms.Count > roomsLimit)
            {
                Debug.LogWarning("Reached room limit");
                break;
            }
        }
        
        int GeneratedRoomsCount = rooms.Count;
        List<RectRoom> newRoomsList = new(rooms.Count);
        for (int i = rooms.Count-1; i > 0; i--)
        {
            if (!rooms[i].markedForDestruction)
            {
                newRoomsList.Add(rooms[i]);
            }
        }
        newRoomsList.TrimExcess();
        rooms = newRoomsList;
        
        watch.Stop();
        
        if (storeStats)
        {
            roomsGenerated.Add(GeneratedRoomsCount);
            roomsRemoved.Add(GeneratedRoomsCount - rooms.Count);
            roomsFinalCounts.Add(rooms.Count);
            generationTimes.Add(Math.Round(watch.Elapsed.TotalMilliseconds, 3));
        }
        else
        {
            Debug.Log("---");
            Debug.Log("Generation time in milliseconds: " + Math.Round(watch.Elapsed.TotalMilliseconds, 3), this);
            Debug.Log("Rooms generated: " + GeneratedRoomsCount, this);
            Debug.Log("Rooms removed: " + (GeneratedRoomsCount - rooms.Count), this);
            Debug.Log("Rooms final: " + rooms.Count, this);
        }
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
        
        room.markedForDestruction = true;
        
        rooms.Add(newRoom1);
        rooms.Add(newRoom2);
    }
    
    bool IsEven(int value) => value % 2 == 0;
    
    bool CanBeSplit(RectRoom room) => room.width > (minRoomSize + wallThickness) * 2 || room.height > (minRoomSize + wallThickness) * 2;
    
    bool MustBeSplit(RectRoom room) => room.width > (maxRoomSize - wallThickness) || room.height > (maxRoomSize - wallThickness);
}
