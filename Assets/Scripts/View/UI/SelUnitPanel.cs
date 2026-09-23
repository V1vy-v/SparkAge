using SparkAge.Config;
using SparkAge.Model.Units;
using SparkAge.View.UI;
using TMPro;
using UnityEngine;

public class SelUnitPanel : BasePanel
{
    [SerializeField] TextMeshProUGUI txtInfo1;
    [SerializeField] TextMeshProUGUI txtInfo2;
    Unit unit;

    protected override void Init()
    {
        
    }

    public void UpdatePanel(Unit unit, string characterName)
    {
        this.unit = unit;
        txtInfo1.SetText("名字：" + unit.Name + "\n攻击力：{0}\n血量：{1}", unit.Atk, unit.Hp);
        txtInfo2.SetText("归属：" + characterName + "\n防御力：{0}\n移动力：{1}\n", unit.Def, unit.MovementLeft);
    }
}
