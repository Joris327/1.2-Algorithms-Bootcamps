using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NaughtyAttributes;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Generates dungeons
/// </summary>
[RequireComponent(typeof(NavMeshSurface))]
public class DungeonGenerator : MonoBehaviour
{
    #region Variables
    
    //enums
    enum SplitAbility { cannot, horizontally, vertically, bothSides }
    enum VisualsMethod { simple, marchingSquares }
    
    //private fields
    readonly List<RectDoor> doors = new();
    int doorSize;
    
    Graph<RectRoom> nodeGraph = new();
    Zone[,] zones = new Zone[0,0];
    
    System.Random random = new();
    
    GameObject visualsContainer;
    
    //public fields
    public Vector2Int WorldSize { get { return new(worldWidth, worldHeight); } }
    public RectRoom GetFirstRoom { get { return nodeGraph.First(); } }
    public RectRoom[] GetRooms { get { return nodeGraph.Keys(); } }
    public RectDoor[] GetDoors { get { return doors.ToArray(); } }
    public int WallThickness { get { return wallThickness; } }
    
    //serialized fields
    [SerializeField] AwaitableUtils awaitableUtils;
    [Tooltip("Please use the \'Start Seed\' value below to set the seed. Don't modify this field directly.")]
    [SerializeField] int seed = 0;
    [Tooltip("The seed used to generate the dungeon. If 0, will generate new random seed for each dungeon. Else will use the given seed for every dungeon generated.")]
    [SerializeField] int startSeed = 0;
    [SerializeField, Min(0)] int dungeonsToGenerate = 1;
    //[SerializeField, Min(0)] float visualDelay = 0.5f;
    [SerializeField] Player player;
    
    [Header("World")]
    [SerializeField, Min(0)] int worldWidth = 100;
    [SerializeField, Min(0)] int worldHeight = 100;
    [SerializeField, Min(1)] Vector2Int zoneAmount = new(10, 10);
    
    [Header("Rooms")]
    [SerializeField] int minRoomSize = 10;
    [SerializeField] int maxRoomSize = 20;
    [SerializeField, Min(0)] int roomsLimit = 1000;
    [SerializeField, Range(0, 100)] int chanceToSplit = 90;
    [SerializeField] bool useZones = true;
    
    /// <summary>
    /// How many times to guarantee rooms split before applying a chance. Prevents dungeon-sized rooms.
    /// </summary>
    [Tooltip("How many times to guarantee rooms split before applying a chance. Prevents dungeon-sized rooms.")]
    [SerializeField, Min(0)] int guaranteedSplits = 4;
    
    [Header("Room Removal")]
    [SerializeField, Range(0, 100)] float roomRemovalPercentage = 10;
    
    [Header("Walls")]
    [SerializeField, Min(0)] int wallThickness = 1;
    
    [Header("Debug")]
    [SerializeField] bool debugDraw = true;
    [SerializeField, Min(0)] float debugWallHeight = 5;
    [SerializeField] bool drawRoomConnections = false;
    [SerializeField] bool drawDoorConnections = true;
    [SerializeField] bool drawZones = true;
    [SerializeField] bool drawWorldBorder = true;
    [SerializeField] bool drawRoomCenters = true;
    [SerializeField, Min(0)] float duration = 0;
    
    [Header("Visuals")]
    [SerializeField] bool createVisuals = true;
    [SerializeField] VisualsMethod visualsMethod = VisualsMethod.simple;
    [SerializeField] VisualsGenerator visualsGenerator;
    
    
    //statistics
    List<int> totalGeneratedRoomsList = new();
    List<int> totalRoomsRemovedList = new();
    List<int> totalRoomsFinalDungeonList = new();
    List<double> DungeonGenerationTimesList = new();
    int dungeonsGeneratedCount = 0;
    
    int roomsGenerated = 0;
    int roomsSplit = 0;
    int roomsBeforeRemoval = 0;
    int roomsRemoved = 0;
    int doorsGenerated = 0;
    
    #endregion
    #region Buttons
    
    [Button("Generate")]
    async void GenerateButton()
    {
        await StartGenerator();
    }
    
    [Button("Clear Dungeon")]
    void ClearDungeonButton()
    {
        ClearGenerator();
    }
    
    #endregion
    #region Awake/Start/Update
    
