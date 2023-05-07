using System.Collections.Generic;
using UnityEngine;

namespace GBG.AnimationSyncDemo
{
    public static class AnimationSyncMarkerExtensions
    {
        #region Sync Leader

        /// <summary>
        /// Attempt to obtain animation synchronization leader data.
        /// </summary>
        /// <param name="asset">Animation sync marker asset.</param>
        /// <param name="normalizedTime">The current playback progress of the animation.</param>
        /// <param name="playbackSpeed">The current playback speed of the animation.</param>
        /// <param name="prevMarkerName">The starting marker of the current sync section.</param>
        /// <param name="nextMarkerName">The ending marker of the current sync section.</param>
        /// <param name="markedPosition">The current progress of the animation within the sync section.</param>
        /// <returns></returns>
        public static bool TryGetSyncLeaderInfo(this AnimationSyncMarkerAsset asset, double normalizedTime, double playbackSpeed,
            out double markedPosition, out string prevMarkerName, out string nextMarkerName)
        {
            asset.EnsureMarkersInAscendOrder();

            if (playbackSpeed >= 0)
            {
                return TryGetSyncLeaderInfoForward(asset.SyncMarkers, normalizedTime, out markedPosition, out prevMarkerName, out nextMarkerName);
            }

            return TryGetSyncLeaderInfoBackward(asset.SyncMarkers, normalizedTime, out markedPosition, out prevMarkerName, out nextMarkerName);
        }

        /// <summary>
        /// Attempt to obtain animation synchronization leader data for forward-playing animation.
        /// </summary>
        /// <param name="markers">Animation sync markers.</param>
        /// <param name="normalizedTime">The current playback progress of the animation.</param>
        /// <param name="prevMarkerName">The starting marker of the current sync section.</param>
        /// <param name="nextMarkerName">The ending marker of the current sync section.</param>
        /// <param name="markedPosition">The current progress of the animation within the sync section.</param>
        /// <returns></returns>
        private static bool TryGetSyncLeaderInfoForward(this IReadOnlyList<AnimationSyncMarker> markers, double normalizedTime,
            out double markedPosition, out string prevMarkerName, out string nextMarkerName)
        {
            var markerCount = markers.Count;
            if (markerCount == 0)
            {
                markedPosition = 0;
                prevMarkerName = null;
                nextMarkerName = null;
                return false;
            }

            // Wrap the value to the range of [0,1]
            normalizedTime = MathTool.Wrap01(normalizedTime);

            // Find markers
            // The current position is before the first marker
            if (normalizedTime < markers[0].Time)
            {
                var prevMarker = markers[markerCount - 1];
                var nextMarker = markers[0];
                prevMarkerName = prevMarker.Name;
                nextMarkerName = nextMarker.Name;
                markedPosition = (normalizedTime + 1 - prevMarker.Time) / (nextMarker.Time + 1 - prevMarker.Time);
                return true;
            }

            // The current position is after the last marker
            if (normalizedTime >= markers[markerCount - 1].Time)
            {
                var prevMarker = markers[markerCount - 1];
                var nextMarker = markers[0];
                prevMarkerName = prevMarker.Name;
                nextMarkerName = nextMarker.Name;
                markedPosition = (normalizedTime - prevMarker.Time) / (nextMarker.Time + 1 - prevMarker.Time);
                return true;
            }

            // The current position is between markers
            for (int i = 0; i < markerCount - 1; i++)
            {
                if (normalizedTime >= markers[i].Time &&
                    normalizedTime < markers[i + 1].Time)
                {
                    var prevMarker = markers[i];
                    var nextMarker = markers[i + 1];
                    prevMarkerName = prevMarker.Name;
                    nextMarkerName = nextMarker.Name;
                    markedPosition = (normalizedTime - prevMarker.Time) / (nextMarker.Time - prevMarker.Time);
                    return true;
                }
            }

            prevMarkerName = null;
            nextMarkerName = null;
            markedPosition = default;
            return false;
        }

