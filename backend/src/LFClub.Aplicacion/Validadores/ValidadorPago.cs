using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de los datos de dinero y de los pagos (spec 015, RF-014, RF-017 a RF-022).
/// Su responsabilidad es exigir que un importe sea un número entero de pesos y mayor que cero, que la
/// fecha de un pago no sea futura, que la entidad sea obligatoria en una transferencia (y no se guarde
/// en efectivo), que la referencia tenga hasta 60 caracteres y que un motivo sea obligatorio y de hasta
/// 300. Si algo falla lanza <c>datos_invalidos</c> con el error en cada campo.
/// No comprueba que el valor quepa en lo pendiente, ni que el jugador exista, ni quién puede hacerlo:
/// eso es de la regla de aplicación y del servicio.
/// </summary>
public static class ValidadorPago
{
    /// <summary>Longitud máxima de la entidad.</summary>
    public const int LongitudDeEntidad = 40;

    /// <summary>Longitud máxima de la referencia.</summary>
    public const int LongitudDeReferencia = 60;

    /// <summary>Longitud máxima de un motivo de rechazo o de anulación.</summary>
    public const int LongitudDeMotivo = 300;

    /// <summary>Valor máximo admitido, muy por debajo del límite de <c>numeric(14,2)</c>.</summary>
    public const decimal ValorMaximo = 999_999_999m;

    /// <summary>Datos de un pago ya validados y limpios.</summary>
    /// <param name="Valor">Valor en pesos enteros.</param>
    /// <param name="Medio">Medio.</param>
    /// <param name="Entidad">Entidad sin espacios sobrantes; nula en efectivo.</param>
    /// <param name="Referencia">Referencia sin espacios sobrantes; nula si va vacía.</param>
    /// <param name="Fecha">Fecha del pago.</param>
    /// <param name="CargoIds">Cargos elegidos, sin repetir; nula si no se eligió ninguno.</param>
    public record DatosDePago(
        decimal Valor, MedioPago Medio, string? Entidad, string? Referencia, DateOnly Fecha, IReadOnlyList<Guid>? CargoIds);

    /// <summary>Valida el registro de un pago por el PRESIDENTE.</summary>
    public static DatosDePago ValidarRegistro(RegistrarPagoDto datos, DateOnly hoy)
    {
        var errores = new ErroresDeValidacion();
        ValidarValor(errores, "valor", datos.Valor);
        if (!Enum.IsDefined(datos.Medio))
        {
            errores.Agregar("medio", "El medio de pago no es válido.");
        }

        var entidad = Limpiar(datos.Entidad);
        if (datos.Medio == MedioPago.TRANSFERENCIA)
        {
            errores.Obligatorio("entidad", entidad, LongitudDeEntidad, "La entidad");
        }
        else
        {
            entidad = null;
        }

        var referencia = Limpiar(datos.Referencia);
        errores.Maximo("referencia", referencia, LongitudDeReferencia, "La referencia");
        ValidarFecha(errores, datos.Fecha, hoy);
        errores.LanzarSiHayErrores();

        return new DatosDePago(
            datos.Valor, datos.Medio, entidad, referencia, datos.Fecha, ElegidosSinRepetir(datos.CargoIds));
    }

    /// <summary>Valida lo que informa la familia: siempre una transferencia, con su entidad.</summary>
    public static DatosDePago ValidarInformado(InformarPagoDto datos, DateOnly hoy)
    {
        var errores = new ErroresDeValidacion();
        ValidarValor(errores, "valor", datos.Valor);
        var entidad = Limpiar(datos.Entidad);
        errores.Obligatorio("entidad", entidad, LongitudDeEntidad, "La entidad");
        var referencia = Limpiar(datos.Referencia);
        errores.Maximo("referencia", referencia, LongitudDeReferencia, "La referencia");
        ValidarFecha(errores, datos.Fecha, hoy);
        errores.LanzarSiHayErrores();

        return new DatosDePago(datos.Valor, MedioPago.TRANSFERENCIA, entidad, referencia, datos.Fecha, null);
    }

    /// <summary>Valida el valor corregido al confirmar, si lo hay.</summary>
    public static void ValidarCorreccion(decimal? valor)
    {
        if (valor is null)
        {
            return;
        }

        var errores = new ErroresDeValidacion();
        ValidarValor(errores, "valor", valor.Value);
        errores.LanzarSiHayErrores();
    }

    /// <summary>Valida el valor de la mensualidad del club o el propio de un jugador.</summary>
    public static void ValidarValorDeMensualidad(decimal valor)
    {
        var errores = new ErroresDeValidacion();
        ValidarValor(errores, "valor", valor);
        errores.LanzarSiHayErrores();
    }

    /// <summary>Valida un motivo de rechazo o de anulación y lo devuelve sin espacios sobrantes.</summary>
    public static string ValidarMotivo(MotivoDto datos)
    {
        var errores = new ErroresDeValidacion();
        var motivo = Limpiar(datos.Motivo);
        errores.Obligatorio("motivo", motivo, LongitudDeMotivo, "El motivo");
        errores.LanzarSiHayErrores();
        return motivo!;
    }

    private static void ValidarValor(ErroresDeValidacion errores, string campo, decimal valor)
    {
        if (valor <= 0m)
        {
            errores.Agregar(campo, "El valor debe ser mayor que cero.");
        }
        else if (valor != decimal.Truncate(valor))
        {
            errores.Agregar(campo, "El valor debe ser un número entero de pesos, sin centavos.");
        }
        else if (valor > ValorMaximo)
        {
            errores.Agregar(campo, "El valor es demasiado alto.");
        }
    }

    private static void ValidarFecha(ErroresDeValidacion errores, DateOnly fecha, DateOnly hoy)
    {
        if (fecha == default)
        {
            errores.Agregar("fecha", "La fecha es obligatoria.");
        }
        else if (fecha > hoy)
        {
            errores.Agregar("fecha", "La fecha no puede ser futura.");
        }
    }

    private static string? Limpiar(string? texto)
    {
        var limpio = NormalizadorTexto.SinEspaciosSobrantes(texto);
        return limpio.Length == 0 ? null : limpio;
    }

    private static IReadOnlyList<Guid>? ElegidosSinRepetir(IReadOnlyList<Guid>? ids) =>
        ids is { Count: > 0 } ? ids.Distinct().ToList() : null;
}
