using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    [SerializeField] private Color _HighlightColor; //color used to highlight the hexagon base
    [SerializeField] private Color _NormalColor; //normal color of hexagon base
    [SerializeField] private Color _LineColor; //color used to highlight the line

    public Vector2 axialCoordinate = new Vector2(); //this cell's axial coordinate values

    //sprite renderers of lines and hexagon base sprites
    [SerializeField] private SpriteRenderer _lineStart;
    [SerializeField] private SpriteRenderer _lineEnd;
    [SerializeField] private SpriteRenderer _hexBorder;

    private HexCellVisual _hexCellVisual;
    public HexCellVisual HexCellVisual => _hexCellVisual;

    private bool _isBoom = false;
    [ShowInInspector][ReadOnly]public bool IsBoom
    {
        get => _isBoom;
        set
        {
           _hexCellVisual?.SetBoomVisual(value);
            _isBoom = value;
        }
    }

    //line game objects to hide/show
    GameObject lineEnd_GO;
    GameObject lineStart_GO;


    // Use this for initialization
    void Awake()
    {
        lineStart_GO = _lineStart.gameObject;
        lineEnd_GO = _lineEnd.gameObject;
        _lineStart.color = _LineColor;
        _lineEnd.color = _LineColor;
        ResetHexCell();
    }

    public void SetHexCellVisual(HexCellVisual b)
    {
        //set a bubble to this cell
        _hexCellVisual = b;
    }

    public void ResetHexCell()
    {
        //hide lines & disable highlight
        lineStart_GO.SetActive(false);
        lineEnd_GO.SetActive(false);
        SetHexCellBorderColor(_NormalColor);
    }

    public void SetHexCellHighlight()
    {
        //highlight the hex cell
        SetHexCellBorderColor(_HighlightColor);
    }

    public void SetHexCellIsBoom()
    {
        //set this cell as a boom cell
        Debug.Log($"Set Cell : {this.transform.name} is BOOM");
        IsBoom = true;
    }

    public void DrawLineTo(int index)
    {
        //line is split in half (2 sprites), this sets the rotation of outgoing half
        lineStart_GO.transform.localRotation =
            Quaternion.Euler(0, 0, 90 - (index * 60)); //a rotation in 60 degrees can point in any necessary direction
        lineStart_GO.SetActive(true);
    }

    public void DrawLineFrom(int index)
    {
        //line is split in half (2 sprites), this sets the rotation of incoming half
        lineEnd_GO.transform.localRotation =
            Quaternion.Euler(0, 0, -90 - (index * 60)); //a rotation in 60 degrees can point in any necessary direction
        lineEnd_GO.SetActive(true);
    }

    void SetHexCellBorderColor(Color c)
    {
        _hexBorder.color = c;
    }

    #region Testing

    [Button("BOOM")]
    public void BoomExplore()
    {
        if (IsBoom)
        {
            GameManager.Instance.StopDrawWithBoom(this);
        }
    }

    #endregion

}