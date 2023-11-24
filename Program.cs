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
            Console.WriteLine(list.First.Value.plaats);

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

        static Route ILS()
        {
            Route route = new Route();
            return route;
        }
        static void Operation()
        {

        }
    }

    public class Route
    {
        public LinkedList<Bedrijf> routes;
        public Route() { }

    }
    public class Bedrijf
    {
        public int order;
        public string plaats;
        public string freq;
        public int cont;
        public int vpc;
        public float ldm;
        public int matrixID;
        public int xcoord; 
        public int ycoord;
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