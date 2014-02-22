using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class SurfaceMesh : Surface
    {
        public Mesh mesh;

        public SurfaceMesh() { }

        public SurfaceMesh FromPoints(Point[] points)
        {
            SurfaceMesh surface = new SurfaceMesh();

            surface.AddPoints(points);

            return surface;
        }
    }
}