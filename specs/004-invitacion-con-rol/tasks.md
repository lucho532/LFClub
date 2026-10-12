---

description: "Lista de tareas de la funcionalidad 004: invitación con rol e ingreso directo al club"
---

# Tareas: Invitación con rol e ingreso directo al club

**Entrada**: documentos de diseño en `specs/004-invitacion-con-rol/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.1.0) y el plan. No van en tareas
aparte: por §27.1, cada tarea de una historia es un corte vertical que entrega el backend, la
pantalla que lo usa contra la API real y sus pruebas.

**Organización**: las tareas se agrupan por historia de usuario para poder implementar y probar
cada historia por separado.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de `specs/001-base-multiclub/tasks.md`, `specs/002-ingreso-club/tasks.md` y
`specs/003-categorias-club/tasks.md`, que siguen vigentes. Resumen y lo que se añade:

- **Sin esquema nuevo** (data-model.md): esta funcionalidad no crea tablas, columnas ni migración.
  Si una tarea parece necesitar una, se detiene y se informa (§25).
- **Sin endpoints nuevos**: cambian once y se elimina uno. `specs/004-invitacion-con-rol/contracts/api.yaml`
  manda en cuerpos, códigos de estado y códigos de error de los once.
- **Nombres** (§2.1, §2.2): todo en español y con el vocabulario del dominio. Sin sinónimos.
- **Documentación** (§3): toda clase que cambia de responsabilidad actualiza su documentación XML
  (o su comentario de componente en el frontend) en la misma tarea. Hoy varias dicen "queda en la
  sala de espera", "no lleva rol" o "el PRESIDENTE y los DIRECTIVOS".
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Varias clases de pruebas
  están cerca (`InvitacionesClubPruebas` 230, `RegistroConInvitacionPruebas` 228, `RechazoPruebas`
  224, `AprobacionPruebas` 219, `UbicacionAutomaticaPruebas` 214): los casos nuevos van en los
  archivos nuevos que indica cada tarea, no en esos.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Las reglas, en
  `backend/src/LFClub.Dominio/Reglas/`. Los controladores no ganan lógica.
- **Autorización** (§15): quién invita y quién aprueba lo decide el atributo
  `[IntegranteDelClub(Rol.PRESIDENTE)]` de la 001, que ya responde `403 rol_no_autorizado`. No se
  escribe código de autorización nuevo.
- **Aislamiento** (§7.1): ninguna consulta nueva usa `IgnoreQueryFilters()`. El registro y la
  aceptación fijan en `IContextoClub` el club de la invitación (research §4) y a partir de ahí usan
  los repositorios del club tal cual.
- **Personas en espera en las pruebas**: ningún camino de la API deja ya a nadie `EN_ESPERA`. Las
  pruebas que la necesitan como punto de partida la siembran con
  `Sembrador.CrearIntegranteEnEsperaAsync` o `SembradorCategorias.CrearJugadorEnEsperaAsync`.
- **Pruebas de la 002 y la 003** (CE-008): las que contradicen la constitución 4.1.0 se
  **reescriben** con el comportamiento nuevo; ninguna se borra sin que otra cubra lo que seguía
  siendo válido, y no se toca lo que comprueban las demás.
- **Pantallas** (RF-022, §24): componentes `Campo`, `Tabla`, `Tarjeta`, `Aviso` y
  `DialogoConfirmacion` existentes; legibles a 360 px y en los dos temas; el rol se dice con texto
  (`nombreDeRol` de `frontend/src/compartido/formato`).
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, `tsc` no da errores y pasa
  `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

El plan pidió confirmarlos (research.md, "Supuestos por confirmar"). Las tareas los dieron por
buenos; desde el 2026-10-09 no queda ninguno por confirmar.

| # | Supuesto | Tarea | Estado |
| --- | --- | --- | --- |
| 1 | El responsable deja de pedirse también en el registro de un PRESIDENTE | T007 | Confirmado por el propietario el 2026-10-09 (RF-013) |
| 2 | Un `nombreResponsable` enviado con una invitación de ENTRENADOR o DIRECTIVO se ignora, sin error | T007 | Cubierto por RF-013 y RF-026 |
| 3 | Quien ya tiene cuenta y acepta una invitación de JUGADOR no ve ningún formulario | T008, T016 | Sustituido el 2026-10-09 por RF-026: se le pide el responsable si es menor de 18 años y su cuenta no lo tiene |
| 4 | La aprobación admite `rol: JUGADOR` o ningún rol; cualquier otro responde `403 rol_no_asignable` | T010 | Cubierto por RF-016 |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: partir de una base en verde. No hay proyectos, paquetes ni tecnologías nuevas.

- [X] T001 Comprobar la línea base en la rama `004-invitacion-con-rol`: ejecutar `dotnet test backend/LFClub.sln`, `npm --prefix frontend test` y `scripts/verificar-tamano.ps1`, y confirmar que todo pasa antes de tocar código. Comprobar en particular que, con `specs/004-invitacion-con-rol/contracts/api.yaml` ya en el repositorio, `La_api_expone_exactamente_los_endpoints_del_contrato` de `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` sigue contando 55: `EndpointsDeLaApi.DelContrato()` no debe contar dos veces las once rutas que el contrato 004 repite, y `POST /api/invitaciones/consulta` y `POST /api/invitaciones/registro` deben seguir marcadas como anónimas. Si algo falla, detenerse e informarlo: no se construye sobre una base rota

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: las dos reglas de dominio, el error nuevo y las ayudas de prueba que usan todas las
historias. Todo es aditivo: al terminar la fase la aplicación se comporta igual que antes.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase

