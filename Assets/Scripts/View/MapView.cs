using SparkAge.Config;
using SparkAge.Controller.Network;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Hex;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.View
{
    /// <summary>
    /// 地图的表现层
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] Transform mapRoot;

        //外部提供字段
        GameState state;
        float hexSize;

        //独占字段
        int seed => NetworkMgr.Instance.MapSeed;
        List<GameObject> grassTiles;
        List<GameObject> mountainTiles;
        List<GameObject> forestTiles;
        List<GameObject> waterTiles;

        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            LoadAllTiles();
            mapRoot = GameObject.Find("MapRoot").transform;

            //创建地图和高亮资源，范围对象
            BuildTiles();
        }

        private void LoadAllTiles()
        {
            grassTiles = new(Resources.LoadAll<GameObject>("Prefabs/Tiles/Grass"));
            mountainTiles = new(Resources.LoadAll<GameObject>("Prefabs/Tiles/Mountain"));
            forestTiles = new(Resources.LoadAll<GameObject>("Prefabs/Tiles/Forest"));
            waterTiles = new(Resources.LoadAll<GameObject>("Prefabs/Tiles/Water"));
        }
        public void BuildTiles()
        {
            foreach(var tile in state.Map.Tiles.Values)
            {
                GameObject obj = Instantiate(GetPrefab(tile.Coord, tile.Type));

                obj.transform.SetParent(mapRoot, false);
                obj.transform.position = HexLayout.HexToPixel(tile.Coord, hexSize, 0);
            }
        }
        public GameObject GetPrefab(HexCoord coord, TerrainType type)
        {
            List<GameObject> tiles;
            switch (type)
            {
                case TerrainType.Plain:
                    tiles = grassTiles;
                    break;
                case TerrainType.Mountain:
                    tiles = mountainTiles;
                    break;
                case TerrainType.Forest:
                    tiles = forestTiles;
                    break;
                case TerrainType.Water:
                    tiles = waterTiles;
                    break;
                default:
                    tiles = grassTiles;
                    break;
            }
            int hash = coord.Q * 73856093 ^ coord.R * 19349663 ^ seed * 83492791;
            int index = Mathf.Abs(hash) % tiles.Count;
            return tiles[index];
        }
    }

}
