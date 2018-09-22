using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

    public Material materialRed;
    public Material materialYellow;
    public Material materialBlue;
    public Material materialGreen;
    public GameObject baseCirc;
    public List<GameObject> pieces;
    private GameObject core;
    private Piece corePiece;
    public float shrinkPercentPerSecond;
    public float movePercentPerSecond;

    // Use this for initialization
    void Start() {
        pieces = new List<GameObject>();

        createCore();
        createPiece(0, 4, Direction.Down);
        createPiece(0, -4, Direction.Up);


    }

    // Update is called once per frame
    void Update() {
        foreach (GameObject piece in pieces)
        {
            piece.GetComponent<Piece>().timeElapsed += Time.deltaTime;

            bool shrinkLocked = !(piece.transform.localScale.x * piece.GetComponent<Piece>().innerRadius > corePiece.outerRadius);
            if (!shrinkLocked)
                shrinkPiece(piece);

            //Debug.Log("Switching on " + piece.GetComponent<Piece>().d);
            if (!piece.GetComponent<Piece>().locked)
            {
                switch (piece.GetComponent<Piece>().d)
                {
                    case Direction.Up:
                        if (piece.transform.position.y >= core.transform.position.y && shrinkLocked) //change condition to lock when shrunk to size of outermost piece. gonna be tough 
                        {
                            lockPiece(piece);
                        }
                        else movePiece(piece);
                        break;

                    case Direction.Down:
                        if (piece.transform.position.y <= core.transform.position.y && shrinkLocked)
                        {
                            lockPiece(piece);
                        }
                        else movePiece(piece);
                        break;

                    case Direction.Right:
                        if (piece.transform.position.x >= core.transform.position.x && shrinkLocked)
                        {
                            lockPiece(piece);
                        }
                        else movePiece(piece);
                        break;

                    case Direction.Left:
                        if (piece.transform.position.x <= core.transform.position.x && shrinkLocked)
                        {
                            lockPiece(piece);
                        }
                        else movePiece(piece);
                        break;
                }
            } 
        }
    }

    private void lockPiece(GameObject piece) {
        piece.transform.position = core.transform.position;
        float matchScale = corePiece.outerRadius / piece.GetComponent<Piece>().innerRadius;
        piece.transform.localScale = new Vector3(matchScale, matchScale, 1);
        piece.transform.GetComponent<Piece>().lockPiece();
        Debug.Log("Piece locked. Position identifier: " + piece.transform.position);
    }

    private void createCore() {
        GameObject piece = new GameObject();
        piece.name = "Piece";
        piece.AddComponent<Piece>();
        piece.GetComponent<MeshRenderer>().material = materialRed;

        piece.GetComponent<Piece>().innerRadius = 0;
        piece.GetComponent<Piece>().outerRadius = 1F;
        piece.GetComponent<Piece>().totalAngle = 360;
        piece.GetComponent<Piece>().needsUpdate = true;
        core = piece;
        corePiece = piece.GetComponent<Piece>();
    }

    private void createPiece() {
        GameObject piece = new GameObject();
        piece.name = "Piece";
        piece.AddComponent<Piece>();
        piece.GetComponent<MeshRenderer>().material = materialRed;
        piece.gameObject.transform.position = core.transform.position;
        piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
        piece.GetComponent<Piece>().ogPos = piece.transform.position;
        piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
        piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
        piece.GetComponent<Piece>().needsUpdate = true;
        pieces.Add(piece);
    }

    private void createPiece(float x, float y, Direction dir)
    {
        GameObject piece = new GameObject();
        piece.name = "Piece";
        piece.AddComponent<Piece>();
        piece.GetComponent<MeshRenderer>().material = materialRed;
        piece.gameObject.transform.position = core.transform.position;
        piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
        piece.transform.position += new Vector3(x, y, 0);
        piece.GetComponent<Piece>().ogPos = piece.transform.position;
        piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
        piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
        piece.GetComponent<Piece>().setDir(dir);
        Debug.Log("Piece created at: " + piece.transform.position);
        pieces.Add(piece);
    }

    private void shrinkPiece(GameObject piece) {
        //float scalar = Mathf.Pow((100 - shrinkPercentPerSecond) / 100F, piece.GetComponent<Piece>().timeElapsed);
        float scalar = (100 - (shrinkPercentPerSecond * piece.GetComponent<Piece>().timeElapsed)) / 100F;
        float ratio = (core.GetComponent<Piece>().outerRadius / piece.GetComponent<Piece>().innerRadius);

        piece.transform.localScale = new Vector3(((piece.GetComponent<Piece>().ogScale.x - ratio) * scalar) + ratio, ((piece.GetComponent<Piece>().ogScale.y - ratio) * scalar) + ratio, piece.transform.localScale.z);
        //Debug.Log(piece.transform.localScale.x);
    }

    private void movePiece(GameObject piece) {
        float scalar = (100 - (movePercentPerSecond * piece.GetComponent<Piece>().timeElapsed)) / 100F;
        //Debug.Log(scalar);
        piece.transform.localPosition = (piece.GetComponent<Piece>().ogPos - core.transform.position) * scalar;
        Debug.Log((piece.GetComponent<Piece>().ogPos - core.transform.position) * scalar);
    }


}
