# Plan de implementación: avisos de entrenamientos y partidos en la campana

**Rama**: `012-avisos-de-actividades` | **Fecha**: 2026-10-12 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación en `specs/012-avisos-de-actividades/spec.md`, más un ajuste de la ficha
pedido por el propietario durante el plan (bloque B, solo de interfaz).

## Resumen

**Bloque A (la spec).** Cuando el equipo del club, o una familia al publicar un resultado, hace algo
con un entrenamiento o un partido, el sistema escribe solo una línea corta ("Nuevo entrenamiento el
sábado a las 9:00") y la deja en la campana de las familias que ya podían ver esa actividad. Se
mezcla con los comunicados, suma al mismo contador y al tocarla abre el detalle. Varios cambios sin
leer sobre la misma actividad se reemplazan por el último. Nada sale de la aplicación.

Enfoque técnico: **dos tablas nuevas** (`AvisosDeActividad` y `AvisosDeActividadDestinatario`) y una
migración. Un servicio nuevo, `ServicioAvisosDeActividad`, lo llaman nueve operaciones de la 009 y
la 010 dentro de su propia transacción y solo si algo cambió de verdad. La campana pasa a tener su
ruta `/campana` (4 operaciones, de las que 3 salen de `/comunicados/recibidos`) que mezcla las dos
fuentes por identificador. Las decisiones están en [research.md](research.md).

**Bloque B (pedido del propietario).** En la ficha del jugador, la tarjeta "Identidad" cambia sus dos
botones ("Cambiar documento" y "Corregir nombre y fecha") por **un lápiz** que abre un solo diálogo
con nombres, apellidos, fecha y documento. El documento **sigue editable** con las reglas de
siempre. Es solo de interfaz: no cambia el backend ni la constitución.

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001. **No se añade ninguna.** Ni servicio de avisos externos
ni cliente de salida: el aviso con la aplicación cerrada es de otra spec.

**Almacenamiento**: PostgreSQL 17. Una migración con dos tablas, un índice parcial y una restricción
de comprobación ([data-model.md](data-model.md)). El bloque B no toca la base de datos.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers, con el
`IReloj` y el servicio de correo sustituidos); Vitest en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable,
también empaquetada con Capacitor para Android.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: contador por índice parcial `(UsuarioId, ClubId) WHERE LeidoEn IS
NULL` en cada tabla; lista por cursor sobre `(UsuarioId, ClubId, AvisoId)`. Generar un aviso es una
inserción por destinatario, dentro de la transacción del cambio (decenas por club).

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3) —`ServicioPartidos` ronda las 200 y se vigila—; toda regla en el
servidor (§15, RF-031); backend y pantalla de cada historia en la misma tarea (§27.1); ningún aviso
fuera de la aplicación (RF-027); el texto nunca se interpreta.

