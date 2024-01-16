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
            Coords = new CustomDbSetRepository<Coords>(connectionString);
            Fields = new CustomDbSetRepository<Field>(connectionString);
            MilitaryShips = new CustomDbSetRepository<MilitaryShip>(connectionString);
            MixedFunctionShips = new CustomDbSetRepository<MixedFunctionShip>(connectionString);
            Positions = new CustomDbSetRepository<Position>(connectionString);
            RepairerShips = new CustomDbSetRepository<RepairerShip>(connectionString);
        }
        public CustomDbSetRepository<Coords> Coords { get; set; }
        public CustomDbSetRepository<Field> Fields { get; set; }
        public CustomDbSetRepository<MilitaryShip> MilitaryShips { get; set; }
        internal CustomDbSetRepository<MixedFunctionShip> MixedFunctionShips { get; set; }
        public CustomDbSetRepository<Position> Positions { get; set; }
        internal CustomDbSetRepository<RepairerShip> RepairerShips { get; set; }
    }
    public class Program
    {
        public static async void StartProgram()
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
            dynamic p3 = await context.Coords.FromSqlRawAsync($"SELECT * FROM Cords WHERE Id = {Guid.NewGuid()}");
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