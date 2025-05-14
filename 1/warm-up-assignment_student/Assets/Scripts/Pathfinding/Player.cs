using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Player : MonoBehaviour
{
    NavMeshAgent agent;
    Vector3 clickPosition = new();

    void Awake()
    {
        if(!TryGetComponent(out agent)) Debug.LogError("Player: could not find NavMeshAgent component.", this);
    }

    void Update() { 
        // Get the mouse click position in world space 
        if (Input.GetMouseButtonDown(0)) { 
            Ray mouseRay = Camera.main.ScreenPointToRay( Input.mousePosition ); 
            if (Physics.Raycast( mouseRay, out RaycastHit hitInfo )) { 
                Vector3 clickWorldPosition = hitInfo.point; 
                //Debug.Log(clickWorldPosition); 
                
                clickPosition = clickWorldPosition;
                SetDestination(clickPosition);
            } 
        }
        
        // Add visual debugging here
        DebugExtension.DebugCircle(clickPosition, Color.blue);
        Debug.DrawLine(Camera.main.transform.position, clickPosition, Color.yellow);
    }
    
    void SetDestination(Vector3 target)
    {
        agent.destination = target;
    }
}
