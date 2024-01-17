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
            // grabbelton:
            Dictionary<int, Bedrijf> grabbelton = new Dictionary<int, Bedrijf>();
            Dictionary<int, Bedrijf> grabbeltonFreq234 = new Dictionary<int, Bedrijf>();
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
                // print all files to choose from
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
                    List<int> f234Orders = new List<int>();

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Replace(" ", "").Split(";");
                        Dag dag = oplossing.trucksEnRoutes[int.Parse(parts[0]) - 1][int.Parse(parts[1]) - 1];

                        if (int.Parse(parts[3]) == 0)
                        {
                            //r.route.Reverse();
                            r.maakLinkedList();
                            dag.routes.Add(r);
                            dag.tijdsduur += 30;

                            r = new Route(oplossing.stort);
                            r.capaciteit = 0;
                        }
                        else
                        {
                            Bedrijf b = grabbelton.ContainsKey(int.Parse(parts[3])) ? grabbelton[int.Parse(parts[3])] : grabbeltonFreq234[int.Parse(parts[3])];
                            if (b.freq == 1)
                            {
                                grabbelton.Remove(b.order); // dit kan niet voor dingen die vaker voor moeten komen. maar wat doe je dan
                            }
                            else
                            {
                                // maak de bedrijven met freq>1 uniek in de route zodat ze unieke pred en succ hebben.
                                b.dagkey = int.Parse(parts[1]);
                                f234Orders.Add(b.order);
                            }

                            r.route.Add(b);
                            r.capaciteit += b.cont * b.vpc;
                        }
                    }

                    // verwijder bedrijven met freq > 1 uit de bijbehorende grabbelton
                    foreach (int order in f234Orders)
                    {
                        grabbeltonFreq234.Remove(order);
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
                    oplossing.T = 12;
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
    public class Oplossing
    {
        public double[,] distDict;
        public Dag[][] trucksEnRoutes;
        public (double, List<string>) beste;
        public List<Bedrijf>? grabbelton;
        public List<Bedrijf>? grabbeltonFreq234;
        public string[] stort;
        public int tellertje;
        public double score;
        public int plateauCount;
        public double increment;
        public long iterations = 4000000000;
        public double T = 12;
        public int Q = 5000000;
        public double alpha;
        public Random rnd;
        public Oplossing(double[,] dictInput, Random rndIn)
        {

            stort = ["0", "-", "0", "0", "0", "0", "287"];
            trucksEnRoutes = [[new Dag(stort, 2), new Dag(stort), new Dag(stort), new Dag(stort, 2), new Dag(stort)], [new Dag(stort, 2), new Dag(stort), new Dag(stort), new Dag(stort, 2), new Dag(stort)]];
            distDict = dictInput;
            alpha = 0.99F;
            rnd = rndIn;
            plateauCount = 0;
            List<string> first = makeString(trucksEnRoutes);
            beste = (1000000, first);
        }

        public void beginScore()
        {
            double travelTime = 0;
            double grabbeltonKosten = 0;
            foreach (Bedrijf b in grabbelton)
            {
                grabbeltonKosten += b.ldm * 3 * b.freq;
            }
            foreach (Bedrijf b in grabbeltonFreq234)
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
                            
                            double routekosten = 30;
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
        public double recurseRoute(Bedrijf b)
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
                double oldscore = score;
                int op = rnd.Next(100);

                // misschien todo: laat alle kansen afhangen van het aantal iteraties
                if (0 <= op && op < 5 && false)          { removeStop(); }
                else if (5 <= op && op < 10)    { addMultiple(); }
                else if (10 <= op && op < 15)   { addOperation(); }
                else if (20 <= op && op < 60)   { shiftBetween(); }
                else if (60 <= op && op < 100)   { shiftWithin(); }


                if (oldscore == score) { plateauCount++; } else { plateauCount = 0; }
                if (score < beste.Item1) { beste = (score, makeString(trucksEnRoutes)); }

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
            if (increment <= 0)
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
                    double tijdDelta = 0;

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
                        double capDelta = -b.cont * b.vpc;
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
                Bedrijf predecessor = r.route[rnd.Next(r.route.Count)]; 
                Bedrijf successor = predecessor.successor;

                // we mogen niet de laatste stort als opvolger hebben
                if (successor != null && b.freq == 1)
                {
                    double capDelta = b.vpc * b.cont;
                    double tijdDelta = b.ldm + rijtijd(b, predecessor) + rijtijd(successor, b) - rijtijd(successor, predecessor);

                    // stel we voegen een bedrijf toe aan een lege route moeten we nog legen bij de stort
                    if (r.route.Count == 2) { tijdDelta += 30; }
                    increment = -(b.ldm * 3) + tijdDelta;

                    // als de increment geaccepteerd worde en er wordt voldaan aan de eisen
                    if (acceptIncrement() && d.tijdsduur + tijdDelta < 12 * 60 && r.capaciteit + capDelta <= 100000)
                    {
                        // update de dag en route
                        d.tijdsduur += tijdDelta;
                        r.capaciteit += capDelta;

                        // vervang de links van de linkedlist
                        b.ReplaceChains(predecessor, successor);
                        predecessor.ReplaceChains(predecessor.predecessor, b);
                        successor.ReplaceChains(b, successor.successor);

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
            if (grabbeltonFreq234.Count > 0)
            {
                int truckkey = rnd.Next(0, 2); // needs to be different
                int key = rnd.Next(grabbeltonFreq234.Count);
                Bedrijf b = grabbeltonFreq234[key];
                if (b.freq > 1)
                {
                    int[][] freq2dagen = [[0, 3], [1, 4]]; // alleen voor freq == 2
                    int[] freq3dagen = [0, 2, 4];
                    int[][] freq4dagen = [[0, 1, 2, 3], [1, 2, 3, 4], [0, 2, 3, 4], [0, 1, 3, 4], [0, 1, 2, 4]];


                    increment = 0;

                    int[] dagIndexen = new int[5];
                    if (b.freq == 2) { dagIndexen = freq2dagen[rnd.Next(0, b.freq)]; }
                    else if (b.freq == 3) { dagIndexen = freq3dagen; }
                    else if (b.freq == 4) { dagIndexen = freq4dagen[rnd.Next(5)]; }

                    int[] routeIndexen = new int[5];
                    double[] tijdDeltas = new double[5];
                    double[] capDeltas = new double[5];
                    (Bedrijf, Bedrijf)[] predsucc = new (Bedrijf, Bedrijf)[5];
                    bool constraints = true;
                    // voor willekeurige trucks en routes voeg toe aan die dag
                    foreach (int i in dagIndexen)
                    {
                        Dag d = trucksEnRoutes[truckkey][i];
                        int routeKey = rnd.Next(d.routes.Count);
                        Route r = d.routes[routeKey];
                        Bedrijf succ = r.route[rnd.Next(1, r.route.Count)]; // laatste stort is mogelijke successor
                        Bedrijf ns_pred = succ.predecessor;

                        double tijdDelta;
                        tijdDelta = -rijtijd(succ, ns_pred) + rijtijd(b, ns_pred) + rijtijd(succ, b) + b.ldm;
                        if (r.route.Count == 2) { tijdDelta += 30; }
                        if (d.tijdsduur + tijdDelta < 12 * 60) { tijdDeltas[i] = tijdDelta; } else { constraints = false; break; }

                        double capDelta;
                        capDelta = b.vpc * b.cont;
                        if (r.capaciteit + capDelta < 100000) { capDeltas[i] = capDelta; } else { constraints = false; break; }

                        predsucc[i] = (ns_pred, succ);
                        routeIndexen[i] = routeKey;
                    }

                    if (constraints)
                    {
                        increment = tijdDeltas.Sum() - (b.ldm * 3 * b.freq);

                        if (acceptIncrement())
                        {
                            foreach (int dagkey in dagIndexen)
                            {
                                Bedrijf b_clone = new Bedrijf(b.inputArray);
                                b_clone.dagkey = dagkey;

                                Dag d = trucksEnRoutes[truckkey][dagkey];
                                Route r = d.routes[routeIndexen[dagkey]];

                                Bedrijf succ = predsucc[dagkey].Item2;
                                Bedrijf ns_pred = predsucc[dagkey].Item1;

                                d.tijdsduur += tijdDeltas[dagkey];
                                r.capaciteit += capDeltas[dagkey];

                                b_clone.ReplaceChains(ns_pred, succ);
                                succ.ReplaceChains(b_clone, succ.successor);
                                ns_pred.ReplaceChains(ns_pred.predecessor, b_clone);

                                r.route.Add(b_clone);
                            }

                            score += increment;
                            grabbeltonFreq234.Remove(b);
                        }
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
                int ind1 = rnd.Next(2, r.route.Count); // sla de storten over
                int ind2 = rnd.Next(1, r.route.Count); // voor de succ moet aan het einde zijn.
                int freqin1 = rnd.Next(4);
                int freqin2 = rnd.Next(4);

                if (ind1 != ind2 && r.route[ind1].successor != r.route[ind2])
                {
                    Bedrijf b1 = r.route[ind1];
                    Bedrijf b1_pred = b1.predecessor;
                    Bedrijf b1_succ = b1.successor;

                    Bedrijf newSucc = r.route[ind2];
                    Bedrijf ns_pred = newSucc.predecessor;

                    // bereken de increment op basis van de neighbours
                    double tijdDelta;
                    tijdDelta = -rijtijd(b1_succ, b1) - rijtijd(b1, b1_pred) - rijtijd(newSucc, ns_pred) + rijtijd(b1, ns_pred) + rijtijd(newSucc, b1);
                    // ns_pred -> newSucc -> b1 -> b1_succ // dit is de enige case die we hoeven te overwegen
                    if (b1_pred == newSucc)
                    {
                        tijdDelta += rijtijd(b1_succ, newSucc);
                    }
                    else
                    {
                        // als ze geen neighbours van elkaar zijn doe dit.
                        tijdDelta += rijtijd(b1_succ, b1_pred);
                    }

                    increment = tijdDelta;

                    if (d.tijdsduur + increment < 12 * 60 && acceptIncrement())
                    {
                        d.tijdsduur += increment;

                        // als het buren zijn
                        if (b1_pred == newSucc) // ns_pred -> newSucc -> b1 -> b1_succ
                        {
                            b1.ReplaceChains(ns_pred, newSucc);
                            b1_succ.ReplaceChains(newSucc, b1_succ.successor);

                            newSucc.ReplaceChains(b1, b1_succ);
                            ns_pred.ReplaceChains(ns_pred.predecessor, b1);
                        }
                        else
                        {
                            b1.ReplaceChains(ns_pred, newSucc);
                            b1_pred.ReplaceChains(b1_pred.predecessor, b1_succ);
                            b1_succ.ReplaceChains(b1_pred, b1_succ.successor);

                            newSucc.ReplaceChains(b1, newSucc.successor);
                            ns_pred.ReplaceChains(ns_pred.predecessor, b1);
                        }

                        score += increment;
                    }
                    
                }

            }
        }
        void shiftBetween()
        {
            // werkt alleen voor frequentie 1
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

            if (routeKey1 != routeKey2 && r1.route.Count > 2)
            {
                int ind1 = rnd.Next(2, r1.route.Count);
                int ind2 = rnd.Next(1, r2.route.Count);
                Bedrijf b1 = r1.route[ind1];
                if (b1.freq != 1) { return; }
                Bedrijf newSucc = r2.route[ind2]; // new successor

                Bedrijf b1_pred = b1.predecessor;
                Bedrijf b1_succ = b1.successor;

                Bedrijf newSucc_pred = newSucc.predecessor;

                // kan nooit neighbors zijn
                double tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b1_succ, b1_pred) - b1.ldm; // voor dag 1
                double tijdDelta2 = -rijtijd(newSucc, newSucc_pred) + rijtijd(b1, newSucc_pred) + rijtijd(newSucc, b1) + b1.ldm; // voor dag 2

                double capDelta1 = -b1.vpc * b1.cont;
                double capDelta2 = b1.vpc * b1.cont;
                if (r1.route.Count == 3)
                {
                    tijdDelta1 -= 30;
                }
                if (r2.route.Count == 2)
                {
                    tijdDelta2 += 30;
                }

                increment = tijdDelta1 + tijdDelta2;

                // check constraints
                if (d1.tijdsduur + tijdDelta1 < 12 * 60 && d2.tijdsduur + tijdDelta2 < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                {
                    // verander chains
                    d1.tijdsduur += tijdDelta1;
                    d2.tijdsduur += tijdDelta2;
                    r1.capaciteit += capDelta1;
                    r2.capaciteit += capDelta2;

                    newSucc.ReplaceChains(b1, newSucc.successor);
                    newSucc_pred.ReplaceChains(newSucc_pred.predecessor, b1);

                    b1.ReplaceChains(newSucc_pred, newSucc);
                    b1_pred.ReplaceChains(b1_pred.predecessor, b1_succ);
                    b1_succ.ReplaceChains(b1_pred, b1_succ.successor);

                    r1.route.Remove(b1);
                    r2.route.Add(b1);

                    score += increment;
                }

            }
        }

        double rijtijd(Bedrijf b, Bedrijf pred)
        {
            return distDict[pred.matrixID, b.matrixID];
        }

    }
    public class Route
    {
        public List<Bedrijf> route;
        public double capaciteit;
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
            bool xdafsfs = checkCycle(route[0], new List<int>());
        }
        // dit is een recursieve functie bedoelt om de route uit te lezen en er een lijst van strings van te maken die te gebruiken valt in de checker
        public List<string> makeString(Bedrijf b, int place)
        {
            //checkCycle(b, new List<int>());
            if (b.successor == null) { return [$"{place}; 0"]; }

            List<string> res = makeString(b.successor, place + 1);
            if (b.predecessor == null) { return res; }
            res.Add($"{place}; {b.order}");
            return res;
        }
        public bool checkCycle(Bedrijf b, List<int> visited)
        {
            if (b.successor == null) { return false; }
            if (visited.Contains(b.matrixID) && b.matrixID != 287) 
            {
                throw new Exception("There has been found a cycle!");
            }
            visited.Add(b.matrixID);
            return checkCycle(b.successor, visited);
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
        public double ldm;
        public int matrixID;
        public int dagkey;
        public string[] inputArray;
        public Bedrijf(string[] parts)
        {
            order = int.Parse(parts[0]);
            freq = int.Parse(parts[2][0].ToString());
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = double.Parse(parts[5].Replace('.', ','));
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
        public double tijdsduur; // in minuten
        public List<Route> routes;
        public Dag(string[] stort, int numRoute=1)
        {
            routes = new List<Route>();
            for (int i = 0; i < numRoute; i++)
            {
                routes.Add(new Route(stort));
            }
            tijdsduur = 0;
        }
        public List<string> makeString()
        {
            List<string> res = new List<string>();
            foreach (Route route in routes)
            {
                List<string> routeRes = route.makeString(route.route[0], res.Count);
                foreach (string s in routeRes)
                {
                    res.Add(s);
                }
            }
            return res;
        }
    }
}