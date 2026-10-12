---

description: "Lista de tareas de la funcionalidad 009: entrenamientos, partidos, convocatorias y estadísticas deportivas"
---

# Tareas: Entrenamientos, partidos, convocatorias y estadísticas deportivas

**Entrada**: documentos de diseño en `specs/009-entrenamientos-y-partidos/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.6.0) y el plan (research §15).
Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa contra la API real
y sus pruebas; las tareas que solo traen pruebas o solo pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario, en el orden de construcción del
plan: historias 1, 2 y 3 (P1), 4 y 5 (P2), 6 (P3), y después el retiro (RF-033) y el cierre.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US6]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 008, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): siete tablas y la migración `EntrenamientosYPartidos` (T004). La
  única otra es `RivalOpcional` (T035), por un ajuste del propietario después de implementar. Si una tarea posterior parece necesitar otra tabla, columna o índice, se detiene y se
  informa (§25).
- **Contrato**: `specs/009-entrenamientos-y-partidos/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs:
  T001 deja comentadas las 22 operaciones nuevas y cada tarea que construye una la descomenta y
  sube los contadores de `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` (contrato
  y endpoints de club). Recorrido: 72/48 → 74/50 (T015) → 77/53 (T016) → 79/55 (T017) → 82/58
  (T019) → 84/60 (T020) → 87/63 (T021) → 88/64 (T023) → 90/66 (T024) → 92/68 (T025) → 93/69
  (T026) → 94/70 (T027).
- **Nada cambia para lo anterior** (§27.2): ninguna respuesta existente cambia. Las pruebas de la
  001 a la 008 no se tocan, salvo los contadores de `AccesoClubPruebas`. Si una falla y no está
  claro si es una regresión, se detiene y se pregunta.
- **Visibilidad** (research §4, data-model "Consultas de visibilidad"): la deciden
  `ReglaVisibilidadDeEntrenamiento` y `ReglaVisibilidadDePartido`, puras. Las listas, el
  calendario y los contadores aplican la misma condición **en SQL** (nunca traen ni cuentan lo que
  no se ve); la consulta por identificador lee la fila y aplica la regla en memoria. No verla,
  no existir y ser de otro club responden el mismo `404 no_encontrado` (RF-011). "Convocado" y
  "su categoría" se evalúan con el **integrante de la petición** (cabecera `X-Jugador-Elegido` de
  la 006), nunca con la cuenta.
- **Quien entrena** (research §3): cualquier integrante con una `AsignacionEntrenadorCategoria`
  activa, sea ENTRENADOR, DIRECTIVO o PRESIDENTE; se lee en cada petición con
  `IRepositorioAsignaciones.CategoriasDeAsync`. Gestionar exige ser PRESIDENTE o entrenar **todas**
  las categorías de la actividad (`ReglaGestionDeActividad`); quien la ve pero no la gestiona
  recibe `403 no_gestiona_actividad`.
- **Partes privadas omitidas** (research §5, RF-012, RF-024): una cuenta de jugador nunca recibe
  convocados, quiénes jugaron, ni respuestas o asistencias de otros. Esos campos se omiten del JSON
  (`JsonIgnore` cuando es nulo, como `FichaJugadorDto`), no van vacíos. Las pruebas lo comprueban
  sobre el **texto** de la respuesta.
- **Hora de Colombia** (research §2): `Fecha` (`date`) y horas (`time`) como las escribe quien
  programa; `HoraDeColombia` convierte `IReloj.AhoraUtc` con el desfase fijo UTC−5 y las reglas
  comparan siempre en esa hora. Fechas `YYYY-MM-DD` y horas `HH:mm` en la API.
- **Lo copiado no cambia** (§13): `PartidoJugador.CategoriaId` y `AsistenciaEntrenamiento.CategoriaId`
  se copian al crear la fila y no cambian después; `ResultadoCambiadoPorNombre` se copia al
  registrar o corregir.
- **Nombres** (§2.1, §2.2, RF-037): todo en español; "convocatoria", "entrenamiento", "partido";
  ni Profesor ni Acudiente. Los nombres de personas usan `NombrePersona` con el `EnlaceFichaDto`
  que calcula `ResolutorDeEnlaces` (008); la pantalla nunca infiere el enlace por el rol.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) con qué representa, su responsabilidad y qué no hace.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas.
  `frontend/src/privado/DisposicionClub.tsx` (216) no crece: el menú pasa a `MenuDelClub.tsx`
  (T013). `frontend/src/compartido/api/tipos.ts` no crece: los tipos van en
  `tiposActividades.ts` y `tiposEstadisticas.ts`. `ContextoLFClub.cs` (99) queda en unas 125.
  Los servicios se dividen por responsabilidad como dice el plan.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores solo
  delegan con `[IntegranteDelClub]`; los repositorios traducen la visibilidad a SQL sin decidirla;
  las reglas son dominio puro.
- **Aislamiento** (§7.1): todo bajo `/api/clubes/{clubId}` con el filtro de club activo. La única
  escritura que cruza clubes es la del retiro por hermano (T028), que ya existe.
- **Pantallas** (RF-036, CE-008, §24): estructura y textos de `Convocatoria` y `M-Calendario` del
  lienzo, sin costo de arbitraje, mensaje para los convocados ni "Guardar borrador" (plan, nota
  1); componentes `Tarjeta`, `Aviso`, `Boton`, `Campo`; legibles a 360 px y en los dos temas, sin
  desplazamiento horizontal de la página; todo estado (Programado, Cancelado, Próximo, Jugado,
  Falta el resultado, Asistirá, No asistirá, Presente, Ausente) escrito con texto.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

Las tareas los dan por buenos; si el propietario cambia alguno, cambia solo la regla indicada, no
el modelo.

| # | Supuesto (research §16) | Dónde | Estado |
| --- | --- | --- | --- |
| 1 | Las categorías de una actividad no cambian después de crearla: `409 categorias_no_cambian` | T016, T021 | Confirmado por el propietario |
| 2 | Los datos de un partido jugado no cambian; solo su resultado | T021 (`ReglaTransicionPartido`, T008) | Confirmado por el propietario |
| 3 | Cancelar se puede deshacer mientras no haya empezado (RF-038) | T016, T021 | Confirmado por el propietario |
| 4 | Se puede programar una actividad con fecha pasada | T005, T015, T019 | Confirmado por el propietario |
| 5 | La asistencia se registra desde el día del entrenamiento, no desde su hora | T008 (`ReglaMomentoDeActividad`), T024 | Lo fija la spec (escenario 4.1, RF-022) |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: que la batería siga en verde mientras el contrato de la 009 ya existe en `specs/`.

- [X] T001 Comentar en `specs/009-entrenamientos-y-partidos/contracts/api.yaml` las 22 operaciones nuevas, cada una con una marca `# 009-T0NN` que diga qué tarea la descomenta: `POST /entrenamientos` y `GET /entrenamientos/{entrenamientoId}` (T015); `PUT /entrenamientos/{entrenamientoId}`, `POST` y `DELETE …/cancelacion` (T016); `GET /calendario` y `GET /entrenamientos/proximos` (T017); `GET /partidos`, `POST /partidos` y `GET /partidos/{partidoId}` (T019); `GET /partidos/{partidoId}/jugadores` y `PUT …/convocatoria` (T020); `PUT /partidos/{partidoId}`, `POST` y `DELETE …/cancelacion` (T021); `PUT …/resultado` (T023); `GET /entrenamientos/{entrenamientoId}/jugadores` y `PUT …/asistencia` (T024); los dos `PUT …/mi-respuesta` (T025); `GET /jugadores/{usuarioRolId}/estadisticas` (T026); `GET /goleadores` (T027). Comprobar que `AccesoClubPruebas` (`backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`) sigue en 72 y 48 y que la batería entera pasa

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: esquema y migración, hora de Colombia, reglas puras con sus pruebas, reloj y sembrador de pruebas, tipos y utilidades del frontend, y el menú fuera de `DisposicionClub.tsx`.

**⚠️ CRÍTICO**: ninguna historia empieza hasta terminar esta fase.

