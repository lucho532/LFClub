---

description: "Lista de tareas de la funcionalidad 012: avisos de entrenamientos y partidos en la campana, y lápiz único de identidad en la ficha"
---

# Tareas: Avisos de entrenamientos y partidos en la campana

**Entrada**: documentos de diseño en `specs/012-avisos-de-actividades/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
contracts/campana.md, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.10.0) y el plan (tabla de
trazabilidad). Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa
contra la API real y sus pruebas; las tareas que solo traen pruebas, solo reglas puras o solo
pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario. Las historias 1, 2 y 3 son P1 y la
4 es P2; el orden de construcción sigue al plan: **primero la historia 3** (la campana unificada,
sin generar nada), después las historias 1 y 2 y al final la 4. El ajuste de la ficha pedido por el
propietario (bloque B del plan, RF-036 a RF-038) es la historia **US5**, independiente de las demás.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US5]**: historia de usuario de spec.md a la que pertenece la tarea (US5 = ajuste de la
  ficha)
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 011, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): dos tablas nuevas (`AvisosDeActividad` y
  `AvisosDeActividadDestinatario`) y una sola migración, `AvisosDeActividad` (T005). Si una tarea
  posterior parece necesitar otra tabla, columna o índice, se detiene y se informa (§25). Ninguna
  columna de una tabla existente cambia.
- **Contrato**: `specs/012-avisos-de-actividades/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs.
  T002 deja comentadas las 4 operaciones nuevas y cada tarea que construye una la descomenta.
  **Punto de partida real: contrato 108 y club 84** (comprobado en
  `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`; confirmarlo con T001 antes de
  fiarse de estos números). Recorrido: 108/84 → **108/84** (T014: tres operaciones se *mueven* de
  `/comunicados/recibidos` a `/campana`, así que se quitan 3 del contrato de la 011 y se añaden 3 en
  el de la 012 en la misma tarea) → **109/85** (T015, lectura de un aviso).
- **Lo anterior no cambia**: salvo las tres operaciones movidas, ningún endpoint existente cambia.
  El detalle de un comunicado recibido y su lectura (`GET` y `PUT …/comunicados/recibidos/{id}…`)
  siguen donde están. Si una prueba de la 001 a la 011 falla y no está claro que sea una regresión,
  se detiene y se pregunta (§27.2).
- **Quién recibe**: solo cuentas de jugador. El equipo del club (PRESIDENTE, DIRECTIVO,
  ENTRENADOR) no tiene filas de destinatario ni campana (RF-028). "Jugador activo" es `Rol =
  JUGADOR`, `EstadoIngreso = APROBADO`, `Activo` y con categoría (research §7).
- **Sin avisos fuera de la aplicación** (RF-027): ningún aviso llama a `IServicioCorreo` ni a
  ningún servicio externo, no se pide permiso de notificaciones y no se añade ningún cliente de
  salida ni dependencia.
- **Mismo contador y misma lista** (RF-018): la campana suma comunicados y avisos, cada elemento una
  sola vez por cuenta; el nombre accesible pasa a "Novedades, N sin leer".
- **Comparar identificadores por bytes** (research §3): para mezclar comunicados y avisos en memoria
  se usa `OrdenDeIdentificadores` (orden de red, igual que PostgreSQL), nunca `Guid.CompareTo`.
- **Un aviso nunca revela más que la actividad visible** (RF-016, RF-017): el texto del aviso de
  asistencia no lleva nombres ni estados individuales; el DTO no lleva datos de la actividad más allá
  de tipo, identificador y `disponible`.
- **Transacción y bloqueo** (research §5): los servicios de la 009 y la 010 que pasan a avisar
  usan `EnTransaccionAsync`, toman `IRepositorioClub.BloquearAsync` y vuelven a leer la actividad
  dentro, como ya hace `ServicioResultado`; el aviso se genera dentro de esa transacción y solo si
  hubo un cambio real (RF-004, RF-026).
- **Texto plano** (research §10): el texto del aviso se guarda ya redactado, con 300 caracteres como
  máximo, y se pinta como texto de React. Nunca `dangerouslySetInnerHTML`.
- **Nombres** (§2.1, §2.2): todo en español; "familia" solo en textos de pantalla, el rol sigue
  siendo JUGADOR. `AvisoDeActividad`, `AvisoDeActividadDestinatario`, `HechoDeActividad`,
  `TipoDeActividad`, `ServicioAvisosDeActividad`, `RedaccionDeAvisos`.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) con qué representa, su responsabilidad y qué no hace; se amplía la de `UsuarioRol`,
  `Entrenamiento` y `Partido`.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. `ServicioPartidos.cs`
  ronda las 200: si el cambio lo acerca al límite, lo nuevo va a un archivo parcial o a
  `ServicioAvisosDeActividad`. Medir con `scripts/verificar-tamano.ps1` al empezar (T001) y al
  cerrar.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Controladores con
  `[IntegranteDelClub]` que solo delegan; reglas en `Dominio/Reglas`, puras.
- **Aislamiento** (§7.1): las dos entidades implementan `IPerteneceAClub`; todo bajo
  `/api/clubes/{clubId}` con el filtro de club activo; otro club y el DESARROLLADOR reciben 404.
- **Pantallas** (RF-033, CE-008, §24): componentes `Tarjeta`, `Aviso`, `Boton`, `Campo`,
  `DialogoConfirmacion`; legibles a 360 px y en los dos temas, sin desplazamiento horizontal;
  todo estado escrito con texto ("Aviso", "Comunicado", "Sin leer", "Leído"). El lienzo no tiene
  estas pantallas: el diseño sigue `contracts/campana.md` con los tokens del proyecto y lo revisa el
  propietario.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

Son técnicos y no tocan ninguna regla de la spec (research §15). Si cambia alguno, cambia solo la
regla o constante indicada, no el modelo.

| # | Supuesto | Dónde |
| --- | --- | --- |
| 1 | Un partido jugado se avisa a las familias de todas sus categorías | `ReglaDestinatariosDeAviso` (T007), T022 |
| 2 | El aviso de asistencia llega solo a las categorías en las que cambió una asistencia | `RegistrarAsync` de `RepositorioAsistencias` (T019) |
| 3 | `Texto` admite 300 caracteres; lugar y rival, 120 cada uno | `RedaccionDeAvisos` (T008), T005 |
| 4 | Corregir solo los goles de un jugador no avisa (el marcador no cambió) | T022 |
| 5 | El nombre visible de la campana pasa a "Novedades" | `textos.ts` (T016) |
| 6 | Un aviso de convocatoria por cuenta y por guardado, nombrando a sus jugadores | T021 |
| 7 | Cambiar solo la nota (entrenamiento) o el rival, la condición o el torneo (partido) no avisa | `ReglaCambiosDeActividad` (T006) |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: medir el punto de partida y que la batería siga en verde con el contrato de la 012 ya
en `specs/`.

