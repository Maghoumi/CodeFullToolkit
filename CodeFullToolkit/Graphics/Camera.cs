using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents a camera in OpenGL
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// Gets or sets the main viewing camera of OpenGL
        /// </summary>
        public static Camera MainCamera { get; set; }

        /// <summary>
        /// Gets or sets the position of this camera in 3D space
        /// </summary>
        public Vector3d Position { get; set; }

        /// <summary>
        /// Gets or sets the point in 3D space that this camera is looking at
        /// </summary>
        public Vector3d LookAt { get; set; }

        /// <summary>
        /// Gets or sets the up direction of this camera
        /// </summary>
        public Vector3d Up { get; set; }

        /// <summary>
        /// Gets or sets the field of view of this camera (in degrees)
        /// </summary>
        public double FieldOfView { get; set; }

        /// <summary>
        /// Camera's near clipping distance
        /// </summary>
        public double NearClip { get; set; }

        /// <summary>
        /// Camera's far clipping distance
        /// </summary>
        public double FarClip { get; set; }

        /// <summary>
        /// Creates a new Camera instance.
        /// </summary>
        /// <param name="positon">The world position of the camera.</param>
        /// <param name="lookAt">The direction of the camera.</param>
        /// <param name="up">The up direction of the camera (usually (0, 1, 0)).</param>
        /// <param name="fieldOfView">The field of view of the camera in degrees.</param>
        /// <param name="nearClip">The near clipping distance of the camera.</param>
        /// <param name="farClip">The far clipping distance of the camera.</param>
        public Camera(Vector3d positon, Vector3d lookAt, Vector3d up, double fieldOfView, double nearClip, double farClip)
        {
            this.Position = positon;
            this.Up = up.Normalized();
            this.LookAt = lookAt;
            this.FieldOfView = fieldOfView;
            this.NearClip = nearClip;
            this.FarClip = farClip;

            if (MainCamera == null)
                MainCamera = this;
        }

        /// <summary>
        /// Creates a new Camera instance located on the origin of the world space, looking at (0, 0, 0) with
        /// the unit Y axis specified as the up direction, with 45 degrees field of view and 0.1 and 64
        /// specified as the near clipping and far clipping distances respectively.
        /// </summary>
        public Camera() : this(Vector3d.Zero, Vector3d.Zero, Vector3d.UnitY, 45, 0.1, 64) { }

        /// <summary>
        /// Creates the model view matrix based on the camera setup.
        /// </summary>
        /// <returns>The model view matrix.</returns>
        public Matrix4d CreateModelViewMatrix()
        {
            return Matrix4d.LookAt(Position, LookAt, Up);
        }

        /// <summary>
        /// Gets the distance of this camera to the specified point.
        /// </summary>
        /// <param name="point">The point to calculate the distance to.</param>
        /// <returns>The distance of this camera to the point.</returns>
        public double GetDistanceTo(Vector3d point)
        {
            return (this.Position - point).Length;
        }
    }
}
