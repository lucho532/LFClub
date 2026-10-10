using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de consultas del apartado "Ingresos".
/// Su responsabilidad es leer la sala de espera y los ingresos aprobados del club de la petición y
/// convertirlos en DTO. Para avisar al PRESIDENTE de que aprobar un ingreso retirará a un jugador
/// de otra cuenta en otro club, lee en una sola consulta los integrantes que tienen los números de
/// quienes esperan, en cualquier club, y les aplica <see cref="ReglaDocumentoCompartido"/>; de
/// ellos no entrega nada más que ese aviso (RF-038 de la 006).
/// No modifica datos, no accede al contexto de Entity Framework y no recibe un identificador de
/// club: solo existe el que fijó la autorización.
/// </summary>
public class ServicioConsultaIngresos : IServicioConsultaIngresos
{
    private readonly IRepositorioIngresos _ingresos;
    private readonly IRepositorioPertenencias _pertenencias;

    /// <summary>Crea el servicio con el acceso a los ingresos del club y a los números de documento.</summary>
    public ServicioConsultaIngresos(IRepositorioIngresos ingresos, IRepositorioPertenencias pertenencias)
    {
        _ingresos = ingresos;
        _pertenencias = pertenencias;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngresoEnEsperaDto>> ListarEnEsperaAsync(CancellationToken cancelacion = default)
    {
        var enEspera = await _ingresos.ListarEnEsperaAsync(cancelacion);
        var numeros = enEspera.Select(ingreso => ingreso.NumeroDocumento).Distinct().ToList();
        var conEsosNumeros = await _pertenencias.ListarPorDocumentosAsync(numeros, cancelacion);

        // Se calcula al abrir la sala de espera: si el otro club lo retiró entre tanto, ya no avisa.
        return enEspera
            .Select(ingreso => MapperIngresos.AIngresoEnEspera(
                ingreso,
                ReglaDocumentoCompartido.Evaluar(
                    conEsosNumeros.Where(integrante => integrante.NumeroDocumento == ingreso.NumeroDocumento).ToList(),
                    ingreso.UsuarioId) == DocumentoEnOtraCuenta.ConBaja))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngresoAprobadoDto>> ListarAprobadosAsync(CancellationToken cancelacion = default) =>
        (await _ingresos.ListarAprobadosAsync(cancelacion)).Select(MapperIngresos.AIngresoAprobado).ToList();
}
