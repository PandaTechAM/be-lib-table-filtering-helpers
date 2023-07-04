using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BaseConverter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PandaTech.IEnumerableFilters;
using PandaTech.IEnumerableFilters.Dto;
using PandaTech.Mapper;
using static System.Linq.Expressions.Expression;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TestFilters.Controllers;

[ApiController]
[Route("[controller]")]
public class SomeController : ControllerBase
{
    private readonly Context _context;
    private readonly FilterProvider _filterProvider;
    private readonly Counter _counter;
    private readonly UpCounter2 _upCounter2;
    private readonly UpCounter _upCounter;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _client;
    private readonly IMapping<Person, PersonDto> _personDtoMapper;

    public SomeController(Context context, Counter counter, UpCounter2 upCounter2, UpCounter upCounter,
        IServiceProvider serviceProvider, HttpClient client, IMapping<Person, PersonDto> personDtoMapper, FilterProvider filterProvider)
    {
        _context = context;
        _counter = counter;
        _upCounter2 = upCounter2;
        _upCounter = upCounter;
        _serviceProvider = serviceProvider;
        _client = client;
        _personDtoMapper = personDtoMapper;
        _filterProvider = filterProvider;

        _filterProvider.Add<PersonDto, Person>();
        

        /*const string exp = @"person.Id == 122";
        var p = Expression.Parameter(typeof(Person), "x");
        var e = DynamicExpressionParser.ParseLambda(new[] { p }, null, exp);*/
        
        _filterProvider.Add(
            new FilterProvider.Filter
            {
                SourceType = typeof(PersonDto),
                TargetType = typeof(Person),
                ComparisonTypes = new List<ComparisonType>
                {
                    ComparisonType.Equal, ComparisonType.In, ComparisonType.NotEqual
                },
                Converter = id => PandaBaseConverter.Base36ToBase10(id as string) ?? -1,
                SourcePropertyName = nameof(PersonDto.Id),
                TargetPropertyType = typeof(long),
                TargetPropertyName = nameof(Person.PersonId),
                SourcePropertyType = typeof(string),
            }
        );
        
        /*_filterProvider.AddFilter(
            new FilterProvider.Filter
            {
                TableName = nameof(PersonDto),
                PropertyName = nameof(PersonDto.FavoriteCat),
                ComparisonTypes = new List<ComparisonType>
                {
                    ComparisonType.Equal, ComparisonType.In, ComparisonType.NotEqual
                },
                Converter = id => long.Parse(id as string ?? "-1"),
                SourcePropertyConverter = Property(Parameter(typeof(Person)), nameof(Person.FavoriteCatId)),
                FilterType = typeof(string),
                TargetPropertyType = typeof(long?)
            }
        );*/
        


        /*_filterProvider.AddFilter(
            new FilterProvider.Filter
            {
                TableName = nameof(PersonDto),
                PropertyName = nameof(PersonDto.Cats),
                ComparisonTypes = new List<ComparisonType>
                {
                    ComparisonType.In
                },
                Converter = value => (int)value,
                SourcePropertyConverter = Call(Property(Parameter(typeof(Person)), nameof(Person.Cats)),
                    typeof(IEnumerable<Cat>).GetMethod("Select", new Type[] { typeof(Cat)}),
                    new List<Expression>()
                    {
                        Property(Parameter(typeof(Cat)), nameof(Cat.Id))
                    }),
                FilterType = typeof(string),
                TargetPropertyType = typeof(Sex)
            }
        );*/
        
        /*
        _filterProvider.AddFilter(
            new FilterProvider.Filter
            {
                TableName = nameof(PersonDto),
                PropertyName = nameof(PersonDto.Name),
                ComparisonTypes = new List<ComparisonType>
                {
                    ComparisonType.Contains
                },
                Converter = name => (name as string)?.ToLower() ?? "",
                SourcePropertyConverter = Call(Property(Parameter(typeof(Person)), nameof(Person.Name)),
                    typeof(string).GetMethod(nameof(string.ToLower), new Type[] { })),
                FilterType = typeof(string),
                TargetPropertyType = typeof(long)
            }
        );*/
    }


