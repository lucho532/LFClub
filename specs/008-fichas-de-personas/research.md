# Investigación y decisiones técnicas: fichas de personas

**Funcionalidad**: `008-fichas-de-personas` | **Fecha**: 2026-10-10

La spec no deja ninguna `[NECESITA ACLARACIÓN]`. Este documento recoge cómo se construye sobre el
código de la 001 a la 007 y los detalles que la spec no fija. Los supuestos marcados con
**(por confirmar)** siguen la lectura más conservadora y se confirman al revisar las pantallas.

## 1. La ficha básica no es una tabla nueva

- **Decisión**: la ficha básica se arma al consultar con lo que ya existe: `UsuarioRol` (nombres,
  apellidos, rol), `Usuario.VersionFoto` y las `AsignacionEntrenadorCategoria` activas de
  categorías activas, con sus `EntrenadorEquipo` de equipos activos. Un servicio nuevo,
  `ServicioFichaBasica`, y un repositorio de lectura, `RepositorioFichaBasica`.
- **Por qué**: la spec la define como "vista" (Entidades clave) y §19 pide no crear tablas sin
  necesidad.
- **Alternativas descartadas**: guardar una copia de los datos públicos de cada persona (se
  desincroniza con el cambio de rol, de asignación o de foto).

## 2. Ruta y respuesta de la ficha básica

