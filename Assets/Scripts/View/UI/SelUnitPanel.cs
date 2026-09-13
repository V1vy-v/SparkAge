using SparkAge.Model.Units;
using SparkAge.View.UI;
using TMPro;
using UnityEngine;

public class SelUnitPanel : BasePanel
{
    [SerializeField] TextMeshProUGUI txtInfo;

    protected override void Init()
    {
        
    }

    public void UpdatePanel(Unit unit)
    {
        txtInfo.SetText("名字：" + unit.Name + "\n血量：{0}\n攻击力：{1}\n防御力：{2}\n移动力：{3}\n",
            unit.Hp, unit.Atk, unit.Def, unit.MovementLeft);
    }
}