    async void Start()
    {
        await StartGenerator();
    }
    
    #endregion
    #region Generator
    
    /// <summary>
    /// start generating a new dungeon
    /// </summary>
    async Task StartGenerator()
    {
        if (Application.isEditor && !Application.isPlaying) return;
        if (dungeonsToGenerate < 1) return;
        
        
        Debug.Log("Generating...");
        
        ClearGenerator();
        if (awaitableUtils.visualDelay > 0 || awaitableUtils.waitForKey != KeyCode.None) DebugDrawingBatcher.GetInstance().BatchCall(DrawDungeon);
        
        await Generate();
        
        if (awaitableUtils.visualDelay == 0 && awaitableUtils.waitForKey == KeyCode.None) DebugDrawingBatcher.GetInstance().BatchCall(DrawDungeon);
    }
    
    /// <summary>
    /// Clear all existing data for the dungeon to prepare it for generation.
    /// </summary>
    void ClearGenerator()
    {
        Debug.ClearDeveloperConsole();
        StopAllCoroutines();
        
        dungeonsGeneratedCount = 0;
        
        totalGeneratedRoomsList.Clear();
        totalRoomsRemovedList.Clear();
        totalRoomsFinalDungeonList.Clear();
        DungeonGenerationTimesList.Clear();
        
        seed = startSeed;
        nodeGraph.Clear();
        doors.Clear();
        
        Destroy(visualsContainer);
        
        DebugDrawingBatcher.GetInstance().ClearCalls();
    }
    
