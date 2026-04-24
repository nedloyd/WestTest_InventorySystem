using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystem.Domain.Constants
{
    public static class Role
    {
        public const string WarehouseAdmin = "WarehouseAdmin";

        public const string Auditor = "Auditor";  //Read only access
    }
}
