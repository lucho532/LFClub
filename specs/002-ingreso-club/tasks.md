---

description: "Lista de tareas de la funcionalidad 002: ingreso de personas al club"
---

# Tareas: Ingreso de personas al club

**Entrada**: documentos de diseño en `specs/002-ingreso-club/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20) y el plan. No van en tareas aparte: por
§27.1, cada tarea de una historia es un corte vertical que entrega el backend, la pantalla que lo
usa contra la API real y sus pruebas.

**Organización**: las tareas se agrupan por historia de usuario para poder implementar y probar
cada historia por separado.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de `specs/001-base-multiclub/tasks.md`, que siguen vigentes. Resumen y lo que se añade:

- **Nombres** (§2.1): todo en español: `ServicioX`, `IServicioX`, `RepositorioX`, `IRepositorioX`,
  `ValidadorX`, `MapperX`, `ControladorX`, `XDto`. Los hooks de React conservan el prefijo `use`.
- **Carpetas de Aplicacion**: `Servicios/` guarda los contratos `IServicio*`; `Implementaciones/`,
  sus clases; `Interfaces/`, los `IRepositorio*` y los puertos. Cada servicio nuevo se registra en
  `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs` y cada repositorio nuevo en
  `backend/src/LFClub.Api/Configuracion/RegistroDeServicios.cs`.
- **Documentación** (§3): cada clase, interfaz y enumeración nueva lleva documentación XML en
  español (qué representa, su responsabilidad y qué no debe asumir). En las clases que cambian de
  responsabilidad se actualiza. La compilación falla si falta.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Si un archivo se
  acerca, se divide por responsabilidad dentro de la misma carpeta.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores no
  tienen reglas de negocio. Entidad → Mapper → DTO; ninguna entidad sale por la API.
- **Contrato**: `specs/002-ingreso-club/contracts/api.yaml` manda en rutas, DTOs, códigos de
  estado y códigos de error de lo nuevo y lo que cambia. Los DTOs van en
  `backend/src/LFClub.Aplicacion/DTOs/` con los nombres del contrato.
- **Aislamiento** (§7.1): los repositorios nuevos (`RepositorioInvitacionesClub`,
  `RepositorioIngresos`) van en `backend/src/LFClub.Infraestructura/Repositorios/`, trabajan con
  el filtro global activo y **no** usan `IgnoreQueryFilters()`.
- **Autorización**: todos los endpoints nuevos llevan
  `[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO)]` (T006). La cuenta y el integrante de quien
  llama se leen con `ValidadorSesion.UsuarioDe` e `IntegranteDelClubAttribute.IntegranteDe`.
- **No regresión** (§27.2): las pruebas de la 001 siguen en verde sin cambiar lo que comprueban,
  salvo los contadores de endpoints de `AccesoClubPruebas.cs`, que cada tarea indica.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, y pasa
  `scripts/verificar-tamano.ps1`.

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: partir de una base en verde y dejar listos los datos de prueba. No hay proyectos,
paquetes ni tecnologías nuevas.

- [X] T001 Comprobar la línea base en la rama `002-ingreso-club`: ejecutar `dotnet test backend/LFClub.sln`, `npm --prefix frontend test` y `scripts/verificar-tamano.ps1`, y confirmar que todo pasa antes de tocar código. Si algo falla, detenerse e informarlo: no se construye sobre una base rota
- [X] T002 [P] Ampliar `backend/pruebas/Integracion/Base/Sembrador.cs`, sin cambiar el resultado de las llamadas que ya existen: `CrearIntegranteAsync` admite un `EstadoIngreso` opcional (por defecto `APROBADO`); `CrearInvitacionAsync` admite el `Rol` de la invitación (por defecto `PRESIDENTE`), la cuenta que la crea y, opcionalmente, `VenceEn`, `UsadaEn` y `AnuladaEn`; y un método nuevo `CrearIntegranteEnEsperaAsync(Club club)` que devuelve la cuenta y su `UsuarioRol` con rol `JUGADOR` y `EN_ESPERA`. Si el archivo se acerca a 250 líneas, mover lo nuevo a `backend/pruebas/Integracion/Base/SembradorIngresos.cs`

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: modelo, reglas de dominio, sala de espera impuesta en la autorización y
`estadoIngreso` en la sesión.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase

- [X] T003 [P] Crear `backend/src/LFClub.Dominio/Enumeraciones/EstadoInvitacion.cs` (`PENDIENTE`, `USADA`, `VENCIDA`, `CANCELADA`; derivada, no se guarda) y añadir a `backend/src/LFClub.Dominio/Entidades/Invitacion.cs` el método `EstadoEn(DateTime ahoraUtc)` con esta derivación exacta: "USADA = UsadaEn tiene valor; CANCELADA = AnuladaEn tiene valor; VENCIDA = ninguna de las anteriores y ahora >= VenceEn; PENDIENTE = ninguna de las anteriores". Actualizar la documentación XML de la entidad: `Rol` "Ya no es siempre `PRESIDENTE`. Una invitación enviada desde el club lleva `JUGADOR` (RF-003)"; `AnuladaEn` "Además de 'reemplazada por otra', ahora también significa 'cancelada por quien invita'"; `CreadaPorUsuarioId` "Puede ser la cuenta de un PRESIDENTE o de un DIRECTIVO del club". No se añade ninguna columna. Pruebas en `backend/pruebas/Unitarias/Entidades/InvitacionPruebas.cs`: los cuatro estados, que usada gana a anulada y a vencida, y el instante exacto `ahora == VenceEn` (vencida)
- [X] T004 [P] Crear las reglas de dominio en `backend/src/LFClub.Dominio/Reglas/`, sin dependencias de HTTP ni de EF: `ReglaIngresoPorInvitacion.cs` (único lugar que deriva el estado de ingreso del rol de la invitación: "`PRESIDENTE` → `APROBADO`; cualquier otro rol → `EN_ESPERA`"; expone también si una invitación pasa por la sala de espera); `ReglaAprobacionIngreso.cs` (roles que puede dejar cada quien: PRESIDENTE → `JUGADOR`, `ENTRENADOR`, `DIRECTIVO`; DIRECTIVO → `JUGADOR`, `ENTRENADOR`; `PRESIDENTE` nunca es asignable (RF-023); ningún otro rol aprueba; nadie aprueba su propio ingreso (RF-021)); y `ReglaMayoriaDeEdad.cs` (`EsMenorDeEdad(DateOnly fechaNacimiento, DateOnly hoy)`: menor de 18 años cumplidos ese día). Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaIngresoPorInvitacionPruebas.cs`, `ReglaAprobacionIngresoPruebas.cs` (toda la tabla quien-aprueba × rol, incluidos ENTRENADOR y JUGADOR como aprobadores y el caso de aprobarse a uno mismo) y `ReglaMayoriaDeEdadPruebas.cs` (el día antes de cumplir 18, el día del cumpleaños, nacido un 29 de febrero)
- [X] T005 [P] Ampliar el modelo y crear la migración. En `backend/src/LFClub.Dominio/Entidades/Usuario.cs`: `NombreResponsable` texto (160), "Opcional. Nombre del padre, madre o responsable. Obligatorio al registrarse si la persona es menor de 18 años ese día (RF-010)". En `backend/src/LFClub.Dominio/Entidades/UsuarioRol.cs`: `EstadoIngreso` "Ya existía, siempre `APROBADO`. Ahora puede ser `EN_ESPERA` (RF-014)" (solo cambia su documentación); `AprobadoEn` fecha y hora, "Nuevo. Nulo mientras está en espera y en quien entró sin sala de espera"; `AprobadoPorUsuarioId` Guid, "Nuevo. Opcional. Foránea a `Usuario`; pasa a nulo si esa cuenta se elimina"; `AprobadoPorNombre` texto (161), "Nuevo. Nombres y apellidos de quien aprobó, copiados en ese momento (§13)"; `RolDeIngreso` `Rol` opcional, "Nuevo. Rol con el que quedó al aprobarse: `JUGADOR`, `ENTRENADOR` o `DIRECTIVO` (RF-025)". Configurar en `backend/src/LFClub.Infraestructura/Datos/Configuraciones/ConfiguracionUsuario.cs` y `ConfiguracionUsuarioRol.cs` las longitudes, `RolDeIngreso` guardado como texto, la foránea `AprobadoPorUsuarioId` → `Usuario` con borrado `SetNull`, y el índice nuevo `(ClubId, EstadoIngreso)`. Crear la migración `IngresoAlClub` en `backend/src/LFClub.Infraestructura/Datos/Migraciones/` del mismo modo que las de la 001: una columna en `Usuarios`, cuatro columnas y un índice en `UsuariosRol`, sin tablas nuevas y dejando nulos los campos nuevos de las filas existentes. `backend/pruebas/Integracion/Aislamiento/ModeloPruebas.cs` debe seguir pasando sin tocarlo
- [X] T006 Imponer la sala de espera en la autorización (depende de T002). `backend/src/LFClub.Dominio/Reglas/ResultadoAcceso.cs` gana `IngresoEnEspera`; `backend/src/LFClub.Dominio/Reglas/ReglaAccesoPorEstado.cs` pasa a recibir también el `EstadoIngreso` y evalúa **primero el estado del club y después el estado de ingreso**: una cuenta en espera de un club suspendido recibe `ClubSuspendido`, y de uno dado de baja `ClubDadoDeBaja` (supuesto 2 de research.md). `backend/src/LFClub.Api/Autorizacion/IntegranteDelClubAttribute.cs`: el constructor pasa de un rol a `params Rol[]` (sin roles = cualquier integrante; `[IntegranteDelClub(Rol.PRESIDENTE)]` de la 001 sigue compilando igual) y responde 403 `ingreso_en_espera` ("Tu ingreso está pendiente de aprobación.") a un integrante `EN_ESPERA` en **cualquier** endpoint `/api/clubes/{clubId}/**`, antes de mirar los roles exigidos y sin fijar el club en `IContextoClub`; 403 `rol_no_autorizado` si el rol no está en la lista. No se abre ningún endpoint de club para las cuentas en espera. Pruebas: ampliar `backend/pruebas/Unitarias/Reglas/ReglaAccesoPorEstadoPruebas.cs` con el estado de ingreso en los tres estados del club; crear `backend/pruebas/Integracion/Ingresos/SalaDeEsperaPruebas.cs`, que recorre con `EndpointsDeLaApi.DeLaApi` todos los endpoints que empiezan por `/api/clubes/{clubId}` y comprueba que una cuenta en espera recibe 403 `ingreso_en_espera` en todos y que la respuesta no contiene el nombre del club (CE-005, RF-016); que en un club suspendido recibe `club_suspendido`; y que un integrante `APROBADO` sigue entrando
- [X] T007 [P] `estadoIngreso` en la sesión (`GET /api/sesion`; depende de T002). Backend: añadir `EstadoIngreso` a `backend/src/LFClub.Aplicacion/DTOs/ClubDeSesionDto.cs` y rellenarlo en `backend/src/LFClub.Aplicacion/Mappers/MapperSesion.cs`. Frontend: en `frontend/src/compartido/api/tipos.ts`, añadir `estadoIngreso` a `ClubDeSesionDto` y todos los tipos nuevos o cambiados del contrato 002 con sus mismos nombres (`EstadoIngreso`, `EstadoInvitacion`, `RolDeIngreso`, `InvitarAlClubDto`, `InvitacionClubDto`, `IngresoEnEsperaDto`, `AprobarIngresoDto`, `IngresoAprobadoDto`, `pasaPorSalaDeEspera` en `InvitacionVigenteDto` y `nombreResponsable` en `RegistrarConInvitacionDto`). Pruebas en `backend/pruebas/Integracion/Cuenta/SesionPruebas.cs`: la sesión de una cuenta aprobada en un club y en espera en otro devuelve los dos clubes, cada uno con su `estadoIngreso`, su nombre y su identidad (RF-018)
- [X] T008 [P] Hacer que la prueba de contrato lea todas las specs. En `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs`, `DelContrato()` une `specs/*/contracts/api.yaml` sin repetir los endpoints que el contrato 002 vuelve a describir (un endpoint es anónimo si algún contrato lo marca con `security: []`). En `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`: el contrato pasa de 25 a 33 endpoints; añadir la lista expresa `PendientesDeConstruir` con los 8 endpoints nuevos del contrato 002, y `La_api_expone_exactamente_los_endpoints_del_contrato` compara la API con el contrato menos esa lista. Cada historia quita de la lista los endpoints que construye; al terminar la historia 4 la lista queda vacía y se elimina. La prueba de endpoints anónimos no cambia: no hay ninguno nuevo

