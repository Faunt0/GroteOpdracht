// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;


namespace GroteOpdracht
{
    class Program
    {
        static void Main()
        {
            // grabbelton:
            Dictionary<int, Bedrijf> grabbelton = new Dictionary<int, Bedrijf>();
            Dictionary<int, Bedrijf> grabbeltonFreq234 = new Dictionary<int, Bedrijf>();
            using (var reader = new StreamReader("../../../orderbestand.txt"))
            {
                string firstline = reader.ReadLine();

                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    string[] parts = line.Split(';');
                    Bedrijf bv = new Bedrijf(parts);
                    if (bv.freq == 1) { grabbelton[bv.order] = bv; }
                    else { grabbeltonFreq234[bv.order] = bv; } // bewaar alle bedrijven met een frequentie > 1 in een aparte grabbelton
                }
            }


            double[,] fileDict = new double[1099,1099]; // maak lijst van
            using (var reader = new StreamReader("../../../afstandenmatrix.txt"))
            {
                string firstline = reader.ReadLine();
                string line;
                while (( line = reader.ReadLine()) != null )
                {
                    string[] parts = line.Split(";");
                    int id1 = int.Parse(parts[0]);
                    int id2 = int.Parse(parts[1]);
                    double tijd = double.Parse(parts[3]) / 60D; // is in seconden maar de rest niet
                    fileDict[id1, id2] = tijd;
                }
            }

            Random rndin = new Random();
            //Console.WriteLine(rndin.ToString());
            Oplossing oplossing = new Oplossing(fileDict, rndin);

            Console.WriteLine("Start from empty [y/n]: ");
            string ans = Console.ReadLine();
            //string ans = "y";
            if (ans == "n")
            {
                Oplossing op = new Oplossing(fileDict, rndin);

                // Get the info from the file
                Console.WriteLine("Which file:");
                string filepath = "./oplossingen/";
                DirectoryInfo d = new DirectoryInfo(filepath);
                List<string> files = new List<string>();
                int index = 0;
                foreach (var file in d.GetFiles("*.txt"))
                {
                    // print the possible files in the folder
                    Console.WriteLine($"\t{index}.\t{file.Name}");
                    index++;
                    files.Add(file.FullName);
                }



                // read the file that has been chosen
                string fileI = Console.ReadLine();
                using (var reader = new StreamReader(files[int.Parse(fileI)]))
                {
                    // maak de routes
                    List<(int, int)>[,] t_and_d = new List<(int, int)>[2, 5];
                    for (int t = 0; t < 2; t++)
                    {
                        for (int dk = 0; dk < 5; dk++)
                        {
                            t_and_d[t, dk] = new List<(int, int)>();
                        }
                    }

                            List<Bedrijf> bedrijven = new List<Bedrijf>();
                    List<int> f234Orders = new List<int>();

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Replace(" ", "").Split(";");
                        Dag dag = oplossing.trucksEnRoutes[int.Parse(parts[0]) - 1][int.Parse(parts[1]) - 1];

                        // save a tuple of the order sequence number and order number
                        t_and_d[int.Parse(parts[0]) - 1, int.Parse(parts[1]) - 1].Add((int.Parse(parts[2]), int.Parse(parts[3])));
                    }
                    // na elke regel gelezen te hebben

                    // bouw de routes op
                    for (int t = 0; t < 2; t++)
                    {
                        for (int dk = 0; dk < 5;  dk++)
                        {
                            Dag dag = oplossing.trucksEnRoutes[t][dk];
                            dag.routes.Clear();

                            Dictionary<int, int> kv = t_and_d[t, dk].ToDictionary(x => x.Item1, x => x.Item2);

                            int r_index = 0;
                            Route r = new Route(oplossing.stort);
                            dag.routes.Add(r);
                            for (int i = 1; i < kv.Count + 1; i++)
                            {
                                if (kv[i] == 0)
                                {
                                    // maak een nieuwe route aan
                                    dag.routes[r_index].maakLinkedList();
                                    if (i != kv.Count)
                                    {
                                        dag.routes.Add(new Route(oplossing.stort));
                                        r_index++;
                                    }
                                }
                                else if (i != kv.Count)
                                {
                                    Bedrijf b = grabbelton.ContainsKey(kv[i]) ? grabbelton[kv[i]] : grabbeltonFreq234[kv[i]];
                                    if (b.freq == 1)
                                    {
                                        grabbelton.Remove(b.order); // dit kan niet voor dingen die vaker voor moeten komen. maar wat doe je dan
                                        dag.routes[r_index].route.Add(b);
                                    }
                                    else
                                    {
                                        // maak de bedrijven met freq>1 uniek in de route zodat ze unieke pred en succ hebben.
                                        Bedrijf b_clone = new Bedrijf(b.inputArray);
                                        b_clone.dagkey = dk;
                                        dag.routes[r_index].route.Add(b_clone);
                                        f234Orders.Add(b.order);
                                    }

                                    dag.routes[r_index].capaciteit += b.cont * b.vpc;
                                }
                            }
                        }
                    }

                    // verwijder bedrijven met freq > 1 uit de bijbehorende grabbelton
                    foreach (int order in f234Orders)
                    {
                        grabbeltonFreq234.Remove(order);
                    }

                    // extra controle dat ze allemaal de juiste hoeveelheid routes hebben.
                    if (oplossing.trucksEnRoutes[0][0].routes.Count == 1)
                    {
                        oplossing.trucksEnRoutes[0][0].routes.Add(new Route(oplossing.stort));
                    }
                    if (oplossing.trucksEnRoutes[1][0].routes.Count == 1)
                    {
                        oplossing.trucksEnRoutes[1][0].routes.Add(new Route(oplossing.stort));
                    }
                    if (oplossing.trucksEnRoutes[0][3].routes.Count == 1)
                    {
                        oplossing.trucksEnRoutes[0][3].routes.Add(new Route(oplossing.stort));
                    }
                    if (oplossing.trucksEnRoutes[0][4].routes.Count == 1)
                    {
                        oplossing.trucksEnRoutes[0][4].routes.Add(new Route(oplossing.stort));
                    }
                    if (oplossing.trucksEnRoutes[1][4].routes.Count == 1)
                    {
                        oplossing.trucksEnRoutes[1][4].routes.Add(new Route(oplossing.stort));
                    }
                }
            }


