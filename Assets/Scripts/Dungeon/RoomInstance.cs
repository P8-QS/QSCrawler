using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    public class RoomInstance
    {
        public readonly GameObject GameObject;
        public readonly Room RoomScript;
        public readonly List<Transform> AvailableDoors;

        public RoomInstance(GameObject go, Room room)
        {
            GameObject = go;
            RoomScript = room;
            AvailableDoors = new List<Transform>(room.GetDoorTransforms());
        }
    }
}