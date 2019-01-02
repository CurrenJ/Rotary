using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public int rotationPosition;
    public GameObject lockedContainer;
    private List<GameObject> removeList;
    public GameObject debugPiece;
    public string[] colors;
    private float turnTimeElapsed;
    public float turnTimeDefault;
    public float turnTime;
    public float lastRotationAngle;
    public float currentRotationAngle;

    private const float DEG2RAD = Mathf.PI / 180;
    private const float pieceOffset = 45;
    private const float spawnDistance = 10;

    // Use this for initialization
    void Start()
    {
        pieces = new List<GameObject>();
        removeList = new List<GameObject>();
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
        handleInput();

        turnTimeElapsed += Time.deltaTime;
        smoothRotateUpdate();

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
                        //if turnTimeDefault is a decent speed, then users keeping pieces unlocked by spamming the turn shouldn't be an issue. also I need to handle users turning falling pieces into existing locked pieces. what should happen? should the core be blocked from turning? piece deleted? or does the user lose?
                        //removePiece(piece); 
                    }
                }
                else
                    movePiece(piece, piece.GetComponent<Piece>().moveTime);
            }
        }
        foreach (GameObject piece in removeList)
        {
            pieces.Remove(piece);
        }
        removeList.Clear();


        randomTimer += Time.deltaTime;
        if (randomTimer >= 3 && random)
        {
            Color c;
            ColorUtility.TryParseHtmlString(colors[Random.Range(0, colors.Length)], out c);
            while (!createPiece(Random.Range(0, nPA), 5, 2, c))
            {
                // float shrinkT = Random.Range(2, 5);

            }
            randomTimer = 0;
        }
    }

    private void handleInput()
    {

        /** PC Input Handling **/
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            if (Input.GetKeyDown(KeyCode.R))
                rotateByPos(1);
            else if (Input.GetKeyDown(KeyCode.P))
            {
                for (int d = 0; d < depth; d++)
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
        piece.transform.position = core.transform.position;
        float matchScale = piece.GetComponent<Piece>().adjacentRadOnGrid / piece.GetComponent<Piece>().innerRadius;
        piece.transform.localScale = new Vector3(matchScale, matchScale, 1);
        piece.transform.GetComponent<Piece>().lockPiece();
        float tempAng = piece.transform.eulerAngles.z;
        piece.transform.SetParent(lockedContainer.transform, true);
        //piece.transform.eulerAngles = new Vector3(0, 0, tempAng + (currentRotationAngle - (rotationPosition * (360F / nPA)) ));
        removeList.Add(piece);

        checkForMatches(piece);
        //Debug.Log("Piece locked. Position identifier: " + piece.transform.position + " Scale: " + matchScale);
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
            //Debug.Log("C: " + c.ToString() + " | " + getPieceComp(x + a, y).color.ToString() + " | " + getPieceComp(x + a, y).color.ToString().Equals(c.ToString()) + " | " + getPieceComp(x + a, y).locked);
            if (getPieceComp(x + a, y) != null && getPieceComp(x + a, y).locked && getPieceComp(x + a, y).color.ToString().Equals(c.ToString()))
            {
                pTRhori.Add(getPiece(x + a, y));
            }
            else breakoutA = true;
        }

        bool breakoutB = false;
        for (int a = 1; a < nPA / 2 && !breakoutB; a++)
        {
            //Debug.Log("C: " + c.ToString() + " | " + getPieceComp(x - a, y).color.ToString() + " | " + getPieceComp(x - a, y).color.ToString().Equals(c.ToString()) + " | " + getPieceComp(x - a, y).locked);
            if (getPieceComp(x - a, y) != null && getPieceComp(x - a, y).locked && getPieceComp(x - a, y).color.ToString().Equals(c.ToString()))
            {
                pTRhori.Add(getPiece(x - a, y));
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

        if (pTRhori.Count > 2)
        {
            foreach (GameObject p in pTRhori)
            {
                removePiece(p);
            }
            recalcMovingPieces();
        }
        if (pTRvert.Count > 2)
        {
            foreach (GameObject p in pTRvert)
            {
                removePiece(p);
            }
            recalcMovingPieces();
        }
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
        piece.GetComponent<MeshRenderer>().material = materialRed;
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
        piece.GetComponent<MeshRenderer>().material = materialRed;
        piece.gameObject.transform.position = core.transform.position;
        piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
        piece.GetComponent<Piece>().ogPos = piece.transform.position;
        piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
        piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
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
            piece.GetComponent<MeshRenderer>().material = materialRed;
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(x, y, 0);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
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
            piece.GetComponent<MeshRenderer>().material = materialRed;
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, 0);
            Debug.Log("Pos: " + (posArAdjusted) + " Deg  (rads): " + ((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) + " X: " + Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5 + " Y: " + Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
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
            piece.GetComponent<MeshRenderer>().material = materialRed;
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * spawnDistance, 0);
            //Debug.Log("Pos: " + (posArAdjusted) + " Deg  (rads): " + ((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) + " X: " + Mathf.Cos(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5 + " Y: " + Mathf.Sin(((posArAdjusted) * (360F / nPA) + pieceOffset + (360F / nPA / 2)) * DEG2RAD) * 5);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            piece.GetComponent<Piece>().setColor(color);
            piece.GetComponent<Piece>().color = color;
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
            piece.GetComponent<MeshRenderer>().material = materialRed;
            piece.gameObject.transform.position = core.transform.position;
            piece.GetComponent<Piece>().ogScale = piece.transform.localScale;
            piece.transform.position += new Vector3(x, y, 0);
            piece.GetComponent<Piece>().ogPos = piece.transform.position;
            piece.GetComponent<Piece>().offsetRotation += rotationPosition * (360F / nPA);
            piece.GetComponent<Piece>().innerSmoothness = core.GetComponent<Piece>().outerSmoothness / 4;
            piece.GetComponent<Piece>().outerSmoothness = piece.GetComponent<Piece>().innerSmoothness;
            piece.GetComponent<Piece>().setOrientation((360F / nPA) * positionAround, (360F / nPA));
            piece.GetComponent<Piece>().moveTime = mT;
            piece.GetComponent<Piece>().shrinkTime = sT;
            piece.GetComponent<Piece>().setColor(color);
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
        piecesGrid = new GameObject[nPA, depth];
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

    public void recalcMovingPieces() {
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
        if (!piece.GetComponent<Piece>().locked)
        {
            piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, (int)piece.GetComponent<Piece>().posOnGrid.y] = null;
            pieces.Remove(piece);
            Destroy(piece);
        }
        else
        {
            piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, (int)piece.GetComponent<Piece>().posOnGrid.y] = null;
            pieces.Remove(piece);
            Destroy(piece);
            List<GameObject> fallingPieces = new List<GameObject>();
            for (int d = (int)piece.GetComponent<Piece>().posOnGrid.y + 1; d < depth; d++)
            {
                if (piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, d] != null && piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, d].GetComponent<Piece>().lockedOnce)
                {
                    fallingPieces.Add(piecesGrid[(int)piece.GetComponent<Piece>().posOnGrid.x, d]);
                }
            }

            foreach (GameObject p in fallingPieces)
            {
                piecesGrid[(int)p.GetComponent<Piece>().posOnGrid.x, (int)p.GetComponent<Piece>().posOnGrid.y] = null;
                p.GetComponent<Piece>().shrinkTime = 1;
                p.GetComponent<Piece>().moveTime = 1;
                p.GetComponent<Piece>().timeElapsed = 0;
                p.GetComponent<Piece>().ogScale = p.transform.localScale;
                p.GetComponent<Piece>().ogPos = p.transform.position;
                p.GetComponent<Piece>().adjacentRadOnGrid = getOuterRadius(p);
                p.GetComponent<Piece>().locked = false;
                p.GetComponent<Piece>().posOnGrid = new Vector2(p.GetComponent<Piece>().posOnGrid.x, p.GetComponent<Piece>().posOnGrid.y - 1);
                piecesGrid[(int)p.GetComponent<Piece>().posOnGrid.x, (int)p.GetComponent<Piece>().posOnGrid.y] = p;
                pieces.Add(p);
            }
        }
    }
}
