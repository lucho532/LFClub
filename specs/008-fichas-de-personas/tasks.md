---

description: "Lista de tareas de la funcionalidad 008: nombres clicables y ficha básica de entrenadores, presidentes y directivos"
---

# Tareas: Nombres clicables y ficha básica de entrenadores, presidentes y directivos

**Entrada**: documentos de diseño en `specs/008-fichas-de-personas/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.5.1) y el plan (research §12).
Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa contra la API real
y sus pruebas; las tareas que solo traen pruebas o solo pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario. Siguen el orden de construcción
del plan: **la historia 3 va antes que la 2**, porque da el primer camino real a la ficha básica
desde una pantalla; después, la historia 2 (los nombres de todas las pantallas) y la 4 ("Mi
perfil"), que no depende de las demás.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 007, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): **ninguna tabla, columna, índice ni migración**. Si una tarea parece
  necesitarlos, se detiene y se informa (§25).
- **Contrato**: `specs/008-fichas-de-personas/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs:
  T001 deja comentadas las cuatro operaciones nuevas y cada tarea que construye una la descomenta y
  sube los contadores (contrato y endpoints de club, en
  `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`). Recorrido: 68/46 → 69/47
  (T008) → 70/48 (T009) → 71/48 (T016) → 72/48 (T017).
- **Rutas reales** de las respuestas que ganan campos (confirmadas en los controladores; el
  comentario del contrato decía `/retirados`): `GET /api/clubes/{clubId}/mi-categoria`,
  `GET /api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha` (y toda operación que responde
  `FichaJugadorDto`), `GET /api/clubes/{clubId}/jugadores/retirados`,
  `GET /api/clubes/{clubId}/ingresos/aprobados` y `GET /api/clubes/{clubId}/ingresos/en-espera`.
  Solo **añaden** campos: ninguno existente cambia de nombre ni de tipo.
- **Nada cambia para lo anterior** (§27.2): las pruebas de la 001 a la 007 no se tocan, salvo las
  que nombra cada tarea (`MiCategoriaPruebas` en T010, `fotosJugador.test.ts` en T005 y los
  contadores de `AccesoClubPruebas`). Si una falla y no está claro si es una regresión, se detiene
  y se pregunta.
- **La API decide los enlaces** (research §6, §15): la pantalla nunca infiere si un nombre es
  enlace por el rol de quien mira. Usa `EnlaceFichaDto` cuando la respuesta lo trae, o el
  `usuarioRolId` de una lista que ya está limitada a quien la ve (jugadores de una categoría,
  entrenadores de una categoría). Que un nombre sea enlace **no concede acceso**: la ficha del
  jugador sigue pasando por `AccesoAFicha` y `ReglaAccesoAFicha` (RF-012). **Ninguna regla de
  acceso nueva** a la ficha del jugador.
- **Ficha básica sin datos privados** (RF-002): `FichaBasicaDto` lleva solo `usuarioRolId`,
  `nombres`, `apellidos`, `rol`, `versionFoto` y `categorias`. Ninguna respuesta de esta
  funcionalidad sobre otra persona lleva documento, fecha de nacimiento, correo, celular ni
  responsable.
- **Como si no existiera** (RF-004): una ficha básica o una foto de un integrante de otro club, de
  un integrante JUGADOR, inexistente o sin foto responden el mismo `404 no_encontrado`. El
  DESARROLLADOR, también `404`.
- **Lo copiado no cambia** (§13): los nombres guardados en retiros, aprobaciones y últimos cambios
  se muestran como están; el enlace lleva a la persona de hoy.
- **Nombres** (§2.1, §2.2): todo en español. "Entrenador", "presidente", "directivo", "ficha
  básica", "datos propios"; ni Profesor ni Acudiente. No se crea ninguna entidad `Jugador`
  (Seguimiento de complejidad del plan).
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) que diga qué representa, su responsabilidad y qué no hace; toda clase que cambia de
  responsabilidad la actualiza en la misma tarea.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas.
  `frontend/src/compartido/api/tipos.ts` tiene 239 y **no crece**: los tipos nuevos van en
  `tiposPersonas.ts`, y los de ingresos se mudan a `tiposIngresos.ts` (T014).
  `frontend/src/privado/jugadores/fotosJugador.ts` (151) adelgaza en T005; `MiPerfil.tsx` (110)
  se divide en T016.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores solo
  delegan; los repositorios no tienen reglas; `ReglaEnlaceDeFicha` es dominio puro.
- **Aislamiento** (§7.1): la ficha básica y su foto, bajo `/api/clubes/{clubId}` con el filtro de
  club activo. La única escritura que cruza clubes es la de los datos propios (T017), limitada a
  los integrantes PRESIDENTE, DIRECTIVO o ENTRENADOR de la cuenta con sesión, la excepción de los
  datos propios que §7.1 recoge desde la 4.5.1.
- **Pantallas** (CE-007, §24): componentes `Tarjeta`, `Avatar`, `Aviso`, `Boton`, `Campo` y el
  bloque de foto de la ficha del jugador de la 007; legibles a 360 px y en los dos temas, sin
  desplazamiento horizontal de la página; nombres largos con `overflow-wrap: anywhere`.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LaPecosa.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

Las tareas los dan por buenos; si el propietario cambia alguno, cambia solo la tarea indicada.

