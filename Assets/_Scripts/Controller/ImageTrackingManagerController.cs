using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Scripts.Controller
{
    public class ImageTrackingManagerController : MonoBehaviour
    {
        // private void OnEnable()
        // {
        //     GetComponent<ARTrackedImageManager>().trackedImagesChanged += OnTrackedImagesChanged;
        // }
        //
        // private void OnDisable()
        // {
        //     GetComponent<ARTrackedImageManager>().trackedImagesChanged -= OnTrackedImagesChanged;
        // }
        //
        // void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        // {
        //     foreach (var trackedImage in eventArgs.added)
        //     {
        //         Debug.Log($"Imagen detectada: {trackedImage.referenceImage.name}");
        //
        //     }
        //
        //     foreach (var trackedImage in eventArgs.updated)
        //     {
        //         Debug.Log($"Imagen actualizada: {trackedImage.referenceImage.name}");
        //     }
        //
        //     foreach (var trackedImage in eventArgs.removed)
        //     {
        //         Debug.Log($"Imagen eliminada: {trackedImage.referenceImage.name}");
        //     }
        // }
    }
}
