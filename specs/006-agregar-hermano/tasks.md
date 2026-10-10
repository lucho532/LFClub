---

description: "Lista de tareas de la funcionalidad 006: agregar un hermano y elegir el jugador"
---

# Tareas: Agregar un hermano y elegir el jugador

**Entrada**: documentos de diseño en `specs/006-agregar-hermano/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.1.0) y el plan. Por §27.1, cada
tarea de una historia entrega el backend, la pantalla que lo usa contra la API real y sus pruebas;
las tareas que solo traen pruebas cubren el aislamiento de algo ya construido.

**Organización**: las tareas se agrupan por historia de usuario. Las historias 1 y 2 son las dos
P1; **la historia 2 se construye antes que la 1** (plan, "Orden de construcción sugerido"): sin
elegir jugador, un hermano recién agregado deja a la cuenta sin poder entrar al club.

**Incrementos** (2026-10-10): T001 a T022 están construidas y validadas y no se tocan. T023 a T031
construyen el primer incremento (RF-034 a RF-040, el documento que ya usa otra cuenta) y T032 a
T037 el segundo (RF-041 a RF-043, la familia retira y la reincorporación), en el orden de las
secciones "Orden de construcción" del plan. Se aplican contra la constitución **4.2.0**.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US3]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 005, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): solo la columna de T002 y su migración `HermanosDeLaCuenta`.
  Ninguna otra tarea crea tablas, columnas ni índices; si parece necesitarlos, se detiene y se
  informa (§25).
- **Contrato**: `specs/006-agregar-hermano/contracts/api.yaml` manda en rutas, cuerpos, códigos de
  estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs: T001 deja
  comentada la única ruta nueva y T012 la descomenta y sube los contadores.
- **Un solo integrante, nada nuevo** (§27.2): una cuenta con un integrante en el club no envía la
  cabecera y recibe exactamente lo de hoy. Las pruebas de la 001 a la 005 no se tocan, salvo las
  que nombra cada tarea. Si una de ellas falla y no está claro si es una regresión, se detiene y
  se pregunta.
- **Quién hace la petición** (research §2 y §3): lo decide solo `ReglaJugadorDeLaSesion`, que
  aplica `IntegranteDelClubAttribute`. Ningún servicio ni controlador lee la cabecera
  `X-Jugador-Elegido` ni la reclamación `jugadores` por su cuenta: reciben `ControladorBase.Integrante`.
- **Como si no existiera** (RF-029): un jugador ajeno, un hermano distinto del elegido y un
  identificador mal formado responden el mismo `404 no_encontrado`.
- **Nombres** (§2.1, §2.2): todo en español. "Hermano" y "jugador"; no se crea ninguna entidad
  `Familia`, `Acudiente` ni `Jugador`.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) que diga qué representa, su responsabilidad y qué no hace; toda clase que cambia de
  responsabilidad la actualiza en la misma tarea.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas.
  `frontend/src/compartido/api/tipos.ts` tiene 243: los tipos de la sesión van en `tiposSesion.ts`.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores solo
  delegan.
- **Aislamiento** (§7.1): las únicas consultas nuevas que cruzan clubes son las de
  `IRepositorioPertenencias`, siempre acotadas a una cuenta.
- **Pantallas** (RF-033, §24): componentes `Campo`, `Tarjeta`, `Aviso`, `Boton`, `EtiquetaEstado`
  y `DialogoConfirmacion` existentes; legibles a 360 px y en los dos temas; el estado de cada
  jugador se dice con texto además de color.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LaPecosa.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

El plan pidió confirmarlos (research.md, "Supuestos por confirmar"). Las tareas los dan por buenos;
si el propietario cambia alguno, cambia solo la tarea indicada.

| # | Supuesto | Tarea | Estado |
| --- | --- | --- | --- |
| 1 | No se admite el documento de un hermano que ya usa otra cuenta en otro club (`409 documento_en_otra_cuenta`) | T012 | **Cambiado por el propietario el 2026-10-09**: se admite y, al aprobarse, queda retirado del otro club (spec, RF-034 a RF-040). Lo construyen T023 a T031 |
| 2 | Confirmar dos veces el mismo hermano devuelve el que ya está en espera (`200`), sin error | T012 | Por confirmar |
| 3 | La elección de jugador sobrevive a recargar la página y se pierde al cerrar la pestaña, al cerrar sesión y al volver a entrar | T010 | Por confirmar |
| 4 | Si la cuenta ya tiene responsable, el que se escriba al agregar un hermano se ignora | T012 | Por confirmar |
| 5 | Si el jugador de origen deja de existir, la sala de espera no dice de quién es hermano | T015 | Por confirmar |
| 6 | La baja automática no guarda quién retiró: "Lo retiró" queda vacío en el otro club | T027 | Confirmado el 2026-10-10 |
| 7 | Al aprobar se vuelve a aplicar RF-039: documento de alguien que no es JUGADOR con otra cuenta, `409 documento_en_otra_cuenta` y no se aprueba | T027 | Confirmado el 2026-10-10 |
| 8 | Documento en dos cuentas del mismo nivel (dos activos, dos en espera o todos retirados): abre la del integrante más reciente | T023, T026 | Confirmado el 2026-10-10 |
| 9 | La reincorporación no mira el documento | T035 | **Decidido por el propietario el 2026-10-10**: se niega mientras esté activo con otra cuenta (RF-041) |
| 10 | El aviso a la familia llega en la respuesta de agregar, después de crear | T029 | Confirmado el 2026-10-10 |
| 11 | Cuando la familia retira, "Lo retiró" muestra el nombre del propio jugador | T033 | Confirmado el 2026-10-10 |
| 12 | Un hermano en espera con ese número en otra cuenta no impide reincorporar; si después se aprueba, retira al reincorporado | T035 | Confirmado el 2026-10-10 |
| 13 | Con varios hermanos, la familia retira al jugador elegido; para retirar a otro, cambia de jugador | T033 | Confirmado el 2026-10-10 |

Dos diferencias con el plan, encontradas al revisar el código: la política de CORS ya usa
`AllowAnyHeader()` (`backend/src/LaPecosa.Api/Configuracion/ConfiguracionSeguridad.cs`), así que no
hay que cambiarla; y `ErroresDeSesion` no se crea, porque `jugador_sin_elegir` solo lo emite el
atributo de autorización, con `Problema`, como los demás errores de acceso.

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: partir de una base en verde. No hay proyectos, paquetes ni tecnologías nuevas.

- [X] T001 Dejar la línea base en verde en la rama `006-agregar-hermano`. El contrato `specs/006-agregar-hermano/contracts/api.yaml` ya describe una ruta que la API todavía no tiene, y `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` (`La_api_expone_exactamente_los_endpoints_del_contrato` y la del `401` sin sesión) lee todos los contratos. En ese contrato, comentar con `#` al inicio de cada línea el bloque completo de `/api/clubes/{clubId}/jugadores/{usuarioRolId}/hermanos` (desde la línea de la ruta hasta la anterior a `# --- Sesion`), precedido de una línea `# PENDIENTE: se descomenta en T012`. Las rutas existentes que el contrato vuelve a describir (`/api/sesion`, `/api/sesion/renovacion`, las tres de `…/ingresos/…` y `…/ficha`) se quedan sin comentar: la prueba no las cuenta dos veces. Ejecutar `dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test` y `scripts/verificar-tamano.ps1` y confirmar que todo pasa, con 60 endpoints en el contrato y 38 de club. Si algo más falla, detenerse e informarlo: no se construye sobre una base rota

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: la columna, la regla que decide quién hace la petición, la limitación en el token,
el atributo de autorización y las ayudas de prueba. Al terminar la fase una cuenta con un solo
integrante se comporta igual que antes.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase

- [X] T002 [P] Columna y migración (data-model.md). En `backend/src/LaPecosa.Dominio/Entidades/UsuarioRol.cs`, añadir `AgregadoDesdeUsuarioRolId` ("Guid, opcional. Foránea a `UsuarioRol` (la misma tabla), borrado a `NULL`. Solo lo tiene el hermano agregado desde una ficha; `NULL` en quien entró con invitación") y su navegación `AgregadoDesde` (`UsuarioRol?`), y reescribir la documentación XML de la clase: una cuenta puede tener varios integrantes con rol JUGADOR en el mismo club. En `backend/src/LaPecosa.Infraestructura/Datos/Configuraciones/ConfiguracionUsuarioRol.cs`, la relación consigo misma con `DeleteBehavior.SetNull`, **sin índice nuevo** y sin tocar los índices existentes (`ClubId, NumeroDocumento` único; `ClubId, UsuarioId` no único). Generar la migración `HermanosDeLaCuenta` en `backend/src/LaPecosa.Infraestructura/Datos/Migraciones/` con `dotnet ef migrations add`, sin editarla a mano; debe contener solo la columna y su foránea. Comprobar que `backend/pruebas/Integracion/Aislamiento/ModeloPruebas.cs` sigue en verde
  - **Diferencia anotada (T002)**: la migración generada trae, además de la columna y la foránea, el índice
    `IX_UsuariosRol_AgregadoDesdeUsuarioRolId`. Entity Framework lo crea por convención para toda foránea
    (también lo tienen `AprobadoPorUsuarioId` y `RetiradoPorUsuarioId`) y no se quita sin editar la migración
    a mano. No se declaró ningún índice en `ConfiguracionUsuarioRol` y los existentes no cambian.
