using UnityEngine;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class SettingPanel : BasePanel
    {
        [SerializeField] Button btnClose;
        [SerializeField] Slider sldMusic, sldSound;
        [SerializeField] Toggle togMusic, togSound;

        protected override void Init()
        {
            //关闭面板
            btnClose.onClick.AddListener(() =>
            {
                UIManager.Instance.HidePanel<SettingPanel>();
            });

            //开关音乐
            togMusic.onValueChanged.AddListener((v) =>
            {
                AudioMgr.Instance.SetIsOpen(v);
            });

            //开关音效
            togSound.onValueChanged.AddListener((v) =>
            {

            });

            //修改音乐音量
            sldMusic.onValueChanged.AddListener((v) =>
            {
                AudioMgr.Instance.ChangeValue(v);
            });

            //修改音效音量
            sldSound.onValueChanged.AddListener((v) =>
            {

            });
        }
    }
}