- [X] T002 [P] Crear `backend/src/LFClub.Dominio/Reglas/ReglaInvitacionDelClub.cs` (research §2), sin dependencias de HTTP ni de EF: una lista `RolesInvitables` con JUGADOR, ENTRENADOR y DIRECTIVO, y `Admite(Rol rol)`, verdadero solo para esos tres. No depende de quién invita: eso lo decide el atributo de autorización. Documentación XML que diga qué representa y que no decide quién puede invitar. Añadir a `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeInvitacion.cs` el error `RolNoInvitable()`: código `rol_no_invitable`, estado 403 y un mensaje en español que diga que desde el club solo se invita como jugador, entrenador o directivo. Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaInvitacionDelClubPruebas.cs`: los cinco valores de `Rol`, con verdadero para JUGADOR, ENTRENADOR y DIRECTIVO y falso para PRESIDENTE y DESARROLLADOR
- [X] T003 [P] Ampliar `backend/src/LFClub.Dominio/Reglas/ReglaIngresoPorInvitacion.cs` (research §3) con dos preguntas nuevas: `EsDelClub(Rol rol)`, verdadero para toda invitación que no es de PRESIDENTE, y `PideResponsable(Rol rol)`, verdadero solo para JUGADOR (RF-013). Reescribir `ElClubPermiteUsarla` sobre `EsDelClub`, sin cambiar su resultado: una invitación del club no sirve en un club dado de baja y la de PRESIDENTE sí (RF-021). `EstadoDeIngreso` y `PasaPorSalaDeEspera` **se conservan por ahora**: dejan de usarse en T007 y T008, y T008 los elimina. Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaIngresoPorInvitacionPruebas.cs`: `EsDelClub` y `PideResponsable` para los cuatro roles de club, y `ElClubPermiteUsarla` para cada rol con cada `EstadoClub`
- [X] T004 [P] Ayudas de prueba en `backend/pruebas/Integracion/Ingresos/EscenarioIngresos.cs`, sin cambiar ninguna llamada existente: las rutas `Invitaciones(club)`, `Reenvio(club, invitacionId)` y `Cancelacion(club, invitacionId)`; `InvitarAsync(this ClienteDePrueba cliente, Club club, string correo, Rol rol)`, que hace el `POST` con `{ correo, rol }` y devuelve la respuesta; `DatosDeRegistro(token, numeroDocumento = null, fechaNacimiento = null, nombreResponsable = null)`, el cuerpo de `POST /api/invitaciones/registro` con valores por defecto válidos (un adulto, documento único), para que las pruebas nuevas no copien un sexto `Datos` privado; y `RegistrarConInvitacionAsync(this FabricaApi fabrica, Club club, Rol rol, DateOnly? fechaNacimiento = null)`, que siembra una invitación de ese rol con `Sembrador.CrearInvitacionAsync`, registra a la persona por la API y devuelve su cliente con sesión y su integrante guardado. Los `Datos` privados que ya tienen otras clases de pruebas no se tocan

**Punto de control**: todas las pruebas de la 001, la 002 y la 003 siguen en verde y la aplicación
no ha cambiado de comportamiento

---

## Fase 3: Historia 1 - El club invita indicando el rol (Prioridad: P1) 🎯 MVP

**Objetivo**: el PRESIDENTE invita eligiendo el rol (JUGADOR, ENTRENADOR o DIRECTIVO) y ve el rol
de cada invitación. Nadie más del club invita ni ve el apartado "Ingresos".

**Prueba independiente**: se inicia sesión como PRESIDENTE, se envía una invitación con el rol
ENTRENADOR y se comprueba que aparece en la lista como pendiente con ese rol; se inicia sesión como
DIRECTIVO y se comprueba que no tiene el apartado "Ingresos" ni puede invitar.

