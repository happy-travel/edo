﻿using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Data.Customers;
using HappyTravel.Edo.Data.Infrastructure;
using HappyTravel.Edo.Data.Locations;
using HappyTravel.Edo.Data.Management;
using HappyTravel.Edo.Data.Numeration;
using HappyTravel.Edo.Data.Payments;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Data
{
    public class EdoContext : DbContext
    {
        public EdoContext(DbContextOptions<EdoContext> options) : base(options)
        {

        }

        [DbFunction("jsonb_to_string")]
        public static string JsonbToString(string target)
            => throw new Exception();

        public Task<long> GetNextItineraryNumber()
        {
            return ExecuteScalarCommand<long>($"SELECT nextval('{ItnSequence}')");
        }

        public Task<int> GenerateNextItnMember(string itn)
        {
            var entityInfo = this.GetEntityInfo<ItnNumerator>();
            string currentNumberColumn = entityInfo.PropertyMapping[nameof(ItnNumerator.CurrentNumber)];
            string itnNumberColumn = entityInfo.PropertyMapping[nameof(ItnNumerator.ItineraryNumber)];;
            
            return ItnNumerators.FromSql($"UPDATE {entityInfo.Schema}.\"{entityInfo.Table}\" SET \"{currentNumberColumn}\" = \"{currentNumberColumn}\" + 1 WHERE \"{itnNumberColumn}\" = '{itn}' RETURNING *;", itn)
                .Select(c => c.CurrentNumber)
                .SingleAsync();
        }

        public Task RegisterItn(string itn)
        {
            ItnNumerators.Add(new ItnNumerator
            {
                ItineraryNumber = itn,
                CurrentNumber = 0
            });
            return SaveChangesAsync();
        }

        public async Task<bool> TryAddEntityLock(string lockId, string lockerInfo, string token)
        {
            var entityInfo = this.GetEntityInfo<EntityLock>();
            var lockIdColumn = entityInfo.PropertyMapping[nameof(EntityLock.EntityDescriptor)];
            var lockerInfoColumn = entityInfo.PropertyMapping[nameof(EntityLock.LockerInfo)];
            var tokenColumn = entityInfo.PropertyMapping[nameof(EntityLock.Token)];

            var sql = "WITH inserted AS " +
                      $"(INSERT INTO {entityInfo.Schema}.\"{entityInfo.Table}\" (\"{lockIdColumn}\", \"{lockerInfoColumn}\", \"{tokenColumn}\") " +
                      $"VALUES ('{lockId}', '{lockerInfo}', '{token}') ON CONFLICT (\"{lockIdColumn}\") DO NOTHING  RETURNING \"{tokenColumn}\") " +
                      $"SELECT \"{tokenColumn}\" FROM inserted " + 
                      $"UNION SELECT \"{tokenColumn}\" FROM public.\"{entityInfo.Table}\" "+
                      $"WHERE \"{lockIdColumn}\" = '{lockId}';";

            var currentLockToken = await ExecuteScalarCommand<string>(sql);
            return currentLockToken == token;
        }
        
        public Task RemoveEntityLock(string lockId)
        {
            var entityMapping = this.GetEntityInfo<EntityLock>();
            return ExecuteNonQueryCommand($"DELETE FROM {entityMapping.Schema}.\"{entityMapping.Table}\" where \"{entityMapping.PropertyMapping[nameof(EntityLock.EntityDescriptor)]}\" = '{lockId}';");
        }

        private DbSet<ItnNumerator> ItnNumerators { get; set; }
        
        private async Task<T> ExecuteScalarCommand<T>(string commandText)
        {
            using (var command = CreateCommand(commandText))
                return (T) (await command.ExecuteScalarAsync());
        }
        
        private async Task ExecuteNonQueryCommand(string commandText)
        {
            using (var command = CreateCommand(commandText))
                await command.ExecuteNonQueryAsync();
        }

        private DbCommand CreateCommand(string commandText)
        {
            var command = Database.GetDbConnection().CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = commandText;

            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();
            
            return command;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp");

            builder.HasSequence<long>(ItnSequence)
                .StartsAt(1)
                .IncrementsBy(1);

            BuildLocation(builder);
            BuildCountry(builder);
            BuildRegion(builder);
            BuildCustomer(builder);
            BuildCompany(builder);
            BuildCustomerCompanyRelation(builder);
            BuildBooking(builder);
            BuildItnNumerator(builder);
            BuildInvitations(builder);
            BuildAdministrators(builder);
            BuildPaymentAccounts(builder);
            BuildAuditEventLog(builder);
            BuildAccountAuditEventLog(builder);
            BuildEntityLocks(builder);

            DataSeeder.AddData(builder);
        }

        private void BuildEntityLocks(ModelBuilder builder)
        {
            builder.Entity<EntityLock>(entityLock =>
            {
                entityLock.HasKey(l => l.EntityDescriptor);
                entityLock.Property(l => l.Token).IsRequired();
                entityLock.Property(l => l.LockerInfo).IsRequired();
            });
        }

        private void BuildLocation(ModelBuilder builder)
        {
            builder.Entity<Location>(loc =>
            {
                loc.HasKey(l => l.Id);
                loc.Property(l => l.Id).HasDefaultValueSql("uuid_generate_v4()").IsRequired();
                loc.Property(l => l.Coordinates).HasColumnType("geography (point)").IsRequired();
                loc.Property(l => l.Name).HasColumnType("jsonb").IsRequired();
                loc.Property(l => l.Locality).HasColumnType("jsonb").IsRequired();
                loc.Property(l => l.Country).HasColumnType("jsonb").IsRequired();
                loc.Property(l => l.DistanceInMeters).IsRequired();
                loc.Property(l => l.Source).IsRequired();
                loc.Property(l => l.Type).IsRequired();
            });
        }

        private void BuildAuditEventLog(ModelBuilder builder)
        {
            builder.Entity<ManagementAuditLogEntry>(log =>
            {
                log.HasKey(l => l.Id);
                log.Property(l => l.Created).IsRequired();
                log.Property(l => l.Type).IsRequired();
                log.Property(l => l.AdministratorId).IsRequired();
                log.Property(l => l.EventData).IsRequired();
            });
        }

        private void BuildInvitations(ModelBuilder builder)
        {
            builder.Entity<UserInvitation>(inv =>
            {
                inv.HasKey(i => i.CodeHash);
                inv.Property(i => i.Created).IsRequired();
                inv.Property(i => i.Data).IsRequired();
                inv.Property(i => i.Email).IsRequired();
                inv.Property(i => i.IsAccepted).HasDefaultValue(false);
                inv.Property(i => i.InvitationType).IsRequired();
            });
        }
        
        private void BuildAdministrators(ModelBuilder builder)
        {
            builder.Entity<Administrator>(adm =>
            {
                adm.HasKey(a => a.Id);
                adm.Property(a => a.LastName).IsRequired();
                adm.Property(a => a.FirstName).IsRequired();
                adm.Property(a => a.Position).IsRequired();
                adm.Property(a => a.Email).IsRequired();
                adm.HasIndex(a => a.IdentityHash);
            });
        }
        
        private void BuildPaymentAccounts(ModelBuilder builder)
        {
            builder.Entity<PaymentAccount>(acc =>
            {
                acc.HasKey(a => a.Id);
                acc.Property(a => a.Currency).IsRequired();
                acc.Property(a => a.CompanyId).IsRequired();
            });
        }

        private void BuildItnNumerator(ModelBuilder builder)
        {
            builder.Entity<ItnNumerator>()
                .HasKey(n => n.ItineraryNumber);
        }

        private static void BuildCountry(ModelBuilder builder)
        {
            builder.Entity<Country>()
                .HasKey(c => c.Code);
            builder.Entity<Country>()
                .Property(c => c.Code)
                .IsRequired();
            builder.Entity<Country>()
                .Property(c => c.Names)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Country>()
                .Property(c => c.RegionId)
                .IsRequired();
        }

        private static void BuildRegion(ModelBuilder builder)
        {
            builder.Entity<Region>()
                .HasKey(c => c.Id);
            builder.Entity<Region>()
                .Property(c => c.Id)
                .IsRequired();
            builder.Entity<Region>()
                .Property(c => c.Names)
                .HasColumnType("jsonb")
                .IsRequired();
        }

        private void BuildCustomer(ModelBuilder builder)
        {
            builder.Entity<Customer>(customer =>
            {
                customer.HasKey(c => c.Id);
                customer.Property(c => c.Id).ValueGeneratedOnAdd();
                customer.Property(c => c.Email).IsRequired();
                customer.Property(c => c.Title).IsRequired();
                customer.Property(c => c.FirstName).IsRequired();
                customer.Property(c => c.LastName).IsRequired();
                customer.Property(c => c.FirstName).IsRequired();
                customer.Property(c => c.Position).IsRequired();
                customer.Property(c => c.IdentityHash).IsRequired();
            });
        }

        private void BuildCompany(ModelBuilder builder)
        {
            builder.Entity<Company>(company =>
            {
                company.HasKey(c => c.Id);
                company.Property(c => c.Id).ValueGeneratedOnAdd();
                company.Property(c => c.Address).IsRequired();
                company.Property(c => c.City).IsRequired();
                company.Property(c => c.CountryCode).IsRequired();
                company.Property(c => c.Name).IsRequired();
                company.Property(c => c.Phone).IsRequired();
                company.Property(c => c.PreferredCurrency).IsRequired();
                company.Property(c => c.PreferredPaymentMethod).IsRequired();
                company.Property(c => c.State).IsRequired();
            });
        }

        private void BuildCustomerCompanyRelation(ModelBuilder builder)
        {
            builder.Entity<CustomerCompanyRelation>(relation =>
            {
                relation.ToTable("CustomerCompanyRelations");

                relation.HasKey(r => new { r.CustomerId, r.CompanyId });
                relation.Property(r => r.CompanyId).IsRequired();
                relation.Property(r => r.CustomerId).IsRequired();
                relation.Property(r => r.Type).IsRequired();
            });
        }

        private void BuildBooking(ModelBuilder builder)
        {
            builder.Entity<Booking.Booking>(booking =>
            {
                booking.HasKey(b => b.Id);
                
                booking.Property(b => b.CustomerId).IsRequired();
                booking.HasIndex(b => b.CustomerId);
                
                booking.Property(b => b.CompanyId).IsRequired();
                booking.HasIndex(b => b.CompanyId);
                
                booking.Property(b => b.ReferenceCode).IsRequired();
                booking.HasIndex(b => b.ReferenceCode);
                
                booking.Property(b => b.BookingDetails)
                    .HasColumnType("jsonb");
                
                booking.Property(b => b.ServiceDetails)
                    .HasColumnType("jsonb");
                
                booking.Property(b => b.Status).IsRequired();
                booking.Property(b => b.ItineraryNumber).IsRequired();
                booking.HasIndex(b => b.ItineraryNumber);
                
                booking.Property(b => b.MainPassengerName).IsRequired();
                booking.HasIndex(b => b.MainPassengerName);

                booking.Property(b => b.ServiceType).IsRequired();
                booking.HasIndex(b => b.ServiceType);
            });
        }

        private void BuildAccountAuditEventLog(ModelBuilder builder)
        {
            builder.Entity<AccountBalanceAuditLogEntry>(log =>
            {
                log.HasKey(l => l.Id);
                log.Property(l => l.Created).IsRequired();
                log.Property(l => l.Type).IsRequired();
                log.Property(l => l.AccountId).IsRequired();
                log.Property(l => l.UserType).IsRequired();
                log.Property(l => l.UserId).IsRequired();
                log.Property(l => l.Amount).IsRequired();
                log.Property(l => l.EventData).IsRequired();
            });
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerCompanyRelation> CustomerCompanyRelations { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Region> Regions { get; set; }

        private const string ItnSequence = "itn_seq";
        public DbSet<Booking.Booking> Bookings { get; set; }
        
        public DbSet<UserInvitation> UserInvitations { get; set; }
        
        public DbSet<PaymentAccount> PaymentAccounts { get; set; }
        
        public DbSet<Administrator> Administrators { get; set; }
        
        public DbSet<ManagementAuditLogEntry> ManagementAuditLog { get; set; }
        public DbSet<AccountBalanceAuditLogEntry> AccountBalanceAuditLogs { get; set; }
    }
}