        /// <summary>
        /// Attempt to obtain animation synchronization leader data for backward-playing animation.
        /// </summary>
        /// <param name="markers">Animation sync markers.</param>
        /// <param name="normalizedTime">The current playback progress of the animation.</param>
        /// <param name="prevMarkerName">The starting marker of the current sync section.</param>
        /// <param name="nextMarkerName">The ending marker of the current sync section.</param>
        /// <param name="markedPosition">The current progress of the animation within the sync section.</param>
        /// <returns></returns>
        private static bool TryGetSyncLeaderInfoBackward(this IReadOnlyList<AnimationSyncMarker> markers, double normalizedTime,
            out double markedPosition, out string prevMarkerName, out string nextMarkerName)
        {
            var markerCount = markers.Count;
            if (markerCount == 0)
            {
                markedPosition = 0;
                prevMarkerName = null;
                nextMarkerName = null;
                return false;
            }

            // Wrap the value to the range of [0,1]
            normalizedTime = MathTool.Wrap01(normalizedTime);

            // Find markers
            // The current position is before the first marker
            if (normalizedTime <= markers[0].Time)
            {
                var prevMarker = markers[0];
                var nextMarker = markers[markerCount - 1];
                prevMarkerName = prevMarker.Name;
                nextMarkerName = nextMarker.Name;
                markedPosition = (prevMarker.Time - normalizedTime) / (prevMarker.Time + 1 - nextMarker.Time);
                return true;
            }

            // The current position is after the last marker
            if (normalizedTime > markers[markerCount - 1].Time)
            {
                var prevMarker = markers[0];
                var nextMarker = markers[markerCount - 1];
                prevMarkerName = prevMarker.Name;
                nextMarkerName = nextMarker.Name;
                markedPosition = (prevMarker.Time + 1 - normalizedTime) / (prevMarker.Time + 1 - nextMarker.Time);
                return true;
            }

            // The current position is between markers
            for (int i = markerCount - 1; i > 0; i--)
            {
                if (normalizedTime <= markers[i].Time &&
                    normalizedTime > markers[i - 1].Time)
                {
                    var prevMarker = markers[i];
                    var nextMarker = markers[i - 1];
                    prevMarkerName = prevMarker.Name;
                    nextMarkerName = nextMarker.Name;
                    markedPosition = (prevMarker.Time - normalizedTime) / (prevMarker.Time - nextMarker.Time);
                    return true;
                }
            }

            prevMarkerName = null;
            nextMarkerName = null;
            markedPosition = default;
            return false;
        }

        #endregion


        #region Sync Follower

        /// <summary>
        /// Attempt to obtain synchronized animation progress.
        /// </summary>
        /// <param name="asset">Animation sync marker asset.</param>
        /// <param name="currentNormalizedTime">The current playback progress of the animation(before synchronization).</param>
        /// <param name="playbackSpeed">The current playback speed of the animation.</param>
        /// <param name="isLooping">The animation is set to loop.</param>
        /// <param name="leaderInfo">Animation sync leader data.</param>
        /// <returns>The synchronized animation playback progress.</returns>
        public static double GetSyncFollowerPosition(this AnimationSyncMarkerAsset asset, double currentNormalizedTime,
            double playbackSpeed, bool isLooping, in AnimationSyncInfo leaderInfo)
        {
            // The animation has been paused, do not synchronize
            if (playbackSpeed > -MathTool.Epsilon && playbackSpeed < MathTool.Epsilon)
            {
                return currentNormalizedTime;
            }

            asset.EnsureMarkersInAscendOrder();

            if (playbackSpeed < 0)
            {
                return GetSyncFollowerPositionBackward(asset.SyncMarkers, currentNormalizedTime, isLooping, leaderInfo);
            }

            return GetSyncFollowerPositionForward(asset.SyncMarkers, currentNormalizedTime, isLooping, leaderInfo);
        }