| # | Supuesto | Tarea | Estado |
| --- | --- | --- | --- |
| 1 | En la sala de espera, el nombre de quien espera se queda como texto (su tarjeta ya muestra sus datos) y el "hermano de" es enlace a la ficha del hermano (research §8, escenario 2.7) | T014 | Confirmado por el propietario |
| 2 | Los datos propios se leen del integrante PRESIDENTE, DIRECTIVO o ENTRENADOR más antiguo (`CreadoEn`) de la cuenta (research §9) | T016 | Decidido en el plan |
| 3 | "Mi perfil" de la cuenta de un jugador muestra un acceso "Mi ficha" por cada club donde es JUGADOR, al jugador elegido o al único (research §11) | T016 | Decidido en el plan |
| 4 | Si la ficha se abre sin `state` de origen (dirección escrita), volver lleva al predeterminado de hoy: "← Inicio" en la ficha básica y lo de la 005/007 en la del jugador (research §7) | T006, T008 | Decidido en el plan |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: que la batería siga en verde mientras el contrato de la 008 ya existe en `specs/`.

- [ ] T001 Comentar en `specs/008-fichas-de-personas/contracts/api.yaml` las cuatro operaciones nuevas (`GET /api/clubes/{clubId}/personas/{usuarioRolId}`, `GET …/personas/{usuarioRolId}/foto`, `GET /api/cuenta/datos-propios` y `PUT /api/cuenta/datos-propios`), cada una con una marca `# 008-T0NN` que diga qué tarea la descomenta (T008, T009, T016 y T017). Corregir en el comentario "Respuestas que cambian" la ruta de retirados a `GET /api/clubes/{clubId}/jugadores/retirados`. Comprobar que `AccesoClubPruebas` (`backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`) sigue en 68 y 46 y que la batería entera pasa

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: el tipo de enlace, el sembrador, los tipos del frontend, la caché de fotos generalizada, el volver genérico y el componente `NombrePersona`.

**⚠️ CRÍTICO**: ninguna historia empieza hasta terminar esta fase.

- [ ] T002 [P] Crear la enumeración `TipoFicha` en `backend/src/LaPecosa.Dominio/Enumeraciones/TipoFicha.cs` con los valores `JUGADOR` ("ficha del jugador de la 005 y la 007") y `BASICA` ("ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR"), serializada como texto como `Rol`, y el DTO `EnlaceFichaDto(Guid UsuarioRolId, TipoFicha Tipo)` en `backend/src/LaPecosa.Aplicacion/DTOs/EnlaceFichaDto.cs`, con documentación XML: "A dónde lleva un nombre. Lo pone la API solo cuando quien mira puede abrir esa ficha; `null` si no puede o si la persona ya no está en el club (RF-011). No concede acceso"
- [ ] T003 [P] Crear `SembradorPersonas` en `backend/pruebas/Integracion/Base/SembradorPersonas.cs` (máx. 250 líneas, apoyado en `Sembrador`, `SembradorCategorias` y `SembradorJugadores`) que arme los datos de partida de quickstart.md: club A con presidente P sin asignación y con foto de perfil, directivo D asignado como entrenador de la 2015 sin foto, entrenador E1 asignado a la 2014 dirigiendo los equipos "A" y "B" con foto, entrenador E2 sin categorías; jugadores Ana (2014) y Caro (2015), Eva retirada por P, Luis hermano de Ana en espera; ficha de Ana cambiada por última vez por P; categoría 2016 desactivada con una asignación de E1 y, en la 2014, un tercer equipo "C" desactivado al que E1 está asignado; club B con su presidente, un jugador y la cuenta de E1 como PRESIDENTE. Más `PonerFotoPerfilAsync(usuario, bytes)` que escribe la `FotoPerfil` y sube `Usuario.VersionFoto` directamente, para las pruebas que solo leen la foto
- [ ] T004 [P] Crear `frontend/src/compartido/api/tiposPersonas.ts` con `TipoFicha = 'JUGADOR' | 'BASICA'`, `EnlaceFichaDto` (`usuarioRolId`, `tipo`), `FichaBasicaDto` (`usuarioRolId`, `nombres`, `apellidos`, `rol`, `versionFoto: number`, `categorias: CategoriaDeFichaBasicaDto[]`), `CategoriaDeFichaBasicaDto` (`anio`, `equipos: string[]`), `DatosPropiosDto` (`nombres`, `apellidos`, `tipoDocumento`, `numeroDocumento`, `fechaNacimiento`, `correo`, `celular`) y `CambiarDatosPropiosDto` (`nombres`, `apellidos`, `fechaNacimiento`, `celular`), copiados de `contracts/api.yaml`, con un comentario por tipo
- [ ] T005 [P] Generalizar la caché de fotos: crear `frontend/src/compartido/fotos/fotosEnMemoria.ts` con lo genérico de `frontend/src/privado/jugadores/fotosJugador.ts` (pedir una imagen con la sesión, guardar la URL de objeto bajo la llave `ruta@sello`, no pedir nada con sello `null` o `0`, `vaciarFotosEnMemoria()` que libera las URL); `fotosJugador.ts` queda como capa fina que arma la ruta del jugador y usa `fotoActualizadaEn` como sello, conservando su API pública (`vaciarFotosJugador` puede delegar o desaparecer si `ProveedorSesion.tsx` pasa a llamar a `vaciarFotosEnMemoria`). Ajustar el cierre de sesión en `frontend/src/compartido/sesion/ProveedorSesion.tsx`. Pruebas: `frontend/pruebas/fotosEnMemoria.test.ts` (la llave cambia con el sello, sello `null` o `0` no pide, rutas distintas no se pisan, la caché queda vacía tras vaciar) y `frontend/pruebas/fotosJugador.test.ts` ajustado solo en lo que nombre el módulo movido, sin perder ningún caso
- [ ] T006 [P] Volver genérico (RF-013): crear `frontend/src/compartido/navegacion/useVolver.ts` con `useVolver(predeterminado: { ruta, texto })`, que lee `location.state.volver` (`{ ruta, texto }`) y, si no está o no es válido, devuelve el predeterminado; y un ayudante `estadoVolver(ruta, texto)` para quien arma el enlace. Pasar la marca `desde: 'jugadores'` de la 007 a este mecanismo en `frontend/src/privado/jugadores/TablaDeGrupo.tsx` (estado `{ volver: { ruta: '/club/:clubId/jugadores', texto: 'Jugadores' } }`) y en `frontend/src/privado/ficha/FichaJugador.tsx` (enlace "← {texto}"; sin estado, lo de la 005). Pruebas: `frontend/pruebas/useVolver.test.ts` (con estado, sin estado, con estado mal formado)
- [ ] T007 Crear `frontend/src/compartido/componentes/NombrePersona.tsx` (depende de T004 y T006): recibe `nombre` (texto ya armado), `enlace: EnlaceFichaDto | null` y `clubId`; con enlace pinta un `Link` de React Router a `/club/:clubId/jugadores/:usuarioRolId/ficha` (`JUGADOR`) o `/club/:clubId/personas/:usuarioRolId` (`BASICA`) con `state` de `estadoVolver` con la ruta actual y el texto de pantalla que reciba como `textoVolver`; sin enlace, un `span` de texto normal sin aspecto ni foco de enlace (RF-011). Estilo con `overflow-wrap: anywhere` en `frontend/src/compartido/tema/componentes.css`. Pruebas: `frontend/pruebas/NombrePersona.test.tsx` (enlace a cada tipo con su ruta y su estado; `null` da texto sin `a`; nombre muy largo se pinta entero)

