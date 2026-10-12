# Plan de implementación: Ingreso de personas al club

**Rama**: `002-ingreso-club` | **Fecha**: 2026-10-07 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/002-ingreso-club/spec.md`

## Resumen

El club empieza a recibir personas: el PRESIDENTE y los DIRECTIVOS invitan por correo, la persona
se registra por el enlace y queda en la sala de espera, y los mismos dos roles aprueban el ingreso
(eligiendo el rol) o lo rechazan (borrándola del club).

Enfoque técnico: no se crea ninguna tabla ni se añade ninguna tecnología. Se reutilizan la
invitación, el registro y la aceptación de la 001; el estado `EN_ESPERA`, que ya existía sin
usarse, empieza a asignarse; y la sala de espera se impone en el atributo que ya autoriza todos
los endpoints del club, de modo que niega por defecto cualquier operación a una cuenta en espera.
La aprobación y el rechazo son sentencias condicionadas al estado, para que entre dos acciones
simultáneas valga la primera. Las decisiones y sus alternativas están en
[research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite, Brevo por HTTP). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Una migración: una columna en `Usuarios`, cuatro columnas y un
índice en `UsuariosRol`.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: respuestas de la API por debajo de 300 ms en el percentil 95.
Invitar y aprobar en menos de 30 segundos de uso (CE-001, CE-003): una pantalla y una acción.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano
por encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15);
backend y pantalla de cada historia en la misma tarea (§27.1); no romper nada de la 001 (§27.2).

**Escala y alcance**: decenas de personas por club (§19); las listas no se paginan. Esta
funcionalidad tiene 0 entidades nuevas, 8 operaciones de API nuevas, 5 que cambian y 2 pantallas
nuevas más 3 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | Ingreso, sala de espera, invitación, responsable; sin "Acudiente" como entidad ni rol | Cumple |
| §2.3 Máximo 250 líneas | Un servicio por caso de uso y un controlador por recurso; el script de la 001 lo verifica | Cumple |
| §3 Documentación XML | En cada clase nueva y actualizada en las que cambian de responsabilidad | Cumple |
| §4 y §5 Capas y dependencias | Controlador → IServicio → IRepositorio → EF Core; reglas en `Dominio/Reglas` | Cumple |
| §6 Tecnologías base | Ninguna nueva | Cumple |
| §7.1 Aislamiento entre clubes | Los repositorios nuevos trabajan con el filtro global activo; no hay excepciones nuevas. Las tres de la 001 se reutilizan sin ampliarlas | Cumple |
| §7.3 Pertenencia a varios clubes | El estado de ingreso es de cada `UsuarioRol` | Cumple |
| §7.4 Suspensión y baja | Suspendido: solo el PRESIDENTE opera y los enlaces siguen sirviendo. Dado de baja: nada funciona | Cumple |
| §8 Roles | Invitan, aprueban y rechazan PRESIDENTE y DIRECTIVO; el DIRECTIVO solo asigna ENTRENADOR y solo al aprobar; el DESARROLLADOR no ve nada del club | Cumple |
| §10 Documento | Único por documento + club y un documento en una sola cuenta, como en la 001 | Cumple |
| §12.1 Registro por invitación | Sin registro abierto; un uso, 7 días, correo y club fijos; responsable obligatorio para menores | Cumple |
| §12.1.1 Sala de espera | Negada por defecto en la autorización; aprobar y rechazar según la spec | Cumple, con la nota 1 |
| §12.2 Asignación de roles | El rol elegido al aprobar reemplaza a JUGADOR | Cumple, con la nota 1 |
| §13 Histórico | La aprobación guarda copia del nombre de quien aprobó y del rol de ingreso | Cumple |
| §14 Eliminación física | Solo el rechazo, que §14 declara justificado | Cumple |
| §15 Reglas en backend | Roles, estado de ingreso, mayoría de edad y correo fijo se validan en la API | Cumple |
| §18 Sin historial de estados | Se guarda solo el estado actual y los datos de la aprobación | Cumple |
| §19 Simplicidad | Sin tablas nuevas; columnas en `UsuarioRol` en lugar de una entidad de aprobación | Cumple |
| §20 Pruebas | Cubre las seis pruebas que la versión 3.7.0 añadió para esta funcionalidad | Cumple |
| §22 Migraciones | Una migración de EF Core | Cumple |
| §23 API | HTTP semántico, DTOs propios, `problem+json`, Swagger | Cumple |
| §24 Diseño visual | Componentes y temas de la 001; estados con texto además de color | Cumple |
| §25 Decisiones no tomadas | Los detalles sin fijar están como supuestos en research.md; ninguno inventa una regla de negocio | Cumple |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna. "Ficha con historial" no se toca: nadie en espera tiene historial | Cumple |

**Resultado de la puerta**: pasa. No hay violaciones que justificar.

**Nota 1**: §12.1.1 dice que al aprobar a un jugador se le asigna categoría y empieza su
mensualidad, y §12.2 que la ficha de Jugador se elimina al pasar a ENTRENADOR o DIRECTIVO. Las
entidades Categoría, Cargo y Jugador todavía no existen. La spec lo deja expresamente a sus
funcionalidades (supuestos de la spec); este plan no adelanta nada de ellas.

**Revisión tras el diseño**: el modelo de datos y el contrato no añaden tablas, excepciones al
aislamiento ni patrones respecto de lo evaluado. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/002-ingreso-club/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Cambios del modelo, reglas y transiciones
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
│   │   ├── ~ Usuario.cs                    # NombreResponsable
│   │   ├── ~ UsuarioRol.cs                 # datos de la aprobación
│   │   └── ~ Invitacion.cs                 # EstadoEn(ahora)
│   ├── Enumeraciones/
│   │   └── + EstadoInvitacion.cs
│   └── Reglas/
│       ├── ~ ReglaAccesoPorEstado.cs       # también el estado de ingreso
│       ├── ~ ResultadoAcceso.cs            # IngresoEnEspera
│       ├── + ReglaIngresoPorInvitacion.cs  # PRESIDENTE entra aprobado; el resto, en espera
│       ├── + ReglaAprobacionIngreso.cs     # qué rol puede asignar cada quien
│       └── + ReglaMayoriaDeEdad.cs
├── LFClub.Aplicacion/
│   ├── DTOs/              + InvitarAlClubDto, InvitacionClubDto, IngresoEnEsperaDto,
│   │                        AprobarIngresoDto, IngresoAprobadoDto
│   │                      ~ ClubDeSesionDto, InvitacionVigenteDto, RegistrarConInvitacionDto
│   ├── Interfaces/        + IRepositorioInvitacionesClub, IRepositorioIngresos
│   │                      ~ IRepositorioPertenencias, IServicioCorreo
│   ├── Servicios/         + IServicioInvitacionesClub, IServicioConsultaIngresos,
│   │                        IServicioAprobacionIngreso, IServicioRechazoIngreso
│   ├── Implementaciones/  + las cuatro implementaciones anteriores
│   │                      ~ ServicioRegistroConInvitacion, ServicioAceptacionInvitacion,
│   │                        ServicioInvitacionPresidente, ServicioRetiroPresidente
│   ├── Mappers/           + MapperIngresos   ~ MapperSesion
│   ├── Validadores/       ~ ValidadorRegistro
│   └── Utilidades/        + construcción compartida de invitaciones; eliminación compartida
│                            de la cuenta que se queda sin club   ~ ErroresDeInvitacion
├── LFClub.Infraestructura/
│   ├── Datos/
│   │   ├── Configuraciones/   ~ ConfiguracionUsuario, ConfiguracionUsuarioRol
│   │   └── Migraciones/       + IngresoAlClub
│   ├── Repositorios/
│   │   ├── + RepositorioInvitacionesClub.cs   # dentro del club, con el filtro activo
│   │   ├── + RepositorioIngresos.cs           # dentro del club, con el filtro activo
│   │   └── Plataforma/
│   │       └── ~ RepositorioInvitacionesPlataforma.cs   # solo invitaciones de presidente
│   └── Correo/                ~ PlantillasCorreo        # la invitación del club no nombra rol
└── LFClub.Api/
    ├── Autorizacion/      ~ IntegranteDelClubAttribute  # varios roles; niega a quien está en espera
    └── Controladores/Club/
        ├── + ControladorInvitacionesClub.cs
        └── + ControladorIngresos.cs

backend/pruebas/
├── Unitarias/Reglas/      + ReglaAprobacionIngreso, ReglaMayoriaDeEdad, ReglaIngresoPorInvitacion
│                          ~ ReglaAccesoPorEstado
├── Unitarias/             + estado derivado de la invitación; ValidadorRegistro con responsable
└── Integracion/
    ├── + Ingresos/        # InvitacionesClub, RegistroEnEspera, SalaDeEspera, Aprobacion,
    │                      # Rechazo, AislamientoIngresos, IngresosPorEstadoDelClub
    └── ~ Base/Sembrador   # integrantes en espera y directivos

frontend/src/
├── compartido/
│   ├── api/               ~ tipos.ts
│   └── sesion/            ~ ultimoClub.ts               # prefiere un club con ingreso aprobado
├── cuenta/                ~ Invitacion.tsx, FormularioRegistro.tsx, AceptarInvitacion.tsx
└── privado/
    ├── ~ DisposicionClub.tsx                            # sala de espera y enlace "Ingresos"
    ├── ~ DesplegableClubes.tsx                          # indica los clubes en espera
    ├── + SalaDeEspera.tsx
    └── + ingresos/
        ├── Ingresos.tsx
        ├── SeccionSalaDeEspera.tsx
        ├── DialogoAprobarIngreso.tsx
        ├── SeccionInvitacionesClub.tsx
        └── SeccionIngresosAprobados.tsx

frontend/pruebas/          ~ ultimoClub.test.ts
```

