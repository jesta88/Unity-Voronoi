using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class VoronoiGraph
    {
        public List<Point> sites;
        public List<Cell> cells;
        public List<Edge> edges;

        public VoronoiGraph()
        {
            sites = new List<Point>();
            cells = new List<Cell>();
            edges = new List<Edge>();
        }

        public static implicit operator bool(VoronoiGraph a)
        {
            return a != null;
        }
    }
}