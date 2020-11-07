using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RonALert.Core.Entities
{
    public class EntityBase
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public static class BaseEntityExtensions
    {
        public static bool IsNull(this EntityBase entity)
        {
            return entity == null;
        }
    }
}
