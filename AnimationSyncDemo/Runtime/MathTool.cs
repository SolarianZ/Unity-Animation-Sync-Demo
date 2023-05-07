using System;

namespace GBG.AnimationSyncDemo
{
    public static class MathTool
    {
        // (1.0 > 1f - Mathf.Epsilon) is judged as false
        public const float Epsilon = 1E-5f;

        /// <summary>
        /// Wrap the value to the range of [0,1].
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Wrap01(double value)
        {
            if (value < 0)
            {
                var result = 1.0 + value % 1;
                // Prevent wrapping -0 to 1
                if (result == 1)
                {
                    result = 0;
                }

                return result;
            }

            if (value > 1)
            {
                var result = value % 1;
                // Prevent wrapping 1 to 0
                if (result == 0)
                {
                    result = 1;
                }

                return result;
            }

            return value;
        }

        /// <summary>
        /// Convert the current time to the target time in the specified direction.
        /// </summary>
        /// <param name="currentPosition">Current time.</param>
        /// <param name="wrappedTargetPosition">The target time wrapped to the range of [0,1].</param>
        /// <param name="backward">Whether to perform the conversion in reverse.</param>
        /// <returns></returns>
        public static double GetNearestDirectionalPosition(double currentPosition, double wrappedTargetPosition, bool backward)
        {
            if (wrappedTargetPosition < 0 || wrappedTargetPosition > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(wrappedTargetPosition),
                    "The target position should be wrapped into the range of [0,1].");
            }

            var wrappedCurrentPosition = Wrap01(currentPosition);

            if (backward)
            {
                if (wrappedTargetPosition <= wrappedCurrentPosition)
                {
                    return currentPosition + (wrappedTargetPosition - wrappedCurrentPosition);
                }

                return currentPosition - wrappedCurrentPosition - (1 - wrappedTargetPosition);
            }

            if (wrappedTargetPosition >= wrappedCurrentPosition)
            {
                return currentPosition + (wrappedTargetPosition - wrappedCurrentPosition);
            }

            return currentPosition + (1 - wrappedCurrentPosition) + wrappedTargetPosition;
        }
    }
}