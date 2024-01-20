using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyOwnORM;
using SeaBattleDomainModel.Enums;
using SeaBattleDomainModel.Models;

namespace SeaBattleDomainModel.Abstractions
{
    public abstract class Ship
    {
        public Guid Id { get; set; }

        public int Speed { get; set; }

        public Position Position { get; set; }

        public Direction Direction { get; set; }
        [ForeignKey(typeof(Field))]
        public string FieldId { get; set; } 
    }
}
