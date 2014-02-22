using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class HalfEdge
    {
        public Point site;
        public Edge edge;
        public float angle;

        public HalfEdge(Edge edge, Point lSite, Point rSite)
        {
            this.site = lSite;
            this.edge = edge;
            // 'angle' is a value to be used for properly sorting the
            // halfsegments counterclockwise. By convention, we will
            // use the angle of the line defined by the 'site to the left'
            // to the 'site to the right'.
            // However, border edges have no 'site to the right': thus we
            // use the angle of line perpendicular to the halfsegment (the
            // edge should have both end points defined in such case.)
            if (rSite)
            {
                this.angle = Mathf.Atan2(rSite.y - lSite.y, rSite.x - lSite.x);
            }
            else
            {
                Point va = edge.va;
                Point vb = edge.vb;
                // rhill 2011-05-31: used to call getStartpoint()/getEndpoint(),
                // but for performance purpose, these are expanded in place here.
                this.angle = (edge.lSite == lSite) ? Mathf.Atan2(vb.x - va.x, va.y - vb.y)
                                                   : Mathf.Atan2(va.x - vb.x, vb.y - va.y);
            }
        }

        public Point GetStartPoint()
        {
            return this.edge.lSite == this.site ? this.edge.va : this.edge.vb;
        }

        public Point GetEndPoint()
        {
            return this.edge.lSite == this.site ? this.edge.vb : this.edge.va;
        }

        public static implicit operator bool(HalfEdge a)
        {
            return a != null;
        }
    }
}