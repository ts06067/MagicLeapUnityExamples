using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LocationStatusExample : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject spatialAnchorsManager;

    public bool isInsideLab = false;
    public Vector3 origin;
    public Vector3 axisPoint;
    public Vector3 axisVector;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float hDist = SpatialAnchorsExample.HDistance;
        float vDist = SpatialAnchorsExample.VDistance;

        origin = SpatialAnchorsExample.origin;
        axisPoint = SpatialAnchorsExample.axisPoint;
        axisVector = axisPoint - origin;

        // update the status text
        statusText.text = "Horizontal Distance: " + hDist + "\nVertical Distance: " + vDist;

        // if the distance is bigger than 7, add to the status text that the player is in a 'LAB' location. Otherwise, add that the player is in a 'CORRIDOR' location.
        if (vDist > 7)
        {
            statusText.text += "\nLocation: CVC_LAB";
            isInsideLab = true;
        }
        else
        {
            statusText.text += "\nLocation: CORRIDOR";
            isInsideLab = false;
        }
    }
}
