using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerGraphicsLab1
{
    public partial class Form1 : Form
    {
        // Режимы работы
        private enum Mode { Idle, PolygonInput, RouteInput, RouteEdit }
        private Mode currentMode = Mode.Idle;

        // Данные
        private List<Point> polygonPoints = new List<Point>();
        private List<Point> routePoints = new List<Point>();

        // Для анимации
        private bool isAnimating = false;
        private double t = 0.0;                     // параметр положения на маршруте [0,1]
        private float shapeAngle = 0.0f;            // текущий угол поворота фигуры
        private bool shapeRotationEnabled = false;
        private float rotationSpeed = 0.05f;
        private int direction = 1;                   // 1 - вперед, -1 - назад (для разомкнутого)

        // Длины сегментов маршрута
        private double[] segmentLengths;
        private double totalRouteLength = 0.0;

        // Редактирование маршрута
        private int draggingVertexIndex = -1;
        private int selectedVertexIndex = -1;        // выделенная вершина для удаления

        // Таймер анимации
        private Timer animationTimer = new Timer { Interval = 30 };

        // Буфер для рисования
        private Bitmap canvasBitmap;
        private PictureBox pictureBox;

        // Флаг замкнутости маршрута
        private bool routeClosed = true;

        // Элементы управления
        private Button btnPolygon, btnRoute, btnEditRoute, btnAnimate, btnStop, btnReset, btnRotateRoute;
        private Button btnDeleteVertex;
        private CheckBox chkRotateShape;
        private CheckBox chkRouteClosed;
        private TrackBar tbRotationSpeed;
        private TrackBar tbRouteAngle;
        private Label lblRotationSpeedValue;
        private Label lblRouteAngleValue;
        private Label lblStatus;

        public Form1()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Настройка формы
            this.Text = "Лабораторная работа №1: Алгоритмы рисования линий";
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            // PictureBox для рисования
            pictureBox = new PictureBox
            {
                Location = new Point(10, 50),
                Size = new Size(800, 600),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.MouseDoubleClick += PictureBox_MouseDoubleClick;
            pictureBox.Paint += PictureBox_Paint;
            pictureBox.MouseClick += PictureBox_MouseClick;

            // Кнопки
            btnPolygon = new Button { Text = "Многоугольник", Location = new Point(820, 50), Size = new Size(150, 30) };
            btnPolygon.Click += (s, e) => SetMode(Mode.PolygonInput);

            btnRoute = new Button { Text = "Маршрут", Location = new Point(820, 90), Size = new Size(150, 30) };
            btnRoute.Click += (s, e) => SetMode(Mode.RouteInput);

            btnEditRoute = new Button { Text = "Редакт. маршрут", Location = new Point(820, 130), Size = new Size(150, 30) };
            btnEditRoute.Click += (s, e) => SetMode(Mode.RouteEdit);

            // Кнопка удаления вершины
            btnDeleteVertex = new Button
            {
                Text = "Удалить точку",
                Location = new Point(820, 165),
                Size = new Size(150, 30),
                Enabled = false
            };
            btnDeleteVertex.Click += BtnDeleteVertex_Click;

            btnAnimate = new Button { Text = "Анимация", Location = new Point(820, 200), Size = new Size(70, 30) };
            btnAnimate.Click += BtnAnimate_Click;

            btnStop = new Button { Text = "Стоп", Location = new Point(900, 200), Size = new Size(70, 30) };
            btnStop.Click += (s, e) => StopAnimation();

            btnReset = new Button { Text = "Сброс", Location = new Point(820, 240), Size = new Size(150, 30) };
            btnReset.Click += BtnReset_Click;

            // Чекбокс для замкнутого/разомкнутого маршрута
            chkRouteClosed = new CheckBox
            {
                Text = "Замкнутый маршрут",
                Location = new Point(820, 275),
                Size = new Size(120, 20),
                Checked = true
            };
            chkRouteClosed.CheckedChanged += (s, e) =>
            {
                routeClosed = chkRouteClosed.Checked;
                ResetAnimationParameters();
                RecalculateRouteLengths();
                RedrawScene();
            };

            // Вращение фигуры
            chkRotateShape = new CheckBox { Text = "Вращать фигуру", Location = new Point(820, 300), Size = new Size(120, 20) };
            chkRotateShape.CheckedChanged += (s, e) => shapeRotationEnabled = chkRotateShape.Checked;

            // ПОЛЗУНОК для скорости вращения фигуры
            Label lblRotationSpeed = new Label { Text = "Скорость вращения:", Location = new Point(820, 325), Size = new Size(120, 20) };

            tbRotationSpeed = new TrackBar
            {
                Location = new Point(820, 345),
                Size = new Size(150, 45),
                Minimum = -50,
                Maximum = 50,
                Value = 5,
                TickFrequency = 10,
                SmallChange = 1,
                LargeChange = 5
            };
            tbRotationSpeed.ValueChanged += TbRotationSpeed_ValueChanged;

            lblRotationSpeedValue = new Label
            {
                Text = "0.05",
                Location = new Point(970, 345),
                Size = new Size(40, 20)
            };

            // Вращение маршрута
            Label lblRouteAngleText = new Label { Text = "Угол поворота маршрута:", Location = new Point(820, 390), Size = new Size(150, 20) };

            tbRouteAngle = new TrackBar
            {
                Location = new Point(820, 410),
                Size = new Size(150, 45),
                Minimum = -360,
                Maximum = 360,
                Value = 0,
                TickFrequency = 45,
                SmallChange = 5,
                LargeChange = 15
            };
            tbRouteAngle.ValueChanged += TbRouteAngle_ValueChanged;

            lblRouteAngleValue = new Label
            {
                Text = "0°",
                Location = new Point(970, 410),
                Size = new Size(40, 20)
            };

            btnRotateRoute = new Button { Text = "Повернуть", Location = new Point(820, 450), Size = new Size(150, 30) };
            btnRotateRoute.Click += BtnRotateRoute_Click;

            // Строка состояния
            lblStatus = new Label { Location = new Point(10, 10), Size = new Size(800, 30), Text = "Режим: ожидание" };

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[] {
                pictureBox, btnPolygon, btnRoute, btnEditRoute, btnDeleteVertex, btnAnimate, btnStop, btnReset,
                chkRouteClosed, chkRotateShape, lblRotationSpeed, tbRotationSpeed, lblRotationSpeedValue,
                lblRouteAngleText, tbRouteAngle, lblRouteAngleValue, btnRotateRoute, lblStatus
            });

            // Таймер
            animationTimer.Tick += AnimationTimer_Tick;

            // Создаём буфер для рисования
            canvasBitmap = new Bitmap(pictureBox.Width, pictureBox.Height);
            pictureBox.Image = canvasBitmap;
        }

        // Сброс параметров анимации
        private void ResetAnimationParameters()
        {
            t = 0.0;
            direction = 1;
        }

        // Обработчик клика мыши для выделения вершины
        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (currentMode == Mode.RouteEdit)
            {
                selectedVertexIndex = FindNearestRouteVertex(e.Location, 10);
                btnDeleteVertex.Enabled = selectedVertexIndex != -1;
                RedrawScene();
            }
        }

        // Обработчик кнопки удаления вершины
        private void BtnDeleteVertex_Click(object sender, EventArgs e)
        {
            if (selectedVertexIndex != -1 && currentMode == Mode.RouteEdit)
            {
                routePoints.RemoveAt(selectedVertexIndex);
                selectedVertexIndex = -1;
                btnDeleteVertex.Enabled = false;
                RecalculateRouteLengths();
                ResetAnimationParameters();

                if (routePoints.Count < 2)
                {
                    StopAnimation();
                }

                UpdateStatusLabel();
                RedrawScene();
            }
        }

        // Обработчик Paint для рисования точек фигуры
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            // Рисуем точки многоугольника
            if (polygonPoints.Count > 0)
            {
                for (int i = 0; i < polygonPoints.Count; i++)
                {
                    e.Graphics.FillEllipse(Brushes.Green, polygonPoints[i].X - 3, polygonPoints[i].Y - 3, 6, 6);

                    using (Font font = new Font("Arial", 8))
                    {
                        e.Graphics.DrawString((i + 1).ToString(), font, Brushes.Black,
                            polygonPoints[i].X + 5, polygonPoints[i].Y - 10);
                    }
                }
            }

            // Рисуем точки маршрута
            if (routePoints.Count > 0)
            {
                for (int i = 0; i < routePoints.Count; i++)
                {
                    Brush brush = (i == selectedVertexIndex && currentMode == Mode.RouteEdit) ?
                                  Brushes.Orange : Brushes.Red;

                    e.Graphics.FillEllipse(brush, routePoints[i].X - 4, routePoints[i].Y - 4, 8, 8);

                    using (Font font = new Font("Arial", 8))
                    {
                        e.Graphics.DrawString((i + 1).ToString(), font, Brushes.Black,
                            routePoints[i].X + 5, routePoints[i].Y - 10);
                    }
                }
            }
        }

        // Обработчик изменения ползунка скорости вращения
        private void TbRotationSpeed_ValueChanged(object sender, EventArgs e)
        {
            rotationSpeed = tbRotationSpeed.Value / 100f;
            lblRotationSpeedValue.Text = rotationSpeed.ToString("0.00");
        }

        // Обработчик изменения ползунка угла поворота маршрута
        private void TbRouteAngle_ValueChanged(object sender, EventArgs e)
        {
            lblRouteAngleValue.Text = tbRouteAngle.Value + "°";
        }

        // Установка режима
        private void SetMode(Mode newMode)
        {
            currentMode = newMode;
            draggingVertexIndex = -1;
            selectedVertexIndex = -1;
            btnDeleteVertex.Enabled = false;
            UpdateStatusLabel();
            RedrawScene();
        }

        private void UpdateStatusLabel()
        {
            string modeStr;
            if (currentMode == Mode.PolygonInput)
                modeStr = "Рисование многоугольника (клики мыши)";
            else if (currentMode == Mode.RouteInput)
                modeStr = "Рисование маршрута (клики мыши)";
            else if (currentMode == Mode.RouteEdit)
                modeStr = "Редактирование маршрута (клик для выделения, кнопка для удаления)";
            else
                modeStr = "Ожидание";

            string directionStr = "";
            if (!routeClosed && isAnimating)
            {
                directionStr = direction > 0 ? " →" : " ←";
            }

            lblStatus.Text = $"Режим: {modeStr} | Точек маршрута: {routePoints.Count} | " +
                           $"Точек фигуры: {polygonPoints.Count} | Анимация: {(isAnimating ? "ДА" + directionStr : "НЕТ")} | " +
                           $"Маршрут: {(routeClosed ? "замкнутый" : "разомкнутый")}";
        }

        // Пересчёт длин сегментов маршрута
        private void RecalculateRouteLengths()
        {
            if (routePoints.Count < 2)
            {
                segmentLengths = null;
                totalRouteLength = 0;
                return;
            }

            int count = routePoints.Count;
            int segmentCount = routeClosed ? count : count - 1;
            segmentLengths = new double[segmentCount];
            totalRouteLength = 0;

            for (int i = 0; i < segmentCount; i++)
            {
                int j = (i + 1) % count;
                double dx = routePoints[j].X - routePoints[i].X;
                double dy = routePoints[j].Y - routePoints[i].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                segmentLengths[i] = len;
                totalRouteLength += len;
            }
        }

        // Получить точку на маршруте по параметру t (работает и для замкнутого, и для разомкнутого)
        private PointF GetPointOnRoute(double t)
        {
            if (routePoints.Count < 2) return PointF.Empty;
            if (totalRouteLength == 0) return routePoints[0];

            double targetDist = t * totalRouteLength;
            double accumulated = 0;
            int segmentCount = routeClosed ? routePoints.Count : routePoints.Count - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                int j = (i + 1) % routePoints.Count; // для замкнутого последний сегмент замыкается на первую вершину
                double len = segmentLengths[i];
                if (targetDist <= accumulated + len || i == segmentCount - 1)
                {
                    double segT = (targetDist - accumulated) / len;
                    float x = (float)(routePoints[i].X + segT * (routePoints[j].X - routePoints[i].X));
                    float y = (float)(routePoints[i].Y + segT * (routePoints[j].Y - routePoints[i].Y));
                    return new PointF(x, y);
                }
                accumulated += len;
            }
            return routePoints[0];
        }

        // Поиск ближайшей вершины маршрута
        private int FindNearestRouteVertex(Point mousePos, int threshold)
        {
            int bestIndex = -1;
            double bestDist = threshold;
            for (int i = 0; i < routePoints.Count; i++)
            {
                double dist = Math.Sqrt(Math.Pow(mousePos.X - routePoints[i].X, 2) + Math.Pow(mousePos.Y - routePoints[i].Y, 2));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        // Поворот точки вокруг центра
        private PointF RotatePoint(PointF point, PointF center, float angleRad)
        {
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);
            return new PointF(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
        }

        // Алгоритм Брезенхема
        private void DrawLineBresenham(int x0, int y0, int x1, int y1, Color color, Bitmap bitmap)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < bitmap.Width && y0 >= 0 && y0 < bitmap.Height)
                    bitmap.SetPixel(x0, y0, color);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        // Перерисовка всей сцены
        private void RedrawScene()
        {
            using (Graphics g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.White);
            }

            // Рисуем маршрут (красным)
            if (routePoints.Count >= 2)
            {
                int segments = routeClosed ? routePoints.Count : routePoints.Count - 1;

                for (int i = 0; i < segments; i++)
                {
                    int j = (i + 1) % routePoints.Count;
                    DrawLineBresenham(routePoints[i].X, routePoints[i].Y, routePoints[j].X, routePoints[j].Y, Color.Red, canvasBitmap);
                }
            }

            // Рисуем многоугольник (зеленым)
            if (polygonPoints.Count >= 2)
            {
                for (int i = 0; i < polygonPoints.Count - 1; i++)
                {
                    DrawLineBresenham(polygonPoints[i].X, polygonPoints[i].Y, polygonPoints[i + 1].X, polygonPoints[i + 1].Y, Color.Green, canvasBitmap);
                }
                if (polygonPoints.Count > 2)
                {
                    DrawLineBresenham(polygonPoints[polygonPoints.Count - 1].X, polygonPoints[polygonPoints.Count - 1].Y,
                                    polygonPoints[0].X, polygonPoints[0].Y, Color.Green, canvasBitmap);
                }
            }

            // Рисуем движущийся многоугольник (синим)
            if (polygonPoints.Count >= 2 && routePoints.Count >= 2)
            {
                PointF center = new PointF(0, 0);
                foreach (Point p in polygonPoints)
                {
                    center.X += p.X;
                    center.Y += p.Y;
                }
                center.X /= polygonPoints.Count;
                center.Y /= polygonPoints.Count;

                PointF routePos = GetPointOnRoute(t);

                List<PointF> transformed = new List<PointF>();
                foreach (Point p in polygonPoints)
                {
                    float dx = p.X - center.X;
                    float dy = p.Y - center.Y;
                    float cos = (float)Math.Cos(shapeAngle);
                    float sin = (float)Math.Sin(shapeAngle);
                    float x = routePos.X + dx * cos - dy * sin;
                    float y = routePos.Y + dx * sin + dy * cos;
                    transformed.Add(new PointF(x, y));
                }

                for (int i = 0; i < transformed.Count; i++)
                {
                    int j = (i + 1) % transformed.Count;
                    DrawLineBresenham(
                        (int)transformed[i].X, (int)transformed[i].Y,
                        (int)transformed[j].X, (int)transformed[j].Y,
                        Color.Blue, canvasBitmap);
                }
            }

            pictureBox.Invalidate();
        }

        // Обработчики мыши
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentMode == Mode.RouteEdit)
            {
                draggingVertexIndex = FindNearestRouteVertex(e.Location, 5);
            }
            else if (currentMode == Mode.PolygonInput)
            {
                polygonPoints.Add(e.Location);
                RedrawScene();
            }
            else if (currentMode == Mode.RouteInput)
            {
                routePoints.Add(e.Location);
                RecalculateRouteLengths();
                ResetAnimationParameters();
                RedrawScene();
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingVertexIndex != -1 && currentMode == Mode.RouteEdit)
            {
                routePoints[draggingVertexIndex] = e.Location;
                RecalculateRouteLengths();
                ResetAnimationParameters();
                RedrawScene();
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            draggingVertexIndex = -1;
        }

        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (currentMode == Mode.PolygonInput || currentMode == Mode.RouteInput)
            {
                currentMode = Mode.Idle;
                UpdateStatusLabel();
                RedrawScene();
            }
        }

        // Анимация
        private void BtnAnimate_Click(object sender, EventArgs e)
        {
            if (routePoints.Count < 2) return;
            if (!isAnimating)
            {
                isAnimating = true;
                animationTimer.Start();
            }
            else
            {
                StopAnimation();
            }
            UpdateStatusLabel();
        }

        private void StopAnimation()
        {
            isAnimating = false;
            animationTimer.Stop();
            UpdateStatusLabel();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!isAnimating) return;

            if (routeClosed)
            {
                // Замкнутый маршрут: циклически увеличиваем t
                t += 0.005;
                if (t >= 1.0) t -= 1.0;
            }
            else
            {
                // Разомкнутый маршрут: движение вперёд-назад
                t += 0.005 * direction;
                if (t >= 1.0)
                {
                    t = 1.0;
                    direction = -1; // меняем направление
                }
                else if (t <= 0.0)
                {
                    t = 0.0;
                    direction = 1;  // меняем направление
                }
            }

            if (shapeRotationEnabled)
                shapeAngle += rotationSpeed;

            UpdateStatusLabel();
            RedrawScene();
        }

        // Вращение маршрута
        private void BtnRotateRoute_Click(object sender, EventArgs e)
        {
            if (routePoints.Count < 2) return;

            PointF center = new PointF(0, 0);
            foreach (Point p in routePoints)
            {
                center.X += p.X;
                center.Y += p.Y;
            }
            center.X /= routePoints.Count;
            center.Y /= routePoints.Count;

            double angleDegrees = tbRouteAngle.Value;
            float angle = (float)(angleDegrees * Math.PI / 180.0);

            for (int i = 0; i < routePoints.Count; i++)
            {
                PointF rotated = RotatePoint(routePoints[i], center, angle);
                routePoints[i] = new Point((int)rotated.X, (int)rotated.Y);
            }

            RecalculateRouteLengths();
            ResetAnimationParameters();
            RedrawScene();
        }

        // Сброс
        private void BtnReset_Click(object sender, EventArgs e)
        {
            StopAnimation();
            polygonPoints.Clear();
            routePoints.Clear();
            ResetAnimationParameters();
            segmentLengths = null;
            totalRouteLength = 0;
            currentMode = Mode.Idle;
            selectedVertexIndex = -1;
            btnDeleteVertex.Enabled = false;
            UpdateStatusLabel();
            RedrawScene();
        }
    }
}