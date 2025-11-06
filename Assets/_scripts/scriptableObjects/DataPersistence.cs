using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace _scripts.scriptableObjects
{
    public abstract class DataPersistence : ScriptableObject
    {
        internal void Save(string fileName=null)
        {
            var binaryFormatter = new BinaryFormatter();
            var file = File.Create(GetPath(fileName));
            var json = JsonUtility.ToJson(this);
            binaryFormatter.Serialize(file, json);
            file.Close();
        }

        internal virtual void Load(string fileName=null)
        {
            if (!File.Exists(GetPath(fileName))) return;
            var binaryFormatter = new BinaryFormatter();
            var file = File.Open(GetPath(fileName), FileMode.Open);
            JsonUtility.FromJsonOverwrite((string)binaryFormatter.Deserialize(file), this);
            file.Close();
        }

        private string GetPath(string fileName=null)
        {
            var archiveName=string.IsNullOrEmpty(fileName)?name:fileName;
            return $"{Application.persistentDataPath}/{archiveName}.txt";
        }
    }
}
