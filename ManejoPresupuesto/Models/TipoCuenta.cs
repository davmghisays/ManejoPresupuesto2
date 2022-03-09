using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Models
{
    public class TipoCuenta
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido.")]
        [StringLength(maximumLength:50,MinimumLength = 3, ErrorMessage ="la longitud del campo {0} debería estar entre {2} y {1}")]
        [Display(Name ="Nombre del tipo de cuenta")]        
        [Remote(action: "YaExisteNombre", controller:"TiposCuentas")]
        public string Nombre { get; set; }
        public int UsuarioId { get; set; }
        public int Orden { get; set; }


    }
}
