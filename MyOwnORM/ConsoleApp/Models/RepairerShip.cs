using MyOwnORM;
using SeaBattleDomainModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SeaBattleDomainModel
{
    [Table("RepairerShip")]
    internal class RepairerShip : Ship, IRepairable
    {
        [Column("MaxDistanceToFix")]
        public int MaxDistanceToFix { get; set; }

        public void Fix()
        {
            throw new NotImplementedException();
        }
    }
}
