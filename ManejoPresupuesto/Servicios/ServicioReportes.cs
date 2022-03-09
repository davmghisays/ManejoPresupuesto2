using ManejoPresupuesto.Models;

namespace ManejoPresupuesto.Servicios
{

    public interface IServicioReportes
    {
        Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerReporteSemanal(int usuario, int mes, int año, dynamic ViewBag);
        Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladas(int usuarioId, int mes, int año, dynamic ViewBag);
        Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladasPorCuenta(int usuarioId, int cuentaId, int mes, int año, dynamic ViewBag);
    }
    public class ServicioReportes : IServicioReportes
    {
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly HttpContext httpContext;

        public ServicioReportes(IRepositorioTransacciones repositorioTransacciones, IHttpContextAccessor httpContextAccessor)
        {
            this.repositorioTransacciones = repositorioTransacciones;
            this.httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerReporteSemanal(int usuarioId, int mes, int año, dynamic ViewBag)
        {
            (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, año);

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            AsignarValoresAlViewBag(ViewBag, fechaInicio);
            var modelo = await repositorioTransacciones.ObtenerPorSemana(parametro);
            return modelo;

        }


        public async Task<ReporteTransaccionesDetalladas> 
            ObtenerReporteTransaccionesDetalladasPorCuenta
            (int usuarioId, int cuentaId, int mes, int año, dynamic ViewBag)
        {

            (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, año);

            /*Se crea un objeto de la clase ObtenerTransaccionesPorCuenta, que en medio de 
              todo es una clase que se desarrolla para pasar los 4 parámetros en un solo 
              objeto. */

            var obtenerTransaccionesPorCuenta = new ObtenerTransaccionesPorCuenta()
            {
                CuentaId = cuentaId,
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            /*la variable transacciones se puebla con un IEnumerable llena de transacciones filtradas para una cuenta
             * con el id CuentaId, para el usuario usuarioId, y en el rango de una fecha de inicio y una fecha de fin.*/

            var transacciones = await repositorioTransacciones.ObtenerPorCuentaId(obtenerTransaccionesPorCuenta);

            /*Aqui se crea un un objeto ReporteTransaccionesDetalladas modelo que va a tener las fechas, las transacciones,
              los balances de las transacciones por gasto y por ingreso y el total.*/

            var modelo = GenerarReporteTransaccionesDetalladas(fechaInicio, fechaFin, transacciones);
            AsignarValoresAlViewBag(ViewBag, fechaInicio);

            return modelo;

        }

        private void AsignarValoresAlViewBag(dynamic ViewBag, DateTime fechaInicio)
        {
            ViewBag.mesAnterior = fechaInicio.AddMonths(-1).Month;
            ViewBag.añoAnterior = fechaInicio.AddMonths(-1).Year;

            ViewBag.mesPosterior = fechaInicio.AddMonths(1).Month;
            ViewBag.añoPosterior = fechaInicio.AddMonths(1).Year;

            ViewBag.urlRetorno = httpContext.Request.Path + httpContext.Request.QueryString;
        }

        private static ReporteTransaccionesDetalladas GenerarReporteTransaccionesDetalladas(DateTime fechaInicio, DateTime fechaFin, IEnumerable<Transaccion> transacciones)
        {
            var modelo = new ReporteTransaccionesDetalladas();

            /*aqui se le da una propiedad al ViewBag con el nombre de la cuenta a la que pertenecen las transacciones,
              de modo que se pase sin tener que incluirla en el modelo que se pasa.*/


            /*Y aqui se crea un objeto IEnumerable lleno de transacciones. Estas transacciones de las que se compone van a
              quedar ordenadas descendentemente por fecha de transacción, luego se crean grupos por fecha de transacción
              y finalmente, a cada grupo se le establece una llave que es la Fecha de Transaccion y el contenido de cada
              llave es el grupo de transacciones.*/

            var transaccionesPorFecha = transacciones.OrderByDescending(x => x.FechaTransaccion)
                        .GroupBy(x => x.FechaTransaccion)
                        .Select(grupo => new ReporteTransaccionesDetalladas.TransaccionesPorFecha()
                        {
                            FechaTransaccion = grupo.Key,
                            Transacciones = grupo.AsEnumerable()
                        });

            /*Aqui se pueblan los datos del objeto ReporteTransaccionesDetalladas con las transacciones agrupadas por fecha
              que se acaban de crear, la fecha de inicio y la fecha de fin.*/

            modelo.TransaccionesAgrupadas = transaccionesPorFecha;
            modelo.FechaInicio = fechaInicio;
            modelo.FechaFin = fechaFin;
            return modelo;
        }

        public async Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetalladas (int usuarioId, int mes, int año, dynamic ViewBag)
        {
            (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, año);

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);
            var modelo = GenerarReporteTransaccionesDetalladas(fechaInicio, fechaFin, transacciones);
            AsignarValoresAlViewBag(ViewBag, fechaInicio);

            return modelo;  
        }

        private (DateTime fechaInicio, DateTime fechaFin) GenerarFechaInicioYFin(int mes, int año)
        {
            // se declaran las variables de fecha inicio y fecha de fin

            DateTime fechaInicio;
            DateTime fechaFin;

            /* se comprueba que los valores de la búsqueda de fecha son válidos.
               si el mes es menor que 0, mayor que 12, o el año es menor a 1900
               entonces se establece la fecha de hoy y se establece además que la
               fecha de inicio es el 1 del mes en curso.
            
               Si mes y año y están bien, entonces se establece que la fecha de
               inicio es el primero de ese mes y de ese año.*/

            if (mes <= 0 || mes > 12 || año <= 1900)
            {
                var hoy = DateTime.Today;
                fechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            }
            else
            {
                fechaInicio = new DateTime(año, mes, 1);
            }

            /*se establece como fecha de fin, un mes adelante de la fecha de inicio,
            pero con un día menos, es decir, el último día del mismo mes de inicio.*/

            fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

            return (fechaInicio, fechaFin);
        }

    }
}
