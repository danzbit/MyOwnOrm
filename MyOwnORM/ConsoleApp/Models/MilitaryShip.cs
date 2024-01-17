using MyOwnORM;
using SeaBattleDomainModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Models
{
    public class MilitaryShip : Ship, IShootable
    {
        [Column("DistanceToShootMax")]
        public int MaxDistanceToShoot { get; set; }

        public void Shoot()
        {
            throw new NotImplementedException();
        }
    }
}
