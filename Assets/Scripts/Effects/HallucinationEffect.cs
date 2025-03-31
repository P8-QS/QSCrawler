using System;
using UnityEngine;

namespace Effects {
    public class HallucinationEffect : IEffect
    {
        private int _level;
        private Sprite _icon;
        public string Name => "Hallucination";

        public int Level { get => _level; set => _level = value; }
        public Sprite Icon { get => _icon; }

        public HallucinationEffect(Sprite icon, int level) {
            _icon = icon;
            _level = level;
        }
        public string Text()
        {
            return $"<color=red>hallucination level {Level}</color>";
        }
        public string Description()
        {
            return Level switch
            {
                1 => "Each room will contain a phantom enemy.",
                2 => "Each room will contain two phantom enemies.",
                _ => throw new ArgumentOutOfRangeException(nameof(Level), "Must be level 1 or 2.")
            };
        }

        public void Apply()
        {
            Debug.Log("Apply hallucination effect not implemented");
        }
    }
}