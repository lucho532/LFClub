# Modelo de datos: fichas de personas

**Funcionalidad**: `008-fichas-de-personas` | **Fecha**: 2026-10-10

Esta funcionalidad **no crea tablas ni migraciones**. Todo se arma al consultar con las entidades de
la 001 a la 007. Las decisiones están en [research.md](research.md).

## Entidades existentes que se usan

| Entidad | Campos que usa la 008 | Cambio |
| --- | --- | --- |
| `UsuarioRol` | `Id`, `ClubId`, `UsuarioId`, `Rol`, `Nombres`, `Apellidos`, `TipoDocumento`, `NumeroDocumento`, `FechaNacimiento`, `CreadoEn` | Solo documentación: nombres, apellidos y fecha de un PRESIDENTE, DIRECTIVO o ENTRENADOR los cambia él mismo, en todos sus clubes a la vez (§12.6) |
| `Usuario` | `Correo`, `Celular`, `VersionFoto` | Solo documentación de `Celular`: también lo cambia desde "Mi perfil" un PRESIDENTE, DIRECTIVO o ENTRENADOR |
| `FotoPerfil` | `Contenido`, `TipoContenido` | Solo documentación: la ven los integrantes de los clubes donde la cuenta es PRESIDENTE, DIRECTIVO o ENTRENADOR (§14.2 4.5.0) |
| `AsignacionEntrenadorCategoria` | `UsuarioRolId`, `CategoriaId`, `Activa`, `Equipos` | Ninguno |
| `EntrenadorEquipo`, `Equipo`, `Categoria` | `Activo`/`Activa`, `Nombre`, `Anio` | Ninguno |
| `FichaJugador` | `UltimoCambioPorUsuarioId`, `UltimoCambioPorNombre` | Ninguno |
| `UsuarioRol` (jugador) | `RetiradoPorUsuarioId`, `RetiradoPorNombre`, `AprobadoPorUsuarioId`, `AprobadoPorNombre`, `AgregadoDesdeUsuarioRolId` | Ninguno |

## Vistas calculadas

### Ficha básica (`FichaBasicaDto`)

Existe para un `UsuarioRol` del club consultado con `Rol` ∈ {PRESIDENTE, DIRECTIVO, ENTRENADOR} y
`EstadoIngreso = APROBADO`.

| Campo | Origen | Regla |
| --- | --- | --- |
| `usuarioRolId` | `UsuarioRol.Id` | |
| `nombres`, `apellidos` | `UsuarioRol` del club consultado | |
| `rol` | `UsuarioRol.Rol` | Único rol en ese club (§8) |
| `versionFoto` | `Usuario.VersionFoto` | 0 = sin foto → iniciales |
| `categorias[]` | asignaciones activas del integrante en categorías activas | Orden por `anio` |
| `categorias[].anio` | `Categoria.Anio` | |
| `categorias[].equipos[]` | `EntrenadorEquipo` → `Equipo` activo | Nombres, orden alfabético; vacía = la categoría en general |

Nunca lleva documento, fecha de nacimiento, correo, celular ni responsable (RF-002).

**Sección de categorías en pantalla** (RF-006): `categorias` vacía y `rol = ENTRENADOR` → "Todavía
no tiene categorías asignadas"; vacía y PRESIDENTE o DIRECTIVO → no se muestra.

### Enlace a una ficha (`EnlaceFichaDto`)

| Campo | Valores |
| --- | --- |
| `usuarioRolId` | integrante del club consultado |
| `tipo` | `JUGADOR` (ficha de la 005/007) o `BASICA` (ficha básica) |

Es `null` cuando quien mira no puede abrir la ficha o la persona ya no está en el club (RF-011).

**Reglas de resolución** (`ReglaEnlaceDeFicha`, dominio):

| Caso | Entrada | Resultado |
| --- | --- | --- |
| Autor copiado (retiro, aprobación, último cambio) | `UsuarioId` guardado + club | Integrante de esa cuenta en ese club con rol PRESIDENTE, DIRECTIVO o ENTRENADOR y aprobado → `BASICA`; si no hay → `null` |
| Persona aprobada (ingresos aprobados) | `UsuarioRol` actual | JUGADOR aprobado (activo o retirado) → `JUGADOR`; PRESIDENTE, DIRECTIVO, ENTRENADOR aprobado → `BASICA`; no existe → `null` |
| Hermano desde cuya ficha se agregó | `AgregadoDesdeUsuarioRolId` | JUGADOR aprobado → `JUGADOR`; otro caso → `null` |

### Datos propios (`DatosPropiosDto`)

Solo para una cuenta con al menos un integrante PRESIDENTE, DIRECTIVO o ENTRENADOR.

| Campo | Origen | ¿Lo cambia la persona? |
| --- | --- | --- |
| `nombres`, `apellidos` | el más antiguo de esos integrantes | Sí, en todos ellos |
| `fechaNacimiento` | ídem | Sí, en todos ellos |
| `tipoDocumento`, `numeroDocumento` | ídem | No (RF-019) |
| `correo` | `Usuario.Correo` | No (RF-018) |
| `celular` | `Usuario.Celular` | Sí |

**Validación** (las del registro, §12.1): nombres y apellidos obligatorios, hasta 80 caracteres,
sin espacios sobrantes; fecha obligatoria y no futura; celular obligatorio, hasta 20 caracteres.
Si algo falla, `400 datos_invalidos` con el campo y el motivo, y no cambia nada.

**Transición**: un único `PUT` en una transacción. No se tocan los integrantes JUGADOR de la misma
cuenta ni los nombres ya copiados en registros históricos (§13).

## Cambios en DTOs existentes

| DTO | Campo nuevo | Tipo |
| --- | --- | --- |
| `EntrenadorParaFamiliaDto` | `usuarioRolId`, `versionFoto` | `Guid`, `int` |
| `UltimoCambioDto` | `autorFicha` | `EnlaceFichaDto?` |
| `JugadorRetiradoDto` | `retiradoPorFicha` | `EnlaceFichaDto?` |
| `IngresoAprobadoDto` | `ficha`, `aprobadoPorFicha` | `EnlaceFichaDto?` |
| `IngresoEnEsperaDto` | `hermanoDeFicha` | `EnlaceFichaDto?` |

Todos son añadidos: ningún campo existente cambia de nombre ni de tipo.
