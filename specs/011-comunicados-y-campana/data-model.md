# Modelo de datos: comunicados y campana

**Funcionalidad**: `011-comunicados-y-campana` | **Fecha**: 2026-10-12

Tres tablas nuevas y una migración: `ComunicadosYCampana`. Todo bajo el filtro de club de la 001
(§7.1): las tres entidades implementan `IPerteneceAClub`. Las decisiones están en
[research.md](research.md); el contrato, en [contracts/api.yaml](contracts/api.yaml).

Convenciones de las specs anteriores: `ClubId` obligatorio con foránea a `Clubes` en cascada,
fechas de registro en UTC (`...En`), autores por integrante (`...PorUsuarioRolId`, a nulo si se
borra) y por nombre copiado en el momento (§13).

## Enumeraciones nuevas (`LaPecosa.Dominio/Enumeraciones`)

| Enumeración | Valores | Guardada como |
| --- | --- | --- |
| `AlcanceComunicado` | `CLUB`, `CATEGORIAS`, `EQUIPOS` | texto |

El rol del autor reutiliza `Rol` (PRESIDENTE, DIRECTIVO o ENTRENADOR).

## Comunicado (`Comunicados`): nueva

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | uuid | Clave; `Guid.CreateVersion7()`. Sirve de cursor de paginación (ordena por creación) |
| `ClubId` | uuid | Obligatorio; foránea a `Clubes` en cascada |
| `Titulo` | texto ≤ 100 | Obligatorio, recortado, sin ser solo espacios |
| `Texto` | texto ≤ 1000 | Obligatorio, recortado, texto plano |
| `Alcance` | `AlcanceComunicado` | Obligatorio |
| `Clave` | uuid | Obligatoria; única por club (idempotencia, research §6) |
| `PublicadoEn` | timestamptz | Obligatorio |
| `PublicadoPorUsuarioRolId` | uuid | Foránea a `UsuarioRoles`; a nulo si el integrante se borra |
| `PublicadoPorNombre` | texto ≤ 161 | Copiado (§13) |
| `PublicadoPorRol` | `Rol` (texto) | Rol que tenía al publicar; copiado (§13) |
| `RetiradoEn` | timestamptz | Nulo mientras está publicado |
| `RetiradoPorUsuarioRolId` | uuid | A nulo si el integrante se borra |
| `RetiradoPorNombre` | texto ≤ 161 | Copiado |

Restricciones de base: `RetiradoEn` y `RetiradoPorNombre` son los dos nulos o los dos con valor;
`char_length(Titulo) > 0` y `char_length(Texto) > 0`. Índices: `(ClubId, Id)` para listar por club
y `(ClubId, Clave)` único.

**No hay** campo de edición ni `Activo`: el retiro es el único cambio posible (research §7, §15).

## ComunicadoDestino (`ComunicadosDestino`): nueva

Una fila por categoría o equipo al que se dirigió. Un comunicado con alcance `CLUB` no tiene filas.

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | uuid | Clave |
| `ClubId` | uuid | Obligatorio |
| `ComunicadoId` | uuid | Foránea a `Comunicados` en cascada |
| `CategoriaId` | uuid | Foránea a `Categorias`; a nulo si se borra |
| `CategoriaNombre` | texto | Copiado (§13) |
| `EquipoId` | uuid | Solo con alcance `EQUIPOS`; foránea a `Equipos`; a nulo si se borra |
| `EquipoNombre` | texto | Copiado; solo con alcance `EQUIPOS` |

Alcance `CATEGORIAS`: una fila por categoría, sin equipo. Alcance `EQUIPOS`: una fila por equipo,
todas con la misma categoría (research §4). La coherencia (alcance contra filas) la garantiza el
servicio; la base no la comprueba.

## ComunicadoDestinatario (`ComunicadosDestinatario`): nueva

Una fila por comunicado y por jugador activo que lo recibe al publicar (research §1 a §3).

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `ComunicadoId` | uuid | Parte de la clave; foránea en cascada |
| `UsuarioRolId` | uuid | Parte de la clave; foránea al jugador, en cascada |
| `ClubId` | uuid | Obligatorio |
| `UsuarioId` | uuid | Cuenta dueña del jugador; foránea a `Usuarios` en cascada |
| `LeidoEn` | timestamptz | Nulo mientras no lo lea esa cuenta; igual en las filas de la misma cuenta y comunicado |

Índices: `(UsuarioId, ClubId, ComunicadoId)` para la lista de la cuenta, y parcial
`(UsuarioId, ClubId) WHERE LeidoEn IS NULL` para el contador.

**Reglas**:

- La clave `(ComunicadoId, UsuarioRolId)` impide recibir dos veces por el mismo jugador.
- Leer es poner `LeidoEn` en todas las filas de la cuenta para ese comunicado, solo si es nulo
  (no se mueve la primera lectura).
- Nunca se crean filas después de publicar (research §1).
- Al retirar a un jugador no se borran sus filas; solo dejan de contar mientras no esté activo.

## Entidades que cambian

| Entidad | Cambio |
| --- | --- |
| `UsuarioRol` | Solo documentación: de él cuelgan los comunicados que recibió |
| `Categoria` y `Equipo` | Solo documentación: pueden ser destino de un comunicado |

## Vistas calculadas (sin tabla)

### Comunicados de una cuenta (campana)

```text
visibles = Destinatarios con UsuarioId = cuenta de la petición, ClubId = club
           y UsuarioRolId en (limitación de la sesión, si la hay)
           y jugador (UsuarioRolId) aprobado y sin retirar
           y Comunicado.RetiradoEn nulo
sinLeer  = COUNT(DISTINCT ComunicadoId) de visibles con LeidoEn nulo
lista    = visibles agrupados por ComunicadoId, del Id más reciente al más antiguo
leido    = ninguna fila del grupo con LeidoEn nulo
```

### Comunicados publicados (equipo del club)

```text
PRESIDENTE, DIRECTIVO : todos los del club, retirados incluidos
ENTRENADOR            : los de PublicadoPorUsuarioRolId = su integrante
familiasAlcanzadas    : COUNT(DISTINCT UsuarioId) de sus destinatarios
```

## Transiciones

```text
(nada) ──publica (rol y audiencia permitidos, al menos un destinatario)──▶ PUBLICADO
PUBLICADO ──autor o PRESIDENTE retira──▶ RETIRADO       (no vuelve)
```

El estado leído de cada cuenta es independiente: `sin leer ──abre / marcar todos──▶ leído`.
