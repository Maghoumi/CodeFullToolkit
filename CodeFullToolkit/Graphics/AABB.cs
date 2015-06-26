using CodeFull.Extensions;
using CodeFull.Graphics.Geometry;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents the axis-aligned bounding box of an object. This AABB
    /// rotates with the object but is axis-aligned in the object space.
    /// </summary>
    public class AABB : Drawable
    {
        /// <summary>
        /// The minimum point of this OBB
        /// </summary>
        public Vector3d MinPoint { get; set; }

        /// <summary>
        /// The maximum point of this OBB
        /// </summary>
        public Vector3d MaxPoint { get; set; }

        /// <summary>
        /// The half-lengths of each direction
        /// </summary>
        public Vector3d HalfPoints { get; set; }

        /// <summary>
        /// The vertices of this AABB. Winding order is clockwise
        /// starting from MinPoint to MinPoint, then move up, wind 
        /// clockwise again.
        /// </summary>
        protected IList<Vector3d> vertices = new List<Vector3d>();

        /// <summary>
        /// Initializes a new AABB instance with the specified Drawable owner and
        /// the minimum and maximum points.
        /// </summary>
        /// <param name="owner">The owner of this AABB.</param>
        /// <param name="minPoint">The min point of this AABB.</param>
        /// <param name="maxPoint">The max point of this AABB.</param>
        public AABB(Drawable owner, Vector3d minPoint, Vector3d maxPoint)
        {
            this.Parent = owner;
            this.MinPoint = minPoint;
            this.MaxPoint = maxPoint;
            ComputeVertices();
            this.AABB = this;
            this.Transform = owner.Transform;

            CalculateCenter();
            // Assign the half points
            this.HalfPoints = MaxPoint - Center;

            ComputeVertices();
        }

        /// <summary>
        /// Computes the other vertices of this bounding box.
        /// </summary>
        protected void ComputeVertices()
        {
            vertices.Clear();

            vertices.Add(MinPoint);
            vertices.Add(MinPoint + new Vector3d(2 * HalfPoints.X, 0, 0));
            vertices.Add(MaxPoint - new Vector3d(0, 2 * HalfPoints.Y, 0));
            vertices.Add(MinPoint + new Vector3d(0, 0, 2 * HalfPoints.Z));
            vertices.Add(MinPoint + new Vector3d(0, 2 * HalfPoints.Y, 0));
            vertices.Add(MaxPoint - new Vector3d(0, 0, 2 * HalfPoints.Z));
            vertices.Add(MaxPoint);
            vertices.Add(MaxPoint - new Vector3d(2 * HalfPoints.X, 0, 0));
        }

        protected override void CalculateCenter()
        {
            this.center = (MaxPoint + MinPoint) / 2;
        }

        public override void Draw()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.PushMatrix();
            Matrix4d transform = this.Transform.Value;
            GL.MultMatrix(ref transform);

            GL.LineWidth(1);

            GL.Color4(Color.Purple);

            GL.Enable(EnableCap.LineSmooth);
            GL.Begin(PrimitiveType.LineStrip);

            GL.Vertex3(vertices[0]);
            GL.Vertex3(vertices[1]);
            GL.Vertex3(vertices[2]);
            GL.Vertex3(vertices[3]);
            GL.Vertex3(vertices[0]);
            GL.Vertex3(vertices[4]);
            GL.Vertex3(vertices[5]);
            GL.Vertex3(vertices[6]);
            GL.Vertex3(vertices[7]);
            GL.Vertex3(vertices[4]);
            GL.Vertex3(vertices[5]);
            GL.Vertex3(vertices[1]);
            GL.Vertex3(vertices[2]);
            GL.Vertex3(vertices[6]);
            GL.Vertex3(vertices[7]);
            GL.Vertex3(vertices[3]);

            GL.End();

            GL.PopMatrix();
            GL.Enable(EnableCap.DepthTest);
        }

        /// <summary>
        /// Tests for whether the specified ray hits this AABB or not. The 
        /// ray WILL NOT be modified and will be used as is. Make sure to pass
        /// the ray that will be in this AABB's space.
        /// </summary>
        /// <param name="ray">The Ray to perform the hit test for.</param>
        /// <returns>The result of the hit test, or null if no hit occurs.</returns>
        public override HitTestResult HitTest(Ray ray)
        {
            double tMin = int.MinValue;
            double tMax = int.MaxValue;

            Vector3d p = this.Center - ray.Origin;

            for (int i = 0; i < 3; i++)
            {
                double e = Helpers.VectorComponentSelector(i, p);
                double f = 1.0 / Helpers.VectorComponentSelector(i, ray.Direction);
                double hi = Helpers.VectorComponentSelector(i, HalfPoints);

                if (Math.Abs(f) > double.Epsilon)
                {
                    double t1 = (e + hi) * f;
                    double t2 = (e - hi) * f;

                    if (t1 > t2)
                    {
                        var temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    if (t1 > tMin)
                        tMin = t1;

                    if (t2 < tMax)
                        tMax = t2;

                    if (tMin > tMax)
                        return null;

                    if (tMax < 0)
                        return null;
                }
                else if (-e - hi > 0 || -e + hi < 0)
                    return null;
            }

            double t = tMin > 0 ? tMin : tMax;
            Vector3d intersection = ray.GetPointOnRay(t);
            return new HitTestResult(this, Vector3d.Transform(intersection, this.Transform.Value.Inverted()));
        }
    }
}
