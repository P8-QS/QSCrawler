using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Data.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Android;

namespace Managers
{
    public static class HealthRecordType
    {
        public const string Steps = "STEPS";
        public const string SleepSession = "SLEEP_SESSION";
    }

    public static class RequiredPermissions
    {
        public const string ReadSteps = "android.permission.health.READ_STEPS";
        public const string ReadSleep = "android.permission.health.READ_SLEEP";

        public static readonly string[] All = { ReadSteps, ReadSleep };
    }
    
    public class HealthConnectManager : MonoBehaviour
    {
        private AndroidJavaObject _healthConnectPlugin;
        private AndroidJavaObject _endLdt;
        private AndroidJavaObject _startLdt;
        
        private void Start()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.LogWarning("Not running on Android. Skipping Health Connect API initialization");
                return;
            }
            
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-1);

            var ldt = new AndroidJavaClass("java.time.LocalDateTime");
            _endLdt = ldt.CallStatic<AndroidJavaObject>("of", endDate.Year, endDate.Month, endDate.Day, 0, 0, 0);
            _startLdt = ldt.CallStatic<AndroidJavaObject>("of", startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
            
            InitializeHealthConnectClient();
        }

        private void InitializeHealthConnectClient()
        {
            var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                _healthConnectPlugin = new AndroidJavaObject("org.p8qs.healthconnectplugin.UnityPlugin", unityActivity);
                
                _healthConnectPlugin.Call(
                    "CheckHealthConnectAvailability",
                    gameObject.name,
                    "OnHealthConnectUnavailable",
                    "OnHealthConnectUpdateRequired",
                    "OnHealthConnectAvailable"
                );
            }));
        }

        private void OnHealthConnectUnavailable(string response)
        {
            Debug.Log("Received Health Connect unavailable response from HealthConnectPlugin");
            RedirectToPlayStore();
        }
        
        private void OnHealthConnectUpdateRequired(string response)
        {
            Debug.Log("Received Health Connect update required response from HealthConnectPlugin");
            // TODO: Implement logic for requiring user to update Health Connect app.
        }
        
        private void OnHealthConnectAvailable(string response)
        {
            Debug.Log("Received Health Connect available response from HealthConnectPlugin");
            var hasPerms = HasAllRequiredPermissions();
            
            if (!hasPerms)
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += OnPermissionGranted;
                Permission.RequestUserPermissions(RequiredPermissions.All, callbacks);
            }

            // All required permissions are available from this point
            GetUserSteps();
        }
        
        private static bool HasAllRequiredPermissions()
        {
            var authorizedPermissions = RequiredPermissions.All.Select(Permission.HasUserAuthorizedPermission);
            
            if (authorizedPermissions.Any(permission => permission == false))
            {
                Debug.Log("All Health Connect permissions have not been granted. Requesting from user");
                return false;    
            }
            else
            {
                Debug.Log("All Health Connect permissions have been granted.");
                return true;
            }
        }

        private void OnPermissionGranted(string permissionName)
        {
            // This method is called for each permission that is granted by the user
            Debug.Log($"Granted permission: {permissionName}");

            switch (permissionName)
            {
                case RequiredPermissions.ReadSteps:
                    GetUserSteps();
                    break;
                case RequiredPermissions.ReadSleep:
                    break;
            }
        }
        
        private void GetUserSteps()
        {
            Debug.Log($"Getting user steps from: {_startLdt.Call<string>("toString")} to: {_endLdt.Call<string>("toString")}");
            
            var timeRangeFilterClass = new AndroidJavaClass("androidx.health.connect.client.time.TimeRangeFilter");
            var timeRangeFilter = timeRangeFilterClass.CallStatic<AndroidJavaObject>("between", _startLdt, _endLdt);
            
            _healthConnectPlugin.Call("ReadHealthRecords", timeRangeFilter, HealthRecordType.Steps, gameObject.name, "OnStepsRecordsReceived");
        }

        private void OnStepsRecordsReceived(string response)
        {
            Debug.Log("Received Health Connect steps response from HealthConnectPlugin::::JSON:::::");
            Debug.Log(response);

            var records = JsonConvert.DeserializeObject<IEnumerable<StepsRecord>>(response);
            UserMetricsHandler.Instance.SetData(UserMetricsType.StepsRecords, records);
        }
        
        private void RedirectToPlayStore()
        {
            // TODO: Implement logic for when user does not have Health Connect installed.
        }
    }
}
