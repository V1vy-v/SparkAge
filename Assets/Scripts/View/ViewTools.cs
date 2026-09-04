using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.View
{
    public static class ViewTools
    {
        public static Color GetPlayerColor(int owner) => owner switch
        {
            1 => Color.red,
            2 => Color.blue,
            3 => Color.green,
            4 => Color.yellow,
            _ => Color.gray
        };
    }
}
