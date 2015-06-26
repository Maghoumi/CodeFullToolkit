using CodeFull.Graphics.Geometry;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CodeFull.Extensions
{
    /// <summary>
    /// Provides various helper methods.
    /// </summary>
    public class Helpers
    {
        /// <summary>
        /// Jan, 1st, 1970 timestamp
        /// </summary>
        private static readonly DateTime Jan1st1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Computes the timestamp based on the number of milliseconds passed
        /// since 01/01/1970
        /// </summary>
        /// <returns>The number of milliseconds passed since 01/01/1970</returns>
        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        /// <summary>
        /// Unprojects the specified point on the screen with the specified depth
        /// to the 3D space.
        /// </summary>
        /// <param name="screenLocation">The point on the screen</param>
        /// <param name="depth">The depth value in the range [0, 1] (near to far)</param>
        /// <returns>The corresponding 3D point on the screen</returns>
        public static Vector3d UnProject(Point screenLocation, double depth)
        {
            int[] viewport = GetViewport();
            Vector4d pos = new Vector4d();

            // Map x and y from window coordinates, map to range -1 to 1 
            pos.X = (screenLocation.X - viewport[0]) / (double)viewport[2] * 2.0f - 1.0f;
            pos.Y = 1 - (screenLocation.Y - viewport[1]) / (double)viewport[3] * 2.0f;
            pos.Z = depth * 2.0f - 1.0f;
            pos.W = 1.0f;

            Vector4d pos2 = Vector4d.Transform(pos, Matrix4d.Invert(GetModelViewMatrix() * GetProjectionMatrix()));
            Vector3d pos_out = new Vector3d(pos2.X, pos2.Y, pos2.Z);

            return pos_out / pos2.W;
        }

        /// <summary>
        /// Constructs a Matrix4d matrix from the given array of doubles.
        /// </summary>
        /// <param name="array">The array of consecutive elements.</param>
        /// <returns>The corresponding Matrix4d instance.</returns>
        public static Matrix4d Matrix4dFromArray(double[] array)
        {
            return new Matrix4d(array[0], array[1], array[2], array[3], 
                                array[4], array[5], array[6], array[7], 
                                array[8], array[9], array[10], array[11], 
                                array[12], array[13], array[14], array[15]);
        }

        /// <summary>
        /// Obtains the OpenGL viewport array.
        /// </summary>
        /// <returns>The OpenGL viewport array.</returns>
        public static int[] GetViewport()
        {
            int[] viewport = new int[4];
            OpenTK.Graphics.OpenGL.GL.GetInteger(OpenTK.Graphics.OpenGL.GetPName.Viewport, viewport);

            return viewport;
        }

        /// <summary>
        /// Obtains the current OpenGL projection matrix.
        /// </summary>
        /// <returns>The curren projection matrix.</returns>
        public static Matrix4d GetProjectionMatrix()
        {
            double[] projectionArray = new double[16];
            OpenTK.Graphics.OpenGL.GL.GetDouble(OpenTK.Graphics.OpenGL.GetPName.ProjectionMatrix, projectionArray);

            return Matrix4dFromArray(projectionArray);
        }

        /// <summary>
        /// Obtains the current OpenGL model view matrix.
        /// </summary>
        /// <returns>The curren model view matrix.</returns>
        public static Matrix4d GetModelViewMatrix()
        {
            double[] modelViewArray = new double[16];
            OpenTK.Graphics.OpenGL.GL.GetDouble(OpenTK.Graphics.OpenGL.GetPName.ModelviewMatrix, modelViewArray);
            return Matrix4dFromArray(modelViewArray);
        }

        /// <summary>
        /// Converts the provided screen point to ray. The screen point should be
        /// in window coordinate system (origin at top left).
        /// </summary>
        /// <param name="screenPoint">The screen point</param>
        /// <returns>The corresponding ray of the screenpoint</returns>
        public static Ray ScreenPointToRay(Point screenPoint)
        {
            Vector3d near = Helpers.UnProject(screenPoint, 0);
            Vector3d far = Helpers.UnProject(screenPoint, 1);

            return new Ray(near, far);
        }

        /// <summary>
        /// Converts the specified mouse position from the window coordinate system to
        /// OpenGL window coordinate system (from origin at top left to origin at bottom left).
        /// </summary>
        /// <param name="mousePosition">The mouse position in window coordinate system.</param>
        /// <returns>The position in OpenGL window coordinate system.</returns>
        public static Point GetGLMouseCoordinates(Point mousePosition)
        {
            return new Point(mousePosition.X, GetViewport()[3] - mousePosition.Y);
        }

        /// <summary>
        /// Determines the depth of the point under the specified mouse cursor.
        /// </summary>
        /// <param name="mousePosition">The mouse position.</param>
        /// <returns>The depth value of the position.</returns>
        public static double GetDepth(Point mousePosition)
        {
            float result = 0;
            Point p = GetGLMouseCoordinates(mousePosition);
            GL.ReadPixels(p.X, p.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref result);

            return result;
        }

        /// <summary>
        /// Gets the value representing the minimum depth value of the current OpenGL setup.
        /// </summary>
        /// <returns>The minimum depth value of the depth buffer.</returns>
        public static double GetMinimumDepthValue()
        {
            float[] depthRange = new float[2];
            GL.GetFloat(GetPName.DepthRange, depthRange);
            return depthRange[0];
        }

        /// <summary>
        /// Gets the value representing the maximum depth value of the current OpenGL setup.
        /// </summary>
        /// <returns>The maximum depth value of the depth buffer.</returns>
        public static double GetMaximumDepthValue()
        {
            float[] depthRange = new float[2];
            GL.GetFloat(GetPName.DepthRange, depthRange);
            return depthRange[1];
        }

        /// <summary>
        /// Selects the vector component that corresponds to the specified index.
        /// </summary>
        /// <param name="i">The index</param>
        /// <param name="vector">The vector</param>
        /// <returns>X, Y or Z component if i=0, 1, 2 respectively.</returns>
        public static double VectorComponentSelector(int i, Vector3d vector)
        {
            switch (i)
            {
                case 0:
                    return vector.X;
                case 1:
                    return vector.Y;
                case 2:
                    return vector.Z;
                default:
                    return double.NaN;
            }
        }

        /// <summary>
        /// Determines the minimum Vector3d in a collection of vertices
        /// </summary>
        /// <param name="collection">The vertex collection</param>
        /// <returns>The minimum vector in the vertices</returns>
        public static Vector3d GetMinVector3d(IEnumerable<Vector3d> collection)
        {
            double minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;

            foreach (var item in collection)
            {
                if (item.X < minX)
                    minX = item.X;
                if (item.Y < minY)
                    minY = item.Y;
                if (item.Z < minZ)
                    minZ = item.Z;
            }

            return new Vector3d(minX, minY, minZ);
        }

        /// <summary>
        /// Determines the maximum Vector3d in a collection of vertices
        /// </summary>
        /// <param name="collection">The vertex collection</param>
        /// <returns>The maximum vector in the vertices</returns>
        public static Vector3d GetMaxVector3d(IEnumerable<Vector3d> collection)
        {
            double maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            foreach (var item in collection)
            {
                if (item.X > maxX)
                    maxX = item.X;
                if (item.Y > maxY)
                    maxY = item.Y;
                if (item.Z > maxZ)
                    maxZ = item.Z;
            }

            return new Vector3d(maxX, maxY, maxZ);
        }
    }
}

