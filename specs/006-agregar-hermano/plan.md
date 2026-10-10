# Plan de implementación: Agregar un hermano y elegir el jugador

**Rama**: `006-agregar-hermano` | **Fecha**: 2026-10-09 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/006-agregar-hermano/spec.md`

**Actualización del 2026-10-09**: este plan se amplía solo para RF-034 a RF-040 (el documento que
ya usa otra cuenta en otro club). Todo lo demás está construido y validado (T001 a T022 de
[tasks.md](tasks.md)) y no se vuelve a planificar. Lo nuevo está en la sección
[Incremento](#incremento-el-documento-que-ya-usa-otra-cuenta-rf-034-a-rf-040), al final.

**Actualización del 2026-10-10**: se añade un segundo incremento, RF-041 a RF-043 (la familia
retira a su jugador y la reincorporación se niega mientras el documento esté activo en otra
cuenta), en la sección
[Segundo incremento](#segundo-incremento-la-familia-retira-y-la-reincorporación-rf-041-a-rf-043).
Resuelve el supuesto 9. La constitución pasó a la 4.2.0, que recoge las decisiones de los dos
incrementos; ninguno de los dos está construido todavía.

## Resumen

La familia que ya tiene un hijo en el club agrega a otro desde "Mi ficha": escribe su identidad y
el nuevo jugador queda en la sala de espera, con el correo, el celular, el responsable y la
contraseña de la cuenta. El PRESIDENTE lo aprueba o lo rechaza con la sala de espera que ya
existe, y ve de qué jugador es hermano. Cuando la cuenta tiene varios jugadores en el club, quien
entra con el correo elige con cuál continuar y puede cambiar sin cerrar sesión; quien entra con el
documento de uno ve solo a ese.

Enfoque técnico: un hermano es otra fila de `UsuarioRol` con la misma cuenta y el mismo club; no
hay tablas nuevas y la migración añade una sola columna, el jugador de origen. El cambio de fondo
está en un único punto: `IntegranteDelClubAttribute` deja de suponer un integrante por cuenta y
club y pregunta a una regla de dominio nueva, `ReglaJugadorDeLaSesion`, cuál es el de la petición.
El elegido viaja en la cabecera `X-Jugador-Elegido`; la sesión iniciada con documento lleva en el
token los jugadores a los que está limitada. Como todos los servicios reciben ya "quien pregunta"
como un `UsuarioRol`, quedan atados al jugador elegido sin cambiarlos. Un endpoint nuevo para
agregar al hermano; la sesión pasa a devolver una entrada por club con sus jugadores. Las
decisiones y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Una migración, `HermanosDeLaCuenta`, con una columna nueva en
`UsuariosRol`. La elección de jugador no se guarda en el servidor.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: los de la 001. La autorización de cada petición sigue haciendo una
sola consulta de integrantes; ahora trae todos los de la cuenta en el club, que son unos pocos.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15, RF-031);
backend y pantalla de cada historia en la misma tarea (§27.1); una cuenta con un solo integrante
debe comportarse exactamente igual que hoy, sin enviar nada nuevo (§27.2).

**Escala y alcance**: decenas de jugadores por club y dos o tres hermanos por cuenta (§19). Esta
funcionalidad tiene 0 entidades y 1 regla de dominio nuevas; 1 operación de API nueva y 6 que
cambian; 3 componentes de pantalla nuevos y 7 archivos de frontend que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.1.0**.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `ReglaJugadorDeLaSesion`, `ServicioAgregarHermano`, `X-Jugador-Elegido`; "hermano" y "jugador", sin entidad Familia ni Acudiente | Cumple |
| §2.3 Máximo 250 líneas | Lo nuevo va en archivos propios; `tipos.ts` (243 líneas) no crece: los tipos nuevos van en `tiposSesion.ts` | Cumple |
| §3 Documentación XML | En cada clase nueva, y se reescribe en las que cambian de responsabilidad (`IntegranteDelClubAttribute`, `ValidadorSesion`, `IEmisorTokenSesion`, `AccesoAFicha`, `ClubDeSesionDto`, `UsuarioRol`) | Cumple |
| §4 y §5 Capas y dependencias | La decisión de quién hace la petición es una regla de `Dominio/Reglas`; el atributo solo la consulta; el controlador nuevo solo delega | Cumple |
| §6 Tecnologías base | Ninguna nueva | Cumple |
| §7.1 Aislamiento entre clubes | La cabecera solo vale para un integrante de la cuenta **en el club de la ruta**; la lectura de integrantes de la cuenta filtra por club | Cumple |
| §7.3 Varios clubes | Un inicio de sesión; el club se sigue eligiendo en el desplegable; el hermano se agrega solo en el club de la ficha | Cumple |
| §7.5 Aislamiento dentro del club | La cuenta solo llega a sus jugadores; con uno elegido, tampoco a la ficha del hermano; conocer un identificador no da acceso | Cumple |
| §8 Jugador | Varios jugadores por cuenta; en cada momento, la información de uno solo | Cumple |
| §10 Documento | Único por club, en cualquier estado; el índice único ya existe | Cumple |
| §12.1.1 Sala de espera | Solo recibe al hermano; en espera no accede a nada del club, sin categoría; aprueba y rechaza solo el PRESIDENTE; rechazar borra sin dejar datos y deja intacta la cuenta | Cumple |
| §12.1.2 Hermanos | Se agrega desde la ficha, comparte el contacto de la cuenta, es un jugador independiente y pasa por la sala de espera | Cumple |
| §12.3 Alcance del JUGADOR | "Una cuenta puede tener varios jugadores… y solamente accede a los suyos". El jugador sigue siendo el `UsuarioRol` | Cumple, con la desviación heredada (Seguimiento de complejidad) |
| §12.4 Inicio de sesión | Con correo se elige después de entrar; con documento se ve solo a ese jugador; la contraseña es la de la cuenta | Cumple |
| §13 Histórico | La aprobación sigue copiando el nombre de quien aprueba; el jugador de origen no es histórico y no se copia | Cumple |
| §14.1 Retiro | Es de cada jugador; retirar a uno no toca a sus hermanos | Cumple |
| §15 Reglas en backend | Quién hace la petición, la limitación por documento y todas las validaciones se deciden en la API | Cumple |
| §18 Estados | `EN_ESPERA` y `APROBADO`, sin estado de rechazo | Cumple |
| §19 Simplicidad | Una columna, una regla, una cabecera; sin tablas, sin sesiones guardadas, sin entidad nueva | Cumple |
| §20 Pruebas | Autorización por rol, aislamiento entre cuentas, entre hermanos y entre clubes, sesión limitada por documento (research §11) | Cumple |
| §22 Migraciones | Una migración de Entity Framework | Cumple |
| §23 API | HTTP semántico, DTOs, `problem+json`, Swagger | Cumple |
| §24 Diseño visual | Componentes y temas existentes; el estado de cada jugador, con texto además de color | Cumple, con la nota 1 |
| §25 Decisiones no tomadas | Cinco detalles que la spec no fija quedan como supuestos en research.md | Cumple, con supuestos por confirmar |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |

**Resultado de la puerta**: pasa, con la misma desviación de la letra de §12.3 que ya justificaron
la 003, la 004 y la 005.

**Nota 1**: no he podido abrir el lienzo de §24 desde esta sesión. Las pantallas se componen con
los componentes ya aprobados; si el lienzo trae una pantalla de elección de jugador, se contrasta
con ella al construir la historia 2.

**Revisión tras el diseño**: el modelo de datos y el contrato no añaden excepciones al aislamiento,
tecnologías ni patrones respecto de lo evaluado. La cabecera no concede nada que la cuenta no
tenga, y la sesión con documento no recibe ningún dato de un hermano, tampoco en `GET /api/sesion`.
La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/006-agregar-hermano/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos del plan
├── data-model.md        # La columna nueva y cómo nace un hermano
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Contrato OpenAPI: un endpoint nuevo y seis que cambian
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LaPecosa.Dominio/
│   ├── Entidades/
│   │   └── ~ UsuarioRol.cs                  # AgregadoDesdeUsuarioRolId y su navegación
│   └── Reglas/
│       ├── + ReglaJugadorDeLaSesion.cs      # cuál integrante hace la petición
│       ├── + JugadorDeLaPeticion.cs         # el resultado: integrante, no encontrado o sin elegir
│       └── ~ ReglaAccesoAFicha.cs           # solo el nombre del parámetro: "es el de la petición"
├── LaPecosa.Aplicacion/
│   ├── DTOs/              + AgregarHermanoDto, JugadorDeSesionDto
│   │                      ~ ClubDeSesionDto (UsuarioRolId, Jugadores),
│   │                        IngresoEnEsperaDto (HermanoDe)
│   ├── Interfaces/        ~ IRepositorioPertenencias  # integrantes de la cuenta en un club;
│   │                                                  # identificadores por documento
│   │                      ~ IEmisorTokenSesion        # admite la limitación
│   ├── Servicios/         + IServicioAgregarHermano
│   │                      ~ IServicioSesion           # la limitación entra en obtener y renovar
│   ├── Implementaciones/  + ServicioAgregarHermano
│   │                      ~ ServicioSesion, ServicioRechazoIngreso
│   ├── Mappers/           ~ MapperSesion (agrupa por club), MapperIngresos (hermanoDe)
│   ├── Validadores/       + ValidadorHermano          # ValidadorIdentidad más el responsable
│   └── Utilidades/        ~ AccesoAFicha              # compara con el jugador de la petición
│                          + ErroresDeSesion           # jugador_sin_elegir
├── LaPecosa.Infraestructura/
│   ├── Datos/
│   │   ├── Configuraciones/  ~ ConfiguracionUsuarioRol
│   │   └── Migraciones/      + HermanosDeLaCuenta (generada)
│   ├── Repositorios/      ~ RepositorioPertenencias, RepositorioIngresos (incluye el origen)
│   └── Seguridad/         ~ EmisorTokenSesion, OpcionesSesion (reclamación `jugadores`)
└── LaPecosa.Api/
    ├── Autorizacion/      ~ IntegranteDelClubAttribute   # lee la cabecera y aplica la regla
    │                      ~ ValidadorSesion              # deja la limitación en la petición
    ├── Configuracion/     ~ ConfiguracionSeguridad (CORS admite la cabecera),
    │                        RegistroDeCasosDeUso
    └── Controladores/
        ├── ~ ControladorBase.cs             # expone la limitación de la sesión
        ├── Cuenta/        ~ ControladorSesion
        └── Club/          + ControladorHermanos

backend/pruebas/
├── Unitarias/
│   ├── Reglas/            + ReglaJugadorDeLaSesionPruebas
│   │                      ~ ReglaAccesoAFichaPruebas (nombre del parámetro)
│   └── Validadores/       + ValidadorHermanoPruebas
└── Integracion/
    ├── Base/              ~ ClienteDePrueba          # enviar la cabecera
    │                      + SembradorHermanos        # cuenta con dos jugadores, uno en espera
    ├── Hermanos/          + EscenarioHermanos
    │                      + AgregarHermanoPruebas    # crea, comparte contacto, queda en espera
    │                      + AgregarHermanoRechazosPruebas  # documento, validación, responsable, doble
    │                      + QuienAgregaPruebas       # roles, retirado, en espera, hermano ajeno
    │                      + HermanoEnEsperaPruebas   # nada del club, fuera de las listas
    │                      + ElegirJugadorPruebas     # sesión por club, cabecera, sin elegir
    │                      + SesionConDocumentoPruebas  # limitada, renovación, cambio de documento
    │                      + AprobarHermanoPruebas    # categoría del año, ficha propia y vacía
    │                      + RechazarHermanoPruebas   # borra solo a ese; conserva la invitación
    │                      + FichaEntreHermanosPruebas  # RF-030
    │                      + HermanosEntreClubesPruebas
    ├── Ingresos/          ~ SalaDeEsperaPruebas (hermanoDe)
    └── Aislamiento/       ~ AccesoClubPruebas        # 61 endpoints en los contratos

frontend/src/
├── compartido/
│   ├── api/               + tiposSesion.ts           # JugadorDeSesionDto, AgregarHermanoDto
│   │                      ~ tipos.ts (ClubDeSesionDto, IngresoEnEsperaDto), cliente.ts (cabecera)
│   └── sesion/            + jugadorElegido.ts        # la elección por club, en sessionStorage
│                          ~ ProveedorSesion.tsx      # la borra al iniciar y al cerrar
├── privado/
│   ├── ~ DisposicionClub.tsx                 # lista antes que nada; nombre del jugador; cambiar
│   ├── + ElegirJugador.tsx                   # la lista de jugadores con su estado
│   ├── + BotonCambiarJugador.tsx
│   ├── ~ SalaDeEspera.tsx, AvisoRetirado.tsx # ofrecen cambiar de jugador
│   ├── ficha/
│   │   ├── ~ FichaJugador.tsx                # "Agregar un hermano", solo en la ficha propia
│   │   └── + DialogoAgregarHermano.tsx
│   └── ingresos/          ~ SeccionSalaDeEspera.tsx  # "Hermano de …"
└── pruebas/               + jugadorElegido.test.ts
```

