using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using LFClub.Infraestructura.Seguridad;
using Microsoft.EntityFrameworkCore;

const string Pass = "Contrasena-de-prueba-1";
var url = new Uri(Environment.GetEnvironmentVariable("PG_URL")!);
var credenciales = url.UserInfo.Split(':', 2).Select(Uri.UnescapeDataString).ToArray();
var conexion = $"Host={url.Host};Port={url.Port};Database={url.AbsolutePath.TrimStart('/')};Username={credenciales[0]};Password={credenciales[1]};SSL Mode=Require;Trust Server Certificate=true";
var opciones = new DbContextOptionsBuilder<ContextoLFClub>().UseNpgsql(conexion).Options;
await using var bd = new ContextoLFClub(opciones, new SinClub());
var hash = new HashContrasena().Calcular(Pass);

var correos = new[] { "presidente", "directivo", "entrenador", "jugador" }.Select(x => $"{x}@prueba.test").ToArray();
var yaHay = await bd.Usuarios.CountAsync(u => correos.Contains(u.Correo));
var clubes = await bd.Clubes.CountAsync();
var cuentas = await bd.Usuarios.CountAsync();
Console.WriteLine($"En la base: {clubes} clubes, {cuentas} cuentas; cuentas de prueba ya existentes: {yaHay}.");
if (args.Contains("ver")) return;
if (yaHay > 0 || await bd.Clubes.AnyAsync(c => c.Nombre == "Club de Prueba")) { Console.WriteLine("Ya existe: no se hace nada."); return; }


var club = new Club { Nombre = "Club de Prueba", NombreNormalizado = NormalizadorTexto.NombreClub("Club de Prueba"), Estado = EstadoClub.ACTIVO, CreadoEn = DateTime.UtcNow };
bd.Clubes.Add(club);
var categoria = new Categoria { ClubId = club.Id, Anio = 2014, Activa = true, Usada = true, CreadaEn = DateTime.UtcNow };
bd.Categorias.Add(categoria);

int n = 0;
UsuarioRol Persona(string correo, Rol rol, string nombres, string apellidos, Guid? cat = null)
{
    n++;
    var u = new Usuario { Correo = correo, CorreoNormalizado = NormalizadorTexto.Correo(correo), ContrasenaHash = hash, Celular = "3001234567", CreadoEn = DateTime.UtcNow };
    var r = new UsuarioRol
    {
        ClubId = club.Id, UsuarioId = u.Id, Rol = rol, EstadoIngreso = EstadoIngreso.APROBADO, Nombres = nombres, Apellidos = apellidos,
        TipoDocumento = TipoDocumento.CEDULA_CIUDADANIA, NumeroDocumento = $"90000000{n}",
        FechaNacimiento = rol == Rol.JUGADOR ? new DateOnly(2014, 5, 20) : new DateOnly(1985, 3, 14),
        CategoriaId = cat, CreadoEn = DateTime.UtcNow,
    };
    bd.Usuarios.Add(u);
    bd.UsuariosRol.Add(r);
    return r;
}

Persona("presidente@prueba.test", Rol.PRESIDENTE, "Pedro", "Presidente");
Persona("directivo@prueba.test", Rol.DIRECTIVO, "Diana", "Directiva");
var entrenador = Persona("entrenador@prueba.test", Rol.ENTRENADOR, "Elena", "Entrenadora");
Persona("jugador@prueba.test", Rol.JUGADOR, "Juan", "Jugador", categoria.Id);
bd.AsignacionesEntrenadorCategoria.Add(new AsignacionEntrenadorCategoria { ClubId = club.Id, CategoriaId = categoria.Id, UsuarioRolId = entrenador.Id, Activa = true, CreadaEn = DateTime.UtcNow });
await bd.SaveChangesAsync();
Console.WriteLine("Sembrado.");

sealed class SinClub : IContextoClub
{
    public Guid? ClubId => null;
    public void Fijar(Guid clubId) { }
}
