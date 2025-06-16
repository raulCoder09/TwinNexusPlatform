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
            Debug.Log("Ruta de guardado: " + _savePath);
        }
        
        internal void SaveData()
        {
            var json = JsonUtility.ToJson(iotConfigurationData);
            Debug.Log("JSON generado: " + json); 
            File.WriteAllText(_savePath, json);
            Debug.Log("Datos guardados en: " + _savePath);
        }
        internal void LoadData()
        {
            if (File.Exists(_savePath))
            {
                var json = File.ReadAllText(_savePath);
                JsonUtility.FromJsonOverwrite(json, iotConfigurationData);
                Debug.Log("Datos cargados desde: " + _savePath);
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
                Debug.Log("Aplicación pausada, datos guardados.");
            }
        }

        private void OnApplicationQuit()
        {
            SaveData();
            Debug.Log("Aplicación cerrada, datos guardados.");
        }

    }
}