- [X] T002 [P] Crear las cinco enumeraciones en `backend/src/LFClub.Dominio/Enumeraciones/`, serializadas y guardadas como texto como `Rol`, cada una con documentación XML: `EstadoEntrenamiento.cs` (`PROGRAMADO`, `CANCELADO`), `EstadoPartido.cs` (`PROXIMO`, `CANCELADO`, `JUGADO`), `CondicionPartido.cs` (`LOCAL`, `VISITANTE`), `TipoPartido.cs` (`AMISTOSO`, `TORNEO`) y `RespuestaFamilia.cs` (`ASISTIRA`, `NO_ASISTIRA`)
- [X] T003 Crear las siete entidades en `backend/src/LFClub.Dominio/Entidades/`, todas `IPerteneceAClub`, con clave `Guid` versión 7 donde tienen `Id` y documentación XML (depende de T002): `Entrenamiento.cs` (`Id`, `ClubId`, `Fecha` `DateOnly`, `HoraInicio` y `HoraFin` `TimeOnly` —"`HoraFin` mayor que `HoraInicio`"—, `Lugar` "texto ≤ 120, obligatorio, sin espacios a los lados", `Nota` "texto ≤ 500, opcional", `Estado` "nace `PROGRAMADO`", `CreadoEn`, `CreadoPorUsuarioRolId` nulable, `Categorias`); `EntrenamientoCategoria.cs` (`EntrenamientoId`, `CategoriaId`, `ClubId`); `AsistenciaEntrenamiento.cs` (`EntrenamientoId`, `UsuarioRolId`, `ClubId`, `CategoriaId` "categoría del jugador al registrarla por primera vez; no cambia", `Presente`, `RegistradaEn`); `RespuestaEntrenamiento.cs` (`EntrenamientoId`, `UsuarioRolId`, `ClubId`, `Respuesta`, `RespondidaEn`); `Partido.cs` (`Id`, `ClubId`, `Rival` "texto ≤ 120, obligatorio", `Fecha`, `Hora`, `Lugar` "texto ≤ 120, obligatorio", `Condicion`, `Tipo`, `NombreTorneo` "texto ≤ 120; obligatorio si `Tipo = TORNEO`; nulo si es amistoso", `Estado` "nace `PROXIMO`", `GolesClub` y `GolesRival` "nulos hasta el resultado; 0 a 99", `ResultadoCambiadoEn`, `ResultadoCambiadoPorUsuarioId`, `ResultadoCambiadoPorNombre`, `CreadoEn`, `CreadoPorUsuarioRolId`, `Categorias`, `Jugadores`); `PartidoCategoria.cs` (`PartidoId`, `CategoriaId`, `ClubId`); `PartidoJugador.cs` (`PartidoId`, `UsuarioRolId`, `ClubId`, `CategoriaId` "categoría del jugador al convocarlo; no cambia (§13, RF-018)", `Jugo` "falso al convocar", `Goles` "0 al convocar; solo mayor que 0 si `Jugo`", `Respuesta` nulable, `RespondidaEn`, `ConvocadoEn`). Reescribir solo la documentación XML de `Categoria.cs` (`Usada` también al dirigirle una actividad; una categoría con actividades no se borra), `UsuarioRol.cs` (de él cuelgan sus convocatorias, asistencias y respuestas; el retiro borra las convocatorias pendientes) y `AsignacionEntrenadorCategoria.cs` (también da la gestión deportiva al PRESIDENTE o DIRECTIVO asignado, constitución 4.6.0)
- [X] T004 Configuraciones, `DbSet` y migración (depende de T003): una configuración por entidad en `backend/src/LFClub.Infraestructura/Datos/Configuraciones/` (enumeraciones como texto; `Lugar`, `Rival` y `NombreTorneo` con `HasMaxLength(120)`, `Nota` con `HasMaxLength(500)`; claves compuestas de las tablas de unión, asistencias, respuestas y convocados; foráneas a `Entrenamientos`/`Partidos` en cascada, a `Categorias` y al jugador en `PartidosJugador` y `AsistenciasEntrenamiento` con `Restrict`, al jugador en `RespuestasEntrenamiento` en cascada, `CreadoPorUsuarioRolId` y `ResultadoCambiadoPorUsuarioId` a nulo al borrarse; índices `(ClubId, Fecha, HoraInicio)` en `Entrenamientos`, `(ClubId, Fecha, Hora)` en `Partidos`, por `CategoriaId` en las dos tablas `…Categoria` y por `UsuarioRolId` en `AsistenciasEntrenamiento` y `PartidosJugador`; restricciones de comprobación: `HoraFin > HoraInicio`; `GolesClub` y `GolesRival` ambos nulos o ambos con valor, con valor si y solo si `Estado = 'JUGADO'`, y entre 0 y 99; `Goles >= 0` y `Goles = 0` si no `Jugo`; `NombreTorneo` no nulo si `Tipo = 'TORNEO'`); los siete `DbSet` en `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs` con el filtro de club de la 001; migración `EntrenamientosYPartidos` en `backend/src/LFClub.Infraestructura/Datos/Migraciones/` generada con `dotnet ef`, revisada a mano y aplicada al arrancar como las demás. Comprobar que la batería sigue en verde
- [X] T005 [P] Crear `HoraDeColombia` en `backend/src/LFClub.Aplicacion/Utilidades/HoraDeColombia.cs`: `Ahora(IReloj)` devuelve `(DateOnly Hoy, TimeOnly Hora)` con el desfase fijo UTC−5 (Colombia no tiene horario de verano desde 1993); documentación XML con el motivo de no usar `TimeZoneInfo`. Pruebas: `backend/pruebas/Unitarias/Utilidades/HoraDeColombiaPruebas.cs` — 04:59 UTC es el día anterior a las 23:59; 05:00 UTC es 00:00 del mismo día; mediodía; fin de año
- [X] T006 [P] Reglas de quién gestiona y quién marca, en `backend/src/LFClub.Dominio/Reglas/`: `ReglaGestionDeActividad.PuedeGestionar(Rol rol, IReadOnlyCollection<Guid> categoriasDeLaActividad, IReadOnlySet<Guid> categoriasQueEntrena)` (PRESIDENTE siempre; DIRECTIVO o ENTRENADOR solo si entrena todas; JUGADOR y DESARROLLADOR nunca; sin categorías, nunca) y `ReglaAlcanceDeAsistencia.CategoriasMarcables(rol, categoriasDelEntrenamiento, categoriasQueEntrena)` (PRESIDENTE todas; quien entrena, la intersección; JUGADOR ninguna). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaGestionDeActividadPruebas.cs` y `ReglaAlcanceDeAsistenciaPruebas.cs` — PRESIDENTE sin asignación; ENTRENADOR con 2014 ante 2014, ante 2014+2015 y ante 2016; DIRECTIVO asignado y sin asignar; PRESIDENTE asignado a una sola; JUGADOR
- [X] T007 [P] Reglas de visibilidad, en `backend/src/LFClub.Dominio/Reglas/`: `ReglaVisibilidadDeEntrenamiento.LaVe(rol, entrenaAlgunaDeSusCategorias, suCategoriaEstaEntreLasDelEntrenamiento, tieneAsistenciaRegistrada)` y `ReglaVisibilidadDePartido.LoVe(rol, estado, entrenaAlgunaDeSusCategorias, estaConvocado)`, que implementan la tabla de research §4 (entrenamiento: PRESIDENTE y DIRECTIVO todos, ENTRENADOR si entrena alguna, JUGADOR si es de su categoría o tiene asistencia; partido: `JUGADO` todos, PRESIDENTE todos, quien entrena alguna, JUGADOR solo convocado; DESARROLLADOR nunca). Documentación XML que diga que los repositorios traducen esta misma condición a SQL. Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaVisibilidadDeEntrenamientoPruebas.cs` y `ReglaVisibilidadDePartidoPruebas.cs`, fila por fila de la tabla y para cada estado (`PROXIMO`, `CANCELADO`, `JUGADO`), con el DIRECTIVO sin asignar ante un partido próximo (no lo ve) y uno jugado (sí)
- [X] T008 [P] Reglas de momento y de estado, en `backend/src/LFClub.Dominio/Reglas/`: `ReglaMomentoDeActividad` (`YaEmpezo(fecha, horaInicio, hoy, ahora)`: `Fecha + HoraInicio <= ahora`; `YaTermino(fecha, horaFin, hoy, ahora)`; `SuFechaLlego(fecha, hoy)`: `Fecha <= hoy`) y `ReglaTransicionPartido` (`PuedeCancelar`, `PuedeReactivar`, `PuedeRegistrarResultado`, `PuedeCambiarDatos` y `PuedeConvocar` según `EstadoPartido`: PROXIMO → CANCELADO, CANCELADO → PROXIMO, PROXIMO → JUGADO, JUGADO → JUGADO; un JUGADO no se cancela, no vuelve a PROXIMO, no cambia datos ni convocatoria, RF-021). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaMomentoDeActividadPruebas.cs` (justo a la hora de inicio, un minuto antes, día anterior, misma fecha a otra hora) y `ReglaTransicionPartidoPruebas.cs` (cada par de estado y acción)
- [X] T009 [P] Reglas del resultado y de la asistencia, en `backend/src/LFClub.Dominio/Reglas/`: `ReglaResultadoDePartido.Validar(golesClub, golesRival, convocados, filasEnviadas)` que devuelve el primer código de error o ninguno: goles fuera de 0 a 99 → `validacion`; una fila de quien no está convocado → `jugador_no_convocado`; goles a quien no jugó → `goles_de_quien_no_jugo`; suma de goles por jugador mayor que `golesClub` → `goles_superan_marcador`; suma menor se acepta (RF-017, escenario 3.4); y `CalculoDeAsistencia.Porcentaje(asistidos, registrados)`: `round(asistidos * 100 / registrados)` al entero, 0 con 0 registrados (RF-030). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaResultadoDePartidoPruebas.cs` (3-1 con 2+1; 4 con 3 del club; gol a quien no jugó; 2 con 3 del club; sin filas) y `CalculoDeAsistenciaPruebas.cs` (0/0, 1/3 → 33, 2/3 → 67, 3/3)
- [X] T010 [P] Crear `ErroresDeActividades` en `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeActividades.cs`, como `ErroresDeCategorias`, con los códigos estables del contrato y su título en español: `no_gestiona_actividad` (403), `actividad_cancelada`, `actividad_no_cancelada`, `actividad_empezada`, `entrenamiento_futuro`, `partido_sin_empezar`, `partido_jugado`, `categoria_inactiva`, `categorias_no_cambian`, `jugador_fuera_de_la_actividad` (409) y `goles_de_quien_no_jugo`, `goles_superan_marcador`, `jugador_no_convocado` (400)
- [X] T011 Reloj y sembrador de pruebas (depende de T004): `RelojDePrueba` en `backend/pruebas/Integracion/Base/RelojDePrueba.cs` (implementa `IReloj`, con `AhoraUtc` que se fija y se mueve desde la prueba; por defecto el reloj real) registrado en `ConfigureTestServices` de `backend/pruebas/Integracion/Base/FabricaApi.cs` en lugar de `RelojSistema`, accesible como `_fabrica.Reloj`, y que vuelve al reloj real al empezar cada prueba que lo use; `SembradorActividades` en `backend/pruebas/Integracion/Base/SembradorActividades.cs` (máx. 250 líneas, apoyado en `Sembrador`, `SembradorCategorias` y `SembradorJugadores`) con los datos de partida de quickstart.md: club A con 2013, 2014, 2015 y 2016 activas; P sin asignación; E1 asignado a la 2014; E2 a la 2014 y la 2015; D1 asignado a la 2014; D2 sin asignaciones; J1 a J10 en la 2014 con J1 y J2 hermanos en la misma cuenta y J2 en la 2015; K1 en la 2015; M1 en la 2016; club B con PB y un jugador; cada integrante con su cliente de prueba (y el de J1/J2 con `X-Jugador-Elegido`). Más ayudantes que escriben directamente en la base para las pruebas que solo leen: `CrearEntrenamientoAsync(categorias, fecha, horaInicio, horaFin, estado)`, `CrearPartidoAsync(categorias, fecha, hora, convocados, estado)` y `RegistrarJugadoAsync(partido, golesClub, golesRival, filas)`
- [X] T012 [P] Tipos y fechas del frontend: `frontend/src/compartido/api/tiposActividades.ts` con `EstadoEntrenamiento`, `EstadoPartido`, `CondicionPartido`, `TipoPartido`, `RespuestaFamilia`, `CategoriaDeActividadDto`, `PermisosDeActividadDto`, `GuardarEntrenamientoDto`, `EntrenamientoResumenDto`, `EntrenamientoDto`, `PlanillaEntrenamientoDto`, `RegistrarAsistenciaDto`, `GuardarPartidoDto`, `PartidoResumenDto`, `PartidoDto`, `GoleadorDePartidoDto`, `PlanillaPartidoDto`, `GuardarConvocatoriaDto`, `RegistrarResultadoDto`, `ResponderDto` y `CalendarioDto`; `frontend/src/compartido/api/tiposEstadisticas.ts` con `CifrasDto`, `EstadisticasJugadorDto` y `GoleadoresDto`; copiados de `contracts/api.yaml` (campos opcionales con `?`), con un comentario por tipo y reutilizando `EnlaceFichaDto` y `UltimoCambioDto` de `tiposPersonas.ts`/`tiposFicha.ts`. `frontend/src/compartido/fechas.ts`: `diaDeLaSemana('2026-10-13')` → "martes 13 de octubre", `horaCorta('16:00')` → "4:00 p. m.", `rangoDeHoras(inicio, fin)`, `inicioDeSemana(fecha)` (lunes), `diasDeLaSemana(lunes)`, `sumarDias(fecha, n)` y `hoyEnColombia()` (`AAAA-MM-DD` con UTC−5, sin depender de la zona del dispositivo). Pruebas: `frontend/pruebas/fechas.test.ts` (formatos en español, semana que cruza de mes y de año, domingo → lunes anterior, `hoyEnColombia` a las 23:30 de Colombia)
- [X] T013 [P] Sacar el menú de `frontend/src/privado/DisposicionClub.tsx` a `frontend/src/privado/MenuDelClub.tsx` sin cambiar nada de lo que se ve (mismas entradas, mismo orden, mismos roles), con un comentario de componente, para que las historias le sumen "Calendario" (T017), "Partidos" (T019) y "Goleadores" (T027). Añadir en `frontend/src/compartido/componentes/Iconos.tsx` los iconos `IconoCalendario`, `IconoPartido` y `IconoGoleadores`, como los existentes. `DisposicionClub.tsx` baja de 216 líneas
- [X] T014 [P] Crear `frontend/src/privado/actividades/EtiquetaActividad.tsx` (depende de T012): etiqueta con tipo ("Entreno" o "Partido", como el lienzo) y estado escrito ("Programado", "Cancelado", "Próximo", "Jugado", "Falta el resultado" cuando `faltaResultado`), con clase de color además del texto (RF-036); estilos en `frontend/src/compartido/tema/componentes.css`. Pruebas: `frontend/pruebas/EtiquetaActividad.test.tsx` (cada estado tiene su texto; un partido próximo con `faltaResultado` dice "Falta el resultado")

