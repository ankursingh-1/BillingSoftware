using Billing.Domain.Entities;
using Billing.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Persistence.Context
{
    public class BillingDbContext : DbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext>options)
            : base(options)
        {
        }
        public DbSet<Product> Products => Set <Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Tax> Taxes => Set<Tax>();
        public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
        public DbSet<Expense> Expenses => Set<Expense>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
            modelBuilder.ApplyConfiguration(new StockLedgerConfiguration());
            modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
        }
    }

}
