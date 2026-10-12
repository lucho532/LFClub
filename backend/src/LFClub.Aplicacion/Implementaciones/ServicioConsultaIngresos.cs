using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de consultas del apartado "Ingresos".
/// Su responsabilidad es leer la sala de espera y los ingresos aprobados del club de la petición y
/// convertirlos en DTO. Para avisar al PRESIDENTE de que aprobar un ingreso retirará a un jugador
/// de otra cuenta en otro club, lee en una sola consulta los integrantes que tienen los números de
/// quienes esperan, en cualquier club, y les aplica <see cref="ReglaDocumentoCompartido"/>; de
/// ellos no entrega nada más que ese aviso (RF-038 de la 006). A los aprobados les añade el enlace a
/// la ficha básica de quien aprobó, resuelto en una sola consulta para toda la lista (spec 008).
/// No modifica datos, no accede al contexto de Entity Framework y no recibe un identificador de
/// club: solo existe el que fijó la autorización.
/// </summary>
public class ServicioConsultaIngresos : IServicioConsultaIngresos
{
    private readonly IRepositorioIngresos _ingresos;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly ResolutorDeEnlaces _enlaces;

    /// <summary>
    /// Crea el servicio con el acceso a los ingresos del club, a los números de documento y a los
    /// enlaces a fichas.
    /// </summary>
    public ServicioConsultaIngresos(
        IRepositorioIngresos ingresos, IRepositorioPertenencias pertenencias, ResolutorDeEnlaces enlaces)
    {
        _ingresos = ingresos;
        _pertenencias = pertenencias;
        _enlaces = enlaces;
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
    public async Task<IReadOnlyList<IngresoAprobadoDto>> ListarAprobadosAsync(CancellationToken cancelacion = default)
    {
        var aprobados = await _ingresos.ListarAprobadosAsync(cancelacion);
        var autores = await _enlaces.AutoresAsync(aprobados.Select(ingreso => ingreso.AprobadoPorUsuarioId), cancelacion);

        return aprobados
            .Select(ingreso => MapperIngresos.AIngresoAprobado(
                ingreso,
                ingreso.AprobadoPorUsuarioId is { } autorId ? autores.GetValueOrDefault(autorId) : null))
            .ToList();
    }
}
