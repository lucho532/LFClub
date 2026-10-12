---

description: "Lista de tareas de la funcionalidad 003: categorías del club"
---

# Tareas: Categorías del club

**Entrada**: documentos de diseño en `specs/003-categorias-club/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 3.8.0) y el plan. No van en tareas
aparte: por §27.1, cada tarea de una historia es un corte vertical que entrega el backend, la
pantalla que lo usa contra la API real y sus pruebas.

**Organización**: las tareas se agrupan por historia de usuario para poder implementar y probar
cada historia por separado.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US7]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de `specs/001-base-multiclub/tasks.md` y `specs/002-ingreso-club/tasks.md`, que siguen
vigentes. Resumen y lo que se añade:

- **Nombres** (§2.1, §2.2): todo en español y con el vocabulario del dominio: `Categoria`, `Equipo`,
  `AsignacionEntrenadorCategoria`, Jugador, Entrenador. Sin sinónimos.
- **Carpetas de Aplicacion**: `Servicios/` guarda los contratos `IServicio*`; `Implementaciones/`,
  sus clases; `Interfaces/`, los `IRepositorio*`. Cada servicio o colaborador nuevo se registra en
  `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs` y cada repositorio nuevo en
  `backend/src/LFClub.Api/Configuracion/RegistroDeServicios.cs`.
- **Documentación** (§3): cada clase, interfaz y enumeración nueva lleva documentación XML en
  español (qué representa, su responsabilidad y qué no debe asumir). En las que cambian de
  responsabilidad se actualiza.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Si un archivo se acerca,
  se divide por responsabilidad dentro de la misma carpeta (vale también para las clases de
  pruebas).
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores no tienen
  reglas de negocio. Entidad → `MapperCategorias` → DTO; ninguna entidad sale por la API.
- **Contrato**: `specs/003-categorias-club/contracts/api.yaml` manda en rutas, DTOs, códigos de
  estado y códigos de error. Los DTOs van en `backend/src/LFClub.Aplicacion/DTOs/` con los
  nombres del contrato. Los errores de esta funcionalidad se crean en
  `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeCategorias.cs`, cada uno con su `codigo` y
  un mensaje en español que diga a la persona qué puede hacer.
- **Aislamiento** (§7.1): los cuatro repositorios nuevos van en
  `backend/src/LFClub.Infraestructura/Repositorios/`, trabajan dentro del club de
  `IContextoClub`, con el filtro global activo, y **no** usan `IgnoreQueryFilters()`. Un
  identificador de categoría, equipo o integrante que no es de este club es `404 no_encontrado`.
- **Autorización** (§15, RF-036): todo endpoint que cambia algo lleva
  `[IntegranteDelClub(Rol.PRESIDENTE)]`; a otro rol, `403 rol_no_autorizado`. Las confirmaciones
  las pide la pantalla con el `DialogoConfirmacion` existente; la API no las pide.
- **Concurrencia** (research §4): toda operación que ubica jugadores, cambia el estado de una
  categoría o toca los equipos de un jugador se ejecuta dentro de
  `IUnidadDeTrabajo.EnTransaccionAsync` y **empieza** bloqueando la fila del club (T005).
- **Listas de jugadores** (RF-032): nombre, apellidos, año de nacimiento, equipos y la marca
  `fueraDeSuAnio`. Nunca documento, correo ni celular.
- **Pantallas** (RF-040, §24): componentes `Tabla`, `Tarjeta`, `EtiquetaEstado` y
  `DialogoConfirmacion` existentes; legibles a 360 px y en los dos temas; "inactiva" y "fuera de
  su año" se dicen con texto además de color. Las acciones solo se pintan al PRESIDENTE.
- **No regresión** (§27.2): las pruebas de la 001 y la 002 siguen en verde sin cambiar lo que
  comprueban, salvo los contadores de `AccesoClubPruebas.cs`, que cada tarea indica.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, y pasa
  `scripts/verificar-tamano.ps1`.

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: partir de una base en verde. No hay proyectos, paquetes ni tecnologías nuevas.

- [X] T001 Comprobar la línea base en la rama `003-categorias-club`: ejecutar `dotnet test backend/LFClub.sln`, `npm --prefix frontend test` y `scripts/verificar-tamano.ps1`, y confirmar que todo pasa antes de tocar código. Si algo falla, detenerse e informarlo: no se construye sobre una base rota
- [X] T002 [P] Añadir a `frontend/src/compartido/api/tipos.ts` todos los tipos nuevos del contrato 003 con sus mismos nombres y campos: `RolDeEntrenador`, `CrearCategoriaDto`, `NombreEquipoDto`, `EquiposDeEntrenadorDto`, `UbicarJugadorDto`, `EquipoDto`, `EquipoDeReferenciaDto`, `EntrenadorDeCategoriaDto`, `CandidatoEntrenadorDto`, `JugadorDeCategoriaDto`, `CategoriaDto`, `CategoriaDetalleDto` (`CategoriaDto` más `jugadores`), `CategoriaConUbicadosDto`, `JugadorRetiradoDto`, `ReincorporacionDto` (`categoriaId` y `anio` pueden ser nulos), `EntrenadorParaFamiliaDto` y `MiCategoriaDto` (`categoria` nulo si no tiene). El campo `retirado` de `ClubDeSesionDto` no se añade aquí: llega con T018, junto con el backend que lo rellena

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: modelo y migración, reglas de dominio, bloqueo del club, ubicación automática,
datos de prueba y prueba de contrato.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase

- [X] T003 Crear el modelo y la migración (data-model.md). En `backend/src/LFClub.Dominio/Entidades/`, cinco entidades nuevas que implementan `IPerteneceAClub`: **`Categoria.cs`** (`Id`; `ClubId` "Obligatorio. Foránea a `Club`, borrado en cascada"; `Anio` entero "Obligatorio. Año de cuatro cifras no posterior al año en curso (RF-002). No se puede cambiar (RF-004). Es el nombre de la categoría (RF-001)"; `Activa` "Obligatorio. Nace en verdadero"; `Usada` "Obligatorio. Nace en falso; pasa a verdadero la primera vez que entra un jugador o un entrenador y no vuelve a falso"; `CreadaEn` UTC); **`Equipo.cs`** (`Id`; `ClubId`; `CategoriaId` "Obligatorio. Foránea a `Categoria`, borrado en cascada. No se puede cambiar"; `Nombre` texto (30) "Obligatorio, sin espacios sobrantes, 30 caracteres como máximo (RF-023)"; `NombreNormalizado` texto (30) "`Nombre` en minúsculas. Solo para la unicidad"; `Activo` "Obligatorio. Nace en verdadero. No existe la reactivación"; `Usado` "Obligatorio. Nace en falso; pasa a verdadero la primera vez que tiene un jugador o un entrenador"; `CreadoEn` UTC); **`AsignacionEntrenadorCategoria.cs`** (`Id`; `ClubId`; `CategoriaId` y `UsuarioRolId` obligatorios, foráneas con borrado en cascada; `Activa` obligatorio; `CreadaEn` UTC); **`EntrenadorEquipo.cs`** (clave primaria compuesta `AsignacionEntrenadorCategoriaId` + `EquipoId`, las dos foráneas con borrado en cascada; `ClubId`); **`JugadorEquipo.cs`** (clave primaria compuesta `UsuarioRolId` + `EquipoId`, las dos foráneas con borrado en cascada; `ClubId`). En `UsuarioRol.cs`, cinco campos: `CategoriaId` Guid "Opcional. Foránea a `Categoria`, sin cascada. Categoría actual del jugador (RF-013)"; `Activo` "Obligatorio. Verdadero por defecto, también para las filas que ya existen. Falso significa retirado del club (§14.1)"; `RetiradoEn` "Opcional. UTC. Solo tiene valor mientras está retirado"; `RetiradoPorUsuarioId` "Opcional. Foránea a `Usuario`; pasa a nulo si esa cuenta se elimina"; `RetiradoPorNombre` texto (161) "Opcional. Nombres y apellidos de quien lo retiró, copiados en ese momento (§13)". Configuraciones en `backend/src/LFClub.Infraestructura/Datos/Configuraciones/` (una por entidad nueva y `ConfiguracionUsuarioRol.cs`): índice único `(ClubId, Anio)` en `Categorias`; índice único **parcial** `(CategoriaId, NombreNormalizado)` sobre los equipos con `Activo` verdadero; índice único `(CategoriaId, UsuarioRolId)` e índice `(UsuarioRolId, Activa)` en `AsignacionesEntrenadorCategoria`; índice `(ClubId, CategoriaId)` en `UsuariosRol`; `RetiradoPorUsuarioId` con borrado `SetNull`; todas las foráneas a `Club` en cascada. Añadir los cinco `DbSet` a `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs` (tablas `Categorias`, `Equipos`, `AsignacionesEntrenadorCategoria`, `EntrenadoresEquipo`, `JugadoresEquipo`) y los nombres de los dos índices únicos de negocio a `backend/src/LFClub.Aplicacion/Utilidades/IndicesUnicos.cs`. Crear la migración `CategoriasDelClub` en `backend/src/LFClub.Infraestructura/Datos/Migraciones/`: cinco tablas, cinco columnas y un índice en `UsuariosRol`; no mueve datos y deja `Activo` en verdadero en las filas existentes. `backend/pruebas/Integracion/Aislamiento/ModeloPruebas.cs` debe pasar sin tocarlo: prueba que las cinco entidades tienen filtro y foránea a `Club` en cascada
- [X] T004 [P] Crear las reglas de dominio en `backend/src/LFClub.Dominio/Reglas/`, sin dependencias de HTTP ni de EF: `ReglaAnioDeCategoria.cs` ("entero entre 1000 y el año en curso"; recibe el año en curso, que quien la llama saca de la fecha UTC de `IReloj`, supuesto 1); `ReglaEntrenadorAsignable.cs` (asignable = `EstadoIngreso` `APROBADO` **y** rol `ENTRENADOR`, `DIRECTIVO` o `PRESIDENTE`; nadie más, RF-017); `ReglaAlcanceDeCategorias.cs` (a partir solo del rol: PRESIDENTE y DIRECTIVO ven todas las categorías, activas e inactivas, y las listas "Sin categoría" y "Retirados"; ENTRENADOR, solo las activas con una asignación activa suya y ninguna de las dos listas; JUGADOR, ninguna; tener una asignación no cambia el alcance de un PRESIDENTE ni de un DIRECTIVO, RF-017a). Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaAnioDeCategoriaPruebas.cs` (999, 1000, el año en curso, el siguiente, 0 y negativos), `ReglaEntrenadorAsignablePruebas.cs` (toda la tabla rol × estado de ingreso, incluido JUGADOR aprobado y ENTRENADOR en espera) y `ReglaAlcanceDeCategoriasPruebas.cs` (los cuatro roles, con y sin asignación, con la categoría activa e inactiva)
- [X] T005 Bloqueo del club y ubicación automática (research §4; depende de T003). En `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioClub.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioClub.cs`: `BloquearAsync`, un `SELECT ... FOR UPDATE` sobre la fila del club de `IContextoClub` (el mismo recurso que ya usa `RepositorioClubesPlataforma.ObtenerBloqueandoAsync`). Crear `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioJugadores.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioJugadores.cs` con las dos sentencias de ubicación, cada una un único `ExecuteUpdateAsync` condicionado a `Rol == JUGADOR && EstadoIngreso == APROBADO && Activo && CategoriaId == null`: **ubicar a uno** (fija la categoría de ese integrante) y **recoger a los de un año** (fija la categoría de todos los nacidos entre el 1 de enero y el 31 de diciembre de ese año, comparando `FechaNacimiento` con el intervalo, sin columna calculada; devuelve cuántos entraron). Crear `backend/src/LFClub.Aplicacion/Utilidades/UbicadorDeJugadores.cs` con dos operaciones que **exigen** que quien las llama ya tenga el club bloqueado dentro de su transacción: `UbicarAUnoAsync` (busca la categoría **activa** del año de nacimiento del jugador; si existe, lo ubica y la marca `Usada`; si no, no hace nada) y `RecogerAsync(categoria)` (recoge a los de su año, marca `Usada = true` si entró alguno y devuelve el número). Nunca mueve a quien ya tiene categoría (RF-011) y nunca crea un `JugadorEquipo` (RF-026). Registrar repositorio y colaborador. Sus pruebas llegan por los endpoints que lo usan (T008, T009 y T011)
- [X] T006 Datos de prueba (depende de T003). Crear `backend/pruebas/Integracion/Base/SembradorCategorias.cs`, accesible desde la `FabricaApi` igual que `Sembrador`, sin cambiar ninguna llamada existente: `CrearJugadorAsync(club, anioNacimiento, categoria = null)` (integrante `JUGADOR`, `APROBADO`, activo, con `FechaNacimiento` en ese año); `CrearJugadorEnEsperaAsync(club, anioNacimiento)`; `CrearCategoriaAsync(club, anio, activa = true, usada = false)`; `CrearEquipoAsync(categoria, nombre, activo = true)`; `AsignarEntrenadorAsync(categoria, integrante, activa = true)` (marca la categoría como usada); `PonerEnEquipoAsync(jugador, equipo)`; `DirigirEquipoAsync(asignacion, equipo)`; `RetirarAsync(jugador, quienRetira)` (deja `Activo` falso, sin categoría ni equipos y con los tres campos del retiro). Crear `backend/pruebas/Integracion/Categorias/EscenarioCategorias.cs`: monta un club con presidente, directivo, entrenador y un jugador, cada uno con su cliente con sesión, más un segundo club con su presidente, para que las pruebas de esta carpeta no repitan el montaje
- [X] T007 [P] Prueba de contrato a 55 endpoints. En `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`: el contrato pasa de 33 a 55 endpoints; volver a añadir la lista expresa `PendientesDeConstruir` con los 22 endpoints nuevos del contrato 003, y `La_api_expone_exactamente_los_endpoints_del_contrato` compara la API con el contrato menos esa lista. Cada tarea quita de la lista los endpoints que construye; al terminar T022 queda vacía y se elimina. Comprobar que `EndpointsDeLaApi.DelContrato()` no cuenta dos veces los cinco endpoints de la 001 y la 002 que el contrato 003 vuelve a describir, y que `POST /api/invitaciones/registro` sigue marcado como anónimo. La prueba de endpoints anónimos no cambia: no hay ninguno nuevo

