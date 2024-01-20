using MyOwnORM;
using MyOwnORM.Attributes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Models
{
    public class Coords
    {
        [CustomPrimaryKey("CoordId")]
        public Guid Id { get; set; }

        [Column("PointX")]
        public int X { get; set; }

        [Column("PointY")]
        public int Y { get; set; }
        [NotMapped]
        public int Quadrant { get; set; } // квадрант в координатной четверти. не маппить в бд!! по 3нф
        [ForeignKey(typeof(Field))]
        public string FieldId { get; set; }    
        [ForeignKey(typeof(Position))]
        public string PostionId { get; set; }
    }
}
