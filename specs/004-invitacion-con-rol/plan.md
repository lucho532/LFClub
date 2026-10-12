# Plan de implementación: Invitación con rol e ingreso directo al club

**Rama**: `004-invitacion-con-rol` | **Fecha**: 2026-10-08 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/004-invitacion-con-rol/spec.md`

## Resumen

Adapta lo construido en la 002 y la 003 a la constitución 4.1.0. Dentro del club solo el
PRESIDENTE invita, y al hacerlo elige el rol; quien se registra con el enlace entra directamente
al club con ese rol, sin sala de espera, y si es JUGADOR queda en la categoría de su año. La sala
de espera, la aprobación y el rechazo se conservan para el hermano agregado desde la ficha, pero
solo los atiende el PRESIDENTE y al aprobar ya no se elige rol: siempre es JUGADOR. El DIRECTIVO
deja de tener el apartado "Ingresos". El DESARROLLADOR solo invita al primer presidente, al crear
el club.

Enfoque técnico: no hay tablas, columnas ni migración, porque `Invitaciones.Rol` ya existe y solo
faltaba dejar elegirlo. No hay endpoints nuevos: cambian once, cuatro de ellos solo en quién
puede llamarlos, y se elimina uno. La autorización se resuelve cambiando el rol admitido en el atributo de la 001.
La regla de aprobación desaparece y una regla pequeña dice qué roles admite una invitación. El
registro y la aceptación fijan en el contexto el club de la invitación para reutilizar, tal cual,
el ubicador de jugadores y el bloqueo del club de la 003. Las decisiones y sus alternativas están
en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Sin migración: el esquema que deja la 003 no cambia.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: los de la 001; esta funcionalidad no añade ninguno.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15); backend y
pantalla de cada historia en la misma tarea (§27.1); no romper nada de la 001, la 002 ni la 003
que la constitución 4.1.0 no haya cambiado (§27.2, CE-008).

**Escala y alcance**: decenas de personas por club (§19). Esta funcionalidad tiene 1 regla de
dominio nueva, 1 que cambia y 1 que se elimina; 11 operaciones de API que cambian, 1 que se
elimina y ninguna nueva; y 7 pantallas que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.1.0**, enmendada el 2026-10-08 durante este plan por decisión del
propietario (solo el PRESIDENTE invita y aprueba o rechaza ingresos; el DESARROLLADOR solo invita
al crear el club).

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `ReglaInvitacionDelClub`, Jugador, Entrenador, Directivo; sin sinónimos | Cumple |
| §2.3 Máximo 250 líneas | Los archivos que crecen quedan lejos del límite; las pruebas más largas se reparten (ver "Cambios sobre lo ya construido") | Cumple |
| §3 Documentación XML | Se reescribe en cada clase que cambia de responsabilidad: hoy varias dicen "queda en la sala de espera", "no lleva rol" o "el PRESIDENTE y los DIRECTIVOS" | Cumple |
| §4 y §5 Capas y dependencias | Las reglas en `Dominio/Reglas`; los controladores no ganan lógica | Cumple |
| §6 Tecnologías base | Ninguna nueva | Cumple |
| §7.1 Aislamiento entre clubes | Sin consultas nuevas fuera del filtro. El registro y la aceptación fijan el club de la invitación, dentro de la tercera excepción que §7.1 ya admite | Cumple, con la nota 1 |
| §7.3 Varios clubes | Quien ya tiene cuenta se añade al club con el rol de la invitación y conserva lo demás | Cumple |
| §7.4 Suspensión y baja | Lo resuelven el atributo de la 001 y la regla de uso de la invitación, sin código nuevo | Cumple |
| §8 Roles | Solo el PRESIDENTE invita, aprueba y rechaza; el DIRECTIVO no invita ni aprueba ni asigna roles; un único rol por club | Cumple |
| §11.2 Categoría del jugador | Sigue en `UsuarioRol.CategoriaId`, como en la 003 | Desviación heredada (nota 2 y Seguimiento de complejidad) |
| §12.1 Registro | Rol en la invitación, entrada directa, ubicación del JUGADOR al registrarse, responsable solo para el jugador | Cumple, con la nota 3 |
| §12.1.1 Sala de espera | Se conserva para el hermano; solo el PRESIDENTE aprueba o rechaza; al aprobar entra como JUGADOR y se ubica | Cumple |
| §12.2 Asignación de roles | Desaparece el único camino por el que un DIRECTIVO fijaba un rol | Cumple |
| §12.3 Alcance de cada rol | Quien entra de ENTRENADOR o DIRECTIVO no tiene ningún dato de jugador | Cumple, con la nota 2 |
| §12.5 Invitación del PRESIDENTE | El DESARROLLADOR solo invita al crear el club; se elimina invitar a un club que ya existe y se conserva reenviar y corregir el correo. El registro del presidente solo cambia en que deja de pedir el responsable (RF-013) | Cumple |
| §13 Histórico | Las aprobaciones anteriores conservan su rol de ingreso y quién aprobó | Cumple |
| §14 Desactivables | No se borra nada nuevo; el rechazo es el de la 002 | Cumple |
| §15 Reglas en backend | Quién invita y aprueba, rol invitable, rol al aprobar y responsable se validan en la API | Cumple |
| §18 Sin historial de estados | No se añade ninguno | Cumple |
| §19 Simplicidad | Sin tablas, columnas, endpoints ni servicios nuevos; se elimina una regla y un diálogo | Cumple |
| §20 Pruebas | Cubre las pruebas que la 4.0.0 y la 4.1.0 añadieron o ajustaron para esta funcionalidad (research §11) | Cumple |
| §22 Migraciones | No hay cambio de esquema | Cumple |
| §23 API | HTTP semántico, DTOs, `problem+json`, Swagger; contrato actualizado | Cumple |
| §24 Diseño visual | Componentes y temas existentes; el rol se muestra con texto | Cumple |
| §25 Decisiones no tomadas | Los cuatro detalles que la spec no fijaba se registraron como supuestos en research.md y quedaron resueltos el 2026-10-09; ninguno inventa una regla de negocio | Cumple |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |

**Resultado de la puerta**: pasa, con la misma desviación de la letra de §11.2 que ya justificó la
003.

**Nota 1**: registrarse y aceptar no llevan club en la ruta. Para ubicar al jugador hay que leer
las categorías del club y bloquear su fila, y eso exige un club en el contexto. Lo fija el propio
servicio a partir de la invitación válida. No es una excepción nueva: es "abrir una invitación por
su enlace", que §7.1 limita a esa invitación y su club, y la respuesta sigue sin devolver datos
del club (research §4).

**Nota 2**: §12.3 habla de la "ficha de Jugador", que todavía no existe: como en la 003, el
jugador es el integrante aprobado con rol JUGADOR. "No tiene ficha de Jugador" se cumple porque
quien entra de ENTRENADOR o DIRECTIVO nunca tiene categoría ni aparece en ninguna lista de
jugadores.

**Nota 3**: §12.1 dice también que al entrar empieza a generarse la mensualidad. La spec lo deja a
la funcionalidad de mensualidades; este plan no adelanta nada de ella.

**Revisión tras el diseño**: el modelo de datos y el contrato no añaden tablas, endpoints,
excepciones al aislamiento, tecnologías ni patrones respecto de lo evaluado. La puerta sigue
pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/004-invitacion-con-rol/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos del plan
├── data-model.md        # Qué cambia en el uso de las entidades (sin migración)
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Contrato OpenAPI de los once endpoints que cambian
└── tasks.md             # Lo genera /speckit-tasks

specs/001-base-multiclub/contracts/
└── ~ api.yaml           # se retira la ruta de invitar a un presidente a un club que ya existe
```

