using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CodeFull.Extensions
{
    /// <summary>
    /// Provides various utility extensions methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Linearly interpolates between the current color and the target color
        /// </summary>
        /// <param name="current">The current color</param>
        /// <param name="target">The target color to interpolate to</param>
        /// <param name="lambda">The interpolation value (must be between 0 and 1)</param>
        /// <returns>A color that is between the current and the target color</returns>
        public static Color Lerp(this Color current, Color target, double lambda)
        {
            return Color.FromArgb(
                (byte)(current.R + (target.R - current.R) * lambda),
                (byte)(current.G + (target.G - current.G) * lambda),
                (byte)(current.B + (target.B - current.B) * lambda));
        }

        /// <summary>
        /// Converts this Vector3d instance to a sequential array of doubles.
        /// Usefull for serializing for compute kernels.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>A double[] array containing X, Y and Z components sequentially.</returns>
        public static double[] ToBlittableArray(this Vector3d vector)
        {
            return new double[] {vector.X, vector.Y, vector.Z};
        }

        /// <summary>
        /// Computes the Euclidean distance between this vector and another vector
        /// </summary>
        /// <param name="a">This vector</param>
        /// <param name="b">The other vector</param>
        /// <returns>The Euclidean distance between the two vectors.</returns>
        public static double Distance(this Vector3d a, Vector3d b)
        {
            return (b - a).Length;
        }

        /// <summary>
        /// Computes the cross product of the current vector with another vector.
        /// </summary>
        /// <param name="a">The current vector</param>
        /// <param name="b">The other vector</param>
        /// <returns>A vector that is the result of the cross product.</returns>
        public static Vector3d Cross(this Vector3d a, Vector3d b)
        {
            return Vector3d.Cross(a, b);
        }

        /// <summary>
        /// Computes the dot product of the current vector with another vector.
        /// </summary>
        /// <param name="a">The current vector</param>
        /// <param name="b">The other vector</param>
        /// <returns>The result of the dot product between the two vectors.</returns>
        public static double Dot(this Vector3d a, Vector3d b)
        {
            return Vector3d.Dot(a, b);
        }
    }
}