using System.Collections.Generic;
using UnityEngine;

public class RectRoom
{
    public RectInt roomData;
    public List<int> doors = new();
    
    public RectRoom(RectInt pData = new())
    {
        roomData = pData;
    }
}
