using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Models
{
    public class LoginViewModel
    {   /*Además de tener las mismas propiedades que el RegistroViewModel, hay una propiedad nueva
         que se llama Recuerdame, la cual es un bool que el usuario puede elegir para garantizar que
         el inicio de sesión se mantenga a pesar de que el usuario cierre el navegador.*/

        [Required(ErrorMessage = "El campo {0} es requerido.")]
        [EmailAddress(ErrorMessage = "El campo debe ser un correo electrónico válido")]
        public string Email { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool Recuerdame { get; set; }

    }
}
