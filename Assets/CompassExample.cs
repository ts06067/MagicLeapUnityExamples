using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassExample : MonoBehaviour
{
    [SerializeField] private Vector3 target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.main.transform.position + Camera.main.transform.forward + new Vector3(0,-.5f,0);
        transform.LookAt(target);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}
