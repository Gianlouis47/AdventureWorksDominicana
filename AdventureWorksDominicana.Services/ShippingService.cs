using AdventureWorksDominicana.Data.Context;
using AdventureWorksDominicana.Data.Models;
using Aplicada1.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AdventureWorksDominicana.Services
{
    public class ShippingService
    {
        private readonly Contexto _context;

        public ShippingService(Contexto context)
        {
            _context = context;
        }

        public async Task<List<SalesOrderHeader>> GetShippingOrdersAsync(DateTime? from, DateTime? to, string? search)
        {
            var query = _context.SalesOrderHeaders
                .AsNoTracking()
                .Include(o => o.Customer)
                    .ThenInclude(c => c.Person)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.Store)
                .Include(o => o.ShipMethod)
                .Include(o => o.ShipToAddress)
                .Where(o => o.Status == 1 || o.Status == 5);

            if (from.HasValue)
            {
                query = query.Where(o => o.OrderDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(o => o.OrderDate < to.Value.Date.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(o =>
                    o.SalesOrderId.ToString().Contains(search) ||
                    (o.Customer.Person != null &&
                        ((o.Customer.Person.FirstName ?? string.Empty) + " " +
                         (o.Customer.Person.LastName ?? string.Empty)).Contains(search)) ||
                    (o.Customer.Store != null &&
                        (o.Customer.Store.Name ?? string.Empty).Contains(search)));
            }

            return await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<SalesOrderHeader?> GetOrderAsync(int orderId)
        {
            return await _context.SalesOrderHeaders
                .AsNoTracking()
                .Include(o => o.Customer)
                    .ThenInclude(c => c.Person)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.Store)
                .Include(o => o.ShipMethod)
                .Include(o => o.ShipToAddress)
                .Include(o => o.BillToAddress)
                .Include(o => o.SalesOrderDetails)
                .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);
        }

        public async Task<List<ShipMethod>> GetShipMethodsAsync()
        {
            return await _context.ShipMethods
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<decimal> CalculateFreightAsync(int shipMethodId, decimal subtotal)
        {
            var shipMethod = await _context.ShipMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShipMethodId == shipMethodId);

            if (shipMethod == null)
            {
                throw new InvalidOperationException("El método de envío no existe.");
            }

            return Math.Round(shipMethod.ShipBase + (subtotal * shipMethod.ShipRate), 2);
        }

        public async Task ConfirmShipmentAsync(int orderId, int shipMethodId)
        {
            var order = await _context.SalesOrderHeaders
                .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

            if (order == null)
            {
                throw new InvalidOperationException("La orden no fue encontrada.");
            }

            if (order.Status == 5)
            {
                throw new InvalidOperationException("La orden ya fue enviada.");
            }

            var subtotal = await _context.SalesOrderDetails
                .Where(d => d.SalesOrderId == orderId)
                .SumAsync(d => d.LineTotal);

            var freight = await CalculateFreightAsync(shipMethodId, subtotal);

            order.ShipMethodId = shipMethodId;
            order.ShipDate = DateTime.Now;
            order.ModifiedDate = DateTime.Now;
            order.SubTotal = subtotal;
            order.Freight = freight;
            order.TotalDue = subtotal + order.TaxAmt + freight;
            order.Status = 5;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateShipmentAsync(int orderId, int shipMethodId, string? comment)
        {
            var order = await _context.SalesOrderHeaders
                .FirstOrDefaultAsync(o => o.SalesOrderId == orderId);

            if (order == null)
            {
                throw new InvalidOperationException("La orden no fue encontrada.");
            }

            if (order.Status != 5)
            {
                throw new InvalidOperationException("Solo se puede editar un envío ya confirmado.");
            }

            var subtotal = await _context.SalesOrderDetails
                .Where(d => d.SalesOrderId == orderId)
                .SumAsync(d => d.LineTotal);

            var freight = await CalculateFreightAsync(shipMethodId, subtotal);

            order.ShipMethodId = shipMethodId;
            order.Comment = comment;
            order.ModifiedDate = DateTime.Now;
            order.SubTotal = subtotal;
            order.Freight = freight;
            order.TotalDue = subtotal + order.TaxAmt + freight;

            await _context.SaveChangesAsync();
        }
    }
}