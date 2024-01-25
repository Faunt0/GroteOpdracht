﻿using System;
using System.Reflection.PortableExecutable;

namespace GroteOpdracht
{
    public class Oplossing
    {
        public Dag[][] trucksEnRoutes;
        public (double, List<string>) beste;
        public List<Bedrijf>? grabbelton;
        public List<Bedrijf>? grabbeltonFreq234;
        public string[] stort;
        public long iterations = 1000000000; // 4 miljard duurt ~16 minuten
        public double score;
        public double increment;
        public double T = 0.05;
        public long Q = 100000;
        public double alpha = 0.99F;
        public long plt_bound = 10000;
        public double old_T;
        public long plateauCount;
        public long tellertje;
        public double[,] distDict;
        public Random rnd;
        public Oplossing(double[,] dictInput, Random rndIn)
        {

            stort = ["0", "-", "0", "0", "0", "0", "287"]; // definieer een "stort" bedrijf

            // initialiseer onze oplossing met de juiste hoeveelheid routes per dag
            trucksEnRoutes = [[new Dag(stort, 2), new Dag(stort), new Dag(stort), new Dag(stort, 2), new Dag(stort, 2)], [new Dag(stort, 2), new Dag(stort), new Dag(stort), new Dag(stort), new Dag(stort, 2)]];
            distDict = dictInput;
            old_T = T;
            rnd = rndIn;
            plateauCount = 0;
            List<string> first = makeString(trucksEnRoutes);
            beste = (1000000, first);
        }

        // Voer local search uit
        public void LocalSearch()
        {
            while (tellertje < iterations && T > 0.00005D)
            {
                double oldscore = score;
                int op = rnd.Next(100);

                // voer een willekeurige operation uit
                if (op < 10) { removeStop(); }
                else if (op < 15) { addMultiple(); }
                else if (op < 30) { addStop(); }
                else if (op < 60) { shiftBetween(); }
                else if (op < 99) { shiftWithin(); }
                else if (op < 100) { removeMultiple(); }


                if (oldscore == score) { plateauCount++; } else { plateauCount = 0; }
                // als er een betere score gevonden is, maak deze oplossing immutable door het naar een string te schrijven
                if (score < beste.Item1) { beste = (score, makeString(trucksEnRoutes)); }


                tellertje++;

                if (tellertje % Q == 0) { T *= alpha; }

                // mini loading bar
                if (tellertje % (iterations / 20) == 0) { Console.Write("#"); }

                // verhoog T door te verdubbelen en reset de waarde van plateauCount om zo uit een plateau te komen.
                if (plateauCount > plt_bound)
                { 
                    T *= 2;
                    plateauCount = 0;
                }
            }
        }
        

        // kijkt of de huidige increment geaccepteerd word in de operation, op basis van simulated annealing
        bool acceptIncrement()
        {
            if (increment <= 0)
            {
                return true;
            }
            if (Math.Exp((-increment) / T) > rnd.NextDouble())
            {
                return true;
            }
            return false;
        }

        // Operations
        void removeStop()
        {
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];