**Punto de control**: todas las pruebas de la 001 siguen en verde; una cuenta sembrada en espera
recibe 403 en todo el club y la sesión dice en qué clubes está en espera

---

## Fase 3: Historia 1 - El club invita a una persona por correo (Prioridad: P1) 🎯 MVP

**Objetivo**: el PRESIDENTE y los DIRECTIVOS envían invitaciones desde el apartado "Ingresos",
ven su estado y reenvían o cancelan las pendientes.

**Prueba independiente**: se inicia sesión como PRESIDENTE de un club, se envía una invitación a
un correo y se comprueba que llega, que aparece como pendiente en la lista y que el enlace lleva
al registro de ese club.

- [X] T009 [US1] Separar las invitaciones del club de las de presidente (research §1, §2 y §8). Crear `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioInvitacionesClub.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioInvitacionesClub.cs`: trabaja dentro del club de `IContextoClub`, con el filtro activo, y añade **siempre** `Rol != PRESIDENTE`; expone: listar "por cada correo, su invitación más reciente", de la más reciente a la más antigua; obtener una por identificador; anular las pendientes de un correo; agregar; el estado de ingreso del integrante del club cuya cuenta tiene ese correo (o nulo); y el nombre del integrante del club cuya cuenta es `CreadaPorUsuarioId` (nulo si ya no está en el club). En `backend/src/LFClub.Infraestructura/Repositorios/Plataforma/RepositorioInvitacionesPlataforma.cs`, sus tres consultas (`ListarPendientesAsync`, `ObtenerAsync`, `AnularPendientesAsync`) añaden `Rol == PRESIDENTE` (RF-029). Extraer de `backend/src/LFClub.Aplicacion/Implementaciones/ServicioInvitacionPresidente.cs` la construcción de la invitación (token de 32 bytes, hash SHA-256, `VenceEn` = `CreadaEn` + 7 días, `EstadoEnvio = PENDIENTE`) a `backend/src/LFClub.Aplicacion/Utilidades/ConstructorInvitaciones.cs`, que recibe el rol, y usarla desde ese servicio sin cambiar su comportamiento. Pruebas en `backend/pruebas/Integracion/Plataforma/InvitacionesPruebas.cs`: con una invitación del club presente, el detalle del panel solo muestra las de presidente, el reenvío del panel responde 404 para la del club, e invitar a un presidente al mismo correo no anula la invitación del club
- [X] T010 [P] [US1] El correo de la invitación del club no nombra ningún rol (RF-003). En `backend/src/LFClub.Infraestructura/Correo/PlantillasCorreo.cs`, la invitación con rol `PRESIDENTE` conserva su texto y la de cualquier otro rol invita a registrarse en el club sin nombrar rol (no dice "para ser jugador"); actualizar la documentación de `backend/src/LFClub.Aplicacion/Interfaces/IServicioCorreo.cs` para decirlo. Pruebas en `backend/pruebas/Unitarias/Correo/PlantillasCorreoPruebas.cs`: el texto de presidente nombra el rol; el del club lleva el nombre del club y el enlace `{Frontend:UrlBase}/invitacion#<token>` y no contiene "jugador", "presidente" ni ningún otro rol
- [X] T011 [US1] Invitar y ver las invitaciones del club (`GET` y `POST /api/clubes/{clubId}/invitaciones`; depende de T009). Backend: `Servicios/IServicioInvitacionesClub.cs`, `Implementaciones/ServicioInvitacionesClub.cs`, `Mappers/MapperIngresos.cs`, `InvitarAlClubDto` (solo `correo`; no lleva rol ni club), `InvitacionClubDto` (`invitacionId`, `correo`, `estado`, `estadoEnvio`, `enviadaPor` nulo si quien la envió ya no está en el club, `creadaEn`, `venceEn`; nunca el token) y `backend/src/LFClub.Api/Controladores/Club/ControladorInvitacionesClub.cs` (espacio de nombres `LFClub.Api.Controladores.Club`; ya existe otro controlador con ese nombre en `Controladores/Plataforma/`, que no se toca). Invitar: correo vacío o no válido 400 `datos_invalidos` con el error en `correo` y no se envía nada; el correo se normaliza con `NormalizadorTexto`; 409 `correo_del_desarrollador`; 409 `ya_esta_en_el_club` si es de un integrante aprobado de ese club y 409 `ya_esta_en_espera` si está en su sala de espera (RF-007), cada uno con su mensaje en español; en una transacción anula las pendientes del club para ese correo y crea la nueva con `Rol = JUGADOR`; el correo se envía después de confirmar la transacción y, si falla, la invitación queda con `estadoEnvio: FALLIDO` (RF-008); responde 201. Listar: una fila por correo, la más reciente. Frontend: `frontend/src/privado/ingresos/Ingresos.tsx` en la ruta `ingresos` de `/club/:clubId` (añadirla en `frontend/src/App.tsx`), que compone secciones; `frontend/src/privado/ingresos/SeccionInvitacionesClub.tsx` con el campo de correo, los errores por campo, el aviso "no se pudo enviar el correo; puedes reenviarla" cuando llega `FALLIDO`, y la lista (componente `Tabla`) con correo, estado con texto además de color (`EtiquetaEstado`), quién la envió, fecha de envío y de vencimiento, y estado vacío; y el enlace "Ingresos" en el menú de `frontend/src/privado/DisposicionClub.tsx`, visible solo para PRESIDENTE y DIRECTIVO. Pruebas en `backend/pruebas/Integracion/Ingresos/InvitacionesClubPruebas.cs`: invitan el PRESIDENTE y un DIRECTIVO y la invitación queda pendiente con vencimiento a 7 días y el enlace enviado; correo vacío y mal escrito; los tres 409; volver a invitar al mismo correo deja una sola fila y el enlace anterior deja de servir (`POST /api/invitaciones/consulta` da 410); fallo de correo; ENTRENADOR y JUGADOR reciben 403 `rol_no_autorizado` sin datos y sin sesión 401, al listar y al invitar; la respuesta nunca contiene el token. En `AccesoClubPruebas.cs`: quitar estos dos endpoints de `PendientesDeConstruir` y subir el contador de endpoints de club de 2 a 4
- [X] T012 [US1] Reenviar y cancelar (`POST /api/clubes/{clubId}/invitaciones/{invitacionId}/reenvio` y `…/cancelacion`). Backend: en `ServicioInvitacionesClub.cs` y `ControladorInvitacionesClub.cs`. "Solo se reenvía o cancela una invitación `PENDIENTE`": usada, vencida o cancelada 409 `invitacion_no_pendiente`; una invitación de otro club, inexistente o de presidente 404 `no_encontrado`. Reenviar anula la indicada y crea otra al mismo correo; 201 con la nueva. Cancelar fija `AnuladaEn`; 200 con la invitación en estado `CANCELADA`. Frontend: en `SeccionInvitacionesClub.tsx`, botones "Reenviar" y "Cancelar" solo en las filas pendientes (cancelar con `DialogoConfirmacion`), y recarga de la lista tras cada acción y tras un 409. Pruebas en `InvitacionesClubPruebas.cs`: reenviar entrega un enlace nuevo, invalida el anterior y deja una sola fila; cancelar deja la fila como cancelada y su enlace da 410; reenviar o cancelar una usada, una vencida y una ya cancelada da 409; lo hace también un DIRECTIVO; la invitación sigue sirviendo aunque quien la envió deje de ser directivo, y entonces `enviadaPor` llega nulo si ya no está en el club. Crear `backend/pruebas/Integracion/Ingresos/AislamientoIngresosPruebas.cs`: el presidente de otro club recibe 404 al listar, reenviar y cancelar con el identificador real de una invitación de este club, y la invitación no cambia (RF-028, escenario 1.7); el DESARROLLADOR recibe 404 en los cuatro endpoints (RF-029). En `AccesoClubPruebas.cs`: quitar estos dos endpoints de `PendientesDeConstruir` y subir el contador de endpoints de club a 6

