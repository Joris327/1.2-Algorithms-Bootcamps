using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public List<RectRoom> rooms = new();
    RectRoom startRoom = new(0,0,100,50);
    const int wallThickness = 5;
    
    void Start()
    {
        rooms.Add(startRoom);
        SplitVertically(rooms[0]);
        
        SplitVertically(rooms[0]);
        //SplitHorizontally(rooms[0]);
        SplitHorizontally(rooms[1]);
        
        foreach (RectRoom room in rooms)
        {
            Debug.Log(room.GetPos() + " " + room.GetSize());
            //room.width++;
            //room.height++;
            if (room.x > 0)
            {
                room.x -= wallThickness;
                room.width += wallThickness;
            }
            else if (room.width < 100) room.width += wallThickness/2;
            
            if (room.y > 0)
            {
                room.y -= wallThickness;
                room.height += wallThickness;
            }
            else if (room.height < 50) room.height += wallThickness/2;
            
            Debug.Log(room.GetPos() + " " + room.GetSize());
        }
    }

    void Update()
    {
        foreach(RectRoom room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.yellow);
        }
    }
    
    void SplitVertically(RectRoom room)
    {
        RectRoom room2 = new(room.x + room.width/2, room.y, room.width/2, room.height);
        room.width = room.width/2;
        
        rooms.Add(room2);
    }
    
    void SplitHorizontally(RectRoom room)
    {
        RectRoom room2 = new(room.x, room.y + room.height/2, room.width, room.height/2);
        room.height = room.height/2;
        
        rooms.Add(room2);
    }
}