    [HttpGet("[action]")]
    public List<PersonDto> test1()
    {
        Expression<Func<Person, bool>> ex;
        //var p = Expression.Parameter(typeof(Person), "x");
        //var exString = "@0.Contains(x.PersonId)";
        //var e = DynamicExpressionParser.ParseLambda(new[] { p }, typeof(bool), exString, list);

        //var a = "Asdasdas".StartsWith();
        
        
        return _context.Persons.Where("Name.StartsWith(@0)", "D").Take(10).AsEnumerable().Select(_personDtoMapper.Map).ToList();
    }

    [HttpGet("[action]")]
    public List<PersonDto> test2()
    {
        var a = new List<long> { 1, 2, 3 };
        
        return _context.Persons.Where($"@0.Contains({nameof(Person.PersonId)})", a).Take(10).AsEnumerable().Select(_personDtoMapper.Map).ToList();
       
        
    }


    [HttpPost("[action]")]
    public IActionResult PopulateDb()
    {
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        var start = DateTime.Now;

        const int count = 10_000;
        Console.Clear();

        var optionBuilder = new DbContextOptionsBuilder<Context>();
        optionBuilder.UseNpgsql("Server=127.0.0.1;Database=xyz;Username=postgres;Password=example");

        var tasks = new List<Task>();
        var catId = 1;
        var context = new Context(optionBuilder.Options, _serviceProvider);
        for (var i = 1; i <= count; i++)
        {
            var catCount = Random.Shared.Next(1, 4);
            var person = new Person
            {
                PersonId = i,
                Name = NameProvider.GetRandomName(),
                Age = Random.Shared.Next(15, 90),
                Cats = new List<Cat>(),
                Address = NameProvider.GetRandomAddress(),
                Email = "test@TEST.am",
                Sex = Enum.GetValues<Sex>()[i % 2],
                Money = Random.Shared.NextDouble() * 100000,
                Phone = "+37412345678",
                Surname = NameProvider.GetRandomName(),
                BirthDate = new DateTime(2000, 1, 1).AddDays(Random.Shared.Next(0, 10000)).ToUniversalTime(),
                IsHappy = Random.Shared.Next(0, 1) == 1,
                IsMarried = Random.Shared.Next(0, 3) == 0,
                IsWorking = Random.Shared.Next(0, 5) != 1,
            };

            for (var j = 0; j < catCount; j++)
            {
                person.Cats.Add(new Cat
                {
                    Id = catId++,
                    Name = NameProvider.GetRandomName(),
                    Age = Random.Shared.Next(1, 20),
                });
            }
            
            person.FavoriteCat = new Dummy();

            context.Add(person);

            if (i % 100 != 0) continue;
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(i);
                //context = new Context(optionBuilder.Options);
            }
        }

        tasks.Add(context.SaveChangesAsync());

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine(DateTime.Now - start);

