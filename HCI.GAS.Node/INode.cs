using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Node
{
    interface INode
    {
        /// <summary>
        /// Starting noding
        /// </summary>
        void startNode();

        /// <summary>
        /// End Noding
        /// </summary>
        void stopNode();
    }
}
