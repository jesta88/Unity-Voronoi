using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class Cell
    {
        public Point site;
        public List<HalfEdge> halfEdges;

        public bool closeMe;

        public Cell(Point site)
        {
            this.site = site;
            this.halfEdges = new List<HalfEdge>();
            this.closeMe = false;
        }

        public Cell Init(Point site)
        {
            this.site = site;
            this.halfEdges = new List<HalfEdge>();
            this.closeMe = false;
            return this;
        }

        public int Prepare()
        {
            Edge edge = null;

            // Get rid of unused halfEdges
            // rhill 2011-05-27: Keep it simple, no point here in trying
            // to be fancy: dangling edges are a typically a minority.
            for (int i = this.halfEdges.Count - 1; i >= 0; i--)
            {
                edge = this.halfEdges[i].edge;
                if (!edge.vb || !edge.va)
                {
                    //this.halfEdges.RemoveAt(i);
                    this.halfEdges.Remove(this.halfEdges[i]);
                }
            }

            // rhill 2011-05-26: I tried to use a binary search at insertion
            // time to keep the array sorted on-the-fly (in Cell.addHalfedge()).
            // There was no real benefits in doing so, performance on
            // Firefox 3.6 was improved marginally, while performance on
            // Opera 11 was penalized marginally.
            /*this.halfEdges.Sort((a, b) =>
                {
                    float r = b.angle - a.angle;
                    return r < 0 ? Mathf.FloorToInt(r) : Mathf.CeilToInt(r);
                });
            */
            this.halfEdges = this.halfEdges.OrderByDescending(h => h.angle).ToList();

            /*string output = "";
            for (int i = 0; i < this.halfEdges.Count; i++)
            {
                HalfEdge he = this.halfEdges[i];
                output += i.ToString() + ": " + he.angle.ToString("F1");
                if (i < this.halfEdges.Count - 1)
                    output += ", ";
            }
            Debug.Log(output);*/

            return this.halfEdges.Count;
        }

        // Return a list of the neighbor Ids
        public List<int> GetNeighborIds()
        {
            List<int> neighbors = new List<int>();
            Edge edge;

            for (int i = this.halfEdges.Count - 1; i >= 0; i--)
            {
                edge = this.halfEdges[i].edge;
                if (edge.lSite != null && edge.lSite.id != this.site.id)
                {
                    neighbors.Add(edge.lSite.id);
                }
                else if (edge.rSite != null && edge.rSite.id != this.site.id)
                {
                    neighbors.Add(edge.rSite.id);
                }
            }
            return neighbors;
        }

        // Compute bounding box
        //
        public Bounds GetBounds()
        {
            float xmin = Mathf.Infinity;
            float ymin = Mathf.Infinity;
            float xmax = -Mathf.Infinity;
            float ymax = -Mathf.Infinity;
            Point v;
            float vx, vy;

            for (int i = this.halfEdges.Count - 1; i >= 0; i--)
            {
                v = this.halfEdges[i].GetStartPoint();
                vx = v.x;
                vy = v.y;
                if (vx < xmin) { xmin = vx; }
                if (vy < ymin) { ymin = vy; }
                if (vx > xmax) { xmax = vx; }
                if (vy > ymax) { ymax = vy; }
                // we dont need to take into account end point,
                // since each end point matches a start point
            }

            // TODO: verify y
            Bounds bounds = new Bounds();
            bounds.SetMinMax(new Vector3(xmin, 0, xmax), new Vector3(ymin, 0, ymax));

            return bounds;
        }

        // Return whether a point is inside, on, or outside the cell:
        //   -1: point is outside the perimeter of the cell
        //    0: point is on the perimeter of the cell
        //    1: point is inside the perimeter of the cell
        //
        public int PointIntersection(float x, float y)
        {
            // Check if point in polygon. Since all polygons of a Voronoi
            // diagram are convex, then:
            // http://paulbourke.net/geometry/polygonmesh/
            // Solution 3 (2D):
            //   "If the polygon is convex then one can consider the polygon
            //   "as a 'path' from the first vertex. A point is on the interior
            //   "of this polygons if it is always on the same side of all the
            //   "line segments making up the path. ...
            //   "(y - y0) (x1 - x0) - (x - x0) (y1 - y0)
            //   "if it is less than 0 then P is to the right of the line segment,
            //   "if greater than 0 it is to the left, if equal to 0 then it lies
            //   "on the line segment"
            HalfEdge halfEdge;
            Point p1, p2;
            float r;

            for (int i = this.halfEdges.Count - 1; i >= 0; i--)
            {
                halfEdge = this.halfEdges[i];
                p1 = halfEdge.GetStartPoint();
                p2 = halfEdge.GetEndPoint();
                r = (y - p1.y) * (p2.x - p1.x) - (x - p1.x) * (p2.y - p1.y);
                if (r == 0)
                {
                    return 0;
                }
                if (r > 0)
                {
                    return -1;
                }
            }
            return 1;
        }
    }
}
