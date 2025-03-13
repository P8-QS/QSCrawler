using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;

namespace Managers
{
    public class HealthConnectManager : MonoBehaviour
    {
        private AndroidJavaObject _healthConnectClient;
        private AndroidJavaObject _healthConnectPlugin;

        private const string ProviderPackageName = "com.google.android.apps.healthdata";
        private static readonly string[] RequiredPermissions = new[]
        {
            "android.permission.health.READ_STEPS",
            "android.permission.health.READ_SLEEP"
        };

        private void Start()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.LogWarning("Not running on Android. Skipping Health Connect API initialization");
                return;
            }
            
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
            // TODO: Implement logic for when user does not have Health Connect installed.
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
                Permission.RequestUserPermissions(RequiredPermissions, callbacks);
            }

            var ldt = new AndroidJavaClass("java.time.LocalDateTime");
            var now = ldt.CallStatic<AndroidJavaObject>("now");
            var yesterday = now.Call<AndroidJavaObject>("minusWeeks", (long)4);
            var timeRangeFilterClass = new AndroidJavaClass("androidx.health.connect.client.time.TimeRangeFilter");
            var timeRangeFilter = timeRangeFilterClass.CallStatic<AndroidJavaObject>("between", yesterday, now);
            
            _healthConnectPlugin.Call("ReadStepsRecords", gameObject.name, "OnStepsRecords", timeRangeFilter);
        }

        private void OnStepsRecords(string response)
        {
            Debug.Log("Received Health Connect steps response from HealthConnectPlugin::::JSON:::::");
            Debug.Log(response);
        }

        private void OnPermissionsStatusReceived(string response)
        {
            Debug.Log("Received Health Connect permissions response from HealthConnectPlugin");
            Debug.LogWarning(response);
        }
        
        private static bool HasAllRequiredPermissions()
        {
            var authorizedPermissions = RequiredPermissions.Select(Permission.HasUserAuthorizedPermission);
            
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
            Debug.Log($"OnPermissionGranted: {permissionName}");

            if (permissionName == RequiredPermissions[0])
            {
                // GetStepsRecords();
            }
        }

        private int GetDayStepCount()
        {
            var ldt = new AndroidJavaClass("java.time.LocalDateTime");
            var now = ldt.CallStatic<AndroidJavaObject>("now");
            var yesterday = now.Call<AndroidJavaObject>("minusDays", (long)1);
            
            var stepsRecordClass = new AndroidJavaClass("androidx.health.connect.client.records.StepsRecord");
            
            var timeRangeFilterClass = new AndroidJavaClass("androidx.health.connect.client.time.TimeRangeFilter");
            var timeRangeFilter = timeRangeFilterClass.CallStatic<AndroidJavaObject>("between", yesterday, now);
            
            var dataOrigin = new AndroidJavaObject("androidx.health.connect.client.records.metadata.DataOrigin", "QSCrawler");
            
            Debug.Log("Created dataOrigin");
            
            var readRequestClass = new AndroidJavaClass("androidx.health.connect.client.request.ReadRecordsRequest");
            Debug.Log("Created request class");
            
            // TODO: FIIIIIIIIIIIIIIIIIIIIIIX
            object[] parameters = {stepsRecordClass, timeRangeFilter, dataOrigin, true, 100, "1"};

            Debug.Log($"stepsrecordclass print: {stepsRecordClass}");
            

            var readRequest = readRequestClass.Call<AndroidJavaObject>(
                "getConstructor",
                stepsRecordClass
                // parameters
            );
            // var readRequest = new AndroidJavaObject("androidx.health.connect.client.request.ReadRecordsRequest", parameters);
            Debug.Log("Created request class");

            
            // Debug.Log($"Read records request: {readRequest}");

            
            return 0;
        }

        private void RedirectToPlayStore()
        {
            throw new System.NotImplementedException();
        }
    }
}
