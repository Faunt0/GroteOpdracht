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
            float totaalLDM = 0;
            List<Bedrijf> grabbelton = new List<Bedrijf>();
            using (var reader = new StreamReader("../../../orderbestand.txt"))
            {
                string firstline = reader.ReadLine();
                Console.WriteLine($"{firstline}");

                int index = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(';');
                    Bedrijf bv = new Bedrijf(parts);

                    grabbelton.Add(bv);
                    totaalLDM += bv.ldm * 3;
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
                    //int afstand = int.Parse(parts[2]); // eigenlijk niet nodig ivm de rijtijd
                    float tijd = int.Parse(parts[3]) / 60; // is in seconden maar de rest niet
                    //afstandenDict[(id1, id2)] = (afstand, tijd);
                    fileDict[(id1, id2)] = tijd;
                }
            }
            Console.WriteLine(fileDict[(0,1)]);
            
            Random rndin = new Random(1);
            Oplossing oplossing = new Oplossing(fileDict, rndin);
            oplossing.grabbelton = grabbelton;
            oplossing.beginScore(grabbelton, fileDict);
            Console.WriteLine($"score eerst: {oplossing.score}");

            DateTime before = DateTime.Now;
            oplossing.ILS();
            DateTime after = DateTime.Now;
            TimeSpan ts = after - before;

            Console.WriteLine($"score daarna: {oplossing.score}");
            Console.WriteLine($"Milliseconden:\t{ts.TotalMilliseconds}");
            Console.WriteLine($"Iteraties:\t{oplossing.tellertje}");
            float speed = oplossing.tellertje / ((float)ts.TotalMilliseconds / 1000);
            Console.WriteLine($"speed:\t\t{speed} iterations/s");
            Console.WriteLine($"T waarde:\t{oplossing.T}");
            Console.WriteLine($"plateau count:\t{oplossing.plateauCount}");
            Console.WriteLine("Oplossing Opslaan? [y/n]:\t");
            string save = Console.ReadLine();
            if (save == "y")
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
        }

    }
    public class Oplossing
    {
        public Dictionary<(int, int), float> distDict;
        public Dag[][] trucksEnRoutes;
        public (float, Dag[][] trucksEnRoutes) beste;
        public List<Bedrijf>? grabbelton; // nullable
        public string[] stort;
        public int tellertje;
        public float score;
        public int plateauCount;
        public float increment;
        public float T = 8000;
        public int Q = 20000;
        public float alpha;
        public Random rnd;
        public Oplossing(Dictionary<(int, int), float> dictInput, Random rndIn)
        {

            stort = ["0", "-", "0", "0", "0", "0", "287"];
            trucksEnRoutes = [[new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)], [new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)]];
            distDict = dictInput;
            alpha = 0.95F;
            rnd = rndIn;
            plateauCount = 0;
            beste = (1000000, trucksEnRoutes);
        }

        public void beginScore(List<Bedrijf> grabbelton, Dictionary<(int, int), float> distDict)
        {
            float routekosten = 0;
            float grabbeltonKosten = 0;
            foreach (Bedrijf b in grabbelton)
            {
                grabbeltonKosten += b.ldm * 3;
            }
            for (int truck = 0; truck < 2; truck++)
            {
                for (int i = 0; i < 5; i++)
                {
                    // calculate the score of each route in the beginning

                    Dag d = trucksEnRoutes[truck][i];
                    if (d.routes == null) continue;
                    foreach(Route r in d.routes)
                    {
                        if (r.route.Count > 2)
                        {
                            routekosten += 30;
                            foreach (Bedrijf b in r.route)
                            {
                                if (b.predecessor == null)
                                {
                                    routekosten += recurseRoute(b);
                                    break;
                                }
                            }
                        }
                    }

                }
            }
            score = routekosten + grabbeltonKosten;
        }
        public float recurseRoute(Bedrijf b)
        {
            if (b.successor == null)
            {
                return 0;
            }
            return rijtijd(b.successor, b) + recurseRoute(b.successor);
        }

        public void ILS()
        {
            // simpeler begin
            int its = 10000;
            while (tellertje < its && T > 0.00005F)
            {
                float oldscore = score;
                int operation = rnd.Next(4);
                switch (operation)
                {
                    //case 0:
                    //    swapWithinDay();
                    //    break;
                    //case 1:
                    //    swapBetweenDays();
                    //    break;
                    //case 2:
                    //    replaceStop();
                    //    break;
                    //case 3:
                    //    removeStop();
                    //    break;
                    //case 1:
                    //    addRoute();
                    //    break;
                    default:
                        addOperation();
                        break;
                }

                if (oldscore == score)
                {
                    plateauCount++;
                }

                if (score < beste.Item1)
                {
                    beste = (score, trucksEnRoutes);
                }

                tellertje++;

                if (tellertje % Q == 0)
                {
                    T = T * alpha;
                    //Console.WriteLine($"{Q} stappen gezet\t\tT = {T}");
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
            if (Math.Exp(increment / T) < rnd.NextDouble())
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

                if (b.successor != null && b.predecessor != null) // haal niet een van de stort weg
                {
                    if (r.route.Count == 3)
                    {
                        // verwijder ook de route uit de dag als er maar 1 stop is
                        float capDelta = -b.cont * b.vpc;
                        increment = b.ldm * 2 - rijtijd(b, b.predecessor) - rijtijd(b.successor, b) - 30;

                        if (acceptIncrement())
                        {
                            grabbelton.Add(b);
                            d.routes.Remove(r);
                            score += increment;
                        }
                    }
                    else
                    {
                        float tijdDelta = rijtijd(b.successor, b.predecessor) - rijtijd(b, b.predecessor) - rijtijd(b.successor, b) + b.ldm * 2;
                        float capDelta = -b.cont * b.vpc;
                        increment = tijdDelta;

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
            int dagkey1 = rnd.Next(0, 5);
            int truckkey1 = rnd.Next(0, 2);
            Dag d1 = trucksEnRoutes[truckkey1][dagkey1];
            Route r1 = d1.routes[rnd.Next(d1.routes.Count)];


            if (r1.route.Count > 2 && grabbelton.Count > 2)
            {
                Bedrijf b1 = r1.route[rnd.Next(0, r1.route.Count)]; // pak een bedrijf
                Bedrijf b2 = grabbelton[rnd.Next(0, grabbelton.Count)]; // kan nooit voor het eerste element komen

                if (b1 != b2 && b1.successor != null && b1.predecessor != null)
                {
                    // werk niet volledig
                    float inc = -b2.ldm * 2 + b1.ldm * 2 + rijtijd(b2, b1.predecessor) + rijtijd(b1.successor, b2) - rijtijd(b1, b1.predecessor) - rijtijd(b1.successor, b1);
                    float capDelta = - b1.cont * b1.vpc + b2.cont * b2.vpc;

                    increment = inc;
                    if (acceptIncrement())
                    {
                        d1.tijdsduur += inc;
                        r1.capaciteit += capDelta;

                        grabbelton.Remove(b2);
                        r1.route.Remove(b1);

                        b2.ReplaceChains(b1.predecessor, b1.successor);

                        grabbelton.Add(b1);
                        r1.route.Add(b2);

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
                Bedrijf b1 = r1.route[rnd.Next(0, r1.route.Count)]; // pak een bedrijf
                Bedrijf b2 = r2.route[rnd.Next(0, r2.route.Count)]; // pak nog een bedrijf


                Bedrijf b1_pred = b1.predecessor; // grab a random element from the route as the predecessor
                Bedrijf b1_succ = b1.successor; // grab a random element from the route as the predecessor

                Bedrijf b2_pred = b2.predecessor;
                Bedrijf b2_succ = b2.successor;

                if (b1.freq == 1 && b2.freq == 1 && b1 != b2 && b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                {
                    // bereken de increment
                    float inc1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b1, b2_pred) + rijtijd(b2_succ, b1);
                    float inc2 = -rijtijd(b2, b2_pred) - rijtijd(b2_succ, b2) + rijtijd(b2, b1_pred) + rijtijd(b1_succ, b2);

                    increment = inc1 + inc2;

                    float capDelta1 = b2.cont * b1.vpc - b1.cont * b1.vpc;
                    float capDelta2 = b1.cont * b1.vpc - b2.cont * b2.vpc;

                    if (d1.tijdsduur + inc1 < 12 * 60 && d2.tijdsduur + inc2 < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                    {
                        d1.tijdsduur += inc1;
                        d2.tijdsduur += inc2;
                        r1.capaciteit += capDelta1;
                        r2.capaciteit += capDelta2;

                        b1.ReplaceChains(b2_pred, b2_succ);
                        b2.ReplaceChains(b1_pred, b1_succ);

                        r1.route.Remove(b1);
                        r1.route.Add(b2);
                        r2.route.Remove(b2);
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
                    // bereken de increment
                    float inc1 = - rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b1, b2_pred) + rijtijd(b2_succ, b1);
                    float inc2 = - rijtijd(b2, b2_pred) - rijtijd(b2_succ, b2) + rijtijd(b2, b1_pred) + rijtijd(b1_succ, b2);

                    increment = inc1 + inc2;

                    float capDelta1 = b2.cont * b2.vpc - b1.cont * b1.vpc;
                    float capDelta2 = b1.cont * b1.vpc - b2.cont * b2.vpc;

                    if (d.tijdsduur + increment < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                    {
                        d.tijdsduur += increment;
                        r1.capaciteit += capDelta1;
                        r2.capaciteit += capDelta2;

                        b1.ReplaceChains(b2_pred, b2_succ);
                        b2.ReplaceChains(b1_pred, b1_succ);

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

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];

            if (grabbelton.Count > 0)
            { 
                int key = rnd.Next(0, grabbelton.Count);
                Bedrijf b = grabbelton[key]; // kan nooit voor het eerste element komen
                
                Bedrijf predecessor = r.route[rnd.Next(r.route.Count)]; // grab a random element from the route as the predecessor
                Bedrijf successor = predecessor.successor; // grab a random element from the route as the predecessor

                if (successor != null && b.freq == 1)
                {
                    float capDelta = b.vpc * b.cont;
                    float tijdDelta = b.ldm + rijtijd(b, predecessor) + rijtijd(successor, b) - rijtijd(successor, predecessor);
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
        public Bedrijf(string[] parts)
        {
            order = int.Parse(parts[0]);
            freq = int.Parse(parts[2][0].ToString());
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5].Replace('.', ','));
            matrixID = int.Parse(parts[6]);
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
            tijdsduur += 30;
        }
    }
}