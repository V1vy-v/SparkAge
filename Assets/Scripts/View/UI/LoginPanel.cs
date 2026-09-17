using SparkAge.Controller.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class LoginPanel : BasePanel
    {
        [SerializeField] TMP_InputField inputName;
        [SerializeField] Button btnCompleted;
        protected override void Init()
        {
            btnCompleted.onClick.AddListener(() =>
            {
                //保存客户端信息
                LocalPlayerProfile.Save(inputName.text);
                UIManager.Instance.GetPanel<BeginPanel>().ShowMe();
                HideMe();
            });
        }
    }
}
