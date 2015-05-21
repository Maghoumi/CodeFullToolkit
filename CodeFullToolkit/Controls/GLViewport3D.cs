using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using CodeFull.Graphics;
using System.Windows.Forms;
using System.ComponentModel;

namespace CodeFull.Controls
{
    /// <summary>
    /// A viewport control is able to render and manipulate triangular meshes in OpenGL.
    /// This control tries to mimic the functionality of WPF's Viewport3D control.
    /// </summary>
    public class GLViewport3D : GLControl
    {
        /// <summary>
        /// The arcball instance that controls the transformations of the meshes
        /// inside this viewport
        /// </summary>
        protected Arcball arcball;

        /// <summary>
        /// The position of the camera in this viewport
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3 CameraPosition { get; set; }

        /// <summary>
        /// The point that the camera must look at
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3 CameraLookAt { get; set; }

        /// <summary>
        /// The up vector of the camera (default = (0, 1, 0))
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3 CameraUp { get; set; }

        /// <summary>
        /// Gets or sets the camera's field of view (default value = 45)
        /// </summary>
        public double FieldOfView { get; set; }

        /// <summary>
        /// Gets or sets the arcball sensitivity for manipulating meshes in this viewport
        /// </summary>
        public double ArcballSensitivity
        {
            get
            {
                return (this.arcball != null) ? this.arcball.Sensitivity : -1;
            }
            set
            {
                if (this.arcball != null)
                    this.arcball.Sensitivity = value;
            }
        }

        /// <summary>
        /// Camera's near clipping distance (default = 0.1)
        /// </summary>
        public double NearClipping { get; set; }

        /// <summary>
        /// Camera's far clipping distance (default = 64)
        /// </summary>
        public double FarClipping { get; set; }

        /// <summary>
        /// The clear color used as the background of this OpenGL control
        /// (Defaults to white)
        /// </summary>
        public Color ClearColor { get; set; }

        /// <summary>
        /// The meshes that this viewport will display
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<Mesh> Meshes { get; set; }

        /// <summary>
        /// The currently selected mesh of this viewport. This mesh will 
        /// be manipulated
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Mesh SelectedMesh { get; set; }

        /// <summary>
        /// Event raised when the selected mesh item has changed
        /// </summary>
        public event EventHandler SelectionChanged;

        public GLViewport3D()
            : base()
        {
            InitializeComponent();

            arcball = new Arcball(Width, Height, 0.01);
            this.ClearColor = Color.White;
            this.CameraPosition = new Vector3(0f, 0f, 5f);
            this.CameraLookAt = new Vector3(0f, 0f, 0f);
            this.CameraUp = new Vector3(0f, 1f, 0f);
            this.Meshes = new List<Mesh>();
            this.FieldOfView = 45;
            this.NearClipping = 0.1;
            this.FarClipping = 64;
            Application.Idle += Application_Idle;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GLViewport3D
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Name = "GLViewport3D";
            this.Resize += new System.EventHandler(this.GLViewport3D_Resize);
            this.ResumeLayout(false);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.GLViewport3D_Resize(this, EventArgs.Empty);
        }

        private void Render()
        {
            // Apply arcball transforms to the selected mesh
            var cursor = OpenTK.Input.Mouse.GetCursorState();
            Point cursorPos = PointToClient(new Point(cursor.X, cursor.Y));
            arcball.ApplyTransforms(cursorPos);

            GL.ClearColor(this.ClearColor);
            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //GL.Enable(EnableCap.ColorMaterial);
            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            //GL.ShadeModel(ShadingModel.Smooth);

            //GL.Light(LightName.Light1, LightParameter.Ambient, OpenTK.Graphics.Color4.Gray);
            //GL.Light(LightName.Light1, LightParameter.Diffuse, OpenTK.Graphics.Color4.White);
            //GL.Light(LightName.Light1, LightParameter.Position, (new Vector4(0f, 0f, 0f, 1f)));
            //GL.Enable(EnableCap.Light1);

            // Setup camera
            Matrix4 lookat = Matrix4.LookAt(CameraPosition, CameraLookAt, CameraUp);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat);

