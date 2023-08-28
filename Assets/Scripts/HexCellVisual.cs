using UnityEngine;

public class HexCellVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer hexCellRenderer;
    [SerializeField] private SpriteRenderer hexCellBoomRenderer;

    [HideInInspector] public bool isDirty = false; //if dirty we need to move this hexcell
    [HideInInspector] public Vector2 FallingPosition = new Vector2(); //which cell position to fall to if dirty
    Color hexCellColor;

    public void SetFallingPosition()
    {
        FallingPosition = transform.position;
    }

    public Color GetHexCellColor()
    {
        return hexCellColor;
    }

    public void SetHexCellColor(Color c)
    {
        hexCellColor = c;
        hexCellRenderer.color = hexCellColor;
    }

    public void SetBoomVisual(bool isBoom)
    {
        hexCellRenderer.enabled = !isBoom;
        hexCellBoomRenderer.enabled = isBoom;
    }

}