- [X] T003 [P] Regla de quién hace la petición (research §2 y §3). Crear `backend/src/LaPecosa.Dominio/Reglas/JugadorDeLaPeticion.cs`, un resultado inmutable con tres casos (`Integrante` con su `UsuarioRol`, `NoEncontrado`, `SinElegir`), y `backend/src/LaPecosa.Dominio/Reglas/ReglaJugadorDeLaSesion.cs`, sin dependencias de HTTP ni de EF, con `Resolver(IReadOnlyList<UsuarioRol> integrantesDeLaCuentaEnElClub, Guid? jugadorElegido, IReadOnlyCollection<Guid>? limitacion)`. Sin limitación: ningún integrante → `NoEncontrado`; uno y sin elegido, o elegido ese mismo → ese; uno y elegido otro → `NoEncontrado`; varios y elegido uno de ellos → ese; varios y sin elegido → `SinElegir`; varios y elegido otro → `NoEncontrado`. Con limitación (sesión iniciada con documento): con un solo integrante no cambia nada; con varios, el de la petición es el que está en la limitación, el elegido no puede cambiarlo (si llega con otro identificador, `NoEncontrado`) y, si ninguno está en ella, `NoEncontrado`; nunca devuelve `SinElegir`. La regla no mira estado de ingreso, retiro ni rol: eso lo aplica después `ReglaAccesoPorEstado`. Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaJugadorDeLaSesionPruebas.cs`: la tabla de research §2 fila por fila y cada caso de la limitación
- [X] T004 [P] La limitación viaja en el token (research §3). Añadir `ReclamacionJugadores = "jugadores"` a `backend/src/LaPecosa.Infraestructura/Seguridad/OpcionesSesion.cs`. Cambiar `backend/src/LaPecosa.Aplicacion/Interfaces/IEmisorTokenSesion.cs` y `backend/src/LaPecosa.Infraestructura/Seguridad/EmisorTokenSesion.cs` a `Emitir(Guid usuarioId, Guid selloSeguridad, IReadOnlyCollection<Guid>? jugadores = null)`: con una colección no vacía añade una reclamación `jugadores` por cada identificador; con `null` el token es idéntico al de hoy. En `backend/src/LaPecosa.Api/Autorizacion/ValidadorSesion.cs`, leer esas reclamaciones al validar el token, guardarlas en `HttpContext.Items` y exponer `static IReadOnlyCollection<Guid>? LimitacionDe(HttpContext contexto)` (`null` si el token no la trae; una reclamación que no sea un `Guid` hace fallar el token). En `backend/src/LaPecosa.Api/Controladores/ControladorBase.cs`, la propiedad protegida `Limitacion`. Actualizar la documentación XML de las cuatro clases (el token ya puede llevar a qué jugadores está limitada la sesión; sigue sin llevar club ni rol). Todavía nadie emite un token limitado: lo hace T009
- [X] T005 El atributo de autorización deja de suponer un integrante por cuenta y club (depende de T003 y T004). Añadir a `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioPertenencias.cs` y `backend/src/LaPecosa.Infraestructura/Repositorios/Plataforma/RepositorioPertenencias.cs` `ListarDeLaCuentaEnClubAsync(Guid usuarioId, Guid clubId)`: todos los integrantes de esa cuenta en ese club, cada uno con su club, del más antiguo al más reciente por `CreadoEn`; `ObtenerAsync` se deja como está para quien ya lo usa. En `backend/src/LaPecosa.Api/Autorizacion/IntegranteDelClubAttribute.cs`: leer esa lista en lugar de `ObtenerAsync`; leer la cabecera `X-Jugador-Elegido` (ausente o vacía es "sin elegido"; presente y que no sea un `Guid` responde `404 no_encontrado`); aplicar `ReglaJugadorDeLaSesion.Resolver` con `ValidadorSesion.LimitacionDe`; `NoEncontrado` → `Problema.NoEncontrado()`; `SinElegir` → `new Problema(409, "jugador_sin_elegir", "Elige con cuál jugador quieres continuar.")`; y al integrante resuelto aplicarle lo de hoy sin cambios (`ReglaAccesoPorEstado`, roles exigidos, `IContextoClub`, `Items`). El nombre de la cabecera, en una constante pública del atributo. Reescribir su documentación XML. Crear `backend/src/LaPecosa.Api/Configuracion/FiltroJugadorElegido.cs`, un filtro de operación de Swagger como `FiltroOperacionAnonima.cs`, que declara la cabecera opcional en las operaciones protegidas con `[IntegranteDelClub]`, y registrarlo en `backend/src/LaPecosa.Api/Configuracion/ConfiguracionApi.cs`. Comprobar que la política de CORS de `ConfiguracionSeguridad.cs` sigue admitiendo cualquier cabecera. Toda la batería de la 001 a la 005 debe seguir en verde sin tocar ninguna prueba
- [X] T006 [P] La ficha se compara con el jugador de la petición (research §9; RF-030; cambia el RF-005 de la 005). En `backend/src/LaPecosa.Aplicacion/Utilidades/AccesoAFicha.cs`, sustituir `jugador.UsuarioId == quienPregunta.UsuarioId` por `jugador.Id == quienPregunta.Id` y reescribir el comentario y la documentación XML ("es el jugador de la petición", no "es de su cuenta"). En `backend/src/LaPecosa.Dominio/Reglas/ReglaAccesoAFicha.cs`, renombrar el parámetro `esDeSuCuenta` a `esElJugadorDeLaPeticion` y ajustar su documentación; la regla no cambia de forma. Ajustar el nombre en `backend/pruebas/Unitarias/Reglas/ReglaAccesoAFichaPruebas.cs`. `backend/pruebas/Integracion/Ficha/FichaAjenaPruebas.cs` y el resto de pruebas de la ficha siguen en verde sin cambios. `MapperFicha` (¿el último cambio lo hizo la familia?) sigue comparando por cuenta: es correcto y no se toca
- [X] T007 Ayudas de prueba (depende de T002, T004 y T005). En `backend/pruebas/Integracion/Base/ClienteDePrueba.cs`: `ElegirJugador(Guid? usuarioRolId)`, que pone o quita la cabecera `X-Jugador-Elegido` en las peticiones siguientes, y `ConSesionLimitadaAsync(Usuario usuario, params Guid[] jugadores)`, que emite un token con la limitación; `ConSesionDeAsync` sigue emitiéndolo sin ella. Crear `backend/pruebas/Integracion/Base/SembradorHermanos.cs`, expuesto desde `FabricaApi` como los otros sembradores: `CrearHermanoAsync(UsuarioRol origen, EstadoIngreso estado = EstadoIngreso.EN_ESPERA, DateOnly? fechaNacimiento = null, bool activo = true)`, que inserta otro `UsuarioRol` con el mismo `UsuarioId` y `ClubId`, rol JUGADOR, documento único, `AgregadoDesdeUsuarioRolId = origen.Id` y un `CreadoEn` posterior al del origen. Crear `backend/pruebas/Integracion/Hermanos/EscenarioHermanos.cs`, sobre `EscenarioCategorias`: un club con presidente, directivo, entrenador y la categoría activa del año de Ana; Ana, jugadora aprobada, cuya cuenta es la de la familia; Beto, jugador de otra cuenta; otro club con su presidente; la ruta `Hermanos(Club club, Guid usuarioRolId)`; `DatosDeHermano(string? numeroDocumento = null, DateOnly? fechaNacimiento = null, string? nombreResponsable = null)` con el cuerpo de `AgregarHermanoDto`; y `ClienteDeLaFamiliaAsync(...)`, un cliente con la sesión de la cuenta de Ana y, opcionalmente, un jugador elegido

**Punto de control**: todas las pruebas de la 001 a la 005 siguen en verde y un sembrador ya crea
cuentas con dos jugadores en un club

---

## Fase 3: Historia 2 - La familia elige con cuál jugador continuar (Prioridad: P1)

**Objetivo**: una cuenta con varios jugadores en el club elige uno al entrar con el correo, ve solo
lo de ese jugador y puede cambiar sin cerrar sesión; si entra con el documento de uno, ve solo a
ese.

**Prueba independiente**: con una cuenta que tiene dos jugadores aprobados (sembrados) se inicia
sesión con el correo, se elige al segundo y "Mi ficha" es la suya; se cambia al primero y la ficha
cambia; se inicia sesión con el documento del segundo y no se ofrece cambiar.

- [X] T008 [US2] La sesión devuelve una entrada por club con sus jugadores (`GET /api/sesion`; research §4; depende de T007). Backend: crear `backend/src/LaPecosa.Aplicacion/DTOs/JugadorDeSesionDto.cs` (`UsuarioRolId`, `Nombres`, `Apellidos`, `EstadoIngreso`, `Retirado`, los cinco obligatorios); añadir al final de `backend/src/LaPecosa.Aplicacion/DTOs/ClubDeSesionDto.cs` `Guid UsuarioRolId` e `IReadOnlyList<JugadorDeSesionDto> Jugadores`. En `backend/src/LaPecosa.Aplicacion/Mappers/MapperSesion.cs`, `ASesion(usuario, integrantes, limitacion)` agrupa por club y conserva el orden actual de los clubes: `Jugadores` trae a todos los integrantes de la cuenta en ese club, del más antiguo al más reciente, **solo** si son más de uno y la sesión no está limitada; en cualquier otro caso va vacía. Los campos de la 001 (`Rol`, `EstadoIngreso`, `Retirado`, `Nombres`, `Apellidos`) y `UsuarioRolId` describen al único integrante; con limitación, al que está en ella; si hay que elegir, al más antiguo. Un club con varios integrantes y una limitación que no contiene a ninguno no aparece en `clubes`. Cambiar `backend/src/LaPecosa.Aplicacion/Servicios/IServicioSesion.cs` y `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioSesion.cs` a `ObtenerAsync(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, …)`, y `backend/src/LaPecosa.Api/Controladores/Cuenta/ControladorSesion.cs` le pasa `Limitacion`. Frontend: crear `frontend/src/compartido/api/tiposSesion.ts` con `JugadorDeSesionDto` y con `ClubDeSesionDto` (movido desde `tipos.ts`, con `usuarioRolId: string` y `jugadores: JugadorDeSesionDto[]`), y reexportar `ClubDeSesionDto` desde `frontend/src/compartido/api/tipos.ts` para que ninguna importación existente cambie. Pruebas: crear `backend/pruebas/Integracion/Hermanos/ElegirJugadorPruebas.cs`: una cuenta con dos jugadores recibe una sola entrada del club, con `jugadores` de dos elementos en orden de antigüedad y el estado de cada uno (activo, `EN_ESPERA`, retirado) (escenario 2.1, RF-022); una cuenta con un solo jugador recibe `jugadores` vacía y su `usuarioRolId` (2.3); `GET /api/clubes/{clubId}` sin cabecera responde `409 jugador_sin_elegir`, con la cabecera de cada jugador responde `200` y `miUsuarioRolId` es el elegido (2.2), con la de un jugador de otra cuenta, la de un integrante de otro club, un `Guid` inexistente y un texto que no es un `Guid` responde `404 no_encontrado` (2.8, RF-029); una cuenta de un solo jugador que envía su propio identificador recibe `200`, y con otro, `404`; elegir al hermano en espera responde `403 ingreso_en_espera` y elegir al retirado `403 integrante_retirado`, sin ningún dato del club (2.9, 2.10, RF-013); con dos hermanos elegidos por turno, `…/mi-categoria` devuelve la de cada uno (RF-024, CE-009). `backend/pruebas/Integracion/Cuenta/SesionPruebas.cs` sigue en verde sin cambios
  - **Diferencia anotada (T008)**: `PUT /api/cuenta/foto` también devuelve la sesión (`SesionDto`). Para que una
    sesión iniciada con documento no reciba a los hermanos por ese camino (RF-026, CE-007),
    `IServicioFotoPerfil.GuardarAsync` recibe la limitación y `ControladorFotoPerfil` le pasa `Limitacion`. No
    estaba en la lista de archivos del plan.
- [X] T009 [P] [US2] Entrar con el documento limita la sesión a ese jugador (`POST /api/sesion`, `POST /api/sesion/renovacion`; research §3; depende de T008). Añadir a `IRepositorioPertenencias` y `RepositorioPertenencias` `IdsDeLaCuentaConDocumentoAsync(Guid usuarioId, string numeroDocumento)`: los identificadores de los integrantes de esa cuenta que tienen ese número ya normalizado, en todos sus clubes. En `ServicioSesion.IniciarAsync`, cuando el identificador no lleva arroba y las credenciales son correctas, emitir el token con esos identificadores; con correo, sin limitación, como hoy. `RenovarAsync(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, …)` emite el token nuevo con la misma limitación, y `ControladorSesion` le pasa `Limitacion`. Se guardan identificadores y no el número, para que cambiar el documento del jugador durante la sesión (005) no la rompa. Actualizar la documentación XML de `ServicioSesion` e `IServicioSesion`. Pruebas en `backend/pruebas/Integracion/Hermanos/SesionConDocumentoPruebas.cs`, con una cuenta de dos jugadores aprobados: entrar con el documento de uno devuelve en `GET /api/sesion` el club con `jugadores` vacía y el `usuarioRolId`, los nombres y el estado de ese jugador, y el cuerpo no contiene el nombre ni el identificador del hermano (escenario 2.5, CE-007); sin cabecera, `GET /api/clubes/{clubId}` responde `200` como ese jugador; con la cabecera del hermano, `404`; la ficha y los documentos del hermano, `404`, con o sin su cabecera (2.6, RF-026); tras renovar el token la limitación sigue; tras cambiar el documento del jugador con `…/ficha/documento-identidad` el mismo token sigue entrando; entrar con el documento de un hermano en espera muestra el club como `EN_ESPERA` y `403 ingreso_en_espera` en el club (caso límite); entrar con el correo de la misma cuenta no tiene limitación; una cuenta de dos clubes que entra con su documento sigue viendo el club donde tiene un solo integrante; un club con varios integrantes y ninguno con ese documento no aparece en la sesión y responde `404`
- [X] T010 [P] [US2] Pantallas: elegir y cambiar de jugador (research §5; depende de T008). Crear `frontend/src/compartido/sesion/jugadorElegido.ts`: `leerJugadorElegido(clubId)`, `guardarJugadorElegido(clubId, usuarioRolId)`, `olvidarJugadorElegido(clubId)` y `olvidarJugadoresElegidos()`, sobre `sessionStorage` con una clave por club y tolerantes a que no haya almacenamiento, como `ultimoClub.ts`; y `jugadorDe(club: ClubDeSesionDto)`, que devuelve el `JugadorDeSesionDto` elegido si sigue en `club.jugadores`, o `null`. En `frontend/src/compartido/api/cliente.ts`, añadir la cabecera `X-Jugador-Elegido` cuando la ruta empieza por `/api/clubes/{clubId}` y hay una elección guardada para ese club; sin elección, la petición es la de hoy. En `frontend/src/compartido/sesion/ProveedorSesion.tsx`, `olvidarJugadoresElegidos()` en `iniciar` y en `cerrar` (RF-027), no al recargar la página (supuesto 3). Crear `frontend/src/privado/ElegirJugador.tsx`: con la identidad del club, la lista de `club.jugadores` con nombres, apellidos y estado en texto ("Activo", "Pendiente de aprobación", "Retirado"), un botón por jugador, el desplegable de clubes y "Cerrar sesión"; al elegir, guarda la elección y navega a `/club/{clubId}`. Crear `frontend/src/privado/BotonCambiarJugador.tsx`, que olvida la elección del club y vuelve a la lista; solo se pinta si `club.jugadores.length > 0`. En `frontend/src/privado/DisposicionClub.tsx`: si `deSesion.jugadores` trae elementos y `jugadorDe` da `null`, mostrar `ElegirJugador` antes que cualquier otra cosa y sin pedir nada al club (RF-021); con elección, sustituir `usuarioRolId`, `nombres`, `apellidos`, `estadoIngreso` y `retirado` de `deSesion` por los del elegido y seguir el flujo actual (sala de espera, aviso de retiro o aplicación del club); montar la aplicación del club con el jugador como `key`, para que no quede ningún dato del anterior (CE-009); el pie del menú, que ya muestra nombres y apellidos, muestra así los del elegido (RF-024), con `BotonCambiarJugador` debajo; añadir `jugador_sin_elegir` a `SESION_DESACTUALIZADA` y, ante él o ante `no_encontrado`, olvidar la elección del club antes de recargar la sesión. En `frontend/src/privado/SalaDeEspera.tsx` y `frontend/src/privado/AvisoRetirado.tsx`, `BotonCambiarJugador` (RF-013; escenarios 2.9 y 2.10); `SalaDeEspera` comprueba "sigue en espera" contra el jugador elegido. `DisposicionClub.tsx` no debe pasar de 250 líneas: si hace falta, sacar `AplicacionDelClub` a su propio archivo. Pruebas en `frontend/pruebas/jugadorElegido.test.ts`: guardar y leer por club; una elección de un club no vale en otro; `jugadorDe` devuelve `null` si el elegido ya no está en la lista o si la lista está vacía; `olvidarJugadoresElegidos` las borra todas
  - **Diferencia anotada (T010)**: el pie del menú (`.lateral-pie`) se ocultaba por debajo de 560 px. Cuando la
    cuenta tiene varios jugadores se queda a la vista (`.lateral-pie-fijo`), porque es donde se dice cuál es el
    elegido (RF-024) y donde está "Cambiar de jugador". La elección se lee con `useJugadorElegido`, para que la
    pantalla se vuelva a pintar al elegir o cambiar.
- [X] T011 [P] [US2] La ficha entre hermanos (RF-030; depende de T006 y T007). Crear `backend/pruebas/Integracion/Hermanos/FichaEntreHermanosPruebas.cs`, con dos hermanos aprobados de la misma cuenta, cada uno con ficha y documentos sembrados con `SembradorFichas`: con Ana elegida, las seis operaciones de la ficha de Luis (`GET` y `PUT …/ficha`, `…/ficha/documento-identidad`, `…/ficha/identidad` y subir, abrir y borrar `…/ficha/documentos/{documento}`) responden `404 no_encontrado` y nada cambia (escenario 2.7, CE-008); con Luis elegido, las mismas responden como a su familia; lo guardado en la ficha de uno no aparece en la del otro; cambiar el celular o el responsable desde la ficha de uno se lee en la del otro (RF-039 de la 005) y sella el último cambio solo en la ficha desde la que se hizo; el PRESIDENTE, el DIRECTIVO y el ENTRENADOR ven las dos fichas con el alcance de la 005, sin cabecera

  - **Diferencia anotada (T011)**: la 005 no tiene operación para borrar un documento (se reemplaza), así que
    las operaciones probadas son cinco más la corrección de identidad. `PUT …/ficha/identidad` exige el rol
    PRESIDENTE en el atributo: a la familia le responde `403 rol_no_autorizado`, sobre la ficha del hermano y
    sobre la propia, igual que en la 005; no `404`.

**Punto de control**: con hermanos sembrados, la familia elige, cambia y entra con documento
(quickstart, bloque 2). No se publica sin la historia 1 ni la historia 1 sin esta

---

## Fase 4: Historia 1 - La familia agrega un hermano desde la ficha (Prioridad: P1) 🎯 MVP

**Objetivo**: la cuenta de un jugador aprobado y activo agrega, desde "Mi ficha", otro jugador a la
misma cuenta y al mismo club. Queda en la sala de espera con el contacto de la cuenta.

**Prueba independiente**: se inicia sesión con la cuenta de un jugador aprobado, se agrega un
hermano con un documento nuevo y se comprueba que la familia lo ve como pendiente y que aparece en
la sala de espera del PRESIDENTE.

- [X] T012 [US1] Agregar al hermano (`POST /api/clubes/{clubId}/jugadores/{usuarioRolId}/hermanos`; research §6; depende de T008). Crear `backend/src/LaPecosa.Aplicacion/DTOs/AgregarHermanoDto.cs`: `Nombres` y `Apellidos` ("máx. 80"), `TipoDocumento`, `NumeroDocumento` ("máx. 20"), `FechaNacimiento` y `NombreResponsable` ("máx. 160, opcional"); no lleva correo, celular ni contraseña (RF-002). Crear `backend/src/LaPecosa.Aplicacion/Validadores/ValidadorHermano.cs`: `ValidadorIdentidad.NombresYApellidos`, `Documento` y `FechaNacimiento` (obligatorios, con el formato del registro y la fecha no futura, RF-006) y, si el hermano es menor según `ReglaMayoriaDeEdad` y la cuenta no tiene responsable, `nombreResponsable` obligatorio con el mismo mensaje del registro (RF-004); errores `400 datos_invalidos` por campo. Crear `backend/src/LaPecosa.Aplicacion/Servicios/IServicioAgregarHermano.cs` e `Implementaciones/ServicioAgregarHermano.cs`, `AgregarAsync(Guid usuarioRolId, UsuarioRol quienPregunta, AgregarHermanoDto datos)`: si `usuarioRolId != quienPregunta.Id`, `404 no_encontrado` (también para un hermano); validar; en `IUnidadDeTrabajo.EnTransaccionAsync` con `IRepositorioClub.BloquearAsync` como primera sentencia: si el número normalizado es de un integrante **en espera de la misma cuenta** en este club, devolverlo sin crear ni cambiar nada (supuesto 2); si lo tiene cualquier otro integrante del club, activo, retirado o en espera, `ErroresDeFicha.DocumentoRepetidoEnClub()` (`409 documento_repetido_en_club`, RF-005); si `ObtenerCuentaPorDocumentoAsync` devuelve otra cuenta, `ErroresDeFicha.DocumentoEnOtraCuenta()` (`409 documento_en_otra_cuenta`, supuesto 1), y si es la misma cuenta en otro club se admite; guardar `NombreResponsable` en la cuenta solo si no lo tenía y el hermano es menor (supuesto 4); crear el `UsuarioRol` con los valores de "Cómo nace un hermano" de data-model.md: `UsuarioId` y `ClubId` del jugador de origen, `Rol = JUGADOR`, `EstadoIngreso = EN_ESPERA`, identidad normalizada como en el registro, `AgregadoDesdeUsuarioRolId` el origen, `CategoriaId = NULL`, `Activo = true`, `AprobadoEn` y quién lo aprobó en `NULL`. No crea `FichaJugador` ni `DocumentoJugador`, no usa `IServicioCorreo` y no cuenta cuántos jugadores tiene la cuenta (RF-010). Una violación del índice `IndicesUnicos.DocumentoEnClub` se traduce al mismo `409`, como en `ServicioIdentidadJugador`. Añadir a `IRepositorioPertenencias` lo que falte para leer al integrante del club con ese documento. Crear `backend/src/LaPecosa.Api/Controladores/Club/ControladorHermanos.cs`, ruta `api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/hermanos`, etiqueta "Hermanos", `[IntegranteDelClub(Rol.JUGADOR)]`, que devuelve `JugadorDeSesionDto` con `201` al crear y `200` cuando ya existía. Registrar el servicio en `backend/src/LaPecosa.Api/Configuracion/RegistroDeCasosDeUso.cs`. Contrato: descomentar la ruta; `AccesoClubPruebas` pasa a 61 endpoints en el contrato y 39 de club. Pruebas: `backend/pruebas/Unitarias/Validadores/ValidadorHermanoPruebas.cs` (cada dato vacío; cada máximo en su límite y un carácter por encima; fecha futura; menor sin responsable con cuenta sin responsable y con él; adulto sin responsable). `backend/pruebas/Integracion/Hermanos/AgregarHermanoPruebas.cs`: `201`, y en la base de datos queda un integrante de la misma cuenta y el mismo club, JUGADOR, `EN_ESPERA`, sin categoría ni equipos, con el origen (escenario 1.2); el correo, el celular, el responsable y el hash de la contraseña de la cuenta no cambian y `GET …/ingresos/en-espera` los muestra para el hermano (1.3, CE-001); no se envía ningún correo (`CorreoEnMemoria`); dos hermanos seguidos quedan los dos en espera; después de agregarlo, la sesión de la familia trae `jugadores` con los dos. `backend/pruebas/Integracion/Hermanos/AgregarHermanoRechazosPruebas.cs`: cada dato obligatorio vacío y la fecha futura, `400` con el campo y nada creado (1.6); el documento de un integrante activo, de uno retirado y de uno en espera de otra cuenta, `409 documento_repetido_en_club` (1.5, CE-002); el documento de otra cuenta en otro club, `409 documento_en_otra_cuenta`; el documento de la misma cuenta en otro club, `201`; cuenta sin responsable y hermano menor sin `nombreResponsable`, `400`, y con él, `201` y la cuenta lo guarda (1.7); cuenta con responsable y otro en el cuerpo, se conserva el que tenía; la misma petición dos veces, `201` y después `200` con el mismo `usuarioRolId` y una sola fila, también enviadas a la vez con `Task.WhenAll` (1.10)
  - **Diferencia anotada (T012)**: dos pruebas de la 003 cuentan los endpoints que empiezan por
    `/api/clubes/{clubId}/jugadores` (`SoloPresidentePruebas`, 16, y `CategoriasPorEstadoDelClubPruebas`, 22) y
    el endpoint nuevo `…/hermanos` caía en su filtro. No es una regresión: en la 005 pasó lo mismo con `…/ficha`
    y se excluyó del filtro. Se hizo igual con `/hermanos`, sin cambiar ninguna comprobación; quién agrega se
    prueba en `QuienAgregaPruebas` y el estado del club en `HermanosEntreClubesPruebas` (T018).
- [X] T013 [P] [US1] Quién puede agregar y qué no ve el hermano en espera (depende de T012). Crear `backend/pruebas/Integracion/Hermanos/QuienAgregaPruebas.cs`: PRESIDENTE, DIRECTIVO y ENTRENADOR reciben `403 rol_no_autorizado` y el DESARROLLADOR `404` (escenario 1.9, RF-007); con un jugador retirado elegido, `403 integrante_retirado`, y con uno en espera, `403 ingreso_en_espera` (1.8, RF-008); con Ana elegida, `POST` sobre el identificador de su hermano, sobre el de Beto y sobre uno de otro club, `404` y nada creado; una cuenta con varios jugadores y sin cabecera, `409 jugador_sin_elegir`; en una sesión iniciada con el documento de Ana, agregar desde Ana responde `201` y la sesión sigue viendo solo a Ana (caso límite). Crear `backend/pruebas/Integracion/Hermanos/HermanoEnEsperaPruebas.cs`: el hermano en espera no aparece en las listas de jugadores de ninguna categoría, en "Sin categoría", en los retirados, en los candidatos a entrenador ni en los ingresos aprobados, para ningún rol (1.4, RF-012, CE-003); elegido, recibe `403 ingreso_en_espera` en todos los endpoints del club (recorrerlos con `EndpointsDeLaApi`, como `SalaDeEsperaPruebas`); mientras tanto, con Ana elegida todo responde como antes (RF-014); el PRESIDENTE recibe `404` ante la ficha del hermano en espera (RF-013)
- [X] T014 [P] [US1] Pantalla: "Agregar un hermano" en la ficha (depende de T010 y T012). Añadir `AgregarHermanoDto` a `frontend/src/compartido/api/tiposSesion.ts`. Crear `frontend/src/privado/ficha/DialogoAgregarHermano.tsx`: nombres, apellidos, tipo de documento (el mismo `select` del registro), número de documento y fecha de nacimiento; el nombre del responsable solo si la ficha trae `contacto.nombreResponsable` vacío y la fecha escrita es de un menor de 18 años (la misma función de edad que ya usa el frontend); sin correo, celular ni contraseña (RF-002); pinta cada error de la API junto a su campo y los `409` como aviso; el botón de confirmar queda deshabilitado mientras se envía (RF-009). Al terminar bien: guardar como elegido en este club al jugador de la ficha (`guardarJugadorElegido`), porque la cuenta pasa a tener varios y la siguiente petición sin cabecera sería `409`; recargar la sesión; y mostrar la confirmación "El ingreso de {nombres} está pendiente de aprobación del club". En `frontend/src/privado/ficha/FichaJugador.tsx`, el botón "Agregar un hermano" solo cuando `club.miRol === 'JUGADOR'` y la ficha es la de `club.miUsuarioRolId` (RF-032; escenarios 1.1, 1.8 y 1.9): un PRESIDENTE, un DIRECTIVO y un ENTRENADOR no lo ven. Sin desplazamiento horizontal a 360 px

**Punto de control**: con las historias 2 y 1, la familia agrega un hermano, lo ve pendiente y
elige entre sus jugadores (quickstart, bloques 1 y 2). Es el mínimo utilizable

---

## Fase 5: Historia 3 - El PRESIDENTE aprueba o rechaza al hermano (Prioridad: P2)

**Objetivo**: el PRESIDENTE ve en la sala de espera de quién es hermano cada ingreso; aprobarlo lo
ubica en la categoría de su año y rechazarlo borra solo a ese jugador.

**Prueba independiente**: con un hermano en espera, el PRESIDENTE lo aprueba y queda en la
categoría de su año con ficha propia; con otro, lo rechaza y desaparece de la lista de la familia
sin que el primer hijo cambie.

- [X] T015 [P] [US3] La sala de espera dice de quién es hermano (`GET /api/clubes/{clubId}/ingresos/en-espera`; research §7; depende de T007). Añadir al final de `backend/src/LaPecosa.Aplicacion/DTOs/IngresoEnEsperaDto.cs` `string? HermanoDe` ("nombres y apellidos del jugador desde cuya ficha se agregó; `null` si ese jugador ya no existe", supuesto 5). `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioIngresos.cs` carga `AgregadoDesde` en `ListarEnEsperaAsync` y `backend/src/LaPecosa.Aplicacion/Mappers/MapperIngresos.cs` lo escribe; el nombre no se copia en ninguna columna (§13). Frontend: `hermanoDe: string | null` en `IngresoEnEsperaDto` de `frontend/src/compartido/api/tipos.ts` y "Hermano de {hermanoDe}" en cada ingreso de `frontend/src/privado/ingresos/SeccionSalaDeEspera.tsx`, solo cuando viene. Pruebas en `backend/pruebas/Integracion/Ingresos/SalaDeEsperaPruebas.cs`: un hermano en espera llega con sus nombres, apellidos, documento y fecha de nacimiento, el correo, el celular y el responsable de la cuenta y `hermanoDe` con el nombre del origen (escenario 3.1, RF-015); con tres jugadores en la cuenta, `hermanoDe` es el de la ficha desde la que se agregó y no otro; si el origen se borra de la base de datos, el hermano sigue en la sala de espera con `hermanoDe` en `null`; un ingreso sembrado sin origen llega con `null`
  - **Diferencia anotada (T015)**: las pruebas de `hermanoDe` están en
    `backend/pruebas/Integracion/Hermanos/HermanoDePruebas.cs` y no en `Ingresos/SalaDeEsperaPruebas.cs`, porque
    usan `EscenarioHermanos`; `SalaDeEsperaPruebas.cs` no cambió. En la pantalla, el diálogo de rechazo de un
    hermano dice que la familia puede volver a agregarlo, en lugar de hablar de una invitación nueva.
- [X] T016 [P] [US3] Rechazar a un hermano conserva las invitaciones de la familia (`POST …/ingresos/{usuarioRolId}/rechazo`; research §8; depende de T007). En `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioRechazoIngreso.cs`, dentro de la misma transacción y después de borrar al integrante, llamar a `_invitaciones.BorrarDelCorreoAsync` **solo** si `IRepositorioPertenencias.ListarDeLaCuentaEnClubAsync(integrante.UsuarioId, integrante.ClubId)` ya no devuelve a nadie; el resto no cambia, y `EliminadorDeCuentaSinClub` tampoco. Reescribir la documentación XML del servicio. Pruebas en `backend/pruebas/Integracion/Hermanos/RechazarHermanoPruebas.cs`: `204`; no queda ninguna fila del hermano en `UsuariosRol`, `FichasJugador` ni `DocumentosJugador`; la cuenta, su hash de contraseña, su celular y su responsable, y el integrante, la ficha, los documentos, la categoría y los equipos de Ana quedan idénticos (escenario 3.4, RF-018, CE-005); la invitación usada con la que entró Ana sigue en la lista de invitaciones del club; no se envía ningún correo; tras el rechazo la sesión de la familia trae `jugadores` vacía y entra sin cabecera (3.5); con Ana y dos hermanos, rechazar a uno deja a los otros dos y la lista sigue; rechazar a un hermano ya aprobado, `409` como hoy. `backend/pruebas/Integracion/Ingresos/RechazoPruebas.cs` sigue en verde sin cambios: sin hermanos, las invitaciones se siguen borrando
- [X] T017 [US3] Aprobar a un hermano y volver a agregarlo (depende de T012). Sin código de producción nuevo: `ServicioAprobacionIngreso` no cambia. Crear `backend/pruebas/Integracion/Hermanos/AprobarHermanoPruebas.cs`: aprobar a un hermano nacido en el año de una categoría activa lo deja `APROBADO`, JUGADOR, en esa categoría, sin equipo y fuera de la sala de espera; nacido en un año sin categoría, queda sin categoría (escenario 3.2, RF-016, CE-004); recién aprobado y elegido por la familia, `GET /api/clubes/{clubId}` responde `200` y su ficha trae la identidad, el contacto de la cuenta, los demás grupos vacíos, los dos documentos pendientes y ninguna propiedad `ultimoCambio` (3.3, 3.8, RF-017); la ficha de Ana no cambió; DIRECTIVO, ENTRENADOR y JUGADOR reciben `403 rol_no_autorizado` al ver la sala de espera, aprobar y rechazar a un hermano (3.7, RF-020); tras rechazarlo, agregar de nuevo el mismo documento responde `201` y queda en espera (3.6, RF-019); aprobar y rechazar a la vez con `Task.WhenAll` deja una sola de las dos acciones, como en la 002; retirar a Ana con el hermano en espera o ya aprobado no cambia al hermano, y retirar al hermano no cambia a Ana (§14.1)

**Punto de control**: las tres historias funcionan de extremo a extremo (quickstart, bloques 1 a 3)

---

## Fase 6: Cierre y aspectos transversales

**Propósito**: lo que afecta a varias historias y la validación de extremo a extremo.

- [X] T018 [P] Pruebas transversales en `backend/pruebas/Integracion/Hermanos/HermanosEntreClubesPruebas.cs`: una cuenta que es jugador en dos clubes agrega un hermano en el Club A; la sesión trae `jugadores` solo en el Club A y en el Club B entra sin cabecera (RF-028); la cabecera del hermano del Club A en una ruta del Club B responde `404`; el presidente del Club B no ve al hermano en su sala de espera; en un club suspendido la familia recibe `403 club_suspendido` al agregar y el hermano en espera sigue ahí cuando el club se reactiva; en uno dado de baja nadie accede; eliminar la cuenta borra a todos sus jugadores, también al que está en espera; eliminar el club borra a los hermanos y no toca los de otro club; rechazar o retirar al jugador elegido con la sesión abierta hace que la siguiente petición con su cabecera responda `404` o `403 integrante_retirado`, sin datos de otro jugador (caso límite)
  - **Diferencia anotada (T018)**: la API todavía no tiene la operación con la que una persona elimina su
    cuenta (§12.4): ninguna spec la ha construido. Lo que sí existe y se prueba es que eliminar el club borra a
    todos los hermanos, también al que está en espera, y con ellos la cuenta que solo pertenecía a ese club.
    Cuando se construya la eliminación de la cuenta habrá que añadirle la prueba con hermanos.
- [X] T019 [P] Repasar la documentación de lo que cambió de responsabilidad (§3). Buscar con `Grep` en `backend/src/` y `frontend/src/` las frases que ya no son ciertas ("un integrante por cuenta", "el integrante de la cuenta en el club", "es de su cuenta" referido a la ficha, "todavía no existe" o "queda para" referido al hermano, "no incluye el club ni el rol" sin mencionar la limitación) y corregir cada comentario y cada documentación XML que las contenga, sin tocar código. Deben quedar revisados, como mínimo: `UsuarioRol.cs`, `EstadoIngreso.cs`, `IRepositorioPertenencias.cs`, `IntegranteDelClubAttribute.cs`, `ValidadorSesion.cs`, `ControladorBase.cs`, `IEmisorTokenSesion.cs`, `EmisorTokenSesion.cs`, `AccesoAFicha.cs`, `ClubDeSesionDto.cs`, `MapperSesion.cs`, `ServicioRechazoIngreso.cs`, `SalaDeEspera.tsx`, `SeccionSalaDeEspera.tsx` y `DisposicionClub.tsx`
- [X] T020 [P] Contrastar la API con `specs/006-agregar-hermano/contracts/api.yaml`: arrancar la API, abrir Swagger y comprobar el endpoint de hermanos (ruta, cuerpo, `201`, `200`, `400`, `403`, `404`, `409`, nombres de DTO), los campos nuevos de `ClubDeSesionDto` e `IngresoEnEsperaDto` y la cabecera `X-Jugador-Elegido` en las operaciones de club. Comprobar con respuestas reales que una sesión iniciada con documento no recibe ningún dato de un hermano. Corregir el código o, si el contrato tenía un error, el contrato, y anotar la diferencia debajo de esta tarea
  - **Resultado (T020)**: sin diferencias. Swagger expone 61 operaciones; `POST …/hermanos` con etiqueta
    "Hermanos", cuerpo `AgregarHermanoDto`, `201` y `200` con `JugadorDeSesionDto`, y `400`, `401`, `403`, `404`
    y `409`; `ClubDeSesionDto` trae `usuarioRolId` y `jugadores`; `IngresoEnEsperaDto` trae `hermanoDe`; y la
    cabecera opcional `X-Jugador-Elegido` está en las 39 operaciones de club y en ninguna otra. Con respuestas
    reales, la sesión iniciada con el documento de Ana no trae ni el nombre ni el identificador de sus hermanos.
- [X] T021 Revisar a 360 px de ancho, en tema claro y en oscuro, con la identidad de un club con colores propios (RF-033, CE-010; quickstart, bloque 4): la lista de jugadores con los tres estados, el menú con el nombre del jugador elegido y "Cambiar de jugador", la pantalla de ingreso pendiente y el aviso de retiro con la opción de cambiar, el diálogo "Agregar un hermano" con sus errores y su confirmación, y la sala de espera con "Hermano de …". Sin desplazamiento horizontal, con texto legible y con los estados leídos como texto, no solo por color. Si el lienzo de §24 (<https://claude.ai/artifact/PiDB5BQXRYCeN6nnB4TikR>) trae una pantalla de elección de jugador, contrastarla con él y anotar las diferencias debajo de esta tarea. Corregir lo que falle en los archivos de `frontend/src/privado/`
  - **Resultado (T021)**: revisadas a 360 px, en tema claro y oscuro y con colores propios del club, la lista de
    jugadores con los tres estados, el menú con el jugador elegido y "Cambiar de jugador", el ingreso pendiente,
    el aviso de retiro, el diálogo "Agregar un hermano" (vacío, con errores por campo, con documento repetido y
    con su confirmación) y la sala de espera con "Hermano de …". Ninguna tiene desplazamiento horizontal y los
    estados se leen como texto. Se corrigió el texto del ingreso pendiente, que mezclaba "tu ingreso" con el
    nombre del jugador: ahora dice "El ingreso de {nombres} está pendiente de aprobación".
  - **Lienzo de §24 (T021)**: ya se puede abrir. No trae ninguna pantalla de elección de jugador ni "Cambiar de
    jugador", así que no hay nada que contrastar para ellas. Diferencias vistas en pantallas que esta spec no
    cambia, anotadas para el propietario: `M-Espera` muestra una tarjeta con el jugador, su documento, su
    categoría por año y la fecha de registro, y tres pasos; la pantalla construida en la 002 solo tiene el texto
    y "Actualizar". Además dice que aprueba "el presidente o un directivo", que ya no es cierto desde la
    constitución 4.1.0. `M-Inicio` saluda al acudiente y muestra una tarjeta con un solo jugador.
- [X] T022 Recorrer `specs/006-agregar-hermano/quickstart.md`, bloques 1 a 4 y la tabla de comprobaciones directas contra la API, y ejecutar la batería entera (`dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test`, `scripts/verificar-tamano.ps1`). Levantar el recorrido en un proyecto de Compose aparte, con la llave de Brevo vacía y su propio volumen, para no enviar correos reales ni tocar los datos de desarrollo, y borrarlo al terminar. Anotar en este archivo, debajo de esta tarea, cualquier paso que no dé el resultado esperado, y corregirlo antes de marcarla

  - **Resultado (T022)**: recorrido en un proyecto de Compose aparte (`la-pecosa-validacion`, sin llave de Brevo
    y con su propio volumen, borrado al terminar). Los bloques 1 a 3 y la tabla de comprobaciones directas dieron
    el resultado esperado en sus 39 comprobaciones contra la API real; los pasos que son de pantalla (1.1 a 1.3,
    1.9, 2.1 a 2.3, 2.5, 2.6, 2.8 y el bloque 4) se comprobaron en la aplicación con el navegador sin interfaz.
    La API aplicó la migración `HermanosDeLaCuenta` al arrancar. Batería final: 258 pruebas unitarias, 554 de
    integración y 108 del frontend en verde; `tsc`, `eslint` y `scripts/verificar-tamano.ps1` sin errores.
    Ningún paso dio un resultado distinto del esperado.

---

# Primer incremento: el documento que ya usa otra cuenta (RF-034 a RF-040)

Decisiones en research.md, secciones 12 a 17, y en data-model.md, "Documento compartido entre
cuentas". Sin tablas, columnas, índices, migración ni endpoints nuevos: si una tarea parece
necesitarlos, se detiene y se informa (§25). El contrato ya está actualizado (`HermanoAgregadoDto`,
`retiraDeOtroClub`) y sigue en 61 endpoints: `AccesoClubPruebas` no cambia. Lo que cruza clubes
vive solo en `IRepositorioPertenencias.ListarPorDocumentosAsync` (lee) y en
`IRepositorioRetiroEntreClubes` (escribe), siempre acotado a números de documento (§7.1, cuarta
excepción de la 4.2.0). Ninguna respuesta lleva el nombre ni el identificador del otro club, ni el
correo, el nombre, el celular o el identificador de la otra cuenta (RF-038).

## Fase 7: Cimientos del primer incremento

**Propósito**: la regla, las ayudas de prueba y la lectura por número. Al terminar la fase nada
cambia para nadie: ningún documento puede quedar todavía en dos cuentas fuera de las pruebas.

- [X] T023 [P] Regla del documento compartido (research §12 y §15). Crear `backend/src/LaPecosa.Dominio/Reglas/DocumentoEnOtraCuenta.cs`, el resultado con tres casos (`SinBaja`, `ConBaja`, `NoAdmitido`), y `backend/src/LaPecosa.Dominio/Reglas/ReglaDocumentoCompartido.cs`, estática, sin dependencias de HTTP ni de EF, que recibe `IReadOnlyList<UsuarioRol> integrantesConEseNumero` (de cualquier club) y responde tres preguntas. (1) `Evaluar(integrantes, Guid cuentaQueAgrega)`: mira solo los integrantes con `UsuarioId != cuentaQueAgrega`; ninguno → `SinBaja`; alguno con `Rol != JUGADOR` → `NoAdmitido` (RF-039); solo jugadores y alguno `APROBADO` y `Activo` → `ConBaja` (RF-034); solo jugadores, todos retirados o en espera → `SinBaja`. (2) `ARetirar(integrantes, Guid cuentaQueQueda)`: los de otra cuenta con rol JUGADOR, `APROBADO` y `Activo`, en cualquier club; la misma cuenta en otro club nunca se retira (es el mismo niño en dos clubes). (3) `CuentaQueAbre(integrantes)`: el `UsuarioId` del integrante `APROBADO` y `Activo`; si no hay, del que está `EN_ESPERA`; si tampoco, del retirado; entre dos del mismo nivel, el de `CreadoEn` más reciente (supuesto 8); lista vacía → `null`. Documentación XML de las dos clases: qué decide, que no lee ni escribe datos y que la comprobación del propio club (`409 documento_repetido_en_club`) no es suya. Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaDocumentoCompartidoPruebas.cs`: la tabla de research §12 fila por fila; la misma cuenta ignorada en las tres preguntas; un PRESIDENTE, un DIRECTIVO y un ENTRENADOR de otra cuenta dan `NoAdmitido` aunque haya también un jugador activo; `ARetirar` con jugadores activos en varios clubes, con uno retirado y con uno en espera; `CuentaQueAbre` con cada nivel y cada desempate, y con una sola cuenta (el resultado de hoy)
- [X] T024 [P] Ayudas de prueba del incremento (quickstart, "Datos de partida", bloque 5). En `backend/pruebas/Integracion/Base/SembradorHermanos.cs`, añadir a `CrearHermanoAsync` el parámetro opcional `string? numeroDocumento = null` (con `null`, el documento único de hoy), para sembrar un hermano con el número de otra cuenta como lo dejará `POST …/hermanos` después de T029. Crear `backend/pruebas/Integracion/Hermanos/EscenarioDocumentoCompartido.cs`, sobre `EscenarioHermanos`: en el otro club, la categoría 2013 activa con un equipo; Caro (2013), con su propia cuenta y contraseña conocida, aprobada, en esa categoría y en el equipo, con ficha y documentos sembrados con `SembradorFichas`; Dani (2013), con su propia cuenta, aprobado; un tercer club con su presidente; `HermanoConDocumentoDeAsync(UsuarioRol otro, EstadoIngreso estado = EstadoIngreso.EN_ESPERA)`, un hermano de Ana en el Club A con el número de `otro`; y `LeerSinFiltroAsync(Guid usuarioRolId)`, que lee de la base de datos al integrante con sus equipos, su ficha y sus documentos sin el filtro del club, para comprobar qué cambió en el otro club. Las pruebas existentes no cambian
- [X] T025 Lectura por número y los sitios que ya preguntaban por el dueño del documento (research §16; depende de T024). Añadir a `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioPertenencias.cs` y `backend/src/LaPecosa.Infraestructura/Repositorios/Plataforma/RepositorioPertenencias.cs` `ListarPorDocumentosAsync(IReadOnlyCollection<string> numerosNormalizados)`: en una consulta, todos los integrantes con alguno de esos números, en todos los clubes (`IgnoreQueryFilters`, como las demás lecturas de ese repositorio), con su `ClubId`, `UsuarioId`, `Rol`, `EstadoIngreso`, `Activo` y `CreadoEn`; con la colección vacía no consulta. Su documentación XML dice que es la única lectura nueva que cruza clubes y que va siempre acotada a números de documento. Reescribir la documentación de `ObtenerCuentaPorDocumentoAsync`: con el número en dos cuentas devuelve una cualquiera, así que solo sirve para saber si el número tiene dueño (lo sigue usando así `ServicioRegistroConInvitacion`, que no cambia). En `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioIdentidadJugador.cs`, `ExigirNumeroLibreAsync` pasa a preguntar si **alguna** otra cuenta lo tiene: con `ListarPorDocumentosAsync([numero])`, `ErroresDeFicha.DocumentoEnOtraCuenta()` si algún integrante tiene `UsuarioId != jugador.UsuarioId`; mismo `409` en los mismos casos, y deja de depender de qué fila devuelva la consulta. Pruebas: en `backend/pruebas/Integracion/Ficha/CambioDeDocumentoPruebas.cs`, un caso con el número en dos cuentas (Caro y un hermano sembrado de Ana con su número): cambiar el documento de un tercer jugador a ese número responde `409 documento_en_otra_cuenta`, y cambiar el del hermano de Ana a su propio número sigue admitiéndose; en `backend/pruebas/Integracion/Cuenta/RegistroConInvitacionPruebas.cs`, un número que está en dos cuentas sigue dando `409` al registrarse. Las demás pruebas de las dos clases, en verde sin cambios
  - **Diferencia (T025)**: en `CambioDeDocumentoPruebas`, "cambiar el del hermano de Ana a su propio número" se prueba como cambiar solo el tipo conservando el número: el número no cambia y no pasa por la comprobación, que es lo que se quería mostrar.