            if (r.route.Count > 2)
            {
                Bedrijf b = r.route[rnd.Next(0, r.route.Count)];

                if (b.freq == 1 && b.successor != null && b.predecessor != null) // haal niet een van de stort weg
                {
                    double tijdDelta = 0;

                    if (r.route.Count == 3)
                    {
                        // verwijder ook de route uit de dag als er maar 1 stop is
                        tijdDelta += -30;
                    }

                    tijdDelta += rijtijd(b.successor, b.predecessor) - rijtijd(b, b.predecessor) - rijtijd(b.successor, b) - b.ldm;
                    double capDelta = -b.cont * b.vpc;
                    increment = tijdDelta + 3 * b.ldm;

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
        void addStop()
        {
            // Kies de willekeurige dag en truck en route van die dag
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];

            if (grabbelton.Count > 0)
            {
                // pak een bedrijf uit de grabbelton
                int key = rnd.Next(0, grabbelton.Count);
                Bedrijf b = grabbelton[key];

                // selecteer een voorganger
                Bedrijf predecessor = r.route[rnd.Next(r.route.Count)];
                Bedrijf successor = predecessor.successor;

                // we mogen niet de laatste stort als opvolger hebben
                if (successor != null && b.freq == 1)
                {
                    double capDelta = b.vpc * b.cont;
                    double tijdDelta = b.ldm + rijtijd(b, predecessor) + rijtijd(successor, b) - rijtijd(successor, predecessor);

                    // stel we voegen een bedrijf toe aan een lege route moeten we nog legen bij de stort
                    if (r.route.Count == 2) { tijdDelta += 30; }
                    increment = -(b.ldm * 3) + tijdDelta;

                    // als de increment geaccepteerd worde en er wordt voldaan aan de eisen
                    if (acceptIncrement() && d.tijdsduur + tijdDelta < 12 * 60 && r.capaciteit + capDelta <= 100000)
                    {
                        // update de dag en route
                        d.tijdsduur += tijdDelta;
                        r.capaciteit += capDelta;

                        // vervang de links van de linkedlist
                        b.ReplaceChains(predecessor, successor);
                        predecessor.ReplaceChains(predecessor.predecessor, b);
                        successor.ReplaceChains(b, successor.successor);

                        // update score en voeg toe aan route en verwijder uit grabbelton
                        score += increment;
                        r.route.Add(b);
                        grabbelton.Remove(b); // dit kan O(1), is vooral O(n)
                    }

                }
            }
        }
        void addMultiple()
        {
            if (grabbeltonFreq234.Count > 0)
            {
                int truckkey = rnd.Next(0, 2); // needs to be different
                int key = rnd.Next(grabbeltonFreq234.Count);
                Bedrijf b = grabbeltonFreq234[key];
                if (b.freq > 1)
                {
                    int[][] freq2dagen = [[0, 3], [1, 4]]; // alleen voor freq == 2
                    int[] freq3dagen = [0, 2, 4];
                    int[][] freq4dagen = [[0, 1, 2, 3], [1, 2, 3, 4], [0, 2, 3, 4], [0, 1, 3, 4], [0, 1, 2, 4]];


                    increment = 0;

                    int[] dagIndexen = new int[5];
                    if (b.freq == 2) { dagIndexen = freq2dagen[rnd.Next(0, b.freq)]; }
                    else if (b.freq == 3) { dagIndexen = freq3dagen; }
                    else if (b.freq == 4) { dagIndexen = freq4dagen[rnd.Next(5)]; }

                    int[] routeIndexen = new int[5];
                    double[] tijdDeltas = new double[5];
                    double[] capDeltas = new double[5];
                    (Bedrijf, Bedrijf)[] predsucc = new (Bedrijf, Bedrijf)[5];
                    bool constraints = true;
                    // voor willekeurige trucks en routes voeg toe aan die dag
                    foreach (int i in dagIndexen)
                    {
                        Dag d = trucksEnRoutes[truckkey][i];
                        int routeKey = rnd.Next(d.routes.Count);
                        Route r = d.routes[routeKey];
                        Bedrijf succ = r.route[rnd.Next(1, r.route.Count)]; // laatste stort is mogelijke successor
                        Bedrijf ns_pred = succ.predecessor;

                        double tijdDelta;
                        tijdDelta = -rijtijd(succ, ns_pred) + rijtijd(b, ns_pred) + rijtijd(succ, b) + b.ldm;
                        if (r.route.Count == 2) { tijdDelta += 30; }
                        if (d.tijdsduur + tijdDelta < 12 * 60) { tijdDeltas[i] = tijdDelta; } else { constraints = false; break; }

                        double capDelta;
                        capDelta = b.vpc * b.cont;
                        if (r.capaciteit + capDelta < 100000) { capDeltas[i] = capDelta; } else { constraints = false; break; }

                        predsucc[i] = (ns_pred, succ);
                        routeIndexen[i] = routeKey;
                    }

                    if (constraints)
                    {
                        increment = tijdDeltas.Sum() - (b.ldm * 3 * b.freq);

                        if (acceptIncrement())
                        {
                            foreach (int dagkey in dagIndexen)
                            {
                                Bedrijf b_clone = new Bedrijf(b.inputArray);
                                b_clone.dagkey = dagkey;

                                Dag d = trucksEnRoutes[truckkey][dagkey];
                                Route r = d.routes[routeIndexen[dagkey]];

                                Bedrijf succ = predsucc[dagkey].Item2;
                                Bedrijf ns_pred = predsucc[dagkey].Item1;

                                d.tijdsduur += tijdDeltas[dagkey];
                                r.capaciteit += capDeltas[dagkey];

                                b_clone.ReplaceChains(ns_pred, succ);
                                succ.ReplaceChains(b_clone, succ.successor);
                                ns_pred.ReplaceChains(ns_pred.predecessor, b_clone);

                                r.route.Add(b_clone);
                            }

                            score += increment;
                            grabbeltonFreq234.Remove(b);
                        }
                    }
                }
            }
        }
        void shiftWithin()
        {
            int dag = rnd.Next(5);
            int truck = rnd.Next(2);
            Dag d = trucksEnRoutes[truck][dag];
            int route = rnd.Next(d.routes.Count); // pak een willekeurige route van een dag
            Route r = d.routes[route];

            if (r.route.Count > 2) // denk niet dat dit nodig is.
            {
                int ind1 = rnd.Next(2, r.route.Count); // sla de storten over
                int ind2 = rnd.Next(1, r.route.Count); // voor de succ moet aan het einde zijn.
                int freqin1 = rnd.Next(4);
                int freqin2 = rnd.Next(4);

                if (ind1 != ind2 && r.route[ind1].successor != r.route[ind2])
                {
                    Bedrijf b1 = r.route[ind1];
                    Bedrijf b1_pred = b1.predecessor;
                    Bedrijf b1_succ = b1.successor;

                    Bedrijf newSucc = r.route[ind2];
                    Bedrijf ns_pred = newSucc.predecessor;

                    // bereken de increment op basis van de neighbours
                    double tijdDelta;
                    tijdDelta = -rijtijd(b1_succ, b1) - rijtijd(b1, b1_pred) - rijtijd(newSucc, ns_pred) + rijtijd(b1, ns_pred) + rijtijd(newSucc, b1);
                    // ns_pred -> newSucc -> b1 -> b1_succ // dit is de enige case die we hoeven te overwegen
                    if (b1_pred == newSucc)
                    {
                        tijdDelta += rijtijd(b1_succ, newSucc);
                    }
                    else
                    {
                        // als ze geen neighbours van elkaar zijn doe dit.
                        tijdDelta += rijtijd(b1_succ, b1_pred);
                    }

                    increment = tijdDelta;

                    if (d.tijdsduur + increment < 12 * 60 && acceptIncrement())
                    {
                        d.tijdsduur += increment;

                        // als het buren zijn
                        if (b1_pred == newSucc) // ns_pred -> newSucc -> b1 -> b1_succ
                        {
                            b1.ReplaceChains(ns_pred, newSucc);
                            b1_succ.ReplaceChains(newSucc, b1_succ.successor);

                            newSucc.ReplaceChains(b1, b1_succ);
                            ns_pred.ReplaceChains(ns_pred.predecessor, b1);
                        }
                        else
                        {
                            b1.ReplaceChains(ns_pred, newSucc);
                            b1_pred.ReplaceChains(b1_pred.predecessor, b1_succ);
                            b1_succ.ReplaceChains(b1_pred, b1_succ.successor);

                            newSucc.ReplaceChains(b1, newSucc.successor);
                            ns_pred.ReplaceChains(ns_pred.predecessor, b1);
                        }

                        score += increment;
                    }

                }

            }
        }
        void shiftBetween()
        {
            // werkt alleen voor frequentie 1
            int dagkey1 = rnd.Next(0, 5);
            int truckkey1 = rnd.Next(0, 2);
            Dag d1 = trucksEnRoutes[truckkey1][dagkey1];
            int dagkey2 = rnd.Next(0, 5);
            int truckkey2 = rnd.Next(0, 2);
            Dag d2 = trucksEnRoutes[truckkey2][dagkey2];

            int routeKey1 = rnd.Next(d1.routes.Count);
            int routeKey2 = rnd.Next(d2.routes.Count);
            Route r1 = d1.routes[routeKey1];
            Route r2 = d2.routes[routeKey2];

            if (routeKey1 != routeKey2 && r1.route.Count > 2)
            {
                int ind1 = rnd.Next(2, r1.route.Count);
                int ind2 = rnd.Next(1, r2.route.Count);
                Bedrijf b1 = r1.route[ind1];
                if (b1.freq != 1) { return; }
                Bedrijf newSucc = r2.route[ind2]; // new successor

                Bedrijf b1_pred = b1.predecessor;
                Bedrijf b1_succ = b1.successor;

                Bedrijf newSucc_pred = newSucc.predecessor;

                // kan nooit neighbors zijn
                double tijdDelta1 = -rijtijd(b1, b1_pred) - rijtijd(b1_succ, b1) + rijtijd(b1_succ, b1_pred) - b1.ldm; // voor dag 1
                double tijdDelta2 = -rijtijd(newSucc, newSucc_pred) + rijtijd(b1, newSucc_pred) + rijtijd(newSucc, b1) + b1.ldm; // voor dag 2

                double capDelta1 = -b1.vpc * b1.cont;
                double capDelta2 = b1.vpc * b1.cont;
                if (r1.route.Count == 3)
                {
                    tijdDelta1 -= 30;
                }
                if (r2.route.Count == 2)
                {
                    tijdDelta2 += 30;
                }

                increment = tijdDelta1 + tijdDelta2;

                // check constraints
                if (d1.tijdsduur + tijdDelta1 < 12 * 60 && d2.tijdsduur + tijdDelta2 < 12 * 60 && r1.capaciteit + capDelta1 < 100000 && r2.capaciteit + capDelta2 < 100000 && acceptIncrement())
                {
                    // verander chains
                    d1.tijdsduur += tijdDelta1;
                    d2.tijdsduur += tijdDelta2;
                    r1.capaciteit += capDelta1;
                    r2.capaciteit += capDelta2;

                    newSucc.ReplaceChains(b1, newSucc.successor);
                    newSucc_pred.ReplaceChains(newSucc_pred.predecessor, b1);

                    b1.ReplaceChains(newSucc_pred, newSucc);
                    b1_pred.ReplaceChains(b1_pred.predecessor, b1_succ);
                    b1_succ.ReplaceChains(b1_pred, b1_succ.successor);

                    r1.route.Remove(b1);
                    r2.route.Add(b1);

                    score += increment;
                }

            }
        }
        void removeMultiple()
        {
            int dagkey = rnd.Next(0, 5);
            int truckkey = rnd.Next(0, 2);

            Dag d = trucksEnRoutes[truckkey][dagkey];
            Route r = d.routes[rnd.Next(d.routes.Count)];
            Bedrijf b = r.route[rnd.Next(r.route.Count)];
            if (b.freq > 1)
            {
                List<Bedrijf> clones = new List<Bedrijf>();
                List<Route> routes = new List<Route>();
                List<int> dagindexen = new List<int>();
                List<int> truckindexen = new List<int>();

                for (int t = 0; t < 2; t++)
                {
                    for (int dk = 0; dk < 5; dk++)
                    {
                        foreach (Route dr in trucksEnRoutes[t][dk].routes)
                        {
                            foreach (Bedrijf bd in dr.route)
                            {
                                if (bd.order == b.order)
                                {
                                    clones.Add(bd);
                                    truckindexen.Add(t);
                                    dagindexen.Add(dk);
                                    routes.Add(dr);
                                }
                            }
                        }
                    }
                }

                List<double> tijdDeltas = new List<double>();
                List<double> capDeltas = new List<double>();
                for (int i = 0; i < clones.Count; i++)
                {
                    Bedrijf clone = clones[i];
                    double tijdDelta;
                    tijdDelta = rijtijd(clone.successor, clone.predecessor) - rijtijd(clone, clone.predecessor) - rijtijd(clone.successor, clone) - b.ldm;
                    if (routes[i].route.Count == 3)
                    {
                        tijdDelta += -30;
                    }
                    double capDelta = -b.vpc * b.cont;
                    tijdDeltas.Add(tijdDelta);
                    capDeltas.Add(capDelta);
                }
                increment = b.ldm * 3 * b.freq + tijdDeltas.Sum();

                if (acceptIncrement())
                {
                    for (int i = 0; i < clones.Count; i++)
                    {
                        Dag dclone = trucksEnRoutes[truckindexen[i]][dagindexen[i]];
                        dclone.tijdsduur += tijdDeltas[i];

                        Route rclone = routes[i];
                        rclone.capaciteit += capDeltas[i];

                        Bedrijf bclone = clones[i];
                        Bedrijf pred = bclone.predecessor;
                        Bedrijf succ = bclone.successor;

                        pred.ReplaceChains(pred.predecessor, succ);
                        succ.ReplaceChains(pred, succ.successor);

                        rclone.route.Remove(bclone);
                    }

                    score += increment;
                    grabbeltonFreq234.Add(b);
                }
            }
        }

        // reken de rijtijd uit tussen een bedrijf en zijn voorganger
        double rijtijd(Bedrijf b, Bedrijf pred)
        {
            return distDict[pred.matrixID, b.matrixID];
        }

        // maak een oplossing van een lijst van strings
        public void SlnfromString(List<string> input, Dictionary<int, Bedrijf> grbltn, Dictionary<int, Bedrijf> grbltnF234)
        {
            // maak de dagen per truck
            List<(int, int)>[,] t_and_d = new List<(int, int)>[2, 5];
            for (int t = 0; t < 2; t++)
            {
                for (int dk = 0; dk < 5; dk++)
                {
                    t_and_d[t, dk] = new List<(int, int)>();
                }
            }

            List<Bedrijf> bedrijven = new List<Bedrijf>();
            List<int> f234Orders = new List<int>(); // hou bij welke van de bedrijven met freq>1 al zijn toegevoegd

            foreach (string line in input)
            {
                string[] parts = line.Replace(" ", "").Split(";");
                Dag dag = trucksEnRoutes[int.Parse(parts[0]) - 1][int.Parse(parts[1]) - 1];

                // save a tuple of the order sequence number and order number
                t_and_d[int.Parse(parts[0]) - 1, int.Parse(parts[1]) - 1].Add((int.Parse(parts[2]), int.Parse(parts[3])));
            }

            // bouw de routes op
            for (int t = 0; t < 2; t++)
            {
                for (int dk = 0; dk < 5; dk++)
                {
                    Dag dag = trucksEnRoutes[t][dk];
                    dag.routes.Clear();

                    Dictionary<int, int> kv = t_and_d[t, dk].ToDictionary(x => x.Item1, x => x.Item2);

                    int r_index = 0;
                    Route r = new Route(stort);
                    dag.routes.Add(r);
                    for (int i = 1; i < kv.Count + 1; i++)
                    {
                        if (kv[i] == 0)
                        {
                            // maak een nieuwe route aan
                            dag.routes[r_index].maakLinkedList();
                            if (i != kv.Count)
                            {
                                dag.routes.Add(new Route(stort));
                                r_index++;
                            }
                        }
                        else if (i != kv.Count)
                        {
                            // haal het bedrijf uit de juiste grabbelton
                            Bedrijf b = grbltn.ContainsKey(kv[i]) ? grbltn[kv[i]] : grbltnF234[kv[i]];

                            if (b.freq == 1)
                            {
                                grbltn.Remove(b.order); // dit kan niet voor dingen die vaker voor moeten komen. maar wat doe je dan
                                dag.routes[r_index].route.Add(b);
                            }
                            else
                            {
                                // maak de bedrijven met freq>1 uniek in de route zodat ze unieke pred en succ kunnen hebben en geen cykels veroorzaken
                                Bedrijf b_clone = new Bedrijf(b.inputArray);
                                b_clone.dagkey = dk;
                                dag.routes[r_index].route.Add(b_clone);
                                f234Orders.Add(b.order);
                            }

                            dag.routes[r_index].capaciteit += b.cont * b.vpc;
                        }
                    }
                }
            }

            // verwijder bedrijven met freq > 1 uit de bijbehorende grabbelton
            foreach (int order in f234Orders)
            {
                grbltnF234.Remove(order);
            }
            // gebruik de aangepaste grabbeltonnen
            grabbelton = grbltn.Values.ToList();
            grabbeltonFreq234 = grbltnF234.Values.ToList();


            // extra controle dat ze allemaal de juiste hoeveelheid routes hebben.
            if (trucksEnRoutes[0][0].routes.Count == 1)
            {
                trucksEnRoutes[0][0].routes.Add(new Route(stort));
            }
            if (trucksEnRoutes[1][0].routes.Count == 1)
            {
                trucksEnRoutes[1][0].routes.Add(new Route(stort));
            }
            if (trucksEnRoutes[0][3].routes.Count == 1)
            {
                trucksEnRoutes[0][3].routes.Add(new Route(stort));
            }
            if (trucksEnRoutes[0][4].routes.Count == 1)
            {
                trucksEnRoutes[0][4].routes.Add(new Route(stort));
            }
            if (trucksEnRoutes[1][4].routes.Count == 1)
            {
                trucksEnRoutes[1][4].routes.Add(new Route(stort));
            }
        }
        public List<string> makeString(Dag[][] trucksenroute)
        {
            List<string> res = new List<string>();
            // loop door trucks
            for (int i = 0; i < 2; i++)
            {
                // loop door dagen
                for (int j = 0; j < 5; j++)
                {
                    List<string> routeString = trucksenroute[i][j].makeString();
                    foreach (string s in routeString)
                    {
                        res.Add($"{i + 1}; {j + 1}; {s}");
                    }
                }
            }
            return res;
        }
        public void beginScore()
        {
            double travelTime = 0;
            double grabbeltonKosten = 0;

            // loop door de grabbeltonnen
            foreach (Bedrijf b in grabbelton)
            {
                grabbeltonKosten += b.ldm * 3 * b.freq;
            }
            foreach (Bedrijf b in grabbeltonFreq234)
            {
                grabbeltonKosten += b.ldm * 3 * b.freq;
            }

            // loop door elke route
            for (int truck = 0; truck < 2; truck++)
            {
                for (int i = 0; i < 5; i++)
                {
                    // calculate the score of each route in the beginning
                    Dag d = trucksEnRoutes[truck][i];
                    d.tijdsduur = 0;

                    foreach (Route r in d.routes)
                    {
                        if (r.route.Count > 2)
                        {
                            double routekosten = 30;
                            routekosten += recurseRoute(r.route[0]);

                            d.tijdsduur += routekosten;
                            travelTime += routekosten;
                        }
                    }
                }
            }
            score = travelTime + grabbeltonKosten;
        }
        public double recurseRoute(Bedrijf b)
        {
            // loop recursief door de route en tel de rijtijden op
            if (b.successor == null)
            {
                return 0;
            }
            return b.ldm + rijtijd(b.successor, b) + recurseRoute(b.successor);
        }

    }
}
