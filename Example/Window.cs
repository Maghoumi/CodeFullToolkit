﻿using CodeFull.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Example
{
    public partial class Window : Form
    {
        public Window()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Mesh mesh = Mesh.LoadFromPLYFile(@"..\..\files\cube.ply");
            viewport.Meshes.Add(mesh);
            lstMeshes.Items.Add(mesh);

            lblHelp.Text = "Click a mesh to select\n" +
                "Pan = Left click\n" +
                "Rotate = Right click\n" +
                "Scale = Middle click";
        }

        private void lstMeshes_SelectedValueChanged(object sender, EventArgs e)
        {
            Mesh m = (lstMeshes.SelectedValue as Mesh);

            if (m != null)
                viewport.SelectedMesh = (Mesh)lstMeshes.SelectedValue;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".ply";
            dlg.Filter = "PLY Meshes (*.ply)|*.ply";

            DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var item in dlg.FileNames)
                {
                    Mesh m = Mesh.LoadFromPLYFile(item);
                    viewport.Meshes.Add(m);
                    lstMeshes.Items.Add(m);
                }
            }
        }

        private void viewport_SelectionChanged(object sender, EventArgs e)
        {
            if (null != viewport.SelectedMesh)
                this.lstMeshes.SelectedItem = viewport.SelectedMesh;
        }
    }
}
