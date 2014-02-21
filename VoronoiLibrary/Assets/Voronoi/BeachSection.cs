using UnityEngine;
using System.Collections;

namespace Voronoi
{
    public class BeachSection : RBNodeBase<BeachSection>
    {
        public Point site;
        public CircleEvent circleEvent;
        public Edge edge;

        public BeachSection(Point site)
        {
            this.site = site;
        }
    }
}