**Decisión de estructura**: la de la 001, sin carpetas de primer nivel nuevas. En el frontend las
pantallas del apartado van en `privado/ingresos/` para que ningún archivo se acerque a las 250
líneas.

### Pantallas

| Ruta | Pantalla | Quién |
| --- | --- | --- |
| `/club/:clubId/ingresos` | "Ingresos": sala de espera (aprobar y rechazar), invitaciones (enviar, reenviar, cancelar) e ingresos aprobados (solo lectura) | PRESIDENTE y DIRECTIVO |
| `/club/:clubId` (ingreso en espera) | Sala de espera de la persona: nombre e identidad del club, "tu ingreso está pendiente de aprobación", actualizar, tema, cambiar de club y cerrar sesión | Cuenta en espera |
| `/invitacion#<token>` (cambia) | Registro sin rol, con el campo del responsable y la explicación de qué documento usar; o aceptación si ya tiene cuenta | Invitado |
| Menú del club (cambia) | Enlace "Ingresos" | PRESIDENTE y DIRECTIVO |
| Desplegable de clubes (cambia) | Marca los clubes en los que la persona está en espera | Quien tiene varios clubes |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. El club invita | RF-001 a 008, 028, 029 | `ControladorInvitacionesClub`, `ServicioInvitacionesClub`, repositorio del club; el de plataforma, limitado a presidentes | Sección de invitaciones | Solo PRESIDENTE y DIRECTIVO; reenviar anula la anterior; cancelar; correos rechazados; otro club recibe `404`; el DESARROLLADOR no las ve |
| 2. Registro y sala de espera | RF-009 a 019 | Registro y aceptación con `EN_ESPERA`; responsable; atributo de autorización; `estadoIngreso` en la sesión | `/invitacion`, sala de espera | Sin invitación no hay registro; un uso, vencida, otro correo; queda en espera; `403` en todos los endpoints del club; menor sin responsable; aprobada en un club y en espera en otro |
| 3. Aprobación | RF-020 a 026, 025a | `ControladorIngresos`, `ServicioConsultaIngresos`, `ServicioAprobacionIngreso` | Sala de espera del club, diálogo de aprobar, ingresos aprobados | Quién aprueba; roles que asigna cada uno; nunca PRESIDENTE; dos aprobaciones a la vez; sesión abierta pasa a aprobada |
| 4. Rechazo | RF-027, 027a, 027b | `ServicioRechazoIngreso` | Botón "Rechazar" con confirmación | Borra integrante e invitaciones; conserva otros clubes; borra la cuenta sin clubes; no rechaza aprobados; vuelve solo con invitación nueva |
| Transversal | RF-030, 031 | Reglas de estado del club | Todas, a 360 px y en los dos temas | Suspendido: solo el presidente opera y el enlace sirve; dado de baja: nada funciona |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: migración, columnas, enumeración y reglas de dominio con sus pruebas unitarias;
   atributo de autorización con varios roles y negación a quien está en espera; `estadoIngreso`
   en la sesión.
