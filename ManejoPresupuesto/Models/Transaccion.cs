using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Models
{
    public class Transaccion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        [Display(Name = "Fecha de transacción")]
        [DataType(DataType.DateTime)]
        public DateTime FechaTransaccion { get; set; } = DateTime.Parse(DateTime.Now.ToString("g"));
        //Aqui se preinicializa la propiedad de la clase, haciendo que por defecto tenga la fecha de hoy.
        public decimal Monto { get; set; }
        [Range(1, maximum: int.MaxValue, ErrorMessage="Debe seleccionar una categoría")]
        /*Como CategoriaId es un entero con un código que se relaciona con la tabla de categorías
            Si no existe un valor escogido, cuyo rango inicia en 1 (el primer posible Id de una categoría)
            y un valor absurdamente alto (int.MaxValue) se debe botar error y no permitir pasar el formulario
            eso hace el atributo [Range]*/
        [Display(Name = "Categoría de Transacción")]
        public int CategoriaId { get; set; }
        [StringLength(maximumLength:1000, ErrorMessage ="La nota no puede pasar de {1} caracteres")]
        public string Nota { get; set; }
        [Range(1, maximum: int.MaxValue, ErrorMessage = "Debe seleccionar una cuenta")]

        [Display(Name = "Cuenta a Asignar")]
        public int CuentaId { get; set; }

        [Display(Name = "Tipo de Operación")]
        public TipoOperacion TipoOperacionId { get; set; } = TipoOperacion.Ingreso;
        public string Cuenta { get; set; }
        public string Categoria { get; set; }

    }
}
