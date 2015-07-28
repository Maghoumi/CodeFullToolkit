using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using CodeFull.Graphics;
using System.Windows.Forms;
using System.ComponentModel;
using CodeFull.Graphics.Geometry;
using CodeFull.Extensions;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Diagnostics;
using CodeFull.Graphics3D;

namespace CodeFull.Controls
{
    /// <summary>
    /// A viewport control is able to render and manipulate Drawable instances in OpenGL.
    /// This control tries to mimic the functionality of WPF's Viewport3D control.
    /// </summary>
    public class GLViewport3D : GLControl
    {
        /// <summary>
        /// Underlying storage for the FPS property.
        /// </summary>
        protected int fps;

        /// <summary>
        /// Underlying storage for the SelectedDrawable property.
        /// </summary>
        protected Drawable3D selectedDrawable;

        /// <summary>
        /// The arcball instance that controls the transformations of the drawables
        /// inside this viewport
        /// </summary>
        protected Arcball arcball;

        /// <summary>
        /// The timer that controls the drawing commands
        /// </summary>
        protected Timer drawCallTimer = new Timer();

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
        public IList<Drawable3D> Children { get; set; }

        /// <summary>
        /// The currently selected drawable of this viewport. This drawable will be manipulated
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Drawable3D SelectedDrawable
        {
            get { return selectedDrawable; }
            set
            {
                selectedDrawable = value;
                arcball.Drawable = value;

                // Raise the SelectedDrawableChanged event if there is a handler set.
                if (this.SelectedDrawableChanged != null)
                    this.SelectedDrawableChanged(this, new SelectedDrawableChangedEventArgs(value));
            }
        }

        /// <summary>
        /// Gets or sets the maximum framerate of the OpenGL renderer
        /// </summary>
        public int FPS
        {
            get { return fps; }
            set { fps = value; SetupRenderTimer(); }
        }

        #region Event Declarations

        /// <summary>
        /// Event raised when the selected drawble of this viewport has changed.
        /// </summary>
        public event SelectedDrawableChangedEventHandler SelectedDrawableChanged;

        /// <summary>
        /// Event raised when stylus down occurs
        /// </summary>
        public event StylusEventHandler StylusDown;

        /// <summary>
        /// Event raised when stylus up occurs
        /// </summary>
        public event StylusEventHandler StylusUp;

        /// <summary>
        /// Event raised when stylus move occurs
        /// </summary>
        public event StylusEventHandler StylusMove;

        /// <summary>
        /// Event raised when stylus click occurs
        /// </summary>
        public event StylusEventHandler StylusClick;

        /// <summary>
        /// Event raised when stylus double click occurs
        /// </summary>
        public event StylusEventHandler StylusDoubleClick;

        /// <summary>
        /// Event raised when Touch down occurs
        /// </summary>
        public event TouchEventHandler TouchDown;

        /// <summary>
        /// Event raised when Touch up occurs
        /// </summary>
        public event TouchEventHandler TouchUp;

        /// <summary>
        /// Event raised when Touch move occurs
        /// </summary>
        public event TouchEventHandler TouchMove;

        /// <summary>
        /// Event raised when Touch click occurs
        /// </summary>
        public event TouchEventHandler TouchClick;

        /// <summary>
        /// Event raised when Touch double click occurs
        /// </summary>
        public event TouchEventHandler TouchDoubleClick;

        #endregion

        public GLViewport3D()
            : base(new OpenTK.Graphics.GraphicsMode(OpenTK.Graphics.GraphicsMode.Default.ColorFormat, 24, OpenTK.Graphics.GraphicsMode.Default.Stencil, 8))
        {
            InitializeComponent();

            arcball = new Arcball(Width, Height, 0.01);
            this.ClearColor = Color.White;
            this.Camera = new Camera() { Position = new Vector3d(0, 0, 5) };
            this.Children = new List<Drawable3D>();
            this.FPS = 80;

            // Setup draw timer.
            SetupRenderTimer();

            // Setup event handlers
            this.MouseDown += GLViewport3D_MouseDown;
            this.MouseUp += GLViewport3D_MouseUp;
            this.Paint += GLViewport3D_Paint;
            this.Load += GLViewport3D_Load;
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

        /// <summary>
        /// Sets up the timer that places the render calls to the renderer.
        /// </summary>
        protected void SetupRenderTimer()
        {
            drawCallTimer.Stop();
            drawCallTimer.Interval = (int)((1.0 / FPS) * 1000);
            drawCallTimer.Tick += drawCallTimer_Tick;
            drawCallTimer.Start();
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

            /*
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Viewport(ClientRectangle);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
            

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Color3(Color.Red);
            GL.PointSize(10);

            GL.Begin(BeginMode.Points);
            GL.Vertex3(new Vector3d(0, 0, 0));
            GL.End();

            GL.Begin(BeginMode.LineStrip);
            GL.Vertex3(new Vector3d(0, 0, 0));
            GL.Vertex3(new Vector3d(0, 1, 0));
            GL.Vertex3(new Vector3d(1, 0, 0));
            GL.End();

            SwapBuffers();
            return;
             */



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

        //------------------------------------------------------
        //
        //  Event Handlers
        //
        //------------------------------------------------------
        #region Even Handlers

        /// <summary>
        /// Load event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The parameters</param>
        private void GLViewport3D_Load(object sender, EventArgs e)
        {
            this.GLViewport3D_Resize(this, EventArgs.Empty);
        }

        /// <summary>
        /// Draw timer event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The parameters</param>
        private void drawCallTimer_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        /// <summary>
        /// Paint event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The parameters</param>
        private void GLViewport3D_Paint(object sender, PaintEventArgs e)
        {
            Render();
        }

        /// <summary>
        /// Resize event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The parameters</param>
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

        /// <summary>
        /// MouseDown event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The parameters</param>
        protected virtual void GLViewport3D_MouseDown(object sender, MouseEventArgs e)
        {
            // Perform picking using any mouse button
            Pick(e.Location);
            // Change arcball's mouse button status
            arcball.OnMouseDown(e);
        }

        /// <summary>
        /// MouseUp event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The parameters</param>
        protected virtual void GLViewport3D_MouseUp(object sender, MouseEventArgs e)
        {
            // Change arcball's mouse button status
            arcball.OnMouseUp(e);
        }

        /// <summary>
        /// Command key handler
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (SelectedDrawable == null)
                return base.ProcessCmdKey(ref msg, keyData);

            HandleKeyboard(keyData);

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Event Raisers -- Raise various events
        //
        //------------------------------------------------------
        #region Event Raisers

        /// <summary>
        /// Raises the MouseDown event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var source = Helpers.GetMouseEventSource();

            switch (source)
            {
                case MouseEventSource.Mouse:
                    base.OnMouseDown(e);
                    break;

                case MouseEventSource.Stylus:
                    this.OnStylusDown(e);
                    break;

                case MouseEventSource.Touch:
                    this.OnTouchDown(e);
                    break;
            }
        }

        /// <summary>
        /// Raises the MouseUp event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            var source = Helpers.GetMouseEventSource();

            switch (source)
            {
                case MouseEventSource.Mouse:
                    base.OnMouseUp(e);
                    break;

                case MouseEventSource.Stylus:
                    this.OnStylusUp(e);
                    break;

                case MouseEventSource.Touch:
                    this.OnTouchUp(e);
                    break;
            }
        }

