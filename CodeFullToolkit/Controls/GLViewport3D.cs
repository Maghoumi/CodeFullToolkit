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
    /// A viewport control is able to render and manipulate Drawable instances in OpenGL.
    /// This control tries to mimic the functionality of WPF's Viewport3D control.
    /// </summary>
    public class GLViewport3D : GLControl
    {
        /// <summary>
        /// The arcball instance that controls the transformations of the drawables
        /// inside this viewport
        /// </summary>
        protected Arcball arcball;

        /// <summary>
        /// The position of the camera in this viewport
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3d CameraPosition { get; set; }

        /// <summary>
        /// The point that the camera must look at
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3d CameraLookAt { get; set; }

        /// <summary>
        /// The up vector of the camera (default = (0, 1, 0))
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3d CameraUp { get; set; }

        /// <summary>
        /// Gets or sets the camera's field of view (default value = 45)
        /// </summary>
        public double FieldOfView { get; set; }

        /// <summary>
        /// Gets or sets the arcball sensitivity for manipulating drawables in this viewport
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
        /// The objects that this viewport will display
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<Drawable> Children { get; set; }

        /// <summary>
        /// The currently selected drawable of this viewport. This drawable will be manipulated
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Drawable SelectedDrawable { get; set; }

        /// <summary>
        /// Event raised when the selected drawable item has changed
        /// </summary>
        public event EventHandler SelectionChanged;

        public GLViewport3D()
            : base()
        {
            InitializeComponent();

            arcball = new Arcball(Width, Height, 0.01);
            this.ClearColor = Color.White;
            this.CameraPosition = new Vector3d(0, 0, 5);
            this.CameraLookAt = new Vector3d(0, 0, 0);
            this.CameraUp = new Vector3d(0, 1, 0);
            this.Children = new List<Drawable>();
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

        /// <summary>
        /// Gets the model view matrix based on the viewport setup
        /// </summary>
        /// <returns>The model view matrix</returns>
        protected Matrix4d GetModelViewMatrix()
        {
            return Matrix4d.LookAt(CameraPosition, CameraLookAt, CameraUp);
        }

        /// <summary>
        /// Gets the projection matrix based on the control setup
        /// </summary>
        /// <returns>The projection matrix</returns>
        protected Matrix4d GetProjectionMatrix()
        {
            float aspect_ratio = this.ClientSize.Width / (float)this.ClientSize.Height;
            return Matrix4d.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(this.FieldOfView), aspect_ratio, (float)NearClipping, (float)FarClipping);
        }

        private void Render()
        {
			if (DesignMode)
                return;
			
            // Apply arcball transforms to the selected drawable
            var cursor = OpenTK.Input.Mouse.GetCursorState();
            Point cursorPos = PointToClient(new Point(cursor.X, cursor.Y));
            arcball.ApplyTransforms(cursorPos);

            GL.ClearColor(this.ClearColor);
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.Lighting);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.ColorMaterial);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.ShadeModel(ShadingModel.Smooth);

            // Setup viewport and projection matrix
            GL.Viewport(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            Matrix4d perpective = GetProjectionMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);

            // Setup camera
            Matrix4d modelView = GetModelViewMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelView);

            

            foreach (var child in Children)
                child.Draw();

            //GL.PushMatrix();

            //GL.Enable(EnableCap.Light0);
            //GL.Light(LightName.Light0, LightParameter.Ambient, OpenTK.Graphics.Color4.Yellow);
            //GL.Light(LightName.Light0, LightParameter.Diffuse, OpenTK.Graphics.Color4.White);
            //GL.Light(LightName.Light0, LightParameter.Position, (new Vector4(10f, 10f, 10f, 1f)));

            //GL.Disable(EnableCap.Lighting);

            //GL.PopMatrix();

            SwapBuffers();
        }

        #region Even Handling

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render();
        }
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
            OpenTK.GLControl c = sender as OpenTK.GLControl;

            if (c.ClientSize.Height == 0)
                c.ClientSize = new System.Drawing.Size(c.ClientSize.Width, 1);

            if (c.ClientSize.Width == 0)
                c.ClientSize = new System.Drawing.Size(1, c.ClientSize.Height);

            // Readjust arcball instance
            arcball.SetBounds(Width, Height);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (SelectedDrawable == null)
                return base.ProcessCmdKey(ref msg, keyData);

            if (keyData == Keys.D)
            {
                SelectedDrawable.Transform.RotateBy(0, 0.1, 0, SelectedDrawable.Transform.Transform(SelectedDrawable.Center));
            }
            if (keyData == Keys.A)
            {
                SelectedDrawable.Transform.RotateBy(0, -0.1, 0, SelectedDrawable.Transform.Transform(SelectedDrawable.Center));
            }
            if (keyData == Keys.W)
            {
                SelectedDrawable.Transform.RotateBy(-0.1, 0, 0, SelectedDrawable.Transform.Transform(SelectedDrawable.Center));
            }
            if (keyData == Keys.S)
            {
                SelectedDrawable.Transform.RotateBy(0.1, 0, 0, SelectedDrawable.Transform.Transform(SelectedDrawable.Center));
            }
            if (keyData == Keys.PageUp)
            {
                SelectedDrawable.Transform.TranslateBy(0, 0, -0.1);
            }
            if (keyData == Keys.PageDown)
            {
                SelectedDrawable.Transform.TranslateBy(0, 0, 0.1);
            }
            if (keyData == Keys.Add)
            {
                SelectedDrawable.Transform.ScaleBy(0.1, 0.1, 0.1, SelectedDrawable.Transform.Transform(SelectedDrawable.Center));
            }

            if (keyData == Keys.Subtract)
            {
                SelectedDrawable.Transform.ScaleBy(-0.1, -0.1, -0.1, SelectedDrawable.Transform.Transform(SelectedDrawable.Center));
            }

            if (keyData == Keys.Left)
            {
                SelectedDrawable.Transform.TranslateBy(-0.1, 0, 0);
            }
            if (keyData == Keys.Right)
            {
                SelectedDrawable.Transform.TranslateBy(0.1, 0, 0);
            }
            if (keyData == Keys.Up)
            {
                SelectedDrawable.Transform.TranslateBy(0, 0.1, 0);
            }
            if (keyData == Keys.Down)
            {
                SelectedDrawable.Transform.TranslateBy(0, -0.1, 0);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Perform picking using any mouse button
            Point p = GetOpenGLMouseCoordinates(e);
            double minDepth = int.MaxValue;

            foreach (var item in this.Children)
            {
                var hitResult = item.HitTest(p);
                // If hit anything, selected drawable is the one closest to the camera
                if (hitResult.Count > 0 && hitResult.ZDistance < minDepth)
                {
                    minDepth = hitResult.ZDistance;
                    arcball.Drawable = SelectedDrawable = hitResult.Drawable;
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
        /// returns a set of hit points
        /// </summary>
        /// <param name="points">A collection of points to use in hit testing</param>
        /// <returns>A set of triangle coordinates that intersect with the ray</returns>
        public HashSet<Vector3d> HitTest(IEnumerable<Point> points)
        {
            HashSet<Vector3d> result = new HashSet<Vector3d>();

            foreach (var item in Children)
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
        /// <param name="drawable">The drawable to perform hit test on</param>
        /// <returns>The hit test result</returns>
        public HitTestResult HitTest(IEnumerable<Point> points, Drawable drawable)
        {
            return drawable.HitTest(points);
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
}
