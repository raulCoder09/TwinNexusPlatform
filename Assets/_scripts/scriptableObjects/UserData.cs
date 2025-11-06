using Amazon;
using UnityEngine;
using UnityEngine.Serialization;

namespace _scripts.scriptableObjects
{
    [CreateAssetMenu(fileName = "UserData", menuName = "Tools/UserData", order = 0)]
    public class UserData:DataPersistence
    {
        private string _username;
        
        
        private RegionEndpoint _region=Amazon.RegionEndpoint.USEast1;
        private string _identityPoolID = "us-east-1:e962d906-6f36-4e52-8771-a2de6a11b19a";
        private string _userPoolID     = "us-east-1_eyRKiPuWJ"; 

        internal string username
        {
            get => _username;
            set => _username = value;
        }

        internal RegionEndpoint region
        {
            get => _region;
            set => _region = value;
        }

        internal string identityPoolID
        {
            get => _identityPoolID;
            set => _identityPoolID = value;
        }

        internal string userPoolID
        {
            get => _userPoolID;
            set => _userPoolID = value;
        }
    }
}