- [X] T005 [US1] Invitar con rol, verlo en la lista, conservarlo al reenviar y nombrarlo en el correo (`GET` y `POST /api/clubes/{clubId}/invitaciones` y `POST …/invitaciones/{invitacionId}/reenvio`; depende de T002 y T004). Backend: `backend/src/LFClub.Aplicacion/DTOs/InvitarAlClubDto.cs` gana `Rol? Rol` y `InvitacionClubDto.cs` gana `Rol Rol` después de `Correo`; `backend/src/LFClub.Aplicacion/Mappers/MapperIngresos.cs` lo copia de la invitación. En `backend/src/LFClub.Aplicacion/Implementaciones/ServicioInvitacionesClub.cs`: **invitar** valida el correo y el rol en el mismo `ErroresDeValidacion`, y un rol ausente o que no es de la enumeración responde `400 datos_invalidos` con el error en el campo `rol` ("el rol es obligatorio", escenario 1.5); después, si `ReglaInvitacionDelClub.Admite` lo rechaza (PRESIDENTE o DESARROLLADOR), `403 rol_no_invitable`, antes de comprobar el correo y sin crear ni enviar nada (escenario 1.4); `CrearYEnviarAsync` recibe el rol en lugar del `Rol.JUGADOR` fijo. **Reenviar** crea la invitación nueva con `anterior.Rol`; no admite cambiarlo (RF-001, RF-004). **Invitar de nuevo** un correo con una invitación pendiente sigue anulando la anterior, y vale el rol de la nueva. "El rol es obligatorio y no se modifica después de crear la invitación": no existe ninguna operación que lo cambie. En `backend/src/LFClub.Infraestructura/Correo/PlantillasCorreo.cs`, la invitación del club pasa a decir "te invita a registrarte como jugador / entrenador / directivo" y pierde la frase "el club revisará tu ingreso antes de darte acceso" (RF-006); la de PRESIDENTE no cambia. Actualizar la documentación XML de `IServicioInvitacionesClub`, `IServicioCorreo` y `backend/src/LFClub.Dominio/Entidades/Invitacion.cs` (el rol lo elige el PRESIDENTE). Frontend: en `frontend/src/compartido/api/tipos.ts`, `InvitarAlClubDto` gana `rol: RolDeIngreso` e `InvitacionClubDto` gana `rol: RolDeIngreso`; en `frontend/src/privado/ingresos/SeccionInvitacionesClub.tsx`, un `select` nativo "Rol" obligatorio, sin valor elegido por defecto, con Jugador, Entrenador y Directivo; sin rol no envía y muestra el error junto al campo, igual que el error que la API devuelve en `rol`; la tabla gana la columna "Rol" con `nombreDeRol`; el aviso de enviada dice el rol; el texto de ayuda deja de decir que el rol "se decide al aprobar su ingreso"; al enviar se vacían el correo y el rol. Pruebas: crear `backend/pruebas/Integracion/Ingresos/InvitacionConRolPruebas.cs`: el PRESIDENTE invita con cada uno de los tres roles y la invitación queda pendiente con ese rol en la respuesta, en la base de datos y en la lista (1.1, 1.6); sin `rol` y con `rol: null`, 400 con el error en `rol` y no se crea ni se envía nada; con PRESIDENTE y con DESARROLLADOR, 403 `rol_no_invitable` y no se crea ni se envía nada; reenviar conserva el rol con cada uno de los tres (1.7); invitar de nuevo con otro rol deja una sola fila con el rol nuevo, el enlace anterior responde 410 y la consulta del enlace nuevo dice el rol nuevo (1.8); un correo que ya es integrante del club se rechaza con `409 ya_esta_en_el_club` sea cual sea el rol de la invitación; una invitación usada (sembrada con `usadaEn`) aparece como `USADA` con su rol y quién la envió (RF-019). En `backend/pruebas/Unitarias/Correo/PlantillasCorreoPruebas.cs`: la invitación del club nombra el club y el rol para cada uno de los tres y no contiene "revisará" (1.10); la de presidente sigue igual. Actualizar las pruebas que invitan para que indiquen `rol` (todas las que hacen `POST …/invitaciones` con solo `correo`): `backend/pruebas/Integracion/Ingresos/InvitacionesClubPruebas.cs`, `ReenvioYCancelacionPruebas.cs`, `AislamientoIngresosPruebas.cs`, `IngresosPorEstadoDelClubPruebas.cs` (en el club suspendido el presidente invita con los tres roles, RF-021) y `backend/pruebas/Integracion/Categorias/PersonaRetiradaPruebas.cs` (que además pasa a invitar como presidente)
- [X] T006 [US1] Solo el PRESIDENTE envía, ve, reenvía y cancela invitaciones, y el DIRECTIVO deja de tener el apartado "Ingresos" (los cuatro endpoints de `/api/clubes/{clubId}/invitaciones`; depende de T005). Backend: en `backend/src/LFClub.Api/Controladores/Club/ControladorInvitacionesClub.cs`, `[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]` pasa a `[IntegranteDelClub(Rol.PRESIDENTE)]` (RF-002, RF-003) y se actualiza su documentación; ningún otro código de autorización. Frontend: en `frontend/src/privado/DisposicionClub.tsx`, el enlace "Ingresos" se pinta solo cuando el rol es PRESIDENTE (escenario 1.2); en `frontend/src/privado/ingresos/Ingresos.tsx`, quien no es PRESIDENTE vuelve al inicio del club, como ya le pasa hoy a un entrenador, sin que se pida ninguna lista, y su comentario deja de decir "y sus directivos". Pruebas: crear `backend/pruebas/Integracion/Ingresos/SoloPresidenteIngresosPruebas.cs`: con una invitación pendiente real, un DIRECTIVO, un ENTRENADOR y un JUGADOR reciben `403 rol_no_autorizado` al listar, al invitar con `rol: JUGADOR`, al reenviar y al cancelar; la respuesta no trae ningún dato, no se envía ningún correo y la invitación no cambia (escenario 1.3, CE-003). En el mismo archivo: en un club con dos presidentes (el segundo, sembrado), uno ve, reenvía y cancela la invitación que envió el otro (caso límite). Reescribir lo que contradice la 4.1.0: en `InvitacionesClubPruebas.cs`, `El_presidente_y_un_directivo_invitan_…` pasa a probar solo al presidente; en `ReenvioYCancelacionPruebas.cs`, las dos teorías por rol pasan a presidente, y `La_invitacion_sigue_sirviendo_aunque_quien_la_envio_deje_de_ser_directivo_o_salga_del_club` pasa a ser "…deje de ser presidente o salga del club" (se le cambia el rol o se le borra directamente en la base de datos; la invitación sigue vigente y `enviadaPor` llega nulo si ya no está); en `IngresosPorEstadoDelClubPruebas.cs`, cualquier invitación hecha por un directivo pasa a hacerla el presidente

**Punto de control**: el presidente invita con rol y nadie más invita (quickstart, bloque 1). Quien
se registra con esas invitaciones todavía queda en espera: **no se publica nada hasta terminar la
fase 4**

---

## Fase 4: Historia 2 - La persona invitada se registra y entra directamente (Prioridad: P1)

**Objetivo**: quien se registra o acepta con una invitación válida queda en el club con el rol de
la invitación, aprobado y sin sala de espera; si es JUGADOR queda en la categoría de su año.

**Prueba independiente**: con una invitación de JUGADOR y la categoría de su año creada, se
completa el registro y se comprueba que la persona ve de inmediato la aplicación de su club y
aparece en esa categoría; con una invitación de ENTRENADOR se comprueba que entra como ENTRENADOR
y no aparece en ninguna categoría ni en "Sin categoría".

