using UnityEngine;
using System.Collections.Generic;

public static class HexGridUtils
{
    public static float SqrtOfThree = Mathf.Sqrt(3); 

    /*
     * Convert Screen point to Axial coordinate
     * */
    public static Vector2 ScreenToAxial(Vector2 screenPoint, float sideLength)
    {
        var axialPoint = new Vector2();
        axialPoint.y = screenPoint.x / (1.5f * sideLength);
        axialPoint.x = (screenPoint.y - (screenPoint.x / SqrtOfThree)) / (SqrtOfThree * sideLength);
        var cubicZ = CalculateCubicZ(axialPoint);
        var round_x = Mathf.Round(axialPoint.x);
        var round_y = Mathf.Round(axialPoint.y);
        var round_z = Mathf.Round(cubicZ);
        if (round_x + round_y + round_z == 0)
        {
            screenPoint.x = round_x;
            screenPoint.y = round_y;
        }
        else
        {
            var delta_x = Mathf.Abs(axialPoint.x - round_x);
            var delta_y = Mathf.Abs(axialPoint.y - round_y);
            var delta_z = Mathf.Abs(cubicZ - round_z);
            if (delta_x > delta_y && delta_x > delta_z)
            {
                screenPoint.x = -round_y - round_z;
                screenPoint.y = round_y;
            }
            else if (delta_y > delta_x && delta_y > delta_z)
            {
                screenPoint.x = round_x;
                screenPoint.y = -round_x - round_z;
            }
            else if (delta_z > delta_x && delta_z > delta_y)
            {
                screenPoint.x = round_x;
                screenPoint.y = round_y;
            }
        }

        return screenPoint;
    }

    /*
     * Convert axial coordinate to screen position
     * */
    public static Vector2 AxialToScreen(Vector2 axialPoint, float sideLength)
    {
        var tileY = SqrtOfThree * sideLength * (axialPoint.x + (axialPoint.y / 2));
        var tileX = 3 * sideLength / 2 * axialPoint.y;
        axialPoint.x = tileX;
        axialPoint.y = tileY;
        return axialPoint;
    }

    /*
     * Convert offset coordinate to axial coordinate 
     * */
    public static Vector2 OffsetToAxial(Vector2 offsetPt)
    {
        offsetPt.x = (offsetPt.x - (Mathf.Floor(offsetPt.y / 2)));
        return offsetPt;
    }

    /*
     * Convert axial coordinate to offset coordinate
     * */
    public static Vector2 AxialToOffset(Vector2 axialPt)
    {
        axialPt.x = (axialPt.x + (Mathf.Floor(axialPt.y / 2)));
        return axialPt;
    }

    /*
     * Find the third value of the cubic coordinate with the logic that x+y+z=0
     * */
    public static float CalculateCubicZ(Vector2 newAxialPoint)
    {
        return -newAxialPoint.x - newAxialPoint.y;
    }

    /*
     * Find all neighbors as a list of axial points for the given axial coordinate
     * */
    public static List<Vector2> GetAllNeighbors(Vector2 axialPoint)
    {
        //assign 6 neighbors
        Vector2 neighbourPoint = new Vector2();
        List<Vector2> neighbors = new List<Vector2>();
        neighbourPoint.x = axialPoint.x + 1; //top
        neighbourPoint.y = axialPoint.y;
        neighbors.Add(neighbourPoint);
        neighbourPoint.x = axialPoint.x; //top right
        neighbourPoint.y = axialPoint.y + 1;
        neighbors.Add(neighbourPoint);
        neighbourPoint.x = axialPoint.x - 1; //bottom right
        neighbourPoint.y = axialPoint.y + 1;
        neighbors.Add(neighbourPoint);
        neighbourPoint.x = axialPoint.x - 1; //bottom
        neighbourPoint.y = axialPoint.y;
        neighbors.Add(neighbourPoint);
        neighbourPoint.x = axialPoint.x; //bottom left
        neighbourPoint.y = axialPoint.y - 1;
        neighbors.Add(neighbourPoint);
        neighbourPoint.x = axialPoint.x + 1; //top left
        neighbourPoint.y = axialPoint.y - 1;
        neighbors.Add(neighbourPoint);
        return neighbors;
    }
}