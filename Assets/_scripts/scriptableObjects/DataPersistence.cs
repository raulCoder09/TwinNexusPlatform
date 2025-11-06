using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

namespace _scripts.scriptableObjects
{
    public abstract class DataPersistence : ScriptableObject
    {
        internal async Task SaveAsync(string fileName = null)
        {
            var path = GetPath(fileName);
            var json = JsonUtility.ToJson(this);

            await Task.Run(() =>
            {
                var binaryFormatter = new BinaryFormatter();
                using (var file = File.Create(path))
                {
                    binaryFormatter.Serialize(file, json);
                }
            });
        }

        internal async Task LoadAsync(string fileName = null)
        {
            var path = GetPath(fileName);
            if (!File.Exists(path))
                return;

            await Task.Run(() =>
            {
                var binaryFormatter = new BinaryFormatter();
                using (var file = File.Open(path, FileMode.Open))
                {
                    var json = (string)binaryFormatter.Deserialize(file);
                    _pendingJson = json;
                }
            });
            
            if (!string.IsNullOrEmpty(_pendingJson))
            {
                JsonUtility.FromJsonOverwrite(_pendingJson, this);
                _pendingJson = null;
            }
        }

        private string _pendingJson;

        private string GetPath(string fileName = null)
        {
            var archiveName = string.IsNullOrEmpty(fileName) ? name : fileName;
            return $"{Application.persistentDataPath}/{archiveName}.txt";
        }
    }
}