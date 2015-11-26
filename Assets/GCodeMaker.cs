using UnityEngine;
using System.Collections.Generic;
using System;
[ExecuteInEditMode]
public class GCodeMaker : MonoBehaviour {

	public Vector3 offset = new Vector3 (500, 0, 0);
	public float radiuslow=600;
	public float radiushigh=10;
	public float low=-800;
	public float high=800;
	public float turncount=6;
	public float subdivisions=100;

	public Vector3[] validpoints;

    static Vector3 spiral(Vector3 offset, float radiuslow, float radiushigh, float low, float high, float turncount, float t) {
		float smallrad = 0;//100
		float a = 2*Mathf.PI*turncount*t;
		float radius = radiuslow + (radiushigh - radiuslow) * t;
		return -offset + new Vector3 (Mathf.Cos (a)*radius+Mathf.Cos (10*a)*smallrad, Mathf.Sin (a)*radius+Mathf.Sin (10*a)*smallrad, low + (high - low) * t);
    }
	/*
	static Vector3 spiral(Vector3 offset, float radiuslow, float radiushigh, float low, float high, float turncount, float t) {
		float a = 2*Mathf.PI*t;
		var c = (Mathf.Cos (2 * Mathf.PI * turncount * t) + 1) / 2;
		float radius = radiuslow + (radiushigh - radiuslow) * c;
		float height = low + (high - low) * c;
		return -offset + new Vector3 (Mathf.Cos (a)*radius, Mathf.Sin (a)*radius, height);
	}*/

	Vector3[] Gen() {
		List<Vector3> result = new List<Vector3> ();
		for (int i=0; i<(subdivisions+1); i++) {
			float t = (float)i/(float)subdivisions;
			result.Add(spiral (offset,radiuslow,radiushigh,low,high,turncount,t));
		}
		return result.ToArray ();
	}
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		var ar = Gen ();
		for (int i=0; i<ar.Length-1; i++) {
			Debug.DrawLine (ar [i], ar [i + 1]);
		}
		Debug.DrawLine (new Vector3 (-500, 0, 0), new Vector3 (-500, 0, -500),Color.yellow);
		Debug.DrawLine (new Vector3 (-500, 0, 0), new Vector3 (296, 0, 0), Color.blue);
		if (Input.GetKeyDown (KeyCode.P)) {
			
			Debug.Log (print (ar));
		}

		for (int i=0;i<validpoints.Length-1;i++) {
			var p1 = validpoints[i]-offset;
			var p2 = validpoints[i+1]-offset;

			Debug.DrawLine(p1,p2,Color.red);
		}
	}

	string print(Vector3[] ar) {
		var result = "G21\n";
		for (var i=0; i<ar.Length; i++) {
			var v= ar[i];
			result = result + "G1 X"+v.x.ToString ("0.00")+" Y"+v.y.ToString ("0.00")+" Z"+v.z.ToString ("0.00")+"\n";
		}
		result = result + "G1 X0 Y0 Z0\n";
		return result;
	}
}
