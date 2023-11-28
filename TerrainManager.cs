using System.Collections.Generic;
using UnityEngine;
public class TerrainManager : UnitySingleton<TerrainManager>
{ 
    public Transform target;
    public List<Texture2D> listHeightmaps = new List<Texture2D>();
    public List<TerrainState> listTerrainStates = new List<TerrainState>();
    public GameObject terrainPrefab;
    public int terrainWidth = 1000;
    public int terrainHeight = 1000;
    public float maxTerrainWidth = 10000;
    public float maxTerrainHeight = 10000;
    public int terrainDepth = 20;
    public int heightmapWidth = 256;
    public int heightmapHeight = 256;
    public int heightmapDepth = 20;
    public int heightmapIndex = 0;
    public int heightmapResolution;
    public int scale = 1;
    public float waterPlaneHeight;
    public ComputeShader convertToFloatArrayCShader;
    public bool dynamicGenerateTerrain = true;
    public bool useWaterPlane = false;

    public enum eTerrainGenerationType
    {
        eWhole,
        eRepeat,
    }
    public eTerrainGenerationType type = eTerrainGenerationType.eWhole;
    ComputeBuffer outputBuffer = null;
    int kernelHandle_convertToFloatArrayCShader;
    RenderTexture heightmapRT;
    Rect heightmapRect; 
    float[,] heightmapArray; 
    TerrainState[,] terrainStateArray;
    int targetGridX = 0;
    int targetGridY = 0;
    int prevGridX;
    int prevGridY;
    List<GameObject> currentActiveTerrains = new List<GameObject>();
    void Awake()
    { 
        kernelHandle_convertToFloatArrayCShader = convertToFloatArrayCShader.FindKernel("CSMain");
        //GenerateTerrainDataArray();
        //GenerateTerrainDataArrayFromPrefab();
        //prevGridX = targetGridX + 1;
        //prevGridY = targetGridY + 1;
    }
    private void Start()
    {
        target = PlayerManager.I.player.transform;
        Vector3 chestPos = Vector3.zero;
        List<int> listTerrainIndex = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int index = UnityEngine.Random.Range(1, listTerrainStates.Count);
            foreach (int idx in listTerrainIndex)
            {
                if (index == idx) index = -1;
            }
            if (index < 0)
            {
                i--;
                continue;
            }
            listTerrainIndex.Add(index);
        }
        foreach (int idx in listTerrainIndex)
        {
            TerrainState ts = listTerrainStates[idx];
            if (ts.transform.position.x < (float)terrainWidth * 0.9f && ts.transform.position.z < (float)terrainHeight * 0.9f) continue;
            chestPos = new Vector3(
                UnityEngine.Random.Range(ts.transform.position.x, ts.transform.position.x + (float)terrainWidth),
                0,
                UnityEngine.Random.Range(ts.transform.position.x, ts.transform.position.z + (float)terrainHeight));
            GameObject gb = ObjectManager.I.ActivateChest(chestPos);
            if (gb == null) break;
            gb.GetComponent<ObjectState>().renderObject.SetActive(ts.gameObject.activeSelf);
        } 
    }
    void FixedUpdate()
    {
        ResetGrid();
    } 
    void ResetGrid()
    {
        targetGridX = GetIndex((int)target.transform.position.x, terrainWidth);
        targetGridY = GetIndex((int)target.transform.position.z, terrainHeight);
        if (targetGridX != prevGridX || targetGridY != prevGridY)
        {
            TerrainState ts = GetTerrainState(prevGridX, prevGridY);
            if (target.transform.position.x > ts.transform.position.x + terrainWidth * 1.1f ||
                target.transform.position.z > ts.transform.position.z + terrainWidth * 1.1f ||
                target.transform.position.x < ts.transform.position.x - terrainWidth * 0.1f ||
                target.transform.position.z < ts.transform.position.z - terrainWidth * 0.1f)
            {
                foreach (GameObject gb in currentActiveTerrains) { gb.SetActive(false); }
                currentActiveTerrains.Clear();
                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        ts = GetTerrainState(targetGridX + x, targetGridY + y);
                        ts.transform.position = new Vector3(terrainWidth * (targetGridX + x), 0, terrainHeight * (targetGridY + y));
                        ts.gameObject.SetActive(true);
                        currentActiveTerrains.Add(ts.gameObject);
                    }
                }
                prevGridX = targetGridX;
                prevGridY = targetGridY;
            }
        }
        //foreach (GameObject gb in currentActiveTerrains) { gb.SetActive(false); }
        //currentActiveTerrains.Clear();
        //for (int x = -1; x < 2; x++)
        //{
        //    for (int y = -1; y < 2; y++)
        //    {
        //        TerrainState ts = GetTerrainState(targetGridX + x, targetGridY + y);
        //        ts.transform.position = new Vector3(terrainWidth * (targetGridX + x), 0, terrainHeight * (targetGridY + y));
        //        ts.gameObject.SetActive(true);
        //        currentActiveTerrains.Add(ts.gameObject);
        //    }
        //}
        //targetGridX = (int)target.transform.position.x / terrainWidth;
        //targetGridY = (int)target.transform.position.z / terrainHeight;
        //targetGridX = GetIndex((int)target.transform.position.x, terrainWidth);
        //targetGridY = GetIndex((int)target.transform.position.z, terrainHeight);
        //foreach (GameObject gb in currentActiveTerrains) { gb.SetActive(false); }
        //currentActiveTerrains.Clear();
        //for (int x = -1; x < 2; x++)
        //{
        //    for (int y = -1; y < 2; y++)
        //    {
        //        TerrainState ts = GetTerrainState(targetGridX + x, targetGridY + y);
        //        ts.transform.position = new Vector3(terrainWidth * (targetGridX + x), 0, terrainHeight * (targetGridY + y));
        //        ts.gameObject.SetActive(true);
        //        //터레인 위치 이동시 해당 터레인 위에 있는 오브젝트 이동을 위해
        //        if (ts.nowTerrainPos != ts.transform.position)
        //        {
        //            //터레인의 이전 위치와
        //            ts.beforeTerrainPos = ts.nowTerrainPos;
        //            //현제 위치 저장
        //            ts.nowTerrainPos = ts.transform.position;
        //            //터레인 이전 위치와 현제 위치 이용해 터레인의 위에 있는 오브젝트 이동
        //            ts.OnTerrainObjMove(new Vector3(ts.nowTerrainPos.x - ts.beforeTerrainPos.x, 0, ts.nowTerrainPos.z - ts.beforeTerrainPos.z));
        //        }
        //        currentActiveTerrains.Add(ts.gameObject);
        //    }
        //}
    }
    void ResetGrid2()
    {
        targetGridX = GetIndex((int)target.transform.position.x, terrainWidth);
        targetGridY = GetIndex((int)target.transform.position.z, terrainHeight);
        if (targetGridX != prevGridX || targetGridY != prevGridY)
        {
            TerrainState ts = GetTerrainState(prevGridX, prevGridY);
            if (target.transform.position.x > ts.transform.position.x + terrainWidth * 1.1f ||
                target.transform.position.z > ts.transform.position.z + terrainWidth * 1.1f ||
                target.transform.position.x < ts.transform.position.x - terrainWidth * 0.1f ||
                target.transform.position.z < ts.transform.position.z - terrainWidth * 0.1f)
            {
                foreach (GameObject gb in currentActiveTerrains) { gb.SetActive(false); }
                currentActiveTerrains.Clear();
                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        ts = GetTerrainState(targetGridX + x, targetGridY + y);
                        ts.transform.position = new Vector3(terrainWidth * (targetGridX + x), 0, terrainHeight * (targetGridY + y));
                        ts.gameObject.SetActive(true);
                        currentActiveTerrains.Add(ts.gameObject);
                    }
                }
                prevGridX = targetGridX;
                prevGridY = targetGridY;
            }
        } 
    }
    public TerrainState GetTerrainState(float x, float z)
    {
        int nx = GetIndex((int)x, terrainWidth);
        int nz = GetIndex((int)z, terrainHeight);
        return GetTerrainState(nx, nz);
    }
    public void OnTerrainListAdd(float x, float z, GameObject g)
    {
        int nx = GetIndex((int)x, terrainWidth);
        int nz = GetIndex((int)z, terrainHeight);
        TerrainState t = GetTerrainState(nx, nz);
        t.onTerrainList.Add(g);
    }
    public void AddTerrainObject(ObjectState objectState)
    {
        int nx = GetIndex((int)objectState.transform.position.x, terrainWidth);
        int nz = GetIndex((int)objectState.transform.position.z, terrainHeight);
        TerrainState ts = GetTerrainState(nx, nz);
        objectState.relativePos = objectState.transform.position - ts.transform.position;
        ts.listTerrainObjects.Add(objectState);
    }
    public TerrainState GetTerrainState(int offsetX, int offsetY)
    {
        offsetX = offsetX % scale;
        offsetY = offsetY % scale;
        if (offsetX < 0) offsetX += scale;
        if (offsetY < 0) offsetY += scale;
        return terrainStateArray[offsetX, offsetY];
    }
    public void GenerateTerrainDataArray()
    {
        terrainStateArray = new TerrainState[scale, scale];
        foreach (TerrainState state in listTerrainStates)
        {
            state.terrain = state.GetComponent<Terrain>();
            state.terrainData = state.terrain.terrainData;
#if UNITY_EDITOR
            if (dynamicGenerateTerrain == true)
            {
                switch (type)
                {
                    case eTerrainGenerationType.eRepeat:
                        {
                            state.terrainData = GenerateTerrain(state.terrainData);
                        }
                        break;
                    case eTerrainGenerationType.eWhole:
                        {
                            state.terrainData = GenerateTerrain(state.terrainData, state.y, state.x);
                        }
                        break;
                }
            }
#endif
            state.gameObject.SetActive(false);
            terrainStateArray[state.x, state.y] = state;
        }
    }
    void GenerateTerrainDataArrayFromPrefab()
    {
        terrainStateArray = new TerrainState[scale, scale];
        for (int x = 0; x < scale; x++)
        {
            for (int y = 0; y < scale; y++)
            {
                GameObject gb = Instantiate(terrainPrefab, transform);
                gb.transform.position = new Vector3(x * terrainWidth, 0, y * terrainHeight);
                TerrainState state = gb.GetComponent<TerrainState>();
                state.x = x;
                state.y = y; 
                state.terrain = state.GetComponent<Terrain>();
                state.terrainData = state.terrain.terrainData;
                state.terrainData = GenerateTerrain(state.terrainData, state.y, state.x);
                state.gameObject.SetActive(false);
                terrainStateArray[state.x, state.y] = state;
            }
        } 
    }
    public TerrainData GenerateTerrain(TerrainData terrainData)
    {
        float[,] heightmap = ConvertToFloatArray();
        terrainData.heightmapResolution = heightmapWidth + 1;
        heightmapResolution = terrainData.heightmapResolution;
        heightmapArray = new float[heightmapResolution, heightmapResolution];
        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                int dx = x > heightmapWidth - 1 ? 0 : x;
                int dy = y > heightmapHeight - 1 ? 0 : y;
                heightmapArray[x, y] = heightmap[dx, dy];
            }
        }
        terrainData.size = new Vector3(terrainWidth, terrainDepth, terrainHeight);   
        terrainData.SetHeights(0, 0, heightmapArray);
        return terrainData;
    }
    public TerrainData GenerateTerrain(TerrainData terrainData, int offsetX, int offsetY)
    {
        float[,] heightmap = ConvertToFloatArray();
        terrainData.heightmapResolution = heightmapWidth + 1;
        heightmapResolution = terrainData.heightmapResolution;
        heightmapArray = new float[heightmapResolution, heightmapResolution];
        offsetX = offsetX % scale;
        offsetY = offsetY % scale;
        if (offsetX < 0) offsetX += scale;
        if (offsetY < 0) offsetY += scale;
        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                float fx = ((float)offsetX * ((float)heightmapResolution - 1.0f) + (float)x) / (float)scale;
                float fy = ((float)offsetY * ((float)heightmapResolution - 1.0f) + (float)y) / (float)scale;
                int x0 = (int)fx;
                int y0 = (int)fy;
                int x1 = x0 + 1;
                int y1 = y0 + 1;
                if (x0 >= heightmapWidth) x0 = 0;
                if (y0 >= heightmapHeight) y0 = 0;
                if (x1 >= heightmapWidth) x1 = 0;
                if (y1 >= heightmapHeight) y1 = 0;
                //if (x0 > heightmapWidth ||
                //    x1 > heightmapWidth ||
                //    y0 > heightmapHeight ||
                //    y1 > heightmapHeight)
                //{
                //    Debug.Log("warning");
                //}
                float u = fx - (float)x0;
                float v = fy - (float)y0;
                //heightmapArray[x, y] = heightmap[x0, y0];
                float h = BilinearInterpolation(
                    heightmap[x0, y0],
                    heightmap[x1, y0],
                    heightmap[x0, y1],
                    heightmap[x1, y1],
                    u, v);
                if (useWaterPlane == true && h < (waterPlaneHeight - 0.05f) / terrainDepth)
                {
                    h = (waterPlaneHeight - 0.05f) / terrainDepth;
                }
                heightmapArray[x, y] = h;
                //int dx = x > heightmapWidth - 1 ? 0 : x; 
                //int dy = y > heightmapHeight - 1 ? 0 : y;
                //heightmapArray[x, y] = heightmap[dx, dy];
            }
        }
        terrainData.size = new Vector3(terrainWidth, terrainDepth, terrainHeight);
        terrainData.SetHeights(0, 0, heightmapArray); 
        return terrainData;
    }
    float[,] ConvertToFloatArray()
    {
        Texture2D tex = GetTexture2D();
        float[,] heights = new float[heightmapWidth, heightmapHeight];
        if (tex == null) return heights;
        float[] result = new float[heightmapWidth * heightmapHeight];
        convertToFloatArrayCShader.SetTexture(kernelHandle_convertToFloatArrayCShader, "InputTexture", tex); 
        outputBuffer = new ComputeBuffer(heightmapWidth * heightmapHeight, sizeof(float));
        outputBuffer.SetData(result);
        convertToFloatArrayCShader.SetBuffer(kernelHandle_convertToFloatArrayCShader, "OutputBuffer", outputBuffer);
        convertToFloatArrayCShader.Dispatch(kernelHandle_convertToFloatArrayCShader, heightmapWidth / 8, heightmapHeight / 8, 1);
        outputBuffer.GetData(result);
        for (int x = 0; x < heightmapWidth; x++)
        {
            for (int y = 0; y < heightmapHeight; y++)
            {
                heights[x, y] = result[y * heightmapWidth + x];
            }
        }
        outputBuffer.Release();
        return heights;
    }
    Texture2D GetTexture2D()
    {
        if (listHeightmaps.Count == 0) return null;
        if (heightmapIndex < 0) heightmapIndex = 0;
        if (heightmapIndex >= listHeightmaps.Count) heightmapIndex = 0;
        return listHeightmaps[heightmapIndex];
    }
    float BilinearInterpolation(float x0y0, float x1y0, float x0y1, float x1y1, float u, float v)
    {
        return ((1.0f - v) * ((1.0f - u) * x0y0 + u * x1y0) + v * ((1.0f - u) * x0y1 + u * x1y1)); 
    }
    public float GetTerrainHeight(float x, float z)
    { 
        //int nx = (int)x / terrainWidth;
        //int nz = (int)z / terrainHeight;
        int nx = GetIndex((int)x, terrainWidth);
        int nz = GetIndex((int)z, terrainHeight);
        TerrainState ts = GetTerrainState(nx, nz);
        return ts.terrainData.GetInterpolatedHeight(
            (x - (float)(nx * terrainWidth)) / (float)terrainWidth,
            (z - (float)(nz * terrainHeight)) / (float)terrainHeight);
    }
    public Vector3 GetTerrainNormal(float x, float z)
    { 
        //int nx = (int)x / terrainWidth;
        //int nz = (int)z / terrainHeight;
        int nx = GetIndex((int)x, terrainWidth);
        int nz = GetIndex((int)z, terrainHeight);
        TerrainState ts = GetTerrainState(nx, nz);
        return ts.terrainData.GetInterpolatedNormal(
            (x - (float)(nx * terrainWidth)) / (float)terrainWidth,
            (z - (float)(nz * terrainHeight)) / (float)terrainHeight);
    }
    public bool IsTerrainActive(float x, float z)
    {
        int nx = GetIndex((int)x, terrainWidth);
        int nz = GetIndex((int)z, terrainHeight);
        return GetTerrainState(nx, nz).gameObject.activeSelf;
    }
    public void SetTarget(Transform t)
    {
        target = t;
        targetGridX = GetIndex((int)target.transform.position.x, terrainWidth);
        targetGridY = GetIndex((int)target.transform.position.z, terrainHeight);
        prevGridX = targetGridX + 1;
        prevGridY = targetGridY + 1;
    }
    int GetIndex(int a, int b)
    { 
        int res = a / b;
        return (a < 0 && a != b * res) ? res - 1 : res;
    }
}
