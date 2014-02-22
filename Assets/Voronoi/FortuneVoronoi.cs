using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class FortuneVoronoi
    {
        public static readonly float EPSILON = 1e-1f;

        protected List<Edge> edges;
        protected List<Cell> cells;

        private List<BeachSection> beachSectionJunkyard;
        private List<CircleEvent> circleEventJunkyard;

        protected RBTree<BeachSection> beachLine;
        protected RBTree<CircleEvent> circleEvents;

        private CircleEvent firstCircleEvent;

        public bool valid = false;

        public FortuneVoronoi()
        {
            this.edges = null;
            this.cells = null;

            this.beachSectionJunkyard = new List<BeachSection>();
            this.circleEventJunkyard = new List<CircleEvent>();
        }

        public void Reset()
        {
            if (!this.beachLine)
            {
                this.beachLine = new RBTree<BeachSection>();
            }

            // Move leftover beachsections to the beachsection junkyard.
            if (this.beachLine.Root)
            {
                BeachSection beachSection = this.beachLine.GetFirst(this.beachLine.Root);
                while (beachSection)
                {
                    this.beachSectionJunkyard.Add(beachSection);
                    beachSection = beachSection.Next;
                }
            }
            this.beachLine.Root = null;

            if (!this.circleEvents)
            {
                this.circleEvents = new RBTree<CircleEvent>();
            }
            this.circleEvents.Root = this.firstCircleEvent = null;

            this.edges = new List<Edge>();
            this.cells = new List<Cell>();
        }

        public VoronoiGraph Compute(List<Point> sites, Bounds bbox)
        {
            this.Reset();

            // Initialize site event queue
            var siteEvents = sites.Take(sites.Count).ToList();
            /*siteEvents.Sort((a, b) =>
                {
                    float r = b.y - a.y;
                    if (r != 0) { return Mathf.CeilToInt(r); }
                    return Mathf.CeilToInt(b.x - a.x);
                });*/
            siteEvents = siteEvents.OrderByDescending(s => s.y).ToList();

            // process queue
            Point site = siteEvents.Last();
            siteEvents.Remove(site);
            int siteid = 0;
            float xsitex = -Mathf.Infinity;
            float xsitey = -Mathf.Infinity;

            // main loop
            for (; ; )
            {
                // we need to figure whether we handle a site or circle event
                // for this we find out if there is a site event and it is
                // 'earlier' than the circle event
                CircleEvent circle = this.firstCircleEvent;

                // add beach section
                if (site && (!circle || site.y < circle.y || (site.y == circle.y && site.x < circle.x)))
                {
                    // only if site is not a duplicate
                    if (site.x != xsitex || site.y != xsitey)
                    {
                        // first create cell for new site
                        //this.cells[siteid] = new Cell(site);
                        this.cells.Insert(siteid, new Cell(site));
                        site.id = siteid++;

                        // then create a beachsection for that site
                        this.AddBeachSection(site);

                        // remember last site coords to detect duplicate
                        xsitey = site.y;
                        xsitex = site.x;
                    }

                    site = siteEvents.Count > 0 ? siteEvents.Last() : null;
                    if (siteEvents.Count > 0) siteEvents.Remove(site);
                }
                // remove beach section
                else if (circle)
                {
                    this.RemoveBeachSection(circle.arc);
                }
                // all done, quit
                else
                {
                    break;
                }
            }

            // wrapping-up
            this.ClipEdges(bbox);

            this.CloseCells(bbox);

            VoronoiGraph graph = new VoronoiGraph();
            graph.sites = sites;
            graph.cells = this.cells;
            graph.edges = this.edges;

            this.Reset();

            return graph;
        }

        public Edge CreateEdge(Point lSite, Point rSite, Point va, Point vb)
        {
            Edge edge = new Edge(lSite, rSite);
            this.edges.Add(edge);

            if (va)
            {
                edge.SetStartPoint(lSite, rSite, va);
            }
            if (vb)
            {
                edge.SetEndPoint(lSite, rSite, vb);
            }

            this.cells[lSite.id].halfEdges.Add(new HalfEdge(edge, lSite, rSite));
            this.cells[rSite.id].halfEdges.Add(new HalfEdge(edge, rSite, lSite));

            return edge;
        }

        public Edge CreateBorderEdge(Point lSite, Point va, Point vb)
        {
            Edge edge = new Edge(lSite, null);
            edge.va = va;
            edge.vb = vb;
            this.edges.Add(edge);

            return edge;
        }

        // rhill 2011-06-02: A lot of Beachsection instanciations
        // occur during the computation of the Voronoi diagram,
        // somewhere between the number of sites and twice the
        // number of sites, while the number of Beachsections on the
        // beachline at any given time is comparatively low. For this
        // reason, we reuse already created Beachsections, in order
        // to avoid new memory allocation. This resulted in a measurable
        // performance gain.
        public BeachSection CreateBeachSection(Point site)
        {
            BeachSection beachSection = this.beachSectionJunkyard.Count > 0 ? this.beachSectionJunkyard.Last() : null;
            if (beachSection)
            {
                this.beachSectionJunkyard.Remove(beachSection);
                beachSection.site = site;
            }
            else
            {
                beachSection = new BeachSection(site);
            }

            return beachSection;
        }

        // calculate the left break point of a particular beach section,
        // given a particular sweep line
        public float LeftBreakPoint(BeachSection arc, float directrix)
        {
            // http://en.wikipedia.org/wiki/Parabola
            // http://en.wikipedia.org/wiki/Quadratic_equation
            // h1 = x1,
            // k1 = (y1+directrix)/2,
            // h2 = x2,
            // k2 = (y2+directrix)/2,
            // p1 = k1-directrix,
            // a1 = 1/(4*p1),
            // b1 = -h1/(2*p1),
            // c1 = h1*h1/(4*p1)+k1,
            // p2 = k2-directrix,
            // a2 = 1/(4*p2),
            // b2 = -h2/(2*p2),
            // c2 = h2*h2/(4*p2)+k2,
            // x = (-(b2-b1) + Math.sqrt((b2-b1)*(b2-b1) - 4*(a2-a1)*(c2-c1))) / (2*(a2-a1))
            // When x1 become the x-origin:
            // h1 = 0,
            // k1 = (y1+directrix)/2,
            // h2 = x2-x1,
            // k2 = (y2+directrix)/2,
            // p1 = k1-directrix,
            // a1 = 1/(4*p1),
            // b1 = 0,
            // c1 = k1,
            // p2 = k2-directrix,
            // a2 = 1/(4*p2),
            // b2 = -h2/(2*p2),
            // c2 = h2*h2/(4*p2)+k2,
            // x = (-b2 + Math.sqrt(b2*b2 - 4*(a2-a1)*(c2-k1))) / (2*(a2-a1)) + x1

            // change code below at your own risk: care has been taken to
            // reduce errors due to computers' finite arithmetic precision.
            // Maybe can still be improved, will see if any more of this
            // kind of errors pop up again.
            Point site = arc.site;
            float rfocx = site.x;
            float rfocy = site.y;
            float pby2 = rfocy - directrix;

            // parabola in degenerate case where focus is on directrix
            if (pby2 == 0)
            {
                return rfocx;
            }

            BeachSection lArc = arc.Prev;
            if (!lArc)
            {
                return -Mathf.Infinity;
            }

            site = lArc.site;
            float lfocx = site.x;
            float lfocy = site.y;
            float plby2 = lfocy - directrix;

            // parabola in degenerate case where focus is on directrix
            if (plby2 == 0)
            {
                return lfocx;
            }

            float hl = lfocx - rfocx;
            float aby2 = 1 / pby2 - 1 / plby2;
            float b = hl / plby2;

            if (aby2 != 0)
            {
                return (-b + Mathf.Sqrt(b * b - 2 * aby2 * (hl * hl / (-2 * plby2) - lfocy + plby2 / 2 + rfocy - pby2 / 2))) / aby2 + rfocx;
            }
            // both parabolas have same distance to directrix, thus break point is midway
            return (rfocx + lfocx) / 2;
        }

        // calculate the right break point of a particular beach section,
        // given a particular directrix
        public float RightBreakPoint(BeachSection arc, float directrix)
        {
            BeachSection rArc = arc.Next;
            if (rArc)
            {
                return this.LeftBreakPoint(rArc, directrix);
            }

            Point site = arc.site;
            return site.y == directrix ? site.x : Mathf.Infinity;
        }

        public void DetachBeachSection(BeachSection beachSection)
        {
            this.DetachCircleEvent(beachSection); // detach potentially attached circle event
            this.beachLine.Remove(beachSection); // remove from RB-tree
            this.beachSectionJunkyard.Add(beachSection); // mark for reuse
        }

        public void RemoveBeachSection(BeachSection beachSection)
        {
            CircleEvent circle = beachSection.circleEvent;
            float x = circle.x;
            float y = circle.yCenter;
            Point vertex = new Point(x, y);

            BeachSection previous = beachSection.Prev;
            BeachSection next = beachSection.Next;

            LinkedList<BeachSection> disappearingTransitions = new LinkedList<BeachSection>();
            disappearingTransitions.AddLast(beachSection);

            // remove collapsed beachsection from beachline
            this.DetachBeachSection(beachSection);

            // there could be more than one empty arc at the deletion point, this
            // happens when more than two edges are linked by the same vertex,
            // so we will collect all those edges by looking up both sides of
            // the deletion point.
            // by the way, there is *always* a predecessor/successor to any collapsed
            // beach section, it's just impossible to have a collapsing first/last
            // beach sections on the beachline, since they obviously are unconstrained
            // on their left/right side.

            // look left
            BeachSection lArc = previous;
            while (lArc.circleEvent &&
                   Mathf.Abs(x - lArc.circleEvent.x) < EPSILON &&
                   Mathf.Abs(y - lArc.circleEvent.yCenter) < EPSILON)
            {
                previous = lArc.Prev;
                disappearingTransitions.AddFirst(lArc);
                this.DetachBeachSection(lArc); // mark for reuse
                lArc = previous;
            }
            // even though it is not disappearing, I will also add the beach section
            // immediately to the left of the left-most collapsed beach section, for
            // convenience, since we need to refer to it later as this beach section
            // is the 'left' site of an edge for which a start point is set.
            disappearingTransitions.AddFirst(lArc);
            this.DetachCircleEvent(lArc);

            // look right
            BeachSection rArc = next;
            while (rArc.circleEvent &&
                   Mathf.Abs(x - rArc.circleEvent.x) < EPSILON &&
                   Mathf.Abs(y - rArc.circleEvent.yCenter) < EPSILON)
            {
                next = rArc.Next;
                disappearingTransitions.AddLast(rArc);
                this.DetachBeachSection(rArc);
                rArc = next;
            }
            // we also have to add the beach section immediately to the right of the
            // right-most collapsed beach section, since there is also a disappearing
            // transition representing an edge's start point on its left.
            disappearingTransitions.AddLast(rArc);
            this.DetachCircleEvent(rArc);

            // walk through all the disappearing transitions between beach sections and
            // set the start point of their (implied) edge.
            int nArcs = disappearingTransitions.Count;
            for (int iArc = 1; iArc < nArcs; iArc++)
            {
                rArc = disappearingTransitions.ElementAt(iArc);
                lArc = disappearingTransitions.ElementAt(iArc - 1);
                rArc.edge.SetStartPoint(lArc.site, rArc.site, vertex);
            }

            // create a new edge as we have now a new transition between
            // two beach sections which were previously not adjacent.
            // since this edge appears as a new vertex is defined, the vertex
            // actually define an end point of the edge (relative to the site
            // on the left)
            lArc = disappearingTransitions.ElementAt(0);
            rArc = disappearingTransitions.ElementAt(nArcs - 1);
            rArc.edge = this.CreateEdge(lArc.site, rArc.site, null, vertex);

            // create circle events if any for beach sections left in the beachline
            // adjacent to collapsed sections
            this.AttachCircleEvent(lArc);
            this.AttachCircleEvent(rArc);
        }

        public void AddBeachSection(Point site)
        {
            float x = site.x;
            float directrix = site.y;

            // find the left and right beach sections which will surround the newly
            // created beach section.
            // rhill 2011-06-01: This loop is one of the most often executed,
            // hence we expand in-place the comparison-against-epsilon calls.
            BeachSection node = this.beachLine.Root;
            BeachSection lArc = null;
            BeachSection rArc = null;
            float dxl, dxr;

            while (node)
            {
                dxl = this.LeftBreakPoint(node, directrix) - x;

                // x LessThanWithEpsilon xl => falls somewhere before the Left edge of the beachsection
                if (dxl > EPSILON)
                {
                    // this case should never happen
                    // if (!node.rbLeft) {
                    //    rArc = node.rbLeft;
                    //    break;
                    //    }
                    /*if (!node.Left) {
                        rArc = node.Left;
                        break;
                    } else {
                        node = node.Left;
                    }*/
                    node = node.Left;
                }
                else
                {
                    dxr = x - this.RightBreakPoint(node, directrix);
                    // x GreaterThanWithEpsilon xr => falls somewhere after the Right edge of the beachsection
                    if (dxr > EPSILON)
                    {
                        if (!node.Right)
                        {
                            lArc = node;
                            break;
                        }

                        node = node.Right;
                    }
                    else
                    {
                        // x EqualWithEpsilon xl => falls exactly on the Left edge of the beachsection
                        if (dxl > -EPSILON)
                        {
                            lArc = node.Prev;
                            rArc = node;
                        }
                        // x EqualWithEpsilon xr => falls exactly on the Right edge of the beachsection
                        else if (dxr > -EPSILON)
                        {
                            lArc = node;
                            rArc = node.Next;
                        }
                        // falls exactly somewhere in the middle of the beachsection
                        else
                        {
                            lArc = rArc = node;
                        }

                        break;
                    }
                }
            }

            // at this point, keep in mind that lArc and/or rArc could be
            // undefined or null.

            // create a new beach section object for the site and add it to RB-tree
            BeachSection newArc = this.CreateBeachSection(site);
            this.beachLine.Insert(lArc, newArc);

            // cases:
            //

            // [null,null]
            // least likely case: new beach section is the first beach section on the
            // beachLine.
            // This case means:
            //   no new transition appears
            //   no collapsing beach section
            //   new beachsection become root of the RB-tree
            if (!lArc && !rArc)
            {
                return;
            }

            // [lArc,rArc] where lArc == rArc
            // most likely case: new beach section split an existing beach
            // section.
            // This case means:
            //   one new transition appears
            //   the Left and Right beach section might be collapsing as a result
            //   two new nodes added to the RB-tree
            if (lArc == rArc)
            {
                // invalidate circle event of split beach section
                this.DetachCircleEvent(lArc);

                // split the beach section into two separate beach sections
                rArc = this.CreateBeachSection(lArc.site);
                this.beachLine.Insert(newArc, rArc);

                // since we have a new transition between two beach sections;
                // a new edge is born
                newArc.edge = rArc.edge = this.CreateEdge(lArc.site, newArc.site, null, null);

                // check whether the Left and Right beach sections are collapsing
                // and if so create circle events, to be notified when the point of
                // collapse is reached.
                this.AttachCircleEvent(lArc);
                this.AttachCircleEvent(rArc);

                return;
            }

            // [lArc,null]
            // even less likely case: new beach section is the *last* beach section
            // on the beachLine -- this can happen *only* if *all* the Prev beach
            // sections currently on the beachLine share the same y value as
            // the new beach section.
            // This case means:
            //   one new transition appears
            //   no collapsing beach section as a result
            //   new beach section become Right-most node of the RB-tree
            if (lArc && !rArc)
            {
                newArc.edge = this.CreateEdge(lArc.site, newArc.site, null, null);
                return;
            }

            // [null,rArc]
            // impossible case: because sites are strictly processed from top to bottom;
            // and Left to Right, which guarantees that there will always be a beach section
            // on the Left -- except of course when there are no beach section at all on
            // the beach line, which case was handled above.
            // rhill 2011-06-02: No point testing in non-debug version
            //if (!lArc && rArc) {
            //	throw "Voronoi.addBeachsection(): What is this I don't even";
            //	}
            if (!lArc && rArc)
            {
                Debug.LogError("Shouldn't appear");
            }

            // [lArc,rArc] where lArc != rArc
            // somewhat less likely case: new beach section falls *exactly* in between two
            // existing beach sections
            // This case means:
            //   one transition disappears
            //   two new transitions appear
            //   the Left and Right beach section might be collapsing as a result
            //   only one new node added to the RB-tree
            if (lArc != rArc)
            {
                // invalidate circle events of Left and Right sites
                this.DetachCircleEvent(lArc);
                this.DetachCircleEvent(rArc);

                // an existing transition disappears, meaning a vertex is defined at
                // the disappearance point.
                // since the disappearance is caused by the new beachsection, the
                // vertex is at the center of the circumscribed circle of the Left;
                // new and Right beachsections.
                // http://mathforum.org/library/drmath/view/55002.html
                // Except that I bring the origin at A to simplify
                // calculation
                Point lSite = lArc.site;
                float ax = lSite.x;
                float ay = lSite.y;
                float bx = site.x - ax;
                float by = site.y - ay;
                Point rSite = rArc.site;
                float cx = rSite.x - ax;
                float cy = rSite.y - ay;
                float d = 2 * (bx * cy - by * cx);
                float hb = bx * bx + by * by;
                float hc = cx * cx + cy * cy;
                Point vertex = new Point((cy * hb - by * hc) / d + ax, (bx * hc - cx * hb) / d + ay);

                // one transition disappear
                rArc.edge.SetStartPoint(lSite, rSite, vertex);

                // two new transitions appear at the new vertex location
                newArc.edge = this.CreateEdge(lSite, site, null, vertex);
                rArc.edge = this.CreateEdge(site, rSite, null, vertex);

                // check whether the Left and Right beach sections are collapsing
                // and if so create circle events, to handle the point of collapse.
                this.AttachCircleEvent(lArc);
                this.AttachCircleEvent(rArc);

                return;
            }
        }

        public void AttachCircleEvent(BeachSection arc)
        {
            BeachSection lArc = arc.Prev;
            BeachSection rArc = arc.Next;

            if (!lArc || !rArc)
            {
                return;
            } // does that ever happen?

            Point lSite = lArc.site;
            Point cSite = arc.site;
            Point rSite = rArc.site;

            // If site of Left beachsection is same as site of
            // Right beachsection, there can't be convergence
            if (lSite == rSite)
            {
                return;
            }

            // Find the circumscribed circle for the three sites associated
            // with the beachsection triplet.
            // rhill 2011-05-26: It is more efficient to calculate in-place
            // rather than getting the resulting circumscribed circle from an
            // object returned by calling Voronoi.circumcircle()
            // http://mathforum.org/library/drmath/view/55002.html
            // Except that I bring the origin at cSite to simplify calculations.
            // The bottom-most part of the circumcircle is our Fortune 'circle
            // event', and its center is a vertex potentially part of the final
            // Voronoi diagram.
            float bx = cSite.x;
            float by = cSite.y;
            float ax = lSite.x - bx;
            float ay = lSite.y - by;
            float cx = rSite.x - bx;
            float cy = rSite.y - by;

            // If points l.c.r are clockwise, then center beach section does not
            // collapse, hence it can't end up as a vertex (we reuse 'd' here, which
            // sign is reverse of the orientation, hence we reverse the test.
            // http://en.wikipedia.org/wiki/Curveorientation#Orientationofasimplepolygon
            // rhill 2011-05-21: Nasty finite precision error which caused circumcircle() to
            // return infinites: 1e-12 seems to fix the problem.
            float d = 2 * (ax * cy - ay * cx);
            if (d >= -2e-12)
            {
                return;
            }

            float ha = ax * ax + ay * ay;
            float hc = cx * cx + cy * cy;
            float x = (cy * ha - ay * hc) / d;
            float y = (ax * hc - cx * ha) / d;
            float ycenter = y + by;

            // Important: ybottom should always be under or at sweep, so no need
            // to waste CPU cycles by checking

            // recycle circle event object if possible
            CircleEvent circleEvent = this.circleEventJunkyard.Count > 0 ? this.circleEventJunkyard.Last() : null;
            this.circleEventJunkyard.Remove(circleEvent);
            if (!circleEvent)
            {
                circleEvent = new CircleEvent();
            }

            circleEvent.arc = arc;
            circleEvent.site = cSite;
            circleEvent.x = x + bx;
            circleEvent.y = ycenter + Mathf.Sqrt(x * x + y * y); // y bottom
            circleEvent.yCenter = ycenter;
            arc.circleEvent = circleEvent;

            // find insertion point in RB-tree: circle events are ordered from
            // smallest to largest
            CircleEvent predecessor = null;
            CircleEvent node = this.circleEvents.Root;

            while (node)
            {
                if (circleEvent.y < node.y || (circleEvent.y == node.y && circleEvent.x <= node.x))
                {
                    if (node.Left)
                    {
                        node = node.Left;
                    }
                    else
                    {
                        predecessor = node.Prev;
                        break;
                    }
                }
                else
                {
                    if (node.Right)
                    {
                        node = node.Right;
                    }
                    else
                    {
                        predecessor = node;
                        break;
                    }
                }
            }

            this.circleEvents.Insert(predecessor, circleEvent);
            if (!predecessor)
            {
                this.firstCircleEvent = circleEvent;
            }
        }

        public void DetachCircleEvent(BeachSection arc)
        {
            CircleEvent circle = arc.circleEvent;

            if (circle)
            {
                if (!circle.Prev)
                {
                    this.firstCircleEvent = circle.Next;
                }

                this.circleEvents.Remove(circle); // remove from RB-tree
                this.circleEventJunkyard.Add(circle);
                arc.circleEvent = null;
            }
        }

        // ---------------------------------------------------------------------------
        // Diagram completion methods

        // connect dangling edges (not if a cursory test tells us
        // it is not going to be visible.
        // return value:
        //   false: the dangling va couldn't be connected
        //   true: the dangling va could be connected
        public bool ConnectEdge(Edge edge, Bounds bbox)
        {
            // skip if end point already connected
            Point vb = edge.vb;
            if (!!vb)
            {
                return true;
            }

            // make local copy for performance purpose
            Point va = edge.va;
            float xl = bbox.min.x;
            float xr = bbox.max.x;
            float yt = bbox.min.z;
            float yb = bbox.max.z;
            Point lSite = edge.lSite;
            Point rSite = edge.rSite;
            float lx = lSite.x;
            float ly = lSite.y;
            float rx = rSite.x;
            float ry = rSite.y;
            float fx = (lx + rx) / 2;
            float fy = (ly + ry) / 2;
            float fm = float.NaN;
            float fb = 0.0f;

            // if we reach here, this means cells which use this edge will need
            // to be closed, whether because the edge was removed, or because it
            // was connected to the bounding box.
            this.cells[lSite.id].closeMe = true;
            this.cells[rSite.id].closeMe = true;

            // get the line equation of the bisector if line is not vertical
            if (ry != ly)
            {
                fm = (lx - rx) / (ry - ly);
                fb = fy - fm * fx;
            }

            // remember, direction of line (relative to left site):
            // upward: left.x < right.x
            // downward: left.x > right.x
            // horizontal: left.x == right.x
            // upward: left.x < right.x
            // rightward: left.y < right.y
            // leftward: left.y > right.y
            // vertical: left.y == right.y

            // depending on the direction, find the best side of the
            // bounding box to use to determine a reasonable start point

            // rhill 2013-12-02:
            // While at it, since we have the values which define the line,
            // clip the end of va if it is outside the bbox.
            // https://github.com/gorhill/Javascript-Voronoi/issues/15
            // TODO: Do all the clipping here rather than rely on Liang-Barsky
            // which does not do well sometimes due to loss of arithmetic
            // precision. The code here doesn't degrade if one of the vertex is
            // at a huge distance.

            // special case: vertical line
            if (float.IsNaN(fm))
            {
                // doesn't intersect with viewport
                if (fx < xl || fx >= xr)
                {
                    return false;
                }

                // downward
                if (lx > rx)
                {
                    if (!va || va.y < yt)
                    {
                        //Debug.Log(yt);
                        va = new Point(fx, yt);
                    }
                    else if (va.y >= yb)
                    {
                        //Debug.Log(yb);
                        return false;
                    }

                    vb = new Point(fx, yb);
                }
                // upward
                else
                {
                    if (!va || va.y > yb)
                    {
                        //Debug.Log(yb);
                        va = new Point(fx, yb);
                    }
                    else if (va.y < yt)
                    {
                        //Debug.Log(yt);
                        return false;
                    }

                    vb = new Point(fx, yt);
                }
            }
            // closer to vertical than horizontal, connect start point to the
            // top or bottom side of the bounding box
            else if (fm < -1 || fm > 1)
            {
                // downward
                if (lx > rx)
                {
                    if (!va || va.y < yt)
                    {
                        //Debug.Log(va.y + " yt: " + yt);
                        va = new Point((yt - fb) / fm, yt);
                    }
                    else if (va.y >= yb)
                    {
                        //Debug.Log(va.y + " yb: " + yb);
                        return false;
                    }

                    vb = new Point((yb - fb) / fm, yb);
                }
                // upward
                else
                {
                    if (!va || va.y > yb)
                    {
                        //Debug.Log(va.y + " yb: " + yb);
                        va = new Point((yb - fb) / fm, yb);
                    }
                    else if (va.y < yt)
                    {
                        //Debug.Log(va.y + " yt: " + yt);
                        return false;
                    }

                    vb = new Point((yt - fb) / fm, yt);
                }
            }
            // closer to horizontal than vertical, connect start point to the
            // left or right side of the bounding box
            else
            {
                // rightward
                if (ly < ry)
                {
                    if (!va || va.x < xl)
                    {
                        //Debug.Log(va.x + " xl: " + xl);
                        va = new Point(xl, fm * xl + fb);
                    }
                    else if (va.x >= xr)
                    {
                        //Debug.Log(va.x + " xr: " + xr);
                        return false;
                    }

                    vb = new Point(xr, fm * xr + fb);
                }
                // leftward
                else
                {
                    if (!va || va.x > xr)
                    {
                        //Debug.Log(va.x + " xr: " + xr);
                        va = new Point(xr, fm * xr + fb);
                    }
                    else if (va.x < xl)
                    {
                        //Debug.Log(va.x + " xl: " + xl);
                        return false;
                    }

                    vb = new Point(xl, fm * xl + fb);
                }
            }

            edge.va = va;
            edge.vb = vb;

            return true;
        }

        // line-clipping code taken from:
        //   Liang-Barsky function by Daniel White
        //   http://www.skytopia.com/project/articles/compsci/clipping.html
        // Thanks!
        // A bit modified to minimize code paths
        public bool ClipEdge(Edge edge, Bounds bbox)
        {
            float ax = edge.va.x;
            float ay = edge.va.y;
            float bx = edge.vb != null ? edge.vb.x : float.NaN;
            float by = edge.vb != null ? edge.vb.y : float.NaN;
            float t0 = 0;
            float t1 = 1;
            float dx = bx - ax;
            float dy = by - ay;

            // left
            float q = ax - bbox.min.x;
            if (dx == 0 && q < 0) { return false; }
            float r = -q / dx;
            if (dx < 0)
            {
                if (r < t0) { return false; }
                if (r < t1) { t1 = r; }
            }
            else if (dx > 0)
            {
                if (r > t1) { return false; }
                if (r > t0) { t0 = r; }
            }
            // right
            q = bbox.max.x - ax;
            if (dx == 0 && q < 0) { return false; }
            r = q / dx;
            if (dx < 0)
            {
                if (r > t1) { return false; }
                if (r > t0) { t0 = r; }
            }
            else if (dx > 0)
            {
                if (r < t0) { return false; }
                if (r < t1) { t1 = r; }
            }
            // top
            q = ay - bbox.min.z;
            if (dy == 0 && q < 0) { return false; }
            r = -q / dy;
            if (dy < 0)
            {
                if (r < t0) { return false; }
                if (r < t1) { t1 = r; }
            }
            else if (dy > 0)
            {
                if (r > t1) { return false; }
                if (r > t0) { t0 = r; }
            }
            // bottom        
            q = bbox.max.z - ay;
            if (dy == 0 && q < 0) { return false; }
            r = q / dy;
            if (dy < 0)
            {
                if (r > t1) { return false; }
                if (r > t0) { t0 = r; }
            }
            else if (dy > 0)
            {
                if (r < t0) { return false; }
                if (r < t1) { t1 = r; }
            }

            // if we reach this point, Voronoi edge is within bbox

            // if t0 > 0, va needs to change
            // rhill 2011-06-03: we need to create a new vertex rather
            // than modifying the existing one, since the existing
            // one is likely shared with at least another edge
            if (t0 > 0)
            {
                edge.va = new Point(ax + t0 * dx, ay + t0 * dy);
            }

            // if t1 < 1, vb needs to change
            // rhill 2011-06-03: we need to create a new vertex rather
            // than modifying the existing one, since the existing
            // one is likely shared with at least another edge
            if (t1 < 1)
            {
                edge.vb = new Point(ax + t1 * dx, ay + t1 * dy);
            }

            // va and/or vb were clipped, thus we will need to close
            // cells which use this edge.
            if (t0 > 0 || t1 < 1)
            {
                this.cells[edge.lSite.id].closeMe = true;
                this.cells[edge.rSite.id].closeMe = true;
            }

            return true;
        }

        // Connect/cut edges at bounding box
        public void ClipEdges(Bounds bbox)
        {
            // connect all dangling edges to bounding box
            // or get rid of them if it can't be done

            // iterate backward so we can splice safely
            //while (iEdge--) {
            for (int iEdge = this.edges.Count - 1; iEdge >= 0; iEdge--)
            {
                Edge edge = this.edges[iEdge];
                // edge is removed if:
                //   it is wholly outside the bounding box
                //   it is actually a point rather than a line
                if (!this.ConnectEdge(edge, bbox) ||
                    !this.ClipEdge(edge, bbox) ||
                    (Mathf.Abs(edge.va.x - edge.vb.x) < EPSILON && Mathf.Abs(edge.va.y - edge.vb.y) < EPSILON))
                {
                    edge.va = edge.vb = null;
                    //array_splice(this.edges, iEdge,1);
                    //this.edges.RemoveAt(iEdge);
                    this.edges.Remove(edge);
                }
            }
        }

        // Close the cells.
        // The cells are bound by the supplied bounding box.
        // Each cell refers to its associated site, and a list
        // of halfedges ordered counterclockwise.
        public void CloseCells(Bounds bbox)
        {
            // prune, order halfedges, then add missing ones
            // required to close cells
            float xl = bbox.min.x;
            float xr = bbox.max.x;
            float yt = bbox.min.z;
            float yb = bbox.max.z;

            int badIterations = 0;

            for (int iCell = this.cells.Count - 1; iCell >= 0; iCell--)
            {
                Cell cell = this.cells[iCell];

                // prune, order halfedges counterclockwise, then add missing ones
                // required to close cells
                if (cell.Prepare() <= 0)
                {
                    continue;
                }
                if (!cell.closeMe)
                {
                    continue;
                }

                // close open cells
                // step 1: find first 'unclosed' point, if any.
                // an 'unclosed' point will be the end point of a halfedge which
                // does not match the start point of the following halfedge
                int nHalfedges = cell.halfEdges.Count;

                // special case: only one site, in which case, the viewport is the cell
                // ...
                // all other cases
                int iLeft = 0;
                int iter = 0;

                while (iLeft < nHalfedges)// && iter < 10) 
                {
                    iter++;
                    /*int iRight = (iLeft+1) % nHalfedges;
                    Point va = cell.halfEdges[iLeft].Getva();
                    Point vz = cell.halfEdges[iRight].Getvz();*/

                    Point va = cell.halfEdges[iLeft].GetEndPoint();
                    Point vz = cell.halfEdges[(iLeft + 1) % nHalfedges].GetStartPoint();

                    // if end point is not equal to start point, we need to add the missing
                    // halfedge(s) to close the cell
                    if ((Mathf.Abs(va.x - vz.x) >= EPSILON || Mathf.Abs(va.y - vz.y) >= EPSILON))
                    {
                        // rhill 2013-12-02:
                        // "Holes" in the halfedges are not necessarily always adjacent.
                        // https://github.com/gorhill/Javascript-Voronoi/issues/16

                        bool lastBorderSegment = false;
                        Point vb;
                        Edge edge;

                        // walk downward along left side
                        if (equalWithEpsilon(va.x, xl) && lessThanWithEpsilon(va.y, yb))
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.x, xl);
                            vb = new Point(xl, lastBorderSegment ? vz.y : yb);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                            if (!lastBorderSegment)
                                va = vb;
                        }
                        // walk rightward along bottom side
                        if (!lastBorderSegment && this.equalWithEpsilon(va.y, yb) && this.lessThanWithEpsilon(va.x, xr))
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.y, yb);
                            vb = new Point(lastBorderSegment ? vz.x : xr, yb);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                            if (!lastBorderSegment)
                                va = vb;
                        }
                        // walk upward along right side
                        if (!lastBorderSegment && this.equalWithEpsilon(va.x, xr) && this.greaterThanWithEpsilon(va.y, yt))
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.x, xr);
                            vb = new Point(xr, lastBorderSegment ? vz.y : yt);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                            if (!lastBorderSegment)
                                va = vb;
                        }
                        // walk leftward along top side
                        if (!lastBorderSegment && this.equalWithEpsilon(va.y, yt) && this.greaterThanWithEpsilon(va.x, xl))
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.y, yt);
                            vb = new Point(lastBorderSegment ? vz.x : xl, yt);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                            if (!lastBorderSegment)
                                va = vb;
                        }

                        // walk downward along left side
                        if (!lastBorderSegment)
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.x, xl);
                            vb = new Point(xl, lastBorderSegment ? vz.y : yb);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                            if (!lastBorderSegment)
                                va = vb;
                        }

                        // walk rightward along bottom side
                        if (!lastBorderSegment)
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.y, yb);
                            vb = new Point(lastBorderSegment ? vz.x : xr, yb);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                            if (!lastBorderSegment)
                                va = vb;
                        }

                        // walk upward along right side
                        if (!lastBorderSegment)
                        {
                            lastBorderSegment = this.equalWithEpsilon(vz.x, xr);
                            vb = new Point(xr, lastBorderSegment ? vz.y : yt);
                            edge = this.CreateBorderEdge(cell.site, va, vb);
                            iLeft++;
                            //halfedges.splice(iLeft, 0, this.createHalfedge(edge, cell.site, null));
                            cell.halfEdges.Insert(iLeft, new HalfEdge(edge, cell.site, null));
                            nHalfedges++;
                        }

                        if (!lastBorderSegment)
                        {
                            Debug.LogError("This makes no sense");
                            badIterations++;
                        }
                    }
                    iLeft++;
                }
                cell.closeMe = false;
            }
            if (badIterations > 0)
            {
                this.valid = false;
            }
            else
            {
                this.valid = true;
            }
        }

        public bool equalWithEpsilon(float a, float b) { return Mathf.Abs(a - b) < EPSILON; }
        public bool greaterThanWithEpsilon(float a, float b) { return a - b > EPSILON; }
        public bool greaterThanOrEqualWithEpsilon(float a, float b) { return b - a < EPSILON; }
        public bool lessThanWithEpsilon(float a, float b) { return b - a > EPSILON; }
        public bool lessThanOrEqualWithEpsilon(float a, float b) { return a - b < EPSILON; }

    }
}