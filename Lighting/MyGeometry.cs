using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using System.Drawing;

namespace Lighting
{
    static class MyGeometry
    {
        public static List<Mesh> mesh;
        public static List<Mesh> meshOrig;

        public static List<Point3D> VertexNormals(Mesh msh, bool clockwise)
        {
            List<Point3D> ans = new List<Point3D>();
            foreach (Point3D point in msh.points)
            {
                List<Point3D> faceNormals = new List<Point3D>();
                foreach (Polygon face in msh.faces)
                {
                    if (face.points.Where((x) => x.index == point.index).Count() > 0) continue;

                    Point3D fnorm = CreateNormal(face, clockwise);
                    faceNormals.Add(fnorm);
                }
                if (faceNormals.Count > 0)
                {
                    Point3D norm = new Point3D(0, 0, 0, point.index);
                    foreach (Point3D p in faceNormals)
                    {
                        norm.X += p.X;
                        norm.Y += p.Y;
                        norm.Z += p.Z;
                    }
                    double len = Distance(new Point3D(0, 0, 0), norm);
                    norm.X /= len;
                    norm.Y /= len;
                    norm.Z /= len;
                    ans.Add(norm);
                }
            }
            return ans;
        }
        public static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
        public static double Distance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
        public static double Distance(Point3D p1, Point3D p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }
        public static double AngleBetween(Point p1, Point p2)
        {
            return Math.Atan2(p1.X, p1.Y) - Math.Atan2(p2.X, p2.Y);
        }
        public static double AngleBetween(Point3D p1, Point3D p2, bool radians = false)
        {
            double a = Math.Sqrt(p1.X * p1.X + p1.Y * p1.Y + p1.Z * p1.Z) * Math.Sqrt(p2.X * p2.X + p2.Y * p2.Y + p2.Z * p2.Z);
            double b = p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z;
            double c = b / a;
            if (!radians)
            {
                return Math.Acos(c) * 180 / Math.PI;
            }
            else
            {
                return Math.Acos(c);
            }
            
        }
        public static Point3D CreateNormal(Polygon polygon, bool clockwise)
        {
            Point3D v1 = new Point3D(polygon.points[0].X - polygon.points[1].X,
                                     polygon.points[0].Y - polygon.points[1].Y,
                                     polygon.points[0].Z - polygon.points[1].Z);
            Point3D v2 = new Point3D(polygon.points[2].X - polygon.points[1].X,
                                     polygon.points[2].Y - polygon.points[1].Y,
                                     polygon.points[2].Z - polygon.points[1].Z);
            Point3D normalv = new Point3D();
            
            if (clockwise)
            {
                normalv = new Point3D(v1.Z * v2.Y - v1.Y * v2.Z, v1.X * v2.Z - v1.Z * v2.X, v1.Y * v2.X - v1.X * v2.Y);
            }
            else
            {
                normalv = new Point3D(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);
            }
            
            double dist = Distance(new Point3D(0, 0, 0), normalv);
            return new Point3D(normalv.X / dist, normalv.Y / dist, normalv.Z / dist);
        }
        public static void SaveMesh(Mesh mesh)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "obj files (*.obj)|*.obj";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileStream Stream = new FileStream(saveFileDialog1.FileName, FileMode.Create);
                using (StreamWriter writer = new StreamWriter(Stream, Encoding.UTF8))
                {
                    writer.WriteLine("o MyMesh");
                    foreach (Point3D p in mesh.points)
                    {
                        string pX = p.X.ToString(CultureInfo.InvariantCulture);
                        string pY = p.Y.ToString(CultureInfo.InvariantCulture);
                        string pZ = p.Z.ToString(CultureInfo.InvariantCulture);
                        writer.WriteLine("v " + pX + " " + pY + " " + pZ);
                    }
                    foreach (Polygon p in mesh.faces)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("f ");
                        int counter = p.points.Count();
                        foreach (Point3D p3 in p.points)
                        {
                            counter--;
                            if (counter==0)
                            {
                                sb.Append((p3.index + 1).ToString());
                            }
                            else
                            {
                                sb.Append((p3.index + 1).ToString() + " ");
                            }
                        }
                        writer.WriteLine(sb);
                    }
                }
            }            
        }

        public static Point Calc_X(Edge2D e1, Edge2D e2)
        {
            Point X = new Point(-1, -1);

            double a_1 = (e1.p2.Y - e1.p1.Y);
            double a_2 = (e2.p2.Y - e2.p1.Y);

            double b_1 = (e1.p1.X - e1.p2.X);
            double b_2 = (e2.p1.X - e2.p2.X);

            double c_1 = (e1.p1.X * (e1.p1.Y - e1.p2.Y) + e1.p1.Y * (e1.p2.X - e1.p1.X));
            double c_2 = (e2.p1.X * (e2.p1.Y - e2.p2.Y) + e2.p1.Y * (e2.p2.X - e2.p1.X));

            double D = a_1 * b_2 - b_1 * a_2;
            double D_X = (-c_1) * b_2 - b_1 * (-c_2);
            double D_Y = a_1 * (-c_2) - (-c_1) * a_2;

            X.X = (int)(D_X / D);
            X.Y = (int)(D_Y / D);

            return X;
        }

        public static bool Is_X_Edge(Edge2D e1, Edge2D e2)
        {
            float P1P2_X = e1.p2.X - e1.p1.X;
            float P3P4_X = e2.p2.X - e2.p1.X;

            float P1P3_X = e2.p1.X - e1.p1.X;
            float P1P4_X = e2.p2.X - e1.p1.X;

            float P3P1_X = e1.p1.X - e2.p1.X;
            float P3P2_X = e1.p2.X - e2.p1.X;

            float P1P2_Y = e1.p2.Y - e1.p1.Y;
            float P3P4_Y = e2.p2.Y - e2.p1.Y;

            float P1P3_Y = e2.p1.Y - e1.p1.Y;
            float P1P4_Y = e2.p2.Y - e1.p1.Y;

            float P3P1_Y = e1.p1.Y - e2.p1.Y;
            float P3P2_Y = e1.p2.Y - e2.p1.Y;

            float v1 = P3P4_X * P3P1_Y - P3P4_Y * P3P1_X;
            float v2 = P3P4_X * P3P2_Y - P3P4_Y * P3P2_X;
            float v3 = P1P2_X * P1P3_Y - P1P2_Y * P1P3_X;
            float v4 = P1P2_X * P1P4_Y - P1P2_Y * P1P4_X;

            int v1v2 = Math.Sign(v1) * Math.Sign(v2);
            int v3v4 = Math.Sign(v3) * Math.Sign(v4);

            if (v1v2 < 0 && v3v4 < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsPointInPoly(Point p, Polygon2D poly)
        {
            PointF p1 = poly.points[0];
            PointF p2 = poly.points[1];
            Point pp = new Point(p.X + 5000, p.Y);
            Edge2D e = new Edge2D(p, pp);
            int crs = 0;
            int cnt = poly.points.Count();

            for (int i = 1; i < cnt; i++)
            {
                Edge2D e1 = new Edge2D(p1, p2);

                Point p_t = Calc_X(e, e1);

                if (p_t == p) return true;

                if (Is_X_Edge(e, e1)) crs += 1;

                p1 = poly.points[i];
                p2 = poly.points[(i + 1) % cnt]; ;
            }

            return (crs % 2) == 0 ? false : true;
        }

        public static string LoadMesh(ref Mesh mesh)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();


            openFileDialog1.Filter = "obj files (*.obj)|*.obj";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            string ans = "object";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = openFileDialog1.FileName;
                string[] text = File.ReadAllLines(filename);
                mesh = new Mesh();
                int cnt = 0;
                foreach (string x in text)
                {
                    if (x.StartsWith("o "))
                    {
                        ans = x.Remove(0, 2);
                    }
                    if (x.StartsWith("v "))
                    {
                        string[] s = x.Remove(0, 2).Split(' ');
                        Point3D point = new Point3D();
                        point.X = Double.Parse(s[0], new CultureInfo("en-us"));
                        point.Y = Double.Parse(s[1], new CultureInfo("en-us"));
                        point.Z = Double.Parse(s[2], new CultureInfo("en-us"));
                        point.index = cnt;
                        mesh.points.Add(point);
                        cnt++;
                    }

                    if (x.StartsWith("f "))
                    {
                        string[] s = x.Remove(0, 2).Split(' ');
                        List<Point3D> l = new List<Point3D>();
                        foreach (string s1 in s)
                        {
                            l.Add(mesh.points[int.Parse(s1.Split('/')[0]) - 1]);
                        }

                        Polygon poly = new Polygon(l);
                        mesh.faces.Add(poly);

                    }
                }

            }
            return ans;
        }

        public class Point3D : IComparable<Point3D>
        {
            public double X;
            public double Y;
            public double Z;
            public double W;
            public Point UV;
            public int index;
            public Point3D(double x, double y, double z, double w, int ind)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
                index = ind;
            }
            public Point3D(double x, double y, double z, int ind)
            {
                X = x;
                Y = y;
                Z = z;
                index = ind;
            }
            public Point3D(double x, double y, double z, int ind, int UX, int UY)
            {
                X = x;
                Y = y;
                Z = z;
                UV = new Point(UX, UY);
                index = ind;
            }
            public Point3D(double x, double y, double z, int ind, Point UVcoord)
            {
                X = x;
                Y = y;
                Z = z;
                UV = new Point(UVcoord.X, UVcoord.Y);
                index = ind;
            }
            public Point3D(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
                UV = new Point(0, 0);
                index = 0;
            }
            public Point3D()
            {
                X = 0;
                Y = 0;
                Z = 0;
                UV = new Point(0,0);
                index = 0;
            }
            public Point3D(Point3D p)
            {
                X = p.X;
                Y = p.Y;
                Z = p.Z;
                UV = p.UV;
                index = p.index;
            }
            public int CompareTo(Point3D that)
            {
                if (X < that.X) return -1;
                if (Y < that.Y) return -1;
                if (X == that.X && Y == that.Y) return 0;
                return 1;
            }
        }
        public class Edge
        {
            public Point3D p1;
            public Point3D p2;
            public Edge(Point3D pp1, Point3D pp2)
            {
                p1 = pp1;
                p2 = pp2;
            }
            public Edge()
            {
                p1 = new Point3D();
                p2 = new Point3D();
            }
        }

        public class Edge2D
        {
            public PointF p1;
            public PointF p2;
            public Edge2D(PointF pp1, PointF pp2)
            {
                p1 = pp1;
                p2 = pp2;
            }
            public Edge2D()
            {
                p1 = new PointF();
                p2 = new PointF();
            }
        }

        public class Polygon
        {
            public List<Point3D> points;
            public Polygon()
            {
                points = new List<Point3D>();
            }
            public Polygon(List<Point3D> l)
            {
                points = new List<Point3D>();
                foreach (Point3D p in l)
                {
                    Point3D t = new Point3D(p);
                    points.Add(t);
                }
            }
        }

        public class Polygon2D
        {
            public List<PointF> points;
            public Polygon2D()
            {
                points = new List<PointF>();
            }
            public Polygon2D(List<PointF> l)
            {
                points = new List<PointF>();
                foreach (PointF p in l)
                {
                    PointF t = new PointF(p.X, p.Y);
                    points.Add(t);
                }
            }
        }

        public class Mesh
        {
            public List<Point3D> points;
            public SortedDictionary<int, List<int>> connections;
            public List<Edge> edges;
            public List<Polygon> faces;
            public Mesh()
            {
                points = new List<Point3D>();
                connections = new SortedDictionary<int, List<int>>();
                edges = new List<Edge>();
                faces = new List<Polygon>();
            }
            public Mesh(List<Point3D> l, SortedDictionary<int, List<int>> sd, List<Edge> le, List<Polygon> lf)
            {
                points = new List<Point3D>();
                connections = new SortedDictionary<int, List<int>>();
                edges = new List<Edge>();
                faces = new List<Polygon>();
                foreach (Point3D p in l)
                {
                    Point3D p3D = new Point3D(p);
                    points.Add(p3D);
                    List<int> temp = new List<int>();
                    if (sd.ContainsKey(p.index))
                        foreach (int pp in sd[p.index])
                        {
                            temp.Add(pp);
                        }
                    connections.Add(p.index, temp);
                }
                if (le.Count() == 0)
                {
                    int countP = points.Count();
                    bool[,] flags = new bool[countP, countP];

                    foreach (Point3D p1 in points)
                    {
                        int p1ind = p1.index;
                        foreach (int p2ind in connections[p1ind])
                        {
                            if (!flags[p1ind, p2ind])
                            {
                                flags[p1ind, p2ind] = true;
                                flags[p2ind, p1ind] = true;
                                Point3D t1 = new Point3D(p1);
                                Point3D t2 = new Point3D(points[p2ind]);
                                edges.Add(new Edge(t1, t2));
                            }
                        }
                    }
                }
                else
                {
                    foreach (Edge e in le)
                    {
                        Point3D t1 = new Point3D(e.p1);
                        Point3D t2 = new Point3D(e.p2);
                        edges.Add(new Edge(t1, t2));
                    }
                }
                if (lf.Count != 0)
                {
                    foreach (Polygon p in lf)
                    {
                        faces.Add(new Polygon(p.points));
                    }
                }

            }
            public Mesh(Mesh oldM)
            {
                var l = oldM.points;
                var sd = oldM.connections;
                var le = oldM.edges;
                var lf = oldM.faces;
                points = new List<Point3D>();
                connections = new SortedDictionary<int, List<int>>();
                edges = new List<Edge>();
                faces = new List<Polygon>();
                foreach (Point3D p in l)
                {
                    Point3D p3D = new Point3D(p);
                    points.Add(p3D);
                    List<int> temp = new List<int>();
                    if (sd.ContainsKey(p.index))
                        foreach (int pp in sd[p.index])
                        {
                            temp.Add(pp);
                        }
                    connections.Add(p.index, temp);
                }
                if (le.Count() == 0)
                {
                    int countP = points.Count();
                    bool[,] flags = new bool[countP, countP];
                    foreach (Point3D p1 in points)
                    {
                        int p1ind = p1.index;
                        foreach (int p2ind in connections[p1ind])
                        {
                            if (!flags[p1ind, p2ind])
                            {
                                flags[p1ind, p2ind] = true;
                                flags[p2ind, p1ind] = true;
                                Point3D t1 = new Point3D(p1);
                                Point3D t2 = new Point3D(points[p2ind]);
                                edges.Add(new Edge(t1, t2));
                            }
                        }
                    }
                }
                else
                {
                    foreach (Edge e in le)
                    {
                        Point3D t1 = new Point3D(e.p1);
                        Point3D t2 = new Point3D(e.p2);
                        edges.Add(new Edge(t1, t2));
                    }
                }
                if (lf.Count != 0)
                {
                    foreach (Polygon p in lf)
                    {
                        faces.Add(new Polygon(p.points));
                    }
                }
            }

            public void Clear()
            {
                points.Clear();
                connections.Clear();
                edges.Clear();
                faces.Clear();
            }
        }
    }
    
}
