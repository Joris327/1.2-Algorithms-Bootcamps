using UnityEngine;

public class RecursionTester : MonoBehaviour
{
    void Start()
    {
        Debug.Log(SumIterative(8));
        Debug.Log(SumRecursive(8));
        
        Debug.Log(FibonacciIterative(10));
        Debug.Log(FibonacciRecursive(10));
    }

    int SumIterative(int n) { 
        int sum = 0; 
        for (int i = 1; i <= n; i++) { 
            sum += i; 
        } 
        return sum;
    } 

    int SumRecursive(int n) {
        if (n <= 0) return 0;
        return n + SumRecursive(n-1);
    }
    
    int FibonacciIterative(int n) { 
        int a = 0, b = 1, temp; 
        for (int i = 2; i <= n; i++) { 
            temp = a + b; 
            a = b; 
            b = temp; 
        } 
        return n == 0 ? a : b; 
    } 
    
    int FibonacciRecursive(int n) {    
        if (n == 1) return 1;
        if (n <= 0) return 0;
        return n + FibonacciRecursive(n-1);
    }
}