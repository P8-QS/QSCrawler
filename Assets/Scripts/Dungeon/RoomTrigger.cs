using System;
using UnityEngine;

namespace Dungeon
{
    public class RoomTrigger : MonoBehaviour
    {
        private Room _room;
        private bool hasBeenTriggered = false;

        private void Awake()
        {
            _room = GetComponentInParent<Room>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasBeenTriggered) return;

            if (other.CompareTag("Player"))
            {
                var room = GetComponentInParent<RoomFogController>();
                if (room != null)
                {
                    room.RevealFog();
                    hasBeenTriggered = true;
                }
            
                if (_room.isPlayerInside) return;
                _room.isPlayerInside = true;
            }
        }
    }
}
