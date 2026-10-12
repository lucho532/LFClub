# Plan de implementación: Categorías del club

**Rama**: `003-categorias-club` | **Fecha**: 2026-10-08 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/003-categorias-club/spec.md`

## Resumen

El PRESIDENTE organiza a su club: crea una categoría por año de nacimiento, le asigna entrenadores,
la divide en equipos, ubica a los jugadores y retira a los que se van. Los jugadores aprobados
entran solos en la categoría de su año. El DIRECTIVO lo ve todo sin tocar nada, el ENTRENADOR ve
solo sus categorías y cada familia ve la categoría, los equipos y los entrenadores de su jugador.

Enfoque técnico: cinco tablas nuevas y cinco columnas en `UsuarioRol`, sin tecnologías nuevas. La
categoría del jugador y su retiro se guardan en el integrante; todavía no se crea la entidad
`Jugador`. Toda operación que ubica jugadores bloquea antes la fila del club, de modo que la
ubicación automática y las acciones simultáneas dan siempre un resultado coherente. El retiro se
impone en el mismo atributo que ya niega el club a quien está en espera, así que cubre todos los
endpoints presentes y futuros. Las decisiones y sus alternativas están en
[research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Una migración: cinco tablas (`Categorias`, `Equipos`,
`AsignacionesEntrenadorCategoria`, `EntrenadoresEquipo`, `JugadoresEquipo`) y cinco columnas y un
índice en `UsuariosRol`. No mueve datos.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: respuestas de la API por debajo de 300 ms en el percentil 95. Crear
una categoría en una pantalla y una acción; repartir 20 jugadores entre dos equipos con un toque
por jugador.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15); backend y
pantalla de cada historia en la misma tarea (§27.1); no romper nada de la 001 ni de la 002 (§27.2).

**Escala y alcance**: decenas de jugadores por club y menos de veinte categorías (§19); las listas
no se paginan. Esta funcionalidad tiene 5 entidades nuevas, 22 operaciones de API nuevas, 5 que
cambian y 3 pantallas nuevas más 4 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **3.8.0**, que recoge las aclaraciones de esta spec y todavía está sin
confirmar en git. La cabecera de la spec cita la 3.7.0.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `Categoria`, `Equipo`, `AsignacionEntrenadorCategoria`, Jugador, Entrenador; sin sinónimos | Cumple |
| §2.3 Máximo 250 líneas | Un servicio por caso de uso y cinco controladores pequeños; el script de la 001 lo verifica | Cumple |
| §3 Documentación XML | En cada clase nueva y actualizada en las que cambian de responsabilidad | Cumple |
| §4 y §5 Capas y dependencias | Controlador → IServicio → IRepositorio → EF Core; reglas en `Dominio/Reglas` | Cumple |
| §6 Tecnologías base | Ninguna nueva | Cumple |
| §7.1 Aislamiento entre clubes | Las cinco entidades implementan `IPerteneceAClub` y quedan bajo el filtro global; ningún repositorio nuevo se lo salta. No hay excepciones nuevas | Cumple |
| §7.3 Varios clubes | La categoría y el retiro son de cada `UsuarioRol` | Cumple |
| §7.4 Suspensión y baja | Lo resuelve el atributo de la 001: suspendido, solo el PRESIDENTE; dado de baja, nadie | Cumple |
| §7.5 Aislamiento dentro del club | El ENTRENADOR solo recibe categorías con una asignación activa suya; el JUGADOR, un endpoint que solo habla de sí mismo | Cumple |
| §8 Roles | Solo el PRESIDENTE cambia algo; el DIRECTIVO consulta; asignar a un PRESIDENTE o DIRECTIVO no toca su rol | Cumple |
| §11 y §11.2 Categorías | Un año por categoría, única por club y año; una sola categoría actual por jugador; sin historial | Desviación justificada (nota 1 y Seguimiento de complejidad) |
| §11.3 Equipos | Nombre único en la categoría, varios equipos por jugador, el entrenador se asigna a la categoría | Cumple |
| §12.1.1 Sala de espera | Al aprobar a un jugador se le ubica por su año; sin categoría activa, se aprueba igual | Cumple, con la nota 2 |
| §12.3 Alcance de cada rol | El alcance sale del rol y, para el ENTRENADOR, de `AsignacionEntrenadorCategoria` | Cumple |
| §13 Histórico | El retiro guarda copia del nombre de quien retiró | Cumple |
| §14 Desactivables | Categorías, equipos y asignaciones se desactivan; solo se borra lo que nunca se usó, que §14 declara justificado | Cumple |
| §14.1 Retiro | Se expresa con `Activo`; conserva datos; correo y documento siguen ocupados | Cumple |
| §15 Reglas en backend | Rol, club, estado de la categoría y pertenencia del jugador se validan en la API | Cumple |
| §18 Sin historial de estados | Solo estado actual; `Usada` y `Usado` son un dato, no un historial | Cumple |
| §19 Simplicidad | Cada tabla responde a un requisito (research §2); sin entidad `Jugador` ni tabla de retiros | Cumple |
| §20 Pruebas | Cubre las ocho pruebas que la versión 3.8.0 añadió para esta funcionalidad | Cumple |
| §22 Migraciones | Una migración de EF Core | Cumple |
| §23 API | HTTP semántico, DTOs propios, `problem+json`, Swagger; el DTO de la familia es un tipo distinto | Cumple |
| §24 Diseño visual | Componentes y temas de la 001; estados con texto además de color | Cumple |
| §25 Decisiones no tomadas | Los detalles sin fijar están como supuestos en research.md; ninguno inventa una regla de negocio | Cumple |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna. "Ficha con historial" no se toca | Cumple |

**Resultado de la puerta**: pasa, con una desviación de la letra de §11.2 justificada en
"Seguimiento de complejidad".

**Nota 1**: §11.2 escribe `Jugador.CategoriaId`. La entidad `Jugador` todavía no existe y la spec
dice que aquí "el jugador es el integrante aprobado con el rol JUGADOR". La categoría se guarda en
`UsuarioRol.CategoriaId`. Se cumple lo que la regla protege (un solo registro por persona y una
sola categoría actual); la funcionalidad de jugadores decidirá si la columna pasa a la ficha.

**Nota 2**: §12.1.1 dice también que al aprobar empieza a generarse la mensualidad. La spec lo
deja a la funcionalidad de mensualidades; este plan no adelanta nada de ella.

**Revisión tras el diseño**: el modelo de datos y el contrato no añaden excepciones al
aislamiento, tecnologías ni patrones respecto de lo evaluado. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/003-categorias-club/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Entidades, reglas y transiciones
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Contrato OpenAPI de lo nuevo y lo que cambia
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/
│   │   ├── + Categoria.cs
│   │   ├── + Equipo.cs
│   │   ├── + AsignacionEntrenadorCategoria.cs
│   │   ├── + EntrenadorEquipo.cs
│   │   ├── + JugadorEquipo.cs
│   │   └── ~ UsuarioRol.cs                    # CategoriaId, Activo y datos del retiro
│   └── Reglas/
│       ├── + ReglaAnioDeCategoria.cs          # cuatro cifras y no posterior al año en curso
│       ├── + ReglaEntrenadorAsignable.cs      # aprobado y ENTRENADOR, DIRECTIVO o PRESIDENTE
│       ├── + ReglaAlcanceDeCategorias.cs      # qué ve cada rol
│       ├── ~ ReglaAccesoPorEstado.cs          # también el retiro
│       └── ~ ResultadoAcceso.cs               # IntegranteRetirado
├── LFClub.Aplicacion/
│   ├── DTOs/              + CrearCategoriaDto, CategoriaDto, CategoriaDetalleDto,
│   │                        CategoriaConUbicadosDto, EquipoDto, EquipoDeReferenciaDto,
│   │                        NombreEquipoDto, EquiposDeEntrenadorDto, EntrenadorDeCategoriaDto,
│   │                        CandidatoEntrenadorDto, JugadorDeCategoriaDto, UbicarJugadorDto,
│   │                        JugadorRetiradoDto, ReincorporacionDto, MiCategoriaDto,
│   │                        EntrenadorParaFamiliaDto
│   │                      ~ ClubDeSesionDto
│   ├── Interfaces/        + IRepositorioCategorias, IRepositorioEquipos,
│   │                        IRepositorioAsignaciones, IRepositorioJugadores
│   │                      ~ IRepositorioClub (bloquear el club de la petición),
│   │                        IRepositorioInvitacionesClub, IRepositorioPertenencias
│   ├── Servicios/         + IServicioCategorias, IServicioConsultaCategorias,
│   │                        IServicioEntrenadoresDeCategoria, IServicioEquipos,
│   │                        IServicioJugadoresDeEquipo, IServicioUbicacionJugador,
│   │                        IServicioRetiroJugador, IServicioMiCategoria
│   ├── Implementaciones/  + las ocho implementaciones anteriores
│   │                      ~ ServicioAprobacionIngreso, ServicioInvitacionesClub,
│   │                        ServicioRegistroConInvitacion, ServicioAceptacionInvitacion
│   ├── Mappers/           + MapperCategorias   ~ MapperSesion
│   ├── Validadores/       + ValidadorNombreEquipo
│   └── Utilidades/        + UbicadorDeJugadores, ErroresDeCategorias
│                          ~ ErroresDeInvitacion (persona retirada), IndicesUnicos
├── LFClub.Infraestructura/
│   ├── Datos/
│   │   ├── ~ ContextoLFClub.cs              # cinco DbSet
│   │   ├── Configuraciones/   + una por cada entidad nueva   ~ ConfiguracionUsuarioRol
│   │   └── Migraciones/       + CategoriasDelClub
│   └── Repositorios/
│       ├── + RepositorioCategorias.cs         # todos dentro del club, con el filtro activo
│       ├── + RepositorioEquipos.cs
│       ├── + RepositorioAsignaciones.cs
│       ├── + RepositorioJugadores.cs
│       ├── ~ RepositorioClub.cs               # SELECT ... FOR UPDATE del club de la petición
│       ├── ~ RepositorioInvitacionesClub.cs   # distingue al retirado
│       └── Plataforma/ ~ RepositorioPertenencias.cs
└── LFClub.Api/
    ├── Autorizacion/      ~ IntegranteDelClubAttribute   # niega a quien está retirado
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/Club/
        ├── + ControladorCategorias.cs               # lista, detalle, crear, desactivar, reactivar, borrar
        ├── + ControladorEntrenadoresDeCategoria.cs  # candidatos, asignar, retirar, equipos que dirige
        ├── + ControladorEquipos.cs                  # crear, renombrar, desactivar, borrar, jugadores
        ├── + ControladorJugadores.cs                # sin categoría, retirados, ubicar, retirar, reincorporar
        └── + ControladorMiCategoria.cs

backend/pruebas/
├── Unitarias/Reglas/      + ReglaAnioDeCategoria, ReglaEntrenadorAsignable, ReglaAlcanceDeCategorias
│                          ~ ReglaAccesoPorEstado
├── Unitarias/Validadores/ + ValidadorNombreEquipo
└── Integracion/
    ├── + Categorias/      # GestionCategorias, UbicacionAutomatica, Entrenadores, CambioDeCategoria,
    │                      # Equipos, RetiroJugador, ConsultaPorRol, MiCategoria,
    │                      # AislamientoCategorias, CategoriasPorEstadoDelClub, EscenarioCategorias
    ├── ~ Aislamiento/AccesoClubPruebas.cs   # 55 endpoints en los contratos
    └── ~ Base/Sembrador                     # jugadores con fecha de nacimiento, categorías y equipos

frontend/src/
├── compartido/
│   ├── api/               ~ tipos.ts
│   └── sesion/            ~ ultimoClub.ts              # no prefiere un club en el que está retirada
├── ~ App.tsx                                           # rutas de categorías
└── privado/
    ├── ~ DisposicionClub.tsx                           # aviso de retiro y enlace "Categorías"
    ├── ~ DesplegableClubes.tsx                         # marca los clubes en los que está retirada
    ├── ~ InicioClub.tsx                                # tarjeta "Mi categoría" para el JUGADOR
    ├── + AvisoRetirado.tsx
    ├── + TarjetaMiCategoria.tsx
    └── + categorias/
        ├── Categorias.tsx                # lista, crear y, para PRESIDENTE y DIRECTIVO, las dos listas
        ├── FormularioCrearCategoria.tsx
        ├── SeccionSinCategoria.tsx
        ├── SeccionRetirados.tsx
        ├── DetalleCategoria.tsx          # cabecera, acciones de la categoría y tres secciones
        ├── SeccionEntrenadores.tsx
        ├── DialogoAsignarEntrenador.tsx
        ├── SeccionEquipos.tsx
        ├── SeccionJugadores.tsx          # una casilla por equipo en cada fila
        └── DialogoCambiarCategoria.tsx

frontend/pruebas/          ~ ultimoClub.test.ts
```