**Escala y alcance**: 2 entidades y 2 enumeraciones nuevas; el contrato pasa de 108 a 109
operaciones (+4 de la campana, −3 que se mueven) y las de club, de 84 a 85; unos 7 archivos de
frontend nuevos y unos 10 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.10.0**, que ya recoge el bloque A (§8, §14, §14.4, §17.2, §20 y §24).
El bloque B no cambia ninguna regla.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `AvisoDeActividad`, `AvisoDeActividadDestinatario`, `HechoDeActividad`, `TipoDeActividad`, `ServicioAvisosDeActividad`, `RedaccionDeAvisos`; "familia" solo en textos de pantalla | Cumple |
| §2.3 Máximo 250 líneas | La generación de avisos vive en un servicio propio; los servicios de la 009 solo añaden llamadas | Cumple, vigilar `ServicioPartidos` |
| §3 Documentación XML | En cada clase nueva; se amplía la de las clases que cambian | Cumple |
| §4 y §5 Capas | Reglas puras en `Dominio/Reglas`; servicios en `Aplicacion`; repositorio en `Infraestructura`; controlador que delega | Cumple |
| §6 y §19 Tecnologías y simplicidad | Sin dependencias nuevas; sin tabla genérica de notificaciones; sin servicio en segundo plano | Cumple |
| §7.1 Aislamiento entre clubes | Las dos entidades implementan `IPerteneceAClub`; las 4 rutas bajo `/api/clubes/{clubId}` entran solas en las pruebas de aislamiento | Cumple |
| §7.5 Aislamiento dentro del club | La cuenta ve solo filas de sus jugadores; la sesión limitada, solo las del suyo; el aviso no revela la actividad (solo `disponible`) | Cumple |
| §8 Roles | Solo JUGADOR recibe y lee; el equipo del club no tiene filas ni campana; DESARROLLADOR 404 | Cumple |
| §13 Histórico | El texto del aviso es una instantánea; no hay historial de avisos reemplazados (supuesto de la spec) | Cumple |
| §14 y §14.4 Desactivables y avisos | Nunca se borra por acción de usuario; el único borrado es el reemplazo del sistema (RF-025) | Cumple |
| §15 Reglas en backend | Destinatarios, texto, cambio real y `disponible` en el servidor | Cumple |
| §17.2 Visibilidad de partidos | Un partido próximo solo se avisa a convocados; `disponible` usa la misma regla | Cumple |
| §18 Estados | El aviso solo está sin leer o leído, por cuenta | Cumple |
| §20 Pruebas | Las pruebas de §20 para el bloque A como integración y las reglas como unitarias | Cumple |
| §22 Migraciones | Una migración controlada, sin datos que migrar | Cumple |
| §23 API | DTOs propios; `problem+json`; Swagger; sin códigos de error nuevos: reutiliza `no_encontrado` y `rol_no_autorizado` | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas, tipo y leído por texto; el lápiz con nombre accesible | Cumple, con la nota 1 |
| §25 Decisiones no tomadas | Los supuestos de research §15 son técnicos y no alteran ninguna regla de la spec | Cumple |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |
| §8 y §10 Documento y ficha | El bloque B conserva quién cambia el documento y sus reglas; solo une dos botones en un lápiz | Cumple |

**Resultado de la puerta**: pasa.

**Nota 1**: el lienzo no tiene la campana con avisos ni la tarjeta con el lápiz. Su diseño sigue
[contracts/campana.md](contracts/campana.md) con los tokens y componentes del proyecto, y lo revisa
el propietario en la validación.

**Nota 2 (resuelta)**: el aviso a quien sale de una convocatoria se descartó por decisión del
propietario: ningún niño debe enterarse por la plataforma de que ya no está convocado (§17.2). No
hace falta la enmienda 4.10.1 y la 4.10.0 se cumple al pie de la letra.