- [X] T001 Medir el punto de partida (sin cambios de código): ejecutar `scripts/verificar-tamano.ps1` y anotar qué archivos de los que se tocan están cerca de 250 líneas (`ServicioPartidos.cs` ronda las 200); confirmar contra `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` que los contadores reales son 108 (contrato) y 84 (club) y que `dotnet test backend/LFClub.sln` y `npm --prefix frontend test` pasan. Si los números difieren de los de este archivo, contarlos con la misma expresión regular de `EndpointsDeLaApi` sobre `specs/*/contracts/api.yaml` y corregir los de las tareas T014 y T015 antes de seguir
- [X] T002 Comentar en `specs/012-avisos-de-actividades/contracts/api.yaml` las 4 operaciones nuevas, cada una con una marca `# 012-T0NN` que diga qué tarea la descomenta: `GET /campana` (T014), `GET /campana/contador` (T014), `POST /campana/lectura-total` (T014) y `PUT /campana/avisos/{avisoId}/lectura` (T015). Comentar cada operación y la ruta que quede vacía. No tocar todavía el contrato de la 011. Comprobar que `AccesoClubPruebas` sigue en 108 y 84 y que la batería entera pasa

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: enumeraciones, entidades, migración, reglas puras con sus pruebas unitarias, textos,
DTOs, tipos del frontend y ayudas de prueba. Al terminar, la migración se aplica y nada cambia para
nadie.

**⚠️ CRÍTICO**: ninguna historia empieza antes de terminar esta fase (salvo US5, que no la usa).

- [X] T003 [P] Enumeraciones en `backend/src/LFClub.Dominio/Enumeraciones/`, serializadas como texto como `Rol`, con documentación XML: `TipoDeActividad.cs` (`ENTRENAMIENTO`, `PARTIDO`) y `HechoDeActividad.cs` (`ENTRENAMIENTO_PROGRAMADO`, `ENTRENAMIENTO_CAMBIADO`, `ENTRENAMIENTO_CANCELADO`, `ENTRENAMIENTO_REACTIVADO`, `ASISTENCIA_REGISTRADA`, `PARTIDO_CAMBIADO`, `PARTIDO_CANCELADO`, `PARTIDO_REACTIVADO`, `JUGADOR_CONVOCADO`, `RESULTADO_PUBLICADO`)
- [X] T004 Entidades (depende de T003). En `backend/src/LFClub.Dominio/Entidades/`, con documentación XML e `IPerteneceAClub`: `AvisoDeActividad.cs` (`Id` con `Guid.CreateVersion7()`, `ClubId` "obligatorio", `Hecho` "obligatorio", `ActividadTipo` "obligatorio; coherente con `Hecho`, lo garantiza el servicio", `ActividadId` "uuid, obligatorio; entrenamiento o partido del club; sin foránea (research §2)", `Texto` "texto ≤ 300, obligatorio, ya redactado, texto plano", `GeneradoEn` en UTC) y `AvisoDeActividadDestinatario.cs` (clave `(AvisoId, UsuarioRolId)`, `ClubId` "obligatorio", `UsuarioId` "cuenta dueña del jugador", `LeidoEn` "nulo mientras no lo lea esa cuenta; igual en las filas de la misma cuenta y aviso"). Ampliar la documentación XML de `UsuarioRol.cs`, `Entrenamiento.cs` y `Partido.cs` ("de él cuelgan los avisos que recibió" / "puede ser la actividad de un aviso"). Sin autor, título, audiencia, retiro ni campo de edición
- [X] T005 Configuraciones, `DbSet` y migración (depende de T004). Nuevas `ConfiguracionAvisoDeActividad.cs` y `ConfiguracionAvisoDeActividadDestinatario.cs` en `backend/src/LFClub.Infraestructura/Datos/Configuraciones/` según data-model.md: tablas `AvisosDeActividad` y `AvisosDeActividadDestinatario`; `Hecho` y `ActividadTipo` como texto; `Texto` con `HasMaxLength(300)` y restricción de comprobación `char_length("Texto") > 0`; `ClubId` con foránea a `Clubes` en cascada; destinatarios en cascada con el aviso, con el jugador (`UsuarioRolId`) y con `Usuarios` (`UsuarioId`); índices `(ClubId, Id)` y `(ClubId, ActividadTipo, ActividadId)` en avisos, y `(UsuarioId, ClubId, AvisoId)` y parcial `(UsuarioId, ClubId) WHERE "LeidoEn" IS NULL` en destinatarios. Dos `DbSet` en `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs`, con el filtro de club como las demás. Migración `AvisosDeActividad` en `backend/src/LFClub.Infraestructura/Datos/Migraciones/`; sin datos que migrar
- [X] T006 [P] Regla de cambios, en `backend/src/LFClub.Dominio/Reglas/ReglaCambiosDeActividad.cs`, pura y con documentación XML: compara fecha, hora (inicio o fin) y lugar de antes y de después y devuelve qué cambió (`Fecha`, `Hora`, `Lugar`, o varios) y si no cambió nada. Cambiar solo la nota del entrenamiento, o el rival, la condición o el torneo del partido, no es un cambio (RF-004, RF-029; research §6). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaCambiosDeActividadPruebas.cs` (nada cambia, solo fecha, solo hora de inicio, solo hora de fin, solo lugar, varias a la vez, solo nota, solo rival, lugar de nulo a valor y de valor a nulo)
- [X] T007 [P] Regla de destinatarios, en `backend/src/LFClub.Dominio/Reglas/ReglaDestinatariosDeAviso.cs`, pura y con documentación XML: dado el `HechoDeActividad` y, para un partido, su estado (`PROXIMO` o `JUGADO`), devuelve la cobertura según research §7: cualquier hecho de entrenamiento y `ASISTENCIA_REGISTRADA` → `CATEGORIAS`; `PARTIDO_CAMBIADO`, `PARTIDO_CANCELADO` y `PARTIDO_REACTIVADO` de un partido próximo → `CONVOCADOS`; `JUGADOR_CONVOCADO` → `ALUDIDOS`; `RESULTADO_PUBLICADO` → `CATEGORIAS`. Un partido jugado que cambia de lugar o se cancela también va a `CATEGORIAS` (lo ve todo el club, 009 RF-019). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaDestinatariosDeAvisoPruebas.cs` (los 10 hechos y las dos situaciones del partido)
- [X] T008 [P] Redacción de los textos, en `backend/src/LFClub.Aplicacion/Utilidades/RedaccionDeAvisos.cs`, pura y con documentación XML, según las plantillas de research §10: "Nuevo entrenamiento el sábado a las 9:00", "Cambió la hora del entrenamiento: ahora el sábado a las 10:00" (con "la fecha", "el lugar" o varias, y el valor nuevo), "Entrenamiento del sábado cancelado", "El entrenamiento del sábado sigue en pie", "Se registró la asistencia del entrenamiento del sábado" (sin quién asistió ni quién faltó, RF-016), "Cambió el lugar del partido contra Rival: ahora en Cancha Minitas", "Partido contra Rival del sábado cancelado", "El partido contra Rival del sábado sigue en pie", "Mateo fue convocado al partido contra Rival el sábado a las 15:00" (varios nombres: "Mateo y Sofía fueron convocados…") y "Resultado del partido contra Rival: LFClub 3 - 1 Rival". La fecha se dice "hoy", "mañana", "el sábado" (próximos 6 días contados en hora de Colombia) o "el 18 de octubre"; la hora, "a las 9:00" (24 horas, sin cero a la izquierda). Los días y los meses van en una tabla propia, no en `CultureInfo`. Sin rival "Por definir": "el partido del sábado". El marcador sigue el orden de `textoDeMarcador` de la pantalla (local: club primero; visitante: rival primero) con el nombre del club copiado al generar. Máximo 300 caracteres: recorta el lugar o el rival (120 cada uno). Pruebas: `backend/pruebas/Unitarias/Utilidades/RedaccionDeAvisosPruebas.cs` (las 10 plantillas, hoy, mañana, día de la semana, fecha lejana, sin rival, local y visitante, texto recortado a 300)
- [X] T009 [P] Comparador de identificadores, en `backend/src/LFClub.Aplicacion/Utilidades/OrdenDeIdentificadores.cs`, puro y con documentación XML: compara dos `Guid` con `ToByteArray(bigEndian: true)` byte a byte sin signo, igual que PostgreSQL (research §3), y ofrece el orden descendente. Pruebas: `backend/pruebas/Unitarias/Utilidades/OrdenDeIdentificadoresPruebas.cs` con parejas cuyo orden **difiere** entre `Guid.CompareTo` y el orden de bytes, y dos UUID v7 creados en instantes distintos (el posterior va primero en descendente)
- [X] T010 [P] DTOs del backend. En `backend/src/LFClub.Aplicacion/DTOs/`: `ActividadDeAvisoDto.cs` (`Tipo`, `Id`, `Disponible`), `AvisoRecibidoDto.cs` (`Id`, `Texto` "máx. 300, texto plano", `GeneradoEn`, `Actividad`, `Leido`, `LeidoEn` "se omite del JSON cuando es nulo"), `ElementoDeCampanaDto.cs` (`Tipo` `COMUNICADO` o `AVISO`; exactamente uno de `Comunicado` o `Aviso`, el otro se omite), `PaginaDeCampanaDto.cs` (`Elementos`, `HayMas`, `SinLeer`) y `ContadorDeCampanaDto.cs` (`SinLeer`), como registros con documentación XML y los nombres, tipos y opcionales de `contracts/api.yaml` (§23). `ComunicadoRecibidoDto` se reutiliza tal cual (spec 011). `PaginaDeRecibidosDto` y `ContadorDeComunicadosDto` se eliminan en T014, no antes
- [X] T011 [P] Tipos del frontend: `frontend/src/compartido/api/tiposCampana.ts` con `TipoDeActividad`, `ActividadDeAvisoDto`, `AvisoRecibidoDto`, `TipoDeElementoDeCampana`, `ElementoDeCampanaDto`, `PaginaDeCampanaDto` y `ContadorDeCampanaDto` según `contracts/api.yaml` (opcionales los que el contrato omite: `leidoEn`, `comunicado`, `aviso`). No se toca `tiposComunicados.ts` hasta T014
- [X] T012 Ayudas de prueba (depende de T005). Nuevo `backend/pruebas/Integracion/Base/SembradorAvisos.cs`: `GenerarAsync(club, hecho, actividadTipo, actividadId, texto, destinatarios)` que escribe las filas como lo haría el servicio (para preparar escenarios sin pasar por la API), `LeerAsync(aviso, cuenta)` y `ContarAsync(cuenta)`. Reutiliza `SembradorActividades`, `SembradorComunicados` y los sembradores de clubes, categorías y jugadores, y añade `ConvocarAsync` o `CambiarCategoriaAsync` solo si faltan. Ninguna prueba existente cambia

