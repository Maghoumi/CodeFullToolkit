using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents the result of a hit test performed on a drawable
    /// </summary>
    public class HitTestResult : IEnumerable<Vector3d>
    {
        /// <summary>
        /// The drawable that was hit by the ray
        /// </summary>
        public Drawable Drawable { get; set; }

        /// <summary>
        /// A set of triangle vertices that intersect the ray
        /// </summary>
        public HashSet<Vector3d> HitPoints { get; set; }

        /// <summary>
        /// The z-Depth of the hit
        /// </summary>
        public double ZDistance { get; set; }

        /// <summary>
        /// The number of the hits that occurred
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="drawable">The drawable that was hit</param>
        /// <param name="hitPoints">The hit points</param>
        /// <param name="zDistance">The depth of the hit</param>
        public HitTestResult(Drawable drawable, HashSet<Vector3d> hitPoints, double zDistance)
        {
            this.Drawable = drawable;
            this.HitPoints = hitPoints;
            this.ZDistance = zDistance;
            this.Count = this.HitPoints.Count;
        }

        /// <summary>
        /// The iterator used for foreach
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Vector3d> GetEnumerator()
        {
            return this.HitPoints.GetEnumerator();
        }

        /// <summary>
        /// The iterator used for foreach
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.HitPoints.GetEnumerator();
        }
    }
}
