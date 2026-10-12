# Plan de implementación: Entrenamientos, partidos, convocatorias y estadísticas deportivas

**Rama**: `009-entrenamientos-y-partidos` | **Fecha**: 2026-10-11 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/009-entrenamientos-y-partidos/spec.md`

## Resumen

Quien entrena una categoría (un ENTRENADOR, o un PRESIDENTE o DIRECTIVO asignado a ella) y el
PRESIDENTE programan entrenamientos y crean partidos para una o varias categorías. Los
entrenamientos los ven todos los jugadores de esas categorías; un partido que no se ha jugado lo
ven solo sus convocados, quienes entrenan alguna de sus categorías y los PRESIDENTES, y para
cualquier otra cuenta no existe. Después del partido se registra el marcador, quiénes jugaron y los
goles, y el resultado pasa a ser visible para todo el club. Quien entrena registra la asistencia y
la familia responde si su jugador asistirá. La ficha del jugador muestra sus estadísticas y una
pantalla nueva muestra los goleadores por categoría. "Próximos entrenamientos" del inicio deja de
ser un espacio reservado.

Enfoque técnico: **siete tablas nuevas y una migración** (`EntrenamientosYPartidos`), todas bajo el
filtro de club de la 001. La visibilidad de §17.2 y la gestión por asignación son **reglas puras de
dominio**, traducidas una vez a SQL para las listas y aplicadas en memoria a la consulta por
identificador, siempre con el mismo 404. Fecha y horas se guardan en hora de Colombia, con desfase
fijo. Las estadísticas se calculan al consultar, por la categoría copiada en cada registro (§13).
22 endpoints nuevos, ninguna respuesta existente cambia. En la pantalla: calendario semanal,
formulario de actividad según el lienzo, detalle con planillas, resultado, goleadores y la sección
de estadísticas en la ficha. Las decisiones y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Una migración con siete tablas, sus índices y sus restricciones
de comprobación (data-model.md). `date` y `time` para las actividades.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers, con el
`IReloj` sustituido para mover el "ahora"); Vitest en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: cada lista es una consulta con la visibilidad en el `WHERE`, más una
por las asignaciones de quien pregunta; las planillas y los goleadores resuelven los enlaces de la
008 con una consulta por lista. Las estadísticas son un `GROUP BY` por jugador sobre índices.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda visibilidad y todo permiso en el servidor (§15, RF-032); backend y
pantalla de cada historia en la misma tarea (§27.1); una cuenta de jugador nunca recibe convocados,
quiénes jugaron, ni respuestas o asistencias de otros (RF-012, RF-024).

**Escala y alcance**: decenas de jugadores, entrenamientos y partidos por club (§19). 7 entidades y
5 enumeraciones nuevas; 22 operaciones de API (el contrato pasa de 72 a 94; las de club, de 48 a
70); unos 25 archivos de frontend nuevos y unos 6 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.6.0**, que ya incorpora la decisión de esta funcionalidad sobre el
PRESIDENTE o DIRECTIVO asignado (§8 "Entrenador" y §17.2). La spec se escribió con la 4.5.1 y ya
cita la 4.6.0; el impacto que pedía está aplicado.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `Entrenamiento`, `Partido`, `PartidoJugador`, `AsistenciaEntrenamiento`, `RespuestaFamilia`, `ServicioConvocatoria`, `Calendario.tsx`; "convocatoria" en el sentido de §2.2; sin Acudiente ni Profesor | Cumple |
| §2.3 Máximo 250 líneas | Servicios divididos por responsabilidad desde el principio; el menú sale de `DisposicionClub.tsx` (216) a `MenuDelClub.tsx`; tipos nuevos en archivos propios (research §14) | Cumple |
| §3 Documentación XML | En cada clase nueva; se reescribe la de `Categoria` (`Usada`), `UsuarioRol` (lo que borra el retiro), `AsignacionEntrenadorCategoria` (gestión deportiva), `ServicioRetiroJugador` y `RepositorioRetiroEntreClubes` | Cumple |
| §4 y §5 Capas | Controladores que solo delegan; reglas de visibilidad, gestión, transiciones y resultado en `Dominio/Reglas`, puras; repositorios que traducen la visibilidad a SQL sin decidirla | Cumple |
| §6 y §19 Tecnologías y simplicidad | Ninguna tecnología nueva. Siete tablas, cada una con su necesidad (research §1); sin tabla de estadísticas ni historial; desfase fijo en lugar de base de zonas horarias | Cumple |
| §7.1 Aislamiento entre clubes | Todas las entidades `IPerteneceAClub`; rutas bajo `/api/clubes/{clubId}`. La única escritura entre clubes es la del retiro por hermano, que ya existe, y solo amplía los efectos de ese mismo retiro (research §10) | Cumple |
| §7.5 Aislamiento dentro del club | Asistencia y convocatoria limitadas a las categorías que se entrenan; la cuenta de jugador ve solo lo de su jugador elegido; estadísticas por `AccesoAFicha` | Cumple |
| §8 Roles | PRESIDENTE en todo el club; ENTRENADOR, y PRESIDENTE o DIRECTIVO asignados, en sus categorías; DIRECTIVO sin asignar, solo consulta de entrenamientos y partidos jugados; JUGADOR confirma asistencia; DESARROLLADOR, 404 | Cumple |
| §9 Sitio público | Ninguna ruta pública nueva; publicar resultados y goleadores queda fuera de alcance | Cumple |
| §11.1 Actividades conjuntas | Una o varias categorías por actividad, sin mover a nadie de categoría | Cumple |
| §11.2 y §13 Histórico | `PartidoJugador.CategoriaId` y `AsistenciaEntrenamiento.CategoriaId` copiadas y fijas; el autor del resultado copiado por nombre | Cumple |
| §14 y §14.1 Desactivables y retiro | Actividades que se cancelan, se pueden reactivar y nunca se borran; categorías con actividades que no se borran; el retirado sale de convocatorias pendientes y conserva su historial | Cumple |
| §15 Reglas en backend | Visibilidad, gestión, momentos y validaciones en la API; la pantalla solo oculta por comodidad | Cumple |
| §17 Datos deportivos | Estadísticas calculadas; corregir corrige; el sistema solo toca una convocatoria por la regla explícita del retiro (RF-033) | Cumple |
| §17.2 Convocatorias | Tabla de visibilidad de research §4, con la misma respuesta 404 para "no existe" y "no convocado"; ningún contador ni lista lo deja pasar | Cumple |
| §18 Estados | Solo estado actual; transiciones en `ReglaTransicionPartido` | Cumple |
| §20 Pruebas | Las de §20 que tocan esta funcionalidad: no convocado no ve el partido y sí los entrenamientos; DIRECTIVO asignado actúa como entrenador solo en lo suyo; cálculo de estadísticas; transiciones; aislamiento | Cumple |
| §22 Migraciones | Dos migraciones controladas: la de las siete tablas y `RivalOpcional`, del ajuste del propietario | Cumple |
| §23 API | DTOs propios; partes privadas omitidas, no vacías; `problem+json` con códigos estables; Swagger | Cumple |
| §24 Diseño visual | Estructura de `Convocatoria` y `M-Calendario` del lienzo; teléfono y escritorio, dos temas, estados con texto | Cumple, con la nota 1 |
| §25 Decisiones no tomadas | Los supuestos de research §16 se presentan al propietario antes de `/speckit-tasks` | Cumple, con la nota 2 |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |

**Resultado de la puerta**: pasa.

**Nota 1**: del lienzo se quitan los elementos de funcionalidades fuera de alcance (costo del
arbitraje, mensaje para los convocados, "Guardar borrador") y los botones de respuesta usan el texto
de la spec ("Asistirá", "No asistirá"). Research §13.

**Nota 2**: cuatro supuestos de diseño (categorías fijas, partido jugado sin cambiar sus datos,
fechas pasadas permitidas, asistencia desde el día) no los decide la spec. Son restricciones de
alcance, no reglas nuevas, y cada una cambia en una regla sin tocar el modelo. El quinto, si cancelar
era definitivo, lo resolvió el propietario: se puede reactivar (RF-038, research §7).

**Revisión tras el diseño**: el modelo añade siete tablas justificadas y ninguna tecnología ni
patrón nuevo. Los 22 endpoints quedan bajo `/api/clubes/{clubId}` y entran en las pruebas de
aislamiento existentes, que se descubren solas desde el contrato. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/009-entrenamientos-y-partidos/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Siete tablas, estados, vistas calculadas y visibilidad
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Veintidós endpoints nuevos
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LaPecosa.Dominio/
│   ├── Entidades/         + Entrenamiento, EntrenamientoCategoria, AsistenciaEntrenamiento,
│   │                        RespuestaEntrenamiento, Partido, PartidoCategoria, PartidoJugador
│   │                      ~ Categoria, UsuarioRol, AsignacionEntrenadorCategoria (documentación)
│   ├── Enumeraciones/     + EstadoEntrenamiento, EstadoPartido, CondicionPartido, TipoPartido,
│   │                        RespuestaFamilia
│   └── Reglas/            + ReglaGestionDeActividad, ReglaAlcanceDeAsistencia,
│                            ReglaVisibilidadDeEntrenamiento, ReglaVisibilidadDePartido,
│                            ReglaTransicionPartido, ReglaResultadoDePartido,
│                            ReglaMomentoDeActividad, CalculoDeAsistencia
├── LaPecosa.Aplicacion/
│   ├── DTOs/              + GuardarEntrenamientoDto, EntrenamientoResumenDto, EntrenamientoDto,
│   │                        PlanillaEntrenamientoDto, RegistrarAsistenciaDto, GuardarPartidoDto,
│   │                        PartidoResumenDto, PartidoDto, PlanillaPartidoDto,
│   │                        GuardarConvocatoriaDto, RegistrarResultadoDto, ResponderDto,
│   │                        CalendarioDto, CategoriaDeActividadDto, PermisosDeActividadDto,
│   │                        GoleadorDePartidoDto, GoleadoresDto, CifrasDto, EstadisticasJugadorDto
│   ├── Interfaces/        + IRepositorioEntrenamientos, IRepositorioPartidos,
│   │                        IRepositorioAsistencias, IRepositorioEstadisticas,
│   │                        IRepositorioConvocatoria
│   │                      ~ IRepositorioJugadores (borrar convocatorias y respuestas pendientes)
│   ├── Servicios/         + IServicioEntrenamientos, IServicioConsultaEntrenamientos,
│   │                        IServicioAsistencia, IServicioPartidos, IServicioConsultaPartidos,
│   │                        IServicioConvocatoria, IServicioResultado, IServicioRespuestas,
│   │                        IServicioCalendario, IServicioEstadisticas
│   ├── Implementaciones/  + uno por interfaz de Servicios
│   │                      ~ ServicioRetiroJugador (RF-033)
│   ├── Mappers/           + MapperEntrenamientos, MapperPartidos, MapperEstadisticas
│   ├── Utilidades/        + HoraDeColombia, AccesoAActividad (lee, aplica la visibilidad y da
│   │                        los permisos, como AccesoAFicha), ErroresDeActividades
│   └── Validadores/       + ValidadorEntrenamiento, ValidadorPartido, ValidadorRangoCalendario
├── LaPecosa.Infraestructura/
│   ├── Datos/             ~ ContextoLaPecosa (siete DbSet)
│   │   ├── Configuraciones/ + una por entidad nueva
│   │   └── Migraciones/   + EntrenamientosYPartidos
│   └── Repositorios/      + RepositorioEntrenamientos, RepositorioPartidos,
│                            RepositorioAsistencias, RepositorioEstadisticas,
│                            RepositorioConvocatoria (planilla, convocables y cambios de la
│                            convocatoria, separados de RepositorioPartidos por tamaño, §2.3)
│                          ~ RepositorioJugadores, Plataforma/RepositorioRetiroEntreClubes (RF-033)
└── LaPecosa.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/Club/ + ControladorCalendario, ControladorEntrenamientos,
                             ControladorAsistencia, ControladorPartidos, ControladorConvocatoria,
                             ControladorResultado, ControladorEstadisticas

backend/pruebas/
├── Unitarias/Reglas/      + una clase de pruebas por regla nueva, y HoraDeColombiaPruebas
└── Integracion/
    ├── Base/              + SembradorActividades, RelojDePrueba
    ├── Entrenamientos/    + ProgramarEntrenamientoPruebas, VisibilidadEntrenamientosPruebas,
    │                        CancelacionEntrenamientoPruebas, AsistenciaPruebas, RespuestasPruebas,
    │                        ProximosEntrenamientosPruebas
    ├── Partidos/          + CrearPartidoPruebas, VisibilidadPartidoPruebas (las diez cuentas),
    │                        ConvocatoriaPruebas, ResultadoPruebas, DirectivoAsignadoPruebas,
    │                        RetiroYConvocatoriaPruebas, CalendarioPruebas
    ├── Estadisticas/      + EstadisticasJugadorPruebas, GoleadoresPruebas
    └── Aislamiento/       ~ AccesoClubPruebas (70 de club; contrato de 94)

frontend/src/
├── App.tsx                ~ rutas nuevas
├── compartido/
│   ├── api/               + tiposActividades.ts, tiposEstadisticas.ts
│   ├── componentes/       ~ Iconos.tsx (calendario, partido, goleadores)
│   └── fechas.ts          + fecha y hora en español, semanas
├── privado/
│   ├── DisposicionClub.tsx ~ usa MenuDelClub
│   ├── MenuDelClub.tsx    + el menú, con Calendario, Partidos y Goleadores
│   ├── inicio/            ~ TarjetaProximosEntrenamientos.tsx (datos reales)
│   ├── calendario/        + Calendario.tsx, TiraDeSemana.tsx, TarjetaActividad.tsx,
│   │                        BotonesRespuesta.tsx
│   ├── actividades/       + FormularioActividad.tsx (tipo, categorías, campos),
│   │                        SelectorCategorias.tsx, EtiquetaActividad.tsx, useCategoriasGestionables.ts
│   ├── entrenamientos/    + DetalleEntrenamiento.tsx, PlanillaAsistencia.tsx, ResumenRespuestas.tsx
│   ├── partidos/          + Partidos.tsx, DetallePartido.tsx, EditorConvocatoria.tsx,
│   │                        FormularioResultado.tsx, Goleadores.tsx
│   └── ficha/             ~ FichaJugador.tsx; + SeccionEstadisticas.tsx
└── (frontend/pruebas/)    + fechas.test.ts, EtiquetaActividad.test.tsx
```