### Código fuente

Solo se listan los archivos nuevos (`+`), los que cambian (`~`) y los que se eliminan (`-`). La
estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/
│   │   ├── ~ Invitacion.cs                  # solo documentación: el rol lo elige el PRESIDENTE
│   │   └── ~ UsuarioRol.cs                  # solo documentación: quien entra con invitación nace aprobado
│   └── Reglas/
│       ├── + ReglaInvitacionDelClub.cs      # qué roles admite una invitación del club
│       ├── ~ ReglaIngresoPorInvitacion.cs   # siempre aprobado; EsDelClub y PideResponsable
│       └── - ReglaAprobacionIngreso.cs      # ya no decide nada (research §6)
├── LFClub.Aplicacion/
│   ├── DTOs/              ~ InvitarAlClubDto (gana Rol), InvitacionClubDto (gana Rol),
│   │                        InvitacionVigenteDto (PideResponsable por PasaPorSalaDeEspera; gana
│   │                        FaltaResponsable, RF-026),
│   │                        AprobarIngresoDto (Rol opcional), RegistrarConInvitacionDto (documentación)
│   │                      + AceptarInvitacionDto   # token y responsable opcional (RF-026)
│   │                      - InvitarPresidenteDto
│   ├── Interfaces/        ~ IRepositorioIngresos (aprobar sin rol), IServicioCorreo (documentación)
│   ├── Servicios/         ~ IServicioInvitacionPresidente (pierde InvitarAsync)
│   ├── Implementaciones/  ~ ServicioInvitacionesClub, ServicioRegistroConInvitacion,
│   │                        ServicioAceptacionInvitacion, ServicioAprobacionIngreso,
│   │                        ServicioInvitacionPresidente (pierde InvitarAsync)
│   ├── Mappers/           ~ MapperIngresos
│   ├── Validadores/       ~ ValidadorRegistro   # recibe el rol de la invitación
│   └── Utilidades/        ~ ErroresDeInvitacion # rol_no_invitable
├── LFClub.Infraestructura/
│   ├── Correo/            ~ PlantillasCorreo    # nombra el rol en toda invitación
│   └── Repositorios/      ~ RepositorioIngresos
└── LFClub.Api/
    └── Controladores/
        ├── Club/          ~ ControladorInvitacionesClub (solo PRESIDENTE),
        │                    ControladorIngresos (solo PRESIDENTE; cuerpo de aprobar opcional)
        ├── Cuenta/        ~ ControladorInvitaciones (solo documentación)
        └── Plataforma/    ~ ControladorInvitacionesClub (pierde Invitar; conserva Reenviar)