        /// <summary>
        /// Raises the MouseMove event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var source = Helpers.GetMouseEventSource();

            switch (source)
            {
                case MouseEventSource.Mouse:
                    base.OnMouseMove(e);
                    break;

                case MouseEventSource.Stylus:
                    this.OnStylusMove(e);
                    break;

                case MouseEventSource.Touch:
                    this.OnTouchMove(e);
                    break;
            }
        }

        /// <summary>
        /// Raises the MouseClick event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            var source = Helpers.GetMouseEventSource();

            switch (source)
            {
                case MouseEventSource.Mouse:
                    base.OnMouseClick(e);
                    break;

                case MouseEventSource.Stylus:
                    this.OnStylusClick(e);
                    break;

                case MouseEventSource.Touch:
                    this.OnTouchClick(e);
                    break;
            }
        }

        /// <summary>
        /// Raises the MouseDoubleClick event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var source = Helpers.GetMouseEventSource();

            switch (source)
            {
                case MouseEventSource.Mouse:
                    base.OnMouseDoubleClick(e);
                    break;

                case MouseEventSource.Stylus:
                    this.OnStylusDoubleClick(e);
                    break;

                case MouseEventSource.Touch:
                    this.OnTouchDoubleClick(e);
                    break;
            }
        }

        /// <summary>
        /// Raises the StylusDown event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnStylusDown(MouseEventArgs e)
        {
            if (this.StylusDown != null)
                this.StylusDown(this, e);
        }

        /// <summary>
        /// Raises the StylusUp event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnStylusUp(MouseEventArgs e)
        {
            if (this.StylusUp != null)
                this.StylusUp(this, e);
        }

        /// <summary>
        /// Raises the StylusMove event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnStylusMove(MouseEventArgs e)
        {
            if (this.StylusMove != null)
                this.StylusMove(this, e);
        }

        /// <summary>
        /// Raises the StylusClick event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnStylusClick(MouseEventArgs e)
        {
            if (this.StylusClick != null)
                this.StylusClick(this, e);
        }

        /// <summary>
        /// Raises the StylusDoubleClick event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnStylusDoubleClick(MouseEventArgs e)
        {
            if (this.StylusDoubleClick != null)
                this.StylusDoubleClick(this, e);
        }

        /// <summary>
        /// Raises the TouchDown event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnTouchDown(MouseEventArgs e)
        {
            if (this.TouchDown != null)
                this.TouchDown(this, e);
        }

        /// <summary>
        /// Raises the TouchUp event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnTouchUp(MouseEventArgs e)
        {
            if (this.TouchUp != null)
                this.TouchUp(this, e);
        }

        /// <summary>
        /// Raises the TouchMove event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnTouchMove(MouseEventArgs e)
        {
            if (this.TouchMove != null)
                this.TouchMove(this, e);
        }

        /// <summary>
        /// Raises the TouchClick event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnTouchClick(MouseEventArgs e)
        {
            if (this.TouchClick != null)
                this.TouchClick(this, e);
        }

        /// <summary>
        /// Raises the TouchDoubleClick event
        /// </summary>
        /// <param name="e">The event argument</param>
        protected virtual void OnTouchDoubleClick(MouseEventArgs e)
        {
            if (this.TouchDoubleClick != null)
                this.TouchDoubleClick(this, e);
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

            double minDepth = int.MaxValue;

            foreach (var drawable in this.Children)
            {
                Mesh m = drawable as Mesh;

                if (m != null)
                {
                    var res = m.HitTest(mouseLocation);
                    if (res == null)
                        continue;

                    // Select the point closest to the main camera.
                    double dist = Camera.MainCamera.GetDistanceTo(res.HitPoint);
                    if (dist < minDepth)
                    {
                        minDepth = dist;
                        SelectedDrawable = res.Drawable;
                    }
                }
            }

            if (minDepth != int.MaxValue && null != this.SelectedDrawableChanged)
            {
                this.SelectedDrawableChanged(this, new SelectedDrawableChangedEventArgs(SelectedDrawable));
            }

            return;
        }
    }
}
