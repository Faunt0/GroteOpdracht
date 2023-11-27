// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;

namespace GroteOpdracht
{
    class Program
    {
        int tellertje = 0;
        public Dictionary<(int, int), (int, int)> afstandenDict;

        static void Main()
        {
            // bedrijvenlijst:
            Dictionary<int, Bedrijf> grabbelton = new Dictionary<int, Bedrijf>();
            using (var reader = new StreamReader("../../../orderbestand.txt"))
            {
                string firstline = reader.ReadLine();
                Console.WriteLine($"{firstline}");

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(';');
                    Bedrijf bv = new Bedrijf(parts);
                    grabbelton.Add(bv.matrixID, bv);
                }
            }
            Console.WriteLine(grabbelton[0]);


            Dictionary<(int, int), (int, int)> dict = new Dictionary<(int, int), (int, int)>();
            using (var reader = new StreamReader("../../../afstandenmatrix.txt"))
            {
                string firstline = reader.ReadLine();
                string line;
                while (( line = reader.ReadLine()) != null )
                {
                    string[] parts = line.Split(";");
                    int id1 = int.Parse(parts[0]);
                    int id2 = int.Parse(parts[1]);
                    int afstand = int.Parse(parts[2]); // eigenlijk niet nodig ivm de rijtijd
                    int tijd = int.Parse(parts[3]);
                    dict[(id1, id2)] = (afstand, tijd);
                }
            }
            Console.WriteLine(dict[(0,1)].Item1);
        }

        static Oplossing ILS()
        {
            Oplossing oplossing = new Oplossing();
            return oplossing;
        }
        static void swapOperation()
        {

        }
        void addBv(Route r, Bedrijf predecessor, Bedrijf b)
        {
            int nextIndex = afstandenDict.Count + 1;
            b.predecessor = predecessor;
            b.successor = predecessor.successor;

            predecessor.successor.predecessor = b;
            predecessor.successor = b;

            b.rtNaar = afstandenDict[(b.predecessor.matrixID, b.matrixID)].Item2; // kan dit niet beter bij het uitrekenen van de increment?
            b.rtVan = afstandenDict[(b.matrixID, b.successor.matrixID)].Item2;
            
            r.route.Add(nextIndex, b);
        }
        public static void removeBv(Route r, int key, Bedrijf b)
        {
            r.route.Remove(key); // O(1)
            b.predecessor.successor = b.successor;
            b.successor.predecessor = b.predecessor;
        }
        float beginkosten(Route r, Dictionary<int, Bedrijf> grabbelton)
        {
            float routekosten = 0;
            float grabbeltonKosten = 0;
            for (int i = 0; i < grabbelton.Count; i++)
            {
                grabbeltonKosten += grabbelton[i].ldm * 3;
            }
            for (int i = 0; i < r.route.Count; i++)
            {
                if (r.route[i].predecessor != null)
                {
                    routekosten += afstandenDict[(r.route[i].predecessor.matrixID, r.route[i].matrixID)].Item2;
                }
            }
            return routekosten + grabbeltonKosten;
        }

        // dit is de increment voor de add functie
        static float increment(Bedrijf b)
        {
            // hangt af van de operation
            // als je een bedrijf toevoegd aan de route:
            float inc = -(b.ldm * 3) + b.rtVan + b.rtNaar;
            return inc;
        }
        public bool isValidRoute(Route r, Bedrijf b)
        {
            if (b.matrixID == 287)
            {
                // leeg een truck binnen 30 minuten
                r.tijdsduur += 30 + afstandenDict[(b.predecessor.matrixID, b.matrixID)].Item2;
                r.capacitiet = 0;
            } else
            {
                r.tijdsduur += afstandenDict[(b.predecessor.matrixID, b.matrixID)].Item2 + b.ldm;
                r.capacitiet += b.cont * b.vpc;
            }
            if (r.tijdsduur > 11.50 * 2 * 60) { return false; }
            if (r.capacitiet > 200000) { return false; }
            return true;
        }
    }
    public class Oplossing
    {
        public Route[] routelijst;
        public Oplossing()
        {
            routelijst = new Route[5]; // maak een lijst van routes die corresponderen met de dagen
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
        public Bedrijf successor;
        public Bedrijf predecessor; // nodig voor als je een bedrijf weghaalt uit je route
        public string freq;
        public int cont;
        public int vpc;
        public float ldm;
        public int matrixID;
        public int rtVan; // rijtijd vanaf dit bedrijf naar zijn successor
        public int rtNaar; // rijtijd naar dit bedrijf vanaf zijn predecessor
        public Bedrijf(string[] parts)
        {
            freq = parts[2];
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5]);
            matrixID = int.Parse(parts[6]);
        }
    }
}