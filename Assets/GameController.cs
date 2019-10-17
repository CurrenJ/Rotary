using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{

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
    public GameObject[,] piecesGrid;
    private int nPA = 8;
    private int depth = 6;
    private bool random = false;
    private float randomTimer;
    private int rotationPosition;
    public GameObject lockedContainer;
    private GameObject[,] removeList;
    public GameObject debugPiece;
    public string[] colors;
    private float turnTimeElapsed;
    public float turnTimeDefault;
    private float turnTime;
    private float lastRotationAngle;
    private float currentRotationAngle;
    private List<GameObject> outlines;
    private List<GameObject> outlinesRemove;
    public float spawnTime;
    public GameObject wall;
    public int score;
    public string[] specialTypes;
    public List<GameObject> fallingListRemove;

    public float camZoomTimeElapsed;
    public float camZoomDest;
    public float camZoomStart;
    public float camTimeToMove;

    private const float DEG2RAD = Mathf.PI / 180;
    private const float pieceOffset = 45;
    private const float spawnDistance = 10;

    public float outlineConst;
    public GameObject particles;

    // Use this for initialization
    void Start()
    {
        pieces = new List<GameObject>();
        outlines = new List<GameObject>();
        outlinesRemove = new List<GameObject>();
        fallingListRemove = new List<GameObject>();
        wall.GetComponent<MeshRenderer>().material.renderQueue = 0;
        score = 0;
        setupGrid(nPA, depth);

        createCore();
        //createPiece(0, 4, 0, 5, 5, Color.blue);
        createPiece(0, 5, 4, Color.red);
        createPiece(1, 5, 4, Color.red);
        createPiece(5, 5, 4, Color.red);
        //createPiece(-3, 0, 3, 6, 5);
        //createPiece(-5, 0, 3, 8, 7);
        //createPiece(-5, 0, 2, 3, 2);

        enableRandomPieces();
    }

    // Update is called once per frame
    void Update()
    {
        
        scoreProgressionUpdate();

        foreach (GameObject piece in pieces)
        {
            piece.GetComponent<Piece>().timeElapsed += Time.deltaTime;

            bool shrinkLocked = ((decimal)piece.transform.localScale.x * (decimal)piece.GetComponent<Piece>().innerRadius) == (decimal)piece.GetComponent<Piece>().adjacentRadOnGrid;
            //Debug.Log("S: " + piece.transform.localScale.x + " x " + piece.GetComponent<Piece>().innerRadius + " = " + piece.transform.localScale.x * piece.GetComponent<Piece>().innerRadius + " | " + piece.GetComponent<Piece>().adjacentRadOnGrid + " " + shrinkLocked);
            if (!shrinkLocked)
                shrinkPiece(piece, piece.GetComponent<Piece>().shrinkTime);

            //Debug.Log("Switching on " + piece.GetComponent<Piece>().d);
            if (!piece.GetComponent<Piece>().locked)
            {
                if ((piece.transform.position).magnitude == 0 && shrinkLocked) {
                    if (turnTimeElapsed >= turnTime)
                        lockPiece(piece);
                    else
                    {
                        lockPiece(piece, rotationPosition);
                        //if turnTimeDefault is a decent speed, then users keeping pieces unlocked by spamming the turn shouldn't be an issue. also I need to handle users turning falling pieces into existing locked pieces. what should happen? should the core be blocked from turning? piece deleted? or does the user lose?
                        //removePiece(piece); 
                    }
                }
                else
                    movePiece(piece, piece.GetComponent<Piece>().moveTime);
            }
        }
        foreach(GameObject piece in fallingListRemove) {
            //Debug.Log("removing falling piece: " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y);
            pieces.Remove(piece);
        }
        fallingListRemove.Clear();
        removePiece();

        foreach (GameObject piece in outlines) {
            fadeOutline(piece);
        }
        foreach (GameObject piece in outlinesRemove) {
            outlines.Remove(piece);
        }
        outlinesRemove.Clear();

        foreach (ParticleSystem p in lockedContainer.GetComponentsInChildren<ParticleSystem>()) {
            //Debug.Log(p.IsAlive());
            if (!p.IsAlive())
                Destroy(p.gameObject);
        }

        randomTimer += Time.deltaTime;
        if (randomTimer >= spawnTime && random)
        {
            Color c;
            ColorUtility.TryParseHtmlString(colors[Random.Range(0, colors.Length)], out c);
            while (!createPiece(Random.Range(0, nPA), 5, 2, c, randSpecType()))
            {
                // float shrinkT = Random.Range(2, 5);

            }

            if (spawnTime > 1)
                spawnTime -= 0.01F;

            randomTimer = 0;
        }

        handleInput();

        turnTimeElapsed += Time.deltaTime;
        smoothRotateUpdate();
    }

    private void handleInput()
    {

        /** PC Input Handling **/
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            if (Input.GetKeyDown(KeyCode.R))
                rotateByPos(1);
            else if (Input.GetKeyDown(KeyCode.L))
            {
                spawnParticles(debugPiece);
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                for (int d = depth - 1; d >= 0; d--)
                {
                    string str = "";
                    for (int p = 0; p < nPA; p++)
                    {
                        if (piecesGrid[p, d] != null)
                            str += "[P] ";
                        else str += "[ ] ";
                    }
                    Debug.Log(str);
                }
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                removePiece(debugPiece);
            }
            else if (Input.GetKeyDown(KeyCode.S)) {
                Color c;
                ColorUtility.TryParseHtmlString(colors[Random.Range(0, colors.Length)], out c);
                while (!createPiece(Random.Range(0, nPA), 5, 2, c, randSpecType()))
                {
                    // float shrinkT = Random.Range(2, 5);

                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.x >= Screen.width / 2)
                    rotateSmooth(1);
                if (Input.mousePosition.x < Screen.width / 2)
                    rotateSmooth(-1);
            }
        }

        /** Mobile Input Handling **/
        else if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.touchCount > 0)
            {
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    if (Input.touches[0].position.x >= Screen.width / 2)
                        rotateSmooth(-1);
                    if (Input.touches[0].position.x < Screen.width / 2)
                        rotateSmooth(1);
                }
            }
        }
    }

    private void lockPiece(GameObject piece)
    {
        bool removeP = false;
        if (piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, depth - 1] != null && piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, depth - 1].GetComponent<Piece>().lockedOnce)
            removeP = true;
        //basically checks if the column the piece is falling into already has the max amount of pieces allowed

        piece.transform.position = core.transform.position;
        float matchScale = piece.GetComponent<Piece>().adjacentRadOnGrid / piece.GetComponent<Piece>().innerRadius;
        piece.transform.localScale = new Vector3(matchScale, matchScale, 1);
        piece.transform.GetComponent<Piece>().lockPiece();
        float tempAng = piece.transform.eulerAngles.z;
        piece.transform.SetParent(lockedContainer.transform, true);
        //piece.transform.eulerAngles = new Vector3(0, 0, tempAng + (currentRotationAngle - (rotationPosition * (360F / nPA)) ));
        outlinePiece(piece, false);
        fallingListRemove.Add(piece); //removes this piece object from the Pieces list (non locked pieces, falling for first time)

        if (removeP)
        {
            removePiece(piece);
            Debug.Log("Removing piece " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y + " because of overflow.");
        }
        else
        {
            checkForMatches(piece);
        }
        //Debug.Log("Piece locked. Position identifier: " + piece.transform.position + " Scale: " + matchScale);
        //Debug.Log("Piece locked. " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y);
    }

    private void lockPiece(GameObject piece, int lockRotPos)
    {
        bool removeP = false;
        if (piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, depth - 1] != null && piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, depth - 1].GetComponent<Piece>().lockedOnce)
            removeP = true;
        //basically checks if the column the piece is falling into already has the max amount of pieces allowed

        piece.transform.position = core.transform.position;
        float matchScale = piece.GetComponent<Piece>().adjacentRadOnGrid / piece.GetComponent<Piece>().innerRadius;
        piece.transform.localScale = new Vector3(matchScale, matchScale, 1);
        float tempAng = piece.transform.eulerAngles.z;
        piece.transform.localEulerAngles = new Vector3(0, 0, tempAng + currentRotationAngle - (rotationPosition * (360 / nPA)));
        piece.transform.GetComponent<Piece>().lockPiece();
        piece.transform.SetParent(lockedContainer.transform, true);
        //piece.transform.eulerAngles = new Vector3(0, 0, tempAng + (currentRotationAngle - (rotationPosition * (360F / nPA)) ));
        outlinePiece(piece, false);
        fallingListRemove.Remove(piece);

        if (removeP)
        {
            removePiece(piece);
            Debug.Log("Removing piece " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y + " because of overflow.");
        }
        else
        {
            checkForMatches(piece);
        }
        //Debug.Log("Piece locked. Position identifier: " + piece.transform.position + " Scale: " + matchScale);
        //Debug.Log("Piece locked. " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y);
    }

    private void checkForMatches(GameObject piece)
    {
        List<GameObject> pTRhori = new List<GameObject>(); //pTR = pieces to remove
        pTRhori.Add(piece);
        List<GameObject> pTRvert = new List<GameObject>(); //pTR = pieces to remove
        pTRvert.Add(piece);
        int x = (int)piece.GetComponent<Piece>().posOnGrid.x;
        int y = (int)piece.GetComponent<Piece>().posOnGrid.y;
        Color c = piece.GetComponent<Piece>().color;

        bool breakoutA = false;
        for (int a = 1; a < nPA / 2 && !breakoutA; a++)
        {
            if (getPieceComp(x + a, y) != null)
            {
                //Debug.Log("C: " + c.ToString() + " | " + getPieceComp(x + a, y).color.ToString() + " | " + getPieceComp(x + a, y).color.ToString().Equals(c.ToString()) + " | " + getPieceComp(x + a, y).locked);
                if (getPieceComp(x + a, y) != null && getPieceComp(x + a, y).locked && getPieceComp(x + a, y).color.ToString().Equals(c.ToString()))
                {
                    pTRhori.Add(getPiece(x + a, y));
                }
                else breakoutA = true;
            }
            else breakoutA = true;
        }

        bool breakoutB = false;
        for (int a = 1; a < nPA / 2 && !breakoutB; a++)
        {
            if (getPieceComp(x - a, y) != null)
            {
                //Debug.Log("C: " + c.ToString() + " | " + getPieceComp(x - a, y).color.ToString() + " | " + getPieceComp(x - a, y).color.ToString().Equals(c.ToString()) + " | " + getPieceComp(x - a, y).locked);
                if (getPieceComp(x - a, y) != null && getPieceComp(x - a, y).locked && getPieceComp(x - a, y).color.ToString().Equals(c.ToString()))
                {
                    pTRhori.Add(getPiece(x - a, y));
                }
                else breakoutB = true;
            }
            else breakoutB = true;
        }

        bool breakoutC = false;
        for (int a = 1; a <= y && !breakoutC; a++)
        {
            //Debug.Log("C: " + c.ToString() + " | " + getPieceComp(x - a, y).color.ToString() + " | " + getPieceComp(x - a, y).color.ToString().Equals(c.ToString()) + " | " + getPieceComp(x - a, y).locked);
            if (getPieceComp(x, y - a) != null && getPieceComp(x, y - a).locked && getPieceComp(x, y - a).color.ToString().Equals(c.ToString()))
            {
                pTRvert.Add(getPiece(x, y - a));
            }
            else breakoutC = true;
        }

        //string str = "";
        //    foreach (GameObject p in pTRhori) {
        //        str += "[ " + p.GetComponent<Piece>().posOnGrid.x + ", " + p.GetComponent<Piece>().posOnGrid.y + "] ";
        //    }
        //    Debug.Log(str);
        int matches = 0;
        if (pTRhori.Count > 2)
        {
            foreach (GameObject p in pTRhori) {
                matches += activateSpecial(p);
            }
            foreach (GameObject p in pTRhori)
            {
                if (p != null)
                {
                    matches++;
                    removePiece(p);
                    Debug.Log("Removing piece " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y + " because of horizontal matches.");
                }

            }
            //recalcMovingPieces();
        }
        if (pTRvert.Count > 2 && false) //disabled because it was too easy to just stack the same colors
        {
            foreach (GameObject p in pTRvert) {
                matches += activateSpecial(p);
            }
            foreach (GameObject p in pTRvert)
            {
                if (p != null)
                {
                    matches++;
                    removePiece(p);
                    Debug.Log("Removing piece " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y + " because of vertical matches.");
                }
            }
            //recalcMovingPieces();
        }
        scorePoints(matches);
    }

    private int activateSpecial(GameObject piece) {
        List<GameObject> toRemove = new List<GameObject>();
        Piece p = piece.GetComponent<Piece>();
        if (p.specialType.Equals("bomb"))
        {
            //Debug.Log("Bomb activated.");
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    bool skip = false;
                    int targetPieceX = (int)p.posOnGrid.x + x;
                    int targetPieceY = (int)p.posOnGrid.y + y;
                    if (targetPieceY < 0 || targetPieceY > depth - 1)
                    {
                        skip = true;
                    }

                    if (!skip && getPiece(targetPieceX, targetPieceY) != null && getPieceComp(targetPieceX, targetPieceY).lockedOnce)
                    {
                        //Debug.Log("obliterating " + targetPieceX + ", " + targetPieceY);
                        removePiece(getPiece(targetPieceX, targetPieceY));
                        Debug.Log("Removing piece " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y + " because of special type " + piece.GetComponent<Piece>().specialType + ".");
                    }
                }
            }
        }
        else if (p.specialType.Equals("color"))
        {
            for (int x = 0; x < nPA; x++)
            {
                for (int y = 0; y < depth; y++)
                {
                    if (getPieceComp(x, y) != null && getPieceComp(x, y).color.ToString().Equals(p.color.ToString()))
                    {
                        toRemove.Add(getPiece(x, y));
                    }
                }
            }
        }
        else if (p.specialType.Equals("ring"))
        {
            for (int x = 0; x < nPA; x++)
            {
                if (getPieceComp(x, (int)p.posOnGrid.y) != null)
                {
                    toRemove.Add(getPiece(x, (int)p.posOnGrid.y));
                }
            }
        }
        else if (p.specialType.Equals("vert")) {
            for (int y = 0; y < depth; y++) {
                if (getPieceComp((int)p.posOnGrid.x, y) != null) {
                    toRemove.Add(getPiece((int)p.posOnGrid.x, y));
                }
            }
        }

        int num = toRemove.Count;
        foreach (GameObject g in toRemove) {
            removePiece(g);
        }
        return num;
    }

    private int normalizeRotPos(int rotPos) {
        if (rotPos >= nPA)
            return rotPos - nPA;
        else if (rotPos < 0)
            return rotPos + nPA;
        else return rotPos;
    }

    private Piece getPieceComp(int x, int y)
    {
        if (x >= nPA)
            x -= nPA;
        else if (x < 0)
            x += nPA;

        if (y < 0 || y >= depth)
            return null;

        if (piecesGrid[x, y] != null)
            return piecesGrid[x, y].GetComponent<Piece>();
        else return null;
    }

    private GameObject getPiece(int x, int y)
    {
        if (x >= nPA)
            x -= nPA;
        else if (x < 0)
            x += nPA;

        if (y < 0 || y >= depth)
            return null;

        return piecesGrid[x, y];
    }

    private void createCore()
    {
        GameObject piece = new GameObject();
        piece.name = "Piece";
        piece.AddComponent<Piece>();
        piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
        piece.GetComponent<MeshRenderer>().material.color = new Color(96F / 255F, 96F / 255F, 96F / 255F);
        piece.transform.SetParent(lockedContainer.transform, true);

        piece.GetComponent<Piece>().innerRadius = 0;
        piece.GetComponent<Piece>().outerRadius = 1F;
        piece.GetComponent<Piece>().totalAngle = 360F;
        piece.GetComponent<Piece>().needsUpdate = true;
        core = piece;
        corePiece = piece.GetComponent<Piece>();
    }

    private void createPiece()
    {
        GameObject piece = new GameObject();
        piece.name = "Piece";
        piece.AddComponent<Piece>();
        piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
        piece.gameObject.transform.position = core.transform.position;
        piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
        piece.GetComponent<Piece>().ogPos = piece.transform.position;
        piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
        piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / nPA;
        piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
        piece.GetComponent<Piece>().needsUpdate = true;
        pieces.Add(piece);
    }

    private void createPiece(float x, float y, int positionAround, float mT, float sT)
    {
        if (piecesGrid[positionAround, depth - 1] == null)
        {
            GameObject piece = new GameObject();
            piece.name = "Piece";
            piece.AddComponent<Piece>();
            piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(x, y, 0);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / nPA;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            //Debug.Log("Piece created at: " + piece.transform.position);
            pieces.Add(piece);
            for (int d = 0; d < depth; d++)
            {
                if (piecesGrid[positionAround, d] == null)
                {
                    piecesGrid[positionAround, d] = piece;
                    piece.GetComponent<Piece>().posOnGrid = new Vector2(positionAround, d);
                    piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
                    //Debug.Log("Piece created at grid location [" + positionAround + ", " + d + "]");
                    return;
                }
            }
        }
    }

    private bool createPiece(int positionAround, float mT, float sT)
    {
        int posArAdjusted = positionAround + rotationPosition;
        if (posArAdjusted < 0)
        {
            posArAdjusted += nPA;
        }
        else if (posArAdjusted >= nPA)
        {
            posArAdjusted -= nPA;
        }

        if (piecesGrid[posArAdjusted, depth - 1] == null)
        {
            GameObject piece = new GameObject();
            piece.name = "Piece";
            piece.AddComponent<Piece>();
            piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, 0);
            //Debug.Log("Pos: " + (posArAdjusted) + " Deg  (rads): " + ((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) + " X: " + Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5 + " Y: " + Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / nPA;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            piece.GetComponent<Piece>().setSpecialType("");
            //Debug.Log("Piece created at: " + piece.transform.position);
            pieces.Add(piece);
            bool breakout = false;
            for (int d = 0; d < depth && !breakout; d++)
            {
                if (piecesGrid[positionAround, d] == null)
                {
                    piecesGrid[positionAround, d] = piece;
                    piece.GetComponent<Piece>().posOnGrid = new Vector2(positionAround, d);
                    piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
                    //Debug.Log("Piece created at grid location [" + positionAround + ", " + d + "]");
                    breakout = true;
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool createPiece(int positionAround, float mT, float sT, Color color)
    {
        int posArAdjusted = positionAround + rotationPosition;
        if (posArAdjusted < 0)
        {
            posArAdjusted += nPA;
        }
        else if (posArAdjusted >= nPA)
        {
            posArAdjusted -= nPA;
        }

        if (piecesGrid[positionAround, depth - 1] == null)
        {
            GameObject piece = new GameObject();
            piece.name = "Piece";
            piece.AddComponent<Piece>();
            piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, 0);
            //Debug.Log("Pos: " + (posArAdjusted) + " Deg  (rads): " + ((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) + " X: " + Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5 + " Y: " + Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / nPA;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            piece.GetComponent<Piece>().setColor(color);
            piece.GetComponent<Piece>().color = color;
            piece.GetComponent<Piece>().setSpecialType("");
            outlinePiece(piece, true);
            //Debug.Log("Piece created at: " + piece.transform.position);
            pieces.Add(piece);
            bool breakout = false;
            for (int d = 0; d < depth && !breakout; d++)
            {
                if (piecesGrid[positionAround, d] == null)
                {
                    piecesGrid[positionAround, d] = piece;
                    piece.GetComponent<Piece>().posOnGrid = new Vector2(positionAround, d);
                    piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
                    //Debug.Log("Piece created at grid location [" + positionAround + ", " + d + "]");
                    breakout = true;
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool createPiece(int positionAround, float mT, float sT, Color color, string specialType)
    {
        int posArAdjusted = positionAround + rotationPosition;
        if (posArAdjusted < 0)
        {
            posArAdjusted += nPA;
        }
        else if (posArAdjusted >= nPA)
        {
            posArAdjusted -= nPA;
        }

        if (piecesGrid[positionAround, depth - 1] == null)
        {
            GameObject piece = new GameObject();
            piece.name = "Piece";
            piece.AddComponent<Piece>();
            piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, 0);
            //Debug.Log("Pos: " + (posArAdjusted) + " Deg  (rads): " + ((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) + " X: " + Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5 + " Y: " + Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / nPA;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            piece.GetComponent<Piece>().setColor(color);
            piece.GetComponent<Piece>().color = color;
            piece.GetComponent<Piece>().setSpecialType(specialType);
            outlinePiece(piece, true);
            //Debug.Log("Piece created at: " + piece.transform.position);
            pieces.Add(piece);
            bool breakout = false;
            for (int d = 0; d < depth && !breakout; d++)
            {
                if (piecesGrid[positionAround, d] == null)
                {
                    piecesGrid[positionAround, d] = piece;
                    piece.GetComponent<Piece>().posOnGrid = new Vector2(positionAround, d);
                    piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
                    //Debug.Log("Piece created at grid location [" + positionAround + ", " + d + "]");
                    breakout = true;
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private void createPiece(float x, float y, int positionAround, float mT, float sT, Color color)
    {
        if (piecesGrid[positionAround, depth - 1] == null)
        {
            GameObject piece = new GameObject();
            piece.name = "Piece";
            piece.AddComponent<Piece>();
            piece.GetComponent<MeshRenderer>().material = Instantiate(materialRed);
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(x, y, 0);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / nPA;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            piece.GetComponent<Piece>().setColor(color);
            piece.GetComponent<Piece>().setSpecialType("");
            //Debug.Log("Piece created at: " + piece.transform.position);
            pieces.Add(piece);
            for (int d = 0; d < depth; d++)
            {
                if (piecesGrid[positionAround, d] == null)
                {
                    piecesGrid[positionAround, d] = piece;
                    piece.GetComponent<Piece>().posOnGrid = new Vector2(positionAround, d);
                    piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
                    //Debug.Log("Piece created at grid location [" + positionAround + ", " + d + "]");
                    return;
                }
            }
        }
    }

    //private void shrinkPiece(GameObject piece)
    //{
    //    //float scalar = Mathf.Pow((100 - shrinkPercentPerSecond) / 100F, piece.GetComponent<Piece>().timeElapsed);
    //    float scalar = (100 - (shrinkPercentPerSecond * piece.GetComponent<Piece>().timeElapsed)) / 100F;
    //    float ratio = (piece.GetComponent<Piece>().adjacentRadOnGrid / piece.GetComponent<Piece>().innerRadius);

    //    piece.transform.localScale = new Vector3(((piece.GetComponent<Piece>().ogScale.x - ratio) * scalar) + ratio, ((piece.GetComponent<Piece>().ogScale.y - ratio) * scalar) + ratio, piece.transform.localScale.z);
    //    //Debug.Log(piece.transform.localScale.x);
    //}

    private void shrinkPiece(GameObject piece, float time)
    {
        float scalar = 1 - (piece.GetComponent<Piece>().timeElapsed / time);
        if (scalar < 0)
            scalar = 0;
        scalar = quadEasingOut(scalar);
        //Debug.Log("scalar: " + scalar + " (" + piece.GetComponent<Piece>().timeElapsed + " / " + time + ")");
        float ratio = (piece.GetComponent<Piece>().adjacentRadOnGrid / piece.GetComponent<Piece>().innerRadius); //destination scale for piece

        piece.transform.localScale = new Vector3(((piece.GetComponent<Piece>().ogScale.x - ratio) * scalar) + ratio, ((piece.GetComponent<Piece>().ogScale.y - ratio) * scalar) + ratio, piece.transform.localScale.z);
        //Debug.Log(piece.transform.localScale.x);
    }

    //private void movePiece(GameObject piece)
    //{
    //    float scalar = (100 - (movePercentPerSecond * piece.GetComponent<Piece>().timeElapsed)) / 100F;
    //    //Debug.Log(scalar);
    //    piece.transform.localPosition = (piece.GetComponent<Piece>().ogPos - core.transform.position) * scalar;
    //    //Debug.Log((piece.GetComponent<Piece>().ogPos - core.transform.position) * scalar);
    //}

    private void movePiece(GameObject piece, float time)
    {
        float scalar = 1 - (piece.GetComponent<Piece>().timeElapsed / time);
        if (scalar < 0)
            scalar = 0;
        //Debug.Log("X in: " + scalar + " X out: " + quadEasing(scalar));
        //Debug.Log("scalar: " + scalar + " (" + piece.GetComponent<Piece>().timeElapsed + " / " + time + ")");
        //Debug.Log("OG Pos: " + piece.GetComponent<Piece>().ogPos.x + ", " + piece.GetComponent<Piece>().ogPos.y);

        scalar = quadEasingOut(scalar);
        piece.transform.localPosition = (piece.GetComponent<Piece>().ogPos - core.transform.position) * scalar;
    }

    private float quadEasing(float x)
    { //input x value from 0 to 1, outputs value from 0 to 1 fitted to the easing function
        if (x <= 0.5f)
            return 2.0f * Mathf.Pow(x, 2);
        x -= 0.5f;
        return 2.0f * x * (1.0f - x) + 0.5F;

        //x -= 0.5f;
        //return 2.0f * x * (1.0f - x) + 0.5F;
    }

    private float quadEasingOut(float x)
    { //input x value from 0 to 1, outputs value from 0 to 1 fitted to the easing function
        return (Mathf.Pow(x, 2));
    }

    private void setupGrid(int numPiecesAround, int maxDepth)
    {
        rotationPosition = 0;
        turnTime = turnTimeDefault;
        turnTimeElapsed = turnTime;
        lastRotationAngle = lockedContainer.transform.eulerAngles.z;
        nPA = numPiecesAround;
        depth = maxDepth;
        piecesGrid = new GameObject[nPA, depth + 1];
        removeList = new GameObject[nPA, depth + 1];
        for (int x = 0; x < nPA; x++) {
            for (int y = 0; y < depth + 1; y++) {
                removeList[x, y] = null;
            }
        }
    }

    private float getOuterRadius(GameObject curPiece)
    {
        float rad = corePiece.outerRadius;
        for (int d = 0; d < curPiece.GetComponent<Piece>().posOnGrid.y; d++)
        {
            if (piecesGrid[(int)(curPiece.GetComponent<Piece>().posOnGrid.x), d] != null)
            {
                GameObject p = piecesGrid[(int)curPiece.GetComponent<Piece>().posOnGrid.x, d];
                rad += (p.GetComponent<Piece>().outerRadius - p.GetComponent<Piece>().innerRadius) * (getOuterRadius(p) / p.GetComponent<Piece>().innerRadius);
            }
        }
        //Debug.Log("[" + curPiece.GetComponent<Piece>().posOnGrid.x + ", " + curPiece.GetComponent<Piece>().posOnGrid.y + "] " + rad);
        return rad;
    }

    private void enableRandomPieces()
    {
        random = true;
        randomTimer = 0;
    }

    private void rotateSmooth(int difference) {
        if (turnTimeElapsed < turnTime)
        {
            //turnTime += turnTime - turnTimeElapsed;
            turnTime = turnTimeDefault;
            lastRotationAngle = currentRotationAngle;
        }
        else {
            turnTime = turnTimeDefault;
            lastRotationAngle = rotationPosition * (360F / nPA);
        }

        turnTimeElapsed = 0;

        rotateByPos(difference);
    }

    private void smoothRotateUpdate() {
        float scalar = (turnTimeElapsed / turnTime);
        if (scalar > 1)
        {
            scalar = 1;
        }
        //Debug.Log("X in: " + scalar + " X out: " + quadEasing(scalar));
        //Debug.Log("scalar: " + scalar + " (" + piece.GetComponent<Piece>().timeElapsed + " / " + time + ")");
        //Debug.Log("OG Pos: " + piece.GetComponent<Piece>().ogPos.x + ", " + piece.GetComponent<Piece>().ogPos.y);
        float angleDif = ((rotationPosition * (360F / nPA)) - lastRotationAngle);
        //Debug.Log("A1: " + (angleDif));
        if (Mathf.Abs(angleDif) > 180) {
            if (angleDif > 0)
                angleDif = -(360 - angleDif);
            else if (angleDif < 0)
                angleDif = angleDif + 360;
        }
        // Debug.Log("A@: " + angleDif);

        scalar = quadEasing(scalar);
        float angle = lastRotationAngle + (angleDif * scalar);
        if (angle >= 360)
            angle -= 360;
        else if (angle < 0)
            angle += 360;

        lockedContainer.transform.eulerAngles = new Vector3(0, 0, angle);
        currentRotationAngle = lockedContainer.transform.eulerAngles.z;
    }

    private void rotateByPos(int difference)
    {
        //int difference = rotationPosition - pos;
        //rotationPosition = pos;
        rotationPosition += difference;
        if (rotationPosition >= nPA)
            rotationPosition -= nPA;
        else if (rotationPosition < 0)
            rotationPosition = nPA + rotationPosition;

        lockedContainer.transform.eulerAngles = new Vector3(0, 0, rotationPosition * (360F / nPA));
        foreach (GameObject piece in pieces)
        {
            if (!piece.GetComponent<Piece>().lockedOnce)
            {
                Vector2 temp = piece.GetComponent<Piece>().posOnGrid;
                piecesGrid[(int)temp.x, (int)temp.y] = null;
            }
        }

        foreach (GameObject piece in pieces)
        {
            if (!piece.GetComponent<Piece>().lockedOnce)
            {
                Vector2 temp = piece.GetComponent<Piece>().posOnGrid;
                //piecesGrid[(int)temp.x, (int)temp.y] = null;
                temp.x -= difference;
                if (temp.x >= nPA)
                    temp.x -= nPA;
                else if (temp.x < 0)
                    temp.x = nPA + temp.x;

                bool breakout = false;
                for (int d = 0; d < depth && !breakout; d++)
                {
                    if (piecesGrid[(int)temp.x, d] == null)
                    {
                        temp.y = d;
                        breakout = true;
                    }
                }
                if (!breakout)
                    temp.y = depth;
                piece.GetComponent<Piece>().posOnGrid = temp;
                piecesGrid[(int)temp.x, (int)temp.y] = piece;
                piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
                piece.GetComponent<Piece>().ogPos = piece.GetComponent<Piece>().transform.position;
                piece.GetComponent<Piece>().ogScale = piece.GetComponent<Piece>().transform.localScale;

                float mTtemp = piece.GetComponent<Piece>().moveTime - piece.GetComponent<Piece>().timeElapsed;
                if (mTtemp <= 0)
                {
                    if (piece.GetComponent<Piece>().shrinkTime > 0) //if the piece is still shrinking, match the shrink time
                        piece.GetComponent<Piece>().moveTime = piece.GetComponent<Piece>().shrinkTime;
                    else piece.GetComponent<Piece>().moveTime = 0;
                }
                else
                    piece.GetComponent<Piece>().moveTime = mTtemp;

                float sTtemp = piece.GetComponent<Piece>().shrinkTime - piece.GetComponent<Piece>().timeElapsed;
                if (sTtemp <= 0)
                {
                    if (piece.GetComponent<Piece>().moveTime > 0) //if the piece is still moving, match the movement time
                        piece.GetComponent<Piece>().shrinkTime = piece.GetComponent<Piece>().moveTime;
                    else piece.GetComponent<Piece>().shrinkTime = 0;
                }
                else
                    piece.GetComponent<Piece>().shrinkTime = sTtemp;

                piece.GetComponent<Piece>().timeElapsed = 0;
            }
        }
    }

    public void recalcMovingPieces() { //deprecated
        foreach (GameObject piece in pieces) {
            Vector2 temp = piece.GetComponent<Piece>().posOnGrid;
            piecesGrid[(int)temp.x, (int)temp.y] = null;
            bool breakout = false;
            for (int d = 0; d < depth && !breakout; d++)
            {
                if (piecesGrid[(int)temp.x, d] == null)
                {
                    temp.y = d;
                    breakout = true;
                }
            }
            piece.GetComponent<Piece>().posOnGrid = temp;
            piecesGrid[(int)temp.x, (int)temp.y] = piece;
            piece.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(piece);
        }
    }

    public void removePiece(GameObject piece)
    {
        //Debug.Log("added " + piece.GetComponent<Piece>().posOnGrid.x + ", " + piece.GetComponent<Piece>().posOnGrid.y + " to remove list.");
        removeList[(int)piece.GetComponent<Piece>().posOnGrid.x, (int)piece.GetComponent<Piece>().posOnGrid.y] = piece;
    }

    public void removePiece() {
        for (int x = 0; x < nPA; x++)
        {
            int tally = 0;
            for (int y = 0; y < depth; y++)
            {
                //tallies the number of pieces that still exist in this column. used to determine where pieces should fall
                if (removeList[x, y] != null)
                {
                    GameObject piece = removeList[x, y];

                    outlines.Remove(piece);
                    spawnParticles(piece);

                    piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, (int)piece.GetComponent<Piece>().posOnGrid.y] = null;
                    pieces.Remove(piece);
                    Destroy(piece);

                    removeList[x, y] = null;
                    //Debug.Log("removed " + x + ", " + y);
                    //recalcMovingPieces();
                }
                else if(piecesGrid[x, y] != null)
                { //nested ifs so tally is increased for every piece in the column that exists and is not being removed, while only setting a piece to fall if it needs to
                    if (y != tally && getPieceComp(x, y).lockedOnce)
                    {
                        Debug.Log("falling piece " + x + ", " + y);
                        GameObject p = piecesGrid[x, y];
                        piecesGrid[x, y] = null;
                        p.GetComponent<Piece>().shrinkTime = 1;
                        p.GetComponent<Piece>().moveTime = 1;
                        p.GetComponent<Piece>().timeElapsed = 0;
                        p.GetComponent<Piece>().ogScale = p.transform.localScale;
                        p.GetComponent<Piece>().ogPos = p.transform.position;
                        p.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(p);
                        p.GetComponent<Piece>().locked = false;
                        p.GetComponent<Piece>().posOnGrid = new Vector2(x, tally);
                        piecesGrid[x, tally] = p;
                        pieces.Add(p);
                    }
                    tally++;
                }
            }
        }


        //if (!piece.GetComponent<Piece>().locked)
        //{
        //    piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, (int)piece.GetComponent<Piece>().posOnGrid.y] = null;
        //    pieces.Remove(piece);
        //    Destroy(piece);
        //}
        //else
        //{
        //    piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, (int)piece.GetComponent<Piece>().posOnGrid.y] = null;
        //    pieces.Remove(piece);
        //    Destroy(piece);
        //    List<GameObject> fallingPieces = new List<GameObject>();
        //    for (int d = (int)piece.GetComponent<Piece>().posOnGrid.y + 1; d < depth; d++)
        //    {
        //        if (piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, d] != null && piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, d].GetComponent<Piece>().lockedOnce)
        //        {
        //            fallingPieces.Add(piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, d]);
        //        }
        //    }

        //    foreach (GameObject p in fallingPieces)
        //    {
        //        piecesGrid[(int)p.GetComponent<Piece>().posOnGrid.x, (int)p.GetComponent<Piece>().posOnGrid.y] = null;
        //        p.GetComponent<Piece>().shrinkTime = 1;
        //        p.GetComponent<Piece>().moveTime = 1;
        //        p.GetComponent<Piece>().timeElapsed = 0;
        //        p.GetComponent<Piece>().ogScale = p.transform.localScale;
        //        p.GetComponent<Piece>().ogPos = p.transform.position;
        //        p.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(p);
        //        p.GetComponent<Piece>().locked = false;
        //        p.GetComponent<Piece>().posOnGrid = new Vector2(p.GetComponent<Piece>().posOnGrid.x, p.GetComponent<Piece>().posOnGrid.y - 1);
        //        piecesGrid[(int)p.GetComponent<Piece>().posOnGrid.x, (int)p.GetComponent<Piece>().posOnGrid.y] = p;
        //        pieces.Add(p);
        //    }
        //}

    }

    public void outlinePiece(GameObject piece, bool enabled) {
        if (piece.GetComponent<Piece>().outlineEnabled != enabled) {
            if (piece.GetComponent<Piece>().outlineEnabled) {
                //Destroy(piece.transform.GetChild(0));
                piece.GetComponent<Piece>().fadeTimeElapsed = 0;
                piece.GetComponent<Piece>().outlineEnabled = false;
            }
            else
            {
                //GameObject smallPiece = Instantiate(piece);
                //smallPiece.transform.SetParent(piece.transform, true);
                //smallPiece.transform.localScale = smallPiece.transform.localScale * (1-outlineConst);
                //float magnitude = ((piece.GetComponent<Piece>().outerRadius - piece.GetComponent<Piece>().innerRadius) * piece.transform.localScale.x * outlineConst * 0.5F) + piece.GetComponent<Piece>().innerRadius * outlineConst;
                //float theta = piece.transform.rotation.z + piece.GetComponent<Piece>().offsetRotation + piece.GetComponent<Piece>().totalAngle / 2;
                //float x = smallPiece.transform.localPosition.x + magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
                //float y = smallPiece.transform.localPosition.y + magnitude * Mathf.Sin(Mathf.Deg2Rad * theta);
                //Debug.Log(theta + ", " + x + ", " + y);

                //smallPiece.transform.localPosition = new Vector3(x, y, 0);

                GameObject smallPiece = Instantiate(piece);
                smallPiece.transform.SetParent(piece.transform);
                smallPiece.GetComponent<Piece>().innerRadius += outlineConst; //assumes that outerRad - innerRad == 1
                smallPiece.GetComponent<Piece>().outerRadius -= outlineConst;
                smallPiece.GetComponent<Piece>().offsetRotation += smallPiece.GetComponent<Piece>().totalAngle * (outlineConst / 4F);
                smallPiece.GetComponent<Piece>().totalAngle *= (1 - outlineConst * 0.5F);

                Color col = piece.GetComponent<Piece>().color;
                Color darkCol = new Color(col.r * 0.75F, col.g * 0.75F, col.b * 0.75F);
                piece.GetComponent<MeshRenderer>().material.color = darkCol;
                piece.GetComponent<Piece>().outlineColor = darkCol;
                smallPiece.GetComponent<MeshRenderer>().material.color = col;
                piece.GetComponent<MeshRenderer>().material.renderQueue = 1; //2d sprites on same render layer will additively blend, we don't want that
                piece.GetComponent<Piece>().outlineEnabled = true;
                outlines.Add(piece);
            }
        }
    }

    public void fadeOutline(GameObject piece) {
        if (piece.GetComponent<Piece>().fadeTimeElapsed < piece.GetComponent<Piece>().fadeTime) {
            piece.GetComponent<Piece>().fadeTimeElapsed += Time.deltaTime;
            //Debug.Log(piece.GetComponent<Piece>().fadeTimeElapsed);
            float scalar = piece.GetComponent<Piece>().fadeTimeElapsed / piece.GetComponent<Piece>().fadeTime;
            if (scalar > 1)
            {
                scalar = 1;
                outlinesRemove.Add(piece);
                Destroy(piece.transform.GetChild(0).gameObject);
            }

            piece.GetComponent<MeshRenderer>().material.color = piece.GetComponent<Piece>().outlineColor + (piece.GetComponent<Piece>().color - piece.GetComponent<Piece>().outlineColor) * scalar;
            Color col = piece.GetComponent<MeshRenderer>().material.color;
            col = new Color(col.r, col.g, col.b, 1);
            piece.GetComponent<MeshRenderer>().material.color = col;
        }
    }

    public void scorePoints(int piecesCleared) {
        if (piecesCleared >= 3)
        {
            score += (int) Mathf.Pow(piecesCleared - 2, 2) + 2;
            GameObject.FindGameObjectWithTag("scoreText").GetComponent<Text>().text = "" + score;
        }
    }

    public void spawnParticles(GameObject piece) {
        Piece component = piece.GetComponent<Piece>();
        foreach (Vector2 point in component.points) {
            GameObject parts = Instantiate(particles, piece.transform, false);
            parts.transform.localPosition = point;
            parts.transform.localScale = new Vector3(0.25F, 0.25F, 0.25F);
            parts.transform.SetParent(piece.transform.parent, true);
            ParticleSystem.MainModule psMain = parts.GetComponent<ParticleSystem>().main;
            psMain.startColor = piece.GetComponent<Piece>().color;
        }
    }

    public void scoreProgressionUpdate() {
        if(colors.Length < 5 && score > 5) {
            addColor("#ff69b4");
            zoomCamera(1.0F, 2);
        }

        zoomCamera();
    }

    public void addColor(string hex) {
        string[] temp = new string[colors.Length + 1];
        for (int c = 0; c < colors.Length; c++) {
            temp[c] = colors[c];
        }
        temp[temp.Length - 1] = hex;
        colors = temp;
    }

    public void zoomCamera(float zoomDiff, int timeToMove) {
        camZoomTimeElapsed = 0;
        camZoomStart = Camera.main.orthographicSize;
        camZoomDest = camZoomStart + zoomDiff;
        camTimeToMove = timeToMove;
    }

    public void zoomCamera() {
        if (camZoomTimeElapsed < camTimeToMove) {
            camZoomTimeElapsed += Time.deltaTime;
        }
        float scalar = camZoomTimeElapsed / camTimeToMove;
        scalar = quadEasing(scalar);
        if (scalar < 1)
        {
            Camera.main.orthographicSize = (scalar * (camZoomDest - camZoomStart)) + camZoomStart;
        }
        else { }
    }

    public string randSpecType() {
        string type = "";
        if (Random.Range(0F, 1F) < 0.25F) {
            float val = Random.Range(0F, 1F);
            for (int t = 0; t < specialTypes.Length; t++) {
                if (val >= (1F / (float)specialTypes.Length) * t && val < (1F / (float)specialTypes.Length) * (t+1)) {
                    type = specialTypes[t];
                }
            }
        }
        return type;
    }
}