**Punto de control**: la migración se aplica, las reglas puras y los textos pasan sus pruebas y la
batería de la 001 a la 011 sigue en verde (contrato 108, club 84).

---

## Fase 3: Historia de usuario 3 - La campana reúne comunicados y avisos (Prioridad: P1) 🎯 MVP

**Objetivo**: la campana de la familia tiene su ruta `/campana`, que mezcla comunicados y avisos
del más reciente al más antiguo, suma ambos al mismo contador, marca como leído al tocar y marca
todos. Todavía no se genera ningún aviso: la historia se prueba con avisos sembrados. Así la campana
ya sirve cuando lleguen las historias 1 y 2.

**Prueba independiente**: entrar con una cuenta con un comunicado y dos avisos sembrados sin leer:
contador 3, lista mezclada, abrir un aviso y "Marcar todos como leídos" (quickstart, bloque 3).

- [X] T013 [US3] Repositorio de avisos (depende de T005, T009 y T010; sin endpoint todavía). `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioAvisos.cs`, `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAvisos.cs` y `RepositorioAvisos.Campana.cs` (parcial), con documentación XML: `ListarVisiblesAsync(clubId, cuentaId, jugadoresLimitadosIds, antesDe, limiteMasUno)` (filas agrupadas por `AvisoId`, del `Id` más reciente al más antiguo; solo filas de la cuenta, del club y, si hay, del jugador de la sesión limitada; jugador aprobado y sin retirar; `leido` = ninguna fila del grupo con `LeidoEn` nulo), `ContarSinLeerAsync` (`COUNT(DISTINCT AvisoId)` con `LeidoEn` nulo, por el índice parcial), `MarcarLeidoAsync` (pone `LeidoEn` en todas las filas de la cuenta para ese aviso, solo donde es nulo), `MarcarTodosAsync`, `ObtenerParaLecturaAsync` (404 sin distinguir inexistente, de otro club o no dirigido a la cuenta) y `DisponibilidadAsync` (research §11: `ENTRENAMIENTO` disponible si alguna de sus categorías es la categoría actual de un jugador activo de la cuenta dentro de la limitación; `PARTIDO` si `Estado = JUGADO` o algún jugador activo de la cuenta tiene fila en `PartidosJugador`). Registrar en `RegistroDeServicios.cs`. Pruebas de repositorio con `SembradorAvisos`: `backend/pruebas/Integracion/Avisos/RepositorioAvisosPruebas.cs` (agrupado por aviso con dos hermanos, retirado y en espera no cuentan, limitación de sesión, otro club vacío)
- [X] T014 [US3] Campana unificada: lista, contador y "marcar todos", de punta a punta (depende de T002, T010, T011 y T013). Backend: `IServicioConsultaCampana` y `ServicioConsultaCampana`, `IServicioLecturaCampana` y `ServicioLecturaCampana` en `backend/src/LFClub.Aplicacion/Servicios/` y `Implementaciones/`, y `backend/src/LFClub.Aplicacion/Mappers/MapperCampana.cs`. La lista pide a cada repositorio (comunicados recibidos y avisos) el mismo cursor `antesDe` y el límite más uno, mezcla con `OrdenDeIdentificadores` y toma `limite` (de 1 a 50, 30 por defecto); `hayMas` si cualquiera trajo más o la mezcla pasó del límite; `sinLeer` = comunicados sin leer + avisos sin leer, cada uno una vez por cuenta; el contador y "marcar todos" tocan los dos tipos, y "marcar todos" es idempotente y devuelve el contador en cero. Nuevo `ControladorCampana.cs` en `backend/src/LFClub.Api/Controladores/Club/` (`[IntegranteDelClub]`, solo delega): `GET /api/clubes/{clubId}/campana`, `GET /api/clubes/{clubId}/campana/contador` y `POST /api/clubes/{clubId}/campana/lectura-total`; solo JUGADOR, los demás roles 403 `rol_no_autorizado`; el DESARROLLADOR y otro club, 404. **Mover**: quitar de `ControladorComunicadosRecibidos.cs` la lista, el contador y "marcar todos" (quedan detalle y lectura del comunicado); quitar `ListarRecibidosAsync` y `ContarSinLeerAsync` de `IServicioConsultaComunicados` y de `ServicioConsultaComunicados`, y `MarcarTodosAsync` de `IServicioLecturaComunicados` y de `ServicioLecturaComunicados`; eliminar `PaginaDeRecibidosDto.cs` y `ContadorDeComunicadosDto.cs`; registrar los servicios nuevos en `RegistroDeCasosDeUso.cs`. **Contrato**: descomentar las 3 operaciones de T002 y quitar las 3 operaciones movidas de `specs/011-comunicados-y-campana/contracts/api.yaml`, dejando una nota de "movida a la 012" en la descripción de la ruta; `AccesoClubPruebas` queda en 108 y 84. Frontend (contra la API real): `frontend/src/compartido/api/tiposComunicados.ts` pierde `PaginaDeRecibidos` y `ContadorDeComunicadosDto`; `useContadorComunicados.ts`, `ListaRecibidos.tsx` y "Marcar todos como leídos" en `PantallaComunicados.tsx` llaman a `/campana`; la lista ya acepta el `ElementoDeCampanaDto` (los avisos se pintan en T016). Pruebas de integración en `backend/pruebas/Integracion/Avisos/CampanaMezclaPruebas.cs`: contador igual a comunicados + avisos, mezcla por fecha con identificadores cuyo orden por bytes difiere del de .NET, hermanos que reciben lo mismo cuentan una vez, `hayMas` con cursor, "marcar todos" marca los dos tipos, leído por cuenta (dos familias no se afectan), sesión limitada al documento de un hermano, 403 al PRESIDENTE, al DIRECTIVO y al ENTRENADOR, 404 al DESARROLLADOR y a otro club. Migrar a `/campana` las pruebas de la 011 que usaban las tres rutas: `backend/pruebas/Integracion/Comunicados/CampanaYContadorPruebas.cs`, `ConsultaRecibidosPruebas.cs`, `LecturaPruebas.cs` y `SinAvisosExternosPruebas.cs`, y las de `frontend/pruebas/CampanaComunicados.test.tsx` y `LecturaDeComunicados.test.tsx`
- [X] T015 [US3] Leer un aviso y saber si su actividad sigue disponible, de punta a punta (depende de T013 y T014). Backend: `ServicioLecturaCampana.MarcarAvisoLeidoAsync` y `PUT /api/clubes/{clubId}/campana/avisos/{avisoId}/lectura` en `ControladorCampana.cs`: idempotente (leerlo otra vez no cambia `LeidoEn`), lo marca leído para toda la cuenta y no solo para el jugador elegido, devuelve `AvisoRecibidoDto` con `actividad.disponible` calculado por `DisponibilidadAsync` y nunca datos de la actividad más allá de tipo e identificador; un aviso inexistente, de otro club o no dirigido a la cuenta → 404 `no_encontrado`, sin distinguir los casos. Descomentar la operación y subir `AccesoClubPruebas` a **109 y 85**. Frontend: `useLecturaDeAviso` (o función equivalente en `useListaPaginada.ts`/`ListaRecibidos.tsx`) que llama a la lectura, baja el contador y devuelve `disponible` para que la pantalla decida (T016). Pruebas en `backend/pruebas/Integracion/Avisos/DisponibilidadPruebas.cs`: `disponible` coincide con que `GET …/entrenamientos/{id}` o `GET …/partidos/{id}` respondan 200 o 404 para esa misma cuenta (entrenamiento de su categoría, de otra, partido con y sin fila de convocatoria, partido jugado), leer dos veces no mueve `LeidoEn`, el aviso de otra cuenta y el de otro club → 404, el identificador de un aviso o de una actividad que no es suya no da acceso (RF-032)
- [X] T016 [US3] Pantalla de la campana con avisos (depende de T014 y T015; sigue `contracts/campana.md`). En `frontend/src/privado/comunicados/`: `FilaDeAviso.tsx` (texto del aviso a una o dos líneas, sin título ni autor, fecha, **"Aviso"** y **"Sin leer"** o **"Leído"** en texto, lo sin leer en negrita; al tocar llama a la lectura y, si `actividad.disponible`, navega a `/club/:clubId/entrenamientos/:id` o `/club/:clubId/partidos/:id`; si no, la fila pasa a "Leído" y debajo aparece `AvisoNoDisponible`), `AvisoNoDisponible.tsx` ("Esa actividad ya no está disponible para ti." en región de estado educada, sin diálogo); `ListaRecibidos.tsx` pinta la mezcla con `FilaDeAviso` o la fila de comunicado de la 011, que ahora dice **"Comunicado"** y **"Sin leer"/"Leído"** en texto; `PantallaComunicados.tsx` (rama JUGADOR): título "Novedades", vacío "Todavía no hay novedades del club.", "Ver más" con `hayMas`; `textos.ts` con las constantes nuevas. `frontend/src/compartido/componentes/CampanaComunicados.tsx`: nombre accesible "Novedades, nada por leer", "Novedades, 3 sin leer" y "Novedades, más de 99 sin leer"; el número muestra "99+" desde 100. La rama del equipo del club no cambia ("Comunicados publicados"). Texto siempre como texto plano, sin enlaces ni formato interpretados. Estilos en el CSS que ya usa la carpeta; si el archivo pasa de 250 líneas, a uno propio. Pruebas: `frontend/pruebas/FilaDeAviso.test.tsx` (texto, "Aviso" y estado en texto, navega al detalle, mensaje de no disponible sin navegar, no interpreta HTML), y se amplían `frontend/pruebas/CampanaComunicados.test.tsx` (nombre accesible con 0, 3 y 100) y `LecturaDeComunicados.test.tsx` (lista mezclada, "Marcar todos" con avisos, "Ver más")