**Punto de control**: todas las pruebas de la 001 y la 002 siguen en verde; la base de datos tiene
las cinco tablas y la prueba de contrato conoce los 22 endpoints que faltan

---

## Fase 3: Historia 1 - El presidente crea las categorías de su club (Prioridad: P1) 🎯 MVP

**Objetivo**: el PRESIDENTE crea categorías por año de nacimiento, las ve ordenadas por año, las
desactiva, las reactiva y borra las que nunca se usaron. El DIRECTIVO las ve sin poder cambiarlas.

**Prueba independiente**: se inicia sesión como PRESIDENTE de un club sin categorías, se crean dos
categorías y se comprueba que aparecen en la lista ordenadas por año; se desactiva una y se
comprueba que queda marcada como inactiva.

- [X] T008 [US1] Listar y crear categorías (`GET` y `POST /api/clubes/{clubId}/categorias`; depende de T005 y T006). Backend: `Interfaces/IRepositorioCategorias.cs` y `RepositorioCategorias.cs` (listar por `Anio` con sus equipos **activos** por nombre, sus asignaciones **activas** por apellidos con los equipos que dirigen, y el número de jugadores del club con ese `CategoriaId`, cada jugador una sola vez; buscar por año; obtener por identificador; agregar); `Servicios/IServicioCategorias.cs` e `Implementaciones/ServicioCategorias.cs`; `Servicios/IServicioConsultaCategorias.cs` e `Implementaciones/ServicioConsultaCategorias.cs`; `Mappers/MapperCategorias.cs`; DTOs `CrearCategoriaDto`, `CategoriaDto` (con `sePuedeBorrar` = `Usada` en falso), `EquipoDto`, `EquipoDeReferenciaDto`, `EntrenadorDeCategoriaDto` y `CategoriaConUbicadosDto`; `Utilidades/ErroresDeCategorias.cs`; y `backend/src/LFClub.Api/Controladores/Club/ControladorCategorias.cs`. El DTO sale completo desde ahora: `equipos` y `entrenadores` se leen de sus tablas aunque nadie las escriba hasta las historias 3 y 5. **Listar**: `[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]`, todas las categorías, activas e inactivas; ENTRENADOR y JUGADOR reciben `403 rol_no_autorizado` (el ENTRENADOR entra con T021). **Crear**: solo PRESIDENTE; `anio` ausente o rechazado por `ReglaAnioDeCategoria` → `400 datos_invalidos` con el error en `anio` y el motivo; con el club bloqueado, si ya existe la del año → `409 categoria_ya_existe` si está activa y `409 categoria_inactiva_ya_existe` si está inactiva (el mensaje dice que se puede reactivar); la crea activa y llama a `UbicadorDeJugadores.RecogerAsync`; una violación del índice único `(ClubId, Anio)` se traduce al mismo `409 categoria_ya_existe`; responde `201` con la categoría y `jugadoresUbicados`. Frontend: `frontend/src/privado/categorias/Categorias.tsx` en la ruta `categorias` de `/club/:clubId` (añadirla en `frontend/src/App.tsx`): lista por año con número de jugadores, equipos y entrenadores, "Inactiva" con `EtiquetaEstado` y texto, y estado vacío; `frontend/src/privado/categorias/FormularioCrearCategoria.tsx`: un campo de año y un botón, visibles sin abrir nada, con el error por campo, el mensaje de los dos 409 y, al crear, el aviso de cuántos jugadores entraron ("no entró ninguno" con 0); solo se pinta al PRESIDENTE; y el enlace "Categorías" en el menú de `frontend/src/privado/DisposicionClub.tsx`, visible para PRESIDENTE y DIRECTIVO. Pruebas en `backend/pruebas/Integracion/Categorias/GestionCategoriasPruebas.cs`: crear deja la categoría activa, sin jugadores ni entrenadores y con `sePuedeBorrar` verdadero; la lista sale ordenada por año e incluye las inactivas; año vacío, `abcd`, de tres cifras y el año próximo → 400; repetida activa y repetida inactiva, cada una con su código; dos altas simultáneas del mismo año (`Task.WhenAll`) dan un 201 y un 409 y queda una sola fila; crear con dos jugadores sin categoría de ese año devuelve `jugadoresUbicados` 2 y `numeroJugadores` 2; dos clubes crean el mismo año y ninguno ve la del otro (escenario 1.7); el DIRECTIVO lista y no crea (403); ENTRENADOR y JUGADOR, 403 en los dos; sin sesión, 401. En `AccesoClubPruebas.cs`: quitar estos dos endpoints de `PendientesDeConstruir` y subir el contador de endpoints de club de 10 a 12
- [X] T009 [US1] Desactivar, reactivar y borrar (`POST …/categorias/{categoriaId}/desactivacion`, `POST …/reactivacion` y `DELETE …/categorias/{categoriaId}`; depende de T008). Backend: en `ServicioCategorias.cs`, `RepositorioCategorias.cs` y `ControladorCategorias.cs`, las tres solo PRESIDENTE y con el club bloqueado. **Desactivar**: `409 categoria_con_jugadores` si algún integrante tiene su `CategoriaId` (el mensaje dice que antes hay que pasarlos a otra categoría o retirarlos, RF-005); si pasa, en la misma transacción pone `Activa = false`, desactiva sus asignaciones y borra los `EntrenadorEquipo` de ellas (RF-006); sus equipos no cambian de estado; desactivar una ya inactiva no tiene efecto; `200` con `CategoriaDto`. **Reactivar**: `Activa = true`, queda sin entrenadores (no reactiva ninguna asignación) y llama a `UbicadorDeJugadores.RecogerAsync`; `200` con `CategoriaConUbicadosDto`; el contrato no define ningún 409, así que reactivar una ya activa responde 200 sin efecto y `jugadoresUbicados` 0. **Borrar**: solo con `Usada` en falso, y sus equipos se van por cascada; si no, `409 categoria_con_historial` (el mensaje dice que solo se puede desactivar, RF-004a); `204`. Una categoría de otro club o inexistente → `404 no_encontrado`. No existe ningún endpoint que cambie el año. Frontend: en `Categorias.tsx`, por cada fila y solo para el PRESIDENTE: "Desactivar" (con `DialogoConfirmacion`) en las activas, "Reactivar" en las inactivas (con el aviso de cuántos entraron) y "Borrar" (con `DialogoConfirmacion`) solo cuando `sePuedeBorrar`; ante un 409 muestra el mensaje del servidor y recarga la lista. Pruebas en `GestionCategoriasPruebas.cs`: desactivar una categoría sin jugadores y con un entrenador sembrado la deja inactiva y sin entrenadores, y al reactivarla sigue sin ellos (escenarios 1.4 y 1.6); con un jugador sembrado, 409 y sigue activa (1.5); una categoría con un equipo sin usar se borra con él y el año se puede volver a crear (1.9); una con `Usada` verdadero, aunque hoy esté vacía, da 409 (1.10); una que recogió jugadores al crearse ya no se puede borrar (caso límite); desactivar dos veces no cambia nada. Crear `backend/pruebas/Integracion/Categorias/AislamientoCategoriasPruebas.cs`: el presidente de otro club, en **su** club y con el identificador real de una categoría de este, recibe 404 en detalle, desactivar, reactivar y borrar, y la categoría no cambia (RF-037); DIRECTIVO, ENTRENADOR y JUGADOR reciben 403 en las tres operaciones (escenario 1.8). En `AccesoClubPruebas.cs`: quitar los tres endpoints de `PendientesDeConstruir` y subir el contador a 15

