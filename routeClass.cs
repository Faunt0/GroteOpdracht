using System;

namespace GroteOpdracht
{
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
        }
        // dit is een recursieve functie bedoelt om de route uit te lezen en er een lijst van strings van te maken die te gebruiken valt in de checker
        public List<string> makeString(Bedrijf b, int place)
        {
            if (b.successor == null) { return [$"{place}; 0"]; }

            List<string> res = makeString(b.successor, place + 1);
            if (b.predecessor == null) { return res; }
            res.Add($"{place}; {b.order}");
            return res;
        }
    }
}