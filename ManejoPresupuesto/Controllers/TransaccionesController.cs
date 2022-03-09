using AutoMapper;
using ClosedXML.Excel;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace ManejoPresupuesto.Controllers
{
    public class TransaccionesController : Controller
    {
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IRepositorioCuentas repositorioCuentas;
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IServicioReportes servicioReportes;
        private readonly IMapper mapper;

        public TransaccionesController(IServicioUsuarios servicioUsuarios, IRepositorioTransacciones repositorioTransacciones,
                                        IRepositorioCuentas repositorioCuentas, IRepositorioCategorias repositorioCategorias,IServicioReportes servicioReportes ,IMapper mapper)
        {
            this.servicioUsuarios = servicioUsuarios;
            this.repositorioTransacciones = repositorioTransacciones;
            this.repositorioCuentas = repositorioCuentas;
            this.repositorioCategorias = repositorioCategorias;
            this.servicioReportes = servicioReportes;
            this.mapper = mapper;
        }

        public async Task<IActionResult> Index(int mes, int año)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var modelo = await servicioReportes.ObtenerReporteTransaccionesDetalladas(usuarioId,mes, año, ViewBag);

            return View(modelo);
        }

        public async Task<IActionResult> Mensual(int año)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            if (año == 0)
            {
                año = DateTime.Today.Year;
            }

            var transaccionesPorMes = await repositorioTransacciones.ObtenerPorMes(usuarioId, año);

            var transaccionesAgrupadas = transaccionesPorMes.GroupBy(x => x.Mes).Select(x => new ResultadoObtenerPorMes()
            {
                Mes = x.Key,
                Ingreso = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gasto = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto).Select(x => x.Monto).FirstOrDefault()
            }).ToList();

            for (int mes = 0; mes < 12; mes++)
            {
                var transaccion = transaccionesAgrupadas.FirstOrDefault(x => x.Mes == mes);
                var fechaReferencia = new DateTime(año, mes, 1);
                if (transaccion is null)
                {
                    transaccionesAgrupadas.Add(new ResultadoObtenerPorMes()
                    {
                        Mes = mes,
                        FechaReferencia = fechaReferencia,
                    });
                }
                else
                {
                    transaccion.FechaReferencia = fechaReferencia;
                }
            }

            transaccionesAgrupadas = transaccionesAgrupadas.OrderByDescending(x => x.Mes).ToList();

            var modelo = new ReporteMensualViewModel();
            modelo.Año = año;
            modelo.TransaccionesPorMes = transaccionesAgrupadas;

            return View();
        }

        public async Task<IActionResult> Semanal(int mes, int año)
        {
            //se obtiene el id del usuario
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            //se obtienen los montos de transacciones por semana. 
            IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana = 
                await servicioReportes.ObtenerReporteSemanal(usuarioId, mes, año, ViewBag);

            //el IEnumerable se ordena. Primero se agrupa por semana, y luego se selecciona de manera que la semana
            // es la llave de cada bloque, donde Se separan ingresos, y luego gastos.

            var agrupado = transaccionesPorSemana.GroupBy(x => x.Semana).Select(x => new ResultadoObtenerPorSemana()
            {
                Semana = x.Key,
                Ingresos = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gastos = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto).Select(x => x.Monto).FirstOrDefault(),
            }).ToList();

            //Si el año o mes son nulos, entonces viene a la fecha de hoy (recordar que eso se hace por si
            // la persona vuelve a la pagina)

            if (año == 0 || mes == 0)
            {
                var Hoy = DateTime.Today;
                año = Hoy.Year;
                mes = Hoy.Month;
            }

            //

            var fechaReferencia = new DateTime(año, mes, 1);
            var diasDelMes = Enumerable.Range(1, fechaReferencia.AddMonths(1).AddDays(-1).Day);

            var diasSegmentados = diasDelMes.Chunk(7).ToList();

            for (int i = 0; i < diasSegmentados.Count(); i++)
            {
                var semana = i + 1;
                var fechaInicio = new DateTime(año, mes, diasSegmentados[i].First());
                var fechaFin = new DateTime(año, mes, diasSegmentados[i].Last());
                var grupoSemana = agrupado.FirstOrDefault(x => x.Semana == semana);

                if (grupoSemana is null)
                {
                    agrupado.Add(new ResultadoObtenerPorSemana()
                    {
                        Semana = semana,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    });
                }
                else
                {
                    grupoSemana.FechaInicio = fechaInicio;
                    grupoSemana.FechaFin = fechaFin;
                }
            }

            agrupado = agrupado.OrderByDescending(x => x.Semana).ToList();

            var modelo = new ReporteSemanalViewModel();
            modelo.TransaccionesPorSemana = agrupado;
            modelo.FechaReferencia = fechaReferencia;

            return View(modelo);
        }

        public IActionResult Calendario()
        {
            return View();
        }


        /*Esta ACCION GET se encarga de devolver un JSON con todas las transacciones entre una fecha start
         y una fecha end que solicita el script de Fullcalendar, de acuerdo al mes que se muestre.*/
        public async Task<JsonResult> ObtenerTransaccionesCalendario(DateTime start, DateTime end)
        {
            /*Aqui se obtiene el usuario activo y se establece el parámetro que recibe el método que obtieen
             transacciones en un rango de fechas. */
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = start,
                FechaFin = end
            };

            /*Aqui se buscan las transacciones con el parámeto creado*/
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);

            /*Aqui se procede a mapear las transacciones de acuerdo con la estructura que recibe FullCalendar,
             teniendo en cuenta que cada elemento transaccion se coloca bajo las propiedades como Title, Start, ENd
             y Color, que además se definieron dentro de un MODELO EventoCalendario. Todos estos son strings, y 
             FullCalendar los recibe tal cual como se están formateando en los método ToString para cada uno.*/

            var eventosCalendario = transacciones.Select(transaccion => new EventoCalendario()
            {
                Title = transaccion.Monto.ToString("N"),
                Start = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                End = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                Color = (transaccion.TipoOperacionId == TipoOperacion.Gasto) ? "Red" : null
            });

            /*Finalmente, con el Enumerable de EventoCalendario que se creó, se convierte en un string de JSON que va
             a leer el script de JS*/
            return Json(eventosCalendario);

        }

        public async Task<JsonResult> ObtenerTransaccionesPorFecha (DateTime fecha)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fecha,
                FechaFin = fecha
            };

            /*Aqui se buscan las transacciones con el parámeto creado*/
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);

            return Json(transacciones);

        }


        public IActionResult ExcelReporte()
        {
            return View();
        }

        /*Esta ACCION devuelve un archivo de Excel para el usuario con el resumen de transacciones
          por mes usando como parámetro el mes y el año que se requiere. */
        public async Task<FileResult> ExportarExcelPorMes(int mes, int año)
        {
            /*con la fecha de inicio fechainicio establecida, se calcula la fecha fin para que sea
             todo el mes. Además, se obtiene el usuario activo del servicio de usuarios*/
            var fechainicio = new DateTime(año, mes, 1);
            var fechaFin = fechainicio.AddMonths(1).AddDays(-1);
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            /*Aqui se plantea el parámetro que recibe el servicio de transacciones para recuperar las transacciones
             por mes.*/

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechainicio,
                FechaFin = fechaFin
            };

            /*Aqui se buscan las transacciones con el parámeto creado*/
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);

            /*Aquí se crea el nombre de archivo que va a ser el que va a obtener el usuario finalmente*/
            var nombreArchivo = $"Manejo Presupuesto - {fechainicio.ToString("MMM yyyy")}.xlsx";

            /*Aqui se llama a la función GenerarExcel, que es la que se va a encargar de crear el archivo como tal.*/
            return GenerarExcel(nombreArchivo, transacciones);
        }

        private async Task<FileResult> ExportarExcelPorAño(int año)
        {
            var fechaInicio = new DateTime(año, 1, 1);
            var fechaFin = fechaInicio.AddYears(1).AddDays(-1);
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("yyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);

        }

        public async Task<FileResult> ExportarExcelTodo()
        {
            var fechaInicio = DateTime.Today.AddYears(-100);
            var fechaFin = DateTime.Today.AddYears(1000);
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);
            var nombreArchivo = $"Manejo Presupuesto - {DateTime.Today.ToString("dd-MM-yyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);

        }



        /*La función GenerarExcel devuelve un tipo FileResult, que devuelve un archivo finalmente al usuario.
         Este tipo de retorno es exclusivo de MVC. En rpincipio recibe el nombre de archivo que se va a generar,
        y tambien un enumerable lleno de transacciones. */
        private FileResult GenerarExcel(string nombreArchivo, IEnumerable<Transaccion> transacciones)
        {

            /* aqui se construye un elemento de tipo tabla de datos DataTable. Recibe en su constructor el nombre
             de la tabla*/
            DataTable dataTable = new DataTable("Transacciones");
            /*Para agregar los titulos de lo que se va a presentar, entonces se utiliza el método AddRange para la
             propiedad Columns de la tabla que se creó. Se le puede enviar un arreglo DataColumn[] hecho de DataColumns
             con los títulos de cada una de las columnas que va a presentar.*/
            dataTable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Fecha"),
                new DataColumn("Cuenta"),
                new DataColumn("Categoria"),
                new DataColumn("Nota"),
                new DataColumn("Monto"),
                new DataColumn("Ingreso/Egreso"),
            });

            /*Para llenar los registros que se construyeron, entonces se recorre el Enumerable que se le pasó a
             la función en cuestión y con el método Add de la propiedad Rows, se agrega cada valor. Estos DEBEN
             ir en el orden exacto tal como se le pasaron las columnas, de manera que el dato se introduzca bajo
             la columna correcta.*/

            foreach (var transaccion in transacciones)
            {
                dataTable.Rows.Add(
                    transaccion.FechaTransaccion,
                    transaccion.Cuenta,
                    transaccion.Categoria,
                    transaccion.Nota,
                    transaccion.Monto,
                    transaccion.TipoOperacionId);
            }

            /*Aquí se utilizan las clases y métodos del paquete NuGet ClosedXML, de modo que se pueda generar efectivamente
             el archivo de excel que se pretende. Utilizando entonces una instancia (wb) de la clase XLWorkbook, entonces se
             procede a poblar con los datos del DataTable que se acaba de generar.*/

            using (XLWorkbook wb = new XLWorkbook())
            {
                /*A la propiedad Worksheets del nuevo Workbook se le agrega una hoja, y además se le agrega el dataTable que se
                 * pobló con los datos del Enumerable.*/
                wb.Worksheets.Add(dataTable);

                /*Ahora se crea un flujo de datos (stream) que usa la memoria RAM como apoyo en lugar del disco duro. En ese momento, 
                 el libro de excel wb se guarda como un stream en la memoria RAM.
                
                 Luego este stream se convierte en un arreglo de datos, y tambien se pasa el tipo de dato que va a ser el que se
                 va a enviar. Este stream, junto con el tipo de dato y el nombre del archivo es lo que requiere el método File, que
                 convierte el stream en un archivo descargable por el usuario. Ese va a ser el FileResult devuelto.*/

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo);
                }
            }
        }


        public async Task<IActionResult> Crear()
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var modelo = new TransaccionCreacionViewModel();
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            /* la creación del modelo que le voy a pasar requiere de la definición de las categorías,
                Pero como no la tengo inicialmente, entonces los creo con ObtenerCategorías y le paso
                el modelo.TipoOperacionId que se configuró en la clase TransaccionCreacionViewModel para
                que por defecto pase Ingreso (TipoOperacion.Ingreso)*/
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
            return View(modelo);

        }

        /*El método ObtenerCuentas pasa a un objeto IEnumerable lleno de SelectListItem todas las
            cuentas opr nombre y por Id.*/

        private async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
        {
            var cuentas = await repositorioCuentas.Buscar(usuarioId);
            return cuentas.Select(x => new SelectListItem ( x.Nombre, x.Id.ToString()));

        }

        /*Método privado que se comunica con el repositorio de categorías y las trae filtradas por tipo de Operación.
          Luego procede a convertir cada una en un SelectListItem de Nombres e ids. (nombre para que el usuario vea,
          ID para que el programa sepa qué valor único seleccionar.*/
        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias (int usuarioId, TipoOperacion tipoOperacion)
        {
            var categorias = await repositorioCategorias.Obtener(usuarioId, tipoOperacion);
            return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }


        /*POST que recibe el cambio de TipoOperacion desde un JSON desde el script de javascript en la VISTA CREAR,
         cuando la VISTA necesita poblar el campo Categorías de acuerdo con el tipo de operación. Este POST envía entonces
         un IEnumerable de SelectListItem de las opciones de categoria que se pueden elegir de acuerdo al tipo de operacion.*/
        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            // Aquí se llama al procedimiento ObtenerCategorías privado de arriba que las filtra por usuario, y tipo de operación.
            var categorias = await ObtenerCategorias(usuarioId, tipoOperacion);
            return Ok(categorias);
        }

        [HttpPost]

        public async Task<IActionResult> Crear (TransaccionCreacionViewModel modelo)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            /*VALIDACIONES:
             
             * Validacion del Modelo: En primer lugar se da la validación de Modelo. Si no es válido, devuelve la vista de
                creación con los datos que venían introducidos
            
             * Validación de la cuenta: Con los datos enviados en el POST, se revisa si la cuenta existe, y si esta cuenta
                le pertenece a este usuario. Si no, entonces envía a la vista No Encontrado.
            
             * Validación de la categorias. Con los datos enviados en el POST, se revisa si la categoria existe, y si esta
                categoria le pertenece al usuario. Si no, entonces envia a la vista No Encontrado.
            
             * Recordar que todas estas validaciones se realizan porque no se puede confiar en el usuario. Entonces este
                puede enviar datos vía mirar el código en inspeccion, o editando algo que no está autorizado.*/

            //VAL Modelo           
            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            //VAL cuenta
            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("No Encontrado", "Home");
            }

            //VAL categoria
            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);

            if (categoria is null)
            {
                return RedirectToAction("No Encontrado", "Home");
            }

            //El usuario no viene en el modelo, asi que se asigna con el que viene del servicio de Usuarios
            modelo.UsuarioId = usuarioId;

            // Si el tipo de operación se clasifica como un GASTO, entonces el monto que el usuario escribe
            //  se va en negativo. (O sea, se multiplica por -1)
            if (modelo.TipoOperacionId == TipoOperacion.Gasto){
                modelo.Monto *= -1;
            }

            //finalmente, se envía el modelo entero hacia el método Crear del Repositorio de Transacciones
            await repositorioTransacciones.Crear(modelo);

            return RedirectToAction("Index");
        }


        /*Esta es la ACCION para poblar la vista EDITAR para editar la transacción.*/
        [HttpGet]
        public async Task<IActionResult> Editar(int id, string urlRetorno = null)
        {

            // se consigue el usuario que está activo
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            /* se obtiene la transacción en específico con el id que le ingresa al usar el botón
                de editar sobre el listado de transacciones. */ 
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            // si la transaccion no existe para el usuario y el id, entonces da error.
            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado","Home");
            }


            /*se procede a mapear los datos de la transaccion en el ViewModel que se va a utilizar
                en la VISTA editar */
            var modelo = mapper.Map<TransaccionActualizacionViewModel>(transaccion);

            modelo.MontoAnterior = modelo.Monto;
            
            /* En caso de que la operación sea de tipo gasto, entonces el monto anterior va
                negativo, para que el monto anterior reste.*/

            if (modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.MontoAnterior = modelo.Monto * -1;
            }

            // se pasa la cuenta que tenía al modelo
            modelo.CuentaAnteriorId = transaccion.CuentaId;
            // se obtienen las categorías para el usuario y el tipo de operacion que llevaba
            modelo.Categorias = await ObtenerCategorias(usuarioId, transaccion.TipoOperacionId);
            // se obtienen las cuentas que el usuario ha creado.
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.UrlRetorno = urlRetorno;

            return View(modelo);

        }

        [HttpPost]

        public async Task<IActionResult> Editar (TransaccionActualizacionViewModel modelo)
        {
            // se consigue el usuario que está activo
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            
            /* en caso de que algo en el modelo sea inválido, entonces se retorna la vista
                poblada otra vez*/
            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);


            if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var transaccion = mapper.Map<Transaccion>(modelo);

            if (modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                transaccion.Monto *= -1;
            }

            await repositorioTransacciones.Actualizar(transaccion, modelo.MontoAnterior, modelo.CuentaAnteriorId);

            if (string.IsNullOrEmpty(modelo.UrlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(modelo.UrlRetorno);
            }


            //return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Borrar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTransacciones.Borrar(id);

            if (string.IsNullOrEmpty(urlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(urlRetorno);
            }

        }

    }
}
