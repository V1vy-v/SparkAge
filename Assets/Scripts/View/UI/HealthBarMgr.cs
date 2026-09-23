using SparkAge.Framework.EventCenter;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;
using Unit = SparkAge.Model.Units.Unit;

public class HealthBarMgr : MonoBehaviour
{
    GameState state;
    UnitView unitView;
    CityView cityView;
    Camera worldCamera;
    RectTransform healthBarRoot;
    HealthBarUI healthBarPrefab;

    Dictionary<int, HealthBarUI> unitBars = new();
    Dictionary<int, HealthBarUI> cityBars = new();
    public void Init(GameState state, UnitView unitView, CityView cityView, Camera worldCamera, Transform canvasRoot)
    {
        this.state = state;
        this.unitView = unitView;
        this.cityView = cityView;
        this.worldCamera = worldCamera;
        this.healthBarRoot = canvasRoot.Find("HealthBarRoot") as RectTransform;

        healthBarPrefab = Resources.Load<HealthBarUI>("Prefabs/UI/HealthBar");

        EventCenter.Instance.AddListener<InitialSettlers>(OnInitialSettlers);
        EventCenter.Instance.AddListener<BuildUnitEvent>(OnBuildUnit);
        EventCenter.Instance.AddListener<RemoveUnitEvent>(OnRemoveUnit);
        EventCenter.Instance.AddListener<BuildCityEvent>(OnBuildCity);
        EventCenter.Instance.AddListener<AttackUnitCompletedEvent>(OnAttackUnitCompleted);
        EventCenter.Instance.AddListener<AttackCityCompletedEvent>(OnAttackCityCompleted);
    } 

    private void LateUpdate()
    {
        UpdateUnitBars();
        UpdateCityBars();
    }
    private void OnDestroy()
    {
        EventCenter.Instance.RemoveListener<InitialSettlers>(OnInitialSettlers);
        EventCenter.Instance.RemoveListener<BuildUnitEvent>(OnBuildUnit);
        EventCenter.Instance.RemoveListener<RemoveUnitEvent>(OnRemoveUnit);
        EventCenter.Instance.RemoveListener<BuildCityEvent>(OnBuildCity);
        EventCenter.Instance.RemoveListener<AttackUnitCompletedEvent>(OnAttackUnitCompleted);
        EventCenter.Instance.RemoveListener<AttackCityCompletedEvent>(OnAttackCityCompleted);
    }

    public void OnInitialSettlers(InitialSettlers e)
    {
        foreach (var s in e.Settlers)
        {
            GetOrCreateUnitBar(s.ID);
        }
    }
    public void OnBuildUnit(BuildUnitEvent e)
    {
        GetOrCreateUnitBar(e.Unit.ID);
    }
    private void OnBuildCity(BuildCityEvent e)
    {
        GetOrCreateCityBar(e.City.ID);
    }
    private void OnRemoveUnit(RemoveUnitEvent e)
    {
        RemoveUnitHealthBar(e.Unit.ID);
    }
    public void OnAttackUnitCompleted(AttackUnitCompletedEvent e)
    {
        RefreshUnitHealth(e.Attacker.ID);
        RefreshUnitHealth(e.Defender.ID);
        if (e.Attacker.IsDead)
            RemoveUnitHealthBar(e.Attacker.ID);
        if (e.Defender.IsDead)
            RemoveUnitHealthBar(e.Defender.ID);
    }
    public void OnAttackCityCompleted(AttackCityCompletedEvent e)
    {
        RefreshUnitHealth(e.Attacker.ID);
        if (e.Attacker.IsDead)
            RemoveUnitHealthBar(e.Attacker.ID);

        RefreshCityHealthBar(e.City.ID);
    }

    private void UpdateUnitBars()
    {
        foreach(var pair in unitView.UnitObjs)
        {
            if (pair.Value == null) continue;

            Unit unit = state.TryGetUnit(pair.Key);
            if (unit == null)
            {
                continue;
            }

            UpdateBarPosition(GetOrCreateUnitBar(unit.ID), pair.Value.transform.position + Vector3.up * 1.2f);
        }
    }
    private void UpdateCityBars()
    {
        foreach (var pair in cityView.CityObjs)
        {
            if (pair.Value == null) continue;

            City city = state.TryGetCity(pair.Key);
            if (city == null)
            {
                continue;
            }

            UpdateBarPosition(GetOrCreateCityBar(city.ID), pair.Value.transform.position + Vector3.up * 1.7f);
        }
    }

    private HealthBarUI GetOrCreateUnitBar(int unitId)
    {
        if (unitBars.TryGetValue(unitId, out HealthBarUI bar))
            return bar;

        Unit unit = state.TryGetUnit(unitId);
        if (unit == null) return null;

        bar = GameObject.Instantiate(healthBarPrefab, healthBarRoot);
        bar.Rect.sizeDelta = new Vector2(60, 14);
        bar.SetValue(unit.Hp, unit.MaxHp);
        bar.SetColor(ViewTools.GetPlayerColor(unit.Owner));

        unitBars[unitId] = bar;
        return bar;
    }
    private HealthBarUI GetOrCreateCityBar(int cityId)
    {
        if (cityBars.TryGetValue(cityId, out HealthBarUI bar))
            return bar;

        City city =state.TryGetCity(cityId);
        if(city == null) return null;

        bar = GameObject.Instantiate(healthBarPrefab, healthBarRoot);
        bar.Rect.sizeDelta = new Vector2(100, 18);
        bar.SetValue(city.Hp, city.MaxHp);
        bar.SetColor(ViewTools.GetPlayerColor(city.Owner));

        cityBars[city.ID] = bar;
        return bar;
    }
    private void UpdateBarPosition(HealthBarUI bar, Vector3 worldPosition)
    {
        if(bar == null) return;

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z <= 0)
        {
            bar.SetVisible(false);
            return;
        }

        bar.SetVisible(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(healthBarRoot, screenPoint, null, out Vector2 localPoint);
        bar.Rect.anchoredPosition = localPoint;
    }

    private void RefreshUnitHealth(int unitId)
    {
        if (!unitBars.TryGetValue(unitId, out HealthBarUI bar))
            return;

        Unit unit = state.TryGetUnit(unitId);
        if (unit == null)
        {
            RemoveUnitHealthBar(unitId);
            return;
        }

        bar.SetValue(unit.Hp, unit.MaxHp);
        bar.SetColor(ViewTools.GetPlayerColor(unit.Owner));
    }
    private void RefreshCityHealthBar(int cityId)
    {
        if (!cityBars.TryGetValue(cityId, out HealthBarUI bar))
            return;

        City city = state.TryGetCity(cityId);
        if (city == null)
        {
            RemoveCityHealthBar(cityId);
            return;
        }

        bar.SetValue(city.Hp, city.MaxHp);
        bar.SetColor(ViewTools.GetPlayerColor(city.Owner));
    }

    public void RemoveUnitHealthBar(int unitId)
    {
        if (!unitBars.TryGetValue(unitId, out HealthBarUI bar))
            return;

        unitBars.Remove(unitId);
        GameObject.Destroy(bar.gameObject);
    }
    public void RemoveCityHealthBar(int cityId)
    {
        if (!cityBars.TryGetValue(cityId, out HealthBarUI bar))
            return;

        cityBars.Remove(cityId);
        GameObject.Destroy(bar.gameObject);
    }
}