**Punto de control**: la batería entera sigue en verde y ya se siembran documentos en dos cuentas

---

## Fase 8: Historia 2 - Un documento en dos cuentas abre una sola (Prioridad: P1)

**Objetivo**: entrar con un documento que está en varias cuentas abre la del jugador activo; con
la contraseña de otra, la respuesta de siempre a un dato incorrecto (RF-040, CE-012). Va antes de
admitir el documento: sin esto, un documento en dos cuentas abriría una cualquiera.

**Prueba independiente**: con el documento de Caro sembrado también en un hermano de Ana, entrar
con ese documento y la contraseña de la cuenta donde está activo abre esa cuenta; con la de la
otra, `401 credenciales_invalidas`.

- [X] T026 [P] [US2] El inicio de sesión elige la cuenta (`POST /api/sesion`; research §15; depende de T023 y T025). En `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioSesion.cs`, `BuscarCuentaAsync` sin arroba deja de usar `ObtenerCuentaPorDocumentoAsync`: lee `ListarPorDocumentosAsync([numeroNormalizado])`, aplica `ReglaDocumentoCompartido.CuentaQueAbre` y carga esa cuenta con `IRepositorioUsuarios`. La contraseña se compara solo con la de la cuenta elegida; no se prueba contra las demás. Si no coincide, el `401 credenciales_invalidas` de siempre, con el mismo texto, el mismo cálculo de hash y el fallo contado en la cuenta elegida (§12.4). La limitación del token sigue saliendo de `IdsDeLaCuentaConDocumentoAsync` con la cuenta elegida. Con el número en una sola cuenta el resultado es idéntico al de hoy. Actualizar la documentación XML de `ServicioSesion` e `IServicioSesion` (un documento puede estar en varias cuentas y abre la del jugador activo). Pruebas en `backend/pruebas/Integracion/Hermanos/SesionConDocumentoCompartidoPruebas.cs`, con `EscenarioDocumentoCompartido`: retirado en una cuenta y activo en otra, solo entra la contraseña de la activa (escenario 2.12); activo en una y en espera en otra, solo entra la de la activa y la otra familia entra con su correo y ve al hermano en espera (2.13); con la contraseña de la otra cuenta, `401` con el mismo cuerpo que una contraseña equivocada; el token queda limitado al jugador de la cuenta elegida y `GET /api/sesion` no trae ningún dato del jugador de la otra cuenta; los desempates del supuesto 8 (dos activos, dos en espera, todos retirados: la del más reciente); cinco intentos con la contraseña de la otra cuenta bloquean la cuenta elegida, como cualquier fallo. `backend/pruebas/Integracion/Cuenta/SesionPruebas.cs` y `backend/pruebas/Integracion/Hermanos/SesionConDocumentoPruebas.cs` siguen en verde sin cambios

