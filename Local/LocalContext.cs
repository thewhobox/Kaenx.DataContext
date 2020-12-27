using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kaenx.DataContext.Local
{
    public class LocalContext : DbContext
    {
        public DbSet<LocalProject> Projects { get; set; }
        public DbSet<LocalConnectionProject> ConnsProject { get; set; }
        public DbSet<LocalConnectionCatalog> ConnsCatalog { get; set; }
        public DbSet<LocalInterface> Interfaces { get; set; }
        public DbSet<LocalRemote> Remotes { get; set; }

        private bool generatePath;


        public LocalContext()
        {
            generatePath = false;
        }

        public LocalContext(bool _generatePath = false)
        {
            generatePath = _generatePath;
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string path = generatePath ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Local.db") : "Local.db";

            optionsBuilder.UseSqlite("Data Source=" + path);
        }
    }
}
