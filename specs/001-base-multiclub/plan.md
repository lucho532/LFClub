# Plan de implementación: Base multiclub y panel de administración de la plataforma

**Rama**: `001-base-multiclub` | **Fecha**: 2026-10-07 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/001-base-multiclub/spec.md`

## Resumen

Se construye la base sobre la que se apoya todo lo demás: clubes aislados entre sí, cuentas con
un rol por club, el panel desde el que el DESARROLLADOR crea y administra clubes, el registro del
presidente por invitación, la identidad visual de cada club y los temas claro y oscuro.

Enfoque técnico: una API en ASP.NET Core por capas sobre PostgreSQL, con una sola base de datos y
aislamiento por `ClubId` aplicado con un filtro global de Entity Framework; y una única aplicación
React que contiene el panel de la plataforma y la aplicación del club. El club elegido va en la
ruta y la pertenencia y el estado del club se comprueban en el servidor en cada petición. Las
decisiones y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 (LTS) en el backend; TypeScript 5 sobre Node 22 en el
frontend.

**Dependencias principales**: ASP.NET Core 10 Web API, Entity Framework Core 10 con Npgsql,
autenticación JWT de ASP.NET Core, `PasswordHasher` de `Microsoft.AspNetCore.Identity`,
Swashbuckle; React 19, React Router y Vite. Brevo por HTTP, sin SDK.

**Almacenamiento**: PostgreSQL 17. El escudo de cada club y la foto de perfil de cada cuenta se
guardan en la base de datos.

**Pruebas**: xUnit (unitarias e integración); integración con `WebApplicationFactory` y
PostgreSQL en contenedor (Testcontainers); Vitest para las utilidades del frontend.

**Plataforma de destino**: API en contenedor Linux; aplicación web adaptable para navegadores
actuales de teléfono y escritorio. El empaquetado Android (Capacitor) y la PWA quedan fuera de
esta funcionalidad.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: respuestas de la API por debajo de 300 ms en el percentil 95;
cambio de club en menos de 5 segundos y cambio de tema en menos de 1 segundo (CE-006).

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano
por encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15);
backend y pantalla de cada funcionalidad en la misma tarea (§27.1).

**Escala y alcance**: decenas de clubes con decenas de jugadores cada uno (§19). Esta
funcionalidad tiene 7 entidades, 25 operaciones de API y 10 pantallas.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 Código en español | Entidades, DTOs, servicios, rutas y componentes en español | Cumple, con la nota 2 |
| §2.2 Vocabulario | Club, Presidente, Desarrollador, Directivo, Entrenador, Jugador; sin sinónimos | Cumple |
| §2.3 Máximo 250 líneas | Controladores y servicios divididos por caso de uso; una tarea lo verifica con un script | Cumple |
| §3 Documentación XML | Exigida en cada clase; forma parte de la definición de terminado | Cumple |
| §4 Arquitectura por capas | Cuatro proyectos `LFClub.*` y `backend/pruebas`; `frontend/` separado | Cumple, con la nota 1 |
| §5 Flujo de dependencias | Controlador → IServicio → IRepositorio → EF Core; entidad → mapper → DTO | Cumple |
| §6 Tecnologías base | Solo las listadas, más las de la tabla de seguimiento | Cumple, con las notas 3 y 4 |
| §7.1 Aislamiento entre clubes | Filtro global por `ClubId`, falla cerrado, con prueba que vigila el modelo. Las tres excepciones de §7.1 viven en `Repositorios/Plataforma/` | Cumple |
| §7.2 Identidad y configuración | En base de datos; el código no menciona a ningún club | Cumple |
| §7.3 y §12.3 Pertenencia a varios clubes | `UsuarioRol` por club; desplegable; pertenencia comprobada por petición | Cumple |
| §7.4 Suspensión, baja, eliminación | Estados y transiciones del modelo de datos; cascada al eliminar | Cumple (solo manual; el impago está fuera de alcance) |
| §8 Roles y alcance del DESARROLLADOR | Cuenta única sin integrante; `/api/plataforma` exclusivo; `404` en endpoints de club | Cumple |
| §10 Documento | Único por documento + club; un documento pertenece a una sola cuenta; claves internas `Guid` | Cumple |
| §12.1 y §12.5 Registro por invitación | Sin registro abierto; invitación de un uso, 7 días, ligada a club, rol y correo | Cumple |
| §12.4 Inicio de sesión | Correo o documento; bloqueo al quinto fallo; recuperación por correo | Cumple |
| §14 Entidades desactivables | Las dos eliminaciones físicas (integrante retirado, club eliminado) las ordena la spec | Cumple, justificado |
| §15 Reglas en backend | Autorización, estados y último presidente se validan en la API | Cumple |
| §18 Sin historial de estados | Solo se guarda el último cambio de estado del club | Cumple |
| §19 Simplicidad | Sin colas, sin caché, sin Identity completo, sin librerías de validación ni de mapeo | Cumple |
| §20 Pruebas | Unitarias e integración para cada regla listada que toca esta funcionalidad | Cumple |
| §21 y §22 Docker y migraciones | `docker-compose.yml` con base de datos, API y frontend; migraciones de EF | Cumple |
| §23 API | HTTP semántico, DTOs, validación, `problem+json`, Swagger | Cumple |
| §24 Diseño visual | Estilo del lienzo; tema claro y oscuro; estados con texto además de color | Cumple |
| §25 Decisiones no tomadas | Los detalles sin fijar están marcados como supuestos en research.md | Cumple |
| §27 Definición de terminado | Las tareas se cortarán por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | Ninguna se resuelve aquí; ver el supuesto 6 sobre el escudo | Cumple |

**Resultado de la puerta**: pasa. Las cuatro notas están justificadas en "Seguimiento de
complejidad".

**Revisión tras el diseño**: el modelo de datos y el contrato no introducen entidades, tablas ni
patrones nuevos respecto de lo evaluado. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/001-base-multiclub/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Entidades, reglas y transiciones
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Contrato OpenAPI
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

```text
backend/
├── LFClub.sln
├── src/
│   ├── LFClub.Api/
│   │   ├── Controladores/
│   │   │   ├── Plataforma/        # clubes, identidad, estado, invitaciones, presidentes
│   │   │   ├── Club/              # club elegido y su configuración
│   │   │   ├── Publico/           # escudo
│   │   │   └── Cuenta/            # sesión, recuperación, invitaciones, foto de perfil
│   │   ├── Autorizacion/          # solo DESARROLLADOR; integrante del club de la ruta
│   │   ├── Errores/               # traducción de errores a problem+json
│   │   └── Program.cs
│   ├── LFClub.Aplicacion/
│   │   ├── DTOs/
│   │   ├── Interfaces/            # IServicio*, IRepositorio*, IServicioCorreo, IHashContrasena,
│   │   │                          # IContextoClub, IUnidadDeTrabajo, IReloj
│   │   ├── Servicios/             # contratos de los servicios
│   │   ├── Implementaciones/      # un servicio por caso de uso
│   │   ├── Mappers/
│   │   ├── Validadores/
│   │   └── Utilidades/            # normalización de correo, nombre y documento; tokens
│   ├── LFClub.Dominio/
│   │   ├── Entidades/             # Club, EscudoClub, Usuario, FotoPerfil, UsuarioRol,
│   │   │                          # Invitacion, SolicitudRecuperacion, IPerteneceAClub
│   │   ├── Enumeraciones/
│   │   └── Reglas/                # transiciones de estado, último presidente, acceso por estado
│   └── LFClub.Infraestructura/
│       ├── Datos/                 # contexto, configuraciones, migraciones, cuenta inicial
│       ├── Repositorios/
│       │   └── Plataforma/        # los únicos que cruzan clubes
│       ├── Correo/                # Brevo y la variante de desarrollo
│       └── Seguridad/             # hash de contraseñas y emisión de tokens
└── pruebas/
    ├── Unitarias/                 # LFClub.Pruebas.Unitarias
    └── Integracion/               # LFClub.Pruebas.Integracion

