using MyOwnORM;
using SeaBattleDomainModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SeaBattleDomainModel
{
    [Table("RepairerShips")]
    internal class RepairerShip : Ship, IRepairable
    {
        [Column("DistanceToFixMax")]
        public int MaxDistanceToFix { get; set; }

        public void Fix()
        {
            throw new NotImplementedException();
        }
    }
}
