using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using UnityEngine.XR.OpenXR.NativeTypes;
using MagicLeap.Android;

public class SpatialAnchorsExample : MonoBehaviour
{
    [SerializeField] private Text playerPoseText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text localizationText;
    [SerializeField] private Dropdown mapsDropdown;
    [SerializeField] private Dropdown exportedDropdown;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject anchorPrefab;
    [SerializeField] private GameObject controllerObject;
    [SerializeField] private GameObject createAnchorPopUp;
    [SerializeField] private GameObject cube;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private Button publishButton;
    private InputActionMap controllerMap;
    private MagicLeapSpatialAnchorsFeature spatialAnchorsFeature;
    private MagicLeapLocalizationMapFeature localizationMapFeature;
    private MagicLeapSpatialAnchorsStorageFeature storageFeature;
    private MagicLeapLocalizationMapFeature.LocalizationMap[] mapList = Array.Empty<MagicLeapLocalizationMapFeature.LocalizationMap>();
    private MagicLeapLocalizationMapFeature.LocalizationEventData mapData;
    private readonly List<string> referenceAnchorMapPositionIds = new() { "6f29d70c-efbc-7018-a24f-a83bfe1757d8", "ff538aa9-527f-7018-9168-667cacaf858b" };

    public Vector3 origin;
    public Vector3 axisPoint;

    private float distance;
    private float hAngle;
    private float yOffset;

    private struct PublishedAnchor
    {
        public ulong AnchorId;
        public string AnchorMapPositionId;
        public ARAnchor AnchorObject;
    }

    private List<PublishedAnchor> publishedAnchors = new();
    private List<ARAnchor> activeAnchors = new();
    private List<ARAnchor> pendingPublishedAnchors = new();
    private List<ARAnchor> localAnchors = new();
    private Dictionary<string, byte[]> exportedMaps = new();
    private bool permissionGranted = true;
    private MLXrAnchorSubsystem activeSubsystem;

    private IEnumerator Start()
    {
        yield return new WaitUntil(AreSubsystemsLoaded);

        spatialAnchorsFeature = OpenXRSettings.Instance.GetFeature<MagicLeapSpatialAnchorsFeature>();
        storageFeature = OpenXRSettings.Instance.GetFeature<MagicLeapSpatialAnchorsStorageFeature>();
        localizationMapFeature = OpenXRSettings.Instance.GetFeature<MagicLeapLocalizationMapFeature>();
        if (!spatialAnchorsFeature || !localizationMapFeature || !storageFeature)
        {
            statusText.text = "Spatial Anchors, Spatial Anchors Storage, or Localization maps features not enabled; disabling";
            enabled = false;
        }

        if (inputActions != null)
        {
            controllerMap = inputActions.FindActionMap("Controller");
            if (controllerMap == null)
            {
                Debug.LogError("Couldn't find Controller action map");
                enabled = false;
            }
            else
            {
                controllerMap.FindAction("Bumper").performed += OnBumper;
                controllerMap.FindAction("MenuButton").performed += OnMenu;
            }
        }

        mapsDropdown.ClearOptions();
        exportedDropdown.ClearOptions();
        storageFeature.OnCreationCompleteFromStorage += OnCreateFromStorageComplete;
        storageFeature.OnPublishComplete += OnPublishComplete;
        storageFeature.OnQueryComplete += OnQueryComplete;
        storageFeature.OnDeletedComplete += OnDeletedComplete;

        //Permissions.RequestPermission(MLPermission.SpatialAnchors, OnPermissionGranted, OnPermissionDenied);

        // ADDED
        if (localizationMapFeature.GetLocalizationMapsList(out mapList) == XrResult.Success)
        {
            mapsDropdown.AddOptions(mapList.Select(map => map.Name).ToList());
            mapsDropdown.Hide();
        }

        XrResult res = localizationMapFeature.EnableLocalizationEvents(true);
        if (res != XrResult.Success)
            Debug.LogError("EnableLocalizationEvents failed: " + res);
    }

    private bool AreSubsystemsLoaded()
    {
        if (XRGeneralSettings.Instance == null) return false;
        if (XRGeneralSettings.Instance.Manager == null) return false;
        var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
        if (activeLoader == null) return false;
        activeSubsystem = activeLoader.GetLoadedSubsystem<XRAnchorSubsystem>() as MLXrAnchorSubsystem;
        return activeSubsystem != null;
    }

    private void OnPermissionDenied(string permission)
    {
        permissionGranted = false;
        Debug.LogError("Spatial anchor publishing and map localization will not work without permission.");
    }