2. **Historia 1**: invitar, listar, reenviar y cancelar desde el club, con su sección.
3. **Historia 2**: registro y aceptación que dejan en espera, responsable, pantalla de sala de
   espera.
4. **Historia 3**: sala de espera del club, aprobación y lista de aprobados.
5. **Historia 4**: rechazo.
6. **Cierre**: estados del club, revisión a 360 px en los dos temas, contrato frente a Swagger y
   recorrido del quickstart.

## Cambios sobre lo ya construido en la 001

Son los puntos donde hay que cuidar la no regresión (§27.2):

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| `IntegranteDelClubAttribute` niega a quien está en espera | RF-016 | Las pruebas de acceso de la 001 siguen en verde; todos sus integrantes son `APROBADO` |
| `RepositorioInvitacionesPlataforma` filtra por rol `PRESIDENTE` | RF-029 | Las pruebas de invitaciones del panel siguen en verde; prueba nueva con una invitación del club presente |
| Aceptar una invitación del club siendo ya integrante responde `409` | No degradar a nadie | Prueba nueva; la sustitución de rol con invitación de presidente conserva su prueba |
| `ValidadorRegistro` exige responsable a menores | RF-010 | Los registros de prueba de la 001 son de adultos |
| La plantilla del correo de invitación depende del rol | RF-003 | Prueba del texto para presidente y para el club |
| `EndpointsDeLaApi.DelContrato()` lee los contratos de todas las specs | La prueba de `AccesoClubPruebas` compara los endpoints de la API con el contrato y hoy solo lee el de la 001 | Une `specs/*/contracts/api.yaml` sin repetir los endpoints que el contrato 002 vuelve a describir |

## Seguimiento de complejidad

No hay violaciones de la constitución ni ajustes que justificar.

## Supuestos pendientes de confirmar

Están detallados al final de [research.md](research.md). Ninguno bloquea las tareas:

1. La lista de invitaciones muestra una fila por correo, la más reciente; una reemplazada por un
   reenvío no aparece.
2. Una cuenta en espera de un club suspendido ve el aviso de incidencia temporal, no la sala de
   espera.
3. "Menor de 18 años" se calcula con la fecha UTC del servidor.
4. Las invitaciones de presidente a un club dado de baja siguen como en la 001.
5. El responsable admite 160 caracteres y no se vuelve a pedir a quien ya tiene cuenta.
6. La lista de ingresos aprobados no incluye a los presidentes invitados por el DESARROLLADOR.
