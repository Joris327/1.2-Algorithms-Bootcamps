using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public List<RectRoom> rooms = new();
    
    [SerializeField] int wallThickness = 1;
    [SerializeField] int worldWidth = 100;
    [SerializeField] int worldHeight = 100;
    [SerializeField] int minRoomSize = 5;
    [SerializeField] int roomsLimit = 1000;
    [SerializeField] int seed = 0;
    
    void Start()
    {
        if (seed == 0) seed = (int)(Random.value * int.MaxValue);
        Random.InitState(seed);
        
        GenerateDungeon();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G)) GenerateDungeon();
        
        if (rooms != null && rooms.Count > 0)
        {
            foreach(RectRoom room in rooms)
            {
                AlgorithmsUtils.DebugRectRoom(room, Color.yellow);
            }
        }
        else Debug.LogWarning("No Rooms");
        
        DebugExtension.DebugLocalCube(transform, new Vector3(worldWidth, 0, worldHeight), new Vector3(worldWidth/2f, 0, worldHeight/2f));
    }
    
    void GenerateDungeon()
    {
        rooms.Clear();
        RectRoom baseRoom = new(0, 0, worldWidth, worldHeight);
        rooms.Add(baseRoom);
        
        if (!CanBeSplit(ref baseRoom))
        {
            Debug.Log("Unsplittable");
            return;
        }
        
        for (int i = 0; i < rooms.Count; i++)
        {
            SplitRoom(rooms[i]);
            
            if (rooms.Count > roomsLimit)
            {
                Debug.Log("Reached room limit");
                break;
            }
        }
        
        int removedRoomsCounter = 0;
        for (int i = rooms.Count-1; i > 0; i--)
        {
            if (rooms[i].markedForDestruction)
            {
                rooms.RemoveAt(i);
                removedRoomsCounter++;
            }
        }
        
        Debug.Log("removed rooms count: " + removedRoomsCounter);
        Debug.Log("Current rooms count: " + rooms.Count);
    }
    
    void SplitRoom(RectRoom room)
    {
        if (room.width <= (minRoomSize + wallThickness) * 2 && room.height <= (minRoomSize + wallThickness) * 2) return;
        
        bool splitVertically = false;
        if (room.IsWiderThanHigh()) splitVertically = true;
        
        RectRoom newRoom1;
        RectRoom newRoom2 = new(room.x, room.y, room.width, room.height);
        
        if (splitVertically)
        {
            int splitPos = Random.Range(minRoomSize, room.width - minRoomSize);
            
            newRoom1 = new(room.x + splitPos - wallThickness, room.y, room.width - splitPos + wallThickness, room.height);
            newRoom2.width = splitPos + wallThickness;
        }
        else
        {
            int splitPos = Random.Range(minRoomSize, room.height - minRoomSize);
            
            newRoom1 = new(room.x, room.y + splitPos - wallThickness, room.width, room.height - splitPos + wallThickness);
            newRoom2.height = splitPos + wallThickness;
        }
        
        room.markedForDestruction = true;
        
        rooms.Add(newRoom1);
        rooms.Add(newRoom2);
    }
    
    bool IsEven(int value) => value % 2 == 0;
    
    bool CanBeSplit(ref RectRoom room) => room.width > minRoomSize * 2 || room.height > minRoomSize * 2;
}
