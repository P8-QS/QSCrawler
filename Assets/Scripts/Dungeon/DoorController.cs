using System;
using UnityEngine;

namespace Dungeon
{
    public class DoorController : MonoBehaviour
    {
        [NonSerialized]
        public Room RoomA;
        
        [NonSerialized]
        public Room RoomB;
        
        public Sprite openSprite;
        public Sprite closedSprite;
        
        private bool _isOpen;
        private Collider2D _doorCollider;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _doorCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        public void Open()
        {
            if (_isOpen) return;

            _spriteRenderer.sprite = openSprite;
            _doorCollider.enabled = false;
            _isOpen = true;
        }

        public void Close()
        {
            if (!_isOpen) return;
            
            _spriteRenderer.sprite = closedSprite;
            _doorCollider.enabled = true;
            _isOpen = false;
        }

        public bool ConnectsTo(Room room) => room == RoomA || room == RoomB;
    }
}