**Punto de control**: la migración se aplica al arrancar, las reglas están probadas, el menú se ve igual que antes y la batería de la 001 a la 008 sigue en verde.

---

## Fase 3: Historia 1 - Programar entrenamientos y que la familia los vea (Prioridad: P1) 🎯 MVP

**Objetivo**: quien entrena una categoría (o el PRESIDENTE) programa, cambia, cancela y reactiva entrenamientos para una o varias categorías; los ven los jugadores activos de esas categorías, quienes entrenan alguna, los PRESIDENTES y los DIRECTIVOS, en el calendario y en "Próximos entrenamientos" del inicio.

**Prueba independiente**: con `SembradorActividades`, E2 programa un entrenamiento conjunto 2014 y 2015; lo ven J3, K1, P, D1 y D2 en calendario e inicio; M1 no; desde el club B, 404 (quickstart, bloque 1).

- [X] T015 [US1] Programar y consultar un entrenamiento, de punta a punta. Backend: DTOs `GuardarEntrenamientoDto`, `CategoriaDeActividadDto`, `PermisosDeActividadDto`, `EntrenamientoResumenDto` y `EntrenamientoDto` en `backend/src/LFClub.Aplicacion/DTOs/` (`miRespuesta`, `miAsistencia` y `nota` nula omitidos con `JsonIgnore` cuando son nulos); `ValidadorEntrenamiento` en `backend/src/LFClub.Aplicacion/Validadores/ValidadorEntrenamiento.cs` (fecha y horas con formato, `HoraFin > HoraInicio`, `Lugar` obligatorio ≤ 120 sin espacios a los lados, `Nota` ≤ 500, `categoriaIds` con al menos una y sin repetir → `400 validacion` con `errores` por campo; RF-005); `IRepositorioEntrenamientos` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioEntrenamientos.cs` y `RepositorioEntrenamientos` en `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEntrenamientos.cs` (agregar con sus categorías; obtener por identificador con categorías y años; si el jugador de la petición tiene asistencia y respuesta en él); `AccesoAActividad` en `backend/src/LFClub.Aplicacion/Utilidades/AccesoAActividad.cs`, como `AccesoAFicha`: lee la actividad, resuelve con `CategoriasDeAsync` las categorías que entrena quien pregunta, aplica `ReglaVisibilidadDeEntrenamiento` (si no la ve, `404 no_encontrado`) y calcula `PermisosDeActividadDto` (`gestiona` con `ReglaGestionDeActividad`; `vePlanilla` si es PRESIDENTE o entrena alguna; `responde` si es JUGADOR, `PROGRAMADO` y sin empezar); `MapperEntrenamientos` en `backend/src/LFClub.Aplicacion/Mappers/MapperEntrenamientos.cs` (categorías por año); `IServicioEntrenamientos`/`ServicioEntrenamientos` (`ProgramarAsync`: JUGADOR → `403 rol_no_autorizado`; categorías activas y del club o `409 categoria_inactiva`; sin gestión → `403 no_gestiona_actividad`; `Estado = PROGRAMADO`, `CreadoEn`, `CreadoPorUsuarioRolId`; `IRepositorioCategorias.MarcarUsadaAsync` por cada categoría, research §11; fecha pasada permitida, supuesto 4) e `IServicioConsultaEntrenamientos`/`ServicioConsultaEntrenamientos` (`ObtenerAsync` con `AccesoAActividad`) en `backend/src/LFClub.Aplicacion/Servicios/` e `Implementaciones/`; `ControladorEntrenamientos` en `backend/src/LFClub.Api/Controladores/Club/ControladorEntrenamientos.cs` con `[Route("api/clubes/{clubId:guid}/entrenamientos")]`, `POST` (201) y `GET {entrenamientoId:guid}`; registro en `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs` y `RegistroDeServicios.cs`; descomentar las dos operaciones y subir los contadores a 74 y 50. Pantalla: rutas `actividades/nueva` y `entrenamientos/:entrenamientoId` dentro de `/club/:clubId` en `frontend/src/App.tsx`; `frontend/src/privado/actividades/useCategoriasGestionables.ts` (con `GET /categorias`: el PRESIDENTE, todas las activas; los demás, las activas en cuya lista de entrenadores figura su `usuarioRolId`; solo comodidad); `frontend/src/privado/actividades/SelectorCategorias.tsx` (categorías como botones conmutables, como el lienzo, con estado elegido también en texto o `aria-pressed`); `frontend/src/privado/actividades/FormularioActividad.tsx` (por ahora solo "Entrenamiento": categorías, fecha, hora de inicio, hora de fin, lugar y nota; errores por campo del `400`; al guardar, al detalle); `frontend/src/privado/entrenamientos/DetalleEntrenamiento.tsx` con `useCarga` (fecha con `diaDeLaSemana`, horas, lugar, nota, categorías, `EtiquetaActividad`; con `404`, `Aviso` "No encontramos este entrenamiento." sin más datos). Pruebas: `backend/pruebas/Integracion/Entrenamientos/ProgramarEntrenamientoPruebas.cs` — E2 programa para 2014 y 2015 (201, `PROGRAMADO`, dos categorías, `Categoria.Usada` verdadera); P para la 2016 sin estar asignado; E1 con la 2015 o la 2016 → `403 no_gestiona_actividad`; D2 → 403; D1 para la 2014 → 201; J3 → `403 rol_no_autorizado`; fin antes del inicio, sin categorías, lugar vacío, nota de 501 → `400 validacion`; categoría desactivada o de otro club → `409 categoria_inactiva`; ningún jugador cambia de categoría (escenario 1.2); `GET` por identificador desde PB en la ruta de A y en la de B → 404 (escenario 1.8); M1 → 404 igual que un identificador inventado; J3 recibe el detalle sin `miRespuesta` ni `miAsistencia` en el texto
- [X] T016 [US1] Cambiar, cancelar y reactivar un entrenamiento, de punta a punta. Backend: en `ServicioEntrenamientos`, `CambiarAsync` (solo `PROGRAMADO`, si no `409 actividad_cancelada`; mismas categorías que tiene o `409 categorias_no_cambian`, supuesto 1; mismo validador), `CancelarAsync` (`PROGRAMADO` → `CANCELADO`; ya cancelado, `409 actividad_cancelada`) y `ReactivarAsync` (solo `CANCELADO` o `409 actividad_no_cancelada`; sin empezar según `HoraDeColombia` y `ReglaMomentoDeActividad` o `409 actividad_empezada`; todas sus categorías activas o `409 categoria_inactiva`; RF-038); los tres exigen ver la actividad (404) y gestionarla (`403 no_gestiona_actividad`); cancelar y reactivar no tocan categorías ni respuestas; nunca se borra (RF-035). En `ControladorEntrenamientos`, `PUT {entrenamientoId:guid}`, `POST {entrenamientoId:guid}/cancelacion` y `DELETE {entrenamientoId:guid}/cancelacion`, que responden `EntrenamientoDto`; descomentar las tres operaciones y subir los contadores a 77 y 53. Pantalla: en `DetalleEntrenamiento.tsx`, con `permisos.gestiona`, botones "Cambiar" (abre `FormularioActividad` en modo edición, con las categorías fijas y sin selector de tipo), "Cancelar entrenamiento" (con confirmación) y, si está cancelado y no ha empezado, "Reactivar"; los mensajes de los `409` con texto claro; sin `gestiona`, ningún botón (escenario quickstart 1.5). Pruebas: `backend/pruebas/Integracion/Entrenamientos/CancelacionEntrenamientoPruebas.cs` — E2 cambia el lugar y J3 y K1 leen el nuevo (escenario 1.5); E1 sobre el conjunto → 403 en las tres; cambiar categorías → `409 categorias_no_cambian`; cancelar: J3, K1, P y D2 siguen viéndolo con `CANCELADO` (escenario 1.6); cancelar dos veces → 409; cambiar uno cancelado → `409 actividad_cancelada`; reactivar antes de empezar → `PROGRAMADO` para todos (escenario 1.10); con `RelojDePrueba` pasada la hora de inicio → `409 actividad_empezada`; con una categoría desactivada → `409 categoria_inactiva`; reactivar uno programado → `409 actividad_no_cancelada`
- [X] T017 [US1] Calendario y "Próximos entrenamientos", de punta a punta. Backend: `CalendarioDto` en `backend/src/LFClub.Aplicacion/DTOs/CalendarioDto.cs` (`partidos` vacía hasta T022); `ValidadorRangoCalendario` en `backend/src/LFClub.Aplicacion/Validadores/ValidadorRangoCalendario.cs` (`desde` y `hasta` obligatorias, `desde <= hasta`, hasta 62 días ambos incluidos → `400 validacion`); en `RepositorioEntrenamientos`, `ListarVisiblesAsync(desde, hasta, quien)` y `ProximosVisiblesAsync(hoy, ahora, quien, 5)` con la condición de visibilidad de data-model **en el `Where`** (PRESIDENTE o DIRECTIVO; alguna categoría en las que entrena; JUGADOR con su categoría actual entre las del entrenamiento o con asistencia registrada), por fecha y hora; próximos = no terminados (`Fecha + HoraFin > ahora`), cancelados incluidos (RF-004, RF-006); `IServicioCalendario`/`ServicioCalendario` y `ProximosAsync` en `ServicioConsultaEntrenamientos`, con `miRespuesta` solo para la cuenta de un jugador; `ControladorCalendario` en `backend/src/LFClub.Api/Controladores/Club/ControladorCalendario.cs` (`GET api/clubes/{clubId:guid}/calendario?desde=&hasta=`) y `GET proximos` en `ControladorEntrenamientos` (declarado antes que `{entrenamientoId:guid}`); descomentar las dos operaciones y subir los contadores a 79 y 55. Pantalla: ruta `calendario` en `frontend/src/App.tsx` y entrada "Calendario" con `IconoCalendario` para todos los roles en `frontend/src/privado/MenuDelClub.tsx`; `frontend/src/privado/calendario/Calendario.tsx` (semana actual por defecto con `inicioDeSemana(hoyEnColombia())`, "← Semana anterior" y "Semana siguiente →", botón "Nuevo" solo si `useCategoriasGestionables` trae alguna categoría o es PRESIDENTE; "No hay actividades esta semana." cuando no hay), `frontend/src/privado/calendario/TiraDeSemana.tsx` (tira de días en el teléfono como `M-Calendario`, columnas por día en escritorio) y `frontend/src/privado/calendario/TarjetaActividad.tsx` (`EtiquetaActividad`, hora, lugar, categorías y enlace al detalle); `frontend/src/privado/inicio/TarjetaProximosEntrenamientos.tsx` pasa a pedir `GET /entrenamientos/proximos` con `useCarga` y lista fecha, horas, lugar y estado escrito, con enlace al detalle y "Ver calendario"; sin datos, "No hay entrenamientos próximos." (escenario 1.9); se borra la constante del espacio reservado y su comentario de la 007. Pruebas: `backend/pruebas/Integracion/Entrenamientos/VisibilidadEntrenamientosPruebas.cs` — con un entrenamiento conjunto 2014+2015 y otro de la 2016: calendario y próximos de J3, K1, P, D1, D2, E1 y E2 lo traen; M1 no, y sí el de la 2016; J1/J2 con cada hermano elegido ve lo de su categoría; para cada cuenta, lo que trae la lista coincide con lo que responde la consulta por identificador (200 o 404); un jugador que entra a la 2014 ve los próximos de la 2014 desde ese momento (caso límite); E1 sin asignación a la 2014 (desactivada) deja de ver el de la 2014. `backend/pruebas/Integracion/Entrenamientos/ProximosEntrenamientosPruebas.cs` — orden por fecha y hora; máximo 5; con `RelojDePrueba`, uno que ya terminó no sale y uno empezado sin terminar sí; el cancelado sale con `CANCELADO`; club sin entrenamientos → lista vacía. `backend/pruebas/Integracion/Partidos/CalendarioPruebas.cs` — rango de 62 días → 200; de 63, `hasta < desde` o sin fechas → `400 validacion`; incluye ambos extremos

**Punto de control**: quickstart, bloque 1, completo. Las familias ven sus entrenamientos en el inicio y en el calendario.

---

## Fase 4: Historia 2 - Convocar a un partido sin que nadie se entere de que no fue convocado (Prioridad: P1)

**Objetivo**: quien entrena todas las categorías de un partido (o el PRESIDENTE) lo crea, elige convocados entre los jugadores activos de esas categorías, lo cambia, cancela y reactiva; un partido no jugado solo existe para sus convocados, quienes entrenan alguna de sus categorías y los PRESIDENTES.

**Prueba independiente**: E1 crea un partido de la 2014 y convoca a siete de diez; con cada una de las diez cuentas se comprueba calendario, inicio, "Partidos", contadores y la consulta por identificador (quickstart, bloque 2).

- [X] T018 [US2] Pruebas primero de la visibilidad de un partido próximo (solo pruebas, que fallan hasta T019 y T020): `backend/pruebas/Integracion/Partidos/VisibilidadPartidoPruebas.cs` con `SembradorActividades.CrearPartidoAsync` (2014, convocados J1 y J3 a J8) — para las diez cuentas de la 2014: los siete convocados lo encuentran en `GET /calendario`, `GET /partidos` y `GET /partidos/{id}`; J9, J10 y J2 (la misma cuenta de J1 con J2 elegido) no lo encuentran en ninguna lista y su consulta por identificador responde exactamente lo mismo (estado y cuerpo, salvo el `traceId`) que un identificador inventado (CE-001, RF-011); con `?estado=PROXIMO` y `?categoriaId=` tampoco aparece ni cambia ningún total; D2 → no aparece, 404; D1, E1, E2 y P → aparece; un convocado no recibe `convocados`, `convocables` ni `jugadores` en el texto de la respuesta (CE-002, escenario 2.3); M1 y PB → 404. `backend/pruebas/Integracion/Partidos/DirectivoAsignadoPruebas.cs` — D1 (asignado a la 2014) ve, cambia y convoca en un partido de la 2014; en uno de la 2016 → 404; D2 no lo ve; tras desactivar la asignación de D1, el partido de la 2014 desaparece para él (caso límite)
- [X] T019 [US2] Crear y consultar partidos, de punta a punta. Backend: DTOs `GuardarPartidoDto`, `PartidoResumenDto` y `PartidoDto` en `backend/src/LFClub.Aplicacion/DTOs/` (`nombreTorneo`, `golesClub`, `golesRival`, `miRespuesta`, `goleadores` y `ultimoCambioResultado` omitidos cuando son nulos; `faltaResultado` calculado: `PROXIMO` y ya empezó); `ValidadorPartido` en `backend/src/LFClub.Aplicacion/Validadores/ValidadorPartido.cs` (`Rival` y `Lugar` obligatorios ≤ 120, fecha y hora con formato, `Condicion` y `Tipo` válidos, `NombreTorneo` obligatorio ≤ 120 si `TORNEO` y descartado (nulo) si `AMISTOSO`, `categoriaIds` con al menos una y sin repetir → `400 validacion`, RF-007, escenario 2.10); `IRepositorioPartidos` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioPartidos.cs` y `RepositorioPartidos` en `backend/src/LFClub.Infraestructura/Repositorios/RepositorioPartidos.cs` (agregar; obtener con categorías, convocados y la fila del jugador de la petición; `ListarVisiblesAsync(estado, categoriaId, quien)` y `EnRangoVisiblesAsync(desde, hasta, quien)` con la condición de visibilidad de data-model **en el `Where`**: `JUGADO`, o PRESIDENTE, o alguna categoría que entrena, o JUGADOR con fila en `PartidosJugador`; próximos por fecha ascendente, jugados y cancelados descendente); `AccesoAActividad` gana la rama del partido con `ReglaVisibilidadDePartido` y sus permisos (`vePlanilla` si PRESIDENTE o entrena alguna; `responde` si es JUGADOR convocado, `PROXIMO` y sin empezar); `MapperPartidos` en `backend/src/LFClub.Aplicacion/Mappers/MapperPartidos.cs`; `IServicioPartidos`/`ServicioPartidos` (`CrearAsync`, con las mismas reglas que `ProgramarAsync` y `MarcarUsadaAsync`, nace `PROXIMO` sin convocados) e `IServicioConsultaPartidos`/`ServicioConsultaPartidos` (`ListarAsync`, `ObtenerAsync`); `ControladorPartidos` en `backend/src/LFClub.Api/Controladores/Club/ControladorPartidos.cs` con `GET` (`?estado=&categoriaId=`), `POST` (201) y `GET {partidoId:guid}`; registro de servicios; descomentar las tres operaciones y subir los contadores a 82 y 58. Pantalla: rutas `partidos` y `partidos/:partidoId` en `frontend/src/App.tsx` y entrada "Partidos" con `IconoPartido` para todos en `MenuDelClub.tsx`; `FormularioActividad.tsx` gana el selector de tipo "Partido / Entrenamiento" del lienzo y, para partido, rival, fecha, hora, lugar, "Local"/"Visitante", "Amistoso"/"Torneo" y nombre del torneo (visible solo con torneo), cada tipo con su DTO y su endpoint; `frontend/src/privado/partidos/Partidos.tsx` (secciones "Próximos" y "Jugados", con `EtiquetaActividad`, rival, fecha, hora, condición escrita y marcador si está jugado; "No hay partidos." si no hay); `frontend/src/privado/partidos/DetallePartido.tsx` con `useCarga` (rival, fecha, hora, lugar, condición, tipo y torneo, categorías, estado; con `404`, "No encontramos este partido." sin más datos). Pruebas: `backend/pruebas/Integracion/Partidos/CrearPartidoPruebas.cs` — E1 crea para la 2014 (201, `PROXIMO`, sin convocados, `faltaResultado` falso); torneo sin nombre → 400; amistoso con nombre → se guarda sin nombre; E1 con la 2014 y la 2015 → `403 no_gestiona_actividad`; E2 con las dos → 201; P para la 2016 → 201; J3 → `403 rol_no_autorizado`; categoría inactiva → `409 categoria_inactiva`; un partido sin convocados lo ven solo E1, E2, D1 y P (caso límite); con `RelojDePrueba` pasada la hora, `faltaResultado` verdadero y sigue `PROXIMO`
- [X] T020 [US2] Convocatoria, de punta a punta. Backend: `GuardarConvocatoriaDto` y `PlanillaPartidoDto` en `backend/src/LFClub.Aplicacion/DTOs/`; en `RepositorioPartidos`, los convocables (jugadores activos y aprobados cuya categoría actual es una de las del partido y no están convocados) y reemplazar la convocatoria en una transacción (altas con `CategoriaId` copiada de la categoría actual, `Jugo = false`, `Goles = 0`, `ConvocadoEn`; bajas que borran la fila con su respuesta; quien sigue convocado conserva su fila, su categoría copiada y su respuesta, RF-013, RF-018, RF-034); `IServicioConvocatoria`/`ServicioConvocatoria`: `PlanillaAsync` (ver el partido o 404; JUGADOR → `403 rol_no_autorizado`; DIRECTIVO o ENTRENADOR que no entrena ninguna → 404 si está próximo, `403 no_gestiona_actividad` si está jugado; convocados por categoría copiada y apellidos con `enlace` de `ResolutorDeEnlaces`, `respuesta`, `jugo` y `goles`; conteos `asistiran`, `noAsistiran` y `sinResponder`; `convocables` vacía si no está `PROXIMO`) y `GuardarAsync` (gestionar o 403; solo `PROXIMO` según `ReglaTransicionPartido`, si no `409 actividad_cancelada` o `partido_jugado`; cada jugador activo, del club y de una categoría del partido o `409 jugador_fuera_de_la_actividad`, RF-009; lista vacía permitida); `ControladorConvocatoria` en `backend/src/LFClub.Api/Controladores/Club/ControladorConvocatoria.cs` con `GET api/clubes/{clubId:guid}/partidos/{partidoId:guid}/jugadores` y `PUT …/convocatoria`; descomentar las dos operaciones y subir los contadores a 84 y 60. Pantalla: `frontend/src/privado/partidos/EditorConvocatoria.tsx` (lista de jugadores por categoría con casillas, "Seleccionar todos" y "Ninguno", contador de seleccionados y el aviso del lienzo "Solo los jugadores que selecciones verán este partido"; nombres con `NombrePersona`); en `FormularioActividad.tsx`, al crear un partido se muestra el editor con los jugadores de las categorías elegidas y, tras el `POST`, se guarda la convocatoria con `PUT` y se va al detalle; en `DetallePartido.tsx`, con `permisos.vePlanilla`, la lista de convocados con su categoría y, con `permisos.gestiona` y `PROXIMO`, "Cambiar convocatoria" con el editor. Pruebas: `backend/pruebas/Integracion/Partidos/ConvocatoriaPruebas.cs` — E1 convoca a siete y la planilla los trae con la categoría 2014 copiada; quitar a J8: deja de verlo en calendario, lista e identificador (404) sin rastro; agregar a J9: lo ve (escenario 2.8); en un partido 2014+2015, E2 puede convocar de las dos y M1 (2016) → `409 jugador_fuera_de_la_actividad` (escenario 2.6); E1 sobre el conjunto: lo ve con su planilla, `PUT` → 403 (escenario 2.7); J3 sobre la planilla → `403 rol_no_autorizado`; un jugador retirado o de otro club → 409; un convocado que cambia de categoría sigue convocado con la categoría copiada (RF-034); convocatoria vacía → 200; P ve todos los partidos próximos con sus convocados (escenario 2.5). Con esta tarea, `VisibilidadPartidoPruebas` y `DirectivoAsignadoPruebas` (T018) pasan en lo que no depende de T021 y T022
- [X] T021 [US2] Cambiar, cancelar y reactivar un partido, de punta a punta. Backend: en `ServicioPartidos`, `CambiarAsync` (solo `PROXIMO`; si no, `409 actividad_cancelada` o `partido_jugado`, supuesto 2; mismas categorías o `409 categorias_no_cambian`), `CancelarAsync` (`PROXIMO` → `CANCELADO`; `JUGADO` → `409 partido_jugado`, RF-021; ya cancelado → `409 actividad_cancelada`) y `ReactivarAsync` (como el entrenamiento: `actividad_no_cancelada`, `actividad_empezada`, `categoria_inactiva`; convocatoria y respuestas como estaban, RF-038), todos con `ReglaTransicionPartido`, ver (404) y gestionar (403); en `ControladorPartidos`, `PUT {partidoId:guid}`, `POST` y `DELETE {partidoId:guid}/cancelacion`; descomentar las tres operaciones y subir los contadores a 87 y 63. Pantalla: en `DetallePartido.tsx`, con `permisos.gestiona`, "Cambiar" (formulario en edición, categorías fijas), "Cancelar partido" con confirmación y "Reactivar" si está cancelado y no ha empezado. Pruebas: `backend/pruebas/Integracion/Partidos/CancelacionPartidoPruebas.cs` — cancelar el conjunto: sus convocados, E1, E2, D1 y P lo ven `CANCELADO`; los no convocados y D2 siguen recibiendo 404 (escenario 2.9, RF-014); reactivar como E2: vuelve a `PROXIMO` con la misma convocatoria y las mismas respuestas, y los no convocados siguen sin verlo (escenario 2.11); empezado → `409 actividad_empezada`; cambiar el rival lo ven los convocados; cambiar categorías → 409; E1 sobre el conjunto → 403
- [X] T022 [US2] El calendario y el inicio suman los partidos visibles, de punta a punta (sin endpoints nuevos). Backend: `ServicioCalendario` llena `partidos` con `RepositorioPartidos.EnRangoVisiblesAsync`, por fecha y hora, con `miRespuesta` solo para la cuenta de un jugador convocado. Pantalla: `Calendario.tsx` y `TarjetaActividad.tsx` muestran los partidos con etiqueta "Partido", rival, hora, lugar y "Local"/"Visitante" (escenario 2.1); "Nuevo" lleva al formulario con los dos tipos. Pruebas: completar `backend/pruebas/Integracion/Partidos/CalendarioPruebas.cs` — el calendario de cada una de las diez cuentas de la 2014 trae el partido solo a los siete convocados; el de J1 y el de J2 en la misma cuenta difieren según el jugador elegido (quickstart 2.3); un partido cancelado solo a quienes lo veían. Con esta tarea, `VisibilidadPartidoPruebas` y `DirectivoAsignadoPruebas` (T018) pasan enteras

