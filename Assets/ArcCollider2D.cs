/*
The MIT License (MIT)

Copyright (c) 2016 GuyQuad

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

You can contact me by email at guyquad27@gmail.com or on Reddit at https://www.reddit.com/user/GuyQuad
*/


#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Physics 2D/Arc Collider 2D")]

[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(ColliderToMesh))]
public class ArcCollider2D : MonoBehaviour {

    [Range(1, 25)]
    public float innerRadius = 3;
    [Range(5,90)]
    public int innerSmoothness = 24;

    [Range(1, 25)]
    public float outerRadius = 4;
    [Range(5, 90)]
    public int outerSmoothness = 24;



    [Range(10, 360)]
    public int totalAngle = 360;

    [Range(0, 360)]
    public int offsetRotation = 0;
    
    Vector2 origin, center;
    
    public Vector2[] getPoints(Vector2 off)
    {
        List<Vector2> points = new List<Vector2>();

        origin = transform.localPosition;
        center = origin + off;
        
        float ang = offsetRotation;

        for (int i = 0; i <= innerSmoothness; i++)
        {

            float x = center.x + innerRadius * Mathf.Cos(ang * Mathf.Deg2Rad);
            float y = center.y + innerRadius * Mathf.Sin(ang * Mathf.Deg2Rad);

            points.Add(new Vector2(x, y));
            ang += (float)totalAngle/innerSmoothness;
        }

        ang = offsetRotation;

        for (int i = 0; i <= outerSmoothness; i++)
        {
            float x = center.x + outerRadius * Mathf.Cos(ang * Mathf.Deg2Rad);
            float y = center.y + outerRadius * Mathf.Sin(ang * Mathf.Deg2Rad);

            points.Insert(0, new Vector2(x, y));
            ang += (float)totalAngle / outerSmoothness;

            if (i == outerSmoothness)
                points.Add(new Vector2(x, y));
        }

        GetComponent<ColliderToMesh>().render(points.ToArray());
        return points.ToArray();
    }

    public void Start()
    {
        GetComponent<EdgeCollider2D>().points = getPoints(GetComponent<EdgeCollider2D>().offset);
    }
}
#endif