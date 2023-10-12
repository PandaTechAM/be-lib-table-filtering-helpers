﻿using Microsoft.EntityFrameworkCore;
using PandaTech.IEnumerableFilters;
using PandaTech.IEnumerableFilters.Dto;

namespace TestFilters.Controllers;

public class Context : DbContext
{
    public virtual DbSet<Person> Persons { get; set; } = null!;
    public virtual DbSet<Cat> Cats { get; set; } = null!;
    public virtual DbSet<Phrase> Phrases { get; set; } = null!;
    public virtual DbSet<Dummy> Dummies { get; set; } = null!;


    private IServiceProvider ServiceProvider { get; }

    public Context(DbContextOptions<Context> options, IServiceProvider serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;
    }

    public List<PersonDto> GetPersons(GetDataRequest request, int page, int pageSize, FilterProvider filterProvider)
    {
        var mapper = ServiceProvider.GetRequiredService<PandaTech.Mapper.IMapping<Person, PersonDto>>();

        return Persons
            .Include(x => x.Cats)
            .Include(x => x.FavoriteCat)
            .ApplyFilters(request.Filters, filterProvider)
            .ApplyOrdering(request.Order, filterProvider)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsEnumerable()
            .Select(mapper.Map).ToList();
    }
}