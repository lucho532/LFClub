---

description: "Lista de tareas de la funcionalidad 011: comunicados del club y campana de notificaciones"
---

# Tareas: Comunicados del club y campana de notificaciones

**Entrada**: documentos de diseño en `specs/011-comunicados-y-campana/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
contracts/campana.md, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.9.0) y el plan (tabla de
trazabilidad). Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa
contra la API real y sus pruebas; las tareas que solo traen pruebas o solo pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario en orden de prioridad: historias 1
y 2 (P1), historia 3 (P2), historia 4 (P3), y después el cierre. La historia 2 necesita
comunicados publicados para tener algo que mostrar, pero **no depende del código de la historia 1**:
sus pruebas siembran los comunicados con `SembradorComunicados` (T010).

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 010, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): tres tablas nuevas (`Comunicados`, `ComunicadosDestino` y
  `ComunicadosDestinatario`) y una sola migración, `ComunicadosYCampana` (T004). Si una tarea
  posterior parece necesitar otra tabla, columna o índice, se detiene y se informa (§25).
- **Contrato**: `specs/011-comunicados-y-campana/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs.
  T001 deja comentadas las 8 operaciones nuevas y cada tarea que construye una la descomenta y
  sube los contadores de `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` (contrato
  y endpoints de club). Recorrido: 99/75 → 100/76 (T013) → 101/77 (T014) → 102/78 y 103/79 y
  104/80 (T016) → 105/81 y 106/82 (T018) → 107/83 (T020). **Corrección al implementar**: el punto de
  partida real de la 001 a la 010 es 100/76 (el contrato de la 009 incluye `PUT …/rival`, que los
  contadores de la 010 no recogían), así que el cierre es 108/84.
- **Lo anterior no cambia**: ningún endpoint existente cambia (api.yaml, cabecera). Si una prueba
  de la 001 a la 010 falla y no está claro que sea una regresión, se detiene y se pregunta (§27.2).
- **La familia nunca ve a los demás** (RF-032, contrato): `ComunicadoRecibidoDto` no lleva
  audiencia, destinos ni cuántas familias lo recibieron; `familiasAlcanzadas` y `destinos` solo
  existen en `ComunicadoPublicadoDto`, que una cuenta de jugador no recibe nunca (403). Las pruebas
  lo comprueban sobre el **texto** de la respuesta. "Su jugador" se evalúa con el **integrante de
  la petición** (cabecera `X-Jugador-Elegido`), y una sesión limitada a un jugador solo ve las
  filas de ese jugador (research §2).
- **Leído por cuenta** (RF-022): leer es poner `LeidoEn` en **todas** las filas de la cuenta para
  ese comunicado, solo donde es nulo (la primera lectura no se mueve). Contador y lista cuentan
  comunicados **distintos** (RF-013).
- **Jugador activo** (research §3): rol `JUGADOR`, ingreso `APROBADO` y `RetiradoEn` nulo. Al
  publicar fija los destinatarios; al leer, una fila solo cuenta si su jugador sigue activo.
  Nunca se crean ni se borran filas de destinatarios después de publicar.
- **Sin avisos fuera de la aplicación** (RF-028, RF-029): ni publicar ni retirar llaman a
  `IServicioCorreo` ni a ningún servicio externo, no se crea tabla de notificaciones genéricas, no
  se pide permiso de notificaciones del navegador. La campana sondea (research §10).
- **Texto plano** (RF-036): el texto se guarda recortado y se pinta como texto de React con
  `white-space: pre-wrap`. Nunca `dangerouslySetInnerHTML`.
- **Nombres** (§2.1, §2.2, RF-035): todo en español; "familia" solo en textos de pantalla, el rol
  sigue siendo JUGADOR. Los nombres de personas usan `NombrePersona` con el `EnlaceFichaDto` de
  `ResolutorDeEnlaces` (008); si `usuarioRolId` del autor es nulo, se muestra el nombre copiado sin
  enlace.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) con qué representa, su responsabilidad y qué no hace; se amplía la de `UsuarioRol`,
  `Categoria` y `Equipo`.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Publicación, consulta y
  lectura van en tres servicios desde el principio (plan). Si `DisposicionClub.tsx`,
  `MenuDelClub.tsx` o `App.tsx` pasaran de 250 con el cambio, lo nuevo va a un archivo propio.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Controladores con
  `[IntegranteDelClub]` que solo delegan; reglas en `Dominio/Reglas`, puras.
- **Aislamiento** (§7.1): todo bajo `/api/clubes/{clubId}` con el filtro de club activo; las tres
  entidades implementan `IPerteneceAClub`.
- **Pantallas** (RF-033, CE-007, §24): componentes `Tarjeta`, `Aviso`, `Boton`, `Campo`,
  `DialogoConfirmacion`; legibles a 360 px y en los dos temas, sin desplazamiento horizontal;
  todo estado escrito con texto ("Sin leer", "Leído", "Publicado", "Retirado").
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

Son técnicos y no tocan ninguna regla de la spec (research §17). Si cambia alguno, cambia solo la
regla o constante indicada, no el modelo.

