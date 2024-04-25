using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class VirtualAnchorsExample : MonoBehaviour
{
    [SerializeField] private GameObject locationStatusManager;
    [SerializeField] private GameObject spatialAnchorsManager;
    [SerializeField] private GameObject prefabToSpawn;

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

    private void InitializeAnchors()
    {
        string[] imageFiles;
        string[] textFiles;
        string[] coordinateFiles;
        string[] folderNames;
        string folderPath = "Assets/MLDB";

        folderNames = UnityEditor.AssetDatabase.GetSubFolders(folderPath);

        for (int i=0; i<folderNames.Length; i++)
        {
            imageFiles = Directory.GetFiles(folderNames[i - 1], "*.jpg");
            textFiles = Directory.GetFiles(folderNames[i - 1], "*.txt");

            string nameText = System.IO.File.ReadAllText(textFiles[0]);
            string descriptionText = System.IO.File.ReadAllText(textFiles[1]);
            string coordinatesText = System.IO.File.ReadAllText(textFiles[2]);

            // parse the coordinatesText to get the x and y coordinates separated by a comma
            string[] coordinates = coordinatesText.Split(',');

            float hDist = float.Parse(coordinates[0]);
            float vDist = float.Parse(coordinates[1]);

            Vector3 origin = SpatialAnchorsExample.origin;
            Vector3 axisPoint = SpatialAnchorsExample.axisPoint;
            Vector3 axisVector = axisPoint - origin;

            Vector3 pos = spatialAnchorsManager.GetComponent<SpatialAnchorsExample>().TranslateByPlanarDistanceOffset(origin, axisVector, hDist, vDist);

            GameObject go = Instantiate(prefabToSpawn, pos, Quaternion.identity);

            TMP_Text nameField = go.transform.Find("Name").gameObject.GetComponent<TMP_Text>();
            TMP_Text descriptionField = go.transform.Find("Text").gameObject.GetComponent<TMP_Text>();
            nameField.text = nameText;
            descriptionField.text = descriptionText;

            Texture2D jpgImage = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(imageFiles[0]);
            Sprite sprite = Sprite.Create(jpgImage, new Rect(0, 0, jpgImage.width, jpgImage.height), Vector2.zero);
            go.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
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