backend/pruebas/
├── Unitarias/
│   ├── Reglas/            + ReglaInvitacionDelClubPruebas
│   │                      ~ ReglaIngresoPorInvitacionPruebas
│   │                      - ReglaAprobacionIngresoPruebas
│   ├── Validadores/       ~ ValidadorRegistroPruebas
│   └── Correo/            ~ PlantillasCorreoPruebas
└── Integracion/
    ├── Ingresos/          + SoloPresidenteIngresosPruebas    # los ocho endpoints con cada rol
    │                      + InvitacionConRolPruebas          # los tres roles, sin rol, rol no invitable
    │                      + RegistroDirectoPruebas           # entra aprobado y con su rol
    │                      + RegistroDirectoUbicacionPruebas  # categoría del jugador al registrarse
    │                      - RegistroEnEsperaPruebas          # lo sustituye la anterior
    │                      ~ InvitacionesClubPruebas, ReenvioYCancelacionPruebas, AprobacionPruebas,
    │                        SalaDeEsperaPruebas, RechazoPruebas, ConsultaIngresosPruebas,
    │                        AislamientoIngresosPruebas, IngresosPorEstadoDelClubPruebas
    ├── Cuenta/            ~ RegistroConInvitacionPruebas, AceptacionInvitacionPruebas
    │                      + AceptacionDirectaPruebas         # ubicación y club suspendido al aceptar
    │                      + AceptacionResponsablePruebas     # menor sin responsable (RF-026)
    ├── Categorias/        ~ UbicacionAutomaticaPruebas, PersonaRetiradaPruebas
    ├── Plataforma/        ~ InvitacionesPruebas   # sin invitar a un club que ya existe
    └── Aislamiento/       ~ AccesoClubPruebas     # 54 endpoints en los contratos

frontend/src/
├── compartido/api/        ~ tipos.ts
├── cuenta/
│   ├── ~ Invitacion.tsx                     # el subtítulo nombra siempre el rol
│   ├── ~ FormularioRegistro.tsx             # responsable solo si pideResponsable; sin aviso de espera
│   └── ~ AceptarInvitacion.tsx              # nombra el rol; sin aviso de espera; responsable si falta
├── plataforma/
│   └── ~ SeccionInvitaciones.tsx            # sin "Invitar a otro presidente"
└── privado/
    ├── ~ DisposicionClub.tsx                # el enlace "Ingresos" solo para el PRESIDENTE
    └── ingresos/
        ├── ~ SeccionInvitacionesClub.tsx    # selector de rol y columna "Rol"
        ├── ~ SeccionSalaDeEspera.tsx        # aprobar con una confirmación simple
        ├── ~ Ingresos.tsx                   # deja de pasar el rol propio
        └── - DialogoAprobarIngreso.tsx      # solo existía para elegir rol
