using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualInformationAnchorTransformDemo : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(transform.rotation.eulerAngles.x, 180, transform.rotation.eulerAngles.z);
    }
}