**Punto de control**: con avisos sembrados, la familia ve la campana unificada completa; los
comunicados de la 011 siguen igual. Es el MVP: ya se puede probar cada pantalla antes de que el
sistema genere avisos.

---

## Fase 4: Historia de usuario 1 - La familia se entera de lo que pasa con los entrenamientos (Prioridad: P1)

**Objetivo**: programar, cambiar fecha, hora o lugar, cancelar, reactivar y registrar la asistencia
de un entrenamiento genera un aviso a las cuentas con un jugador activo en sus categorías, una
sola vez por cuenta y sin que nadie lo escriba.

**Prueba independiente**: programar, cambiar, cancelar, reactivar y registrar asistencia como
ENTRENADOR y mirar la campana de cuentas de la categoría y de otras que no lo son (quickstart,
bloque 1).

- [X] T017 [US1] Generación de avisos (depende de T006, T007, T008 y T013; sin llamadores todavía). `backend/src/LFClub.Aplicacion/Servicios/IServicioAvisosDeActividad.cs` e `Implementaciones/ServicioAvisosDeActividad.cs`, con documentación XML, y en `IRepositorioAvisos`/`RepositorioAvisos.cs` `GenerarAsync(aviso, destinatarios)`: una operación por hecho (`ProgramadoAsync`, `CambiadoAsync`, `CanceladoAsync`, `ReactivadoAsync`, `AsistenciaRegistradaAsync` para entrenamientos; `PartidoCambiadoAsync`, `PartidoCanceladoAsync`, `PartidoReactivadoAsync`, `JugadorConvocadoAsync`, `ResultadoPublicadoAsync` para partidos). Cada una arma el texto con `RedaccionDeAvisos`, resuelve los destinatarios con `ReglaDestinatariosDeAviso` en **una consulta del club** (research §7: jugadores activos de las categorías, de los convocados o de los aludidos), excluye siempre las filas de la cuenta que hizo la acción (RF-005), deja **una fila por jugador** y no genera nada si no hay destinatarios. Un aviso sin ningún destinatario no se guarda. Debe ejecutarse dentro de la transacción del llamador y no abrir una propia. Registrar en `RegistroDeCasosDeUso.cs`. Pruebas: `backend/pruebas/Integracion/Avisos/GeneracionDeAvisosPruebas.cs` llamando al servicio directamente (jugadores en espera, retirados y sin categoría no reciben; hermanos con dos filas del mismo aviso; excluye a la cuenta de quien actúa; sin destinatarios no guarda nada)
- [X] T018 [US1] Programar, cambiar, cancelar y reactivar un entrenamiento, de punta a punta (depende de T017). Backend: en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioEntrenamientos.cs`, `ProgramarAsync` genera `ENTRENAMIENTO_PROGRAMADO` y `CambiarAsync`, `CancelarAsync` y `ReactivarAsync` pasan a `EnTransaccionAsync`, toman `IRepositorioClub.BloquearAsync`, vuelven a leer el entrenamiento y generan `ENTRENAMIENTO_CAMBIADO` (solo si `ReglaCambiosDeActividad` dice que fecha, hora o lugar cambiaron, y diciendo cuál y cómo queda), `ENTRENAMIENTO_CANCELADO` o `ENTRENAMIENTO_REACTIVADO` solo si el estado cambió de verdad. Cambiar solo la nota no avisa. Un entrenamiento de dos categorías avisa a la cuenta con hermanos en las dos una sola vez (RF-006). Frontend: sin pantalla nueva; la fila de T016 muestra el aviso y su detalle (ya muestra "Cancelado" cuando corresponde). Pruebas en `backend/pruebas/Integracion/Avisos/AvisosDeEntrenamientoPruebas.cs`: programar avisa a la categoría y no a la 2016, a un jugador en espera ni a uno retirado; dos categorías con hermanos → un solo aviso; cambiar hora, fecha o lugar (el texto dice qué cambió y cómo queda); guardar lo mismo no avisa (RF-004); solo la nota no avisa; cancelar y reactivar; el equipo del club no recibe ni tiene campana (403 en `/campana`); un fallo de validación o un 409 no deja aviso; el aviso abre el entrenamiento cancelado con 200; ninguna respuesta de los endpoints existentes cambia
- [X] T019 [US1] Registrar la asistencia, de punta a punta (depende de T017). Backend: `IRepositorioAsistencias.RegistrarAsync` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioAsistencias.cs` pasa a devolver las categorías en las que alguna asistencia fue nueva o cambió de valor, y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioAsistencias.cs` las calcula (research §6); `ServicioAsistencia.RegistrarAsync` pasa a `EnTransaccionAsync` con `BloquearAsync` y, si hay categorías con cambio, genera `ASISTENCIA_REGISTRADA` solo para esas categorías (supuesto 2), con el texto general que no dice quién asistió ni quién faltó (RF-016). Volver a guardar lo mismo no avisa. Frontend: sin pantalla nueva. Pruebas en `backend/pruebas/Integracion/Avisos/AvisosDeEntrenamientoPruebas.cs` (ampliar, o `AvisosDeAsistenciaPruebas.cs` si pasa de 250 líneas): registra y avisa; el texto de la respuesta de `/campana` y del detalle no contiene ningún nombre ni estado de otros jugadores (CE-004; spec 009 RF-024); guardar lo mismo no avisa; una asistencia nueva en una sola categoría de un entrenamiento conjunto avisa solo a esa categoría; la respuesta de `RegistrarAsync` de la 009 no cambia

**Punto de control**: las familias reciben avisos de todos los hechos de un entrenamiento y la
historia 3 los muestra. La historia 1 se valida sola con el quickstart (bloque 1).

---

## Fase 5: Historia de usuario 2 - La familia se entera de lo que pasa con los partidos (Prioridad: P1)

**Objetivo**: convocar a un jugador, cambiar fecha, hora o lugar, cancelar, reactivar y registrar o
corregir el resultado de un partido avisa a las familias que corresponden. Un partido próximo
avisa solo a las familias de los convocados; sacar a alguien de la convocatoria no avisa a nadie.

**Prueba independiente**: convocar a algunos jugadores de una categoría, cambiar y cancelar el
partido y publicar el resultado, y comprobar quién recibe cada aviso (quickstart, bloque 2).

- [X] T020 [US2] Cambiar, cancelar y reactivar un partido, de punta a punta (depende de T017). Backend: en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioPartidos.cs`, `CambiarAsync`, `CancelarAsync` y `ReactivarAsync` pasan a `EnTransaccionAsync` con `BloquearAsync` y vuelven a leer el partido; generan `PARTIDO_CAMBIADO` (solo si `ReglaCambiosDeActividad` dice que fecha, hora o lugar cambiaron), `PARTIDO_CANCELADO` o `PARTIDO_REACTIVADO` solo si el estado cambió. Un partido próximo avisa solo a las cuentas con un jugador convocado (RF-007); un partido jugado, a las categorías (T007). `CrearAsync` **no** genera aviso (RF-003) y `CorregirRivalAsync` tampoco (RF-029). Si el archivo pasa de 240 líneas, mover lo nuevo a `ServicioPartidos.Avisos.cs` (parcial) o a `ServicioAvisosDeActividad`. Frontend: sin pantalla nueva. Pruebas en `backend/pruebas/Integracion/Avisos/AvisosDePartidoPruebas.cs`: crear sin convocados no avisa a nadie; con convocados, cambiar hora o lugar avisa solo a sus familias; una familia no convocada no ve ningún aviso del partido y `GET …/partidos/{id}` le responde 404 (CE-002); cancelar y reactivar avisan a los convocados; cambiar rival, condición o torneo no avisa; guardar lo mismo no avisa; un partido cancelado sigue abriéndose desde el aviso marcado como cancelado
- [X] T021 [US2] Convocar a jugadores, de punta a punta (depende de T017). Backend: `ServicioConvocatoria.GuardarAsync` en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConvocatoria.cs` pasa a `EnTransaccionAsync` con `BloquearAsync` y vuelve a leer el partido; con las altas que ya calcula agrupa por cuenta y genera **un** `JUGADOR_CONVOCADO` por cuenta, con texto que nombra a sus jugadores convocados ("Mateo y Sofía fueron convocados al partido contra …", RF-015) y filas de destinatario **solo de esos jugadores**, para que una sesión limitada al documento de otro hermano no vea el aviso (research §9). Las bajas no generan aviso, ni a la familia del jugador sacado ni a las demás (RF-002). Un guardado con varios cambios genera un aviso por familia afectada y una sola vez. Un jugador retirado queda fuera de las convocatorias sin generar aviso (spec 009, RF-033). Frontend: sin pantalla nueva. Pruebas en `backend/pruebas/Integracion/Avisos/ConvocatoriaYAvisosPruebas.cs`: convocar a un jugador avisa solo a su familia; dos hermanos en un guardado → un aviso que nombra a los dos; con una sesión limitada a J1 se ve el aviso que nombra a J1 y no el de J2; sacar a un jugador no avisa a nadie y el aviso sin leer que ya tenía se conserva, se puede leer y dice que la actividad ya no está disponible (RF-021); guardar la misma convocatoria no avisa; una cuenta no convocada no recibe nada
- [X] T022 [US2] Registrar o publicar el resultado, de punta a punta (depende de T017). Backend: `ServicioResultado.RegistrarAsync` en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioResultado.cs` y `ServicioResultadoFamilia.PublicarAsync` en `ServicioResultadoFamilia.cs` generan `RESULTADO_PUBLICADO` dentro de su transacción actual, con el marcador tal como lo ve la familia (RF-014), a las cuentas con un jugador activo en **todas** las categorías del partido (supuesto 1), solo si el partido no estaba jugado o algún gol del marcador cambió; corregir solo los goles de un jugador o el rival no avisa (supuesto 4). El resultado que publica una familia (spec 010) avisa a las demás familias de la categoría y **no** a la que lo publicó (RF-005); el que registra el equipo del club avisa a todas. Frontend: sin pantalla nueva. Pruebas en `backend/pruebas/Integracion/Avisos/AvisosDePartidoPruebas.cs` (ampliar, o `AvisosDeResultadoPruebas.cs` si pasa de 250 líneas): resultado del equipo avisa a toda la categoría, incluidas las no convocadas; el texto lleva el marcador en el orden local o visitante; corregir el marcador avisa y corregir solo un gol de jugador no; resultado de la familia avisa a las demás y no a quien lo publicó; ningún aviso lleva nombres de otros jugadores (CE-004); el contrato de `PUT …/resultado` y `POST …/resultado-familia` no cambia