**Punto de control**: el club tiene sus categorías (quickstart, paso 1). Los jugadores que ya
estaban sin categoría entran al crear la de su año; verlos por dentro llega con la historia 2

---

## Fase 4: Historia 2 - Los jugadores aprobados quedan en la categoría de su año (Prioridad: P1)

**Objetivo**: al aprobar a un jugador queda en la categoría activa de su año; si no existe, queda
en "Sin categoría" y entra solo cuando se crea o se reactiva. El club ve "Sin categoría" y los
jugadores de cada categoría.

**Prueba independiente**: con la categoría 2014 creada, se aprueba el ingreso de un jugador nacido
en 2014 y se comprueba que aparece en ella; se aprueba otro nacido en 2016, se comprueba que queda
en "Sin categoría", se crea la categoría 2016 y se comprueba que pasa a ella.

- [X] T010 [US2] "Sin categoría" y detalle de una categoría (`GET /api/clubes/{clubId}/jugadores/sin-categoria` y `GET /api/clubes/{clubId}/categorias/{categoriaId}`; depende de T008). Backend: en `IRepositorioJugadores.cs` / `RepositorioJugadores.cs`, los **sin categoría** ("jugador del club con `CategoriaId` nulo", donde jugador del club = "Rol JUGADOR y EstadoIngreso APROBADO y Activo") y los jugadores de una categoría, ambos por apellidos y con sus equipos activos; en `ServicioConsultaCategorias.cs`, el detalle y la lista; DTOs `CategoriaDetalleDto` y `JugadorDeCategoriaDto` (`fueraDeSuAnio` = año de nacimiento ≠ `Categoria.Anio`, y siempre falso en "Sin categoría"); la acción de detalle en `ControladorCategorias.cs` y un controlador nuevo, `backend/src/LFClub.Api/Controladores/Club/ControladorJugadores.cs`. Los dos endpoints llevan `[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]`; el detalle se abre al ENTRENADOR en T021. Frontend: `frontend/src/privado/categorias/SeccionSinCategoria.tsx`, sección de `Categorias.tsx` con nombre, apellidos y año de nacimiento, y estado vacío; `frontend/src/privado/categorias/DetalleCategoria.tsx` en la ruta `categorias/:categoriaId` (añadirla en `App.tsx`), con la cabecera (año, estado y número de jugadores) y enlace desde cada fila de `Categorias.tsx`; `frontend/src/privado/categorias/SeccionJugadores.tsx`: nombre, apellidos, año de nacimiento, equipos ("sin equipo" si no tiene) y "fuera de su año" con texto y su año de nacimiento (RF-016). Pruebas en `backend/pruebas/Integracion/Categorias/UbicacionAutomaticaPruebas.cs`: "Sin categoría" trae solo a los jugadores aprobados, activos y sin categoría, por apellidos, y no trae a nadie en espera, ni a un ENTRENADOR, un DIRECTIVO o un PRESIDENTE (escenarios 2.4 y 2.5, RF-012); el detalle trae a los jugadores de la categoría y a nadie más; ningún campo de las dos respuestas es documento, correo ni celular (se comprueba sobre el JSON); ENTRENADOR y JUGADOR, 403. En `AislamientoCategoriasPruebas.cs`: el presidente de otro club recibe 404 en el detalle con el identificador real. En `AccesoClubPruebas.cs`: quitar los dos endpoints de `PendientesDeConstruir` y subir el contador a 17
- [X] T011 [US2] Ubicar al aprobar y pruebas completas de la ubicación automática (`POST /api/clubes/{clubId}/ingresos/{usuarioRolId}/aprobacion`, sin cambio de esquema; depende de T010). Backend: `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAprobacionIngreso.cs` pasa a ejecutar, en una transacción con el club bloqueado, la sentencia condicionada de aprobación que ya tiene y, si el rol aprobado es `JUGADOR`, `UbicadorDeJugadores.UbicarAUnoAsync`. Sin categoría activa de ese año, el ingreso se aprueba igual (RF-009). Con `ENTRENADOR` o `DIRECTIVO` no se ubica a nadie. No asigna equipo, no genera cobros y no envía correos; cuerpo, respuestas y códigos son los de la 002. Actualizar la documentación XML de la clase, que hoy dice "No asigna categoría". Frontend: sin pantalla nueva; el resultado se ve en las de T010, y `frontend/src/privado/ingresos/DialogoAprobarIngreso.tsx` no cambia. Pruebas en `UbicacionAutomaticaPruebas.cs`: aprobar como JUGADOR con la categoría activa de su año lo deja en ella (2.1) y lo mismo si aprueba un DIRECTIVO (2.7); sin esa categoría, o con ella inactiva, se aprueba y queda en "Sin categoría" (2.2); aprobar como ENTRENADOR o DIRECTIVO no deja categoría ni lo muestra en "Sin categoría" (2.4); crear y reactivar la categoría recogen a todos los sin categoría de ese año y a ninguno de otro año, y dicen cuántos (2.3, CE-004); no mueven a un jugador de ese año que ya está en otra categoría (2.6, RF-011); un jugador aprobado antes de esta funcionalidad (sembrado sin categoría) entra al crearse la suya; aprobar a un jugador y crear la categoría de su año a la vez (`Task.WhenAll`, repetido varias veces) lo deja siempre en la categoría (caso límite); ninguna de estas operaciones crea un `JugadorEquipo`. `backend/pruebas/Integracion/Ingresos/AprobacionPruebas.cs` sigue en verde sin tocarlo, incluida la de dos aprobaciones simultáneas

