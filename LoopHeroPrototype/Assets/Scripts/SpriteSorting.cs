using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.IO;

public class SpriteSorting : MonoBehaviour
{

    private struct Pixel : IComparable<Pixel>
    {
        public int x { get; set; }
        public int y { get; set; }

        public Color color { get; set; }
        public float grayscale { get; set; }

        public Pixel(int x, int y, Color color)
        {
            this.x = x;
            this.y = y;
            this.color = color;

            grayscale = color.grayscale; 
        }

        public int CompareTo(Pixel other)
        {
            if (other.grayscale > grayscale)
                return -1;
            else if(other.grayscale < grayscale)
                return 1;

            return 0;
        }
    }

    [Header("Input")]
    public Sprite sprite;
    private Sprite targetSprite;
    private Texture2D tex;
    private Texture2D texCopy;
    private Color[,] pixelColors;
    private Color[,] blackAndWhite;
    private Pixel[,] pixels;


    [Header("Threshold")]
    [Range(0f, 0.5f)]
    public float min;
    [Range(0.5f, 1f)]
    public float max;


    [Header("Output")]
    public GameObject renderedObject;
    public bool process;
    public string path;
    public string outputName;


    private int index;


    [ContextMenu("Process")]
    public void ProcessData()
    {
        Process();
    }


    // Start is called before the first frame update
    void Process()
    {
        //Initialize
        tex = sprite.texture;
        pixelColors = new Color[tex.width, tex.height];
        blackAndWhite = new Color[tex.width, tex.height];
        pixels = new Pixel[tex.width, tex.height];

        //Create an empty copy of the sprite texture
        texCopy = new Texture2D(tex.width, tex.height);
        //targetSprite = Sprite.Create(texCopy, sprite.rect, sprite.pivot);
        renderedObject.GetComponent<SpriteRenderer>().sprite = sprite;

        //Get Colors from texture
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                pixelColors[x, y] = tex.GetPixel(x,y);
                pixels[x, y] = new Pixel(x, y, pixelColors[x,y]);
            }
        }

        //Set to black or white
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                float grayscale = pixelColors[x,y].grayscale;

                if(grayscale < min || grayscale > max)
                {
                    blackAndWhite[x, y] = Color.black;
                }
                if(grayscale >= min && grayscale <= max)
                {
                    blackAndWhite[x, y] = Color.white;
                }
            }
        }

        //reorder down to up
        List<Pixel> interval = new List<Pixel>();
        int startY = 0;
        for (int x = 0; x < tex.width; x++)
        {
            startY = 0;
            interval.Clear();

            for (int y = 0; y < tex.height; y++)
            {
                if (blackAndWhite[x, y] == Color.black)
                {
                    //reorder when pixel is black
                    if(interval.Count > 0)
                    {
                        //sort interval then split new order pixels
                        interval.Sort(); 
                        for (int i = 0; i < interval.Count; i++)
                        {
                            pixels[x, startY + i] = interval[i];
                        }

                        //reset interval list
                        interval.Clear();
                        startY = 0;
                    }    
                }

                if (blackAndWhite[x, y] == Color.white)
                {
                    interval.Add(pixels[x,y]);
                    if (startY == 0)
                        startY = y;
                }

                if(y == tex.height - 1)
                {
                    //sort interval then split new order pixels
                    interval.Sort();
                    for (int i = 0; i < interval.Count; i++)
                    {
                        pixels[x, startY + (i - 1)] = interval[i];
                    }

                    //reset interval list
                    interval.Clear();
                    startY = 0;
                }
            }
        }

        //set reordered pixels to texture
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                if(process)
                    tex.SetPixel(x, y, pixels[x,y].color);
                else
                {
                    tex.SetPixel(x, y, blackAndWhite[x, y]);
                }
            }
        }

        tex.Apply();

        /*path += $"/{outputName}.asset";
        string _path = AssetDatabase.GenerateUniqueAssetPath(path);
        Save
        AssetDatabase.CreateAsset(targetSprite, _path);*/
        AssetDatabase.SaveAssets();

    }
    
}
