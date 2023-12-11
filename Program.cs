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
        }

    }
    public class Oplossing
    {
        public Dictionary<(int, int), float> distDict;
        public Dag[][] trucksEnRoutes;
        public List<Bedrijf>? grabbelton; // nullable
        public Bedrijf stort;
        public int tellertje;
        public float score;
        public float increment;
        public float T = 60;
        public int Q = 16;
        public float alpha;
        public Random rnd;
        public Oplossing(Dictionary<(int, int), float> dictInput, Random rndIn)
        {

            stort = new Bedrijf(["", "-", "0", "0", "0", "0", "287"]);
            trucksEnRoutes = [[new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)], [new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort)]];
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

                    Dag d = trucksEnRoutes[truck][i];
                    if (d.routes == null) continue;
                    foreach(Route r in d.routes)
                    {
                        if (r.route != null)
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
            return rijtijdKosten(b) + recurseRoute(b.successor);
        }

        public void ILS()
        {
            // eerst alleen de 1pwk, daarna de andere toevoegen

            // simpeler begin
            int its = 1000000;
            while (tellertje < its)
            {
                addOperation();
                tellertje++;
            }

            // negeer dit
            int maxIteraties = 1;
            while (false || tellertje < maxIteraties || Math.Exp(-Math.Abs(increment) / T) > 2.0)
            {
                // doe iteraties
                int dagkey = rnd.Next(0, 5);
                int truckkey = rnd.Next(0, 2);
                Dag d = trucksEnRoutes[truckkey][dagkey];
                Route r = d.routes[rnd.Next(d.routes.Count)];


                int operation = rnd.Next(3);
                switch (operation)
                {
                    case 0:
                        swapWithinDay(r);
                        break;
                    case 1:
                        //replaceStop(r);
                        break;
                    case 2:
                        swapBetweenDays(); // werkt niet
                        break;
                    default:
                        //addOperation(r);
                        break;
                }

                if (tellertje % Q == 0)
                {
                    T = T * alpha;
                    //Console.WriteLine(Math.Exp(-Math.Abs(increment) / T));
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
            if (Math.Exp(-Math.Abs(increment) / T) > 0.00005)
            {
                return true; // heb ik niet alleen dit nodig, zodat ik ook de kans van acceptatie van een verbetering implementeer
            }
            return false;
        }
        void removeStop()
        {
            // kies een route
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);
            Dag d = trucksEnRoutes[truckkey][dagkey];

            Route r = d.routes[rnd.Next(d.routes.Count)];
            // kies een bedrijf om te verwijderen
            Bedrijf ene = r.route[rnd.Next(r.route.Count)];


            r.route.Remove(ene);
            r.capaciteit -= ene.cont * ene.vpc;
            r.tijdsduur -= rijtijdKosten(ene);
        }
        //void replaceStop(Route r)
        //{
        //    int key = rnd.Next(0, grabbelton.Count);
        //    Bedrijf ene = grabbelton[key];

        //    int otherKey = rnd.Next(0, r.route.Count);
        //    Bedrijf andere = r.route[otherKey];

        //    r.route.Remove(andere);
        //    grabbelton.Remove(ene);


        //    if (isValidRoute(r, ene, andere.predecessor, andere.successor).Item1)
        //    {
        //        ene.predecessor = andere.predecessor;
        //        ene.successor = andere.successor;
        //        increment = -(ene.ldm * 3) + andere.ldm * 3 + rijtijdKosten(ene) - rijtijdKosten(andere);
        //        if (acceptIncrement())
        //        r.route.Add(ene);
        //        grabbelton.Add(andere);
        //    }
        //}
        void swapBetweenDays()
        {
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);
            Dag d = trucksEnRoutes[truckkey][dagkey];

            int dagkey2 = rnd.Next(0, 5);
            int truckkey2 = rnd.Next(0, 2);
            Dag d2 = trucksEnRoutes[truckkey2][dagkey2];

        }
        void swapWithinDay(Route r)
        {
            Bedrijf ene = r.route[rnd.Next(r.route.Count)];
            r.route.Remove(ene);
            Bedrijf andere = r.route[rnd.Next(r.route.Count)];
            r.route.Remove(andere);

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

            increment = kostenNaSwap - kostenVoorSwap;
            r.route.Add(ene);
            r.route.Add(andere);
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


            int key = rnd.Next(0, grabbelton.Count);
            Bedrijf b = grabbelton[key]; // kan nooit voor het eerste element komen
            
            // als er nog niet genoeg dingen in de lijst zitten
            if (r.route.Count < 2) 
            { 
                r.route.Add(b);
            }

            Bedrijf predecessor = r.route[rnd.Next(r.route.Count)]; // grab a random element from the route as the predecessor
            Bedrijf successor = predecessor.successor; // grab a random element from the route as the predecessor

            (bool, float, float) response = isValidRoute(d, r, b, predecessor, successor);
            if (successor != null && response.Item1)
            {
                d.tijdsduur += response.Item2;
                r.capaciteit += response.Item3;

                predecessor.successor = b;
                predecessor.successor.predecessor = b;
                b.predecessor = predecessor;
                b.successor = predecessor.successor;

                // als we toevoegen aan de enige lege lijst, is er geen lege lijst meer.
                if (r.route.Count == 2) { d.eenlegeroute = false; }

                // reken de increment uit
                float inc = -(b.ldm * 3) + rijtijdKosten(b);
                increment = inc; // weet niet of dit de bedoeling is.

                if (acceptIncrement())
                {
                    score += inc;
                    r.route.Add(b);
                    grabbelton.Remove(b); // dit kan O(1), is vooral O(n)
                }
            }
            else if (!isValidRoute(d, r, b, predecessor, successor).Item1)
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
        float rijtijdKosten(Bedrijf b)
        {
            // je moet alleen maar gebruik maken van de een of de andere, niet allebei
            float naar;
            if (b.predecessor != null)
            { 
                naar = distDict[(b.predecessor.matrixID, b.matrixID)];
            } else
            {
                naar = distDict[(287, b.matrixID)]; // gebruik de stort als predecessor als die er niet is
            }

            float kosten = naar;
            return kosten;
        }

        // check of de gegeven route valide is als dit bedrijf erbij komt
        (bool, float, float) isValidRoute(Dag d, Route r, Bedrijf b, Bedrijf pred, Bedrijf suc)
        {
            // capaciteit werkt niet
            float tijdAdd;
            tijdAdd = distDict[(pred.matrixID, suc.matrixID)] + distDict[(b.matrixID, suc.matrixID)] + b.ldm - distDict[(pred.matrixID, suc.matrixID)];
            float capAdd = b.cont * b.vpc;

            if (d.tijdsduur + tijdAdd > 12 * 60) { return (false, 0, 0); }
            if (r.capaciteit + capAdd > 100000) { return (false, 0, 0); } // hier moet de dag geupdate 

            return (true, tijdAdd, capAdd);
        }
    }
    public class Route
    {
        public List<Bedrijf> route;
        public float tijdsduur; // dit heb je niet nodig
        public float capaciteit;
        // willen we een laatste element bijhouden?
        public Route(Bedrijf stort)
        {
            route = new List<Bedrijf>();
            Bedrijf s1 = stort;
            Bedrijf s2 = stort;
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
        public string freq;
        public int cont; // kan misschien beter in een ding door vpc * cont te doen als totaal volume.
        public int vpc;
        public float ldm;
        public int matrixID;
        public Bedrijf(string[] parts)
        {
            //order = int.Parse(parts[0]);
            freq = parts[2];
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5]);
            matrixID = int.Parse(parts[6]);
        }
    }
    public class Dag
    {
        public float tijdsduur; // in minuten
        public List<Route> routes;
        public bool eenlegeroute; // hou een boolean bij van of er een lege route is
        public Dag(Bedrijf stort)
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