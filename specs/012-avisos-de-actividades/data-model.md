# Modelo de datos: avisos de entrenamientos y partidos

**Funcionalidad**: `012-avisos-de-actividades` | **Fecha**: 2026-10-12

Dos tablas nuevas y una migración: `AvisosDeActividad`. Todo bajo el filtro de club de la 001
(§7.1): las dos entidades implementan `IPerteneceAClub`. Las decisiones están en
[research.md](research.md); el contrato, en [contracts/api.yaml](contracts/api.yaml).

Convenciones de las specs anteriores: `ClubId` obligatorio con foránea a `Clubes` en cascada, fechas
de registro en UTC (`...En`), enumeraciones guardadas como texto.

## Enumeraciones nuevas (`LaPecosa.Dominio/Enumeraciones`)

| Enumeración | Valores | Guardada como |
| --- | --- | --- |
| `TipoDeActividad` | `ENTRENAMIENTO`, `PARTIDO` | texto |
| `HechoDeActividad` | `ENTRENAMIENTO_PROGRAMADO`, `ENTRENAMIENTO_CAMBIADO`, `ENTRENAMIENTO_CANCELADO`, `ENTRENAMIENTO_REACTIVADO`, `ASISTENCIA_REGISTRADA`, `PARTIDO_CAMBIADO`, `PARTIDO_CANCELADO`, `PARTIDO_REACTIVADO`, `JUGADOR_CONVOCADO`, `RESULTADO_PUBLICADO` | texto |

## AvisoDeActividad (`AvisosDeActividad`): nueva

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | uuid | Clave; `Guid.CreateVersion7()`. Sirve de cursor de la campana (ordena por creación) |
| `ClubId` | uuid | Obligatorio; foránea a `Clubes` en cascada |
| `Hecho` | `HechoDeActividad` | Obligatorio |
| `ActividadTipo` | `TipoDeActividad` | Obligatorio; coherente con `Hecho` (lo garantiza el servicio) |
| `ActividadId` | uuid | Obligatorio; entrenamiento o partido del club; **sin foránea** (research §2) |
| `Texto` | texto ≤ 300 | Obligatorio, ya redactado, texto plano (research §10) |
| `GeneradoEn` | timestamptz | Obligatorio |

Restricción de base: `char_length(Texto) > 0`. Índices: `(ClubId, Id)` y
`(ClubId, ActividadTipo, ActividadId)`.

**No hay** autor, título, audiencia, retiro ni campo de edición: el aviso lo escribe el sistema y
solo desaparece por el reemplazo del §"Transiciones".

## AvisoDeActividadDestinatario (`AvisosDeActividadDestinatario`): nueva

Una fila por aviso y por jugador activo que lo recibe al generarse (research §7).

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `AvisoId` | uuid | Parte de la clave; foránea a `AvisosDeActividad` en cascada |
| `UsuarioRolId` | uuid | Parte de la clave; foránea al jugador, en cascada |
| `ClubId` | uuid | Obligatorio |
| `UsuarioId` | uuid | Cuenta dueña del jugador; foránea a `Usuarios` en cascada |
| `LeidoEn` | timestamptz | Nulo mientras no lo lea esa cuenta; igual en las filas de la misma cuenta y aviso |

Índices: `(UsuarioId, ClubId, AvisoId)` para la lista de la cuenta, y parcial
`(UsuarioId, ClubId) WHERE LeidoEn IS NULL` para el contador.

**Reglas**:

- La clave `(AvisoId, UsuarioRolId)` impide recibir dos veces por el mismo jugador.
- Leer es poner `LeidoEn` en todas las filas de la cuenta para ese aviso, solo si es nulo.
- Nunca se crean filas después de generar el aviso (RF-011).
- Una convocatoria crea filas solo para los jugadores convocados (research §9); un aviso de partido
  próximo, para los convocados activos; los demás, para los jugadores activos de las categorías.
- Al retirar a un jugador no se borran sus filas; solo dejan de contar mientras no esté activo.

## Entidades que cambian

| Entidad | Cambio |
| --- | --- |
| `UsuarioRol` | Solo documentación: de él cuelgan los avisos que recibió |
| `Entrenamiento` y `Partido` | Solo documentación: pueden ser la actividad de un aviso |
| `IRepositorioAsistencias.RegistrarAsync` | Devuelve las categorías con asistencia nueva o cambiada (research §6) |
| `IServicioConsultaComunicados` y `IServicioLecturaComunicados` | Pierden la lista, el contador y "marcar todos", que pasan a la campana (research §4) |

Ninguna columna de las tablas existentes cambia.

## Vistas calculadas (sin tabla)

### Avisos de una cuenta (campana)

```text
visibles = Destinatarios con UsuarioId = cuenta de la petición, ClubId = club
           y UsuarioRolId en (limitación de la sesión, si la hay)
           y jugador (UsuarioRolId) aprobado y sin retirar
sinLeerAvisos = COUNT(DISTINCT AvisoId) de visibles con LeidoEn nulo
lista         = visibles agrupados por AvisoId, del Id más reciente al más antiguo
leido         = ninguna fila del grupo con LeidoEn nulo
```

### Campana

```text
sinLeer  = sinLeerComunicados (011) + sinLeerAvisos
elementos = mezcla por Id descendente (bytes en orden de red) de los comunicados recibidos
            y de los avisos visibles, con el mismo cursor y el límite más uno de cada lado
hayMas   = alguno de los dos lados trajo más del límite, o la mezcla pasó del límite
```

### Disponibilidad de la actividad de un aviso (research §11)

```text
ENTRENAMIENTO: alguna categoría del entrenamiento (EntrenamientoCategoria) coincide con la
               categoría actual de un jugador activo de la cuenta (dentro de la limitación)
PARTIDO:       Partido.Estado = JUGADO
               o algún jugador activo de la cuenta (dentro de la limitación) tiene fila en
               PartidosJugador de ese partido
```

## Transiciones

```text
(nada) ──cambio real en una actividad, con destinatarios──▶ AVISO sin leer en cada cuenta
AVISO sin leer ──la cuenta lo abre, o "marcar todos"──▶ leído (por cuenta, no vuelve)
AVISO sin leer ──otro aviso de la misma actividad para esa cuenta──▶ reemplazado (fila borrada)
AVISO leído ──otro aviso de la misma actividad──▶ queda; el nuevo se suma
AVISO sin ningún destinatario ──en la misma transacción──▶ borrado
```

El leído de cada cuenta es independiente: dos familias del mismo aviso no se afectan.
