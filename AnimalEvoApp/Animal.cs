using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimalEvoApp
{
    class Animal
    {
        public int X, Y;
        public int Energy;
        private Random rand = new Random();
        private NeuralNetwork brain;
        public Color Color { get; set; }

        public Animal(int x, int y, int energy)
        {
            X = x;
            Y = y;
            Energy = energy;
            brain = new NeuralNetwork();
        }

        public Animal(int x, int y, int energy, NeuralNetwork brain)
        {
            X = x;
            Y = y;
            Energy = energy;
            this.brain = brain;
        }

        public void Update(Form1.CellType[,] grid)
        {
            Energy--;

            // Trova cibo più vicino
            int nearestDx = 0, nearestDy = 0;
            int minDist = int.MaxValue;

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (grid[i, j] == Form1.CellType.Food)
                    {
                        int dxx = i - X;
                        int dyy = j - Y;
                        int dist = Math.Abs(dxx) + Math.Abs(dyy);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearestDx = dxx;
                            nearestDy = dyy;
                        }
                    }
                }
            }

            double hunger = 1.0 - (Energy / 100.0);
            double foodDx = Math.Tanh(nearestDx / 5.0); // Normalizza direzione
            double foodDy = Math.Tanh(nearestDy / 5.0);

            var (dx, dy) = brain.Forward(hunger, foodDx, foodDy);

            int newX = Math.Clamp(X + dx, 0, grid.GetLength(0) - 1);
            int newY = Math.Clamp(Y + dy, 0, grid.GetLength(1) - 1);

            if (grid[newX, newY] != Form1.CellType.Animal)
            {
                X = newX;
                Y = newY;
            }
        }

        public void Eat()
        {
            Energy += 5;
            if (Energy > 100) Energy = 100;
        }

        public NeuralNetwork GetBrain() => brain;
    }
}
