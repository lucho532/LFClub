---

description: "Lista de tareas de la funcionalidad 013: entrenamientos y partidos dirigidos a equipos y a jugadores añadidos"
---

# Tareas: Entrenamientos y partidos dirigidos a equipos y a jugadores añadidos

**Entrada**: documentos de diseño en `specs/013-actividades-por-equipo/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.11.0) y el plan (tabla de
trazabilidad). Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa
contra la API real y sus pruebas; las tareas que solo traen pruebas, solo reglas puras o solo
pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario. Orden de construcción del plan:
base, historia 1 (P1), historia 2 (P2), historia 3 (P2), historia 4 (P3) y cierre.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 012, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): dos tablas nuevas (`EntrenamientosEquipo` y `EntrenamientosJugador`),
  dos índices, una restricción de comprobación y una sola migración, `ActividadesPorEquipo` (T006).
  Los partidos no llevan tablas (research §2). Si una tarea posterior parece necesitar otra tabla,
  columna o índice, se detiene y se informa (§25). Ninguna columna de una tabla existente cambia.
- **Contrato**: `specs/013-actividades-por-equipo/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. Las dos operaciones nuevas ya están comentadas en el archivo
  y cada tarea que construye una la descomenta. **Punto de partida esperado: contrato 109 y club
  85** (confirmarlo en T002 con la expresión regular de `EndpointsDeLaApi` antes de fiarse). Recorrido:
  109/85 → **110/86** (T041, `PUT …/audiencia`) → **111/87** (T043, jugadores sumables).
- **Lo anterior no cambia**: los entrenamientos y partidos sin equipos ni añadidos se comportan como
  antes y los datos guardados se leen como "toda la categoría" (RF-025). Si una prueba de la 001 a
  la 012 falla y no está claro que sea una regresión, se detiene y se pregunta (§27.2).
- **Regla de visibilidad** (research §3): `ReglaVisibilidadDeEntrenamiento` y la consulta
  `RepositorioEntrenamientos.Visibles` dicen lo mismo; una prueba de integración las compara por rol.
  Los calendarios, listados, contadores, campana e identificador usan la misma condición (RF-004).
- **Nadie ve lo que no puede ver** (RF-018, RF-024): ningún DTO, aviso o contador lleva a una cuenta
  de jugador la lista de convocados ni de añadidos, ni los equipos de una actividad que no ve.
- **Quitar no deja rastro** (RF-017, CE-004): ni aviso ni texto en pantalla para quien sale.
- **Quien dirige y quien gestiona** (research §4): cambiar la audiencia de un entrenamiento es de
  `ReglaDireccionDeActividad` (PRESIDENTE o quien entrena **alguna** categoría); programar, cambiar
  datos, cancelar y convocar siguen siendo de `PuedeGestionar`.
- **Nombres** (§2.1, §2.2): todo en español; "familia" solo en textos de pantalla, el rol sigue
  siendo JUGADOR. `EntrenamientoEquipo`, `EntrenamientoJugador`, `OrigenDeJugadorEnEntrenamiento`,
  `ReglaAudienciaDeEntrenamiento`, `ReglaDireccionDeActividad`, `ServicioAudienciaDeEntrenamiento`.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) con qué representa, su responsabilidad y qué no hace; se amplía la de las que cambian.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. `ServicioEntrenamientos.cs`
  (228) y `ServicioAsistencia.cs` (190) están cerca: lo nuevo vive en clases propias y ellos solo
  suman llamadas (research §12). Medir con `scripts/verificar-tamano.ps1` al empezar (T001) y al
  cerrar; si algo supera 240, se parte antes de añadir. Sin clases parciales para esquivar el límite.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Controladores con
  `[IntegranteDelClub]` que solo delegan; reglas en `Dominio/Reglas`, puras.
- **Aislamiento** (§7.1): las dos entidades implementan `IPerteneceAClub`; las rutas nuevas quedan
  bajo `/api/clubes/{clubId}`; otro club y el DESARROLLADOR reciben 404.
- **Concurrencia** (research §11): programar y cambiar la audiencia toman
  `IRepositorioClub.BloquearAsync` y releen dentro de la transacción.
- **Pantallas** (§24, CE-007): componentes `Tarjeta`, `Aviso`, `Boton`, `Campo`,
  `DialogoConfirmacion`; legibles a 360 px y en los dos temas, sin desplazamiento horizontal; la
  pertenencia ("Equipo A", "Añadido", "Equipo desactivado") se escribe con texto, no solo con color.
  El lienzo no tiene estas pantallas: el diseño sigue research §13 con los tokens del proyecto y lo
  revisa el propietario.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

---

## Fase 1: Preparación

**Propósito**: medir de dónde se parte antes de tocar nada.

- [X] T001 Ejecutar `scripts/verificar-tamano.ps1` y anotar en el informe de la tarea el tamaño de
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioEntrenamientos.cs`,
      `ServicioAsistencia.cs`, `ServicioRespuestas.cs`, `ServicioConvocatoria.cs`,
      `ServicioEquipos.cs`, `ServicioAvisosDeActividad.cs`,
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEntrenamientos.cs` y
      `RepositorioEquipos.cs`. Todo archivo por encima de 240 líneas se parte en una clase nueva
      antes de añadirle nada (research §12).
