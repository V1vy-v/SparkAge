using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class BeginPanel : BasePanel
    {
        public Button btnStart, btnSetting, btnAbout, btnQuit;

        protected override void Init()
        {
            btnStart.onClick.AddListener(() =>
            {
                //保存数据

                //隐藏自己
                HideMe();
                //异步切换场景
                var ao = SceneManager.LoadSceneAsync("GameScene");
                //初始化

            });
            btnSetting.onClick.AddListener(() =>
            {

            });
            btnAbout.onClick.AddListener(() =>
            {

            });
            btnQuit.onClick.AddListener(() =>
            {
                Application.Quit();
            });
        }
    }
}
