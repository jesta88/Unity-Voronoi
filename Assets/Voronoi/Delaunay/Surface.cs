using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public abstract class Surface
    {
        protected List<Point> points;

        public void AddPoints(Point[] points)
        {
            this.points.AddRange(points);
        }

        public float GetPonderation(float distance, float min, float max)
        {
            return (max / min) * distance + max;
        }

        public List<Point> GetPoints()
        {
            return this.points;
        }
    }
}