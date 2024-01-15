using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using MyOwnORM;
using TableAttribute = MyOwnORM.TableAttribute;
using ForeignKeyAttribute = MyOwnORM.ForeignKeyAttribute;
using StringLengthAttribute = MyOwnORM.StringLengthAttribute;

namespace Program
{
    public class Context : CustomDbContext
    {
        public Context(string connectionString) : base(connectionString)
        {
            People = new CustomDbSetRepository<Person>(connectionString);
            Author = new CustomDbSetRepository<Author>(connectionString);
            Book = new CustomDbSetRepository<Book>(connectionString);
            Libraries = new CustomDbSetRepository<Library>(connectionString);
            Librarians = new CustomDbSetRepository<Librarian>(connectionString);
        }
        public CustomDbSetRepository<Person> People { get; set; }
        public CustomDbSetRepository<Author> Author { get; set; }
        public CustomDbSetRepository<Book> Book { get; set; }
        public CustomDbSetRepository<Library> Libraries { get; set; }
        public CustomDbSetRepository<Librarian> Librarians { get; set; }
    }
    public class Program
    {
        public static async void StartProgram()
        {
            Context context = new Context("Data Source=1068001579A\\SQLEXPRESS;Initial Catalog=TestDbMyOrm;Trusted_Connection=True;TrustServerCertificate=True;");
            var p = await context.People.GetAllAsync();
            //GetAll with filter
            var p1 = await context.People.WhereAsync(p => p.Id == 2);
            var p2 = await context.People.GetByIdAsync(1);
            var newPerson = new Person()
            {
                Id = 1,
                Name = "Nikita Beloshapka",
                Age = 20
            };
            await context.People.InsertAsync(newPerson);
            var newPerson1 = new Person()
            {
                Id = 1,
                Name = "Nikita Beloshapka",
                Age = 20,
                Author = new Author
                {
                    Id = 1,
                    Name = "Danya Zbitnyev",
                    PersonId = 1,
                },
                Sportsmen = new Sportsmen
                {
                    Id = 1,
                    Name = "Kiriil Drygach",
                    IdPerson = 1,
                },
                Book = new List<Book>
                {
                    new Book()
                    {
                        Id = 1,
                        Title = "Title",
                        PersonId = 1
                    },
                    new Book()
                    {
                        Id = 2,
                        Title = "Title",
                        PersonId = 1
                    },
                    new Book()
                    {
                        Id = 3,
                        Title = "Title",
                        PersonId = 1
                    }
                }
            };
            //Cascade insert
            await context.People.InsertCascadeAsync(newPerson1);
            var updatePerson = new Person()
            {
                Id = 1,
                Name = "Nikita Beloshapka",
                Age = 20
            };
            await context.People.UpdateAsync(newPerson);
            var updatePerson1 = new Person()
            {
                Id = 1,
                Name = "Nikita Beloshapka",
                Age = 20,
                Author = new Author
                {
                    Id = 1,
                    Name = "Danya Zbitnyev",
                    PersonId = 1,
                },
                Sportsmen = new Sportsmen
                {
                    Id = 1,
                    Name = "Kiriil Drygach",
                    IdPerson = 1,
                },
                Book = new List<Book>
                {
                    new Book()
                    {
                        Id = 1,
                        Title = "Title",
                        PersonId = 1
                    },
                    new Book()
                    {
                        Id = 2,
                        Title = "Title",
                        PersonId = 1
                    },
                    new Book()
                    {
                        Id = 3,
                        Title = "Title",
                        PersonId = 1
                    }
                }
            };
            //Cascade update
            await context.People.UpdateCascadeAsync(newPerson1);
            await context.People.DeleteAsync(p => p.Id == 1);
            //Cascade delete
            await context.People.DeleteCascadeAsync(p => p.Id == 1);
            dynamic p3 = await context.People.FromSqlRawAsync("SELECT * FROM Person WHERE Id = 2");
            //Eager loading
            var p4 = context.People.IncludeAsync(p => p.Author);
            Expression<Func<Person, object>>[] expressions = new Expression<Func<Person, object>>[]
            {
                a => a.Author,
                b => b.Book,
                s => s.Sportsmen
            };
            //Eager loading with expression arrays
            var p5 = await context.People.IncludeAsync(expressions);
        }
        public static void Main()
        {
            StartProgram();
        }
    }
    [Table("Person")]
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public Author Author { get; set; }
        public Sportsmen Sportsmen { get; set; }
        public List<Book> Book { get; set; }
    }
    [Table("Sportsmen")]
    public class Sportsmen
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [ForeignKey(typeof(Person))]
        public int IdPerson { get; set; }
    }
    [Table("Author")]
    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [ForeignKey(typeof(Person))]
        public int PersonId { get; set; }
    }
    [Table("Library")]
    public class Library
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Librarian Librarians { get; set; }
    }
    [Table("Librarian")]
    public class Librarian
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [ForeignKey(typeof(Library))]
        public int LibraryId { get; set; }
    }

    public class Book
    {
        public int Id { get; set; }
        [StringLength(40, ErrorMessage = "{0} value does not match the mask {1}.")]
        public string Title { get; set; }
        public int PersonId { get; set; }
        public Person Person { get; set; } = null!;
    }
}