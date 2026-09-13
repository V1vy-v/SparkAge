using SparkAge.View.UI;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.ShowPanel<BeginPanel>();
    }
}
