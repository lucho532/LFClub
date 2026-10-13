using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Pruebas.Unitarias.Utilidades;

public class ArmadoDeAudienciaPruebas
{
    private static readonly Guid C2014 = Guid.NewGuid();
    private static readonly Guid C2015 = Guid.NewGuid();
    private static readonly Guid EquipoA = Guid.NewGuid();
    private static readonly Guid EquipoDeOtraCategoria = Guid.NewGuid();
    private static readonly Guid EquipoInactivo = Guid.NewGuid();
    private static readonly Guid EquipoDeCategoriaInactiva = Guid.NewGuid();
    private static readonly Guid CategoriaInactiva = Guid.NewGuid();
    private static readonly Guid Natural = Guid.NewGuid();
    private static readonly Guid Retirado = Guid.NewGuid();
    private static readonly Guid Sumable = Guid.NewGuid();

    private readonly ArmadoDeAudiencia _armado = new(new RepositorioFalso());

    [Fact]
    public async Task Sin_equipos_ni_anadidos_queda_vacia()
    {
        var armada = await _armado.ArmarAsync([C2014], null, null, []);

        Assert.Empty(armada.EquipoIds);
        Assert.Empty(armada.JugadorAnadidoIds);
    }

    [Fact]
    public async Task Un_equipo_valido_de_la_categoria_se_guarda()
    {
        var armada = await _armado.ArmarAsync([C2014], [EquipoA], null, []);

        Assert.Equal([EquipoA], armada.EquipoIds);
    }

    [Theory]
    [MemberData(nameof(EquiposNoValidos))]
    public async Task Un_equipo_no_valido_se_rechaza(Guid equipo)
    {
        var error = await Assert.ThrowsAsync<ExcepcionDeAplicacion>(
            () => _armado.ArmarAsync([C2014], [equipo], null, []));

        Assert.Equal("equipo_no_valido", error.Codigo);
        Assert.Equal(422, error.EstadoHttp);
    }

    public static TheoryData<Guid> EquiposNoValidos() =>
        [EquipoDeOtraCategoria, EquipoInactivo, EquipoDeCategoriaInactiva, Guid.NewGuid()];

    [Fact]
    public async Task El_equipo_desactivado_que_ya_tenia_se_conserva()
    {
        var armada = await _armado.ArmarAsync([C2014], [EquipoA], null, [EquipoInactivo]);

        Assert.Equivalent(new[] { EquipoA, EquipoInactivo }, armada.EquipoIds);
    }

    [Fact]
    public async Task Un_anadido_retirado_se_rechaza()
    {
        var error = await Assert.ThrowsAsync<ExcepcionDeAplicacion>(
            () => _armado.ArmarAsync([C2014], null, [Retirado], []));

        Assert.Equal("jugador_fuera_de_la_actividad", error.Codigo);
    }

    [Fact]
    public async Task Un_anadido_repetido_se_cuenta_una_vez()
    {
        var armada = await _armado.ArmarAsync([C2014], null, [Sumable, Sumable], []);

        Assert.Equal([Sumable], armada.JugadorAnadidoIds);
    }

    [Fact]
    public async Task Un_anadido_de_la_audiencia_natural_se_acepta_sin_duplicarlo()
    {
        var armada = await _armado.ArmarAsync([C2014], null, [Natural, Sumable], []);

        Assert.Equal([Sumable], armada.JugadorAnadidoIds);
    }

    [Fact]
    public async Task Un_anadido_de_otra_categoria_se_acepta()
    {
        var armada = await _armado.ArmarAsync([C2015], null, [Sumable], []);

        Assert.Equal([Sumable], armada.JugadorAnadidoIds);
    }

    private sealed class RepositorioFalso : IRepositorioAudiencia
    {
        public Task<IReadOnlyList<EquipoParaAudiencia>> EquiposAsync(IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default)
        {
            var todos = new List<EquipoParaAudiencia>
            {
                new(EquipoA, C2014, true, true),
                new(EquipoDeOtraCategoria, C2015, true, true),
                new(EquipoInactivo, C2014, false, true),
                new(EquipoDeCategoriaInactiva, CategoriaInactiva, true, false),
            };
            return Task.FromResult<IReadOnlyList<EquipoParaAudiencia>>(todos.Where(e => equipoIds.Contains(e.EquipoId)).ToList());
        }

        public Task<IReadOnlySet<Guid>> JugadoresSumablesAsync(IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(usuarioRolIds.Where(id => id == Natural || id == Sumable).ToHashSet());

        public Task<IReadOnlySet<Guid>> AudienciaNaturalAsync(
            IReadOnlyCollection<Guid> categoriaIds, IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(categoriaIds.Contains(C2014) ? new HashSet<Guid> { Natural } : new HashSet<Guid>());

        public Task<IReadOnlySet<Guid>> AudienciaDeAsync(Guid entrenamientoId, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<JugadorSumableDto>> BuscarSumablesAsync(string texto, int limite, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<JugadorSumableDto>> AnadidosAsync(Guid entrenamientoId, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task ReemplazarAsync(
            Guid entrenamientoId, IReadOnlyCollection<Guid> equipoIds, IReadOnlyCollection<Guid> anadidoIds, DateTime ahoraUtc,
            CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task QuitarRespuestasAsync(Guid entrenamientoId, IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();
    }
}