**Punto de control**: el club envía, reenvía y cancela invitaciones (quickstart, paso 1). El
enlace abre el registro de la 001; lo que pasa al registrarse llega con la historia 2

---

## Fase 4: Historia 2 - La persona invitada se registra y queda en la sala de espera (Prioridad: P1)

**Objetivo**: quien abre el enlace se registra (o acepta con su cuenta), queda como JUGADOR en
espera y solo ve la pantalla de sala de espera de ese club.

**Prueba independiente**: con una invitación recién enviada, se abre el enlace, se completa el
registro y se comprueba que la persona solo ve la pantalla de espera; se cierra la sesión, se
vuelve a entrar y se comprueba que sigue viendo lo mismo.

- [X] T013 [US2] Registro que deja en espera, con responsable (`POST /api/invitaciones/consulta`, `POST /api/invitaciones/registro`). Backend: `backend/src/LFClub.Aplicacion/DTOs/InvitacionVigenteDto.cs` gana `PasaPorSalaDeEspera` y `RegistrarConInvitacionDto.cs` gana `NombreResponsable` (opcional, `maxLength: 160`); `backend/src/LFClub.Aplicacion/Validadores/ValidadorRegistro.cs` exige `nombreResponsable` cuando `ReglaMayoriaDeEdad` dice que la persona es menor de 18 años el día del registro (fecha UTC del servidor, la misma que ya recibe), lo acepta vacío en un adulto y rechaza más de 160 caracteres, con el error en el campo `nombreResponsable` (400 `datos_invalidos`); aplica a todo registro, también al de presidente. En `backend/src/LFClub.Aplicacion/Implementaciones/ServicioRegistroConInvitacion.cs`: el `EstadoIngreso` del integrante sale de `ReglaIngresoPorInvitacion` (invitación del club → rol `JUGADOR` y `EN_ESPERA`; de presidente → `APROBADO`, como en la 001), `Usuario.NombreResponsable` se guarda sin espacios sobrantes (nulo si viene vacío), y la consulta devuelve `pasaPorSalaDeEspera`. El correo, el club y el rol siguen saliendo siempre de la invitación. Frontend: `frontend/src/cuenta/Invitacion.tsx` no nombra ningún rol cuando `pasaPorSalaDeEspera` es verdadero (muestra el nombre del club y el correo, ninguno editable); `frontend/src/cuenta/FormularioRegistro.tsx` añade el campo "Nombre del padre, madre o responsable", marcado como obligatorio cuando la fecha de nacimiento escrita es de un menor de 18, y el texto de RF-011: un jugador se registra con el documento del jugador y el correo de su acudiente; un entrenador o directivo, con su propio documento; al terminar entra a `/club/:clubId`. Pruebas: `backend/pruebas/Unitarias/Validadores/ValidadorRegistroPruebas.cs` (menor sin responsable, menor con responsable, adulto sin responsable, 161 caracteres) y `backend/pruebas/Integracion/Ingresos/RegistroEnEsperaPruebas.cs`: con una invitación del club la persona queda `JUGADOR` y `EN_ESPERA` en ese club y entra con correo y con documento; sin token no hay registro (CE-004); invitación usada, vencida, cancelada y reemplazada dan 410; un correo, un rol o un club enviados en el cuerpo se ignoran; menor sin responsable 400; documento repetido en el club 409 `documento_repetido_en_club`; documento de otra cuenta 409 `documento_en_otra_cuenta`; la consulta devuelve `pasaPorSalaDeEspera` verdadero para la del club y falso para la de presidente; el registro con invitación de presidente sigue quedando `APROBADO`
- [X] T014 [US2] Aceptar una invitación del club con una cuenta que ya existe (`POST /api/invitaciones/aceptacion`, RF-013). Backend: en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAceptacionInvitacion.cs`, con una invitación del club el integrante nuevo se crea con rol `JUGADOR` y `EN_ESPERA` (vía `ReglaIngresoPorInvitacion`), copiando la identidad del integrante más reciente de la cuenta y sin pedir datos de nuevo (tampoco el responsable, supuesto 5); si la cuenta **ya** es integrante de ese club, responde 409 `ya_perteneces_al_club`, no marca la invitación como usada y no cambia nada. La sustitución de rol de la 001 se conserva solo para invitaciones de presidente. Añadir el error a `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeInvitacion.cs`. Frontend: `frontend/src/cuenta/AceptarInvitacion.tsx` no nombra rol con una invitación del club, explica que el ingreso quedará pendiente de aprobación, muestra el mensaje del 409 y, al aceptar, recarga la sesión y entra a `/club/:clubId`. Pruebas en `backend/pruebas/Integracion/Cuenta/AceptacionInvitacionPruebas.cs`: una cuenta aprobada en el club A acepta una invitación del club B, no se crea una segunda cuenta, queda `EN_ESPERA` en B y sigue obteniendo `GET /api/clubes/{A}` con normalidad mientras `GET /api/clubes/{B}` da 403 `ingreso_en_espera` (escenarios 2.7 y 2.8); un PRESIDENTE y un DIRECTIVO del propio club reciben 409 y conservan su rol y su estado; la prueba de sustitución de rol con invitación de presidente sigue en verde
- [X] T015 [P] [US2] Pantalla de sala de espera de la persona (RF-015, RF-018, RF-019). Frontend: crear `frontend/src/privado/SalaDeEspera.tsx` (nombre e identidad del club tomados de la sesión, "Tu ingreso está pendiente de aprobación", botón "Actualizar" que recarga la sesión, botón de tema, desplegable de clubes y cerrar sesión; sin menú). `frontend/src/privado/DisposicionClub.tsx` mira `estadoIngreso` del club en la sesión **antes** de pedir nada al club: si es `EN_ESPERA` y el club no está suspendido ni dado de baja, muestra solo `SalaDeEspera` y no llama a `GET /api/clubes/{clubId}`; si el club está suspendido o dado de baja, muestra el aviso de `AvisoClubNoDisponible.tsx` (supuesto 2); si la API responde 403 `ingreso_en_espera` con una sesión desactualizada, recarga la sesión. Si la recarga de sesión no existe en `frontend/src/compartido/sesion/ProveedorSesion.tsx` y `contextoSesion.ts`, añadirla. Dividir `DisposicionClub.tsx` si se acerca a 250 líneas. `frontend/src/privado/DesplegableClubes.tsx` marca con texto ("en espera") los clubes en los que la persona está en espera. `frontend/src/compartido/sesion/ultimoClub.ts`: sin un último club recordado que siga siendo suyo, `clubDeEntrada` prefiere el primer club con ingreso aprobado y solo si no hay ninguno devuelve el primero (escenario 2.8). Pruebas en `frontend/pruebas/ultimoClub.test.ts` (prefiere el aprobado; respeta el último elegido aunque esté en espera; todos en espera devuelve el primero) y en `backend/pruebas/Integracion/Ingresos/SalaDeEsperaPruebas.cs`: una cuenta en espera inicia sesión con correo y con documento y su sesión trae el club con `EN_ESPERA`; recupera su contraseña por correo y sigue en espera (caso límite)

**Punto de control**: invitación → registro → sala de espera funciona de extremo a extremo
(quickstart, paso 2). Para sacar a alguien de la espera hace falta la historia 3

---

## Fase 5: Historia 3 - El presidente o un directivo aprueba el ingreso (Prioridad: P1)

**Objetivo**: el PRESIDENTE y los DIRECTIVOS ven la sala de espera de su club, aprueban cada
ingreso eligiendo el rol y consultan la lista de ingresos aprobados.

**Prueba independiente**: con una cuenta en espera, se inicia sesión como DIRECTIVO, se aprueba el
ingreso y se comprueba que esa cuenta ya entra a la aplicación del club y desaparece de la sala de
espera.

- [X] T016 [US3] Crear `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioIngresos.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioIngresos.cs`, dentro del club de `IContextoClub` y con el filtro activo: la **sala de espera** ("sus integrantes `EN_ESPERA`, ordenados por `CreadoEn` ascendente", cada uno con su cuenta para leer correo, celular y responsable); los **ingresos aprobados** ("sus integrantes con `AprobadoEn` no nulo, ordenados por `AprobadoEn` descendente"; no incluye a los presidentes que entraron con invitación del DESARROLLADOR, supuesto 6); obtener un integrante del club por identificador; y **aprobar** como una única sentencia condicionada (`ExecuteUpdateAsync` con `Id == id && EstadoIngreso == EN_ESPERA`) que fija a la vez `EstadoIngreso = APROBADO`, `Rol`, `RolDeIngreso`, `AprobadoEn`, `AprobadoPorUsuarioId` y `AprobadoPorNombre`, y devuelve si modificó la fila ("Los cuatro campos de aprobación se rellenan juntos, en la misma sentencia que cambia `EstadoIngreso` a `APROBADO`, y no se modifican después"). Registrar el repositorio
- [X] T017 [US3] Sala de espera del club (`GET /api/clubes/{clubId}/ingresos/en-espera`; depende de T016). Backend: `Servicios/IServicioConsultaIngresos.cs`, `Implementaciones/ServicioConsultaIngresos.cs`, `IngresoEnEsperaDto` (`usuarioRolId`, `nombres`, `apellidos`, `tipoDocumento`, `numeroDocumento`, `fechaNacimiento`, `correo`, `celular`, `nombreResponsable` nulo si no lo tiene, `registradoEn` = `CreadoEn`), su conversión en `Mappers/MapperIngresos.cs` y `backend/src/LFClub.Api/Controladores/Club/ControladorIngresos.cs`. `NombreResponsable` no sale por ningún otro endpoint. Frontend: `frontend/src/privado/ingresos/SeccionSalaDeEspera.tsx`, primera sección de `Ingresos.tsx`: cada persona como ficha apilada (legible a 360 px) con todos sus datos y la fecha de registro, de la más antigua a la más reciente, y estado vacío. Pruebas en `backend/pruebas/Integracion/Ingresos/AprobacionPruebas.cs`: el PRESIDENTE y un DIRECTIVO ven a todas las personas en espera con todos sus campos y en orden; no aparecen los integrantes aprobados; ENTRENADOR y JUGADOR reciben 403 `rol_no_autorizado`, una cuenta en espera 403 `ingreso_en_espera` y sin sesión 401, sin ningún dato. En `AislamientoIngresosPruebas.cs`: el presidente de otro club y el DESARROLLADOR reciben 404. En `AccesoClubPruebas.cs`: quitar este endpoint de `PendientesDeConstruir` y subir el contador de endpoints de club a 7
- [X] T018 [US3] Aprobar un ingreso (`POST /api/clubes/{clubId}/ingresos/{usuarioRolId}/aprobacion`). Backend: `Servicios/IServicioAprobacionIngreso.cs`, `Implementaciones/ServicioAprobacionIngreso.cs`, `AprobarIngresoDto` (`rol`: `JUGADOR`, `ENTRENADOR` o `DIRECTIVO`), `IngresoAprobadoDto` (`usuarioRolId`, `nombres`, `apellidos`, `rolDeIngreso`, `aprobadoPor`, `aprobadoEn`) y la acción en `ControladorIngresos.cs`. `rol` ausente o fuera de la enumeración `Rol` 400 `datos_invalidos`; si `ReglaAprobacionIngreso` no permite ese rol a quien aprueba (un DIRECTIVO con `DIRECTIVO`, o cualquiera con `PRESIDENTE`) 403 `rol_no_asignable`; aprobar el propio ingreso 403 `rol_no_autorizado` (en la práctica quien está en espera ya recibe `ingreso_en_espera`). Aprueba con la sentencia condicionada de T016; `AprobadoPorNombre` son los nombres y apellidos del integrante de quien aprueba en ese club, copiados en ese momento. Si no modifica ninguna fila: 409 `ingreso_ya_aprobado` si el integrante existe en el club, 404 `no_encontrado` si no existe o fue rechazado; nunca un segundo efecto (RF-026). Responde 200 con `IngresoAprobadoDto`. No asigna categoría, no genera cobros y no envía ningún correo. No existe ningún endpoint que cambie el rol de alguien ya aprobado. Frontend: `frontend/src/privado/ingresos/DialogoAprobarIngreso.tsx`, abierto desde "Aprobar" en `SeccionSalaDeEspera.tsx`: muestra a quién se aprueba y pide elegir cómo entra (el PRESIDENTE ve Jugador, Entrenador y Directivo; un DIRECTIVO, solo Jugador y Entrenador), con Jugador por defecto; al aprobar recarga la sala de espera y la lista de aprobados; ante 409 o 404 muestra el mensaje del servidor y recarga. Pruebas en `AprobacionPruebas.cs`: el PRESIDENTE aprueba como JUGADOR, ENTRENADOR y DIRECTIVO y el integrante queda `APROBADO` con ese único rol y los cuatro campos de aprobación; un DIRECTIVO aprueba como JUGADOR y ENTRENADOR; un DIRECTIVO con `DIRECTIVO` y con `PRESIDENTE`, y el PRESIDENTE con `PRESIDENTE`, reciben 403 `rol_no_asignable` y la persona sigue en espera (CE-007); ENTRENADOR, JUGADOR, en espera y sin sesión no aprueban (CE-006); la persona aprobada, con el **mismo token de sesión** que tenía en espera, pasa de 403 a 200 en `GET /api/clubes/{clubId}` (RF-024); aprobar dos veces da 409 y no cambia quién aprobó ni el rol; dos aprobaciones simultáneas (`Task.WhenAll`) dan un 200 y un 409; un `usuarioRolId` inexistente da 404. En `AislamientoIngresosPruebas.cs`: el presidente de otro club recibe 404 con el identificador real y la persona sigue en espera. En `AccesoClubPruebas.cs`: quitar el endpoint de `PendientesDeConstruir` y subir el contador a 8
- [X] T019 [US3] Lista de ingresos aprobados (`GET /api/clubes/{clubId}/ingresos/aprobados`, RF-025a). Backend: método en `ServicioConsultaIngresos.cs`, conversión en `MapperIngresos.cs` y acción en `ControladorIngresos.cs`; `aprobadoPor` sale de `AprobadoPorNombre` y `rolDeIngreso` de `RolDeIngreso`, nunca del rol ni del nombre actuales (§13). Frontend: `frontend/src/privado/ingresos/SeccionIngresosAprobados.tsx`, tercera sección de `Ingresos.tsx`: tabla de solo lectura con nombre, rol con el que entró, quién aprobó y cuándo, del más reciente al más antiguo, sin ninguna acción, y estado vacío. Pruebas en `AprobacionPruebas.cs`: tras aprobar, la persona aparece con su rol de ingreso, quién la aprobó y cuándo; el orden es del más reciente al más antiguo; un presidente que entró con invitación del DESARROLLADOR no aparece; la lista conserva el nombre de quien aprobó aunque esa cuenta se elimine después (`AprobadoPorUsuarioId` pasa a nulo); ENTRENADOR y JUGADOR reciben 403. En `AislamientoIngresosPruebas.cs`: otro club y el DESARROLLADOR, 404. En `AccesoClubPruebas.cs`: quitar el endpoint de `PendientesDeConstruir` y subir el contador a 9

**Punto de control**: las historias 1, 2 y 3 completan el ingreso de una persona (quickstart,
pasos 1 a 3)

---

## Fase 6: Historia 4 - El club no acepta a una persona en espera (Prioridad: P2)

**Objetivo**: el PRESIDENTE o un DIRECTIVO rechaza un ingreso en espera y la persona desaparece
del club sin dejar datos; solo vuelve con una invitación nueva.

**Prueba independiente**: con una cuenta en espera, se rechaza su ingreso y se comprueba que
desaparece de la sala de espera y ya no entra a ese club; después se le invita de nuevo y se
comprueba que puede registrarse otra vez.

- [X] T020 [US4] Extraer la regla "una cuenta que pierde su último integrante se elimina" de `backend/src/LFClub.Aplicacion/Implementaciones/ServicioRetiroPresidente.cs` a un colaborador compartido, `backend/src/LFClub.Aplicacion/Utilidades/EliminadorDeCuentaSinClub.cs` (usa `IRepositorioPertenencias.TieneAlgunaAsync` e `IRepositorioUsuarios`; "La cuenta DESARROLLADOR nunca se toca"; se llama dentro de la transacción de quien lo usa), registrarlo en `RegistroDeCasosDeUso.cs` y usarlo desde `ServicioRetiroPresidente.cs` sin cambiar su comportamiento. `backend/pruebas/Integracion/Plataforma/RetiroPresidentePruebas.cs` debe seguir en verde sin tocarlo
- [X] T021 [US4] Rechazar un ingreso (`POST /api/clubes/{clubId}/ingresos/{usuarioRolId}/rechazo`; depende de T016 y T020). Backend: `Servicios/IServicioRechazoIngreso.cs`, `Implementaciones/ServicioRechazoIngreso.cs`, la acción en `ControladorIngresos.cs` (sin cuerpo) y, en `IRepositorioIngresos.cs` / `RepositorioIngresos.cs` e `IRepositorioInvitacionesClub.cs` / `RepositorioInvitacionesClub.cs`, el borrado condicionado del integrante y el borrado de las invitaciones del club de un correo. En **una sola transacción** (RF-027a): 1) borra el integrante con una sentencia condicionada a `EstadoIngreso == EN_ESPERA`; si no borra nada, 409 `ingreso_ya_aprobado` si existe y está aprobado (RF-027) y 404 `no_encontrado` si no existe; 2) borra "las invitaciones del club, con rol distinto de `PRESIDENTE`, enviadas al correo de su cuenta", usadas o no; 3) con `EliminadorDeCuentaSinClub`, borra la cuenta si se quedó sin ningún integrante (con ella se van por cascada su `FotoPerfil` y sus `SolicitudRecuperacion`). Los integrantes de esa cuenta en otros clubes no se modifican. Responde 204. No envía ningún correo ni aviso y no deja registro del rechazo. Frontend: botón "Rechazar" en `frontend/src/privado/ingresos/SeccionSalaDeEspera.tsx` con `DialogoConfirmacion`, que advierte de que el registro se borrará y de que la persona solo podrá volver con una invitación nueva; al confirmar recarga la sala de espera y la lista de invitaciones; ante 409 o 404 muestra el mensaje y recarga. En `frontend/src/privado/DisposicionClub.tsx`, quien recibe 404 de un club que tenía en su sesión (lo rechazaron con la pantalla abierta) recarga la sesión y va a uno de sus clubes, o a `/entrar` si su cuenta ya no existe. Pruebas en `backend/pruebas/Integracion/Ingresos/RechazoPruebas.cs`: tras rechazar, la persona no está en la sala de espera ni entre los integrantes y su invitación ya no aparece en la lista de invitaciones; si era su único club, la cuenta no existe, su token da 401 y el inicio de sesión responde el `credenciales_invalidas` de siempre; si tiene otro club, lo conserva intacto, `GET /api/sesion` ya no trae el club que la rechazó y ese club le responde 404; no se envía ningún correo (`CorreoEnMemoria` sin mensajes nuevos); con una invitación nueva se registra otra vez con el mismo correo y el mismo documento y vuelve a quedar en espera, y sin ella no puede (RF-027b); rechazar a un integrante aprobado da 409 `ingreso_ya_aprobado` y no borra nada; rechazan el PRESIDENTE y un DIRECTIVO; ENTRENADOR, JUGADOR, en espera y sin sesión no; aprobar y rechazar a la vez a la misma persona (`Task.WhenAll`): vale la primera y la otra recibe 409 o 404; rechazar dos veces da 404; no se borra la invitación de presidente enviada a ese mismo correo. En `AislamientoIngresosPruebas.cs`: el presidente de otro club y el DESARROLLADOR reciben 404 y la persona sigue en espera. En `AccesoClubPruebas.cs`: quitar el último endpoint, eliminar la lista `PendientesDeConstruir` (la comparación vuelve a ser exacta) y dejar el contador de endpoints de club en 10

**Punto de control**: las cuatro historias funcionan y se prueban por separado (quickstart, pasos
1 a 5)

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: estados del club, contrato, revisión visual y recorrido completo

- [X] T022 Club suspendido y dado de baja (RF-030). Backend: en `ServicioRegistroConInvitacion.cs` (consulta y registro) y `ServicioAceptacionInvitacion.cs`, una invitación **del club** cuyo club está `DADO_DE_BAJA` responde 410 `invitacion_no_valida`; con el club `SUSPENDIDO` sigue sirviendo y la persona queda en espera; la suspensión no alarga `VenceEn`. Las invitaciones de presidente no cambian de comportamiento (supuesto 4). Actualizar el texto de `ErroresDeInvitacion.NoValida()` solo si hace falta para cubrir "cancelada" y "club no disponible". Pruebas en `backend/pruebas/Integracion/Ingresos/IngresosPorEstadoDelClubPruebas.cs`: en un club suspendido el PRESIDENTE invita, reenvía, cancela, aprueba y rechaza; un DIRECTIVO recibe 403 `club_suspendido` en los ocho endpoints; quien tiene un enlace válido se registra y queda en espera; una cuenta en espera recibe `club_suspendido`. En un club dado de baja nadie invita, aprueba ni rechaza (403 `club_dado_de_baja`), y consulta, registro y aceptación dan 410; al revertir la baja el mismo enlace vuelve a servir si no ha vencido
- [X] T023 [P] Contrastar la API con `specs/002-ingreso-club/contracts/api.yaml`: arrancar la API, abrir Swagger y comprobar que cada endpoint nuevo o cambiado tiene las rutas, los códigos de estado, los nombres de DTO y los campos del contrato (incluidos `estadoIngreso`, `pasaPorSalaDeEspera`, `nombreResponsable` y `enviadaPor` nulo), y que `frontend/src/compartido/api/tipos.ts` coincide. Corregir el código si difiere; si lo que está mal es el contrato, corregirlo y decirlo. Confirmar que `AccesoClubPruebas.cs` compara de forma exacta, con 33 endpoints en el contrato y 10 de club
- [X] T024 Revisar a 360 px de ancho, en tema claro y en oscuro, con la identidad de un club con colores propios (RF-031, CE-009): `/club/:clubId/ingresos` con sus tres secciones con datos y vacías, el diálogo de aprobar, la confirmación de rechazo, la sala de espera de la persona, el desplegable con un club en espera y `/invitacion#<token>` con el campo del responsable. Sin desplazamiento horizontal, con el texto legible y con todo estado dicho con texto además de color. Corregir en `frontend/src/privado/ingresos/`, `frontend/src/privado/SalaDeEspera.tsx`, `frontend/src/cuenta/` y `frontend/src/compartido/tema/componentes.css` lo que no cumpla
- [X] T025 Recorrer `specs/002-ingreso-club/quickstart.md` completo, pasos 1 a 7, con `docker compose up --build`, y ejecutar la batería entera (`dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `scripts/verificar-tamano.ps1`). Anotar en la propia guía cualquier paso cuyo resultado no coincida y corregir el código. Marcar la funcionalidad como terminada solo con todo en verde, incluidas las pruebas de la 001

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (fase 1)**: sin dependencias.
- **Cimientos (fase 2)**: depende de la fase 1. Bloquea todas las historias.
- **Historias (fases 3 a 6)**: dependen de los cimientos.
- **Cierre (fase 7)**: depende de las cuatro historias.

### Entre historias

- **Historia 1 (P1)**: empieza al terminar los cimientos. No depende de ninguna otra.
- **Historia 2 (P1)**: su código no depende de la historia 1 y sus pruebas automáticas siembran
  la invitación. El recorrido manual sí necesita la historia 1 para enviar la invitación.
- **Historia 3 (P1)**: sus pruebas automáticas siembran personas en espera. `Ingresos.tsx` y el
  enlace del menú los crea T011; si se construye antes que la historia 1, T017 debe crearlos.
- **Historia 4 (P2)**: depende de T016 (repositorio de ingresos) y de `SeccionSalaDeEspera.tsx`
  (T017). Usa `RepositorioInvitacionesClub` (T009).

Orden recomendado: 1 → 2 → 3 → 4, el del plan.

### Dentro de cada fase

- Fase 2: T003, T004 y T005 en paralelo. T006, T007 y T008 necesitan T002; T007 y T008 pueden ir
  en paralelo con T006.
- Historia 1: T009 → T011 → T012. T010 en paralelo con cualquiera.
- Historia 2: T013 → T014 (comparten las pantallas de `frontend/src/cuenta/`). T015 en paralelo.
- Historia 3: T016 → T017 → T018 → T019 (comparten `ControladorIngresos.cs` y `AprobacionPruebas.cs`).
- Historia 4: T020 → T021.
- Cierre: T022 y T023 en paralelo; después T024 y T025.

`AccesoClubPruebas.cs` lo tocan T008, T011, T012, T017, T018, T019 y T021: nunca dos a la vez.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos, primera tanda:
T003  EstadoInvitacion e Invitacion.EstadoEn
T004  Reglas de dominio y sus pruebas unitarias
T005  Columnas nuevas y migración IngresoAlClub
T002  Sembrador

# Cimientos, segunda tanda:
T006  Sala de espera en IntegranteDelClubAttribute
T007  estadoIngreso en la sesión y tipos del frontend
T008  Contrato unido en EndpointsDeLaApi

# Historia 1:
T009  Repositorios de invitaciones          ||  T010  Plantilla del correo

# Historia 2:
T013 → T014  Registro y aceptación           ||  T015  Pantalla de sala de espera
```

---

## Estrategia de implementación

### Primero lo mínimo

Las historias 1, 2 y 3 son P1 y solo juntas dejan entrar a una persona al club: con la historia 1
el club ya invita (primer incremento que se puede enseñar), pero el ingreso se cierra con la 3.

1. Fases 1 y 2: base en verde y cimientos.
2. Historia 1 → **parar y validar** el paso 1 del quickstart.
3. Historia 2 → validar el paso 2.
4. Historia 3 → validar el paso 3. Aquí la funcionalidad ya sirve a un club real.
5. Historia 4 → validar los pasos 4 y 5.
6. Cierre → pasos 6 y 7 y recorrido completo.

### Entrega incremental

Cada historia termina con un punto de control en el que todas las pruebas están en verde y la API
coincide con el contrato menos lo que queda por construir. Conviene un commit por tarea y revisar
en cada punto de control.

---

## Notas

- Los seis supuestos de research.md ya están aplicados en las tareas (T011: una fila por correo;
  T006 y T015: en espera en club suspendido ve el aviso; T013: fecha UTC; T022: invitaciones de
  presidente sin cambios; T013 y T014: responsable de 160 y no se vuelve a pedir; T016 y T019:
  presidentes invitados por el DESARROLLADOR fuera de la lista). Si el propietario cambia alguno,
  el cambio es de una consulta o de una validación.
- Fuera de estas tareas, porque sus entidades no existen todavía (nota 1 del plan): asignar
  categoría, generar la mensualidad y eliminar la ficha de Jugador al aprobar.
- Si una tarea descubre una regla de negocio sin decidir, se detiene esa parte y se pregunta
  (§25); no se inventa.
