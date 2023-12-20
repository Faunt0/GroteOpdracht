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


            Dictionary<(int, int), float> fileDict = new Dictionary<(int, int), float>(); // maak lijst van
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
                    fileDict[(id1, id2)] = tijd;
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
                    Route r = new Route(oplossing.stort);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Replace(" ", "").Split(";");
                        Dag dag = oplossing.trucksEnRoutes[int.Parse(parts[0]) - 1][int.Parse(parts[1]) - 1];

                        if (int.Parse(parts[3]) == 0)
                        {
                            r.maakLinkedList();
                            dag.routes.Add(r);

                            r = new Route(oplossing.stort);
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

            string save = "-";
            while (save == "-" || save == "4" || save == "5")
            {
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
                    List<string> lines = new List<string>();

                    for (int t = 0; t < 2; t++)
                    {
                        for (int d = 0; d < 5; d++)
                        {
                            List<Route> routes = oplossing.trucksEnRoutes[t][d].routes;

                            int index = 0;
                            List<string> strings = new List<string>();
                            foreach (Route r in routes)
                            {
                                List<string> res = r.makeString(r.route[0], index);
                                res.Reverse();
                                foreach (string s in res) { strings.Add(s); }

                                index += res.Count;
                            }

                            foreach (string s in strings)
                            {
                                //Console.WriteLine($"{t}; {d}; " + s);
                                lines.Add($"{t + 1}; {d + 1}; " + s);
                            }
                        }
                    }
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
                    List<string> lines = new List<string>();

                    for (int t = 0; t < 2; t++)
                    {
                        for (int d = 0; d < 5; d++)
                        {
                            List<Route> routes = oplossing.beste.Item2[t][d].routes;

                            int index = 0;
                            List<string> strings = new List<string>();
                            foreach (Route r in routes)
                            {
                                List<string> res = r.makeString(r.route[0], index);
                                res.Reverse();
                                foreach (string s in res) { strings.Add(s); }

                                index += res.Count;
                            }

                            foreach (string s in strings)
                            {
                                //Console.WriteLine($"{t}; {d}; " + s);
                                lines.Add($"{t + 1}; {d + 1}; " + s);
                            }
                        }
                    }
                    Console.WriteLine("Name the save file:\t");
                    string fileName = Console.ReadLine();
                    StreamWriter sw = new StreamWriter($"./oplossingen/{fileName}.txt");
                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }
                else if (save == "4")
                {
                    oplossing.tellertje = 0;
                    oplossing.T = 60;
                }
                else if (save == "5")
                {
                    oplossing.trucksEnRoutes = oplossing.beste.Item2;
                    oplossing.tellertje = 0;
                    oplossing.T = 0;
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
        public Dictionary<(int, int), float> distDict;
        public Dag[][] trucksEnRoutes;
        public (float, Dag[][]) beste;
        public List<Bedrijf>? grabbelton;
        public string[] stort;
        public int tellertje;
        public float score;
        public int plateauCount;
        public float increment;
        public long iterations = 10000000;
        public float T = 60;
        public int Q = 200000;
        public float alpha;
        public Random rnd;
        public Oplossing(Dictionary<(int, int), float> dictInput, Random rndIn)
        {

            stort = ["0", "-", "0", "0", "0", "0", "287"];
            trucksEnRoutes = [[new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)], [new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)]];
            distDict = dictInput;
            alpha = 0.99F;
            rnd = rndIn;
            plateauCount = 0;
            beste = (1000000, trucksEnRoutes);
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

        public void ILS()
        {
            // simpeler begin
            while (tellertje < iterations && T > 0.00005F)
            {
                float oldscore = score;
                int operation = rnd.Next(6);
                switch (operation)
                {
                    //case 0: // werkt niet helemaal lijkt het, is nogsteeds een klein beetje off
                    //    swapWithinDay();
                    //    break;
                    case 4: // lijkt te werken
                        swapBetweenDays();
                        break;
                    case 2: // werkt
                        replaceStop();
                        break;
                    //case 3: // werkt
                    //    removeStop();
                    //    break;
                    case 1: // werkt
                        addRoute();
                        break;
                    default:
                        addOperation();
                        break;

                    // replace from another route
                    // merge two routes
                }

                if (oldscore == score)
                {
                    plateauCount++;
                } else { plateauCount = 0; }

                if (score < beste.Item1)
                {
                    beste = (score, trucksEnRoutes);
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
                return true; // heb ik niet alleen dit nodig, zodat ik ook de kans van acceptatie van een verbetering implementeer
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
                Bedrijf b1 = r.route[rnd.Next(r.route.Count)]; // pak een bedrijf
                Bedrijf b2 = grabbelton[rnd.Next(grabbelton.Count)]; // kan nooit voor het eerste element komen

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

            if (r1.route.Count > 2 && r2.route.Count > 2 && truckkey1 != truckkey2) // waarom mogen de truckkeys niet gelijk zijn?
            {
                Bedrijf b1 = r1.route[rnd.Next(r1.route.Count)]; // pak een bedrijf
                Bedrijf b2 = r2.route[rnd.Next(r2.route.Count)]; // pak nog een bedrijf

                Bedrijf b1_pred = b1.predecessor; // grab a random element from the route as the predecessor
                Bedrijf b1_succ = b1.successor; // grab a random element from the route as the predecessor
                Bedrijf b2_pred = b2.predecessor;
                Bedrijf b2_succ = b2.successor;

                if (b1.freq == 1 && b2.freq == 1 && b1 != b2 && b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                {
                    // bereken de increment

                    // dit is alleen mogelijk als de bedrijven hetzelfde zijn.
                    float tijdDelta1; // het verschil in tijd voor dag 1
                    float tijdDelta2; // het verschil in tijd voor dag 2

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
                        // stel het zijn geen neighbours dan is het ongeacht de dag het volgende:

                        tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b2, b1_pred) + rijtijd(b1_succ, b2) - b1.ldm + b2.ldm;
                        tijdDelta2 = -rijtijd(b2, b2_pred) - rijtijd(b2_succ, b2) + rijtijd(b1, b2_pred) + rijtijd(b2_succ, b1) - b2.ldm + b1.ldm;

                        //tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b1, b2_pred) + rijtijd(b2_succ, b1) + b1.ldm - b2.ldm;
                        //tijdDelta2 = -rijtijd(b2, b2_pred) - rijtijd(b2_succ, b2) + rijtijd(b2, b1_pred) + rijtijd(b1_succ, b2) - b2.ldm + b1.ldm;
                    }

                    increment = tijdDelta1 + tijdDelta2;

                    float capDelta1 = b2.cont * b1.vpc - b1.cont * b1.vpc;
                    float capDelta2 = b1.cont * b1.vpc - b2.cont * b2.vpc;

                    if (d1.tijdsduur + tijdDelta1 < 12 * 60 && d2.tijdsduur + tijdDelta2 < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                    {
                        d1.tijdsduur += tijdDelta1;
                        d2.tijdsduur += tijdDelta2;

                        r1.capaciteit += capDelta1;
                        r2.capaciteit += capDelta2;

                        r1.route.Remove(b1);
                        r2.route.Remove(b2);

                        //b1_pred.ReplaceChains(b1_pred.predecessor, b2);
                        //b1_succ.ReplaceChains(b2, b1_succ.successor);

                        //b2_pred.ReplaceChains(b2_pred.predecessor, b1);
                        //b2_succ.ReplaceChains(b1, b2_succ.successor);

                        //b1.ReplaceChains(b2_pred, b2_succ);
                        //b2.ReplaceChains(b1_pred, b1_succ);


                        // in case the two are neighbors, where order matters
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
                
                Bedrijf b1_pred = b1.predecessor; // grab a random element from the route as the predecessor
                Bedrijf b1_succ = b1.successor; // grab a random element from the route as the predecessor

                Bedrijf b2_pred = b2.predecessor;
                Bedrijf b2_succ = b2.successor;

                if (b1.freq == 1 && b2.freq == 1 && b1 != b2 && b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
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

                        // in case the two are neighbors, where order matters
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
        void addRoute()
        {
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];

            Route newRoute = new Route(stort);

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
            // doe het in de operation, is netter
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);
            //dagkey = 0;
            //truckkey = 0;

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];
            //r = d.routes[0];

            if (grabbelton.Count > 0)
            { 
                int key = rnd.Next(0, grabbelton.Count);
                //key = 2;
                Bedrijf b = grabbelton[key]; // kan nooit voor het eerste element komen
                
                Bedrijf predecessor = r.route[rnd.Next(r.route.Count)]; // grab a random element from the route as the predecessor
                Bedrijf successor = predecessor.successor; // grab a random element from the route as the predecessor

                if (successor != null && b.freq == 1)
                {
                    float capDelta = b.vpc * b.cont;
                    float tijdDelta = b.ldm + rijtijd(b, predecessor) + rijtijd(successor, b) - rijtijd(successor, predecessor);

                    if (r.route.Count == 2)
                    {
                        tijdDelta += 30;
                    }
                    increment = -(b.ldm * 3) + tijdDelta;

                    if (acceptIncrement() && d.tijdsduur + tijdDelta < 12 * 60 && r.capaciteit + capDelta <= 100000)
                    {
                        d.tijdsduur += tijdDelta;
                        r.capaciteit += capDelta;

                        b.ReplaceChains(predecessor, successor);
                        predecessor.ReplaceChains(predecessor.predecessor, b);
                        successor.ReplaceChains(b, successor.successor);

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
            return distDict[(pred.matrixID, b.matrixID)];

            float naar;
            if (pred != null)
            { 
                naar = distDict[(pred.matrixID, b.matrixID)];
            } else
            {
                naar = distDict[(287, b.matrixID)]; // gebruik de stort als predecessor als die er niet is
            }

            float kosten = naar;
            return kosten;
        }

    }
    public class Route
    {
        public List<Bedrijf> route;
        public float capaciteit;
        // willen we een laatste element bijhouden?
        public Route(string[] stort)
        {
            route = new List<Bedrijf>();
            Bedrijf s1 = new Bedrijf(stort);
            Bedrijf s2 = new Bedrijf(stort);
            s1.successor = s2;
            s2.predecessor = s1;
            route.Add(s1);
            route.Add(s2);
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
        public int cont; // kan misschien beter in een ding door vpc * cont te doen als totaal volume.
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
    public class Dag
    {
        public float tijdsduur; // in minuten
        public List<Route> routes;
        public Dag(string[] stort)
        {
            routes = new List<Route>();
            routes.Add(new Route(stort));
            //tijdsduur = 30;
            tijdsduur = 0;
        }
    }
}