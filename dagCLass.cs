using System;

namespace GroteOpdracht
{
    public class Dag
    {
        public double tijdsduur; // in minuten
        public List<Route> routes;
        public Dag(string[] stort, int numRoute = 1)
        {
            routes = new List<Route>();
            for (int i = 0; i < numRoute; i++)
            {
                routes.Add(new Route(stort));
            }
            tijdsduur = 0;
        }
        public List<string> makeString()
        {
            List<string> res = new List<string>();
            foreach (Route route in routes)
            {
                List<string> routeRes = route.makeString(route.route[0], res.Count);
                foreach (string s in routeRes)
                {
                    res.Add(s);
                }
            }
            return res;
        }
    }
}