    private void OnPermissionGranted(string permission)
    {
        permissionGranted = true;
        if (localizationMapFeature.GetLocalizationMapsList(out mapList) == XrResult.Success)
        {
            mapsDropdown.AddOptions(mapList.Select(map => map.Name).ToList());
            mapsDropdown.Hide();
        }

        XrResult res = localizationMapFeature.EnableLocalizationEvents(true);
        if (res != XrResult.Success)
            Debug.LogError("EnableLocalizationEvents failed: " + res);
    }

    private void OnPublishComplete(ulong anchorId, string anchorMapPositionId)
    {
        for (int i = activeAnchors.Count - 1; i >= 0; i--)
        {
            if (activeSubsystem.GetAnchorId(activeAnchors[i]) == anchorId)
            {
                PublishedAnchor newPublishedAnchor;
                newPublishedAnchor.AnchorId = anchorId;
                newPublishedAnchor.AnchorMapPositionId = anchorMapPositionId;
                newPublishedAnchor.AnchorObject = activeAnchors[i];

                activeAnchors[i].GetComponent<MeshRenderer>().material.color = Color.white;

                publishedAnchors.Add(newPublishedAnchor);
                activeAnchors.RemoveAt(i);
                break;
            }
        }
    }

    private void OnBumper(InputAction.CallbackContext _)
    {
        /*
        Pose currentPose = new Pose(controllerObject.transform.position, controllerObject.transform.rotation);

        GameObject newAnchor = Instantiate(anchorPrefab, currentPose.position, currentPose.rotation);

        ARAnchor newAnchorComponent = newAnchor.AddComponent<ARAnchor>();

        newAnchorComponent.GetComponent<MeshRenderer>().material.color = Color.grey;

        activeAnchors.Add(newAnchorComponent);
        localAnchors.Add(newAnchorComponent);
        */

        createAnchorPopUp.SetActive(true);

        // place the pop-up in front of the controller
        createAnchorPopUp.transform.position = controllerObject.transform.position + controllerObject.transform.forward * 0.5f;
    }

    private void OnMenu(InputAction.CallbackContext _)
    {
        // delete all local anchors
        for (int i = localAnchors.Count - 1; i >= 0; i--)
        {
            Destroy(localAnchors[i].gameObject);
            localAnchors.RemoveAt(i);
        }

        //Deleting each published anchor.
        for (int i = publishedAnchors.Count - 1; i >= 0; i--)
        {
            if (publishedAnchors[i].AnchorObject != null)
            {
                Destroy(publishedAnchors[i].AnchorObject.gameObject);
            }
            publishedAnchors.RemoveAt(i);
        }
    }

    private void OnQueryComplete(List<string> anchorMapPositionIds)
    {
        if (publishedAnchors.Count == 0)
        {
            if (!storageFeature.CreateSpatialAnchorsFromStorage(anchorMapPositionIds))
                Debug.LogError("Couldn't create spatial anchors from storage");
            return;
        }

        foreach (string anchorMapPositionId in anchorMapPositionIds)
        {
            var matches = publishedAnchors.Where(p => p.AnchorMapPositionId == anchorMapPositionId);
            if (matches.Count() == 0)
            {
                if (!storageFeature.CreateSpatialAnchorsFromStorage(new List<string>() { anchorMapPositionId }))
                    Debug.LogError("Couldn't create spatial anchors from storage");
            }

            /*
             240408 ADDED: Update the anchor's text component to display the anchor's map position ID
             */

            ARAnchor anchor = publishedAnchors.Find(p => p.AnchorMapPositionId == anchorMapPositionId).AnchorObject;

            if (anchor == null)
            {
                Debug.LogError("Couldn't find anchor object for anchorMapPositionId: " + anchorMapPositionId);
                continue;
            }
        }

        for (int i = publishedAnchors.Count - 1; i >= 0; i--)
        {
            if (!anchorMapPositionIds.Contains(publishedAnchors[i].AnchorMapPositionId))
            {
                GameObject.Destroy(publishedAnchors[i].AnchorObject.gameObject);
                publishedAnchors.RemoveAt(i);
            }
        }

    }

