using Timer = System.Windows.Forms.Timer;

namespace AnimalEvoApp;

public partial class Form1 : Form
{
	const int gridSize = 50;
	const int cellSize = 12;

	public enum CellType { Empty, Food, Animal }

	CellType[,] grid = new CellType[gridSize, gridSize];
	List<Animal> animals = new List<Animal>();
    List<Point> foodList = new List<Point>();
	Random rand = new Random();

	Timer timer = new Timer();

    Bitmap backgroundGrid;

    const int energy = 10;
    const int animalCount = 10;
    const int foodCount = 200;

    private int generation = 0;
    private List<Color> generationColors = new List<Color>
    {
        Color.Red,
        Color.Blue,
        Color.Green,
        Color.Orange,
        Color.Purple,
        Color.Brown,
        Color.Teal,
        Color.Magenta,
        Color.CadetBlue,
        Color.Gold
    };

    public Form1()
	{
		InitializeComponent();

        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

        this.ClientSize = new Size(gridSize * cellSize + 200, gridSize * cellSize);
        this.Text = "Simulazione Animali Evolutivi";

        InitializeGrid();
		SpawnAnimals(animalCount);
		SpawnFood(foodCount);

		timer.Interval = 400; // 100 ms ~ 10 FPS
		timer.Tick += Timer_Tick;
		timer.Start();

		this.Paint += Form1_Paint;
	}

