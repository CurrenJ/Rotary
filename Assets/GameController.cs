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


	// Use this for initialization
	void Start () {
        pieces = new List<GameObject>();
        createPiece();
	}
	
	// Update is called once per frame
	void Update () {
        //foreach (GameObject piece in pieces) {
        //    piece.GetComponent<MeshRenderer>().material = materialRed;
        //}
    }

    private void createPiece() {
        GameObject piece = new GameObject();
        piece.name = "Piece";
        piece.AddComponent<Piece>();
        piece.GetComponent<MeshRenderer>().material = materialRed;
        pieces.Add(piece);
    }
}