**Decisión de estructura**: la de la 001, sin carpetas nuevas salvo `pruebas/Integracion/Hermanos/`.
`DisposicionClub.tsx` tiene 141 líneas; la lista y el botón van en componentes propios para que no
pase de 250.

### Pantallas

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| `/club/:clubId/**` | Si la cuenta tiene varios jugadores y no hay elección, se muestra la lista antes que cualquier otra cosa | JUGADOR que entró con el correo |
| Menú del club | El nombre que ya se muestra es el del jugador elegido; "Cambiar de jugador" | JUGADOR con varios jugadores |
| Pantalla de ingreso pendiente y aviso de retiro | "Cambiar de jugador" | JUGADOR con varios jugadores |
| `/club/:clubId/jugadores/:usuarioRolId/ficha` | Botón "Agregar un hermano" y su diálogo, solo en la ficha propia | JUGADOR |
| `/club/:clubId/ingresos` → Sala de espera | "Hermano de …" en cada ingreso | PRESIDENTE |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. La familia agrega un hermano | RF-001 a 012, 014, 032 | `ServicioAgregarHermano`, `ValidadorHermano`, `ControladorHermanos`, columna de origen | Botón y diálogo en la ficha | Crea en espera con el contacto de la cuenta; sin categoría y fuera de las listas; documento repetido `409`; validación; responsable; doble confirmación; roles `403`; desde un hermano `404` |
| 2. La familia elige con cuál continuar | RF-013, 021 a 031 | `ReglaJugadorDeLaSesion`, `IntegranteDelClubAttribute`, `ServicioSesion`, `EmisorTokenSesion`, `MapperSesion`, `AccesoAFicha` | Lista de jugadores, cambiar de jugador, pendiente y retirado | Una entrada por club con `jugadores`; uno solo, lista vacía; sin cabecera `409`; ajeno `404`; en espera y retirado `403`; con documento, limitada y sin hermanos en la sesión; ficha del hermano `404` |
| 3. El PRESIDENTE aprueba o rechaza | RF-015 a 020 | `MapperIngresos`, `RepositorioIngresos`, `ServicioRechazoIngreso` | "Hermano de …" en la sala de espera | `hermanoDe`; aprobar ubica por año y da ficha vacía; rechazar borra solo a ese y conserva la invitación del primero; volver a agregarlo; otros roles `403` |
| Transversal | RF-033 | Estado del club, dos clubes | Todas, a 360 px y en los dos temas | Hermano solo en el club de origen; club suspendido; el contrato pasa a 61 endpoints; la 001 a la 005 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: la columna y su migración; `ReglaJugadorDeLaSesion` con sus pruebas unitarias; la
   lectura de integrantes de la cuenta en un club; `IntegranteDelClubAttribute` con la cabecera y
   CORS; `AccesoAFicha` por jugador de la petición. Al terminar, todas las pruebas anteriores
   siguen en verde y un sembrador de pruebas ya puede crear cuentas con dos jugadores.
