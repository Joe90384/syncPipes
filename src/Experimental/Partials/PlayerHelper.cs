using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        public partial class PlayerHelper
        {
            internal SidebarMenu SideBar { get; private set; }
            internal MenuTest MenuTest { get; private set; }

            partial void ExperimentalCleanup()
            {
                SideBar?.Close();
                MenuTest?.Close();
            }

            partial void ExperimentalConstructor()
            {

                SideBar = new SidebarMenu(this);
                MenuTest = new MenuTest(this);
            }
        }
    }
}
