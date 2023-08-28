using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;


    [Header("Prefab Setup")]
    [SerializeField]
    private GameObject _hexCellVisualPrefab;

    [SerializeField] private GameObject _hexCellPrefab;
    [SerializeField][Required] private Transform _hexGridParent;
    [SerializeField][Required] private Transform _hexCellVisualParent;

    [Header("Grid Setup")] public Vector2 gridOffset; //position of the full grid
    public float TopGridYPos; //hexcells drop from this position on top
    public int maximumMoves; //game gets over ones we reach this many moves
    public Color[] blockColors; //set different colors for the hexcells in grid, min 3
    public Vector2 gridDimensions; //grid size colum x row
    public float LengthOfHex; //this is the length of one side of the hexCell


    [Header("UI Setup")]
    [SerializeField] private TextMeshProUGUI moveLeftTxt;
    [SerializeField] private TextMeshProUGUI scoreTxt;
    [SerializeField] private TextMeshProUGUI comboTxt;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Animation Setup")]
    [SerializeField] private float removeAnimationTime = 1f;


    bool IsHoldingLeftMouse = false;
    [ShowInInspector][ReadOnly] List<HexCell> selectedCells = new List<HexCell>(); //list of selected cells (those which we have dragged over)
    List<HexCell> allHexCells = new List<HexCell>(); //list of all the cells in the grid
    List<HexCellVisual> allHexCellVisuals = new List<HexCellVisual>(); //list of all the hexcells in grid
    bool _isCanDraw = false; //boolean to enable/disable user interaction
    Color _currentSelectedColor; //which color hexcells are currently being selected
    int _minSelectedTiles = 3; //number of tiles that need to be selected for a successfull collapse (min 3)


    Vector2 prevCellPos = new Vector2(-1000, -1000);
    Vector2 mouseOffsetPos;
    bool _isGameOver = false;
    int movesLeft;
    int score;
    int currentComboValue = 0;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        movesLeft = maximumMoves; //set maximum available moves
        score = 0;

        GenerateHexCellGrid(); //create the grid & add hexcells
        UpdateUI(); //set UI values

        Camera.main.aspect = 1080f / 1920.0f;

        _isCanDraw = true; //enable user interaction
    }

    void UpdateUI()
    {
        gameOverPanel.SetActive(false); //hide gameOverPanel

        moveLeftTxt.text = movesLeft.ToString();
        scoreTxt.text = score.ToString();
        if (currentComboValue > 1)
        {
            comboTxt.text = currentComboValue.ToString() + "Combos";
            comboTxt.gameObject.SetActive(true);
        }
        else
        {
            comboTxt.gameObject.SetActive(false);
        }

        SimpleSoundManager.Instance.PlaySound(SoundName.GetScore);
    }

    void GenerateHexCellGrid()
    {
        GameObject hexCell;
        Vector2 axialPoint = new Vector2();
        Vector2 screenPoint;
        GameObject hexCellVisual_GO;
        HexCell hc;
        HexCellVisual hexCellVisual;

        for (var i = 0; i < gridDimensions.y; i++)
        {
            for (var j = 0; j < gridDimensions.x; j++)
            {
                axialPoint.x = i;
                axialPoint.y = j;
                //convert offset points to axial points
                axialPoint = HexGridUtils.OffsetToAxial(axialPoint);
                //convert axial points to screen points
                screenPoint = HexGridUtils.AxialToScreen(axialPoint, LengthOfHex);
                //add the grid offset value to position the grid
                screenPoint.x += gridOffset.x;
                screenPoint.y += gridOffset.y;

                hexCell = Instantiate(_hexCellPrefab, screenPoint, Quaternion.identity, _hexGridParent);
                hexCell.name = "hexCell" + i.ToString() + "_" + j.ToString();
                hc = hexCell.GetComponent<HexCell>();
                hc.axialCoordinate = axialPoint;

                allHexCells.Add(hc);

                //create visual of hexcell
                hexCellVisual_GO = Instantiate(_hexCellVisualPrefab, screenPoint, Quaternion.identity, _hexCellVisualParent);
                hexCellVisual = hexCellVisual_GO.GetComponent<HexCellVisual>();

                //set random color to hexcell
                int colorIdx = Random.Range(0, blockColors.Length);
                hexCellVisual.SetHexCellColor(blockColors[colorIdx]);

                allHexCellVisuals.Add(hexCellVisual);
                //assign hexcell visual to its cell
                hc.SetHexCellVisual(hexCellVisual);
                hc.IsBoom = false;
            }
        }
    }

    GameObject currentHexCell;
    void Update()
    {
        if (_isGameOver) return;

        if (!_isCanDraw)
        {
            return;
        }

        if (movesLeft <= 0)
        {
            ShowGameOver();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            IsHoldingLeftMouse = true;
            selectedCells.Clear(); 
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDraw(); //check the chain of selected hexcells
        }

        if (IsHoldingLeftMouse)
        {
            //get offset coordinates of the cell under mouse position
            mouseOffsetPos = HexGridUtils.AxialToOffset(FindCubicHexCell());
            if (!Vector2.Equals(prevCellPos, mouseOffsetPos))
            {
                //if we draw outside the grid, stop
                if (!IsIndexInGrid(mouseOffsetPos.x, mouseOffsetPos.y))
                {
                    StopDraw();
                    return;
                }

                currentHexCell = _hexGridParent.transform.Find("hexCell" + mouseOffsetPos.x.ToString() + "_" + mouseOffsetPos.y.ToString()).gameObject;
                HexCell hexCell = currentHexCell.GetComponent<HexCell>();
                /*if (hexCell != null)
                    UpdateSelectedCells(hexCell);*/

                if (hexCell != null)
                {
                    if (hexCell.IsBoom)
                    {
                        StopDrawWithBoom(hexCell);
                    }
                    else
                    {
                        UpdateSelectedCells(hexCell);
                    }
                }
            }
        }
    }

    public HexCell FindHexCellFromAxial(Vector2 axialPoint)
    {
        HexCell hc = null;
        for (var i = 0; i < allHexCells.Count; i++)
        {
            hc = allHexCells[i];
            if (hc.axialCoordinate.Equals(axialPoint))
            {
                return hc;
            }
        }
        return null;
    }

    void UpdateSelectedCells(HexCell hc)
    {
        if (selectedCells.Count == 0)
        {
            prevCellPos = mouseOffsetPos;
            selectedCells.Add(hc);
            _currentSelectedColor = hc.HexCellVisual.GetHexCellColor();
            
            SimpleSoundManager.Instance.PlaySound(SoundName.SelectedBlock);
        }
        else
        {
            if (hc.HexCellVisual.GetHexCellColor() != _currentSelectedColor || !IsNeighborToLastSelectedCell(hc))
            {
                return;
            }

            prevCellPos = mouseOffsetPos;
            //we need to find it the tile is already present in the selected list.
            bool alreadyPresent = false;
            HexCell presentCell;
            int cellIndex = -1;
            for (var i = 0; i < selectedCells.Count; i++)
            {
                presentCell = selectedCells[i];
                if (hc.Equals(presentCell))
                {
                    alreadyPresent = true;
                    cellIndex = i;
                }
            }

            if (!alreadyPresent)
            {
                //this cell is not in the list, add it to the list
                selectedCells.Add(hc);
                SimpleSoundManager.Instance.PlaySound(SoundName.SelectedBlock);
            }
            else
            {
                //this cell is present in the selected list
                if (cellIndex == selectedCells.Count - 2)
                {
                    //the cell is the previous cell, so we are going back, we need to remove the cell from the list.
                    selectedCells.RemoveAt(selectedCells.Count - 1);
                    SimpleSoundManager.Instance.PlaySound(SoundName.RemovedBlock);
                }
                else
                {
                    //when reaching an already selected cell we can end the line or ignore it
                    StopDraw();
                }
            }
        }

        DrawLineAndHiglithCells(); //draw grid
    }

    void DrawLineAndHiglithCells()
    {
        ResetAllCells();
        HexCell hexCell;

        for (var i = 0; i < selectedCells.Count; i++)
        {
            //highlight & draw lines for selected cells
            hexCell = selectedCells[i];
            if (i > 0)
            {
                //find direction & draw incoming line as this is not the first cell
                hexCell.DrawLineFrom(GetNeighborIndex(hexCell, selectedCells[i - 1]));
            }

            if (i < selectedCells.Count - 1)
            {
                //find direction & draw outgoing line as this is not the last cell
                hexCell.DrawLineTo(GetNeighborIndex(hexCell, selectedCells[i + 1]));
            }

            hexCell.SetHexCellHighlight(); //highlight the cell
        }
    }

    int GetNeighborIndex(HexCell hCell, HexCell neighborCell)
    {
        /* Finds the direction of the hexagonal neighbor so that we can align the line arrows.
         * we use a clockwise sequence to determine neighbors ie, return value 0-5 represent top,top right,bottom right,bottom,bottom left,top left neighbor
         */
        int index = -1;
        Vector2 axialPoint = hCell.axialCoordinate;
        Vector2 thisNeighbourPoint = neighborCell.axialCoordinate;
        List<Vector2> neighbors = HexGridUtils.GetAllNeighbors(axialPoint);
        for (int i = 0; i < neighbors.Count; i++)
        {
            //compare axial coordinates to find the neighbor index
            if (Vector2.Equals(neighbors[i], thisNeighbourPoint))
            {
                index = i;
                break;
            }
        }

        return index;
    }

    void StopDraw()
    {
        CheckCells(); //check logic conditions for successful selection
        IsHoldingLeftMouse = false;
        prevCellPos = new Vector2(-1000, -1000);
    }

    public void StopDrawWithBoom(HexCell boomCell)
    {
        movesLeft--;
        score += 10;
        Debug.Log("Boom: " + "Add Score: " + 10);

        UpdateUI();
        _isCanDraw = false;
        IsHoldingLeftMouse = false;
        prevCellPos = new Vector2(-1000, -1000);

        PlayBoomAnimation(boomCell);
    }

    void CheckCells()
    {
        if (!IsHoldingLeftMouse) return;
        if (selectedCells.Count < _minSelectedTiles)
        {
            //reset if we selected less than min number tiles 
            ResetAllCells();
        }
        else
        {
            //successful selection, add score and remove tiles
            movesLeft--;
            currentComboValue = selectedCells.Count / 3;
            //update scores
            int addScore = 0;
            if (currentComboValue > 1)
            {
                addScore = currentComboValue * selectedCells.Count;
            }
            else
            {
                addScore = selectedCells.Count;
            }
            score += addScore;
            Debug.Log("Combo: " + currentComboValue + "Add Score: " + addScore);

            UpdateUI(); //show new score values
            _isCanDraw = false; //disable user interaction while hexcell are moving down

            HexCell presentCell;
            for (var i = 0; i < selectedCells.Count; i++)
            {
                /*//set all selected HexCell as dirty, so that they needs to remove
                presentCell = selectedCells[i];
                presentCell.HexCellVisual.isDirty = true;*/

                presentCell = selectedCells[i];
                if (selectedCells.Count >= 5 && i == selectedCells.Count - 1)
                {
                    presentCell.SetHexCellIsBoom();
                    //play animation create boom block
                    Vector3 _originalScale = presentCell.HexCellVisual.transform.localScale;
                    presentCell.HexCellVisual.gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    presentCell.HexCellVisual.gameObject.transform.DOScale(_originalScale, removeAnimationTime * 0.5f).SetEase(Ease.InOutElastic);
                }
                else
                {
                    //set all selected HexCell as dirty, so that they needs to remove
                    presentCell = selectedCells[i];
                    presentCell.HexCellVisual.isDirty = true;
                }
            }

            CheckAndPlayAnimations().Forget(); //find all HexCell above these dirty HexCell so that they can all drop
            //playSoundFx(successSnd);
        }
    }

    private void PlayBoomAnimation(HexCell boomCell)
    {
        _isCanDraw = false;

        boomCell.IsBoom = false;
        boomCell.HexCellVisual.isDirty = true;

        List<Vector2> neighbors = HexGridUtils.GetAllNeighbors(boomCell.axialCoordinate);
        foreach (Vector2 axialPoint in neighbors)
        {
            var cell = GameManager.Instance.FindHexCellFromAxial(axialPoint);
            if (cell != null)
            {
                cell.HexCellVisual.isDirty = true;
            }
        }

        CheckAndPlayAnimations().Forget();

        SimpleSoundManager.Instance.PlaySound(SoundName.Boom);
    }

    private async UniTask CheckAndPlayAnimations()
    {
        //play remove animation for all selected HexCell
        HexCell currentCell;
        HexCellVisual hexCellVisual;
        for (var i = 0; i < allHexCells.Count; i++)
        {
            currentCell = allHexCells[i];
            hexCellVisual = currentCell.HexCellVisual;
            if (hexCellVisual.isDirty)
            {
                hexCellVisual.gameObject.transform.DOPunchScale(new Vector3(50, 50, 50), removeAnimationTime, 5);
            }
        }
        await UniTask.Delay((int)(removeAnimationTime * 1000));


        /*
         * For better performance, we don't destroy any HexCell, we just move them up.
         * if there non dirty HexCell on top, then we set a new random color & move this HexCell on top of the grid. 
         * it will then fall down to its original position.
		 */
        
        HexCell topCell;
        bool foundReplacement;
        for (var i = 0; i < allHexCells.Count; i++)
        {
            //find all dirty HexCell from Bottom Left to TopRight
            currentCell = allHexCells[i];
            hexCellVisual = currentCell.HexCellVisual;
            foundReplacement = false;
            if (hexCellVisual.isDirty)
            {
                //look for non dirty HexCell on top of this HexCell
                for (int j = i + (int)gridDimensions.x; j < allHexCells.Count; j += (int)gridDimensions.x)
                {
                    topCell = allHexCells[j];
                    if (!topCell.HexCellVisual.isDirty)
                    {
                        //found a non dirty cell on top
                        //make our dirty HexCell as a copy of the top non dirty HexCell with same color and position
                        topCell.HexCellVisual.isDirty = true;
                        hexCellVisual.SetHexCellColor(topCell.HexCellVisual.GetHexCellColor());
                        currentCell.IsBoom = topCell.IsBoom;

                        hexCellVisual.SetFallingPosition();
                        hexCellVisual.gameObject.transform.position = topCell.HexCellVisual.gameObject.transform.position;

                        foundReplacement = true;
                        break;
                    }
                }

                if (!foundReplacement)
                {
                    //there was no non dirty hexcell on top of this dirty hexcell, so we place it in top of grid & assign random color
                    int randomColor = Random.Range(0, blockColors.Length);
                    hexCellVisual.SetHexCellColor(blockColors[randomColor]); //set random color
                    currentCell.IsBoom = false;

                    hexCellVisual.SetFallingPosition();
                    hexCellVisual.gameObject.transform.position = new Vector2(hexCellVisual.gameObject.transform.position.x, TopGridYPos);
                }
            }
        }

        ResetAllCells();

        PlayFallDownAnimation().Forget();
    }

    async UniTask PlayFallDownAnimation()
    {
        bool fallingAnimationDone = true;
        int _fallingDownHexCellCount = 0;
        foreach (HexCellVisual hexCellVisual in allHexCellVisuals)
        {
            if (hexCellVisual.isDirty)
            {
                _fallingDownHexCellCount++;

                hexCellVisual.gameObject.transform.DOMove(hexCellVisual.FallingPosition, 1f).SetEase(Ease.OutBounce)
                    .OnComplete(() =>
                    {
                        _fallingDownHexCellCount--;
                        hexCellVisual.isDirty = false; //reset
                        if (_fallingDownHexCellCount == 0)
                        {
                            //all hexcell have fallen down
                            fallingAnimationDone = true;
                        }
                    });
            }
        }

        SimpleSoundManager.Instance.PlaySound(SoundName.BlockFallDown);

        await UniTask.WaitUntil(() => fallingAnimationDone);
        await UniTask.DelayFrame(1);
        _isCanDraw = true; //enable user input when all hexcell have fallen to place
    }

    bool IsNeighborToLastSelectedCell(HexCell hc)
    {
        //check if this cell is neighbor to last cell
        bool notNeighbor = true;
        HexCell lastSelectedCell = selectedCells[selectedCells.Count - 1]; //find the last cell in the line from selected list
        List<Vector2> neighbors = HexGridUtils.GetAllNeighbors(lastSelectedCell.axialCoordinate); //get all neighbors for lastSelectedCell
        foreach (Vector2 axPt in neighbors)
        {
            //compare the axial coordinates to see if this one is a neighbour
            if (Vector2.Equals(hc.axialCoordinate, axPt))
            {
                notNeighbor = false;
                break;
            }
        }

        return !notNeighbor;
    }

    void ResetAllCells()
    {
        foreach (HexCell hc in allHexCells)
        {
            //reset all cells
            hc.ResetHexCell();
        }
    }

    Vector2 FindCubicHexCell()
    {
        //find the cell under mouse clicked
        var pos = Input.mousePosition;
        pos = Camera.main.ScreenToWorldPoint(pos); //convert mouse position to world position
        pos.x -= gridOffset.x;
        pos.y -= gridOffset.y;
        return HexGridUtils.ScreenToAxial(pos, LengthOfHex); //find axial coordinates
    }

    bool IsIndexInGrid(float i, float j)
    {
        //check if the index values are within grid dimensions
        if (i < 0 || j < 0 || i >= gridDimensions.y || j >= gridDimensions.x)
        {
            return false;
        }

        return true;
    }

    //set event on restart btn
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ShowGameOver()
    {
        SimpleSoundManager.Instance.PlaySound(SoundName.GameOver);
        gameOverPanel.SetActive(true);
        _isGameOver = true;
    }


}