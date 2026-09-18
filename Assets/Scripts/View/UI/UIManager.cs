using SparkAge.Controller;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SparkAge.View.UI
{
    public class UIManager
    {
        private static UIManager instance = new UIManager();
        public static UIManager Instance => instance;

        private Transform canvas;
        //面板字典
        Dictionary<string, BasePanel> panelDic = new();

        public IUIInput UIInput { get; private set; }
        public void SetUIInput(IUIInput uiInput) => UIInput = uiInput;
        public bool IsBlockingUI => panelDic.TryGetValue("SelCityPanel", out BasePanel panel) && (panel as SelCityPanel).gameObject.activeSelf;
        public bool IsPointerOverUI => EventSystem.current.IsPointerOverGameObject();

        private UIManager()
        {
            GameObject canvasObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Canvas"));
            canvas = canvasObj.transform;
            GameObject.DontDestroyOnLoad(canvasObj);
        }
        public T ShowPanel<T>() where T : BasePanel
        {
            string panelName = typeof(T).Name;
            if (!panelDic.TryGetValue(panelName, out BasePanel panel))
            {
                GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/" + panelName));
                obj.transform.SetParent(canvas, false);
                panel = obj.GetComponent<T>();
                panelDic[panelName] = panel;
            }
            panel.ShowMe();
            return panel as T;
        }

        public void HidePanel<T>() where T : BasePanel
        {
            string panelName = typeof(T).Name;
            if (panelDic.TryGetValue(panelName, out BasePanel panel))
            {
                panel.HideMe();
            }
        }

        public T GetPanel<T>() where T : BasePanel
        {
            string panelName = typeof(T).Name;
            if (!panelDic.TryGetValue(panelName, out BasePanel panel))
            {
                GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/" + panelName));
                obj.transform.SetParent(canvas, false);
                panel = obj.GetComponent<T>();
                panelDic[panelName] = panel;
            }
            return panel as T;
        }
        public void CloseAllPanel()
        {
            foreach (var panel in panelDic.Values)
                panel.HideMe();
        }
    }
}
