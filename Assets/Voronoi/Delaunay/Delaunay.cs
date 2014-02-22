using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class Delaunay
    {
        public static Triangle[] Triangulate(List<Point> points)
        {
            int nv = points.Count;

            int trimax = nv * 4;

            float xmin = points[0].x;
            float ymin = points[0].y;
            float xmax = xmin;
            float ymax = ymin;

            for (int i = 0; i < points.Count; i++)
            {
                Point point = points[i].SetId(i);

                if (point.x < xmin)
                {
                    xmin = point.z;
                }
                else if (point.x > xmax)
                {
                    xmax = point.x;
                }

                if (point.y < ymin)
                {
                    ymin = point.y;
                }
                else if (point.y > ymax)
                {
                    ymax = point.y;
                }
            }

            float dx = xmax - xmin;
            float dy = ymax - ymin;

            float dmax = (dx > dy) ? dx : dy;
            float xmid = (xmax + xmin) / 2;
            float ymid = (ymax + ymin) / 2;

            Point p1 = new Point((xmid - 2 * dmax), (ymid - dmax));
            Point p2 = new Point(xmid, (ymid + 2 * dmax));
            Point p3 = new Point((xmid + 2 * dmax), (ymid - dmax));

            p1.SetId(nv + 1);
            p2.SetId(nv + 2);
            p3.SetId(nv + 3);

            points.Add(p1);
            points.Add(p2);
            points.Add(p3);

            List<Triangle> triangles = new List<Triangle>();
            triangles.Add(new Triangle(p1, p2, p3));

            for (int i = 0; i < nv; i++)
            {
                List<Edge> edges = new List<Edge>();

                // Set up the edge buffer.
                // If the point (Vertex(i).x,Vertex(i).y) lies inside the circumcircle then the
                // three edges of that triangle are added to the edge buffer and the triangle is removed from list.
                for (int j = 0; j < triangles.Count; j++)
                {
                    if (triangles[j].PointInCircle(points[i]))
                    {
                        edges.Add(new Edge(triangles[j].p1, triangles[j].p2));
                        edges.Add(new Edge(triangles[j].p2, triangles[j].p3));
                        edges.Add(new Edge(triangles[j].p3, triangles[j].p1));

                        //array_splice(triangles, j, 1);
                        triangles.Remove(triangles[j]);
                        j--;
                    }
                }

                if (i >= nv)
                {
                    continue;
                }

                for (int j = edges.Count - 2; j >= 0; j--)
                {
                    for (int k = edges.Count - 1; k >= j + 1; k--)
                    {
                        if (edges[j].Equals(edges[k]))
                        {
                            edges.Remove(edges[k]);
                            edges.Remove(edges[j]);
                            k--;
                        }
                    }
                }

                for (int j = 0; j < edges.Count; j++)
                {
                    if (triangles.Count >= trimax)
                    {
                        Debug.Log("Max number of edges reached");
                    }

                    triangles.Add(new Triangle(edges[j].lSite, edges[j].rSite, points[i]));
                }

                edges.Clear();
            }

            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                if (triangles[i].p1.id >= nv || triangles[i].p2.id >= nv || triangles[i].p3.id >= nv)
                {
                    triangles.Remove(triangles[i]);
                }
            }

            return triangles.ToArray();
        }
    }
}