```

**Decisión de estructura**: la de la 001, sin carpetas ni proyectos nuevos. Las pruebas de
integración nuevas van en archivos propios para que `InvitacionesClubPruebas` (230 líneas) no
supere el límite.

### Pantallas

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| Menú del club | El enlace "Ingresos" deja de mostrarse al DIRECTIVO | PRESIDENTE |
| `/club/:clubId/ingresos` → Invitaciones | Selector de rol obligatorio (Jugador, Entrenador, Directivo) y columna "Rol" | PRESIDENTE |
| `/club/:clubId/ingresos` → Sala de espera | Aprobar es una confirmación, sin elegir rol | PRESIDENTE |
| `/invitacion#<token>` | Muestra siempre el rol; pide el responsable solo al JUGADOR; desaparece el aviso de aprobación pendiente | Persona invitada |
| `/invitacion#<token>` con cuenta existente | Nombra el rol; al aceptar entra directamente. Pide el responsable solo al JUGADOR menor de 18 años cuya cuenta no lo tiene (RF-026) | Persona invitada con cuenta |
| `/club/:clubId` tras registrarse | La aplicación del club en lugar de la pantalla de espera (sin código nuevo) | Persona recién registrada |
| Detalle de un club en el panel de la plataforma | Desaparece "Invitar a otro presidente"; quedan la lista de invitaciones sin usar, reenviar y corregir el correo | DESARROLLADOR |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. El presidente invita con rol | RF-001 a 004 y 006 | `ReglaInvitacionDelClub`, `ServicioInvitacionesClub`, `ControladorInvitacionesClub`, `PlantillasCorreo` | Menú, formulario y tabla de invitaciones | Cada uno de los tres roles; sin rol; PRESIDENTE y DESARROLLADOR como rol; DIRECTIVO, ENTRENADOR y JUGADOR reciben `403` en los cuatro endpoints; reenviar conserva el rol; invitar de nuevo lo cambia; el correo nombra el rol |
| 2. Ingreso directo | RF-008 a 014 y 026 | `ReglaIngresoPorInvitacion`, `ServicioRegistroConInvitacion`, `ServicioAceptacionInvitacion`, `ValidadorRegistro`, `UbicadorDeJugadores` | Enlace de invitación, registro y aceptación | Cada rol entra aprobado y con ese único rol; acceso inmediato al club; JUGADOR con y sin categoría; registrarse mientras se crea la categoría; ENTRENADOR y DIRECTIVO sin datos de jugador; responsable por rol y edad; rol del cuerpo ignorado; cuenta existente; menor con cuenta sin responsable; enlaces que ya no sirven |
| 3. Sala de espera del presidente, sin rol | RF-015 a 019 | `ServicioAprobacionIngreso`, `RepositorioIngresos`, `ControladorIngresos` | Sala de espera | DIRECTIVO, ENTRENADOR y JUGADOR reciben `403` en los cuatro endpoints; aprobar deja JUGADOR y ubica; otro rol, `403`; rechazo igual; sala vacía tras los registros; aprobados anteriores intactos; quien entra con invitación no aparece en aprobados |
| 4. El desarrollador solo invita al crear el club | RF-024, RF-025 | `ControladorInvitacionesClub` de la plataforma, `ServicioInvitacionPresidente` | Detalle del club en el panel | La ruta de invitar ya no existe; crear un club sigue invitando; reenviar y corregir el correo siguen funcionando |
| Transversal | RF-020 a 023 | Filtro global y estado del club | Todas, a 360 px y en los dos temas | Otro club recibe `404`; suspendido y dado de baja; el contrato pasa a 54 endpoints |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: `ReglaInvitacionDelClub` y el cambio de `ReglaIngresoPorInvitacion`, con sus pruebas
   unitarias; error nuevo del contrato.
2. **Historia 1**: solo el PRESIDENTE en las invitaciones; invitar con rol, lista con rol, reenviar
   conservándolo, correo; con el menú, su formulario y su tabla.
3. **Historia 2**: registro y aceptación directos con ubicación del jugador, responsable según el
   rol; con la pantalla del enlace, el registro y la aceptación. Aquí se reescriben las pruebas que
   esperaban la sala de espera.
4. **Historia 3**: solo el PRESIDENTE en los ingresos; aprobación sin rol; se elimina
   `ReglaAprobacionIngreso`; la sala de espera con su confirmación simple.
5. **Historia 4**: se elimina invitar a un presidente a un club que ya existe, con su formulario;
   se retira la ruta del contrato de la 001 y la prueba de contrato pasa a 54.
6. **Cierre**: estados del club, revisión a 360 px en los dos temas, contrato frente a Swagger,
   documentación XML de las clases que cambiaron de responsabilidad y recorrido del quickstart.

Las historias 1 y 2 son el mínimo utilizable y van juntas: con solo la 1, una invitación de
ENTRENADOR dejaría a la persona en la sala de espera. No se debe publicar nada entre una y otra.

## Cambios sobre lo ya construido en la 001, la 002 y la 003

