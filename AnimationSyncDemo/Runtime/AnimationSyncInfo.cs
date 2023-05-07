namespace GBG.AnimationSyncDemo
{
    public readonly struct AnimationSyncInfo
    {
        /// <summary>
        /// Overall playback progress.
        /// </summary>
        public readonly double Position;

        /// <summary>
        /// Playback progress between synchronization markers.
        /// Only valid when both <see cref="PrevMarker"/> and <see cref="NextMarker"/> are not empty.
        /// </summary>
        public readonly double MarkedPosition;

        /// <summary>
        /// The name of the starting marker for the sync section.
        /// </summary>
        public readonly string PrevMarker;

        /// <summary>
        /// The name of the ending marker for the sync section.
        /// </summary>
        public readonly string NextMarker;


        public AnimationSyncInfo(double position, double markedPosition, string prevMarker, string nextMarker)
        {
            Position = position;
            MarkedPosition = markedPosition;
            PrevMarker = prevMarker;
            NextMarker = nextMarker;
        }

        public bool HasValidMarks()
        {
            return !string.IsNullOrEmpty(PrevMarker) && !string.IsNullOrEmpty(NextMarker);
        }

        public override string ToString()
        {
            if (HasValidMarks())
            {
                return $"Position = {Position * 100:F2}%, MarkedPosition = ({PrevMarker} -> {NextMarker} @ {MarkedPosition * 100:F2}%)";
            }

            return $"Position={Position * 100:F2}%, No valid marker";
        }
    }
}