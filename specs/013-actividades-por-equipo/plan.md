# Plan de implementación: entrenamientos y partidos dirigidos a equipos y a jugadores añadidos

**Rama**: `013-actividades-por-equipo` | **Fecha**: 2026-10-12 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación en `specs/013-actividades-por-equipo/spec.md`, con la constitución
**4.11.0** ya enmendada (§7.5, §8, §11.1, §11.3, §14.4, §17.2 y §20).

## Resumen

Hoy un entrenamiento se dirige a categorías completas y un partido a una convocatoria de jugadores
de esas categorías. Esta funcionalidad permite que un entrenamiento se dirija además a **uno o varios
equipos** de sus categorías (sin equipos, sigue siendo de toda la categoría), que la convocatoria se
arme **por equipo** con "marcar a todos", y que quien dirige **sume a jugadores** de otros equipos o
de otras categorías, que ven solo esa actividad sin cambiar de categoría ni de equipo. Cambiar la
audiencia avisa a quien pasa a verla y no deja rastro en quien sale.

Enfoque técnico: **dos tablas nuevas** (`EntrenamientosEquipo` y `EntrenamientosJugador`) y una
migración; los partidos no necesitan tablas (un añadido es un convocado de otra categoría). La
visibilidad de un entrenamiento pasa a una regla pura con tres hechos más, traducida a SQL en el
repositorio. Dos operaciones nuevas (cambiar la audiencia y buscar jugadores sumables) y cambios
aditivos en las de la 009. Al desactivar un equipo, el sistema congela su audiencia en los
entrenamientos próximos (RF-026). Las decisiones están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001. **No se añade ninguna.**

**Almacenamiento**: PostgreSQL 17. Una migración, `ActividadesPorEquipo`, con dos tablas, dos
índices y una restricción de comprobación ([data-model.md](data-model.md)). Sin datos que migrar:
un entrenamiento sin filas de equipo es de toda la categoría.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers, con el
`IReloj` y el servicio de correo sustituidos); Vitest en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable,
también empaquetada con Capacitor para Android.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: la visibilidad es un `EXISTS` sobre tablas con índice por equipo y por
jugador; la audiencia de un aviso se resuelve en una sola consulta del club (decenas de jugadores).
Buscar jugadores es una consulta acotada a 20 filas.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3) —`ServicioEntrenamientos` (228) y `ServicioAsistencia` (190) están cerca
y se descargan en clases nuevas, research §12—; toda regla en el servidor (§15, RF-023); backend y
pantalla de cada historia en la misma tarea (§27.1); nunca se revela a quien no ve una actividad
que existe (RF-024).

**Escala y alcance**: 2 entidades y 1 enumeración nuevas; el contrato pasa de 109 a 111
operaciones y las de club, de 85 a 87; unos 8 archivos de frontend nuevos y unos 10 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.11.0**, que ya recoge toda la funcionalidad.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `EntrenamientoEquipo`, `EntrenamientoJugador`, `ReglaAudienciaDeEntrenamiento`, `ReglaDireccionDeActividad`, `ServicioAudienciaDeEntrenamiento`; "equipo", "categoría", "jugador" | Cumple |
| §2.3 Máximo 250 líneas | Lo nuevo vive en clases propias; los servicios existentes solo suman llamadas; `verificar-tamano` al empezar | Cumple, vigilar `ServicioEntrenamientos` |
| §3 Documentación XML | En cada clase nueva; se amplía la de las que cambian | Cumple |
| §4 y §5 Capas | Reglas puras en `Dominio/Reglas`; servicios en `Aplicacion`; SQL en `Infraestructura`; controladores delegan | Cumple |
| §6 y §19 Tecnologías y simplicidad | Sin dependencias; dos tablas con necesidad clara; sin tabla para partidos | Cumple |
| §7.1 Aislamiento entre clubes | Las dos entidades son `IPerteneceAClub`; las dos rutas nuevas entran solas en `AccesoClubPruebas` | Cumple |
| §7.5 Aislamiento dentro del club | El añadido de otra categoría ve solo esa actividad; el buscador devuelve lo mínimo y no da acceso a la ficha | Cumple, con la nota 2 |
| §8 Roles | Dirige PRESIDENTE o quien entrena alguna categoría; JUGADOR 403 en lo nuevo; DESARROLLADOR 404 | Cumple |
| §11.1 y §11.3 | Equipos y añadidos sin cambiar categoría ni equipos; el entrenador elige cualquier equipo de sus categorías | Cumple |
| §13 y §14 | La asistencia copia la categoría del jugador; las filas de audiencia se borran solo por acción de la actividad; nada se elimina del histórico | Cumple |
| §14.4 Avisos | Llegan a la audiencia y a los añadidos; el que sale no recibe nada | Cumple |
| §15 Reglas en backend | Visibilidad, dirección, validación y congelado en el servidor | Cumple |
| §17.2 Visibilidad | Equipos y añadidos como dice el texto; DIRECTIVO sin asignar no ve los de equipos (nota 1) | Cumple, con la nota 1 |
| §18 Estados | Sin estados nuevos | Cumple |
| §20 Pruebas | Visibilidad por equipo y por añadido; categoría y equipos intactos; sin aviso a quien sale | Cumple |
| §22 Migraciones | Una migración controlada | Cumple |
| §23 API | DTOs propios; `problem+json`; un código nuevo (`equipo_no_valido`) | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas, pertenencia en texto | Cumple, con la nota 3 |
| §25 y §28 Decisiones | No se resuelve ninguna decisión pendiente | Cumple |
| §27 Definición de terminado | Tareas por historia con backend, pantalla y pruebas | Cumple |