**Punto de control**: quickstart, bloque 2, completo. Ninguna cuenta no convocada encuentra rastro del partido.

---

## Fase 5: Historia 3 - Registrar el resultado, los goles y quién jugó (Prioridad: P1)

**Objetivo**: después de empezar el partido, quien lo gestiona registra marcador, quiénes jugaron y goles; el partido queda jugado y su resultado y sus goleadores los ve todo el club; se puede corregir.

**Prueba independiente**: se registra 3-1 con dos goles de J3 y uno de J4; se comprueba lo que ven J3, J10, D2 y el club B; se corrige a 3-2 cambiando un goleador (quickstart, bloque 3).

- [X] T023 [US3] Resultado y goles, de punta a punta. Backend: `RegistrarResultadoDto` y `GoleadorDePartidoDto` en `backend/src/LFClub.Aplicacion/DTOs/`; en `RepositorioPartidos`, guardar marcador y filas de convocados en una transacción (las no enviadas quedan `Jugo = false`, `Goles = 0`) y leer los goleadores de un partido (`Goles > 0`, de mayor a menor y por apellidos); `IServicioResultado`/`ServicioResultado`: gestionar o 403; `CANCELADO` → `409 actividad_cancelada`; `PROXIMO` sin empezar → `409 partido_sin_empezar` (RF-015, con `HoraDeColombia` y `ReglaMomentoDeActividad`); `ReglaResultadoDePartido` con sus códigos 400 (RF-017); estado a `JUGADO` con `ReglaTransicionPartido`; `ResultadoCambiadoEn`, `ResultadoCambiadoPorUsuarioId` y `ResultadoCambiadoPorNombre` copiado (RF-020); en `MapperPartidos` y `ServicioConsultaPartidos`, `goleadores` para todos en un `JUGADO` (con `enlace` de `ResolutorDeEnlaces`, que para una cuenta de jugador es nulo) y `ultimoCambioResultado` (`UltimoCambioDto` de la 008, `porLaCuentaDelJugador` falso) solo para quien ve la planilla; `ControladorResultado` en `backend/src/LFClub.Api/Controladores/Club/ControladorResultado.cs` con `PUT api/clubes/{clubId:guid}/partidos/{partidoId:guid}/resultado`; descomentar la operación y subir los contadores a 88 y 64. Pantalla: `frontend/src/privado/partidos/FormularioResultado.tsx` (goles del club y del rival; la lista de convocados, todos marcados como que jugaron de entrada, escenario 3.3; goles por jugador solo habilitados para quien jugó; aviso de la suma frente al marcador; mensajes de los 400 y 409); en `DetallePartido.tsx`, "Registrar resultado" o "Corregir resultado" solo con `permisos.gestiona` y el partido empezado o jugado; para un `JUGADO`, el marcador "LFClub 3 - 1 Rival" según la condición, los goleadores con `NombrePersona` y, para quien ve la planilla, "Último cambio por {autor}, {fechaYHora}"; "Falta el resultado" cuando `faltaResultado`; `Partidos.tsx` muestra el marcador en los jugados. Pruebas: `backend/pruebas/Integracion/Partidos/ResultadoPruebas.cs` — antes de la hora → `409 partido_sin_empezar` (con `RelojDePrueba`); empezado: 3-1 con J3 ×2 y J4 ×1 → `JUGADO`; gol a quien no jugó → `400 goles_de_quien_no_jugo`; 4 goles con 3 del club → `400 goles_superan_marcador`; 2 con 3 → 200; fila de J9 no convocado → `400 jugador_no_convocado`; jugado: J10, D2, M1 y P lo ven con rival, fecha, condición, tipo, torneo, marcador y goleadores, en `GET /partidos` y por identificador (escenario 3.5, RF-019); el texto de la respuesta para J10 y J3 no contiene `convocados`, `jugo` ni nombres de convocados que no hicieron goles (escenario 3.6, CE-002); corregir a 3-2 cambiando un goleador → los goleadores cambian y `ultimoCambioResultado` dice quién y cuándo (escenario 3.7); J3 pasa a la 2013 y el partido y sus goles siguen con la categoría 2014 (escenario 3.8, CE-004); cancelar un jugado → `409 partido_jugado`; reactivarlo → `409 actividad_no_cancelada`; cambiar sus datos o su convocatoria → `409 partido_jugado`; PB → 404

