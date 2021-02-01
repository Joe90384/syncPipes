using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        static class UICleanup
        {
            public static void Cleanup()
            {
                UINumericUpDown.Cleanup();
            }
        }
    }
}
