using ManejoPresupuesto.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManejoPresupuesto.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UserManager<Usuario> userManager;
        private readonly SignInManager<Usuario> signInManager;

        public UsuariosController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [AllowAnonymous]
        public IActionResult Registro()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Registro(RegistroViewModel modelo)
        {
            // verificacion de que el modelo esté bien, de otro modo devuelve ala vista poblada
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // aqui se crea un objeto de tipo usuario y se puebla el email de una con lo que se
            //se recibió del modelo
            var usuario = new Usuario() { Email = modelo.Email };

            // se crea una variable resultado, que es la que va a recibir el estado del proceso 
            // del CreateAsync. A este se le entrega el usuario, y la clave que el usuario pasó
            // en el modelo.
            var resultado = await userManager.CreateAsync(usuario, password: modelo.Password);

            //Si resulta bien, nos lleva el indice, si no, muestra todos los erreos y devuelve
            // al formulario de creación
            if (resultado.Succeeded)
            {
                /*Al incluir a la clase SignInManager, habilito el servicio de ASP.NET para que
                 haga el inicio de sesión de un usuario y mantenga esa sesión iniciada. En este caso
                 despues de crear el usuario. La opción isPersistent pregunta si, despues de que el 
                 usuario haya cerrado el navegador, va a seguir con la sesión iniciada.*/
                await signInManager.SignInAsync(usuario, isPersistent: true);
                return RedirectToAction("Index", "Transacciones");
            }
            else
            {
                foreach(var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description); 
                }

                return View(modelo);
            }

        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /*ACCION que recibe el inicio de sesión.*/
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            /*Verifica si hay errores a nivel de modelo*/
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }
            /*Se utiliza el método PasswordSignInAsync para iniciar sesión. Este método
             recibe el nombre de usuario, la clave y un bool que le afirme is isPersistent o no.
             además recibe un parámetro para el bloqueo de cuenta en caso de escribir mal la clave
             despues de cierta cantidad de intentos lockoutOnFailure*/
            var resultado = await signInManager.PasswordSignInAsync(modelo.Email,
                modelo.Password, modelo.Recuerdame, lockoutOnFailure: false);

            /*El resultado arroja si se logró o no, en cuyo caso, redirige a la página de transacciones
              o en general a la que se quiera. Si no, entonces devuelve a la vista con los datos ingresados
              y bota un error de modelo (el que aparece en el asp-validation-summary de la vista Login).*/
            if (resultado.Succeeded)
            {
                return RedirectToAction("Index", "Transacciones");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o Password Incorrecto.");
                return View(modelo);
            }

        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Transacciones");
        }

    }
}
