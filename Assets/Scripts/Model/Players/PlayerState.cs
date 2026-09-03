using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Model.Players
{
    public class PlayerState
    {
        public int Id;
        //public Color PlayerColor; // 玩家颜色
        public bool IsAlive; // 是否存活
        public int CityNum; // 拥有城市数

        public PlayerState(int id, /*Color playerColor,*/ bool isAlive = true, int cityNum = 0)
        {
            Id = id;
            //PlayerColor = playerColor;
            IsAlive = isAlive;
            CityNum = cityNum;
        }
    }
}
