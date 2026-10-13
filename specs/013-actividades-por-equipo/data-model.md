# Modelo de datos: actividades por equipo y jugadores añadidos

**Funcionalidad**: `013-actividades-por-equipo` | **Fecha**: 2026-10-12

Dos tablas nuevas y una migración, `ActividadesPorEquipo`. Ambas entidades implementan
`IPerteneceAClub`: el filtro global de club de la 001 las limita al club de la petición (§7.1). Las
decisiones están en [research.md](research.md) y el contrato en [contracts/api.yaml](contracts/api.yaml).

Convenciones de las specs anteriores: `ClubId` obligatorio con foránea a `Clubes`, nombres de
columnas en español, enumeraciones guardadas como texto.

## Enumeraciones nuevas (`LFClub.Dominio/Enumeraciones`)

| Enumeración | Valores | Guardada como |
| --- | --- | --- |
| `OrigenDeJugadorEnEntrenamiento` | `ANADIDO`, `EQUIPO_DESACTIVADO` | texto |

## EntrenamientoEquipo (`EntrenamientosEquipo`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `EntrenamientoId` | uuid | Clave compuesta; foránea, en cascada |
| `EquipoId` | uuid | Clave compuesta; foránea `Restrict` |
| `ClubId` | uuid | Obligatorio |

- Al guardar: cada equipo existe en el club, está activo y es de una de las categorías del
  entrenamiento, que están activas (research §10). Marcar el equipo como `Usado` (research §6).
- Índice por `EquipoId`: para encontrar los entrenamientos de un equipo al desactivarlo.
- Un entrenamiento sin filas es de toda la categoría. Una fila a un equipo ya desactivado se
  conserva: el entrenamiento sigue "con equipos".

## EntrenamientoJugador (`EntrenamientosJugador`)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `EntrenamientoId` | uuid | Clave compuesta; foránea, en cascada |
| `UsuarioRolId` | uuid | Clave compuesta; foránea al jugador, en cascada |
| `ClubId` | uuid | Obligatorio |
| `Origen` | `OrigenDeJugadorEnEntrenamiento` | Obligatorio |
| `EquipoDeOrigenId` | uuid | Solo con `EQUIPO_DESACTIVADO`; foránea al equipo, `SetNull` |
| `AnadidoEn` | timestamptz | UTC |

- `ANADIDO`: lo suma quien dirige. Debe ser un jugador activo, aprobado, no retirado y con
  categoría del club. Un jugador que ya es de la audiencia natural se acepta y no se cuenta dos veces.
- `EQUIPO_DESACTIVADO`: lo escribe el sistema al desactivar un equipo (research §6). No lo escribe
  ni lo quita ninguna persona.
- Restricción de comprobación: si `Origen = ANADIDO`, `EquipoDeOrigenId` es nulo. No se exige lo
  contrario, porque la foránea es `SetNull` y un equipo puede borrarse después.
- Quitar a un `ANADIDO` borra la fila, sin rastro (RF-017). Si deja de estar en la audiencia y no
  tiene asistencia registrada, se borra también su respuesta.
- Índice por `UsuarioRolId` para la visibilidad del jugador.
- Borrar al jugador (eliminación de cuenta, §12.4) la elimina en cascada.

## Cambios en entidades existentes

| Entidad | Cambio |
| --- | --- |
| `Entrenamiento` | Dos listas: `Equipos` (`EntrenamientoEquipo`) y `Jugadores` (`EntrenamientoJugador`); documentación |
| `Equipo` | Sin columnas nuevas; se marca `Usado` al dirigirle un entrenamiento |
| `Partido`, `PartidoJugador` | Ninguno. El añadido es el convocado con categoría copiada ajena (research §2) |
| `JugadorEquipo` | Ninguno; `RepositorioEquipos.DesactivarAsync` copia antes de borrarla |

## Cálculos

**Audiencia de un entrenamiento** (jugadores con `Rol = JUGADOR`, `EstadoIngreso = APROBADO`,
`Activo`, sin retirar y con categoría):

```text
audiencia natural =
    si el entrenamiento tiene filas en EntrenamientosEquipo:
        jugadores con fila en JugadoresEquipo de alguno de esos equipos
    si no:
        jugadores cuya categoría actual es una de las del entrenamiento
  ∪ filas EQUIPO_DESACTIVADO de EntrenamientosJugador
audiencia total = audiencia natural ∪ filas ANADIDO
```

**Ve el entrenamiento** (jugador) = audiencia total ∪ tiene asistencia registrada. **Responde** =
audiencia total, entrenamiento `PROGRAMADO` y que no ha empezado.

**Planilla**: audiencia total restringida a lo que quien pregunta puede marcar (los de sus
categorías; los añadidos, si dirige) más los que ya tienen asistencia en una categoría marcable.

**Añadido de un partido** = convocado cuya categoría copiada no es una de las del partido.

## Consultas de visibilidad

`RepositorioEntrenamientos.Visibles(quien)` traduce `ReglaVisibilidadDeEntrenamiento.LaVe` a SQL
con `EXISTS` sobre `EntrenamientosEquipo`, `EntrenamientosJugador` y `JugadoresEquipo` (la forma
está en research §3). `ListarVisibles`, `ProximosVisibles`, el calendario, los contadores del
inicio y la resolución de destinatarios usan la misma condición.

## DTOs que cambian (`LFClub.Aplicacion/DTOs`)

| DTO | Cambio |
| --- | --- |
| `GuardarEntrenamientoDto` | `EquipoIds`, `JugadorAnadidoIds` opcionales |
| `EntrenamientoDto` / `EntrenamientoResumenDto` | `Equipos` (nombres y categoría); `Anadidos` solo para quien dirige |
| `PermisosDeActividadDto` | `Dirige` |
| `PlanillaEntrenamientoDto.Jugadores[]` | `Anadido` |
| `PlanillaPartidoDto` | `Equipos` de las categorías; `EquipoIds` y `Anadido` en convocados y convocables |
| Nuevos | `AudienciaDeEntrenamientoDto`, `JugadorSumableDto`, `EquipoDeActividadDto` |

Ningún DTO lleva la lista de convocados ni de añadidos a una cuenta de jugador (RF-018, RF-024).

## Estados y transiciones

Sin estados nuevos. Cambiar la audiencia exige un entrenamiento `PROGRAMADO`; en un partido,
convocar exige `PROXIMO` (`ReglaTransicionPartido.PuedeConvocar`). Cancelar y reactivar no tocan la
audiencia.
