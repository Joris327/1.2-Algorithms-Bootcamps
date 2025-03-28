using System.Collections.Generic;
using UnityEngine;

public class Zone
{
    public List<RectRoom> rooms = new();
    public RectInt data;
    
    public Zone(RectInt pData)
    {
        data = pData;
    }
    
    public Zone()
    {
        data = new();
    }
}
