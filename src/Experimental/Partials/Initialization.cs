using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        partial void ExperimentalUnload()
        {

            SyncPipesDevelopment.UICleanup.Cleanup();
        }
    }
}
