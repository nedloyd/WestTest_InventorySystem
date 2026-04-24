using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystem.Domain.Entities
{
    public class Batch
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public string BatchNumber { get; set; } = string.Empty;

        public int InitialQuantity { get; set; }

        public int RemainingQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public Product? Product { get; set; }
    }
}