**Punto de control**: el inicio de sesión ya sabe qué hacer con un documento en dos cuentas

---

## Fase 9: Historia 3 - El PRESIDENTE aprueba sabiendo que retira de otro club (Prioridad: P2)

**Objetivo**: la sala de espera avisa cuando el documento está activo en otro club; aprobar lo
retira de allí en la misma operación; rechazar no toca nada (RF-035 a RF-038, CE-011).

**Prueba independiente**: con un hermano sembrado con el documento de Caro, el PRESIDENTE del
Club A ve el aviso, lo aprueba y Caro queda retirada en el otro club con su ficha intacta; con otro
sembrado con el documento de Dani, lo rechaza y Dani no cambia.

- [X] T027 [P] [US3] Aprobar retira de otro club (`POST /api/clubes/{clubId}/ingresos/{usuarioRolId}/aprobacion`; research §13; data-model "Qué cambia en el jugador de la otra cuenta al aprobar"; depende de T023 y T025). Crear `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioRetiroEntreClubes.cs` y `backend/src/LaPecosa.Infraestructura/Repositorios/Plataforma/RepositorioRetiroEntreClubes.cs`, con dos operaciones: `BloquearClubesAsync(IReadOnlyCollection<Guid> clubIds)`, que bloquea todos en **una sola sentencia** ordenada por identificador (`SELECT 1 FROM "Clubes" WHERE "Id" = ANY(…) ORDER BY "Id" FOR UPDATE`, el mismo recurso que `RepositorioClub.BloquearAsync`), para que dos aprobaciones cruzadas no se esperen la una a la otra; y `RetirarDeOtrasCuentasAsync(string numeroDocumento, Guid cuentaQueQueda, DateTime retiradoEn)`, que, sin filtro de club, borra las filas de `JugadorEquipo` y, con una sentencia condicionada, pone `Activo = false`, `CategoriaId = NULL`, `RetiradoEn = retiradoEn`, `RetiradoPorUsuarioId = NULL` y `RetiradoPorNombre = NULL` (supuesto 6) a todo `UsuarioRol` con ese número, `UsuarioId != cuentaQueQueda`, rol JUGADOR, `APROBADO` y `Activo`; no toca `EstadoIngreso`, identidad, `FichaJugador`, `DocumentoJugador` ni `Usuario` (RF-037). Su documentación XML dice que es el único repositorio que escribe fuera del club de la petición y por qué (§7.1). Registrarlo en `backend/src/LaPecosa.Api/Configuracion/RegistroDeServicios.cs`, junto a `RepositorioPertenencias`. En `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioAprobacionIngreso.cs`, dentro de la misma transacción y en este orden: leer el ingreso (`404` si no existe, como hoy) y los integrantes con su número; si `Evaluar` da `NoAdmitido`, `ErroresDeFicha.DocumentoEnOtraCuenta()` sin cambiar nada (supuesto 7); bloquear con `BloquearClubesAsync` el club propio más los de `ARetirar` (sin nadie que retirar, solo el propio, como hoy) en lugar de `IRepositorioClub.BloquearAsync`; aprobar con la sentencia condicionada y ubicar como hoy; y llamar **siempre** a `RetirarDeOtrasCuentasAsync` con la hora de la aprobación. Si la baja falla, la aprobación se deshace con ella (RF-036). Reescribir la documentación XML del servicio. Pruebas en `backend/pruebas/Integracion/Hermanos/BajaEnOtroClubPruebas.cs`, con `EscenarioDocumentoCompartido`: aprobar al hermano con el documento de Caro lo deja aprobado y ubicado aquí y, en la misma operación, a Caro `Activo = false`, sin categoría, fuera del equipo, `RetiradoEn` igual a la hora de la aprobación y sin autor (escenario 3.10, CE-011); la ficha, los documentos, el estado de ingreso y la identidad de Caro, su cuenta, su hash de contraseña y los demás jugadores de su cuenta quedan idénticos y no se envía ningún correo (RF-037); el presidente del otro club la ve en sus retirados con la fecha y "Lo retiró" vacío; con el número activo en dos clubes de otras cuentas, quedan retirados en los dos; ya retirado allí, nada cambia; la misma cuenta activa en otro club no se retira; quien espera con ese número en otro club no cambia y sigue en su sala de espera (dos familias de clubes distintos con el mismo número); rechazar al hermano con el documento de Dani lo borra y Dani queda idéntico (3.11, RF-035); si antes de aprobar el número pasa a ser de un DIRECTIVO de otra cuenta, `409 documento_en_otra_cuenta` y nada cambia; si Caro se retira o se reincorpora entre agregar y aprobar, la baja se decide con lo que hay al aprobar; dos aprobaciones cruzadas a la vez con `Task.WhenAll` (A aprueba a alguien activo en B y B a alguien activo en A) terminan las dos; la familia de Caro entra con su correo, la elige y recibe `403 integrante_retirado`, y su cuenta no se borra (3.12). `backend/pruebas/Integracion/Ingresos/AprobacionPruebas.cs` y `backend/pruebas/Integracion/Hermanos/AprobarHermanoPruebas.cs` siguen en verde sin cambios
- [X] T028 [P] [US3] El aviso en la sala de espera (`GET /api/clubes/{clubId}/ingresos/en-espera`; research §14; depende de T023 y T025). Añadir al final de `backend/src/LaPecosa.Aplicacion/DTOs/IngresoEnEsperaDto.cs` `bool RetiraDeOtroClub` (obligatorio; "si aprobar este ingreso retirará a un jugador de otra cuenta en otro club"). En `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioConsultaIngresos.cs`, inyectar `IRepositorioPertenencias`, leer en **una** consulta (`ListarPorDocumentosAsync`) los integrantes con los números de todos los que esperan y, para cada ingreso, `RetiraDeOtroClub = Evaluar(…, ingreso.UsuarioId) == ConBaja`; se calcula al abrir y no se guarda (§13). `backend/src/LaPecosa.Aplicacion/Mappers/MapperIngresos.cs` recibe el booleano. Reescribir la documentación XML de `ServicioConsultaIngresos` (lee números en otros clubes, solo para el aviso). Frontend: `retiraDeOtroClub: boolean` en `IngresoEnEsperaDto` de `frontend/src/compartido/api/tipos.ts`; en `frontend/src/privado/ingresos/SeccionSalaDeEspera.tsx`, con `Aviso`, "Este documento está activo en otro club. Si apruebas su ingreso, quedará retirado de allí." en el ingreso y repetido en el diálogo de aprobar, solo cuando viene a `true`; legible a 360 px y en los dos temas. Pruebas en `backend/pruebas/Integracion/Hermanos/AvisoEnSalaDeEsperaPruebas.cs`: con el documento de Caro activa, `true` (escenario 3.9); con el de un jugador retirado o en espera de otra cuenta, con uno de la misma cuenta en otro club y con uno nuevo, `false`; si Caro se retira después de agregar, `false`, y si se reincorpora, otra vez `true` (caso límite); el cuerpo no contiene el nombre ni el identificador del otro club, ni el correo, el nombre, el celular o el identificador de la cuenta de Caro ni el de su integrante (RF-038); con varios ingresos se hace una sola lectura por números. `backend/pruebas/Integracion/Hermanos/HermanoDePruebas.cs` y `backend/pruebas/Integracion/Ingresos/SalaDeEsperaPruebas.cs` siguen en verde
  - **Diferencia con el plan (T028)**: el plan pone las pruebas del aviso en `BajaEnOtroClubPruebas`; van en un archivo propio para que T027 y T028 puedan hacerse en paralelo.
  - **Diferencia (T028)**: "con varios ingresos se hace una sola lectura por números" no tiene prueba propia: el servicio hace una sola llamada a `ListarPorDocumentosAsync` con todos los números, y la prueba comprueba que cada ingreso lleva su propio aviso.

