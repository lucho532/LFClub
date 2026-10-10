# Modelo de datos: Agregar un hermano y elegir el jugador

**Funcionalidad**: `006-agregar-hermano` | **Fecha**: 2026-10-09

Esta funcionalidad **no crea tablas**. Añade una columna a `UsuariosRol`, con una migración
(`HermanosDeLaCuenta`), y cambia una suposición: una cuenta puede tener varios integrantes en el
mismo club. Los modelos anteriores están en la [003](../003-categorias-club/data-model.md), la
[004](../004-invitacion-con-rol/data-model.md) y la [005](../005-ficha-jugador/data-model.md); los
motivos de cada decisión, en [research.md](research.md).

## Cómo se corresponden las entidades de la spec

| Entidad de la spec | Dónde vive | Novedad |
| --- | --- | --- |
| Cuenta | `Usuario` | Sin cambios: correo, contraseña, celular y responsable |
| Jugador | `UsuarioRol` con rol JUGADOR | Puede haber varios con el mismo `UsuarioId` y `ClubId` |
| Estado de ingreso | `UsuarioRol.EstadoIngreso` | Sin cambios: `EN_ESPERA` vuelve a usarse |
| Jugador de origen | **`UsuarioRol.AgregadoDesdeUsuarioRolId`** | Columna nueva |
| Jugador elegido | No se guarda | Cabecera `X-Jugador-Elegido` en cada petición |
| Sesión limitada a un jugador | No se guarda | Reclamación `jugadores` del token |

## UsuarioRol (cambia)

| Campo | Tipo | Reglas |
| --- | --- | --- |
| AgregadoDesdeUsuarioRolId | Guid, opcional | **Nuevo**. Foránea a `UsuarioRol` (la misma tabla), borrado a `NULL`. Solo lo tiene el hermano agregado desde una ficha; `NULL` en quien entró con invitación |

Sin índice nuevo: solo se lee junto con la sala de espera, que ya filtra por
`ClubId, EstadoIngreso`.

**Índices que ya existen y sostienen esta funcionalidad**:

- `ClubId, NumeroDocumento`, único (`IndicesUnicos.DocumentoEnClub`): impide un hermano con un
  documento repetido en el club, en cualquier estado (RF-005).
- `ClubId, UsuarioId`, **no único**: sirve para leer los integrantes de una cuenta en un club. Se
  deja como está.

### Cómo nace un hermano

| Campo | Valor |
| --- | --- |
| UsuarioId, ClubId | Los del jugador de origen |
| Rol | `JUGADOR` |
| EstadoIngreso | `EN_ESPERA` |
| Nombres, Apellidos, TipoDocumento, NumeroDocumento, FechaNacimiento | Los que escribe la familia, normalizados como en el registro |
| AgregadoDesdeUsuarioRolId | El jugador de origen |
| CategoriaId | `NULL` |
| Activo | `true` |
| AprobadoEn, AprobadoPor…, RolDeIngreso | `NULL` hasta que se apruebe |
| CreadoEn | El momento en que se agregó |

No se crea `FichaJugador` ni `DocumentoJugador`: la ficha no tiene fila hasta su primer cambio
(005).

### Reglas de validación

| Regla | Requisito | Respuesta |
| --- | --- | --- |
| Nombres, apellidos, tipo y número de documento y fecha de nacimiento, obligatorios y con el formato del registro | RF-002, RF-006 | `400 datos_invalidos` |
| Fecha de nacimiento no futura | RF-006 | `400 datos_invalidos` |
| Hermano menor y cuenta sin responsable: `nombreResponsable` obligatorio | RF-004 | `400 datos_invalidos` |
| Documento que ya tiene otro integrante del club | RF-005 | `409 documento_repetido_en_club` |
| Documento que, con otra cuenta, tiene en cualquier club un integrante que no es JUGADOR | RF-039 | `409 documento_en_otra_cuenta` |
| Documento que, con otra cuenta, solo tienen jugadores de otros clubes | RF-034 | `201`; `retiraDeOtroClub` dice si alguno está aprobado y activo |
| Documento de un jugador en espera de la misma cuenta en el club (supuesto 2) | RF-009 | `200` con el que ya existe |

### Transiciones del estado de ingreso

```text
(no existe) ──agregar hermano──▶ EN_ESPERA ──aprobar──▶ APROBADO
                                     │
                                     └──rechazar──▶ (no existe)
```

- **Aprobar**: `APROBADO`, rol JUGADOR, `AprobadoEn` y quién lo aprobó; se ubica en la categoría
  activa de su año o queda sin categoría. Desde el incremento, además retira a los jugadores
  activos de otra cuenta con su mismo documento (ver "Documento compartido entre cuentas").
- **Rechazar**: se borra la fila. No queda estado ni registro. La cuenta y sus demás integrantes
  no se tocan. Las invitaciones del club a ese correo solo se borran si a la cuenta no le queda
  ningún integrante en el club (cambio respecto de la 002).
- Retirar y reincorporar siguen siendo de cada jugador por separado (`Activo`).

## Documento compartido entre cuentas (incremento, RF-034 a RF-040)

