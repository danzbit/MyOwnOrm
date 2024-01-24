using MyOwnORM;
using MyOwnORM.Attributes;
using SeaBattleDomainModel.Abstractions;

namespace SeaBattleDomainModel.Models
{
    public class Field
    {
        [CustomPrimaryKey("Id")]
        public Guid Id { get; set; }
        [StringLength(56, ErrorMessage = "{0} value does not match the mask {1}.")]
        public string Name { get; set; }

        public List<Ship> Ships { get; set; }

        public List<Coords> Points { get; set; }
    }
}