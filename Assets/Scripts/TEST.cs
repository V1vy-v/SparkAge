using SparkAge.Framework.Hex;
using UnityEngine;

public class TEST : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = HexMeshFactory.CreateHexMesh(1);
        transform.GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
