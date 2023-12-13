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
            string save = "";
            bool input = false;
            while (save != "n" || save != "y")
            {
                save = Console.ReadLine();
            }

            if (save == "y")
            {
                // sla de oplossing op op de manier die ze willen met
                // 0; 0; 1; 0;
            }
        }

    }
    public class Oplossing
    {
        public Dictionary<(int, int), float> distDict;
        public Dag[][] trucksEnRoutes;
        public List<Bedrijf>? grabbelton; // nullable
        public string[] stort;
        public int tellertje;
        public float score;
        public int plateauCount;
        public float increment;
        public float T = 160;
        public int Q = 500;
        public float alpha;
        public Random rnd;
        public Oplossing(Dictionary<(int, int), float> dictInput, Random rndIn)
        {

            stort = ["", "-", "0", "0", "0", "0", "287"];
            trucksEnRoutes = [[new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)], [new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)]];
            distDict = dictInput;
            alpha = 0.95F;
            rnd = rndIn;
            plateauCount = 0;
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
            return rijtijdKosten(b.successor, b) + recurseRoute(b.successor);
        }

        public void ILS()
        {
            // simpeler begin
            int its = 100000000;
            while (tellertje < its)
            {
                float oldscore = score;
                int operation = rnd.Next(4);
                switch (operation)
                {
                    case 0:
                        swapWithinDay();
                        break;
                    case 1:
                        swapBetweenDays();
                        break;
                    case 2:
                        replaceStop();
                        break;
                    default:
                        addOperation();
                        break;
                }

                if (oldscore == score)
                {
                    plateauCount++;
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
            if (Math.Exp(-Math.Abs(increment) / T) < rnd.NextDouble())
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
                    float tijdDelta = rijtijdKosten(b.successor, b.predecessor) - rijtijdKosten(b, b.predecessor) - rijtijdKosten(b.successor, b) + b.ldm * 3 - b.ldm;
                    float capDelta = -b.cont * b.vpc;
                    increment = tijdDelta;

                    if (acceptIncrement())
                    {
                        d.tijdsduur += tijdDelta;
                        r.capaciteit += capDelta;

                        Bedrijf pred = b.predecessor; // grab a random element from the route as the predecessor
                        Bedrijf succ = b.successor; // grab a random element from the route as the predecessor

                        b.successor.predecessor = pred;
                        b.predecessor.successor = succ;

                        b.predecessor = null;
                        b.successor = null;

                        r.route.Remove(b);
                        grabbelton.Add(b);

                        score += increment;
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

                if (b1.successor != null && b1.predecessor != null)
                {
                    float inc = -b2.ldm * 2 + b1.ldm * 2 + rijtijdKosten(b2, b1.predecessor) + rijtijdKosten(b1.successor, b2) - rijtijdKosten(b1, b1.predecessor) - rijtijdKosten(b1.successor, b1);
                    increment = inc;
                    if (acceptIncrement())
                    {
                        grabbelton.Remove(b2);
                        r1.route.Remove(b1);
                         
                        b2.predecessor = b1.predecessor;
                        b2.successor = b1.successor;

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

                if (b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                {
                    // bereken de increment
                    float inc1 = -rijtijdKosten(b1, b1_pred) - rijtijdKosten(b1_succ, b1) + rijtijdKosten(b1, b2_pred) + rijtijdKosten(b2_succ, b1);
                    float inc2 = -rijtijdKosten(b2, b2_pred) - rijtijdKosten(b2_succ, b2) + rijtijdKosten(b2, b1_pred) + rijtijdKosten(b1_succ, b2);

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

                if (b1_succ != null && b2_succ != null && b1_pred != null && b2_pred != null)
                {
                    // bereken de increment
                    float inc1 = -rijtijdKosten(b1, b1_pred) - rijtijdKosten(b1_succ, b1) + rijtijdKosten(b1, b2_pred) + rijtijdKosten(b2_succ, b1);
                    float inc2 = -rijtijdKosten(b2, b2_pred) - rijtijdKosten(b2_succ, b2) + rijtijdKosten(b2, b1_pred) + rijtijdKosten(b1_succ, b2);

                    increment = inc1 + inc2;

                    float capDelta1 = b2.cont * b1.vpc - b1.cont * b1.vpc;
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

                if (successor != null)
                {
                    (bool, float, float) response = isValidRouteAdd(d, r, b, predecessor, successor);
                    if (response.Item1)
                    {
                        // als we toevoegen aan de enige lege lijst, is er geen lege lijst meer.
                        if (r.route.Count == 2) { d.eenlegeroute = false; }

                        // reken de increment uit
                        float inc = -(b.ldm * 3) + rijtijdKosten(b, predecessor) + rijtijdKosten(successor, b) - rijtijdKosten(successor, predecessor);
                        increment = inc;

                        if (acceptIncrement())
                        {
                            // pas de route en dag stats aan en switch de pred en succ
                            d.tijdsduur += response.Item2;
                            r.capaciteit += response.Item3;

                            // gebruik functie
                            predecessor.successor = b;
                            predecessor.successor.predecessor = b;
                            b.predecessor = predecessor;
                            b.successor = predecessor.successor;

                            score += inc;
                            r.route.Add(b);
                            grabbelton.Remove(b); // dit kan O(1), is vooral O(n)
                        }
                    }
                    else if (!isValidRouteAdd(d, r, b, predecessor, successor).Item1)
                    {
                        // voeg nieuwe lijst toe aan dag
                        if (!d.eenlegeroute)
                        {
                            d.routes.Add(new Route(stort));
                            d.tijdsduur += 30; // omdat er een stort is toegevoegd.
                            d.eenlegeroute = true;
                        }
                    }
                }
            }


        }
        float rijtijdKosten(Bedrijf b, Bedrijf pred)
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

        // check of de gegeven route valide is als dit bedrijf erbij komt
        (bool, float, float) isValidRouteAdd(Dag d, Route r, Bedrijf b, Bedrijf pred, Bedrijf suc)
        {
            float tijdDelta = rijtijdKosten(b, pred) + rijtijdKosten(suc, b) - rijtijdKosten(suc, pred);
            float capAdd = b.cont * b.vpc;

            if (d.tijdsduur + tijdDelta > 12 * 60) { return (false, 0, 0); }
            if (r.capaciteit + capAdd > 100000) { return (false, 0, 0); } // hier moet de dag geupdate 

            return (true, tijdDelta, capAdd);
        }
    }
    public class Route
    {
        public List<Bedrijf> route;
        public float tijdsduur; // dit heb je niet nodig
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
    }
    public class Bedrijf
    {
        public Bedrijf? successor; // kan null zijn
        public Bedrijf? predecessor; // nodig voor als je een bedrijf weghaalt uit je route
        //public int order; // nodig want matrixID is niet een lijst unieke nummers
        public int freq;
        public int cont; // kan misschien beter in een ding door vpc * cont te doen als totaal volume.
        public int vpc;
        public float ldm;
        public int matrixID;
        public Bedrijf(string[] parts)
        {
            //order = int.Parse(parts[0]);
            freq = int.Parse(parts[2][0].ToString());
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5]);
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
        public bool eenlegeroute; // hou een boolean bij van of er een lege route is
        public Dag(string[] stort)
        {
            routes = new List<Route>();
            routes.Add(new Route(stort));
            eenlegeroute = true;
            tijdsduur += 30;
        }
        public void updateTijd()
        {
            foreach (Route route in routes)
            {
                tijdsduur += route.tijdsduur;
            }
        }
    }
}