2. **Historia 2**: la sesión agrupada por club con `jugadores`; la limitación por documento en el
   token, la validación y la renovación; la lista de jugadores, el cambio de jugador y las
   pantallas de pendiente y retirado. Se prueba con hermanos sembrados.
3. **Historia 1**: el endpoint de agregar con su validador; el botón y el diálogo en la ficha.
4. **Historia 3**: `hermanoDe` en la sala de espera; el cambio del rechazo; pruebas de aprobar y
   rechazar a un hermano.
5. **Cierre**: dos clubes, estados del club, revisión a 360 px en los dos temas, contrato frente a
   Swagger, documentación XML de las clases que cambiaron de responsabilidad y recorrido del
   quickstart.

La historia 2 va antes que la 1 aunque las dos sean P1: sin elegir jugador, un hermano recién
agregado deja a la cuenta sin poder entrar al club (`409 jugador_sin_elegir`). Ninguna de las dos
debe publicarse sin la otra.

## Cambios sobre lo ya construido en la 001, la 002, la 004 y la 005

Son los puntos donde hay que cuidar la no regresión (§27.2):

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| `IntegranteDelClubAttribute` lee todos los integrantes de la cuenta en el club y aplica la regla | RF-021 a 029 | Con un solo integrante y sin cabecera el resultado es el de hoy; todas las pruebas de integración anteriores pasan por aquí sin cambios |
| El token puede llevar la reclamación `jugadores` | RF-026 | Un token sin ella se comporta como hoy; las pruebas de sesión de la 001 que entran con documento usan cuentas de un integrante y siguen en verde |
| `GET /api/sesion` devuelve una entrada por club | Con hermanos habría dos del mismo club | Para cuentas de un integrante por club la lista es idéntica; `ClubDeSesionDto` solo gana campos |
| La ficha compara con el jugador de la petición y no con la cuenta | RF-030 | Con un jugador por cuenta es lo mismo; `FichaAjenaPruebas` sigue en verde; prueba nueva entre hermanos |
| El rechazo conserva las invitaciones si a la cuenta le queda otro integrante en el club | La invitación usada del primer hijo es el registro de su ingreso (004) | `RechazoPruebas` no cambia: sin hermanos se siguen borrando; prueba nueva |
| `IngresoEnEsperaDto` gana `hermanoDe` | RF-015 | Campo añadido; `SalaDeEsperaPruebas` lo comprueba |
| `cliente.ts` añade una cabecera a las peticiones del club | RF-021 | Solo cuando hay elección; sin ella, las peticiones son las de hoy |
| `AccesoClubPruebas` espera 61 endpoints en lugar de 60 | Un endpoint nuevo | El contrato de la 006 se añade en la misma tarea que el endpoint |

