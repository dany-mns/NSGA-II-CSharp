﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSGA_II
{
    class MainProgram
    {
        static List<Chromosome> GeneratePopulation(int N)
        {
            List<Chromosome> population = new List<Chromosome>();
            double[] minGenesVal = { 1.4, 500.0, 0.0 };
            double[] maxGenesVal = { 2.5, 10000.0, 100.0 };

            for (int i = 0; i < N; i++)
            {
                /*
                    Individ cu gene care ii dau :
                    - inaltimea in m.
                    - salariul/luna
                    - sanatatea (0-100)
                */
                population.Add(new Chromosome(3, minGenesVal, maxGenesVal));
            }

            return population;
        }
        
        static void ComputeFitness(List<Chromosome> population)
        {
            /*
                Pentru functia de fitness ne vom referi la 2 domenii ( atractivitatea sexuala si rata de supravetuire )
                care vor fi calculate folosind 2 formule fictive.
            */

            population.ForEach(p =>
            {
                double sexAppeal = Math.Round(p.Genes[2] + Math.Log10(3 * p.Genes[1]) + Math.Sqrt(Math.PI) * p.Genes[0], 3);
                double surviveRatio = Math.Round(Math.Sqrt(Math.Abs(1200 * p.Genes[2] - p.Genes[1] - p.Genes[0])), 3);
                p.Fitness.Add(sexAppeal);
                p.Fitness.Add(surviveRatio);
            });

        }

        static void Mutation(Chromosome ch, double pm)
        {
            Random rand = new Random();
            for(int i = 0; i < ch.Genes.Length; i++)
            {
                if(rand.NextDouble() < pm)
                    ch.Genes[i] = ch.MinValues[i] + rand.NextDouble() * (ch.MaxValues[i] - ch.MinValues[i]);
            }
        }

        static List<Chromosome> MakeChildren(List<Chromosome> population, double pc, double pm)
        {
            List<Chromosome> children = new List<Chromosome>();
            List<Chromosome> randomParents = population.OrderBy(p => Guid.NewGuid()).ToList();
            Random rand = new Random();

            for(int i = 0; i < population.Count; i+=2)
            {
                int next = i + 1;
                double a = rand.NextDouble();
                int noGenes = randomParents[i].NoGenes;

                //children.Add(new Chromosome(randomParents[i].NoGenes))
                double[] setOfGenes = new double[randomParents[i].NoGenes];
                double[] setOfGenesSec = new double[randomParents[i].NoGenes];

                // Daca exista prob de incrucisare 
                if(pc > rand.NextDouble())
                {
                    for (int j = 0; j < noGenes; j++)
                    {
                        setOfGenes[j] = a * randomParents[i].Genes[j] + (1 - a) * randomParents[next].Genes[j];
                        setOfGenesSec[j] = a * randomParents[next].Genes[j] + (1 - a) * randomParents[i].Genes[j];
                    }
                } else
                {
                    setOfGenes = randomParents[i].Genes;
                    setOfGenesSec = randomParents[next].Genes;
                }

                Chromosome childFirst = new Chromosome(noGenes, setOfGenes, randomParents[i].MinValues, randomParents[i].MaxValues);
                Chromosome childSecond = new Chromosome(noGenes, setOfGenesSec, randomParents[i].MinValues, randomParents[i].MaxValues);

                Mutation(childFirst, pm);
                Mutation(childSecond, pm);

                children.Add(childFirst);
                children.Add(childSecond);
            }

            return children;
        }

        static List <Chromosome> CombinePopulation(List<Chromosome> parents, List<Chromosome> children)
        {
            return parents.Concat(children).ToList();
        }

        static List<Chromosome> NonDominatedSorting(List<Chromosome> population)
        {
            for(int i = 0; i < population.Count; i++)
            {
                for(int j = 0; j < population.Count; j++)
                {
                    if(j != i)
                    {
                        if(population[i].Fitness[0] < population[j].Fitness[0] && population[i].Fitness[1] < population[j].Fitness[1])
                        {
                            population[i].Np++;
                        }
                    }
                }
                population[i].FrontLevel = population[i].Np + 1;
                if (population[i].FrontLevel == 1)
                    population[i].ParetoOptimal = true;
                else
                    population[i].ParetoOptimal = false;
            }

            return population.OrderBy(p => p.FrontLevel).ToList();
        }

        static int CountPopFront(List<Chromosome> population, int front)
        {
            int count = 0;
            population.ForEach(p =>
            {
                if (p.FrontLevel == front)
                    count++;
            });
            return count;
        }

        public static List<Chromosome> GetBestByCrawdingDistFromFront(List<Chromosome> population, int N)
        {
            List<Chromosome> theBest = new List<Chromosome>();

            Dictionary<int, double> distances = new Dictionary<int, double>();

            for (int i = 0; i < population.Count; i++)
            {
                if (i == 0 || i == population.Count - 1)
                {
                    distances[i] = double.PositiveInfinity;
                }
                else
                {
                    distances[i] = Math.Abs(population[i - 1].Fitness[0] - population[i + 1].Fitness[0]) + Math.Abs(population[i - 1].Fitness[1] - population[i + 1].Fitness[1]);
                }
            }

            List<int> indexBestDistance = distances.OrderByDescending(d => d.Value).Select(k => k.Key).ToList();

            for (int i = 0; i < N; i++)
            {
                theBest.Add(population[indexBestDistance[i]]);
            }

            return theBest;
        }

        static List<Chromosome> TakeTheBestPop(List<Chromosome> population, int N)
        {
            List<Chromosome> newPop = new List<Chromosome>();
            int countN = N;
            int frontLevel = 0;

            for (int i = 0; i < N; i++)
            {
                frontLevel++;
                int popFrontCount = CountPopFront(population, frontLevel);

                // pot avea cel mult pop count front uri
                while(popFrontCount == 0)
                {
                    frontLevel++;
                    popFrontCount = CountPopFront(population, frontLevel);
                }

                countN -= popFrontCount;
                if (countN >= 0)
                {
                    population.ForEach(p =>
                    {
                        if (p.FrontLevel == frontLevel)
                            newPop.Add(p);
                    });
                }
                else
                {
                    List<Chromosome> popFromFront = new List<Chromosome>();
                    int necesar = countN + popFrontCount;
                    population.ForEach(p =>
                    {
                        if (p.FrontLevel == frontLevel)
                        {
                            popFromFront.Add(p);
                        }
                    });
                    List<Chromosome> selectedPopFromFront = GetBestByCrawdingDistFromFront(popFromFront, necesar);
                    selectedPopFromFront.ForEach(sp => { newPop.Add(sp); });
                    break;
                }
                if(i == N - 1)
                {
                    Console.WriteLine("");
                }
            }

            return newPop;
        }

        static void ShowParetoOptimal(List<Chromosome> population)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"C:\Users\Public\OutputNSGA-2.txt"))
            {
                population.ForEach(p =>
                {
                    if (p.FrontLevel == 1)
                    {
                        Console.WriteLine(String.Format("Individ: [I: {0}, M: {1}, H: {2}] => F(ch) = [SA: {3}, SR: {4}]", p.Genes[0], p.Genes[1], p.Genes[2], p.Fitness[0], p.Fitness[1]));
                        file.WriteLine(String.Format("Optimal= {0} {1}", p.Fitness[0], p.Fitness[1]));
                    }
                    else
                    {
                        file.WriteLine(String.Format("NeOptimal= {0} {1}", p.Fitness[0], p.Fitness[1]));
                    }
                });
            }
        }

        static int Main(string[] args)
        {
            const int N = 100;
            int epochs = 12;
            const double pc = 0.9, pm = 0.02;
            // generam populatia
            List<Chromosome> parents = GeneratePopulation(N);
            while(epochs != 0)
            {
                List<Chromosome> children = MakeChildren(parents, pc, pm);
                List<Chromosome> population = CombinePopulation(parents, children);
                ComputeFitness(population);
                List<Chromosome> sortedPop = NonDominatedSorting(population);
                List<Chromosome> elites = TakeTheBestPop(population, N);
                parents = elites;
                epochs--;
            }
            ShowParetoOptimal(parents);
            // luam jumatate, pe cei mai buni

            return 0;
        }

    }
}
