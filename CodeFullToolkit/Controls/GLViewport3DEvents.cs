//-----------------------------------------------------------------------------
// Description:
//     Defines the Delegates, EventHandlers and EventArgs for GLViewport3D
// Author:
//     Mehran Maghoumi
//-----------------------------------------------------------------------------
using CodeFull.Graphics;
using CodeFull.Graphics3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CodeFull.Controls
{
    /// <summary>
    /// The delegate to use for the GLViewport3D SelectedDrawableChanged event
    /// </summary>
    public delegate void SelectedDrawableChangedEventHandler(object sender, SelectedDrawableChangedEventArgs e);

    /// <summary>
    ///    GLViewport3DSelectedDrawableChangedEventArgs
    /// </summary>
    public class SelectedDrawableChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the drawable associated with this event.
        /// </summary>
        public Drawable3D Drawable { get; protected set; }

        /// <summary>
        /// Instantiates a new event argument with the specified drawable.
        /// </summary>
        /// <param name="drawable">The drawable of this event.</param>
        public SelectedDrawableChangedEventArgs(Drawable3D drawable)
        {
            this.Drawable = drawable;
        }
    }

    /// <summary>
    /// The delegate to use for the GLViewport3D stylus events
    /// </summary>
    public delegate void StylusEventHandler(object sender, MouseEventArgs e);

    /// <summary>
    /// The delegate to use for the GLViewport3D touch events
    /// </summary>
    public delegate void TouchEventHandler(object sender, MouseEventArgs e);
}