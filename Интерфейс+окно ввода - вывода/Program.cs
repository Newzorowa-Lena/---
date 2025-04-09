using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Data;
using System.Drawing;

class FlightSimulator
{
    public double Mass { get; set; }
    public double Force { get; set; }
    public double Angle { get; set; }
    public double DeltaTime { get; set; } = 0.05;
    private double g = 9.8;
    private double airDensity = 1.225;
    private double dragCoefficient = 0.47;
    private double crossSectionalArea = 0.01;

    public double InitialSpeed { get; private set; }
    public double FinalSpeed { get; private set; }

    public delegate void FlightCompletedEventHandler(object sender, EventArgs e);
    public event FlightCompletedEventHandler OnFlightCompleted;

    public FlightSimulator(double mass, double force, double angle)
    {
        Mass = mass;
        Force = force;
        Angle = angle;
        InitialSpeed = Force / Mass;
    }

    public List<(double time, double x, double y, double speed)> CalculateTrajectory()
    {
        List<(double time, double x, double y, double speed)> trajectory = new List<(double time, double x, double y, double speed)>();

        double angleRad = Angle * Math.PI / 180.0;
        double vX = InitialSpeed * Math.Cos(angleRad);
        double vY = InitialSpeed * Math.Sin(angleRad);

        double time = 0;
        double x = 0, y = 0;

        while (y >= 0)
        {
            trajectory.Add((time, x, y, Math.Sqrt(vX * vX + vY * vY)));

            double speed = Math.Sqrt(vX * vX + vY * vY);
            double dragForce = 0.5 * airDensity * dragCoefficient * crossSectionalArea * speed * speed;
            double dragAcceleration = dragForce / Mass;

            double dragAx = dragAcceleration * (vX / speed);
            double dragAy = dragAcceleration * (vY / speed);

            vX -= dragAx * DeltaTime;
            vY -= (g + dragAy) * DeltaTime;

            x += vX * DeltaTime;
            y += vY * DeltaTime;
            time += DeltaTime;
        }

        FinalSpeed = Math.Sqrt(vX * vX + vY * vY);
        OnFlightCompleted?.Invoke(this, EventArgs.Empty);
        return trajectory;
    }

    public double CalculateImpactForce(double contactTime = 0.1)
    {
        return (Mass * FinalSpeed) / contactTime;
    }
}

class Obstacle
{
    public string Material { get; }
    public int Durability { get; private set; }

    public Obstacle(string material, int durability)
    {
        Material = material;
        Durability = durability;
    }

    public bool TakeDamage(double force)
    {
        Durability -= (int)force;
        return Durability <= 0;
    }
}

class Pig
{
    public string Name { get; }
    public int Health { get; private set; }

    public Pig(string name, int health)
    {
        Name = name;
        Health = health;
    }

    public bool TakeDamage(double force)
    {
        Health -= (int)force;
        return Health <= 0;
    }
}

class MainForm : Form
{
    private ComboBox birdComboBox;
    private TextBox forceTextBox;
    private TextBox angleTextBox;
    private ComboBox attackMethodComboBox;
    private Button simulateButton;
    private RichTextBox outputTextBox;
    private DataGridView trajectoryDataGridView;

    public MainForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "Angry Birds Flight Simulator";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Создаем элементы управления
        Label birdLabel = new Label { Text = "Выберите птицу:", Left = 20, Top = 20 };
        birdComboBox = new ComboBox { Left = 150, Top = 20, Width = 150 };
        birdComboBox.Items.AddRange(new object[] { "the Blues", "Stella", "Red" });
        birdComboBox.SelectedIndex = 0;

        Label forceLabel = new Label { Text = "Сила броска (Н):", Left = 20, Top = 50 };
        forceTextBox = new TextBox { Left = 150, Top = 50, Width = 150 };

        Label angleLabel = new Label { Text = "Угол броска (1-89°):", Left = 20, Top = 80 };
        angleTextBox = new TextBox { Left = 150, Top = 80, Width = 150 };

        Label attackMethodLabel = new Label { Text = "Метод атаки:", Left = 20, Top = 110 };
        attackMethodComboBox = new ComboBox { Left = 150, Top = 110, Width = 150 };
        attackMethodComboBox.Items.AddRange(new object[] { "Обычный удар", "Разрывной удар", "Ускорение" });
        attackMethodComboBox.SelectedIndex = 0;

        simulateButton = new Button { Text = "Запустить симуляцию", Left = 20, Top = 140, Width = 280 };
        simulateButton.Click += SimulateButton_Click;

