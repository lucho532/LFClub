using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Hermanos;

/// <summary>
/// El escenario de <see cref="EscenarioHermanos"/> más, en el otro club, la categoría 2013 con un
/// equipo y dos jugadores de otras cuentas: Caro, en esa categoría y en el equipo, con su ficha y
/// un documento entregado, y Dani, en la categoría. Hay además un tercer club con su presidente.
/// Sirve para sembrar hermanos de Ana con el número de Caro o de Dani y comprobar qué cambia en el
/// otro club (quickstart de la 006, bloque 5).
/// </summary>
internal sealed class EscenarioDocumentoCompartido
{
    public const int AnioDelOtroClub = 2013;

    private EscenarioDocumentoCompartido()
    {
    }

    public required EscenarioHermanos Hermanos { get; init; }

    public required Categoria CategoriaDelOtroClub { get; init; }

    public required Equipo EquipoDelOtroClub { get; init; }

    public required Usuario CuentaDeCaro { get; init; }

    public required UsuarioRol Caro { get; init; }

    public required Usuario CuentaDeDani { get; init; }

    public required UsuarioRol Dani { get; init; }

    public required Club TercerClub { get; init; }

    public required ClienteDePrueba PresidenteDelTercerClub { get; init; }

    public FabricaApi Fabrica => Hermanos.Fabrica;

    public Club Club => Hermanos.Club;

    public Club OtroClub => Hermanos.OtroClub;

    public UsuarioRol Ana => Hermanos.Ana;

    public ClienteDePrueba Presidente => Hermanos.Presidente;

    public ClienteDePrueba PresidenteDelOtroClub => Hermanos.Base.PresidenteDeOtroClub;

    public static async Task<EscenarioDocumentoCompartido> CrearAsync(FabricaApi fabrica)
    {
        var hermanos = await EscenarioHermanos.CrearAsync(fabrica);
        var otroClub = hermanos.OtroClub;
        var categoria = await fabrica.Categorias.CrearCategoriaAsync(otroClub, AnioDelOtroClub);
        var equipo = await fabrica.Categorias.CrearEquipoAsync(categoria, "A");

        var (cuentaDeCaro, caro) = await fabrica.Categorias.CrearJugadorAsync(
            otroClub, AnioDelOtroClub, categoria, "Caro", "Ruiz");
        await fabrica.Categorias.PonerEnEquipoAsync(caro, equipo);
        await fabrica.Fichas.CrearFichaAsync(caro);
        await fabrica.Fichas.CrearDocumentoAsync(
            caro, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");

        var (cuentaDeDani, dani) = await fabrica.Categorias.CrearJugadorAsync(
            otroClub, AnioDelOtroClub, categoria, "Dani", "Mora");

        var tercerClub = await fabrica.Sembrador.CrearClubAsync();
        var (presidenteDelTercero, _) = await fabrica.Sembrador.CrearIntegranteAsync(tercerClub, Rol.PRESIDENTE);

        return new EscenarioDocumentoCompartido
        {
            Hermanos = hermanos,
            CategoriaDelOtroClub = categoria,
            EquipoDelOtroClub = equipo,
            CuentaDeCaro = cuentaDeCaro,
            Caro = caro,
            CuentaDeDani = cuentaDeDani,
            Dani = dani,
            TercerClub = tercerClub,
            PresidenteDelTercerClub = await fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidenteDelTercero),
        };
    }

    /// <summary>Un hermano de Ana, en el Club A, con el número de documento de <paramref name="otro"/>.</summary>
    public Task<UsuarioRol> HermanoConDocumentoDeAsync(UsuarioRol otro, EstadoIngreso estado = EstadoIngreso.EN_ESPERA) =>
        Fabrica.Hermanos.CrearHermanoAsync(Ana, estado, numeroDocumento: otro.NumeroDocumento);

    /// <summary>
    /// El integrante tal como está guardado, sin el filtro del club, con sus equipos, su ficha y sus
    /// documentos: para ver qué cambió en el otro club.
    /// </summary>
    public Task<(UsuarioRol Integrante, FichaJugador? Ficha, List<DocumentoJugador> Documentos)> LeerSinFiltroAsync(
        Guid usuarioRolId) => Fabrica.ConContextoAsync(async contexto =>
        {
            var integrante = await contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking()
                .Include(fila => fila.Equipos)
                .FirstAsync(fila => fila.Id == usuarioRolId);
            var ficha = await contexto.FichasJugador.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(fila => fila.UsuarioRolId == usuarioRolId);
            var documentos = await contexto.DocumentosJugador.IgnoreQueryFilters().AsNoTracking()
                .Where(fila => fila.UsuarioRolId == usuarioRolId)
                .ToListAsync();
            return (integrante, ficha, documentos);
        });
}
