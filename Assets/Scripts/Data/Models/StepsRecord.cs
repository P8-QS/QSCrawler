using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Data.Models
{
    [Serializable]
    public class StepsRecord
    {
        public DateTime StartTime { get; set; }
        public string StartZoneOffset { get; set; }
        public DateTime EndTime { get; set; }
        public string EndZoneOffset { get; set; }
        public int Count { get; set; }
        public Metadata Metadata { get; set; }
    }

    [Serializable]
    public class Metadata
    {
        public int ClientRecordVersion { get; set; }
        public DataOrigin DataOrigin { get; set; }
        public Device Device { get; set; }
        public string Id { get; set; }
        public Object LastModifiedTime { get; set; }
        public int RecordingMethod { get; set; }
    }
    
    [Serializable]
    public class DataOrigin
    {
        public string PackageName { get; set; }
    }
    
    [Serializable]
    public class Device
    {
        public int Type { get; set; }
    }
}