# Modelo de datos: eliminar mi cuenta

**Rama**: `014-eliminar-cuenta` | **Plan**: [plan.md](plan.md) | **Investigación**: [research.md](research.md)

**No hay entidades, columnas, índices ni migración nuevos.** Esta funcionalidad solo decide qué hace
cada tabla existente cuando se elimina una cuenta.

## Qué pasa con cada tabla

| Tabla | Relación con la cuenta | Qué ocurre | Cómo |
| --- | --- | --- | --- |
| `Usuarios` | la cuenta | Se borra | Último paso |
| `UsuariosRol` | integrante (rol y jugador) | Se borran todos, en todos los clubes | Paso 5 |
| `FichasJugador`, `DocumentosJugador`, `FotosJugador` | del jugador | Se borran | Cascada |
| `CartasJugador` | del jugador | Se borra (dorsal, posición, estadísticas) | Cascada |
| `JugadoresEquipo`, `EntrenamientosJugador`, `RespuestasEntrenamiento` | del jugador | Se borran | Cascada |
| `ComunicadosDestinatario`, `AvisosDeActividadDestinatario` | de la cuenta y del jugador | Se borran | Cascada |
| `AsignacionesEntrenadorCategoria`, `EntrenadoresEquipo` | del entrenador | Se borran | Cascada |
| `FotosPerfil`, `SolicitudesRecuperacion` | de la cuenta | Se borran | Cascada |
| `PartidosJugador` | del jugador | Se borran (convocatoria y goles) | Borrado explícito (`NoAction`) |
| `AsistenciasEntrenamiento` | del jugador | Se borran | Borrado explícito (`NoAction`) |
| `Partidos` | marcador y autoría | **Se conservan**; `GolesClub` y `GolesRival` no cambian; la autoría pasa a "Persona eliminada" | Actualización y `SetNull` |
| `Comunicados` | autoría | **Se conservan**; autor y quien lo retiró pasan a "Persona eliminada" | Actualización y `SetNull` |
| `Entrenamientos` | quién lo creó | Se conservan; el vínculo pasa a nulo | `SetNull` |
| `Invitaciones` | quién la envió | Las pendientes pasan a anuladas; sin vínculo | Actualización |
| Pagos (`§16`) | del jugador | **No existen todavía.** Se conservarán con nombre completo y documento copiados; su clave foránea será `SetNull` | Nota 4 del plan |

## Columnas con el nombre copiado que se reescriben

Reciben `"Persona eliminada"` (`NombresDeAutoria.PersonaEliminada`) donde la cuenta es la autora:

| Tabla | Columna | Se busca por |
| --- | --- | --- |
| `Comunicados` | `PublicadoPorNombre` | `PublicadoPorUsuarioRolId` en sus integrantes |
| `Comunicados` | `RetiradoPorNombre` | `RetiradoPorUsuarioRolId` en sus integrantes |
| `Partidos` | `PublicadoPorNombre` | `PublicadoPorUsuarioId` |
| `Partidos` | `ResultadoCambiadoPorNombre` | `ResultadoCambiadoPorUsuarioId` |
| `PartidosJugador` | `IndicadoPorNombre` | `IndicadoPorUsuarioId` (en las filas de otros jugadores) |
| `CartasJugador` | `EstadisticasCambiadasPorNombre` | `EstadisticasCambiadasPorUsuarioId` |
| `CartasJugador` | `DorsalCambiadoPorNombre` | `DorsalCambiadoPorUsuarioId` |
| `FichasJugador` | `UltimoCambioPorNombre` (obligatoria) | `UltimoCambioPorUsuarioId` |
| `UsuariosRol` | `AprobadoPorNombre` | `AprobadoPorUsuarioId` |
| `UsuariosRol` | `RetiradoPorNombre` | `RetiradoPorUsuarioId` |

Quedan sin tocar las filas que se borran en el mismo paso.

## Reglas

- **Presidente único**: la cuenta no se elimina si en algún club donde es PRESIDENTE hay un solo
  PRESIDENTE aprobado (`ReglaEliminacionDeCuenta`).
- **Cuenta DESARROLLADOR**: `EsDesarrollador` → 404, en la lectura y en la escritura.
- **Contraseña**: debe coincidir con `Usuario.ContrasenaHash`; el fallo suma `IntentosFallidos`.
- **Atomicidad**: una única transacción; si falla un paso no queda nada borrado.
- **Efectos observables**: correo y documentos libres (las filas ya no existen); tokens inválidos
  (`ValidadorSesion` no encuentra la cuenta).

## Guardián de esquema

Prueba de integración sobre el modelo de EF: para cada clave foránea cuyo principal sea `Usuario` o
`UsuarioRol`, el comportamiento al borrar debe ser `Cascade` o `SetNull`, o `NoAction` en la lista
explícita `{PartidoJugador.UsuarioRolId, AsistenciaEntrenamiento.UsuarioRolId}` que el repositorio
borra a mano. Cualquier otra combinación falla y obliga a decidir (en particular, la futura tabla de
pagos de RF-007).

## DTOs

**`ResumenEliminacionCuentaDto`** (respuesta de `GET /api/cuenta/eliminacion`)

| Campo | Tipo | Notas |
| --- | --- | --- |
| `puedeEliminar` | bool | Falso si hay clubes bloqueantes |
| `jugadores` | lista de `JugadorQueSeEliminaDto` | Todos los de la cuenta, de cualquier club y estado |
| `clubesQueLoImpiden` | lista de `ClubQueImpideDto` | Clubes donde es presidente único |

**`JugadorQueSeEliminaDto`**: `nombreCompleto`, `clubNombre`, `retirado` (bool, solo para mostrarlo
en texto) y `enEspera` (bool). Sin documento ni ningún otro dato de la ficha.

**`ClubQueImpideDto`**: `clubId` y `clubNombre`.

**`EliminarCuentaDto`** (cuerpo de `POST /api/cuenta/eliminacion`): `contrasena` (texto,
obligatorio). Respuesta: 204 sin cuerpo.

## Códigos de error nuevos

| Estado | Código | Cuándo |
| --- | --- | --- |
| 400 | `contrasena_incorrecta` | La contraseña no coincide |
| 403 | `cuenta_bloqueada` | La cuenta ya está bloqueada por intentos |
| 409 | `presidente_unico` | Es el único PRESIDENTE de algún club; lleva `clubes` |
