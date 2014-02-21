using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Voronoi;
using Cell = Voronoi.Cell;

public class VoronoiDemo : MonoBehaviour 
{
	public int numSites = 36;
	public Bounds bounds;
	
	private List<Point> sites;
	private FortuneVoronoi voronoi;
	private VoronoiGraph graph;
	
	void Start()
	{
		sites = new List<Point>();
		voronoi = new FortuneVoronoi();
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.G))
		{
            /*int i = 0;
            do
            {*/
                CreateSites(true, false);
            /*    i++;
            }
            while (!voronoi.valid || i < 20);*/
		}
		if(Input.GetKeyDown(KeyCode.R))
		{
			RelaxSites(1);
		}
	}
	
	void Compute(List<Point> sites)
	{
		this.sites = sites;
		this.graph = this.voronoi.Compute(sites, this.bounds);
	}
	
	void CreateSites(bool clear = true, bool relax = false, int relaxCount = 2)
	{
		List<Point> sites = new List<Point>();
		if(!clear)
		{
			sites = this.sites.Take(this.sites.Count).ToList();
		}
		
		// create vertices
		for(int i = 0; i < numSites; i++)
		{
			Point site = new Point(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.z, bounds.max.z), 0);
			sites.Add(site);
		}

        //QuantizeSites(sites);

		Compute (sites);
		
		if(relax)
		{
			RelaxSites(relaxCount);
		}
	}

    void QuantizeSites(List<Point> sites)
    {
        var e = FortuneVoronoi.EPSILON;
        Debug.Log(1 / e);
        for (int i = sites.Count - 1; i >= 0; i--)
        {
            Point site = sites[i];
            site.x = Mathf.Floor(site.x / e) * e;
            site.y = Mathf.Floor(site.y / e) * e;
        }
    }

    void RelaxSites(int iterations)
	{
		for(int i = 0; i < iterations; i++)
		{
			if(!this.graph)
			{
				return;
			}
			
			Point site;
			List<Point> sites = new List<Point>();
			float dist = 0;
			//bool again = false;
			
			float p = 1 / graph.cells.Count * 0.1f;
			
			for(int iCell = graph.cells.Count - 1; iCell >= 0; iCell--)
			{
				Voronoi.Cell cell = graph.cells[iCell];
				float rn = Random.value;
				
				// probability of apoptosis
				if (rn < p)
				{
					continue;
				}
				
				site = CellCentroid(cell);
				dist = Distance(site, cell.site);
				
				// don't relax too fast
				if(dist > 2)
				{
					site.x = (site.x + cell.site.x)/2;
					site.y = (site.y + cell.site.y)/2;
				}
				// probability of mytosis
				if (rn > (1-p))
				{
					dist /= 2;
					sites.Add(new Point(site.x+(site.x-cell.site.x)/dist, site.y+(site.y-cell.site.y)/dist));
				}
				sites.Add(site);
			}
			
			Compute (sites);
		}
	}
	
	float Distance(Point a, Point b)
	{
		float dx = a.x - b.x;
		float dy = a.y - b.y;
		return Mathf.Sqrt(dx*dx + dy*dy);
	}
	
	Point CellCentroid(Voronoi.Cell cell)
	{
		float x = 0f;
		float y = 0f;
		Point p1, p2;
		float v;
		
		for(int iHalfEdge = cell.halfEdges.Count - 1; iHalfEdge >= 0; iHalfEdge--)
		{
			HalfEdge halfEdge = cell.halfEdges[iHalfEdge];
			p1 = halfEdge.GetStartPoint();
			p2 = halfEdge.GetEndPoint();
			v = p1.x * p2.y - p2.x * p1.y;
			x += (p1.x+p2.x) * v;
			y += (p1.y+p2.y) * v;
		}
		v = CellArea(cell) * 6;
		return new Point(x/v, y/v);
	}
	
	float CellArea(Voronoi.Cell cell)
	{
		float area = 0.0f;
		Point p1, p2;
		
		for(int iHalfEdge = cell.halfEdges.Count - 1; iHalfEdge >= 0; iHalfEdge--)
		{
			HalfEdge halfEdge = cell.halfEdges[iHalfEdge];
			p1 = halfEdge.GetStartPoint();
			p2 = halfEdge.GetEndPoint();
			area += p1.x * p2.y;
			area -= p1.y * p2.x;
		}
		area /= 2;
		return area;
	}
	
	void OnDrawGizmos()
	{
		if(graph && voronoi.valid)
		{
			foreach(Voronoi.Cell cell in graph.cells)
			{
				List<Point> points = new List<Point>();
				
				Gizmos.color = Color.black;
				Gizmos.DrawCube(new Vector3(cell.site.x, 0, cell.site.y), Vector3.one);
				
				if (cell.halfEdges.Count > 0)
				{
					Point v = cell.halfEdges[0].GetStartPoint();
					if (v)
					{
						points.Add(v);
					}
				
					foreach(HalfEdge halfEdge in cell.halfEdges)
					{
						Edge edge = halfEdge.edge;
						
						if(edge.va && edge.vb)
						{
							Gizmos.color = Color.red;
							Gizmos.DrawLine(new Vector3(edge.va.x, 0, edge.va.y),
											new Vector3(edge.vb.x, 0, edge.vb.y));
						}
						
						v = halfEdge.GetEndPoint();
						if (v)
						{
							points.Add(v);
						}
					}
				}
			}
		}
	}
}