## Seguimiento de complejidad

| Desviación | Por qué hace falta | Alternativa más simple descartada |
| --- | --- | --- |
| §12.3 escribe `Jugador.UsuarioId`; el jugador sigue siendo el `UsuarioRol` con rol JUGADOR | Heredada de la 003, la 004 y la 005. El `UsuarioRol` ya cumple lo que §12.3 pide del jugador (pertenece a una única cuenta y tiene su propio registro); varios por cuenta no exigen la entidad | Dejarlo como está **es** la opción simple. La entidad `Jugador` completa se descarta por §19 y §27.2; si el propietario la quiere, es una spec de refactorización propia |

Las tres desviaciones del incremento frente a §7.1, §10 y §14.1 que figuraban aquí quedaron
resueltas por la enmienda 4.2.0 del 2026-10-10, que incorpora esas decisiones a la constitución.

## Consecuencias que conviene tener presentes

- **Cuenta con varios jugadores que recibe una invitación de PRESIDENTE**: la aceptación de la 001
  convierte en PRESIDENTE al integrante más antiguo y los demás siguen como JUGADOR. La cuenta
  queda con dos roles en el club y la lista de elección los mostraría juntos. Este plan no lo
  cambia: es el caso que §28 deja abierto ("Ficha con historial") y no debe resolverse aquí.
- **La cabecera es obligatoria para las cuentas con hermanos**: cualquier cliente futuro de la API
  (por ejemplo la aplicación móvil de §6.1) tendrá que enviarla.
- **Sesiones abiertas al desplegar**: un token anterior no lleva limitación. Quien había entrado
  con un documento y después agrega un hermano tendrá, hasta que vuelva a entrar, una sesión que
  elige como si hubiera entrado con el correo.
- **Entrar con el documento de un hermano en espera** funciona y muestra solo la pantalla de
  pendiente, como pide la spec.

## Supuestos del plan

Detallados al final de [research.md](research.md). Conviene que el propietario los confirme antes
de `/speckit-tasks` o durante `/speckit-analyze`:

1. No se admite el documento de un hermano que ya usa otra cuenta en otro club, como en el
   registro y en el cambio de documento. **Cambiado por el propietario el 2026-10-09**: se admite
   y, al aprobarse, el jugador queda retirado del otro club (spec, RF-034 a RF-040). Lo planifica
   el incremento de abajo, que sustituye a este supuesto.
2. Confirmar dos veces el mismo hermano devuelve el que ya está en espera, sin error.
3. La elección de jugador sobrevive a recargar la página y se pierde al cerrar la pestaña, al
   cerrar sesión y al volver a entrar.
4. Si la cuenta ya tiene responsable, el que se escriba al agregar un hermano se ignora.
5. Si el jugador de origen deja de existir, la sala de espera no dice de quién es hermano.

Los supuestos 6 a 10 son del incremento y están en su sección; los 11 a 13, del segundo
incremento.

## Incremento: el documento que ya usa otra cuenta (RF-034 a RF-040)

Planifica únicamente RF-034 a RF-040, las historias 1.11, 1.12, 2.12, 2.13 y 3.9 a 3.12 y los
criterios CE-011 y CE-012. Sustituye a lo que este plan y research §6 decían del
`409 documento_en_otra_cuenta` al agregar un hermano. Las decisiones están en
[research.md](research.md), secciones 12 a 17.

### Resumen del incremento

Hoy, agregar un hermano cuyo documento usa otra cuenta en otro club responde `409`. Pasa a
admitirse cuando ese documento es de un JUGADOR: el hermano queda en espera y la respuesta avisa a
la familia de que, si se aprueba, dejará de estar en el otro club. La sala de espera se lo avisa al
PRESIDENTE antes de decidir. Al aprobar, en la misma transacción, todo jugador activo de otra
cuenta con ese documento queda retirado en su club, con el retiro que ya existe. Rechazar y
esperar no tocan nada del otro club. Si el documento es de alguien que no es JUGADOR, el `409`
sigue. Y el inicio de sesión con un documento que está en varias cuentas abre una sola: la del
jugador activo.

Enfoque técnico: no hay tablas, columnas, migración ni endpoints nuevos. Una regla de dominio
nueva, `ReglaDocumentoCompartido`, decide las tres preguntas (¿se admite?, ¿aprobarlo retira a
alguien?, ¿qué cuenta abre el documento?) a partir de los integrantes que tienen ese número. Lo
que cruza clubes queda en dos sitios: una lectura más en `IRepositorioPertenencias` y un
repositorio nuevo y pequeño, `IRepositorioRetiroEntreClubes`, que es el único que escribe en otro
club. Dos DTO ganan un booleano, `retiraDeOtroClub`.

### Contexto técnico del incremento

**Lenguaje, dependencias, plataforma y pruebas**: los mismos. No se añade nada.

**Almacenamiento**: sin migración. El retiro automático usa las columnas del retiro de la 003.

**Rendimiento**: la sala de espera hace una consulta más, por los números de los que esperan. La
aprobación hace una lectura y dos sentencias más. Las búsquedas por número de documento sin club
ya existían en el inicio de sesión.

**Restricciones**: las del plan, más dos. El aviso no lleva el nombre del otro club ni ningún dato
de la otra cuenta (RF-038). La baja y la aprobación van en una sola transacción (RF-036).

**Escala y alcance**: 1 regla de dominio, 1 repositorio y 1 DTO nuevos; 0 operaciones de API
nuevas y 4 que cambian (`POST …/hermanos`, `GET …/ingresos/en-espera`,
`POST …/ingresos/{id}/aprobacion`, `POST /api/sesion`); 4 archivos de frontend que cambian. El
contrato sigue en 61 endpoints.

### Comprobación de la constitución (incremento)

Evaluada primero contra la **4.1.0** y de nuevo contra la **4.2.0**. Solo se listan los
principios que el incremento toca.