    private void OnDeletedComplete(List<string> anchorMapPositionIds)
    {
        foreach (string anchorMapPositionId in anchorMapPositionIds)
        {
            for (int i = publishedAnchors.Count - 1; i >= 0; i--)
            {
                if (publishedAnchors[i].AnchorMapPositionId == anchorMapPositionId)
                {
                    GameObject.Destroy(publishedAnchors[i].AnchorObject.gameObject);
                    publishedAnchors.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void OnCreateFromStorageComplete(Pose pose, ulong anchorId, string anchorMapPositionId, XrResult result)
    {
        if (result != XrResult.Success)
        {
            Debug.LogError("Could not create anchor from storage: " + result);
            return;
        }

        PublishedAnchor newPublishedAnchor;
        newPublishedAnchor.AnchorId = anchorId;
        newPublishedAnchor.AnchorMapPositionId = anchorMapPositionId;

        GameObject newAnchor = Instantiate(anchorPrefab, pose.position, pose.rotation);

        ARAnchor newAnchorComponent = newAnchor.AddComponent<ARAnchor>();

        // change activeAnchor's Text component's text to anchorMapPositionId
        Canvas canvas = newAnchor.GetComponentInChildren<Canvas>();

        // get canvas's Text Mesh Pro component
        TMPro.TextMeshProUGUI text = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        // set text to anchorMapPositionId
        text.text = anchorMapPositionId;

        // if anchorMapPositionId is in referenceAnchorMapPositionIds, set color to cyan
        if (referenceAnchorMapPositionIds.Contains(anchorMapPositionId))
        {
            // if the search index is 0, set the origin to the anchor's position
            if (referenceAnchorMapPositionIds.IndexOf(anchorMapPositionId) == 0)
            {
                origin = pose.position;
                newAnchorComponent.GetComponent<MeshRenderer>().material.color = Color.cyan;
            }
            // if the search index is 1, set the axisPoint to the anchor's position
            else if (referenceAnchorMapPositionIds.IndexOf(anchorMapPositionId) == 1)
            {
                axisPoint = pose.position;
                newAnchorComponent.GetComponent<MeshRenderer>().material.color = Color.yellow;
            }
        }

        newPublishedAnchor.AnchorObject = newAnchorComponent;

        publishedAnchors.Add(newPublishedAnchor);
    }

    public void PublishAnchors()
    {
        foreach (ARAnchor anchor in localAnchors)
            pendingPublishedAnchors.Add(anchor);

        localAnchors.Clear();
    }

    public void LocalizeMap()
    {
        if (permissionGranted == false || localizationMapFeature == null)
            return;

        string map = mapList.Length > 0 ? mapList[mapsDropdown.value].MapUUID : "";
        var res = localizationMapFeature.RequestMapLocalization(map);
        if (res != XrResult.Success)
        {
            Debug.LogError("Failed to request localization: " + res);
            return;
        }

        //On map change, we need to clear up present published anchors and query new ones
        foreach (PublishedAnchor obj in publishedAnchors)
            Destroy(obj.AnchorObject.gameObject);
        publishedAnchors.Clear();

        foreach (ARAnchor anchor in localAnchors)
            Destroy(anchor.gameObject);
        localAnchors.Clear();

        activeAnchors.Clear();
    }

    public void ExportMap()
    {
        if (permissionGranted == false || localizationMapFeature == null || mapList.Length == 0)
            return;
        string uuid = mapList[mapsDropdown.value].MapUUID;
        var res = localizationMapFeature.ExportLocalizatioMap(uuid, out byte[] mapData);
        if (res != XrResult.Success)
        {
            Debug.LogError("Failed to export map: " + res);
            return;
        }
        exportedMaps.Add(mapList[mapsDropdown.value].Name, mapData);
        exportedDropdown.ClearOptions();
        exportedDropdown.AddOptions(exportedMaps.Keys.ToList());
        exportedDropdown.Hide();
    }

    public void ImportMap()
    {
        if (permissionGranted == false || localizationMapFeature == null || exportedMaps.Count == 0)
            return;

        var idx = exportedDropdown.value;
        var mapName = exportedDropdown.options[idx].text;
        if (exportedMaps.TryGetValue(mapName, out byte[] mapData))
        {
            var res = localizationMapFeature.ImportLocalizationMap(mapData, out _);
            if (res == XrResult.Success)
            {
                exportedMaps.Remove(mapName);
                exportedDropdown.ClearOptions();
                exportedDropdown.AddOptions(exportedMaps.Keys.ToList());
                exportedDropdown.Hide();
            }
        }
    }

    public void QueryAnchors()
    {
        if (!storageFeature.QueryStoredSpatialAnchors(controllerObject.transform.position, 99999f))
        {
            Debug.LogError("Could not query stored anchors");
        }
    }

    void Update()
    {
        if (permissionGranted)
        {
            if (pendingPublishedAnchors.Count > 0)
            {
                for (int i = pendingPublishedAnchors.Count - 1; i >= 0; i--)
                {
                    if (pendingPublishedAnchors[i].trackingState == TrackingState.Tracking)
                    {
                        ulong anchorId = activeSubsystem.GetAnchorId(pendingPublishedAnchors[i]);
                        if (!storageFeature.PublishSpatialAnchorsToStorage(new List<ulong>() { anchorId }, 0))
                        {
                            Debug.LogError($"Failed to publish anchor {anchorId} at position {pendingPublishedAnchors[i].gameObject.transform.position} to storage");
                            statusText.text = "Failed to publish anchor to storage";
                        }
                        else
                        {
                            pendingPublishedAnchors.RemoveAt(i);
                        }
                    }
                }
            }

            /*
             * 240409 ADDED: Update the playerPoseText to display the player's position and rotation
             */

            playerPoseText.text = $"Player Pose:\nPosition: {controllerObject.transform.position}\nRotation: {controllerObject.transform.rotation}";

            if (localizationMapFeature != null)
            {
                localizationMapFeature.GetLatestLocalizationMapData(out mapData);
                localizationText.text = string.Format("Localization info:\nName:{0}\nUUID:{1}\nType:{2}\nState:{3}\nConfidence:{4}\nErrors:{5}",
                    mapData.Map.Name, mapData.Map.MapUUID, mapData.Map.MapType, mapData.State, mapData.Confidence, (mapData.Errors.Length > 0) ? string.Join(",", mapData.Errors) : "None");

                publishButton.interactable = mapData.State == MagicLeapLocalizationMapFeature.LocalizationMapState.Localized;
            }
            else
            {
                publishButton.interactable = false;
            }

            if (origin != null && axisPoint != null)
            {
                Vector3 axisVector = axisPoint - origin;
                Vector3 controllerPosition = controllerObject.transform.position;
                Vector3 controllerVector = controllerPosition - origin;

                (distance, hAngle) = GetDistanceAndSignedAngleFromOrigin(origin, axisPoint, controllerPosition);

                Vector3 newPosition = TranslateByDistanceAndSignedAngle(origin, axisVector, 1, -90);

                newPosition.y = origin.y;

                cube.transform.position = newPosition;

                (float hDistance, float vDistance) = GetPlanarDistanceFromOrigin(origin, axisPoint, new Vector3(controllerPosition.x, origin.y, controllerPosition.z));
                
                if (-90 < hAngle && hAngle < 90)
                {
                    hDistance = -hDistance;
                }
                
                if (0 < hAngle && hAngle < 180)
                {
                    vDistance = -vDistance;
                }

                playerPoseText.text = $"hDist {hDistance} \nvDist: {vDistance} \nhAngle: {hAngle}";
            }
        }
    }

    private (float, float) GetPlanarDistanceFromOrigin(Vector3 origin, Vector3 axisPoint, Vector3 target)
    {
        Vector3 axisVector = axisPoint - origin;
        Vector3 targetVector = target - origin;

        // find the projection of targetVector onto axisVector
        Vector3 projection = Vector3.Project(targetVector, axisVector);

        // get projection's magnitude
        float hDistance = projection.magnitude;

        // find the 90 degrees rotated vector of axisVector using TranslateByDistanceAndSignedAngle
        Vector3 rotatedAxisVector = Quaternion.AngleAxis(-90, Vector3.up) * axisVector;

        // find the projection of targetVector onto rotatedAxisVector
        Vector3 rotatedProjection = Vector3.Project(targetVector, rotatedAxisVector);

        // get rotatedProjection's magnitude
        float vDistance = rotatedProjection.magnitude;

        return (hDistance, vDistance);
    }

    private (float, float) GetDistanceAndSignedAngleFromOrigin(Vector3 origin, Vector3 axisPoint, Vector3 target)
    {
        Vector3 axisVector = axisPoint - origin;
        Vector3 targetVector = target - origin;

        // get horizontal angle between axisVector and targetVector
        float hAngle = Vector3.SignedAngle(new Vector3(axisVector.x, 0, axisVector.z), new Vector3(targetVector.x, 0, targetVector.z), Vector3.up);

        // get distance between target and origin
        float distance = Vector3.Distance(target, origin);

        return (distance, hAngle);
    }

    private Vector3 TranslateByDistanceAndSignedAngle(Vector3 vector, Vector3 axisVector, float distance, float hAngle)
    {
        // rotate axisVector by hAngle, with respect to Vector3.up
        Vector3 rotatedAxisVector = Quaternion.AngleAxis(hAngle, Vector3.up) * axisVector;

        // shorten rotatedAxisVector to distance
        Vector3 newRotatedAxisVector = rotatedAxisVector.normalized * distance;

        return vector + newRotatedAxisVector;
    }

    private void OnDestroy()
    {
        if (inputActions != null && controllerMap != null)
        {
            controllerMap.FindAction("Bumper").performed -= OnBumper;
            controllerMap.FindAction("MenuButton").performed -= OnMenu;
        }

        if (localizationMapFeature != null)
        {
            localizationMapFeature.EnableLocalizationEvents(false);
        }

        storageFeature.OnCreationCompleteFromStorage -= OnCreateFromStorageComplete;
        storageFeature.OnPublishComplete -= OnPublishComplete;
        storageFeature.OnQueryComplete -= OnQueryComplete;
        storageFeature.OnDeletedComplete -= OnDeletedComplete;
    }
}
