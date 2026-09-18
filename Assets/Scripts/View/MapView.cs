using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Hex;
using UnityEngine;

namespace SparkAge.View
{
    /// <summary>
    /// 地图的表现层
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] Material plainMaterial;
        [SerializeField] Material forestMaterial;
        [SerializeField] Material mountainMaterial;
        [SerializeField] Material waterMaterial;
        [SerializeField] Transform mapRoot;

        //外部提供字段
        GameState state;
        float hexSize;

        //独占字段
        Mesh hexMesh;//地块网格
        public Mesh HexMesh => hexMesh;

        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            hexMesh = HexMeshFactory.CreateHexMesh(hexSize);
            EnsureMaterials();
            mapRoot = GameObject.Find("MapRoot").transform;

            //创建地图和高亮资源，范围对象
            BuildTiles();
        }

        /// <summary>
        /// 确认材质已创建
        /// </summary>
        private void EnsureMaterials()
        {
            if (plainMaterial == null)
                plainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0.45f, 0.75f, 0.45f)
                };
            if (forestMaterial == null)
                forestMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0f, 0.45f, 0f)
                };
            if (mountainMaterial == null)
                mountainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0.55f, 0.27f, 0.07f)
                };
            if (waterMaterial == null)
                waterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0f, 0.75f, 1f)
                };
        }
        /// <summary>
        /// 根据地形获取材质
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Material GetMaterial(TerrainType type) => type switch
        {
            TerrainType.Plain => plainMaterial,
            TerrainType.Forest => forestMaterial,
            TerrainType.Mountain => mountainMaterial,
            TerrainType.Water => waterMaterial,
            _ => plainMaterial
        };
        /// <summary>
        /// 创建地图中的地块
        /// </summary>
        public void BuildTiles()
        {
            foreach(var tile in state.Map.Tiles.Values)
            {
                GameObject obj = new GameObject($"tile {tile.Coord.R}, {tile.Coord.Q}");
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                mf.mesh = hexMesh;
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                mr.material = GetMaterial(tile.Type);

                obj.transform.SetParent(mapRoot, false);
                obj.transform.position = HexLayout.HexToPixel(tile.Coord, hexSize, 0);
            }
        }
    }

}
