using System.Collections;
using UnityEngine;

public class Currotine : MonoBehaviour
{
    [SerializeField] Light Light;
    [SerializeField] Light greenLight;
    [SerializeField] Light yellowLight;
    [SerializeField] Light redLight;
    
    void Awake()
    {
        StartCoroutine(TrafficLight());
    }
    
    IEnumerator ThreeSeconds()
    {
        yield return new WaitForSeconds(3);
        Debug.Log("3 seconds passed");
    }
    
    IEnumerator ToggleLight()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            Light.enabled = !Light.enabled;
        }
    }
    
    IEnumerator TrafficLight()
    {
        redLight.enabled = true;
        yellowLight.enabled = false;
        greenLight.enabled = false;
        
        while (true)
        {
            //yield return new WaitForSeconds(5);
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
            yield return null;
            
            if (redLight.enabled)
            {
                redLight.enabled = false;
                greenLight.enabled = true;
            }
            else if (greenLight.enabled)
            {
                greenLight.enabled = false;
                yellowLight.enabled = true;
            }
            else
            {
                redLight.enabled = true;
                yellowLight.enabled = false;
            }
        }
    }
}
