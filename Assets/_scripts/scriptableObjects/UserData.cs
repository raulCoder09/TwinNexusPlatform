using Amazon;
using UnityEngine;
using UnityEngine.Serialization;

namespace _scripts.scriptableObjects
{
    [CreateAssetMenu(fileName = "UserData", menuName = "Tools/UserData", order = 0)]
    public class UserData:DataPersistence
    {
        private string _username;
        
        internal string username
        {
            get => _username;
            set => _username = value;
        }
        
    }
}