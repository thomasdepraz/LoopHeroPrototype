using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;


public class SpriteReader : MonoBehaviour
{

    public struct Pixel
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }

        public Color pixelColor { get; set; }

        public bool _checked;

        public bool partOfMesh;

        public int mesh;

        public List<Pixel> list;
        public Vector3 position { get; set; }

        public bool upClear;
        public bool downClear;
        public bool eastClear;
        public bool westClear;
        public bool northClear;
        public bool southClear;

        public Pixel(int x, int y, int z, Color c)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            pixelColor = c;

            _checked = false;
            partOfMesh = false;

            mesh = 0;

            list = new List<Pixel>();
            position = new Vector3(x, z, y);

            upClear = false;
            downClear = false;
            eastClear = false;
            westClear = false;
            northClear = false;
            southClear = false;
        }
    }

    [Header("Input")]
    public Sprite spriteSheet;
    public int width; //x
    public int length; // y
    public int modelHeight; //z

    [Header("Output")]
    public string outputName;
    public string path;
    private string prefabPath;

    private GameObject emptyParent;
    private GameObject uniqueObjectParent;
    private GameObject terrainObjectParent;

    private Texture2D inputTexture;
    private Texture2D[] textures;

    [Header("Unique Objects")]
    public GameObject[] uniqueObjectsReferences;
    public List<Color> uniqueObjectsColorReferences = new List<Color>();
   
    private Color[,,] color;
    private Pixel[,,] pixels;
    private int differentMeshes = 0;
    private List<Pixel> queue = new List<Pixel>();


    [Header("Terrain")]
    public List<GameObject> terrainReferences = new List<GameObject>();
    public List<Color> terrainColorReferences = new List<Color>();

    private List<Pixel>[] clusters;
    private List<Vector3>[] pointClouds;
    private List<int>[] triangles;

    private List<GameObject> finalObjects = new List<GameObject>();
    private List<Mesh> meshes = new List<Mesh>();


    // Start is called before the first frame update
    void Start()
    {
        prefabPath = path;

        //create object root
        emptyParent = new GameObject();
        emptyParent.name = outputName;
        emptyParent.transform.position = Vector3.zero;

        uniqueObjectParent = new GameObject();
        uniqueObjectParent.transform.SetParent(emptyParent.transform);
        uniqueObjectParent.name = "Unique";

        terrainObjectParent = new GameObject();
        terrainObjectParent.transform.SetParent(emptyParent.transform);
        terrainObjectParent.name = "Terrain";

        color = new Color[modelHeight,width, length];
        pixels = new Pixel[modelHeight, width, length];
        textures = new Texture2D[modelHeight];

        inputTexture = spriteSheet.texture;
        Color[] temp;

        //split spriteSheet
        for (int i = 0; i < modelHeight; i++)
        {
            temp = inputTexture.GetPixels(0, i * length, width, length);
            textures[i] = new Texture2D(width, length);
            textures[i].SetPixels(temp);
        }

        //get every pixel color
        for (int i = 0; i < modelHeight; i++)
        {
            for (int j = 0; j < width; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    color[i, j, k] = textures[i].GetPixel(j, k);
                    if (color[i, j, k].a == 1)
                    {
                        pixels[i,j,k] = new Pixel(j,k,i, color[i,j,k]);
                    }
                }
            }
        }

        ProcessData(pixels, uniqueObjectsColorReferences);
    }

    private void ProcessData(Pixel[,,] input, List<Color> refs)
    {
        for (int i = 0; i < modelHeight; i++)
        {
            for (int j = 0; j < width; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    //Spawn unique object based on the pixel color
                    for (int l = 0; l < refs.Count; l++)
                    {
                        if (input[i, j, k].pixelColor == refs[l])
                        {

                            //Instantiate
                            GameObject go = Instantiate(uniqueObjectsReferences[l], pixels[i, j, k].position, Quaternion.identity, uniqueObjectParent.transform);
                            go.GetComponent<MeshRenderer>().material.color = pixels[i, j, k].pixelColor;

                            //Disable this pixel from being checked in the future
                            input[i, j, k].pixelColor = Color.clear;
                        }
                    }

                    //Split other pixels into clusters of same color & connected  
                    for (int m = 0; m < terrainColorReferences.Count; m++)
                    {
                        if(input[i, j, k].pixelColor == terrainColorReferences[m])
                        {
                            if (!input[i, j, k]._checked && input[i, j, k].pixelColor != Color.clear)
                            {
                                differentMeshes++;
                                input[i, j, k].mesh = differentMeshes;
                                queue.Add(input[i, j, k]);

                                while(queue.Count > 0)
                                    CheckNeigbours(input, queue[0]);
                            }
                        }
                    }

                    //yield return new WaitForSeconds(0.001f);
                }
            }
        }

        //part clusters in different lists and then create pointClouds
        clusters = new List<Pixel>[differentMeshes];
        pointClouds = new List<Vector3>[differentMeshes];
        triangles = new List<int>[differentMeshes];

        for (int i = 1; i < differentMeshes + 1; i++)
        {
            clusters[i-1] = new List<Pixel>();
            foreach(var p in pixels)
            {
                if(p.mesh == i)
                {
                    clusters[i-1].Add(p);
                    if(!p.northClear && !p.southClear && !p.upClear && !p.downClear && !p.westClear && !p.eastClear)
                    {
                        CheckNeigbours(pixels, p);
                    }
                }
            }
        }



        for (int i = 0; i < differentMeshes; i++)
        {
            meshes.Add(new Mesh());

            for (int j = 0; j < terrainColorReferences.Count; j++)
            {
                Color c = clusters[i][0].pixelColor;
                if(c == terrainColorReferences[j])
                {
                    finalObjects.Add(Instantiate(terrainReferences[j], emptyParent.transform.position,Quaternion.identity, terrainObjectParent.transform));
                }
            }

            var filter = finalObjects[i].GetComponent<MeshFilter>();
            meshes[i] = filter.mesh;

            pointClouds[i] = new List<Vector3>();
            triangles[i] = new List<int>();

            //create points
            foreach (Pixel p in clusters[i])
            {
                Vector3 origin = p.position;


                if(p.southClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }
                

                if(p.northClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, 1);
                }
                
                if(p.eastClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }
                
                if(p.westClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }

                if (p.downClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, 1);
                }

                if (p.upClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }
                
            }

            meshes[i].vertices = pointClouds[i].ToArray();
            meshes[i].triangles = triangles[i].ToArray();
            meshes[i].OptimizeIndexBuffers();
            meshes[i].OptimizeReorderVertexBuffer();
            meshes[i].RecalculateNormals();
            meshes[i].RecalculateBounds();

            //SAVE MESH to path
            SaveMesh(meshes[i], outputName + "Mesh_" + (i + 1), path);
        }

        //Change Object Name
        emptyParent.name = outputName;
        print("Different meshes : " + differentMeshes);
        
        prefabPath = prefabPath + "/" +emptyParent.name + ".prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        PrefabUtility.SaveAsPrefabAssetAndConnect(emptyParent, prefabPath, InteractionMode.UserAction);
    }

    private bool hasSameNeigbour(Pixel[,,] input, Pixel pixel) 
    {
        int x = pixel.x;
        int y = pixel.y;
        int z = pixel.z;

        //Check above voxel;
        if (z + 1 < modelHeight)
        {
            if (pixels[z + 1, x, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z + 1, x, y]._checked)
            {
                return true;
            }
            if (pixels[z + 1, x, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].upClear = true;
            }
        }
        else
        {
            pixels[z, x, y].upClear = true;
        }

        //Check Under voxel
        if (z > 0)
        {
            if (pixels[z - 1, x, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z - 1, x, y]._checked)
            {
                return true;
            }
            if (pixels[z - 1, x, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].downClear = true;
            }
        }
        else
        {
            pixels[z, x, y].downClear = true;
        }

        //check west voxel;
        if (x > 0)
        {
            if (pixels[z, x - 1, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x - 1, y]._checked)
            {
                return true;
            }
            if (pixels[z, x - 1, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].westClear = true;
            }
        }
        else
        {
            pixels[z, x, y].westClear = true;
        }

        //check east voxel;
        if (x + 1 < width)
        {
            if (pixels[z, x + 1, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x + 1, y]._checked)
            {
                return true;
            }
            if (pixels[z, x + 1, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].eastClear = true;
            }
        }
        else
        {
            pixels[z, x, y].eastClear = true;
        }

        //check south voxel;
        if (y > 0)
        {
            if (pixels[z, x, y - 1].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x, y - 1]._checked)
            {
                return true;
            }

            if (pixels[z, x, y - 1].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].southClear = true;
            }
        }
        else
        {
            pixels[z, x, y].southClear = true;
        }

        //check south voxel;
        if (y + 1 < length)
        {
            if (pixels[z, x, y + 1].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x, y + 1]._checked)
            {
                return true;
            }
            if (pixels[z, x, y + 1].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].northClear = true;
            }
        }
        else
        {
            pixels[z, x, y].northClear = true;
        }

        return false;
    }

    private void CheckNeigbours(Pixel[,,] input, Pixel pixel)
    {
        if(queue.Count!=0)
            queue.RemoveAt(0);

        List<Pixel> checkedPixels = new List<Pixel>();

        int x = pixel.x;
        int y = pixel.y;
        int z = pixel.z;
        int mesh = pixel.mesh;

        pixels[z, x, y]._checked = true;

        checkedPixels.Clear();

        //Check above voxel;
        if (z + 1 < modelHeight)
        {
            if(pixels[z + 1, x, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z + 1, x, y]._checked)
            {
                pixels[z + 1, x, y]._checked = true;
                pixels[z + 1, x, y].mesh = mesh;


                checkedPixels.Add(pixels[z + 1, x, y]);
            }
            
            if(pixels[z + 1, x, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].upClear = true;
            }
        }
        else
        {
            pixels[z, x, y].upClear = true;
        }

        //Check Under voxel
        if(z > 0)
        {
            if (pixels[z - 1, x, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z - 1, x, y]._checked)
            {
                pixels[z - 1, x, y]._checked = true;
                pixels[z - 1, x, y].mesh = mesh;

                checkedPixels.Add(pixels[z - 1, x, y]);
            }

            if (pixels[z - 1, x, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].downClear = true;
            }
        }
        else
        {
            pixels[z, x, y].downClear = true;
        }

        //check west voxel;
        if (x > 0)
        {
            if (pixels[z, x - 1, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z , x - 1, y]._checked)
            {
                 pixels[z , x - 1, y]._checked = true;
                 pixels[z , x - 1, y].mesh = mesh;

                checkedPixels.Add(pixels[z, x - 1, y]);
            }

            if (pixels[z, x - 1, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].westClear = true;
            }
        }
        else
        {
            pixels[z, x, y].westClear = true;
        }

        //check east voxel;
        if (x + 1 < width)
        {
            if (pixels[z, x + 1, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x + 1, y]._checked)
            {
                pixels[z, x + 1, y]._checked = true;
                pixels[z, x + 1, y].mesh = mesh;

                checkedPixels.Add(pixels[z, x + 1, y]);
            }

            if (pixels[z, x + 1, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].eastClear = true;
            }
        }
        else
        {
            pixels[z, x, y].eastClear = true;
        }

        //check south voxel;
        if (y > 0)
        {
            if (pixels[z, x , y - 1].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x, y - 1]._checked)
            {
                pixels[z, x, y - 1]._checked = true;
                pixels[z, x, y - 1].mesh = mesh;

                checkedPixels.Add(pixels[z, x, y - 1]);

            }

            if (pixels[z, x, y - 1].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].southClear = true;
            }
        }
        else
        {
            pixels[z, x, y].southClear = true;
        }

        //check north voxel;
        if (y + 1 < length)
        {
            if (pixels[z, x, y + 1].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x, y + 1]._checked)
            {
                pixels[z, x, y + 1]._checked = true;
                pixels[z, x, y + 1].mesh = mesh;

                checkedPixels.Add(pixels[z, x, y + 1]);
            }

            if (pixels[z, x, y + 1].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].northClear = true;
            }
        }
        else
        {
            pixels[z, x, y].northClear = true;
        }

        for (int i = 0; i < checkedPixels.Count; i++)
        {
            queue.Add(checkedPixels[i]);
        }
    }
    
    private void CreateTriangles(int mesh, int index, int orientation)
    {
        if(orientation == -1)
        {
            triangles[mesh].Add(index - 2);
            triangles[mesh].Add(index - 1);
            triangles[mesh].Add(index - 3);
        
            triangles[mesh].Add(index - 2);
            triangles[mesh].Add(index);
            triangles[mesh].Add(index - 1);
        }
        if(orientation == 1)
        {
            triangles[mesh].Add(index - 3);
            triangles[mesh].Add(index - 1);
            triangles[mesh].Add(index - 2);

            triangles[mesh].Add(index - 1);
            triangles[mesh].Add(index);
            triangles[mesh].Add(index - 2);
        }
        
               
    }

    private void SaveMesh(Mesh mesh, string name, string path)
    {
        path = path + "/" + name + ".asset";

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }

    /*private void OnDrawGizmosSelected()
    {
        Color c = Color.white;
        if(pointClouds!=null)
        {
            for (int i = 0; i < pointClouds.Length; i++)
            {

                for (int j = 0; j < pointClouds[i].Count; j++)
                {
                    Gizmos.DrawWireSphere(pointClouds[i][j], 0.1f);
                }
           
            }
        }
    }*/
}
