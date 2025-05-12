using UnityEngine;

namespace _Scripts.Controller
{
    public class AugmentedRealityEnvironmentController : MonoBehaviour
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
