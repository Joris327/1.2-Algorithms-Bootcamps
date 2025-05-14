using System.Threading.Tasks;
using UnityEngine;

public class VisualsGenerator : MonoBehaviour
{
    //serialized fields
    [SerializeField] DungeonGenerator dungeonGenerator;
    [Min(0)] public float visualDelay = 0;
    [SerializeField] bool debugDraw = false;
    [SerializeField] Transform[] wallPrefabs;
    
    //private fields
    int[,] tileMap;
    Vector2Int worldSize;
    GameObject visualsContainer;

    void Update()
    {
        if (debugDraw && tileMap != null)
        {
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                for (int j = 0; j < tileMap.GetLength(1); j++)
                {
                    if (tileMap[j, i] == 1) AlgorithmsUtils.DebugRectInt(new(i, j, 1, 1), Color.blue);
                }
            }
        }
    }

    public async Task Generate()
    {
        worldSize = dungeonGenerator.WorldSize;
        tileMap = new int[worldSize.x, worldSize.y];
        if (visualsContainer) Destroy(visualsContainer);
        
        await GenerateTileMap();
        await SpawnWalls();
    }
    
    async Task GenerateTileMap()
    {
        RectRoom[] rooms = dungeonGenerator.GetRooms;
        foreach (RectRoom room in rooms)
        {
            RectInt roomData = room.roomData;
            
            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(roomData, Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }
            
            //we log the edges (inset WallThickness, because walls are more than one tile thick) of each room as needing walls
            for (int i = dungeonGenerator.WallThickness*2-1; i < roomData.width - dungeonGenerator.WallThickness*2+1; i++)
            {
                tileMap[roomData.yMin + dungeonGenerator.WallThickness*2 - 1, roomData.xMin + i] = 1;
			    tileMap[roomData.yMax - dungeonGenerator.WallThickness*2, roomData.xMin + i    ] = 1;
            }
            
            for (int i = dungeonGenerator.WallThickness*2; i < roomData.height - dungeonGenerator.WallThickness*2; i++)
            {
                tileMap[roomData.yMin + i, roomData.xMin + dungeonGenerator.WallThickness*2 - 1] = 1;
                tileMap[roomData.yMin + i, roomData.xMax - dungeonGenerator.WallThickness*2    ] = 1;
            }
        }
        
        RectDoor[] doors = dungeonGenerator.GetDoors;
        foreach (RectDoor door in doors)
        {
            if (visualDelay > 0)
            {
                AlgorithmsUtils.DebugRectInt(door.doorData, Color.red, visualDelay);
                await Awaitable.WaitForSecondsAsync(visualDelay);
            }
            
            foreach (Vector2Int doorPos in door.doorData.allPositionsWithin)
            {
                tileMap[doorPos.y, doorPos.x] = 0;
            }
        }
    }
    
    async Task SpawnWalls()
    {
        visualsContainer = new("Dungeon Visuals");
        
        int rows = tileMap.GetLength(0);
        int cols = tileMap.GetLength(1);
        
        for (int i = 1; i < rows-2; i++)
        {
            for (int j = 1; j < cols-2; j++)
            {
                int tileCase = tileMap[i,j] * 8 + tileMap[i+1,j] * 4 + tileMap[i,j+1] * 1 + tileMap[i+1,j+1] * 2;
                
                if (visualDelay > 0)
                {
                    AlgorithmsUtils.DebugRectInt(new(j, i, 2, 2), Color.red, visualDelay);
                    await Awaitable.WaitForSecondsAsync(visualDelay);
                }
                
                if (tileCase < 1 || tileCase >= wallPrefabs.Length) continue;
                
                Instantiate(wallPrefabs[tileCase], new Vector3(j+1, 0, i+1), wallPrefabs[tileCase].transform.rotation, visualsContainer.transform);
            }
        }
    }
}