**Punto de control**: entrenamientos y partidos avisan. La historia 2 se valida sola con el
quickstart (bloque 2).

---

## Fase 6: Historia de usuario 4 - Sin ruido ni avisos de más (Prioridad: P2)

**Objetivo**: varios cambios seguidos sobre una actividad dejan una sola entrada sin leer por
actividad en cada familia, sin subir el contador; lo ya leído no se toca; y no sale nada fuera de la
aplicación.

**Prueba independiente**: cambiar tres veces la hora de un entrenamiento sin que la familia lo lea
y comprobar qué ve; leerlo y cambiar otra vez; revisar que no hubo correo (quickstart, bloque 4).

- [X] T023 [US4] Reemplazo de lo sin leer, de punta a punta (depende de T018, T020 y T021). Backend: en `RepositorioAvisos.GenerarAsync` (T017), para cada cuenta destinataria se borran sus filas de `AvisosDeActividadDestinatario` **sin leer** de la misma actividad (`ActividadTipo` y `ActividadId`) y después se borran los avisos que quedaron sin ningún destinatario, todo en la misma transacción (RF-025, research §8); un aviso ya leído no se toca y el nuevo se suma. El contador no sube porque entra una fila y sale otra. Es un borrado del sistema, no una acción de usuario (§14, enmienda 4.10.0): dejarlo documentado en el XML. Frontend: sin pantalla nueva; la lista se actualiza con el sondeo de 60 segundos de la 011. Pruebas en `backend/pruebas/Integracion/Avisos/ReemplazoDeAvisosPruebas.cs`: tres cambios seguidos de hora dejan un solo aviso sin leer por familia con el texto más reciente y el contador en 1; con un aviso leído y un cambio nuevo, quedan el leído y uno nuevo sin leer (contador 1); dos familias del mismo aviso: una lo lee y la otra recibe el siguiente sin que se pierda el suyo; un aviso de convocatoria sin leer se reemplaza por uno de cambio de lugar (consecuencia aceptada de research §8, dejar la prueba como documentación); un aviso sin destinatarios queda borrado, no huérfano; cambios de otra actividad no reemplazan; cada actividad deja su propia entrada
- [X] T024 [US4] Nada fuera de la aplicación y nada por otros hechos (depende de T018 a T022; solo pruebas y comprobaciones). `backend/pruebas/Integracion/Avisos/SinAvisosExternosPruebas.cs`: con el `IServicioCorreo` sustituido por `CorreoEnMemoria` (como en la 011), ejecutar programar, cambiar, cancelar, reactivar, asistencia, convocar, resultado de equipo y de familia, y comprobar que no se invocó ningún envío (RF-027); `backend/pruebas/Integracion/Avisos/AvisosPorOtrosHechosPruebas.cs`: ingresar a un jugador, retirarlo, que una familia responda a una convocatoria, cambiar de categoría o corregir la ficha no genera ningún aviso (RF-029); un jugador que entra a la categoría después no recibe avisos anteriores y quien cambió de categoría conserva los que ya recibió sin recibir los anteriores de la nueva (RF-011); un jugador retirado deja de acceder y al reincorporarlo vuelve a ver lo que ya le había llegado; con el club suspendido (§7.4) solo entra el PRESIDENTE y las familias ven los avisos al volver a la normalidad; una persona de dos clubes ve los avisos y el contador del club elegido; el DESARROLLADOR no ve avisos ni contadores. Frontend: `frontend/pruebas/SinPermisoDeNotificaciones.test.tsx` comprueba que la aplicación no llama a `Notification.requestPermission` ni registra notificaciones (RF-027)