| # | Supuesto | Dónde |
| --- | --- | --- |
| 1 | El contador se consulta cada 60 segundos mientras la aplicación está visible | `useContadorComunicados.ts` (T017) |
| 2 | Un ENTRENADOR dirige a cualquier equipo activo de sus categorías, no solo a los que dirige | `ReglaAudienciaDeComunicado` (T006), T013 |
| 3 | Un comunicado con alcance `EQUIPOS` admite equipos de una sola categoría | `ReglaAudienciaDeComunicado` (T006), T013 |
| 4 | Las listas usan cursor por identificador y 30 por página | T012, T014, T016 |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: que la batería siga en verde con el contrato de la 011 ya en `specs/`.

- [X] T001 Comentar en `specs/011-comunicados-y-campana/contracts/api.yaml` las 8 operaciones nuevas, cada una con una marca `# 011-T0NN` que diga qué tarea la descomenta: `POST /comunicados` (T013); `GET /comunicados` (T014); `GET /comunicados/recibidos` (T016); `GET /comunicados/recibidos/contador` (T016); `GET /comunicados/recibidos/{comunicadoId}` (T016); `PUT /comunicados/recibidos/{comunicadoId}/lectura` (T018); `POST /comunicados/recibidos/lectura-total` (T018); `POST /comunicados/{comunicadoId}/retiro` (T020). Como `POST` y `GET /comunicados` comparten ruta, comentar cada operación y no la ruta. Comprobar que `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` sigue en 99 y 75 y que la batería entera pasa

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: esquema, reglas puras con sus pruebas unitarias, errores, DTOs, tipos del frontend y
ayudas de prueba. Al terminar, la migración se aplica y nada cambia para nadie.

**⚠️ CRÍTICO**: ninguna historia empieza antes de terminar esta fase.

