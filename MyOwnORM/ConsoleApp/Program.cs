using MyOwnORM;
using MyOwnORM.Implementations;
using SeaBattleDomainModel.Models;
using SeaBattleDomainModel;
using SeaBattleDomainModel.Abstractions;
using System.Linq.Expressions;
using SeaBattleDomainModel.Enums;

namespace Program
{
    public class Context : CustomDbContext
    {
        public Context(string connectionString) : base(connectionString)
        {
            Coords = new CustomDbSetRepository<Coords, Guid>(connectionString);
            Fields = new CustomDbSetRepository<Field, Guid>(connectionString);
            Positions = new CustomDbSetRepository<Position, Guid>(connectionString);
            Ships = new CustomDbSetRepository<Ship, Guid>(connectionString);
        }
        public CustomDbSetRepository<Ship, Guid> Ships { get; set; }
        public CustomDbSetRepository<Coords, Guid> Coords { get; set; }
        public CustomDbSetRepository<Field, Guid> Fields { get; set; }
        public CustomDbSetRepository<Position, Guid> Positions { get; set; }
    }
    public static class Program
    {
        public static async Task StartProgram()
        {
            Context context = new Context("Data Source=1068001579A\\SQLEXPRESS;Initial Catalog=TestDbMyOrm;Trusted_Connection=True;TrustServerCertificate=True;");
            //var p = await context.Coords.GetAllAsync();
            //GetAll with filter
            //Guid id = Guid.NewGuid();
            Guid id = new Guid("3fabaac8-aceb-41d7-a087-6a9967b965ff");
            //var p1 = await context.Coords.WhereAsync(p => p.Id == id);
            //var p2 = await context.Coords.GetByIdAsync(id);
            //var newCoord = new Coords()
            //{
            //    Id = Guid.NewGuid(),
            //    X = 45,
            //    Y = 35,
            //    Quadrant = 0,
            //    FieldId = "BA0A0B15-6CB7-4F2D-AF5D-69D1D8324804",
            //    PostionId = "0b9de308-5c7a-4464-9a82-66ed74400be3"
            //};
            //var ship = new MixedFunctionShip()
            //{
            //    Id = Guid.NewGuid(),
            //    Speed = 50,
            //    Direction = Direction.Up,
            //    MaxDistanceToFix = 20,
            //    MaxDistanceToShoot = 20,
            //    FieldId = "BA0A0B15-6CB7-4F2D-AF5D-69D1D8324804"
            //};
            ////var id = Guid.NewGuid();
            //var newPosition = new Position()
            //{
            //    Id = id,
            //    Name = "Gisdifsii",
            //    Points = new List<Coords>
            //    {
            //        new Coords()
            //        {
            //            Id= Guid.NewGuid(),
            //            X = 20,
            //            Y = 20,
            //            FieldId = "BA0A0B15-6CB7-4F2D-AF5D-69D1D8324804",
            //            PostionId = id.ToString(),
            //        },
            //        new Coords()
            //        {
            //            Id= Guid.NewGuid(),
            //            X = 30,
            //            Y = 30,
            //            FieldId = "BA0A0B15-6CB7-4F2D-AF5D-69D1D8324804",
            //            PostionId = id.ToString(),
            //        },
            //    },
            //    ShipId = "2c605500-67c5-4880-a638-7750c41d4d01"
            //};
            //await context.Coords.InsertAsync(newCoord);
            //var newPosition = new Position()
            //{
            //    Id = Guid.NewGuid(),
            //    Points = new List<Coords> 
            //    { 
            //        new Coords
            //        {
            //            Id = Guid.NewGuid(),
            //            X = 25,
            //            Y = 35,
            //            Quadrant = 1
            //        },
            //        new Coords
            //        {
            //            Id = Guid.NewGuid(),
            //            X = 15,
            //            Y = 35,
            //            Quadrant = 2
            //        },
            //        new Coords
            //        {
            //            Id = Guid.NewGuid(),
            //            X = 55,
            //            Y = 15,
            //            Quadrant = 25
            //        },
            //    }
            //};
            ////Cascade insert
            //await context.Positions.InsertCascadeAsync(newPosition);
            //var updateCoord = new Coords()
            //{
            //    Id = id,
            //    X = 15,
            //    Y = 15,
            //    Quadrant = 1
            //};
            //var updateShip = new MixedFunctionShip()
            //{
            //    Id = new Guid("440ba603-723f-48c8-af6f-12fd688be7b6"),
            //    Speed = 20,
            //    MaxDistanceToFix = 100,
            //    MaxDistanceToShoot = 100
            //};
            //await context.Ships.UpdateAsync(updateShip);
            //var updatePostions = new Position()
            //{
            //    Id = new Guid("BA0A0B15-6CB7-4F2D-AF5D-69D1D8324804"),
            //    Name = "Afanasii",
            //    Points = new List<Coords>
            //    {
            //        new Coords
            //        {
            //            Id = new Guid("3fabaac8-aceb-41d7-a087-6a9967b965ff"),
            //            X = 25,
            //            Y = 35,
            //            Quadrant = 1
            //        },
            //        new Coords
            //        {
            //            Id = new Guid("8959c528-98ba-4393-9f30-b87832a80a37"),
            //            X = 15,
            //            Y = 35,
            //            Quadrant = 2
            //        },
            //    }
            //};
            ////Cascade update
            //await context.Positions.UpdateCascadeAsync(updatePostions);
            Guid guid = new Guid("3fabaac8-aceb-41d7-a087-6a9967b965ff");
            //await context.Coords.DeleteAsync(p => p.Id == guid);
            //await context.Positions.DeleteByIdAsync(guid);
            ////Cascade delete
            //await context.Fields.DeleteCascadeAsync(p => p.Id == guid);
            //object p3 = await context.Coords.FromSqlRawAsync($"SELECT * FROM Coords WHERE CoordId = '{guid}'");
            ////Eager loading
            //var p4 = context.Positions.IncludeAsync(p => p.Points);
            Expression<Func<Field, object>>[] expressions = new Expression<Func<Field, object>>[]
            {
                s => s.Ships,
                p => p.Points
            };
            //Eager loading with expression arrays
            var p5 = await context.Fields.IncludeAsync(expressions);
        }
        public static void Main()
        {
            StartProgram();
        }
    }
}