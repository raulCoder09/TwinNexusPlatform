using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ArPlaceObjects : MonoBehaviour
{
    [SerializeField] private ARRaycastManager arRaycastManager;
    private bool isPlaced = false;
    private void Update()
    {
        if (!arRaycastManager) return;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !isPlaced)
        {
            isPlaced = true;
            if (Input.touchCount>0)
            {
                PlaceObject(Input.GetTouch(0).position);
            }
        }

    }

    private void PlaceObject(Vector2 touchPosition)
    {
        var raycastHits = new List<ARRaycastHit>();
        arRaycastManager.Raycast(touchPosition, raycastHits, TrackableType.AllTypes);
        if (raycastHits.Count>0)
        {
            var hitPosePosition = raycastHits[0].pose.position;
            var hitPoseRotation = raycastHits[0].pose.rotation;
            Instantiate(arRaycastManager.raycastPrefab, hitPosePosition, hitPoseRotation);
        }
        StartCoroutine(SetPlacingTofalseWithDelay(0.25f));
    }

    IEnumerator SetPlacingTofalseWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isPlaced = false;
    }
}
