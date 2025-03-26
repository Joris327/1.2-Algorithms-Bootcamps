using System;
using UnityEngine;

public class AwaitableUtils : MonoBehaviour
{
    Action keyPressedAction;
    
    public KeyCode waitForKey = KeyCode.None;

    void Update()
    {
        if (waitForKey != KeyCode.None && Input.GetKeyDown(waitForKey)) keyPressedAction?.Invoke();
    }
    
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
}
