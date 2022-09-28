using Microsoft.EntityFrameworkCore;
using SQLite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite.Data;

public class DataContext : DbContext
{
    public DbSet<Host> Hosts { get; set; }
    public DbSet<Share> Shares { get; set; }
    public DbSet<User> Users { get; set; }
    public string ConnectionString { get; } = "Data source=db.sql";

    public DataContext() { }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options) { }


    public DataContext(string connectionString)
    {
        ConnectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options
                .UseLazyLoadingProxies()
                .UseSqlite(ConnectionString);
        }
    }

}

