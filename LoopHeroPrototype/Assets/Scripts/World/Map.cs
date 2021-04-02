using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Map : MonoBehaviour
{
    [Header("Tiles")]
    public List<TileContainer> tempContainers = new List<TileContainer>();
    public TileContainer[,] tileContainers = new TileContainer[21,12];
    public Tile[,] tiles = new Tile[21,12];

    public Tile emptyTile;
    public Tile roadTile;

    

    // Start is called before the first frame update
    void Start()
    {
        int y = 0;
        int x = 0;
        for (int i = 0; i < tempContainers.Count; i++)
        {
            if (i % 12 == 0 && i != 0)
            {
                x++;
                y = 0;
            }

            if(i %12 != 0)
                y++;
            print(x + " ," + y);
            tileContainers[x, y] = tempContainers[i];
            
        }
        tempContainers.Clear();

        SetMapEmpty();

        CreateRoad();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateRoad()
    {
        //create road and the the tiles to road tiles

        //set all roadside tiles to roadside tiles
    }

    public void SetMapEmpty()
    {
        for (int x = 0; x < 21; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                tileContainers[x, y].tile = emptyTile;
                tileContainers[x, y].UpdateSprite(emptyTile.emptySprite);
            }
        }
    }

}