**Punto de control**: con hermanos sembrados, el PRESIDENTE ve el aviso, aprueba con baja y
rechaza sin tocar el otro club (quickstart, bloque 5, pasos 6 a 11)

---

## Fase 10: Historia 1 - La familia agrega un hermano con el documento de otra cuenta (Prioridad: P1)

**Objetivo**: el `409 documento_en_otra_cuenta` deja de responderse cuando el documento es de un
JUGADOR de otra cuenta: el hermano queda en espera y la familia recibe el aviso (RF-034, RF-035,
RF-039). Va al final porque es la que abre la puerta a que un documento quede en dos cuentas.

**Prueba independiente**: con la cuenta de Ana, agregar un hermano con el documento de Caro
responde `201` con `retiraDeOtroClub: true` y Caro no cambia; con el del presidente del otro club,
`409`.

- [X] T029 [US1] Agregar con el documento de un jugador de otra cuenta (`POST /api/clubes/{clubId}/jugadores/{usuarioRolId}/hermanos`; research §12; depende de T026, T027 y T028). Crear `backend/src/LaPecosa.Aplicacion/DTOs/HermanoAgregadoDto.cs`: los cinco campos de `JugadorDeSesionDto` (`UsuarioRolId`, `Nombres`, `Apellidos`, `EstadoIngreso`, `Retirado`) más `bool RetiraDeOtroClub`, los seis obligatorios; `JugadorDeSesionDto` no cambia. `backend/src/LaPecosa.Aplicacion/Servicios/IServicioAgregarHermano.cs` y `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioAgregarHermano.cs` devuelven `HermanoAgregadoDto`. En el servicio, dentro de la transacción y después de la comprobación del propio club, que no cambia (`409 documento_repetido_en_club`), sustituir `ObtenerCuentaPorDocumentoAsync` por `ListarPorDocumentosAsync([numero])` y `ReglaDocumentoCompartido.Evaluar(…, cuenta del jugador de origen)`: `NoAdmitido` → `ErroresDeFicha.DocumentoEnOtraCuenta()` y nada creado (RF-039); `ConBaja` → crea al hermano como hoy y `RetiraDeOtroClub = true`; `SinBaja` → `false`. Agregar no escribe nada en el otro club (RF-035). El `200` que devuelve al hermano que ya esperaba (supuesto 2) calcula el aviso en ese momento. Reescribir la documentación XML del servicio y, en `backend/src/LaPecosa.Aplicacion/Utilidades/ErroresDeFicha.cs`, la de `DocumentoEnOtraCuenta` (al agregar un hermano, solo cuando la otra cuenta tiene un integrante que no es JUGADOR). `backend/src/LaPecosa.Api/Controladores/Club/ControladorHermanos.cs` declara `HermanoAgregadoDto` en el `201` y el `200`. Frontend: `HermanoAgregadoDto` en `frontend/src/compartido/api/tiposSesion.ts`; en `frontend/src/privado/ficha/DialogoAgregarHermano.tsx` y `frontend/src/privado/ficha/FichaJugador.tsx`, cuando la respuesta trae `retiraDeOtroClub`, junto a "El ingreso de {nombres} está pendiente de aprobación del club" el aviso "Si el club lo aprueba, dejará de estar en el otro club donde está inscrito." (supuesto 10); el `409 documento_en_otra_cuenta` se sigue mostrando como aviso; sin desplazamiento horizontal a 360 px. Pruebas: en `backend/pruebas/Integracion/Hermanos/AgregarHermanoRechazosPruebas.cs`, reescribir `El_documento_de_otra_cuenta_en_otro_club_responde_409_y_el_de_la_misma_cuenta_se_admite`: el documento de un JUGADOR de otra cuenta, `201`; el de un PRESIDENTE, `409 documento_en_otra_cuenta`; el de la misma cuenta en otro club, `201`. Crear `backend/pruebas/Integracion/Hermanos/DocumentoDeOtraCuentaPruebas.cs`, con `EscenarioDocumentoCompartido`: con el documento de Caro, `201`, `retiraDeOtroClub: true`, el hermano en espera con el número normalizado y Caro idéntica (categoría, equipo, `Activo`, ficha y documentos) (escenario 1.11, RF-035); con el de un jugador retirado o en espera de otra cuenta, `201` y `false`; con el de un PRESIDENTE, un DIRECTIVO o un ENTRENADOR de otra cuenta, en cualquier club, `409 documento_en_otra_cuenta` y nada creado (1.12, RF-039); con el de la misma cuenta en otro club, `201` y `false`; la misma petición dos veces, `201` y después `200` con el aviso calculado en el segundo momento (Caro retirada entre las dos: `false`); el cuerpo de la respuesta no contiene el nombre ni el identificador del otro club ni datos de la cuenta de Caro; después, la sala de espera del Club A trae ese ingreso con `retiraDeOtroClub: true`. Las demás pruebas de la 006, en verde

