using UnityEngine;

public class HelloWorldDebugger : MonoBehaviour
{
    // Variables to inspect
    public string message = "Hello, Unity!";
    public int counter = 0;
    public float timeSinceStart = 0f;

    void Start()
    {
        // Print initial message
        Debug.Log("Starting the script...");
        Debug.Log(message);

        int result = AddTwoNumbers(10, 5);
        Debug.Log("Result of 10 + 5 = " + result);
    }

    int AddTwoNumbers(int a, int b)
    {
        int sum = a + b;
        return sum;
    }

    void Update()
    {
        // Increase counter every frame
        counter++;

        // Track the time since the game started
        timeSinceStart += Time.deltaTime;

        // Debug log every 60 frames
        if (counter % 60 == 0)
        {
            Debug.Log("Counter: " + counter + " | Time since start: " + timeSinceStart);
        }
    }
}