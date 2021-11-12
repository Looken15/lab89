using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Lighting.MyGeometry;
using static Lighting.Athens;
using static Lighting.MyDrawing;

namespace Lighting
{
    public partial class Form1 : Form
    {
        Bitmap pic;
        Bitmap texture;
        int curMeshInd = 0;
        bool[] transformAxis;
        bool mDown;
        Point curP;
        ActType at;
        Point prev_pos;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pic = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = pic;
            mesh = new List<Mesh>();
            meshOrig = new List<Mesh>();
            zeroPoint = new Point3D(pictureBox1.Width / 2, pictureBox1.Height / 2, 0, 0);
            transformAxis = new bool[3] { false, false, false };
            cameraPoint = new Point3D(0, 0, 500, 0);
            cameraLength = 200;
            Light = new Point3D(0, -1000, 0);
            at = ActType.Move;
            ResetAthene();
            DrawScene(pic, texture, pictureBox1);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            int step = 50;
            double angle = Math.Atan(step / cameraPoint.Z);
            var camera_matr = new double[4, 4];
            switch (keyData)
            {
                case Keys.W: camera_matr = AtheneMove(0, -step, 0); break;
                case Keys.A: camera_matr = AtheneMove(step, 0, 0); break;
                case Keys.S: camera_matr = AtheneMove(0, step, 0); break;
                case Keys.D: camera_matr = AtheneMove(-step, 0, 0); break;
                case Keys.Left: camera_matr = MatrixMult(AtheneMove(step, 0, 0), AtheneRotate(angle, 'y')); break;
                case Keys.Right: camera_matr = MatrixMult(AtheneMove(-step, 0, 0), AtheneRotate(-angle, 'y')); break;
                case Keys.Up: camera_matr = MatrixMult(AtheneMove(0, step, 0), AtheneRotate(-angle, 'x')); break;
                case Keys.Down: camera_matr = MatrixMult(AtheneMove(0, -step, 0), AtheneRotate(angle, 'x')); break;
            }
            for (int i = 0; i < mesh.Count; ++i)
            {
                var z = mesh[i];
                AtheneTransform(ref z, camera_matr);
                mesh[i] = z;
            }
            DrawScene(pic, texture, pictureBox1);
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void comboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mDown = true;
            curP = e.Location;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mDown)
            {
                if (checkBox6.Checked)
                    pictureBox1_Move(e as MouseEventArgs);
                return;
            }
            if (mesh.Count == 0) return;
            if (at == ActType.Move)
            {
                if (transformAxis[0])
                {
                    translateX = e.Location.X - curP.X;
                }
                if (transformAxis[1])
                {
                    translateY = e.Location.Y - curP.Y;
                }
                if (transformAxis[2])
                {
                    translateZ = e.Location.Y - curP.Y;
                }
                int t1 = translateX;
                int t2 = translateY;
                int t3 = translateZ;
                MoveMatrix = AtheneMove(translateX, translateY, translateZ);
                double[,] matr = MatrixMult(MatrixMult(firstMatrix, MoveMatrix), lastMatrix);
                //mesh = new Mesh(meshOrig);
                Mesh newMesh = new Mesh(meshOrig[curMeshInd]);
                AtheneTransform(ref newMesh, matr);
                mesh[curMeshInd] = new Mesh(newMesh);
                DrawScene(pic, texture, pictureBox1);
            }
            else if (at == ActType.Rotate)
            {
                Point p1 = new Point(pictureBox1.Width / 2 - curP.X, pictureBox1.Height / 2 - curP.Y);
                Point p2 = new Point(pictureBox1.Width / 2 - e.X, pictureBox1.Height / 2 - e.Y);
                double[,] RotateMatrixX = new double[4, 4];
                double[,] RotateMatrixY = new double[4, 4];
                double[,] RotateMatrixZ = new double[4, 4];
                if (transformAxis[0])
                {
                    rotateAngleX = AngleBetween(p1, p2);
                    RotateMatrixX = AtheneRotate(rotateAngleX, 'x');
                }
                else
                {
                    RotateMatrixX = AtheneRotate(0, 'x');
                }

                if (transformAxis[1])
                {
                    rotateAngleY = AngleBetween(p1, p2);
                    RotateMatrixY = AtheneRotate(rotateAngleY, 'y');
                }
                else
                {
                    RotateMatrixY = AtheneRotate(0, 'y');
                }

                if (transformAxis[2])
                {
                    rotateAngleZ = AngleBetween(p1, p2);
                    RotateMatrixZ = AtheneRotate(rotateAngleZ, 'z');
                }
                else
                {
                    RotateMatrixZ = AtheneRotate(0, 'z');
                }

                RotateMatrix = MatrixMult(MatrixMult(RotateMatrixX, RotateMatrixY), RotateMatrixZ);
                double[,] matr = MatrixMult(MatrixMult(firstMatrix, RotateMatrix), lastMatrix);
                //mesh = new Mesh(meshOrig);
                Mesh newMesh = new Mesh(meshOrig[curMeshInd]);
                AtheneTransform(ref newMesh, matr);
                mesh[curMeshInd] = new Mesh(newMesh);
                DrawScene(pic, texture, pictureBox1);
            }
            else if (at == ActType.Scale)
            {
                if (curP != e.Location)
                {
                    Point curPP = curP;
                    Point graphAnchor = new Point(pictureBox1.Width / 2, pictureBox1.Height / 2);
                    if (curPP.X == graphAnchor.X) curPP.X += 1;
                    if (curPP.Y == graphAnchor.Y) curPP.Y += 1;
                    double ss = Distance(e.Location, graphAnchor) / Distance(curPP, graphAnchor);
                    if (transformAxis[0])
                    {
                        scaleFactorX = ss;
                        if (Math.Abs(scaleFactorX) > 1000) scaleFactorX = 1000;
                    }
                    if (transformAxis[1])
                    {
                        scaleFactorY = ss;
                        if (Math.Abs(scaleFactorY) > 1000) scaleFactorY = 1000;
                    }
                    if (transformAxis[2])
                    {
                        scaleFactorZ = ss;
                        if (Math.Abs(scaleFactorZ) > 1000) scaleFactorZ = 1000;
                    }

                    ScaleMatrix = AtheneScale(scaleFactorX, scaleFactorY, scaleFactorZ);
                    double[,] matr = MatrixMult(MatrixMult(firstMatrix, ScaleMatrix), lastMatrix);
                    Mesh newMesh = new Mesh(meshOrig[curMeshInd]);
                    AtheneTransform(ref newMesh, matr);
                    mesh[curMeshInd] = new Mesh(newMesh);
                    DrawScene(pic, texture, pictureBox1);
                }
            }

        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            mDown = false;
            if (mesh.Count > 0)
                meshOrig[curMeshInd] = new Mesh(mesh[curMeshInd]);
            ResetAthene();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Mesh newMesh = new Mesh();
            string name = LoadMesh(ref newMesh);
            if (name == "")
                return;
            if (comboBox1.Items.Contains(name))
            {
                int counter = 1;
                string newname = name + "_" + counter;
                while (comboBox1.Items.Contains(newname))
                {
                    counter++;
                    newname = name + "_" + counter;
                }
                name = newname;
            }
            mesh.Add(new Mesh(newMesh));
            meshOrig.Add(new Mesh(newMesh));
            comboBox1.Items.Add(name);
            comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
            DrawScene(pic, texture, pictureBox1);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (mesh.Count == 0)
                return;
            mesh.Remove(mesh[comboBox1.SelectedIndex]);
            comboBox1.Items.RemoveAt(comboBox1.SelectedIndex);
            comboBox1.Text = "";
            DrawScene(pic, texture, pictureBox1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            at = ActType.Move;
            button3.Enabled = false;
            button4.Enabled = true;
            button5.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            at = ActType.Rotate;
            button3.Enabled = true;
            button4.Enabled = false;
            button5.Enabled = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            at = ActType.Scale;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            transformAxis[0] = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            transformAxis[1] = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            transformAxis[2] = checkBox3.Checked;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            mesh = new List<Mesh>();
            meshOrig = new List<Mesh>();
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            curMeshInd = 0;
            DrawScene(pic, texture, pictureBox1);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            curMeshInd = comboBox1.SelectedIndex;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            showWireframe = checkBox4.Checked;
            DrawScene(pic, texture, pictureBox1);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            flatColor = checkBox5.Checked;
            DrawScene(pic, texture, pictureBox1);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int lx = Convert.ToInt32(textBox1.Text);
            int ly = Convert.ToInt32(textBox2.Text);
            int lz = Convert.ToInt32(textBox3.Text);
            Light = new Point3D(-lx, -ly, -lz);
            DrawScene(pic, texture, pictureBox1);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string path;
            OpenFileDialog myfile = new OpenFileDialog();
            myfile.Filter = "(*.jpg)|*.jpg|(*.png)|*.png|All files (*.*)|*.*";
            if (myfile.ShowDialog() == DialogResult.OK)
            {
                path = myfile.FileName;
                texture = new Bitmap(Image.FromFile(path));
                checkBox1.Enabled = true;
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void pictureBox1_Move(EventArgs e)
        {
            var me = e as MouseEventArgs;
            if (checkBox6.Checked)
            {
                var step_x = me.Location.X - prev_pos.X;
                var step_y = me.Location.Y - prev_pos.Y;
                double angle_x = Math.Atan(step_x / cameraPoint.Z);
                double angle_y = Math.Atan(step_y / cameraPoint.Z);
                var camera_matr = MatrixMult(MatrixMult(AtheneMove(step_x, 0, 0), AtheneMove(0, step_y, 0)),
                    MatrixMult(AtheneRotate(angle_x, 'x'), AtheneRotate(angle_y, 'y')));
                
                for (int i = 0; i < mesh.Count; ++i)
                {
                    var z = mesh[i];
                    AtheneTransform(ref z, camera_matr);
                    mesh[i] = z;
                }
                DrawScene(pic, texture, pictureBox1);
            }
            prev_pos = me.Location;
        }
    }
}
