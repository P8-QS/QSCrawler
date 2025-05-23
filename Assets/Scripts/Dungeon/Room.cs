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

        [Header("Enemy Settings")] [Tooltip("Tag used to identify enemies that need to be defeated")]
        public string enemyTag = nameof(Fighter);

        [HideInInspector] public List<DoorController> connectedDoors = new();

        [HideInInspector] public bool isPlayerInside;

        public readonly List<GameObject> RoomEnemies = new();
        private readonly List<Transform> _doorTransforms = new();
        private Bounds? _calculatedBounds;
        public bool _isCleared;
        public bool DoorsAlwaysOpen;

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

        private void Start()
        {
            FindDoorTransforms();
            FindEnemiesInRoom();

            var trigger = GetComponentInChildren<RoomTrigger>();
            if (trigger != null)
            {
                trigger.OnPlayerEnterRoom += OnPlayerEnterRoom;
            }
        }

        private void Update()
        {
            if (_isCleared)
                return;

            if (!DoorsAlwaysOpen && isPlayerInside && RoomEnemies.Count > 0)
            {
                foreach (var door in connectedDoors)
                {
                    door.Close();
                }
            }

            // Iterate through enemies to see if any are still alive
            var allDefeated = true;
            for (var i = RoomEnemies.Count - 1; i >= 0; i--)
            {
                if (RoomEnemies[i])
                {
                    allDefeated = false;
                    break;
                }

                RoomEnemies.RemoveAt(i);
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

            if (DoorsAlwaysOpen)
            {
                foreach (var door in connectedDoors)
                {
                    door.Open();
                }
            }
        }

        public void FindEnemiesInRoom()
        {
            RoomEnemies.Clear();
            GetOrCalculateRoomBounds();

            var taggedObjects = GameObject.FindGameObjectsWithTag(enemyTag);

            foreach (var obj in taggedObjects)
            {
                if (obj.name != "Player" && _calculatedBounds != null &&
                    _calculatedBounds.Value.Contains(obj.transform.position))
                {
                    RoomEnemies.Add(obj);
                }
            }
        }

        public List<Transform> GetDoorTransforms()
        {
            if (_doorTransforms.Count == 0)
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

            if (doorData.Count == 4)
            {
                foreach (var door in doorData)
                {
                    switch (door.direction)
                    {
                        case "North": maxY = door.localPosition.y; break;
                        case "South": minY = door.localPosition.y; break;
                        case "East": maxX = door.localPosition.x; break;
                        case "West": minX = door.localPosition.x; break;
                    }
                }
            }
            else
            {
                var floorTM = GetComponentsInChildren<Tilemap>().FirstOrDefault(t => t.gameObject.name == "Floor");
                if (floorTM)
                {
                    var minLocal = floorTM.CellToLocal(floorTM.cellBounds.min);
                    var maxLocal = floorTM.CellToLocal(floorTM.cellBounds.max);

                    minX = minLocal.x;
                    minY = minLocal.y;
                    maxX = maxLocal.x;
                    maxY = maxLocal.y;
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(new Vector2(minX, minY), new Vector2(maxX, maxY));
            bounds.center += simulatedWorldPosition;

            return bounds;
        }

        private void OnPlayerEnterRoom()
        {
            isPlayerInside = true;
        }

        private void FindDoorTransforms()
        {
            _doorTransforms.Clear();

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Door_")) continue;

                var dir = GetDoorDirection(child);
                if (dir == null) continue;

                if (doorData.Any(data =>
                        data.direction == dir && Vector3.Distance(data.localPosition, child.localPosition) < 0.01f))
                {
                    _doorTransforms.Add(child);
                }
                else
                {
                    Debug.LogWarning($"Door transform {child.name} found but no matching entry in doorData list.",
                        this);
                }
            }
        }
    }
}