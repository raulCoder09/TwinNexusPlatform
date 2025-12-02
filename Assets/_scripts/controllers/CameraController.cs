using System;
using UnityEngine;

namespace _scripts.controllers
{
    public class CameraController:MonoBehaviour
    {
        private void Start()
        {
            SetView("Default");
        }
        internal void SetView(string view)
        {
            switch (view)
            {
                case "Top":
                    transform.position = new Vector3(-0.00103000004f,0.340000004f,0.140000001f);
                    transform.rotation = Quaternion.Euler(90f,0f,0f);
                    break;
                case "Front":
                    transform.position = new Vector3(0.00999999978f,0.0900000036f,0.419999987f);
                    transform.rotation = Quaternion.Euler(0f,180f,0f);
                    break;
                case "Back":
                    transform.position = new Vector3(0f,0.0900000036f,-0.200000003f);
                    transform.rotation = Quaternion.Euler(0f,0f,0f);
                    break;
                case "Left":
                    transform.position = new Vector3(-0.379999995f,0.0900000036f,0.129999995f);
                    transform.rotation = Quaternion.Euler(0f,90f,0f);
                    break;
                case "Right":
                    transform.position = new Vector3(0.388999999f,0.0900000036f,0.129999995f);
                    transform.rotation = Quaternion.Euler(0f,270f,0f);
                    break;
                default:
                    transform.position = new Vector3(-0.00103000004f,0.317999989f,-0.00700000022f);
                    transform.rotation = Quaternion.Euler(66.0000153f,0f,0f);
                    break;
                
            }
        }
    }
}

//
// private void Update()
// {
//     print($"Position, x: {transform.position.x}, y: {transform.position.y}, z: {transform.position.z}");
//     print($"Rotation, x: {transform.rotation.x}, y: {transform.rotation.y}, z: {transform.rotation.z}");
// }