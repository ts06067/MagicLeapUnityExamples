using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class VirtualAnchorsExample : MonoBehaviour
{
    [SerializeField] private GameObject locationStatusManager;
    [SerializeField] private GameObject spatialAnchorsManager;
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Text fileStatusText;

    // location for professor's desk
    private const float sampleHDist = 7.95f;
    private const float sampleVDist = 15.15f;

    // Start is called before the first frame update
    void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }

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

    public void InitializeAnchors()
    {
        string[] imageFiles;
        string[] textFiles;
        string[] folderNames = { };
        string folderPath = Application.persistentDataPath + "/MLDB";

        try
        {
            folderNames = Directory.GetDirectories(folderPath);
            fileStatusText.text = "";

            foreach (string folderName in folderNames)
            {
                imageFiles = Directory.GetFiles(folderName, "*.jpg");
                textFiles = Directory.GetFiles(folderName, "*.txt");

                fileStatusText.text += textFiles.Length + " ";

                string nameText = File.ReadAllText(textFiles[0]);
                string descriptionText = File.ReadAllText(textFiles[1]);
                string coordinatesText = File.ReadAllText(textFiles[2]);

                // parse the coordinatesText to get the x and y coordinates separated by a comma
                string[] coordinates = coordinatesText.Split(',');

                float hDist = float.Parse(coordinates[0]);
                float vDist = float.Parse(coordinates[1]);

                Vector3 origin = SpatialAnchorsExample.origin;
                Vector3 axisPoint = SpatialAnchorsExample.axisPoint;
                Vector3 axisVector = axisPoint - origin;

                Vector3 pos = spatialAnchorsManager.GetComponent<SpatialAnchorsExample>().TranslateByPlanarDistanceOffset(origin, axisVector, hDist, vDist);

                GameObject go = Instantiate(prefabToSpawn, pos, Quaternion.identity);

                GameObject canvas = go.transform.Find("Canvas").gameObject;

                TMP_Text nameField = canvas.transform.Find("AnchorIDText").gameObject.GetComponent<TMP_Text>();
                TMP_Text descriptionField = canvas.transform.Find("InformationText").gameObject.GetComponent<TMP_Text>();
                nameField.text = nameText;
                descriptionField.text = descriptionText;

                GameObject image = canvas.transform.Find("RawImage").gameObject;

                // Load the image from the file into a Texture
                byte[] fileData = File.ReadAllBytes(imageFiles[0]);
                Texture2D tex = new(2, 2);
                tex.LoadImage(fileData);

                // Assign the texture to the RawImage component
                image.GetComponent<RawImage>().texture = tex;
            }
        }
        catch (Exception e)
        {
            //fileStatusText.text = e.ToString();
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