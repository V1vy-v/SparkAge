using SparkAge.Model.Hex;

namespace SparkAge.Model.Cities
{
    public class City
    {
        public int Owner;
        public int Radius;
        public HexCoord Position;
        public int Production;

        public City(int owner, HexCoord position, int radius, int production = 5)
        {
            Owner = owner;
            Position = position;
            Radius = radius;
            Production = production;
        }
    }
}