        /// <summary>
        /// Attempt to obtain the target synchronized progress of the forward-playing animation.
        /// </summary>
        /// <param name="markers">Animation sync markers.</param>
        /// <param name="currentNormalizedTime">The current playback progress of the animation(before synchronization).</param>
        /// <param name="isLooping">The animation is set to loop.</param>
        /// <param name="leaderInfo">Animation sync leader data.</param>
        /// <returns>The synchronized animation playback progress.</returns>
        private static double GetSyncFollowerPositionForward(this IReadOnlyList<AnimationSyncMarker> markers,
            double currentNormalizedTime, bool isLooping, in AnimationSyncInfo leaderInfo)
        {
            // If there are no synchronization markers for the self or the leader, synchronize directly based on the overall progress
            if (markers.Count == 0 || !leaderInfo.HasValidMarks())
            {
                return leaderInfo.Position;
            }

            // Wrap the value to the range of [0,1]
            var wrappedNormalizedTime = MathTool.Wrap01(currentNormalizedTime);
            var wrappedTargetPosition = MathTool.Wrap01(leaderInfo.Position);

            // Find the sync section where the animation is currently located,
            // and start searching for the synchronization position from that section
            var searchStartIndex = markers.Count - 1;
            for (int i = 0; i < markers.Count - 1; i++)
            {
                if (markers[i].Time <= wrappedNormalizedTime && wrappedNormalizedTime < markers[i + 1].Time)
                {
                    searchStartIndex = i;
                    break;
                }
            }

            var isOversteppingTest = false;
            var syncedPosition = 0.0;
            while (true)
            {
                var prevMarkerIndex = -1;
                var nextMarkerIndex = -1;

                // Starting from searchStartIndex, search for prevMarker and nextMarker towards the end of the list
                for (int i = searchStartIndex; i < markers.Count; i++)
                {
                    if (markers[i].Name.Equals(leaderInfo.PrevMarker))
                    {
                        prevMarkerIndex = i;
                        continue;
                    }

                    if (prevMarkerIndex == -1)
                    {
                        continue;
                    }

                    if (markers[i].Name.Equals(leaderInfo.NextMarker))
                    {
                        nextMarkerIndex = i;
                        break;
                    }
                }

                // prevMarker was not found
                if (prevMarkerIndex == -1)
                {
                    // Search for prevMarker from the start of the list towards searchStartIndex position
                    for (int i = 0; i < searchStartIndex; i++)
                    {
                        if (markers[i].Name.Equals(leaderInfo.PrevMarker))
                        {
                            prevMarkerIndex = i;
                            continue;
                        }

                        if (prevMarkerIndex == -1)
                        {
                            continue;
                        }

                        if (markers[i].Name.Equals(leaderInfo.NextMarker))
                        {
                            nextMarkerIndex = i;
                            break;
                        }
                    }

                    // If prevMarker still cannot be found, then fallback to synchronizing based on the overall progress
                    if (prevMarkerIndex == -1)
                    {
                        if (!isOversteppingTest)
                        {
                            syncedPosition = MathTool.GetNearestDirectionalPosition(currentNormalizedTime, wrappedTargetPosition, false);
                        }

                        break;
                    }
                }

                // nextMarker was not found
                if (nextMarkerIndex == -1)
                {
                    // Starting from the prevMarkerIndex+1 position, search for nextMarker towards the end of the list
                    for (int i = prevMarkerIndex + 1; i < markers.Count; i++)
                    {
                        // There is no need to check prevMarkerIndex again, as the preceding code already covers the various cases for prevMarker
                        if (markers[i].Name.Equals(leaderInfo.NextMarker))
                        {
                            nextMarkerIndex = i;
                            break;
                        }
                    }

                    // If nextMarker still cannot be found, search for nextMarker from the start of the list towards the prevMarkerIndex,
                    // allowing for prevMarkerIndex to be found because the two markers may be the same (there is only one marker
                    // with a matching name in the entire animation)
                    if (nextMarkerIndex == -1)
                    {
                        for (int i = 0; i <= prevMarkerIndex; i++)
                        {
                            // There is no need to check prevMarkerIndex again, as the preceding code already covers the various cases for prevMarker
                            if (markers[i].Name.Equals(leaderInfo.NextMarker))
                            {
                                nextMarkerIndex = i;
                                break;
                            }
                        }
                    }

                    // If nextMarker still cannot be found, then fallback to synchronizing based on the overall progress
                    if (nextMarkerIndex == -1)
                    {
                        if (!isOversteppingTest)
                        {
                            syncedPosition = MathTool.GetNearestDirectionalPosition(currentNormalizedTime, wrappedTargetPosition, false);
                        }

                        break;
                    }
                }

                var prevMarker = markers[prevMarkerIndex];
                var nextMarker = markers[nextMarkerIndex];
                var nextMarkerPosition = prevMarkerIndex < nextMarkerIndex
                    ? nextMarker.Time
                    : nextMarker.Time + 1; // If there is a crossing of one cycle, add the missing progress for easier calculation
                var targetPosition = prevMarker.Time + (nextMarkerPosition - prevMarker.Time) * leaderInfo.MarkedPosition;
                wrappedTargetPosition = MathTool.Wrap01(targetPosition);
                syncedPosition = MathTool.GetNearestDirectionalPosition(currentNormalizedTime, wrappedTargetPosition, false);

                // If the current playback position happens to be within a sync section that matches the name exactly,
                // it can cause the algorithm to stop checking for possible matching sections.
                // If the calculated marker position at this point is closer to the prevMarker than the current playback position,
                // GetNearestDirectionalPosition could cause the animation to step directly to the next cycle.
                // Therefore, an additional check is performed here to prevent skipping over any possible matching sections in the future.
                if (!isOversteppingTest && prevMarkerIndex == searchStartIndex && wrappedNormalizedTime - targetPosition > MathTool.Epsilon)
                {
                    searchStartIndex++;
                    if (searchStartIndex >= markers.Count)
                    {
                        searchStartIndex = 0;
                    }

                    isOversteppingTest = true;
                    continue;
                }

                break;
            }

            if (isLooping)
            {
                return syncedPosition;
            }

            return Mathf.Clamp((float)syncedPosition, (float)wrappedNormalizedTime, 1f);
        }

