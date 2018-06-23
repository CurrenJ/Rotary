using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

public class Piece : MonoBehaviour {

    public float innerRadius = 3;
    public int innerSmoothness = 24;

    public float outerRadius = 4;
    public int outerSmoothness = 24;

    public int totalAngle = 90;
    public int offsetRotation = 45;

    Vector2 origin, center;

    PolygonCollider2D pc2;

    public bool needsUpdate;

    public Vector2[] getPoints(Vector2 off)
    {
        List<Vector2> points = new List<Vector2>();

        origin = new Vector2(0, 0);
        center = origin + off;

        float ang = offsetRotation;

        for (int i = 0; i <= innerSmoothness; i++)
        {

            float x = center.x + innerRadius * Mathf.Cos(ang * Mathf.Deg2Rad);
            float y = center.y + innerRadius * Mathf.Sin(ang * Mathf.Deg2Rad);

            points.Add(new Vector2(x, y));
            ang += (float)totalAngle / innerSmoothness;
        }

        ang = offsetRotation;

        for (int i = 0; i <= outerSmoothness; i++)
        {
            float x = center.x + outerRadius * Mathf.Cos(ang * Mathf.Deg2Rad);
            float y = center.y + outerRadius * Mathf.Sin(ang * Mathf.Deg2Rad);

            points.Insert(0, new Vector2(x, y));
            ang += (float)totalAngle / outerSmoothness;

            //if (i == outerSmoothness)
            //    points.Add(new Vector2(x, y));
            //
            // ^ needs this to connect shape if using original edgeCollider script
        }

        pc2.points = points.ToArray();

        //Render thing
        int pointCount = 0;
        pointCount = pc2.GetTotalPointCount();
        Debug.Log(pointCount);
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Vector2[] polyPoints = pc2.points;
        Vector3[] vertices = new Vector3[pointCount];
        Vector2[] uv = new Vector2[pointCount];
        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = polyPoints[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }
        Triangulator tr = new Triangulator(polyPoints);
        int[] triangles = tr.Triangulate();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mf.mesh = mesh;

        needsUpdate = false;
        return points.ToArray();
    }

    void Start () {
        needsUpdate = false;

        pc2 = GetComponent<PolygonCollider2D>();
        getPoints(new Vector2(0, 0));
	}
	
	// Update is called once per frame
	void Update () {
        if (needsUpdate)
            getPoints(new Vector2(0, 0));
	}
}