| Principio | Cómo lo cumple el incremento | Resultado |
| --- | --- | --- |
| §2.3 y §3 Tamaño y documentación | Lo nuevo va en archivos propios; `tipos.ts` pasa de 237 a 238 líneas. Se reescribe la documentación de las clases que cambian | Cumple |
| §5 Capas | Las tres decisiones son de una regla de `Dominio/Reglas`; los repositorios solo leen y ejecutan | Cumple |
| §7.1 Aislamiento entre clubes | El PRESIDENTE y la familia llegan a saber que un documento está activo en otro club, y la aprobación escribe en él. En la 4.1.0 era una desviación; la 4.2.0 la añade como cuarta excepción, sin nombre del club ni datos de la otra cuenta | Cumple (4.2.0) |
| §7.5 Aislamiento dentro del club | El aviso es un booleano: sin nombre de club, sin cuenta, sin identificadores | Cumple |
| §10 Documento | Sigue siendo único por club. Puede estar en dos cuentas solo por el hermano agregado, y el inicio de sesión abre la del jugador activo, como dice la 4.2.0 | Cumple (4.2.0) |
| §12.1.1 Sala de espera | Aprueba y rechaza solo el PRESIDENTE; rechazar sigue borrando solo a ese jugador | Cumple |
| §12.4 Inicio de sesión | Un documento abre una sola cuenta; con la contraseña de otra, la misma respuesta que cualquier dato incorrecto | Cumple |
| §13 Histórico | El retirado conserva su ficha y su historial. No se copia el nombre de quien aprueba: es de otro club (supuesto 6) | Cumple |
| §14 y §14.1 Retiro | Es el retiro que ya existe, con `Activo`; no se borra nada. La 4.2.0 incluye el retiro al aprobarse un hermano en otro club, en la misma operación y sin autor | Cumple (4.2.0) |
| §15 Reglas en backend | Admitir, avisar, retirar y elegir la cuenta se deciden en la API | Cumple |
| §19 Simplicidad | Sin tabla, columna, migración ni endpoint | Cumple |
| §20 Pruebas | Aislamiento del aviso, baja al aprobar, nada al rechazar ni al esperar, inicio de sesión (research §17) | Cumple |
| §22 y §23 Migraciones y API | Sin migración; contrato actualizado, DTOs, `problem+json` | Cumple |
| §25 Decisiones no tomadas | Cinco detalles que la spec no fija quedan como supuestos 6 a 10. La contradicción con la constitución se documenta aquí y no se resuelve por cuenta propia | Cumple |

**Resultado de la puerta**: pasaba contra la 4.1.0 con tres desviaciones justificadas por una
decisión expresa del propietario (spec, Aclaraciones del 2026-10-09).

**Enmienda aplicada**: la 4.2.0 (2026-10-10) incorpora esas decisiones: la cuarta excepción de
§7.1, el documento en dos cuentas en §10, la regla de §12.1.2, el retiro por ingreso en otro club
en §14.1 y sus pruebas en §20. Reevaluado contra la 4.2.0, el incremento cumple sin desviaciones.

**Revisión tras el diseño**: el contrato no entrega el nombre del otro club ni datos de la otra
cuenta por ningún camino, y el único que escribe en otro club es `RepositorioRetiroEntreClubes`,
acotado a un número de documento, que es lo que permite la cuarta excepción de §7.1.

### Código fuente del incremento

```text
backend/src/
├── LaPecosa.Dominio/Reglas/
│   ├── + ReglaDocumentoCompartido.cs    # ¿se admite?, ¿a quién retira?, ¿qué cuenta abre?
│   └── + DocumentoEnOtraCuenta.cs       # el resultado: SinBaja, ConBaja o NoAdmitido
├── LaPecosa.Aplicacion/
│   ├── DTOs/              + HermanoAgregadoDto            # JugadorDeSesionDto más retiraDeOtroClub
│   │                      ~ IngresoEnEsperaDto (RetiraDeOtroClub)
│   ├── Interfaces/        + IRepositorioRetiroEntreClubes # bloquear clubes en orden; retirar
│   │                      ~ IRepositorioPertenencias      # integrantes con esos números
│   ├── Servicios/         ~ IServicioAgregarHermano       # devuelve HermanoAgregadoDto
│   ├── Implementaciones/  ~ ServicioAgregarHermano, ServicioAprobacionIngreso,
│   │                        ServicioConsultaIngresos, ServicioSesion, ServicioIdentidadJugador
│   ├── Mappers/           ~ MapperIngresos, MapperSesion
│   └── Utilidades/        ~ ErroresDeFicha                # solo la documentación del 409
├── LaPecosa.Infraestructura/Repositorios/Plataforma/
│   ├── + RepositorioRetiroEntreClubes.cs
│   └── ~ RepositorioPertenencias.cs
└── LaPecosa.Api/
    ├── Configuracion/     ~ registro del repositorio nuevo
    └── Controladores/Club/ ~ ControladorHermanos           # tipo de la respuesta

backend/pruebas/
├── Unitarias/Reglas/      + ReglaDocumentoCompartidoPruebas
└── Integracion/
    ├── Hermanos/          + DocumentoDeOtraCuentaPruebas        # agregar: RF-034, 035, 039
    │                      + BajaEnOtroClubPruebas               # sala de espera, aprobar, rechazar
    │                      + SesionConDocumentoCompartidoPruebas # RF-040
    │                      ~ AgregarHermanoRechazosPruebas       # el 409 pasa a ser 201
    ├── Ficha/             ~ CambioDeDocumentoPruebas            # documento en dos cuentas
    └── Cuenta/            ~ RegistroConInvitacionPruebas        # documento en dos cuentas

frontend/src/
├── compartido/api/        ~ tiposSesion.ts (HermanoAgregadoDto), tipos.ts (retiraDeOtroClub)
└── privado/
    ├── ficha/             ~ DialogoAgregarHermano.tsx, FichaJugador.tsx  # el aviso a la familia
    └── ingresos/          ~ SeccionSalaDeEspera.tsx                      # el aviso al PRESIDENTE
```