**Punto de control**: quickstart, bloque 3, completo. Con las historias 1 a 3 se puede operar la temporada.

---

## Fase 6: Historia 4 - Registrar la asistencia a entrenamientos (Prioridad: P2)

**Objetivo**: quien entrena registra y corrige, desde el día del entrenamiento, quién asistió de las categorías que entrena; el PRESIDENTE, de todas; el jugador ve solo la suya.

**Prueba independiente**: en un entrenamiento 2014+2015 de hoy, E1 marca solo a los de la 2014 y E2 a los de las dos; J3 ve solo "Asististe" o "No asististe" (quickstart, bloque 4).

- [X] T024 [US4] Planilla y asistencia, de punta a punta. Backend: `PlanillaEntrenamientoDto` y `RegistrarAsistenciaDto` en `backend/src/LFClub.Aplicacion/DTOs/`; `IRepositorioAsistencias` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioAsistencias.cs` y `RepositorioAsistencias` en `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAsistencias.cs` (jugadores activos cuya categoría actual está entre las marcables, más los que ya tienen asistencia en categorías marcables, data-model "Planilla de un entrenamiento"; sus respuestas y asistencias; escribir solo las filas enviadas, copiando `CategoriaId` la primera vez y sin cambiarla al corregir, `RegistradaEn`); `IServicioAsistencia`/`ServicioAsistencia`: `PlanillaAsync` (ver o 404; JUGADOR → `403 rol_no_autorizado`; quien no es PRESIDENTE ni entrena ninguna → `403 no_gestiona_actividad`; solo los de las categorías marcables con `ReglaAlcanceDeAsistencia`, escenario 4.2; `marcable`, `respuesta`, `presente` nulo si no se registró, `enlace`; conteos de respuestas; por categoría y apellidos) y `RegistrarAsync` (`CANCELADO` → `409 actividad_cancelada`; fecha que no ha llegado → `409 entrenamiento_futuro`, supuesto 5; cada jugador marcable o con asistencia en una categoría marcable, si no `409 jugador_fuera_de_la_actividad`; las filas no enviadas no cambian); en `ServicioConsultaEntrenamientos`, `miAsistencia` para la cuenta del jugador con asistencia registrada; `ControladorAsistencia` en `backend/src/LFClub.Api/Controladores/Club/ControladorAsistencia.cs` con `GET api/clubes/{clubId:guid}/entrenamientos/{entrenamientoId:guid}/jugadores` y `PUT …/asistencia`; descomentar las dos operaciones y subir los contadores a 90 y 66. Pantalla: `frontend/src/privado/entrenamientos/PlanillaAsistencia.tsx` (por categoría, cada jugador con `NombrePersona` y "Presente"/"Ausente" escritos en controles conmutables; los no marcables, de solo lectura; "Sin registrar" cuando `presente` es nulo; guardar envía solo los cambiados; en un entrenamiento futuro, "La asistencia se registra desde el día del entrenamiento."); en `DetalleEntrenamiento.tsx`, la planilla con `permisos.vePlanilla` y, para la cuenta de un jugador, "Asististe" o "No asististe" si hay `miAsistencia`. Pruebas: `backend/pruebas/Integracion/Entrenamientos/AsistenciaPruebas.cs` — entrenamiento 2014+2015 de hoy: E1 recibe solo a los de la 2014 y marcar a K1 → `409 jugador_fuera_de_la_actividad` (escenario 4.2); E2 recibe los de las dos y corrige a uno de la 2014; P marca a todos; D2 → `403 no_gestiona_actividad`; J3 → `403 rol_no_autorizado`; mañana → `409 entrenamiento_futuro`; cancelado → `409 actividad_cancelada` (escenario 4.3); J3 recibe `miAsistencia` y el texto de su respuesta no contiene la de otros (escenario 4.5, CE-002); J3 pasa a la 2013 y su asistencia conserva la 2014; un jugador con asistencia que cambió de categoría sigue en la planilla de quien entrena la categoría copiada; un jugador de la 2014 con asistencia en un entrenamiento de la 2014 y que ahora es de la 2013 sigue viéndolo (regla de visibilidad por asistencia)

**Punto de control**: quickstart, bloque 4, completo.

---

## Fase 7: Historia 5 - La familia confirma si su jugador asistirá (Prioridad: P2)

**Objetivo**: la cuenta del jugador responde "Asistirá" o "No asistirá" a los entrenamientos próximos de su categoría y a los partidos próximos a los que está convocado, hasta la hora de inicio; quien gestiona ve conteos y nombres.

**Prueba independiente**: tres familias responden "Asistirá", "No asistirá" y nada; quien entrena ve los conteos y los nombres; ninguna familia ve las respuestas de otras (quickstart, bloque 5).

- [X] T025 [US5] Respuestas de la familia, de punta a punta. Backend: `ResponderDto` en `backend/src/LFClub.Aplicacion/DTOs/ResponderDto.cs`; escribir y reemplazar la respuesta en `RepositorioEntrenamientos` (`RespuestaEntrenamiento` con `RespondidaEn`) y en `RepositorioPartidos` (`PartidoJugador.Respuesta` y `RespondidaEn` de la fila del convocado); `IServicioRespuestas`/`ServicioRespuestas`: solo JUGADOR (los demás, `403 rol_no_autorizado`), siempre sobre el integrante de la petición; entrenamiento: verlo (404), ser activo de una de sus categorías, `PROGRAMADO` o `409 actividad_cancelada`, sin empezar o `409 actividad_empezada`; partido: convocado o 404 como si no existiera, `PROXIMO` (cancelado → `409 actividad_cancelada`) y sin empezar o `409 actividad_empezada` (RF-023); `PUT {entrenamientoId:guid}/mi-respuesta` en `ControladorEntrenamientos` y `PUT {partidoId:guid}/mi-respuesta` en `ControladorPartidos`, que responden el DTO de la actividad con `miRespuesta`; descomentar las dos operaciones y subir los contadores a 92 y 68. Pantalla: `frontend/src/privado/calendario/BotonesRespuesta.tsx` (los dos botones del lienzo con los textos "Asistirá" y "No asistirá", la opción elegida también escrita "Respondiste: Asistirá", deshabilitados cuando `permisos.responde` es falso o en los resúmenes sin permiso, con el mensaje de los 409); se usa en `TarjetaActividad.tsx` para la cuenta del jugador y en `DetalleEntrenamiento.tsx` y `DetallePartido.tsx`; `frontend/src/privado/entrenamientos/ResumenRespuestas.tsx` (conteos "Asistirán", "No asistirán", "Sin responder" y los nombres de cada grupo con `NombrePersona`, a partir de la planilla) en los dos detalles para quien ve la planilla (escenario 5.2). Pruebas: `backend/pruebas/Integracion/Entrenamientos/RespuestasPruebas.cs` — en un entrenamiento de mañana de la 2014, J3 "Asistirá", J4 "No asistirá", J5 nada: la planilla de E1 trae 1, 1 y 8 con los nombres; J3 cambia su respuesta; el texto de la respuesta para J4 no contiene la de J3 (escenario 5.4); con `RelojDePrueba` pasada la hora → `409 actividad_empezada`; cancelado → `409 actividad_cancelada`; M1 (2016) → 404; E1 → `403 rol_no_autorizado`; con hermanos, la cuenta de J1/J2 responde por cada uno según el elegido; registrar asistencia "Ausente" a J3 conserva su "Asistirá" (RF-025, escenario 5.5); en un partido, un convocado responde y sale en la planilla; un no convocado → 404; quitarlo de la convocatoria borra su respuesta y al volver a convocarlo está sin responder; reactivar un entrenamiento cancelado conserva las respuestas

**Punto de control**: quickstart, bloque 5, completo.

---

## Fase 8: Historia 6 - Estadísticas del jugador y goleadores del club (Prioridad: P3)

**Objetivo**: la ficha del jugador muestra partidos jugados, goles y porcentaje de asistencia, en total y por categoría, calculados de lo registrado; una pantalla de goleadores por categoría para todo el club.

**Prueba independiente**: con los registros de las historias 3 y 4, las cifras de la ficha de J3 y los goleadores de la 2014 coinciden con un conteo a mano, antes y después de corregir (quickstart, bloque 6).

- [X] T026 [US6] Estadísticas en la ficha, de punta a punta. Backend: `CifrasDto` y `EstadisticasJugadorDto` (con `porCategoria` de `CifrasDto` + `CategoriaDeActividadDto`, por año descendente) en `backend/src/LFClub.Aplicacion/DTOs/`; `IRepositorioEstadisticas` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioEstadisticas.cs` y `RepositorioEstadisticas` en `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEstadisticas.cs`, con `GROUP BY` por `CategoriaId` copiada: partidos jugados = filas de `PartidosJugador` con `Jugo` en partidos `JUGADO`; goles = suma de `Goles` en partidos `JUGADO`; registrados y asistidos = `AsistenciasEntrenamiento` de entrenamientos no cancelados y, de esas, con `Presente`; `MapperEstadisticas` en `backend/src/LFClub.Aplicacion/Mappers/MapperEstadisticas.cs` (total = suma de las categorías; porcentaje con `CalculoDeAsistencia`); `IServicioEstadisticas`/`ServicioEstadisticas` con `DelJugadorAsync` que pasa por `AccesoAFicha` de la 005 (quien no ve la ficha, el mismo 404, RF-028); `ControladorEstadisticas` en `backend/src/LFClub.Api/Controladores/Club/ControladorEstadisticas.cs` con `GET api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/estadisticas`; descomentar la operación y subir los contadores a 93 y 69. Pantalla: `frontend/src/privado/ficha/SeccionEstadisticas.tsx` (pide las estadísticas con `useCarga`; "Partidos jugados", "Goles" y "Asistencia a entrenamientos: 67 % (2 de 3)" en total y una fila por categoría "Categoría 2014"; sin registros, todo en cero con "Todavía no hay partidos ni entrenamientos registrados."); `frontend/src/privado/ficha/FichaJugador.tsx` la añade sin pasar de 250 líneas. Pruebas: `backend/pruebas/Integracion/Estadisticas/EstadisticasJugadorPruebas.cs` — con dos partidos jugados (uno donde J3 fue convocado y no jugó), tres asistencias registradas (dos presentes) y una en un entrenamiento cancelado, las cifras de J3 coinciden con un conteo hecho en la prueba desde las filas (CE-003); corregir el resultado y una asistencia cambia las cifras en la siguiente consulta (RF-027); J3 pasado a la 2013 tiene filas para 2014 y 2013 (CE-004); un jugador sin registros → todo 0 y `porCategoria` vacía (escenario 6.2); J5 sobre J3 → 404, P y E1 → 200 (escenario 6.6); PB y el DESARROLLADOR → 404; un partido `PROXIMO` con filas no cuenta
- [X] T027 [US6] Goleadores del club, de punta a punta. Backend: `GoleadoresDto` en `backend/src/LFClub.Aplicacion/DTOs/GoleadoresDto.cs`; en `RepositorioEstadisticas`, las categorías con goles (por año descendente) y los goleadores de una categoría (`PartidosJugador.CategoriaId` elegida, partidos `JUGADO`, `Goles > 0`, sumados por jugador, de mayor a menor, luego apellidos y nombres, retirados incluidos, research §9); `GoleadoresAsync(categoriaId?)` en `ServicioEstadisticas`, solo nombres, apellidos, goles y `enlace` de `ResolutorDeEnlaces` (RF-029); una categoría de otro club o inexistente → 404; `GET api/clubes/{clubId:guid}/goleadores?categoriaId=` en `ControladorEstadisticas`; descomentar la operación y subir los contadores a 94 y 70. Pantalla: ruta `goleadores` en `frontend/src/App.tsx` y entrada "Goleadores" con `IconoGoleadores` para todos en `MenuDelClub.tsx`; `frontend/src/privado/partidos/Goleadores.tsx` (selector de categoría con `categoriasConGoles`, la primera elegida por defecto; tabla de posición, nombre con `NombrePersona` y goles; "Todavía no hay goles registrados." si no hay). Pruebas: `backend/pruebas/Integracion/Estadisticas/GoleadoresPruebas.cs` — sin `categoriaId`, las categorías con goles y `goleadores` vacía; con la 2014, J3 y J4 de mayor a menor (escenario 6.3); J3 en la 2013 sigue en la 2014 y no aparece en la 2013 (escenario 6.4); J4 retirado sigue (escenario 6.5); el texto de la respuesta no contiene documento, fecha de nacimiento, categoría actual ni otro dato del jugador; para la cuenta de J5 todos los `enlace` son nulos; J10, D2 y P obtienen lo mismo; PB → 404

