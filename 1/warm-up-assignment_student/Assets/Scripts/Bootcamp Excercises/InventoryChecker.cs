using UnityEngine;

public class InventoryChecker : MonoBehaviour
{
    void Start()
    {
        InventorySystem.instance.Add("potion", 2);
        InventorySystem.instance.Add("sword", 1);
        InventorySystem.instance.Remove("potion");
        InventorySystem.instance.PrintToConsole();
    }
}
