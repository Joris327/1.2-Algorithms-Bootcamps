using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem instance = null;

    readonly Dictionary<string, int> inventory = new(); //<name, amount>
    
    void Awake()
    {
        if (!instance) instance = this;
        else Destroy(gameObject);
    }
    
    public bool Add(string name, int amount)
    {
        if (name == "" || name == null) return false;
        
        if (amount == 0)
        {
            if (Contains(name)) return false;
            
            inventory.Add(name, 0);
            return true;
        }
        
        amount = Mathf.Abs(amount);
        
        if (Contains(name))
        {
            inventory[name] += amount;
            return true;
        }
        
        inventory.Add(name, amount);
        return true;
    }

    public bool Remove(string name, int amount = 0)
    {
        if (name == "" || name == null) return false;
        if (!Contains(name)) return false;
        
        if (amount == 0)
        {
            inventory.Remove(name);
            return true;
        }
        
        amount = -Mathf.Abs(amount);
        
        if (inventory[name] > amount)
        {
            inventory[name] -= amount;
            return true;
        }
        
        inventory.Remove(name);
        return true;
    }
    
    public bool Contains(string name) => inventory.ContainsKey(name);
    
    public void PrintToConsole()
    {
        Debug.ClearDeveloperConsole();
        
        if (inventory.Count == 0)
        {
            Debug.Log("Inventory is empty.");
        }
        
        foreach (var kv in inventory)
        {
            Debug.Log("Key: " + kv.Key + ". Value: " + kv.Value);
        }
    }
}