- [X] T002 Contar las operaciones del contrato con la expresión regular de
      `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs` y confirmar el punto de partida
      **109 en el contrato y 85 de club**. Si el número real es otro, anotarlo en la sección
      "Convenciones" de este archivo y en `contracts/api.yaml` y usar el real en T041 y T043.
      Comprobar que `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` lee el contrato
      de la 013 y que sus dos operaciones comentadas no suman.

---

## Fase 2: Base (bloquea todas las historias)

**Propósito**: esquema, reglas puras y piezas compartidas. Ninguna historia empieza sin esto.

**⚠️ CRÍTICO**: esta fase no cambia comportamiento visible; deja las tablas, las reglas y las
consultas listas.

- [X] T003 [P] Crear la enumeración `OrigenDeJugadorEnEntrenamiento` con los valores `ANADIDO` y
      `EQUIPO_DESACTIVADO` en
      `backend/src/LFClub.Dominio/Enumeraciones/OrigenDeJugadorEnEntrenamiento.cs`, con
      documentación XML (qué significa cada valor y que `EQUIPO_DESACTIVADO` solo lo escribe el
      sistema).
- [X] T004 [P] Crear las entidades `EntrenamientoEquipo` (`EntrenamientoId`, `EquipoId`, `ClubId`) y
      `EntrenamientoJugador` (`EntrenamientoId`, `UsuarioRolId`, `ClubId`, `Origen`,
      `EquipoDeOrigenId` nulo, `AnadidoEn` UTC) en
      `backend/src/LFClub.Dominio/Entidades/EntrenamientoEquipo.cs` y `EntrenamientoJugador.cs`,
      ambas `IPerteneceAClub`. Añadir las listas `Equipos` y `Jugadores` a
      `backend/src/LFClub.Dominio/Entidades/Entrenamiento.cs` y ampliar la documentación de esa
      clase y de `backend/src/LFClub.Dominio/Entidades/Equipo.cs` ("se marca `Usado` al dirigirle un
      entrenamiento").
- [X] T005 Crear `ConfiguracionEntrenamientoEquipo` y `ConfiguracionEntrenamientoJugador` en
      `backend/src/LFClub.Infraestructura/Datos/Configuraciones/` y añadir los dos `DbSet` a
      `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs`. Claves compuestas
      (`EntrenamientoId`+`EquipoId` y `EntrenamientoId`+`UsuarioRolId`); foránea al entrenamiento
      en cascada; foránea al equipo `Restrict`; foránea del jugador en cascada;
      `EquipoDeOrigenId` con `SetNull`; `Origen` guardado como texto; índice por `EquipoId` en
      `EntrenamientosEquipo` e índice por `UsuarioRolId` en `EntrenamientosJugador`; restricción de
      comprobación "si `Origen = ANADIDO`, `EquipoDeOrigenId` es nulo" (no se exige lo contrario).
      Filtro global de club en las dos, como `ConfiguracionEntrenamientoCategoria.cs`.
- [X] T006 Generar la migración `ActividadesPorEquipo` en
      `backend/src/LFClub.Infraestructura/Datos/Migraciones/` (la siguiente a
      `20261012200304_AvisosDeActividad`). Sin datos que migrar. Comprobar que la instantánea del
      modelo cambia solo por las dos tablas y que `dotnet ef migrations has-pending-model-changes`
      no pide más.
- [X] T007 [P] Crear `ReglaAudienciaDeEntrenamiento.EnLaAudiencia` (pura) en
      `backend/src/LFClub.Dominio/Reglas/ReglaAudienciaDeEntrenamiento.cs` con sus pruebas en
      `backend/pruebas/Unitarias/Reglas/ReglaAudienciaDeEntrenamientoPruebas.cs`: sin equipos entra
      por categoría (incluido el jugador sin equipo); con equipos entra por equipo y la categoría
      sola no basta; el añadido y el `EQUIPO_DESACTIVADO` entran siempre; retirado, inactivo y sin
      categoría no entran; sin duplicados por varios caminos.
- [X] T008 [P] Crear `ReglaDireccionDeActividad.PuedeDirigir(rol, categorias,
      categoriasQueEntrena)` (pura) en
      `backend/src/LFClub.Dominio/Reglas/ReglaDireccionDeActividad.cs` con sus pruebas en
      `backend/pruebas/Unitarias/Reglas/ReglaDireccionDeActividadPruebas.cs`: PRESIDENTE siempre;
      DIRECTIVO o ENTRENADOR si entrena **alguna** categoría; DIRECTIVO sin asignar, ENTRENADOR de
      otra categoría y JUGADOR no. Documentar la diferencia con `PuedeGestionar` (entrena todas).
- [X] T009 Ampliar `ReglaVisibilidadDeEntrenamiento.LaVe` en
      `backend/src/LFClub.Dominio/Reglas/ReglaVisibilidadDeEntrenamiento.cs` con los tres hechos
      nuevos (el entrenamiento tiene equipos, el jugador está en alguno de ellos, el jugador está
      añadido) según la tabla de research §3, y sus pruebas en
      `backend/pruebas/Unitarias/Reglas/ReglaVisibilidadDeEntrenamientoPruebas.cs`: DIRECTIVO sin
      asignar ve los de toda la categoría y **no** ve los de equipos; el jugador con asistencia
      registrada lo sigue viendo; jugador de la categoría pero de otro equipo no lo ve. Depende de
      T007.
- [X] T010 [P] Añadir la cobertura `AUDIENCIA_DE_ENTRENAMIENTO` a `ReglaDestinatariosDeAviso` en
      `backend/src/LFClub.Dominio/Reglas/ReglaDestinatariosDeAviso.cs` y a su prueba en
      `backend/pruebas/Unitarias/Reglas/ReglaDestinatariosDeAvisoPruebas.cs`: la audiencia (equipos
      o categorías) más los añadidos; sin equipos ni añadidos da exactamente lo de `CATEGORIAS`
      (RF-025); en un partido ya jugado, `CATEGORIAS` suma a los convocados (research §8).
- [X] T011 [P] Crear los DTOs nuevos `EquipoDeActividadDto` (`equipoId`, `nombre`, `categoriaId`,
      `anio`, `activo`), `AudienciaDeEntrenamientoDto` (`equipoIds`, `jugadorIds`) y
      `JugadorSumableDto` (`usuarioRolId`, `nombres`, `apellidos`, `categoria`) en
      `backend/src/LFClub.Aplicacion/DTOs/`, y añadir `Dirige` a
      `backend/src/LFClub.Aplicacion/DTOs/PermisosDeActividadDto.cs`. Nombres y tipos como en
      `contracts/api.yaml`.
- [X] T012 [P] Añadir el código `equipo_no_valido` (422) a
      `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeActividades.cs`, con su texto en español.
- [X] T013 Añadir `EquipoIds` (los equipos actuales del jugador de la petición; vacío para los demás
      roles) a `backend/src/LFClub.Aplicacion/Interfaces/QuienPregunta.cs`, un método para leerlos
      en `IRepositorioEquipos` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEquipos.cs`,
      y llenarlo en `backend/src/LFClub.Aplicacion/Utilidades/AccesoAActividad.cs`. Una sola
      consulta, solo para el rol JUGADOR.
- [X] T014 Crear `IRepositorioAudiencia` en
      `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioAudiencia.cs` y
      `RepositorioAudiencia` en `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAudiencia.cs`:
      dado un entrenamiento, devuelve los jugadores activos de su audiencia natural (según el
      cálculo de data-model.md, incluidas las filas `EQUIPO_DESACTIVADO`) y sus añadidos, en una
      sola consulta del club; y dados varios entrenamientos o una lista de categorías y equipos,
      lo mismo sin entrenamiento guardado (para avisar antes de crear). Registrarlo en
      `backend/src/LFClub.Api/Configuracion/RegistroDeServicios.cs`.
- [X] T015 Crear `ArmadoDeAudiencia` en
      `backend/src/LFClub.Aplicacion/Utilidades/ArmadoDeAudiencia.cs`: valida que cada equipo exista
      en el club, esté activo y sea de una de las categorías activas de la actividad (si no,
      `equipo_no_valido`), conserva los equipos desactivados que el entrenamiento ya tenía
      (RF-026), y valida que cada añadido sea un jugador activo, aprobado, no retirado y con
      categoría del club (si no, `jugador_fuera_de_la_actividad`); acepta sin duplicar al que ya es
      de la audiencia natural. Sin dependencias de EF Core.
- [X] T016 Pruebas unitarias de `ArmadoDeAudiencia` en
      `backend/pruebas/Unitarias/Utilidades/ArmadoDeAudienciaPruebas.cs`: equipo de otra categoría,
      equipo inactivo, categoría inactiva, equipo desactivado conservado, añadido retirado, añadido
      repetido y añadido de la audiencia natural.

**Punto de control**: la solución compila, la migración se aplica y las pruebas de la 001 a la 012
siguen en verde.

---

## Fase 3: Historia de usuario 1 - Publicar un entrenamiento solo para un equipo (Prioridad: P1) 🎯 MVP

**Objetivo**: programar un entrenamiento para uno o varios equipos; solo lo ven y reciben su aviso
los jugadores de esos equipos, quienes entrenan la categoría y el PRESIDENTE.

**Prueba independiente**: una categoría con los equipos A y B y jugadores en cada uno, en ambos y en
ninguno; se programan uno para A, otro para B, otro para A y B y otro sin equipos; se comprueba qué
ve cada familia, qué aviso recibe y qué ve quien entrena (quickstart, bloque 1).

### Implementación de la historia 1

- [X] T017 [US1] Traducir la visibilidad a SQL en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEntrenamientos.cs` y
      `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioEntrenamientos.cs`: `Visibles(quien)`
      añade el `EXISTS` sobre `EntrenamientosEquipo`, `EntrenamientosJugador` y los equipos del
      jugador (forma de research §3); el DIRECTIVO sin asignar ve los que no tienen equipos o los
      que entrena; `ListarVisibles`, `ProximosVisibles` y la carga del detalle incluyen `Equipos`.
      Documentar la equivalencia con `ReglaVisibilidadDeEntrenamiento`.
- [X] T018 [US1] Que el calendario, los próximos y los contadores del inicio usen la misma condición:
      revisar `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConsultaEntrenamientos.cs` y
      las consultas de calendario e inicio que lean entrenamientos (buscar con `Grep` quien llama a
      `Visibles`/`ListarVisibles` y a `EntrenamientosCategoria` para filtrar por categoría) y
      corregir las que filtren solo por categoría. Añadir a `EntrenamientoResumenDto` y
      `EntrenamientoDto` (`backend/src/LFClub.Aplicacion/DTOs/`) la lista `Equipos`
      (`EquipoDeActividadDto`), y mapearla en
      `backend/src/LFClub.Aplicacion/Mappers/MapperEntrenamientos.cs`.
- [X] T019 [US1] Programar con equipos: añadir `EquipoIds` opcional a `GuardarEntrenamientoDto`
      (`backend/src/LFClub.Aplicacion/DTOs/GuardarEntrenamientoDto.cs`), validarlo con
      `ArmadoDeAudiencia` desde
      `backend/src/LFClub.Aplicacion/Validadores/ValidadorEntrenamiento.cs`, guardar las filas de
      `EntrenamientosEquipo` y marcar cada equipo como `Usado` en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioEntrenamientos.cs` con el bloqueo del
      club. El `PUT /entrenamientos/{id}` acepta `equipoIds` solo si coinciden con los actuales; si
      no, 422 apuntando a la audiencia (research §5). Un equipo sin jugadores se guarda (casos
      límite).
- [X] T020 [US1] Responder y planilla por audiencia: en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioRespuestas.cs` y en el permiso
      `Responde` (`MapperEntrenamientos.cs`), usar `ReglaAudienciaDeEntrenamiento.EnLaAudiencia`;
      tener solo asistencia registrada permite ver, no responder. En
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAsistencia.cs`,
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAsistencias.cs` y
      `backend/src/LFClub.Aplicacion/Mappers/MapperAsistencia.cs`, la planilla parte de la
      audiencia: con equipos trae a los jugadores activos de esos equipos (y de los congelados) que
      estén en una categoría marcable, sin equipos como hoy, más los que ya tienen asistencia en una
      categoría marcable.
- [X] T021 [US1] Avisos a la audiencia: en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAvisosDeActividad.cs` y
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAvisos.cs`, los hechos de
      entrenamiento (programado, cambiado, cancelado, reactivado, asistencia registrada) resuelven
      destinatarios con la cobertura `AUDIENCIA_DE_ENTRENAMIENTO` por `entrenamientoId` en una sola
      consulta; `AsistenciaRegistrada` avisa a la audiencia cuya categoría copiada está entre las que
      cambiaron. Un jugador del equipo B no recibe nada del equipo A (RF-024).
- [X] T022 [US1] Congelar la audiencia al desactivar un equipo (RF-026): en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEquipos.cs`, `DesactivarAsync`
      copia, en la misma transacción y antes de borrar `JugadoresEquipo`, a cada jugador activo del
      equipo a `EntrenamientosJugador` con `Origen = EQUIPO_DESACTIVADO` y `EquipoDeOrigenId` en cada
      entrenamiento dirigido a él con `Fecha >= hoy`, cancelados incluidos; el entrenamiento
      conserva su fila de equipo y no se cancela. `ServicioEquipos.DesactivarAsync`
      (`backend/src/LFClub.Aplicacion/Implementaciones/ServicioEquipos.cs`) recibe `IReloj` para
      calcular `hoy` en hora de Colombia. Vigilar el tamaño de `RepositorioEquipos.cs`.
- [X] T023 [P] [US1] Tipos y selector en el frontend: añadir `equipos`, `equipoIds` y `permisos.dirige`
      a `frontend/src/compartido/api/tiposActividades.ts`; que
      `frontend/src/privado/actividades/useCategoriasGestionables.ts` entregue los equipos activos
      de cada categoría; crear `frontend/src/privado/actividades/SelectorEquipos.tsx` (casilla por
      equipo activo de las categorías elegidas; sin ninguno marcado dice "Toda la categoría") y
      `frontend/src/privado/actividades/EtiquetaDePertenencia.tsx` ("Equipo A", "Añadido", "Equipo
      desactivado", siempre en texto).
- [X] T024 [US1] Pantallas: en `frontend/src/privado/actividades/FormularioEntrenamiento.tsx`, mostrar
      "Equipos" bajo las categorías solo si alguna de las elegidas tiene equipos activos (RF-005) y
      enviar `equipoIds`; con varios días a la vez, enviar los mismos equipos en cada petición
      (RF-007). En `frontend/src/privado/entrenamientos/DetalleEntrenamiento.tsx` y
      `frontend/src/privado/calendario/TarjetaActividad.tsx` mostrar "Para: 2014 · Equipo A" cuando
      hay equipos. Depende de T023.

### Pruebas de la historia 1

- [X] T025 [P] [US1] Pruebas de integración de visibilidad en
      `backend/pruebas/Integracion/Entrenamientos/VisibilidadPorRolPruebas.cs`: A, B, A y B, y
      ninguno; jugador en dos equipos lo ve una sola vez; jugador sin equipo no ve el de un equipo y
      sí el de toda la categoría; PRESIDENTE, ENTRENADOR y DIRECTIVO asignado lo ven; DIRECTIVO sin
      asignar no ve el de equipos; ENTRENADOR de otra categoría, 404; calendario, listados,
      contadores, campana y `GET` por identificador con 404 para quien no lo ve. Incluir una prueba
      que compara la regla pura con la consulta por rol.
- [X] T026 [P] [US1] Pruebas de integración en
      `backend/pruebas/Integracion/Entrenamientos/EntrenamientoPorEquipoPruebas.cs`: crear con
      equipos, `equipo_no_valido` (de otra categoría, inactivo, de otro club), varios días a la vez,
      `PUT` con otros equipos rechazado, responder y registrar asistencia, equipo sin jugadores, y
      entrenamientos sin equipos idénticos a antes (CE-005).
- [X] T027 [P] [US1] Pruebas de integración en
      `backend/pruebas/Integracion/Entrenamientos/EquipoDesactivadoPruebas.cs`: al desactivar el
      equipo A, J1 y J2 siguen viendo el entrenamiento próximo, J3 no, no pasa a toda la categoría,
      no se cancela y los de fecha pasada no se copian; un equipo dirigido por un entrenamiento no
      se borra (`Usado`).
- [X] T028 [P] [US1] Pruebas de integración en
      `backend/pruebas/Integracion/Avisos/AvisosDeAudienciaPruebas.cs` (parte de la historia 1): al
      programar para el equipo A solo reciben aviso los de A; un jugador de A y B recibe uno solo;
      sin equipos recibe toda la categoría como antes.
- [X] T029 [P] [US1] Prueba de componente `frontend/pruebas/SelectorEquipos.test.tsx`: no aparece sin
      equipos, "Toda la categoría" sin marcas, marcar y desmarcar, equipos de varias categorías
      agrupados por año.

**Punto de control**: la historia 1 funciona de extremo a extremo y sola ya le sirve al club.

---

## Fase 4: Historia de usuario 2 - Armar la convocatoria de un partido por equipo (Prioridad: P2)

**Objetivo**: la lista de convocables sale agrupada por equipo, con "Marcar a todos" por equipo; el
resto de la convocatoria funciona igual.

**Prueba independiente**: una categoría con dos equipos; se convoca "todos del equipo A", se quita a
uno y se añade uno del equipo B; se comprueba quién ve el partido (quickstart, bloque 2).

### Implementación de la historia 2

- [X] T030 [US2] Planilla de partido con equipos: añadir `Equipos` (los activos de las categorías
      del partido) a `PlanillaPartidoDto` y `EquipoIds` a convocados y convocables en
      `backend/src/LFClub.Aplicacion/DTOs/`, cargarlos en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioConvocatoria.cs` y mapearlos en
      `backend/src/LFClub.Aplicacion/Mappers/MapperPartidos.cs`. La API no cambia lo que se guarda.
- [X] T031 [US2] Un jugador repetido por dos caminos queda convocado una sola vez (RF-011): quitar
      duplicados en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConvocatoria.cs` antes
      de validar y guardar. No cambia la visibilidad del partido (RF-012).
- [X] T032 [P] [US2] Crear `frontend/src/privado/partidos/GrupoDeEquipoConvocatoria.tsx` (encabezado
      con el nombre del equipo, "Marcar a todos" y "Quitar a todos", casillas por jugador) y usarlo
      en `frontend/src/privado/partidos/EditorConvocatoria.tsx` y
      `frontend/src/privado/partidos/SeccionConvocatoria.tsx`: grupos por equipo, grupo "Sin equipo",
      un jugador de varios equipos aparece en cada uno y su marca es la misma en todos. Añadir los
      tipos a `frontend/src/compartido/api/tiposActividades.ts`.

### Pruebas de la historia 2

- [X] T033 [P] [US2] Pruebas de integración en
      `backend/pruebas/Integracion/Partidos/ConvocatoriaPorEquipoPruebas.cs`: la planilla trae los
      equipos y el `equipoIds` de cada jugador, un jugador de dos equipos aparece en ambos, la
      convocatoria con un jugador repetido guarda una fila, y solo los convocados ven el partido.
- [X] T034 [P] [US2] Prueba de componente `frontend/pruebas/EditorConvocatoriaPorEquipo.test.tsx`:
      agrupación, "Marcar a todos", desmarcar a uno, jugador de dos equipos marcado en ambos grupos,
      grupo "Sin equipo".

**Punto de control**: las historias 1 y 2 funcionan por separado.

---

## Fase 5: Historia de usuario 3 - Sumar a jugadores de otros equipos o categorías (Prioridad: P2)

**Objetivo**: quien dirige suma a jugadores de otros equipos o de otras categorías a un entrenamiento
o a un partido; el añadido ve y responde solo esa actividad, sin cambiar de categoría ni de equipo.

**Prueba independiente**: un entrenamiento del equipo A de la 2014 al que se suma un jugador del
equipo B y otro de la 2015; ambos lo ven y responden, siguen en su categoría y equipo y no ven nada
más de la 2014 (quickstart, bloque 3).

### Implementación de la historia 3

- [X] T035 [US3] Relajar la convocatoria: en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConvocatoria.cs`, aceptar a cualquier
      jugador activo, aprobado, no retirado y con categoría del club (si no,
      `jugador_fuera_de_la_actividad`); la fila copia la categoría del propio jugador y nunca la
      cambia (RF-015). Marcar como `Anadido` en `PlanillaPartidoDto` al convocado cuya categoría
      copiada no es una de las del partido (`backend/src/LFClub.Aplicacion/Mappers/MapperPartidos.cs`),
      visible solo para quien gestiona.
- [X] T036 [US3] Que el añadido de un partido reciba lo que le toca: en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAvisos.cs`, la cobertura
      `CATEGORIAS` de un partido ya jugado suma a los convocados (research §8), para que un añadido
      de otra categoría reciba el aviso del resultado; sacar a un añadido no avisa. Revisar que sus
      goles y su asistencia cuentan en `RepositorioEstadisticas` con su propia categoría (RF-019).
- [X] T037 [US3] Añadidos al programar y en la planilla: añadir `JugadorAnadidoIds` opcional a
      `GuardarEntrenamientoDto`, validarlo con `ArmadoDeAudiencia` y guardar las filas `ANADIDO` en
      `ServicioEntrenamientos.cs`; la planilla de entrenamiento (`ServicioAsistencia.cs`,
      `MapperAsistencia.cs`) trae a los añadidos solo para quien dirige, marcados con `anadido`, y
      marca los que él puede marcar; `PlanillaEntrenamientoDto` gana `Anadido`. El `PUT` de datos
      acepta `jugadorAnadidoIds` solo si coinciden con los actuales.
- [X] T038 [US3] Que el añadido vea y responda: `RepositorioEntrenamientos.Visibles` ya incluye las
      filas de `EntrenamientosJugador` (T017); comprobar que el añadido de otra categoría ve **solo**
      esa actividad (ni los demás entrenamientos, partidos ni jugadores de la categoría) y que
      `ServicioRespuestas.cs` le permite responder. Su categoría y sus equipos no cambian.
- [X] T039 [US3] Crear `IServicioAudienciaDeEntrenamiento` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioAudienciaDeEntrenamiento.cs` y
      `ServicioAudienciaDeEntrenamiento` en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAudienciaDeEntrenamiento.cs`: cambiar
      equipos y añadidos de un entrenamiento `PROGRAMADO` (cancelado, `actividad_cancelada`) para
      quien cumple `ReglaDireccionDeActividad` (403 `no_gestiona_actividad` si lo ve y no dirige; 404
      si no lo ve). Con el bloqueo del club: calcula la audiencia antes y después, reemplaza los
      equipos activos y los añadidos conservando los equipos desactivados y las filas
      `EQUIPO_DESACTIVADO`, genera el aviso de "nuevo entrenamiento" (cobertura `ALUDIDOS`, hecho
      `ENTRENAMIENTO_PROGRAMADO`) solo a quienes entran, y a quienes salen no les avisa ni les deja
      nada: borra sus avisos sin leer de esa actividad y su respuesta si ya no ven el entrenamiento y
      no tienen asistencia (data-model.md, research §8).
- [X] T040 [US3] Crear `IServicioJugadoresSumables` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioJugadoresSumables.cs` y
      `ServicioJugadoresSumables` en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioJugadoresSumables.cs`: busca por
      nombres o apellidos entre los jugadores activos del club con categoría, mínimo 2 letras, 20
      resultados como máximo ordenados por apellidos, solo `usuarioRolId`, `nombres`, `apellidos` y
      categoría; sin foto, documento, equipos ni enlace a la ficha (research §9). PRESIDENTE y
      DIRECTIVO o ENTRENADOR asignado a alguna categoría; sin asignación, lista vacía; JUGADOR 403
      `rol_no_autorizado`; una letra, 422 de validación. Consulta en `IRepositorioAudiencia`.
- [X] T041 [US3] Exponer `PUT /api/clubes/{clubId}/entrenamientos/{entrenamientoId}/audiencia`
      en `backend/src/LFClub.Api/Controladores/Club/ControladorEntrenamientos.cs` delegando en
      `ServicioAudienciaDeEntrenamiento`; devuelve el `EntrenamientoDto` con `Equipos` y `Anadidos`
      (solo para quien dirige). Registrar en
      `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs`. **Descomentar la operación** en
      `specs/013-actividades-por-equipo/contracts/api.yaml`: contrato 110, club 86.
- [X] T042 [US3] `PermisosDeActividadDto.Dirige` y `Anadidos`: llenarlos en
      `MapperEntrenamientos.cs` con `ReglaDireccionDeActividad`; el DTO que recibe una cuenta de
      jugador nunca lleva `anadidos` ni la lista de convocados (RF-018, RF-024).
- [X] T043 [US3] Exponer `GET /api/clubes/{clubId}/actividades/jugadores-sumables?busqueda=` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorJugadoresSumables.cs` delegando en
      `ServicioJugadoresSumables`; registrarlo en `RegistroDeCasosDeUso.cs`. **Descomentar la
      operación** en `contracts/api.yaml`: contrato 111, club 87.
- [X] T044 [P] [US3] Crear `frontend/src/privado/actividades/BuscadorDeJugadores.tsx`: campo con
      mínimo 2 letras, resultados con nombre y categoría (año), manejo con teclado, estado vacío que
      dice qué hacer ("Escribe al menos 2 letras del nombre o apellido") y sin enlace a la ficha.
      Añadir los tipos a `frontend/src/compartido/api/tiposActividades.ts`.
- [X] T045 [US3] Pantallas de añadidos: crear `frontend/src/privado/entrenamientos/DialogoAudiencia.tsx`
      (lista de añadidos con "Quitar", `BuscadorDeJugadores` para sumar, guardar con el `PUT` de la
      audiencia, `DialogoConfirmacion` al quitar), mostrarlo desde
      `frontend/src/privado/entrenamientos/DetalleEntrenamiento.tsx` solo si `permisos.dirige`, marcar
      "Añadido" en `frontend/src/privado/entrenamientos/PlanillaAsistencia.tsx`, y ofrecer el
      buscador en `frontend/src/privado/partidos/EditorConvocatoria.tsx` para sumar a quien no está
      en la lista. El añadido se lee "Añadido" en texto (research §13). Depende de T041, T043, T044.

### Pruebas de la historia 3

- [X] T046 [P] [US3] Pruebas de integración en
      `backend/pruebas/Integracion/Entrenamientos/AudienciaDeEntrenamientoPruebas.cs`: el añadido
      del equipo B y el de otra categoría ven y responden solo ese entrenamiento y no ven nada más de
      la categoría; su categoría y sus equipos no cambian; la familia de otro jugador no ve añadidos
      ni convocados; quitar no genera aviso ni deja respuesta ni texto; quien no dirige recibe 403 y
      quien no lo ve 404; entrenamiento cancelado, 409; retirado, 422; sin duplicar al que ya es de
      la audiencia natural; ENTRENADOR de solo una de las categorías sí puede cambiar la audiencia.
- [X] T047 [P] [US3] Pruebas de integración en
      `backend/pruebas/Integracion/Partidos/JugadorAnadidoAPartidoPruebas.cs`: convocar a un jugador
      de otra categoría, lo ve y responde, no cambia su categoría ni equipos, publica el resultado
      de su hijo y sus goles cuentan en sus estadísticas, y sacarlo no avisa.
- [X] T048 [P] [US3] Pruebas de integración de jugadores sumables en
      `backend/pruebas/Integracion/Entrenamientos/JugadoresSumablesPruebas.cs`: máximo 20, solo
      nombre y categoría, mínimo 2 letras, retirado no aparece, JUGADOR 403, DIRECTIVO sin asignar
      lista vacía, otro club y DESARROLLADOR 404.
- [X] T049 [P] [US3] Prueba de componente `frontend/pruebas/BuscadorDeJugadores.test.tsx`: no busca
      con una letra, muestra resultados, se elige con teclado, estado vacío legible, no hay enlace a
      la ficha.

**Punto de control**: las historias 1 a 3 funcionan; el contrato está en 111/87.

---

## Fase 6: Historia de usuario 4 - Ver y corregir a quién se dirige una actividad (Prioridad: P3)

**Objetivo**: quien dirige ve en el detalle a qué equipos va y quiénes fueron añadidos, y corrige
equipos y añadidos sin cancelar y volver a crear.

**Prueba independiente**: se cambia el equipo de un entrenamiento próximo de A a B y se comprueba
qué ve cada familia y qué avisos nuevos hay (quickstart, bloque 4).

### Implementación de la historia 4

- [X] T050 [US4] Que el cambio de equipos avise solo a quien entra: en
      `ServicioAudienciaDeEntrenamiento.cs` y `ArmadoDeAudiencia.cs`, cambiar A por B avisa a los que
      entran por B y que no estaban, no repite el aviso a quien estaba en ambos (J2) y no avisa a
      quien sale (J1); cancelar y reactivar no tocan la audiencia. Los equipos desactivados que ya
      tenía se conservan y no se envían (RF-026).
- [X] T051 [US4] El detalle para quien dirige: `EntrenamientoDto` lleva `Equipos` (con `activo`) para
      todos los que lo ven y `Anadidos` solo para quien dirige, distinguidos de la audiencia natural
      (RF-021); las filas `EQUIPO_DESACTIVADO` se muestran como audiencia natural y no como
      "añadidos". Ajustar `MapperEntrenamientos.cs` y la planilla (`Anadido` solo para quien dirige).
- [X] T052 [P] [US4] Crear `frontend/src/privado/entrenamientos/SeccionAudiencia.tsx` (para todos
      "Para: 2014 · Equipo A", con "Equipo desactivado" en texto cuando corresponde; para quien dirige,
      además "Jugadores añadidos" con `EtiquetaDePertenencia` y el botón "Cambiar audiencia") y usarla
      en `frontend/src/privado/entrenamientos/DetalleEntrenamiento.tsx`.
- [X] T053 [US4] Editar equipos desde el detalle: ampliar `DialogoAudiencia.tsx` con
      `SelectorEquipos` (los equipos activos de las categorías del entrenamiento; los desactivados
      que ya tenía aparecen fijos y marcados), de modo que se corrija equipos y añadidos en un solo
      guardado. Depende de T045 y T052.

### Pruebas de la historia 4

- [X] T054 [P] [US4] Ampliar `backend/pruebas/Integracion/Avisos/AvisosDeAudienciaPruebas.cs` con la
      historia 4: cambiar de A a B (J1 deja de verlo sin aviso, J3 lo ve con un aviso nuevo, J2 no
      recibe uno repetido), el aviso de quien sale no existe en su campana y la prueba sobre
      `GET /campana` de J1 no muestra rastro.
- [X] T055 [P] [US4] Ampliar `AudienciaDeEntrenamientoPruebas.cs` con: el detalle de quien dirige
      trae `equipos` y `anadidos`; el de una familia solo `equipos`; cancelar y reactivar no cambian
      la audiencia; conservar el equipo desactivado al cambiar.

**Punto de control**: las cuatro historias funcionan.

---

## Fase 7: Cierre y transversal

**Propósito**: contrato, aislamiento, pantallas y batería completa.

- [X] T056 Contrato sin marcas: `contracts/api.yaml` sin operaciones comentadas; comparar los
      DTOs y códigos con Swagger de la API en marcha; `AccesoClubPruebas`
      (`backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`) cuenta **111** operaciones y
      **87** de club, con las dos rutas nuevas devolviendo 404 a otro club y al DESARROLLADOR.
- [X] T057 [P] Prueba de aislamiento y de no filtración en
      `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` y
      `backend/pruebas/Integracion/Entrenamientos/VisibilidadPorRolPruebas.cs`: un entrenamiento de
      un equipo del club A no se ve ni se encuentra por identificador con la sesión del club B, y el
      buscador no devuelve jugadores de otro club.
- [ ] T058 [P] Revisión de pantallas a 360 px y en escritorio, tema claro y oscuro, con 4 equipos
      marcados en el formulario, el diálogo de audiencia, la convocatoria agrupada y el buscador:
      sin desplazamiento horizontal, pertenencia en texto, foco visible. Ajustar los archivos de
      `frontend/src/privado/actividades/`, `entrenamientos/`, `partidos/` y `calendario/`.
- [ ] T059 Ejecutar el recorrido de `specs/013-actividades-por-equipo/quickstart.md` (bloques 1 a 5,
      aislamiento y pantallas) en un proyecto de Compose aparte (`docker compose -p
      lfclub-validacion …`) con `Brevo__Llave` vacía; registrar lo que no se pudo comprobar.
- [X] T060 Batería entera: `dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `tsc`,
      `eslint` y `scripts/verificar-tamano.ps1`; las especificaciones 001 a 012 en verde.
- [X] T061 [P] Documentación: ampliar la documentación XML de las clases que cambiaron, y comprobar
      que `plan.md` (estructura y contador 111/87) y `contracts/api.yaml` coinciden con lo
      construido; anotar en `plan.md` las notas 1 a 4 que el propietario resolvió en la validación.

---

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación (fase 1)**: sin dependencias; T002 fija los números del contrato.
- **Base (fase 2)**: depende de la preparación y bloquea todas las historias. Dentro de ella: T003 y
  T004 → T005 → T006; T007 → T009; T013 y T014 dependen de T004/T005; T015 depende de T012 y T014.
- **Historia 1 (fase 3)**: depende de la base. Es el MVP.
- **Historia 2 (fase 4)**: depende de la base; independiente de la historia 1 en pruebas, aunque
  comparte `tiposActividades.ts`.
- **Historia 3 (fase 5)**: depende de la base y de T017 (consulta de visibilidad con añadidos) y de
  T019 (`ArmadoDeAudiencia` en el servicio).
- **Historia 4 (fase 6)**: depende de la historia 3 (`ServicioAudienciaDeEntrenamiento` y
  `DialogoAudiencia`).
- **Cierre (fase 7)**: depende de las historias que se quieran entregar.

### Dentro de cada historia

- Reglas y DTOs antes que servicios; servicios antes que controladores; controladores antes que
  pantallas; las pruebas de integración se escriben con la implementación y deben fallar sin ella.
- Tareas sobre el mismo archivo no son paralelas: `tiposActividades.ts` (T023, T032, T044),
  `ServicioEntrenamientos.cs` (T019, T037), `ServicioConvocatoria.cs` (T031, T035),
  `DetalleEntrenamiento.tsx` (T024, T045, T052) y `EditorConvocatoria.tsx` (T032, T045).

### Oportunidades de paralelismo

- Base: T003, T004, T007, T008, T010, T011 y T012 en paralelo.
- Historia 1: T023 en paralelo con T017 a T022; T025 a T029 en paralelo al final.
- Historia 2: T032 en paralelo con T030/T031; T033 y T034 en paralelo.
- Historia 3: T044 en paralelo con T035 a T043; T046 a T049 en paralelo.
- Historia 4: T052 en paralelo con T050/T051; T054 y T055 en paralelo.
- Con varias personas: tras la base, la historia 1 y la 2 pueden avanzar a la vez.

### Ejemplo de paralelismo: base

```text
Tarea: "Enumeración OrigenDeJugadorEnEntrenamiento en backend/src/LFClub.Dominio/Enumeraciones/"
Tarea: "Entidades EntrenamientoEquipo y EntrenamientoJugador en backend/src/LFClub.Dominio/Entidades/"
Tarea: "ReglaAudienciaDeEntrenamiento y sus pruebas en backend/src/LFClub.Dominio/Reglas/"
Tarea: "ReglaDireccionDeActividad y sus pruebas en backend/src/LFClub.Dominio/Reglas/"
Tarea: "DTOs nuevos en backend/src/LFClub.Aplicacion/DTOs/"
```

---

## Estrategia de implementación

### Primero el MVP (solo la historia 1)

1. Completar la fase 1 (preparación) y la fase 2 (base).
2. Completar la fase 3: entrenamiento por equipo con su visibilidad, avisos y congelado.
3. **Parar y validar**: quickstart bloque 1 y bloque 5 (cambio de equipo y equipo desactivado).
4. Entregar si el club ya puede publicar entrenamientos distintos para cada equipo.

### Entrega incremental

1. Base lista → historia 1 (MVP) → validar.
2. Historia 2: convocatoria por equipo → validar.
3. Historia 3: sumar jugadores (contrato 111/87) → validar.
4. Historia 4: ver y corregir → validar.
5. Cierre: aislamiento, pantallas, recorrido completo y batería.

---

## Notas

- [P] = archivos distintos y sin dependencias entre sí.
- La etiqueta [US#] enlaza cada tarea con su historia de la especificación.
- Cada historia debe poder terminarse y probarse por separado.
- Commit después de cada tarea o grupo lógico.
- Evitar: tareas vagas, conflictos en el mismo archivo y dependencias entre historias que rompan su
  independencia.
- Las notas 1 a 4 del plan (DIRECTIVO sin asignar, quien dirige, buscador y categoría sin equipos
  elegidos) están implementadas como dice el plan; si el propietario decide otra cosa, cada una es
  un cambio localizado en la regla indicada.