**Punto de control**: quickstart, bloque 6, completo salvo el retiro (T028).

---

## Fase 9: Retiro del jugador (RF-033)

**Propósito**: que el retiro saque al jugador de las convocatorias pendientes en los dos caminos de retiro, conservando su historial.

- [X] T028 Retiro y convocatorias, de punta a punta (sin pantalla nueva). Backend: en `IRepositorioJugadores` (`backend/src/LFClub.Aplicacion/Interfaces/IRepositorioJugadores.cs`) y `RepositorioJugadores` (`backend/src/LFClub.Infraestructura/Repositorios/RepositorioJugadores.cs`), `BorrarPendientesDeActividadesAsync(usuarioRolId, hoy, ahora)` que borra sus filas de `PartidosJugador` en partidos que no están `JUGADO` y sus `RespuestasEntrenamiento` de entrenamientos que no han empezado; `ServicioRetiroJugador` (`backend/src/LFClub.Aplicacion/Implementaciones/ServicioRetiroJugador.cs`) la llama en la misma transacción que lo saca de sus equipos; `RepositorioRetiroEntreClubes.RetirarDeOtrasCuentasAsync` (`backend/src/LFClub.Infraestructura/Repositorios/Plataforma/RepositorioRetiroEntreClubes.cs`) hace lo mismo con el `IgnoreQueryFilters` acotado por documento que ya usa para equipos y foto; documentación XML de las dos clases reescrita con este efecto. Asistencias pasadas y filas de partidos jugados no se tocan. Pruebas: `backend/pruebas/Integracion/Partidos/RetiroYConvocatoriaPruebas.cs` — J4 convocado a un partido próximo y a uno cancelado, con goles en uno jugado, asistencia pasada y respuesta a un entrenamiento futuro: al retirarlo P sale de las dos convocatorias pendientes y su respuesta futura desaparece; el jugado, sus goles en `GET /goleadores` y su asistencia pasada siguen (quickstart 6.4); reactivar el cancelado no lo vuelve a convocar; la planilla de un entrenamiento futuro ya no lo trae; el mismo resultado cuando el retiro ocurre por la aprobación de un hermano en otro club, y las filas del otro club no se tocan más allá de ese jugador