**Punto de control**: las historias 1 a 4 funcionan juntas.

---

## Fase 7: Historia de usuario 5 - Un solo lápiz para la identidad de la ficha (ajuste del propietario)

**Objetivo**: la tarjeta "Identidad" de la ficha del jugador tiene un solo control de edición, un
lápiz que abre un solo diálogo con nombres, apellidos, fecha, tipo y número de documento. El
documento sigue editable con las reglas de siempre (§10). Solo cambia la interfaz.

**Prueba independiente**: abrir la ficha como la cuenta del jugador y como el PRESIDENTE y revisar
la tarjeta y el diálogo (quickstart, bloque 5). Es independiente del bloque A y puede hacerse antes
o después, incluso en paralelo.

- [X] T025 [P] [US5] Diálogo unificado (sin cambiar el backend). `frontend/src/privado/ficha/DialogoEditarIdentidad.tsx`, que sustituye a `DialogoCorregirIdentidad.tsx` (renombrar con `git mv` para conservar el historial) y absorbe los campos de `DialogoCambiarDocumento.tsx`: Nombres, Apellidos, Fecha de nacimiento (habilitados con `permisos.puedeCorregirIdentidad`) y Tipo de documento y Número de documento (habilitados con `permisos.puedeCambiar`); lo que la persona no puede editar se ve como texto. Al guardar envía **solo lo que cambió**, **primero el documento** (`PUT …/ficha/documento-identidad`) y después la identidad (`PUT …/ficha/identidad`); si el documento falla (duplicado, otra cuenta, §10) el error sale en su campo y no se envía lo demás, de modo que un fallo no deja nada a medias. Mismos mensajes y validaciones de los diálogos actuales; el diálogo atrapa el foco y se cierra con Escape. Archivo de 250 líneas como máximo; si hace falta, los campos van a un componente aparte. Pruebas: `frontend/pruebas/DialogoEditarIdentidad.test.tsx` (solo cambia el nombre → solo se llama a identidad; solo el documento → solo a documento; ambos → primero documento y después identidad; documento duplicado → error en el campo y la identidad no se envía; campos según permiso; solo texto para lo no editable; Escape cierra)
- [X] T026 [US5] Lápiz en la tarjeta "Identidad" (depende de T025). `frontend/src/privado/ficha/SeccionIdentidad.tsx` quita los botones "Cambiar documento" y "Corregir nombre y fecha" y muestra **un solo botón de icono** (lápiz) en la esquina superior derecha de la tarjeta, con nombre accesible "Editar identidad", zona táctil de 44 × 44 px, foco visible y el mismo lenguaje visual que el lápiz de la foto; se muestra si hay `permisos.puedeCambiar` o `permisos.puedeCorregirIdentidad` y, si no, la tarjeta es de solo vista sin control de edición (RF-037). Categoría y equipos siguen en la tarjeta como solo vista y no los toca el lápiz (RF-038, §11). Eliminar `frontend/src/privado/ficha/DialogoCambiarDocumento.tsx` y `DialogoCorregirIdentidad.tsx` (comprobar con `grep` que nadie más los importa) y actualizar `textos.ts` y el CSS de la ficha; a 360 px la tarjeta no desplaza horizontalmente y el lápiz no tapa el título "Identidad". Pruebas: `frontend/pruebas/SeccionIdentidad.test.tsx` (un solo lápiz y ningún botón de texto "Cambiar documento" ni "Corregir nombre y fecha"; el lápiz abre el diálogo; con un solo permiso el lápiz aparece; sin permisos no aparece; categoría y equipos como texto)