**Punto de control**: las historias 1 y 2 son el mínimo utilizable: el club queda con sus
categorías y sus jugadores ubicados (quickstart, pasos 1 y 2)

---

## Fase 5: Historia 3 - El presidente asigna entrenadores a las categorías (Prioridad: P2)

**Objetivo**: el PRESIDENTE asigna a una categoría a entrenadores, directivos o presidentes de su
club, incluido él mismo, y les retira la asignación. Nadie cambia de rol.

**Prueba independiente**: con una categoría creada y un integrante aprobado con el rol ENTRENADOR,
se le asigna la categoría y se comprueba que aparece como su entrenador; se le retira y se
comprueba que deja de aparecer.

- [X] T012 [US3] Candidatos, asignar y retirar (`GET …/categorias/{categoriaId}/entrenadores/candidatos`, `PUT` y `DELETE …/entrenadores/{usuarioRolId}`; depende de T010). Backend: `Interfaces/IRepositorioAsignaciones.cs` y `RepositorioAsignaciones.cs`; `Servicios/IServicioEntrenadoresDeCategoria.cs` e `Implementaciones/ServicioEntrenadoresDeCategoria.cs`; `CandidatoEntrenadorDto`; y `backend/src/LFClub.Api/Controladores/Club/ControladorEntrenadoresDeCategoria.cs`. Las tres, solo PRESIDENTE. **Candidatos**: integrantes del club que cumplen `ReglaEntrenadorAsignable` y no tienen una asignación activa en esa categoría, incluido quien pregunta, por apellidos. **Asignar**: integrante inexistente o de otro club → `404`; si no cumple la regla (JUGADOR o en espera) → `409 no_asignable_como_entrenador`; categoría inactiva → `409 categoria_inactiva` (RF-007); crea la fila o vuelve a activar la que ya existía para esa pareja (supuesto 2), marca la categoría `Usada`, y asignar a quien ya está asignado no tiene efecto (RF-020); **no toca `UsuarioRol.Rol`** (RF-017a); `200` con `CategoriaDetalleDto`. **Retirar**: pone `Activa = false`, borra sus `EntrenadorEquipo` y nunca borra la fila (RF-021); retirar a quien no está asignado no tiene efecto; `200` con `CategoriaDetalleDto`. Frontend: `frontend/src/privado/categorias/SeccionEntrenadores.tsx`, sección de `DetalleCategoria.tsx`: nombre, apellidos, rol y equipos que dirige ("de la categoría en general" si no dirige ninguno), "todavía no tiene entrenador" si está vacía, y "Retirar" con `DialogoConfirmacion` solo para el PRESIDENTE; `frontend/src/privado/categorias/DialogoAsignarEntrenador.tsx`: lista de candidatos con su rol y estado vacío; no se ofrece en una categoría inactiva. Pruebas en `backend/pruebas/Integracion/Categorias/EntrenadoresPruebas.cs`: los candidatos son los aprobados con rol ENTRENADOR, DIRECTIVO o PRESIDENTE, incluido quien pregunta, sin jugadores, sin gente en espera y sin quien ya está asignado (3.1); un entrenador en dos categorías y dos entrenadores en una (3.3, 3.4); retirar lo quita de una y conserva la otra (3.5); asignar dos veces deja una sola fila; reasignar tras retirar reutiliza la misma fila; un JUGADOR y una persona en espera, 409; un integrante de otro club, 404 (3.6); categoría inactiva, 409 (3.7); la categoría queda con `sePuedeBorrar` falso aunque después se le retire; DIRECTIVO, ENTRENADOR y JUGADOR reciben 403 en los tres endpoints (3.8). En `AislamientoCategoriasPruebas.cs`: el presidente de otro club recibe 404 en los tres con identificadores reales y nada cambia. En `AccesoClubPruebas.cs`: quitar los tres endpoints de `PendientesDeConstruir` y subir el contador a 20
- [X] T013 [US3] Quien entrena conserva su rol, y el retiro de un presidente de la 001 (depende de T012). Sin código de producción nuevo salvo que una prueba falle. Pruebas en `backend/pruebas/Integracion/Categorias/EntrenadoresConRolPruebas.cs`: un PRESIDENTE asignado a una categoría sigue con rol `PRESIDENTE` en `GET /api/sesion`, sigue creando categorías y aprobando ingresos, y aparece como entrenador con `rol: PRESIDENTE` (3.9); un DIRECTIVO asignado sigue con rol `DIRECTIVO`, sigue viendo todas las categorías y recibe 403 en crear, asignar y desactivar (3.10, CE-012); el integrante tiene una sola fila de `UsuarioRol` en el club antes y después. En `backend/pruebas/Integracion/Plataforma/RetiroPresidentePruebas.cs`, dos pruebas nuevas: quitarle el rol a un presidente que entrena pasándolo a DIRECTIVO o ENTRENADOR conserva sus asignaciones; eliminarlo del club borra sus asignaciones en cascada sin error y la categoría sigue con `sePuedeBorrar` falso (supuesto 7). Las pruebas que ya había en ese archivo no se tocan

**Punto de control**: cada categoría muestra a sus entrenadores (quickstart, paso 3)

---

## Fase 6: Historia 4 - El presidente ubica o cambia de categoría a un jugador (Prioridad: P2)

**Objetivo**: el PRESIDENTE ubica a un jugador sin categoría y pasa a un jugador de una categoría
a otra, sea o no la de su año; la pantalla señala a quien está fuera de su año.

**Prueba independiente**: con dos categorías y un jugador en una de ellas, se le pasa a la otra y
se comprueba que aparece solo en la nueva y que la pantalla señala que no es la de su año.

- [X] T014 [US4] Ubicar y cambiar de categoría (`PUT /api/clubes/{clubId}/jugadores/{usuarioRolId}/categoria`; depende de T010). Backend: `Servicios/IServicioUbicacionJugador.cs`, `Implementaciones/ServicioUbicacionJugador.cs`, `UbicarJugadorDto` y la acción en `ControladorJugadores.cs`; solo PRESIDENTE. `categoriaId` ausente → `400 datos_invalidos`; integrante o categoría que no existen en este club → `404 no_encontrado`; integrante que no es "Rol JUGADOR, EstadoIngreso APROBADO y Activo" (otro rol, en espera o retirado) → `409 no_es_jugador` (RF-012); categoría inactiva → `409 categoria_inactiva` (RF-007). Con el club bloqueado: fija `CategoriaId`, marca la categoría de destino `Usada` y, si venía de **otra** categoría, borra todas sus filas de `JugadoresEquipo` (RF-027); ubicarlo en la categoría en la que ya está no tiene efecto y conserva sus equipos. No crea otro integrante ni cambia ningún otro dato suyo (RF-015). Responde `200` con el `CategoriaDetalleDto` de la categoría de destino. No existe "dejar sin categoría". Frontend: `frontend/src/privado/categorias/DialogoCambiarCategoria.tsx`: elige entre las categorías **activas** del club y avisa cuando la elegida no es la de su año; "Ubicar" en cada fila de `SeccionSinCategoria.tsx` (sin confirmación adicional: elegir la categoría ya es la acción) y "Cambiar de categoría" en cada fila de `SeccionJugadores.tsx` (con confirmación dentro del diálogo, RF-014); los dos solo para el PRESIDENTE; al terminar recargan la lista o el detalle. Pruebas en `backend/pruebas/Integracion/Categorias/CambioDeCategoriaPruebas.cs`: ubicar a un sin categoría lo saca de la lista y lo pone en la categoría (4.1); pasarlo a otra lo deja solo en la nueva, con el mismo `usuarioRolId` y los mismos datos, y baja en uno el total de la anterior (4.2); fuera de su año llega con `fueraDeSuAnio` verdadero y su `anioNacimiento` (4.3); categoría inactiva, 409; categoría de otro club, 404 (4.4); un ENTRENADOR, un DIRECTIVO, un PRESIDENTE y una persona en espera, 409 `no_es_jugador` (4.5); dos cambios simultáneos del mismo jugador a categorías distintas (`Task.WhenAll`) lo dejan en una sola (caso límite); tras todo el recorrido, la suma de `numeroJugadores` de todas las categorías más los sin categoría es igual al número de jugadores del club (CE-009); crear después la categoría de su año no lo mueve; DIRECTIVO, ENTRENADOR y JUGADOR, 403 (4.6). En `AislamientoCategoriasPruebas.cs`: el presidente de otro club recibe 404 con los identificadores reales del jugador y de la categoría y el jugador no se mueve. En `AccesoClubPruebas.cs`: quitar el endpoint de `PendientesDeConstruir` y subir el contador a 21

**Punto de control**: las excepciones a la regla automática se resuelven a mano (quickstart,
paso 4)

---

## Fase 7: Historia 5 - El presidente divide una categoría en equipos (Prioridad: P3)

**Objetivo**: el PRESIDENTE crea equipos dentro de una categoría, pone a cada jugador en uno, en
varios o en ninguno, e indica qué equipos dirige cada entrenador.