            foreach (var child in Meshes)
                child.Draw();

            SwapBuffers();
        }

        #region Even Handling
        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            Application.Idle -= Application_Idle;
        }

        void Application_Idle(object sender, EventArgs e)
        {
            // Continuously render if application is idle
            while (this.IsIdle)
                Render();
        }

        private void GLViewport3D_Resize(object sender, EventArgs e)
        {
            if (DesignMode)
                return;

            OpenTK.GLControl c = sender as OpenTK.GLControl;

            if (c.ClientSize.Height == 0)
                c.ClientSize = new System.Drawing.Size(c.ClientSize.Width, 1);

            // Reset OpenGL size properties
            GL.Viewport(0, 0, c.ClientSize.Width, c.ClientSize.Height);
            float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, (float)NearClipping, (float)FarClipping);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);

            // Readjust arcball instance
            arcball.SetBounds(Width, Height);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (SelectedMesh == null)
                return base.ProcessCmdKey(ref msg, keyData);

            if (keyData == Keys.D)
            {
                SelectedMesh.RotateBy(0, 0.1, 0);
            }
            if (keyData == Keys.A)
            {
                SelectedMesh.RotateBy(0, -0.1, 0);
            }
            if (keyData == Keys.W)
            {
                SelectedMesh.RotateBy(-0.1, 0, 0);
            }
            if (keyData == Keys.S)
            {
                SelectedMesh.RotateBy(0.1, 0, 0);
            }
            if (keyData == Keys.PageUp)
            {
                SelectedMesh.TranslateBy(0, 0, -0.1);
            }
            if (keyData == Keys.PageDown)
            {
                SelectedMesh.TranslateBy(0, 0, 0.1);
            }
            if (keyData == Keys.Add)
            {
                SelectedMesh.ScaleBy(0.1, 0.1, 0.1);
            }

            if (keyData == Keys.Subtract)
            {
                SelectedMesh.ScaleBy(-0.1, -0.1, -0.1);
            }

            if (keyData == Keys.Left)
            {
                SelectedMesh.TranslateBy(-0.1, 0, 0);
            }
            if (keyData == Keys.Right)
            {
                SelectedMesh.TranslateBy(0.1, 0, 0);
            }
            if (keyData == Keys.Up)
            {
                SelectedMesh.TranslateBy(0, 0.1, 0);
            }
            if (keyData == Keys.Down)
            {
                SelectedMesh.TranslateBy(0, -0.1, 0);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Perform picking using any mouse button
            Point p = GetOpenGLMouseCoordinates(e);
            double minDepth = int.MaxValue;

            foreach (var item in this.Meshes)
            {
                var hitResult = item.HitTest(p);
                if (hitResult.Count > 0)
                {
                    minDepth = hitResult.ZDistance;
                    arcball.Mesh = SelectedMesh = hitResult.Mesh;
                }
            }

            if (minDepth != int.MaxValue && null != this.SelectionChanged)
            {
                this.SelectionChanged(this, EventArgs.Empty);
            }

            // Change arcball's mouse button status
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                arcball.SetMouseButtonStatus(MouseButtons.Left, true);
                arcball.SetMousePosition(e.Location);
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                arcball.SetMousePosition(e.Location);
                arcball.SetMouseButtonStatus(MouseButtons.Middle, true);
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                arcball.SetMousePosition(e.Location);
                arcball.SetMouseButtonStatus(MouseButtons.Right, true);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                arcball.SetMouseButtonStatus(MouseButtons.Left, false);
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
                arcball.SetMouseButtonStatus(MouseButtons.Middle, false);
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                arcball.SetMouseButtonStatus(MouseButtons.Right, false);
        }

        #endregion

        /// <summary>
        /// Performs a hit test on all the children of this viewport and
        /// returns a set of hit points (mesh triangle coordinates)
        /// </summary>
        /// <param name="points">A collection of points to use in hit testing</param>
        /// <returns>A set of triangle coordinates that intersect with the ray</returns>
        public HashSet<Vector3d> HitTest(IEnumerable<Point> points)
        {
            HashSet<Vector3d> result = new HashSet<Vector3d>();

            foreach (var item in Meshes)
            {
                var hits = item.HitTest(points);

                foreach (var hit in hits)
                    result.Add(hit);
            }

            return result;
        }

        /// <summary>
        /// Performs a hit test on the specified child and returns the result
        /// </summary>
        /// <param name="points">A collection of points to use in hit testing</param>
        /// <param name="mesh">The mesh to perform hit test on</param>
        /// <returns>The hit test result</returns>
        public HitTestResult HitTest(IEnumerable<Point> points, Mesh mesh)
        {
            return mesh.HitTest(points);
        }

        /// <summary>
        /// Converts the mouse cursor location to OpenGL window coordinate system
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Point GetOpenGLMouseCoordinates(MouseEventArgs e)
        {
            return new Point(e.X, this.Height - e.Y);
        }
    }

    public class Arcball
    {
        private const float Epsilon = 1.0e-5f;

        /// <summary>
        /// The last set mouse cursor position
        /// </summary>
        private Point mousePosition;

        /// <summary>
        /// Start of the click vector (mapped to the sphere)
        /// </summary>
        private Vector3d clickStartVector;

        /// <summary>
        /// End of the click vector (mapped to the sphere)
        /// </summary>
        private Vector3d clickEndVector;

        /// <summary>
        /// Adjusted mouse bounds width
        /// </summary>
        private double adjustedWidth;

        /// <summary>
        /// Adjusted mouse bounds height
        /// </summary>
        private double adjustedHeight;

        /// <summary>
        /// The height of the OpenGL canvas
        /// </summary>
        private int height;

        /// <summary>
        /// A mapping of the mouse button to their pressed status
        /// </summary>
        private IDictionary<MouseButtons, bool> buttonMapping = new Dictionary<MouseButtons, bool>();

        /// <summary>
        /// The sensitivity of this arcball (default is 0.01)
        /// </summary>
        public double Sensitivity { get; set; }

        /// <summary>
        /// The mesh that this arcball instance performs on
        /// </summary>
        public Mesh Mesh { get; set; }

        /// <summary>
        /// Instantiates a new Arcball with the specified boundaries
        /// for the width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Arcball(int width, int height, double sensitivity = 0.01)
        {
            this.Sensitivity = sensitivity;
            this.Mesh = null;
            clickStartVector = new Vector3d();
            clickEndVector = new Vector3d();
            SetBounds(width, height);

            buttonMapping[MouseButtons.Left] = false;
            buttonMapping[MouseButtons.Middle] = false;
            buttonMapping[MouseButtons.Right] = false;
        }

        /// <summary>
        /// Maps the given point to the sphere and returns the resulting vector
        /// </summary>
        /// <param name="point">The point to map to sphere</param>
        /// <returns>The vector of the mapped point</returns>
        private Vector3d MapToSphere(Point point)
        {
            Vector3d result = new Vector3d();

            PointF tempPoint = new PointF(point.X, point.Y);

            //Adjust point coords and scale down to range of [-1 ... 1]
            tempPoint.X = (float)(tempPoint.X * this.adjustedWidth) - 1.0f;
            tempPoint.Y = (float)(1.0f - (tempPoint.Y * this.adjustedHeight));

            //Compute square of the length of the vector from this point to the center
            float length = (tempPoint.X * tempPoint.X) + (tempPoint.Y * tempPoint.Y);

            //If the point is mapped outside the sphere... (length > radius squared)
            if (length > 1.0f)
            {
                //Compute a normalizing factor (radius / sqrt(length))
                float norm = (float)(1.0 / Math.Sqrt(length));

                //Return the "normalized" vector, a point on the sphere
                result.X = tempPoint.X * norm;
                result.Y = tempPoint.Y * norm;
                result.Z = 0.0f;
            }
            //Else it's inside
            else
            {
                //Return a vector to a point mapped inside the sphere sqrt(radius squared - length)
                result.X = tempPoint.X;
                result.Y = tempPoint.Y;
                result.Z = (float)System.Math.Sqrt(1.0f - length);
            }

            return result;
        }

        /// <summary>
        /// Set the boundaries of the mouse click
        /// </summary>
        /// <param name="width">The width boundary</param>
        /// <param name="height">The height boundary</param>
        public void SetBounds(int width, int height)
        {
            //Set adjustment factor for width/height
            this.adjustedWidth = 1.0 / ((width - 1.0) * 0.5);
            this.adjustedHeight = 1.0 / ((height - 1.0) * 0.5);
            this.height = height;
        }

        /// <summary>
        /// Sets the pressed status of the specified mouse button
        /// </summary>
        /// <param name="button">The mouse button to set</param>
        /// <param name="isPressed">The pressed status of that button</param>
        public void SetMouseButtonStatus(MouseButtons button, bool isPressed)
        {
            this.buttonMapping[button] = isPressed;
        }

        /// <summary>
        /// Sets the start position of the mouse
        /// </summary>
        /// <param name="position"></param>
        public void SetMousePosition(Point position)
        {
            this.mousePosition = position;
            this.clickStartVector = MapToSphere(position);
        }

        /// <summary>
        /// Calculate the rotation for the current point
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        protected Quaterniond GetRotation(Point currentPoint)
        {
            Quaterniond result = Quaterniond.Identity; // Must be identity! Not zero!!

            //Map the point to the sphere
            this.clickEndVector = this.MapToSphere(currentPoint);

            //Return the quaternion equivalent to the rotation
            //Compute the vector perpendicular to the begin and end vectors
            Vector3d Perp = Vector3d.Cross(clickStartVector, clickEndVector);

            //Compute the length of the perpendicular vector
            if (Perp.Length > Epsilon)
            //if its non-zero
            {
                //We're ok, so return the perpendicular vector as the transform after all
                result.X = Perp.X;
                result.Y = Perp.Y;
                result.Z = Perp.Z;
                //In the quaternion values, w is cosine (theta / 2), where theta is the rotation angle
                result.W = Vector3d.Dot(clickStartVector, clickEndVector);
            }

            return result;
        }

        /// <summary>
        /// Applies all the transformations possible based on the current status of mouse buttons
        /// </summary>
        /// <param name="currentCursorPosition">The current position of the mouse cursor</param>
        public virtual void ApplyTransforms(Point currentCursorPosition)
        {
            if (this.Mesh == null)
                return;

            if (this.buttonMapping[MouseButtons.Left])
            {
                // Convert current and previous mouse positions to OpenGL window coordinates
                Point prevPosition = new Point(this.mousePosition.X, this.height - this.mousePosition.Y);
                Point currentPosition = new Point(currentCursorPosition.X, this.height - currentCursorPosition.Y);

                int deltaX = currentPosition.X - prevPosition.X;
                int deltaY = currentPosition.Y - prevPosition.Y;

                var keyboard = OpenTK.Input.Keyboard.GetState();

                if (keyboard.IsKeyDown(OpenTK.Input.Key.ControlLeft) || keyboard.IsKeyDown(OpenTK.Input.Key.ControlRight))
                {
                    this.Mesh.TranslateBy(0, 0, -deltaY * Sensitivity * 3);
                }
                else
                    this.Mesh.TranslateBy(deltaX * Sensitivity, deltaY * Sensitivity, 0);
            }

            if (this.buttonMapping[MouseButtons.Middle])
            {
                // Convert current and previous mouse positions to OpenGL window coordinates
                Point prevPosition = new Point(this.mousePosition.X, this.height - this.mousePosition.Y);
                Point currentPosition = new Point(currentCursorPosition.X, this.height - currentCursorPosition.Y);

                double scale = (currentPosition.X - prevPosition.X) * Sensitivity;
                this.Mesh.ScaleBy(scale, scale, scale);
            }

            if (this.buttonMapping[MouseButtons.Right])
            {
                Quaterniond newRot = GetRotation(currentCursorPosition);
                Matrix4d rot = Matrix4d.Rotate(newRot);
                Mesh.Rotation *= rot;
            }

            // Update the cursor position
            SetMousePosition(currentCursorPosition);
        }
    }
}