**Punto de control**: el primer incremento funciona de extremo a extremo (quickstart, bloque 5)

---

## Fase 11: Cierre del primer incremento

- [X] T030 [P] Repasar la documentación de lo que cambió de responsabilidad (§3). Buscar con `Grep` en `backend/src/` y `frontend/src/` las frases que ya no son ciertas ("la cuenta de ese número", "el dueño del documento", "documento de otra cuenta" referido al `409` al agregar, "solo retira su propio club", "un documento, una cuenta") y corregir cada comentario y cada documentación XML que las contenga, sin tocar código. Deben quedar revisados, como mínimo: `IRepositorioPertenencias.cs`, `RepositorioPertenencias.cs`, `ServicioSesion.cs`, `ServicioIdentidadJugador.cs`, `ServicioAgregarHermano.cs`, `ServicioAprobacionIngreso.cs`, `ServicioConsultaIngresos.cs`, `ErroresDeFicha.cs`, `IngresoEnEsperaDto.cs`, `ControladorHermanos.cs`, `UsuarioRol.cs` (el número puede estar en dos cuentas, en clubes distintos) y `SeccionSalaDeEspera.tsx`
- [X] T031 Contrato, pantallas y recorrido del primer incremento (depende de T029 y T030). Arrancar la API y contrastar Swagger con `specs/006-agregar-hermano/contracts/api.yaml`: siguen siendo 61 operaciones; `POST …/hermanos` declara `HermanoAgregadoDto` en `201` y `200`; `IngresoEnEsperaDto` trae `retiraDeOtroClub`. Revisar a 360 px, en tema claro y en oscuro y con colores propios del club, el aviso tras agregar y el de la sala de espera con su diálogo de aprobar (quickstart, bloque 5, paso 12). Recorrer el bloque 5 y su tabla de comprobaciones directas en un proyecto de Compose aparte, con la llave de Brevo vacía y su propio volumen, y borrarlo al terminar; comprobar con respuestas reales que ninguna trae el nombre del otro club ni datos de la otra cuenta. Ejecutar la batería entera (`dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test`, `tsc`, `eslint`, `scripts/verificar-tamano.ps1`). Anotar debajo de esta tarea cualquier diferencia o paso que no dé lo esperado, y corregirlo antes de marcarla
  - **Resultado (T031 y T037, juntos)**: Swagger con 61 operaciones, `HermanoAgregadoDto` en el `201` y el `200` de `POST …/hermanos`, `retiraDeOtroClub` en `IngresoEnEsperaDto`, `404` en el retiro y `409` en la reincorporación. Recorrido de los bloques 5 y 6 y de sus tablas de comprobaciones directas en un proyecto de Compose aparte (`la-pecosa-validacion`, puertos 8081 y 5174, sin llave de Brevo, volumen propio borrado al terminar), con un script contra la API real: 35 comprobaciones en verde, dos pasadas. La API aplicó las migraciones al arrancar. Pantallas a 360 px en claro y oscuro con Chrome sin interfaz: aviso en la sala de espera y en su diálogo de aprobar, aviso tras agregar, botón y diálogo "Retirar del club" y aviso de retiro; todo sin desplazamiento horizontal. Se corrigió el texto del diálogo de retiro, que decía que los hermanos "siguen en el club" aunque estuvieran en espera: ahora dice que "no cambian". No se capturó el mensaje del `409` en "Retirados" (`SeccionRetirados.tsx` no cambia y ya muestra el motivo de la API). Batería: 280 unitarias, 589 de integración y 108 del frontend en verde; `tsc`, `eslint` y `scripts/verificar-tamano.ps1` sin errores.
  - **Diferencia (T031)**: `AccesoClubPruebas.La_api_expone_exactamente_los_endpoints_del_contrato` falla mientras exista `specs/007-jugadores-inicio-por-rol/contracts/api.yaml` sin construir (68 del contrato frente a 61 de la API). Con ese archivo apartado, la batería entera da verde. Se resuelve al implementar la 007.

