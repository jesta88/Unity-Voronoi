using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Voronoi
{
    public class Triangle : Surface
    {
        public Point p1;
        public Point p2;
        public Point p3;

        public Triangle(Point p1, Point p2, Point p3)
        {
            this.AddPoints(new Point[] { p1, p2, p3 });

            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        public bool IsValid()
        {
            float epsilon = 1e-15f;

            return !(Mathf.Abs(this.p1.y - this.p2.y) < epsilon && Mathf.Abs(this.p2.y - this.p3.y) < epsilon);
        }

        public bool PointInCircle(Point point)
        {
            if (!this.IsValid())
            {
                return false;
            }

            float epsilon = 1e-15f;
            float xc, yc;

            if (Mathf.Abs(this.p2.y - this.p1.y) < epsilon)
            {
                float m2 = -(this.p3.x - this.p2.x) / (this.p3.y - this.p2.y);
                float mx2 = (this.p2.x + this.p3.x) * 0.5f;
                float my2 = (this.p2.y + this.p3.y) * 0.5f;

                //Calculate CircumCircle center (xc,yc)
                xc = (this.p2.x + this.p1.x) * 0.5f;
                yc = m2 * (xc - mx2) + my2;
            }
            else if (Mathf.Abs(this.p3.y - this.p2.y) < epsilon)
            {
                float m1 = -(this.p2.x - this.p1.x) / (this.p2.y - this.p1.y);
                float mx1 = (this.p1.x + this.p2.x) * 0.5f;
                float my1 = (this.p1.y + this.p2.y) * 0.5f;
                xc = (this.p3.x + this.p2.x) * 0.5f;
                yc = m1 * (xc - mx1) + my1;
            }
            else
            {
                float m1 = -(this.p2.x - this.p1.x) / (this.p2.y - this.p1.y);
                float m2 = -(this.p3.x - this.p2.x) / (this.p3.y - this.p2.y);
                float mx1 = (this.p1.x + this.p2.x) * 0.5f;
                float mx2 = (this.p2.x + this.p3.x) * 0.5f;
                float my1 = (this.p1.y + this.p2.y) * 0.5f;
                float my2 = (this.p2.y + this.p3.y) * 0.5f;
                xc = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2);
                yc = m1 * (xc - mx1) + my1;
            }

            float dx = this.p2.x - xc;
            float dy = this.p2.y - yc;
            float rsqr = dx * dx + dy * dy;

            //double r = Math.Sqrt(rsqr); //Circumcircle radius
            dx = point.x - xc;
            dy = point.y - yc;
            float drsqr = dx * dx + dy * dy;

            return (drsqr <= rsqr);
        }

        public bool PointIn(Point point)
        {
            return true;
        }
    }
}