using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicios
{

    public interface IRepositorioCuentas
    {
        Task Actualizar(CuentaCreacionViewModel cuenta);
        Task Borrar(int id);
        Task<IEnumerable<Cuenta>> Buscar(int usuarioId);
        Task Crear(Cuenta cuenta);
        Task<Cuenta> ObtenerPorId(int id, int usuarioId);
    }
    public class RepositorioCuentas : IRepositorioCuentas
    {

        private readonly string connectionString;
        public RepositorioCuentas(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        //METODO QUE CREA UNA CUENTA NUEVA EN LA TABLA CUENTAS
        public async Task Crear(Cuenta cuenta)
        {
            using var connection = new SqlConnection(connectionString);
            var id = await connection.QuerySingleAsync<int>("INSERT INTO Cuentas (Nombre, TipoCuentaId, Descripcion, Balance)" +
                                                       "VALUES (@Nombre, @TipoCuentaId, @Descripcion, @Balance);" +
                                                       "SELECT SCOPE_IDENTITY();", cuenta);

            cuenta.Id = id;
        }

        public async Task<IEnumerable<Cuenta>> Buscar(int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);

            /*El query que se envía va a devolver una tabla con los campos Id, Nombre, Balance y una columna adicional que se
             * va a llamar TipoCuenta y que proviene de la relación entre el campo TipoCuentaId de la tabla Cuentas con el Id
             * de la tabla TiposCuentas (el tc es una manera de apodar la tabla TiposCuentas para no escribir tanto)
             * los resultados que se van a obtener, van a estar organizados por el orden que tienen los TipoCuentas del usuario*/
            return await connection.QueryAsync<Cuenta>(@"SELECT Cuentas.Id, Cuentas.Nombre, Balance, tc.Nombre AS TipoCuenta
                                                        FROM Cuentas
                                                        INNER JOIN TiposCuentas tc
                                                        ON tc.Id = Cuentas.TipoCuentaId
                                                        WHERE tc.UsuarioId = @UsuarioId
                                                        ORDER BY tc.Orden", new { usuarioId });
        }

        public async Task<Cuenta> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<Cuenta>(@"SELECT Cuentas.Id, Cuentas.Nombre, Balance, Descripcion, TipoCuentaId
                                                                        FROM Cuentas
                                                                        INNER JOIN TiposCuentas tc
                                                                        ON tc.Id = Cuentas.TipoCuentaId
                                                                        WHERE tc.UsuarioId = @UsuarioId AND
                                                                        Cuentas.Id = @Id", new {id, usuarioId});
        }


        public async Task Actualizar (CuentaCreacionViewModel cuenta)
        {
            using var connection = new SqlConnection(connectionString);

            await connection.ExecuteAsync(@"UPDATE CUentas
                                                SET Nombre = @Nombre, Balance = @Balance, Descripcion = @Descripcion, TipoCuentaId = @TipoCuentaId
                                                WHERE Id = @Id ",cuenta);
        }

        public async Task Borrar (int id)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(@"DELETE Cuentas WHERE Id = @Id",new {id});
        }


    }
}
