using UnityEngine;

public class RectDoor
{
    public RectInt doorData;
    
    public RectDoor(RectInt pDoordata)
    {
        doorData = pDoordata;
    }
    
    public RectDoor()
    {
        doorData = new();
    }
}