frontend/
├── index.html                     # aplica el tema antes de pintar
├── src/
│   ├── compartido/
│   │   ├── api/                   # cliente de la API y tipos del contrato
│   │   ├── sesion/                # contexto de sesión, rutas protegidas
│   │   ├── tema/                  # tema claro y oscuro, colores del club, contraste
│   │   └── componentes/           # botón de tema, formularios, avisos, tablas
│   ├── cuenta/                    # entrar, recuperar, restablecer, invitación, mi perfil
│   ├── plataforma/                # panel del DESARROLLADOR
│   └── privado/                   # aplicación del club: inicio, configuración, desplegable
└── pruebas/                       # Vitest

docker-compose.yml                 # base de datos, API y frontend
.env.ejemplo
```

**Decisión de estructura**: aplicación web con `backend/` y `frontend/` separados, tal como fija
§4. El backend conserva los cuatro proyectos y las carpetas de la constitución, y añade
`Infraestructura/Seguridad` y las carpetas de la API para autorización y errores. El frontend es
una sola aplicación React (§6.1) cuyas partes viven en `frontend/src/`.

### Pantallas

| Ruta | Pantalla | Quién |
| --- | --- | --- |
| `/entrar` | Inicio de sesión | Sin sesión |
| `/recuperar` | Pedir recuperación de contraseña | Sin sesión |
| `/restablecer#<token>` | Crear contraseña nueva | Sin sesión |
| `/invitacion#<token>` | Registro con invitación, o aceptación si ya tiene cuenta | Invitado |
| `/perfil` | Mi perfil: foto de perfil | Cualquier cuenta con sesión |
| `/plataforma` | Lista de clubes y creación | DESARROLLADOR |
| `/plataforma/clubes/:clubId` | Detalle: datos, identidad, presidentes, invitaciones, estado | DESARROLLADOR |
| `/club/:clubId` | Inicio del club, con su identidad | Integrante |
| `/club/:clubId/configuracion` | Datos del club | PRESIDENTE |
| `/club/:clubId` (club no disponible) | Aviso de incidencia temporal o de club no disponible | Integrante |

