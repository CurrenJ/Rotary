using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]

public class ColliderToMesh : MonoBehaviour
{
    PolygonCollider2D pc2;
    void Start()
    {
       render();
    }

    public void render(Vector2[] ps)
    {
        Vector2[] newPs = new Vector2[ps.Length - 1];
        for (int p = 0; p < ps.Length - 1; p++)
        {
            newPs[p] = ps[p];
        }


        pc2 = gameObject.GetComponent<PolygonCollider2D>();
        pc2.points = newPs;

        //Render thing
        int pointCount = 0;
        pointCount = pc2.GetTotalPointCount();
        Debug.Log(pointCount);
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Vector2[] points = pc2.points;
        Vector3[] vertices = new Vector3[pointCount];
        Vector2[] uv = new Vector2[pointCount];
        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = points[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }
        Triangulator tr = new Triangulator(points);
        int[] triangles = tr.Triangulate();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mf.mesh = mesh;
        //Render thing
    }

    public void render(EdgeCollider2D edge) {

        Vector2[] ps = edge.points;
        Vector2[] newPs = new Vector2[ps.Length - 1];
        for (int p = 0; p < ps.Length-1; p++) {
            newPs[p] = ps[p];
        }


        pc2 = gameObject.GetComponent<PolygonCollider2D>();
        pc2.points = newPs;

        //Render thing
        int pointCount = 0;
        pointCount = pc2.GetTotalPointCount();
        Debug.Log(pointCount);
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Vector2[] points = pc2.points;
        Vector3[] vertices = new Vector3[pointCount];
        Vector2[] uv = new Vector2[pointCount];
        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = points[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }
        Triangulator tr = new Triangulator(points);
        int[] triangles = tr.Triangulate();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mf.mesh = mesh;
        //Render thing
    }

    public void render()
    {

        Vector2[] ps = GetComponent<EdgeCollider2D>().points;
        Vector2[] newPs = new Vector2[ps.Length - 1];
        for (int p = 0; p < ps.Length - 1; p++)
        {
            newPs[p] = ps[p];
        }


        pc2 = gameObject.AddComponent<PolygonCollider2D>();
        pc2.points = newPs;

        //Render thing
        int pointCount = 0;
        pointCount = pc2.GetTotalPointCount();
        Debug.Log(pointCount);
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Vector2[] points = pc2.points;
        Vector3[] vertices = new Vector3[pointCount];
        Vector2[] uv = new Vector2[pointCount];
        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = points[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }
        Triangulator tr = new Triangulator(points);
        int[] triangles = tr.Triangulate();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mf.mesh = mesh;
        //Render thing
    }

}