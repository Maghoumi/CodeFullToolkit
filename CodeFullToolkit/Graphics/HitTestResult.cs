using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents the generalized result of a hit test performed on a Drawable
    /// </summary>
    public class HitTestResult
    {
        /// <summary>
        /// The drawable that was hit by the ray
        /// </summary>
        public Drawable Drawable { get; protected set; }

        /// <summary>
        /// A set of triangle vertices that intersect the ray
        /// </summary>
        public Vector3d HitPoint { get; protected set; }

        /// <summary>
        /// Instantiates a new HitTestResult instance on the specified Drawable with the 
        /// specified hit point.
        /// </summary>
        /// <param name="drawable">The hit Drawable.</param>
        /// <param name="hitPoint">The point of hit.</param>
        public HitTestResult(Drawable drawable, Vector3d hitPoint)
        {
            this.Drawable = drawable;
            this.HitPoint = hitPoint;
        }
    }
}
