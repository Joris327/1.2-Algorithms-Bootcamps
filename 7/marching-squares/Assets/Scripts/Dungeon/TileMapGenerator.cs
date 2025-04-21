using System.Text;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(3)]
public class TileMapGenerator : MonoBehaviour
{
    
    [SerializeField]
    private UnityEvent onGenerateTileMap;
    
    [SerializeField]
    DungeonGenerator dungeonGenerator;
    
    private int [,] _tileMap;
    
    private void Start()
    {
        dungeonGenerator = GetComponent<DungeonGenerator>();
    }
    
    [Button]
    public void GenerateTileMap()
    {
        int [,] tileMap = new int[dungeonGenerator.GetDungeonBounds().height, dungeonGenerator.GetDungeonBounds().width];
        int rows = tileMap.GetLength(0);
        int cols = tileMap.GetLength(1);
		
        //Fill the map with empty spaces
        
        //Draw the rooms
        foreach (RectInt room in dungeonGenerator.GetRooms())
        {
            for (int i = 0; i < room.width; i++)
            {
                tileMap[room.yMin  , room.xMin + i] = 1;
                tileMap[room.yMax-1, room.xMin + i] = 1;
            }
            
            for (int i = 0; i < room.height; i++)
            {
                tileMap[room.yMin + i, room.xMin  ] = 1;
                tileMap[room.yMin + i, room.xMax-1] = 1;
            }
        }
		
        //Draw the doors
        RectInt door = dungeonGenerator.GetDoor();
        tileMap[door.y, door.x] = 0;
        
        _tileMap = tileMap;
        
        onGenerateTileMap.Invoke();
    }

    public string ToString(bool flip)
    {
        if (_tileMap == null) return "Tile map not generated yet.";
        
        int rows = _tileMap.GetLength(0);
        int cols = _tileMap.GetLength(1);
        
        var sb = new StringBuilder();
    
        int start = flip ? rows - 1 : 0;
        int end = flip ? -1 : rows;
        int step = flip ? -1 : 1;

        for (int i = start; i != end; i += step)
        {
            for (int j = 0; j < cols; j++)
            {
                sb.Append((_tileMap[i, j]==0?'0':'#')); //Replaces 1 with '#' making it easier to visualize
            }
            sb.AppendLine();
        }
    
        return sb.ToString();
    }
    
    public int[,] GetTileMap()
    {
        return _tileMap.Clone() as int[,];
    }
    
    [Button]
    public void PrintTileMap()
    {
        Debug.Log(ToString(true));
    }
    
    
}