---

## Fase 10: Cierre y aspectos transversales

**Propósito**: aislamiento, contrato, pantallas y validación de extremo a extremo.

- [X] T029 Comprobar que no queda ninguna operación comentada en `specs/009-entrenamientos-y-partidos/contracts/api.yaml`, que `AccesoClubPruebas` pasa con 94 y 70 (las 22 rutas nuevas responden `404` al integrante de otro club y al DESARROLLADOR, y `401` sin sesión; CE-005) y que Swagger (`/swagger`) describe las 22 operaciones con sus DTOs, sus códigos de error y sus `problem+json`
- [X] T030 [P] Revisión de pantallas a 360 px y en escritorio, en tema claro y oscuro (RF-036, CE-008): calendario con semana llena y vacía, tira de días, formulario de entrenamiento y de partido, editor de convocatoria con 20 jugadores, detalles con planilla, resumen de respuestas, formulario de resultado, goleadores, estadísticas en la ficha y la tarjeta del inicio; nombres y rivales muy largos con `overflow-wrap: anywhere`; todo estado legible como texto. Contrastar con `Convocatoria` y `M-Calendario` del lienzo (plan, nota 1). Los ajustes van en `frontend/src/compartido/tema/componentes.css`, `frontend/src/compartido/tema/movil.css` o un `frontend/src/compartido/tema/actividades.css` nuevo importado donde se importan los demás
- [X] T031 [P] Revisar la documentación XML y los comentarios de componente de todo lo nuevo y de las clases que nombra el plan en §3 (`Categoria`, `UsuarioRol`, `AsignacionEntrenadorCategoria`, `ServicioRetiroJugador`, `RepositorioRetiroEntreClubes`), y que ningún archivo pase de 250 líneas con `scripts/verificar-tamano.ps1` (en especial `DisposicionClub.tsx`, `FichaJugador.tsx`, `FormularioActividad.tsx`, `DetallePartido.tsx`, `RepositorioPartidos.cs`, `ControladorPartidos.cs`, `ContextoLFClub.cs` y `SembradorActividades.cs`)
- [X] T032 Recorrer `specs/009-entrenamientos-y-partidos/quickstart.md` entero en un proyecto de Compose aparte (`docker compose -p lfclub-validacion …` con `Brevo__Llave` vacía, para no enviar correos reales con la llave del `.env`), incluidas las comprobaciones directas contra la API de los bloques 1.3, 1.7, 2.2, 2.4, 3.1, 6.5 y 7.1. Si Docker se duerme sin contenedores, mantener uno mínimo en marcha y borrarlo al final junto con `docker compose -p lfclub-validacion down -v`
- [X] T033 Batería completa: `dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `tsc`, `eslint`, `npm --prefix frontend run build` y `./scripts/verificar-tamano.ps1`, con las pruebas de la 001 a la 008 en verde

---

## Fase 11: Ajustes del propietario (2026-10-11, después de implementar)

**Propósito**: cambios pedidos por el propietario al revisar las pantallas (spec, "Ajustes del
propietario 2026-10-11", RF-001, RF-007, RF-039, RF-040 y RF-041).

- [X] T034 [US1] Tira de días con actividad (RF-039, solo pantalla). En `frontend/src/privado/calendario/TiraDeSemana.tsx`, clase `con-actividad` en los días con alguna actividad; en `frontend/src/compartido/tema/calendario.css`, tinte del color del club con `color-mix` y borde, sin quitar el número escrito; el día elegido conserva su estilo
- [X] T035 [US2] Rival opcional (RF-007, escenario 2.12). Backend: `Partido.Rival` nulable, sin `IsRequired` en `ConfiguracionPartido`, migración `RivalOpcional`; `ValidadorPartido` con `Maximo` en lugar de `Obligatorio` (vacío o en blanco queda nulo); `rival` omitido de `PartidoResumenDto` y `PartidoDto` cuando es nulo; contrato sin `rival` en `required`. Pantalla: "Rival (opcional)" con el texto de ayuda "Por definir"; `nombreRival` en `frontend/src/privado/partidos/textos.ts`, usado en la tarjeta, el detalle y el marcador. Pruebas: `CrearPartidoPruebas.El_rival_se_puede_dejar_por_definir_y_escribir_despues` (sin rival → 201 y sin `rival` en el texto; en blanco queda sin rival; escribirlo después; 121 caracteres → 400) y `frontend/pruebas/textosPartidos.test.ts`
- [X] T036 [US1] Varios días al programar un entrenamiento (RF-001, escenario 1.11, solo pantalla). `frontend/src/privado/actividades/SelectorFechas.tsx` ("+ Agregar otro día" propone el mismo día de la semana siguiente; "Quitar"); `FormularioEntrenamiento.tsx` crea un entrenamiento por día distinto con el `POST` existente, de uno en uno; si uno falla, avisa cuántos se programaron y deja en la lista solo los días que faltan; con varios, al terminar lleva al calendario. Al editar sigue habiendo una sola fecha
- [X] T037 [US1] "Editar" más visible (RF-040, solo pantalla). "Cambiar" pasa a "Editar" en `AccionesDeEstado.tsx` y en los títulos de los dos detalles; `TarjetaActividad.tsx` muestra "Editar" (enlace al detalle con `?editar=1`) si `Calendario.tsx` calcula que quien mira es PRESIDENTE o entrena todas las categorías de la actividad, y la actividad está programada o próxima; los detalles abren el formulario con `?editar=1` solo si `permisos.gestiona` y el estado lo permite
- [X] T038 [US1] Dos días en el teléfono (RF-039, solo pantalla). `TiraDeSemana.tsx` muestra debajo el día elegido y el siguiente (el domingo con el sábado), en dos columnas; en la tira, el día que acompaña al elegido lleva borde (`acompanado`)
- [X] T039 [US5] Casillas de respuesta (RF-041, solo pantalla). `BotonesRespuesta.tsx`: "Confirma asistencia" arriba y, en una línea, casilla verde con chulo y roja con X (`aria-label` "Asistirá" y "No asistirá", `aria-pressed`), la elegida rellena y escrita al lado; estilos en `actividades.css` con los colores de éxito y error del tema
- [X] T040 Lápiz en las fotos (solo pantalla, specs 005, 007 y 008). `frontend/src/compartido/componentes/LapizDeImagen.tsx` (sin imagen abre el selector; con imagen que se puede quitar, menú "Cambiar…"/"Quitar…"; formatos en la descripción del lápiz) e `IconoLapiz`, usados en `SeccionFoto.tsx` (foto del jugador), `SeccionFotoPerfil.tsx` (foto de perfil) y `EditorIdentidad.tsx` (escudo, sin quitar); sin campo de archivo visible ni textos de formato; estilos en `frontend/src/compartido/tema/imagenes.css`. Los documentos de la ficha no cambian

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (T001)**: sin dependencias.
- **Cimientos (T002 a T014)**: después de T001. Bloquean todas las historias.
- **Historia 1 (T015 a T017)**: después de los cimientos.
- **Historia 2 (T018 a T022)**: después de T015 (usa `AccesoAActividad`, `FormularioActividad` y el patrón del servicio) y de T017 para T022 (calendario).
- **Historia 3 (T023)**: después de T020 (necesita convocados).
- **Historia 4 (T024)**: después de T016 (necesita entrenamientos y su detalle); no depende de las historias 2 y 3.
- **Historia 5 (T025)**: después de T017, T020 y T024 (los botones van en el calendario y el resumen sale de las dos planillas).
- **Historia 6 (T026, T027)**: después de T023 y T024 (cuenta lo que esas registran).
- **Retiro (T028)**: después de T020 y T025.
- **Cierre (T029 a T033)**: después de todo lo anterior.

### Entre tareas

- T002 → T003 → T004 → T011 (enumeraciones, entidades, esquema, sembrador).
- T012 → T014 (la etiqueta usa los tipos).
- T005 y T008 → T016, T021, T023, T024, T025 (momentos en hora de Colombia).
- T006 y T007 → T015 (`AccesoAActividad`).
- T009 → T023 y T026.
- T018 se escribe antes de T019 y T020 y queda en rojo hasta T022.
- T015 → T016 → T017 (mismo servicio, controlador y detalle).
- T019 → T020 → T021 (mismo repositorio, controlador y detalle); T022 después de T017 y T020.
- T026 → T027 (mismo repositorio, servicio y controlador).
- Los contadores de `AccesoClubPruebas` suben en el orden T015, T016, T017, T019, T020, T021, T023, T024, T025, T026, T027. Si se cambia el orden, cada tarea sube los suyos sobre lo que haya.

### Dentro de cada tarea

- Backend (DTO → validador → interfaz → repositorio → regla o utilidad → servicio → controlador →
  registro → contrato), luego pantalla contra la API real, luego pruebas. La tarea no se marca
  hecha sin sus pruebas en verde.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos, tras T001
T002  Enumeraciones           (luego T003 → T004 → T011)
T005  HoraDeColombia
T006  ReglaGestionDeActividad y ReglaAlcanceDeAsistencia
T007  Reglas de visibilidad
T008  ReglaMomentoDeActividad y ReglaTransicionPartido
T009  ReglaResultadoDePartido y CalculoDeAsistencia
T010  ErroresDeActividades
T012  tiposActividades.ts, tiposEstadisticas.ts y fechas.ts   (luego T014)
T013  MenuDelClub.tsx e iconos

# Historia 4 mientras avanza la historia 2 (archivos distintos)
T024  Planilla y asistencia
T019  Crear y consultar partidos

# Cierre
T030  Revisión a 360 px y en los dos temas
T031  Documentación y tamaño
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T015 a T017).
3. **Parar y validar**: quickstart, bloque 1. Las familias ya ven los entrenamientos en el inicio.

### Entrega incremental

1. Cimientos → la migración se aplica y nada cambia para nadie.
2. Historia 1 → entrenamientos en el calendario y en el inicio.
3. Historia 2 → partidos y convocatoria, protegidos por §17.2. **No se publica T019 sin T020**:
   un partido sin forma de convocar solo lo verían quienes lo gestionan, pero la historia no está
   completa.
4. Historia 3 → resultados visibles para todo el club.
5. Historia 4 → asistencia.
6. Historia 5 → respuestas de la familia.
7. Historia 6 → estadísticas y goleadores.
8. Retiro → convocatorias pendientes limpias al retirarse.
9. Cierre → contrato, aislamiento, 360 px, quickstart y batería.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- Las migraciones de esta funcionalidad son `EntrenamientosYPartidos` y `RivalOpcional` (T035). La
  aplicación está en construcción: no hay datos que migrar.
- Ninguna tarea modifica la constitución: la decisión sobre el DIRECTIVO asignado ya está en la
  4.6.0.
- Si una prueba de la 001 a la 008 falla al tocar `DisposicionClub.tsx`, `FichaJugador.tsx`,
  `TarjetaProximosEntrenamientos.tsx`, `ServicioRetiroJugador`, `RepositorioRetiroEntreClubes`,
  `RepositorioJugadores` o `FabricaApi` (por el `RelojDePrueba`) y no está claro si es una
  regresión o un cambio previsto, se detiene esa parte y se pregunta (§25).
- Confirmar el cambio en git al terminar cada tarea o cada historia.
- [X] T041 Corregir el rival de un partido jugado (RF-042). `PUT /partidos/{id}/rival` (`CorregirRivalDto`, `IServicioPartidos.CorregirRivalAsync`, `IRepositorioPartidos.CambiarRivalAsync`, error `partido_no_jugado`), `frontend/src/privado/partidos/CorregirRival.tsx` en `DetallePartido.tsx`; pruebas `RivalDePartidoJugadoPruebas.cs` y `CorregirRival.test.tsx`
- [X] T042 Bloque "Falta el resultado" en la lista de partidos (RF-043), `frontend/src/privado/partidos/Partidos.tsx`
- [X] T043 Texto que explica la casilla de goles de cada jugador en `FormularioResultado.tsx`
