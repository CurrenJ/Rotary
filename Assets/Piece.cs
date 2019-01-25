using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

public class Piece : MonoBehaviour {

    public float innerRadius = 5;
    public int innerSmoothness = 56;

    public float outerRadius = 6;
    public int outerSmoothness = 56;

    public float totalAngle = 90;
    public float offsetRotation = 45;

    public Vector3 ogScale;
    public Vector3 ogPos;
    public float timeElapsed;

    Vector2 origin, center;

    PolygonCollider2D pc2;

    public bool needsUpdate;

    public float angle;
    public bool locked;
    public float shrinkTime;
    public float moveTime;
    public Vector2 posOnGrid;
    public float adjacentRadOnGrid;
    public bool lockedOnce;
    public Color color;
    public Color outlineColor;
    public bool outlineEnabled;

    public float fadeTime;
    public float fadeTimeElapsed;
    public Vector2[] points;
    public Vector2[] realPoints;

    public Vector2[] getPoints(Vector2 off)
    {
        timeElapsed = 0;
        locked = false;
        lockedOnce = false;
        fadeTime = 0.5F;
        fadeTimeElapsed = fadeTime;

        List<Vector2> points = new List<Vector2>();

        origin = new Vector2(0, 0);
        center = origin + off;

        float ang = offsetRotation;

        if (innerRadius != 0)
            for (int i = 0; i <= innerSmoothness; i++)
            {

                float x = center.x + innerRadius * Mathf.Cos(ang * Mathf.Deg2Rad);
                float y = center.y + innerRadius * Mathf.Sin(ang * Mathf.Deg2Rad);

                points.Add(new Vector2(x, y));
                ang += (float)totalAngle / innerSmoothness;
            }

        ang = offsetRotation;

        int oS = outerSmoothness;
        if (totalAngle == 360)
            oS++;

        for (int i = 0; i <= oS; i++)
        {
            float x = center.x + outerRadius * Mathf.Cos(ang * Mathf.Deg2Rad);
            float y = center.y + outerRadius * Mathf.Sin(ang * Mathf.Deg2Rad);

            points.Insert(0, new Vector2(x, y));
            ang += (float)totalAngle / oS;

            //if (i == outerSmoothness)
            //    points.Add(new Vector2(x, y));
        }

        pc2.points = points.ToArray();

        //Render thing
        int pointCount = 0;
        pointCount = pc2.GetTotalPointCount();
        //Debug.Log(pointCount);
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

    public void lockPiece(){
        locked = true;
        lockedOnce = true;
    }

    public void setOrientation(float angle, float arcSize)
    {
        this.angle = angle;
        transform.eulerAngles = new Vector3(0, 0, angle);
        totalAngle = arcSize;
    }

    public void setColor(Color color) {
        GetComponent<MeshRenderer>().material.color = color;
    }

    void Start () {
        needsUpdate = false;

        pc2 = GetComponent<PolygonCollider2D>();
        points = getPoints(new Vector2(0, 0));
	}
	
	// Update is called once per frame
	void Update () {
        if (needsUpdate)
            points = getPoints(new Vector2(0, 0));
	}
}
