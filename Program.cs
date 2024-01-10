// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GroteOpdracht
{
    class Program
    {

        static void Main()
        {
            // bedrijvenlijst:
            Dictionary<int, Bedrijf> grabbelton = new Dictionary<int, Bedrijf>();
            using (var reader = new StreamReader("../../../orderbestand.txt"))
            {
                string firstline = reader.ReadLine();
                //Console.WriteLine($"{firstline}");

                int index = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(';');
                    Bedrijf bv = new Bedrijf(parts);

                    grabbelton[bv.order] = bv;
                }
            }

            float[,] fileDict = new float[1099, 1099];
            //Dictionary<(int, int), float> fileDict = new Dictionary<(int, int), float>(); // maak lijst van
            using (var reader = new StreamReader("../../../afstandenmatrix.txt"))
            {
                string firstline = reader.ReadLine();
                string line;
                while (( line = reader.ReadLine()) != null )
                {
                    string[] parts = line.Split(";");
                    int id1 = int.Parse(parts[0]);
                    int id2 = int.Parse(parts[1]);
                    float tijd = float.Parse(parts[3]) / 60f; // is in seconden maar de rest niet
                    fileDict[id1, id2] = tijd;
                }
            }

            Random rndin = new Random();
            Oplossing oplossing = new Oplossing(fileDict, rndin);

            Console.WriteLine("Start from empty [y/n]: ");
            string ans = Console.ReadLine();
            //string ans = "y";
            if (ans == "n")
            {
                Oplossing op = new Oplossing(fileDict, rndin);
                Console.WriteLine("Which file:");
                string filepath = "./oplossingen/";
                DirectoryInfo d = new DirectoryInfo(filepath);

                List<string> files = new List<string>();
                int index = 0;
                foreach (var file in d.GetFiles("*.txt"))
                {
                    Console.WriteLine($"\t{index}.\t{file.Name}");
                    index++;
                    files.Add(file.FullName);
                }

                string fileI = Console.ReadLine();
                using (var reader = new StreamReader(files[int.Parse(fileI)]))
                {
                    // maak de routes
                    Route r = new Route();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Replace(" ", "").Split(";");
                        Dag dag = oplossing.trucksEnRoutes[int.Parse(parts[0]) - 1][int.Parse(parts[1]) - 1];

                        if (int.Parse(parts[3]) == 0)
                        {
                            r.maakLinkedList();
                            dag.routes.Add(r);

                            r = new Route();
                            r.capaciteit = 0;
                        }
                        else
                        {
                            Bedrijf b = grabbelton[int.Parse(parts[3])]; // pak het bedrijf uit de lijst van mogelijkheden
                            if (b.freq == 1)
                            {
                                grabbelton.Remove(b.order); // dit kan niet voor dingen die vaker voor moeten komen. maar wat doe je dan
                            }

                            r.route.Add(b);
                            r.capaciteit += b.cont * b.vpc;
                        }
                    }
                }
            }


            oplossing.grabbelton = grabbelton.Values.ToList();

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
                float speed = oplossing.tellertje / ((float)ts.TotalMilliseconds / 1000);
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
                    // 1; 1; 1; 10
                    List<string> lines = oplossing.makeString(oplossing.trucksEnRoutes);

                    //List<string> lines = new List<string>();
                    //for (int t = 0; t < 2; t++)
                    //{
                    //    for (int d = 0; d < 5; d++)
                    //    {
                    //        List<Route> routes = oplossing.trucksEnRoutes[t][d].routes;

                    //        int index = 0;
                    //        List<string> strings = new List<string>();
                    //        foreach (Route r in routes)
                    //        {
                    //            List<string> res = r.makeString(r.route[0], index);
                    //            res.Reverse();
                    //            foreach (string s in res) { strings.Add(s); }

                    //            index += res.Count;
                    //        }

                    //        foreach (string s in strings)
                    //        {
                    //            //Console.WriteLine($"{t}; {d}; " + s);
                    //            lines.Add($"{t + 1}; {d + 1}; " + s);
                    //        }
                    //    }
                    //}
                    
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
                    //List<string> lines = new List<string>();

                    //for (int t = 0; t < 2; t++)
                    //{
                    //    for (int d = 0; d < 5; d++)
                    //    {
                    //        List<Route> routes = oplossing.beste.Item2[t][d].routes;

                    //        int index = 0;
                    //        List<string> strings = new List<string>();
                    //        foreach (Route r in routes)
                    //        {
                    //            List<string> res = r.makeString(r.route[0], index);
                    //            res.Reverse();
                    //            foreach (string s in res) { strings.Add(s); }

                    //            index += res.Count;
                    //        }

                    //        foreach (string s in strings)
                    //        {
                    //            //Console.WriteLine($"{t}; {d}; " + s);
                    //            lines.Add($"{t + 1}; {d + 1}; " + s);
                    //        }
                    //    }
                    //}

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
                    oplossing.tries = 0;
                    oplossing.T = 18 / 2;
                    ///TODO bewaar de originele T ergens zodat je deze hier kunt vermenigvuldigen naar een kleinere waarde (delen?)
                }
                // gebruik de tot nu toe beste oplossing opnieuw
                else if (save == "5")
                {
                    //oplossing.trucksEnRoutes = oplossing.beste.Item2;
                    // lees het in
                    oplossing.tellertje = 0;
                    oplossing.tries = 0;
                    oplossing.T = 14;
                }
                else
                {
                    break;
                }
            }
        }
    }
    public class Oplossing
    {
        public float[,] distDict;
        public Dag[][] trucksEnRoutes;
        public (float, List<string>) beste;
        public List<Bedrijf>? grabbelton;
        public int tellertje;
        public float score;
        public int plateauCount;
        public float increment;
        public long iterations = 10000000;
        public long tries = 0;
        public long effective = 100000;
        public float T = 18;
        public int Q = 200000;
        public float alpha;
        public Random rnd;
        public Oplossing(float[,] dictInput, Random rndIn)
        {
            trucksEnRoutes = [[new Dag(), new Dag(), new Dag(), new Dag(), new Dag()], [new Dag(), new Dag(), new Dag(), new Dag(), new Dag()]];
            distDict = dictInput;
            alpha = 0.99F;
            rnd = rndIn;
            plateauCount = 0;
            beste = (1000000, new List<string>());
        }

        public void beginScore()
        {
            float travelTime = 0;
            float grabbeltonKosten = 0;
            foreach (Bedrijf b in grabbelton)
            {
                grabbeltonKosten += b.ldm * 3 * b.freq;
            }
            for (int truck = 0; truck < 2; truck++)
            {
                for (int i = 0; i < 5; i++)
                {
                    // calculate the score of each route in the beginning
                    Dag d = trucksEnRoutes[truck][i];
                    d.tijdsduur = 0;

                    foreach(Route r in d.routes)
                    {
                        if (r.route.Count > 2)
                        {
                            
                            float routekosten = 30;
                            foreach (Bedrijf b in r.route)
                            {
                                if (b.predecessor == null)
                                {
                                    routekosten += recurseRoute(b);
                                    break;
                                }
                            }
                            d.tijdsduur += routekosten;
                            travelTime += routekosten;
                        }
                    }
                }
            }
            score = travelTime + grabbeltonKosten;
        }
        public float recurseRoute(Bedrijf b)
        {
            if (b.successor == null)
            {
                return 0;
            }
            return b.ldm +  rijtijd(b.successor, b) + recurseRoute(b.successor);
        }
        public List<string> makeString(Dag[][] trucksenroute)
        {
            List<string> res = new List<string>();
            // loop door trucks
            for (int i = 0; i < 2; i++)
            {
                // loop door dagen
                for (int j = 0; j < 5; j++)
                {
                    List<string> routeString = trucksenroute[i][j].makeString();
                    foreach (string s in routeString)
                    {
                        res.Add($"{i + 1}; {j + 1}; {s}");
                    }
                }
            }
            return res;
        }
        public void ILS()
        {
            while (tellertje < iterations && T > 0.00005F)
            {
                // implementeer random walk.
                float oldscore = score;
                int op = rnd.Next(100);

                addOperation();

                //if      (0 <= op && op < 40) { swapWithinDay(); }
                //if (40 <= op && op < 50) { swapBetweenDays(); }
                ////else if (80 <= op && op < 85) { replaceStop(); }
                ////else if (86 <= op && op < 92) { removeStop(); }
                ////else if (88 <= op && op < 92) { addRoute(); }
                //else if (50 <= op && op < 80) { shiftBetween(); }
                //else if (80 <= op && op < 100) { addOperation(); }
                //else
                //{
                //    shiftWithin();
                //}

                if (oldscore == score)
                {
                    plateauCount++;
                    //if (plateauCount == 100)
                    //{
                    //    // random walk.
                    //    // verander T.
                    //}
                }
                if (oldscore < score)
                {
                    tries++;
                    plateauCount = 0;
                }
                if (oldscore > score)
                {
                    tries++;
                    plateauCount = 0;
                }

                if (score < beste.Item1)
                {
                    List<string> best = makeString(trucksEnRoutes);

                    beste = (score, best);
                }

                tellertje++;

                if (tellertje % Q == 0)
                {
                    T = T * alpha;
                }
            }
        }
        // kijkt of de huidige increment geaccepteerd word in de operation, op basis van simulated annealing
        bool acceptIncrement()
        {
            if (increment < 0)
            {
                return true;
            }
            if (Math.Exp((-increment) / T) < rnd.NextDouble())
            {
                return true;
            }
            return false;
        }
        
        void removeStop()
        {
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];


            if (r.route.Count > 2)
            {
                Bedrijf b = r.route[rnd.Next(0, r.route.Count)];

                if (b.freq == 1 && b.successor != null && b.predecessor != null) // haal niet een van de stort weg
                {
                    float tijdDelta = 0;

                    if (r.route.Count == 3)
                    {
                        // verwijder ook de route uit de dag als er maar 1 stop is
                        tijdDelta = - rijtijd(b, b.predecessor) - rijtijd(b.successor, b) - 30 - b.ldm;
                        increment = b.ldm * 3 + tijdDelta;

                        Bedrijf pred = b.predecessor;
                        Bedrijf succ = b.successor;

                        if (acceptIncrement())
                        {
                            grabbelton.Add(b);

                            if (d.routes.Count == 1)
                            {
                                r.route.Remove(b);
                                pred.ReplaceChains(null, succ);
                                succ.ReplaceChains(pred, null);
                                r.capaciteit = 0;
                            }
                            else
                            {
                                d.routes.Remove(r);
                            }

                            d.tijdsduur += tijdDelta;
                            score += increment;
                        }
                    }
                    else
                    {
                        tijdDelta = rijtijd(b.successor, b.predecessor) - rijtijd(b, b.predecessor) - rijtijd(b.successor, b) - b.ldm;
                        float capDelta = -b.cont * b.vpc;
                        increment = tijdDelta + 3 * b.ldm;

                        if (acceptIncrement())
                        {
                            d.tijdsduur += tijdDelta;
                            r.capaciteit += capDelta;

                            Bedrijf pred = b.predecessor;
                            Bedrijf succ = b.successor;

                            pred.ReplaceChains(pred.predecessor, succ);
                            succ.ReplaceChains(pred, succ.successor);

                            r.route.Remove(b);
                            grabbelton.Add(b);

                            score += increment;
                        }
                    }
                }
            }
        }
        void replaceStop()
        {
            // haal bedrijf uit grabbelton en vervang een willekeurig bedrijf met deze.
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);
            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];

            if (r.route.Count > 2 && grabbelton.Count > 0)
            {
                // pak bedrijven
                Bedrijf b1 = r.route[rnd.Next(r.route.Count)];
                Bedrijf b2 = grabbelton[rnd.Next(grabbelton.Count)];

                // Zorg ervoor dat we niet een stort pakken
                if (b1.freq == 1 && b2.freq == 1 && b1.successor != null && b1.predecessor != null)
                {
                    float tijdDelta = rijtijd(b2, b1.predecessor) + rijtijd(b1.successor, b2) - rijtijd(b1, b1.predecessor) - rijtijd(b1.successor, b1) - b1.ldm + b2.ldm;
                    float capDelta = - b1.cont * b1.vpc + b2.cont * b2.vpc;

                    increment = -b2.ldm * 3 + b1.ldm * 3 + tijdDelta;
                    if (acceptIncrement())
                    {
                        d.tijdsduur += tijdDelta;
                        r.capaciteit += capDelta;


                        Bedrijf b1pred = b1.predecessor;
                        Bedrijf b1succ = b1.successor;

                        b2.ReplaceChains(b1pred, b1succ);
                        b1pred.ReplaceChains(b1pred.predecessor, b2);
                        b1succ.ReplaceChains(b2, b1succ.successor);

                        grabbelton.Remove(b2);
                        r.route.Remove(b1);
                        grabbelton.Add(b1);
                        r.route.Add(b2);

                        score += increment;
                    }
                }
            }
        }
        void swapBetweenDays()
        {
            int dagkey1 = rnd.Next(0, 5);
            int truckkey1 = rnd.Next(0, 2);
            Dag d1 = trucksEnRoutes[truckkey1][dagkey1];
            int dagkey2 = rnd.Next(0, 5);
            int truckkey2 = rnd.Next(0, 2);
            Dag d2 = trucksEnRoutes[truckkey2][dagkey2];

            Route r1 = d1.routes[rnd.Next(d1.routes.Count)];
            Route r2 = d2.routes[rnd.Next(d2.routes.Count)];

            if (r1.route.Count > 2 && r2.route.Count > 2)
            {
                Bedrijf b1 = r1.route[rnd.Next(r1.route.Count)]; // pak een bedrijf
                Bedrijf b2 = r2.route[rnd.Next(r2.route.Count)]; // pak nog een bedrijf

                // gebruik pointers
                Bedrijf b1_pred = b1.predecessor;
                Bedrijf b1_succ = b1.successor;
                Bedrijf b2_pred = b2.predecessor;
                Bedrijf b2_succ = b2.successor;

                if (b1.freq == 1 && b2.freq == 1 && b1 != b2 && b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                {
                    // bereken de increment

                    // dit is alleen mogelijk als de bedrijven hetzelfde zijn.
                    float tijdDelta1; // het verschil in tijd voor dag 1
                    float tijdDelta2; // het verschil in tijd voor dag 2

                    // dit zijn speciale cases voor als de bedrijven buren zijn val elkaar
                    // b1_pred -> b1 -> b2 -> b2_succ
                    if (b1_succ == b2 && b2_pred == b1)
                    {
                        // je hoeft geen rekening te houden met de ldm aangezien deze weggestreept kan worden
                        tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b2, b1) - rijtijd(b2_succ, b2) + rijtijd(b2, b1_pred) + rijtijd(b1, b2) + rijtijd(b2_succ, b1);
                        tijdDelta2 = 0;
                    }
                    // b2_pred -> b2 -> b1 -> b1_succ // ook aleen mogelijk als de bedrijven hetzelfde zijn.
                    else if (b2_succ == b1 && b1_pred == b2)
                    {
                        tijdDelta1 = -rijtijd(b2, b2_pred) - rijtijd(b1, b2) - rijtijd(b1_succ, b1) + rijtijd(b1, b2_pred) + rijtijd(b2, b1) + rijtijd(b1_succ, b2);
                        tijdDelta2 = 0;
                    }
                    else
                    {
                        // stel het zijn geen buren dan is het ongeacht de dag het volgende:
                        tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b2, b1_pred) + rijtijd(b1_succ, b2) - b1.ldm + b2.ldm;
                        tijdDelta2 = -rijtijd(b2, b2_pred) - rijtijd(b2_succ, b2) + rijtijd(b1, b2_pred) + rijtijd(b2_succ, b1) - b2.ldm + b1.ldm;
                    }

                    increment = tijdDelta1 + tijdDelta2;

                    float capDelta1 = b2.cont * b2.vpc - b1.cont * b1.vpc;
                    float capDelta2 = b1.cont * b1.vpc - b2.cont * b2.vpc;

                    if (d1.tijdsduur + tijdDelta1 < 12 * 60 && d2.tijdsduur + tijdDelta2 < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                    {
                        d1.tijdsduur += tijdDelta1;
                        d2.tijdsduur += tijdDelta2;

                        r1.capaciteit += capDelta1;
                        r2.capaciteit += capDelta2;

                        r1.route.Remove(b1);
                        r2.route.Remove(b2);


                        // voor het geval dat ze buren zijn
                        // b1_pred -> b1 -> b2 -> b2_succ
                        if (b1_succ == b2 && b2_pred == b1)
                        {
                            b1.ReplaceChains(b2, b2_succ);
                            b2.ReplaceChains(b1_pred, b1);

                            b1_pred.ReplaceChains(b1_pred.predecessor, b2);
                            b2_succ.ReplaceChains(b1, b2_succ.successor);
                        }
                        else if (b2_succ == b1 && b1_pred == b2) // b2_pred -> b2 -> b1 -> b1_succ
                        {
                            b1.ReplaceChains(b2_pred, b2);
                            b2.ReplaceChains(b1, b1_succ);

                            b2_pred.ReplaceChains(b2_pred.predecessor, b1);
                            b1_succ.ReplaceChains(b2, b1_succ.successor);
                        }
                        else
                        {
                            b1.ReplaceChains(b2_pred, b2_succ);
                            b2.ReplaceChains(b1_pred, b1_succ);

                            b2_pred.ReplaceChains(b2_pred.predecessor, b1);
                            b2_succ.ReplaceChains(b1, b2_succ.successor);
                            b1_pred.ReplaceChains(b1_pred.predecessor, b2);
                            b1_succ.ReplaceChains(b2, b1_succ.successor);
                        }

                        r1.route.Add(b2);
                        r2.route.Add(b1);

                        score += increment;
                    }
                }
            }
        }
        void swapWithinDay()
        {
            // doe het in de operation, is netter
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r1 = d.routes[rnd.Next(d.routes.Count)];
            Route r2 = d.routes[rnd.Next(d.routes.Count)];

            if (r1.route.Count > 2 && r2.route.Count > 2)
            {
                Bedrijf b1 = r1.route[rnd.Next(0, r1.route.Count)]; // pak een bedrijf
                Bedrijf b2 = r2.route[rnd.Next(0, r2.route.Count)]; // pak nog een bedrijf
                
                Bedrijf b1_pred = b1.predecessor;
                Bedrijf b1_succ = b1.successor;

                Bedrijf b2_pred = b2.predecessor;
                Bedrijf b2_succ = b2.successor;

                if (b1 != b2 && b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                {
                    // bereken de increment op basis van de neighbours
                    float tijdDelta;
                    // b1_pred -> b1 -> b2 -> b2_succ
                    if (b1_succ == b2 && b2_pred == b1)
                    {
                        tijdDelta = - rijtijd(b1, b1_pred) - rijtijd(b2, b1) - rijtijd(b2_succ, b2) + rijtijd(b2, b1_pred) + rijtijd(b1, b2) + rijtijd(b2_succ, b1);
                    }
                    // b2_pred -> b2 -> b1 -> b1_succ
                    else if (b2_succ == b1 && b1_pred == b2)
                    {
                        tijdDelta = - rijtijd(b2, b2_pred) - rijtijd(b1, b2) - rijtijd(b1_succ, b1) + rijtijd(b1, b2_pred) + rijtijd(b2, b1) + rijtijd(b1_succ, b2);
                    }
                    else
                    {
                        // als ze geen neighbours van elkaar zijn doe dit.
                        tijdDelta = - rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) - rijtijd(b2, b2_pred) - rijtijd(b2_succ, b2) + rijtijd(b1, b2_pred) + rijtijd(b2_succ, b1) + rijtijd(b2, b1_pred) + rijtijd(b1_succ, b2);
                    }

                    increment = tijdDelta;

                    float capDelta1 = b2.cont * b2.vpc - b1.cont * b1.vpc;
                    float capDelta2 = b1.cont * b1.vpc - b2.cont * b2.vpc;

                    if (d.tijdsduur + increment < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                    {
                        d.tijdsduur += increment;
                        r1.capaciteit += capDelta1;
                        r2.capaciteit += capDelta2;

                        // als het buren zijn
                        // b1_pred -> b1 -> b2 -> b2_succ
                        if (b1_succ == b2 && b2_pred == b1)
                        {
                            b1.ReplaceChains(b2, b2_succ);
                            b2.ReplaceChains(b1_pred, b1);

                            b1_pred.ReplaceChains(b1_pred.predecessor, b2);
                            b2_succ.ReplaceChains(b1, b2_succ.successor);
                        }
                        else if (b2_succ == b1 && b1_pred == b2) // b2_pred -> b2 -> b1 -> b1_succ
                        {
                            b1.ReplaceChains(b2_pred, b2);
                            b2.ReplaceChains(b1, b1_succ);

                            b2_pred.ReplaceChains(b2_pred.predecessor, b1);
                            b1_succ.ReplaceChains(b2, b1_succ.successor);
                        }
                        else
                        {
                            b1.ReplaceChains(b2_pred, b2_succ);
                            b2.ReplaceChains(b1_pred, b1_succ);

                            b2_pred.ReplaceChains(b2_pred.predecessor, b1);
                            b2_succ.ReplaceChains(b1, b2_succ.successor);
                            b1_pred.ReplaceChains(b1_pred.predecessor, b2);
                            b1_succ.ReplaceChains(b2, b1_succ.successor);
                        }

                        r1.route.Remove(b1);
                        r1.route.Add(b2);
                        r2.route.Remove(b2);
                        r2.route.Add(b1);

                        score += increment;
                    }
                }
            }
        }
        void shiftWithin()
        {
            int dag = rnd.Next(5);
            int truck = rnd.Next(2);
            Dag d = trucksEnRoutes[truck][dag];
            int route = rnd.Next(d.routes.Count); // pak een willekeurige route van een dag
            Route r = d.routes[route];

            if (r.route.Count > 2) // denk niet dat dit nodig is.
            {
                int ind1 = rnd.Next(r.route.Count);
                int ind2 = rnd.Next(r.route.Count);
                if (ind1 != ind2)
                { 
                    Bedrijf b1 = r.route[ind1];
                    Bedrijf b2 = r.route[ind2]; // new predecessor

                    Bedrijf b1_pred = b1.predecessor;
                    Bedrijf b1_succ = b1.successor;

                    Bedrijf b2_pred = b2.predecessor;
                    Bedrijf b2_succ = b2.successor;

                    if (b1 != b2 && b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                    {
                        // bereken de increment op basis van de neighbours
                        float tijdDelta;
                        // b1_pred -> b1 -> b2 -> b2_succ
                        if (b1_succ == b2 && b2_pred == b1)
                        {
                            tijdDelta = -rijtijd(b1, b1_pred) - rijtijd(b2, b1) - rijtijd(b2_succ, b2) + rijtijd(b2, b1_pred) + rijtijd(b1, b2) + rijtijd(b2_succ, b1);
                        }
                        // b2_pred -> b2 -> b1 -> b1_succ
                        else if (b2_succ == b1 && b1_pred == b2)
                        {
                            tijdDelta = -rijtijd(b2, b2_pred) - rijtijd(b1, b2) - rijtijd(b1_succ, b1) + rijtijd(b1, b2_pred) + rijtijd(b2, b1) + rijtijd(b1_succ, b2);
                        }
                        else
                        {
                            // als ze geen neighbours van elkaar zijn doe dit.
                            tijdDelta = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) - rijtijd(b2_succ, b2) + rijtijd(b1, b2) + rijtijd(b2_succ, b1) + rijtijd(b1_succ, b1_pred);
                        }

                        increment = tijdDelta;

                        if (d.tijdsduur + increment < 12 * 60 && acceptIncrement())
                        {
                            d.tijdsduur += increment;

                            // als het buren zijn
                            // b1_pred -> b1 -> b2 -> b2_succ
                            if (b1_succ == b2 && b2_pred == b1)
                            {
                                b1.ReplaceChains(b2, b2_succ);
                                b2.ReplaceChains(b1_pred, b1);

                                b1_pred.ReplaceChains(b1_pred.predecessor, b2);
                                b2_succ.ReplaceChains(b1, b2_succ.successor);
                            }
                            else if (b2_succ == b1 && b1_pred == b2) // b2_pred -> b2 -> b1 -> b1_succ
                            {
                                b1.ReplaceChains(b2_pred, b2);
                                b2.ReplaceChains(b1, b1_succ);

                                b2_pred.ReplaceChains(b2_pred.predecessor, b1);
                                b1_succ.ReplaceChains(b2, b1_succ.successor);
                            }
                            else
                            {
                                b1.ReplaceChains(b2, b2_succ);
                                b2.ReplaceChains(b2_pred, b1);

                                b2_succ.ReplaceChains(b1, b2_succ.successor);
                                b1_pred.ReplaceChains(b1_pred.predecessor, b1_succ);
                                b1_succ.ReplaceChains(b1_pred, b1_succ.successor);
                            }

                            score += increment;
                        }
                    }
                }

            }
        }
        void shiftBetween()
        {
            int dagkey1 = rnd.Next(0, 5);
            int truckkey1 = rnd.Next(0, 2);
            Dag d1 = trucksEnRoutes[truckkey1][dagkey1];
            int dagkey2 = rnd.Next(0, 5);
            int truckkey2 = rnd.Next(0, 2);
            Dag d2 = trucksEnRoutes[truckkey2][dagkey2];

            int routeKey1 = rnd.Next(d1.routes.Count);
            int routeKey2 = rnd.Next(d2.routes.Count);
            Route r1 = d1.routes[routeKey1];
            Route r2 = d2.routes[routeKey2];

            if (routeKey1 != routeKey2)
            {
                int ind1 = rnd.Next(r1.route.Count);
                int ind2 = rnd.Next(r2.route.Count);
                Bedrijf b1 = r1.route[ind1];
                Bedrijf npred = r2.route[ind2]; // new predecessor

                Bedrijf b1_pred = b1.predecessor;
                Bedrijf b1_succ = b1.successor;

                Bedrijf npred_pred = npred.predecessor;
                Bedrijf npred_succ = npred.successor;

                // hou rekening met frequenties
                if (ind1 != ind2 && b1_pred != null && b1_succ != null && npred_pred != null && npred_succ != null)
                {
                    // kan nooit neighbors zijn
                    float tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) - b1.ldm; // voor dag 1
                    float tijdDelta2 = rijtijd(b1, npred) + rijtijd(npred_succ, b1) + rijtijd(b1_succ, b1_pred) - rijtijd(npred_succ, npred) + b1.ldm; // voor dag 2

                    float capDelta1 = -b1.vpc * b1.cont;
                    float capDelta2 = b1.vpc * b1.cont;

                    increment = tijdDelta1 + tijdDelta2;

                    // check constraints
                    if (d1.tijdsduur + tijdDelta1 < 12 * 60 && d2.tijdsduur + tijdDelta2 < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                    {
                        // verander chains
                        d1.tijdsduur += tijdDelta1;
                        d2.tijdsduur += tijdDelta2;
                        r1.capaciteit += capDelta1;
                        r2.capaciteit += capDelta2;

                        b1_pred.ReplaceChains(b1_pred.predecessor, b1_succ);
                        b1_succ.ReplaceChains(b1_pred, b1_succ.successor);
                        npred.ReplaceChains(npred_pred, b1);
                        npred_succ.ReplaceChains(b1, npred_succ.successor);
                        r1.route.Remove(b1);
                        r2.route.Add(b1);
                    }
                }
            }
        }
        void addRoute()
        {
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];

            Route newRoute = new Route();

            if (grabbelton.Count > 0)
            {
                int key = rnd.Next(0, grabbelton.Count);
                Bedrijf b = grabbelton[key];
                Bedrijf pred = newRoute.route[0];
                Bedrijf succ = newRoute.route[1];

                float tijdDelta = b.ldm + rijtijd(b, pred) + rijtijd(succ, b) + 30;
                increment = -(b.ldm * 3) + tijdDelta;
                float capAdd = b.vpc * b.cont;

                if (acceptIncrement() && d.tijdsduur + tijdDelta < 12 * 60 && b.freq == 1)
                {
                    newRoute.capaciteit += capAdd;

                    b.ReplaceChains(pred, succ);
                    pred.ReplaceChains(null, b);
                    succ.ReplaceChains(b, null);

                    newRoute.route.Add(b);

                    d.routes.Add(newRoute);
                    d.tijdsduur += tijdDelta;

                    grabbelton.Remove(b);

                    score += increment;
                }
            }
        }
        void addOperation()
        {
            // Kies de willekeurige dag en truck en route van die dag
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];

            if (grabbelton.Count > 0)
            {
                // pak een bedrijf uit de grabbelton
                int key = rnd.Next(0, grabbelton.Count);
                Bedrijf b = grabbelton[key];

                // selecteer een voorganger
                Bedrijf pred;
                Bedrijf succ;
                int predkey = rnd.Next(r.route.Count + 1);
                if (r.route.Count == 0)
                {
                    pred = null;
                    succ = null;
                }
                else if (predkey == 0)
                {
                    // dan moet ie voor het eerste element
                    pred = null;
                    succ = r.route[0];
                }
                else
                {
                    pred = r.route[predkey - 1];
                    succ = pred.successor;
                }

                if (b.freq == 1)
                {
                    float capDelta = b.vpc * b.cont;
                    float tijdDelta = b.ldm + rijtijd(b, pred) + rijtijd(succ, b) - rijtijd(succ, pred);

                    // stel we voegen een bedrijf toe aan een lege route moeten we nog legen bij de 
                    if (r.route.Count == 0)
                    {
                        tijdDelta += 30;
                    }
                    increment = -(b.ldm * 3) + tijdDelta;

                    // als de increment geaccepteerd worde en er wordt voldaan aan de eisen
                    if (acceptIncrement() && d.tijdsduur + tijdDelta < 12 * 60 && r.capaciteit + capDelta <= 100000)
                    {
                        // update de dag en route
                        d.tijdsduur += tijdDelta;
                        r.capaciteit += capDelta;

                        // vervang de links van de linkedlist
                        b.ReplaceChains(pred, succ);
                        if (pred != null && succ != null)
                        {
                            pred.ReplaceChains(pred.predecessor, b);
                            succ.ReplaceChains(b, succ.successor);
                        }

                        // update score en voeg toe aan route en verwijder uit grabbelton
                        score += increment;
                        r.route.Add(b);
                        grabbelton.Remove(b); // dit kan O(1), is vooral O(n)
                    }

                }
            }
        }
        void addMultiple()
        {
            if (grabbelton.Count > 0)
            {
                int key = rnd.Next(0, grabbelton.Count);
                Bedrijf b = grabbelton[key];
                int[][] dagen = [[0], [0, 3], [0, 2, 4], [0, 1, 2, 3]];
                int randomness = 0;

                bool constraints = true;
                List<float> tijdDeltas = new List<float>();
                List<float> capDeltas = new List<float>();
                List<(Bedrijf, Bedrijf)> predsucc = new List<(Bedrijf, Bedrijf)>();
                List<Dag> dagenlijst = new List<Dag>();
                List<Route> routes = new List<Route>();

                for (int i = 0; i < b.freq; i++)
                {
                    int truckkey = rnd.Next(0, 2);
                    Dag dag = trucksEnRoutes[truckkey][dagen[b.freq][i]]; // get the day, truck can be random
                    Route r = dag.routes[rnd.Next(dag.routes.Count)];

                    Bedrijf predecessor = r.route[rnd.Next(r.route.Count)]; // grab a random element from the route as the predecessor
                    Bedrijf successor = predecessor.successor; // grab a random element from the route as the predecessor

                    if (successor != null)
                    {
                        float capDelta = b.vpc * b.cont;
                        float tijdDelta = b.ldm + rijtijd(b, predecessor) + rijtijd(successor, b) - rijtijd(successor, predecessor);

                        // hou een boolean bij die zegt of er voor elke dag aan de constraints voldaan wordt. 
                        constraints = constraints && (r.capaciteit + capDelta <= 100000) && (dag.tijdsduur + tijdDelta < 12 * 60);
                        capDeltas.Add(capDelta);
                        tijdDeltas.Add(tijdDelta);
                        predsucc.Add((predecessor, successor));

                        dagenlijst.Add(dag);
                        routes.Add(r);
                    }
                }

                increment = -(b.ldm * 3) + tijdDeltas.Sum(); // hou hier geen rekening met de frequentie

                if (acceptIncrement() && constraints)
                {
                    for (int i = 0; i < b.freq; i++)
                    {
                        dagenlijst[i].tijdsduur += tijdDeltas[i];
                        routes[i].capaciteit += capDeltas[i];

                        Bedrijf bclone = new Bedrijf(b.inputArray);
                    }
                    score += increment;
                }
            }
        }
        
        float rijtijd(Bedrijf b, Bedrijf pred)
        {
            if (b == null && pred == null) { return 0; }
            if (pred == null)
            {
                return distDict[287, b.matrixID];
            }
            if (b == null)
            {
                return distDict[pred.matrixID, 287];
            }
            return distDict[pred.matrixID, b.matrixID];
        }

    }
    public class Dag
    {
        public float tijdsduur; // in minuten
        public List<Route> routes;
        public Dag()
        {
            routes = new List<Route>();
            routes.Add(new Route());
            tijdsduur = 0;
        }
        public List<string> makeString() 
        {
            List<string> res = new List<string>();
            foreach (Route route in routes)
            {
                if (route.route.Count != 0)
                {
                    Bedrijf start = null;
                    foreach (Bedrijf b in route.route)
                    {
                        if (b.predecessor == null) { start = b; break; }
                    }
                    if (start == null) { continue; }
                    List<string> routeRes = route.makeString(start, res.Count);
                    routeRes.Reverse();
                    foreach (string s in routeRes)
                    {
                        res.Add(s);
                    }
                }
            }
            return res;
        }
    }
    public class Route
    {
        public List<Bedrijf> route;
        //public List<Bedrijf>[] route;
        public float capaciteit;
        public Route()
        {
            route = new List<Bedrijf>();
        }
        public void maakLinkedList()
        {
            // roep dit aan om de linked list structuur te maken in de route lijst
            for (int i = 2; i < route.Count; i++)
            {
                Bedrijf pred;
                if (i == 2)
                {
                    pred = route[0];
                }
                else
                {
                    pred = route[i - 1];
                }
                Bedrijf succ;
                if (i + 1 == route.Count)
                {
                    succ = route[1];
                }
                else
                {
                    succ = route[i + 1];
                }
                Bedrijf b = route[i];
                pred.ReplaceChains(pred.predecessor, b);
                succ.ReplaceChains(b, succ.successor);
                b.ReplaceChains(pred, succ);
            }
        }
        // dit is een recursieve functie bedoelt om de route uit te lezen en er een lijst van strings van te maken die te gebruiken valt in de checker
        public List<string> makeString(Bedrijf b, int place)
        {
            if (b.successor == null) { return [$"{place}; 0"]; }

            List<string> res = makeString(b.successor, place + 1);
            if (b.predecessor == null) { return res; }
            res.Add($"{place}; {b.order}");
            return res;
        } 
    }
    public class Bedrijf
    {
        public Bedrijf? successor; // kan null zijn
        public Bedrijf? predecessor; // nodig voor als je een bedrijf weghaalt uit je route
        public int order;
        public int freq;
        public int cont;
        public int vpc;
        public float ldm;
        public int matrixID;
        public string[] inputArray;
        public Bedrijf(string[] parts)
        {
            order = int.Parse(parts[0]);
            freq = int.Parse(parts[2][0].ToString());
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5].Replace('.', ','));
            matrixID = int.Parse(parts[6]);
            inputArray = parts;
        }

        public void ReplaceChains(Bedrijf otherPred, Bedrijf otherSucc)
        {
            predecessor = otherPred;
            successor = otherSucc;
        }
    }
}