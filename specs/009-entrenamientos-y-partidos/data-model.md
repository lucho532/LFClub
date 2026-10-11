# Modelo de datos: entrenamientos, partidos, convocatorias y estadísticas

**Funcionalidad**: `009-entrenamientos-y-partidos` | **Fecha**: 2026-10-11

Siete tablas nuevas y una migración, `EntrenamientosYPartidos`. Todas las entidades implementan
`IPerteneceAClub`: el filtro global de club de la 001 las limita al club de la petición (§7.1).
Las decisiones están en [research.md](research.md); el contrato, en
[contracts/api.yaml](contracts/api.yaml).

Convenciones de las specs anteriores: clave `Guid` versión 7, `ClubId` obligatorio con foránea a
`Clubes`, fechas de registro en UTC (`...En`), nombres de autores copiados en el momento (§13).
Fecha y horas de las actividades en hora de Colombia, sin zona (research §2).

## Enumeraciones nuevas (`LaPecosa.Dominio/Enumeraciones`)

| Enumeración | Valores | Guardada como |
| --- | --- | --- |
| `EstadoEntrenamiento` | `PROGRAMADO`, `CANCELADO` | texto |
| `EstadoPartido` | `PROXIMO`, `CANCELADO`, `JUGADO` | texto |
| `CondicionPartido` | `LOCAL`, `VISITANTE` | texto |
| `TipoPartido` | `AMISTOSO`, `TORNEO` | texto |
| `RespuestaFamilia` | `ASISTIRA`, `NO_ASISTIRA` | texto |

## Entrenamiento (`Entrenamientos`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | uuid | Clave |
| `ClubId` | uuid | Obligatorio |
| `Fecha` | date | Obligatoria |
| `HoraInicio` | time | Obligatoria |
| `HoraFin` | time | Obligatoria; mayor que `HoraInicio` (RF-005) |
| `Lugar` | texto ≤ 120 | Obligatorio, sin espacios a los lados |
| `Nota` | texto ≤ 500 | Opcional |
| `Estado` | `EstadoEntrenamiento` | Nace `PROGRAMADO` |
| `CreadoEn` | timestamptz | |
| `CreadoPorUsuarioRolId` | uuid | Foránea a `UsuariosRol`, a nulo al borrarse |
| `Categorias` | lista de `EntrenamientoCategoria` | Una o más |

Índice: `(ClubId, Fecha, HoraInicio)` para el calendario y los próximos.

**Transiciones**: `PROGRAMADO → CANCELADO` y, mientras no haya empezado y sus categorías sigan
activas, `CANCELADO → PROGRAMADO` (reactivar, RF-038). Un entrenamiento cancelado no se modifica, no
admite asistencia ni respuestas (RF-004) y nunca se borra (RF-035). Cancelar y reactivar no tocan
sus categorías ni sus respuestas.

**Se modifica**: `Fecha`, `HoraInicio`, `HoraFin`, `Lugar` y `Nota`, mientras esté `PROGRAMADO`. Las
categorías no (research §16.1).

## EntrenamientoCategoria (`EntrenamientosCategoria`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `EntrenamientoId` | uuid | Clave compuesta; foránea, en cascada |
| `CategoriaId` | uuid | Clave compuesta; foránea `Restrict` |
| `ClubId` | uuid | Obligatorio |

Al crear: categorías distintas, activas y del club (RF-005). Índice por `CategoriaId`. Dirigirle una
actividad marca `Categoria.Usada` (research §11).

## AsistenciaEntrenamiento (`AsistenciasEntrenamiento`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `EntrenamientoId` | uuid | Clave compuesta; foránea, en cascada |
| `UsuarioRolId` | uuid | Clave compuesta; foránea al jugador, `Restrict` |
| `ClubId` | uuid | Obligatorio |
| `CategoriaId` | uuid | Categoría del jugador al registrarla por primera vez; no cambia (§13, §11.2) |
| `Presente` | bool | Presente o ausente |
| `RegistradaEn` | timestamptz | Última vez que se escribió |