---

# Segundo incremento: la familia retira y la reincorporación (RF-041 a RF-043)

Decisiones en research.md, secciones 18 a 22, y en data-model.md, "Retiro por la familia y
reincorporación". Sin tablas, columnas, migración ni endpoints nuevos; el contrato sigue en 61
endpoints y ya describe el cambio de `…/retiro` y el `409 documento_activo_en_otro_club` de
`…/reincorporacion`. Reutiliza `ListarPorDocumentosAsync`, `IRepositorioRetiroEntreClubes` y
`ReglaDocumentoCompartido` del primer incremento.

**Diferencia con el plan**: el plan y el quickstart llaman a estos escenarios "2.13 a 2.16", pero en
spec.md son los escenarios 13 a 16 de la **historia 3** (el 13 de la historia 2 es el del inicio de
sesión). Las tareas usan la numeración de spec.md (3.13 a 3.16) y la etiqueta [US3].

## Fase 12: Historia 3 - La familia retira a su jugador y la reincorporación se niega (Prioridad: P2)

**Objetivo**: la cuenta de un jugador aprobado y activo lo retira desde su ficha; solo ella y el
PRESIDENTE retiran, y solo el PRESIDENTE reincorpora (RF-042, RF-043); la reincorporación se niega
mientras el documento esté activo con otra cuenta (RF-041, CE-013).

**Prueba independiente**: con Caro retirada en el otro club por la baja de T027, su presidente no
puede reincorporarla (`409`); la familia de Ana retira desde la ficha al hermano que tiene el
documento de Caro y entonces el presidente del otro club sí la reincorpora.

- [X] T032 [P] [US3] Regla de quién retira (research §18). Crear `backend/src/LaPecosa.Dominio/Reglas/ReglaQuienRetira.cs`, estática, sin dependencias de HTTP ni de EF: `PuedeRetirar(UsuarioRol quienRetira, Guid usuarioRolId)`; el PRESIDENTE, a cualquiera; un integrante con rol JUGADOR, solo si `usuarioRolId == quienRetira.Id` (el jugador de la petición: el elegido o el de la sesión con documento, supuesto 13); cualquier otro rol, a nadie. No mira el estado del jugador que se retira: eso lo sigue haciendo el servicio. Documentación XML (qué decide y qué no). Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaQuienRetiraPruebas.cs`: PRESIDENTE con su propio identificador y con otro; JUGADOR consigo mismo, con un hermano y con un ajeno; DIRECTIVO, ENTRENADOR y DESARROLLADOR
- [X] T033 [US3] La familia retira a su jugador (`POST /api/clubes/{clubId}/jugadores/{usuarioRolId}/retiro`; research §18 y §20; depende de T032). En `backend/src/LaPecosa.Api/Controladores/Club/ControladorJugadores.cs`, el retiro pasa a `[IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]`; la reincorporación sigue en `[IntegranteDelClub(Rol.PRESIDENTE)]` (RF-043). En `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioRetiroJugador.cs`, `RetirarAsync` aplica `ReglaQuienRetira.PuedeRetirar` **antes** de abrir la transacción y de leer al jugador; si no puede, `ExcepcionDeAplicacion.NoEncontrado()` (`404`), para que la respuesta no dependa de si el jugador existe ni de su estado (§7.5). El resto no cambia: mismo efecto, mismo bloqueo, misma idempotencia, y quien retira se copia como siempre, así que si es la familia "Lo retiró" muestra el nombre del propio jugador (supuesto 11). Reescribir la documentación XML de `backend/src/LaPecosa.Aplicacion/Servicios/IServicioRetiroJugador.cs`, `ServicioRetiroJugador.cs` y `ControladorJugadores.cs` (también retira la familia, solo a su jugador; solo el PRESIDENTE reincorpora). Pruebas: en `backend/pruebas/Integracion/Categorias/RetiroJugadorPruebas.cs`, ajustar `Solo_el_presidente_de_ese_club_retira_y_reincorpora_y_el_directivo_solo_ve_la_lista`, que cuenta al jugador entre quienes no retiran: el jugador se retira a sí mismo con `204` y a otro jugador le responde `404`; el DIRECTIVO y el ENTRENADOR siguen en `403`; las demás pruebas de la clase, sin cambios. Crear `backend/pruebas/Integracion/Hermanos/FamiliaRetiraPruebas.cs`, con `EscenarioHermanos`: con Ana elegida, `POST …/retiro` sobre Ana responde `204`; queda `Activo = false`, sin categoría y fuera de sus equipos, con `RetiradoPorUsuarioId` su cuenta y `RetiradoPorNombre` su nombre; su ficha y sus documentos, la cuenta (correo, celular, responsable, hash) y sus hermanos no cambian (escenario 3.14, RF-042); la siguiente petición con su cabecera responde `403 integrante_retirado` y la sesión la trae como retirada; el PRESIDENTE la ve en sus retirados con su nombre en "Lo retiró"; con Ana elegida, retirar a su hermano aprobado o a Beto responde `404` y no cambian (3.16, RF-043); el DIRECTIVO y el ENTRENADOR, `403 rol_no_autorizado`; con un hermano en espera elegido, `403 ingreso_en_espera`; en una sesión iniciada con el documento de Ana, retirarla responde `204`; la familia que intenta `POST …/reincorporacion` sobre su jugador retirado recibe `403 rol_no_autorizado` (RF-043)
  - **Diferencia (T033)**: también cambió `backend/pruebas/Integracion/Categorias/SoloPresidentePruebas.cs`, que recorre todos los endpoints de escritura esperando `403` para el jugador; el retiro sale de ese recorrido (como ya salía agregar un hermano) y quedan 15 endpoints. Quién retira se prueba en `RetiroJugadorPruebas` y `FamiliaRetiraPruebas`.
- [X] T034 [P] [US3] Pantalla: "Retirar del club" en la ficha propia (research §21; depende de T033). Crear `frontend/src/privado/ficha/BotonRetirarseDelClub.tsx`: `Boton` y `DialogoConfirmacion` en modo peligro, como el retiro del PRESIDENTE (`BotonRetirarJugador.tsx` no se reutiliza: recibe otro tipo de jugador); el texto dice que el jugador deja de entrar a este club, que sus datos se conservan, que solo el club puede reincorporarlo y, si `club.jugadores` trae otros, que ellos no cambian; al confirmar, `POST …/retiro` sobre el jugador de la ficha; el botón queda deshabilitado mientras se envía; un error de la API se muestra con `Aviso`; al terminar bien, recarga la sesión, y `DisposicionClub` muestra el `AvisoRetirado` que ya existe, con "Cambiar de jugador" si hay otros (escenario 3.14). En `frontend/src/privado/ficha/FichaJugador.tsx`, mostrarlo con la misma condición que "Agregar un hermano": `club.miRol === 'JUGADOR'` y la ficha es la de `club.miUsuarioRolId` (RF-043): un PRESIDENTE, un DIRECTIVO, un ENTRENADOR y la ficha de un hermano no elegido no lo muestran. `FichaJugador.tsx` no debe pasar de 250 líneas. Sin desplazamiento horizontal a 360 px y legible en los dos temas
- [X] T035 [US3] La reincorporación se niega con el documento activo en otra cuenta (`POST /api/clubes/{clubId}/jugadores/{usuarioRolId}/reincorporacion`; research §19; depende de T023, T025, T027 y T033). En `backend/src/LaPecosa.Dominio/Reglas/ReglaDocumentoCompartido.cs`, la cuarta pregunta: `HayOtraCuentaActiva(integrantes, Guid cuenta)`, si algún integrante con `UsuarioId != cuenta` está `APROBADO` y `Activo`, con **cualquier** rol; en espera no cuenta (supuesto 12); la misma cuenta no cuenta. Añadir sus casos a `backend/pruebas/Unitarias/Reglas/ReglaDocumentoCompartidoPruebas.cs` (otra cuenta activa, en espera y retirada; la misma cuenta activa; otro rol activo). En `backend/src/LaPecosa.Aplicacion/Utilidades/ErroresDeCategorias.cs`, `DocumentoActivoEnOtroClub()`: `409 documento_activo_en_otro_club`, "Este documento está activo en otro club. Para reincorporarlo, primero debe retirarse de allí, por su presidente o por su familia." En `ServicioRetiroJugador.ReincorporarAsync`, dentro de la transacción y en este orden: leer al retirado (`404` si no existe); leer con `ListarPorDocumentosAsync` los integrantes con su número; bloquear con `IRepositorioRetiroEntreClubes.BloquearClubesAsync` el club propio más los de esos integrantes, en una sola sentencia y en orden (sin documento compartido, solo el propio, como hoy), en lugar de `IRepositorioClub.BloquearAsync`; **volver a leer** los integrantes con ese número; si `HayOtraCuentaActiva`, `DocumentoActivoEnOtroClub()` y nada cambia (RF-041); si no, reincorporar y ubicar como hoy. El motivo no nombra el club ni da datos de la otra cuenta. Reescribir la documentación XML de `ServicioRetiroJugador` e `IServicioRetiroJugador`; `ServicioRetiroJugador.cs` no debe pasar de 250 líneas. `frontend/src/privado/categorias/SeccionRetirados.tsx` ya muestra el mensaje del `409`: no cambia. Pruebas en `backend/pruebas/Integracion/Hermanos/ReincorporarConDocumentoCompartidoPruebas.cs`, con `EscenarioDocumentoCompartido`: con Caro retirada por la baja de T027 y el hermano de Ana activo en el Club A, su presidente recibe `409 documento_activo_en_otro_club`, Caro sigue retirada y el cuerpo no contiene el nombre ni el identificador del Club A, ni el correo, el nombre o el identificador de la cuenta de Ana (escenario 3.13, RF-041); tras retirar la familia de Ana a ese hermano (T033), la reincorporación de Caro responde `200` y queda en la categoría de su año, sin el equipo que tenía (3.15); después, el PRESIDENTE del Club A recibe `409` al reincorporar al hermano (CE-013); con el número activo solo en la misma cuenta en otro club, `200`; con un hermano de otra cuenta en espera con ese número, `200`, y si después se aprueba, la baja retira al reincorporado (supuesto 12); dos reincorporaciones a la vez con `Task.WhenAll`, en clubes distintos, de cuentas distintas y con el mismo número, dejan exactamente una activa (una `200` y la otra `409`); una reincorporación y una aprobación con baja a la vez sobre el mismo número terminan sin abrazo mortal y nunca con dos cuentas activas. Las pruebas de reincorporación de `backend/pruebas/Integracion/Categorias/RetiroJugadorPruebas.cs` siguen en verde sin cambios

**Punto de control**: el segundo incremento funciona de extremo a extremo (quickstart, bloque 6)

---

## Fase 13: Cierre del segundo incremento

- [X] T036 [P] Repasar la documentación de lo que cambió de responsabilidad (§3). Buscar con `Grep` en `backend/src/` y `frontend/src/` las frases que ya no son ciertas ("solo el PRESIDENTE retira", "solo lo retira su club", "la reincorporación no mira el documento", "dos formas de retiro") y corregir cada comentario y documentación XML que las contenga, sin tocar código. Deben quedar revisados, como mínimo: `IServicioRetiroJugador.cs`, `ServicioRetiroJugador.cs`, `ControladorJugadores.cs`, `ReglaDocumentoCompartido.cs`, `ErroresDeCategorias.cs`, `IRepositorioRetiroEntreClubes.cs`, `UsuarioRol.cs` (las tres formas de retiro), `FichaJugador.tsx` y `SeccionRetirados.tsx`
- [X] T037 Contrato, pantallas y recorrido del segundo incremento (depende de T034, T035 y T036). Arrancar la API y contrastar Swagger con `specs/006-agregar-hermano/contracts/api.yaml`: siguen siendo 61 operaciones; `POST …/retiro` admite al JUGADOR y declara `404`; `POST …/reincorporacion` declara el `409`. Revisar a 360 px, en tema claro y en oscuro y con colores propios del club, el botón "Retirar del club", su diálogo, el aviso de retiro después y el mensaje del `409` en "Retirados" (quickstart, bloque 6, paso 9). Recorrer el bloque 6 y su tabla de comprobaciones directas en un proyecto de Compose aparte, con la llave de Brevo vacía y su propio volumen, y borrarlo al terminar. Ejecutar la batería entera (`dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test`, `tsc`, `eslint`, `scripts/verificar-tamano.ps1`). Anotar debajo de esta tarea cualquier diferencia o paso que no dé lo esperado, y corregirlo antes de marcarla

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (fase 1)**: sin dependencias.
- **Cimientos (fase 2)**: depende de la fase 1. Bloquea todas las historias.
- **Historia 2 (fase 3)**: depende de los cimientos. Se prueba con hermanos sembrados.
- **Historia 1 (fase 4)**: depende de la historia 2 (T008 para el DTO de respuesta, T010 para la
  pantalla). No se entrega sin ella.
- **Historia 3 (fase 5)**: T015 y T016 dependen solo de los cimientos; T017 depende de la
  historia 1 (T012).
- **Cierre (fase 6)**: depende de las tres historias.

### Entre tareas

```text
T001 ─┬─ T002 ───────────┐
      ├─ T003 ─┐         ├─ T007 ─┬─ T008 ─┬─ T009 ───────────────┐
      ├─ T004 ─┴─ T005 ──┘        │        ├─ T010 ─┐             │
      └─ T006 ────────────────────┼─ T011  └─ T012 ─┼─ T014       ├─ T018, T019, T020 ── T021 ── T022
                                  │                 ├─ T013       │
                                  │                 └─ T017 ──────┤
                                  └─ T015, T016 ──────────────────┘