No añade tablas, columnas, índices ni migración. Es una situación de los datos que hasta ahora no
podía darse: el mismo `NumeroDocumento` en filas de `UsuarioRol` con `UsuarioId` distintos, siempre
en clubes distintos (el índice `ClubId, NumeroDocumento` sigue siendo único). Solo nace al agregar
un hermano.

| Pregunta | Se responde con | Requisito |
| --- | --- | --- |
| ¿Se admite como hermano? | Ningún integrante de otra cuenta con ese número tiene un rol distinto de JUGADOR | RF-034, RF-039 |
| ¿Aprobarlo retira a alguien? (`retiraDeOtroClub`) | Algún integrante de otra cuenta con ese número es JUGADOR, `APROBADO` y `Activo` | RF-034, RF-038 |
| ¿Qué cuenta abre ese documento? | La del integrante aprobado y activo; si no hay, la del que espera; si no, la del retirado. En un empate, el más reciente | RF-040 |

Ninguna de las tres respuestas se guarda: se calculan al agregar, al abrir la sala de espera, al
aprobar y al iniciar sesión.

### Qué cambia en el jugador de la otra cuenta al aprobar

| Campo | Valor |
| --- | --- |
| Activo | `false` |
| CategoriaId | `NULL` |
| Equipos | Ninguno: se borran sus filas de `JugadorEquipo` |
| RetiradoEn | El momento de la aprobación |
| RetiradoPorUsuarioId, RetiradoPorNombre | `NULL`: quien aprueba es de otro club (supuesto 6) |

Es el retiro de la 003 con el autor vacío. No cambian su `EstadoIngreso`, su identidad, su
`FichaJugador`, sus `DocumentoJugador` ni su `Usuario`. Se aplica a todas las filas que cumplan la
segunda pregunta de la tabla, en cualquier club, en la misma transacción que la aprobación.

```text
Otro club:   APROBADO y activo ──aprobar al hermano aquí──▶ APROBADO y retirado
             APROBADO y activo ──agregar, esperar o rechazar aquí──▶ sin cambios
             retirado o EN_ESPERA ──cualquier cosa aquí──▶ sin cambios
```

## Retiro por la familia y reincorporación (incremento, RF-041 a RF-043)

Tampoco añade tablas, columnas ni migración. Usa las columnas del retiro de la 003.

| Pregunta | Se responde con | Requisito |
| --- | --- | --- |
| ¿Quién puede retirar a este jugador? | El PRESIDENTE de su club, a cualquier jugador aprobado; la cuenta del jugador, solo al integrante de la petición | RF-042, RF-043 |
| ¿Se puede reincorporar? | Ningún integrante de **otra** cuenta con ese número está `APROBADO` y `Activo`, en cualquier club y con cualquier rol | RF-041 |

Las dos se calculan en el momento de la operación y no se guardan.

### Qué guarda el retiro según quién lo hace

| Campo | Lo retira el PRESIDENTE | Lo retira su familia | Baja al aprobar un hermano en otro club |
| --- | --- | --- | --- |
| Activo | `false` | `false` | `false` |
| CategoriaId | `NULL` | `NULL` | `NULL` |
| Equipos | Ninguno | Ninguno | Ninguno |
| RetiradoEn | Ahora | Ahora | El momento de la aprobación |
| RetiradoPorUsuarioId | Cuenta del PRESIDENTE | Cuenta del jugador | `NULL` |
| RetiradoPorNombre | Nombre del PRESIDENTE | Nombre del propio jugador (supuesto 11) | `NULL` |

En los tres casos no cambian su `EstadoIngreso`, su identidad, su ficha, sus documentos, su
`Usuario` ni sus hermanos.

### Transiciones de un jugador aprobado

```text
activo ──retira el PRESIDENTE o su familia──▶ retirado
activo ──se aprueba en otro club un hermano de otra cuenta con su número──▶ retirado
retirado ──reincorpora el PRESIDENTE, sin su número activo en otra cuenta──▶ activo
retirado ──reincorpora el PRESIDENTE, con su número activo en otra cuenta──▶ retirado (409)
```

La familia nunca hace la tercera transición. Con estas reglas, un número queda activo como mucho
en una cuenta (CE-013): la reincorporación lo comprueba con los clubes bloqueados, y la
aprobación con baja retira a todos los de otra cuenta en la misma transacción.

## Usuario (sin cambios de estructura)

`NombreResponsable` puede escribirse al agregar un hermano menor cuando la cuenta no lo tenía
(RF-004). Es el mismo dato de la cuenta que ya cambia la ficha (005, RF-039).

## Lo que no se guarda

- **El jugador elegido**: viaja en la cabecera `X-Jugador-Elegido` y solo vale si es un
  integrante de la cuenta de la sesión en el club de la ruta.
- **La limitación de la sesión con documento**: viaja en la reclamación `jugadores` del token,
  con los identificadores de los integrantes de la cuenta que tenían ese número al entrar.

## Borrados en cascada

No cambian. Rechazar a un hermano, eliminar la cuenta o eliminar el club borran sus filas de
`UsuarioRol` y, con ellas, su ficha, sus documentos y sus equipos. Borrar al jugador de origen
deja `AgregadoDesdeUsuarioRolId` en `NULL` en sus hermanos y no los borra.
