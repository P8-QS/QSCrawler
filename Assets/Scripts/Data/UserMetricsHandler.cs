using System;
using System.Collections.Generic;
using Data.Models;
using UnityEngine;

namespace Data
{
    public enum UserMetricsType
    {
        StepsRecords,
        SleepSession,
        TotalScreenTime
    }
    
    public class UserMetricsHandler : MonoBehaviour
    {
        public static UserMetricsHandler Instance { get; private set; }
        
        /// <summary>
        /// Contains the user's steps count records. Can be null, so better to subscribe to the event instead.
        /// </summary>
        public IEnumerable<StepsRecord> StepsRecords { get; private set; }
        
        public int SleepSession { get; private set; }
        public long TotalScreenTime { get; private set; }

        public event Action<IEnumerable<StepsRecord>> OnStepsRecordsUpdated;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"{nameof(UserMetricsHandler)} instance created");
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
                case UserMetricsType.StepsRecords:
                    if (data is IEnumerable<StepsRecord> stepsRecords)
                    {
                        StepsRecords = stepsRecords;
                        Debug.Log("Steps records have been updated");
                        OnStepsRecordsUpdated?.Invoke(StepsRecords);
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