**Resultado de la puerta**: pasa. Sin violaciones; no hay nada que justificar en el seguimiento de
complejidad.

**Nota 1 (para el propietario)**: hoy un DIRECTIVO sin asignar ve todos los entrenamientos; la §17.2
dice que uno dirigido a equipos solo lo ven sus jugadores, los añadidos, quienes entrenan alguna de
sus categorías y el PRESIDENTE. Se aplica literal: sigue viendo los de toda la categoría y no ve los
de equipos ([research.md](research.md) §3).

**Nota 2 (para el propietario)**: "quien dirige" es quien entrena **alguna** categoría, no todas. En
un entrenamiento de dos categorías, quien entrena solo una puede cambiar equipos y añadidos aunque no
pueda cambiar la hora ([research.md](research.md) §4). Y para sumar hace falta poder elegir: el
buscador muestra nombres y categoría de jugadores de otras categorías, acotado y sin ficha
([research.md](research.md) §9). Ambas son decisiones técnicas dentro de lo que la spec ya permite.

**Nota 4 (para el propietario)**: si un entrenamiento junta la 2014 y la 2015 y se marca solo el
equipo A de la 2014, la audiencia es solo ese equipo; la 2015 no entra completa, porque RF-003 y el
escenario 1.7 dicen "la unión de lo elegido". Para incluirla hay que sumar a sus jugadores o
programar otro entrenamiento. Si se prefiere que una categoría sin equipos elegidos entre completa,
es una decisión de negocio que cambia RF-003 y no se toma aquí.

**Nota 3**: el lienzo no tiene equipos en el formulario, el grupo por equipo de la convocatoria ni el
buscador. Su diseño sigue [research.md](research.md) §13 con los tokens y componentes del proyecto, y
lo revisa el propietario en la validación.

