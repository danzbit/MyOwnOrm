using MyOwnORM;
using MyOwnORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Models
{
    [Table("Position")]
    public class Position
    {
        [CustomPrimaryKey]
        public Guid Id { get; set; }
        [StringLength(56, ErrorMessage = "{0} value does not match the mask {1}.")]
        public string Name { get; set; }

        public List<Coords> Points { get; set; }
    }
}
