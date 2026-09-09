

namespace SparkAge.Model.Players
{
    public class PlayerState
    {
        public int Id;
        //public Color PlayerColor; // 玩家颜色
        public bool IsAlive; // 是否存活

        public PlayerState(int id, /*Color playerColor,*/ bool isAlive = true)
        {
            Id = id;
            //PlayerColor = playerColor;
            IsAlive = isAlive;
        }
    }
}
