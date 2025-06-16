using System;
using UnityEngine;

public class AwaitableUtils : MonoBehaviour
{
    Action keyPressedAction;
    
    public KeyCode waitForKey = KeyCode.None;
    [Min(0)] public float visualDelay = 0;

    void Update()
    {
        if (waitForKey != KeyCode.None && Input.GetKeyDown(waitForKey)) keyPressedAction?.Invoke();
    }
    
    /// <summary>
    /// subscribes and unsubscribes keypress to/from the action. will return as completed when the key is pressed. 
    /// </summary>
    /// <returns></returns>
    public Awaitable.Awaiter GetAwaiter()
    {
        var acs = new AwaitableCompletionSource();
        
        keyPressedAction += SetResult;
        
        return acs.Awaitable.GetAwaiter();
        
        void SetResult()
        {
            keyPressedAction -= SetResult;
            acs.TrySetResult();
        }
    }
    
    
    public async Awaitable Delay(RectInt? rect = null)
    {
        if (rect != null) AlgorithmsUtils.DebugRectInt((RectInt)rect, Color.red, visualDelay);
        
        if (visualDelay > 0) await Awaitable.WaitForSecondsAsync(visualDelay);
        if (waitForKey != KeyCode.None) await this;
    }
}