**Revisión tras el diseño**: dos tablas justificadas por la audiencia fija y el leído por cuenta;
ninguna dependencia nueva; las 4 rutas entran en las pruebas de aislamiento existentes. La puerta
del bloque A sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/012-avisos-de-actividades/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas, supuestos y el bloque B (§16)
├── data-model.md        # Tablas nuevas, cálculos y transiciones
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   ├── api.yaml         # Cuatro operaciones nuevas y tres que cambian de ruta
│   └── campana.md       # Comportamiento de la campana, de las filas de aviso y del lápiz
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`), los que cambian (`~`) y los que se eliminan (`-`). La
estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/         + AvisoDeActividad, AvisoDeActividadDestinatario
│   │                      ~ UsuarioRol, Entrenamiento, Partido (documentación)
│   ├── Enumeraciones/     + TipoDeActividad, HechoDeActividad
│   └── Reglas/            + ReglaCambiosDeActividad, ReglaDestinatariosDeAviso
├── LFClub.Aplicacion/
│   ├── DTOs/              + AvisoRecibidoDto, ActividadDeAvisoDto, ElementoDeCampanaDto,
│   │                        PaginaDeCampanaDto, ContadorDeCampanaDto
│   │                      - PaginaDeRecibidosDto, ContadorDeComunicadosDto
│   ├── Interfaces/        + IRepositorioAvisos
│   │                      ~ IRepositorioAsistencias (RegistrarAsync devuelve categorías con cambio)
│   ├── Servicios/         + IServicioAvisosDeActividad, IServicioConsultaCampana,
│   │                        IServicioLecturaCampana
│   │                      ~ IServicioConsultaComunicados, IServicioLecturaComunicados
│   ├── Implementaciones/  + ServicioAvisosDeActividad, ServicioConsultaCampana,
│   │                        ServicioLecturaCampana
│   │                      ~ ServicioEntrenamientos, ServicioPartidos, ServicioConvocatoria,
│   │                        ServicioAsistencia, ServicioResultado, ServicioResultadoFamilia,
│   │                        ServicioConsultaComunicados, ServicioLecturaComunicados
│   ├── Mappers/           + MapperCampana
│   └── Utilidades/        + RedaccionDeAvisos, OrdenDeIdentificadores
├── LFClub.Infraestructura/
│   ├── Datos/             ~ ContextoLFClub (dos DbSet)
│   │   ├── Configuraciones/ + ConfiguracionAvisoDeActividad, ConfiguracionAvisoDeActividadDestinatario
│   │   └── Migraciones/   + AvisosDeActividad
│   └── Repositorios/      + RepositorioAvisos, RepositorioAvisos.Campana (parcial)
│                          ~ RepositorioAsistencias
└── LFClub.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/Club/ + ControladorCampana
                           ~ ControladorComunicadosRecibidos (quedan detalle y lectura)

backend/pruebas/
├── Unitarias/
│   ├── Reglas/            + ReglaCambiosDeActividadPruebas, ReglaDestinatariosDeAvisoPruebas
│   └── Utilidades/        + RedaccionDeAvisosPruebas, OrdenDeIdentificadoresPruebas
└── Integracion/
    ├── Avisos/            + AvisosDeEntrenamientoPruebas, AvisosDePartidoPruebas,
    │                        ConvocatoriaYAvisosPruebas, CampanaMezclaPruebas,
    │                        ReemplazoDeAvisosPruebas, DisponibilidadPruebas,
    │                        SinAvisosExternosPruebas
    ├── Comunicados/       ~ las que usaban lista, contador y "marcar todos" pasan a /campana
    └── Aislamiento/       ~ AccesoClubPruebas (contrato de 109; 85 de club)

frontend/src/
├── App.tsx                ~ sin rutas nuevas (la campana usa /comunicados)
├── compartido/
│   ├── api/               + tiposCampana.ts
│   │                      ~ tiposComunicados.ts
│   └── componentes/       ~ CampanaComunicados.tsx (nombre accesible "Novedades")
├── privado/
│   ├── comunicados/       + FilaDeAviso.tsx, AvisoNoDisponible.tsx
│   │                      ~ PantallaComunicados.tsx, ListaRecibidos.tsx,
│   │                        useContadorComunicados.ts, textos.ts
│   └── ficha/             + DialogoEditarIdentidad.tsx (reúne los dos diálogos, bloque B)
│                          ~ SeccionIdentidad.tsx
│                          - DialogoCambiarDocumento.tsx, DialogoCorregirIdentidad.tsx
└── (frontend/pruebas/)    + FilaDeAviso.test.tsx
                           ~ CampanaComunicados.test.tsx, SeccionIdentidad.test.tsx
```

**Decisión de estructura**: la de la 001, con carpeta nueva `Avisos/` en las pruebas de integración.

### Pantallas