**Punto de control**: la ficha tiene un solo lápiz; ninguna prueba del backend cambia.

---

## Fase 8: Cierre y aspectos transversales

**Propósito**: comprobaciones que afectan a todas las historias.

- [X] T027 Contrato y aislamiento: confirmar que `specs/012-avisos-de-actividades/contracts/api.yaml` no tiene operaciones comentadas con marca `# 012-T0NN`, que las 3 operaciones movidas ya no están en el contrato de la 011 y que `AccesoClubPruebas` dice **109 y 85**; revisar contra Swagger que las 4 rutas de `/campana` coinciden con el contrato, que otro club y el DESARROLLADOR reciben 404 y que cada rol no autorizado recibe 403 `rol_no_autorizado`. No se añaden códigos de error nuevos (se reutilizan `no_encontrado` y `rol_no_autorizado`)
- [X] T028 Documentación y tamaño (§2.3, §3): revisar que toda clase nueva tiene su comentario XML y que `UsuarioRol`, `Entrenamiento` y `Partido` quedaron ampliados; ejecutar `scripts/verificar-tamano.ps1` y partir cualquier archivo que pase de 250 líneas (vigilar `ServicioPartidos.cs`, `ListaRecibidos.tsx`, `PantallaComunicados.tsx` y los archivos de pruebas de `Avisos/`)
- [ ] T029 [P] Revisión visual (§24, CE-008): comprobar a 360 px y en escritorio, en tema claro y oscuro y con los colores de al menos dos clubes, la campana, la lista mezclada, la fila de aviso (leído y sin leer, por texto y no solo por color), el mensaje de actividad no disponible y la tarjeta "Identidad" con el lápiz y su diálogo; sin desplazamiento horizontal y con el foco visible. Registrar lo que el propietario deba revisar, porque el lienzo no tiene estas pantallas (nota 1 del plan)
- [ ] T030 Validar el quickstart de punta a punta (`specs/012-avisos-de-actividades/quickstart.md`, bloques 1 a 5 y cierre) en un proyecto de Compose aparte (`docker compose -p lfclub-validacion …`) con `Brevo__Llave` vacía, para no enviar correos reales ni tocar los datos de desarrollo; dejar un contenedor mínimo en marcha durante la validación y borrarlo al final. Anotar en el quickstart lo que difiera
- [X] T031 Batería completa: `dotnet test backend/LFClub.sln` (la integración tarda; lanzarla con `--no-build` y esperar a que termine, y matar cualquier `testhost` colgado antes de volver a compilar), `npm --prefix frontend test`, `tsc` y `eslint` sin errores, y la 001 a la 011 en verde (CE-007, RF-035)

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (Fase 1)**: sin dependencias.
- **Cimientos (Fase 2)**: depende de la Fase 1 y bloquea las historias 1 a 4. **US5 (Fase 7) no
  depende de ella** y puede hacerse en cualquier momento.
