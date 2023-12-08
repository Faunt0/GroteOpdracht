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
        public Route[][] trucksEnRoutes;
        public List<Bedrijf> grabbelton;
        public int tellertje;
        public float score;
        public float increment;
        public float T = 60;
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

            int its = 1000000;
            while (tellertje < its)
            {
                int routekey = rnd.Next(0, 5);
                int truckkey = rnd.Next(0, 2);
                //routekey = 0;
                //truckkey = 0;

                Route r = trucksEnRoutes[truckkey][routekey];

                addOperation(r);
                tellertje++;
            }


            int maxIteraties = 1;
            while (false || tellertje < maxIteraties || Math.Exp(-Math.Abs(increment) / T) > 2.0)
            {
                // doe iteraties
                int routekey = rnd.Next(0, 5);
                int truckkey = rnd.Next(0, 2);
                Route r = trucksEnRoutes[truckkey][routekey];


                int operation = rnd.Next(3);
                switch (operation)
                {
                    case 0:
                        swapWithinDay(r);
                        break;
                    case 1:
                        replaceStop(r);
                        break;
                    case 2:
                        swapBetweenDays(); // werkt niet
                        break;
                    default:
                        addOperation(r);
                        break;
                }

                if (tellertje % Q == 0)
                {
                    T = T * alpha;
                    //Console.WriteLine(Math.Exp(-Math.Abs(increment) / T));
                }

            }
        }
        // kijkt of de huidige increment geaccepteerd word in de operation
        bool acceptIncrement()
        {
            if (increment > 0)
            {
                return true;
            }
            if (Math.Exp(-Math.Abs(increment) / T) > 0.00005)
            {
                return true; // heb ik niet alleen dit nodig, zodat ik ook de kans van acceptatie van een verbetering implementeer
            }
            return false;
        }
        void removeStop(Route r)
        {
            Bedrijf ene = r.route[rnd.Next(r.route.Count)];
            r.route.Remove(ene);
            r.capacitiet -= ene.cont * ene.vpc;
            r.tijdsduur -= rijtijdKosten(ene);
        }
        void replaceStop(Route r)
        {
            int key = rnd.Next(0, grabbelton.Count);
            Bedrijf ene = grabbelton[key];

            int otherKey = rnd.Next(0, r.route.Count);
            Bedrijf andere = r.route[otherKey];

            r.route.Remove(andere);
            grabbelton.Remove(ene);


            if (isValidRoute(r, ene, andere.predecessor, andere.successor))
            {
                ene.predecessor = andere.predecessor;
                ene.successor = andere.successor;
                increment = -(ene.ldm * 3) + andere.ldm * 3 + rijtijdKosten(ene) - rijtijdKosten(andere);
                if (acceptIncrement())
                r.route.Add(ene);
                grabbelton.Add(andere);
            }
        }
        void swapBetweenDays()
        {
            int routekey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);
            Route r = trucksEnRoutes[truckkey][routekey];

            int routekey2 = rnd.Next(0, 5);
            int truckkey2 = rnd.Next(0, 2);
            Route r2 = trucksEnRoutes[truckkey2][routekey2];

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
        void addOperation(Route r)
        {
            int key = rnd.Next(0, grabbelton.Count);
            Bedrijf b = grabbelton[key]; // kan nooit voor het eerste element komen
            if (r.route.Count < 2) 
            { 
                // als je de stort kies
                if (b.matrixID == 287)
                {
                    // als er geen predecessor is is rijtijd 0 anders rij naar b.
                    float rijtijd = b.predecessor == null ? 0 : distDict[(b.predecessor.matrixID, b.matrixID)];
                    r.tijdsduur += 30 + rijtijd; // voeg leegtijd toe
                    r.capacitiet = 0; // leeg de truck
                    grabbelton.Add(b); // laat de mogelijkheid toe om hem nog eens te kiezen.
                }
                r.route.Add(b); 
            }

            Bedrijf predecessor = r.route[rnd.Next(r.route.Count)]; // grab a random element from the route as the predecessor
            Bedrijf successor = r.route[rnd.Next(r.route.Count)]; // grab a random element from the route as the predecessor


            if (isValidRoute(r, b, predecessor, successor) && predecessor.matrixID != successor.matrixID)
            {
                predecessor.successor = b;
                predecessor.successor.predecessor = b;
                b.predecessor = predecessor;
                b.successor = predecessor.successor;

                if (b.matrixID == 287)
                {
                    // als er geen predecessor is is rijtijd 0 anders rij naar b.
                    float rijtijd = b.predecessor == null ? 0 : distDict[(b.predecessor.matrixID, b.matrixID)];
                    r.tijdsduur += 30 + rijtijd;
                    r.capacitiet = 0;
                    grabbelton.Add(b);
                    Console.WriteLine("STORT BEZOCHT");
                }


                // reken de increment uit
                float inc = -(b.ldm * 3) + rijtijdKosten(b);
                increment = inc; // weet niet of dit de bedoeling is.

                if (acceptIncrement())
                {
                    score += inc;
                    r.route.Add(b);
                    grabbelton.Remove(b); // dit kan O(1), is soms O(n)
                }
            }
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
            }

            float van;
            if (b.successor != null) // kan in 1 regel
            {
                van = distDict[(b.matrixID, b.successor.matrixID)];
            }
            else
            {
                van = distDict[(b.matrixID, 287)]; // gebruik de stort als successor als die er niet is
                // houdt dit in dat je dan ook weer tijden moet aanpassen?
            }

            float kosten = van + naar;
            return kosten;
        }

        // check of de gegeven route valide is als dit bedrijf erbij komt
        bool isValidRoute(Route r, Bedrijf b, Bedrijf pred, Bedrijf suc)
        {
            float tijdAdd;
            if (b.predecessor == null)
            {
                tijdAdd = distDict[(b.matrixID, suc.matrixID)] + b.ldm;
            }
            else
            {
                tijdAdd = distDict[(pred.matrixID, b.matrixID)] + b.ldm;
            }
            float capAdd = b.cont * b.vpc;
            if (r.tijdsduur + tijdAdd > 12 * 60) { return false; }
            if (r.capacitiet + capAdd > 100000) { return false; }
            r.tijdsduur += tijdAdd;
            r.capacitiet += capAdd;
            return true;
        }
    }
    public class Route
    {
        public List<Bedrijf> route;
        public float tijdsduur;
        public float capacitiet;
        // willen we een laatste element bijhouden.
        public Route()
        {
            route = new List<Bedrijf>();
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
}