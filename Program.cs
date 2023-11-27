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
            Console.WriteLine(grabbelton[0]);

            
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

            Oplossing oplossing = new Oplossing(fileDict);
            oplossing.grabbelton = grabbelton;
            oplossing.beginScore(grabbelton, fileDict);
            Console.WriteLine(oplossing.score);

            DateTime before = DateTime.Now;
            oplossing.ILS();
            DateTime after = DateTime.Now;
            TimeSpan ts = before - after;
            Console.WriteLine(ts.Milliseconds);
            Console.WriteLine(oplossing.routelijst.ToString());
        }

    }
    public class Oplossing
    {
        public Dictionary<(int, int), float> distDict;
        public Route[] routelijst;
        public List<Bedrijf> grabbelton;
        public int tellertje;
        public float score;
        public float delta;
        public float T = 0;
        public Oplossing(Dictionary<(int, int), float> dictInput)
        {
            routelijst = [new Route(), new Route(), new Route(), new Route(), new Route()]; // maak een lijst van routes die corresponderen met de dagen
            distDict = dictInput;
        }

        public void beginScore(List<Bedrijf> grabbelton, Dictionary<(int, int), float> distDict)
        {
            float routekosten = 0;
            float grabbeltonKosten = 0;
            foreach (Bedrijf b in grabbelton)
            {
                grabbeltonKosten += b.ldm * 3;
            }
            for (int i = 0; i < 5; i++)
            {
                // calculate the score of each route in the beginning

                Route r = routelijst[i];
                for (int j = 0; j < r.route.Count; j++)
                {
                    if (r.route[j].predecessor != null)
                    {
                        routekosten += distDict[(r.route[j].predecessor.matrixID, r.route[j].matrixID)];
                    }
                }
            }
            score = routekosten + grabbeltonKosten;
        }

        public void ILS()
        {
            // eerst alleen de 1pwk, daarna de andere toevoegen
            // simulated annealing
            int maxIteraties = 100;
            while (tellertje < maxIteraties || T > 0.02)
            {
                // doe iteraties

                Random rnd = new Random();
                int key = rnd.Next(0, 5);
                Route r = routelijst[key];
                float oldscore = score;

                Route newRoute = addOperation(r, grabbelton);

                if (oldscore <= score)
                {
                    routelijst[key] = newRoute;
                }
                else if (rnd.Next(0, 100) < Math.Exp(-delta / T))
                {
                    routelijst[key] = newRoute;
                }

                tellertje++;
            }
        }
        static void swapOperation()
        {

        }
        Route addOperation(Route r, List<Bedrijf> grabbelton)
        {
            Random rnd = new Random();
            int key = rnd.Next(0, grabbelton.Count);
            Bedrijf b = grabbelton[key]; // kan nooit voor het eerste element komen
            if (r.route.Count < 2)
            {
                r.route.Add(b);
                return r;
            }
            Bedrijf predecessor = r.route[rnd.Next(0, r.route.Count)];
            b.predecessor = predecessor;
            b.successor = predecessor.successor;
            predecessor.successor.predecessor = b;
            predecessor.successor = b;

            grabbelton.ElementAt(key);
            if (!isValidRoute(r, b))
            {
                return r;
            }

            r.route.Add(b);
            grabbelton.Remove(b);

            score += increment(b); // pas de score aan obv de aanpassing

            return r;
        }
        // dit is de increment voor de add functie
        float increment(Bedrijf b)
        {
            // hangt af van de operation
            // als je een bedrijf toevoegd aan de route:

            float rtNaar = distDict[(b.predecessor.matrixID, b.matrixID)];
            float rtVan = distDict[(b.matrixID, b.successor.matrixID)];
            float inc = -(b.ldm * 3) + rtVan + rtNaar;
            return inc;
        }
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
            if (r.tijdsduur > 11.50 * 2 * 60) { return false; }
            if (r.capacitiet > 200000) { return false; }
            return true;
        }
    }
    public class Route
    {
        public List<Bedrijf> route; // key is een index
        public float tijdsduur;
        public float capacitiet;
        // willen we een laatste elementen bijhouden.
        public Route() 
        {
            route = new List<Bedrijf>(); // is dit nodig
        }

        //static void removeBv(Route r, Bedrijf b)
        //{
        //    r.route.Remove(b); // O(1)
        //    b.predecessor.successor = b.successor;
        //    b.successor.predecessor = b.predecessor;
        //}
    }
    public class Bedrijf
    {
        public Bedrijf? successor; // kan null zijn
        public Bedrijf? predecessor; // nodig voor als je een bedrijf weghaalt uit je route
        public int order; // nodig want matrixID is niet een lijst unieke nummers
        public string freq;
        public int cont;
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