        outputTextBox = new RichTextBox { Left = 20, Top = 180, Width = 360, Height = 350, ReadOnly = true };

        trajectoryDataGridView = new DataGridView { Left = 400, Top = 20, Width = 360, Height = 510 };
        trajectoryDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Добавляем элементы на форму
        this.Controls.AddRange(new Control[] {
            birdLabel, birdComboBox,
            forceLabel, forceTextBox,
            angleLabel, angleTextBox,
            attackMethodLabel, attackMethodComboBox,
            simulateButton,
            outputTextBox,
            trajectoryDataGridView
        });
    }

    private void SimulateButton_Click(object sender, EventArgs e)
    {
        outputTextBox.Clear();

        // Проверка ввода
        if (!double.TryParse(forceTextBox.Text, out double force) || force <= 0)
        {
            MessageBox.Show("Некорректное значение силы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!double.TryParse(angleTextBox.Text, out double angle) || angle <= 0 || angle >= 90)
        {
            MessageBox.Show("Некорректное значение угла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Массы птиц
        Dictionary<string, double> birds = new Dictionary<string, double>
        {
            {"the Blues", 0.7},
            {"Stella", 1.0},
            {"Red", 1.5}
        };

        string selectedBird = birdComboBox.SelectedItem.ToString();
        double mass = birds[selectedBird];
        int attackMethod = attackMethodComboBox.SelectedIndex + 1;

        // Запуск симуляции
        FlightSimulator simulator = new FlightSimulator(mass, force, angle);
        simulator.OnFlightCompleted += (senderObj, evt) =>
            outputTextBox.Invoke((Action)(() => outputTextBox.AppendText("\nПолёт завершён! Птица достигла земли.\n")));

        var trajectory = simulator.CalculateTrajectory();
        double impactForce = simulator.CalculateImpactForce();

        if (attackMethod == 2) // Разрывной удар
        {
            impactForce *= 1.5;
            outputTextBox.AppendText("Птица взрывается при ударе, увеличивая урон!\n");
        }
        else if (attackMethod == 3) // Ускорение
        {
            impactForce *= 1.3;
            outputTextBox.AppendText("Птица использует ускорение перед ударом!\n");
        }

        outputTextBox.AppendText($"\nСила удара при падении: {impactForce:F2} Н\n");

        // Создаем препятствия и свиней
        List<Obstacle> obstacles = new List<Obstacle> { new Obstacle("Wood", 30), new Obstacle("Stone", 80) };
        List<Pig> pigs = new List<Pig> { new Pig("Green Pig", 50), new Pig("Big Pig", 100) };

        // Наносим урон
        foreach (var obstacle in obstacles)
        {
            if (obstacle.TakeDamage(impactForce))
            {
                outputTextBox.AppendText($"{obstacle.Material} разрушено!\n");
            }
            else
            {
                outputTextBox.AppendText($"{obstacle.Material} получил {impactForce:F2} Н урона! Прочность: {obstacle.Durability}\n");
            }
        }

        foreach (var pig in pigs)
        {
            if (pig.TakeDamage(impactForce))
            {
                outputTextBox.AppendText($"{pig.Name} уничтожен!\n");
            }
            else
            {
                outputTextBox.AppendText($"{pig.Name} получил {impactForce:F2} Н урона! Осталось {pig.Health} HP.\n");
            }
        }

        // Сохраняем данные в файл
        string filePath = "flight_data.csv";
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Время (с),X (м),Y (м),Скорость (м/с)");
                foreach (var point in trajectory)
                {
                    writer.WriteLine($"{point.time:F1},{point.x:F2},{point.y:F3},{point.speed:F2}");
                }
            }
            outputTextBox.AppendText($"\nДанные сохранены в файл: {filePath}\n");
        }
        catch (Exception ex)
        {
            outputTextBox.AppendText($"\nОшибка при сохранении файла: {ex.Message}\n");
        }

        // Отображаем траекторию в DataGridView
        DisplayTrajectory(trajectory);
    }

    private void DisplayTrajectory(List<(double time, double x, double y, double speed)> trajectory)
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("Время (с)", typeof(double));
        dt.Columns.Add("X (м)", typeof(double));
        dt.Columns.Add("Y (м)", typeof(double));
        dt.Columns.Add("Скорость (м/с)", typeof(double));

        foreach (var point in trajectory)
        {
            dt.Rows.Add(point.time, point.x, point.y, point.speed);
        }

        trajectoryDataGridView.DataSource = dt;
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}