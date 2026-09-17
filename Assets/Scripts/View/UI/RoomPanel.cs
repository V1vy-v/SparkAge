using Mirror;
using SparkAge.Config;
using SparkAge.Controller.Network;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

namespace SparkAge.View.UI
{
    public class RoomPanel : BasePanel
    {
        [SerializeField] List<TextMeshProUGUI> txtNames;
        [SerializeField] List<TMP_Dropdown> dropSelCharacters;
        [SerializeField] List<Image> imgReadys;
        [SerializeField] Button btnClose, btnCancel, btnReady;

        TMP_Dropdown MyDropdown;

        private void OnEnable()
        {
            NetworkMgr.Instance.Disconnected += OnDisconnected;
            NetworkMgr.Instance.RoomUpdateEvent += Refresh;
        }
        protected override void Init()
        {
            //角色库注入与初始化可交互dropdown
            InitAllDropdowns();
            Refresh(NetworkMgr.Instance.Slots);
            btnClose.onClick.AddListener(() =>
            {
                //与服务端断开连接
                NetworkMgr.Instance.LeaveRoom();
            });
            btnCancel.onClick.AddListener(() =>
            {
                NetworkMgr.Instance.PlayerIsReady(false);
            });
            btnReady.onClick.AddListener(() =>
            {
                NetworkMgr.Instance.PlayerIsReady(true);
            });
            MyDropdown.onValueChanged.AddListener(i =>
            {
                NetworkMgr.Instance.SetCharacter(i);
            });
        }
        private void OnDisconnected()
        {
            HideMe();
        }
        private void Refresh(SlotData[] slots)
        {
            for (int i = 0; i < slots.Length; i++) 
            {
                txtNames[i].SetText(slots[i].Name);
                dropSelCharacters[i].SetValueWithoutNotify(slots[i].CharacterId);
                dropSelCharacters[i].RefreshShownValue();
                imgReadys[i].color = slots[i].Ready ? Color.green : Color.red;
            }
        }
        private void InitAllDropdowns()
        {
            List<string> characters = new();
            foreach(var cfg in ConfigMgr.Instance.characterCfgs)
            {
                characters.Add(cfg.Name);
            }
            for (int i = 0; i < dropSelCharacters.Count; i++)
            {
                dropSelCharacters[i].ClearOptions();
                dropSelCharacters[i].AddOptions(characters); 
                if (i != NetworkMgr.Instance.MyPlayerId - 1)
                    dropSelCharacters[i].interactable = false;
                else
                    MyDropdown = dropSelCharacters[i];
            }
        }

        private void OnDisable()
        {
            NetworkMgr.Instance.Disconnected -= OnDisconnected;
            NetworkMgr.Instance.RoomUpdateEvent -= Refresh;
        }
    }
}
