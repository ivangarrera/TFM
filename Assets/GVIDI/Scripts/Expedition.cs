using System;
using System.Collections.Generic;

namespace Assets.GVIDI.Scripts
{
    [Serializable]
    public class Expedition
    {
        /// <summary>
        /// Collection which contains all the participants of the expedition
        /// </summary>
        public IEnumerable<IEnumerable<ExpeditionUserData>> participants;

        /// <summary>
        /// Guide of the expedition
        /// </summary>
        public IEnumerable<ExpeditionUserData> guide;
    }
}
