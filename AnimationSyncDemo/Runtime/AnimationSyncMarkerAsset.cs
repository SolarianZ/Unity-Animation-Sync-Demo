using System.Collections.Generic;
using UnityEngine;

namespace GBG.AnimationSyncDemo
{
    /// <summary>
    /// Animation sync marker asset.
    /// <para>
    /// Sync method:
    /// <list type="bullet">
    ///   <item>
    ///     If there are no markers in Leader, Follower synchronizes according to the overall playback progress of Leader;
    ///   </item>
    ///   <item>
    ///     If there are markers in Leader:
    ///     <list type="bullet">
    ///       <item>
    ///         If the Leader's marker does not exist in Follower, Follower synchronizes according to the overall playback progress of Leader;
    ///       </item>
    ///       <item>
    ///         If the Leader's marker exists in Follower, Follower synchronizes according to the progress between the markers in Leader.
    ///       </item>
    ///     </list>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Animation Sync Marker", fileName = "NewAnimationSyncMarkerAsset")]
    public class AnimationSyncMarkerAsset : ScriptableObject
    {
        [Tooltip("List of sync markers.\n" +
            "When the start and end poses of the AnimationClip are the same, " +
            "only add one marker either at the beginning or the end, " +
            "but not at both endpoints with the same marker name.")]
        [SerializeField]
        public List<AnimationSyncMarker> SyncMarkers = new();


#pragma warning disable CS0649
        private bool m_ordered;
#pragma warning restore CS0649

        public void EnsureMarkersInAscendOrder()
        {
            if (m_ordered)
            {
                return;
            }

            SyncMarkers.Sort((a, b) =>
            {
                if (a.Time < b.Time) return -1;
                if (a.Time > b.Time) return 1;
                return 0;
            });

#if !UNITY_EDITOR
            m_ordered = true;
#endif
        }
    }
}