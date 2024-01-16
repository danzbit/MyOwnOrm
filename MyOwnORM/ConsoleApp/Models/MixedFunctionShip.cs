using MyOwnORM;
using SeaBattleDomainModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Models
{
    [Table("MixedFunctionShip")]
    internal class MixedFunctionShip : Ship, IShootable, IRepairable
    {
        [Column("MaxDistanceToShoot")]
        public int MaxDistanceToShoot { get; set; }
        [Column("MaxDistanceToFix")]
        public int MaxDistanceToFix { get; set; }

        public void Fix()
        {
            throw new NotImplementedException();
        }

        public void Shoot()
        {
            throw new NotImplementedException();
        }
    }
}
