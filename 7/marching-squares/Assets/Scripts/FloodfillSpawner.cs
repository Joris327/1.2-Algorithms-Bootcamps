using System.Collections.Generic;
using UnityEngine;

public class FloodfillSpawner : MonoBehaviour
{
    [SerializeField] GameObject floorTilePrefab;
    
    public void Flood()
    {
        int[,] tileMap = GetComponent<TileMapGenerator>().GetTileMap();
        
        RectInt startRoom = GetComponent<DungeonGenerator>().GetRooms()[0];
        
        List<Vector2Int> discovered = new();
        Queue<Vector2Int> queue = new();
        
        queue.Enqueue(new((int)startRoom.center.x, (int)startRoom.center.y));
        discovered.Add(queue.Peek());
        
        while (queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();
            
            if (tileMap[tile.y, tile.x] == 0)
            {
                tileMap[tile.y, tile.x] = 2;
                
                Instantiate(floorTilePrefab, new(tile.x+0.5f, 0, tile.y+0.5f), Quaternion.identity);
            }
            
            Vector2Int rightTile = new(tile.x+1, tile.y);
            if (tileMap[tile.y, tile.x+1] == 0 && !discovered.Contains(rightTile))
            {
                queue.Enqueue(rightTile);
                discovered.Add(rightTile);
            }
            
            Vector2Int leftTile = new(tile.x-1, tile.y);
            if (tileMap[tile.y, tile.x-1] == 0 && !discovered.Contains(leftTile))
            {
                queue.Enqueue(leftTile);
                discovered.Add(leftTile);
            }
            
            Vector2Int upTile = new(tile.x, tile.y+1);
            if (tileMap[tile.y+1, tile.x] == 0 && !discovered.Contains(upTile))
            {
                queue.Enqueue(upTile);
                discovered.Add(upTile);
            }
            
            Vector2Int downTile = new(tile.x, tile.y-1);
            if (tileMap[tile.y-1, tile.x] == 0 && !discovered.Contains(downTile))
            {
                queue.Enqueue(downTile);
                discovered.Add(downTile);
            }
        }
    }
}
