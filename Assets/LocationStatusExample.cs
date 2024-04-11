using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocationStatusExample : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject spatialAnchorsManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float hDist = spatialAnchorsManager.GetComponent<SpatialAnchorsExample>().HDistance;
        float vDist = spatialAnchorsManager.GetComponent<SpatialAnchorsExample>().VDistance;

        // update the status text
        statusText.text = "Horizontal Distance: " + hDist + "\nVertical Distance: " + vDist;

        // if the distance is bigger than 7, add to the status text that the player is in a 'LAB' location. Otherwise, add that the player is in a 'CORRIDOR' location.
        if (vDist > 7)
        {
            statusText.text += "\nLocation: CVC_LAB";
        }
        else
        {
            statusText.text += "\nLocation: CORRIDOR";
        }
    }
}
