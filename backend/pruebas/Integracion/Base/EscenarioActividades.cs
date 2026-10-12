using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>Un integrante del escenario con su cuenta y su cliente con sesión.</summary>
public record Persona(Usuario Cuenta, UsuarioRol Integrante, ClienteDePrueba Cliente)
{
    public Guid Id => Integrante.Id;
}

/// <summary>El club de "Datos de partida" de la guía de la 009, con todo lo que crea el sembrador.</summary>
public record EscenarioActividades(
    Club Club,
    Categoria C2013,
    Categoria C2014,
    Categoria C2015,
    Categoria C2016,
    Persona P,
    Persona E1,
    Persona E2,
    Persona D1,
    Persona D2,
    IReadOnlyList<Persona> J,
    Persona K1,
    Persona M1,
    Club ClubB,
    Categoria CB,
    Persona PB,
    Persona JugadorB)
{
    /// <summary>J1 a J10 por su número (J(1) es J1).</summary>
    public Persona Jn(int numero) => J[numero - 1];

    /// <summary>Ruta de la API del club A, con lo que siga.</summary>
    public string Ruta(string resto = "") => $"/api/clubes/{Club.Id}{resto}";

    /// <summary>Ruta de la API del club B, con lo que siga.</summary>
    public string RutaB(string resto = "") => $"/api/clubes/{ClubB.Id}{resto}";
}