- [X] T002 [P] Crear la enumeración `AlcanceComunicado.cs` en `backend/src/LFClub.Dominio/Enumeraciones/` (`CLUB`, `CATEGORIAS`, `EQUIPOS`), serializada como texto como `Rol`, con documentación XML ("`CLUB` no tiene destinos; `CATEGORIAS` tiene una fila por categoría; `EQUIPOS` tiene una fila por equipo, todos de una sola categoría")
- [X] T003 Entidades (depende de T002). En `backend/src/LFClub.Dominio/Entidades/`, con documentación XML e `IPerteneceAClub`: `Comunicado.cs` (`Id` con `Guid.CreateVersion7()`, `ClubId`, `Titulo` "texto ≤ 100, obligatorio, recortado, sin ser solo espacios", `Texto` "texto ≤ 1000, obligatorio, recortado, texto plano", `Alcance`, `Clave` "uuid, única por club", `PublicadoEn` en UTC, `PublicadoPorUsuarioRolId` "a nulo si el integrante se borra", `PublicadoPorNombre` "texto ≤ 161, copiado (§13)", `PublicadoPorRol` (`Rol`, "PRESIDENTE, DIRECTIVO o ENTRENADOR; el que tenía al publicar"), `RetiradoEn`, `RetiradoPorUsuarioRolId`, `RetiradoPorNombre` "texto ≤ 161, copiado"; sin campo de edición ni `Activo`), `ComunicadoDestino.cs` (`Id`, `ClubId`, `ComunicadoId`, `CategoriaId`, `CategoriaNombre`, `EquipoId`, `EquipoNombre`; los dos nombres copiados; los ids a nulo si se borra) y `ComunicadoDestinatario.cs` (`ComunicadoId` y `UsuarioRolId` como clave, `ClubId`, `UsuarioId`, `LeidoEn` "nulo mientras la cuenta no lo lea; igual en las filas de la misma cuenta y comunicado"). Ampliar la documentación de `UsuarioRol.cs`, `Categoria.cs` y `Equipo.cs` ("de él cuelgan los comunicados que recibió" / "puede ser destino de un comunicado")
- [X] T004 Configuraciones, `DbSet` y migración (depende de T003). Nuevas `ConfiguracionComunicado.cs`, `ConfiguracionComunicadoDestino.cs` y `ConfiguracionComunicadoDestinatario.cs` en `backend/src/LFClub.Infraestructura/Datos/Configuraciones/` según data-model.md: tablas `Comunicados`, `ComunicadosDestino` y `ComunicadosDestinatario`; `Alcance` y `PublicadoPorRol` como texto; nombres con `HasMaxLength(161)`; `Titulo` con `HasMaxLength(100)` y `Texto` con `HasMaxLength(1000)`; `ClubId` con foránea a `Clubes` en cascada; destinatarios en cascada con el comunicado, con el integrante y con la cuenta; `PublicadoPorUsuarioRolId`, `RetiradoPorUsuarioRolId`, `CategoriaId` y `EquipoId` con `OnDelete(SetNull)`; restricciones de comprobación "`RetiradoEn` y `RetiradoPorNombre` los dos nulos o los dos con valor", `char_length(Titulo) > 0` y `char_length(Texto) > 0`; índices `(ClubId, Id)`, único `(ClubId, Clave)`, `(UsuarioId, ClubId, ComunicadoId)` y parcial `(UsuarioId, ClubId) WHERE LeidoEn IS NULL`. Añadir los tres `DbSet` a `ContextoLFClub.cs`, con filtro de club. Generar la migración `ComunicadosYCampana` en `backend/src/LFClub.Infraestructura/Datos/Migraciones/` y comprobar que se aplica a una base vacía. Añadir la traducción de la violación del índice `(ClubId, Clave)` a `backend/src/LFClub.Aplicacion/Utilidades/IndicesUnicos.cs`
- [X] T005 [P] Errores de la funcionalidad. Nuevo `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeComunicados.cs`, como `ErroresDeCarta`, con título en español: `audiencia_invalida` (400, "La audiencia elegida no es válida."), `sin_destinatarios` (400, "Esa audiencia no tiene jugadores activos: el comunicado no le llegaría a nadie."), `clave_en_uso` (409, "Esa clave de publicación ya la usó otra persona."), `comunicado_retirado` (409, "El comunicado ya está retirado.") y `no_retira_comunicado` (403, "Solo quien lo publicó o el PRESIDENTE puede retirar un comunicado."). `no_entrena_categoria` y `datos_invalidos` se reutilizan tal cual (`ErroresDeCarta`, `ErroresDeValidacion`)
- [X] T006 [P] Regla de audiencia, en `backend/src/LFClub.Dominio/Reglas/ReglaAudienciaDeComunicado.cs`, pura y con documentación XML: `Validar(Rol rol, AlcanceComunicado alcance, IReadOnlyCollection<Guid> categoriasSolicitadas, IReadOnlyCollection<Guid> categoriasDelEntrenador, IReadOnlyCollection<Guid> categoriasDeLosEquipos)` según research §4: PRESIDENTE y DIRECTIVO → cualquier alcance; ENTRENADOR → `CATEGORIAS` o `EQUIPOS` solo de categorías de `categoriasDelEntrenador` (las que devuelve `ReglaAlcanceDeCategorias.PuedeVer`), nunca `CLUB`; JUGADOR y DESARROLLADOR → ninguna. Devuelve `Permitida`, `FueraDeSuAlcance` (→ `403 no_entrena_categoria`), `RolNoPublica` (→ `403 rol_no_autorizado`) o `AudienciaInvalida` (alcance `CLUB` con categorías o equipos, `CATEGORIAS` sin categorías o con equipos, `EQUIPOS` sin equipos o con categorías, o equipos de más de una categoría → `400 audiencia_invalida`). Un PRESIDENTE o DIRECTIVO asignado como entrenador se evalúa con el alcance de su rol (RF-006). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaAudienciaDeComunicadoPruebas.cs`, rol por alcance, entrenador con y sin asignación, categoría ajena, equipos de dos categorías y combinaciones incoherentes
- [X] T007 [P] Reglas de retiro y de visibilidad, en `backend/src/LFClub.Dominio/Reglas/`, puras y con documentación XML. `ReglaRetiroDeComunicado.Evaluar(Rol rol, Guid integranteId, Guid? autorId, bool retirado)` → `Retira`, `YaRetirado` (→ 409 `comunicado_retirado`), `NoLoVe` (→ 404, ENTRENADOR que no es el autor), `NoRetira` (→ 403 `no_retira_comunicado`, DIRECTIVO que no es el autor); el PRESIDENTE retira cualquiera, el autor retira el suyo, y con `autorId` nulo solo el PRESIDENTE (research §7 y §8). `ReglaVisibilidadDeComunicadosPublicados.Ve(Rol rol, Guid integranteId, Guid? autorId)`: PRESIDENTE y DIRECTIVO ven todo; ENTRENADOR solo los suyos; JUGADOR y DESARROLLADOR nada. Pruebas: `ReglaRetiroDeComunicadoPruebas.cs` y `ReglaVisibilidadDeComunicadosPublicadosPruebas.cs` en `backend/pruebas/Unitarias/Reglas/`, fila por fila, con autor nulo y ya retirado
- [X] T008 [P] Tipos del frontend: `frontend/src/compartido/api/tiposComunicados.ts` con `AlcanceComunicado`, `PublicarComunicadoDto`, `AutorDeComunicadoDto`, `DestinoDeComunicadoDto`, `RetiroDeComunicadoDto`, `ComunicadoPublicadoDto`, `ComunicadoRecibidoDto`, `PaginaDePublicados`, `PaginaDeRecibidos` y `ContadorDeComunicadosDto` según `contracts/api.yaml` (opcionales los que el contrato omite: `retirado`, `leidoEn`, `usuarioRolId`, `equipoId`, `equipoNombre`)
- [X] T009 [P] DTOs del backend (depende de T002). En `backend/src/LFClub.Aplicacion/DTOs/`: `PublicarComunicadoDto.cs`, `AutorDeComunicadoDto.cs`, `DestinoDeComunicadoDto.cs`, `RetiroDeComunicadoDto.cs`, `ComunicadoPublicadoDto.cs`, `ComunicadoRecibidoDto.cs`, `PaginaDePublicadosDto.cs`, `PaginaDeRecibidosDto.cs` y `ContadorDeComunicadosDto.cs`, como registros con documentación XML y los nombres, tipos y opcionales de `contracts/api.yaml` (§23). `ComunicadoRecibidoDto` no lleva `alcance`, `destinos` ni `familiasAlcanzadas`; `retirado` y `leidoEn` se omiten del JSON cuando son nulos
- [X] T010 Ayudas de prueba (depende de T004). Nuevo `backend/pruebas/Integracion/Base/SembradorComunicados.cs`: `PublicarAsync(club, autor, titulo, texto, alcance, categorias, equipos, destinatarios)` que escribe las filas como lo haría el servicio (para preparar escenarios sin pasar por la API), `RetirarAsync(comunicado, por)` y `LeerAsync(comunicado, cuenta)`. Reutiliza los sembradores de clubes, categorías, equipos y jugadores de la 003 a la 010 y añade `AgregarEquipoAsync(categoria, nombre, jugadores)` y `PonerEnEsperaAsync(jugador)` solo si faltan. Ninguna prueba existente cambia

**Punto de control**: la migración se aplica, las reglas puras pasan sus pruebas y la batería de
la 001 a la 010 sigue en verde (el punto de partida real del contrato es 100/76).

---

## Fase 3: Historia de usuario 1 - El equipo del club publica un comunicado (Prioridad: P1) 🎯 MVP

**Objetivo**: PRESIDENTE, DIRECTIVO y ENTRENADOR publican título y texto a todo el club, a
categorías o a equipos; llega solo a las cuentas con un jugador activo en la audiencia, una vez, y
un doble toque no crea dos.

**Prueba independiente**: entrar con cada rol, publicar y comprobar con varias cuentas quién lo
recibe y quién no (quickstart, bloque 1).

- [X] T011 [P] [US1] Validador (sin endpoint todavía). `backend/src/LFClub.Aplicacion/Validadores/ValidadorPublicarComunicado.cs`: `clave` obligatoria y no vacía; `titulo` obligatorio, recortado, sin ser solo espacios y "hasta 100 caracteres"; `texto` obligatorio, recortado y "hasta 1000 caracteres"; `alcance` válido. Errores por campo con `datos_invalidos`. Pruebas: `backend/pruebas/Unitarias/Validadores/ValidadorPublicarComunicadoPruebas.cs` (vacío, solo espacios, 100 y 101, 1000 y 1001, clave vacía, alcance inventado)
- [X] T012 [US1] Repositorio (depende de T004 y T009; sin endpoint todavía). `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioComunicados.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioComunicados.cs`, con documentación XML: `ResolverDestinatariosAsync(clubId, alcance, categoriaIds, equipoIds)` (una consulta del club: integrantes con rol `JUGADOR`, ingreso `APROBADO` y `RetiradoEn` nulo; `CLUB` todos, con o sin categoría; `CATEGORIAS` los de esas categorías; `EQUIPOS` los de `JugadorEquipo`, research §3), `BuscarPorClaveAsync`, `AgregarAsync` (comunicado, destinos y destinatarios en la misma unidad de trabajo), `ListarPublicadosAsync(clubId, autorIdONulo, antesDe, limite)` por cursor sobre `Id` descendente que pide uno más para calcular `hayMas`, y `ContarFamiliasAsync(comunicadoIds)` (`COUNT(DISTINCT UsuarioId)`). Registrar en `RegistroDeServicios.cs`. Se prueba a través de T013 y T014
- [X] T013 [US1] Publicar, de punta a punta (depende de T005, T006, T007, T009, T011 y T012). Backend: `IServicioPublicacionComunicados` y `ServicioPublicacionComunicados` en `backend/src/LFClub.Aplicacion/Servicios/` y `Implementaciones/`: valida con el validador y la regla; carga las categorías activas del club y, para el ENTRENADOR, las suyas con `IRepositorioAsignaciones.CategoriasDeAsync`; categoría o equipo de otro club, inexistente o inactivo → `400 audiencia_invalida`; sin ningún jugador activo → `400 sin_destinatarios` y no guarda nada; copia nombre y rol del autor y los nombres de categoría y equipo (§13); idempotencia por `Clave` (research §6): mismo integrante → `200` con el ya creado, otro → `409 clave_en_uso`, y si dos llegan a la vez, la que pierde relee y devuelve el existente. No llama a `IServicioCorreo` (RF-028). `MapperComunicados.cs` en `backend/src/LFClub.Aplicacion/Mappers/` arma `ComunicadoPublicadoDto` con `puedeRetirar` según `ReglaRetiroDeComunicado`. `ControladorComunicados.cs` en `backend/src/LFClub.Api/Controladores/Club/`, con `[IntegranteDelClub]`: `POST /api/clubes/{clubId}/comunicados` → `201`; JUGADOR y DESARROLLADOR → `403 rol_no_autorizado` / `404`. Registrar en `RegistroDeCasosDeUso.cs`, descomentar `POST` y subir a 100 y 76. Pruebas de integración en `backend/pruebas/Integracion/Comunicados/`: `PublicarComunicadoPruebas.cs` (los 10 pasos del bloque 1 salvo la lista), `AudienciaPruebas.cs` (tabla rol por alcance con cuentas de varias familias: la 2014, la 2015, el equipo A, "Todo el club" con J11 sin categoría y sin J12 en espera ni J10 retirado; E1 a la 2015 y a `CLUB` → 403; equipos de dos categorías → 400; hermanos J1 y J2 reciben una vez; categoría sin jugadores → 400; PRESIDENTE y DIRECTIVO asignados como entrenador publican con el alcance de su rol), `IdempotenciaPruebas.cs` (misma clave dos veces → un solo comunicado; dos peticiones simultáneas; otra persona con la clave → 409) y `SinAvisosExternosPruebas.cs` con el servicio de correo sustituido por uno que falla si se llama (CE-006)
- [X] T014 [US1] Lista de publicados del equipo, de punta a punta (depende de T013). Backend: `IServicioConsultaComunicados` y `ServicioConsultaComunicados` con `ListarPublicadosAsync`: PRESIDENTE y DIRECTIVO todos, retirados incluidos; ENTRENADOR solo los suyos, con `ReglaVisibilidadDeComunicadosPublicados`; con `familiasAlcanzadas`, `destinos` y `puedeRetirar`; JUGADOR → `403 rol_no_autorizado`; `limite` de 1 a 50 con 30 por defecto y `antesDe` (research §9). `GET /api/clubes/{clubId}/comunicados` en `ControladorComunicados.cs`; descomentar y subir a 101 y 77. Pruebas: `backend/pruebas/Integracion/Comunicados/PublicadosPruebas.cs` (E1 ve solo los suyos; P y D1 ven todos con autor y destinos; JUGADOR 403; cursor sin repetir ni saltar con 35 comunicados; club B y el DESARROLLADOR → 404; el texto del JSON de la lista no trae datos de otro club)
- [X] T015 [US1] Pantalla del equipo: publicar y ver lo publicado (depende de T013 y T014; sigue `contracts/campana.md`). En `frontend/src/privado/comunicados/`: `PantallaComunicados.tsx` (rama del equipo: título "Comunicados publicados", botón "Nuevo comunicado"; la rama JUGADOR llega en T017), `ListaPublicados.tsx` (título, a quién iba —"Todo el club", "Categoría 2015, 2016", "Equipos A y B de la 2015"—, "N familias", quién lo publicó solo para PRESIDENTE y DIRECTIVO, estado en texto "Publicado", "Ver más" con `hayMas`), `FormularioComunicado.tsx` (título hasta 100 y texto hasta 1.000 con contador de restantes; "Para quién": "Todo el club" solo PRESIDENTE y DIRECTIVO, "Categorías" con casillas, "Equipos de una categoría" con categoría y luego casillas; ENTRENADOR solo ve las suyas con los endpoints de categorías y equipos de la 003; `clave` generada con `crypto.randomUUID()` al abrir y conservada en los reintentos; botón deshabilitado mientras la petición va; errores por campo, "Elige al menos una categoría" y "Esa categoría no tiene jugadores activos"; al publicar vuelve a la lista con "Comunicado publicado. Les llegará a N familias."), `SelectorAudiencia.tsx` y `textos.ts`. Estilos en `frontend/src/compartido/tema/comunicados.css`. Rutas `/club/:clubId/comunicados` y `/club/:clubId/comunicados/nuevo` en `frontend/src/App.tsx` y entrada "Comunicados" en `frontend/src/privado/MenuDelClub.tsx` para los cuatro roles. Pruebas: `frontend/pruebas/FormularioComunicado.test.tsx` (límites, opciones de audiencia según el rol, ENTRENADOR sin "Todo el club", clave estable entre reintentos, botón deshabilitado en vuelo) y `frontend/pruebas/textoDeComunicado.test.tsx` (`<script>alert(1)</script>` y `**negrita**` se ven tal cual)

**Punto de control**: quickstart, bloque 1, completo. Las familias aún no ven nada: la historia 2
lo cierra.

---

## Fase 4: Historia de usuario 2 - La familia ve la campana con el contador (Prioridad: P1)

**Objetivo**: la cuenta de jugador ve en la barra superior de todas sus pantallas una campana con
el contador de sin leer, abre la lista, lee cada comunicado y solo ve lo que le llegó.

**Prueba independiente**: publicar un comunicado y entrar con la cuenta de un destinatario y con la
de uno que no lo es (quickstart, bloque 2).

- [X] T016 [US2] Recibidos, contador y detalle, de punta a punta (depende de T004, T009 y T010; no depende de T013). Backend: ampliar `ServicioConsultaComunicados` con `ListarRecibidosAsync`, `ContarSinLeerAsync` y `ObtenerRecibidoAsync` y `RepositorioComunicados` con sus consultas según data-model "Comunicados de una cuenta": filas de la cuenta de la petición en el club, de los jugadores de la limitación de la sesión si la hay, con jugador aprobado y sin retirar y comunicado sin retirar; agrupadas por `ComunicadoId` (un comunicado por hermanos sale una vez, RF-013), del `Id` más reciente al más antiguo con cursor, `leido` cuando ninguna fila del grupo tiene `LeidoEn` nulo; `PaginaDeRecibidos` lleva `sinLeer`. `ControladorComunicadosRecibidos.cs` en `backend/src/LFClub.Api/Controladores/Club/`: `GET …/comunicados/recibidos`, `GET …/comunicados/recibidos/contador` y `GET …/comunicados/recibidos/{comunicadoId}`, solo JUGADOR (`403 rol_no_autorizado` a los demás roles); un comunicado que no le llegó a la cuenta, retirado o de otro club → `404 no_encontrado` sin distinguir (RF-032); la ruta fija `contador` no choca con `{comunicadoId}`. El detalle no marca como leído. Descomentar las tres y subir a 102/78, 103/79 y 104/80. Pruebas en `backend/pruebas/Integracion/Comunicados/`: `CampanaYContadorPruebas.cs` (contador igual a los sin leer; hermanos J1 y J2 cuentan uno; M1 sin comunicados → 0 y lista vacía; orden del más reciente al más antiguo con autor, rol y fecha; otra cuenta, otro club y el DESARROLLADOR → 404; E1 y P → 403; cambiar de categoría después de publicar conserva lo recibido y no recibe lo anterior; entrar después no recibe lo anterior; J12 en espera nunca recibe; J10 retirado deja de contar y al reincorporarlo vuelve; sesión limitada al documento de J1 solo ve lo de J1; retirado fuera de lista y de contador vía `SembradorComunicados`) y `ConsultaRecibidosPruebas.cs` (el texto del JSON no trae `alcance`, `destinos` ni `familiasAlcanzadas`; cursor sin repetir)
- [X] T017 [US2] Campana y lista de la familia (depende de T016; sigue `contracts/campana.md`). En `frontend/src/compartido/componentes/CampanaComunicados.tsx`: botón de icono de 44 × 44 px, sin texto visible, que recibe `sinLeer` y el destino (no conoce la sesión), insignia con el número en la esquina superior derecha y "99+" desde 100, color de acento del club con contraste 4,5:1 en los dos temas, nombre accesible "Comunicados, sin novedades" / "Comunicados, 3 sin leer" / "Comunicados, más de 99 sin leer", región `aria-live="polite"` que anuncia el cambio, sin animación; icono nuevo en `Iconos.tsx`. En `frontend/src/privado/comunicados/useContadorComunicados.ts`: consulta el contador al montar la zona del club, al volver a primer plano (`visibilitychange`), al cambiar de pantalla y cada 60 segundos mientras esté visible; si falla conserva el último número sin error; expone el valor y una función para fijarlo con la respuesta de lectura. `frontend/src/privado/DisposicionClub.tsx`: la campana a la izquierda del menú del perfil, **solo cuando el rol de la sesión es JUGADOR**, sin saturar la barra a 360 px. `ListaRecibidos.tsx` y la rama JUGADOR de `PantallaComunicados.tsx` ("Comunicados", del más reciente al más antiguo; fila con título, autor con su rol y ficha básica vía `NombrePersona`, fecha y "Sin leer" o "Leído" en texto además del título en negrita para lo sin leer; "Ver más"; vacío "Todavía no hay comunicados del club."), `DetalleComunicado.tsx` (título, texto completo con `white-space: pre-wrap` y fecha; en escritorio panel lateral, en teléfono pantalla completa) y ruta `/club/:clubId/comunicados/:id` en `App.tsx`. Pruebas: `frontend/pruebas/CampanaComunicados.test.tsx` (sin insignia con 0, "5", "99+" desde 100, los tres nombres accesibles, no aparece para PRESIDENTE, DIRECTIVO ni ENTRENADOR, el sondeo vuelve a consultar con el reloj simulado, conserva el número si la consulta falla)

**Punto de control**: quickstart, bloque 2, completo. Con la historia 1 y la 2 la familia ya recibe y
ve lo que publica el club: es el MVP.

---

## Fase 5: Historia de usuario 3 - Leído y no leído (Prioridad: P2)

**Objetivo**: abrir un comunicado lo marca leído y baja el contador; "Marcar todos como leídos"
los marca todos; lo leído es por cuenta y persiste entre sesiones y aparatos.

**Prueba independiente**: una cuenta con varios sin leer: abrir uno, revisar el contador y luego
marcar todos (quickstart, bloque 3).

- [X] T018 [US3] Lectura, de punta a punta (depende de T016). Backend: `IServicioLecturaComunicados` y `ServicioLecturaComunicados`: `MarcarLeidoAsync` pone `LeidoEn` (hora del `IReloj`) en todas las filas de la cuenta para ese comunicado donde es nulo, es idempotente (leerlo otra vez no mueve la fecha) y devuelve `ComunicadoRecibidoDto`; `MarcarTodosAsync` hace lo mismo con todos los visibles de la cuenta y devuelve `ContadorDeComunicadosDto`. `PUT …/comunicados/recibidos/{comunicadoId}/lectura` y `POST …/comunicados/recibidos/lectura-total` en `ControladorComunicadosRecibidos.cs`, solo JUGADOR, con los mismos 404 que la consulta; con la sesión limitada a un jugador, ve solo esas filas pero marca leído para la cuenta entera (RF-022). Descomentar y subir a 105/81 y 106/82. Pruebas: `backend/pruebas/Integracion/Comunicados/LecturaPruebas.cs` (leer baja el contador de 3 a 2; marcar todos lo deja en 0; la otra familia sigue sin leer; segunda lectura no cambia `LeidoEn`; persiste con otra sesión; hermanos J1 y J2 con un comunicado a las dos categorías: leerlo desde J1 lo deja leído visto desde J2; sesión limitada a J1 marca leído para la cuenta entera; comunicado ajeno, retirado o de otro club → 404 y no marca nada)
- [X] T019 [US3] Pantalla de lectura (depende de T017 y T018). `DetalleComunicado.tsx` llama a la lectura al abrir y fija el contador con la respuesta (sin otra consulta); la fila pasa a "Leído"; "Marcar todos como leídos" en `ListaRecibidos.tsx`, deshabilitado sin comunicados sin leer, que fija el contador con la respuesta y deja la insignia sin número; el estado leído se distingue por texto además del color. Pruebas: `frontend/pruebas/LecturaDeComunicados.test.tsx` (abrir baja el contador y marca "Leído", "Marcar todos" deshabilitado con 0, la insignia desaparece tras marcarlos, no hay segunda consulta del contador)

**Punto de control**: quickstart, bloque 3, completo.

---

## Fase 6: Historia de usuario 4 - Ver y retirar lo publicado (Prioridad: P3)

**Objetivo**: quien publica ve lo que publicó y lo retira; el PRESIDENTE retira cualquiera; lo
retirado sale de la campana y del contador de todas las familias y queda marcado en lo publicado.

**Prueba independiente**: publicar, retirar y comprobar que las familias dejan de verlo y el
contador baja (quickstart, bloque 4).

- [X] T020 [US4] Retiro, de punta a punta (depende de T013 y T014). Backend: `RetirarAsync` en `ServicioPublicacionComunicados` con `ReglaRetiroDeComunicado` y `ReglaVisibilidadDeComunicadosPublicados`: ENTRENADOR que no es el autor → `404`; DIRECTIVO que lo ve y no es el autor → `403 no_retira_comunicado`; ya retirado → `409 comunicado_retirado`; copia `RetiradoPorNombre` (§13) y `RetiradoEn` con el `IReloj`; no borra nada ni toca los destinatarios; no llama a `IServicioCorreo` (RF-028). `POST /api/clubes/{clubId}/comunicados/{comunicadoId}/retiro` en `ControladorComunicados.cs` devuelve `ComunicadoPublicadoDto` con `retirado` y `puedeRetirar` falso. Descomentar y subir a 107 y 83. Pruebas: `backend/pruebas/Integracion/Comunicados/RetiroPruebas.cs` (E1 retira uno suyo → 200 y J3 deja de verlo y el contador baja si estaba sin leer; E1 sobre uno de P → 404; D1 sobre uno de P → 403 `no_retira_comunicado`; P sobre uno de E1 → 200; DIRECTIVO retira los suyos; ya retirado → 409; retirado sigue en la lista de publicados con quién y cuándo; E1 movido fuera de la 2014 conserva y retira lo suyo y ya no publica a la 2014; comunicado con autor nulo solo lo retira P; club B y el DESARROLLADOR → 404; un retirado no vuelve a publicarse con la misma clave: devuelve el existente, retirado)
- [X] T021 [US4] Pantalla de retiro y lista completa (depende de T015 y T020). `ListaPublicados.tsx`: estado "Retirado" con quién y cuándo en texto, "Retirar" solo donde `puedeRetirar`, con `DialogoConfirmacion` ("Dejará de verse en la campana de las familias. Esto no se puede deshacer.", foco atrapado); al confirmar actualiza la fila con la respuesta. `DetalleComunicado.tsx` para el equipo del club: título, texto, audiencia con sus destinos, "N familias", autor y estado. Pruebas: `frontend/pruebas/RetiroDeComunicado.test.tsx` (el botón solo aparece con `puedeRetirar`, pide confirmación, la fila pasa a "Retirado" con nombre y hora, cancelar no llama a la API)

**Punto de control**: quickstart, bloque 4, completo.

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: contrato, aislamiento, pantallas, documentación, validación de extremo a extremo y
batería.

- [X] T022 Comprobar que no queda ninguna marca `# 011-T0NN` ni operación comentada en `specs/011-comunicados-y-campana/contracts/api.yaml`, que Swagger expone las 108 operaciones del contrato y que `AccesoClubPruebas` pasa con 108 y 84 (las 8 rutas nuevas responden 404 al integrante de otro club y al DESARROLLADOR, y 401 sin sesión) (RF-030, CE-005)
- [ ] T023 [P] Revisión de pantallas a 360 px y en escritorio, en tema claro y oscuro (RF-033, CE-007): la campana con 0, 1, 99 y 100 o más, con la marca y el perfil visibles, `ListaRecibidos` y `DetalleComunicado` con un título de 100 caracteres y un texto de 1.000 sin espacios, `ListaPublicados` con "Retirado", `FormularioComunicado` con sus tres audiencias y el diálogo de retiro; con los colores del club oscuros, claros y sin acento. Comprobar contraste 4,5:1 de la insignia. Corregir lo que falle en `frontend/src/compartido/tema/comunicados.css` y los componentes **Pendiente de la revisión visual del propietario**: al implementar solo se revisó el CSS (tokens del proyecto, `minmax(0, 1fr)`, `overflow-wrap`, insignia con `--color-club-acento-texto` y borde de la superficie); no hay navegador en este entorno para verla a 360 px.
- [X] T024 [P] Revisar la documentación XML y los comentarios de componente de todo lo nuevo y de las clases que cambian según el plan (`UsuarioRol`, `Categoria`, `Equipo`, `ContextoLFClub`, `IndicesUnicos`, `DisposicionClub`, `MenuDelClub`), y pasar `scripts/verificar-tamano.ps1` (ningún archivo escrito a mano por encima de 250 líneas)
- [ ] T025 Recorrer `specs/011-comunicados-y-campana/quickstart.md` entero en un proyecto de Compose aparte (`docker compose -p lfclub-validacion -f docker-compose.yml -f <override>` con `Brevo__Llave` vacía, para no enviar correos reales con la llave del `.env`), con un contenedor mínimo en marcha si Docker se duerme, y limpiar al final (`down -v` y borrar el contenedor mínimo). Anotar cualquier diferencia con la spec y detenerse a preguntar si alguna no es un error claro. La revisión de la campana (el lienzo no la tiene, plan nota 1) la hace el propietario en esta tarea **Hecho en parte**: en un proyecto de Compose aparte (`lfclub-validacion`, `Brevo__Llave` vacía, puertos 18080 y 15173, limpiado con `down -v`) la API arrancó, aplicó `ComunicadosYCampana`, Swagger expone las 8 operaciones (108 en total), las rutas responden 401 sin sesión y el frontend sirve. Los bloques 1 a 5 de la guía los cubren las pruebas de integración (Postgres real); queda para el propietario el recorrido con la aplicación abierta (sondeo de un minuto, sin avisos con la app cerrada, 360 px, temas).
- [X] T026 Batería completa: `dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `tsc`, `npm --prefix frontend run lint`, `npm --prefix frontend run build` y `./scripts/verificar-tamano.ps1`, con las pruebas de la 001 a la 010 en verde

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (T001)**: sin dependencias.
- **Cimientos (T002 a T010)**: después de T001. Bloquean todas las historias.
- **Historia 1 (T011 a T015)**: después de los cimientos. No depende de otras historias.
- **Historia 2 (T016, T017)**: después de los cimientos. No depende del código de la historia 1
  (siembra con `SembradorComunicados`); `PantallaComunicados.tsx` la crea T015, y si la historia 2
  va antes, T017 la crea con la rama JUGADOR y T015 le añade la del equipo.
- **Historia 3 (T018, T019)**: después de T016 y T017.
- **Historia 4 (T020, T021)**: después de T013 y T014 (T021 también de T015).
- **Cierre (T022 a T026)**: después de todo lo anterior.

### Entre tareas

- T002 → T003 → T004 → T010 (enumeración, entidades, esquema, ayudas de prueba); T002 → T009.
- T005, T006 y T007 → T013, T020. T008 → T015, T017.
- T011 y T012 → T013 → T014 → T015 (mismo `ControladorComunicados`, mismo `ServicioConsultaComunicados`
  y `PantallaComunicados`).
- T016 → T017 → T019; T016 → T018 → T019 (mismo `ControladorComunicadosRecibidos` y
  `DetalleComunicado`).
- T013, T014 → T020 → T021.
- Los contadores de `AccesoClubPruebas` suben en el orden T013, T014, T016, T018, T020. Si se
  cambia el orden, cada tarea sube los suyos sobre lo que haya.

### Dentro de cada tarea

- Backend (DTO → interfaz → repositorio → regla o utilidad → servicio → controlador → registro →
  contrato), luego pantalla contra la API real, luego pruebas. La tarea no se marca hecha sin sus
  pruebas en verde.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos, tras T001
T002  Enumeración AlcanceComunicado     (luego T003 → T004 → T010)
T005  Errores
T006  ReglaAudienciaDeComunicado
T007  ReglaRetiro y ReglaVisibilidad
T008  tiposComunicados.ts
T009  DTOs                               (tras T002)

# Historia 1
T011  ValidadorPublicarComunicado        (en paralelo con T012)
T012  RepositorioComunicados

# Historias en paralelo tras los cimientos (archivos distintos)
T011  Validador                          (historia 1)
T016  Recibidos y contador               (historia 2)

# Cierre
T023  Revisión a 360 px y en los dos temas
T024  Documentación y tamaño
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T011 a T015) e historia 2 (T016, T017).
3. **Parar y validar**: quickstart, bloques 1 y 2. El club publica y las familias ven la campana.

### Entrega incremental

1. Cimientos → la migración se aplica y nada cambia para nadie.
2. Historia 1 → el equipo publica y ve lo publicado; las familias aún no ven nada.
3. Historia 2 → la campana y la lista. **No se publica la historia 1 sin la 2**: publicar sin que
   nadie reciba no tiene valor y dejaría comunicados sin poder retirarse.
4. Historia 3 → leído y "Marcar todos". Sin ella el contador nunca baja: no se despliega la
   historia 2 sin la 3.
5. Historia 4 → retiro y lista completa.
6. Cierre → contrato de 108, aislamiento de 84, 360 px, quickstart y batería.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- La migración de esta funcionalidad es `ComunicadosYCampana`. La aplicación está en construcción:
  no hay datos que migrar.
- Ninguna tarea modifica la constitución: la 4.9.0 ya recoge todo lo que aquí se construye.
- No se añade ninguna dependencia (plan, "Seguimiento de complejidad").
- La campana no está en el lienzo (plan, nota 1): su diseño sigue `contracts/campana.md` y lo
  revisa el propietario en T023 y T025.
- El aviso con la aplicación cerrada y el número sobre el icono son de otra spec: ninguna tarea los
  construye ni deja ganchos para ellos más allá de que `Comunicados` es la tabla sobre la que se
  construirán (research §14).
- Si una prueba de la 001 a la 010 falla al tocar `ContextoLFClub`, `IndicesUnicos`,
  `DisposicionClub.tsx`, `MenuDelClub.tsx` o `App.tsx` y no es una regresión clara, se detiene esa
  parte y se pregunta (§25).
