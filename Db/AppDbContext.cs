using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Server.DB.DataModel;

namespace Server.DB
{
    public class AppDbContext : DbContext
    {
        public DbSet<AccountDb> Accounts { get; set; }
        public DbSet<PlayerDb> Players { get; set; }
        public DbSet<ItemDb> Items { get; set; }
        public DbSet<QuestDb> Quests { get; set; }

        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        string _connectionDb = "Server =tcp:127.0.0.1,1433; Database=EfCore; User =sa; Password=6ehd809gh!!!; TrustServerCertificate=true;";
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder
                //.UseLoggerFactory(MyLoggerFactory) //sql 출력 
                .UseSqlServer(_connectionDb);//Db에 접속
            //Config Manager ? 
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AccountDb>() //non-clustered 인덱스 
                .HasIndex("AccountDbName")
                .IsUnique();

            builder.Entity<PlayerDb>()//non-clustered 인덱스 
                .HasIndex("PlayerDbName")
                .IsUnique();

        }
    }
}

