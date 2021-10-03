using Dapper;
using Discount.API.Entities;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discount.API.Repository
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly IConfiguration _configuration;

        public DiscountRepository(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public async Task<Coupon> GetDiscount(string productName)
        {
            using var connection = new NpgsqlConnection(_configuration.GetValue<string>("DabaseSettings:ConnectionString"));
            var coupon = await connection.QueryFirstOrDefaultAsync<Coupon>($"SELECT * FROM Coupon Where ProductName = '{productName}'").ConfigureAwait(false);

            if(coupon == null)
            {
                return new Coupon 
                { ProductName = "No Discount", Amount = 0.0, Description = "No Discount Desc" };
            }

            return coupon;
        }

        public async Task<bool> CreateDiscount(Coupon coupon)
        {
            using var conncetion = new NpgsqlConnection(_configuration.GetValue<string>("DabaseSettings:ConnectionString"));

            var affected = await conncetion.ExecuteAsync($"Insert into Coupon (ProductName, Description, Amount) Values('{coupon.ProductName}','{coupon.Description}',{coupon.Amount})").ConfigureAwait(false);

            if(affected == 0)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateDiscount(Coupon coupon)
        {
            using var conncetion = new NpgsqlConnection(_configuration.GetValue<string>("DabaseSettings:ConnectionString"));

            var affected = await conncetion.ExecuteAsync($"Update Coupon set Description = '{coupon.Description}',Amount = {coupon.Amount} where ProductName = '{coupon.ProductName}'").ConfigureAwait(false);

            if (affected == 0)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteDiscount(string productName)
        {
            using var conncetion = new NpgsqlConnection(_configuration.GetValue<string>("DabaseSettings:ConnectionString"));

            var affected = await conncetion.ExecuteAsync($"Delete from Coupon where ProductName = '{productName}'").ConfigureAwait(false);

            if (affected == 0)
            {
                return false;
            }

            return true;
        }
    }
}
