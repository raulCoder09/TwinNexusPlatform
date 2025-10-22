using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ArPlaceObjects : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private GameObject placementPrefab;   

    private static readonly List<ARRaycastHit> hits = new();
    private bool placed = false;

    void Update()
    {
        if (placed) return;                    
        if (!arRaycastManager) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        TryPlace(touch.position);
    }

    private void TryPlace(Vector2 touchPos)
    {
        if (arRaycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon) && hits.Count > 0)
        {
            Pose pose = hits[0].pose;

            pose.rotation = Quaternion.Euler(0f, pose.rotation.eulerAngles.y, 0f);

            Instantiate(placementPrefab, pose.position, pose.rotation);

            placed = true;
            Debug.Log("[ArPlaceObjects] Robot instanciado en: " + pose.position);
        }
    }
}
