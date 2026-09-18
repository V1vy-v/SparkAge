using SparkAge.Config;
using SparkAge.Controller.Network;
using System.Collections.Generic;

namespace SparkAge.Controller
{
    public enum ControllerType
    {
        Human, 
        AI
    }
    public class GameSession
    {
        Dictionary<int, ControllerType> PlayerType = new Dictionary<int, ControllerType>();
        public ControllerType GetControllerType(int playerId)
        {
            if (PlayerType.TryGetValue(playerId, out ControllerType type))
                return type;
            return default;
        }

        public void Init(SlotData[] slots)
        {
            foreach(var slot in slots)
            {
                if (slot.isAI)
                    PlayerType[slot.PlayerId] = ControllerType.AI;
                else
                    PlayerType[slot.PlayerId] = ControllerType.Human;
            }
        }
    }
}
