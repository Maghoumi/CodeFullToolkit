using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CodeFull.Graphics
{
    /// <summary>
    /// Represents the structure of mesh vertices that can be drawn using OpenGL
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MeshVertex
    {
        public Vector3d Position;
        public uint Color;

        public MeshVertex(double x, double y, double z, Color color)
        {
            Position = new Vector3d(x, y, z);
            Color = ToRgba(color);
        }

        static uint ToRgba(Color color)
        {
            return (uint)color.A << 24 | (uint)color.B << 16 | (uint)color.G << 8 | (uint)color.R;
        }
    }
}
