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
        protected override void Init()
        {
            btnOK.onClick.AddListener(() =>
            {
                SceneManager.LoadSceneAsync("BeginScene");
                UIManager.Instance.CloseAllPanel();
                HideMe();
            });
        }
        public void SetGameRes(bool win)
        {
            txtRes.SetText("游戏" + (win ? "胜利" : "失败"));
        }
    }
}