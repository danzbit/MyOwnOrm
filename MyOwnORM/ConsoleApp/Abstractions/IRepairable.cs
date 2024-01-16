using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Abstractions
{
    internal interface IRepairable
    {
        int MaxDistanceToFix { get; set; }

        void Fix();
    }
}
