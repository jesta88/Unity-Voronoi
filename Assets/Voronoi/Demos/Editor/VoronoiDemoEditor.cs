using UnityEngine;
using UnityEditor;
using System.Collections;

using Voronoi;

[CustomEditor(typeof(VoronoiDemo))] 
public class VoronoiDemoEditor : Editor 
{
	void OnSceneGUI()
	{
		VoronoiDemo voronoi = (VoronoiDemo) target;
		if (voronoi.graph)
        {
            foreach (Voronoi.Cell cell in voronoi.graph.cells)
            {
				Handles.color = Color.black;
                Handles.Label(cell.site.ToVector3(), "Site " + cell.site.id);

                if (cell.halfEdges.Count > 0)
                {
					int i = 0;
                    foreach (HalfEdge halfEdge in cell.halfEdges)
                    {
						if (halfEdge.edge.va && halfEdge.edge.vb)
                        {
							if (cell.site.id == 1)
							{
								Vector3 posA = halfEdge.edge.va.ToVector3();
								Vector3 posB = halfEdge.edge.vb.ToVector3();
								Handles.Label(posA + Vector3.up * i * 10, "Cell " + cell.site.id + " Edge " + i + " Vertex A " + posA);
								Handles.Label(posB + Vector3.up * i * 10, "Cell " + cell.site.id + " Edge " + i + " Vertex B " + posB);
								i++;
							}
						}
					}
				}
			}
		}
	}
}