Se escribe solo en un entrenamiento `PROGRAMADO` cuya `Fecha` ya llegó (RF-022). Índice por
`UsuarioRolId` para las estadísticas. Una fila existe solo si alguien registró la asistencia: el
porcentaje cuenta solo esas (RF-030).

## RespuestaEntrenamiento (`RespuestasEntrenamiento`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `EntrenamientoId` | uuid | Clave compuesta; foránea, en cascada |
| `UsuarioRolId` | uuid | Clave compuesta; foránea al jugador, en cascada |
| `ClubId` | uuid | Obligatorio |
| `Respuesta` | `RespuestaFamilia` | |
| `RespondidaEn` | timestamptz | |

La escribe solo la cuenta del jugador de la petición, mientras el entrenamiento esté `PROGRAMADO`,
no haya empezado y el jugador sea activo de una de sus categorías (RF-023). Es independiente de la
asistencia (RF-025).

## Partido (`Partidos`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | uuid | Clave |
| `ClubId` | uuid | Obligatorio |
| `Rival` | texto ≤ 120 | Obligatorio |
| `Fecha` | date | Obligatoria |
| `Hora` | time | Obligatoria |
| `Lugar` | texto ≤ 120 | Obligatorio |
| `Condicion` | `CondicionPartido` | Obligatoria |
| `Tipo` | `TipoPartido` | Obligatorio |
| `NombreTorneo` | texto ≤ 120 | Obligatorio si `Tipo = TORNEO`; nulo si es amistoso (RF-007, escenario 2.10) |
| `Estado` | `EstadoPartido` | Nace `PROXIMO` |
| `GolesClub` | int | Nulo hasta el resultado; 0 a 99 |
| `GolesRival` | int | Nulo hasta el resultado; 0 a 99 |
| `ResultadoCambiadoEn` | timestamptz | Último cambio del resultado (RF-020) |
| `ResultadoCambiadoPorUsuarioId` | uuid | Foránea a `Usuarios`, a nulo si la cuenta se elimina |
| `ResultadoCambiadoPorNombre` | texto | Copiado en ese momento (§13) |
| `CreadoEn` | timestamptz | |
| `CreadoPorUsuarioRolId` | uuid | Foránea a `UsuariosRol`, a nulo al borrarse |
| `Categorias` | lista de `PartidoCategoria` | Una o más |
| `Jugadores` | lista de `PartidoJugador` | La convocatoria; puede estar vacía |

Restricciones de base: `GolesClub` y `GolesRival` son ambos nulos o ambos con valor, y tienen valor
si y solo si `Estado = JUGADO`. Índice `(ClubId, Fecha, Hora)`.

**Transiciones** (`ReglaTransicionPartido`, research §7):

```text
PROXIMO ──cancelar──▶ CANCELADO
CANCELADO ──reactivar, si no ha empezado──▶ PROXIMO
PROXIMO ──registrar resultado──▶ JUGADO
JUGADO  ──corregir resultado──▶ JUGADO
```

Un `JUGADO` no se cancela ni vuelve a `PROXIMO` (RF-021). Reactivar exige que no haya empezado
y que sus categorías sigan activas, y deja convocatoria y respuestas como estaban (RF-038). Registrar el resultado exige que ya
haya empezado (`Fecha + Hora <= ahora`, RF-015).

**Se modifica**: rival, fecha, hora, lugar, condición, tipo y torneo, solo en `PROXIMO`. Las
categorías no (research §16.1 y §16.2).

**Dato calculado**: `FaltaResultado = Estado == PROXIMO && ya empezó` (Supuestos de la spec).

## PartidoCategoria (`PartidosCategoria`)

Igual que `EntrenamientoCategoria`, con `PartidoId`.

