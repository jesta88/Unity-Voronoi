using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class Edge
    {
        public Point lSite;
        public Point rSite;

        public Point va;
        public Point vb;

        public Edge(Point lSite = null, Point rSite = null)
        {
            this.lSite = lSite;
            this.rSite = rSite;
            this.va = null;
            this.vb = null;
        }

        public void SetStartPoint(Point lSite, Point rSite, Point vertex)
        {
            if (!this.va && !this.vb)
            {
                this.va = vertex;
                this.lSite = lSite;
                this.rSite = rSite;
            }
            else if (this.lSite == rSite)
            {
                this.vb = vertex;
            }
            else
            {
                this.va = vertex;
            }
        }

        public void SetEndPoint(Point lSite, Point rSite, Point vertex)
        {
            this.SetStartPoint(rSite, lSite, vertex);
        }

        public static implicit operator bool(Edge a)
        {
            return a != null;
        }
    }
}