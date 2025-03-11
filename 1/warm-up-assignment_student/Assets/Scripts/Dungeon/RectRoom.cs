using System.Collections.Generic;
using UnityEngine;

public class RectRoom
{
    public RectInt roomData;
    public readonly Dictionary<RectRoom, RectInt> connections; //other room, door between these rooms
    
    public RectRoom(RectInt pData, Dictionary<RectRoom, RectInt> pConnections)
    {
        roomData = pData;
        connections = pConnections;
    }
    
    public void AddConnection(RectRoom room, RectInt door) => connections.Add(room, door);
    
    public RectInt GetConnection(RectRoom room)
    {
        if (connections.ContainsKey(room)) return connections[room];
        else return new();
    }
    
    public void SetWidth(int value)
    {
        roomData.width = value;
    }
    
    public void SetHeight(int value)
    {
        roomData.height = value;
    }
    
    // public int x;
    // public int y;
    // public int width;
    // public int height;
    
    // public int xMin { get { return math.min(x, x + width); } }
    // public int xMax { get { return math.max(x, x + width); } }
    // public int yMin { get { return math.min(y, y + height); } }
    // public int yMax { get { return math.max(y, y + height); } }
    
    // public List<RectInt> connections = new();
    
    // public RectRoom(int pX, int pY, int pWidth, int pHeight)
    // {
    //     x = pX;
    //     y = pY;
    //     width = pWidth;
    //     height = pHeight;
    // }
    
    // public RectRoom(Vector2 pos, Vector2 size)
    // {
    //     x = (int)pos.x;
    //     y = (int)pos.y;
    //     width = (int)size.x;
    //     height = (int)size.y;
    // }
    
    // public void SetPos(int newX, int newY)
    // {
    //     x = newX;
    //     y = newY;
    // }
    
    // public void SetSize(int newWidth, int newHeight)
    // {
    //     width = newWidth;
    //     height = newHeight;
    // }
    
    // public Vector2 GetPos() => new(x, y);
    
    // public Vector2 GetSize() => new(width, height);
    
    // public Vector2 Center()
    // {
    //     return new(x + (width/2f), y + (height/2f));
    // }
    
    // public bool IsWiderThanHigh() => width > height;
}