Son los puntos donde hay que cuidar la no regresión (§27.2):

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| Los ocho endpoints de invitaciones e ingresos admiten solo al PRESIDENTE | RF-002, RF-017 | Prueba nueva que los recorre con DIRECTIVO, ENTRENADOR y JUGADOR; las pruebas de la 002 que actuaban como DIRECTIVO pasan a actuar como PRESIDENTE |
| El menú deja de mostrar "Ingresos" al DIRECTIVO | RF-015 | Recorrido del quickstart; la protección real es la del servidor |
| `POST …/invitaciones` exige `rol` | RF-001 | Todas las pruebas que invitan pasan a indicar rol; prueba nueva del `400` sin rol |
| `InvitacionVigenteDto` cambia `pasaPorSalaDeEspera` por `pideResponsable` | RF-006, RF-013 | Se actualizan a la vez el DTO, `tipos.ts` y las tres pantallas que lo leían; `tsc` falla si queda algún uso |
| Registrarse con una invitación del club deja `APROBADO` | RF-008 | Las pruebas que partían de "se registra y queda en espera" siembran al integrante en espera con `Sembrador.CrearIntegranteEnEsperaAsync` |
| El registro y la aceptación fijan el club en el contexto | RF-011 | Las pruebas de aislamiento de la 001 siguen en verde; prueba nueva de que la respuesta del registro no trae datos del club |
| El registro y la aceptación bloquean la fila del club | Caso límite "mientras se crea la categoría" | Prueba de concurrencia nueva, con el mismo patrón de la 003 |
| El responsable deja de pedirse fuera del JUGADOR, también al PRESIDENTE | RF-013, supuesto 1 | `ValidadorRegistroPruebas` por rol; las pruebas de registro de presidente dejan de enviarlo |
| La aprobación deja siempre JUGADOR y su cuerpo es opcional | RF-016 | Se reescriben las pruebas que elegían otro rol y la del `400` sin rol; las de doble aprobación y simultaneidad siguen igual |
| Se elimina `ReglaAprobacionIngreso` | Ya no decide nada | El compilador señala cualquier uso que quede; sus pruebas unitarias se eliminan con ella |
| El correo de la invitación del club nombra el rol | RF-006 | `PlantillasCorreoPruebas` |
| Se elimina `POST /api/plataforma/clubes/{clubId}/invitaciones` y el formulario "Invitar a otro presidente" | RF-024 | `InvitacionesPruebas` de la plataforma pierde los casos de invitar y gana el de que la ruta no existe; los de reenviar y corregir siguen igual; crear club conserva sus pruebas |
| `AccesoClubPruebas` espera 54 endpoints en lugar de 55 | Se elimina uno y no hay nuevos | La ruta se retira del contrato de la 001 en la misma tarea que elimina el endpoint. El contrato de la 004 solo repite rutas existentes, que la prueba no cuenta dos veces |

## Seguimiento de complejidad

| Desviación | Por qué hace falta | Alternativa más simple descartada |
| --- | --- | --- |
| §11.2 escribe `Jugador.CategoriaId`; la categoría sigue en `UsuarioRol.CategoriaId` | Heredada de la 003: la entidad `Jugador` no existe todavía y la spec deja la ficha del jugador fuera de alcance | Crear ahora la tabla `Jugador` adelanta esa funcionalidad (§19, §25) |

## Consecuencia que conviene tener presente

Con esta funcionalidad nadie puede sumar un segundo presidente a un club: el DESARROLLADOR ya no
invita presidentes adicionales y "un PRESIDENTE elige a otro" (§12.5) todavía no está construido.
Hasta que lo esté, cada club tiene un solo presidente, y quitarle el rol a un presidente desde el
panel queda sin uso, porque exige que el club conserve otro.

## Supuestos del plan

Detallados al final de [research.md](research.md). Ya no queda ninguno por confirmar:

1. El responsable deja de pedirse también en el registro de un PRESIDENTE. Confirmado por el
   propietario el 2026-10-09 (RF-013).
2. Un responsable enviado con una invitación de ENTRENADOR o DIRECTIVO se ignora, sin error.
   Cubierto por RF-013 ("no pedirlo ni guardarlo") y RF-026.
3. Quien ya tiene cuenta y acepta una invitación de JUGADOR no ve ningún formulario. Sustituido
   el 2026-10-09 por RF-026: se le pide el responsable si es menor de 18 años y su cuenta no lo
   tiene.
4. La aprobación admite `rol: JUGADOR` o ningún rol; cualquier otro responde `403`. Cubierto por
   RF-016.
