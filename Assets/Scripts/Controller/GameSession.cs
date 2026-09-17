using SparkAge.Config;
using SparkAge.Controller.Network;
using System.Collections.Generic;

namespace SparkAge.Controller
{
    public enum ControllerType
    {
        HumanLocal, 
        HumanRemote, 
        AI,
        WrongType
    }
    public class GameSession
    {
        Dictionary<int, ControllerType> PlayerType = new Dictionary<int, ControllerType>();
        public ControllerType GetControllerType(int playerId)
        {
            if (PlayerType.TryGetValue(playerId, out ControllerType type))
                return type;
            return ControllerType.WrongType;
        }

        public void Init(SlotData[] slots)
        {
            foreach(var slot in slots)
            {
                if (slot.isAI)
                    PlayerType[slot.PlayerId] = ControllerType.AI;
                else if(slot.PlayerId != NetworkMgr.Instance.MyPlayerId)
                    PlayerType[slot.PlayerId] = ControllerType.HumanRemote;
                else if (slot.PlayerId == NetworkMgr.Instance.MyPlayerId)
                    PlayerType[slot.PlayerId] = ControllerType.HumanLocal;
                else
                    PlayerType[slot.PlayerId] = ControllerType.WrongType;
            }
        }
    }
}
