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

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents a mesh built using vertices and triangle indices that can be
    /// rendered in OpenGL, manipulated in C# and be the subject of CSG operations
    /// in Carve
    /// </summary>
    public class Mesh
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
        /// The ID of each triangle in terms of colors (hack for picking)
        /// </summary>
        private uint[] selectColors;

        /// <summary>
        /// A lookup table for mapping face ID to triangle vertices
        /// </summary>
        private Dictionary<uint, List<Vector3d>> revLookup = new Dictionary<uint, List<Vector3d>>();

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
        /// Internally stores the centroid of this mesh
        /// </summary>
        private Vector3d center;

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
        private Matrix4d translation = Matrix4d.Identity;

        /// <summary>
        /// Gets or sets the translation matrix applied to this mesh
        /// </summary>
        public Matrix4d Translation
        {
            get { return this.translation; }
            set { this.translation = value; }
        }

        /// <summary>
        /// An arbitrary ID string associated with this mesh
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The rotation applied to this mesh
        /// </summary>
        private Matrix4d rotation = Matrix4d.Identity;

        /// <summary>
        /// Gets or sets the rotation matrix applied to this mesh
        /// </summary>
        public Matrix4d Rotation
        {
            get { return this.rotation; }

            set{ this.rotation = value; }
        }

        /// <summary>
        /// The scale applied to this mesh
        /// </summary>
        private Matrix4d scale = Matrix4d.Identity;

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
        /// Calculates the array of vertices of this mesh after applying
        /// the transforms applied to this mesh.
        /// </summary>
        /// <returns>The transformed vertices of this mesh</returns>
        public Vector3d[] GetTransformedVertices()
        {
            Vector3d[] result = new Vector3d[this.vertices.Length];
            Matrix4d transform = Transform;

            // Transform all vertices in parallel
            Parallel.For(0, this.vertices.Length, i =>
            {
                result[i] = Vector3d.Transform(this.vertices[i], transform);
            });

            return result;
        }

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

            // Fill in color codes for selection;
            this.selectColors = new uint[vertices.Length];

            int triangleCounter = -1;

            for (int i = 0; i < vertices.Length; i++)
            {
                // Index
                if (i % 3 == 0)
                    triangleCounter++;

                if (!revLookup.ContainsKey((uint)triangleCounter))
                    revLookup[(uint)triangleCounter] = new List<Vector3d>();

                this.selectColors[i] = (uint)triangleCounter;
                revLookup[(uint)triangleCounter].Add(this.vertices[i]);
            }

            Init();
            CalculateCenter();
            
            Mesh.idGen++;
            this.ID = "Mesh-" + idGen.ToString();
        }

        /// <summary>
        /// Calculates the center point of this mesh
        /// </summary>
        private void CalculateCenter()
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

            if (this.selectColors != null)
            {
                GL.GenBuffers(1, out handle.selectColorId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, handle.selectColorId);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(selectColors.Length * BlittableValueType.StrideOf(selectColors)), selectColors, BufferUsageHint.StaticDraw);
                GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
                if (selectColors.Length * BlittableValueType.StrideOf(selectColors) != size)
                    throw new ApplicationException("Color data not uploaded correctly");

                GL.GenBuffers(1, out handle.faceId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, handle.faceId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(triangleIndices.Length * sizeof(int)), triangleIndices,
                              BufferUsageHint.StaticDraw);
                GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
                if (triangleIndices.Length * sizeof(int) != size)
                    throw new ApplicationException("Element data not uploaded correctly"); 
            }

            handle.numElements = triangleIndices.Length;
        }

        /// <summary>
        /// Draws the mesh using OpenGL. The method must be called in a drawing context (after setting
        /// the view properties and performing clearing)
        /// </summary>
        public virtual void Draw()
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
            Matrix4d transform = this.Transform;
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
        }

        /// <summary>
        /// Performs a ray casting hit test using the specified points. The points 
        /// must be specified in OpenGL window coordinate system. (Bottom left is the origin)
        /// </summary>
        /// <param name="hitPoints">The points to perform hittest for</param>
        /// <returns>A set containing the vertices that form the triangle that the intersects with the points</returns>
        public HitTestResult HitTest(IEnumerable<Point> hitPoints)
        {
            // Temporarily change the background color to white
            GL.ClearColor(Color.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.VertexArray);

            // Handle the transforms applied to this object
            GL.PushMatrix();
            Matrix4d transform = this.Transform;
            GL.MultMatrix(ref transform);

            // Bind the vertex array
            GL.BindBuffer(BufferTarget.ArrayBuffer, handle.vertexId);
            GL.VertexPointer(3, VertexPointerType.Double, BlittableValueType.StrideOf(vertices), IntPtr.Zero);


            if (this.selectColors != null)
            {
                // Bind the select color array
                GL.BindBuffer(BufferTarget.ArrayBuffer, handle.selectColorId);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, BlittableValueType.StrideOf(selectColors), IntPtr.Zero); 
            }

            // Bind the elements array
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handle.faceId);
            GL.DrawElements(PrimitiveType.Triangles, handle.numElements, DrawElementsType.UnsignedInt, IntPtr.Zero);

            HashSet<Vector3d> hits = new HashSet<Vector3d>();
            float dist = 0;

            uint selectedTriangle = new uint();

            foreach (var item in hitPoints)
            {
                GL.ReadPixels(item.X, item.Y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ref selectedTriangle);

                if (selectedTriangle != uint.MaxValue)
                {
                    foreach (var x in revLookup[selectedTriangle])
                        hits.Add(x);

                    float z = 0;
                    GL.ReadPixels(item.X, item.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref z);
                    dist += z;
                }
            }

            GL.PopMatrix();

            dist /= hits.Count;

            return new HitTestResult(this, hits, dist);
        }

        public HitTestResult HitTest(params Point[] p)
        {
            return this.HitTest(p.ToList());
        }

        /// <summary>
        /// Determines whether the specified screen point lies on the mesh. If so, the distance will
        /// be returned. If not, null will be returned.
        /// </summary>
        /// <param name="screenPoint">The point on the screen (in OpenGL window coordinates)</param>
        /// <returns>The dept of the hit if the point lies on the mesh, null otherwise</returns>
        public double? GetIntersectionDistance(Point screenPoint)
        {
            var hit = this.HitTest(new Point[] { screenPoint });

            if (hit.Count > 0)
                return hit.ZDistance;

            return null;
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

    /// <summary>
    /// Represents the result of a hit test performed on a mesh
    /// </summary>
    public class HitTestResult : IEnumerable<Vector3d>
    {
        /// <summary>
        /// The mesh that was hit by the ray
        /// </summary>
        public Mesh Mesh { get; set; }

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
        /// <param name="mesh">The mesh that was hit</param>
        /// <param name="hitPoints">The hit points</param>
        /// <param name="zDistance">The depth of the hit</param>
        public HitTestResult(Mesh mesh, HashSet<Vector3d> hitPoints, double zDistance)
        {
            this.Mesh = mesh;
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