            oplossing.grabbelton = grabbelton.Values.ToList();
            oplossing.grabbeltonFreq234 = grabbeltonFreq234.Values.ToList();
            int timesloped = 0;
            string save = "-";
            while (save == "-" || save == "4" || save == "5")
            {
                timesloped++;
                oplossing.beginScore();
                Console.WriteLine($"score eerst: {oplossing.score}");
                DateTime before = DateTime.Now;
                oplossing.ILS();
                DateTime after = DateTime.Now;
                TimeSpan ts = after - before;

                Console.WriteLine($"incrementele score daarna: {oplossing.score}");
                oplossing.beginScore();
                Console.WriteLine($"berekende score daarna: {oplossing.score}");
                Console.WriteLine($"beste score?: {oplossing.beste.Item1}");
                Console.WriteLine($"Milliseconden:\t{ts.TotalMilliseconds}");
                Console.WriteLine($"Iteraties:\t{oplossing.tellertje}");
                double speed = oplossing.tellertje / ((double)ts.TotalMilliseconds / 1000);
                Console.WriteLine($"speed:\t\t{speed} iterations/s");
                Console.WriteLine($"T waarde:\t{oplossing.T}");
                Console.WriteLine($"plateau count:\t{oplossing.plateauCount}");
                Console.WriteLine("Kies een oplossing actie:");
                Console.WriteLine($"\t0.\tBeste Oplossing:\tScore = {oplossing.beste.Item1}");
                Console.WriteLine($"\t1.\tLaatste Oplossing:\tScore = {oplossing.score}");
                Console.WriteLine($"\t2.\tBeide Oplossingen");
                Console.WriteLine($"\t3.\tGeen van beide");
                Console.WriteLine($"\t4.\tDoe nogmaals hetzelfde aantal iteraties met de laatste oplossing");
                Console.WriteLine($"\t5.\tDoe nogmaals hetzelfde aantal iteraties met de beste oplossing");
                save = Console.ReadLine().Trim();
                if (save == "1") // alleen de laatste oplossing saven
                {
                    List<string> lines = oplossing.makeString(oplossing.trucksEnRoutes);
                    Console.WriteLine("Name the save file:\t");
                    string fileName = Console.ReadLine();
                    StreamWriter sw = new StreamWriter($"./oplossingen/{fileName}.txt");
                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }
                else if (save == "0") // alleen de beste oplossing saven
                {
                    Console.WriteLine("Name the save file:\t");
                    string fileName = Console.ReadLine();
                    StreamWriter sw = new StreamWriter($"./oplossingen/{fileName}.txt");
                    foreach (string line in oplossing.beste.Item2)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }
                // gebruik de laatst gevonden oplossing opnieuw
                else if (save == "4")
                {
                    oplossing.tellertje = 0;
                    oplossing.T = oplossing.old_T;
                }
                // gebruik de tot nu toe beste oplossing opnieuw
                else if (save == "5")
                {
                    throw new Exception("This does not work properly, seeing as the best solution is in text format and the way to convert this into a usable format has yet to be properly implemented");
                    //oplossing.trucksEnRoutes = oplossing.beste.Item2;
                    //oplossing.tellertje = 0;
                    //oplossing.T = 30;
                }
                else
                {
                    break;
                }
            }
        }
    }
}