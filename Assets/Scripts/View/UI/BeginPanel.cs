using Mirror;
using SparkAge.Controller.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class BeginPanel : BasePanel
    {
        [SerializeField] TextMeshProUGUI txtNickName;
        public Button btnSetUp, btnJoin, btnSetting, btnQuit;

        protected override void Init()
        {
            btnSetUp.onClick.AddListener(() =>
            {
                //创建房间
                NetworkMgr.Instance.StartHost();
            });
            btnJoin.onClick.AddListener(() =>
            {
                //加入房间->输入服务器地址（连接界面）
                UIManager.Instance.ShowPanel<ConnectPanel>();
            });
            btnSetting.onClick.AddListener(() =>
            {
                UIManager.Instance.ShowPanel<SettingPanel>();
            });
            btnQuit.onClick.AddListener(() =>
            {
                Application.Quit();
            });
        }
        public override void ShowMe()
        {
            base.ShowMe();
            txtNickName.SetText(LocalPlayerProfile.NickName);
        }
    }
}
