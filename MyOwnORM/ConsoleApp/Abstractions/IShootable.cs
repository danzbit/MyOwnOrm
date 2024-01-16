using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Abstractions
{
    internal interface IShootable
    {
        int MaxDistanceToShoot { get; set; }

        void Shoot();
    }
}
