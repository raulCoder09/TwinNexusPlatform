using UnityEngine;

namespace _Scripts.Controller
{
    public class AbstractController : MonoBehaviour
    {
        internal void EnableLevel()
        {
            gameObject.SetActive(true);
        }

        internal void DisableLevel()
        {
            gameObject.SetActive(false);
        }
    }
}
