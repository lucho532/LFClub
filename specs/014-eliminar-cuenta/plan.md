# Plan de implementación: eliminar mi cuenta

**Rama**: `014-eliminar-cuenta` | **Fecha**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación en `specs/014-eliminar-cuenta/spec.md`, con la constitución **4.12.0** ya
enmendada (§12.4 y §8).

## Resumen

Toda persona con sesión, salvo el DESARROLLADOR, puede eliminar su cuenta desde "Mi perfil": la
pantalla le dice qué se borra y qué se conserva, le lista por nombre y club a los jugadores que se
van con ella, y le pide la contraseña. Al confirmar, en una sola transacción, se borran la cuenta, todos
sus integrantes (jugadores incluidos, en todos los clubes) y lo que cuelga de ellos; lo que la persona
publicó se conserva con "Persona eliminada" como autor, y el marcador de los partidos no cambia. Un
presidente único de algún club no puede: se le dice cuál. Además, una página pública
`/eliminar-cuenta` explica el camino y la política `/privacidad` lo describe.

Enfoque técnico: **sin tablas nuevas ni migración**. Dos operaciones nuevas en `/api/cuenta`
(consultar el resumen y eliminar), un servicio de aplicación con una regla pura del presidente único
en el dominio, y un repositorio con cuatro operaciones de borrado masivo (anonimizar la autoría,
anular invitaciones, borrar lo que no cae en cascada y borrar la cuenta). Un **guardián de esquema**
(prueba que recorre las claves foráneas hacia cuenta e integrante) obliga a decidir qué pasa con
cualquier tabla futura, en especial los pagos (RF-007). En el frontend, una pantalla privada, una página
pública y dos enlaces. Las decisiones están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001. **No se añade ninguna.**

**Almacenamiento**: PostgreSQL 17. **Sin migración**: todo se apoya en las claves foráneas y los
borrados en cascada que ya existen ([data-model.md](data-model.md)).

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers, con el
`IReloj` y el servicio de correo sustituidos); Vitest en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable, también
empaquetada con Capacitor para Android.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: una cuenta tiene decenas de filas; la eliminación son cinco sentencias
en una transacción. El resumen es una lectura por integrante.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda regla en el servidor (§15); backend y pantalla en la misma tarea
(§27.1); o se borra todo o nada (RF-011); la respuesta a una credencial desconocida no cambia
(RF-013).

**Escala y alcance**: 0 entidades nuevas; el contrato pasa de 111 a 113 operaciones y las de club
siguen en 87 (las dos son de `/api/cuenta`); unos 5 archivos de frontend nuevos y 4 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.12.0**, que ya recoge toda la funcionalidad.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `ServicioEliminacionCuenta`, `ReglaEliminacionDeCuenta`, `RepositorioEliminacionCuenta`, "cuenta", "integrante", "jugador" | Cumple |
| §2.3 Máximo 250 líneas | Un servicio de consulta y otro de eliminación, repositorio propio; `verificar-tamano` al empezar | Cumple |
| §3 Documentación XML | En cada clase nueva, con lo que no debe asumir | Cumple |
| §4 y §5 Capas | Regla pura en `Dominio/Reglas`; servicios en `Aplicacion`; SQL en `Infraestructura`; el controlador delega | Cumple |
| §6 y §19 Tecnologías y simplicidad | Sin dependencias, tablas ni migración; reutiliza `IRepositorioPertenencias` y `IRepositorioClubesPlataforma` | Cumple |
| §7.1 Aislamiento entre clubes | Las lecturas parten de la cuenta con sesión; el borrado cruza clubes solo para esa cuenta (nueva excepción acotada, ver nota 3) | Cumple, con la nota 3 |
| §8 Presidente único | `ReglaUltimoPresidente` + clubes bloqueados en orden estable; 409 `presidente_unico` | Cumple |
| §10 y §12.4 Documento y correo | Se borran integrantes y cuenta: correo y documentos quedan libres; las demás cuentas con ese documento no cambian | Cumple |
| §13 y §14 Histórico y eliminación física | La eliminación física de la cuenta la manda §12.4; los comunicados y resultados no se borran | Cumple |
| §14.2 Fotos | Foto de perfil y de jugador se borran en cascada | Cumple |
| §15 Reglas en backend | Contraseña, presidente único, rol y atomicidad en el servidor | Cumple |
| §16.3 Pagos | Aún no existen; el guardián de esquema impide añadir su clave foránea sin decidir | Cumple, con la nota 4 |
| §18 Estados | Sin estados nuevos | Cumple |
| §20 Pruebas | Presidente único, borrado total, autoría anónima, marcador intacto, sesiones y credenciales | Cumple |
| §22 Migraciones | Ninguna | Cumple |
| §23 API | DTOs propios; `problem+json`; tres códigos nuevos | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas, estados por texto, acción destructiva distinguible sin color | Cumple, con la nota 5 |
| §25 y §28 Decisiones | No se resuelve ninguna decisión pendiente; las dudas van como notas | Cumple |
| §27 Definición de terminado | Tareas por historia con backend, pantalla y pruebas | Cumple |

**Resultado de la puerta**: pasa. Sin violaciones; no hay nada que justificar en el seguimiento de
complejidad.