        return Ok();
        return Ok();
    }

    [HttpGet("[action]")]
    public IActionResult Count()
    {
        return Ok($"{_counter.Count()} {_upCounter.Count()} {_upCounter2.Count()}");
    }

    [HttpGet("[action]")]
    public IActionResult Count2()
    {
        _client.BaseAddress = new Uri("http://localhost/Some/Count");

        var response = _client.GetAsync("").Result;
        var content = response.Content.ReadAsStringAsync().Result;
        response.Dispose(); // Dispose of the response

        response = _client.GetAsync("").Result;
        content += response.Content.ReadAsStringAsync().Result;
        response.Dispose(); // Dispose of the response

        response = _client.GetAsync("").Result;
        content += response.Content.ReadAsStringAsync().Result;
        response.Dispose(); // Dispose of the response

        response = _client.GetAsync("").Result;
        content += response.Content.ReadAsStringAsync().Result;
        response.Dispose(); // Dispose of the response

        return Ok(content);
    }


    [HttpGet("[action]")]
    public IActionResult DT(DateTime date)
    {
        return Ok(date);
    }


    [HttpPost("persons/{page}/{pageSize}")]
    public List<PersonDto> GetPerson([FromBody] GetDataRequest request, int page, int pageSize)
    {
        return _context.GetPersons(request, page, pageSize, _filterProvider);
    }
    
    [HttpGet("persons/{page}/{pageSize}")]
    public List<PersonDto> GetPerson([FromQuery] string request, int page, int pageSize)
    {
        return _context.GetPersons(GetDataRequest.FromString(request), page, pageSize, _filterProvider);
    }


    /*[HttpGet("GetFilters")]
    public List<FilterInfo> GetFilters(string tableName)
    {
        return _filterProvider.GetFilters(tableName);
    }

    [HttpGet("GetTables")]
    public List<string> GetTables()
    {
        return _filterProvider.GetTables();
    }
    */

    /*
    [HttpGet("DistinctColumnValues")]
    public List<string> DistinctColumnValues([FromQuery] string filtersString, string tableName, string columnName,
        int page,
        int pageSize)
    {
        var request = GetDataRequest.FromString(filtersString);

        if (request == null)
            return new List<string>();

        var type = _filterProvider.GetDbTable(tableName);
        if (type == null)
        {
            return new List<string>();
        }

        var dbSetType = typeof(DbSet<>).MakeGenericType(type);
        var set = _context.GetType().GetProperties().First(p => p.PropertyType == dbSetType);

        var context = Parameter(typeof(Context));
        var property = Property(context, set.Name);

        // call method GetDistinctColumnValues on property

        var method = typeof(EnumerableExtenders).GetMethod(nameof(EnumerableExtenders.DistinctColumnValues));
        var genericMethod = method?.MakeGenericMethod(type);

        var call = Call(
            genericMethod!,
            property,
            Constant(request.Filters),
            Constant(columnName),
            Constant(pageSize),
            Constant(page),
            Constant(0L)
        );

        var lambda = Lambda<Func<Context, List<string>>>(call, context);
        var func = lambda.Compile();
        var result = func(_context);


        return result;

        /*
        public static List<string> DistinctColumnValues<T>(this IEnumerable<T> dbSet, List<FilterDto> filters,
            string columnName,
            int pageSize, int page, out long totalCount) where T : class
            #1#
    }*/

    [HttpPost("FilterDto")]
    public string FilterDto()
    {
        var request = new GetDataRequest()
        {
            Aggregates = new List<AggregateDto>()
            {
                new()
                {
                    AggregateType = AggregateType.Max,
                    PropertyName = "Age"
                }
            },
            Filters = new List<FilterDto>(),
        };

        return request.ToString();
    }

    [HttpGet("GetPersons")]
    public FilteredDataResult<Person> GetPersons([FromQuery] string? filtersString, int page, int pageSize)
    {
        var now = DateTime.Now;
        var filters = JsonSerializer.Deserialize<GetDataRequest>(filtersString ?? "");

        if (filters == null)
        {
            return new FilteredDataResult<Person>();
        }

        var query = _context.Persons.ApplyFilters(filters.Filters, _filterProvider).ApplyOrdering(filters.Order);

        var response = new FilteredDataResult<Person>
        {
            Data = query.Include(p => p.Cats).Skip((page - 1) * pageSize).Take(pageSize)
                .ToList(),
            TotalCount = query.Count(),
            Aggregates = query.GetAggregates(filters.Aggregates)
        };
        return response;
    }
}

public class Phrase
{
    public string Text { get; set; } = null!;
    public DateTime Date { get; set; }
    [Key]
    public int Id { get; set; }
}

public class UpCounter
{
    private Counter _counter;

    public UpCounter(Counter counter)
    {
        _counter = counter;
    }

    public int Count() => _counter.Count();
}

public class UpCounter2
{
    private Counter _counter;

    public UpCounter2(Counter counter)
    {
        _counter = counter;
    }

    public int Count() => _counter.Count();
}