using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyScript : MonoBehaviour
{
    public Bounds bounds;
    
    public NavMeshAgent navMeshAgent;
    
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        StartCoroutine(GoToRandomLocation());
    }

    IEnumerator GoToRandomLocation()
    {
        for(;;)
        {
            Vector3 newLocation = new Vector3(Random.Range(bounds.min.x, bounds.max.x), 0, Random.Range(bounds.min.z, bounds.max.z));
            navMeshAgent.destination = newLocation;
            yield return new WaitForSeconds(2f);
        }
    }
}
