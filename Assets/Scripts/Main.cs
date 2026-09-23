using SparkAge.Controller.Network;
using SparkAge.Framework;
using SparkAge.View.UI;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        LocalPlayerProfile.Load();
        GameSettings.Load();

        UIManager.Instance.ShowPanel<BeginPanel>();

        if (LocalPlayerProfile.IsFirst)
            UIManager.Instance.ShowPanel<LoginPanel>();
    }
}