using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.AI.Navigation;
using UnityEditor.Rendering;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    
    [SerializeField] NavMeshSurface navMeshSurface;
    [SerializeField] RectInt dungeonBounds;
    
    [SerializeField] List<RectInt> rooms = new List<RectInt>();
    [SerializeField] RectInt door;
    
    [SerializeField] GameObject wallPrefab;
    [SerializeField] GameObject floorPrefab;
    
    private void Start()
    {
        GenerateDungeon();
        SpawnDungeonAssets();
        BakeNavMesh();
    }

    [Button]
    public void GenerateDungeon()
    {
        rooms.Clear();
        door = RectInt.zero;
        DebugDrawingBatcher.ClearCalls();
        
        (RectInt roomA, RectInt roomB) = SplitVertically(dungeonBounds);
        rooms.Add(roomA);
        rooms.Add(roomB);
        
        DebugDrawingBatcher.BatchCall( () =>
        {
            foreach (var room in rooms)
            {
                AlgorithmsUtils.DebugRectInt(roomA, Color.red);
                RectInt innerRoomA = new RectInt(roomA.x + 1, roomA.y + 1, roomA.width - 2, roomA.height - 2);
                AlgorithmsUtils.DebugRectInt(innerRoomA, Color.red);
                
                AlgorithmsUtils.DebugRectInt(roomB, Color.red);
                RectInt innerRoomB = new RectInt(roomB.x + 1, roomB.y + 1, roomB.width - 2, roomB.height - 2);
                AlgorithmsUtils.DebugRectInt(innerRoomB, Color.red);
                
            }
        });
        
        RectInt intersection = AlgorithmsUtils.Intersect(roomA, roomB);
        int randomY = UnityEngine.Random.Range(intersection.y + 1, intersection.y + intersection.height - 1);
        
        door = new RectInt(intersection.x, randomY, intersection.width, intersection.width);
        
        DebugDrawingBatcher.BatchCall( () =>
        {
            AlgorithmsUtils.DebugRectInt(door, Color.cyan);
        });
    }
    
    private (RectInt, RectInt) SplitVertically (RectInt pRect)
    {
        RectInt roomA = pRect;
        RectInt roomB = pRect;

        roomA.width = (roomA.width / 2) + UnityEngine.Random.Range(-2, 2);
        roomB.width -= (roomA.width - 1);

        roomB.x += roomA.width - 1;

        return (roomA, roomB);
    }

    [Button]
    public void SpawnDungeonAssets()
    {
        GameObject DungeonRooms = new("Dungeon");
        
        foreach (RectInt room in rooms)
        {
            GameObject roomObject = new("room" + rooms.IndexOf(room).ToString());
            roomObject.transform.SetParent(DungeonRooms.transform);
            
            foreach (Vector2Int pos in room.allPositionsWithin)
            {
                Vector3 spawnPos = new(pos.x + 0.5f, 0.5f, pos.y + 0.5f);
                
                if (pos.x == room.xMin || pos.y == room.yMin)
                {
                    if (pos == door.position) continue;
                    if (pos.x == room.xMax - 1) continue;
                    if (pos.y == room.yMax - 1) continue;
                    
                    Instantiate(wallPrefab, spawnPos, Quaternion.identity, roomObject.transform);
                }
            }
            
            GameObject floor = Instantiate(floorPrefab, new(), floorPrefab.transform.rotation);
            floor.transform.localScale = new(room.width, room.height, 1);
            floor.transform.position = new(room.center.x, 0, room.center.y);
        }
        
        GameObject upperWall = new("UpperWall");
        GameObject lowerWall = new("LowerWall");
        upperWall.transform.SetParent(DungeonRooms.transform);
        lowerWall.transform.SetParent(DungeonRooms.transform);
        
        for (int i = 0; i < dungeonBounds.width; i++)
        {
            Vector3 spawnpos = new(i + 0.5f, 0.5f, dungeonBounds.height - 0.5f);
            Instantiate(wallPrefab, spawnpos, Quaternion.identity, upperWall.transform);
        }
        
        for (int i = 0; i < dungeonBounds.height-1; i++)
        {
            Vector3 spawnpos = new(dungeonBounds.width - 0.5f, 0.5f, i + 0.5f);
            Instantiate(wallPrefab, spawnpos, Quaternion.identity, lowerWall.transform);
        }
    }
    
    [Button]
    void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }
}
