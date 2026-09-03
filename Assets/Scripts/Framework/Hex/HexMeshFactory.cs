using UnityEngine;

namespace SparkAge.Framework.Hex
{
    /// <summary>
    ///临时占位：运行时生成六边形网格，不用美术素材。换真实美术后删掉。
    ///</summary>
    public static class HexMeshFactory
    {
        //生成六边形网格
        public static Mesh CreateHexMesh(float size)
        {
            Mesh mesh = new Mesh();
            //六边形6个顶点
            Vector3[] vertices = new Vector3[6];
            vertices[0] = new Vector3(0, 0, size);//上顶点
            vertices[1] = new Vector3(size * Mathf.Sqrt(3) / 2f, 0, size / 2f);//右上顶点
            vertices[2] = new Vector3(size * Mathf.Sqrt(3) / 2f, 0, -size / 2f);//右下顶点
            vertices[3] = new Vector3(0, 0, -size);//下顶点
            vertices[4] = new Vector3(-size * Mathf.Sqrt(3) / 2f, 0, -size / 2f);//左下顶点
            vertices[5] = new Vector3(-size * Mathf.Sqrt(3) / 2f, 0, size / 2f);//左上顶点

            //4个三角形，12个顶点索引
            int[] triangles = new int[12]
            {
                0,1,2,   0,2,3,   0,3,4,   0,4,5
            };

            //6个法线
            Vector3[] normals = new Vector3[6];
            for (int i = 0; i < 6; i++) normals[i] = Vector3.up;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}