**Prueba independiente**: en una categoría con jugadores y un entrenador, se crean los equipos "A"
y "B", se pone a un jugador en los dos y a otro solo en el "B", se indica que el entrenador dirige
el "A" y se comprueba que la categoría muestra cada equipo con sus jugadores y su entrenador.

- [X] T015 [US5] Crear, renombrar, desactivar y borrar equipos (`POST …/categorias/{categoriaId}/equipos`, `PUT` y `DELETE …/equipos/{equipoId}`, `POST …/equipos/{equipoId}/desactivacion`; depende de T010). Backend: `backend/src/LFClub.Aplicacion/Validadores/ValidadorNombreEquipo.cs` ("Obligatorio, sin espacios sobrantes, 30 caracteres como máximo"; devuelve el nombre recortado y su forma normalizada en minúsculas); `Interfaces/IRepositorioEquipos.cs` y `RepositorioEquipos.cs`; `Servicios/IServicioEquipos.cs` e `Implementaciones/ServicioEquipos.cs`; `NombreEquipoDto`; y `backend/src/LFClub.Api/Controladores/Club/ControladorEquipos.cs`. Las cuatro, solo PRESIDENTE, y todas responden con el `CategoriaDetalleDto` de la categoría (`201` al crear, `200` en las demás). Un equipo inactivo, inexistente o que no es de la categoría de la ruta → `404`. **Crear**: nombre inválido → `400 datos_invalidos` con el error en `nombre`; categoría inactiva → `409 categoria_inactiva` (RF-022); mismo nombre normalizado en un equipo **activo** de esa categoría → `409 equipo_ya_existe`, también cuando lo detecta el índice único parcial en dos altas simultáneas; nace activo, sin jugadores, sin entrenadores y con `Usado` falso. **Renombrar**: mismas validaciones de nombre y `409 equipo_ya_existe`; conserva jugadores y entrenadores; no admite cambiar de categoría (RF-024). **Desactivar**: `Activo = false` y borra sus `JugadorEquipo` y sus `EntrenadorEquipo`; los jugadores siguen en la categoría y los entrenadores siguen asignados (RF-030); no existe la reactivación y su nombre queda libre (supuesto 3). **Borrar**: solo con `Usado` en falso; si no, `409 equipo_con_historial` (RF-024a). `EquipoDto.sePuedeBorrar` = `Usado` en falso. Frontend: `frontend/src/privado/categorias/SeccionEquipos.tsx`, sección de `DetalleCategoria.tsx`: cada equipo con su número de jugadores y sus entrenadores; para el PRESIDENTE, campo para crear con el error por campo, renombrar en el sitio, "Desactivar" con `DialogoConfirmacion` y "Borrar" con `DialogoConfirmacion` solo cuando `sePuedeBorrar`; no se ofrece crear en una categoría inactiva. Pruebas en `backend/pruebas/Unitarias/Validadores/ValidadorNombreEquipoPruebas.cs` (vacío, solo espacios, 30 y 31 caracteres, espacios sobrantes, normalización de "Élite" y "A") y en `backend/pruebas/Integracion/Categorias/EquiposPruebas.cs`: crear deja el equipo vacío (5.1); "A" y después "a" o " A " → 409, y el mismo nombre en otra categoría se admite (5.2); renombrar conserva a sus jugadores y entrenadores sembrados (5.9); desactivar lo saca de todas las respuestas, deja a sus jugadores en la categoría sin ese equipo y a sus entrenadores asignados, y permite crear otro con el mismo nombre (5.10); borrar uno sin usar lo quita, y uno con `Usado` verdadero da 409 (5.12); crear en una categoría inactiva, 409; un equipo de otra categoría en la ruta, 404; DIRECTIVO, ENTRENADOR y JUGADOR, 403 en las cuatro (5.11). En `AislamientoCategoriasPruebas.cs`: el presidente de otro club recibe 404 en las cuatro con identificadores reales. En `AccesoClubPruebas.cs`: quitar los cuatro endpoints de `PendientesDeConstruir` y subir el contador a 25
- [X] T016 [US5] Jugadores de cada equipo (`PUT` y `DELETE …/categorias/{categoriaId}/equipos/{equipoId}/jugadores/{usuarioRolId}`; depende de T015). Backend: `Servicios/IServicioJugadoresDeEquipo.cs`, `Implementaciones/ServicioJugadoresDeEquipo.cs` y las dos acciones en `ControladorEquipos.cs`; solo PRESIDENTE y con el club bloqueado. **Poner**: integrante inexistente en el club → `404`; "El equipo debe ser activo y de la categoría actual del jugador": si el jugador no está en la categoría de ese equipo (está en otra, sin categoría, retirado o no es jugador) → `409 jugador_de_otra_categoria`; crea la fila, marca el equipo `Usado` y no toca sus demás equipos; ponerlo dos veces no tiene efecto. **Sacar**: borra la fila; sacarlo de un equipo en el que no está no tiene efecto; sigue en su categoría y en sus demás equipos. Las dos responden `200` con `CategoriaDetalleDto`. Frontend: en `SeccionJugadores.tsx`, cuando la categoría tiene equipos y quien mira es el PRESIDENTE, cada fila muestra **una casilla por equipo** que se marca o desmarca con un toque y guarda al instante (repartir 20 jugadores entre dos equipos son 20 toques); a 360 px las casillas se apilan bajo el nombre; los demás roles ven los nombres de los equipos; ante un 409 muestra el mensaje y recarga. Pruebas en `EquiposPruebas.cs`: un jugador en "A" y en "B" aparece en los dos y cuenta una vez en `numeroJugadores` de la categoría (5.3 y caso límite); sacarlo de uno lo deja en el otro (5.4); un jugador de otra categoría, 409 (5.5); pasarlo a otra categoría con T014 lo saca de todos sus equipos y queda sin equipo en la nueva, y devolverlo no se los devuelve (5.6); un jugador de la categoría sin equipo sigue en `jugadores` con `equipos` vacío; poner y sacar dos veces es idempotente; el equipo queda con `sePuedeBorrar` falso aunque después se quede vacío; poner a un jugador en un equipo y cambiarlo de categoría a la vez (`Task.WhenAll`) nunca lo deja en un equipo de una categoría que no es la suya; DIRECTIVO, ENTRENADOR (también el que dirige ese equipo) y JUGADOR, 403 (RF-025). En `AislamientoCategoriasPruebas.cs`: otro club, 404 en las dos. En `AccesoClubPruebas.cs`: quitar los dos endpoints de `PendientesDeConstruir` y subir el contador a 27
- [X] T017 [US5] Equipos que dirige cada entrenador (`PUT …/categorias/{categoriaId}/entrenadores/{usuarioRolId}/equipos`; depende de T012 y T015). Backend: `EquiposDeEntrenadorDto`, el método en `ServicioEntrenadoresDeCategoria.cs` y `RepositorioAsignaciones.cs`, y la acción en `ControladorEntrenadoresDeCategoria.cs`; solo PRESIDENTE. `equipoIds` ausente → `400 datos_invalidos`; categoría, integrante o alguno de los equipos inexistente, inactivo o de otra categoría → `404 no_encontrado`; integrante sin asignación **activa** en esa categoría → `409 entrenador_no_asignado` (RF-028). **Reemplaza la lista completa** en una transacción: borra los `EntrenadorEquipo` de la asignación que no vengan y crea los que falten, marcando `Usado` cada equipo que gana un entrenador; una lista vacía lo deja como entrenador de la categoría en general. No cambia lo que ve el entrenador (RF-029). Responde `200` con `CategoriaDetalleDto`. Frontend: en `SeccionEntrenadores.tsx`, cuando la categoría tiene equipos y quien mira es el PRESIDENTE, una casilla por equipo en cada entrenador, que guarda la lista completa al cambiar. Pruebas en `EquiposPruebas.cs`: indicar que dirige "A" lo muestra en ese equipo y en `entrenadores[].equipos` (5.7); sin ninguno, `equipos` vacío (5.8); enviar otra lista reemplaza la anterior; un equipo de otra categoría o desactivado en la lista, 404 y no cambia nada; un integrante no asignado, 409; desactivar el único equipo que dirigía lo deja como entrenador de la categoría en general; retirarle la categoría con T012 borra los equipos que dirigía, y al reasignarlo vuelve sin ninguno (caso límite); desactivar la categoría con T009 borra lo que dirigían sus entrenadores; DIRECTIVO, ENTRENADOR y JUGADOR, 403. En `AislamientoCategoriasPruebas.cs`: otro club, 404. En `AccesoClubPruebas.cs`: quitar el endpoint de `PendientesDeConstruir` y subir el contador a 28

**Punto de control**: una categoría se divide en equipos con sus jugadores y entrenadores
(quickstart, paso 5)

---

## Fase 8: Historia 6 - El presidente retira a un jugador que se fue del club (Prioridad: P3)

**Objetivo**: el PRESIDENTE retira a un jugador, que sale de su categoría y de sus equipos y deja
de entrar a ese club conservando sus datos, y lo reincorpora si vuelve.

