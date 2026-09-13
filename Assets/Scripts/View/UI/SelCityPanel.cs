using SparkAge.Model.Cities;
using SparkAge.Model.Orders;
using SparkAge.Model.Units;
using SparkAge.View.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelCityPanel : BasePanel
{
    [SerializeField] TextMeshProUGUI txtName;
    [SerializeField] Button btnClose;
    [SerializeField] Button btnItem1, btnItem2;
    City city;
    protected override void Init()
    {
        btnClose.onClick.AddListener(() =>
        {
            HideMe();
        });
        btnItem1.onClick.AddListener(() =>
        {
            UIManager.Instance.Sink?.SubmitOrder(new BuildUnitOrder(city.Owner, city, UnitType.Settler));
        });
        btnItem2.onClick.AddListener(() =>
        {
            UIManager.Instance.Sink?.SubmitOrder(new BuildUnitOrder(city.Owner, city, UnitType.Warrior));
        });
    }

    public void UpdatePanel(City city)
    {
        this.city = city;
        txtName.SetText(city.Name);
    }
}
