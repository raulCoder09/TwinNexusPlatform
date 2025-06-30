using UnityEngine;

namespace _Scripts.Controller
{
    public class RealDeviceEnvironmentController : MonoBehaviour
    {
        internal void EnableEnvironment()
        {
            gameObject.SetActive(true);
        }
        internal void DisableEnvironment()
        {
            gameObject.SetActive(false);
        }
    }
}
