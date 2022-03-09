using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

/*Se agrega una pol�tica de autorizaci�n para usuarios autenticados*/
var politicaUsuariosAutenticados = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().Build();

/*Se agrega al servicio de agregar Controladores con Vistas, la opci�n de un filtro
 de autorizaci�n tal como se construy� arriba.*/
builder.Services.AddControllersWithViews(opciones =>
{
    opciones.Filters.Add(new AuthorizeFilter(politicaUsuariosAutenticados));
});
builder.Services.AddTransient<IRepositorioTiposCuentas, RepositorioTiposCuentas>();
builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();
builder.Services.AddTransient<IRepositorioCuentas, RepositorioCuentas>();
builder.Services.AddTransient<IRepositorioCategorias, RepositorioCategorias>();
builder.Services.AddTransient<IRepositorioTransacciones, RepositorioTransacciones>();
builder.Services.AddTransient<IServicioReportes, ServicioReportes>();
builder.Services.AddTransient<IRepositorioUsuarios, RepositorioUsuarios>();
/*Se inyecta tambien la clase de SignInManager con la clase que maneja el Usuario,
 para poderla utilizar por ejemplo en la VISTA de Logins.*/
builder.Services.AddTransient<SignInManager<Usuario>>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddTransient<IUserStore<Usuario>, UsuarioStore>();
builder.Services.AddIdentityCore<Usuario>(opciones =>
{
    //opciones.Password.RequireDigit = false;
    //opciones.Password.RequireLowercase = false;
    //opciones.Password.RequireUppercase = false;
    //opciones.Password.RequireNonAlphanumeric = false;
});

/*Para activar el SignIn Manager, se utilizan estas lineas. Aqu� se le est� diciendo al 
 backend que use cookies para autenticaci�n.
    
 Adem�s se estableci� la ruta predeterminada cuando no hay autorizaci�n y el usuario no est�
 loggeado.
 */
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;

}).AddCookie(IdentityConstants.ApplicationScheme, opciones =>
{
    opciones.LoginPath = "/usuarios/login";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Esta linea para que el programa use autenticaci�n
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Transacciones}/{action=Index}/{id?}");

app.Run();