## PartidoJugador (`PartidosJugador`): el convocado

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `PartidoId` | uuid | Clave compuesta; foránea, en cascada |
| `UsuarioRolId` | uuid | Clave compuesta; foránea al jugador, `Restrict` |
| `ClubId` | uuid | Obligatorio |
| `CategoriaId` | uuid | Categoría del jugador al convocarlo; no cambia (§13, RF-018) |
| `Jugo` | bool | Falso al convocar; lo fija el resultado |
| `Goles` | int | 0 al convocar; solo mayor que 0 si `Jugo` (RF-017) |
| `Respuesta` | `RespuestaFamilia` | Nula hasta que la familia responde |
| `RespondidaEn` | timestamptz | |
| `ConvocadoEn` | timestamptz | |

Restricción de base: `Goles >= 0` y `Goles = 0` si no `Jugo`. Índice por `UsuarioRolId`.

- Alta: solo en `PROXIMO`; el jugador debe ser activo y su categoría actual una de las del partido
  (RF-009).
- Baja: solo en `PROXIMO`; borra la fila con su respuesta, sin rastro (RF-013).
- Retiro del jugador: se borran sus filas de partidos que no están `JUGADO` (RF-033, research §10).
- Cambio de categoría: nada cambia (RF-034).
- Suma de `Goles` del partido `<= GolesClub` (RF-017), validada por `ReglaResultadoDePartido`.

## Entidades que cambian

| Entidad | Cambio |
| --- | --- |
| `Categoria` | Solo documentación: `Usada` pasa a verdadero también al dirigirle una actividad, y una categoría con actividades no se borra |
| `UsuarioRol` | Solo documentación: de él cuelgan sus convocatorias, asistencias y respuestas; el retiro borra las convocatorias pendientes |
| `AsignacionEntrenadorCategoria` | Solo documentación: también da la gestión deportiva al PRESIDENTE o DIRECTIVO asignado (constitución 4.6.0) |

## Vistas calculadas (sin tabla)

### EstadisticasJugadorDto (RF-026 a RF-030)

```text
Total y, por cada categoría donde tenga registros:
  categoriaId, anio
  partidosJugados   = PartidosJugador con Jugo, de partidos JUGADO
  goles             = suma de Goles, de partidos JUGADO
  entrenamientosRegistrados = AsistenciasEntrenamiento de entrenamientos no cancelados
  entrenamientosAsistidos   = de esas, con Presente
  porcentajeAsistencia      = round(asistidos * 100 / registrados), 0 si registrados = 0
```

### Goleador (RF-029)

```text
Por categoría (PartidosJugador.CategoriaId), de partidos JUGADO, Goles > 0:
  nombres, apellidos, goles, enlace (EnlaceFichaDto de la 008 o null)
Orden: goles descendente, apellidos, nombres. Incluye retirados (escenario 6.5).
```

### Planilla de un entrenamiento

Para quien entrena alguna de sus categorías y para el PRESIDENTE: los jugadores activos cuya
categoría actual es una de las marcables por él (research §3), más los que ya tienen asistencia en
categorías marcables; cada uno con su respuesta (o "sin respuesta"), su asistencia (o "sin
registrar") y si quien pregunta puede marcarlo.

### Planilla de un partido

Para quien entrena alguna de sus categorías y para el PRESIDENTE: los convocados con su categoría
copiada, respuesta, `jugo` y `goles`; y, si el partido está `PROXIMO`, los jugadores activos de sus
categorías que se pueden convocar. Nunca para una cuenta de jugador (RF-012).

## Consultas de visibilidad (research §4)

La misma condición se aplica en SQL a las listas y en memoria a la consulta por identificador.
`E` = categorías con asignación activa de quien pregunta; `yo` = el integrante de la petición.

```text
Entrenamiento visible:
  PRESIDENTE o DIRECTIVO
  o algún EntrenamientoCategoria.CategoriaId en E
  o (JUGADOR y (yo.CategoriaId entre sus categorías o existe Asistencia(yo)))

Partido visible:
  Estado = JUGADO
  o PRESIDENTE
  o algún PartidoCategoria.CategoriaId en E
  o (JUGADOR y existe PartidoJugador(yo))
```
