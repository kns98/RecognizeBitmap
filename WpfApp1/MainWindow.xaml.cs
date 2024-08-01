using OpenCvSharp;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace WpfApp1
{


    public partial class MainWindow : System.Windows.Window
    {
        private Model3DGroup _modelGroup;

        public MainWindow()
        {
            InitializeComponent();
            SetupViewport();

            Mat bitmap = LoadBitmap("path_to_image.bmp");
            string deformationType = RecognizeDeformationType(bitmap);
            ApplyDeformation(deformationType);
            string xml3d = ExportToXML3D();
            System.IO.File.WriteAllText("output.xml3d", xml3d);
        }

        private void SetupViewport()
        {
            _modelGroup = new Model3DGroup();
            // Here we create a simple 3D model for demonstration purposes
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(-1, -1, 0));
            mesh.Positions.Add(new Point3D(1, -1, 0));
            mesh.Positions.Add(new Point3D(1, 1, 0));
            mesh.Positions.Add(new Point3D(-1, 1, 0));
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(0);
            var geometryModel = new GeometryModel3D(mesh, new DiffuseMaterial(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)));
            _modelGroup.Children.Add(geometryModel);
        }

        private Mat LoadBitmap(string path)
        {
            return Cv2.ImRead(path, ImreadModes.Color);
        }

        private string RecognizeDeformationType(Mat bitmap)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(bitmap, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(5, 5), 0);
            Mat cannyOutput = new Mat();
            Cv2.Canny(gray, cannyOutput, 50, 150);

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(cannyOutput, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var approx = Cv2.ApproxPolyDP(contour, 0.04 * Cv2.ArcLength(contour, true), true);

                if (approx.Length == 3)
                {
                    return "triangle";
                }
                else if (approx.Length == 4)
                {
                    var rect = Cv2.BoundingRect(approx);
                    float aspectRatio = (float)rect.Width / rect.Height;

                    if (aspectRatio >= 0.95 && aspectRatio <= 1.05)
                    {
                        return "square";
                    }
                    else
                    {
                        return "rectangle";
                    }
                }
                else if (approx.Length == 5)
                {
                    return "pentagon";
                }
                else if (approx.Length == 6)
                {
                    return "hexagon";
                }
                else if (approx.Length == 8)
                {
                    return "octagon";
                }
                else if (approx.Length > 8)
                {
                    if (IsCircle(contour))
                    {
                        return "circle";
                    }
                    if (IsEllipse(contour))
                    {
                        return "ellipse";
                    }
                }
            }

            return "unknown";
        }

        private bool IsCircle(OpenCvSharp.Point[] contour)
        {
            // Calculate the area of the contour
            double area = Cv2.ContourArea(contour);

            // Calculate the minimum enclosing circle
            Cv2.MinEnclosingCircle(contour, out OpenCvSharp.Point2f center, out float radius);

            // Calculate the circularity
            double circularity = area / (Math.PI * Math.Pow(radius, 2));

            // Check if the circularity is within an acceptable range
            return Math.Abs(1 - circularity) <= 0.2;
        }

        private bool IsEllipse(OpenCvSharp.Point[] contour)
        {
            // Fit an ellipse to the contour
            var rect = Cv2.FitEllipse(contour);

            // Create an ellipse using Ellipse2Poly
            var ellipse = Cv2.Ellipse2Poly(
                new OpenCvSharp.Point((int)rect.Center.X, (int)rect.Center.Y),
                new OpenCvSharp.Size((int)(rect.Size.Width / 2), (int)(rect.Size.Height / 2)),
                (int)rect.Angle, 0, 360, 1);

            // Calculate the match between the ellipse and the contour
            double match = Cv2.MatchShapes(ellipse, contour, OpenCvSharp.ShapeMatchModes.I3);

            // Return true if the match is below the threshold
            return match < 0.2;
        }


        private void ApplyDeformation(string deformationType)
        {
            switch (deformationType.ToLower())
            {
                case "triangle":
                    ApplyTriangleDeformation();
                    break;
                case "square":
                    ApplySquareDeformation();
                    break;
                case "rectangle":
                    ApplyRectangleDeformation();
                    break;
                case "pentagon":
                    ApplyPentagonDeformation();
                    break;
                case "hexagon":
                    ApplyHexagonDeformation();
                    break;
                case "octagon":
                    ApplyOctagonDeformation();
                    break;
                case "circle":
                    ApplyCircleDeformation();
                    break;
                case "ellipse":
                    ApplyEllipseDeformation();
                    break;
                default:
                    MessageBox.Show("Unknown deformation type.");
                    break;
            }
        }

        private void ApplyTriangleDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Sin(point.X) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplySquareDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Cos(point.X) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplyRectangleDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Tan(point.X) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplyPentagonDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Sin(point.X + point.Y) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplyHexagonDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Cos(point.X + point.Y) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplyOctagonDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Tan(point.X + point.Y) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplyCircleDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Sin(point.X * point.Y) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private void ApplyEllipseDeformation()
        {
            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var points = mesh.Positions.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    points[i] = new Point3D(point.X, point.Y, point.Z + Math.Cos(point.X * point.Y) * 0.1);
                }

                mesh.Positions = new Point3DCollection(points);
            }
        }

        private string ExportToXML3D()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("xml3d");
            doc.AppendChild(root);

            XmlElement defs = doc.CreateElement("defs");
            root.AppendChild(defs);

            foreach (GeometryModel3D geometryModel in _modelGroup.Children)
            {
                var mesh = (MeshGeometry3D)geometryModel.Geometry;
                var positions = mesh.Positions.ToArray();
                var indices = mesh.TriangleIndices.ToArray();

                XmlElement meshElem = doc.CreateElement("mesh");
                meshElem.SetAttribute("type", "triangles");

                XmlElement positionsElem = doc.CreateElement("float3");
                positionsElem.SetAttribute("name", "position");
                positionsElem.InnerText = string.Join(" ", positions.Select(p => $"{p.X} {p.Y} {p.Z}"));
                meshElem.AppendChild(positionsElem);

                XmlElement indicesElem = doc.CreateElement("int");
                indicesElem.SetAttribute("name", "index");
                indicesElem.InnerText = string.Join(" ", indices);
                meshElem.AppendChild(indicesElem);

                defs.AppendChild(meshElem);
            }

            return doc.OuterXml;
        }
    }

}