- [X] T007 [US2] Registro directo con el rol de la invitación, ubicación del jugador y responsable según el rol (`POST /api/invitaciones/consulta` y `POST /api/invitaciones/registro`; depende de T003, T004 y T005). Backend: `backend/src/LFClub.Aplicacion/DTOs/InvitacionVigenteDto.cs` cambia `PasaPorSalaDeEspera` por `PideResponsable` ("verdadero solo en las invitaciones de JUGADOR"), que la consulta rellena con `ReglaIngresoPorInvitacion.PideResponsable`. `backend/src/LFClub.Aplicacion/Validadores/ValidadorRegistro.cs` recibe el rol de la invitación: con JUGADOR, el responsable es "obligatorio si quien ingresa es menor de 18 años el día del registro y opcional si es adulto", con su máximo de 160 caracteres, como hoy; con cualquier otro rol, también PRESIDENTE (supuesto 1), no se exige ni se valida. En `backend/src/LFClub.Aplicacion/Implementaciones/ServicioRegistroConInvitacion.cs` (research §3 y §4): en cuanto la invitación es válida, fija su club en `IContextoClub`; el integrante nace con el rol de la invitación, como único rol, `EstadoIngreso` `APROBADO` y `AprobadoEn`, `AprobadoPorUsuarioId`, `AprobadoPorNombre` y `RolDeIngreso` vacíos, exactamente como ya nace un PRESIDENTE (RF-008, RF-019); `NombreResponsable` "solo se pide y se guarda cuando la invitación es de JUGADOR. En los demás roles queda nulo aunque llegue en el cuerpo" (RF-013, supuesto 2); dentro de la transacción, en este orden: bloquear la fila del club (`IRepositorioClub.BloquearAsync`), marcar la invitación como usada con la sentencia condicionada de hoy, crear la cuenta y el integrante, guardar y, si el rol es JUGADOR, `UbicadorDeJugadores.UbicarAUnoAsync` (RF-011); con ENTRENADOR o DIRECTIVO no se llama al ubicador y `CategoriaId` queda "nulo siempre" (RF-012). El rol sale solo de la invitación: el DTO sigue sin campo de rol y un `rol` enviado de más se ignora (RF-009). La respuesta sigue siendo el `TokenSesionDto`, sin ningún dato del club. Comprobar que las consultas previas del servicio (correo, documento en el club, documento en otra cuenta) dan el mismo resultado con el club ya fijado. Este servicio deja de usar `EstadoDeIngreso` y `PasaPorSalaDeEspera`. Actualizar la documentación XML del servicio, de `IServicioRegistroConInvitacion`, de `RegistrarConInvitacionDto`, de `backend/src/LFClub.Dominio/Entidades/UsuarioRol.cs` (quien entra con invitación nace aprobado) y de `backend/src/LFClub.Api/Controladores/Cuenta/ControladorInvitaciones.cs`. Frontend: en `frontend/src/compartido/api/tipos.ts`, `InvitacionVigenteDto` cambia `pasaPorSalaDeEspera` por `pideResponsable`; `frontend/src/cuenta/Invitacion.tsx`: el subtítulo nombra siempre el rol ("Invitación para ser entrenador del club"), que con el club y el correo no se puede cambiar (2.1); `frontend/src/cuenta/FormularioRegistro.tsx`: el campo del responsable solo se muestra y se envía con `pideResponsable` (2.7), y desaparece el aviso de que el ingreso quedará pendiente de aprobación; `frontend/src/cuenta/AceptarInvitacion.tsx`: un solo texto, que nombra el club y el rol y no avisa de ninguna espera (su backend llega en T008, la tarea siguiente). `tsc` no debe encontrar ningún uso de `pasaPorSalaDeEspera`. Tras registrarse, la navegación que ya existe lleva a `/club/:clubId` y `DisposicionClub` muestra la aplicación, sin código nuevo; `frontend/src/privado/SalaDeEspera.tsx` se conserva. Pruebas: crear `backend/pruebas/Integracion/Ingresos/RegistroDirectoPruebas.cs`, que sustituye a `RegistroEnEsperaPruebas.cs` (se elimina; sus casos que siguen valiendo se traen aquí: sin token, enlaces usados, vencidos, cancelados o reemplazados con 410, y documento repetido): con cada uno de los tres roles la persona queda `APROBADO`, con una sola fila de `UsuarioRol` en el club y ese rol, sin datos de aprobación; `GET /api/sesion` la da por aprobada y un endpoint del club que su rol admite responde 200 de inmediato (2.2, 2.3, CE-001, CE-002); la sala de espera y los ingresos aprobados del club siguen vacíos y la invitación aparece como `USADA` con su rol (2.11, RF-019, CE-006); un `rol: PRESIDENTE`, un correo o un club enviados en el cuerpo se ignoran (2.9); la respuesta no contiene el nombre del club; la consulta trae `pideResponsable` verdadero solo con JUGADOR y ya no trae `pasaPorSalaDeEspera`; JUGADOR menor sin responsable, 400 en `nombreResponsable` y la invitación no se gasta, y con responsable se registra; JUGADOR adulto sin responsable se registra; ENTRENADOR y DIRECTIVO se registran sin responsable y, si lo envían, no se guarda. Crear `backend/pruebas/Integracion/Ingresos/RegistroDirectoUbicacionPruebas.cs`: JUGADOR con la categoría activa de su año queda en ella (2.4, CE-004); sin ella o con ella inactiva se registra igual, aparece en "Sin categoría" y entra al crearla o reactivarla (2.5); un adulto invitado como JUGADOR queda en "Sin categoría" si no existe la de su año; ENTRENADOR y DIRECTIVO quedan sin categoría, fuera de "Sin categoría" y como candidatos a entrenador (2.6, CE-005); registrarse mientras el presidente crea la categoría de su año (`Task.WhenAll`, repetido varias veces, el mismo patrón de `UbicacionAutomaticaPruebas`) lo deja siempre dentro (caso límite); ningún registro crea un `JugadorEquipo`. En `backend/pruebas/Unitarias/Validadores/ValidadorRegistroPruebas.cs`: los casos actuales pasan a ser de JUGADOR y se añade que con ENTRENADOR, DIRECTIVO y PRESIDENTE un menor sin responsable es válido y un responsable de 161 caracteres no da error. Reescribir lo que esperaba la sala de espera: en `IngresosPorEstadoDelClubPruebas.cs`, quien se registra en un club suspendido queda `APROBADO`, recibe `403 club_suspendido` al entrar al club y entra con su rol al levantar la suspensión (RF-021), y en uno dado de baja el enlace sigue respondiendo 410; en `backend/pruebas/Integracion/Cuenta/RegistroConInvitacionPruebas.cs`, el registro de un presidente deja de exigir y de guardar el responsable; en `RechazoPruebas.cs`, `SalaDeEsperaPruebas.cs`, `ConsultaIngresosPruebas.cs`, `AprobacionPruebas.cs` y `backend/pruebas/Integracion/Categorias/PersonaRetiradaPruebas.cs`, toda persona en espera que se creaba registrándola pasa a sembrarse, y quien vuelve con una invitación nueva tras un rechazo entra ya aprobado
- [X] T008 [US2] Aceptación directa para quien ya tiene cuenta, y retirada de lo que quedaba de la sala de espera en la regla (`POST /api/invitaciones/aceptacion`; depende de T007). Backend: en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAceptacionInvitacion.cs`: fija en `IContextoClub` el club de la invitación válida; la comprobación "una invitación del club no cambia a quien ya es integrante" pasa a usar `ReglaIngresoPorInvitacion.EsDelClub` y sigue respondiendo `409 ya_perteneces_al_club` o `409 persona_retirada`; el integrante nuevo nace con el rol de la invitación y `APROBADO` (RF-010); dentro de la transacción: bloquear el club, marcar la invitación como usada, agregar el integrante, guardar y, si es nuevo y de JUGADOR, `UbicadorDeJugadores.UbicarAUnoAsync`, con la fecha de nacimiento que ya se copia de su integrante más reciente; la rama de quien ya es integrante y acepta una invitación de PRESIDENTE no cambia de resultado. No se pide ningún dato: la cuenta conserva el responsable que tenga (supuesto 3). No se crea otra cuenta ni cambia nada en sus otros clubes. Eliminar `EstadoDeIngreso` y `PasaPorSalaDeEspera` de `backend/src/LFClub.Dominio/Reglas/ReglaIngresoPorInvitacion.cs`, que ya no tienen ningún uso, con sus casos en `ReglaIngresoPorInvitacionPruebas.cs`, y actualizar la documentación XML de la regla, del servicio y de `IServicioAceptacionInvitacion`. Frontend: la pantalla ya quedó en T007; comprobar contra la API real que, al aceptar, `frontend/src/cuenta/AceptarInvitacion.tsx` lleva a la aplicación del club y no a la pantalla de espera, y que con una invitación de JUGADOR no aparece ningún formulario. Pruebas en `backend/pruebas/Integracion/Cuenta/AceptacionInvitacionPruebas.cs`: `Con_una_invitacion_del_club_queda_en_espera_…` pasa a ser, con cada uno de los tres roles, "entra aprobada con el rol de la invitación, la respuesta trae `estadoIngreso: APROBADO`, y conserva su rol y sus datos en el otro club con la misma cuenta" (2.8); quien ya está en el club sigue recibiendo 409 con cada rol y conserva el suyo. Crear `backend/pruebas/Integracion/Cuenta/AceptacionDirectaPruebas.cs`: quien acepta una invitación de JUGADOR queda en la categoría activa de su año o en "Sin categoría"; quien acepta una de ENTRENADOR o DIRECTIVO no tiene categoría; el `NombreResponsable` de la cuenta no cambia; en un club suspendido la aceptación se completa y al entrar recibe `403 club_suspendido`; tras aceptar, la sala de espera del club sigue vacía. `PersonaRetiradaPruebas.cs` sigue en verde
  - Nota (2026-10-09): "no se pide ningún dato" y "no aparece ningún formulario" dejaron de ser ciertos para un JUGADOR menor de 18 años cuya cuenta no tiene responsable. Lo cambió T016 (RF-026).

**Punto de control**: las historias 1 y 2 son el mínimo utilizable: el presidente invita con rol y
cada persona entra directamente con el suyo (quickstart, bloques 1 y 2)

---

## Fase 5: Historia 3 - La sala de espera queda solo para jugadores, la atiende el presidente y nadie elige rol al aprobar (Prioridad: P2)

**Objetivo**: solo el PRESIDENTE ve la sala de espera y los aprobados, aprueba y rechaza; al
aprobar no se elige rol y la persona entra siempre como JUGADOR y se ubica.

**Prueba independiente**: con una persona en espera preparada como dato de prueba, se inicia
sesión como PRESIDENTE, se comprueba que al aprobar no se ofrece ningún rol y que la persona entra
como JUGADOR y queda en la categoría de su año; se comprueba que un DIRECTIVO no puede aprobarla.

- [X] T009 [US3] Solo el PRESIDENTE ve la sala de espera y los aprobados, aprueba y rechaza (los cuatro endpoints de `/api/clubes/{clubId}/ingresos`; depende de T006). Backend: en `backend/src/LFClub.Api/Controladores/Club/ControladorIngresos.cs`, el atributo pasa a `[IntegranteDelClub(Rol.PRESIDENTE)]` (RF-017, RF-018) y se actualiza su documentación y la de `IServicioRechazoIngreso` y `ServicioRechazoIngreso` (solo cambia quién rechaza). Frontend: el apartado ya es exclusivo del presidente desde T006; comprobar contra la API real que el presidente sigue viendo las tres secciones. Pruebas: en `backend/pruebas/Integracion/Ingresos/SoloPresidenteIngresosPruebas.cs`, con una persona en espera sembrada, un DIRECTIVO, un ENTRENADOR y un JUGADOR reciben `403 rol_no_autorizado` en la sala de espera, en los aprobados, al aprobar y al rechazar, sin ningún dato en la respuesta, y la persona sigue `EN_ESPERA` (escenario 3.7, CE-003); con esto el archivo cubre los ocho endpoints con cada rol. Reescribir lo que contradice la 4.1.0: en `ConsultaIngresosPruebas.cs`, la teoría por rol de la sala de espera pasa a presidente y `Nadie_mas_ve_…` incluye al DIRECTIVO; en `AprobacionPruebas.cs` y `RechazoPruebas.cs`, las teorías con `quienAprueba` o `quienRechaza` DIRECTIVO pasan a presidente y las de "no aprueban" / "no rechazan" incluyen al DIRECTIVO; en `backend/pruebas/Integracion/Categorias/UbicacionAutomaticaPruebas.cs`, las aprobaciones hechas con `e.Directivo` pasan a `e.Presidente`; en `IngresosPorEstadoDelClubPruebas.cs`, lo mismo. Buscar con `Grep` cualquier otra prueba en `backend/pruebas/Integracion/` que apruebe, rechace o consulte ingresos como directivo y corregirla igual
- [X] T010 [US3] Aprobar sin elegir rol: siempre JUGADOR y ubicado (`POST /api/clubes/{clubId}/ingresos/{usuarioRolId}/aprobacion`; depende de T009). Backend: `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioIngresos.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioIngresos.cs`: `AprobarAsync` pierde el parámetro del rol y deja `Rol` y `RolDeIngreso` "siempre en `JUGADOR`" (RF-016). `backend/src/LFClub.Aplicacion/DTOs/AprobarIngresoDto.cs` conserva `Rol? Rol`, ahora opcional. En `backend/src/LFClub.Aplicacion/Servicios/IServicioAprobacionIngreso.cs` y `Implementaciones/ServicioAprobacionIngreso.cs`: el cuerpo puede no venir; sin cuerpo, sin `rol` o con `rol: JUGADOR` aprueba; con cualquier otro rol responde `403 rol_no_asignable` y la persona sigue en espera (escenario 3.3, supuesto 4); desaparece el `400` por no indicar rol; siempre llama a `UbicadorDeJugadores.UbicarAUnoAsync`; la transacción, el bloqueo del club, el `409 ingreso_ya_aprobado` y el nombre de quien aprueba no cambian. Mover el error a `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeIngreso.cs` como `RolNoAsignable()`. En `ControladorIngresos.cs`, el cuerpo de `Aprobar` pasa a opcional con `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]`, como ya hace el reenvío de la plataforma. **Eliminar** `backend/src/LFClub.Dominio/Reglas/ReglaAprobacionIngreso.cs` y `backend/pruebas/Unitarias/Reglas/ReglaAprobacionIngresoPruebas.cs` (research §6): el compilador señala cualquier uso que quede. Las filas aprobadas antes "conservan el `RolDeIngreso` y el `AprobadoPorNombre` que tuvieran": no se toca `ListarAprobadosAsync` ni `MapperIngresos.AIngresoAprobado`. Actualizar la documentación XML del servicio, del DTO y del repositorio. Frontend: en `frontend/src/privado/ingresos/SeccionSalaDeEspera.tsx`, aprobar pasa a un `DialogoConfirmacion` que nombra a la persona y dice que entrará como jugador, sin ofrecer ningún rol (escenario 3.1), y llama a la API sin cuerpo; pierde la propiedad `miRol`, que `frontend/src/privado/ingresos/Ingresos.tsx` deja de pasar; se **elimina** `frontend/src/privado/ingresos/DialogoAprobarIngreso.tsx`; en `tipos.ts`, `AprobarIngresoDto.rol` pasa a opcional. El rechazo y su confirmación no cambian (3.4), ni el texto de la sala vacía (3.5). Pruebas en `AprobacionPruebas.cs`: sin cuerpo, con `{}`, con `rol: null` y con `rol: "JUGADOR"` la persona queda `APROBADO` con rol JUGADOR, `rolDeIngreso` JUGADOR, quién aprobó y cuándo (3.2, CE-007); con `ENTRENADOR`, `DIRECTIVO` y `PRESIDENTE`, 403 `rol_no_asignable` y sigue en espera; se elimina el caso del 400 sin rol; aprobar dos veces, las dos aprobaciones simultáneas y el 404 siguen igual. En `UbicacionAutomaticaPruebas.cs`: `Aprobar_con_otro_rol_no_asigna_categoria_…` se sustituye por "aprobar deja al jugador en la categoría activa de su año o en Sin categoría" sin enviar rol (lo que comprobaba de ENTRENADOR y DIRECTIVO ya lo cubre `RegistroDirectoUbicacionPruebas`). En `ConsultaIngresosPruebas.cs`: las aprobaciones anteriores con rol de ingreso ENTRENADOR o DIRECTIVO y aprobadas por un directivo se siembran directamente y la lista las sigue mostrando intactas (3.6, RF-019); quien entró con una invitación no aparece en ella. `RechazoPruebas.cs` sigue en verde, incluida la de aprobar y rechazar a la vez

**Punto de control**: la sala de espera es del presidente y aprueba siempre como jugador
(quickstart, bloque 3)

---

## Fase 6: Historia 4 - El desarrollador solo invita al crear el club (Prioridad: P3)

**Objetivo**: desaparece invitar a un presidente a un club que ya existe; crear un club sigue
invitando a su presidente y esa invitación se puede reenviar o corregir mientras no se use.

**Prueba independiente**: se inicia sesión como DESARROLLADOR, se abre un club que ya existe y se
comprueba que no hay forma de invitar a otro presidente; se crea un club nuevo y se comprueba que
su invitación se puede reenviar y corregir.

- [X] T011 [US4] Eliminar la invitación de un presidente a un club que ya existe (`POST /api/plataforma/clubes/{clubId}/invitaciones`, que deja de existir; research §12; no depende de las historias anteriores). Backend: quitar la acción `Invitar` de `backend/src/LFClub.Api/Controladores/Plataforma/ControladorInvitacionesClub.cs` (conserva `Reenviar`); quitar `InvitarAsync` de `backend/src/LFClub.Aplicacion/Servicios/IServicioInvitacionPresidente.cs` y de `Implementaciones/ServicioInvitacionPresidente.cs`, junto con lo que solo usaba ese método (si `ya_es_presidente` o algún método de repositorio queda sin uso, se elimina también); eliminar `backend/src/LFClub.Aplicacion/DTOs/InvitarPresidenteDto.cs`. `ComprobarCorreoInvitableAsync`, `PrepararAsync`, `EnviarAsync` y `ReenviarAsync` no cambian: crear un club sigue enviando la invitación en su misma transacción (RF-024) y el reenvío con corrección de correo sigue igual (RF-025). Actualizar la documentación XML del controlador, la interfaz y el servicio. Contrato: retirar de `specs/001-base-multiclub/contracts/api.yaml` la ruta `/api/plataforma/clubes/{clubId}/invitaciones` (solo el `post` de invitar; la ruta de `…/reenvio` se queda) y el esquema `InvitarPresidenteDto`, dejando un comentario que remita a la 004. Frontend: en `frontend/src/plataforma/SeccionInvitaciones.tsx`, desaparece el formulario "Invitar a otro presidente" con su estado y su función; quedan la lista de invitaciones sin usar, "Reenviar" y "Corregir correo" (4.1), y su comentario se actualiza; en `tipos.ts` se elimina `InvitarPresidenteDto`. Pruebas en `backend/pruebas/Integracion/Plataforma/InvitacionesPruebas.cs`: se eliminan los casos que invitaban (`Se_puede_invitar_a_otro_presidente_…`, `Invitar_a_quien_ya_es_presidente_…` y la mitad de invitar de los demás); se añade que `POST /api/plataforma/clubes/{clubId}/invitaciones`, llamada por el DESARROLLADOR sobre un club que existe, responde 404 o 405 y no envía ningún correo ni crea ninguna invitación (4.2, CE-010); siguen igual reenviar, corregir el correo (incluido el `409` del correo del desarrollador al corregir), la vencida que se reenvía, la ya usada con 409, la de otro club con 404, el correo mal escrito al corregir con 400 y "solo el desarrollador reenvía" (4.3); `Las_invitaciones_que_envia_el_club_no_se_ven_ni_se_tocan_desde_el_panel` pierde su último bloque, que invitaba. Las pruebas de crear club de la 001 siguen en verde sin tocarlas (4.4). Si alguna otra prueba creaba un segundo presidente llamando a esa ruta, pasa a sembrar la invitación con `Sembrador.CrearInvitacionAsync`. En `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`, el contrato pasa de 55 a 54 endpoints; en `AccesoPlataformaPruebas.cs`, los endpoints del panel pasan de 11 a 10. El contador de 32 endpoints de club no cambia

**Punto de control**: dentro de la aplicación solo invita el presidente (quickstart, bloque 4)

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: lo que afecta a varias historias y la validación de extremo a extremo.

- [X] T012 [P] Repasar la documentación de lo que cambió de responsabilidad (§3). Buscar con `Grep` en `backend/src/` y `frontend/src/` las frases que ya no son ciertas ("sala de espera" referida a quien se registra con invitación, "queda en espera", "no lleva rol", "se decide al aprobar", "directivos" junto a invitar, aprobar o rechazar, "invitar a otro presidente") y corregir cada comentario y cada documentación XML que las contenga, sin tocar código. Deben quedar revisados, como mínimo: `Invitacion.cs`, `UsuarioRol.cs`, `ReglaIngresoPorInvitacion.cs`, `IServicioCorreo.cs`, `ControladorInvitaciones.cs` de Cuenta, `ErroresDeInvitacion.cs` y `frontend/src/privado/SalaDeEspera.tsx` (ahora solo la ve el jugador agregado desde la ficha de un hermano). El mensaje `ya_esta_en_espera` de `ServicioInvitacionesClub.cs` se conserva: sigue siendo cierto para quien esté en espera
- [X] T013 [P] Contrastar la API con `specs/004-invitacion-con-rol/contracts/api.yaml`: arrancar la API, abrir Swagger y comprobar en los once endpoints que cambian los cuerpos, los códigos de estado, los nombres de DTO y los campos del contrato (`rol` en `InvitarAlClubDto` e `InvitacionClubDto`, `pideResponsable` en `InvitacionVigenteDto` y ningún `pasaPorSalaDeEspera`, cuerpo opcional en la aprobación), y que `POST /api/plataforma/clubes/{clubId}/invitaciones` ya no aparece. Corregir el código o, si el contrato tenía un error, el contrato, y anotar la diferencia
  - Resultado (2026-10-08): Swagger expone 54 endpoints y los once coinciden con el contrato en rutas, códigos de estado, DTO y campos. Dos diferencias de forma, que ya tenían la 001 y la 002 y no se corrigen aquí: Swagger no marca ningún cuerpo como obligatorio, y describe `rol` de `InvitacionClubDto` con la enumeración `Rol` completa donde el contrato dice `RolDeIngreso`.
- [X] T014 Revisar a 360 px de ancho, en tema claro y en oscuro, con la identidad de un club con colores propios (RF-022, CE-009; quickstart, bloque 6): el formulario de invitar con su selector de rol y sus errores, la tabla de invitaciones con la columna "Rol", la confirmación de aprobar, la sala de espera vacía, la pantalla del enlace de invitación con cada rol (con y sin el campo del responsable), la aceptación con cuenta existente y el detalle de un club en el panel sin el formulario de invitar. Sin desplazamiento horizontal, con texto legible y con el rol leído como texto, no solo por color. Corregir lo que falle en los archivos de `frontend/src/` de cada pantalla
- [X] T015 Recorrer `specs/004-invitacion-con-rol/quickstart.md`, bloques 1 a 6, y ejecutar la batería entera (`dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `scripts/verificar-tamano.ps1`). Levantar el recorrido en un proyecto de Compose aparte, con la llave de Brevo vacía y su propio volumen, para no enviar correos reales ni tocar los datos de desarrollo, y borrarlo al terminar. En el paso 1.7 la pantalla devuelve al directivo al inicio del club en lugar de mostrar un mensaje (así quedó en T006): ajustar la redacción de ese paso del quickstart. Anotar en este archivo, debajo de esta tarea, cualquier paso que no dé el resultado esperado, y corregirlo antes de marcarla
  - Resultado (2026-10-08): bloques 1 a 5 recorridos por la API contra un proyecto de Compose aparte (31 comprobaciones) y bloque 6 con Chrome a 360 px en los dos temas (17 pantallas); todo da lo esperado. Batería: 165 unitarias, 375 de integración, 91 de frontend, `tsc` y tamaño correctos.
  - Paso 1.4: sin llave de Brevo el registro de la API solo escribe destinatario, asunto y enlace, no el texto del correo, así que el rol no se puede leer ahí. Se ajustó la redacción del paso; el texto lo comprueba `PlantillasCorreoPruebas`.
  - Paso 1.7: redacción ajustada (la pantalla devuelve al directivo al inicio del club).
