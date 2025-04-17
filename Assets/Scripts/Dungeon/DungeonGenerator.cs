using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The grid component.")]
        public Grid grid;
        
        public TileBase wallRuleTile;
        public GameObject doorHorizontal;
        public GameObject doorVertical;
        
        [Tooltip("Optional. Will be populated at runtime.")]
        public Transform generatedRoomsParent;

        [Header("Room Prefabs")]
        public GameObject startRoomPrefab;
        public List<GameObject> roomPrefabs;

        [Header("Generation Settings")]
        public int maxRooms = 15;
        public int maxAttemptsPerDoor = 10;
        public float doorPositionMatchTolerance = 0.01f;

        [Header("Seeding")]
        public bool useRandomSeed = true;
        public int seed;
        public bool logSeedOnGeneration = true;
         
        private System.Random _seededRandom;
        private int _currentSeed;

        public readonly List<RoomInstance> PlacedRooms = new();
        
        public void GenerateDungeon()
        {
            ClearDungeon();

            if (grid is null || startRoomPrefab is null || roomPrefabs.Count == 0)
            {
                Debug.LogError("Dungeon Generator is not properly configured!");
                return;
            }

            InitializeSeed();

            if (startRoomPrefab.GetComponent<Room>()?.GetDoorPrefabData() is null)
            {
                Debug.LogError($"Start Room Prefab '{startRoomPrefab.name}' is missing Room script or GetDoorPrefabData() method is not working correctly. Ensure Room.cs is set up for Method 2.");
                return;
            }
            if (roomPrefabs.Any(p => p?.GetComponent<Room>()?.GetDoorPrefabData() is null))
            {
                Debug.LogError($"One or more Room Prefabs are null, missing the Room script, or GetDoorPrefabData() is not working correctly. Ensure Room.cs is set up for Method 2 and all prefabs are assigned.");
                return;
            }

            generatedRoomsParent ??= grid.transform;

            // Place starting room
            if (!TryPlaceRoom(startRoomPrefab, Vector3Int.zero))
            {
                Debug.LogError("Failed to place the starting room!");
                return;
            }

            GenerationLoop();
            FillDoorLocations();
            SetRoomTilemapOrder();
            Debug.Log($"Dungeon generation complete. Placed {PlacedRooms.Count} rooms.");
        }

        private void InitializeSeed()
         {
             if (useRandomSeed)
             {
                 _currentSeed = System.DateTime.Now.Millisecond;
                 seed = _currentSeed;
             }
             else
             {
                 _currentSeed = seed;
             }
             _seededRandom = new System.Random(_currentSeed);
             if (logSeedOnGeneration)
             {
                 Debug.Log($"Dungeon generation seed: {_currentSeed}");
             }
         }
 
         private int GetRandomRange(int min, int max)
         {
             return _seededRandom.Next(min, max);
         }
 
         private float GetRandomValue()
         {
             return (float)_seededRandom.NextDouble();
         }

        private void GenerationLoop()
        {
            var roomsToProcess = new Queue<RoomInstance>();
            roomsToProcess.Enqueue(PlacedRooms[0]);

            var roomsPlacedCount = 1;
            while (roomsToProcess.Count > 0 && roomsPlacedCount < maxRooms)
            {
                var currentRoomInstance = roomsToProcess.Dequeue();

                // Shuffle actual door Transforms for randomness before processing
                var shuffledDoorTransforms = currentRoomInstance.AvailableDoors.OrderBy(d => GetRandomValue()).ToList();
                var numberOfDoors = GetRandomRange(1, shuffledDoorTransforms.Count + 1);

                var doorsPopulatedCount = 0;
                foreach (var currentDoorTransform in shuffledDoorTransforms)
                {
                    if (doorsPopulatedCount >= numberOfDoors) continue;
                    if (!currentRoomInstance.AvailableDoors.Contains(currentDoorTransform)) continue;

                    if (roomsPlacedCount >= maxRooms) break;

                    var currentDoorDir = Room.GetDoorDirection(currentDoorTransform);
                    var requiredOppositeDir = Room.GetOppositeDirection(currentDoorDir);
                    if (requiredOppositeDir == null)
                    {
                        Debug.LogWarning($"Door {currentDoorTransform.name} on {currentRoomInstance.GameObject.name} has invalid direction format.", currentDoorTransform);
                        continue; // Invalid door name/format
                    }

                    for (var attempt = 0; attempt < maxAttemptsPerDoor; attempt++)
                    {
                        var nextRoomPrefab = roomPrefabs[GetRandomRange(0, roomPrefabs.Count)];
                        var nextRoomScript = nextRoomPrefab.GetComponent<Room>();

                        if (nextRoomScript is null)
                        {
                            Debug.LogError($"Prefab {nextRoomPrefab.name} is missing Room script! Skipping.", nextRoomPrefab);
                            continue;
                        }

                        var potentialNewDoorsData = nextRoomScript.GetDoorPrefabData()
                            .Where(doorInfo => doorInfo.direction == requiredOppositeDir)
                            .OrderBy(d => GetRandomValue()) // Shuffle potential matches
                            .ToList();

                        if (potentialNewDoorsData.Count == 0)
                        {
                            Debug.Log($"Prefab {nextRoomPrefab.name} has no door data for direction {requiredOppositeDir}");
                            continue;
                        }

                        var chosenDoorData = potentialNewDoorsData[0];
                        var currentDoorWorldPos = currentDoorTransform.position;
                        var newRoomDoorLocalPos = chosenDoorData.localPosition;

                        var targetNewRoomWorldPos = currentDoorWorldPos - newRoomDoorLocalPos;
                        var targetNewRoomGridPos = grid.WorldToCell(targetNewRoomWorldPos);

                        if (!TryPlaceRoom(nextRoomPrefab, targetNewRoomGridPos)) continue;

                        doorsPopulatedCount++;
                        roomsPlacedCount++;
                        var newRoomInstance = PlacedRooms[^1];
                        roomsToProcess.Enqueue(newRoomInstance);

                        RemoveConnectedDoors(newRoomInstance);
                        break;
                    }
                }
            }
        }

        public void ClearDungeon()
        {
            PlacedRooms.Clear();
            var parentToClear = generatedRoomsParent ?? grid.transform;

            for (var i = parentToClear.childCount - 1; i >= 0; i--)
            {
                var childGo = parentToClear.GetChild(i).gameObject;
                // if (childGo.GetComponent<Room>() is null) continue;
                if (Application.isPlaying)
                    Destroy(childGo);
                else
                    DestroyImmediate(childGo);
            }
            switch (Application.isPlaying)
            {
                case false when parentToClear.childCount == 0:
                    Debug.Log("Cleared previous dungeon (Editor).");
                    break;
                case false when parentToClear.childCount > 0:
                    Debug.LogWarning("Clear Dungeon finished, but some children remained (maybe not rooms?).");
                    break;
            }
        }

        private void RemoveConnectedDoors(RoomInstance newRoomInstance)
        {
            foreach (var placedRoom in PlacedRooms)
            {
                if (placedRoom == newRoomInstance) continue;

                var doorsToRemove = placedRoom.AvailableDoors
                    .Where(doorA => newRoomInstance.AvailableDoors
                        .Any(doorB => Vector3.Distance(doorA.position, doorB.position) < doorPositionMatchTolerance))
                    .Select(doorA => doorA.position)
                    .ToList();

                placedRoom.AvailableDoors.RemoveAll(door => doorsToRemove.Any(pos => Vector3.Distance(door.position, pos) < 0.1f));
                newRoomInstance.AvailableDoors.RemoveAll(door => doorsToRemove.Any(pos => Vector3.Distance(door.position, pos) < 0.1f));
            }
        }

        private void FillDoorLocations()
        {
            HashSet<Vector3> placedDoorPositions = new();
            
            foreach (var room in PlacedRooms)
            {
                foreach (var doorInfo in room.RoomScript.GetDoorPrefabData())
                {
                    var worldDoorPos = doorInfo.localPosition + room.GameObject.transform.position;
                    
                    if (placedDoorPositions.Any(p => Vector3.Distance(p, worldDoorPos) < doorPositionMatchTolerance)) continue;
                    
                    var isConnected = !room.AvailableDoors.Any(dt =>
                        Vector3.Distance(dt.localPosition, doorInfo.localPosition) < doorPositionMatchTolerance);
                    
                    if (isConnected)
                    {
                        var doorGo = (doorInfo.direction is "North" or "South") 
                            ? doorHorizontal 
                            : doorVertical;
                        
                        var doorInstance = Instantiate(doorGo, worldDoorPos, Quaternion.identity, grid.transform);
                        var doorController = doorInstance.GetComponent<DoorController>();
                        
                        doorController.RoomA = room.RoomScript;
                        doorController.RoomB = FindOtherRoomAtPosition(room, worldDoorPos);
                        
                        room.RoomScript.connectedDoors.Add(doorController);
                        doorController.RoomB?.connectedDoors.Add(doorController);
                        
                        placedDoorPositions.Add(worldDoorPos);
                    }
                    else
                    {
                        var cellDoorPos = room.RoomScript.wallsTilemap.WorldToCell(worldDoorPos);
                        var offset = doorInfo.direction is "North" or "South" ? Vector3Int.left : Vector3Int.down;
                        
                        room.RoomScript.wallsTilemap.SetTile(cellDoorPos, wallRuleTile);
                        room.RoomScript.wallsTilemap.SetTile(cellDoorPos + offset, wallRuleTile);
                    }
                }
            }
        }

        private Room FindOtherRoomAtPosition(RoomInstance currentRoom, Vector3 worldDoorPos)
        {
            return PlacedRooms.SingleOrDefault(otherRoom => 
                    otherRoom != currentRoom &&
                    otherRoom.RoomScript.GetDoorPrefabData()
                        .Select(door => otherRoom.GameObject.transform.position + door.localPosition)
                        .Any(pos => Vector3.Distance(pos, worldDoorPos) < doorPositionMatchTolerance))
                    ?.RoomScript;
        }

        private void SetRoomTilemapOrder()
        {
            var sortedRooms = PlacedRooms
                .OrderByDescending(r => r.GameObject.transform.position.y)
                .ToList();

            for (var i = 0; i < sortedRooms.Count; i++)
            {
                var baseOrder = i * 100;
                var renderers = sortedRooms[i].GameObject.GetComponentsInChildren<TilemapRenderer>();

                foreach (var tilemapRenderer in renderers)
                {
                    var layerOrder = SortingLayer.GetLayerValueFromID(tilemapRenderer.sortingLayerID);
                    tilemapRenderer.sortingOrder = baseOrder + layerOrder;
                }
            }
        }
        
        private bool TryPlaceRoom(GameObject roomPrefab, Vector3Int gridPosition)
        {
            var roomPrefabScript = roomPrefab.GetComponent<Room>();
            if (roomPrefabScript is null)
            {
                Debug.LogError($"Prefab {roomPrefab.name} is missing Room script!", roomPrefab);
                return false;
            }

            var targetWorldPos = grid.GetCellCenterWorld(gridPosition);
            var potentialBounds = roomPrefabScript.CalculateRoomBoundsAt(targetWorldPos);

            if (PlacedRooms.Any(existingRoom => BoundsOverlap(potentialBounds, existingRoom.RoomScript.GetOrCalculateRoomBounds())))
            {
                return false;
            }

            // No collisions detected, safe to instantiate
            var roomInstanceGo = Instantiate(roomPrefab, generatedRoomsParent, true);
            roomInstanceGo.name = $"{roomPrefab.name}_({potentialBounds.center.x},{potentialBounds.center.y})";
            roomInstanceGo.transform.position = targetWorldPos;
            
            var roomScript = roomInstanceGo.GetComponent<Room>();
            
            roomScript.GetOrCalculateRoomBounds();
            var newInstance = new RoomInstance(roomInstanceGo, roomScript);
            PlacedRooms.Add(newInstance);
            return true;
        }

        private static bool BoundsOverlap(Bounds a, Bounds b)
        {
            return !Mathf.Approximately(a.max.x, b.min.x) && a.max.x > b.min.x &&
                   !Mathf.Approximately(b.max.x, a.min.x) && b.max.x > a.min.x &&
                   !Mathf.Approximately(a.max.y, b.min.y) && a.max.y > b.min.y &&
                   !Mathf.Approximately(b.max.y, a.min.y) && b.max.y > a.min.y;
        }
    }
}