using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Threading.Tasks;
using CodeFull.Graphics.Geometry;
using System.Diagnostics;
using CodeFull.Extensions;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents a mesh built using vertices and triangle indices that can be
    /// rendered in OpenGL, manipulated in C# and be the subject of CSG operations
    /// in Carve
    /// </summary>
    public class Mesh : Drawable
    {
        /// <summary>
        /// Internal ID counter for meshes
        /// </summary>
        private static int idGen = -1;

        /// <summary>
        /// The vertices of this mesh
        /// </summary>
        private Vector3d[] vertices;

        /// <summary>
        /// Gets the array of vertices of this mesh
        /// </summary>
        public Vector3d[] Vertices 
        {
            get { return this.vertices; }
        }

        /// <summary>
        /// The color array of this mesh
        /// </summary>
        private uint[] colors;

        /// <summary>
        /// Gets the array of colors of this mesh
        /// </summary>
        public uint[] Colors
        {
            get { return this.colors; }
        }

        /// <summary>
        /// Gets the value indicating whether this mesh has vertex colors
        /// </summary>
        public bool HasColor { get; private set; }

        /// <summary>
        /// A lookup table for mapping face ID to triangle vertices
        /// </summary>
        private Dictionary<uint, Triangle> revLookup = new Dictionary<uint, Triangle>();

        /// <summary>
        /// The triangle indices of this mesh
        /// </summary>
        private int[] triangleIndices;

        /// <summary>
        /// Gets the array of triangle indices of this mesh
        /// </summary>
        public int[] TriangleIndices 
        {
            get { return this.triangleIndices; }
        }

        /// <summary>
        /// The OpenGL handles
        /// </summary>
        private Vbo handle;

        /// <summary>
        /// An arbitrary ID string associated with this mesh
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets the collection of all the triangles of this Mesh.
        /// </summary>
        public TriangleCollection Triangles { get; protected set; }

        /// <summary>
        /// Calculates the array of vertices of this mesh after applying
        /// the transforms applied to this mesh.
        /// </summary>
        /// <returns>The transformed vertices of this mesh</returns>
        public Vector3d[] GetTransformedVertices()
        {
            Vector3d[] result = new Vector3d[this.vertices.Length];
            Matrix4d transform = Transform.Value;

            // Transform all vertices in parallel
            Parallel.For(0, this.vertices.Length, i =>
            {
                result[i] = Vector3d.Transform(this.vertices[i], transform);
            });

            return result;
        }

        ///// <summary>
        ///// Creates a list of the Triangles of this mesh.
        ///// </summary>
        //protected void CreateTriangleList()
        //{
        //    IList<Triangle> result = new List<Triangle>();

        //    for (int i = 0; i < this.triangleIndices.Length; i+=3)
        //        result.Add(new Triangle(this.vertices[this.triangleIndices[i]],
        //            this.vertices[this.triangleIndices[i + 1]],
        //            this.vertices[this.triangleIndices[i + 2]]));

        //    this.Triangles = result;
        //}

        /// <summary>
        /// Initializes a new mesh.
        /// </summary>
        /// <param name="vertices">The vertex coordinates of this mesh</param>
        /// <param name="triangleIndices">The face triangle indices of this mesh</param>
        /// <param name="colors">Vertex colors of this mesh</param>
        public Mesh(Vector3d[] vertices, int[] triangleIndices, uint[] colors = null)
        {
            this.vertices = vertices;
            this.triangleIndices = triangleIndices;
            this.Triangles = new TriangleCollection(vertices, triangleIndices);

            if (colors != null) 
            {
                this.colors = colors;
                this.HasColor = true;
            }
                
            else // If no color array is specified, fill it with gray!
            {
                this.HasColor = false;
                this.colors = new uint[vertices.Length];
                Color color = Color.Gray;
                uint grayCode = (uint)color.A << 24 | (uint)color.B << 16 | (uint)color.G << 8 | (uint)color.R;                

                for (int i = 0; i < this.colors.Length; i++)
                    this.colors[i] = grayCode;
            }

            Init();
            CalculateCenter();

            this.Attachments = new DrawableCollection(this);
            ComputeAABB();
            
            Mesh.idGen++;
            this.ID = "Mesh-" + idGen.ToString();
        }

        /// <summary>
        /// Computes the axis-aligned bounding box of this mesh.
        /// </summary>
        protected void ComputeAABB()
        {
            this.AABB = new AABB(this, Helpers.GetMinVector3d(this.vertices), Helpers.GetMaxVector3d(this.vertices));
        }

        /// <summary>
        /// Calculates the center point of this mesh
        /// </summary>
        protected override void CalculateCenter()
        {
            this.center = new Vector3d(0);

            foreach (var vertex in this.vertices)
                center += vertex;

            center /= this.vertices.Count();
        }

        /// <summary>
        /// Registers the handles VBO of this mesh with OpenGL and initializes the data.
        /// </summary>
        private void Init()
        {
            // To create a VBO:
            // 1) Generate the buffer handles for the vertex and element buffers.
            // 2) Bind the vertex buffer handle and upload your vertex data. Check that the buffer was uploaded correctly.
            // 3) Bind the element buffer handle and upload your element data. Check that the buffer was uploaded correctly.

            this.handle = new Vbo();
            int size;

            GL.GenBuffers(1, out handle.vertexId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, handle.vertexId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * BlittableValueType.StrideOf(vertices)), vertices, BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * BlittableValueType.StrideOf(vertices) != size)
                throw new ApplicationException("Vertex data not uploaded correctly");
            
            if (this.colors != null)
            {
                GL.GenBuffers(1, out handle.colorId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, handle.colorId);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(colors.Length * BlittableValueType.StrideOf(colors)), colors, BufferUsageHint.StaticDraw);
                GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
                if (colors.Length * BlittableValueType.StrideOf(colors) != size)
                    throw new ApplicationException("Color data not uploaded correctly"); 
            }

            GL.GenBuffers(1, out handle.faceId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handle.faceId);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(triangleIndices.Length * sizeof(int)), triangleIndices,
                          BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (triangleIndices.Length * sizeof(int) != size)
                throw new ApplicationException("Element data not uploaded correctly"); 

            handle.numElements = triangleIndices.Length;
        }

        /// <summary>
        /// Draws the mesh using OpenGL. The method must be called in a drawing context (after setting
        /// the view properties and performing clearing)
        /// </summary>
        public override void Draw()
        {
            // To draw a VBO:
            // 1) Ensure that the VertexArray client state is enabled.
            // 2) Bind the vertex and element buffer handles.
            // 3) Set up the data pointers (vertex, normal, color) according to your vertex format.
            // 4) Call DrawElements. (Note: the last parameter is an offset into the element buffer
            //    and will usually be IntPtr.Zero).

            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.VertexArray);

            // Handle the transforms applied to this object
            GL.PushMatrix();
            Matrix4d transform = this.Transform.Value;
            GL.MultMatrix(ref transform);

            // Bind the vertex array
            GL.BindBuffer(BufferTarget.ArrayBuffer, handle.vertexId);
            GL.VertexPointer(3, VertexPointerType.Double, BlittableValueType.StrideOf(vertices), IntPtr.Zero);

            if (this.colors != null)
            {
                // Bind the color array
                GL.BindBuffer(BufferTarget.ArrayBuffer, handle.colorId);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, BlittableValueType.StrideOf(colors), IntPtr.Zero);
            }

            // Bind the elements array
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handle.faceId);
            GL.DrawElements(PrimitiveType.Triangles, handle.numElements, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.PopMatrix();

            foreach (var attachment in this.Attachments)
                attachment.Draw();

            if (ShowAABB) 
                this.AABB.Draw();
        }

        /// <summary>
        /// Performs a ray casting hit test using the specified ray. This function does not
        /// take the ray into its own space! Thus make sure the ray is specified in the space of this
        /// object (with proper transforms applied to it).
        /// </summary>
        /// <param name="ray">The ray to perform hit test for</param>
        /// <returns>The result of the hit test if anything occurred. null otherwise.</returns>
        public override HitTestResult HitTest(Ray ray)
        {
            // First determine if the ray hits the AABB
            if (this.AABB.HitTest(ray) == null)
                return null;

            System.Collections.Concurrent.ConcurrentBag<MeshHitTestResult> hits = new System.Collections.Concurrent.ConcurrentBag<MeshHitTestResult>();

            Parallel.ForEach(Triangles, item =>
            {
                Vector3d? intersection = item.IntersectsWith(ray);

                if (intersection != null)
                {
                    hits.Add(new MeshHitTestResult(this, Transform.Transform(intersection.Value), item));
                }
            });

            double minDist = int.MaxValue;
            Vector3d bestHit = Vector3d.Zero;
            Triangle triHit = null;
            Vector3d cameraPoistion = Camera.MainCamera.Position;

            foreach (var item in hits)
            {
                if (item == null)
                    continue;

                double dist = (cameraPoistion - item.HitPoint).LengthSquared;
                if (dist < minDist)
                {
                    minDist = dist;
                    bestHit = Vector3d.Transform(item.HitPoint, Transform.Value.Inverted());
                    triHit = item.TriangleHit;
                }
            }

            // If nothing was hit
            if (triHit == null)
                return null;

            return new MeshHitTestResult(this, bestHit, triHit);
        }

        /// <summary>
        /// Saves this mesh into a PLY format
        /// </summary>
        /// <param name="path">The path to the file to save this mesh to</param>
        public void SaveMesh(string path)
        {
            var vertices = GetTransformedVertices();
            var faces = this.triangleIndices;

            StreamWriter file = new StreamWriter(path);

            file.WriteLine("ply");
            file.WriteLine("format ascii 1.0");
            file.WriteLine("comment CodeFullToolkit generated");
            file.WriteLine("element vertex " + vertices.Length);
            file.WriteLine("property float x");
            file.WriteLine("property float y");
            file.WriteLine("property float z");
            file.WriteLine("element face " + faces.Length / 3);
            file.WriteLine("property list uchar int vertex_indices");
            file.WriteLine("end_header");

            foreach (var v in vertices)
                file.WriteLine(v.X + " " + v.Y + " " + v.Z);

            for (int i = 0; i < faces.Length; i += 3)
                file.WriteLine("3 " + faces.ElementAt(i) + " " + faces.ElementAt(i + 1) + " " + faces.ElementAt(i + 2));

            file.Close();
        }

        /// <summary>
        /// Creates a mesh by parsing a PLY file
        /// </summary>
        /// <param name="path">The path to the PLY mesh file</param>
        /// <returns>A mesh corresponding to the information in the PLY file</returns>
        public static Mesh LoadFromPLYFile(String path)
        {
            //MeshObject result = new MeshObject();
            int numVertices = 0;
            int numFaces = 0;
            bool containsColor = false;
            Dictionary<string, int> colorMapping = new Dictionary<string, int>();
            int colorIndex = 0;

            // open scene.ply and convert its contents to an array of strings
            StreamReader sr = new StreamReader(path);

            // determine the number of vertices and faces and determine whether or not geometry will be colored
            string line = sr.ReadLine();

            while (!(line = sr.ReadLine()).Contains("end_header"))
            {
                string[] parsedLine = line.Split(null);

                if (line.Contains("element vertex"))
                {
                    numVertices = int.Parse(line.Split(' ')[2]);
                }

                if (line.Contains("element face"))
                {
                    numFaces = int.Parse(line.Split(' ')[2]);
                }

                if (line.StartsWith("property"))
                {
                    if (line.Contains("list"))
                        continue;

                    if (!containsColor && line.Contains("red") || line.Contains("green") || line.Contains("blue") || line.Contains("alpha"))
                        containsColor = true;

                    if (line.Contains("red"))
                        colorMapping["red"] = colorIndex++;
                    else if (line.Contains("green"))
                        colorMapping["green"] = colorIndex++;
                    else if (line.Contains("blue"))
                        colorMapping["blue"] = colorIndex++;
                    else if (line.Contains("alpha"))
                        colorMapping["alpha"] = colorIndex++;
                }
            }

            Vector3d[] vertices = new Vector3d[numVertices];
            uint[] colors = new uint[numVertices];
            int[] faces = new int[numFaces * 3];

            string[] lines = new string[numVertices];

            for (int i = 0; i < numVertices; i++)
                lines[i] = sr.ReadLine();

            Parallel.For(0, numVertices, i => {
                string thisLine = lines[i];

                string[] parsedLine = thisLine.Split(null);

                double x = double.Parse(parsedLine[0]);
                double y = double.Parse(parsedLine[1]);
                double z = double.Parse(parsedLine[2]);

                Color color = Color.Gray;

                if (containsColor)
                {
                    int a = colorMapping.ContainsKey("alpha") ? int.Parse(parsedLine[3 + colorMapping["alpha"]]) : 255;
                    int r = colorMapping.ContainsKey("red") ? int.Parse(parsedLine[3 + colorMapping["red"]]) : 128;
                    int g = colorMapping.ContainsKey("green") ? int.Parse(parsedLine[3 + colorMapping["green"]]) : 128;
                    int b = colorMapping.ContainsKey("blue") ? int.Parse(parsedLine[3 + colorMapping["blue"]]) : 128;

                    color = Color.FromArgb(a, r, g, b);
                }

                vertices[i] = new Vector3d(x, y, z);
                colors[i] = (uint)color.A << 24 | (uint)color.B << 16 | (uint)color.G << 8 | (uint)color.R;
            });

            lines = new string[numFaces];

            for (int i = 0; i < numFaces; i++)
                lines[i] = sr.ReadLine();

            Parallel.For(0, numFaces, i =>
            {
                string thisLine = lines[i];

                string[] parsedLine = thisLine.Split(null);

                int verticesPerFace = int.Parse(parsedLine[0]);

                // triangle
                if (verticesPerFace == 3)
                {
                    faces[3 * i] = int.Parse(parsedLine[1]);
                    faces[3 * i + 1] = int.Parse(parsedLine[2]);
                    faces[3 * i + 2] = int.Parse(parsedLine[3]);
                }
            });

            #region Old single threaded parsing
            /*
            // collect vertex data and face data
            int vIndex = 0;
            int fIndex = 0;
            for (int i = 0; i < (numVertices + numFaces); i++)
            {
                line = sr.ReadLine();
                if (line == null)
                    break;

                string[] parsedLine = line.Split(null);

                if (i < numVertices)
                {
                    double x = double.Parse(parsedLine[0]);
                    double y = double.Parse(parsedLine[1]);
                    double z = double.Parse(parsedLine[2]);

                    Color color = Color.Gray;

                    if (containsColor)
                    {
                        int a = colorMapping.ContainsKey("alpha") ? int.Parse(parsedLine[3 + colorMapping["alpha"]]) : 255;
                        int r = colorMapping.ContainsKey("red") ? int.Parse(parsedLine[3 + colorMapping["red"]]) : 128;
                        int g = colorMapping.ContainsKey("green") ? int.Parse(parsedLine[3 + colorMapping["green"]]) : 128;
                        int b = colorMapping.ContainsKey("blue") ? int.Parse(parsedLine[3 + colorMapping["blue"]]) : 128;

                        color = Color.FromArgb(a, r, g, b);
                    }

                    vertices[vIndex] = new Vector3d(x, y, z);
                    colors[vIndex++] = (uint)color.A << 24 | (uint)color.B << 16 | (uint)color.G << 8 | (uint)color.R;
                }
                else
                {
                    // determine the number of vertices in this face and assign them to a geometry as appropriate
                    int verticesPerFace = int.Parse(parsedLine[0]);

                    // triangle
                    if (verticesPerFace == 3)
                    {
                        faces[fIndex++] = int.Parse(parsedLine[1]);
                        faces[fIndex++] = int.Parse(parsedLine[2]);
                        faces[fIndex++] = int.Parse(parsedLine[3]);
                    }
                }
            }*/

            #endregion

            Mesh result = new Mesh(vertices, faces, containsColor ? colors : null);
            result.ID = path.Substring(path.LastIndexOf("\\") == -1 ? 0 : path.LastIndexOf("\\") + 1);
            return result;
        }
        
        public override string ToString()
        {
            return this.ID;
        }
    }
}