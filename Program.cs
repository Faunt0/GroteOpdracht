// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;

namespace GroteOpdracht
{
    class Program
    {

        static void Main()
        {
            // bedrijvenlijst:
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
                    if (bv.freq == "1PWK") // eerst alleen de 1pwk
                    {
                        grabbelton.Add(bv);
                        index++;
                    }
                }
            }

            
            //Dictionary<(int, int), (int, float)> fileDict = new Dictionary<(int, int), (int, float)>();
            Dictionary<(int, int), float> fileDict = new Dictionary<(int, int), float>();
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
        }

    }
    public class Oplossing
    {
        public Dictionary<(int, int), float> distDict;
        public Route[][] trucksEnRoutes;
        public List<Bedrijf> grabbelton;
        public int tellertje;
        public float score;
        public float delta;
        public float T = 500;
        public int Q = 16;
        public float alpha;
        public Random rnd;
        public Oplossing(Dictionary<(int, int), float> dictInput, Random rndIn)
        {
            // maak een lijst van routes voor elke dag voor elke truck, ds 10 routes in totaal
            //trucksEnRoutes = new Route[2, 5];
            trucksEnRoutes = [[new Route(), new Route(), new Route(), new Route(), new Route()], [new Route(), new Route(), new Route(), new Route(), new Route()]];
            distDict = dictInput;
            alpha = 0.95F;
            rnd = rndIn;
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

                    Route r = trucksEnRoutes[truck][i];
                    foreach (Bedrijf b in r.route.Values)
                    {
                        if (b.predecessor == null)
                        {
                            routekosten += recurseRoute(b); 
                            break;
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
            return rijtijdKosten(b) + recurseRoute(b.successor);
        }

        public void ILS()
        {
            // eerst alleen de 1pwk, daarna de andere toevoegen
            // simulated annealing
            int maxIteraties = 10000000;
            while (tellertje < maxIteraties || Math.Exp(-delta / T) > 2)
            {
                // doe iteraties
                int routekey = rnd.Next(0, 5);
                int truckkey = rnd.Next(0, 2);

                Route r = trucksEnRoutes[truckkey][routekey];
                float oldscore = score;
                Route newRoute;
                if (rnd.Next(2) == 1 && r.route.Count > 2)
                {
                    newRoute = swapWithinDay(r);
                } else
                {
                    newRoute = addOperation(r);
                }
                //switch (switch_on)
                //{
                //    default:
                //}

                if (oldscore <= score)
                {
                    trucksEnRoutes[truckkey][routekey] = newRoute;
                    tellertje++;
                }
                else if (rnd.Next(0, 100) < Math.Exp(-delta / T) || false) // ignore this for now
                {
                    trucksEnRoutes[truckkey][routekey] = newRoute;
                    tellertje++;
                }

                if (tellertje % Q == 0)
                {
                    T = T * alpha;
                }

            }
        }
        (Route, Route) swapBetweenDays()
        {
            int routekey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Route r = trucksEnRoutes[truckkey][routekey];
            int routekey2 = rnd.Next(0, 5);
            int truckkey2 = rnd.Next(0, 2);

            Route r2 = trucksEnRoutes[truckkey2][routekey2];

            // doe dingen.

            return (r, r2);
        }
        Route swapWithinDay(Route r)
        {
            Bedrijf ene = r.route.ToArray()[rnd.Next(r.route.Count)].Value;
            int enekey = r.route.ToArray()[rnd.Next(r.route.Count)].Key;
            r.route.Remove(enekey);
            Bedrijf andere = r.route.ToArray()[rnd.Next(r.route.Count)].Value;
            int anderekey = r.route.ToArray()[rnd.Next(r.route.Count)].Key;
            r.route.Remove(anderekey);

            float kostenVoorSwap = rijtijdKosten(ene) + rijtijdKosten(andere);

            Bedrijf eneSuc = ene.successor;
            Bedrijf enePred = ene.predecessor;
            
            // verwissel de predecessors en successors
            ene.predecessor = andere.predecessor;
            ene.successor = andere.successor;
            andere.predecessor = enePred;
            andere.successor = eneSuc;
            

            // bereken increment
            float kostenNaSwap = rijtijdKosten(ene) + rijtijdKosten(andere);

            delta = Math.Abs(kostenNaSwap - kostenVoorSwap);

            r.route.Add(enekey, ene);
            r.route.Add(anderekey, andere);
            return r;
        }
        float rijtijdKosten(Bedrijf b)
        {
            float naar;
            if (b.predecessor != null) // kan in 1 regel
            { 
                naar = distDict[(b.predecessor.matrixID, b.matrixID)];
            } else
            {
                naar = distDict[(287, b.matrixID)]; // gebruik de stort als predecessor als die er niet is
                naar = 0;
            }

            float van;
            if (b.successor != null) // kan in 1 regel
            {
                van = distDict[(b.matrixID, b.successor.matrixID)];
            }
            else
            {
                van = distDict[(b.matrixID, 287)]; // gebruik de stort als successor als die er niet is
                van = 0;
            }

            float kosten = van + naar;
            return kosten;
        }
        Route addOperation(Route r)
        {
            int key = rnd.Next(0, grabbelton.Count);
            Bedrijf b = grabbelton[key]; // kan nooit voor het eerste element komen
            if (r.route.Count < 2)
            {
                r.route.Add(r.route.Count, b);
                return r;
            }
            Bedrijf predecessor = r.route.ToArray()[rnd.Next(r.route.Count)].Value; // grab a random element from the route as the predecessor
            predecessor.successor = b;
            predecessor.successor.predecessor = b;
            b.predecessor = predecessor;
            b.successor = predecessor.successor;

            if (!isValidRoute(r, b))
            {
                return r;
            }

            r.route.Add(r.route.Count, b);
            grabbelton.Remove(b);

            // reken de increment uit
            float inc = -(b.ldm * 3) + rijtijdKosten(b);
            delta = Math.Abs(inc); // weet niet of dit de bedoeling is.

            score += delta; // pas de score aan obv de aanpassing

            return r;
        }

        // check of de gegeven route valide is als dit bedrijf erbij komt
        bool isValidRoute(Route r, Bedrijf b)
        {
            if (b.matrixID == 287 && r.capacitiet != 0)
            {
                // leeg een truck binnen 30 minuten
                r.tijdsduur += 30 + distDict[(b.predecessor.matrixID, b.matrixID)];
                r.capacitiet = 0;
            }
            else
            {
                if (b.predecessor == null)
                {
                    r.tijdsduur += distDict[(b.matrixID, b.successor.matrixID)] + b.ldm;
                }
                else
                {
                    r.tijdsduur += distDict[(b.predecessor.matrixID, b.matrixID)] + b.ldm;
                }
                r.capacitiet += b.cont * b.vpc;
            }
            if (r.tijdsduur > 11.50 * 60) { return false; }
            if (r.capacitiet > 100000) { return false; }
            return true;
        }
    }
    public class Route
    {
        public Dictionary<int, Bedrijf> route; // key is een index
        public float tijdsduur;
        public float capacitiet;
        // willen we een laatste elementen bijhouden.
        public Route() 
        {
            route = new Dictionary<int, Bedrijf>(); // is dit nodig
        }
    }
    public class Bedrijf
    {
        public Bedrijf? successor; // kan null zijn
        public Bedrijf? predecessor; // nodig voor als je een bedrijf weghaalt uit je route
        public int order; // nodig want matrixID is niet een lijst unieke nummers
        public string freq;
        public int cont; // kan misschien beter in een ding door vpc * cont te doen als totaal volume.
        public int vpc;
        public float ldm;
        public int matrixID;
        public Bedrijf(string[] parts)
        {
            order = int.Parse(parts[0]);
            freq = parts[2];
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5]);
            matrixID = int.Parse(parts[6]);
        }
    }
}