- [X] T016 [US2] Un JUGADOR menor de 18 años no entra sin responsable al aceptar con una cuenta que ya existe (RF-026, escenario 2.12; `POST /api/invitaciones/consulta` y `POST /api/invitaciones/aceptacion`; añadida el 2026-10-09 tras la revisión del código, sustituye al supuesto 3). Backend: `backend/src/LFClub.Dominio/Reglas/ReglaIngresoPorInvitacion.cs` gana `ExigeResponsable(rol, fechaNacimiento, hoy)`, verdadero solo para un JUGADOR menor de edad, y `ValidadorRegistro` pasa a usarla; se crea `backend/src/LFClub.Aplicacion/DTOs/AceptarInvitacionDto.cs` (`Token` y `NombreResponsable` opcional), que la aceptación recibe en lugar de `TokenDto`; `InvitacionVigenteDto` gana `FaltaResponsable`, que `ServicioRegistroConInvitacion.ConsultarAsync` rellena con la cuenta del correo invitado y la fecha de nacimiento de su integrante más reciente; `ServicioAceptacionInvitacion` exige el responsable cuando el integrante nuevo es un JUGADOR menor y la cuenta no lo tiene (`400 datos_invalidos` en `nombreResponsable`, máximo 160, sin gastar la invitación), lo guarda en la cuenta dentro de la misma transacción e ignora el que llegue en cualquier otro caso. Frontend: `tipos.ts` gana `faltaResponsable` y `AceptarInvitacionDto`; `frontend/src/cuenta/AceptarInvitacion.tsx` muestra el campo del responsable solo con `faltaResponsable`, lo envía y pinta su error junto al campo. Pruebas: `ExigeResponsable` en `ReglaIngresoPorInvitacionPruebas.cs`; crear `backend/pruebas/Integracion/Cuenta/AceptacionResponsablePruebas.cs`: un menor sin responsable recibe 400, no entra y la invitación no se gasta, y con el nombre entra y queda guardado; más de 160 caracteres se rechaza; a un adulto, a quien entra como ENTRENADOR o DIRECTIVO y a un menor cuya cuenta ya tiene responsable no se les pide, lo enviado se ignora y la consulta trae `faltaResponsable` falso. Se actualizan spec.md, research.md (decisión 5), data-model.md, contracts/api.yaml y quickstart.md (paso 2.12)
  - Resultado (2026-10-09): 170 unitarias, 382 de integración y 91 de frontend en verde; `tsc`, `eslint` y tamaño correctos. El paso 2.12 del quickstart no se recorrió en el navegador durante la tarea; el propietario lo recorrió después y lo confirmó el 2026-10-09.

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (fase 1)**: sin dependencias.
- **Cimientos (fase 2)**: depende de la fase 1. Bloquea las historias 1, 2 y 3.
- **Historia 1 (fase 3)**: depende de los cimientos.
- **Historia 2 (fase 4)**: depende de la historia 1. Las dos son el mínimo utilizable y van juntas:
  con solo la 1, una invitación de ENTRENADOR dejaría a la persona en la sala de espera.
