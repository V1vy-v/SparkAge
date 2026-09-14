

namespace SparkAge.Model.GameInfos
{
    public class SlotInfo
    {
        public int PlayerID;

        public int CharacterId;
        public string Name;
        public string Description;
        public SlotInfo(int id)
        {
            PlayerID = id;
        }
    }
}