**Decisión de estructura**: la de la 001, sin carpetas de primer nivel nuevas. El backend reparte
las 22 operaciones en cinco controladores y ocho servicios para que ningún archivo se acerque a
las 250 líneas; el frontend agrupa las pantallas en `privado/categorias/`.

### Pantallas

| Ruta | Pantalla | Quién |
| --- | --- | --- |
| `/club/:clubId/categorias` | "Categorías": lista por año con jugadores, equipos y entrenadores; crear; "Sin categoría" y "Retirados" | PRESIDENTE (gestiona), DIRECTIVO (solo lectura), ENTRENADOR (solo las suyas, sin las dos listas) |
| `/club/:clubId/categorias/:categoriaId` | Detalle: entrenadores, equipos y jugadores; desactivar, reactivar y borrar | Los mismos, con el mismo alcance |
| `/club/:clubId` (cambia) | Tarjeta "Mi categoría": categoría, equipos y entrenadores, o "todavía no tienes categoría" | JUGADOR |
| `/club/:clubId` (jugador retirado) | Aviso "Ya no estás en este club", con tema, cambio de club y cerrar sesión | Jugador retirado |
| Menú del club (cambia) | Enlace "Categorías" | PRESIDENTE, DIRECTIVO y ENTRENADOR |
| Desplegable de clubes (cambia) | Marca los clubes en los que la persona está retirada | Quien tiene varios clubes |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Crear categorías | RF-001 a 007 | `ControladorCategorias`, `ServicioCategorias`, `ReglaAnioDeCategoria` | Lista y formulario de crear | Año inválido; repetida activa e inactiva; dos altas a la vez; desactivar con y sin jugadores; reactivar sin entrenadores; borrar solo lo nunca usado; otro club; cada rol |
| 2. Ubicación automática | RF-008 a 013 | `UbicadorDeJugadores`, `ServicioAprobacionIngreso` | "Sin categoría", aviso de cuántos entraron | Aprobar con y sin categoría; crear y reactivar recogen a los de ese año y a nadie más; no mueve a quien ya tiene; ENTRENADOR y en espera, fuera; aprobar mientras se crea |
| 3. Entrenadores | RF-017 a 021 | `ControladorEntrenadoresDeCategoria`, `ServicioEntrenadoresDeCategoria`, `ReglaEntrenadorAsignable` | Sección de entrenadores y diálogo | Candidatos; varios por categoría y varias por entrenador; idempotencia; JUGADOR, en espera y otro club; categoría inactiva; PRESIDENTE y DIRECTIVO conservan rol y alcance |
| 4. Ubicar y cambiar | RF-014 a 016 | `ControladorJugadores`, `ServicioUbicacionJugador` | Sección de jugadores y diálogo de cambio | Ubicar y pasar; marca de fuera de su año; categoría inactiva o ajena; no jugadores; dos cambios a la vez; nunca en dos categorías |
| 5. Equipos | RF-022 a 030 | `ControladorEquipos`, `ServicioEquipos`, `ServicioJugadoresDeEquipo` | Sección de equipos y casillas por jugador | Nombre repetido sin distinguir mayúsculas; varios equipos por jugador; equipo de otra categoría; salir al cambiar de categoría; equipos que dirige; desactivar y borrar |
| 6. Retiro | RF-041 a 047 | `ServicioRetiroJugador`, `ReglaAccesoPorEstado`, `IntegranteDelClubAttribute`, invitación y registro | "Retirados", aviso de retiro | `403` en todos los endpoints del club; otros clubes intactos; datos conservados; correo y documento ocupados; reincorporar ubica y no devuelve equipos; solo jugadores; idempotencia |
| 7. Consulta por rol | RF-031 a 036 | `ServicioConsultaCategorias`, `ReglaAlcanceDeCategorias`, `ControladorMiCategoria` | Las mismas, sin acciones; tarjeta "Mi categoría" | DIRECTIVO ve todo y no cambia nada; ENTRENADOR solo las suyas, con todos los equipos; JUGADOR solo lo suyo y sin datos de contacto |
| Transversal | RF-037 a 040 | Filtro global y estado del club | Todas, a 360 px y en los dos temas | Otro club recibe `404` por identificador; el DESARROLLADOR, `404`; suspendido y dado de baja |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: entidades, configuraciones y migración; reglas de dominio con sus pruebas unitarias;
   bloqueo del club; `UbicadorDeJugadores`; `Sembrador` y la prueba de contrato a 55 endpoints.
