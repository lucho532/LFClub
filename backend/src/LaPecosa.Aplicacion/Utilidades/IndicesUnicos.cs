namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa los nombres de los índices únicos de la base de datos.
/// Su responsabilidad es que la configuración del modelo y los servicios que traducen una
/// violación de unicidad a un error del contrato usen el mismo nombre.
/// No crea los índices: eso lo hace la configuración de Entity Framework.
/// </summary>
public static class IndicesUnicos
{
    /// <summary>Nombre normalizado de un club (RF-007).</summary>
    public const string NombreDeClub = "IX_Clubes_NombreNormalizado";

    /// <summary>Correo normalizado de una cuenta (RF-002).</summary>
    public const string CorreoDeUsuario = "IX_Usuarios_CorreoNormalizado";

    /// <summary>Como máximo una cuenta DESARROLLADOR (RF-001).</summary>
    public const string UnicoDesarrollador = "IX_Usuarios_EsDesarrollador";

    /// <summary>Documento dentro de un club (RF-017).</summary>
    public const string DocumentoEnClub = "IX_UsuariosRol_ClubId_NumeroDocumento";

    /// <summary>Una sola categoría por club y año de nacimiento, contando las inactivas (RF-003).</summary>
    public const string CategoriaEnClub = "IX_Categorias_ClubId_Anio";

    /// <summary>Nombre de un equipo activo dentro de su categoría, sin distinguir mayúsculas (RF-023).</summary>
    public const string EquipoEnCategoria = "IX_Equipos_CategoriaId_NombreNormalizado";

    /// <summary>Clave de publicación de un comunicado dentro de un club (spec 011, RF-010).</summary>
    public const string ClaveDeComunicado = "IX_Comunicados_ClubId_Clave";
}
