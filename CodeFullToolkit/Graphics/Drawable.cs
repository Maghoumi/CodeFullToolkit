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
        /// Internally stores the centroid of this mesh
        /// </summary>
        protected Vector3d center;

        /// <summary>
        /// Gets the center point of this mesh
        /// </summary>
        public Vector3d Center
        {
            get { return this.center; }
        }

        /// <summary>
        /// The translation applied to this mesh
        /// </summary>
        protected Matrix4d translation = Matrix4d.Identity;

        /// <summary>
        /// Gets or sets the translation matrix applied to this mesh
        /// </summary>
        public Matrix4d Translation
        {
            get { return this.translation; }
            set { this.translation = value; }
        }

        /// <summary>
        /// The rotation applied to this mesh
        /// </summary>
        protected Matrix4d rotation = Matrix4d.Identity;

        /// <summary>
        /// Gets or sets the rotation matrix applied to this mesh
        /// </summary>
        public Matrix4d Rotation
        {
            get { return this.rotation; }

            set { this.rotation = value; }
        }

        /// <summary>
        /// The scale applied to this mesh
        /// </summary>
        protected Matrix4d scale = Matrix4d.Identity;

        /// <summary>
        /// Gets or sets the scale matrix applied to this mesh
        /// </summary>
        public Matrix4d Scale
        {
            get { return this.scale; }

            set { this.scale = value; }
        }

        /// <summary>
        /// Gets or sets the Matrix4d transform applied to this mesh.
        /// The projection part of the transform is ignored. The rotation
        /// and scaling applied to the mesh are interpreted as rotation and
        /// scaling around the centroid of the mesh.
        /// </summary>
        public Matrix4d Transform
        {
            get
            {
                Matrix4d transform = Matrix4d.Identity;
                // Apply center rotation and scaling
                transform *= Matrix4d.CreateTranslation(-Center);
                transform *= this.rotation;
                transform *= this.scale;
                transform *= Matrix4d.CreateTranslation(Center);
                // Apply translation
                transform *= this.translation;

                return transform;
            }

            set
            {
                this.translation = Matrix4d.CreateTranslation(value.ExtractTranslation());
                this.rotation = Matrix4d.Rotate(value.ExtractRotation());
                this.scale = Matrix4d.Scale(value.ExtractScale());
            }
        }

        /// <summary>
        /// Relatively transform this mesh by the specified transformation
        /// matrix
        /// </summary>
        /// <param name="transform">The transform to apply</param>
        public void TransformBy(Matrix4d transform)
        {
            this.translation *= Matrix4d.CreateTranslation(transform.ExtractTranslation());
            this.rotation *= Matrix4d.Rotate(transform.ExtractRotation());
            this.scale = Matrix4d.Scale(this.scale.ExtractScale() + transform.ExtractScale());
        }

        /// <summary>
        /// Sets the translation of this mesh to the specified offsets
        /// </summary>
        /// <param name="offsetX">The X offset</param>
        /// <param name="offsetY">The Y offset</param>
        /// <param name="offsetZ">The Z offset</param>
        public void SetTranslation(double offsetX, double offsetY, double offsetZ)
        {
            this.translation = Matrix4d.CreateTranslation(offsetX, offsetY, offsetZ);
        }

        /// <summary>
        /// Sets the rotation of this mesh to the specified angles
        /// </summary>
        /// <param name="angleX">The X angle</param>
        /// <param name="angleY">The Y angle</param>
        /// <param name="angleZ">The Z angle</param>
        public void SetRotation(double angleX, double angleY, double angleZ)
        {
            Matrix4d rotX = Matrix4d.CreateRotationX(angleX);
            Matrix4d rotY = Matrix4d.CreateRotationX(angleY);
            Matrix4d rotZ = Matrix4d.CreateRotationX(angleZ);

            this.rotation = rotX * rotY * rotZ;
        }

        /// <summary>
        /// Sets the scale of this mesh to the specified amounts
        /// </summary>
        /// <param name="scaleX">The X scale</param>
        /// <param name="scaleY">The Y scale</param>
        /// <param name="scaleZ">The Z scale</param>
        public void SetScale(double scaleX, double scaleY, double scaleZ)
        {
            this.scale = Matrix4d.Scale(scaleX, scaleY, scaleZ);
        }

        /// <summary>
        /// Relatively transform this mesh by the specified offsets
        /// </summary>
        /// <param name="offsetX">The X offset</param>
        /// <param name="offsetY">The Y offset</param>
        /// <param name="offsetZ">The Z offset</param>
        public void TranslateBy(double offsetX, double offsetY, double offsetZ)
        {
            this.translation *= Matrix4d.CreateTranslation(new Vector3d(offsetX, offsetY, offsetZ));
        }

        /// <summary>
        /// Relatively rotates this mesh by the specified angle
        /// </summary>
        /// <param name="angleX">The X angle</param>
        /// <param name="angleY">The Y angle</param>
        /// <param name="angleZ">The Z angle</param>
        public void RotateBy(double angleX, double angleY, double angleZ)
        {
            this.rotation *= Matrix4d.CreateRotationX(angleX);
            this.rotation *= Matrix4d.CreateRotationY(angleY);
            this.rotation *= Matrix4d.CreateRotationZ(angleZ);
        }

        /// <summary>
        /// Relatively scales this mesh by the specified amounts
        /// </summary>
        /// <param name="scaleX">The X scale</param>
        /// <param name="scaleY">The Y scale</param>
        /// <param name="scaleZ">The Z scale</param>
        public void ScaleBy(double scaleX, double scaleY, double scaleZ)
        {
            this.scale = Matrix4d.Scale(this.scale.ExtractScale() + new Vector3d(scaleX, scaleY, scaleZ));
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