2. **Historia 1**: categorías (lista, crear, desactivar, reactivar, borrar) con su pantalla y el
   enlace del menú.
3. **Historia 2**: ubicación al aprobar, al crear y al reactivar; "Sin categoría"; detalle de la
   categoría con sus jugadores.
4. **Historia 3**: entrenadores de la categoría.
5. **Historia 4**: ubicar y cambiar de categoría, con la marca de fuera de su año.
6. **Historia 5**: equipos, jugadores de cada equipo y equipos que dirige cada entrenador.
7. **Historia 6**: retiro, reincorporación, lista de retirados, aviso y mensajes de invitación y
   registro.
8. **Historia 7**: alcance del ENTRENADOR y del DIRECTIVO en las pantallas y tarjeta "Mi
   categoría".
9. **Cierre**: estados del club, revisión a 360 px en los dos temas, contrato frente a Swagger y
   recorrido del quickstart.

Las historias 1 y 2 son el mínimo utilizable: el club queda con sus categorías y sus jugadores
ubicados.

## Cambios sobre lo ya construido en la 001 y la 002

Son los puntos donde hay que cuidar la no regresión (§27.2):

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| `IntegranteDelClubAttribute` y `ReglaAccesoPorEstado` niegan a quien está retirado | RF-043 | Las pruebas de acceso anteriores siguen en verde: todos sus integrantes son activos. Prueba nueva que recorre todos los endpoints del club con un retirado |
| `ServicioAprobacionIngreso` aprueba y ubica en una transacción con el club bloqueado | RF-008, RF-009 | Las pruebas de aprobación de la 002 siguen en verde, incluida la de dos aprobaciones a la vez |
| `ClubDeSesionDto` gana `retirado` | RF-043 | Campo nuevo; `ultimoClub.test.ts` añade el caso |
| Invitar, registrarse y aceptar distinguen al retirado con `409 persona_retirada` | RF-046 | Los demás códigos de la 002 no cambian; pruebas nuevas |
| Aceptar una invitación de presidente siendo jugador del club lo saca de su categoría y de sus equipos y lo deja activo | RF-012, RF-041 | Prueba nueva; la sustitución de rol de la 001 conserva la suya |
| Eliminar del club a un presidente borra en cascada sus asignaciones | Integridad referencial | Prueba nueva sobre el retiro de presidente con una asignación |
| `AccesoClubPruebas` espera 55 endpoints en lugar de 33 | El contrato de la 003 añade 22 | Se actualiza en el bloque base. Estará en rojo desde que existe este contrato hasta que se construya el último endpoint |