- **Historia 3 (Fase 3)**: depende de la Fase 2. Va primero para que la campana ya sirva.
- **Historia 1 (Fase 4) y historia 2 (Fase 5)**: dependen de T017 (generación) y, para verse en la
  pantalla, de la Fase 3. Son independientes entre sí.
- **Historia 4 (Fase 6)**: depende de que al menos entrenamientos y partidos ya generen avisos
  (T018, T020 y T021).
- **Cierre (Fase 8)**: depende de todas las historias que se quieran entregar.

### Entre tareas

- T003 → T004 → T005 → T012 y T013.
- T006, T007, T008, T009, T010 y T011 son independientes entre sí y de T004/T005 (reglas puras y
  tipos).
- T013 → T014 → T015 → T016.
- T017 → T018, T019, T020, T021 y T022 (se pueden repartir entre personas, pero T018/T019
  tocan `ServicioEntrenamientos` y `ServicioAsistencia`, y T020 a T022 tocan `ServicioPartidos`,
  `ServicioConvocatoria` y los dos servicios de resultado: cada grupo de archivos va con una
  persona).
- T023 depende de T018, T020 y T021; T024 de T018 a T022.
- T025 → T026.

### Dentro de cada tarea

Una tarea entrega backend, pantalla y pruebas en el mismo cambio (§27.1). Si una prueba de la 001
a la 011 falla, se detiene y se pregunta antes de seguir (§27.2).

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos, tras T001 y T002:
Tarea: T003 enumeraciones         (luego T004 y T005, en cadena)
Tarea: T006 ReglaCambiosDeActividad
Tarea: T007 ReglaDestinatariosDeAviso
Tarea: T008 RedaccionDeAvisos
Tarea: T009 OrdenDeIdentificadores
Tarea: T010 DTOs del backend
Tarea: T011 tipos del frontend

# Una vez hecha T017:
Persona A: T018 y T019 (entrenamientos y asistencia)
Persona B: T020, T021 y T022 (partidos, convocatoria y resultado)

# En cualquier momento, sin depender del bloque A:
Persona C: T025 y T026 (lápiz de identidad)
```

---

## Estrategia de implementación

### Primero lo mínimo (historia 3)

1. Fase 1 y Fase 2.
2. Fase 3: la campana unificada con avisos sembrados. Parar y validar: contador, lista mezclada,
   leer un aviso, "ya no está disponible" y "Marcar todos".
3. Es el MVP técnico: la 011 sigue igual y la familia ya tiene dónde ver los avisos.

### Entrega incremental

1. + Historia 1: los entrenamientos avisan. Validar con el bloque 1 del quickstart.
2. + Historia 2: los partidos avisan. Validar con el bloque 2.
3. + Historia 4: reemplazo y nada fuera de la aplicación. Validar con el bloque 4.
4. + Historia 5 (puede ir antes o en paralelo): el lápiz único de la ficha.
5. Cierre: contrato 109/85, tamaño, revisión visual, quickstart y batería entera.

### Riesgos conocidos

- **Orden de los identificadores** al mezclar en memoria: usar `OrdenDeIdentificadores` y no
  `Guid.CompareTo` (T009, T014).
- **`ServicioPartidos.cs` cerca de 250 líneas**: sacar lo nuevo a un parcial antes de pasarse (T020).
- **Servicios que hoy no usan transacción** (`CambiarAsync`, `CancelarAsync`, `ReactivarAsync`,
  asistencia y convocatoria): cada uno pasa a `EnTransaccionAsync` con el bloqueo del club y debe
  conservar su respuesta y sus errores actuales (T018 a T021).
- **El reemplazo pierde el aviso viejo aunque cuente algo distinto** (research §8): es lo que pide
  CE-005; si molesta, se revisa en `/speckit-clarify`.

---

## Notas

- [P] = archivos distintos, sin dependencias entre sí.
- [US#] liga cada tarea con su historia para trazabilidad; US5 es el ajuste de la ficha (RF-036 a
  RF-038).
- Hacer commit tras cada tarea o grupo lógico; detenerse en cada punto de control para validar la
  historia por sí sola.
- Evitar: tareas vagas, conflictos en un mismo archivo, dependencias entre historias que rompan su
  independencia.
