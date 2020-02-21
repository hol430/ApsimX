using System;

namespace DCAPST
{
    /// <summary>
    /// Extensions used by DCaPST
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts an angle from degrees to radians
        /// </summary>
        public static double ToRadians(this double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Converts an angle from radians to degrees
        /// </summary>
        public static double ToDegrees(this double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}
