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
using CodeFull.Graphics.Geometry;
using System.Diagnostics;
using CodeFull.Extensions;

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
        /// The viewport's camera
        /// (defaults:      Position:    0, 0, 5
        ///                 LookAt:      0, 0, 0
        ///                 Up:          0, 1, 0
        ///                 FieldOfView: 45)
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Camera Camera { get; set; }
        
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
            : base(new OpenTK.Graphics.GraphicsMode(OpenTK.Graphics.GraphicsMode.Default.ColorFormat, OpenTK.Graphics.GraphicsMode.Default.Depth, OpenTK.Graphics.GraphicsMode.Default.Stencil, 8))
        {
            InitializeComponent();

            arcball = new Arcball(Width, Height, 0.01);
            this.ClearColor = Color.White;
            this.Camera = new Camera() { Position = new Vector3d(0, 0, 5)};
            this.Children = new List<Drawable>();
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
        /// Creates the projection matrix based on the control setup.
        /// </summary>
        /// <returns>The projection matrix</returns>
        protected Matrix4d CreateProjectionMatrix()
        {
            float aspect_ratio = this.ClientSize.Width / (float)this.ClientSize.Height;
            return Matrix4d.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Camera.FieldOfView), aspect_ratio, (float)Camera.NearClip, (float)Camera.FarClip);
        }

        /// <summary>
        /// Is called when it is time to render the objects in this viewport.
        /// </summary>
        protected void Render()
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
            Matrix4d perpective = CreateProjectionMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);

            // Setup camera
            Matrix4d modelView = Camera.CreateModelViewMatrix();
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
            //if (SelectedDrawable == null)
            //    return base.ProcessCmdKey(ref msg, keyData);

            HandleKeyboard(keyData);

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Perform picking using any mouse button
            Pick(e.Location);

            // Change arcball's mouse button status
            arcball.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            // Change arcball's mouse button status
            arcball.OnMouseUp(e);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Event Helpers
        //
        //------------------------------------------------------

        #region Event Helpers

        /// <summary>
        /// Handles various keyboard keypresses for object manipulation
        /// </summary>
        /// <param name="keyData">The key that is pressed</param>
        protected void HandleKeyboard(Keys keyData)
        {
            if (keyData == Keys.D)
            {
                SelectedDrawable.Transform.RotateBy(0, 0.1, 0, SelectedDrawable.TransformedCenter);
            }
            if (keyData == Keys.A)
            {
                SelectedDrawable.Transform.RotateBy(0, -0.1, 0, SelectedDrawable.TransformedCenter);
            }
            if (keyData == Keys.W)
            {
                SelectedDrawable.Transform.RotateBy(-0.1, 0, 0, SelectedDrawable.TransformedCenter);
            }
            if (keyData == Keys.S)
            {
                SelectedDrawable.Transform.RotateBy(0.1, 0, 0, SelectedDrawable.TransformedCenter);
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
                SelectedDrawable.Transform.ScaleBy(0.1, 0.1, 0.1, SelectedDrawable.TransformedCenter);
            }

            if (keyData == Keys.Subtract)
            {
                SelectedDrawable.Transform.ScaleBy(-0.1, -0.1, -0.1, SelectedDrawable.TransformedCenter);
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
        }
        #endregion

        /// <summary>
        /// Performs picking and changes the SelectedDrawable property based on
        /// the drawable that was under the mouse cursor.
        /// </summary>
        /// <param name="mouseLocation">The cursor location</param>
        protected void Pick(Point mouseLocation)
        {
            // If depth is 1, means do not do hit test (nothing under the cursor).
            if (Helpers.GetDepth(mouseLocation) == 1)
                return;

            Ray ray = Helpers.ScreenPointToRay(mouseLocation);

            double minDepth = int.MaxValue;

            foreach (var drawable in this.Children)
            {
                Mesh m = drawable as Mesh;

                if (m != null)
                {
                    var res = m.HitTest(ray.ToObjectSpace(drawable));
                    if (res == null)
                        continue;

                    // Select the point closest to the main camera.
                    double dist = Camera.MainCamera.GetDistanceTo(res.HitPoint);
                    if (dist < minDepth)
                    {
                        minDepth = dist;
                        arcball.Drawable = SelectedDrawable = res.Drawable;
                    }
                }
            }

            if (minDepth != int.MaxValue && null != this.SelectionChanged)
            {
                this.SelectionChanged(this, EventArgs.Empty);
            }

            return;
        }
    }
}
