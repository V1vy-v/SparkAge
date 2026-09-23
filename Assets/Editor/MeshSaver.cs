using UnityEngine;
using UnityEditor;

public class MeshSaver : Editor
{
    [MenuItem("CONTEXT/MeshFilter/Save Mesh As Asset")]
    static void SaveMeshAsAsset(MenuCommand command)
    {
        MeshFilter meshFilter = command.context as MeshFilter;
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Mesh",
            meshFilter.sharedMesh.name,
            "asset",
            "Please enter a file name to save the mesh to"
        );

        if (string.IsNullOrEmpty(path)) return;

        Mesh meshToSave = Object.Instantiate(meshFilter.sharedMesh);
        meshToSave.name = System.IO.Path.GetFileNameWithoutExtension(path);

        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
    }
}