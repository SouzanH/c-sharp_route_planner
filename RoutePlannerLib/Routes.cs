
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Fhnw.Ecnf.RoutePlanner.RoutePlannerLib;
using Fhnw.Ecnf.RoutePlanner.RoutePlannerLib.Util;
using System.Diagnostics;

namespace Fhnw.Ecnf.RoutePlanner.RoutePlannerLib
{
    /// <summary>
    /// Manages a routes from a city to another city.
    /// </summary>
    public class Routes : IRoutes
    {
        private readonly List<Link> routes = new List<Link>();
        private readonly Cities cities;
        public delegate void RouteRequestHandler(object sender, RouteRequestEventArgs e);
        public event RouteRequestHandler RouteRequestEvent;
        private static TraceSource routesLogger = new TraceSource("Routes");

        public int Count
        {
            get { return routes.Count; }
        }

        /// <summary>
        /// Initializes the Routes with the cities.
        /// </summary>
        /// <param name="cities"></param>
        public Routes(Cities cities)
        {
            this.cities = cities;
        }

        /// <summary>
        /// Reads a list of links from the given file.
        /// Reads only links where the cities exist.
        /// </summary>
        /// <param name="filename">name of links file</param>
        /// <returns>number of read route</returns>
        public int ReadRoutes(string filename)
        {
            routesLogger.TraceEvent(TraceEventType.Information, 3, "ReadRoutes started");
            try
            {
                TextReader reader = new StreamReader(filename);

                foreach (var line in reader.GetSplittedLines('\t'))
                {

                    City city1 = cities.FindCity(line[0]);
                    City city2 = cities.FindCity(line[1]);

                    // only add links, where the cities are found 
                    if ((city1 != null) && (city2 != null))
                    {
                        routes.Add(new Link(city1, city2, city1.Location.Distance(city2.Location), TransportModes.Rail));
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                routesLogger.TraceEvent(TraceEventType.Critical, 9, e.ToString());
            }

                
            
            routesLogger.TraceEvent(TraceEventType.Information, 4, "ReadRoutes ended");
            return Count;

        }

        public List<Link> FindShortestRouteBetween(string fromCity, string toCity, TransportModes mode)
        {
            if (RouteRequestEvent != null)
            {
                RouteRequestEvent(this, new RouteRequestEventArgs(fromCity, toCity, mode));
            }
            var citiesBetween = cities.FindCitiesBetween(cities.FindCity(fromCity), cities.FindCity(toCity));
            if (citiesBetween == null || citiesBetween.Count < 1 || routes == null || routes.Count < 1)
                return null;

            var source = citiesBetween[0];
            var target = citiesBetween[citiesBetween.Count - 1];

            Dictionary<City, double> dist;
            Dictionary<City, City> previous;
            var q = FillListOfNodes(citiesBetween, out dist, out previous);
            dist[source] = 0.0;

            // the actual algorithm
            previous = SearchShortestPath(mode, q, dist, previous);

            // create a list with all cities on the route
            var citiesOnRoute = GetCitiesOnRoute(source, target, previous);

            // prepare final list if links
            return FindPath(citiesOnRoute, mode);
        }

        private List<Link> FindPath(List<City> citiesOnRoute, TransportModes mode)
        {
            var citiesAsLinks = new List<Link>(citiesOnRoute.Count);
            for(int i = 0; i < citiesOnRoute.Count - 1; i++)
            {
                City c1 = citiesOnRoute[i];
                City c2 = citiesOnRoute[i + 1];
                citiesAsLinks.Add(new Link(c1, c2, c1.Location.Distance(c2.Location)));
            }
           
            return citiesAsLinks;
        }

        private static List<City> FillListOfNodes(List<City> cities, out Dictionary<City, double> dist, out Dictionary<City, City> previous)
        {
            var q = new List<City>(); // the set of all nodes (cities) in Graph ;
            dist = new Dictionary<City, double>();
            previous = new Dictionary<City, City>();
            foreach (var v in cities)
            {
                dist[v] = double.MaxValue;
                previous[v] = null;
                q.Add(v);
            }

            return q;
        }

        /// <summary>
        /// Searches the shortest path for cities and the given links
        /// </summary>
        /// <param name="mode">transportation mode</param>
        /// <param name="q"></param>
        /// <param name="dist"></param>
        /// <param name="previous"></param>
        /// <returns></returns>
        private Dictionary<City, City> SearchShortestPath(TransportModes mode, List<City> q, Dictionary<City, double> dist, Dictionary<City, City> previous)
        {
            while (q.Count > 0)
            {
                City u = null;
                var minDist = double.MaxValue;

                // find city u with smallest dist
                // also possible with q.Where(c => dist[c] < minDist)
                foreach (var c in q)
                {
                    if (dist[c] < minDist)
                    {
                        u = c;
                        minDist = dist[c];
                    }
                }
                    

                if (u != null)
                {
                    q.Remove(u);
                    foreach (var n in FindNeighbours(u, mode))
                    {
                        var l = FindLink(u, n, mode);
                        var d = dist[u];
                        if (l != null) 
                        {
                            d += l.Distance;
                        }   
                        else
                        {
                            d += double.MaxValue;
                        }
                           

                        if (dist.ContainsKey(n) && d < dist[n])
                        {
                            dist[n] = d;
                            previous[n] = u;
                        }
                    }
                }
                else
                {
                    break;
                }  
            }
            return previous;
        }

        //F�r was wird der transportMode �bergeben, dieser ist als readonly markiert im waypoint?
        private Link FindLink(City u, City n, TransportModes mode)
        {
            return u != null && n != null ? new Link(u, n, u.Location.Distance(n.Location)) : null;
        }


        /// <summary>
        /// Finds all neighbor cities of a city. 
        /// </summary>
        /// <param name="city">source city</param>
        /// <param name="mode">transportation mode</param>
        /// <returns>list of neighbor cities</returns>
        private List<City> FindNeighbours(City city, TransportModes mode)
        {
            return (
                from route in routes
                where route.TransportMode.Equals(mode) && (route.FromCity.Equals(city) || route.ToCity.Equals(city))
                select route.FromCity.Equals(city) ? route.ToCity : route.FromCity
            ).ToList();

        }

        private List<City> GetCitiesOnRoute(City source, City target, Dictionary<City, City> previous)
        {
            var citiesOnRoute = new List<City>();
            var cr = target;
            while (previous[cr] != null)
            {
                citiesOnRoute.Add(cr);
                cr = previous[cr];
            }
            citiesOnRoute.Add(source);

            citiesOnRoute.Reverse();
            return citiesOnRoute;
        }

        /// <summary>
        /// Returns a set (unique entries) of cities, which have routes with this transport mode.
        /// </summary>
        /// <param name="transportMode"></param>
        /// <returns></returns>
        public City[] FindCities(TransportModes transportMode)
        {
            //Not correct
            return routes.Where(r => r.TransportMode == transportMode).SelectMany(r => new City[]{ r.FromCity, r.ToCity }).Distinct().ToArray();
        }
    }
}
