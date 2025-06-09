using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;

/// <summary>
/// generates visuals for the dungeon
/// </summary>
public class VisualsGenerator : MonoBehaviour
{
    //serialized fields
    [SerializeField] DungeonGenerator dungeonGenerator;
    [SerializeField] AwaitableUtils awaitableUtils;
    [Min(0)] public float visualDelay = 0;
    [SerializeField] bool floodFillRecursively = true;
    [SerializeField] bool debugDraw = false;
    [SerializeField] Player player;
    [SerializeField] GameObject simpleWallPrefab;
    [SerializeField] GameObject simpleFloorPrefab;
    [SerializeField] Transform floorTilePrefab;
    [SerializeField] Transform[] wallPrefabs;

    //private fields
    int[,] tileMap;
    Vector2Int worldSize;
    GameObject visualsContainer;

    void Update()
    {
        if (debugDraw && tileMap != null)
        {
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                for (int j = 0; j < tileMap.GetLength(1); j++)
                {
                    if (tileMap[j, i] == 1) AlgorithmsUtils.DebugRectInt(new(i, j, 1, 1), Color.blue);
                }
            }
        }
    }
    
    /// <summary>
    /// Resets all private variables to their default state.
    /// </summary>
    public void Reset()
    {
        tileMap = null;
        worldSize = new();
        DestroyImmediate(visualsContainer);
    }

    #region Create Simple Visuals
    /// <summary>
    /// creates visuals for the dungeon in line with the sufficient criteria.
    /// </summary>
    public async Task CreateSimpleVisuals(Graph<RectRoom> nodeGraph, RectDoor[] doors)
    {
        System.Diagnostics.Stopwatch visualsGenerationWatch = System.Diagnostics.Stopwatch.StartNew();

        visualsContainer = new GameObject("Dungeon geometry");
        worldSize = dungeonGenerator.WorldSize;
        byte[,] map = new byte[worldSize.x, worldSize.y];
        
        //create tileMap
        RectRoom[] keys = nodeGraph.Keys();
        foreach (RectRoom room in keys)
        {
            if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
            if (awaitableUtils.waitForKey != KeyCode.None) await awaitableUtils;
            
            //Place floors
            Vector3 floorPos = new(room.roomData.center.x, 0, room.roomData.center.y);
            GameObject floor = Instantiate(simpleFloorPrefab, floorPos, simpleFloorPrefab.transform.rotation, visualsContainer.transform);
            floor.transform.localScale = new Vector3(room.roomData.width, room.roomData.height, 1);

            RectInt roomData = room.roomData;
            for (int i = roomData.yMin; i < roomData.yMax - 1; i++)
            {
                map[roomData.xMax - 1, i] = 1;
                map[roomData.xMax - 2, i] = 1;
                map[roomData.xMin, i] = 1;
                map[roomData.xMin + 1, i] = 1;
            }
            for (int i = roomData.xMin; i < roomData.xMax - 1; i++)
            {
                map[i, roomData.yMax - 1] = 1;
                map[i, roomData.yMax - 2] = 1;
                map[i, roomData.yMin] = 1;
                map[i, roomData.yMin + 1] = 1;
            }
        }

        foreach (RectDoor door in doors)
        {
            foreach (Vector2Int doorPos in door.doorData.allPositionsWithin)
            {
                map[doorPos.x, doorPos.y] = 2;
            }
        }
        
        //place walls
        for (int i = 0; i < worldSize.x; i++)
        {
            for (int j = 0; j < worldSize.y; j++)
            {
                byte pos = map[i, j];
                if (pos == 1) Instantiate(simpleWallPrefab, new Vector3(i + 0.5f, 0.5f, j + 0.5f), Quaternion.identity, visualsContainer.transform);
            }
        }

        visualsGenerationWatch.Stop();
        Debug.Log("Sufficient Visuals generation time: " + Math.Round(visualsGenerationWatch.Elapsed.TotalMilliseconds, 3));
    }

    #endregion
    
    /// <summary>
    /// generate visuals in line with the excelent criteria.
    /// </summary>
    public async Task CreateGoodVisuals()
    {
        System.Diagnostics.Stopwatch visualsGenerationWatch = System.Diagnostics.Stopwatch.StartNew();

        worldSize = dungeonGenerator.WorldSize;
        tileMap = new int[worldSize.x, worldSize.y];
        if (visualsContainer) Destroy(visualsContainer);

        System.Diagnostics.Stopwatch tileMapWatch = System.Diagnostics.Stopwatch.StartNew();
        await GenerateTileMap();
        tileMapWatch.Stop();

        System.Diagnostics.Stopwatch wallsWatch = System.Diagnostics.Stopwatch.StartNew();
        await SpawnWalls();
        wallsWatch.Stop();

        System.Diagnostics.Stopwatch floodFillWatch = System.Diagnostics.Stopwatch.StartNew();
        if (floodFillRecursively) await FloodFillRecursive();
        else await FloodFill();
        floodFillWatch.Stop();
        
        System.Diagnostics.Stopwatch graphWatch = System.Diagnostics.Stopwatch.StartNew();
        player.SetGraph(ConstructGraph());
        graphWatch.Stop();

        visualsGenerationWatch.Stop();

        Debug.Log("-----");
        Debug.Log("Good Visuals generation time: " + Math.Round(visualsGenerationWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    TileMap generation Time: " + Math.Round(tileMapWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Walls generation Time: " + Math.Round(wallsWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Floor generation Time: " + Math.Round(floodFillWatch.Elapsed.TotalMilliseconds, 3));
        Debug.Log("    Pathfinding graph generation Time: " + Math.Round(graphWatch.Elapsed.TotalMilliseconds, 3));
    }
    
    /// <summary>
    /// generates a tilemap to be used by the marching squares algorithm.
    /// </summary>
    async Task GenerateTileMap()
    {
        RectRoom[] rooms = dungeonGenerator.GetRooms;
        foreach (RectRoom room in rooms)
        {
            RectInt roomData = room.roomData;

            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(roomData, Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }
            
            //we log the edges (inset WallThickness, because walls are more than one tile thick) of each room as needing walls
            for (int i = dungeonGenerator.WallThickness * 2 - 1; i < roomData.width - dungeonGenerator.WallThickness * 2 + 1; i++)
            {
                tileMap[roomData.yMin + dungeonGenerator.WallThickness * 2 - 1, roomData.xMin + i] = 1;
                tileMap[roomData.yMax - dungeonGenerator.WallThickness * 2, roomData.xMin + i] = 1;
            }
            
            for (int i = dungeonGenerator.WallThickness * 2; i < roomData.height - dungeonGenerator.WallThickness * 2; i++)
            {
                tileMap[roomData.yMin + i, roomData.xMin + dungeonGenerator.WallThickness * 2 - 1] = 1;
                tileMap[roomData.yMin + i, roomData.xMax - dungeonGenerator.WallThickness * 2] = 1;
            }
        }

        RectDoor[] doors = dungeonGenerator.GetDoors;
        foreach (RectDoor door in doors)
        {
            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(door.doorData, Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }

            foreach (Vector2Int doorPos in door.doorData.allPositionsWithin)
            {
                tileMap[doorPos.y, doorPos.x] = 0;
            }
        }
    }
    
    /// <summary>
    /// uses a marching squares algorithm to populate the dungeon with walls.
    /// </summary>
    async Task SpawnWalls()
    {
        visualsContainer = new("Dungeon Visuals");

        int rows = tileMap.GetLength(0);
        int cols = tileMap.GetLength(1);

        for (int i = 1; i < rows - 2; i++)
        {
            for (int j = 1; j < cols - 2; j++)
            {
                int tileCase = tileMap[i, j] * 8 + tileMap[i + 1, j] * 4 + tileMap[i, j + 1] * 1 + tileMap[i + 1, j + 1] * 2;

                if (visualDelay > 0)
                {
                    AlgorithmsUtils.DebugRectInt(new(j, i, 2, 2), Color.red, visualDelay);
                    await Awaitable.WaitForSecondsAsync(visualDelay);
                }

                if (tileCase < 1 || tileCase >= wallPrefabs.Length) continue;

                Instantiate(wallPrefabs[tileCase], new Vector3(j + 1, 0, i + 1), wallPrefabs[tileCase].transform.rotation, visualsContainer.transform);
            }
        }
    }
    
    /// <summary>
    /// uses a DFS algorithm to floodfill the floor of the dungeon with visuals.
    /// </summary>
    async Task FloodFill()
    {
        RectRoom startRoom = dungeonGenerator.GetFirstRoom;

        HashSet<Vector2Int> discovered = new();
        Queue<Vector2Int> queue = new();

        queue.Enqueue(new((int)startRoom.roomData.center.x, (int)startRoom.roomData.center.y));
        discovered.Add(queue.Peek());

        while (queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();

            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(new(tile.x, tile.y, 1, 1), Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }

            if (tileMap[tile.y, tile.x] != 0) continue;

            tileMap[tile.y, tile.x] = 2;
            Instantiate(floorTilePrefab, new Vector3(tile.x + 0.5f, 0, tile.y + 0.5f), floorTilePrefab.rotation, visualsContainer.transform);

            Vector2Int rightTile = new(tile.x + 1, tile.y);
            if (tileMap[rightTile.y, rightTile.x] == 0 && !discovered.Contains(rightTile))
            {
                queue.Enqueue(rightTile);
                discovered.Add(rightTile);
            }

            Vector2Int leftTile = new(tile.x - 1, tile.y);
            if (tileMap[leftTile.y, leftTile.x] == 0 && !discovered.Contains(leftTile))
            {
                queue.Enqueue(leftTile);
                discovered.Add(leftTile);
            }

            Vector2Int upTile = new(tile.x, tile.y + 1);
            if (tileMap[upTile.y, upTile.x] == 0 && !discovered.Contains(upTile))
            {
                queue.Enqueue(upTile);
                discovered.Add(upTile);
            }

            Vector2Int downTile = new(tile.x, tile.y - 1);
            if (tileMap[downTile.y, downTile.x] == 0 && !discovered.Contains(downTile))
            {
                queue.Enqueue(downTile);
                discovered.Add(downTile);
            }
        }
    }
    
    /// <summary>
    /// uses a recursive algorithm to floodfill the floor of the dungeon with visuals.
    /// </summary>
    async Task FloodFillRecursive()
    {
        if (visualDelay > 0)
        {
            //AlgorithmsUtils.DebugRectInt(new(pos.x, pos.y, 1, 1), Color.red, visualDelay);
            await Awaitable.WaitForSecondsAsync(visualDelay);
        }
        
        RectRoom startRoom = dungeonGenerator.GetFirstRoom;
        await PlaceTile(new Vector2Int((int)startRoom.roomData.center.x, (int)startRoom.roomData.center.y));
    }
    
    /// <summary>
    /// the actual recursive part of the recursive floodfill
    /// </summary>
    async Task PlaceTile(Vector2Int pos)
    {
        if (visualDelay > 0)
        {
            AlgorithmsUtils.DebugRectInt(new(pos.x, pos.y, 1, 1), Color.red, visualDelay);
            await Awaitable.WaitForSecondsAsync(visualDelay);
        }
        
        if (tileMap[pos.y, pos.x] != 0) return;
        
        tileMap[pos.y, pos.x] = 2;
        Instantiate(floorTilePrefab, new Vector3(pos.x + 0.5f, 0, pos.y + 0.5f), floorTilePrefab.rotation, visualsContainer.transform);
        
        if (tileMap[pos.y  , pos.x+1] == 0) await PlaceTile(new(pos.x+1, pos.y  ));
        if (tileMap[pos.y  , pos.x-1] == 0) await PlaceTile(new(pos.x-1, pos.y  ));
        if (tileMap[pos.y+1, pos.x  ] == 0) await PlaceTile(new(pos.x,   pos.y+1));
        if (tileMap[pos.y-1, pos.x  ] == 0) await PlaceTile(new(pos.x,   pos.y-1));
    }
    
    /// <summary>
    /// construct a nodegraph for the player to use during pathfinding.
    /// </summary>
    Graph<Vector3> ConstructGraph()
    {
        Graph<Vector3> graph = new();
        graph.AddNode(new());

        for (int i = 1; i < tileMap.GetLength(0) - 1; i++) //we can skip checking the edges as those will never be walkable
        {
            for (int j = 1; j < tileMap.GetLength(1) - 1; j++)
            {
                if (tileMap[j, i] == 2) graph.AddNode(new(i+0.5f, 0, j+0.5f));
            }
        } 

        foreach (Vector3 tilePos in graph.Keys())
        {
            //cardinal directions
            TryConnectEdge( 1,  0, tilePos, graph);
            TryConnectEdge(-1,  0, tilePos, graph);
            TryConnectEdge( 0,  1, tilePos, graph);
            TryConnectEdge( 0, -1, tilePos, graph);
            
            //diagonals
            TryConnectEdge( 1,  1, tilePos, graph);
            TryConnectEdge( 1, -1, tilePos, graph);
            TryConnectEdge(-1,  1, tilePos, graph);
            TryConnectEdge(-1, -1, tilePos, graph);
        }

        return graph;
    }
    
    /// <summary>
    /// tries to make an edge between two nodes. simply makes no edge if that is not possible.
    /// </summary>
    void TryConnectEdge(int xOffset, int yOffset, Vector3 currentPos, Graph<Vector3> graph)
    {
        Vector3 otherPos = new(currentPos.x + xOffset, 0, currentPos.z + yOffset);
        
        if (graph.ContainsKey(otherPos))
        {
            graph.AddEdge(currentPos, otherPos);
        }
    }
}