### Pantallas del incremento

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| `/club/:clubId/jugadores/:usuarioRolId/ficha` | Tras agregar, junto a "pendiente de aprobación", el aviso de que al aprobarse dejará de estar en el otro club | JUGADOR |
| `/club/:clubId/ingresos` → Sala de espera | En el ingreso y en el diálogo de aprobar: "Este documento está activo en otro club. Si apruebas su ingreso, quedará retirado de allí" | PRESIDENTE |

El inicio de sesión no cambia de pantalla. La familia anterior ve el aviso de retiro que ya existe.

### Trazabilidad del incremento

| Requisito | Backend | Pantalla | Pruebas clave |
| --- | --- | --- | --- |
| RF-034 | `ServicioAgregarHermano`, `ReglaDocumentoCompartido`, `HermanoAgregadoDto` | Aviso tras agregar | Jugador activo de otra cuenta: `201` con `retiraDeOtroClub: true`; retirado o en espera allí: `201` con `false` |
| RF-035 | Nada: agregar y rechazar no escriben en otro club | — | En espera y tras el rechazo, el jugador del otro club queda idéntico |
| RF-036 | `ServicioAprobacionIngreso`, `RepositorioRetiroEntreClubes` | — | Aprobado aquí y retirado allí; en varios clubes, de todos; dos aprobaciones cruzadas a la vez terminan las dos |
| RF-037 | El retiro de la 003, sin autor | — | Ficha, documentos, cuenta, contraseña y demás jugadores de la otra cuenta, intactos; ningún correo |
| RF-038 | `ServicioConsultaIngresos`, `IngresoEnEsperaDto` | Aviso en la sala de espera | `retiraDeOtroClub` según el estado al abrir; el cuerpo no trae el nombre del otro club ni nada de la otra cuenta |
| RF-039 | `ReglaDocumentoCompartido` | El `409` como aviso, ya existe | PRESIDENTE, DIRECTIVO o ENTRENADOR de otra cuenta: `409 documento_en_otra_cuenta` y nada creado |
| RF-040 | `ServicioSesion`, `ReglaDocumentoCompartido` | — | Activo en una y retirado o en espera en otra: entra solo la contraseña de la activa; la otra, `401` igual que un dato incorrecto |

### Orden de construcción del incremento

Para `/speckit-tasks`: tareas nuevas a partir de T023; las anteriores no se tocan. Cada bloque deja
la batería en verde.

1. **Regla y lecturas**: `ReglaDocumentoCompartido` con sus pruebas unitarias; la lectura de
   integrantes por número en `IRepositorioPertenencias`; `ServicioIdentidadJugador` pasa a usarla.
   Nada cambia todavía para nadie.
2. **Inicio de sesión (RF-040)**: `ServicioSesion` elige la cuenta con la regla. Va antes de
   admitir el documento: sin esto, un documento en dos cuentas abriría una cualquiera.
3. **Aprobar con baja (RF-036, RF-037)** y **aviso en la sala de espera (RF-038)**, con su
   pantalla. Se prueba con hermanos sembrados con el documento de otra cuenta.
4. **Agregar (RF-034, RF-035, RF-039)**: el `409` pasa a ser `201` con el aviso, y su pantalla. Es
   el último porque es el que abre la puerta: hasta aquí ningún documento puede quedar en dos
   cuentas fuera de las pruebas.
5. **Cierre**: contrato frente a Swagger, 360 px en los dos temas para los dos avisos,
   documentación XML y el bloque 5 del quickstart.

### Cambios sobre lo ya construido

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| `POST …/hermanos` deja de responder `409` cuando el documento es de un JUGADOR de otra cuenta | RF-034 | La prueba `El_documento_de_otra_cuenta_en_otro_club_responde_409…` se reescribe: `201` para un jugador, `409` para los demás roles |
| La respuesta de `POST …/hermanos` pasa de `JugadorDeSesionDto` a `HermanoAgregadoDto` | RF-034 | Mismos cinco campos más uno: las pruebas y la pantalla de la 006 leen lo mismo |
| `IngresoEnEsperaDto` gana `retiraDeOtroClub` | RF-038 | Campo añadido; `HermanoDePruebas` y `SalaDeEsperaPruebas` siguen en verde |
| La aprobación lee antes de bloquear y, si hay baja, bloquea varios clubes | RF-036 | Sin documento compartido bloquea solo su club, como hoy; `AprobacionPruebas` y `AprobarHermanoPruebas` no cambian |
| El inicio de sesión con documento elige entre varias cuentas | RF-040 | Con el documento en una sola cuenta el resultado es el de hoy; `SesionPruebas` y `SesionConDocumentoPruebas` no cambian |
| El cambio de documento de la 005 pregunta "¿lo tiene otra cuenta?" en lugar de "¿de quién es?" | Con el número en dos cuentas, "de quién es" deja de tener una sola respuesta | Mismo `409` en los mismos casos; prueba nueva con el número en dos cuentas |
| El registro con invitación (004) | No cambia | Prueba nueva: un número que está en dos cuentas sigue dando `409` |

### Consecuencias del incremento

