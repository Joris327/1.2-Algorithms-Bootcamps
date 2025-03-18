using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    //enums
    enum SplitAbility { cannot, horizontally, vertically, bothSides }
    
    //private fields
    DoorGenerator doorGenerator;
    [HideInInspector] public bool doneGeneratingDoors = false;
    
    List<RectRoom> splitRooms = new();
    readonly Queue<RectRoom> toDoQueue = new();
    Graph<RectRoom> nodeGraph = new();
    
    System.Diagnostics.Stopwatch perDungeonWatch = new();
    System.Diagnostics.Stopwatch totalWatch = new();
    System.Random random = new();
    
    //serialized fields
    [Tooltip("The seed used to generate the dungeon. If 0, will generate new random seed for each dungeon. Else will use the same seed for every dungeon generated.")]
    [SerializeField] int seed = 0;
    [SerializeField] int startSeed = 0;
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
    
    //statistics
    List<int> totalGeneratedRoomsList = new();
    List<int> totalRoomsRemovedList = new();
    List<int> totalRoomsFinalDungeonList = new();
    List<double> DungeonGenerationTimesList = new();
    int DungeonsGeneratedCount = 0;

    //notes for tree map:
    //store for each room which rooms they touch.
    //take the last room in the list, connect to each touching room that does not have a connection yet.
    //go down the list, if a room has a connection, connect to every other room that does not yet have a connection.
    //this should result in a tree/spider web nodegraph.
    //for removing rooms: take rooms that only have one connection (and rooms) or maybe 0, and remove them until a certain percentage has been removed.

    #region Generator

    [Button("Generate")]
    void Start()
    {
        ClearGenerator();
        
        if (Application.isEditor && !Application.isPlaying) return;
        
        if (!TryGetComponent(out doorGenerator)) Debug.Log(name + ": could not find DoorGenerator on itself.", this);
        
        totalGeneratedRoomsList = new(amountToGenerate);
        totalRoomsRemovedList = new(amountToGenerate);
        totalRoomsFinalDungeonList = new(amountToGenerate);
        DungeonGenerationTimesList = new(amountToGenerate);
        
        DebugDrawingBatcher.GetInstance().BatchCall(DrawDungeon);
        
        totalWatch = System.Diagnostics.Stopwatch.StartNew();
        StartCoroutine(GenerateDungeon());
    }
    
    [Button("Clear Dungeon")]
    void ClearGenerator()
    {
        StopAllCoroutines();
        
        totalGeneratedRoomsList.Clear();
        totalRoomsRemovedList.Clear();
        totalRoomsFinalDungeonList.Clear();
        DungeonGenerationTimesList.Clear();
        nodeGraph = new();
        
        seed = startSeed;
        
        splitRooms.Clear();
        
        DungeonsGeneratedCount = 0;
        
        if (doorGenerator) doorGenerator.ClearGenerator();
        
        Debug.ClearDeveloperConsole();
        DebugDrawingBatcher.GetInstance().ClearCalls();
    }
    
    IEnumerator GenerateDungeon()
    {
        if (amountToGenerate < 1) StopCoroutine(GenerateDungeon());
        
        doneGeneratingDoors = false;
        int removedRooms = 0;
        int roomsMade = 0;
        
        splitRooms = new((worldHeight / minRoomSize) * (worldWidth / minRoomSize));
        toDoQueue.Clear();
        
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        else seed = startSeed;
        
        random = new(seed);
        
        perDungeonWatch = System.Diagnostics.Stopwatch.StartNew();
        
        RectRoom firstRoom = new(new(0, 0, worldWidth, worldHeight));
        toDoQueue.Enqueue(firstRoom);
        nodeGraph.AddNode(firstRoom); 
        
        if ( !CanBeSplit(toDoQueue.Peek()))
        {
            Debug.LogWarning("Unsplittable");
            splitRooms = toDoQueue.ToList();
            
            StopCoroutine(GenerateDungeon());
        }
        
        while (toDoQueue.Count > 0)
        {
            if (visualDelay > 0) yield return new WaitForSeconds(visualDelay);
            
            if (splitRooms.Count > guaranteedSplits && random.Next(0, 100) > chanceToSplit && !MustBeSplit(toDoQueue.Peek())) //decide if we're going to split or not
            {
                splitRooms.Add(toDoQueue.Dequeue());
                continue;
            }
            
            SplitRoom(toDoQueue.Dequeue(), ref roomsMade);
            removedRooms++;
            
            if (toDoQueue.Count > roomsLimit)
            {
                Debug.LogWarning("Reached room limit");
                splitRooms.AddRange(toDoQueue.ToList());
                break;
            }
        }
        
        splitRooms.TrimExcess();
        
        MakeSpanningTree();
        
        perDungeonWatch.Stop();
        
        DungeonsGeneratedCount++;
        
        if (amountToGenerate == 1)
        {
            totalWatch.Stop();
            Debug.Log("---------------------------------");
            Debug.Log("Rooms Generation time: " + Math.Round(perDungeonWatch.Elapsed.TotalMilliseconds, 3), this);
            Debug.Log("Rooms generated: " + roomsMade, this);
            Debug.Log("Rooms removed: " + removedRooms, this);
            Debug.Log("Rooms final: " + splitRooms.Count, this);
        }
        else if (DungeonsGeneratedCount < amountToGenerate)
        {
            totalGeneratedRoomsList.Add(roomsMade);
            totalRoomsRemovedList.Add(removedRooms);
            totalRoomsFinalDungeonList.Add(splitRooms.Count);
            DungeonGenerationTimesList.Add(Math.Round(perDungeonWatch.Elapsed.TotalMilliseconds, 3));
            doorGenerator.StartGenerator(splitRooms, visualDelay, wallThickness, seed, false, nodeGraph);
            yield return new WaitUntil(() => doneGeneratingDoors);
            
            StartCoroutine(GenerateDungeon());
        }
        else
        {
            totalWatch.Stop();
            totalGeneratedRoomsList.Add(roomsMade);
            totalRoomsRemovedList.Add(removedRooms);
            totalRoomsFinalDungeonList.Add(splitRooms.Count);
            DungeonGenerationTimesList.Add(Math.Round(perDungeonWatch.Elapsed.TotalMilliseconds, 3));
            
            Debug.Log("-------------------------------------------");
            Debug.Log("Total Rooms generation time: " + Math.Round(totalWatch.Elapsed.TotalMilliseconds, 3));
            Debug.Log("Average rooms generation time: " + DungeonGenerationTimesList.Average(), this);
            Debug.Log("Average rooms generated: " + totalGeneratedRoomsList.Average(), this);
            Debug.Log("Average rooms removed: " + totalRoomsRemovedList.Average(), this);
            Debug.Log("Average rooms final: " + totalRoomsFinalDungeonList.Average(), this);
            Debug.Log("Amount of dungeons generated: " + DungeonsGeneratedCount, this);
        }
        
        doorGenerator.StartGenerator(splitRooms, visualDelay, wallThickness, seed, true, nodeGraph);
    }
    
    void MakeSpanningTree()
    {
        // Graph<RectRoom> newGraph = new();
        // Queue<RectRoom> toDo = new();
        // RectRoom startNode = splitRooms[splitRooms.Count/2];
        
        // toDo.Enqueue(startNode);
        
        // while (toDo.Count > 0)
        // {
        //     foreach(RectRoom edge in nodeGraph.GetNeighbors(toDo.Peek()))
        //     {
        //         if (newGraph.HasKey(edge)) continue;
                
        //         newGraph.AddNode(edge);
        //         newGraph.AddEdge(toDo.Dequeue(), edge);
        //         toDo.Enqueue(edge);
        //     }
        // }
        
        // nodeGraph = newGraph;
        nodeGraph = nodeGraph.BFS(splitRooms[splitRooms.Count/2]);
    }
    
    #endregion
    #region Room Splitting
    
    void SplitRoom(RectRoom room, ref int roomsMade)
    {
        SplitAbility splitMode = DetermineSplitAbility(room);
        
        if (splitMode == SplitAbility.cannot)
        {
            Debug.Log("trigger");
            return;
        }
        if (splitMode == SplitAbility.bothSides) splitMode = (SplitAbility)random.Next(1, 3);
        
        RectRoom newRoom1;
        RectRoom newRoom2 = new(new(room.roomData.x, room.roomData.y, room.roomData.width, room.roomData.height));
        
        int adjustedMinRoomSize = minRoomSize + wallThickness * 3;
        
        if (splitMode == SplitAbility.vertically)
        {
            int splitPos = random.Next(adjustedMinRoomSize, room.roomData.width - adjustedMinRoomSize);
            
            newRoom1 = new(new(room.roomData.x + splitPos - wallThickness, room.roomData.y, room.roomData.width - splitPos + wallThickness, room.roomData.height));
            newRoom2.roomData.width = splitPos + wallThickness;
        }
        else
        {
            int splitPos = random.Next(adjustedMinRoomSize, room.roomData.height - adjustedMinRoomSize);
            
            newRoom1 = new(new(room.roomData.x, room.roomData.y + splitPos - wallThickness, room.roomData.width, room.roomData.height - splitPos + wallThickness));
            newRoom2.roomData.height = splitPos + wallThickness;
        }
        
        nodeGraph.AddNode(newRoom1);
        nodeGraph.AddNode(newRoom2);
        
        nodeGraph.AddEdge(newRoom1, newRoom2);
        
        foreach (RectRoom nearbyRoom in nodeGraph.GetNeighbors(room))
        {
            nodeGraph.GetNeighbors(nearbyRoom).Remove(room);
            RectInt room1Intersect = AlgorithmsUtils.Intersect(newRoom1.roomData, nearbyRoom.roomData);
            RectInt room2intersect = AlgorithmsUtils.Intersect(newRoom2.roomData, nearbyRoom.roomData);
            
            if (room1Intersect.width >= (wallThickness * 6) || room1Intersect.height >= (wallThickness * 6)) nodeGraph.AddEdge(newRoom1, nearbyRoom);
            if (room2intersect.width >= (wallThickness * 6) || room2intersect.height >= (wallThickness * 6)) nodeGraph.AddEdge(newRoom2, nearbyRoom);
        }
        
        nodeGraph.RemoveNode(room);
        
        if (CanBeSplit(newRoom2)) toDoQueue.Enqueue(newRoom2);
        else splitRooms.Add(newRoom2);
        
        if (CanBeSplit(newRoom1)) toDoQueue.Enqueue(newRoom1);
        else splitRooms.Add(newRoom1);
        
        roomsMade += 2;
    }
    
    #endregion
    #region Helper Methods
    
    void DrawDungeon()
    {
        foreach (var room in nodeGraph.GetGraph())
        {
            AlgorithmsUtils.DebugRectInt(room.Key.roomData, Color.yellow, 0, false, debugWallHeight);
            
            //lines between rooms
            // foreach (RectRoom r in room.Value)
            // {
            //     Vector3 rCenter = new (r.roomData.center.x, 0, r.roomData.center.y);
            //     Vector3 roomCenter = new (room.Key.roomData.center.x, 0, room.Key.roomData.center.y);
            //     Debug.DrawLine(rCenter, roomCenter, Color.red);
            // }
            
            //line between room and doors
            foreach (RectDoor d in room.Key.doors)
            {
                AlgorithmsUtils.DebugRectInt(d.doorData, Color.blue, 0, false, debugWallHeight);
                
                Vector3 doorCenter = new (d.doorData.center.x, 0, d.doorData.center.y);
                Vector3 roomCenter = new (room.Key.roomData.center.x, 0, room.Key.roomData.center.y);
                Debug.DrawLine(doorCenter, roomCenter);
            }
        }
        
        DebugExtension.DebugLocalCube( //shows world border
            transform,
            new Vector3(worldWidth, 0, worldHeight),
            new Vector3(worldWidth/2f, 0, worldHeight/2f)
        );
    }
    
    bool IsEven(int value) => value % 2 == 0;
    
    bool CanBeSplit(RectRoom room) => room.roomData.width > (minRoomSize * 2) + (wallThickness * 6) || room.roomData.height > (minRoomSize * 2) + (wallThickness * 6);
    
    bool MustBeSplit(RectRoom room) => room.roomData.width > (maxRoomSize - (wallThickness * 4)) || room.roomData.height > (maxRoomSize - (wallThickness * 4));
    
    SplitAbility DetermineSplitAbility(RectRoom room)
    {
        //the smallest size at which a room can be split and produce 2 new rooms equal or bigger than minRoomSize.
        int splitSize = (minRoomSize * 2) + (wallThickness * 6);
        
        //if both sides cannot be split
        if (room.roomData.width <= splitSize && room.roomData.height <= splitSize) return SplitAbility.cannot;
        
        //if one side cannot be split
        if (room.roomData.width <= splitSize) return SplitAbility.horizontally;
        if (room.roomData.height <= splitSize) return SplitAbility.vertically;
        
        //if both sides can be split
        return SplitAbility.bothSides;
    }
    
    #endregion
}
