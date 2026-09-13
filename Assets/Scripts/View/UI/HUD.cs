using SparkAge.Controller;
using SparkAge.Model.Orders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparkAge.View.UI
{
    public class HUD : BasePanel
    {
        [SerializeField] TextMeshProUGUI txtProduction, txtTurnNum;
        [SerializeField] Button btnEndTurn;
        

        protected override void Init()
        {
            txtProduction.text = "生产力：0";
            txtTurnNum.text = "当前回合：1";

            btnEndTurn.onClick.AddListener(() =>
            {
                UIManager.Instance.Sink?.SubmitOrder(new EndPhaseOrder());
            });
        }

        public void UpdateHUD(int production, int turnNum)
        {
            txtProduction.SetText("生产力：{0}", production);
            txtTurnNum.SetText("当前回合：{0}", turnNum);
        }
    }
}