using UnityEngine;

public class RectRoom
{
    public int x;
    public int y;
    public int width;
    public int height;
    public bool markedForDestruction;
    
    public RectRoom(int pX, int pY, int pWidth, int pHeight)
    {
        x = pX;
        y = pY;
        width = pWidth;
        height = pHeight;
        markedForDestruction = false;
    }
    
    public void SetPos(int newX, int newY)
    {
        x = newX;
        y = newY;
    }
    
    public void SetSize(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
    }
    
    public Vector2 GetPos() => new(x, y);
    
    public Vector2 GetSize() => new(width, height);
    
    public Vector2 Center()
    {
        return new(x + (width/2f), y + (height/2f));
    }
    
    public bool IsWiderThanHigh() => width > height;
}
