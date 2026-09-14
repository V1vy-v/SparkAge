using SparkAge.Config;
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
        bool isServer;
        public bool IsServer => isServer;
        int myPlayerId;
        public int MyPlayerId => myPlayerId;
        Dictionary<int, ControllerType> PlayerType = new Dictionary<int, ControllerType>();
        public ControllerType GetControllerType(int playerId)
        {
            if (PlayerType.TryGetValue(playerId, out ControllerType type))
                return type;
            return ControllerType.WrongType;
        }

        public void Init(GameSetUpCfg cfg)
        {
            isServer = true;
            myPlayerId = 1;
            foreach(var slotCfg in cfg.Slots)
            {
                PlayerType[slotCfg.PlayerId] = slotCfg.Type;
            }
        }
    }
}