    /// <summary>
    /// called by the StartGenerator() method. manages the generation process. 
    /// </summary>
    async Task Generate()
    {
        Camera.main.transform.position = new(
            worldWidth/2,
            worldHeight > worldWidth ? worldHeight : worldWidth,
            worldHeight/2
        );
        
        if (awaitableUtils.visualDelay == 0 && awaitableUtils.waitForKey == KeyCode.None) await Awaitable.BackgroundThreadAsync();
        System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Stopwatch dataWatch = System.Diagnostics.Stopwatch.StartNew();
        
        SetupGenerator();
        
        System.Diagnostics.Stopwatch roomGenerationWatch = System.Diagnostics.Stopwatch.StartNew();
        await GenerateRooms();
        roomGenerationWatch.Stop();
        
        System.Diagnostics.Stopwatch zoneGenerationWatch = System.Diagnostics.Stopwatch.StartNew();
        if (useZones) await GenerateZones();
        zoneGenerationWatch.Stop();
        
        roomsBeforeRemoval = nodeGraph.KeyCount();
        
        System.Diagnostics.Stopwatch spanningTreeWatch = System.Diagnostics.Stopwatch.StartNew();
        await ConvertToSpanningTree();
        spanningTreeWatch.Stop();
        
        System.Diagnostics.Stopwatch roomRemovalWatch = System.Diagnostics.Stopwatch.StartNew();
        await RemoveRooms();
        roomRemovalWatch.Stop();
        
        System.Diagnostics.Stopwatch doorGenerationWatch = System.Diagnostics.Stopwatch.StartNew();
        await PlaceDoors();
        doorGenerationWatch.Stop();
        
        dataWatch.Stop();
        
        await Awaitable.MainThreadAsync();
        
        if (createVisuals)
        {
            if (visualsMethod == VisualsMethod.simple) await visualsGenerator.CreateSimpleVisuals(nodeGraph, doors.ToArray());
            else await visualsGenerator.Generate();
        }

        Vector2 roomCenter = nodeGraph.First().roomData.center;
        Vector3 newPos = new(roomCenter.x, 1, roomCenter.y);
        player.transform.position = newPos;
        
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        agent.nextPosition = newPos;
        
        totalWatch.Stop();
        dungeonsGeneratedCount++;
        await Awaitable.MainThreadAsync();
        
        Debug.Log("---");
        Debug.Log("Room generarion time: " + Math.Round(roomGenerationWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Rooms generated: " + roomsGenerated);
        Debug.Log("    Rooms Split: " + roomsSplit);
        Debug.Log("    Rooms Total: " + roomsBeforeRemoval);
        Debug.Log("Zone generarion time: " + Math.Round(zoneGenerationWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("Convert to spanning tree time: " + Math.Round(spanningTreeWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("Room removal time: " + Math.Round(roomRemovalWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Rooms Removed: " + roomsRemoved);
        Debug.Log("    Rooms left: " + nodeGraph.KeyCount());
        Debug.Log("Door generation time: " + Math.Round(doorGenerationWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Doors generated: " + doorsGenerated);
        Debug.Log("Data generation time: " + Math.Round(dataWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("Total generation time: " + Math.Round(totalWatch.Elapsed.TotalMilliseconds, 3));
        
        if (dungeonsGeneratedCount < dungeonsToGenerate)
        {
            await StartGenerator();
            return;
        }
        
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }
    
    /// <summary>
    /// prepares the generator for a new cycle
    /// </summary>
    void SetupGenerator()
    {
        nodeGraph.Clear();
        zones = new Zone[zoneAmount.x, zoneAmount.y];
        
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        else seed = startSeed;
        
        random = new(seed);
        doorSize = wallThickness * 2;
        
        roomsSplit = 0;
        roomsGenerated = 0;
        roomsRemoved = 0;
        roomsBeforeRemoval = 0;
        
        DebugDrawingBatcher.duration = duration;
    }
    
    #endregion
    #region Room Splitting
    /// <summary>
    /// populate the dungeon with rooms, by making one big room and splitting it until all rooms are between a minimum and maximum size.
    /// </summary>
    async Task GenerateRooms()
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
            //if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
            //if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
            await awaitableUtils.Delay();
            
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
    
    /// <summary>
    /// split a single room and add it to the queue if it can be split further.
    /// </summary>
    void SplitRoom(RectRoom room, SplitAbility splitAbility, Queue<RectRoom> toDoQueue)
    {
        if (splitAbility == SplitAbility.cannot)
        {
            Debug.LogWarning("Tried to split unsplittable room");
            return;
        }
        if (splitAbility == SplitAbility.bothSides) splitAbility = (SplitAbility)random.Next(1, 3);
        
        RectRoom newRoom1;
        RectRoom newRoom2 = new(new RectInt(room.roomData.x, room.roomData.y, room.roomData.width, room.roomData.height));
        
        int adjustedMinRoomSize = minRoomSize + (wallThickness * 3);
        
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
        
        if (!useZones)
        {
            nodeGraph.AddEdge(newRoom1, newRoom2);
        
            foreach (RectRoom nearbyRoom in nodeGraph.Edges(room))
            {
                nodeGraph.Edges(nearbyRoom).Remove(room);
                RectInt room1Intersect = AlgorithmsUtils.Intersect(newRoom1.roomData, nearbyRoom.roomData);
                RectInt room2intersect = AlgorithmsUtils.Intersect(newRoom2.roomData, nearbyRoom.roomData);
                
                if (OverlapsProperly(room1Intersect)) nodeGraph.AddEdge(newRoom1, nearbyRoom);
                if (OverlapsProperly(room2intersect)) nodeGraph.AddEdge(newRoom2, nearbyRoom);
            }
        }
        
        nodeGraph.RemoveNode(room);
        
        if (CanBeSplit(newRoom2)) toDoQueue.Enqueue(newRoom2);
        else nodeGraph.AddNode(newRoom2);
        
        if (CanBeSplit(newRoom1)) toDoQueue.Enqueue(newRoom1);
        else nodeGraph.AddNode(newRoom1);
    }
    
    /// <summary>
    /// checks if the overlap between two rooms is big enough to fit a door along their shared wall.
    /// </summary>
    bool OverlapsProperly(RectInt overlap) => overlap.width >= (wallThickness * 4) + doorSize || overlap.height >= (wallThickness * 4) + doorSize;
    
    #endregion
    #region Generating Zones
    /// <summary>
    /// generates zones which will be used to organise the dungeon into smaller chunks. this will speed up overlap checking later
    /// </summary>
    async Task GenerateZones()
    {
        int zoneWidth = worldWidth / zoneAmount.x;
        int zoneheight = worldHeight / zoneAmount.y;
        
        if ((worldWidth + 0f / zoneAmount.x) % 1 > 0) zoneWidth++;
        if ((worldHeight + 0f / zoneAmount.y) % 1 > 0) zoneheight++;
        
        for (int i = 0; i < zoneAmount.x; i++)
        {
            for (int j = 0; j < zoneAmount.y; j++)
            {
                //if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
                //if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
                await awaitableUtils.Delay();
                
                Zone newZone = new(new RectInt(i * zoneWidth, j * zoneheight, zoneWidth, zoneheight));
                zones[i, j] = newZone;
            }
        }
        
        RectRoom[] keys = nodeGraph.Keys();
        foreach (RectRoom room in keys)
        {
            Vector2Int topLeft = new(room.roomData.xMin / zoneWidth, room.roomData.yMax / zoneheight);
            Vector2Int topRight = new(room.roomData.xMax / zoneWidth, topLeft.y);
            Vector2Int bottomLeft = new(topLeft.x, room.roomData.yMin / zoneheight);
            Vector2Int bottomRight = new(topRight.x, bottomLeft.y);
            
            if (topLeft.x < zones.GetLength(0)
             && topLeft.y < zones.GetLength(1))
            {
                zones[topLeft.x, topLeft.y].rooms.Add(room);
            }
            
            if (topRight.x != topLeft.x
             && topRight.x < zones.GetLength(0)
             && topRight.y < zones.GetLength(1))
            {
                zones[topRight.x, topRight.y].rooms.Add(room);
            }
            
            if (bottomLeft.y != topLeft.y
             && bottomLeft.x < zones.GetLength(0)
             && bottomLeft.y < zones.GetLength(1))
            {
                zones[bottomLeft.x, bottomLeft.y].rooms.Add(room);
            }
            
            if (bottomRight.x != bottomLeft.x
             && bottomRight.y != topRight.y
             && bottomRight.x < zones.GetLength(0)
             && bottomRight.y < zones.GetLength(1))
            {
                zones[bottomRight.x, bottomRight.y].rooms.Add(room);
            }
        }
        
        foreach (Zone zone in zones)
        {
            for (int i = 0; i < zone.rooms.Count(); i++)
            {
                RectRoom roomA = zone.rooms[i];
                for (int j = i + 1; j < zone.rooms.Count(); j++)
                {
                    RectRoom roomB = zone.rooms[j];
                    RectInt overlap = AlgorithmsUtils.Intersect(roomA.roomData, roomB.roomData);
                    if (OverlapsProperly(overlap)) nodeGraph.AddEdge(roomA, roomB);
                }
            }
        }
    }
    
    #endregion
    #region Spanning Tree
    /// <summary>
    /// removes edges from the nodegraph until a spanning tree is left.
    /// </summary>
    async Task ConvertToSpanningTree()
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
            if (drawRoomConnections)
            {
                //if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
                //if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
                await awaitableUtils.Delay();
            }
            
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
        
        Debug.Log(nodeGraph.KeyCount() == discovered.KeyCount());
        
        nodeGraph = discovered;
    }
    
    #endregion
    #region Room Removal
    /// <summary>
    /// remove rooms from the graph until the desired amount remains
    /// </summary>
    async Task RemoveRooms()
    {
        if (roomRemovalPercentage <= 0) return;
        
        int amountToRemove = Mathf.RoundToInt(nodeGraph.KeyCount() * (roomRemovalPercentage / 100));
        
        List<RectRoom> toRemoveList = new();
        RectRoom[] roomsList = nodeGraph.Keys();
        
        foreach (var item in roomsList)
        {
            if (nodeGraph.EdgeCount(item) < 2) toRemoveList.Add(item);
        }
        
        while (roomsRemoved < amountToRemove)
        {
            await awaitableUtils.Delay();
            
            RectRoom nodeToRemove = toRemoveList[random.Next(0, toRemoveList.Count)];
            
            RectRoom adjacentRoom = nodeGraph.Edges(nodeToRemove)[0];
            if (nodeGraph.Edges(adjacentRoom).Count == 0) toRemoveList.Add(adjacentRoom);
            
            toRemoveList.Remove(nodeToRemove);
            nodeGraph.RemoveNode(nodeToRemove);
            roomsRemoved++;
            
            if (nodeGraph.KeyCount() == 1) break;
        }
    }
    
    #endregion
    #region Door Placement
    /// <summary>
    /// places doors at valid positions along a shared wall between rooms.
    /// </summary>
    async Task PlaceDoors()
    {
        RectRoom[] keyList = nodeGraph.Keys();
        foreach (RectRoom key in keyList)
        {
            List<RectRoom> connectionsList = new(nodeGraph.Edges(key));
            
            for (int i = 0; i < connectionsList.Count; i++)
            {
                RectRoom connectedRoom = connectionsList[i];
                
                if (key.doors.Any(connectedRoom.doors.Contains)) continue;
                
                await awaitableUtils.Delay();
                
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
                
                newDoor = new RectDoor(new RectInt(xPos, yPos, doorSize, doorSize));
                doors.Add(newDoor);
                
                key.doors.Add(doors.Count-1);
                connectedRoom.doors.Add(doors.Count-1);
                
                doorsGenerated++;
            }
        }
    }
    
    #endregion
    #region Helper Methods
    /// <summary>
    /// draws a 2d representation of the dungeon data
    /// </summary>
    void DrawDungeon()
    {
        if (!debugDraw) return;
        
        foreach (var room in nodeGraph.GetGraph())
        {
            if (room.Value.Count > 0) AlgorithmsUtils.DebugRectInt(room.Key.roomData, Color.yellow, duration, false, debugWallHeight);
            else AlgorithmsUtils.DebugRectInt(room.Key.roomData, Color.red, duration, false, debugWallHeight);
            
            RectInt innerWall = room.Key.roomData;
            innerWall.x += wallThickness*2;
            innerWall.y += wallThickness*2;
            innerWall.width -= wallThickness*4;
            innerWall.height -= wallThickness*4;
            
            if (room.Value.Count > 0) AlgorithmsUtils.DebugRectInt(innerWall, Color.yellow, duration, false, debugWallHeight);
            else AlgorithmsUtils.DebugRectInt(innerWall, Color.red, duration, false, debugWallHeight);
            
            if (drawRoomCenters) DebugExtension.DebugCircle(new(innerWall.center.x, 0, innerWall.center.y), 1, duration);
            
            if (drawRoomConnections)
            {
                foreach (RectRoom r in room.Value)
                {
                    Vector3 rCenter = new(r.roomData.center.x, 0, r.roomData.center.y);
                    Vector3 roomCenter = new(room.Key.roomData.center.x, 0, room.Key.roomData.center.y);
                    Debug.DrawLine(rCenter, roomCenter, Color.red, duration);
                }
            }
            
            if (drawDoorConnections)
            {
                foreach (int d in room.Key.doors)
                {
                    RectDoor door = doors[d];
                    AlgorithmsUtils.DebugRectInt(door.doorData, Color.blue, duration, false, debugWallHeight);
                    
                    Vector3 doorCenter = new(door.doorData.center.x, 0, door.doorData.center.y);
                    Vector3 roomCenter = new(room.Key.roomData.center.x, 0, room.Key.roomData.center.y);
                    Debug.DrawLine(doorCenter, roomCenter, Color.white, duration);
                }
            }
        }
        
        if (drawZones)
        {
            foreach (Zone z in zones)
            {
                if (z == null) continue;
                AlgorithmsUtils.DebugRectInt(z.data, Color.green, 0, false, 0);
            }
        }
        
        if (drawWorldBorder)
        {
            RectInt wordBorder = new(0, 0, worldWidth, worldHeight);
            AlgorithmsUtils.DebugRectInt(wordBorder, Color.white, duration, false, 0);
        }
    }
    
    /// <summary>
    /// checks whether the given value is even or not.
    /// </summary>
    bool IsEven(int value) => value % 2 == 0;
    
    /// <summary>
    /// checks whether a room can be split in a way that leaves both new rooms at at least minimum room size.
    /// </summary>
    bool CanBeSplit(RectRoom room) => room.roomData.width > (minRoomSize * 2) + (wallThickness * 6) || room.roomData.height > (minRoomSize * 2) + (wallThickness * 6);
    
    /// <summary>
    /// checks whether a room is so big that it must be split (bigger then maxRoomSize).
    /// </summary>
    bool MustBeSplit(RectRoom room) => room.roomData.width > (maxRoomSize - (wallThickness * 4)) || room.roomData.height > (maxRoomSize - (wallThickness * 4));
    
    /// <summary>
    /// determine whether a room can be split horizontally, vertically, both or not.
    /// </summary>
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
