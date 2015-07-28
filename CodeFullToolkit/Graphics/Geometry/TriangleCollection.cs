using CodeFull.Graphics.Geometry;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeFull.Geometry
{
    /// <summary>
    /// Represents a collection of renderable triangles.
    /// </summary>
    public class TriangleCollection : IEnumerable<Triangle>
    {
        /// <summary>
        /// The list of triangles in this collection
        /// </summary>
        protected IList<Triangle> triangles = new List<Triangle>();

        /// <summary>
        /// Gets the vertices array of this collection.
        /// </summary>
        public Vector3d[] Vertices { get; protected set; }

        /// <summary>
        /// Gets the index array of this collection.
        /// </summary>
        public int[] Indices { get; protected set; }

        /// <summary>
        /// Gets the number of triangles in this collection.
        /// </summary>
        public int Count { get; protected set; }

        /// <summary>
        /// Gets the list of triangles in this collection in a blitted format.
        /// </summary>
        public double[] BlittedList { get; protected set; }

        /// <summary>
        /// Gets the triangle at the specified index.
        /// </summary>
        /// <param name="index">The index of the element to access</param>
        /// <returns>The triangle at the specified index.</returns>
        public Triangle this[int index]
        {
            get
            {
                if (index >= this.Count)
                    throw new ArgumentOutOfRangeException("Argument \"index\" is out of bounds.");

                return this.triangles[index];
            }
        }

        /// <summary>
        /// Initializes a new TriangleCollection object with the specified vertices array
        /// and indices array.
        /// </summary>
        /// <param name="vertices">The vertices array.</param>
        /// <param name="triangleIndices">The triangle indices array.</param>
        public TriangleCollection(Vector3d[] vertices, int[] triangleIndices)
        {
            this.Vertices = vertices;
            this.Indices = triangleIndices;

            PopulateList();
            Blit();
        }

        /// <summary>
        /// Populates the list of triangles in this collection.
        /// </summary>
        protected void PopulateList()
        {
            this.triangles = new List<Triangle>();

            for (int i = 0; i < this.Indices.Length; i += 3)
                this.triangles.Add(new Triangle(this.Vertices[this.Indices[i]],
                    this.Vertices[this.Indices[i + 1]],
                    this.Vertices[this.Indices[i + 2]]));
        }

        /// <summary>
        /// Blits the triangles into the BlittedList property.
        /// </summary>
        protected void Blit()
        {
            List<double> result = new List<double>();

            foreach (var item in this.triangles)
                result.AddRange(item.ToBlittableArray());

            this.BlittedList = result.ToArray();
        }

        public IEnumerator<Triangle> GetEnumerator()
        {
            return this.triangles.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.triangles.GetEnumerator();
        }
    }
}
