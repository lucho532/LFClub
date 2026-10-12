using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Crea hermanos directamente en la base de datos: otro jugador de la misma cuenta y el mismo club
/// que uno que ya existe, para que las pruebas partan de una cuenta con varios jugadores sin
/// depender del endpoint que los agrega.
/// </summary>
public class SembradorHermanos
{
    private readonly FabricaApi _fabrica;

    public SembradorHermanos(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Un hermano de ese jugador: misma cuenta y mismo club, rol JUGADOR, documento único, agregado
    /// desde él y creado después que él. Por defecto queda en espera, como al agregarlo desde la
    /// ficha; con <paramref name="activo"/> en falso queda retirado. Puede nacer ya en una categoría.
    /// Con <paramref name="numeroDocumento"/> lleva ese número en lugar de uno único: el de un
    /// jugador de otra cuenta, como lo deja agregar un hermano desde la 006 (RF-034).
    /// </summary>
    public Task<UsuarioRol> CrearHermanoAsync(
        UsuarioRol origen,
        EstadoIngreso estado = EstadoIngreso.EN_ESPERA,
        DateOnly? fechaNacimiento = null,
        bool activo = true,
        string nombres = "Luis",
        Categoria? categoria = null,
        string? numeroDocumento = null) => _fabrica.ConContextoAsync(async contexto =>
        {
            var ahora = DateTime.UtcNow;
            var hermano = new UsuarioRol
            {
                ClubId = origen.ClubId,
                UsuarioId = origen.UsuarioId,
                Rol = Rol.JUGADOR,
                EstadoIngreso = estado,
                Nombres = nombres,
                Apellidos = origen.Apellidos,
                TipoDocumento = TipoDocumento.TARJETA_IDENTIDAD,
                NumeroDocumento = NormalizadorTexto.Documento(numeroDocumento ?? Sembrador.Unico("doc")),
                FechaNacimiento = fechaNacimiento ?? origen.FechaNacimiento,
                AgregadoDesdeUsuarioRolId = origen.Id,
                CategoriaId = categoria?.Id,
                Activo = activo,
                RetiradoEn = activo ? null : ahora,
                CreadoEn = ahora > origen.CreadoEn ? ahora : origen.CreadoEn.AddMilliseconds(1),
            };

            contexto.UsuariosRol.Add(hermano);
            await contexto.SaveChangesAsync();
            return hermano;
        });
}
