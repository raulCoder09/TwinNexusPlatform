using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace _Scripts.Controllers
{
    public class ARVisibilityPlanePointsController : MonoBehaviour
    {
        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private ARPointCloudManager arPointCloudManager;

        private bool planesVisible = true;
        private bool pointsVisible = true;

        public void TogglePlaneVisibility()
        {
            planesVisible = !planesVisible;
            ApplyPlaneVisibility();
        }

        public void TogglePointCloudVisibility()
        {
            pointsVisible = !pointsVisible;
            ApplyPointCloudVisibility();
        }

        private void ApplyPlaneVisibility()
        {
            if (arPlaneManager == null) return;

            // Ocultar/mostrar los planos sin afectar la detección
            foreach (var plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(planesVisible);
            }
        }

        private void ApplyPointCloudVisibility()
        {
            if (arPointCloudManager == null) return;

            // Ocultar/mostrar los puntos de nube sin afectar la detección
            foreach (var pointCloud in arPointCloudManager.trackables)
            {
                pointCloud.gameObject.SetActive(pointsVisible);
            }
        }

        // Método para garantizar que el estado del usuario no sea sobrescrito
        public void RestoreUserDefinedVisibility()
        {
            ApplyPlaneVisibility();
            ApplyPointCloudVisibility();
        }
    }
}
