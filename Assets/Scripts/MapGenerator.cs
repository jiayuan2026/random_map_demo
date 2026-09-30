using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class ItemSpawnData
{
    public TileBase tile;
    public int weight;
}

public class MapGenerator : MonoBehaviour
{
    public Tilemap groundTileMap;
    public Tilemap itemTileMap;
    public int width;
    public int height;
    public int seed;
    public bool useRandomSeed;
    public float lacunarity = 0.1f;
    [Range(0, 1f)]
    public float waterProbability;
    public List<ItemSpawnData> itemSpawnDatas;
    public int removeSeparateTileNumberOfTimes;
    public TileBase groundTile;
    public TileBase waterTile;
    private float[,] mapData;   // true: ground; false: water

    public void GenerateMap()
    {
        itemSpawnDatas.Sort((data1, data2) =>
        {
            return data1.weight.CompareTo(data2.weight);
        });

        GenerateMapData();
        //地图处理 
        for (int i = 0; i < removeSeparateTileNumberOfTimes; i++)
        {
            if (!RemoveSeparateTile())
                break;
        }
        GenerateTileMap();
    }

    private void GenerateMapData()
    {
        //use seed
        if (!useRandomSeed)
            seed = Time.time.GetHashCode();
        UnityEngine.Random.InitState(seed);

        mapData = new float[width, height];
        float randomOffset = UnityEngine.Random.Range(-10000, 10000);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = Mathf.PerlinNoise(x * lacunarity + randomOffset, y * lacunarity + randomOffset);
                mapData[x, y] = noiseValue;
                if (noiseValue < minValue) minValue = noiseValue;
                if (noiseValue > maxValue) maxValue = noiseValue;
            }
        }
        //平滑到0~1 
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                mapData[x, y] = Mathf.InverseLerp(minValue, maxValue, mapData[x, y]);
            }
        }
    }

    private bool RemoveSeparateTile()
    {
        bool res = false;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //是地面且只有一个令居也是地面
                if (IsGround(x, y) && GetFourNeighborsGroundCount(x, y) <= 1)
                {
                    mapData[x, y] = 0;// 设置为水
                    res = true;
                }
            }
        }
        return res;
    }

    private int GetFourNeighborsGroundCount(int x, int y)
    {
        int count = 0;
        //top
        if (IsInMapRange(x, y + 1) && IsGround(x, y + 1)) count += 1;
        //bottom
        if (IsInMapRange(x, y - 1) && IsGround(x, y - 1)) count += 1;
        //left
        if (IsInMapRange(x - 1, y) && IsGround(x - 1, y)) count += 1;
        //right
        if (IsInMapRange(x + 1, y) && IsGround(x + 1, y)) count += 1;
        return count;
    }
    
    private int GetEightNeighborsGroundCount(int x, int y)
    {
        int count = 0;
        //top
        if (IsInMapRange(x, y + 1) && IsGround(x, y + 1)) count += 1;
        //bottom
        if (IsInMapRange(x, y - 1) && IsGround(x, y - 1)) count += 1;
        //left
        if (IsInMapRange(x - 1, y) && IsGround(x - 1, y)) count += 1;
        //right
        if (IsInMapRange(x + 1, y) && IsGround(x + 1, y)) count += 1;

         //left top
        if (IsInMapRange(x - 1, y + 1) && IsGround(x-1, y + 1)) count += 1;
        //right top
        if (IsInMapRange(x + 1, y + 1) && IsGround(x+1, y + 1)) count += 1;
        //left bottom
        if (IsInMapRange(x - 1, y-1) && IsGround(x - 1, y-1)) count += 1;
        //right bottom
        if (IsInMapRange(x + 1, y-1) && IsGround(x + 1, y-1)) count += 1;
        return count;
    }

    private void GenerateTileMap()
    {
        ClearTileMap();
        // 地面
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileBase tile = IsGround(x, y) ? groundTile : waterTile;
                groundTileMap.SetTile(new Vector3Int(x, y), tile);
            }
        }
        //物品
        int weightTotal = 0;
        for (int i = 0; i < itemSpawnDatas.Count; i++)
        {
            weightTotal += itemSpawnDatas[i].weight;
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (IsGround(x, y) && GetEightNeighborsGroundCount(x, y) == 8) //只有地面才能生成物品
                {
                    float randomValue = UnityEngine.Random.Range(1, weightTotal + 1);
                    float temp = 0;
                    for (int i = 0; i < itemSpawnDatas.Count; i++)
                    {
                        temp += itemSpawnDatas[i].weight;
                        if (randomValue < temp)
                        {
                            if (itemSpawnDatas[i].tile)
                            {
                                itemTileMap.SetTile(new Vector3Int(x, y), itemSpawnDatas[i].tile);
                            }
                            break;
                        }
                    }
                }


            }
        }
    }

    public bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsGround(int x, int y)
    {
        return mapData[x, y] > waterProbability;
    }

    public void ClearTileMap()
    {
        groundTileMap.ClearAllTiles();
        itemTileMap.ClearAllTiles();
    }

}