- **Decisión**: `GET /api/clubes/{clubId}/personas/{usuarioRolId}`, con `[IntegranteDelClub]` sin
  roles exigidos: la abre cualquier rol del club, también la cuenta de un jugador (RF-003). Responde
  `404 no_encontrado` si el integrante no es de ese club (lo da el filtro de club) o si su rol es
  JUGADOR: un jugador no tiene ficha básica y el 404 no revela si existe (RF-004, "Fuera de
  alcance"). El DESARROLLADOR no tiene integrante en el club y recibe 404, como en todas las rutas
  de club.
- **Por qué**: "personas" no choca con `jugadores`, que ya tiene su ficha, y deja la regla de
  acceso en el atributo que ya existe: un jugador en espera o retirado, y cualquiera en un club
  suspendido salvo el PRESIDENTE, recibe los 403 de siempre.
- **Alternativas descartadas**: colgarla de `/entrenadores/{id}` (también es de presidentes y
  directivos que no entrenan) o devolverla dentro de otras respuestas (cada pantalla tendría que
  repetirla).

## 3. Qué lleva la ficha básica

- **Decisión**: `FichaBasicaDto(UsuarioRolId, Nombres, Apellidos, Rol, VersionFoto, Categorias)`,
  con `Categorias` como lista de `CategoriaDeFichaBasicaDto(Anio, Equipos)` (nombres de equipo).
  Sin identificadores de categoría ni de equipo: la familia no puede abrir categorías y no los
  necesita. Orden: categorías por año, equipos por nombre.
- **Por qué**: RF-001 y RF-002 enumeran lo que se muestra; un tipo propio garantiza que ningún dato
  de la cuenta salga por error (§23).
- **Regla de la sección de categorías** (RF-006): la decide la pantalla con `Rol` y la lista vacía:
  ENTRENADOR sin categorías → "Todavía no tiene categorías asignadas"; PRESIDENTE o DIRECTIVO sin
  categorías → no se pinta la sección.

## 4. La foto de perfil ante el club

- **Decisión**: `GET /api/clubes/{clubId}/personas/{usuarioRolId}/foto`, con el mismo atributo y la
  misma condición que la ficha básica: el integrante debe ser de ese club y tener rol PRESIDENTE,
  DIRECTIVO o ENTRENADOR. Devuelve la `FotoPerfil` de su cuenta, con
  `Cache-Control: private, no-store` y `X-Content-Type-Options: nosniff`, como la foto del jugador.
  Sin foto, 404.
- **Por qué**: la foto queda atada al integrante del club desde el que se pide, así que una cuenta
  que es ENTRENADOR en un club y JUGADOR en otro solo expone su foto en el primero (caso límite de
  la spec, §14.2). `/api/cuenta/foto` no cambia: sigue siendo solo la de la sesión.
- **Alternativas descartadas**: abrir `/api/cuenta/foto/{usuarioId}` (identificador de cuenta,
  fuera del filtro de club, viola §7.1).
- `IRepositorioFotosPerfil` gana `ObtenerDeIntegranteAsync(usuarioRolId)`, que resuelve la cuenta
  a través del integrante del club (bajo el filtro de club) y solo si su rol no es JUGADOR. La
  documentación de `FotoPerfil`, `IRepositorioFotosPerfil` y `ControladorFotoPerfil` deja de decir
  "solo la ve su dueño" y remite a §14.2 4.5.0.

## 5. Miniaturas en el inicio de la familia

- **Decisión**: `EntrenadorParaFamiliaDto` pasa a llevar `UsuarioRolId` y `VersionFoto`, además de
  nombres, apellidos y equipos. La tarjeta "Mi categoría" pinta la miniatura y el nombre como
  enlace. La foto se pide con la ruta de §4.
- **Por qué**: RF-014 y la historia 3. El 003 (RF-035) prohibía el identificador porque la familia
  solo veía el nombre; esa parte queda sin efecto (spec, "Deja sin efecto").
- **Caché**: el módulo `fotosJugador.ts` de la 007 se generaliza en `compartido/fotos/fotosEnMemoria.ts`
  con una llave `ruta@sello`; los jugadores usan como sello `fotoActualizadaEn` y las personas
  `versionFoto`. Se vacía al cerrar sesión como hoy. `FotoJugador` y la nueva `FotoPersona` usan el
  mismo módulo.
- **Alternativas descartadas**: un segundo módulo de caché casi idéntico (duplica 150 líneas).

## 6. Qué nombres pasan a ser enlaces, y cómo sabe la pantalla a dónde llevan

Inventario de las pantallas del club que muestran nombres de personas (historia 2):

| Pantalla | Nombre | Hoy | Con la 008 | Dato que hace falta |
| --- | --- | --- | --- | --- |
| Inicio de la familia, "Mi categoría" | entrenadores | texto | ficha básica + miniatura | `usuarioRolId`, `versionFoto` (nuevo, §5) |
| Inicio de la familia, "Mi jugador" | su jugador | botón "Abrir mi ficha" | el nombre también abre "Mi ficha" | ya existe |
| Inicio del entrenador | categorías | enlace a categoría | sin cambio (no hay nombres) | — |
| Lista "Jugadores" | jugadores | enlace | sin cambio; volver genérico (§7) | — |
| Categorías (lista) | entrenadores de cada categoría | texto | ficha básica | `usuarioRolId` ya está |
| Detalle de categoría, jugadores | jugadores | enlace | sin cambio | — |
| Detalle de categoría, entrenadores | entrenadores | texto | ficha básica | ya está |
| Sin categoría | jugadores | enlace | sin cambio | — |
| Retirados | jugador | enlace | sin cambio | — |
| Retirados | "retirado por" | texto | ficha básica si sigue en el club | `retiradoPorFicha` (nuevo) |
| Ingresos aprobados | persona aprobada | texto | su ficha actual | `ficha` (nuevo) |
| Ingresos aprobados | "aprobado por" | texto | ficha básica si sigue en el club | `aprobadoPorFicha` (nuevo) |
| Sala de espera | persona en espera | tarjeta con sus datos | texto (ver §8) | — |
| Sala de espera | "hermano de" | texto | ficha del hermano | `hermanoDeFicha` (nuevo) |
| Ficha del jugador | "último cambio por" | texto o "la cuenta del jugador" | ficha básica si sigue en el club | `autorFicha` (nuevo) |
| Barra superior | nombre propio | menú con "Mi perfil" | sin cambio (historia 4) | — |

- **Decisión**: un DTO común, `EnlaceFichaDto(UsuarioRolId, Tipo)`, con
  `TipoFicha = JUGADOR | BASICA`, que la API pone solo cuando quien mira puede abrir esa ficha, y
  `null` en caso contrario. Se usa en los cinco campos "nuevo" de la tabla. Donde el tipo es
  evidente por la lista (jugadores de una categoría, entrenadores de una categoría) la pantalla usa
  el `usuarioRolId` que ya llega, porque la lista misma ya está limitada a quien la puede ver.
- **Resolución de autores** (`retiradoPor`, `aprobadoPor`, `autor` del último cambio): con el
  `UsuarioId` guardado se busca un integrante **del mismo club** con rol PRESIDENTE, DIRECTIVO o
  ENTRENADOR; si existe, enlace `BASICA`; si no (salió del club, la cuenta se eliminó, o fue la
  familia), `null` y el nombre copiado se muestra como texto (RF-011, caso 2.5). El nombre que se
  muestra sigue siendo el copiado (§13); el enlace lleva a la persona de hoy.
- **Resolución de la persona aprobada**: según su integrante actual: JUGADOR aprobado (activo o
  retirado) → `JUGADOR`; PRESIDENTE, DIRECTIVO o ENTRENADOR → `BASICA`; si ya no existe, `null`.
  Esa lista solo la ve el PRESIDENTE, que abre cualquier ficha de su club.
- Un único componente de pantalla, `NombrePersona`, recibe el nombre y el enlace (o `null`) y pinta
  un `Link` o un texto normal (RF-011). Los nombres largos se cortan con `overflow-wrap: anywhere`
  (caso límite).
- **Por qué**: la API decide (§15); la pantalla no infiere permisos por el rol. RF-012 queda
  cubierto porque abrir la ficha sigue pasando por `AccesoAFicha` y por §2.
- **Alternativas descartadas**: que la pantalla calcule el enlace con el rol de quien mira (repite
  reglas del servidor) o una consulta extra por nombre (N+1).
- **Fuera**: el diálogo "Asignar entrenador" (es un selector, no una lista para consultar) y el
  panel del DESARROLLADOR (supuesto de la spec).

## 7. Volver a la pantalla de origen (RF-013)

- **Decisión**: todo enlace de `NombrePersona` pasa en el `state` de React Router
  `{ volver: { ruta, texto } }` con la ruta actual y el nombre de la pantalla. La ficha del jugador
  y la ficha básica usan un gancho `useVolver(predeterminado)` que lee ese estado y, sin él (por
  ejemplo, al abrir la dirección directamente), usa el predeterminado de hoy. El caso
  `desde: 'jugadores'` de la 007 pasa a este mecanismo.
- **Por qué**: el texto del enlace dice a dónde vuelve, como hoy "← Jugadores"; `navigate(-1)`
  sacaría de la aplicación si la ficha se abrió directamente.

## 8. Sala de espera: qué abre el nombre de quien espera

- **Hecho**: un jugador en espera no tiene ficha: la API responde 404 también al PRESIDENTE
  (`FichaAjenaPruebas.Solo_un_jugador_aprobado_tiene_ficha`, RF-004 de la 005). Lo que el
  PRESIDENTE revisa antes de decidir es la tarjeta de la sala de espera, con todos sus datos.
- **Decisión**: en la sala de espera, el nombre de quien espera se queda como texto (su tarjeta ya
  es "la ficha que hoy le permite revisar", escenario 2.7), y el nombre del hermano desde cuya ficha
  se agregó pasa a ser enlace a la ficha de ese hermano. Darle ficha al jugador en espera ampliaría
  lo que se ve de él, contra RF-012.
- **Confirmado** por el propietario el 2026-10-10; el escenario 2.7 de la spec ya lo dice así.

## 9. "Mi perfil": datos propios de PRESIDENTE, DIRECTIVO y ENTRENADOR

- **Hecho**: nombres, apellidos, tipo y número de documento y fecha de nacimiento viven en cada
  `UsuarioRol` (uno por club); el celular, en `Usuario`; el correo, en `Usuario`.
- **Decisión**: dos operaciones de cuenta, sin club en la ruta:
  - `GET /api/cuenta/datos-propios` → `DatosPropiosDto(Nombres, Apellidos, TipoDocumento,
    NumeroDocumento, FechaNacimiento, Correo, Celular)`. Lee los integrantes de la cuenta con rol
    PRESIDENTE, DIRECTIVO o ENTRENADOR (`IRepositorioPertenencias.ListarDeUsuarioAsync`, que ya
    existe para la sesión) y toma el más antiguo. Si la cuenta no tiene ninguno, `404
    no_encontrado`.
  - `PUT /api/cuenta/datos-propios` con `CambiarDatosPropiosDto(Nombres, Apellidos,
    FechaNacimiento, Celular)` → `DatosPropiosDto`. Valida con `ValidadorIdentidad`
    (`NombresYApellidos`, `FechaNacimiento`) y la regla del celular del registro (obligatorio,
    hasta 20), normaliza igual que el registro y, en una transacción, escribe nombres, apellidos y
    fecha en **todos** los integrantes de la cuenta con esos tres roles, en todos sus clubes, y el
    celular en la cuenta.
- **Integrantes JUGADOR de la misma cuenta**: no se tocan. Cada jugador es otra persona, con su
  documento; sus datos se cambian desde su ficha (§12.6, RF-017). El celular sí es de la cuenta y
  se ve en sus jugadores, como ya pasa al cambiarlo desde la ficha (RF-039 de la 005).
- **Documento y correo**: el DTO de entrada no los tiene. Un cuerpo con `tipoDocumento`,
  `numeroDocumento` o `correo` se ignora (la deserialización no los lee) y la prueba comprueba que
  no cambian (RF-019, escenario 4.4). No existe ninguna otra operación que cambie el documento de
  un integrante que no es jugador.
- **Lo copiado no cambia**: los nombres ya copiados en aprobaciones, retiros y últimos cambios
  siguen como estaban (§13).
- **Sesión**: tras guardar, la pantalla recarga la sesión para que la barra superior muestre el
  nombre nuevo (`ClubDeSesionDto.Nombres`).
- **Alternativas descartadas**: cambiarlos solo en el club elegido (RF-016 pide todos sus clubes);
  un formulario dentro del club (la persona es la misma en todos).

## 10. Escritura entre clubes de la propia cuenta y §7.1

- **Hecho**: §7.1 enumera como excepciones al límite de club "las consultas de la propia cuenta,
  necesarias para iniciar sesión y elegir club". El `PUT` de §9 escribe en integrantes de varios
  clubes.
- **Decisión**: se limita estrictamente a los integrantes de la cuenta con sesión, con roles
  PRESIDENTE, DIRECTIVO o ENTRENADOR, y no devuelve nada de ningún club. Lo exige §12.6 de la misma
  versión ("El cambio vale en todos sus clubes"), así que no es una regla inventada; pero el texto
  de §7.1 no la nombraba. La enmienda de redacción 4.5.1 (2026-10-10) añade a las excepciones de
  §7.1 los datos propios de §12.6, limitados a la cuenta con sesión, sin tocar sus integrantes
  JUGADOR y sin devolver datos de ningún club.

## 11. "Mi perfil" de la cuenta de un jugador

- **Decisión**: la pantalla mira la sesión. Por cada club donde la cuenta es JUGADOR muestra un
  acceso "Mi ficha" al jugador que corresponde: el elegido en ese club si lo hay
  (`jugadorElegido.ts`), o el único. No muestra un formulario de datos (RF-017, escenario 4.5). Si
  la cuenta tiene además un rol de club en otro club (caso límite), muestra los dos bloques.
- El correo se muestra arriba, como hoy, de solo lectura (RF-018).

## 12. Pruebas

- **Integración** (nuevas carpetas `Personas/` y `Cuenta/`):
  - Ficha básica por cada rol que pregunta (familia, ENTRENADOR, DIRECTIVO, PRESIDENTE) sobre un
    ENTRENADOR con dos equipos, un DIRECTIVO que entrena y un PRESIDENTE que no; JSON sin
    documento, fecha, correo ni celular (comprobado sobre el texto de la respuesta).
  - Categorías y equipos desactivados no salen; asignación retirada deja de salir.
  - Integrante JUGADOR → 404; otro club → 404; DESARROLLADOR → 404; en espera y retirado → 403.
  - Persona con roles en dos clubes: cada ficha solo con lo de su club, la misma foto.
  - Foto: la ven todos los roles del club; 404 desde otro club, para el DESARROLLADOR y para un
    integrante JUGADOR (la foto de perfil de una cuenta de jugador sigue privada, CE-003).
  - Enlaces: `retiradoPorFicha`, `aprobadoPorFicha`, `autorFicha` y `hermanoDeFicha` presentes
    cuando la persona sigue y `null` cuando salió del club o fue la familia.
  - "Mi categoría" lleva `usuarioRolId` y `versionFoto` de cada entrenador.
  - Datos propios: lectura por los tres roles; cambio aplicado en todos los clubes y en el
    celular; rechazos con motivo y sin cambios; documento y correo intactos aunque vengan en el
    cuerpo; integrantes JUGADOR de la misma cuenta intactos; cuenta solo de jugador → 404.
  - Aislamiento: las 2 rutas nuevas de club entran en `AccesoClubPruebas` (46 → 48) y el contrato
    pasa de 68 a 72.
- **Unitarias**: la resolución del tipo de enlace (`ReglaEnlaceDeFicha`) por rol y estado.
- **Frontend (Vitest)**: `NombrePersona` (enlace o texto), `useVolver` y la caché generalizada.
