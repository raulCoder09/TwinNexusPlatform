using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Scripts.Controllers
{
    public class ImageTrackingHandlerController : MonoBehaviour
    {
        [SerializeField] private ARTrackedImageManager trackedImageManager;
        [SerializeField] private GameObject[] prefabsToInstantiate;
        [SerializeField] private ARVisibilityPlanePointsController arVisibilityController; 

        private readonly Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();

        private void Update()
        {
            if (trackedImageManager == null) return;

            foreach (var trackedImage in trackedImageManager.trackables)
            {
                if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                    if (!instantiatedPrefabs.TryGetValue(trackedImage.referenceImage.name, out var prefab1))
                    {
                        foreach (var prefab in prefabsToInstantiate)
                        {
                            if (prefab.name == trackedImage.referenceImage.name)
                            {
                                var newPrefab = Instantiate(prefab, trackedImage.transform.position, trackedImage.transform.rotation);
                                newPrefab.transform.parent = trackedImage.transform;
                                instantiatedPrefabs[trackedImage.referenceImage.name] = newPrefab;
                                break;
                            }
                        }
                    }
                    else
                    {
                        prefab1.transform.position = trackedImage.transform.position;
                        prefab1.transform.rotation = trackedImage.transform.rotation;
                        prefab1.SetActive(true);
                    }
                    
                    arVisibilityController?.RestoreUserDefinedVisibility();
                }
                else if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Limited || 
                         trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.None)
                {
                    if (instantiatedPrefabs.TryGetValue(trackedImage.referenceImage.name, out var prefab))
                    {
                        prefab.SetActive(false);
                    }
                }
            }
        }
    }
}