- **Historia 3 (fase 5)**: depende de la historia 2, porque sus pruebas ya parten de personas en
  espera sembradas y de que el apartado "Ingresos" sea solo del presidente.
- **Historia 4 (fase 6)**: solo depende de la fase 1. Puede hacerse en cualquier momento.
- **Cierre (fase 7)**: depende de las historias que se quieran entregar.

### Entre tareas

```text
T001 ─┬─ T002 ─┐
      ├─ T003 ─┼─ T005 ── T006 ── T007 ── T008 ── T009 ── T010 ─┐
      ├─ T004 ─┘                                                ├─ T012, T013 ── T014 ── T015
      └─ T011 ──────────────────────────────────────────────────┘
```

- T005 → T006: primero el rol en la invitación, después quién puede enviarla.
- T007 → T008: el registro deja de usar las dos preguntas antiguas de la regla; la aceptación
  también, y entonces se eliminan.
- T009 → T010: primero solo aprueba el presidente; después se elimina `ReglaAprobacionIngreso`,
  que hasta entonces sigue diciendo qué rol puede dejar cada quien.
- T016 no está en el diagrama: es de la historia 2, depende de T008 y se añadió el 2026-10-09,
  después del cierre, tras la revisión del código.

### Dentro de cada tarea

- Regla o DTO → servicio → controlador → pantalla → pruebas nuevas → pruebas reescritas.
- La batería completa en verde antes de pasar a la tarea siguiente.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos: tres archivos distintos, sin dependencias entre sí
T002  ReglaInvitacionDelClub.cs y ErroresDeInvitacion.cs
T003  ReglaIngresoPorInvitacion.cs
T004  EscenarioIngresos.cs

