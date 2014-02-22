using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class Point
    {
        public float x;
        public float y;
        public float z;

        public int id;

        public List<HalfEdge> halfEdges;

        public Point(float x, float y, float z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.halfEdges = new List<HalfEdge>();
        }

        public Point SetId(int id)
        {
            this.id = id;
            return this;
        }

        public static implicit operator bool(Point a)
        {
            return a != null;
        }

        public override string ToString()
        {
            return string.Concat("(", this.x, ", ", this.y, ", ", this.z, ")");
        }
    }
}