	private void InitializeGrid()
	{
        backgroundGrid = new Bitmap(gridSize * cellSize, gridSize * cellSize);

        using (Graphics g = Graphics.FromImage(backgroundGrid))
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    grid[x, y] = CellType.Empty;
                    Rectangle cellRect = new Rectangle(x * cellSize, y * cellSize, cellSize, cellSize);
                    g.FillRectangle(Brushes.White, cellRect);
                    g.DrawRectangle(Pens.Gray, cellRect);
                }
            }
        }

        //for (int x = 0; x < gridSize; x++)
        //    for (int y = 0; y < gridSize; y++)
        //        grid[x, y] = CellType.Empty;
    }

	private void SpawnAnimals(int count)
	{
		for (int i = 0; i < count; i++)
		{
			int x, y;
			do
			{
				x = rand.Next(gridSize);
				y = rand.Next(gridSize);
			} while (grid[x, y] != CellType.Empty);

            Animal a = new Animal(x, y, energy)
            {
                Color = generationColors[generation % generationColors.Count]
            };
            animals.Add(a);
			grid[x, y] = CellType.Animal;
		}
	}

	private void SpawnFood(int count)
	{
		for (int i = 0; i < count; i++)
		{
			int x, y;
			do
			{
				x = rand.Next(gridSize);
				y = rand.Next(gridSize);
			} while (grid[x, y] != CellType.Empty);

			grid[x, y] = CellType.Food;
		}
	}

    private void Timer_Tick(object sender, EventArgs e)
    {
        bool almenoUnoMorto = false;
        List<Point> deadPositions = new List<Point>();

        foreach (var animal in animals.ToList())
        {
            grid[animal.X, animal.Y] = CellType.Empty;
            animal.Update(grid);

            // Se l'animale trova cibo
            if (grid[animal.X, animal.Y] == CellType.Food)
            {
                animal.Eat(); // metodo che aumenta l'energia
                              // Rimuovi il cibo dalla lista
                var foodPoint = foodList.FirstOrDefault(p => p.X == animal.X && p.Y == animal.Y);
                if (foodPoint != Point.Empty)
                    foodList.Remove(foodPoint);
                grid[animal.X, animal.Y] = CellType.Animal;
            }
            else if (animal.Energy <= 0)
            {
                almenoUnoMorto = true;
                deadPositions.Add(new Point(animal.X, animal.Y));
                animals.Remove(animal);
            }
            else
            {
                grid[animal.X, animal.Y] = CellType.Animal;
            }
        }

        var survivors = animals.ToList();

        // Se almeno uno è morto, nuova "generazione" per i rimpiazzi
        if (almenoUnoMorto)
        {
            generation++;
            var color = generationColors[generation % generationColors.Count];

            int minRadius = 1;
            int maxRadius = 5;
            int radius = Math.Max(minRadius, maxRadius * animals.Count / Math.Max(1, animalCount));

            // Calcola la somma totale dei Lifetime dei sopravvissuti
            int totalLifetime = survivors.Sum(a => a.Lifetime);

            foreach (var pos in deadPositions)
            {
                int x = pos.X;
                int y = pos.Y;

                if (grid[x, y] == CellType.Animal)
                {
                    bool trovato = false;
                    for (int rx = -radius; rx <= radius && !trovato; rx++)
                    {
                        for (int ry = -radius; ry <= radius && !trovato; ry++)
                        {
                            int nx = x + rx;
                            int ny = y + ry;
                            if (nx >= 0 && nx < gridSize && ny >= 0 && ny < gridSize && grid[nx, ny] == CellType.Empty)
                            {
                                x = nx;
                                y = ny;
                                trovato = true;
                            }
                        }
                    }
                }

                grid[x, y] = CellType.Animal;

                NeuralNetwork brain;
                if (survivors.Count > 0 && totalLifetime > 0)
                {
                    // Selezione ponderata: più Lifetime, più probabilità
                    int r = rand.Next(totalLifetime);
                    int acc = 0;
                    Animal? parent = null;
                    foreach (var s in survivors)
                    {
                        acc += s.Lifetime;
                        if (r < acc)
                        {
                            parent = s;
                            break;
                        }
                    }
                    if (parent == null)
                        parent = survivors[rand.Next(survivors.Count)];
                    brain = parent.GetBrain().CloneAndMutate();
                }
                else if (survivors.Count > 0)
                {
                    // fallback: scelta casuale se tutti Lifetime sono 0
                    var parent = survivors[rand.Next(survivors.Count)];
                    brain = parent.GetBrain().CloneAndMutate();
                }
                else
                {
                    brain = new NeuralNetwork();
                }
                var newAnimal = new Animal(x, y, energy, brain)
                {
                    Color = color
                };
                animals.Add(newAnimal);
            }
        }

        Invalidate();
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;

        // Disegna lo sfondo una volta
        g.DrawImageUnscaled(backgroundGrid, 0, 0);

        for (int x = 0; x < gridSize; x++)
		{
			for (int y = 0; y < gridSize; y++)
			{
				Rectangle cellRect = new Rectangle(x * cellSize, y * cellSize, cellSize, cellSize);

				switch (grid[x, y])
				{
					//case CellType.Empty:
     //                   g.FillRectangle(Brushes.White, cellRect);
     //                   g.DrawRectangle(Pens.Gray, cellRect);
					//	break;
					case CellType.Food:
						g.FillRectangle(Brushes.Green, cellRect);
						break;
					case CellType.Animal:
                        var animal = animals.FirstOrDefault(a => a.X == x && a.Y == y);
                        using (var brush = new SolidBrush(animal.Color))
                            g.FillEllipse(brush, cellRect);
                        break;
				}
			}
		}

        // Disegna il numero di generazione a destra della mappa
        using (var font = new Font("Arial", 16, FontStyle.Bold))
        using (var brush = new SolidBrush(Color.Black))
        {
            g.DrawString($"Generazione: {generation}", font, brush, gridSize * cellSize + 10, 20);
        }

        if (animals.Count > 0)
        {
            var brain = animals[0].GetBrain(); // Adatta questo se necessario

            int inputCount = NeuralNetwork.inputCount;     // Es: 3
            int hiddenCount = NeuralNetwork.hiddenCount;   // Es: 4
            int outputCount = NeuralNetwork.outputCount;   // Es: 2

            // Posizioni di partenza
            int startX = gridSize * cellSize + 10;
            int startY = 100;
            int layerSpacing = 200;
            int nodeSpacing = 80;
            int nodeRadius = 12;

            // Calcola posizioni nodi
            Point[] inputNodes = new Point[inputCount];
            Point[] hiddenNodes = new Point[hiddenCount];
            Point[] outputNodes = new Point[outputCount];

            for (int i = 0; i < inputCount; i++)
                inputNodes[i] = new Point(startX, startY + i * nodeSpacing + nodeSpacing / 2 );

            for (int i = 0; i < hiddenCount; i++)
                hiddenNodes[i] = new Point(startX + layerSpacing, startY + i * nodeSpacing);

            for (int i = 0; i < outputCount; i++)
                outputNodes[i] = new Point(startX + 2 * layerSpacing, startY + i * nodeSpacing + nodeSpacing);

            // Disegna connessioni input-hidden con pesi
            for (int i = 0; i < inputCount; i++)
            {
                for (int j = 0; j < hiddenCount; j++)
                {
                    double weight = brain.w1[i, j];
                    g.DrawLine(Pens.Gray, inputNodes[i], hiddenNodes[j]);
                    // Etichetta peso
                    var mid = new Point(
                        (inputNodes[i].X + hiddenNodes[j].X) / 2,
                        (inputNodes[i].Y + hiddenNodes[j].Y) / 2
                    );
                    g.DrawString(weight.ToString("0.00"), this.Font, Brushes.Black, mid);
                }
            }

            // Disegna connessioni hidden-output con pesi
            for (int i = 0; i < hiddenCount; i++)
            {
                for (int j = 0; j < outputCount; j++)
                {
                    double weight = brain.w2[i, j];
                    g.DrawLine(Pens.Gray, hiddenNodes[i], outputNodes[j]);
                    // Etichetta peso
                    var mid = new Point(
                        (hiddenNodes[i].X + outputNodes[j].X) / 2,
                        (hiddenNodes[i].Y + outputNodes[j].Y) / 2
                    );
                    g.DrawString(weight.ToString("0.00"), this.Font, Brushes.Black, mid);
                }
            }

            // Disegna nodi
            foreach (var p in inputNodes)
                g.FillEllipse(Brushes.LightBlue, p.X - nodeRadius, p.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);
            foreach (var p in hiddenNodes)
                g.FillEllipse(Brushes.LightGreen, p.X - nodeRadius, p.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);
            foreach (var p in outputNodes)
                g.FillEllipse(Brushes.Orange, p.X - nodeRadius, p.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);
        }
    }
}

//class Animal
//{
//	public int X, Y;
//	public int Energy;
//	private Random rand = new Random();

//	public Animal(int x, int y)
//	{
//		X = x;
//		Y = y;
//		Energy = 100;
//	}

//	public void Update(Form1.CellType[,] grid)
//	{
//		// Energia diminuisce a ogni passo
//		Energy--;

//		// Movimento casuale semplice (puoi poi sostituirlo con NN)
//		int dx = 0, dy = 0;
//		int move = rand.Next(4);
//		switch (move)
//		{
//			case 0: dx = 1; break;   // destra
//			case 1: dx = -1; break;  // sinistra
//			case 2: dy = 1; break;   // giù
//			case 3: dy = -1; break;  // su
//		}

//		int newX = Math.Clamp(X + dx, 0, grid.GetLength(0) - 1);
//		int newY = Math.Clamp(Y + dy, 0, grid.GetLength(1) - 1);

//		// Muoviti solo se cella vuota o cibo
//		if (grid[newX, newY] != Form1.CellType.Animal)
//		{
//			X = newX;
//			Y = newY;
//		}
//	}

//	public void Eat()
//	{
//		Energy += 20;
//		if (Energy > 100) Energy = 100;
//	}
//}
