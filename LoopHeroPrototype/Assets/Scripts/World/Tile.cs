using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Road,
    Roadside,
    Landscape, 
    Special, 
    Golden
}

[CreateAssetMenu(menuName = "Assets/Tile")]
public class Tile : ScriptableObject
{
    [Header("Nature")]
    public bool isEmpty;
    public TileType tileType;

    [Header("FeedBack")]
    public Sprite tileSprite;
    public Sprite emptySprite;

    public int indexX;
    public int indexY;


    public void TileEffect()
    {

    }
}

