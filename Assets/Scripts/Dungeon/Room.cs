using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class Room : MonoBehaviour
    {
        [Serializable]
        public struct DoorInfo
        {
            public string direction;
            public Vector3 localPosition;
        }

        public Tilemap wallsTilemap;
        
        public List<DoorInfo> doorData = new();
        
        [Header("Enemy Settings")]
        [Tooltip("Tag used to identify enemies that need to be defeated")]
        public string enemyTag = nameof(Fighter);
        
        [HideInInspector]
        public List<DoorController> connectedDoors = new();
        
        [HideInInspector]
        public bool isPlayerInside;
        
        private readonly List<Transform> _doorTransforms = new();
        private readonly List<GameObject> _roomEnemies = new();
        private Bounds? _calculatedBounds;
        private bool _isCleared;


#if UNITY_EDITOR
        public void PopulateDoorDataFromChildren()
        {
            doorData.Clear();
            UnityEditor.Undo.RecordObject(this, "Populate Room Door Data"); // For Undo support
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Door_")) continue;
                
                var dir = GetDoorDirection(child);
                if (dir is null) continue;
                
                doorData.Add(new DoorInfo
                {
                    direction = dir,
                    localPosition = child.localPosition
                });
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Populated door data for {gameObject.name}", this);
        }
#endif
        
        private void Awake()
        {
            FindDoorTransforms();
            FindEnemiesInRoom();
        }
        
        private void Update()
        {
            if (_isCleared)
                return;

            if (isPlayerInside && _roomEnemies.Count > 0)
            {
                foreach (var door in connectedDoors)
                {
                    door.Close();
                }
            }
            
            // Iterate through enemies to see if any are still alive
            var allDefeated = true;
            for (var i = _roomEnemies.Count - 1; i >= 0; i--)
            {
                if (_roomEnemies[i])
                {
                    allDefeated = false;
                    break;
                }
                
                _roomEnemies.RemoveAt(i);
            }
            
            // If all enemies are defeated and the player is inside the room, open the doors
            if (allDefeated && isPlayerInside)
            {
                _isCleared = true;
                
                foreach (var door in connectedDoors)
                {
                    door.Open();
                }
            }
        }
        
        private void FindEnemiesInRoom()
        {
            _roomEnemies.Clear();
            GetOrCalculateRoomBounds();
            
            var taggedObjects = GameObject.FindGameObjectsWithTag(enemyTag);

            foreach (var obj in taggedObjects)
            {
                if (obj.name != "Player" && _calculatedBounds != null && _calculatedBounds.Value.Contains(obj.transform.position))
                {
                    _roomEnemies.Add(obj);
                }
            }
        }
        
        private void FindDoorTransforms()
        {
            _doorTransforms.Clear();

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Door_")) continue;
                
                var dir = GetDoorDirection(child);
                if (dir == null) continue;
                
                if (doorData.Any(data => data.direction == dir && Vector3.Distance(data.localPosition, child.localPosition) < 0.01f))
                {
                    _doorTransforms.Add(child);
                }
                else {
                    Debug.LogWarning($"Door transform {child.name} found but no matching entry in doorData list.", this);
                }
            }
        }

        public List<Transform> GetDoorTransforms()
        {
            if(_doorTransforms.Count == 0)
            {
                FindDoorTransforms();
            }
            return _doorTransforms;
        }

        public List<DoorInfo> GetDoorPrefabData()
        {
            return doorData;
        }

        public static string GetDoorDirection(Transform door)
        {
            if (door is null || !door.name.Contains("_")) return null;
            return door.name.Split('_')[1]; // Assumes "Door_Direction" format
        }
        
        public static string GetOppositeDirection(string direction)
        {
            return direction switch
            {
                "North" => "South",
                "South" => "North",
                "East" => "West",
                "West" => "East",
                _ => null
            };
        }
        
        public Bounds GetOrCalculateRoomBounds()
        {
            _calculatedBounds ??= CalculateRoomBoundsAt(transform.position);
            return _calculatedBounds.Value;
        }
        
        public Bounds CalculateRoomBoundsAt(Vector3 simulatedWorldPosition)
        {
            float minX = 0, minY = 0, maxX = 0, maxY = 0;

            foreach (var door in doorData)
            {
                switch (door.direction)
                {
                    case "North": maxY = door.localPosition.y; break;
                    case "South": minY = door.localPosition.y; break;
                    case "East":  maxX = door.localPosition.x; break;
                    case "West":  minX = door.localPosition.x; break;
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(new Vector2(minX, minY), new Vector2(maxX, maxY));
            bounds.center += simulatedWorldPosition;

            return bounds;
        }
    }
}