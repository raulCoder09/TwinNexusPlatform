using UnityEngine;

namespace _Scripts.Controller
{
    public class LinkController : MonoBehaviour
    {
        [SerializeField] private GameObject link;
        [SerializeField] private Mode mode;

        private enum Mode
        {
            Rotary,
            Linear
        }
        
        // private void Update()
        // {
        //     switch (mode)
        //     {
        //         case Mode.Rotary:
        //             Debug.Log("Modo Rotary seleccionado");
        //             break;
        //         case Mode.Linear:
        //             Debug.Log("Modo Linear seleccionado");
        //             break;
        //     }
        // }
    }
}