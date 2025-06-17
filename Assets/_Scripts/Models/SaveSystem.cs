using System.IO;
using _ScriptableObjects;
using UnityEngine;

namespace _Scripts.Models
{
    public class SaveSystem : MonoBehaviour
    {
        [SerializeField]private IotConfigurationData iotConfigurationData;
        private string _savePath;
        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "iotConfig.json");
        }
        
        internal void SaveData()
        {
            var json = JsonUtility.ToJson(iotConfigurationData);
            File.WriteAllText(_savePath, json);
        }
        internal void LoadData()
        {
            if (File.Exists(_savePath))
            {
                var json = File.ReadAllText(_savePath);
                JsonUtility.FromJsonOverwrite(json, iotConfigurationData);
            }
            else
            {
                Debug.LogWarning("No se encontró archivo de guardado.");
            }
        }
        public void ResetToDefault()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
                Debug.Log("Configuración restablecida a valores por defecto.");
            }
        }
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveData();
            }
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

    }
}
