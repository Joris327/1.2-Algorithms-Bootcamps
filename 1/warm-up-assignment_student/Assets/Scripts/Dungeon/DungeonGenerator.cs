using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    #region Variables
    
    //enums
    enum SplitAbility { cannot, horizontally, vertically, bothSides }
    
    //private fields
    DoorGenerator doorGenerator;
    [HideInInspector] public bool doneGeneratingDoors = false;
    int doorSize;
    
    
    Graph<RectRoom> nodeGraph = new();
    
    System.Diagnostics.Stopwatch perDungeonWatch = new();
    System.Random random = new();
    
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
    #region Awake/Start
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
        if (Application.isEditor && !Application.isPlaying)
        {
            StopAllCoroutines();
            return;
        }
        if (dungeonsToGenerate < 1)
        {
            StopAllCoroutines();
            return;
        }
        
        ClearGenerator();
        //StartCoroutine(Generate());
        Generate();
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
        nodeGraph = new();
        
        if (doorGenerator) doorGenerator.ClearGenerator();
        
        Debug.ClearDeveloperConsole();
        DebugDrawingBatcher.GetInstance().ClearCalls();
    }
    
    async void Generate()
    {
        System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
        
        SetupGenerator();
        
        System.Diagnostics.Stopwatch roomGenerationWatch = System.Diagnostics.Stopwatch.StartNew();
        await GenerateRooms();
        roomGenerationWatch.Stop();
        
        System.Diagnostics.Stopwatch spanningTreeWatch = System.Diagnostics.Stopwatch.StartNew();
        nodeGraph.ConvertToSpanningTree();
        spanningTreeWatch.Stop();
        
        System.Diagnostics.Stopwatch roomRemovalWatch = System.Diagnostics.Stopwatch.StartNew();
        await RemoveRooms();
        roomRemovalWatch.Stop();
        
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
        
        //if (dungeonsGeneratedCount < dungeonsToGenerate) StartCoroutine(Generate());
        if (dungeonsGeneratedCount < dungeonsToGenerate) Generate();
    }
    
    void SetupGenerator()
    {
        nodeGraph = new();
        
        DebugDrawingBatcher.GetInstance().BatchCall(DrawDungeon);
        
        if (startSeed == 0) seed = random.Next(0, int.MaxValue);
        else seed = startSeed;
        
        random = new(seed);
    }
    
    #endregion
    #region Room Splitting
    async Task GenerateRooms()
    {
        await Task.Factory.StartNew(() =>
        {
            roomsSplit = 0;
            roomsGenerated = 0;
            
            Queue<RectRoom> toDoQueue = new();
            RectRoom firstRoom = new(new(0, 0, worldWidth, worldHeight));
            toDoQueue.Enqueue(firstRoom);
            nodeGraph.AddNode(firstRoom);
            
            if ( !CanBeSplit(toDoQueue.Peek()))
            {
                Debug.LogWarning("Unsplittable");
                //yield break;
            }
            
            while (toDoQueue.Count > 0)
            {
                //if (visualDelay > 0) yield return new WaitForSeconds(visualDelay);
                if (visualDelay > 0) Thread.Sleep((int)(visualDelay / 1000));
                
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
        });
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
        
        foreach (RectRoom nearbyRoom in nodeGraph.Neighbors(room))
        {
            nodeGraph.Neighbors(nearbyRoom).Remove(room);
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
    #region Room Removal
    
    async Task RemoveRooms()
    {
        await Task.Factory.StartNew(() =>
        {
            roomsRemoved = 0;
        
            int amountToRemove = (int)(nodeGraph.KeyCount() * (roomRemovalPercentage / 100));
            
            while (roomsRemoved < amountToRemove)
            {
                for (int i = 0; i < nodeGraph.KeyCount(); i++)
                {
                    //if (visualDelay > 0) yield return new WaitForSeconds(visualDelay);
                    if (visualDelay > 0) Thread.Sleep((int)(visualDelay/1000));
                    if (nodeGraph.KeyCount() == 1) break;
                    
                    RectRoom nodeToRemove = nodeGraph.ElementAt(i);
                    
                    if (nodeGraph.Neighbors(nodeToRemove).Count > 1) continue;
                    
                    //spanningTree.RemoveNode(nodeToRemove);
                    nodeGraph.RemoveNode(nodeToRemove);
                    roomsRemoved++;
                }
                if (nodeGraph.KeyCount() == 1) break;
            }
        });
    }
    
    #endregion
    
    IEnumerator GenerateDungeon()
    {
        if (dungeonsToGenerate < 1) StopCoroutine(GenerateDungeon());
        
        System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
        LinkedList<RectRoom> splitRooms = new();
        
        doneGeneratingDoors = false;
        int removedRooms = 0;
        int roomsMade = 0;
        
        //splitRooms = new((worldHeight / minRoomSize) * (worldWidth / minRoomSize));
        splitRooms = new();
        Queue<RectRoom> toDoQueue = new();
        
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
            //splitRooms = toDoQueue.ToList();
            splitRooms.AddLast(toDoQueue.Dequeue());
            
            StopCoroutine(GenerateDungeon());
        }
        
        while (toDoQueue.Count > 0)
        {
            if (visualDelay > 0) yield return new WaitForSeconds(visualDelay);
            
            if (splitRooms.Count > guaranteedSplits && random.Next(0, 100) > chanceToSplit && !MustBeSplit(toDoQueue.Peek())) //decide if we're going to split or not
            {
                splitRooms.AddLast(toDoQueue.Dequeue());
                continue;
            }
            
           SplitAbility splitAbility = DetermineSplitAbility(toDoQueue.Peek());
            
            if (splitAbility != SplitAbility.cannot)
            {
                SplitRoom(toDoQueue.Dequeue(), splitAbility, toDoQueue);
                roomsMade += 2;
                removedRooms++;
            }
            
            if (toDoQueue.Count > roomsLimit)
            {
                Debug.LogWarning("Reached room limit");
                //splitRooms.Add(toDoQueue.ToList());
                while (toDoQueue.Count > 0)
                {
                    splitRooms.AddLast(toDoQueue.Dequeue());
                }
                break;
            }
        }
        
        //splitRooms.TrimExcess();
        System.Diagnostics.Stopwatch removeWatch = System.Diagnostics.Stopwatch.StartNew();
        RemoveRooms();
        removeWatch.Stop();
        Debug.Log("Time to remove rooms: " + removeWatch.Elapsed.TotalMilliseconds);
        MakeSpanningTree();
        
        perDungeonWatch.Stop();
        
        dungeonsGeneratedCount++;
        
        if (dungeonsToGenerate == 1)
        {
            totalWatch.Stop();
            Debug.Log("---------------------------------");
            Debug.Log("Rooms Generation time: " + Math.Round(perDungeonWatch.Elapsed.TotalMilliseconds, 3), this);
            Debug.Log("Rooms generated: " + roomsMade, this);
            Debug.Log("Rooms removed: " + removedRooms, this);
            Debug.Log("Rooms final: " + splitRooms.Count, this);
        }
        else if (dungeonsGeneratedCount < dungeonsToGenerate)
        {
            totalGeneratedRoomsList.Add(roomsMade);
            totalRoomsRemovedList.Add(removedRooms);
            totalRoomsFinalDungeonList.Add(splitRooms.Count);
            DungeonGenerationTimesList.Add(Math.Round(perDungeonWatch.Elapsed.TotalMilliseconds, 3));
            doorGenerator.StartGenerator(splitRooms, visualDelay, wallThickness, doorSize, seed, false, nodeGraph);
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
            Debug.Log("Amount of dungeons generated: " + dungeonsGeneratedCount, this);
        }
        
        doorGenerator.StartGenerator(splitRooms, visualDelay, wallThickness, doorSize, seed, true, nodeGraph);
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
        //nodeGraph = nodeGraph.BFS(splitRooms[splitRooms.Count/2]);
        LinkedList<RectRoom> splitRooms = new();
        //nodeGraph = nodeGraph.BFS(splitRooms.First.Value);
        nodeGraph.ConvertToSpanningTree();
    }
    
    
    #region Helper Methods
    
    void DrawDungeon()
    {
        Dictionary<RectRoom, List<RectRoom>> drawGraph = nodeGraph.GetGraph();
        foreach (var room in drawGraph)
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
            foreach (RectRoom r in room.Value)
            {
                Vector3 rCenter = new (r.roomData.center.x, 0, r.roomData.center.y);
                Vector3 roomCenter = new (room.Key.roomData.center.x, 0, room.Key.roomData.center.y);
                Debug.DrawLine(rCenter, roomCenter, Color.red);
            }
            
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
