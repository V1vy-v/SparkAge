

namespace SparkAge.Model.Players
{
    public class PlayerState
    {
        public int ID;
        public bool IsAlive; // 是否存活

        public PlayerState(int id, bool isAlive = true)
        {
            ID = id;
            IsAlive = isAlive;
        }
    }
}
