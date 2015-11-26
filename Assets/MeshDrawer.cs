using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class MeshDrawer : MonoBehaviour {
	public Mesh m;

	public Vector3 MinBounds;
	public Vector3 MaxBounds;
	public float tolerence = 0.0001f;
	private int vertcount;
	public bool recompute=false;
	public bool removeinterioredges=true;
	public Vector3 rotation = Vector3.zero;

	Vector3[] GenPoints(out int[] pathindices) {
		Profiler.BeginSample ("init");
		var tol = m.bounds.extents.magnitude*tolerence;
		var tris = m.triangles;
		List<Vector3> tri_normals = new List<Vector3> (tris.Length/3);
		List<int> edges = new List<int> (tris.Length*2);
		List<Vector3> edge_normals = new List<Vector3> (tris.Length);
		var verts = m.vertices;
		var normals = m.normals;
		List<int>[] connectedto = new List<int>[verts.Length];
		for (var i=0; i<connectedto.Length; i++) {
			connectedto[i]=new List<int>();
				}
		vertcount = verts.Length;
		int ea;
		int eb;

		for (var i=0; i<tris.Length; i+=3) {
			var a = tris[i];
			var b = tris[i+1];
			var c = tris[i+2];
			var n = (normals[a]+normals[b]+normals[c]).normalized;
			tri_normals.Add(n);
		}
		//Debug.Log ("vertex count " + verts.Length);
		for (var i=0; i<verts.Length; i++) {
			var v_i = verts[i];
			for (var j=0;j<i;j++) {
				var v_j=verts[j];
				if (Vector3.Distance(v_i,v_j)<tol) {
					//Debug.Log ("replacing vert " +i+" with vert " + j);
					for (var k=0;k<tris.Length;k++) {
						if (tris[k]==i) {
							tris[k]=j;
						}
					}
					break;
				}
			}
		}

		List<int> edgeswithdups=new List<int>();
		List<int> edgeswithdups_normals=new List<int>();

		for (var i=0; i<tris.Length; i+=3) {
			int x = tris[i];
			int y = tris[i+1];
			int z = tris[i+2];
			var n = tri_normals[i/3];
			//Debug.Log ("tri " + x+","+y+","+z);
			
			int e1a = x<y?x:y;
			int e1b = x<y?y:x;
			int e2a = y<z?y:z;
			int e2b = y<z?z:y;
			int e3a = x<z?x:z;
			int e3b = x<z?z:x;

			bool found1=false;
			bool found2=false;
			bool found3=false;
			if (x==y) {
				found1=true;
			}
			if (y==z){
				found2=true;
			}
			if (x==z) {
				found3=true;
			}
			for (int j=0;j<edges.Count;j+=2) {
				ea = edges[j];
				eb = edges[j+1];
				var en = edge_normals[j/2];

				if (ea==e1a&&eb==e1b) {
					found1=true;
					edgeswithdups.Add(ea);
					edgeswithdups.Add(eb);

					if(Vector3.Angle(n,en)<1.0f) {
						edgeswithdups_normals.Add(ea);
						edgeswithdups_normals.Add(eb);
					}
				}
				if (ea==e2a&&eb==e2b) {
					found2=true;
					edgeswithdups.Add(ea);
					edgeswithdups.Add(eb);
					
					if(Vector3.Angle(n,en)<1.0f) {
						edgeswithdups_normals.Add(ea);
						edgeswithdups_normals.Add(eb);
					}
				}
				if (ea==e3a&&eb==e3b) {
					found3=true;
					edgeswithdups.Add(ea);
					edgeswithdups.Add(eb);
					
					if(Vector3.Angle(n,en)<1.0f) {
						edgeswithdups_normals.Add(ea);
						edgeswithdups_normals.Add(eb);
					}
				}
			}
			if (!found1) {
				edges.Add(e1a);
				edges.Add(e1b);
				edge_normals.Add(n);
				connectedto[e1a].Add(e1b);
				connectedto[e1b].Add(e1a);
			}
			if (!found2) {
				edges.Add(e2a);
				edges.Add(e2b);
				edge_normals.Add(n);
				connectedto[e2a].Add(e2b);
				connectedto[e2b].Add(e2a);
			}
			if (!found3) {
				edges.Add(e3a);
				edges.Add(e3b);
				edge_normals.Add(n);
				connectedto[e3a].Add(e3b);
				connectedto[e3b].Add(e3a);
			}
		}

		if (removeinterioredges) {
			edgeswithdups=edgeswithdups_normals;
			int oldedgecount = edges.Count;
			for (var i=0;i<edgeswithdups.Count;i+=2){
				ea = edgeswithdups[i];
				eb = edgeswithdups[i+1];
				for (var j=edges.Count-2;j>=0;j-=2) {
					var e2a = edges[j];
					var e2b = edges[j+1];
					if (e2a==ea && e2b==eb) {
						edges.RemoveRange(j,2);
						connectedto[ea].Remove(eb);
						connectedto[eb].Remove(ea);
					}
				}
			}
			int newedgecount = edges.Count;
			Debug.Log((oldedgecount-newedgecount)+" of " + oldedgecount+" edges removed.");
		}
		if (edges.Count == 0) {
			Debug.Log("removed all edges in preprocessing. eep.");
			pathindices = new int[0];
			return new Vector3[0];
		}

		List<int> unvisitededges = new List<int> (edges);
		List<int> visitededges = new List<int> (edges.Count);
		List<int> path = new List<int> ();
		ea = unvisitededges [0];
		eb = unvisitededges [1];
		path.Add (ea);
		path.Add (eb);
		unvisitededges.RemoveRange(0,2);
		visitededges.Add (ea);
		visitededges.Add (eb);
		connectedto [ea].Remove (eb);
		connectedto [eb].Remove (ea);

		Profiler.EndSample ();
		int lastskip = -1;
		//visitededges.Add (ea);
		bool anychanges = true;
		while (unvisitededges.Count>0&&anychanges) {
			Profiler.BeginSample("A");
			anychanges=false;
			var currenthead = path[path.Count-1];
			int vfounda=0;
			int vfoundb=0;
			bool found=false;

			float minangle=360;
			int minindex=-1;
			Vector3 headvert = verts[currenthead];
			Vector3 vec = headvert-verts[path[path.Count-2]];

			for (int i=0;i<unvisitededges.Count;i+=2) {

				var edgea = unvisitededges[i];
				var edgeb = unvisitededges[i+1];
				Vector3 vec2;
				if (edgea==currenthead) {
					vec2 = verts[edgeb]-headvert;
				} else if (edgeb==currenthead) {
					vec2 = verts[edgea]-headvert;
				} else {
					continue;
				}

				float a = Vector3.Angle(vec,vec2);
				if (a<minangle) {
					minangle=a;
					minindex=i;
				}
				if (a<80) {
					break;
				}

			}
			//visit connected edge that makes smallest angle
			if (minindex>-1) {
				var edgea = unvisitededges[minindex];
				var edgeb = unvisitededges[minindex+1];
				if (edgea==currenthead || edgeb==currenthead) {
					vfounda = edgea;
					vfoundb = edgeb;
					found=true;
					unvisitededges.RemoveRange(minindex,2);
					visitededges.Add (edgea);
					visitededges.Add (edgeb);					
					connectedto [edgea].Remove (edgeb);
					connectedto [edgeb].Remove (edgea);

					if (edgea==currenthead) {
						path.Add(edgeb);
					} else {
						path.Add(edgea);
					}
					anychanges=true;

				}
			} else {
				//otherwise, work backwards in the path until you find a vertex with outgoing edges
				Profiler.BeginSample("C");

				//work backwards until you find a good one
				int targetindex=-1;
				for (int i=path.Count-1;i>=0;i--) {
					if (connectedto[path[i]].Count>0) {
						targetindex=i;
						break;
					}
				}
                    
                if (targetindex>-1){
	                for (var i=path.Count-2;i>=targetindex;i--) {
						var v = path[i];
						path.Add(v);
					}
					anychanges=true;
				}
				Profiler.EndSample();
			}
			//if no outgoin edges on current path, skip to distant one
			if (anychanges==false && unvisitededges.Count>0) {	
				Profiler.BeginSample("D");
				//find nearest visited point to current head
				var h = verts[currenthead];
				float mindist=1000000000.0f;
				var minindex_from=-1;
				var minindex_to=-1;
				for (int j=0;j<unvisitededges.Count;j+=2) {
					ea = unvisitededges[j];
					for (int k=0;k<visitededges.Count;k+=2) {
						eb = visitededges[k];						
						var d1 = Vector3.Distance(verts[ea],verts[eb]);
						if (d1<mindist) {
							minindex_to=ea;
							minindex_from=eb;
                            mindist=d1;
                        }
					}
				}

				int i=path.Count-1;
				for (;;i--) {
					var v = path[i];
					path.Add(v);
					if (minindex_from==v) {
						path.Add(minindex_to);
						break;
					}

				}

				Debug.Log ("trail length "+(path.Count-i));

				anychanges=true;
				Profiler.EndSample();
			}
			Debug.Log (visitededges.Count+"/"+(unvisitededges.Count+visitededges.Count));
            if (path.Count>10000) {
				break;
			}
        }

		/*var s = "";
		foreach (var i in path) {
			s+=i+",";
		}
		Debug.Log ("path = " + s);
		*/			
		pathindices = path.ToArray ();
		var result = new Vector3[path.Count];
		for (var i=0; i<path.Count; i++) {
			var v_index=path[i];
			result[i]=verts[v_index];
		}
		return result;
	}

	private void scalePoints(Vector3[] points) {
		Quaternion rot = Quaternion.Euler (rotation);
		float x_min = points [0].x;
		float x_max = points [0].x;
		float y_min = points [0].y;
		float y_max = points [0].y;
		float z_min = points [0].z;
		float z_max = points [0].z;
		for (int i=0; i<points.Length; i++) {
			points[i] = rot*points[i];
			var v = points[i];
			if (v.x<x_min) {
				x_min=v.x;
			} else if (v.x>x_max) {
				x_max=v.x;
			}
			if (v.y<y_min) {
				y_min=v.y;
			} else if (v.y>y_max) {
				y_max=v.y;
			}
			if (v.z<z_min) {
				z_min=v.z;
			} else if (v.z>z_max) {
				z_max=v.z;
			}
		}

		Vector3 source_origin = new Vector3 (x_min, y_min, z_min);
		Vector3 source_extents = new Vector3 (x_max - x_min, y_max - y_min, z_max - z_min);

		Vector3 target_origin = MinBounds;
		Vector3 target_extents = MaxBounds - MinBounds;

		Vector3 dilation_v = new Vector3 (target_extents.x / source_extents.x, target_extents.y / source_extents.y, target_extents.z / source_extents.z);
		float dilation = Mathf.Min (dilation_v.x, dilation_v.y, dilation_v.z);

		Vector3 dilated_bounds = dilation*source_extents;
		Vector3 centering_offset = (target_extents - dilated_bounds) / 2;
		for (var i=0; i<points.Length; i++) {
			points[i] =centering_offset+ (points[i]-source_origin)*dilation+target_origin;
		}
	}

	private Vector3[] points;
	private int[] pathindices;
	void Compute() {		
		points = GenPoints (out pathindices);
		scalePoints (points);
		bbox = boundBox ();
		bboxpoints = boundBoxPoints ();
		SetWeights ();
    }
    // Use this for initialization
	void Start () {
		recompute = false;
		Compute ();
	}
	Vector3[] bbox;
	Vector3[] bboxpoints;

	private Vector3[] boundBox() {
		var o = MinBounds;
		var p = MaxBounds;
		var dx = new Vector3 (p.x - o.x, 0, 0);
		var dy = new Vector3 (0,p.y-o.y, 0);
		var dz = new Vector3 (0, 0, p.z-o.z);

		return new Vector3[] {
			o,o+dx,o+dx+dy,o+dy,o,//back
			dz+o,dz+o+dx,dz+o+dx+dy,dz+o+dy,dz+o,//front
			dz+o+dy, o+dy,//left
			o+dy+dx,o+dy+dx+dz,o+dx+dz,o+dx//right
		};
	}
	
	private Vector3[] boundBoxPoints() {
		var o = MinBounds;
		var p = MaxBounds;
		var dx = new Vector3 (p.x - o.x, 0, 0);
		var dy = new Vector3 (0,p.y-o.y, 0);
		var dz = new Vector3 (0, 0, p.z-o.z);
		
		return new Vector3[] {
			o,o+dx,o+dx+dy,o+dx+dy+dz,
			o+dy,o+dy+dz,o+dz,o+dx+dz
        };
    }

	int[] weights;
	int maxweight;

	void SetWeights() {
		maxweight = 0;
		weights = new int[vertcount*vertcount];
		for (int i=0;i<pathindices.Length-1;i++) {
			int ea = pathindices[i];
			int eb = pathindices[i+1];
			weights[ea+vertcount*eb]++;
			weights[eb+vertcount*ea]++;
			if (weights[ea+vertcount*eb]>maxweight) {
				maxweight=weights[ea+vertcount*eb];
			}

		}
	}
	Vector3[] boundpoints;


	void DrawEverything() {
		for (var i=0; i<points.Length-1; i++) {
			//float a=(float)i/(float)points.Length;
			int w = weights[pathindices[i]+vertcount*pathindices[i+1]];
			float weight = (float)w/(float)maxweight;
			float r = Mathf.Clamp(w-1,0,5)/5.0f;
			Color c = Color.yellow*(1-weight)+Color.red*weight;
			if (weight==0) {
				c = Color.black;
			}
			UnityEngine.Random.seed=i.GetHashCode();
			Debug.DrawLine(points[i]+UnityEngine.Random.onUnitSphere*r,points[i+1]+UnityEngine.Random.onUnitSphere*r,c);
		}
		for (var i=0; i<bbox.Length-1; i++) {
			Debug.DrawLine(bbox[i],bbox[i+1],Color.blue);
        }
	}


	// Update is called once per frame
	void Update () {
		DrawEverything ();
		if (Input.GetKeyDown (KeyCode.P)) {
			Compute();
			Debug.Log (printPath());
			Debug.Log ("bounding box:");
			Debug.Log(print (bboxpoints));
			Debug.Log ("shape:");
			Debug.Log (print (points));
		}
		if (recompute) {
			recompute=false;
			Compute();
		}
	}

	string printPath() {

		var s = "path length " + pathindices.Length+"\n";
		for (int i=0;i<pathindices.Length;i++) {
			s+=","+pathindices[i];
		}
		return s;
	}
	public int subdivisions=10;
	string print(Vector3[] ar) {
		var result = "G21\n";
		for (var i=0; i<ar.Length; i++) {
			var v= ar[i];
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
			if (i<ar.Length-1) {
				var next = ar[i+1];
				for (var j=1;j<(subdivisions-1);j++) {
					var v2 = v*(float)(subdivisions-j)/(float)(subdivisions)+next*(float)(j)/(float)(subdivisions);
					result = result + "G1 X"+v2.x.ToString ("0.00")+" Y"+v2.y.ToString ("0.00")+" Z"+v2.z.ToString ("0.00")+"\n";
				}
			}
		}
		return result;
	}
}