# La historia 4 no comparte archivos de producción con las demás
T011  en paralelo con T005 a T010
      (coincide con ellas solo en tipos.ts y, con T012, en comentarios)

# Cierre
T012  documentación        T013  contrato frente a Swagger
```

Dentro de las historias 1, 2 y 3 no hay tareas en paralelo: cada una reescribe pruebas de los
mismos archivos de `backend/pruebas/Integracion/Ingresos/`.

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T005, T006) e historia 2 (T007, T008), seguidas y sin publicar entre una y otra.
3. **Parar y validar**: quickstart, bloques 1 y 2.

### Entrega incremental

1. Historias 1 y 2 → el presidente invita con rol y cada quien entra directamente.
2. Historia 3 → la sala de espera queda en manos del presidente y aprueba siempre como jugador.
3. Historia 4 → el desarrollador solo invita al crear el club.
4. Cierre → documentación, contrato, 360 px y quickstart completo.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- Sin migración: sirve la base de datos que deja la 003.
- Tras esta funcionalidad cada club tiene un solo presidente hasta que exista "un PRESIDENTE elige
  a otro" (§12.5); quitarle el rol a un presidente desde el panel queda sin uso. Es una
  consecuencia asumida en el plan, no una tarea.
- Si al reescribir una prueba no está claro si contradice la constitución 4.1.0 o si destapa un
  defecto, se detiene esa parte y se pregunta (§25).
- Confirmar el cambio en git al terminar cada tarea o cada historia.
