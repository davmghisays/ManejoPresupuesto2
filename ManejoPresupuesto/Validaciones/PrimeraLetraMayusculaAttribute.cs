using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Validaciones
{
    /*CLASE QUE SE CONVIERTE EN ATRIBUTO DE VALIDACION PARA PROPIEDADES DE UN MODELO.
     * EN ESTE CASO, ESTE ATRIBUTO REVISA SI EL DATO RECIBIDO POR LA PROPIEDAD INICIA CON
     * MAYUSCULA. SI NO INICIA CON MAÝUSCULA, DEVUELVE EL ERROR "La primera letra debe ser mayúscula"
     
     
     * TAMBIEN REVISA SI EL VALOR ES NULO O VACIO. EN ESE CASO PASA DERECHO LA VALIDACION*/

    /*El atributo hereda de ValidationAttribute*/
    public class PrimeraLetraMayusculaAttribute : ValidationAttribute
    {

        /*Este es el método heredado de ValidationAttribute de nombre IsValid*/
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                /*Si todo sale bien, la función debe devolver ValidationResult.Success*/
                return ValidationResult.Success;
            }

            var primeraLetra = value.ToString()[0].ToString();

            if (primeraLetra != primeraLetra.ToUpper())
            {
                /*Si las cosas no salen bien, debe devolver un nuevo objeto ValidationResult con el
                 * mensaje que convenga*/
                return new ValidationResult("La primera letra debe ser mayúscula");
            }

            /*En este caso, como logró pasar los dos return anteriores, llega aquí devolviendo exito
             * en la validación.*/
            return ValidationResult.Success;
        }

    }
}