| Ruta | Qué muestra | Quién |
| --- | --- | --- |
| Barra superior de todo `/club/:clubId/**` | La campana ya existente; ahora cuenta comunicados y avisos y se llama "Novedades" | JUGADOR |
| `/club/:clubId/comunicados` | JUGADOR: "Novedades", lista mezclada y "Marcar todos". Equipo del club: sin cambios | Según el rol |
| `/club/:clubId/entrenamientos/:id` y `/partidos/:id` | Sin cambios; es a donde navega un aviso disponible | Los de siempre |
| Ficha del jugador, tarjeta "Identidad" | **Bloque B**: un lápiz que abre un solo diálogo con nombres, apellidos, fecha y documento | Quien hoy cambia el documento o corrige la identidad |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Entrenamientos | RF-001, 004, 006, 010 a 013, 016 | `ServicioAvisosDeActividad`, `ServicioEntrenamientos`, `ServicioAsistencia`, `ReglaCambiosDeActividad`, `RepositorioAvisos` | Fila de aviso | Programar, cambiar fecha/hora/lugar, solo nota (no avisa), cancelar, reactivar, asistencia sin nombres, dos categorías con hermanos, categoría ajena sin aviso |
| 2. Partidos | RF-002, 003, 005, 007 a 009, 014, 015, 017 | `ServicioPartidos`, `ServicioConvocatoria`, `ServicioResultado`, `ServicioResultadoFamilia`, `ReglaDestinatariosDeAviso` | Fila de aviso | Crear sin convocados no avisa; convocar; sacar sin aviso; cambio solo a convocados; resultado de equipo y de familia (no a quien publica); no convocado no ve nada |
| 3. Campana | RF-018 a 024, 034 | `ControladorCampana`, `ServicioConsultaCampana`, `ServicioLecturaCampana`, `OrdenDeIdentificadores` | `PantallaComunicados`, `ListaRecibidos`, `FilaDeAviso`, `AvisoNoDisponible` | Contador igual a la suma; mezcla por fecha con orden de bytes; leer baja el contador; por cuenta; marcar todos; "99+"; `disponible` igual al 404 real |
| 4. Sin ruido | RF-025 a 029 | `ServicioAvisosDeActividad`, `RepositorioAvisos` | Ninguna | Tres cambios seguidos dejan un aviso; uno leído no se toca; el equipo del club sin campana; nada de correo |
| Transversal | RF-030 a 033, 035 | Aislamiento | Todas, a 360 px y en los dos temas | Contrato de 109; 85 de club con 404 a otro club y al DESARROLLADOR; la 001 a la 011 en verde |
| B. Identidad | RF-036 a RF-038 (ajuste en [spec.md](spec.md)) | Ninguno: usa los endpoints de la 005 | `SeccionIdentidad`, `DialogoEditarIdentidad` | Un solo lápiz y ningún botón de texto; envía solo lo que cambió y primero el documento; un fallo del documento no guarda nada; campos según permiso; sin lápiz sin permisos |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: medir el tamaño de los archivos que se tocan (`verificar-tamano`); contrato comentado
   hasta que exista cada endpoint (como en la 008 a la 011, para que `AccesoClubPruebas` siga en
   verde); enumeraciones, entidades, configuraciones y migración; reglas puras, `RedaccionDeAvisos` y
   `OrdenDeIdentificadores` con sus pruebas.
2. **Historia 3 primero (sin generar nada)**: `/campana` con la mezcla, el contador y la lectura, y
   migrar a ella las pruebas y la pantalla de la 011. Así la campana ya sirve cuando lleguen los
   avisos.
3. **Historia 1**: entrenamientos, con su aviso y la fila en la lista.
4. **Historia 2**: partidos, convocatoria y resultado.
5. **Historia 4**: reemplazo, no reemplazo de lo leído y comprobación de que nada sale de la app.
6. **Bloque B**: el diálogo unificado y el lápiz, con sus pruebas de Vitest. Es independiente del
   bloque A y puede ir antes o después.
7. **Cierre**: contrato sin marcas contra Swagger (109), aislamiento (85), revisión a 360 px en los
   dos temas, recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y batería
   entera.

## Seguimiento de complejidad

Sin desviaciones: no hay dependencias nuevas ni patrones nuevos. Las dos decisiones que se apartan de
lo más corto son guardar a los destinatarios (como la 011) y mover tres operaciones de la 011 a
`/campana` (research §1 y §4).