**Revisión tras el diseño**: dos tablas justificadas por la audiencia calculada y la lista guardada;
ninguna dependencia nueva; dos rutas nuevas y todas las demás aditivas. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/013-actividades-por-equipo/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas, hallazgos del código y notas al propietario
├── data-model.md        # Tablas nuevas, cálculos de audiencia y DTOs
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Dos operaciones nuevas (comentadas hasta construirlas) y siete que cambian
├── checklists/
│   └── requirements.md  # De /speckit-specify
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/         + EntrenamientoEquipo, EntrenamientoJugador
│   │                      ~ Entrenamiento (listas Equipos y Jugadores), Equipo (documentación)
│   ├── Enumeraciones/     + OrigenDeJugadorEnEntrenamiento
│   └── Reglas/            + ReglaAudienciaDeEntrenamiento, ReglaDireccionDeActividad
│                          ~ ReglaVisibilidadDeEntrenamiento, ReglaDestinatariosDeAviso
├── LFClub.Aplicacion/
│   ├── DTOs/              + AudienciaDeEntrenamientoDto, JugadorSumableDto, EquipoDeActividadDto
│   │                      ~ GuardarEntrenamientoDto, EntrenamientoDto, EntrenamientoResumenDto,
│   │                        PermisosDeActividadDto, PlanillaEntrenamientoDto, PlanillaPartidoDto
│   ├── Interfaces/        + IRepositorioAudiencia
│   │                      ~ IRepositorioEntrenamientos, IRepositorioEquipos, IRepositorioAsistencias,
│   │                        IRepositorioConvocatoria, IRepositorioAvisos, QuienPregunta
│   ├── Servicios/         + IServicioAudienciaDeEntrenamiento, IServicioJugadoresSumables
│   ├── Implementaciones/  + ServicioAudienciaDeEntrenamiento, ServicioJugadoresSumables
│   │                      ~ ServicioEntrenamientos, ServicioAsistencia, ServicioRespuestas,
│   │                        ServicioConvocatoria, ServicioEquipos, ServicioAvisosDeActividad,
│   │                        ServicioConsultaEntrenamientos
│   ├── Mappers/           ~ MapperEntrenamientos, MapperPartidos, MapperAsistencia
│   ├── Utilidades/        + ArmadoDeAudiencia
│   │                      ~ AccesoAActividad, ErroresDeActividades (equipo_no_valido)
│   └── Validadores/       ~ ValidadorEntrenamiento
├── LFClub.Infraestructura/
│   ├── Datos/             ~ ContextoLFClub (dos DbSet)
│   │   ├── Configuraciones/ + ConfiguracionEntrenamientoEquipo, ConfiguracionEntrenamientoJugador
│   │   └── Migraciones/   + ActividadesPorEquipo
│   └── Repositorios/      + RepositorioAudiencia
│                          ~ RepositorioEntrenamientos, RepositorioEquipos, RepositorioAsistencias,
│                            RepositorioConvocatoria, RepositorioAvisos
└── LFClub.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/Club/ + ControladorJugadoresSumables
                           ~ ControladorEntrenamientos (audiencia)

backend/pruebas/
├── Unitarias/
│   └── Reglas/            + ReglaAudienciaDeEntrenamientoPruebas, ReglaDireccionDeActividadPruebas
│                          ~ ReglaVisibilidadDeEntrenamientoPruebas, ReglaDestinatariosDeAvisoPruebas
└── Integracion/
    ├── Entrenamientos/    + EntrenamientoPorEquipoPruebas, AudienciaDeEntrenamientoPruebas,
    │                        EquipoDesactivadoPruebas, VisibilidadPorRolPruebas
    ├── Partidos/          + ConvocatoriaPorEquipoPruebas, JugadorAnadidoAPartidoPruebas
    ├── Avisos/            + AvisosDeAudienciaPruebas
    └── Aislamiento/       ~ AccesoClubPruebas (contrato de 111; 87 de club)

frontend/src/
├── compartido/api/        ~ tiposActividades.ts
├── privado/
│   ├── actividades/       + SelectorEquipos.tsx, BuscadorDeJugadores.tsx, EtiquetaDePertenencia.tsx
│   │                      ~ FormularioEntrenamiento.tsx, useCategoriasGestionables.ts
│   ├── entrenamientos/    + SeccionAudiencia.tsx, DialogoAudiencia.tsx
│   │                      ~ DetalleEntrenamiento.tsx, PlanillaAsistencia.tsx
│   ├── partidos/          + GrupoDeEquipoConvocatoria.tsx
│   │                      ~ EditorConvocatoria.tsx, SeccionConvocatoria.tsx
│   └── calendario/        ~ TarjetaActividad.tsx
└── (frontend/pruebas/)    + SelectorEquipos.test.tsx, EditorConvocatoriaPorEquipo.test.tsx,
                             BuscadorDeJugadores.test.tsx
