namespace _Scripts.Models.CloudWatchManagement
{
    [System.Serializable]
    public class MonitoringLogEntry
    {
        public string timestamp;
        public string logLevel;
        public string eventType;
        public string userId;
        public string sessionId;
        public string eventData;
        public DeviceInfo deviceInfo;
        public string unityVersion;
        public string platform;
        public int systemMemorySize;
        public int graphicsMemorySize;
    }

    [System.Serializable]
    public class DeviceInfo
    {
        public string deviceModel;
        public string deviceName;
        public string operatingSystem;
        public string processorType;
        public int processorCount;
        public string graphicsDeviceName;
        public string screenResolution;
        public float screenDPI;
    }

    [System.Serializable]
    public class MetricInfo
    {
        public string Name;
        public string Namespace;
        public string DimensionsInfo;
    }
}