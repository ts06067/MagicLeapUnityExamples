using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class AnchorCreationExample : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Text planeIDText;

    void Start()
    {
        // deactivate itself first
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        ARPlane leftPlane = PlaneExample.GetPerpendicularPlane(PlaneExample.selectedPerpendicularARPlane, "left");
        ARPlane rightPlane = PlaneExample.GetPerpendicularPlane(PlaneExample.selectedPerpendicularARPlane, "right");
        ARPlane adjLeftPlane = PlaneExample.GetAdjacentPlane(PlaneExample.selectedAdjacentARPlane, "left");
        ARPlane adjRightPlane = PlaneExample.GetAdjacentPlane(PlaneExample.selectedAdjacentARPlane, "right");

        if (!PlanePrefabExample.trackableIds.Contains(leftPlane.trackableId))
        {
            PlanePrefabExample.trackableIds.Add(leftPlane.trackableId);
        }

        if (!PlanePrefabExample.trackableIds.Contains(rightPlane.trackableId))
        {
            PlanePrefabExample.trackableIds.Add(rightPlane.trackableId);
        }

        planeIDText.text = "\nLeft ID: " + leftPlane.trackableId;
        planeIDText.text += "\nRight ID: " + rightPlane.trackableId;
    }
}
