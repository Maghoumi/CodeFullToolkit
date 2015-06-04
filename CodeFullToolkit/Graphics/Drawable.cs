using CodeFull.Graphics.Transform;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Defines the interface for objects that can be drawn using OpenGL
    /// </summary>
    public abstract class Drawable
    {
        /// <summary>
        /// Gets or sets the list of drawables attached to this CodeFull.Graphics.Drawable instance
        /// </summary>
        public IList<Drawable> Attachments { get; set; }

        /// <summary>
        /// Internally stores the centroid of this drawable
        /// </summary>
        protected Vector3d center;

        /// <summary>
        /// Gets the center point of this drawable
        /// </summary>
        public Vector3d Center
        {
            get { return this.center; }
        }

        /// <summary>
        /// Gets the center point of this drawable after performing all the transformations on
        /// </summary>
        public Vector3d TransformedCenter
        {
            get 
            {
                return this.Transform.Transform(this.Center);
            }
        }

        protected Transform3DGroup transform = new Transform3DGroup();

        public Transform3DGroup Transform
        {
            get { return this.transform; }
            set { this.transform = value; }
        }

        /// <summary>
        /// Draws the contents of this drawable using OpenGL
        /// </summary>
        public abstract void Draw();

        /// <summary>
        /// Performs a ray casting hit test using the specified points. The points 
        /// must be specified in OpenGL window coordinate system. (Bottom left is the origin)
        /// </summary>
        /// <param name="hitPoints">The points to perform hittest for</param>
        /// <returns>A set containing the results of the hit test</returns>
        public abstract HitTestResult HitTest(IEnumerable<Point> hitPoints);

        /// <summary>
        /// Performs a ray casting hit test using the specified points. The points 
        /// must be specified in OpenGL window coordinate system. (Bottom left is the origin)
        /// </summary>
        /// <param name="hitPoints">The points to perform hittest for</param>
        /// <returns>A set containing the results of the hit test</returns>
        public virtual HitTestResult HitTest(params Point[] hitPoints)
        {
            return this.HitTest(hitPoints.ToList());
        }
    }
}