- **Una familia y su PRESIDENTE pueden retirar a un niño de otro club** conociendo su documento,
  sin que ese club ni su familia lo sepan ni lo confirmen. Es lo que la spec decide ("el único
  control es la aprobación del PRESIDENTE de este club"); queda anotado porque no tiene vuelta
  automática.
- **La familia anterior deja de entrar con el documento.** Sigue entrando con el correo. Si lo
  intenta con el documento y su contraseña, cada intento cuenta como fallo de la cuenta nueva, y
  al quinto esa cuenta queda bloqueada hasta recuperar la contraseña (§12.4). Ya era posible con
  cualquier documento; ahora es fácil que pase sin mala intención.
- **Agregar un hermano dice si un documento está activo en otro club.** El `409` de hoy ya decía
  que estaba registrado con otra cuenta.
- **Si el otro club quiere reincorporarlo**, no puede mientras siga activo aquí: lo resuelve el
  segundo incremento (RF-041 a RF-043).

### Supuestos del incremento

Rellenan lo que la spec no fija. **Confirmados por el propietario el 2026-10-10**:

6. La baja automática no guarda quién retiró: el otro club ve al jugador en su lista de retirados
   con la fecha y con "Lo retiró" vacío. Poner el nombre de quien aprobó llevaría un dato de este
   club al otro.
7. Al aprobar se vuelve a aplicar RF-039: si en ese momento el documento es, con otra cuenta, de
   alguien que no es JUGADOR, la aprobación responde `409 documento_en_otra_cuenta` y no cambia
   nada. El PRESIDENTE puede rechazarlo.
8. Desempate de RF-040: si el documento está activo en dos cuentas, en espera en dos y activo en
   ninguna, o retirado en todas, abre la del integrante más reciente.
9. La reincorporación en el otro club no cambia y no comprueba si el documento está activo con
   otra cuenta. **Decidido por el propietario el 2026-10-10**: se niega mientras el documento esté
   activo con otra cuenta, y la familia puede retirar a su jugador. Lo planifica el segundo
   incremento.
10. El aviso a la familia llega en la respuesta de agregar, después de crear al hermano (historia
    1.11), no como una confirmación previa.

## Segundo incremento: la familia retira y la reincorporación (RF-041 a RF-043)

Planifica únicamente RF-041 a RF-043, las historias 2.13 a 2.16 (escenarios 13 a 16 de la
historia 2), los casos límite nuevos y el criterio CE-013. Resuelve el supuesto 9. Las decisiones
están en [research.md](research.md), secciones 18 a 22. Se construye después del primer
incremento, porque reutiliza su lectura por número de documento, su bloqueo ordenado de clubes y
`ReglaDocumentoCompartido`.

### Resumen del segundo incremento

La cuenta de un jugador aprobado y activo puede retirarlo del club desde su ficha, con "Retirar
del club" y una confirmación. Es el retiro de la 003, por la misma operación: la cuenta y sus
demás jugadores no cambian y la familia ve el aviso de retiro que ya existe. Solo la propia cuenta
y el PRESIDENTE retiran; la familia no reincorpora. La reincorporación se niega con `409
documento_activo_en_otro_club` mientras el documento del jugador esté aprobado y activo con otra
cuenta en cualquier club, sin decir cuál; con la misma cuenta sigue como hoy. Así un documento no
queda nunca activo en dos cuentas, y la familia que se fue tiene un camino de vuelta que no
depende de un PRESIDENTE.

Enfoque técnico: sin tablas, columnas, migración ni endpoints nuevos. `POST …/retiro` admite el
rol JUGADOR y una regla de dominio nueva, `ReglaQuienRetira`, limita a la cuenta al jugador de la
petición. `ServicioRetiroJugador.ReincorporarAsync` bloquea los clubes donde está el número con el
repositorio del primer incremento, vuelve a leer y pregunta a `ReglaDocumentoCompartido` si hay
otra cuenta activa con ese número. En la pantalla, un botón nuevo en la ficha propia.

### Contexto técnico del segundo incremento

**Lenguaje, dependencias, plataforma, almacenamiento y pruebas**: los mismos. Sin migración.

**Rendimiento**: la reincorporación hace dos lecturas por número de documento y un bloqueo más;
el retiro, ninguna consulta nueva.

**Restricciones**: las del plan, más dos. El motivo del `409` no nombra el otro club ni da datos
de la otra cuenta (RF-041). La comprobación y la reincorporación van con los clubes del número
bloqueados, para que dos operaciones a la vez no dejen el documento activo en dos cuentas
(CE-013).

**Escala y alcance**: 1 regla de dominio nueva; 1 pregunta más en `ReglaDocumentoCompartido`; 0
operaciones de API nuevas y 2 que cambian (`POST …/retiro`, `POST …/reincorporacion`); 1
componente de pantalla nuevo y 1 archivo de frontend que cambia. El contrato sigue en 61
endpoints.

### Comprobación de la constitución (segundo incremento)

Evaluada contra la versión **4.2.0**. Solo se listan los principios que el incremento toca.

| Principio | Cómo lo cumple el segundo incremento | Resultado |
| --- | --- | --- |
| §2.3 y §3 Tamaño y documentación | `ServicioRetiroJugador` pasa de 93 a unas 130 líneas; el botón va en un archivo propio. Se reescribe la documentación de `IServicioRetiroJugador`, `ServicioRetiroJugador` y `ControladorJugadores`, que hoy dicen que solo retira el PRESIDENTE | Cumple |
| §5 Capas | Quién retira y si se reincorpora lo deciden reglas de `Dominio/Reglas`; el servicio lee, bloquea y ejecuta | Cumple |
| §7.1 Aislamiento entre clubes | La reincorporación consulta, por número, si está activo en otro club, que es lo que permite la cuarta excepción. Bloquear esos clubes no cambia ningún dato. El motivo no revela el club ni la otra cuenta | Cumple |
| §7.5 Aislamiento dentro del club | La cuenta solo retira al jugador de la petición; otro identificador, incluso de un hermano, responde `404` | Cumple |
| §8 Jugador | "Retirar del club a su propio jugador. No puede reincorporarlo" | Cumple |
| §10 Documento | La reincorporación se niega mientras el número esté activo con otra cuenta; con la misma, sigue | Cumple |
| §13 Histórico | El retiro por la familia copia el nombre de quien retira, que es el propio jugador (supuesto 11) | Cumple |
| §14.1 Retiro | Las tres formas de retiro y ninguna más; solo un aprobado y activo; solo el PRESIDENTE reincorpora; la negativa con motivo sin datos del otro club | Cumple |
| §15 Reglas en backend | Quién retira y si se reincorpora se deciden en la API; el botón solo se oculta | Cumple |
| §18 Estados | Sin estados nuevos: el retiro sigue siendo `Activo = false` | Cumple |
| §19 Simplicidad | Reutiliza la operación de retiro y el bloqueo ordenado del primer incremento | Cumple |
| §20 Pruebas | Las cinco viñetas nuevas de la 4.2.0 sobre retiro y reincorporación (research §22) | Cumple |
| §23 API | Mismas rutas; un código de error nuevo con `problem+json`; contrato actualizado | Cumple |
| §24 Diseño visual | `Boton` y `DialogoConfirmacion` en modo peligro, como el retiro del PRESIDENTE | Cumple |
| §25 Decisiones no tomadas | Tres detalles que la spec no fija quedan como supuestos 11 a 13 | Cumple, con supuestos por confirmar |

**Resultado de la puerta**: pasa, sin desviaciones.

**Revisión tras el diseño**: el contrato no añade rutas, la respuesta del `409` no lleva datos del
otro club, y no aparece ninguna escritura fuera del club de la petición distinta de la baja del
primer incremento. La puerta sigue pasando.

### Código fuente del segundo incremento

```text
backend/src/
├── LaPecosa.Dominio/Reglas/
│   ├── + ReglaQuienRetira.cs            # PRESIDENTE a cualquiera; JUGADOR solo al de la petición
│   └── ~ ReglaDocumentoCompartido.cs    # ¿hay otra cuenta activa con ese número?
├── LaPecosa.Aplicacion/
│   ├── Servicios/         ~ IServicioRetiroJugador     # documentación: también retira la familia
│   ├── Implementaciones/  ~ ServicioRetiroJugador      # la regla al retirar; bloqueo y
│   │                                                   # comprobación al reincorporar
│   └── Utilidades/        ~ ErroresDeCategorias        # documento_activo_en_otro_club
└── LaPecosa.Api/Controladores/Club/
    └── ~ ControladorJugadores.cs        # retiro: [PRESIDENTE, JUGADOR]; documentación

backend/pruebas/
├── Unitarias/Reglas/      + ReglaQuienRetiraPruebas
│                          ~ ReglaDocumentoCompartidoPruebas  # la cuarta pregunta
└── Integracion/
    ├── Hermanos/          + FamiliaRetiraPruebas                       # RF-042, RF-043
    │                      + ReincorporarConDocumentoCompartidoPruebas  # RF-041, CE-013
    └── Categorias/        ~ RetiroJugadorPruebas    # el jugador ya no está entre quienes no retiran

frontend/src/privado/ficha/
├── + BotonRetirarseDelClub.tsx          # botón, confirmación y recarga de la sesión
└── ~ FichaJugador.tsx                   # lo muestra en la ficha propia
```

### Pantallas del segundo incremento

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| `/club/:clubId/jugadores/:usuarioRolId/ficha` | "Retirar del club" con confirmación, solo en la ficha propia; después, el aviso de retiro que ya existe | JUGADOR |
| `/club/:clubId/categorias` → Retirados | Nada en la pantalla: el mensaje del nuevo `409` al reincorporar ya se muestra | PRESIDENTE |

### Trazabilidad del segundo incremento

| Requisito | Backend | Pantalla | Pruebas clave |
| --- | --- | --- | --- |
| RF-041 | `ServicioRetiroJugador.ReincorporarAsync`, `ReglaDocumentoCompartido`, `IRepositorioRetiroEntreClubes` | El mensaje en "Retirados" | Activo en otra cuenta: `409 documento_activo_en_otro_club` y nada cambia; sin nombre ni identificador del otro club; misma cuenta activa en otro club: `200`; otra cuenta en espera: `200`; tras retirarlo allí: `200`; dos reincorporaciones a la vez: una sola activa |
| RF-042 | `ReglaQuienRetira`, `ControladorJugadores`, `ServicioRetiroJugador.RetirarAsync` | Botón y diálogo en la ficha | La familia retira a su jugador: `204`, fuera de su categoría y sus equipos, ficha intacta; la cuenta y los hermanos igual; luego `403 integrante_retirado` y el aviso; con sesión de documento |
| RF-043 | `ReglaQuienRetira`, la autorización de siempre | Sin botón para otros roles ni en fichas ajenas | Hermano no elegido o jugador de otra cuenta: `404`; DIRECTIVO y ENTRENADOR: `403`; la familia reincorpora: `403` |
| CE-013 | Los bloqueos ordenados de la reincorporación y de la aprobación con baja | — | Reincorporación y aprobación con baja a la vez: nunca dos cuentas activas con el número |

### Orden de construcción del segundo incremento

Para `/speckit-tasks`: tareas a continuación de las del primer incremento. Cada bloque deja la
batería en verde.

1. **La familia retira (RF-042, RF-043)**: `ReglaQuienRetira` con sus pruebas; la autorización y
   la regla en el retiro; `RetiroJugadorPruebas` ajustada; el botón en la ficha. No depende del
   primer incremento y podría ir antes, pero sin el bloque 2 no sirve como camino de vuelta.
2. **Reincorporación (RF-041, CE-013)**: la cuarta pregunta de `ReglaDocumentoCompartido`; el
   bloqueo y la comprobación en `ReincorporarAsync`; las pruebas de concurrencia.
3. **Cierre**: contrato frente a Swagger, el botón a 360 px en los dos temas, documentación XML y
   el bloque 6 del quickstart.

### Cambios sobre lo ya construido (segundo incremento)

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| `POST …/retiro` admite el rol JUGADOR | RF-042 | Para el PRESIDENTE no cambia nada. La regla se aplica **antes** de leer al jugador, para que el `404` no dependa de si existe ni de su estado. `RetiroJugadorPruebas` se ajusta en la prueba de roles y las demás siguen en verde |
| La reincorporación lee por número y puede bloquear varios clubes | RF-041 | Sin documento compartido bloquea solo su club, como hoy; las pruebas de reincorporación de la 003 no cambian |
| La documentación del retiro dice que también retira la familia | §3 | Se reescribe en la misma tarea que el cambio |

### Consecuencias del segundo incremento

- **Una familia puede retirar por error a su único jugador del club**: queda retirado como
  cualquier otro y solo el PRESIDENTE lo reincorpora (caso límite de la spec). El diálogo de
  confirmación lo advierte.
- **El PRESIDENTE del club anterior no tiene forma de pedir la vuelta**: sabe que el documento
  está activo en otro club, pero no cuál. La familia o el otro PRESIDENTE deciden (spec, fuera de
  alcance).
- **El retiro por la familia no avisa al PRESIDENTE**: lo verá en "Retirados", como hoy cualquier
  retiro.

### Supuestos del segundo incremento

Rellenan lo que la spec no fija. **Confirmados por el propietario el 2026-10-10**:

11. Cuando la familia retira, el autor del retiro es el propio jugador: en "Retirados", "Lo
    retiró" muestra su nombre.
12. Un hermano en espera con ese número en otra cuenta no impide reincorporar; si después se
    aprueba, retira al reincorporado.
13. Con varios hermanos, la familia retira al jugador elegido; para retirar a otro, cambia de
    jugador.
