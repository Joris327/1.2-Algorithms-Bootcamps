using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    #region Variables
    
    //enums
    enum SplitAbility { cannot, horizontally, vertically, bothSides }
    
    //private fields
    [HideInInspector] public bool doneGeneratingDoors = false;
    int doorSize;
    
    Graph<RectRoom> nodeGraph = new();
    
    System.Random random = new();
    
    [SerializeField] AwaitableUtils awaitableUtils;
    
    //serialized fields
    [Tooltip("The seed used to generate the dungeon. If 0, will generate new random seed for each dungeon. Else will use the same seed for every dungeon generated.")]
    [SerializeField] int seed = 0;
    [SerializeField] int startSeed = 0;
    [SerializeField, Min(0)] int dungeonsToGenerate = 1;
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
    
    [Header("Room Removal")]
    [SerializeField, Range(0, 100)] float roomRemovalPercentage = 10;
    
    [Header("Walls")]
    [SerializeField, Min(0)] int wallThickness = 1;
    [SerializeField, Min(0)] float debugWallHeight = 5;
    
    //statistics
    List<int> totalGeneratedRoomsList = new();
    List<int> totalRoomsRemovedList = new();
    List<int> totalRoomsFinalDungeonList = new();
    List<double> DungeonGenerationTimesList = new();
    int dungeonsGeneratedCount = 0;
    
    int roomsGenerated = 0;
    int roomsSplit = 0;
    int roomsRemoved = 0;
    int doorsGenerated = 0;
    
    #endregion
    #region Buttons
    
    [Button("Generate")]
    void GenerateButton()
    {
        StartGenerator();
    }
    
    [Button("Clear Dungeon")]
    void ClearDungeonButton()
    {
        ClearGenerator();
    }
    
    #endregion
    #region Awake/Start/Update
    void Awake()
    {
        doorSize = wallThickness * 2;
    }
    
    void Start()
    {
        StartGenerator();
    }
    
    #endregion
    #region Generator
    
    void StartGenerator()
    {
        if (Application.isEditor && !Application.isPlaying) return;
        if (dungeonsToGenerate < 1) return;
        
        ClearGenerator();
        StartCoroutine(Generate());
    }
    
    void ClearGenerator()
    {
        StopAllCoroutines();
        
        dungeonsGeneratedCount = 0;
        
        totalGeneratedRoomsList.Clear();
        totalRoomsRemovedList.Clear();
        totalRoomsFinalDungeonList.Clear();
        DungeonGenerationTimesList.Clear();
        
        seed = startSeed;
        nodeGraph.Clear();
        
        Debug.ClearDeveloperConsole();
        DebugDrawingBatcher.GetInstance().ClearCalls();
    }
    
    async Awaitable Generate()
    {
        System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
        
        SetupGenerator();
        
        System.Diagnostics.Stopwatch roomGenerationWatch = System.Diagnostics.Stopwatch.StartNew();
        await GenerateRooms();
        roomGenerationWatch.Stop();
        
        System.Diagnostics.Stopwatch spanningTreeWatch = System.Diagnostics.Stopwatch.StartNew();
        ConvertToSpanningTree();
        spanningTreeWatch.Stop();
        
        System.Diagnostics.Stopwatch roomRemovalWatch = System.Diagnostics.Stopwatch.StartNew();
        await RemoveRooms();
        roomRemovalWatch.Stop();
        
        System.Diagnostics.Stopwatch doorGenerationWatch = System.Diagnostics.Stopwatch.StartNew();
        await PlaceDoors();
        doorGenerationWatch.Stop();
        
        totalWatch.Stop();
        dungeonsGeneratedCount++;
        
        Debug.Log("---");
        Debug.Log("Total generation time: " + Math.Round(totalWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("Room generarion time: " + Math.Round(roomGenerationWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Rooms generated: " + roomsGenerated);
        Debug.Log("    Rooms Split: " + roomsSplit);
        Debug.Log("Convert to spanning tree time: " + Math.Round(spanningTreeWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("Room removal time: " + Math.Round(roomRemovalWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Rooms Removed: " + roomsRemoved);
        Debug.Log("Door generation time: " + Math.Round(doorGenerationWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Doors generated: " + doorsGenerated);
        
        if (dungeonsGeneratedCount < dungeonsToGenerate) StartCoroutine(Generate());
    }
    
    void SetupGenerator()
    {
        nodeGraph.Clear();
        
        DebugDrawingBatcher.GetInstance().BatchCall(DrawDungeon);
        
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        else seed = startSeed;
        
        random = new(seed);
        
        roomsSplit = 0;
        roomsGenerated = 0;
        roomsRemoved = 0;
    }
    
    #endregion
    #region Room Splitting
    async Awaitable GenerateRooms()
    {
        Queue<RectRoom> toDoQueue = new();
        RectRoom firstRoom = new(new(0, 0, worldWidth, worldHeight));
        toDoQueue.Enqueue(firstRoom);
        nodeGraph.AddNode(firstRoom);
        
        if ( !CanBeSplit(toDoQueue.Peek()))
        {
            Debug.LogWarning("Unsplittable");
            return;
        }
        
        while (toDoQueue.Count > 0)
        {
            if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
            if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
            
            if (nodeGraph.KeyCount() > guaranteedSplits && !MustBeSplit(toDoQueue.Peek()) && random.Next(0, 100) > chanceToSplit) //decide if we're going to split or not
            {
                nodeGraph.AddNode(toDoQueue.Dequeue());
                continue;
            }
            
            RectRoom roomToSplit = toDoQueue.Dequeue();
            SplitAbility splitAbility = DetermineSplitAbility(roomToSplit);
            
            if (splitAbility != SplitAbility.cannot)
            {
                SplitRoom(roomToSplit, splitAbility, toDoQueue);
                roomsGenerated += 2;
                roomsSplit++;
            }
            
            if (toDoQueue.Count > roomsLimit)
            {
                Debug.LogWarning("Reached room limit");
                while (toDoQueue.Count > 0)
                {
                    nodeGraph.AddNode(toDoQueue.Dequeue());
                }
                break;
            }
        }
    }
    
    void SplitRoom(RectRoom room, SplitAbility splitAbility, Queue<RectRoom> toDoQueue)
    {
        if (splitAbility == SplitAbility.cannot)
        {
            Debug.LogWarning("Tried to split unsplittable room");
            return;
        }
        if (splitAbility == SplitAbility.bothSides) splitAbility = (SplitAbility)random.Next(1, 3);
        
        RectRoom newRoom1;
        RectRoom newRoom2 = new(new(room.roomData.x, room.roomData.y, room.roomData.width, room.roomData.height));
        
        int adjustedMinRoomSize = minRoomSize + wallThickness * 3;
        
        if (splitAbility == SplitAbility.vertically)
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
        
        foreach (RectRoom nearbyRoom in nodeGraph.Edges(room))
        {
            nodeGraph.Edges(nearbyRoom).Remove(room);
            RectInt room1Intersect = AlgorithmsUtils.Intersect(newRoom1.roomData, nearbyRoom.roomData);
            RectInt room2intersect = AlgorithmsUtils.Intersect(newRoom2.roomData, nearbyRoom.roomData);
            
            if (OverlapsProperly(room1Intersect)) nodeGraph.AddEdge(newRoom1, nearbyRoom);
            if (OverlapsProperly(room2intersect)) nodeGraph.AddEdge(newRoom2, nearbyRoom);
        }
        
        nodeGraph.RemoveNode(room);
        
        if (CanBeSplit(newRoom2)) toDoQueue.Enqueue(newRoom2);
        else nodeGraph.AddNode(newRoom2);
        
        if (CanBeSplit(newRoom1)) toDoQueue.Enqueue(newRoom1);
        else nodeGraph.AddNode(newRoom1);
    }
    
    bool OverlapsProperly(RectInt overlap) => overlap.width >= (wallThickness * 4) + doorSize || overlap.height >= (wallThickness * 4) + doorSize;
    
    #endregion
    #region Spanning Tree
    void ConvertToSpanningTree()
    {
        RectRoom[] keys = nodeGraph.Keys();
        RectRoom startNode = keys[0];
        foreach (RectRoom room in keys)
        {
            if (room.roomData.size.magnitude > startNode.roomData.size.magnitude)
            {
                startNode = room;
            }
        }
        
        Stack<RectRoom> toDo = new();
        toDo.Push(startNode);
        
        Graph<RectRoom> discovered = new();
        discovered.AddNode(startNode);
        
        while (toDo.Count > 0)
        {
            RectRoom node = toDo.Pop();
            
            List<RectRoom> sortedEdges = nodeGraph.Edges(node).OrderBy(t => t.roomData.size.magnitude).ToList();
            
            foreach (RectRoom connectedNode in sortedEdges)
            {
                if (discovered.ContainsKey(connectedNode)) continue;
                
                toDo.Push(connectedNode);
                discovered.AddNode(connectedNode);
                discovered.AddEdge(node, connectedNode);
            }
        }
        
        nodeGraph = discovered;
    }
    
    #endregion
    #region Room Removal
    
    async Awaitable RemoveRooms()
    {
        if (roomRemovalPercentage <= 0) return;
        
        int amountToRemove = Mathf.RoundToInt(nodeGraph.KeyCount() * (roomRemovalPercentage / 100));
        
        while (roomsRemoved < amountToRemove)
        {
            List<RectRoom> toRemoveList = new();
            RectRoom[] roomsList = nodeGraph.Keys();
            
            foreach (var item in roomsList)
            {
                if (nodeGraph.EdgeCount(item) < 2) toRemoveList.Add(item);
            }
            
            for (int i = toRemoveList.Count; i > 0; i--)
            {
                if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
                if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
                
                RectRoom nodeToRemove = toRemoveList[random.Next(0, toRemoveList.Count)];
                
                toRemoveList.Remove(nodeToRemove);
                nodeGraph.RemoveNode(nodeToRemove);
                roomsRemoved++;
                
                if (roomsRemoved >= amountToRemove) break;
                if (nodeGraph.KeyCount() == 1) break;
            }
            
            if (nodeGraph.KeyCount() == 1) break;
        }
    }
    
    #endregion
    #region Door Placement
    
    async Awaitable PlaceDoors()
    {
        RectRoom[] keyList = nodeGraph.Keys();
        foreach (RectRoom key in keyList)
        {
            List<RectRoom> connectionsList = new(nodeGraph.Edges(key));
            
            for (int i = 0; i < connectionsList.Count; i++)
            {
                RectRoom connectedRoom = connectionsList[i];
                
                if (key.doors.Any(connectedRoom.doors.Contains)) continue;
                
                if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
                if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
                
                RectInt overLap = AlgorithmsUtils.Intersect(key.roomData, connectedRoom.roomData);
                
                RectDoor newDoor;
                int xPos;
                int yPos;
                
                if (overLap.width > overLap.height)
                {
                    xPos = random.Next(
                        Math.Max(key.roomData.xMin, connectedRoom.roomData.xMin) + (wallThickness * 2), 
                        Math.Min(key.roomData.xMax, connectedRoom.roomData.xMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    if (key.roomData.y < connectedRoom.roomData.y) yPos = key.roomData.yMax - doorSize;
                    else yPos = key.roomData.yMin;
                }
                else
                {
                    yPos = random.Next(
                        Math.Max(key.roomData.yMin, connectedRoom.roomData.yMin) + (wallThickness * 2),
                        Math.Min(key.roomData.yMax, connectedRoom.roomData.yMax) - (wallThickness * 2) - doorSize + 1
                    );
                    
                    if (key.roomData.x < connectedRoom.roomData.x) xPos = key.roomData.xMax - doorSize;
                    else xPos = key.roomData.xMin;
                }
                
                newDoor = new(new(xPos, yPos, doorSize, doorSize));
                
                key.doors.Add(newDoor);
                connectedRoom.doors.Add(newDoor);
                
                doorsGenerated++;
            }
        }
    }
    
    #endregion
    #region Helper Methods
    
    void DrawDungeon()
    {
        foreach (var room in nodeGraph.GetGraph())
        {
            AlgorithmsUtils.DebugRectInt(room.Key.roomData, Color.yellow, 0, false, debugWallHeight);
            RectInt innerWall = room.Key.roomData;
            innerWall.x += 2;
            innerWall.y += 2;
            innerWall.width -= 4;
            innerWall.height -= 4;
            AlgorithmsUtils.DebugRectInt(innerWall, Color.yellow, 0, false, debugWallHeight);
            DebugExtension.DebugCircle(new(innerWall.center.x, 0, innerWall.center.y));
            
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