La raíz `/` redirige según la sesión: al panel, al único club o al último club elegido. El botón
de tema y, para quien tiene varios clubes, el desplegable están en la cabecera de todas las
pantallas con sesión; el botón de tema está también en las pantallas sin sesión.

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Crear club e invitar | RF-001, 003, 004, 006, 007, 011, 012, 019a | `Plataforma/` clubes, invitaciones y presidentes | `/plataforma`, detalle | Solo el DESARROLLADOR entra; club sin correo no se crea; último presidente |
| 2. Registro y acceso | RF-002, 005, 005a, 013 a 022 | `Cuenta/` sesión, recuperación e invitaciones | `/entrar`, `/recuperar`, `/restablecer`, `/invitacion` | Sin invitación no hay registro; bloqueo; correo y documento únicos; segunda pertenencia |
| 3. Identidad | RF-008, 009, 023 | Colores y escudo | Detalle del club; cabecera del club | Solo el DESARROLLADOR cambia identidad; contraste |
| 4. Datos del club | RF-010, 024, 025 | `Club/` configuración | `/club/:clubId/configuracion` | Presidente de otro club recibe `404`; otro rol, `403` |
| 5. Estados | RF-026 a 031 | Estado y eliminación | Detalle del club; avisos | Quién entra en cada estado; sesiones abiertas; eliminar no toca otros clubes |
| 6. Temas | RF-032 a 035 | Ninguno | Todas | Persistencia y tema del dispositivo |
| 7. Foto de perfil | RF-036 a 038 | `Cuenta/` foto | `/perfil`; cabecera | Formato y tamaño; nadie obtiene la foto de otra cuenta |

## Seguimiento de complejidad

Ninguna es una violación de un principio; son ajustes que la constitución pide justificar.

| # | Ajuste | Por qué hace falta | Alternativa más simple descartada |
| --- | --- | --- | --- |
| 1 | `frontend/src/` con `plataforma/`, `cuenta/` y `compartido/` además de `publico/` y `privado/` | El panel del DESARROLLADOR es una tercera parte (§1) y las tres comparten sesión, tema y cliente de la API | Solo `publico/` y `privado/`: obligaría a meter el panel dentro de la zona de un club, de la que el DESARROLLADOR está excluido |
| 2 | Los hooks de React empiezan por `use` (`useSesion`, `useTema`) | React y sus reglas de análisis lo exigen | `usarSesion`: desactiva las comprobaciones de hooks |
| 3 | Vitest en el frontend | El cálculo de contraste (RF-009) y la elección de tema (RF-033) son lógica que hay que probar | Probarlos a mano: no protege contra regresiones (§27) |
| 4 | Testcontainers en las pruebas de integración | Las reglas de unicidad, cascada y aislamiento solo se comprueban contra PostgreSQL real | Proveedor en memoria de EF: no aplica índices únicos ni cascadas |

## Supuestos pendientes de confirmar

Están detallados al final de [research.md](research.md). Ninguno bloquea las tareas: duración de
la sesión (7 días), reglas de la contraseña (8 a 128 caracteres), caducidad del enlace de
recuperación (60 minutos), que los datos de contacto del club son un correo y un teléfono, que el
escudo se sirve en cualquier estado del club y que la foto de perfil solo la ve su dueño.

Confirmados por el propietario el 2026-10-07: .NET 10; la entidad se llama `UsuarioRol`; una
persona es única y tiene un único inicio de sesión, así que un documento no puede estar en dos
cuentas; el escudo y la foto de perfil admiten PNG, JPEG o WebP de hasta 1 MB.