**Decisión de estructura**: la de la 001, con carpetas nuevas `privado/calendario/`,
`privado/actividades/`, `privado/entrenamientos/`, `privado/partidos/` y, en las pruebas de
integración, `Entrenamientos/`, `Partidos/` y `Estadisticas/`.

### Pantallas

| Ruta | Qué muestra | Quién |
| --- | --- | --- |
| `/club/:clubId` (inicio) | "Próximos entrenamientos" con datos reales o el mensaje de que no hay | Todos los roles del club |
| `/club/:clubId/calendario` | **Nueva**: semana con entrenamientos y partidos visibles; respuesta de la familia; "Nuevo" para quien gestiona | Todos |
| `/club/:clubId/actividades/nueva` | **Nueva**: formulario de entrenamiento o partido, según el lienzo | PRESIDENTE y quien entrena |
| `/club/:clubId/entrenamientos/:id` | **Nueva**: detalle, cambiar, cancelar, reactivar, respuestas y planilla de asistencia | Quien lo ve; planilla solo para quien entrena alguna de sus categorías y PRESIDENTE |
| `/club/:clubId/partidos` | **Nueva**: próximos visibles y jugados | Todos |
| `/club/:clubId/partidos/:id` | **Nueva**: detalle, cancelar, reactivar, convocatoria, respuestas, resultado y goleadores | Quien lo ve; convocatoria y resultado según permisos |
| `/club/:clubId/goleadores` | **Nueva**: goleadores por categoría | Todos |
| `/club/:clubId/jugadores/:id/ficha` | Nueva sección "Estadísticas" | Quien ve la ficha |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Entrenamientos | RF-001 a 006, 031, 032, 035, 038 | `ControladorEntrenamientos`, `ServicioEntrenamientos`, `ServicioConsultaEntrenamientos`, `ReglaGestionDeActividad`, `ReglaVisibilidadDeEntrenamiento`, `ValidadorEntrenamiento` | `FormularioActividad`, `DetalleEntrenamiento`, `Calendario`, `TarjetaProximosEntrenamientos` | Cada rol ve lo suyo; E1 no programa la 2016; P sí; cancelado visible con texto; reactivar antes de empezar sí, después 409; otro club 404; inicio vacío con mensaje |
| 2. Convocatoria | RF-007 a 014, 034, 038 | `ControladorPartidos`, `ControladorConvocatoria`, `ServicioPartidos`, `ServicioConvocatoria`, `ReglaVisibilidadDePartido`, `ValidadorPartido` | `FormularioActividad`, `EditorConvocatoria`, `DetallePartido`, `Partidos` | Las diez cuentas: calendario, listas, inicio e identificador; hermanos; DIRECTIVO asignado y no asignado; quitar sin rastro; torneo sin nombre; reactivado con la misma convocatoria y sin aparecer a los no convocados |
| 3. Resultado | RF-015 a 021 | `ControladorResultado`, `ServicioResultado`, `ReglaResultadoDePartido`, `ReglaTransicionPartido` | `FormularioResultado`, `DetallePartido` | Antes de la hora, 409; rechazos de goles; jugado visible a todos sin convocados para jugadores; último cambio; categoría copiada tras cambio de categoría; no se cancela |
| 4. Asistencia | RF-022, 025, 027 | `ControladorAsistencia`, `ServicioAsistencia`, `ReglaAlcanceDeAsistencia` | `PlanillaAsistencia` | Solo categorías propias en conjunto; futuro y cancelado, 409; jugador ve solo la suya |
| 5. Respuestas | RF-023 a 025 | `ServicioRespuestas`, endpoints `mi-respuesta` | `BotonesRespuesta`, `ResumenRespuestas` | Hasta la hora de inicio; conteos y nombres para quien gestiona; nadie ve las de otros; independientes de la asistencia |
| 6. Estadísticas | RF-026 a 030 | `ControladorEstadisticas`, `ServicioEstadisticas`, `RepositorioEstadisticas`, `CalculoDeAsistencia` | `SeccionEstadisticas`, `Goleadores` | Coinciden con el conteo antes y después de corregir; cero sin registros; retirados en goleadores; ficha ajena 404 |
| Transversal | RF-031 a 033, 036, 037 | Retiro (dos caminos), aislamiento | Todas, a 360 px y en los dos temas | Contrato de 94; 70 de club con 404 a otro club y al DESARROLLADOR; retiro saca de convocatorias pendientes; la 001 a la 008 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: contrato comentado hasta que exista cada endpoint (como en la 008, para que
   `AccesoClubPruebas` siga en verde); enumeraciones, entidades, configuraciones y migración;
   `HoraDeColombia`, `RelojDePrueba`, `SembradorActividades`; las reglas puras con sus pruebas;
   `MenuDelClub` y `fechas.ts`.
