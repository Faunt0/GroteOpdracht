// See https://aka.ms/new-console-template for more information
using System;
using System.IO;

namespace GroteOpdracht
{
    class Program
    {
        int tellertje = 0;
        static void Main()
        {
            // bedrijvenlijst:
            Bedrijf[] bedrijvenlijst = new Bedrijf[10];
            LinkedList<Bedrijf> list = new LinkedList<Bedrijf>();
            using (var reader = new StreamReader("../../../orderbestand.txt"))
            {
                string firstline = reader.ReadLine();
                Console.WriteLine($"{firstline}");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(';');
                    list.AddFirst(new Bedrijf(parts));
                }
            }
            Console.WriteLine(list.First.Value.order);

            // get the distance matrix
            int len = 0;
            foreach (var bedrijf in list)
            {
                if (bedrijf.matrixID > len) { len = bedrijf.matrixID; }
            }
            Console.WriteLine($"{len}");
            MatrixVak[,] matrix = new MatrixVak[len+1,len+1];
            using (var reader = new StreamReader("../../../afstandenmatrix.txt"))
            {
                string firstline = reader.ReadLine() ;
                string line;
                while (( line = reader.ReadLine()) != null )
                {
                    string[] parts = line.Split(";");
                    int id1 = int.Parse(parts[0]);
                    int id2 = int.Parse(parts[1]);
                    matrix[id1, id2] = new MatrixVak(parts);
                }
            }
            Console.WriteLine(matrix[0,1].afstand);
        }

        static Oplossing ILS()
        {
            Oplossing oplossing = new Oplossing();
            return oplossing;
        }
        static void Operation()
        {
            // check de buurruimte dmv add and remove
            // je kan werken met een enkele truck die twee keer zo lang mag rijden,
            // 200.000L capaciteit <- is dat nog hetzelfde dan?

        }
        static float kosten(Route r)
        {
            return 0;
        }
        static bool isValidRoute(Route r, MatrixVak[,] matrix)
        {
            // is de rijtijd + leegtijd binnen een dag
            // eindigt de route bij id 287

            float tijdspanne = 0; // minuten
            float truckvolume = 0;
            LinkedListNode<Bedrijf> node = r.route.First;
            while (node.Next != null)
            {
                if (node.Value.matrixID == 287)
                {
                    // leeg een truck binnen 30 minuten
                    tijdspanne += 30;
                    truckvolume = 0;
                } else
                {
                    tijdspanne += matrix[node.Value.matrixID, node.Next.Value.matrixID].rijtijd + node.Value.ldm;
                    truckvolume += node.Value.cont * node.Value.vpc;
                }
            }
            if (tijdspanne > 11.50 * 2 * 60) { return false; }
            if (r.route.Last.Value.matrixID != 287) { return false; }
            if (truckvolume > 200000) { return false; }
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
        public LinkedList<Bedrijf> route;
        public Route() { }
    }
    public class Bedrijf
    {
        public int order; // niet nodig toch?
        public string plaats; // niet nodig
        public string freq;
        public int cont;
        public int vpc;
        public float ldm;
        public int matrixID;
        public int xcoord; // niet nodig
        public int ycoord; // niet nodig
        public Bedrijf(string[] parts)
        {
            order = int.Parse(parts[0]);
            plaats = parts[1];
            freq = parts[2];
            cont = int.Parse(parts[3]);
            vpc = int.Parse(parts[4]);
            ldm = float.Parse(parts[5]);
            matrixID = int.Parse(parts[6]);
            xcoord = int.Parse(parts[7]);
            ycoord = int.Parse(parts[8]);
        }
    }
    public class MatrixVak
    {
        public int afstand;
        public int rijtijd;
        public MatrixVak(string[] parts)
        {
            afstand = int.Parse(parts[2]);
            rijtijd = int.Parse(parts[3]);
        }
    }
}