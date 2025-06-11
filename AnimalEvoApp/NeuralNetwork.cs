using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimalEvoApp
{
    public class NeuralNetwork
    {
        public const int hiddenCount = 4;
        public const int inputCount = 3;
        public const int outputCount = 2;

        public double[,] w1 = new double[3, hiddenCount]; // input -> hidden
        public double[,] w2 = new double[hiddenCount, 2]; // hidden -> output
        private Random rand = new Random();

        public NeuralNetwork()
        {
            RandomizeWeights();
        }

        private void RandomizeWeights()
        {
            for (int i = 0; i < w1.GetLength(0); i++)
                for (int j = 0; j < w1.GetLength(1); j++)
                    w1[i, j] = rand.NextDouble() * 2 - 1;

            for (int i = 0; i < w2.GetLength(0); i++)
                for (int j = 0; j < w2.GetLength(1); j++)
                    w2[i, j] = rand.NextDouble() * 2 - 1;
        }

        private double Step(double x)
        {
            if (x > 0.1) return 1;
            else if (x < -0.1) return -1;
            else return 0;
        }

        public (int dx, int dy) Forward(double hunger, double foodDx, double foodDy)
        {
            double[] input = new double[] { hunger, foodDx, foodDy };
            double[] hidden = new double[hiddenCount];

            // Input -> Hidden
            for (int j = 0; j < hiddenCount; j++)
            {
                double sum = 0;
                for (int i = 0; i < 3; i++)
                    sum += input[i] * w1[i, j];
                hidden[j] = ReLU(sum);
            }

            // Hidden -> Output
            double[] output = new double[2];
            for (int j = 0; j < 2; j++)
            {
                double sum = 0;
                for (int i = 0; i < hiddenCount; i++)
                    sum += hidden[i] * w2[i, j];
                output[j] = Math.Tanh(sum); // range -1..1
            }

            // Convert output to direction: round to int -1, 0, or 1
            int dx = (int)Step(output[0]);
            int dy = (int)Step(output[1]);

            return (dx, dy);
        }

        public NeuralNetwork CloneAndMutate(double mutationRate = 0.1, double mutationStrength = 0.5)
        {
            NeuralNetwork clone = new NeuralNetwork();

            for (int i = 0; i < w1.GetLength(0); i++)
            {
                for (int j = 0; j < w1.GetLength(1); j++)
                {
                    clone.w1[i, j] = w1[i, j];
                    if (rand.NextDouble() < mutationRate)
                        clone.w1[i, j] += (rand.NextDouble() * 2 - 1) * mutationStrength;
                }
            }

            for (int i = 0; i < w2.GetLength(0); i++)
            {
                for (int j = 0; j < w2.GetLength(1); j++)
                {
                    clone.w2[i, j] = w2[i, j];
                    if (rand.NextDouble() < mutationRate)
                        clone.w2[i, j] += (rand.NextDouble() * 2 - 1) * mutationStrength;
                }
            }

            return clone;
        }

        private double ReLU(double x) => x > 0 ? x : 0;
    }
}
