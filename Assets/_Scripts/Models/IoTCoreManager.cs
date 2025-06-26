using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.IoT;
using Amazon;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using Amazon.IotData;
using Amazon.IotData.Model;
using UnityEngine.Serialization;

namespace _Scripts.Models
{
    public class IoTCoreManager : MonoBehaviour
    {
        [Header("AWS IoT Core Configuration")]
        [SerializeField] private string domainName = "d06815421so7uq8jzy8ln-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private string domainArn = "arn:aws:iot:us-east-1:156041417101:domainconfiguration/TwinNexusPlatformDomainConfiguration/axnbn";
        
        // Singleton instance
        public static IoTCoreManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}