using System.Text;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Crea fichas y documentos de jugadores directamente en la base de datos, con datos reconocibles,
/// y da los archivos de prueba en memoria: a la ficha le basta la firma binaria.
/// </summary>
public class SembradorFichas
{
    public const string EmergenciaNombre = "Marta Gómez (emergencia sembrada)";
    public const string EmergenciaParentesco = "Madre";
    public const string EmergenciaCelular = "3115550001";
    public const string EntidadSalud = "Salud Sembrada EPS";
    public const string LugarAtencion = "Clínica Sembrada, sede Minitas";
    public const string Alergias = "Alergia sembrada a la penicilina";
    public const string Enfermedades = "Asma leve sembrada";
    public const string Medicamentos = "Salbutamol sembrado";
    public const string Observaciones = "Observación clínica sembrada";

    /// <summary>Los cuatro textos clínicos sembrados: ninguno debe llegar a un DIRECTIVO.</summary>
    public static readonly string[] TextosClinicos = [Alergias, Enfermedades, Medicamentos, Observaciones, "O_NEGATIVO"];

    private readonly FabricaApi _fabrica;

    public SembradorFichas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>La ficha completa de ese jugador, cambiada por última vez por quien se indique.</summary>
    public Task<FichaJugador> CrearFichaAsync(
        UsuarioRol jugador, UsuarioRol? cambiadaPor = null, DateTime? cambiadaEn = null) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var autor = cambiadaPor ?? jugador;
            var ficha = new FichaJugador
            {
                UsuarioRolId = jugador.Id,
                ClubId = jugador.ClubId,
                EmergenciaNombre = EmergenciaNombre,
                EmergenciaParentesco = EmergenciaParentesco,
                EmergenciaCelular = EmergenciaCelular,
                EntidadSalud = EntidadSalud,
                LugarAtencion = LugarAtencion,
                GrupoSanguineo = GrupoSanguineo.O_NEGATIVO,
                Alergias = Alergias,
                Enfermedades = Enfermedades,
                Medicamentos = Medicamentos,
                Observaciones = Observaciones,
                UltimoCambioEn = cambiadaEn ?? DateTime.UtcNow,
                UltimoCambioPorUsuarioId = autor.UsuarioId,
                UltimoCambioPorNombre = $"{autor.Nombres} {autor.Apellidos}",
            };
            contexto.FichasJugador.Add(ficha);
            await contexto.SaveChangesAsync();
            return ficha;
        });

    /// <summary>Un archivo ya entregado para ese documento pedido.</summary>
    public Task<DocumentoJugador> CrearDocumentoAsync(
        UsuarioRol jugador, DocumentoPedido documento, byte[] contenido, string tipoContenido) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var archivo = new DocumentoJugador
            {
                UsuarioRolId = jugador.Id,
                Documento = documento,
                ClubId = jugador.ClubId,
                Contenido = contenido,
                TipoContenido = tipoContenido,
                TamanoBytes = contenido.Length,
                SubidoEn = DateTime.UtcNow,
            };
            contexto.DocumentosJugador.Add(archivo);
            await contexto.SaveChangesAsync();
            return archivo;
        });

    public static byte[] Pdf() => [.. "%PDF-1.7\n"u8, .. new byte[16]];

    /// <summary>Un PDF de exactamente ese tamaño, para probar el límite.</summary>
    public static byte[] Pdf(int tamanoBytes)
    {
        var contenido = new byte[tamanoBytes];
        "%PDF-"u8.CopyTo(contenido);
        return contenido;
    }

    public static byte[] Jpeg() => Imagenes.Jpeg();

    public static byte[] Png() => Imagenes.Png();

    public static byte[] Webp() => Imagenes.Webp();

    /// <summary>Texto con extensión falsa: se envía con nombre <c>.pdf</c> y no lo es.</summary>
    public static byte[] TextoDisfrazado() => Encoding.UTF8.GetBytes("esto no es un PDF ni una imagen");
}
