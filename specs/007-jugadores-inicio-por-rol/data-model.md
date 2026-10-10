# Modelo de datos: Jugadores, inicio por rol e identidad del club por el presidente

**Funcionalidad**: `007-jugadores-inicio-por-rol` | **Fecha**: 2026-10-10

Una entidad nueva, `FotoJugador`, y su migración, `FotosDeJugadores`. Nada más cambia de
estructura: la lista, el resumen y los inicios se arman con datos que ya existen, y la identidad
del club ya está en `Club` y `EscudoClub` (spec 001).

## Cómo se corresponden las entidades de la spec

| Entidad de la spec | Dónde vive |
| --- | --- |
| Jugador en la lista | `UsuarioRol` con `EsJugadorDelClub`, su `Categoria` y sus `JugadorEquipo` activos, más la fecha de su foto. Se arma al consultar; no se guarda |
| Foto del jugador | **Nueva**: `FotoJugador` |
| Resumen del club | Cuatro cuentas sobre `UsuarioRol`, `Equipo`, `Categoria` y `AsignacionEntrenadorCategoria`. Se calcula al consultar; no se guarda |
| Identidad del club | `Club.ColorPrincipal`, `Club.ColorAcento`, `Club.VersionEscudo` y `EscudoClub`, sin cambios |

## FotoJugador (nueva)

Imagen propia de un jugador en un club (constitución §14.2). Es independiente de `FotoPerfil`, que
sigue siendo de la cuenta y privada.

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `UsuarioRolId` | `Guid` | Clave primaria y foránea a `UsuarioRol`, borrado en cascada. Una foto por jugador y club |
| `ClubId` | `Guid` | Foránea a `Club`, borrado en cascada. Siempre el del jugador. Implementa `IPerteneceAClub`: la tabla queda bajo el filtro de aislamiento |
| `Contenido` | `byte[]` (`bytea`) | Obligatorio. Máximo 1 MB |
| `TipoContenido` | `string` (máx. 20) | Obligatorio. `image/png`, `image/jpeg` o `image/webp`, según la firma del archivo, nunca la extensión |
| `GuardadaEn` | `DateTime` (UTC) | Obligatorio. Se reescribe en cada carga. Es lo que la API entrega como `fotoActualizadaEn` |

Tabla: `FotosJugador`. Sin índices adicionales: se lee siempre por la clave primaria o por una lista
de claves primarias.

### Reglas de validación

- La imagen la valida `ValidadorImagen` (001): PNG, JPEG o WebP reconocidos por su firma binaria y
  de hasta 1 MB. Rechazos: `400 foto_no_es_imagen`, `400 foto_demasiado_grande`. Un rechazo no
  cambia la fila existente.
- Solo puede tener foto un jugador aprobado y activo: el servicio niega la carga a un retirado
  (`409 jugador_retirado`) y la autorización no deja llegar a quien está en espera.
- Cargar y quitar actualizan en la misma transacción el último cambio de la ficha
  (`FichaJugador.UltimoCambioEn`, `UltimoCambioPorUsuarioId`, `UltimoCambioPorNombre`), creando la
  fila de `FichaJugador` si todavía no existía, como ya hace la 005.

### Ciclo de vida

```text
(sin fila) ──cargar──▶ con foto ──cambiar──▶ con foto (GuardadaEn nueva)
     ▲                    │
     └──── quitar ────────┤
     └──── retirar ───────┤  (PRESIDENTE, familia o aprobación de un hermano en otro club)
     └──── cambio de rol ─┘  (cuando exista; §12.2)
```

- No hay estado `Activo` ni historial: la foto se borra físicamente. Es la excepción expresa de
  §14.2 a la conservación de los datos del retirado.
- Al reincorporar al jugador no se recupera nada: vuelve sin foto.

## Qué cambia en las entidades que ya existen

| Entidad | Cambio |
| --- | --- |
| `UsuarioRol` | Ninguno de estructura. Su documentación añade que de él cuelga la foto y que se borra al retirarlo |
| `FichaJugador` | Ninguno de estructura. Su documentación añade que cargar o quitar la foto sella el último cambio, aunque la foto vive en otra tabla |
| `Club`, `EscudoClub` | Ninguno de estructura. Su documentación deja de decir que solo el DESARROLLADOR cambia el escudo y los colores |

## Qué hace cada operación sobre las tablas

| Operación | Tablas que escribe |
| --- | --- |
| Cargar o cambiar la foto | `FotosJugador` (alta o reemplazo), `FichasJugador` (sello) |
| Quitar la foto | `FotosJugador` (baja, si había), `FichasJugador` (sello) |
| Retirar a un jugador | Lo de la 003 (`UsuariosRol`, `JugadoresEquipo`) más la baja de su fila en `FotosJugador` |
| Aprobar un hermano que retira en otro club (006, T027) | Lo de la 006 más la baja de las filas de `FotosJugador` de los jugadores que retira |
| Colores por el PRESIDENTE | `Clubes.ColorPrincipal`, `Clubes.ColorAcento` |
| Escudo por el PRESIDENTE | `EscudosClub` (alta o reemplazo), `Clubes.VersionEscudo` |

## Cálculos que no se guardan

### Lista de jugadores

- Entra cada `UsuarioRol` con `Rol = JUGADOR`, `EstadoIngreso = APROBADO` y `Activo = true`.
- ENTRENADOR: solo si su `CategoriaId` es una categoría activa con una asignación activa de quien
  pregunta. PRESIDENTE y DIRECTIVO: todos, y los que no tienen categoría forman el grupo "Sin
  categoría".
- Grupos por `Categoria.Anio` ascendente; "Sin categoría" al final; jugadores por apellidos y
  nombres; equipos activos por nombre.

### Resumen del club

| Cifra | Cuenta |
| --- | --- |
| Jugadores | `UsuarioRol` con `EsJugadorDelClub` |
| Equipos | `Equipo` con `Activo` cuya `Categoria` está activa |
| Entrenadores | `UsuarioRolId` distintos de la unión de: asignaciones activas en categorías activas, e integrantes aprobados y activos con `Rol = ENTRENADOR` |
| Categorías | `Categoria` con `Activa` |

Todas se calculan en el club de la petición, con el filtro de aislamiento activo.

## Borrados en cascada

- `Club` → `FotoJugador` (eliminar un club, §7.4).
- `UsuarioRol` → `FotoJugador` (rechazar a un hermano en espera, que no puede tenerla; o cualquier
  borrado futuro del integrante).

## Migración

`FotosDeJugadores`: crea `FotosJugador` con sus dos foráneas en cascada. No toca filas existentes:
todos los jugadores empiezan sin foto y se muestran con sus iniciales.
