using System.IO;
using UnityEngine;

namespace _Scripts.Models.CognitoManagement
{
    public static class CognitoSettingsManager
    {
        private static readonly string ConfigFileName = "cognito_settings.json";
        private static readonly string ConfigFilePath = Path.Combine(Application.persistentDataPath, ConfigFileName);
        
        /// <summary>
        /// Saves configuration to JSON file
        /// </summary>
        public static bool SaveConfiguration(SettingsCognitoParametersData configData)
        {
            try
            {
                var serializableData = configData.ToSerializableData();
                var json = JsonUtility.ToJson(serializableData, true);
                
                File.WriteAllText(ConfigFilePath, json);
                
                Debug.Log($"[CognitoSettingsManager] Configuration saved to: {ConfigFilePath}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CognitoSettingsManager] Failed to save configuration: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        public static bool LoadConfiguration(SettingsCognitoParametersData configData)
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    Debug.Log("[CognitoSettingsManager] No saved configuration found, using defaults");
                    return false;
                }
                
                var json = File.ReadAllText(ConfigFilePath);
                var serializableData = JsonUtility.FromJson<SettingsCognitoParametersData.SerializableData>(json);
                
                if (serializableData != null)
                {
                    configData.FromSerializableData(serializableData);
                    Debug.Log("[CognitoSettingsManager] Configuration loaded successfully");
                    return true;
                }
                
                Debug.LogWarning("[CognitoSettingsManager] Failed to deserialize configuration");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CognitoSettingsManager] Failed to load configuration: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if configuration file exists
        /// </summary>
        public static bool ConfigurationExists()
        {
            return File.Exists(ConfigFilePath);
        }
        
        /// <summary>
        /// Deletes the configuration file
        /// </summary>
        public static bool DeleteConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    File.Delete(ConfigFilePath);
                    Debug.Log("[CognitoSettingsManager] Configuration file deleted");
                    return true;
                }
                
                Debug.Log("[CognitoSettingsManager] No configuration file to delete");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CognitoSettingsManager] Failed to delete configuration: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        public static string GetConfigurationPath()
        {
            return ConfigFilePath;
        }
    }
}