```

- T003 y T004 → T005: el atributo aplica la regla con la limitación que deja el validador.
- T008 → T009: las dos cambian `ServicioSesion`, `IServicioSesion` y `ControladorSesion`.
- T008 → T012: el endpoint devuelve `JugadorDeSesionDto`.
- T010 → T014: el diálogo guarda la elección con `jugadorElegido.ts`.
- T012 es la única tarea que toca el contrato y los contadores de `AccesoClubPruebas.cs`.

### Primer incremento (T023 a T031)

Empieza con T001 a T022 terminadas.

```text
T023 ──────────────┬─ T026 (US2) ─┐
T024 ── T025 ──────┼─ T027 (US3) ─┼─ T029 (US1) ─┬─ T031
                   └─ T028 (US3) ─┘              T030 ┘
```

- T023 y T025 → T026, T027, T028: los tres usan la regla y la lectura por número.
- T026 → T029: el inicio de sesión tiene que elegir la cuenta antes de que un documento pueda
  quedar en dos cuentas fuera de las pruebas.
- T027 y T028 → T029: la aprobación ya sabe retirar y la sala de espera ya avisa cuando agregar
  empieza a admitir el documento.
- T024 → T025: los casos nuevos de la 005 y la 004 siembran el número en dos cuentas.

### Segundo incremento (T032 a T037)

Empieza con el primer incremento terminado: reutiliza la lectura por número, el bloqueo ordenado
de clubes y `ReglaDocumentoCompartido`.

```text
T032 ── T033 ─┬─ T034 ─────────┐
              └─ T035 ─ T036 ──┴─ T037
```

- T033 → T035: las dos cambian `ServicioRetiroJugador.cs`, y las pruebas de T035 retiran desde la
  familia.
- T027 → T035: la reincorporación bloquea con `IRepositorioRetiroEntreClubes` y sus pruebas parten
  de la baja al aprobar.
- T036 puede empezar en cuanto termine T035; T034 es independiente de T035.

### Dentro de cada tarea

- Entidad o DTO → repositorio → servicio → controlador → contrato → pantalla → pruebas.
- La batería completa en verde antes de pasar a la tarea siguiente.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos: archivos distintos, sin dependencias entre sí
T002  UsuarioRol.cs, su configuración y la migración
T003  ReglaJugadorDeLaSesion.cs y sus pruebas
T004  emisor, validador y ControladorBase
T006  AccesoAFicha.cs y ReglaAccesoAFicha.cs

# Historia 2, tras T008
T009  sesión con documento (backend)
T010  pantallas de elección (frontend)
T011  pruebas de la ficha entre hermanos

# Historia 1, tras T012
T013  pruebas de quién agrega y del hermano en espera
T014  botón y diálogo en la ficha

# Historia 3: no comparten archivos
T015  hermanoDe en la sala de espera
T016  el rechazo conserva las invitaciones

# Cierre
T018  pruebas transversales    T019  documentación    T020  contrato frente a Swagger

# Primer incremento: cimientos
T023  ReglaDocumentoCompartido y sus pruebas
T024  sembrador y EscenarioDocumentoCompartido

# Primer incremento, tras T025: no comparten archivos
T026  ServicioSesion (US2)
T027  ServicioAprobacionIngreso y RepositorioRetiroEntreClubes (US3)
T028  ServicioConsultaIngresos, IngresoEnEsperaDto y SeccionSalaDeEspera (US3)

# Segundo incremento
T032  ReglaQuienRetira (mientras se cierra el primer incremento)
T034  BotonRetirarseDelClub, tras T033, a la vez que T035
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 2 (T008 a T011) e historia 1 (T012 a T014).
3. **Parar y validar**: quickstart, bloques 1 y 2. El hermano agregado queda en espera y se
   aprueba con la sala de espera de la 004, que ya funciona sin cambios.

### Entrega incremental

1. Cimientos → nada cambia para nadie; la batería anterior en verde.
2. Historias 2 y 1, juntas → la familia agrega al hermano y elige con cuál continuar.
3. Historia 3 → el PRESIDENTE ve de quién es hermano y el rechazo no borra la invitación del
   primer hijo.
4. Cierre → pruebas transversales, documentación, contrato, 360 px y quickstart completo.

Cada paso deja la batería en verde y no rompe lo anterior. **Antes de la historia 3 no conviene
rechazar a un hermano en un entorno con datos reales**: el rechazo todavía borraría las
invitaciones del club al correo de la familia (lo corrige T016).

### Los incrementos

5. **Primer incremento** (T023 a T031), en el orden del plan: regla y lecturas → inicio de sesión
   → aprobar con baja y aviso → agregar → cierre. Hasta T029 ningún documento puede quedar en dos
   cuentas fuera de las pruebas, así que cada paso se puede publicar sin cambiar nada para nadie.
   **No se publica T029 sin T026, T027 y T028**: abriría una cuenta cualquiera al entrar con el
   documento o aprobaría sin retirar del otro club.
6. **Segundo incremento** (T032 a T037): la familia retira (T032 a T034) y la reincorporación se
   niega (T035). **Conviene publicarlos juntos, o T035 antes de que haya bajas reales**: entre la
   publicación de T029 y la de T035, el PRESIDENTE del club anterior puede reincorporar a un
   jugador retirado por la baja y dejar su documento activo en dos cuentas (supuesto 9 antes de
   la decisión del 2026-10-10).

---

## Notas

- La API aplica la migración `HermanosDeLaCuenta` al arrancar; no cambia ningún dato existente.
- Sesiones abiertas al desplegar: un token anterior no lleva limitación. Quien había entrado con
  un documento y después agrega un hermano elige, hasta que vuelva a entrar, como si hubiera
  entrado con el correo. Es una consecuencia asumida en el plan, no una tarea.
- Cuenta con varios jugadores que acepta una invitación de PRESIDENTE: el más antiguo pasa a
  PRESIDENTE y los demás siguen como JUGADOR. El plan no lo cambia (toca §28) y ninguna tarea lo
  resuelve; si aparece al probar, se anota y se pregunta.
- La cabecera `X-Jugador-Elegido` es obligatoria para las cuentas con hermanos: cualquier cliente
  futuro de la API tendrá que enviarla.
- Si una prueba de la 001 a la 005 falla al tocar `IntegranteDelClubAttribute`, `ServicioSesion`,
  `MapperSesion` o `AccesoAFicha` y no está claro si es una regresión o un cambio previsto, se
  detiene esa parte y se pregunta (§25).
- Confirmar el cambio en git al terminar cada tarea o cada historia.
