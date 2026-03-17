using AdventureWorksDominicana.Data.Context;
using AdventureWorksDominicana.Data.Models;
using Aplicada1.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AdventureWorksDominicana.Services;

public class ShipMethodService(IDbContextFactory<Contexto> DbFactory): IService<ShipMethod, int> 
{
    //Metodo listar
    public async Task<List<ShipMethod>> Listar(Expression<Func<ShipMethod, bool>> criterio)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.ShipMethods.Where(criterio).ToListAsync();
    }

    //Metodo guardar
    public async Task<bool> Guardar(ShipMethod ship)
    {
        if (!await Existe(ship.ShipMethodId))
        {
            return await Insertar(ship);
        }
        else
        {
            return await Modificar(ship);
        }
    }

    //Metodo insertar
    public async Task<bool> Insertar(ShipMethod ship)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        contexto.ShipMethods.Add(ship);
        return await contexto.SaveChangesAsync() > 0;
    }

    //Metodo existe
    public async Task<bool> Existe(int idShip)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.ShipMethods.AnyAsync(p => p.ShipMethodId == idShip);
    }

    //metodo modificar
    public async Task<bool> Modificar(ShipMethod ship)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        contexto.ShipMethods.Update(ship);
        return await contexto.SaveChangesAsync() > 0;
    }

    //metodo buscar
    public async Task<ShipMethod?> Buscar(int idShip)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.ShipMethods.FirstOrDefaultAsync(p => p.ShipMethodId == idShip);
    }

    //eliminar
    public async Task<bool> Eliminar(int idShip)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        var ship = await Buscar(idShip);

        if (ship == null) return false;

        //Hard delete
        contexto.ShipMethods.Remove(ship);
        return await contexto.SaveChangesAsync() > 0;
    }

    public Task<List<ShipMethod>> GetList(Expression<Func<ShipMethod, bool>> criterio)
    {
        throw new NotImplementedException();
    }
}