        /// <summary>
        /// Attempt to obtain the target synchronized progress of the backward-playing animation.
        /// </summary>
        /// <param name="markers">Animation sync markers.</param>
        /// <param name="currentNormalizedTime">The current playback progress of the animation(before synchronization).</param>
        /// <param name="isLooping">The animation is set to loop.</param>
        /// <param name="leaderInfo">Animation sync leader data.</param>
        /// <returns>The synchronized animation playback progress.</returns>
        private static double GetSyncFollowerPositionBackward(this IReadOnlyList<AnimationSyncMarker> markers,
            double currentNormalizedTime, bool isLooping, in AnimationSyncInfo leaderInfo)
        {
            // 自身或Leader没有同步标记，直接按照整体进度同步
            if (markers.Count == 0 || !leaderInfo.HasValidMarks())
            {
                return leaderInfo.Position;
            }

            // Wrap the value to the range of [0,1]f
            var wrappedNormalizedTime = MathTool.Wrap01(currentNormalizedTime);
            var wrappedTargetPosition = MathTool.Wrap01(leaderInfo.Position);

            // Find the sync section where the animation is currently located,
            // and start searching for the synchronization position from that section
            var searchStartIndex = 0;
            for (int i = markers.Count - 1; i > 0; i--)
            {
                if (markers[i].Time >= wrappedNormalizedTime && wrappedNormalizedTime > markers[i - 1].Time)
                {
                    searchStartIndex = i;
                    break;
                }
            }

            var isOversteppingTest = false;
            var syncedPosition = 0.0;
            while (true)
            {
                var prevMarkerIndex = -1;
                var nextMarkerIndex = -1;

                // Starting from searchStartIndex, search for prevMarker and nextMarker towards the start of the list
                for (int i = searchStartIndex; i >= 0; i--)
                {
                    if (markers[i].Name.Equals(leaderInfo.PrevMarker))
                    {
                        prevMarkerIndex = i;
                        continue;
                    }

                    if (prevMarkerIndex == -1)
                    {
                        continue;
                    }

                    if (markers[i].Name.Equals(leaderInfo.NextMarker))
                    {
                        nextMarkerIndex = i;
                        break;
                    }
                }

                // prevMarker was not found
                if (prevMarkerIndex == -1)
                {
                    // Search for prevMarker from the end of the list towards searchStartIndex position
                    for (int i = markers.Count - 1; i > searchStartIndex; i--)
                    {
                        if (markers[i].Name.Equals(leaderInfo.PrevMarker))
                        {
                            prevMarkerIndex = i;
                            continue;
                        }

                        if (prevMarkerIndex == -1)
                        {
                            continue;
                        }

                        if (markers[i].Name.Equals(leaderInfo.NextMarker))
                        {
                            nextMarkerIndex = i;
                            break;
                        }
                    }
                }

                // If prevMarker still cannot be found, then fallback to synchronizing based on the overall progress
                if (prevMarkerIndex == -1)
                {
                    if (!isOversteppingTest)
                    {
                        syncedPosition = MathTool.GetNearestDirectionalPosition(currentNormalizedTime, wrappedTargetPosition, true);
                    }

                    break;
                }

                // nextMarker was not found
                if (nextMarkerIndex == -1)
                {
                    // Starting from the prevMarkerIndex-1 position, search for nextMarker towards the start of the list
                    for (int i = prevMarkerIndex - 1; i >= 0; i--)
                    {
                        // There is no need to check prevMarkerIndex again, as the preceding code already covers the various cases for prevMarker
                        if (markers[i].Name.Equals(leaderInfo.NextMarker))
                        {
                            nextMarkerIndex = i;
                            break;
                        }
                    }
                }

                // If nextMarker still cannot be found, search for nextMarker from the end of the list towards the prevMarkerIndex,
                // allowing for prevMarkerIndex to be found because the two markers may be the same (there is only one marker
                // with a matching name in the entire animation)
                if (nextMarkerIndex == -1)
                {
                    for (int i = markers.Count - 1; i >= prevMarkerIndex; i--)
                    {
                        // There is no need to check prevMarkerIndex again, as the preceding code already covers the various cases for prevMarker
                        if (markers[i].Name.Equals(leaderInfo.NextMarker))
                        {
                            nextMarkerIndex = i;
                            break;
                        }
                    }
                }

                // If nextMarker still cannot be found, then fallback to synchronizing based on the overall progress
                if (nextMarkerIndex == -1)
                {
                    if (!isOversteppingTest)
                    {
                        syncedPosition = MathTool.GetNearestDirectionalPosition(currentNormalizedTime, wrappedTargetPosition, true);
                    }

                    break;
                }

                var prevMarker = markers[prevMarkerIndex];
                var nextMarker = markers[nextMarkerIndex];
                var nextMarkerPosition = prevMarkerIndex > nextMarkerIndex
                    ? nextMarker.Time
                    : nextMarker.Time - 1; // If there is a crossing of one cycle, add the missing progress for easier calculation
                var targetPosition = prevMarker.Time + (nextMarkerPosition - prevMarker.Time) * leaderInfo.MarkedPosition;
                wrappedTargetPosition = MathTool.Wrap01(targetPosition);
                syncedPosition = MathTool.GetNearestDirectionalPosition(currentNormalizedTime, wrappedTargetPosition, true);

                // If the current playback position happens to be within a sync section that matches the name exactly,
                // it can cause the algorithm to stop checking for possible matching sections.
                // If the calculated marker position at this point is closer to the prevMarker than the current playback position,
                // GetNearestDirectionalPosition could cause the animation to step directly to the next cycle.
                // Therefore, an additional check is performed here to prevent skipping over any possible matching sections in the future.
                if (!isOversteppingTest && prevMarkerIndex == searchStartIndex && targetPosition - wrappedNormalizedTime > MathTool.Epsilon)
                {
                    searchStartIndex--;
                    if (searchStartIndex < 0)
                    {
                        searchStartIndex = markers.Count - 1;
                    }

                    isOversteppingTest = true;
                    continue;
                }

                break;
            }

            if (isLooping)
            {
                return syncedPosition;
            }

            return Mathf.Clamp((float)syncedPosition, 0f, (float)wrappedNormalizedTime);
        }

        #endregion
    }
}