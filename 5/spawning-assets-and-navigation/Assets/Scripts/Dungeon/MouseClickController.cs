using UnityEngine;
using UnityEngine.Events;

public class MouseClickController : MonoBehaviour
{
    public Vector3 clickPosition;
    public UnityEvent<Vector3> onClick;
    
    void Update() { 
        // Get the mouse click position in world space 
        if (Input.GetMouseButtonDown(0)) { 
            Ray mouseRay = Camera.main.ScreenPointToRay( Input.mousePosition ); 
            if (Physics.Raycast( mouseRay, out RaycastHit hitInfo )) { 
                Vector3 clickWorldPosition = hitInfo.point; 
                Debug.Log(clickWorldPosition); 
                
                // Store the click position here
                clickPosition = clickWorldPosition;
                
                // Trigger an unity event to notify other scripts about the click here
                onClick?.Invoke(clickPosition);
            } 
        } 
        
        // Add visual debugging here
        DebugExtension.DebugCircle(clickPosition, Color.blue);
        Debug.DrawLine(Camera.main.transform.position, clickPosition, Color.yellow);
    } 

}
