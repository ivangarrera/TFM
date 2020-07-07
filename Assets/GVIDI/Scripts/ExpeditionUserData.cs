using System;
using System.Numerics;

namespace Assets.GVIDI.Scripts
{
    [Serializable]
    public class ExpeditionUserData
    {
        /// <summary>
        /// Latitude of the user
        /// </summary>
        public string lat { get; set; }

        /// <summary>
        /// Longitude of the user
        /// </summary>
        public string lon { get; set; }

        public Vector2 CoordinatesRadians { get; set; }

        /// <summary>
        /// Timestamp when these coordinates where obtained
        /// </summary>
        public string time { get; set; }
    }
}
