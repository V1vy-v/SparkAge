using Mirror;
using SparkAge.Controller.Network;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class ConnectPanel : BasePanel
    {
        [SerializeField] TextMeshProUGUI txtDisconnected;
        [SerializeField] TMP_InputField inAddress;
        [SerializeField] Button btnBack, btnJoin;
        private void OnEnable()
        {
            NetworkMgr.Instance.Connected += OnConnected;
            NetworkMgr.Instance.Disconnected += OnDisconnected;
            NetworkMgr.Instance.ConnectFailed += OnFailConntection;
        }
        protected override void Init()
        {
            txtDisconnected.alpha = 0f;
            btnBack.onClick.AddListener(() => 
            {
                HideMe();
            });
            btnJoin.onClick.AddListener(() =>
            {
                if (NetworkClient.active) return;
                //加入房间
                print("开始连接");
                NetworkMgr.Instance.networkAddress = inAddress.text;
                NetworkMgr.Instance.StartClient();
            });
        }
        private void OnConnected()
        {
            print("连接成功");
            HideMe();
        }
        private void OnDisconnected()
        {
            txtDisconnected.SetText("连接断开");
            txtDisconnected.alpha = 1.0f;

            StopCoroutine(FadeOut());
            StartCoroutine(FadeOut());
        }
        private void OnFailConntection(string reason)
        {
            txtDisconnected.SetText("连接失败：" + reason);
            txtDisconnected.alpha = 1.0f;

            StopCoroutine(FadeOut());
            StartCoroutine(FadeOut());
        }
        WaitForSeconds wait = new WaitForSeconds(3);
        IEnumerator FadeOut()
        {
            yield return wait;
            txtDisconnected.alpha = 0;
        }
        private void OnDisable()
        {
            NetworkMgr.Instance.Connected -= OnConnected;
            NetworkMgr.Instance.Disconnected -= OnDisconnected;
            NetworkMgr.Instance.ConnectFailed -= OnFailConntection;
        }
    }
}
