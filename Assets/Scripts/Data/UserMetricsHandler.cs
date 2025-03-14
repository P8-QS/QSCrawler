using System;
using UnityEngine;

namespace Data
{
    public enum UserMetricsType
    {
        StepsCount,
        SleepSession,
        TotalScreenTime
    }
    
    public class UserMetricsHandler : MonoBehaviour
    {
        public static UserMetricsHandler Instance { get; private set; }
        
        public int StepsCount { get; private set; }
        public int SleepSession { get; private set; }
        public long TotalScreenTime { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetData<T>(UserMetricsType userMetricsType, T data)
        {
            switch (userMetricsType)
            {
                case UserMetricsType.StepsCount:
                    if (data is int stepsCount)
                    {
                        StepsCount = stepsCount;
                    }
                    break;
                case UserMetricsType.SleepSession:
                    break;
                case UserMetricsType.TotalScreenTime:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(userMetricsType), userMetricsType, null);
            }
        }
    }
}