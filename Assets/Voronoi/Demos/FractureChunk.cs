using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Voronoi;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class FractureChunk : MonoBehaviour 
{
	public Material material;
	
	private MeshFilter meshFilter;
	public Mesh mesh;
	
	public void CreateMesh(Cell cell)
	{
		int k = 0;
		foreach(HalfEdge hedge in cell.halfEdges)
		{
			Edge edge = hedge.edge;
			//Debug.Log("Cell " + cell.site.id + " HalfEdge " + k + " Vertex: " + edge.va.ToVector3());
			k++;
		}
		
        if (cell.halfEdges.Count > 0)
        {
			GetComponent<MeshFilter>().sharedMesh = mesh = new Mesh();
			mesh.name = "Chunk " + cell.site.id;
			
			Vector3[] vertices = new Vector3[cell.halfEdges.Count + 1];
			int[] triangles = new int[(cell.halfEdges.Count + 0) * 3];
			
			vertices[0] = cell.site.ToVector3() - transform.position;
			triangles[0] = 0;
			for (int v = 1, t = 1; v < vertices.Length; v++, t += 3)
			{
				Edge edge = cell.halfEdges[v-1].edge;

				vertices[v] = edge.va.ToVector3() - transform.position;
				triangles[t] = v;
				triangles[t + 1] = v + 1;
			}
			triangles[triangles.Length - 1] = 1;
			
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.RecalculateBounds();
			
			renderer.sharedMaterial = material;
		}
	}
}