**Prueba independiente**: con un jugador en una categoría y en un equipo, se le retira y se
comprueba que desaparece de ambos, que aparece en la lista de retirados y que su cuenta ya no
entra a ese club; se le reincorpora y se comprueba que vuelve a entrar y queda en la categoría de
su año.

- [X] T018 [US6] El retirado no entra al club y ve solo el aviso (RF-043; depende de T006). Backend: `backend/src/LFClub.Dominio/Reglas/ResultadoAcceso.cs` gana `IntegranteRetirado`; `backend/src/LFClub.Dominio/Reglas/ReglaAccesoPorEstado.cs` recibe también `Activo` y evalúa en este orden: **estado del club, después el ingreso y después el retiro** (un retirado de un club suspendido recibe `ClubSuspendido`, supuesto 6); `backend/src/LFClub.Api/Autorizacion/IntegranteDelClubAttribute.cs` lo traduce a `403 integrante_retirado` ("Ya no estás en este club.") en **todos** los endpoints `/api/clubes/{clubId}/**`, antes de mirar los roles exigidos y sin fijar el club en `IContextoClub`. `backend/src/LFClub.Aplicacion/DTOs/ClubDeSesionDto.cs` gana `Retirado` y `backend/src/LFClub.Aplicacion/Mappers/MapperSesion.cs` lo rellena con `Activo` en falso. Frontend: `retirado` en `ClubDeSesionDto` de `frontend/src/compartido/api/tipos.ts`; `frontend/src/privado/AvisoRetirado.tsx` ("Ya no estás en este club", con el nombre y la identidad del club tomados de la sesión, botón de tema, desplegable de clubes y cerrar sesión; sin menú); `frontend/src/privado/DisposicionClub.tsx` mira `retirado` en la sesión **antes** de pedir nada al club, igual que hace con la sala de espera, y si la API responde `403 integrante_retirado` con una sesión desactualizada recarga la sesión; con el club suspendido o dado de baja sigue mostrando `AvisoClubNoDisponible.tsx`; `frontend/src/privado/DesplegableClubes.tsx` marca con texto ("retirado") esos clubes; `frontend/src/compartido/sesion/ultimoClub.ts` deja de preferir un club en el que la persona está retirada. Pruebas: ampliar `backend/pruebas/Unitarias/Reglas/ReglaAccesoPorEstadoPruebas.cs` con `Activo` falso en los tres estados del club y con el orden de las tres comprobaciones; crear `backend/pruebas/Integracion/Categorias/RetiroJugadorPruebas.cs`: un jugador retirado (sembrado) recibe `403 integrante_retirado` en **cada** endpoint de `EndpointsDeLaApi.DeLaApi` que empieza por `/api/clubes/{clubId}` y la respuesta no contiene el nombre del club (CE-014, escenario 6.5); en un club suspendido recibe `club_suspendido`; `GET /api/sesion` trae ese club con `retirado` verdadero y sus otros clubes con falso, y en ellos sigue obteniendo `GET /api/clubes/{otro}` con 200 (6.4); sus filas de `Usuario` y `UsuarioRol` conservan todos sus datos. En `frontend/pruebas/ultimoClub.test.ts`: prefiere un club en el que no está retirada; respeta el último elegido aunque esté retirada en él; si lo está en todos, devuelve el primero. Las pruebas de acceso de la 001 y la 002 siguen en verde sin tocarlas
- [X] T019 [US6] Retirar, reincorporar y lista de retirados (`POST /api/clubes/{clubId}/jugadores/{usuarioRolId}/retiro`, `POST …/reincorporacion` y `GET /api/clubes/{clubId}/jugadores/retirados`; depende de T010 y T018). Backend: `Servicios/IServicioRetiroJugador.cs`, `Implementaciones/ServicioRetiroJugador.cs`, `JugadorRetiradoDto`, `ReincorporacionDto`, los métodos de `IRepositorioJugadores.cs` / `RepositorioJugadores.cs` y las tres acciones en `ControladorJugadores.cs`. **Retirar** (solo PRESIDENTE, club bloqueado): una sentencia condicionada a "Rol JUGADOR, EstadoIngreso APROBADO y Activo" que pone `Activo = false`, `CategoriaId = null`, `RetiradoEn`, `RetiradoPorUsuarioId` y `RetiradoPorNombre` (nombres y apellidos de quien retira en ese club, copiados en ese momento, §13), y borra sus `JugadorEquipo`; "Los tres campos del retiro se rellenan juntos"; si no cambia ninguna fila: `204` sin efecto si ya estaba retirado (RF-047), `409 no_es_jugador` si es otro rol o está en espera (RF-041), `404` si no existe en el club; responde `204`; no envía ningún correo. **Reincorporar** (solo PRESIDENTE, club bloqueado): si no está retirado → `409 jugador_no_retirado`; pone `Activo = true`, vacía juntos los tres campos del retiro (supuesto 5) y llama a `UbicadorDeJugadores.UbicarAUnoAsync`; no recupera equipos (RF-045); `200` con `ReincorporacionDto` (`categoriaId` y `anio` nulos si quedó sin categoría). **Retirados** (`[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]`): "Rol JUGADOR y Activo falso", del retiro más reciente al más antiguo; `retiradoPor` sale de `RetiradoPorNombre`, nunca del nombre actual. Frontend: `frontend/src/privado/categorias/SeccionRetirados.tsx`, sección de `Categorias.tsx` para PRESIDENTE y DIRECTIVO: nombre, apellidos, año de nacimiento, quién lo retiró y cuándo, estado vacío y "Reincorporar" solo para el PRESIDENTE, que avisa de en qué categoría quedó; botón "Retirar" con `DialogoConfirmacion` (advierte de que dejará de entrar al club y de que sus datos se conservan) en cada fila de `SeccionJugadores.tsx` y de `SeccionSinCategoria.tsx`, solo para el PRESIDENTE. Pruebas en `RetiroJugadorPruebas.cs`: tras retirar a un jugador con categoría y equipo no está en la categoría, ni en el equipo, ni en "Sin categoría", y aparece en retirados con sus cinco datos (6.2); con el **mismo token de sesión** pasa de 200 a `403 integrante_retirado` en `GET /api/clubes/{clubId}` (RF-043); la lista conserva el nombre de quien lo retiró aunque esa cuenta se elimine después; retirar dos veces, y dos retiros simultáneos (`Task.WhenAll`), dan 204 y no cambian quién ni cuándo (6.x, RF-047); un ENTRENADOR, un DIRECTIVO, un PRESIDENTE y una persona en espera, 409 (6.9); reincorporar lo deja en la categoría activa de su año, o sin categoría si no existe o está inactiva, sin equipos y con los tres campos del retiro vacíos, y con su misma cuenta vuelve a entrar (6.6); reincorporar a quien no está retirado, 409; una categoría cuyos jugadores se retiraron todos se puede desactivar (6.8); el equipo y la categoría siguen existiendo vacíos tras retirar al último; el DIRECTIVO ve la lista y recibe 403 al retirar y reincorporar (6.10); ENTRENADOR y JUGADOR, 403 en los tres (6.11); `GET …/ingresos/aprobados` sigue mostrando al retirado (supuesto 8). En `AislamientoCategoriasPruebas.cs`: el presidente de otro club recibe 404 al retirar y reincorporar con el identificador real, y el jugador no cambia. En `AccesoClubPruebas.cs`: quitar los tres endpoints de `PendientesDeConstruir` y subir el contador a 31
- [X] T020 [US6] El correo y el documento de un retirado siguen ocupados, y la invitación de presidente a un jugador (`POST /api/clubes/{clubId}/invitaciones`, `POST /api/invitaciones/registro`, `POST /api/invitaciones/aceptacion`; RF-046, RF-012; depende de T018). Backend: `ErroresDeInvitacion.PersonaRetirada()` en `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeInvitacion.cs` (`409 persona_retirada`, con un mensaje que explica que esa persona está retirada del club y que el PRESIDENTE puede reincorporarla). `IRepositorioInvitacionesClub.cs` / `RepositorioInvitacionesClub.cs` y `ServicioInvitacionesClub.cs`: invitar al correo de un jugador retirado de este club → `409 persona_retirada` en lugar de `ya_esta_en_el_club`, y no se crea ni se envía nada. `IRepositorioPertenencias.cs` / `Plataforma/RepositorioPertenencias.cs` y `ServicioRegistroConInvitacion.cs`: registrarse en ese club con el documento de un jugador retirado → `409 persona_retirada` en lugar de `documento_repetido_en_club`. `ServicioAceptacionInvitacion.cs`: con una invitación **del club**, si la cuenta es la de un jugador retirado de ese club → `409 persona_retirada` en lugar de `ya_perteneces_al_club`, sin marcar la invitación como usada; con una invitación **de presidente**, quien era jugador del club pasa a PRESIDENTE y, en esa misma transacción, queda con `CategoriaId` nulo, sin filas en `JugadoresEquipo`, con `Activo` verdadero y con los tres campos del retiro vacíos. Los demás códigos de la 002 no cambian. Frontend: `frontend/src/privado/ingresos/SeccionInvitacionesClub.tsx`, `frontend/src/cuenta/FormularioRegistro.tsx` y `frontend/src/cuenta/AceptarInvitacion.tsx` muestran el mensaje del servidor para `persona_retirada`; comprobarlo y corregir solo si alguno lo sustituye por un texto genérico. Pruebas: en `backend/pruebas/Integracion/Ingresos/InvitacionesClubPruebas.cs`, invitar al correo de un retirado da 409 y `CorreoEnMemoria` no recibe nada; en `backend/pruebas/Integracion/Ingresos/RegistroEnEsperaPruebas.cs`, registrarse con su documento da `persona_retirada`, y con el documento de un integrante activo sigue dando `documento_repetido_en_club`; en `backend/pruebas/Integracion/Cuenta/AceptacionInvitacionPruebas.cs`, aceptar una invitación antigua del club con la cuenta de un retirado da 409 y la invitación sigue sin usar; un jugador con categoría y equipo que acepta una invitación de presidente queda PRESIDENTE, sin categoría ni equipos y no aparece en "Sin categoría"; un jugador **retirado** que la acepta queda PRESIDENTE y activo, vuelve a entrar al club y ya no aparece en retirados. La prueba de sustitución de rol de la 001 sigue en verde