```

**Decisión de estructura**: la de la 001; sin carpetas nuevas en backend, salvo `Audiencia` dentro de
las pruebas de integración si crece.

### Pantallas

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| `/club/:clubId/entrenamientos/nuevo` y editar | "Equipos" bajo las categorías, solo si alguna tiene equipos | Quien gestiona |
| `/club/:clubId/entrenamientos/:id` | "Para: 2014 · Equipo A"; "Jugadores añadidos" y "Cambiar audiencia" | Todos / quien dirige |
| `/club/:clubId/partidos/:id` | Convocatoria agrupada por equipo, "Marcar a todos" y buscador | Quien gestiona |
| Calendario y próximos | La tarjeta dice el equipo cuando lo hay | Quien ve la actividad |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Entrenamiento por equipo | RF-001 a 008, 023 a 026 | `ReglaVisibilidadDeEntrenamiento`, `ReglaAudienciaDeEntrenamiento`, `ServicioEntrenamientos`, `ServicioRespuestas`, `RepositorioEntrenamientos`, `RepositorioEquipos` | `SelectorEquipos`, `FormularioEntrenamiento`, `DetalleEntrenamiento` | A, B, A y B y ninguno; jugador sin equipo; equipo en 2 categorías; varios días; DIRECTIVO sin asignar; equipo desactivado; calendario, listas, contadores, campana y 404 por id |
| 2. Convocar por equipo | RF-009 a 012 | `ServicioConvocatoria`, `MapperPartidos`, `RepositorioConvocatoria` | `EditorConvocatoria`, `GrupoDeEquipoConvocatoria` | Agrupación, jugador en dos equipos una vez, "todos" y desmarcar, visibilidad intacta |
| 3. Sumar jugadores | RF-013 a 019 | `ServicioAudienciaDeEntrenamiento`, `ServicioJugadoresSumables`, `ReglaDireccionDeActividad`, `ServicioAvisosDeActividad` | `BuscadorDeJugadores`, `DialogoAudiencia` | Ve solo esa actividad; categoría y equipos intactos; quien no dirige 403; quitar sin aviso; goles cuentan; sin lista a la familia |
| 4. Ver y corregir | RF-020 a 022 | `ServicioAudienciaDeEntrenamiento`, `RepositorioAudiencia`, `ReglaDestinatariosDeAviso` | `SeccionAudiencia` | Cambiar A por B; aviso solo a quien entra; sin duplicados; detalle para quien dirige |
| Transversal | RF-023 a 026, CE-001 a 007 | Aislamiento, migración | Todas, a 360 px y en los dos temas | Contrato de 111; 87 de club con 404 a otro club y al DESARROLLADOR; la 001 a la 012 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: medir el tamaño de los archivos que se tocan (`scripts/verificar-tamano.ps1`); contar
   operaciones del contrato con la expresión de `EndpointsDeLaApi` (partida real 109/85); la
   enumeración, las entidades, las configuraciones y la migración; reglas puras con sus pruebas
   unitarias.
2. **Historia 1**: crear con equipos, visibilidad en SQL, responder y planilla, y la
   congelación al desactivar un equipo (RF-026). Pantalla: selector de equipos y detalle.
3. **Historia 2**: planilla de partido con equipos, agrupación y "marcar a todos".
4. **Historia 3**: relajar la convocatoria; `PUT .../audiencia` y el buscador (descomentar sus
   rutas del contrato); pantallas de añadidos.
5. **Historia 4**: avisos al cambiar la audiencia y sin rastro al salir; detalle para quien dirige.
6. **Cierre**: contrato sin marcas contra Swagger (111/87), aislamiento, revisión a 360 px en los
   dos temas, recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y
   batería entera (la 001 a la 012 en verde).

## Seguimiento de complejidad

Sin desviaciones: no hay dependencias nuevas ni patrones nuevos. Lo único que se aparta de lo más
corto es copiar la audiencia al desactivar un equipo (research §6): es lo que hace falta para cumplir
RF-026 sin cambiar la 003.

## Notas de la implementación

Lo que se decidió al construir, dentro de lo que la spec ya permite y sin cambiar ninguna regla:

1. **Códigos de error**: la validación de campos del proyecto responde `400 datos_invalidos` (como en
   la 009), no `422 validacion`; `jugador_fuera_de_la_actividad` sigue siendo `409`, como lo usan las
   specs anteriores. Solo `equipo_no_valido` es `422`. El contrato se alineó con lo construido.
2. **Añadido que ya es de la audiencia natural**: se acepta y no se guarda fila, para no contarlo
   dos veces ni mostrarlo como "añadido". Si después cambia la audiencia y deja de ser natural, hay
   que sumarlo otra vez.
3. **Planilla de asistencia**: con equipos trae a los jugadores de esos equipos y a los congelados
   que estén en una categoría marcable; los añadidos, de cualquier categoría, solo para quien dirige.
4. **Avisos**: el hecho de entrenamiento resuelve su audiencia por `entrenamientoId` en una sola
   consulta; en un partido ya jugado, `CATEGORIAS` suma a los convocados.
5. **Prueba del 012 que dependía de la hora**: `SinAvisosExternosPruebas` programaba "hoy a las
   4:00 p. m." con el reloj real y fallaba pasadas las 4:00 p. m. al reactivar; ahora fija el reloj.