**Nota 1 (para el propietario) — sesión iniciada con el documento de un jugador.** Quien entra con
el documento de un jugador ve solo a ese jugador (§12.4), pero la contraseña es la de toda la cuenta y
eliminarla borra a todos. El plan lista en la pantalla **a todos los jugadores de la cuenta**, no solo
al de la sesión: ocultarlos sería dejar que alguien borre lo que no sabe que borra. Quien tiene esa
contraseña es la propia familia, así que no sale información de la cuenta. La alternativa es exigir
iniciar sesión con el correo para eliminar. No se toma aquí; si se prefiere, cambia RF-003
([research.md](research.md) §5).

**Nota 2 (para el propietario) — la solicitud por correo.** La spec dice que la ejecuta el
DESARROLLADOR, pero la §8 le prohíbe tocar cuentas y fichas, y el panel no tiene esa función. El plan
**no añade** nada al panel. El camino de la página es: recuperar la contraseña con "Olvidé mi
contraseña" (llega al correo de la cuenta) y eliminar desde la aplicación; y, si la persona ni
siquiera puede hacer eso, el correo a `luisrak25@gmail.com` lo atiende una persona fuera de la
aplicación, con el procedimiento de [quickstart.md](quickstart.md). Si se quiere una acción en el
panel hay que enmendar la §8 ([research.md](research.md) §9).

**Nota 3 (de constitución)**: la eliminación de una cuenta toca integrantes de varios clubes en una
sola operación. Es la misma clase de excepción que ya existe para los datos propios de §12.6 (§7.1),
limitada a la cuenta con sesión y que no devuelve datos de ningún club. La §12.4 la exige y este plan
la aplica como está escrita; si el propietario quiere que §7.1 la liste expresamente, es una
enmienda de redacción, no de regla.

**Nota 4**: los pagos no existen todavía (§16). La regla RF-007 queda escrita en el guardián de
esquema y en el modelo de datos; cuando se cree la tabla de pagos habrá que copiar nombre y documento
en ella y poner su clave foránea en `SetNull`.

**Nota 5**: el lienzo no tiene esta pantalla ni la página pública. Su diseño sigue
[research.md](research.md) §10 con los tokens y componentes del proyecto y lo revisa el propietario en
la validación.

**Revisión tras el diseño**: ninguna tabla ni dependencia nueva; dos rutas nuevas, todas las demás
sin cambios. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/014-eliminar-cuenta/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas, hallazgos del código y notas al propietario
├── data-model.md        # Qué hace cada tabla al eliminar, DTOs y guardián de esquema
├── quickstart.md        # Recorrido de validación
├── contracts/
│   └── api.yaml         # Las dos operaciones nuevas
├── checklists/
│   └── requirements.md  # (de /speckit-specify)
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
backend/
├── src/
│   ├── LFClub.Dominio/
│   │   └── Reglas/
│   │       ├── ReglaEliminacionDeCuenta.cs        # NUEVO: clubes donde es presidente único
│   │       └── NombresDeAutoria.cs                # NUEVO: "Persona eliminada"
│   ├── LFClub.Aplicacion/
│   │   ├── DTOs/
│   │   │   ├── ResumenEliminacionCuentaDto.cs     # NUEVO: jugadores, bloqueos, puede
│   │   │   └── EliminarCuentaDto.cs               # NUEVO: contraseña
│   │   ├── Interfaces/
│   │   │   └── IRepositorioEliminacionCuenta.cs   # NUEVO
│   │   ├── Servicios/
│   │   │   └── IServicioEliminacionCuenta.cs      # NUEVO
│   │   └── Implementaciones/
│   │       ├── ServicioResumenEliminacionCuenta.cs  # NUEVO: lectura
│   │       └── ServicioEliminacionCuenta.cs         # NUEVO: contraseña + transacción
│   ├── LFClub.Infraestructura/
│   │   └── Repositorios/
│   │       └── RepositorioEliminacionCuenta.cs    # NUEVO: borrado masivo
│   └── LFClub.Api/
│       └── Controladores/Cuenta/
│           └── ControladorEliminacionCuenta.cs    # NUEVO: /api/cuenta/eliminacion
└── pruebas/
    ├── Unitarias/ReglaEliminacionDeCuentaPruebas.cs         # NUEVO
    └── Integracion/Cuenta/
        ├── EliminarCuentaPruebas.cs                          # NUEVO
        ├── EliminarCuentaPresidentePruebas.cs                # NUEVO
        ├── AutoriaAnonimaPruebas.cs                          # NUEVO
        └── EsquemaDeEliminacionPruebas.cs                    # NUEVO: guardián de claves foráneas

frontend/
├── src/cuenta/
│   ├── EliminarCuenta.tsx             # NUEVO: pantalla privada /cuenta/eliminar
│   ├── EliminarCuentaPublica.tsx      # NUEVO: página pública /eliminar-cuenta
│   ├── MiPerfil.tsx                   # CAMBIA: enlace "Eliminar mi cuenta"
│   ├── Entrar.tsx                     # CAMBIA: enlace a la página pública
│   └── Privacidad.tsx                 # CAMBIA: derechos y enlace
├── src/compartido/api/tiposCuenta.ts  # NUEVO: tipos del resumen
├── src/App.tsx                        # CAMBIA: dos rutas
└── pruebas/EliminarCuenta.test.tsx    # NUEVO

docs/play-store.md                     # CAMBIA: URL de eliminación y texto de privacidad
```

**Decisión de estructura**: se mantiene la separación `backend/` y `frontend/`. La lectura y la
eliminación son clases distintas para no acercarse a las 250 líneas; el repositorio es propio porque
borra tablas de varias áreas y no pertenece a ninguna de ellas.

## Seguimiento de la complejidad

No hay violaciones de la constitución que justificar.
