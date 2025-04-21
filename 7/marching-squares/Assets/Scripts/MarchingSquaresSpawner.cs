using UnityEngine;

public class MarchingSquaresSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs;
    
    public void SpawnWalls()
    {
        int[,] tileMap = GetComponent<TileMapGenerator>().GetTileMap();
        
        for (int i = 0; i < tileMap.GetLength(0)-1; i+=2)
        {
            for (int j = 0; j < tileMap.GetLength(1)-1; j+=2)
            {
                int tileCase = tileMap[i,j] * 8 + tileMap[i,j+1] * 4 + tileMap[i+1,j] * 1 + tileMap[i+1,j+1] * 2;
                
                // if (tileCase >= prefabs.Length || tileCase < 1)
                // {
                //     Debug.Log("too long");
                //     continue;
                // }
                switch (tileCase)
                {
                    case 0:
                        // Instantiate(prefabs[0], new (j+0.5f, 0, i+0.5f), Quaternion.identity);
                        // Instantiate(prefabs[0], new (j+1.5f, 0, i+0.5f), Quaternion.identity);
                        // Instantiate(prefabs[0], new (j+0.5f, 0, i+1.5f), Quaternion.identity);
                        // Instantiate(prefabs[0], new (j+1.5f, 0, i+1.5f), Quaternion.identity);
                        break;
                    case 1:
                        Instantiate(prefabs[0], new (j+1, 0, i), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+1.5f, 0, i), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+1, 0, i+0.5f), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+1.5f, 0, i+0.5f), Quaternion.identity);
                        //Debug.Log("1");
                        //Debug.Log(j + ", " + i);
                        break;
                    case 2:
                        Instantiate(prefabs[0], new (j+1, 0, i+1), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+1.5f, 0, i+1), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+1, 0, i+1.5f), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+1.5f, 0, i+1.5f), Quaternion.identity);
                        //Debug.Log("2");
                        //Debug.Log(j + ", " + i);
                        break;
                    case 3:
                        Instantiate(prefabs[1], new (j+1, 0, i+1), Quaternion.identity);
                        break;
                    case 4:
                        Instantiate(prefabs[0], new (j, 0, i+1), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+0.5f, 0, i+1), Quaternion.identity);
                        Instantiate(prefabs[0], new (j, 0, i+1.5f), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+0.5f, 0, i+1.5f), Quaternion.identity);
                        //Debug.Log("4");
                        //Debug.Log(j + ", " + i);
                        break;
                    case 8:
                        Instantiate(prefabs[0], new (j, 0, i), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+0.5f, 0, i), Quaternion.identity);
                        Instantiate(prefabs[0], new (j, 0, i+0.5f), Quaternion.identity);
                        Instantiate(prefabs[0], new (j+0.5f, 0, i+0.5f), Quaternion.identity);
                        //Debug.Log("8");
                        //Debug.Log(j + ", " + i);
                        break;
                }
                
                //Instantiate(prefabs[tileCase], new (j+0.5f, 0, i+0.5f), Quaternion.identity);
                GameObject g = new(j + "-" + i + " | " + (j+1) + "-" + i + " | " + j + "-" + (i+1) + " | " + (j+1) + "-" + (i+1));
                g.transform.position = new(j, 0, i);
            }
        }
    }
}