## Seguimiento de complejidad

| Desviación | Por qué hace falta | Alternativa más simple descartada |
| --- | --- | --- |
| §11.2 escribe `Jugador.CategoriaId`; la categoría se guarda en `UsuarioRol.CategoriaId` | La entidad `Jugador` no existe todavía y la spec deja la ficha a su funcionalidad. Se cumple lo que la regla protege: un solo registro por persona y una sola categoría actual | Crear una tabla `Jugador` con una sola columna útil adelanta esa funcionalidad (§19, §25) |

Queda pendiente una enmienda de §11.2 que recoja esta ubicación provisional; se hace con
`/speckit-constitution`, fuera de este plan.

## Supuestos confirmados por el propietario

Confirmados el 2026-10-08 y recogidos en la spec (sección "Supuestos", RF-042 y RF-046). Están
detallados al final de [research.md](research.md):

1. "El año en curso" se calcula con la fecha UTC del servidor.
2. Reasignar a un entrenador reactiva su asignación anterior.
3. El nombre de un equipo desactivado queda libre, y un equipo desactivado no se reactiva.
4. Un ENTRENADOR recibe `404` al pedir una categoría que no tiene asignada.
5. Al reincorporar no se conserva quién retiró al jugador ni cuándo.
6. Un jugador retirado de un club suspendido ve el aviso de incidencia temporal.
7. Eliminar del club a un presidente que entrenaba borra sus asignaciones.
8. "Ingresos aprobados" sigue mostrando a un jugador retirado.
