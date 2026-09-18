using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class HUD : BasePanel
    {
        [SerializeField] TextMeshProUGUI txtTurnNum, txtCurPlayer, txtMyInfo, txtTips;
        [SerializeField] Button btnEndTurn;
        

        protected override void Init()
        {
            txtTurnNum.text = "当前回合：1";

            btnEndTurn.onClick.AddListener(() =>
            {
                UIManager.Instance.UIInput?.RequestEndPhase();
            });
        }

        public void InitMyInfo(Model.PlayerInfo info)
        {
            txtMyInfo.SetText("{0}\n" + info.Name + "\n" + info.CharacterInfo.Name, info.Id);
        }
        public void UpdateHUD(string name, int turnNum)
        {
            txtCurPlayer.SetText("当前玩家：" + name);
            txtTurnNum.SetText("当前回合：{0}", turnNum);
        }
        public void UpdateTips(string text)
        {
            StopCoroutine(FadeOut());

            txtTips.SetText(text);
            txtTips.alpha = 1.0f;

            StartCoroutine(FadeOut());
        }

        WaitForSeconds wait = new WaitForSeconds(3);
        IEnumerator FadeOut()
        {
            yield return wait;
            txtTips.alpha = 0;
        }
    }
}