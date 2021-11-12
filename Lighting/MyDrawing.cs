using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using System.Drawing;
using static Lighting.MyGeometry;
using static Lighting.Athens;
using static Lighting.FastBitmap;

namespace Lighting
{
    static class MyDrawing
    {

        public static int penWidth = 1;
        public static Point3D cameraPoint;
        public static int cameraLength;
        public static bool showWireframe = false;
        public static bool flatColor = false;
        public static Point3D Light;
        public static Point ScreenPos(Point3D p)
        {
            return new Point((int)p.X + (int)zeroPoint.X, (int)p.Y + (int)zeroPoint.Y);
        }

        public static void DrawScene(Bitmap bm, Bitmap tex, PictureBox pb)
        {
            Graphics g = Graphics.FromImage(bm);
            g.Clear(Color.Transparent);
            double[,] Zbuf = new double[bm.Width, bm.Height];
            Color[,] Cbuf = new Color[bm.Width, bm.Height];
            double[,] Darkness = new double[bm.Width, bm.Height];
            //init
            for (int i = 0; i < bm.Width; i++)
            {
                for (int j = 0; j < bm.Height; j++)
                {
                    Zbuf[i, j] = 0;
                    Cbuf[i, j] = Color.Black;
                    Darkness[i, j] = 0;
                }
            }
            double zMax = 0;
            double zMin = int.MaxValue;

            foreach (Mesh msh in mesh)
            {
                //все нормали фигуры
                List<Point3D> vNorms = VertexNormals(msh, false);

                foreach (Polygon pol in msh.faces)
                {
                    List<Point3D> curVNorms = new List<Point3D>();
                    foreach (var tp in pol.points)
                    {
                        curVNorms.Add(vNorms.Where((x) => x.index == tp.index).First());
                    }

                    Color curColor;
                    {
                        Point3D normal = CreateNormal(pol, false);

                        if (flatColor)
                        {
                            int angle = (int)(AngleBetween(normal, Light, true) * 127 / Math.PI);
                            curColor = Color.FromArgb(angle, angle, angle);
                        }
                        else
                            curColor = Color.GreenYellow;

                        //отсечение невидимых граней
                        if (AngleBetween(new Point3D(0, 0, 1), normal, false) <= 90)
                            continue;
                    }


                    List<Edge> listEd = new List<Edge>();
                    int maxInd = 0;
                    int maxIndP = 0;
                    double maxY = int.MinValue;
                    double minY = int.MaxValue;
                    int minInd = 0;
                    int minIndP = 0;
                    for (int i = 0; i < pol.points.Count(); i++)
                    {
                        Point3D screenp1 = pol.points[i];
                        if (screenp1.Y > maxY)
                        {
                            maxY = screenp1.Y;
                            maxInd = i;
                            maxIndP = pol.points[i].index;
                        }
                        if (screenp1.Y < minY)
                        {
                            minY = screenp1.Y;
                            minInd = i;
                            minIndP = pol.points[i].index;
                        }
                        if (i > 0)
                        {
                            listEd.Add(new Edge(pol.points[i - 1], screenp1));
                        }
                    }
                    listEd.Add(new Edge(pol.points[pol.points.Count() - 1], pol.points[0]));

                    int counter = pol.points.Count();
                    int lInd = (maxInd - 1 + counter) % counter;
                    int rInd = maxInd;
                    int iters = counter;
                    Edge lEd = listEd[lInd];
                    Edge rEd = listEd[rInd];
                    float distL = (float)Distance(new Point((int)lEd.p1.X, (int)lEd.p1.Y), new Point((int)lEd.p2.X, (int)lEd.p2.Y));
                    float distR = (float)Distance(new Point((int)rEd.p1.X, (int)rEd.p1.Y), new Point((int)rEd.p2.X, (int)rEd.p2.Y));
                    float lStep = (float)((lEd.p1.X - lEd.p2.X) / Math.Abs(lEd.p1.Y - lEd.p2.Y));
                    float rStep = (float)((rEd.p2.X - rEd.p1.X) / Math.Abs(rEd.p2.Y - rEd.p1.Y));
                    if (float.IsNaN(lStep)) lStep = 0;
                    if (lStep < -distL) lStep = -distL;
                    if (lStep > distL) lStep = distL;
                    if (float.IsNaN(rStep)) rStep = 0;
                    if (rStep < -distR) rStep = -distR;
                    if (rStep > distR) rStep = distR;
                    Point3D lCurP = new Point3D(lEd.p2.X, lEd.p2.Y, lEd.p2.Z, lEd.p2.index, lEd.p2.UV);
                    Point3D rCurP = new Point3D(rEd.p1.X, rEd.p1.Y, rEd.p1.Z, rEd.p1.index, rEd.p1.UV);

                    PointF L_UV = new PointF(0, 0);
                    PointF R_UV = new PointF(0, 0);

                    float L_UV_Step = 0;
                    float R_UV_Step = 0;

                    if (tex != null)
                    {
                        L_UV_Step = tex.Width / (float)Math.Abs(rEd.p2.Y - rEd.p1.Y);
                        R_UV_Step = tex.Height / (float)Math.Abs(lEd.p1.Y - lEd.p2.Y);
                    }

                    bool toggle = false;
                    bool tri = false;

                    if (pol.points.Count == 3)
                    {
                        tri = true;
                    }

                    bool l = true;
                    bool r = true;

                    // Гуро
                    Point3D lcurNorm = curVNorms.Where((x) => x.index == maxIndP).First();
                    Point3D rcurNorm = lcurNorm;
                    Point3D lNorm = curVNorms.Where((x) => x.index == listEd[lInd].p1.index).First();
                    Point3D rNorm = curVNorms.Where((x) => x.index == listEd[((rInd + 1 + counter) % counter)].p1.index).First();
                    Point3D curLight = new Point3D(msh.points[maxIndP].X - Light.X, msh.points[maxIndP].Y - Light.Y, msh.points[maxIndP].Z - Light.Z);
                    double lcurDark = DarknessByAngle(lcurNorm, curLight);
                    double rcurDark = lcurDark;
                    Point3D lLight = new Point3D(lEd.p1.X - Light.X, lEd.p1.Y - Light.Y, lEd.p1.Z - Light.Z);
                    double lDark = DarknessByAngle(lNorm, lLight);
                    Point3D rLight = new Point3D(rEd.p2.X - Light.X, rEd.p2.Y - Light.Y, rEd.p2.Z - Light.Z);
                    double rDark = DarknessByAngle(rNorm, rLight);
                    double lt = Math.Abs(lEd.p1.Y - lEd.p2.Y);
                    if (lt < 1) lt = 1;
                    double lydiff = (lDark - lcurDark) / lt;
                    double rt = Math.Abs(rEd.p2.Y - rEd.p1.Y);
                    if (rt < 1) rt = 1;
                    double rydiff = (rDark - rcurDark) / rt;
                    while (iters > 0)
                    {
                        while (lCurP.Y > lEd.p1.Y && rCurP.Y > rEd.p2.Y)
                        {
                            lCurP = new Point3D(lCurP.X + lStep, lCurP.Y - 1, lCurP.Z);
                            rCurP = new Point3D(rCurP.X + rStep, rCurP.Y - 1, rCurP.Z);

                            if (tex != null)
                            {
                                if (l)
                                {
                                    L_UV = new PointF(L_UV.X + L_UV_Step, 0);
                                }
                                else
                                {
                                    if (!tri)
                                    {
                                        L_UV = new PointF(L_UV.X, L_UV.Y + L_UV_Step);
                                    }
                                    else
                                    {
                                        L_UV = new PointF(L_UV.X - L_UV_Step, L_UV.Y + L_UV_Step);
                                    }
                                }

                                if (r)
                                {
                                    R_UV = new PointF(0, R_UV.Y + R_UV_Step);
                                }
                                else
                                {
                                    if (!tri)
                                    {
                                        R_UV = new PointF(R_UV.X + R_UV_Step, R_UV.Y);
                                    }
                                    else
                                    {
                                        R_UV = new PointF(R_UV.X + R_UV_Step, R_UV.Y - R_UV_Step);
                                    }
                                }
                            }

                            lcurDark += lydiff;
                            rcurDark += rydiff;

                            if (lCurP.Y <= lEd.p1.Y)
                            {
                                lCurP = lEd.p1;
                            }
                            if (rCurP.Y <= rEd.p2.Y)
                            {
                                rCurP = rEd.p2;
                            }

                            int lineX = Math.Abs((int)(lCurP.X - rCurP.X));

                            int tt = lineX;

                            double xdiff = Math.Abs(lcurDark - rcurDark) / (tt + 1);

                            float textureX_Step = 0;
                            float textureY_Step = 0;

                            if (lineX != 0)
                            {
                                textureX_Step = (R_UV.X - L_UV.X) / (lineX + 1);
                                textureY_Step = (R_UV.Y - L_UV.Y) / (lineX + 1);
                            }

                            float textureX = L_UV.X;
                            float textureY = L_UV.Y;

                            for (int i = 0; i < lineX + 1; i++)
                            {
                                int t1 = 0;
                                int t2 = (int)lCurP.Y;
                                double curDark;

                                if (rCurP.X < lCurP.X)
                                {
                                    t1 = (int)(rCurP.X) + i;

                                    if (rcurDark < lcurDark)
                                        curDark = rcurDark + xdiff * i;
                                    else
                                        curDark = rcurDark - xdiff * i;
                                }
                                else
                                {
                                    t1 = (int)(lCurP.X) + i;

                                    if (rcurDark < lcurDark)
                                        curDark = lcurDark - xdiff * i;
                                    else
                                        curDark = lcurDark + xdiff * i;
                                }

                                t1 += bm.Width / 2;
                                t2 += bm.Height / 2;

                                if (t1 >= 0 && t1 < bm.Width && t2 >= 0 && t2 < bm.Height)
                                {
                                    t1 -= bm.Width / 2;
                                    t2 -= bm.Height / 2;
                                    double zVal = ZbyPlane(pol.points[0], pol.points[1], pol.points[2], t1, t2) + cameraPoint.Z;

                                    t1 += bm.Width / 2;
                                    t2 += bm.Height / 2;

                                    if (zVal > zMax && zVal < 1000) zMax = zVal;
                                    if (zVal < zMin) zMin = zVal;
                                    if (zVal > Zbuf[t1, t2])
                                    {
                                        if (tex != null)
                                        {
                                            if (textureX >= tex.Width)
                                            {
                                                textureX = tex.Width - 1;
                                            }

                                            if (textureY >= tex.Height)
                                            {
                                                textureY = tex.Height - 1;
                                            }

                                            if (textureX <= 0)
                                            {
                                                textureX = 0;
                                            }

                                            if (textureY <= 0)
                                            {
                                                textureY = 0;
                                            }
                                        }

                                        Zbuf[t1, t2] = zVal;

                                        if (tex != null)
                                        {
                                            Cbuf[t1, t2] = tex.GetPixel((int)textureX, (int)textureY);
                                        }
                                        else
                                        {
                                            Cbuf[t1, t2] = curColor;
                                        }

                                        Darkness[t1, t2] = curDark;

                                        textureX += textureX_Step;
                                        textureY += textureY_Step;

                                    }
                                }
                            }

                        }
                        if (lCurP.Y <= lEd.p1.Y)
                        {
                            lInd = (lInd - 1 + counter) % counter;
                            iters--;
                            lEd = listEd[lInd];
                            lCurP = new Point3D(lEd.p2.X, lEd.p2.Y, lEd.p2.Z);
                            lStep = (float)((lEd.p1.X - lEd.p2.X) / Math.Abs(lEd.p1.Y - lEd.p2.Y));
                            distL = (float)Distance(new Point((int)lEd.p1.X, (int)lEd.p1.Y), new Point((int)lEd.p2.X, (int)lEd.p2.Y));
                            if (float.IsNaN(lStep)) lStep = 0;
                            if (lStep < -distL) lStep = -distL;
                            if (lStep > distL) lStep = distL;

                            lcurNorm = curVNorms.Where((x) => x.index == listEd[lInd].p2.index).First();
                            lNorm = curVNorms.Where((x) => x.index == listEd[lInd].p1.index).First();
                            curLight = new Point3D(lCurP.X - Light.X, lCurP.Y - Light.Y, lCurP.Z - Light.Z);
                            lcurDark = DarknessByAngle(lcurNorm, curLight);
                            lLight = new Point3D(lEd.p1.X - Light.X, lEd.p1.Y - Light.Y, lEd.p1.Z - Light.Z);
                            lDark = DarknessByAngle(lNorm, lLight);
                            lt = Math.Abs(lEd.p1.Y - lEd.p2.Y);
                            if (lt < 1) lt = 1;
                            lydiff = (lDark - lcurDark) / lt;

                            if (tex != null)
                            {
                                R_UV = new PointF(0, tex.Height - 1); ;

                                if (toggle)
                                {
                                    R_UV_Step = tex.Height / (float)Math.Abs(lEd.p1.Y - lEd.p2.Y);
                                }
                                else
                                {
                                    R_UV_Step = tex.Height / (float)Math.Abs(rEd.p2.Y - rEd.p1.Y);
                                }
                            }

                            r = false;
                            toggle = !toggle;
                        }
                        if (iters == 0) break;
                        if (rCurP.Y <= rEd.p2.Y)
                        {
                            rInd = (rInd + 1 + counter) % counter;
                            iters--;
                            rEd = listEd[rInd];
                            rCurP = new Point3D(rEd.p1.X, rEd.p1.Y, rEd.p1.Z);
                            rStep = (float)((rEd.p2.X - rEd.p1.X) / Math.Abs(rEd.p2.Y - rEd.p1.Y));
                            distR = (float)Distance(new Point((int)rEd.p1.X, (int)rEd.p1.Y), new Point((int)rEd.p2.X, (int)rEd.p2.Y));
                            if (float.IsNaN(rStep)) rStep = 0;
                            if (rStep < -distR) rStep = -distR;
                            if (rStep > distR) rStep = distR;

                            rcurNorm = curVNorms.Where((x) => x.index == listEd[rInd].p1.index).First();
                            rNorm = curVNorms.Where((x) => x.index == listEd[rInd].p2.index).First();
                            curLight = new Point3D(rCurP.X - Light.X, rCurP.Y - Light.Y, rCurP.Z - Light.Z);
                            rcurDark = DarknessByAngle(rcurNorm, curLight);
                            rLight = new Point3D(rEd.p2.X - Light.X, rEd.p2.Y - Light.Y, rEd.p2.Z - Light.Z);
                            rDark = DarknessByAngle(rNorm, rLight);
                            rt = Math.Abs(rEd.p2.Y - rEd.p1.Y);
                            if (rt < 1) rt = 1;
                            rydiff = (rDark - rcurDark) / rt;

                            if (tex != null)
                            {
                                L_UV = new PointF(tex.Width - 1, 0);

                                if (toggle)
                                {
                                    L_UV_Step = tex.Width / (float)Math.Abs(rEd.p2.Y - rEd.p1.Y);

                                }
                                else
                                {

                                    L_UV_Step = tex.Width / (float)Math.Abs(lEd.p1.Y - lEd.p2.Y);
                                }
                            }

                            l = false;
                            toggle = !toggle;
                        }
                    }
                }

                RefreshBitmap(bm, Cbuf, Darkness);
            }
            if (showWireframe)
                DrawWireframe(g, mesh);

            pb.Refresh();
        }


        

