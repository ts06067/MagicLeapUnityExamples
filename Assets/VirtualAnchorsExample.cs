using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class VirtualAnchorsExample : MonoBehaviour
{
    [SerializeField] private GameObject locationStatusManager;
    [SerializeField] private GameObject spatialAnchorsManager;

    // location for professor's desk
    private const float sampleHDist = 7.95f;
    private const float sampleVDist = 15.15f;

    // Start is called before the first frame update
    void Start()
    {
        // deactivate all of its children
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void DestroyPlanes()
    {
        // Destrohy all GameObjects layered as 'Plane'
        GameObject[] planes = GameObject.FindGameObjectsWithTag("Plane");
        foreach (GameObject plane in planes)
        {
            Destroy(plane);
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool isInsideLab = locationStatusManager.GetComponent<LocationStatusExample>().isInsideLab;
        Vector3 origin = SpatialAnchorsExample.origin;
        Vector3 axisPoint = SpatialAnchorsExample.axisPoint;
        Vector3 axisVector = axisPoint - origin;

        // if the player is inside the lab, activate the virtual anchors tagged as 'CVC_LAB'
        if (isInsideLab)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
                child.position = spatialAnchorsManager.GetComponent<SpatialAnchorsExample>().TranslateByPlanarDistanceOffset(origin, axisVector, sampleHDist, sampleVDist);
            }
        }
        // if the player is not inside the lab, deactivate the virtual anchors tagged as 'CVC_LAB'
        else
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
