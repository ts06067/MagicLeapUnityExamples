using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour
{
    public Vector3 destPos = Vector3.zero;
    private Transform userPos;
    // Start is called before the first frame update
    void Start()
    {
        userPos = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = destPos - userPos.position;

        this.transform.LookAt(direction);
        this.transform.Rotate(0, 90, 0);
    }
}
