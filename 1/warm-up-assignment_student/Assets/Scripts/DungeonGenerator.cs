using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    readonly List<RectRoom> rooms = new();
    RectRoom startRoom = new(0,0,100,50);
    const int wallThickness = 1;
    
    void Start()
    {
        rooms.Add(startRoom);
        SplitVertically(rooms[0]);
        //SplitHorizontally(rooms[0]);
        Debug.Log(rooms[0].GetPos() + " " + rooms[0].GetSize());
        Debug.Log(rooms[1].GetPos() + " " + rooms[1].GetSize());
    }

    void Update()
    {
        foreach(RectRoom room in rooms)
        {
            //AlgorithmsUtils.DebugRectInt(room, Color.yellow);
        }
    }
    
    void SplitVertically(RectRoom room)
    {
        //RectInt room1 = new(room.x, room.y, room.width/2+wallThickness, room.height);
        RectRoom room2 = new(room.width/2-wallThickness, room.y, room.width/2+wallThickness, room.height+1);
        room.width /= 2+wallThickness;
        //room.width = room.width/2+wallThickness;
        //room.size = new(, room.height);
        //rooms.Remove(room);
        //rooms.Add(room1);
        rooms.Add(room2);
    }
    
    void SplitHorizontally(RectRoom room)
    {
        //RectInt room2 = 
        //rooms.Add(room2);
    }
}
