using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{

    #region Enemy Spawner Stuff
    
    [Header("Spawner Settings")]
    [DisableIf("Enabled"), SerializeField]
    private int initialSize = 5;
    [DisableIf("Enabled"), ShowAssetPreview]
    public GameObject enemyPrefab;
    
    [HorizontalLine]
    [Header("Debugging Values")]
    [ReadOnly, SerializeField]
    private GameObject[] enemies;
    [ReadOnly, SerializeField]
    private int enemyCount = 0;
    [FormerlySerializedAs("enemyID")] [ReadOnly, SerializeField]
    private int nextEnemyID = 0;
  
    [HorizontalLine]
    [Header("Parameters")]
    public int index = 0;
    
    public bool Enabled() { return enemies.Length > 0; }
    
    void Start()
    {
        enemies = new GameObject[initialSize];
        enemyCount = 0;
    }
    
    Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(1.5f, 12.5f), 0, Random.Range(-10.5f, -1.5f));
    }
    
    Quaternion GetRandomYRotation()
    {
        return Quaternion.Euler(0, Random.Range(0, 360), 0);
    }
    
    private GameObject SpawnEnemy()
    {
        var newEnemy = Instantiate(enemyPrefab, GetRandomPosition(), GetRandomYRotation());
        newEnemy.name = "Enemy " + nextEnemyID++;
        newEnemy.transform.parent = transform;
        
        return newEnemy;
    }
    
    #endregion
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    void SpawnEnemyAtTheEnd()
    {
        GameObject newEnemy = SpawnEnemy();
        
        if (enemyCount == enemies.Length)
        {
            IncreaseArraySize();
        }

        //Add new enemy to the end of the array and increment the count
  
        //ADD CODE HERE
        enemies[enemyCount] = newEnemy;
        enemyCount++;
    }
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void InsertEnemyAtIndex()
    {
        InsertEnemyAtIndex(index);
    }
    
    private void InsertEnemyAtIndex(int indexToInsert)
    {
        //Check if the index is valid
        if (indexToInsert < 0 || indexToInsert > enemyCount)
        {
            throw new IndexOutOfRangeException("Index out of bounds");
        }
        
        //If the array is full, increase its size
        if (enemyCount == enemies.Length)
        {
            IncreaseArraySize();
        }
        
        GameObject newEnemy = SpawnEnemy();
        enemyCount++;
        //Shift all elements to the right starting from the end of the array to the index to insert and insert the new enemy at the index and increment the count
        
        //ADD CODE HERE
        for (int i = enemies.Length-2; i >= indexToInsert; i--)
        {
            if (enemies[i]) enemies[i+1] = enemies[i];
            
            if (i == indexToInsert)
            {
                enemies[i] = newEnemy;
                break;
            }
        }
    }
    
    private void RemoveEnemyAtIndex(int indexToRemove)
    {
        //Check if the index is valid
        if (indexToRemove < 0 || indexToRemove >= enemyCount)
        {
            throw new IndexOutOfRangeException("Index out of bounds");
        }
        
        //Destroy the enemy at the index
        Destroy(enemies[indexToRemove]);
        enemies[indexToRemove] = null;
        
        //Shift all elements to the left starting from the index to remove to the end of the array and decrement the count
        
        //ADD CODE HERE
        for (int i = indexToRemove; i < enemies.Length-1; i++)
        {
            enemies[i] = enemies[i+1];
        }
        
        enemyCount--;
        
        //If the array is too big, reduce its size
        int sizeToReduce = (int)(enemies.Length * .2f);
        Debug.Log(sizeToReduce);
        if (enemies.Length > initialSize && enemyCount <= sizeToReduce)
        {
            DecreaseArraySize();
        }
    }
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void RemoveEnemyAtIndex()
    {
        RemoveEnemyAtIndex(index);
    }

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    private void RemoveLastEnemy()
    {
        if (enemyCount == 0)
        {
            throw new IndexOutOfRangeException("No enemies to remove");
        }
        
        RemoveEnemyAtIndex(enemyCount - 1);
    }
    
    private void IncreaseArraySize()
    {
        //Create a new array with double the size of the current array and copy the elements
        
        //ADD CODE HERE
        GameObject[] newArray = new GameObject[enemies.Length * 2];
        
        for (int i = 0; i < enemies.Length; i++)
        {
            newArray[i] = enemies[i];
        }

        enemies = newArray;
    }
    
    private void DecreaseArraySize()
    {
        //Create a new array with half the size of the current array and copy the elements
        
        //ADD CODE HERE
        GameObject[] newArray = new GameObject[enemies.Length / 2];
        
        for (int i = 0; i < newArray.Length; i++)
        {
            newArray[i] = enemies[i];
        }
        
        enemies = newArray;
    }
}
