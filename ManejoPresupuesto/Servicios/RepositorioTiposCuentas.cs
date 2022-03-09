using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;
using System.Collections;

namespace ManejoPresupuesto.Servicios
{

    public interface IRepositorioTiposCuentas
    {
        Task Actualizar(TipoCuenta tipoCuenta);
        Task Borrar(int id);
        Task Crear(TipoCuenta tipoCuenta);
        Task<bool> Existe(string nombre, int usuarioId);
        Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId);
        Task<TipoCuenta> ObtenerPorId(int id, int usuarioId);
        Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasOrdenados);
    }

    public class RepositorioTiposCuentas : IRepositorioTiposCuentas
    {
        
        private readonly string connectionString;

        public RepositorioTiposCuentas(IConfiguration configuration)
        {
            
            connectionString = configuration.GetConnectionString("DefaultConnection");

        }

        public async Task Crear(TipoCuenta tipoCuenta)
        {
            using var connection = new SqlConnection(connectionString);
            

            var id = await connection.QuerySingleAsync<int>("TiposCuentas_Insertar", 
                                                            new { UsuarioId = tipoCuenta.UsuarioId, Nombre = tipoCuenta.Nombre },
                                                            commandType: System.Data.CommandType.StoredProcedure);
            tipoCuenta.Id = id;
        }
        
        public async Task<bool> Existe(string nombre, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);

            var existe = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM TiposCuentas WHERE Nombre = @nombre AND UsuarioId = @usuarioId", new { nombre, usuarioId });

            return existe == 1;

        }

        public async Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);

            return await connection.QueryAsync<TipoCuenta>(@"SELECT Id, Nombre, Orden
                                                                    FROM TiposCuentas
                                                                    WHERE UsuarioId = @usuarioId
                                                                    ORDER BY Orden", new {usuarioId});

        }

        public async Task Actualizar(TipoCuenta tipoCuenta)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(@"UPDATE TiposCuentas
                                            SET Nombre = @Nombre
                                            WHERE Id = @Id", tipoCuenta);

        }

        public async Task<TipoCuenta> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);

            TipoCuenta cuenta = await connection.QueryFirstOrDefaultAsync<TipoCuenta>(@"SELECT Id,Nombre,Orden
                                                                                FROM TiposCuentas
                                                                                WHERE Id = @Id AND UsuarioId = @usuarioId", new { id, usuarioId });

            return cuenta;

        }

        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(connectionString);

            await connection.ExecuteAsync(@"DELETE TiposCuentas WHERE Id = @id", new { id });

        }

        public async Task Ordenar (IEnumerable<TipoCuenta> tipoCuentasOrdenados)
        {
            var query = "UPDATE TiposCuentas SET Orden = @Orden Where Id = @Id;";

            using var connection = new SqlConnection(connectionString);

            /*Dapper tiene la ventaja de que va a ejecutar queries con ExecuteAsync con
             cuantos tipoCuentasOrdenados haya en el IEnumerable. No hay que hacer un foreach*/
            await connection.ExecuteAsync(query, tipoCuentasOrdenados);

        }


    }
}