2. **Historia 1**: programar, ver, cambiar, cancelar y reactivar entrenamientos; calendario (solo
   entrenamientos) y la tarjeta del inicio.
3. **Historia 2**: partidos y convocatoria; el calendario suma los partidos visibles; pantalla de
   partidos.
4. **Historia 3**: resultado y goles; partidos jugados visibles para todos.
5. **Historia 4**: planilla y asistencia.
6. **Historia 5**: respuestas de la familia en el calendario y en los detalles; resumen para quien
   gestiona.
7. **Historia 6**: estadísticas en la ficha y pantalla de goleadores.
8. **Retiro (RF-033)** en los dos caminos, con sus pruebas.
9. **Cierre**: contrato sin marcas contra Swagger (94), aislamiento (70), revisión a 360 px en los
   dos temas, recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y
   batería entera.

## Seguimiento de complejidad

| Desviación | Por qué se necesita | Alternativa más simple descartada |
| --- | --- | --- |
| §12.3 dice "JUGADOR: mediante `Jugador.UsuarioId`", pero no hay entidad `Jugador`: el jugador es el `UsuarioRol` con rol JUGADOR (heredada de la 003 a la 008). `PartidoJugador.UsuarioRolId` apunta a él | Las filas deportivas cuelgan del integrante como la ficha y la foto | Crear una entidad `Jugador`: una tabla y una migración de datos sin necesidad (§19) |
| La visibilidad se escribe dos veces: como regla pura y como `Where` en las listas | Las listas no deben traer ni contar lo que no se ve (CE-001); la regla pura es la que se lee y se prueba | Filtrar en memoria después de traer todo: cualquier lista o contador nuevo podría olvidarlo. Una prueba de integración compara las dos por rol |