**Punto de control**: el club retira y reincorpora jugadores (quickstart, paso 6)

---

## Fase 9: Historia 7 - Cada quien consulta las categorías que le corresponden (Prioridad: P3)

**Objetivo**: el DIRECTIVO ve todo sin cambiar nada, el ENTRENADOR ve solo sus categorías
asignadas y la familia ve en el inicio la categoría, los equipos y los entrenadores de su jugador.

**Prueba independiente**: con un entrenador asignado a una de dos categorías, se inicia sesión con
su cuenta y se comprueba que ve solo esa categoría y sus jugadores; se inicia sesión como
DIRECTIVO y se comprueba que ve las dos y ninguna opción para modificarlas.

- [X] T021 [US7] El ENTRENADOR ve solo sus categorías, y el DIRECTIVO todo sin acciones (`GET /api/clubes/{clubId}/categorias` y `GET …/categorias/{categoriaId}`; depende de T012). Backend: los dos endpoints pasan a `[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]` y `ServicioConsultaCategorias.cs` resuelve el alcance con `ReglaAlcanceDeCategorias` a partir del rol del integrante que pregunta, con una consulta nueva en `IRepositorioAsignaciones.cs` / `RepositorioAsignaciones.cs` (las categorías con una asignación activa suya). Para un `ENTRENADOR`: la lista trae solo las categorías **activas** que tiene asignadas, vacía si no tiene ninguna; el detalle de una que no tiene asignada o que está inactiva responde `404 no_encontrado`, igual que si no existiera (supuesto 4); en las suyas ve todos los equipos y todos los jugadores, dirija o no su equipo (RF-029). `GET …/jugadores/sin-categoria` y `GET …/jugadores/retirados` siguen negándole con 403. Un PRESIDENTE o un DIRECTIVO con asignaciones sigue viendo todas (RF-017a). El alcance se consulta en cada petición, así que retirarle una categoría surte efecto en su siguiente acción. Frontend: el enlace "Categorías" del menú de `DisposicionClub.tsx` se muestra también al ENTRENADOR; en `Categorias.tsx`, el ENTRENADOR no ve el formulario de crear ni las secciones "Sin categoría" y "Retirados" (y no se piden a la API), y con la lista vacía ve "Todavía no tienes categorías asignadas" (7.4); `DetalleCategoria.tsx` ante un 404 vuelve a la lista con un aviso; revisar que DIRECTIVO y ENTRENADOR no ven ningún botón, casilla ni diálogo de modificación en `Categorias.tsx`, `DetalleCategoria.tsx` y sus secciones. Pruebas en `backend/pruebas/Integracion/Categorias/ConsultaPorRolPruebas.cs`: el DIRECTIVO ve todas las categorías, activas e inactivas, con equipos, entrenadores y jugadores, y las listas "Sin categoría" y "Retirados" (7.1); el ENTRENADOR asignado a una de dos ve solo esa, con el nombre, los apellidos, el año de nacimiento y los equipos de todos sus jugadores, incluidos los de un equipo que no dirige (7.2); recibe 404 en el detalle de la otra con su identificador real y 403 en "Sin categoría" y "Retirados", sin ningún dato en la respuesta (7.3, CE-007); sin asignaciones, lista vacía; con su única categoría desactivada, lista vacía y 404; tras retirarle la asignación, con el **mismo token**, deja de verla (caso límite); un JUGADOR recibe `403 rol_no_autorizado` en lista, detalle, "Sin categoría" y "Retirados" (7.7); el integrante de otro club, 404 (7.9)
- [X] T022 [P] [US7] "Mi categoría" de la familia (`GET /api/clubes/{clubId}/mi-categoria`; depende de T010). Backend: `Servicios/IServicioMiCategoria.cs`, `Implementaciones/ServicioMiCategoria.cs`, `MiCategoriaDto`, `EntrenadorParaFamiliaDto`, su conversión en `MapperCategorias.cs` y `backend/src/LFClub.Api/Controladores/Club/ControladorMiCategoria.cs` con `[IntegranteDelClub(Rol.JUGADOR)]`. No recibe ningún identificador: responde siempre sobre el integrante de quien llama. Devuelve `categoria` nulo si no tiene; si tiene: `anio`, los **nombres** de los equipos activos en los que está y, de cada asignación activa de su categoría, `nombres`, `apellidos` y los nombres de los equipos que dirige (lista vacía si lo es de la categoría en general). Es un tipo distinto de `EntrenadorDeCategoriaDto` (§23): "Sin identificador, rol ni contacto". Frontend: `frontend/src/privado/TarjetaMiCategoria.tsx`, que `frontend/src/privado/InicioClub.tsx` muestra solo al rol JUGADOR: "Categoría 2014", sus equipos (o "sin equipo"), cada entrenador con el equipo que dirige, "todavía no tiene entrenador" si no hay ninguno y "Todavía no tienes categoría asignada" si `categoria` es nulo; el JUGADOR no tiene enlace a "Categorías". Pruebas en `backend/pruebas/Integracion/Categorias/MiCategoriaPruebas.cs`: un jugador con categoría, en el equipo "A", con un entrenador que dirige "A" y un PRESIDENTE asignado sin equipo ve el año, "A" y los dos entrenadores con sus equipos (7.5, CE-011); el JSON no contiene ningún `usuarioRolId`, correo, celular, documento ni rol de los entrenadores, ni el nombre de ningún otro jugador (7.8); sin categoría, `categoria` nulo (7.6); con categoría sin entrenadores, lista vacía; no aparecen los equipos desactivados ni los entrenadores retirados de la categoría; dos jugadores de categorías distintas ven cada uno solo la suya; PRESIDENTE, DIRECTIVO y ENTRENADOR reciben `403 rol_no_autorizado`; un jugador retirado, `403 integrante_retirado`; el de otro club, 404. En `AccesoClubPruebas.cs`: quitar el último endpoint, eliminar la lista `PendientesDeConstruir` (la comparación vuelve a ser exacta) y dejar el contador de endpoints de club en 32

**Punto de control**: las siete historias funcionan y se prueban por separado (quickstart, pasos 1
a 7)

---

## Fase 10: Cierre y aspectos transversales

**Propósito**: estados del club, recorrido de la autorización, contrato, revisión visual y
recorrido completo

