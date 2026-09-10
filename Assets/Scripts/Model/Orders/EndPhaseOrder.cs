using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class EndPhaseOrder : BaseOrder
    {
        public EndPhaseOrder(int id)
        {
            PlayerId = id;
        }
    }
}
