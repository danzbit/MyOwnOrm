using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyOwnORM
{
    public class Context : CustomDbContext
    {
        public Context(string connectionString) : base(connectionString)
        {
            People = new CustomDbSet<Person>(connectionString);
            Author = new CustomDbSet<Author>(connectionString);
            Book = new CustomDbSet<Book>(connectionString);
            Libraries = new CustomDbSet<Library>(connectionString);
            Librarians = new CustomDbSet<Librarian>(connectionString);
        }
        public CustomDbSet<Person> People { get; set; }
        public CustomDbSet<Author> Author { get; set; }
        public CustomDbSet<Book> Book { get; set; }
        public CustomDbSet<Library> Libraries { get; set; }
        public CustomDbSet<Librarian> Librarians { get; set; }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            Context context = new Context("Data Source=1068001579A\\SQLEXPRESS;Initial Catalog=TestDbMyOrm;Trusted_Connection=True;TrustServerCertificate=True;");
            var p = context.People.GetAll();
            //GetAll with filter
            var p1 = context.People.Where(p => p.Id == 2);
            var p2 = context.People.GetById(1);
            var newPerson = new Person()
            {
                Id = 1,
                Name = "Nikita Beloshapka",
                Age = 20
            };
            context.People.Insert(newPerson);
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
            context.People.InsertCascade(newPerson1);
            var updatePerson = new Person()
            {
                Id = 1,
                Name = "Nikita Beloshapka",
                Age = 20
            };
            context.People.Update(newPerson);
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
            context.People.UpdateCascade(newPerson1);
            context.People.Delete(p => p.Id == 1);
            //Cascade delete
            context.People.DeleteCascade(p => p.Id == 1);
            dynamic p3 = context.People.FromSqlRaw("SELECT * FROM Person WHERE Id = 2");
            //Eager loading
            var p4 = context.People.Include(p => p.Author);
            Expression<Func<Person, object>>[] expressions = new Expression<Func<Person, object>>[]
            {
                a => a.Author,
                b => b.Book,
                s => s.Sportsmen
            };
            //Eager loading with expression arrays
            var p5 = context.People.Include(expressions);
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
        [CascadeDelete(typeof(Person))]
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
        [CascadeDelete(typeof(Library))]
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
