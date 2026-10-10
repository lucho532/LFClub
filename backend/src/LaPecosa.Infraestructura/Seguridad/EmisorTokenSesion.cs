using System.Security.Claims;
using System.Text;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LaPecosa.Infraestructura.Seguridad;

/// <summary>
/// Representa la emisión de tokens JWT de sesión (research §4).
/// Su responsabilidad es firmar un token con el identificador del usuario y su sello de seguridad
/// y, si la sesión se inició con el documento de un jugador, con una reclamación por cada
/// integrante al que queda limitada (research §3 de la 006).
/// No incluye el club ni el rol: la pertenencia se comprueba en cada petición contra la base de
/// datos. Tampoco valida tokens recibidos.
/// </summary>
public class EmisorTokenSesion : IEmisorTokenSesion
{
    private readonly OpcionesSesion _opciones;
    private readonly IReloj _reloj;

    /// <summary>Crea el emisor con la configuración de la sesión.</summary>
    public EmisorTokenSesion(IOptions<OpcionesSesion> opciones, IReloj reloj)
    {
        _opciones = opciones.Value;
        _reloj = reloj;
    }

    /// <summary>Clave simétrica con la que se firman y se validan los tokens.</summary>
    public static SymmetricSecurityKey CrearClave(string claveFirma) => new(Encoding.UTF8.GetBytes(claveFirma));

    /// <inheritdoc />
    public TokenEmitido Emitir(Guid usuarioId, Guid selloSeguridad, IReadOnlyCollection<Guid>? jugadores = null)
    {
        var ahora = _reloj.AhoraUtc;
        var venceEn = ahora.AddDays(_opciones.DiasVigencia);

        List<Claim> reclamaciones =
        [
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(OpcionesSesion.ReclamacionSello, selloSeguridad.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        // Sin limitación el token es el de siempre: no lleva la reclamación, ni vacía.
        reclamaciones.AddRange((jugadores ?? []).Select(jugador =>
            new Claim(OpcionesSesion.ReclamacionJugadores, jugador.ToString())));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(reclamaciones),
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = venceEn,
            SigningCredentials = new SigningCredentials(
                CrearClave(_opciones.ClaveFirma), SecurityAlgorithms.HmacSha256),
        };

        return new TokenEmitido(new JsonWebTokenHandler().CreateToken(descriptor), venceEn);
    }
}
