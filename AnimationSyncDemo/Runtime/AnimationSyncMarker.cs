using System;
using UnityEngine;

namespace GBG.AnimationSyncDemo
{
    [Serializable]
    public class AnimationSyncMarker
    {
        public string Name = string.Empty;

        [Range(0f, 1f)]
        public float Time;


        public bool IsValid()
        {
            return Time >= 0f && Time <= 1f && !string.IsNullOrEmpty(Name);
        }

        public override string ToString()
        {
            return $"{Name}@{Time:F3}";
        }
    }
}