using System;
using System.Collections.Generic;
using System.Text;
using NaughtyAttributes;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;
[DefaultExecutionOrder(4)]
public class DungeonGenerator : MonoBehaviour
{
    [SerializeField]
    private  UnityEvent onGenerateDungeon;
    
    [SerializeField]
    private NavMeshSurface navMeshSurface;
    
    [SerializeField]
    private RectInt dungeonBounds;
    
    [SerializeField]
    private List<RectInt> rooms = new List<RectInt>();
    
    [SerializeField]
    private RectInt door;

    [SerializeField]
    private GameObject roomsParent = null;
    
    [SerializeField]
    private bool generateOnStart = false;
    private void Start()
    {
        if(generateOnStart) {
            GenerateDungeon(); 
        }
    }

    [Button]
    public void GenerateDungeon()
    {
        rooms.Clear();
        door = RectInt.zero;
        DebugDrawingBatcher.ClearCalls();
        
        if (roomsParent != null)
        {
            Destroy(roomsParent);
        }
        
        
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
        
        door = new RectInt(intersection.x, 5, intersection.width, intersection.width);
        
        DebugDrawingBatcher.BatchCall( () =>
        {
            AlgorithmsUtils.DebugRectInt(door, Color.cyan);
        });
        
        onGenerateDungeon.Invoke();
    }

    private (RectInt, RectInt) SplitVertically(RectInt pRect)
    {
        RectInt roomA = pRect;
        RectInt roomB = pRect;

        roomA.width = (roomA.width / 2) + UnityEngine.Random.Range(-2, 2);
        roomB.width -= (roomA.width - 1);

        roomB.x += roomA.width - 1;

        return (roomA, roomB);
    }
    
    [Button]
    public void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }
    
    public RectInt GetDungeonBounds()
    {
        return dungeonBounds;
    }
    
    public List<RectInt> GetRooms()
    {
        return rooms;
    }

    public RectInt GetDoor()
    {
        return door;
    }
    
    
}
