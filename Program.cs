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
            // grabbelton en grabbelton voor frequentie > 1
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

            // lees de afstanden matrix in
            double[,] fileDict = new double[1099,1099];
            using (var reader = new StreamReader("../../../afstandenmatrix.txt"))
            {
                string firstline = reader.ReadLine();
                string line;
                while (( line = reader.ReadLine()) != null )
                {
                    string[] parts = line.Split(";");
                    int id1 = int.Parse(parts[0]);
                    int id2 = int.Parse(parts[1]);
                    double tijd = double.Parse(parts[3]) / 60D; // is in het bestand in seconden
                    fileDict[id1, id2] = tijd;
                }
            }

            Random rndin = new Random();
            Oplossing oplossing = new Oplossing(fileDict, rndin);

            Console.WriteLine("Start from empty [y/n]: ");
            string ans = Console.ReadLine();
            if (ans == "n")
            {
                Oplossing op = new Oplossing(fileDict, rndin);

                // lees de informatie van de directory waar we files opslaan
                Console.WriteLine("Which file:");
                string filepath = "./oplossingen/";
                DirectoryInfo d = new DirectoryInfo(filepath);
                List<string> files = new List<string>();
                int index = 0;
                foreach (var file in d.GetFiles("*.txt"))
                {
                    // print de namen van alle files
                    Console.WriteLine($"\t{index}.\t{file.Name}");
                    index++;
                    files.Add(file.FullName);
                }


                // lees de regels van de gekozen file
                string fileI = Console.ReadLine();
                List<string> inputs = new List<string>();
                using (var reader = new StreamReader(files[int.Parse(fileI)]))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        inputs.Add(line);
                    }
                }

                // bouw de oplossing op in onze data structuur
                oplossing.SlnfromString(inputs, grabbelton, grabbeltonFreq234);
            }


            // maak er lijsten van voor makkelijk willekeurig indexen
            oplossing.grabbelton = grabbelton.Values.ToList();
            oplossing.grabbeltonFreq234 = grabbeltonFreq234.Values.ToList();
            string save = "-";
            while (save == "-" || save == "4" || save == "5")
            {
                oplossing.beginScore();
                Console.Clear();
                double scoreEerst = oplossing.score;
                Console.WriteLine($"score eerst: {scoreEerst}");

                // mogelijkheid om parameters te veranderen
                bool changing = true;
                while (changing)
                {
                    // geef een lijst van de huidige parameters
                    double duration = (oplossing.iterations / (double)4000000) / 60; // 4 miljoen is ongeveer ons gemiddelde
                    Console.WriteLine($"Est. duration: {duration} minutes");
                    Console.WriteLine("Do you want to change parameters?");
                    Console.WriteLine($"\t1. Num. Iterations:\t\t{oplossing.iterations}");
                    Console.WriteLine($"\t2.T (start):t\t{oplossing.T}");
                    Console.WriteLine($"\t3.Q:\t\t{oplossing.Q}");
                    Console.WriteLine($"\t4.Plateau boundary:\t\t{oplossing.plt_bound}");
                    Console.WriteLine("\tPress Enter to continue");

                    string c = Console.ReadLine();
                    if (c == "1")
                    {
                        Console.WriteLine("New Value for max iterations: ");
                        long i = long.Parse(Console.ReadLine());
                        oplossing.iterations = i;
                    }
                    else if (c == "2")
                    {
                        Console.WriteLine("New value for T (start) (use ','): ");
                        double i = double.Parse(Console.ReadLine());
                        oplossing.T = i;
                    }
                    else if (c == "3")
                    {
                        Console.WriteLine("New value for Q: ");
                        long i = long.Parse(Console.ReadLine());
                        oplossing.Q = i;
                    }
                    else if (c == "4")
                    {
                        Console.WriteLine("New value for plateau boundary: ");
                        long i = long.Parse(Console.ReadLine());
                        oplossing.plt_bound = i;
                    }
                    else { changing = false; }
                }
                Console.Clear();
                Console.WriteLine($"score eerst: {oplossing.score}");
                Console.WriteLine("|--------------------|");
                Console.Write(" ");

                DateTime before = DateTime.Now;
                oplossing.LocalSearch();
                DateTime after = DateTime.Now;
                TimeSpan ts = after - before;
                Console.Clear();
                Console.WriteLine($"Score eerst:\t{scoreEerst}");
                Console.WriteLine($"Laatste score:\t{oplossing.score}");
                Console.WriteLine($"Beste score:\t{oplossing.beste.Item1}");
                Console.WriteLine($"Duration:\t{ts}");
                Console.WriteLine($"Iteraties:\t{oplossing.tellertje}");

                double speed = oplossing.tellertje / ((double)ts.TotalMilliseconds / 1000);
                Console.WriteLine($"speed:\t\t{speed} iterations/s");
                Console.WriteLine($"T waarde:\t{oplossing.T}");
                Console.WriteLine($"plateau count:\t{oplossing.plateauCount}");

                // opties voor vervolg acties
                Console.WriteLine("Kies een oplossing actie:");
                Console.WriteLine($"\t0.\tBeste Oplossing:\t\tScore = {oplossing.beste.Item1}");
                Console.WriteLine($"\t1.\tLaatste Oplossing:\t\tScore = {oplossing.score}");
                Console.WriteLine($"\t2.\tBeide Oplossingen");
                Console.WriteLine($"\t3.\tGeen van beide");
                Console.WriteLine($"\t4.\tDoe nogmaals hetzelfde aantal iteraties met de laatste oplossing");
                Console.WriteLine($"\t5.\tDoe nogmaals hetzelfde aantal iteraties met de beste oplossing");


                // geef de mogelijkheid om bestanden op te slaan of om meer iteraties te doen
                save = Console.ReadLine().Trim();
                if (save == "0") // alleen de beste oplossing saven
                {
                    Console.WriteLine("Name the save file [Enter for default]:\t");
                    string fileName = Console.ReadLine();
                    fileName = fileName == "" ? Math.Round(oplossing.beste.Item1).ToString() + " " + after.ToShortTimeString() : fileName;
                    StreamWriter sw = new StreamWriter($"./oplossingen/{fileName}.txt");
                    foreach (string line in oplossing.beste.Item2)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }
                else if (save == "1") // alleen de laatste oplossing saven
                {
                    List<string> lines = oplossing.makeString(oplossing.trucksEnRoutes);
                    Console.WriteLine("Name the save file [Enter for default]:\t");
                    string fileName = Console.ReadLine();
                    fileName = fileName == "" ? Math.Round(oplossing.score).ToString() + " " + after.ToShortTimeString() : fileName;
                    StreamWriter sw = new StreamWriter($"./oplossingen/{fileName}.txt");
                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }
                else if (save == "2") // beide oplossingen saven
                {
                    // laatste oplossing saven
                    List<string> lines = oplossing.makeString(oplossing.trucksEnRoutes);
                    Console.WriteLine("Name the save file for the last solution [Enter for default]:\t");
                    string fileName = Console.ReadLine();
                    fileName = fileName == "" ? Math.Round(oplossing.score).ToString() + " " + after.ToShortTimeString() : fileName;
                    StreamWriter sw = new StreamWriter($"./oplossingen/{fileName}.txt");
                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();

                    // beste oplossing
                    Console.WriteLine("Name the save file for the best solution [Enter for default]:\t");
                    fileName = Console.ReadLine();
                    fileName = fileName == "" ? Math.Round(oplossing.beste.Item1).ToString() + " " + after.ToShortTimeString() : fileName;
                    sw = new StreamWriter($"./oplossingen/{fileName}.txt");
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
                    grabbelton = new Dictionary<int, Bedrijf>();
                    grabbeltonFreq234 = new Dictionary<int, Bedrijf>();
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

                    oplossing.SlnfromString(oplossing.beste.Item2, grabbelton, grabbeltonFreq234);
                    oplossing.score = oplossing.beste.Item1;
                    oplossing.tellertje = 0;
                }
                else
                {
                    break;
                }
            }
        }
    }
}