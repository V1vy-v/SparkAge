using SparkAge.Controller.Network;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class GameOverPanel : BasePanel
    {
        [SerializeField] TextMeshProUGUI txtRes;
        [SerializeField] Button btnOK;

        bool returning;

        protected override void Init()
        {
            btnOK.onClick.AddListener(() =>
            {
                if (returning)
                    return;

                returning = true;
                StartCoroutine(ReturnToBeginScene());
            });
        }

        public void SetGameRes(bool win)
        {
            txtRes.SetText("游戏" + (win ? "胜利" : "失败"));
        }

        IEnumerator ReturnToBeginScene()
        {
            if (NetworkMgr.Instance != null)
            {
                NetworkMgr.Instance.ResetForNewSession();

                Destroy(NetworkMgr.Instance.gameObject);

                yield return null;
            }

            UIManager.Instance.CloseAllPanel();

            SceneManager.LoadSceneAsync("BeginScene");
        }

        public override void ShowMe()
        {
            base.ShowMe();
            returning = false;
        }
    }
}