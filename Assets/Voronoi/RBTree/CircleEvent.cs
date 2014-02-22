using UnityEngine;
using System.Collections;

namespace Voronoi
{
    public class CircleEvent : RBNodeBase<CircleEvent>
    {
        public BeachSection arc;
        public Point site;

        public float x;
        public float y;
        public float yCenter;

        public CircleEvent()
        {
            arc = null;
            site = null;
            x = y = yCenter = 0;
        }
    }
}