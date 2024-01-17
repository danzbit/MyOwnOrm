using MyOwnORM;
using MyOwnORM.Attributes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattleDomainModel.Models
{
    [Table("Coord")]
    public class Coords
    {
        [CustomPrimaryKey("CoordId")]
        public Guid Id { get; set; }

        [Column("PointX")]
        public int X { get; set; }

        [Column("PointY")]
        public int Y { get; set; }
        [NotMapped]
        public int Quadrant
        {
            get { return Quadrant; }
            set
            {
                //логика сеттинга квадранта
            }
        } // квадрант в координатной четверти. не маппить в бд!! по 3нф
        [ForeignKey(typeof(Position))]
        public int FieldId { get; set; }    
        [ForeignKey(typeof(Position))]
        public int PositionId { get; set; }
    }
}