        private static void DrawWireframe(Graphics g, List<Mesh> meshes)
        {
            Color col = Color.Orange;
            Pen pen = new Pen(col, penWidth);
            foreach (Mesh msh in mesh)
                foreach (Polygon pol in msh.faces)
                {
                    for (int i = 0; i < pol.points.Count() - 1; i++)
                    {
                        Point screenp1 = ScreenPos(pol.points[i]);
                        Point screenp2 = ScreenPos(pol.points[i + 1]);
                        g.DrawLine(pen, screenp1, screenp2);
                    }
                    Point screenp11 = ScreenPos(pol.points[pol.points.Count() - 1]);
                    Point screenp22 = ScreenPos(pol.points[0]);
                    g.DrawLine(pen, screenp11, screenp22);
                    continue;
                }
        }

        /// <summary>
        /// Обновление изображения битмапа через быстрый битмап
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="Cbuf"></param>
        /// <param name="Darkness"></param>
        private static void RefreshBitmap(Bitmap bm, Color[,] Cbuf, double[,] Darkness)
        {
            using (var fastBitmap = new FastBitmap(bm))
            {
                for (int i = 0; i < bm.Width; i++)
                    for (int j = 0; j < bm.Height; j++)
                    {
                        Color color;
                        if (flatColor)
                        {
                            color = Cbuf[i, j];
                        }
                        else
                        {
                            color = Color.FromArgb((int)(Cbuf[i, j].R * (1 - Darkness[i, j])),
                                                    (int)(Cbuf[i, j].G * (1 - Darkness[i, j])),
                                                    (int)(Cbuf[i, j].B * (1 - Darkness[i, j])));
                        }
                        fastBitmap[i, j] = color;
                    }
            }
        }

        public static double DarknessByAngle(Point3D p1, Point3D p2)
        {
            double ans = 0;
            double tang = AngleBetween(p1, p2, true) / (Math.PI);
            ans = tang > 1 ? (2 - tang) : tang;
            ans = ans > 0.5 ? 0.95 : ans;
            return ans;
        }

        public static double ZbyPlane(Point3D p1, Point3D p2, Point3D p3, double x, double y)
        {
            double x1 = p1.X, y1 = p1.Y, z1 = p1.Z;
            double x2 = p2.X, y2 = p2.Y, z2 = p2.Z;
            double x3 = p3.X, y3 = p3.Y, z3 = p3.Z;

            double a = (y2 - y1) * (z3 - z1) - (z2 - z1) * (y3 - y1);
            double b = (z2 - z1) * (x3 - x1) - (x2 - x1) * (z3 - z1);
            double c = (x2 - x1) * (y3 - y1) - (y2 - y1) * (x3 - x1);
            double d = (-1) * (a * x1 + b * y1 + c * z1);

            double z = ((a * x + b * y + d) / (-c));
            return z;
        }
    }
}
