using SparkAge.Model.Hex;

namespace SparkAge.Model.Cities
{
    public class City
    {
        public HexCoord Position;
        public int Radius;
        public int Owner;

        public City(HexCoord position, int radius, int owner)
        {
            Position = position;
            Radius = radius;
            Owner = owner;
        }
    }
}