**Punto de control**: nada cambia para nadie; "← Jugadores" de la 007 sigue funcionando con el mecanismo nuevo y la batería de la 001 a la 007 sigue en verde.

---

## Fase 3: Historia 1 - Ver la ficha básica de un entrenador, presidente o directivo (Prioridad: P1) 🎯 MVP

**Objetivo**: cualquier integrante del club, también la familia, abre la ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR con nombre, rol, foto o iniciales y las categorías y equipos activos que entrena; nadie de fuera la obtiene.

**Prueba independiente**: con el club de `SembradorPersonas`, abrir las fichas básicas de E1, E2, D y P como la cuenta de Ana, como E2, como D y como P, y desde el club B (quickstart, bloque 1).

- [ ] T008 [US1] Ficha básica, de punta a punta. Backend: `FichaBasicaDto(Guid UsuarioRolId, string Nombres, string Apellidos, Rol Rol, int VersionFoto, IReadOnlyList<CategoriaDeFichaBasicaDto> Categorias)` y `CategoriaDeFichaBasicaDto(int Anio, IReadOnlyList<string> Equipos)` en `backend/src/LaPecosa.Aplicacion/DTOs/` (sin identificadores de categoría ni de equipo; documentación XML: "nunca lleva documento, fecha de nacimiento, correo, celular ni responsable"); `IRepositorioFichaBasica` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioFichaBasica.cs` y `RepositorioFichaBasica` en `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioFichaBasica.cs`, con el filtro de club activo y dos consultas: el `UsuarioRol` del club con `Rol` ∈ {PRESIDENTE, DIRECTIVO, ENTRENADOR} y `EstadoIngreso = APROBADO`, y la `VersionFoto` de su `Usuario` (`null` si no existe, es JUGADOR o no está aprobado), y sus `AsignacionEntrenadorCategoria` con `Activa = true` de categorías con `Activa = true`, con los nombres de sus `EntrenadorEquipo` cuyo `Equipo` está activo; `MapperFichaBasica` en `backend/src/LaPecosa.Aplicacion/Mappers/MapperFichaBasica.cs` (categorías por `Anio` ascendente, equipos por nombre; lista vacía = la categoría en general); `IServicioFichaBasica` en `backend/src/LaPecosa.Aplicacion/Servicios/IServicioFichaBasica.cs` y `ServicioFichaBasica` en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioFichaBasica.cs` (`ObtenerAsync(usuarioRolId)`: sin integrante, `404 no_encontrado`); `ControladorPersonas` en `backend/src/LaPecosa.Api/Controladores/Club/ControladorPersonas.cs` con `[Route("api/clubes/{clubId:guid}/personas")]` y `GET {usuarioRolId:guid}` con `[IntegranteDelClub]` sin roles exigidos (los 403 de pertenencia de siempre); registro en `backend/src/LaPecosa.Api/Configuracion/RegistroDeCasosDeUso.cs` y `RegistroDeServicios.cs`; descomentar la operación en el contrato y subir los contadores a 69 y 47. Pantalla: ruta `personas/:usuarioRolId` dentro de `/club/:clubId` en `frontend/src/App.tsx`; `frontend/src/privado/personas/FichaBasica.tsx` con `useCarga`: enlace de volver con `useVolver({ ruta: '/club/:clubId', texto: 'Inicio' })`, `Avatar` con iniciales en tamaño grande (T009 pone la foto), nombre completo, rol en texto ("Presidente", "Directivo", "Entrenador") y la sección de categorías: "Categoría {anio} · {equipos separados por coma}" o solo "Categoría {anio}" con lista vacía; sin categorías, "Todavía no tiene categorías asignadas." si es ENTRENADOR y ninguna sección si es PRESIDENTE o DIRECTIVO (RF-006); con `404`, un `Aviso` "No encontramos esta ficha." sin más datos. Pruebas: `backend/pruebas/Integracion/Personas/FichaBasicaPruebas.cs` — E1 con "Entrenador", la 2014 con "A" y "B" y sin la 2016 ni el equipo "C"; D con "Directivo" y la 2015; P con "Presidente" y lista vacía; E2 con lista vacía; el texto de la respuesta no contiene `documento`, `nacimiento`, `correo`, `celular` ni el número de documento ni el correo sembrados (CE-001); al desactivar la asignación de E1 o un equipo, la siguiente consulta ya no los muestra; al cambiar el rol de D a ENTRENADOR, la siguiente dice ENTRENADOR; un integrante ENTRENADOR con `EstadoIngreso = EN_ESPERA` (escrito por el sembrador) da `404`; tras quitar a P del club (como en el bloque 2.2 del quickstart), su ficha básica y su foto responden `404 no_encontrado`. `backend/pruebas/Integracion/Personas/FichaBasicaPorRolPruebas.cs` — la misma ficha de E1 para la cuenta de Ana, Caro (que no es de su categoría), E2, D y P (RF-003); Ana sobre la `usuarioRolId` de Caro o la propia `404`; la cuenta de Ana con Luis elegido `403 ingreso_en_espera`, la de Eva `403 integrante_retirado`; con el club suspendido, E1 `403 club_suspendido` y P la abre. `backend/pruebas/Integracion/Personas/FichaBasicaEntreClubesPruebas.cs` — el presidente de B sobre E1 en A `404 no_encontrado` sin el nombre de E1 en el cuerpo; el DESARROLLADOR `404` (CE-002); E1 en B: "Presidente", sin categorías de A (RF-005, RF-020)
- [ ] T009 [US1] Foto de perfil ante el club, de punta a punta. Backend: `ObtenerDeIntegranteAsync(usuarioRolId)` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioFotosPerfil.cs` y `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioFotosPerfil.cs`: resuelve la cuenta a través del `UsuarioRol` del club de la petición (con el filtro de club activo) y solo si su `Rol` no es JUGADOR y está `APROBADO`; devuelve contenido y tipo, o `null`; `ObtenerFotoAsync(usuarioRolId)` en `IServicioFichaBasica` y `ServicioFichaBasica` (sin foto o sin integrante válido, `404 no_encontrado`); `GET {usuarioRolId:guid}/foto` en `ControladorPersonas`, con el mismo atributo, que devuelve los bytes con su `TipoContenido`, `Cache-Control: private, no-store` y `X-Content-Type-Options: nosniff`, como `ControladorFotoJugador`; descomentar la operación en el contrato y subir los contadores a 70 y 48. Documentación XML de `backend/src/LaPecosa.Dominio/Entidades/FotoPerfil.cs`, `IRepositorioFotosPerfil.cs` y `backend/src/LaPecosa.Api/Controladores/Cuenta/ControladorFotoPerfil.cs`: deja de decir "solo la ve su dueño"; la ven los integrantes de los clubes donde la cuenta es PRESIDENTE, DIRECTIVO o ENTRENADOR, solo en la ficha básica y en la miniatura junto a su nombre (§14.2 4.5.0); `/api/cuenta/foto` sigue siendo solo la de la sesión y la única que carga, cambia o quita (RF-009). Pantalla: `frontend/src/privado/personas/FotoPersona.tsx` (tamaño `miniatura` o `grande`, pide `/api/clubes/{clubId}/personas/{usuarioRolId}/foto` con `fotosEnMemoria` y sello `versionFoto`; con `versionFoto = 0` o si falla la carga, `Avatar` con iniciales sin petición); `FichaBasica.tsx` la usa en grande. Pruebas: `backend/pruebas/Integracion/Personas/FotoDePersonaPruebas.cs` (con `PonerFotoPerfilAsync`) — la foto de E1 la obtienen la cuenta de Ana, E2, D y P, con su tipo de contenido y las dos cabeceras; D sin foto `404`; el presidente de B y el DESARROLLADOR `404`; la `usuarioRolId` de Ana (JUGADOR) `404` aunque su cuenta tenga foto de perfil (RF-008, CE-003); una cuenta que es ENTRENADOR en A y JUGADOR en B: su foto sale por A y `404` por B; E1 desde B (allí PRESIDENTE) da la misma foto; tras cambiar la foto por `PUT /api/cuenta/foto`, `versionFoto` de la ficha básica sube

**Punto de control**: quickstart, bloque 1, completo abriendo las fichas por dirección.

---

## Fase 4: Historia 3 - La familia ve la cara de los entrenadores de su jugador (Prioridad: P2)

**Objetivo**: en el inicio de la familia, cada entrenador de la categoría de su jugador aparece con su miniatura (o iniciales), su nombre como enlace a su ficha básica y el equipo que dirige.

**Prueba independiente**: con la 2014 con E1 (con foto) y un segundo entrenador sin foto, entrar como la cuenta de Ana y ver la foto de uno y las iniciales del otro (quickstart, bloque 3).

- [ ] T010 [US3] "Mi categoría" con identificador y foto, de punta a punta. Backend: `UsuarioRolId` (`Guid`) y `VersionFoto` (`int`) en `backend/src/LaPecosa.Aplicacion/DTOs/EntrenadorParaFamiliaDto.cs`, con su documentación XML reescrita (ya lleva identificador; deja sin efecto la parte de RF-035 de la 003); `RepositorioCategorias` (`backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioCategorias.cs`) lee el `UsuarioRolId` y la `Usuario.VersionFoto` de cada entrenador en la misma consulta; `backend/src/LaPecosa.Aplicacion/Mappers/MapperCategorias.cs` y `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioMiCategoria.cs` los pasan. Pantalla: los dos campos en `EntrenadorParaFamiliaDto` de `frontend/src/compartido/api/tiposCategorias.ts`; en `frontend/src/privado/TarjetaMiCategoria.tsx`, cada entrenador con `FotoPersona` en miniatura, su nombre con `NombrePersona` y enlace `{ usuarioRolId, tipo: 'BASICA' }` (`textoVolver: 'Inicio'`) y "dirige {equipos}"; el jugador sin categoría sigue igual. Pruebas: ajustar `backend/pruebas/Integracion/Categorias/MiCategoriaPruebas.cs` solo en la afirmación de que la respuesta no lleva identificador del entrenador; nueva `backend/pruebas/Integracion/Personas/MiCategoriaConFotoPruebas.cs` — cada entrenador trae su `usuarioRolId` y su `versionFoto` (E1 > 0, el segundo 0); tras quitar E1 su foto, `versionFoto` es 0; la respuesta sigue sin documento, correo ni celular de los entrenadores; jugador sin categoría igual que antes

**Punto de control**: quickstart, bloque 3, completo. Primer camino real a la ficha básica: inicio de la familia → nombre del entrenador → ficha básica → "← Inicio".

---

## Fase 5: Historia 2 - Todo nombre de persona lleva a su ficha (Prioridad: P1)

**Objetivo**: los nombres de personas de las pantallas del club (research §6) abren la ficha del jugador o la ficha básica cuando quien mira puede abrirla, y son texto normal cuando no; volver regresa a la pantalla de origen.

**Prueba independiente**: recorrer la tabla del bloque 2 de quickstart con cada rol y comprobar a dónde lleva cada nombre, cuáles son texto y que volver regresa al origen.

- [ ] T011 [P] [US2] Regla de dominio `ReglaEnlaceDeFicha` en `backend/src/LaPecosa.Dominio/Reglas/ReglaEnlaceDeFicha.cs`, pura, con los tres casos de data-model.md: `ParaAutor(rol?, estadoIngreso?)` → `BASICA` si hay integrante de esa cuenta en el club con rol PRESIDENTE, DIRECTIVO o ENTRENADOR y `EstadoIngreso = APROBADO`, si no `null`; `ParaPersonaAprobada(rol?, estadoIngreso?)` → JUGADOR aprobado (activo o retirado) `JUGADOR`, PRESIDENTE, DIRECTIVO o ENTRENADOR aprobado `BASICA`, no existe `null`; `ParaHermano(rol?, estadoIngreso?)` → JUGADOR aprobado `JUGADOR`, cualquier otro caso `null`. Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaEnlaceDeFichaPruebas.cs` con todas las combinaciones de rol y estado (en espera, rechazado, retirado, ausente), incluido "ENTRENADOR en espera → `null`" en `ParaAutor`
- [ ] T012 [US2] "Último cambio por" como enlace, de punta a punta, con el resolutor común. Backend: `IRepositorioEnlaces` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioEnlaces.cs` y `RepositorioEnlaces` en `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioEnlaces.cs`, con el filtro de club activo: `IntegrantesDeCuentasAsync(usuarioIds)` (para cada `UsuarioId`, su `UsuarioRol` del club con rol y estado, en **una** consulta) y `IntegrantesAsync(usuarioRolIds)` (rol y estado por identificador, en una consulta); `ResolutorDeEnlaces` en `backend/src/LaPecosa.Aplicacion/Utilidades/ResolutorDeEnlaces.cs`, que recibe todos los identificadores de una respuesta a la vez, consulta una vez y aplica `ReglaEnlaceDeFicha` (nunca una consulta por fila); `AutorFicha` (`EnlaceFichaDto?`) al final de `backend/src/LaPecosa.Aplicacion/DTOs/UltimoCambioDto.cs`, llenado en `backend/src/LaPecosa.Aplicacion/Mappers/MapperFicha.cs` con lo que resuelve `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioConsultaFicha.cs` a partir de `FichaJugador.UltimoCambioPorUsuarioId` (cambio de la familia: `null`); registro. Pantalla: `autorFicha: EnlaceFichaDto | null` en `UltimoCambioDto` de `frontend/src/compartido/api/tiposFicha.ts`; en `frontend/src/privado/ficha/UltimoCambio.tsx`, el autor con `NombrePersona` (`textoVolver`: el nombre del jugador); "la cuenta del jugador" sigue como texto. Pruebas: `backend/pruebas/Integracion/Personas/EnlacesDeFichaPruebas.cs` — la ficha de Ana cambiada por P trae `autorFicha` `BASICA` con la `usuarioRolId` de P, para P, D, E1 y la cuenta de Ana; tras quitar a P del club, `autor` sigue con su nombre y `autorFicha` es `null`; un cambio de la familia trae `null`; si P cambia sus apellidos, `autor` sigue con el nombre de entonces (§13); la cuenta de Ana no obtiene la ficha de Caro aunque use su `usuarioRolId` (`404`, RF-012, CE-005)
- [ ] T013 [US2] "Retirado por" como enlace, de punta a punta. Backend: `RetiradoPorFicha` (`EnlaceFichaDto?`) al final de `backend/src/LaPecosa.Aplicacion/DTOs/JugadorRetiradoDto.cs`, resuelto con `ResolutorDeEnlaces` sobre los `RetiradoPorUsuarioId` de toda la lista en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioConsultaCategorias.cs` y pasado por `MapperCategorias` (`GET /api/clubes/{clubId}/jugadores/retirados`). Pantalla: `retiradoPorFicha` en `JugadorRetiradoDto` de `frontend/src/compartido/api/tiposCategorias.ts`; en `frontend/src/privado/categorias/SeccionRetirados.tsx`, "retirado por" con `NombrePersona` y el jugador retirado con `NombrePersona` y enlace `{ usuarioRolId, tipo: 'JUGADOR' }` (`textoVolver: 'Categorías'`). Pruebas: en `EnlacesDeFichaPruebas.cs` — Eva retirada por P trae `retiradoPorFicha` `BASICA`; un retiro hecho por la familia trae `null`; tras quitar a P del club, `null` con el nombre copiado intacto
- [ ] T014 [US2] Ingresos aprobados y sala de espera, de punta a punta. Backend: `Ficha` y `AprobadoPorFicha` (`EnlaceFichaDto?`) al final de `backend/src/LaPecosa.Aplicacion/DTOs/IngresoAprobadoDto.cs`, y `HermanoDeFicha` (`EnlaceFichaDto?`) al final de `backend/src/LaPecosa.Aplicacion/DTOs/IngresoEnEsperaDto.cs`, resueltos con `ResolutorDeEnlaces` (una consulta por lista) en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioConsultaIngresos.cs` y pasados por `backend/src/LaPecosa.Aplicacion/Mappers/MapperIngresos.cs`: la persona aprobada según su integrante actual, el aprobador como autor y el hermano por `AgregadoDesdeUsuarioRolId`. Pantalla: mudar `IngresoEnEsperaDto` e `IngresoAprobadoDto` de `frontend/src/compartido/api/tipos.ts` a un nuevo `frontend/src/compartido/api/tiposIngresos.ts` (para que `tipos.ts` no crezca) y actualizar sus importaciones; añadir los tres campos; en `frontend/src/privado/ingresos/SeccionIngresosAprobados.tsx`, la persona aprobada y "aprobado por" con `NombrePersona` (`textoVolver: 'Ingresos'`); en `frontend/src/privado/ingresos/SeccionSalaDeEspera.tsx`, "hermano de" con `NombrePersona` y el nombre de quien espera como texto (supuesto 1). Pruebas: en `EnlacesDeFichaPruebas.cs` — un JUGADOR aprobado trae `ficha` `JUGADOR` y, si luego se retira, sigue `JUGADOR`; un ENTRENADOR aprobado trae `BASICA`; "aprobado por" P trae `BASICA` y `null` tras quitar a P del club; Luis en espera trae `hermanoDeFicha` `JUGADOR` de Ana; con Ana retirada, `hermanoDeFicha` sigue siendo `JUGADOR` y con Ana rechazada o inexistente, `null`
- [ ] T015 [US2] Nombres de las listas que ya traen identificador (solo pantalla; las listas ya están limitadas por la API a quien las ve): en `frontend/src/privado/categorias/Categorias.tsx` y `frontend/src/privado/categorias/SeccionEntrenadores.tsx`, los entrenadores con `NombrePersona` y enlace `{ usuarioRolId, tipo: 'BASICA' }` (`textoVolver: 'Categorías'` o el año de la categoría); en `frontend/src/privado/categorias/SeccionJugadores.tsx` y `frontend/src/privado/categorias/SeccionSinCategoria.tsx`, los jugadores con `NombrePersona` y `tipo: 'JUGADOR'`, para que la ficha vuelva a la pantalla de origen; en `frontend/src/privado/inicio/TarjetaMiJugador.tsx`, el nombre del jugador también abre "Mi ficha" (con el botón "Abrir mi ficha" de la 007). El diálogo "Asignar entrenador" y el panel del DESARROLLADOR no cambian (research §6). Comprobar a mano la tabla del bloque 2 de quickstart, incluido que el propio nombre abre la propia ficha (escenario 2.9) y que volver regresa al origen con su nombre en el enlace

**Punto de control**: quickstart, bloque 2, completo. Historias 1, 2 y 3 juntas: toda persona del club tiene a dónde llevar y la familia ve la cara de sus entrenadores.

---

## Fase 6: Historia 4 - Cambiar mis datos personales desde "Mi perfil" (Prioridad: P2)

**Objetivo**: un PRESIDENTE, DIRECTIVO o ENTRENADOR ve sus datos en "Mi perfil" y cambia nombres, apellidos, fecha de nacimiento y celular en todos sus clubes; documento y correo de solo lectura; la cuenta de un jugador encuentra ahí "Mi ficha".

**Prueba independiente**: E1 abre "Mi perfil", ve sus datos, cambia los permitidos y se comprueba en su ficha básica de A y de B; los no permitidos no cambian; la cuenta de Ana ve "Mi ficha" (quickstart, bloque 4).

- [ ] T016 [US4] Ver los datos propios y "Mi perfil" dividido, de punta a punta. Backend: `DatosPropiosDto(string Nombres, string Apellidos, TipoDocumento TipoDocumento, string NumeroDocumento, DateOnly FechaNacimiento, string Correo, string Celular)` en `backend/src/LaPecosa.Aplicacion/DTOs/DatosPropiosDto.cs`; `IServicioDatosPropios` en `backend/src/LaPecosa.Aplicacion/Servicios/IServicioDatosPropios.cs` y `ServicioDatosPropios` en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioDatosPropios.cs` con `ObtenerAsync(usuarioId)`: con `IRepositorioPertenencias.ListarDeUsuarioAsync` toma los integrantes de la cuenta con rol PRESIDENTE, DIRECTIVO o ENTRENADOR, el de `CreadoEn` más antiguo (supuesto 2), y el correo y el celular de `Usuario`; sin ninguno, `404 no_encontrado`; `ControladorDatosPropios` en `backend/src/LaPecosa.Api/Controladores/Cuenta/ControladorDatosPropios.cs` con `[Route("api/cuenta/datos-propios")]` y `GET`, para la cuenta con sesión, sin club; registro; descomentar el `GET` en el contrato y subir el contador del contrato a 71. Pantalla: dividir `frontend/src/cuenta/MiPerfil.tsx` en un compositor corto con el correo arriba de solo lectura (RF-018), `frontend/src/cuenta/SeccionFotoPerfil.tsx` (lo de la 001, sin cambios de comportamiento), `frontend/src/cuenta/SeccionDatosPropios.tsx` (si la sesión tiene algún rol PRESIDENTE, DIRECTIVO o ENTRENADOR: nombres, apellidos, tipo y número de documento, fecha de nacimiento y celular, de solo lectura en esta tarea) y `frontend/src/cuenta/AccesosMiFicha.tsx` (por cada club donde la cuenta es JUGADOR, un acceso "Mi ficha" al jugador elegido en `frontend/src/compartido/sesion/jugadorElegido.ts` o al único; sin formulario de datos, supuesto 3); con roles de ambos tipos se ven los dos bloques. Pruebas: `backend/pruebas/Integracion/Cuenta/DatosPropiosPruebas.cs` — E1, D y P leen sus siete datos; E1 (en A y B) recibe los del integrante más antiguo; la cuenta de Ana `404 no_encontrado`; sin sesión `401`
- [ ] T017 [US4] Cambiar los datos propios, de punta a punta. Backend: `CambiarDatosPropiosDto(string Nombres, string Apellidos, DateOnly FechaNacimiento, string Celular)` en `backend/src/LaPecosa.Aplicacion/DTOs/CambiarDatosPropiosDto.cs` (sin documento ni correo: si vienen en el cuerpo se ignoran); `ValidadorDatosPropios` en `backend/src/LaPecosa.Aplicacion/Validadores/ValidadorDatosPropios.cs`, que reutiliza `ValidadorIdentidad` (`NombresYApellidos`, `FechaNacimiento`) y la regla del celular del registro, con la misma normalización: "nombres y apellidos obligatorios, hasta 80 caracteres, sin espacios sobrantes; fecha obligatoria y no futura; celular obligatorio, hasta 20 caracteres"; si algo falla, `400 datos_invalidos` con `errores` por campo y nada cambia; `CambiarDatosPropiosAsync(usuarioId, nombres, apellidos, fechaNacimiento, celular)` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioPertenencias.cs` y `backend/src/LaPecosa.Infraestructura/Repositorios/Plataforma/RepositorioPertenencias.cs`: en una transacción, escribe nombres, apellidos y fecha en **todos** los `UsuarioRol` de esa cuenta con rol PRESIDENTE, DIRECTIVO o ENTRENADOR, en todos sus clubes (sin filtro de club, como el resto de ese repositorio de plataforma), y el celular en `Usuario`; no toca integrantes JUGADOR de la cuenta ni los nombres copiados en retiros, aprobaciones y últimos cambios; `CambiarAsync` en `ServicioDatosPropios` (cuenta sin esos roles, `404`) que devuelve el `DatosPropiosDto` guardado; `PUT` en `ControladorDatosPropios`; descomentar el `PUT` y subir el contador del contrato a 72. Documentación XML de `backend/src/LaPecosa.Dominio/Entidades/UsuarioRol.cs` (nombres, apellidos y fecha de un PRESIDENTE, DIRECTIVO o ENTRENADOR los cambia él mismo, en todos sus clubes a la vez, §12.6) y `backend/src/LaPecosa.Dominio/Entidades/Usuario.cs` (`Celular`: también lo cambia desde "Mi perfil"). Pantalla: en `frontend/src/cuenta/SeccionDatosPropios.tsx`, nombres, apellidos, fecha de nacimiento y celular editables con `Campo` y un botón "Guardar"; tipo y número de documento de solo lectura; los errores de `datos_invalidos` junto a cada campo y los valores anteriores conservados; al guardar, "Datos guardados." y recarga de la sesión para que la barra superior muestre el nombre nuevo. Pruebas: `backend/pruebas/Integracion/Cuenta/DatosPropiosPruebas.cs` (ampliada) — E1 cambia celular, apellidos y fecha: se ven en `GET /api/cuenta/datos-propios`, en su ficha básica de A y de B y en la sesión; el celular de los jugadores de la misma cuenta cambia y sus nombres no; un cuerpo con `numeroDocumento`, `tipoDocumento` y `correo` responde `200` y no los cambia (RF-019, CE-006); "último cambio por" de la ficha de Ana sigue con el nombre anterior de P. `backend/pruebas/Integracion/Cuenta/DatosPropiosRechazosPruebas.cs` — nombres vacíos o de 81 caracteres, fecha futura y celular vacío o de 21 caracteres: `400 datos_invalidos` con el campo y el motivo, y ningún dato cambió en ningún club; la cuenta de Ana `404`

**Punto de control**: quickstart, bloque 4, completo. Las cuatro historias funcionan.

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: aislamiento, contrato, pantallas y validación de extremo a extremo.

- [ ] T018 Comprobar que no queda ninguna operación comentada en `specs/008-fichas-de-personas/contracts/api.yaml`, que `AccesoClubPruebas` pasa con 72 y 48 (las dos rutas nuevas de club responden `404` al integrante de otro club y al DESARROLLADOR, y `401` sin sesión) y que Swagger (`/swagger`) describe las cuatro operaciones y los cinco campos nuevos con sus DTOs y sus `problem+json`
- [ ] T019 [P] Revisión de pantallas a 360 px y en escritorio, en tema claro y oscuro (CE-007): ficha básica con nombre muy largo, foto grande e iniciales, "Mi categoría" con miniaturas, los nombres-enlace de cada pantalla de research §6 y "Mi perfil" con sus secciones y errores. Los ajustes van en `frontend/src/compartido/tema/fichas.css` o `frontend/src/compartido/tema/movil.css`. Contrastar con el lienzo de §24 (plan, nota 1): no trae ficha de entrenador; la ficha básica sigue los componentes de la ficha del jugador y la identidad visible del club (`identidad.css`)
- [ ] T020 [P] Revisar la documentación XML y los comentarios de componente de todo lo nuevo y de las clases que nombra el plan en §3 (`FotoPerfil`, `IRepositorioFotosPerfil`, `ControladorFotoPerfil`, `EntrenadorParaFamiliaDto`, `UsuarioRol`, `Usuario`), y que ningún archivo pase de 250 líneas con `scripts/verificar-tamano.ps1` (en especial `tipos.ts`, `fotosJugador.ts`, `MiPerfil.tsx` y `ControladorPersonas.cs`)
- [ ] T021 Recorrer `specs/008-fichas-de-personas/quickstart.md` entero en un proyecto de Compose aparte (`docker compose -p la-pecosa-validacion …` con `Brevo__Llave` vacía, para no enviar correos reales con la llave del `.env`), incluidas las comprobaciones directas contra la API. Si Docker se duerme sin contenedores, mantener uno mínimo en marcha y borrarlo al final junto con `docker compose -p la-pecosa-validacion down -v`. Comprobar la sala de espera como dice el escenario 2.7
- [ ] T022 Batería completa: `dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test`, `tsc`, `eslint` y `./scripts/verificar-tamano.ps1`, con las pruebas de la 001 a la 007 en verde

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (T001)**: sin dependencias.
- **Cimientos (T002 a T007)**: después de T001. Bloquean todas las historias.
- **Historia 1 (T008, T009)**: después de los cimientos.
- **Historia 3 (T010)**: después de T009 (usa `FotoPersona` y la ruta de la ficha básica).
- **Historia 2 (T011 a T015)**: T011 después de T002; T012 a T014 después de T008 (los enlaces `BASICA` llevan a la ficha básica); T015 después de T008.
- **Historia 4 (T016, T017)**: después de los cimientos; no depende de las historias 1 a 3, salvo la comprobación de T017 en la ficha básica (T008).
- **Cierre (T018 a T022)**: después de todas las historias.

### Entre tareas

- T002 → T011 (la regla devuelve `TipoFicha`) → T012 (el resolutor usa la regla).
- T004, T006 → T007 (`NombrePersona` usa el tipo y el volver).
- T005 → T009 (`FotoPersona` usa la caché generalizada).
- T008 → T009 (mismo controlador, servicio y pantalla).
- T012 → T013 → T014 (el resolutor y el mismo archivo `EnlacesDeFichaPruebas.cs`).
- T016 → T017 (mismo servicio, controlador y `SeccionDatosPropios`).
- Los contadores de `AccesoClubPruebas` suben en el orden T008, T009, T016, T017. Si se cambia el orden de las historias, cada tarea sube los suyos sobre lo que haya.

### Dentro de cada tarea

- Backend (DTO → interfaz → repositorio → servicio → controlador → registro → contrato), luego
  pantalla contra la API real, luego pruebas. La tarea no se marca hecha sin sus pruebas en verde.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos, tras T001
T002  TipoFicha y EnlaceFichaDto
T003  SembradorPersonas
T004  tiposPersonas.ts
T005  fotosEnMemoria.ts
T006  useVolver.ts
# y después T007 NombrePersona

# Historia 2 mientras avanza la historia 1 (archivos distintos)
T011  ReglaEnlaceDeFicha y sus pruebas unitarias
T008  Ficha básica

# Historia 4 mientras avanza la historia 2 (backend y pantallas distintos)
T016  Datos propios y "Mi perfil" dividido
T012  "Último cambio por" y el resolutor

# Cierre
T019  Revisión a 360 px y en los dos temas
T020  Documentación y tamaño
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T008, T009).
3. **Parar y validar**: quickstart, bloque 1, abriendo las fichas por dirección.
4. Historia 3 (T010): el primer camino real desde una pantalla, para la familia.

### Entrega incremental

1. Cimientos → nada cambia para nadie; la batería anterior en verde.
2. Historia 1 → la ficha básica y su foto existen y se protegen.
3. Historia 3 → la familia ve a sus entrenadores con cara y llega a su ficha.
4. Historia 2 → todos los nombres del club llevan a su ficha.
5. Historia 4 → "Mi perfil" con datos propios. **No se publica T016 sin T017** si eso dejara a
   la persona viendo datos que no puede corregir; si se publica sola, es solo lectura y no rompe
   nada.
6. Cierre → contrato, aislamiento, 360 px, quickstart y batería.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- Esta funcionalidad no tiene migración: la API arranca igual y ningún dato existente cambia.
- La escritura de datos propios en varios clubes (T017) es la excepción de §7.1 que añadió la
  enmienda 4.5.1. Ninguna tarea modifica la constitución.
- El caso límite "una cuenta pasa de JUGADOR a ENTRENADOR o DIRECTIVO" no tiene prueba propia:
  todavía no existe la operación de §12.2. `ReglaEnlaceDeFicha.ParaPersonaAprobada` ya resuelve por
  el rol actual (`BASICA` para un ENTRENADOR aprobado, probado en T014), así que funcionará cuando
  esa operación exista.
- Si una prueba de la 001 a la 007 falla al tocar `MapperFicha`, `MapperCategorias`,
  `MapperIngresos`, `RepositorioCategorias`, `RepositorioPertenencias`, `fotosJugador.ts` o
  `FichaJugador.tsx` y no está claro si es una regresión o un cambio previsto, se detiene esa
  parte y se pregunta (§25).
- Confirmar el cambio en git al terminar cada tarea o cada historia.
