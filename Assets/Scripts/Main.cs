using SparkAge.Controller.Network;
using SparkAge.View.UI;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.ShowPanel<BeginPanel>();

        if (LocalPlayerProfile.IsFirst)
            UIManager.Instance.ShowPanel<LoginPanel>();
    }
}