- [X] T023 Club suspendido y dado de baja (RF-039). Sin código de producción nuevo: lo resuelve el atributo de la 001; corregir solo si una prueba falla. Pruebas en `backend/pruebas/Integracion/Categorias/CategoriasPorEstadoDelClubPruebas.cs`: en un club suspendido el PRESIDENTE crea, desactiva y reactiva categorías, asigna entrenadores, crea equipos, cambia a un jugador de categoría y lo retira; el DIRECTIVO, el ENTRENADOR y el JUGADOR reciben `403 club_suspendido` en los 22 endpoints nuevos; un jugador retirado recibe `club_suspendido` (supuesto 6). En un club dado de baja todos, también el PRESIDENTE, reciben `403 club_dado_de_baja` en los 22; al revertir la baja, las categorías, los equipos, las asignaciones y los jugadores siguen como estaban. Eliminar el club borra las cinco tablas en cascada y no toca las categorías de otro club (ampliar `backend/pruebas/Integracion/Plataforma/EliminarClubPruebas.cs` con un club que tiene categoría, equipo, asignación y jugador en equipo)
- [X] T024 [P] Solo el PRESIDENTE cambia algo, recorrido completo (§20, CE-006, RF-036). Crear `backend/pruebas/Integracion/Categorias/SoloPresidentePruebas.cs`: toma de `EndpointsDeLaApi.DeLaApi` todos los endpoints de categorías y jugadores cuyo método no es `GET` (deben ser 16) y, con identificadores reales de un escenario sembrado, comprueba que un DIRECTIVO, un ENTRENADOR asignado a esa categoría, un JUGADOR de esa categoría y una cuenta en espera reciben 403 en todos (`rol_no_autorizado` o `ingreso_en_espera`), que sin sesión es 401, y que después del recorrido el estado sembrado no ha cambiado (mismas categorías, equipos, asignaciones, categoría y equipos de cada jugador, y nadie retirado). Añadir la prueba de que el DESARROLLADOR recibe 404 en `GET /api/clubes/{clubId}/categorias` y en `GET …/mi-categoria` (RF-038) si `AccesoClubPruebas.cs` no la cubre ya de forma explícita
- [X] T025 [P] Contrastar la API con `specs/003-categorias-club/contracts/api.yaml`: arrancar la API, abrir Swagger y comprobar que cada endpoint nuevo o cambiado tiene las rutas, los métodos, los códigos de estado, los nombres de DTO y los campos del contrato (incluidos `sePuedeBorrar`, `fueraDeSuAnio`, `jugadoresUbicados`, `retirado`, y `categoriaId` y `anio` nulos en `ReincorporacionDto`), y que `frontend/src/compartido/api/tipos.ts` coincide. Corregir el código si difiere; si lo que está mal es el contrato, corregirlo y decirlo. Confirmar que `AccesoClubPruebas.cs` compara de forma exacta, con 55 endpoints en el contrato y 32 de club
- [X] T026 Revisar a 360 px de ancho, en tema claro y en oscuro, con la identidad de un club con colores propios (RF-040, CE-015): `/club/:clubId/categorias` con datos y vacía, con sus secciones "Sin categoría" y "Retirados"; el detalle de una categoría con equipos, con las casillas de 20 jugadores y dos equipos; los diálogos de asignar entrenador y de cambiar de categoría; las confirmaciones de desactivar, borrar y retirar; la vista del ENTRENADOR con y sin categorías; la tarjeta "Mi categoría" con y sin categoría; el aviso de retiro y el desplegable con un club retirado. Sin desplazamiento horizontal, con el texto legible y con "inactiva", "fuera de su año" y "retirado" dichos con texto además de color. Corregir en `frontend/src/privado/categorias/`, `frontend/src/privado/TarjetaMiCategoria.tsx`, `frontend/src/privado/AvisoRetirado.tsx` y `frontend/src/compartido/tema/componentes.css` lo que no cumpla
- [X] T027 Recorrer `specs/003-categorias-club/quickstart.md`, pasos 1 a 9, y ejecutar la batería entera (`dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `scripts/verificar-tamano.ps1`). Levantar el recorrido en un proyecto de Compose aparte, con la llave de Brevo vacía y su propio volumen, para no enviar correos reales ni tocar los datos de desarrollo, y borrarlo al terminar. Anotar en la propia guía cualquier paso cuyo resultado no coincida y corregir el código. Marcar la funcionalidad como terminada solo con todo en verde, incluidas las pruebas de la 001 y la 002

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (fase 1)**: sin dependencias.
- **Cimientos (fase 2)**: depende de la fase 1. Bloquea todas las historias.
- **Historias (fases 3 a 9)**: dependen de los cimientos.
- **Cierre (fase 10)**: depende de las siete historias.

### Entre historias

- **Historia 1 (P1)**: empieza al terminar los cimientos. No depende de ninguna otra.
- **Historia 2 (P1)**: necesita la lista y el alta de la historia 1 (T008), porque su pantalla
  cuelga de `Categorias.tsx` y sus pruebas crean categorías por la API.
- **Historia 3 (P2)**: necesita el detalle de la categoría (T010), que es donde vive su sección.
- **Historia 4 (P2)**: necesita T010. No depende de la historia 3.
- **Historia 5 (P3)**: T015 y T016 necesitan T010; T016 prueba además la salida de los equipos al
  cambiar de categoría, que construye T014; T017 necesita la historia 3 (T012).
- **Historia 6 (P3)**: T018 solo necesita los cimientos; T019 necesita T010 y T018; T020 necesita
  T018. No depende de las historias 3, 4 ni 5: sus pruebas siembran la categoría y el equipo.
- **Historia 7 (P3)**: T021 necesita la historia 3 (T012); T022 necesita T010 y siembra lo demás.

Orden recomendado: 1 → 2 → 3 → 4 → 5 → 6 → 7, el del plan.

### Dentro de cada fase

- Fase 2: T003 y T004 en paralelo, y T007 con ellos. T005 y T006 necesitan T003 y pueden ir en
  paralelo entre sí.
- Historia 1: T008 → T009 (comparten `ControladorCategorias.cs` y `GestionCategoriasPruebas.cs`).
- Historia 2: T010 → T011.
- Historia 3: T012 → T013.
- Historia 5: T015 → T016 → T017 (comparten `EquiposPruebas.cs`).
- Historia 6: T018 → T019; T020 después de T018, en paralelo con T019 si no se pisan en
  `AccesoClubPruebas.cs` (T020 no lo toca).
- Historia 7: T021 y T022 en paralelo.
- Cierre: T023, T024 y T025 en paralelo; después T026 y T027.

`AccesoClubPruebas.cs` lo tocan T007, T008, T009, T010, T012, T014, T015, T016, T017, T019 y T022:
nunca dos a la vez. `AislamientoCategoriasPruebas.cs` crece con casi todas las historias: si se
acerca a 250 líneas, se divide por historia.

---

## Ejemplos de trabajo en paralelo

```text
# Preparación y cimientos, primera tanda:
T002  Tipos del contrato en tipos.ts
T003  Entidades, configuraciones y migración CategoriasDelClub
T004  Reglas de dominio y sus pruebas unitarias
T007  Prueba de contrato a 55 endpoints

# Cimientos, segunda tanda:
T005  Bloqueo del club y UbicadorDeJugadores   ||  T006  SembradorCategorias y EscenarioCategorias

# Con la historia 2 terminada, por personas distintas:
T012 → T013  Entrenadores    ||  T014  Cambio de categoría    ||  T018  El retirado no entra

# Historia 7:
T021  Alcance del ENTRENADOR  ||  T022  Tarjeta "Mi categoría"

# Cierre:
T023  Estados del club  ||  T024  Solo el PRESIDENTE  ||  T025  Contrato frente a Swagger
```

---

## Estrategia de implementación

### Primero lo mínimo

Las historias 1 y 2 son P1 y juntas son el mínimo utilizable: el club queda con sus categorías y
sus jugadores ubicados.

1. Fases 1 y 2: base en verde y cimientos.
2. Historia 1 → **parar y validar** el paso 1 del quickstart.
3. Historia 2 → validar el paso 2. Aquí la funcionalidad ya sirve a un club real.

### Entrega incremental

4. Historia 3 → paso 3. Historia 4 → paso 4.
5. Historia 5 → paso 5. Historia 6 → paso 6. Historia 7 → paso 7.
6. Cierre → pasos 8 a 10 y recorrido completo.

Cada historia termina con un punto de control en el que todas las pruebas están en verde y la API
coincide con el contrato menos lo que queda en `PendientesDeConstruir`. Conviene un commit por
tarea y revisar en cada punto de control.

---

## Notas

- El plan dice que la prueba de contrato "estará en rojo" hasta el último endpoint. Estas tareas
  usan en su lugar la lista `PendientesDeConstruir` de la 002 (T007), para que cada tarea pueda
  cerrarse con toda la batería en verde (§27.2). El resultado final es el mismo: comparación
  exacta con 55 endpoints.
- El alta y la reactivación de una categoría recogen a los jugadores desde la historia 1 (T008,
  T009), porque el contrato exige `jugadoresUbicados` en su respuesta. La historia 2 añade la
  ubicación al aprobar y las pruebas completas de la regla.
- Hasta T021, un ENTRENADOR recibe 403 en la lista y en el detalle de categorías. Así ningún punto
  de control intermedio le deja ver una categoría que no es suya.
- Los ocho supuestos de research.md ya están aplicados: 1 en T004 y T008; 2 en T012; 3 en T015;
  4 en T021; 5 en T019; 6 en T018 y T023; 7 en T013; 8 en T019. Si el propietario cambia alguno,
  el cambio es de una consulta, un índice o un mensaje.
- Un detalle que el contrato fija por omisión y conviene que el propietario conozca: reactivar una
  categoría que ya está activa responde 200 sin efecto (T009), igual que desactivar una inactiva.
- La cabecera de spec.md cita la constitución 3.7.0; el plan está evaluado contra la 3.8.0, que
  sigue sin confirmar en git.
- Fuera de estas tareas: la entidad `Jugador` y su ficha, la mensualidad al aprobar, el historial
  de categorías y equipos, y la baja de entrenadores o directivos (spec, "Fuera de alcance").
- Recorrido del quickstart: los pasos 1 a 9 se validaron contra la aplicación levantada (T027). El
  paso 10 (tiempos con cronómetro) y su tarea T028 los retiró el propietario el 2026-10-08, junto
  con CE-001, CE-002, CE-005, CE-010 y CE-013.
- Los ocho supuestos de research.md los confirmó el propietario el 2026-10-08 y están en la spec.
- Si una tarea descubre una regla de negocio sin decidir, se detiene esa parte y se pregunta
  (§25); no se inventa.
