using MyOwnORM;
using MyOwnORM.Implementations;
using SeaBattleDomainModel.Models;
using SeaBattleDomainModel;
using SeaBattleDomainModel.Abstractions;
using System.Linq.Expressions;

namespace Program
{
    public class Context : CustomDbContext
    {
        public Context(string connectionString) : base(connectionString)
        {
            Coords = new CustomDbSetRepository<Coords, Guid>(connectionString);
            Fields = new CustomDbSetRepository<Field, Guid>(connectionString);
            MilitaryShips = new CustomDbSetRepository<MilitaryShip, Guid>(connectionString);
            MixedFunctionShips = new CustomDbSetRepository<MixedFunctionShip, Guid>(connectionString);
            Positions = new CustomDbSetRepository<Position, Guid>(connectionString);
            RepairerShips = new CustomDbSetRepository<RepairerShip, Guid>(connectionString);
        }
        public CustomDbSetRepository<Coords, Guid> Coords { get; set; }
        public CustomDbSetRepository<Field, Guid> Fields { get; set; }
        public CustomDbSetRepository<MilitaryShip, Guid> MilitaryShips { get; set; }
        internal CustomDbSetRepository<MixedFunctionShip, Guid> MixedFunctionShips { get; set; }
        public CustomDbSetRepository<Position, Guid> Positions { get; set; }
        internal CustomDbSetRepository<RepairerShip, Guid> RepairerShips { get; set; }
    }
    public static class Program
    {
        public static async Task StartProgram()
        {
            Context context = new Context("Data Source=1068001579A\\SQLEXPRESS;Initial Catalog=TestDbMyOrm;Trusted_Connection=True;TrustServerCertificate=True;");
            var p = await context.Coords.GetAllAsync();
            //GetAll with filter
            Guid id = Guid.NewGuid();
            var p1 = await context.Coords.WhereAsync(p => p.Id == id);
            var p2 = await context.Coords.GetByIdAsync(id);
            var newCoord = new Coords()
            {
                Id = id,
                X = 45,
                Y = 35,
                Quadrant = 1
            };
            await context.Coords.InsertAsync(newCoord);
            var newPosition = new Position()
            {
                Id = Guid.NewGuid(),
                Points = new List<Coords> 
                { 
                    new Coords
                    {
                        Id = Guid.NewGuid(),
                        X = 25,
                        Y = 35,
                        Quadrant = 1
                    },
                    new Coords
                    {
                        Id = Guid.NewGuid(),
                        X = 15,
                        Y = 35,
                        Quadrant = 2
                    },
                    new Coords
                    {
                        Id = Guid.NewGuid(),
                        X = 55,
                        Y = 15,
                        Quadrant = 25
                    },
                }
            };
            //Cascade insert
            await context.Positions.InsertCascadeAsync(newPosition);
            var updateCoord = new Coords()
            {
                Id = id,
                X = 45,
                Y = 35,
                Quadrant = 1
            };
            await context.Coords.UpdateAsync(updateCoord);
            var updatePostions = new Position()
            {
                Id = Guid.NewGuid(),
                Points = new List<Coords>
                {
                    new Coords
                    {
                        Id = Guid.NewGuid(),
                        X = 25,
                        Y = 35,
                        Quadrant = 1
                    },
                    new Coords
                    {
                        Id = Guid.NewGuid(),
                        X = 15,
                        Y = 35,
                        Quadrant = 2
                    },
                    new Coords
                    {
                        Id = Guid.NewGuid(),
                        X = 55,
                        Y = 15,
                        Quadrant = 25
                    },
                }
            };
            //Cascade update
            await context.Positions.UpdateCascadeAsync(updatePostions);
            await context.Coords.DeleteAsync(p => p.Id == Guid.NewGuid());
            //Cascade delete
            await context.Positions.DeleteCascadeAsync(p => p.Id == Guid.NewGuid());
            object p3 = await context.Coords.FromSqlRawAsync($"SELECT * FROM Cords WHERE Id = {Guid.NewGuid()}");
            //Eager loading
            var p4 = context.Positions.IncludeAsync(p => p.Points);
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