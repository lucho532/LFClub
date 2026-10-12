---

description: "Lista de tareas de la funcionalidad 001: base multiclub y panel de administración de la plataforma"
---

# Tareas: Base multiclub y panel de administración de la plataforma

**Entrada**: documentos de diseño en `specs/001-base-multiclub/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20) y el plan. No van en tareas aparte: por
§27.1, cada tarea de una historia es un corte vertical que entrega el backend, la pantalla que lo
usa contra la API real y sus pruebas.

**Organización**: las tareas se agrupan por historia de usuario para poder implementar y probar
cada historia por separado.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US7]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

- **Nombres** (§2.1): todo en español, con el estilo del plan: `ServicioX`, `IServicioX`,
  `RepositorioX`, `IRepositorioX`, `ValidadorX`, `MapperX`, `ControladorX`, `XDto`. Los hooks de
  React conservan el prefijo `use`.
- **Carpetas de Aplicacion**: `Servicios/` guarda los contratos `IServicio*` de los casos de uso;
  `Implementaciones/`, sus clases; `Interfaces/`, los `IRepositorio*` y los puertos
  (`IServicioCorreo`, `IHashContrasena`, `IContextoClub`, `IUnidadDeTrabajo`, `IReloj`,
  `IEmisorTokenSesion`).
- **Documentación** (§3): cada clase, interfaz y enumeración lleva documentación XML en español
  que dice qué representa, cuál es su responsabilidad y qué no debe asumir. La compilación falla
  si falta (T002).
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Si un archivo de una
  tarea se acerca, se divide por responsabilidad dentro de la misma carpeta.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores no
  tienen reglas de negocio. Entidad → Mapper → DTO; ninguna entidad sale por la API.
- **Errores** (§23): `application/problem+json` con `title` en español, `status` y el `codigo`
  estable que fija `contracts/api.yaml`.
- **Contrato**: `specs/001-base-multiclub/contracts/api.yaml` manda en rutas, DTOs, códigos de
  estado y códigos de error. Los DTOs van en `backend/src/LFClub.Aplicacion/DTOs/` con los
  nombres del contrato.
- **Aislamiento** (§7.1): `IgnoreQueryFilters()` solo se usa dentro de
  `backend/src/LFClub.Infraestructura/Repositorios/Plataforma/`.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan `dotnet test
  backend/LFClub.sln` y `npm --prefix frontend test`, y pasa `scripts/verificar-tamano.ps1`.

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: crear los proyectos y las herramientas

- [X] T001 Crear la solución `backend/LFClub.sln` en .NET 10 con los proyectos `backend/src/LFClub.Api` (Web API), `backend/src/LFClub.Aplicacion`, `backend/src/LFClub.Dominio`, `backend/src/LFClub.Infraestructura` (bibliotecas) y las pruebas xUnit `backend/pruebas/Unitarias/LFClub.Pruebas.Unitarias.csproj` y `backend/pruebas/Integracion/LFClub.Pruebas.Integracion.csproj`. Referencias: Api → Aplicacion e Infraestructura; Infraestructura → Aplicacion y Dominio; Aplicacion → Dominio; Dominio sin referencias. Paquetes: EF Core 10 y Npgsql en Infraestructura; `Microsoft.AspNetCore.Authentication.JwtBearer` y Swashbuckle en Api; `Microsoft.AspNetCore.Mvc.Testing` y `Testcontainers.PostgreSql` en Integracion. Crear las carpetas vacías del plan (`Controladores/{Plataforma,Club,Publico,Cuenta}`, `Autorizacion`, `Errores`, `DTOs`, `Interfaces`, `Servicios`, `Implementaciones`, `Mappers`, `Validadores`, `Utilidades`, `Entidades`, `Enumeraciones`, `Reglas`, `Datos`, `Repositorios/Plataforma`, `Correo`, `Seguridad`)
- [X] T002 [P] Crear `backend/Directory.Build.props` (`Nullable` activado, `TreatWarningsAsErrors`, `GenerateDocumentationFile` con CS1591 como error en los proyectos de `src/`) y `.editorconfig` en la raíz (UTF-8, 4 espacios en C#, 2 en TypeScript)
- [X] T003 [P] Crear la aplicación `frontend/` con Vite, React 19, TypeScript 5, React Router y Vitest: `frontend/package.json` (guiones `dev`, `build`, `test`, `lint`), `frontend/vite.config.ts`, `frontend/tsconfig.json`, `frontend/eslint.config.js` (con `eslint-plugin-react-hooks`), `frontend/index.html`, `frontend/src/main.tsx`, y las carpetas `frontend/src/{compartido/{api,sesion,tema,componentes},cuenta,plataforma,privado}` y `frontend/pruebas/`. La dirección de la API se lee de `VITE_URL_API`
- [X] T004 [P] Crear `scripts/verificar-tamano.ps1`: recorre `backend/src`, `backend/pruebas` y `frontend/src`, ignora `Migraciones/`, `bin/`, `obj/` y `node_modules/`, y termina con error listando cada archivo `.cs`, `.ts`, `.tsx` o `.css` de más de 250 líneas (§2.3)
- [X] T005 Crear `docker-compose.yml` (servicios `bd` con PostgreSQL 17 y base `lfclub`, `api` en el puerto 8080 y `frontend` en el 5173), `backend/Dockerfile`, `frontend/Dockerfile`, `.dockerignore`, `.gitignore` y `.env.ejemplo` con `Plataforma__CorreoDesarrollador`, `Sesion__ClaveFirma`, `Sesion__DiasVigencia=7`, `Brevo__Llave` (vacía), `Brevo__Remitente`, `Frontend__UrlBase=http://localhost:5173`, `BaseDatos__MigrarAlArrancar=true` y la cadena de conexión

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: modelo base, aislamiento entre clubes, sesión y recuperación de contraseña. El
DESARROLLADOR crea su contraseña con "olvidé mi contraseña" (research §6), así que el inicio de
sesión y la recuperación son previos a cualquier historia.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase

- [X] T006 [P] Crear las enumeraciones en `backend/src/LFClub.Dominio/Enumeraciones/`: `Rol.cs` (`DESARROLLADOR`, `PRESIDENTE`, `DIRECTIVO`, `ENTRENADOR`, `JUGADOR`), `EstadoClub.cs` (`ACTIVO`, `SUSPENDIDO`, `DADO_DE_BAJA`), `EstadoIngreso.cs` (`EN_ESPERA`, `APROBADO`), `EstadoEnvio.cs` (`PENDIENTE`, `ENVIADO`, `FALLIDO`) y `TipoDocumento.cs` (`REGISTRO_CIVIL`, `TARJETA_IDENTIDAD`, `CEDULA_CIUDADANIA`, `CEDULA_EXTRANJERIA`)
- [X] T007 [P] Crear `backend/src/LFClub.Aplicacion/Utilidades/NormalizadorTexto.cs` (correo: sin espacios en los extremos y en minúsculas; nombre de club: minúsculas y sin espacios sobrantes; documento: sin espacios ni puntos y en minúsculas; identificador de inicio de sesión: sin espacios en los extremos y en minúsculas). Es el único lugar donde se normaliza: todo correo y todo documento pasa por aquí antes de guardarse y antes de buscarse, para que lo guardado y lo escrito al entrar coincidan siempre. La contraseña nunca se normaliza y `GeneradorTokens.cs` (token de 32 bytes aleatorios en base64url y su hash SHA-256 en hexadecimal de 64 caracteres), con pruebas en `backend/pruebas/Unitarias/Utilidades/NormalizadorTextoPruebas.cs` y `GeneradorTokensPruebas.cs`
- [X] T008 [P] Crear los puertos en `backend/src/LFClub.Aplicacion/Interfaces/`: `IContextoClub.cs` (club de la petición, o ninguno), `IUnidadDeTrabajo.cs` (guardar y ejecutar en transacción), `IReloj.cs`, `IHashContrasena.cs`, `IServicioCorreo.cs` (enviar invitación y enviar recuperación; devuelve si se pudo enviar), `IEmisorTokenSesion.cs`. Crear también `backend/src/LFClub.Aplicacion/Utilidades/ExcepcionDeAplicacion.cs` (lleva `codigo`, estado HTTP, mensaje en español y errores por campo opcionales) y `RelojSistema.cs`
- [X] T009 [P] Crear `backend/src/LFClub.Aplicacion/Validadores/ValidadorImagen.cs`, compartido por el escudo y la foto de perfil: acepta solo PNG, JPEG o WebP reconocidos por su firma binaria (nunca por la extensión ni por la cabecera enviada; SVG no se admite), "Máximo 1 MB", y devuelve el `TipoContenido` detectado (`image/png`, `image/jpeg` o `image/webp`). Pruebas en `backend/pruebas/Unitarias/Validadores/ValidadorImagenPruebas.cs`: cada formato, un archivo que no es imagen, un SVG y uno de más de 1 MB
- [X] T010 Crear las entidades base en `backend/src/LFClub.Dominio/Entidades/` (depende de T006). Claves `Guid` versión 7 generadas por la aplicación; fechas en UTC
  - `IPerteneceAClub.cs`: expone `ClubId`
  - `Club.cs`: `Id`; `Nombre` texto (120), obligatorio; `NombreNormalizado` texto (120), "Minúsculas, sin espacios sobrantes. Índice único (RF-007)"; `Sede` texto (120), opcional; `Direccion` texto (200), opcional; `CorreoContacto` texto (254), "Opcional; formato de correo"; `TelefonoContacto` texto (20), opcional; `ColorPrincipal` texto (7), "Opcional; `#RRGGBB`. Nulo = identidad neutra"; `ColorAcento` texto (7), "Opcional; `#RRGGBB`"; `VersionEscudo` entero, "Sube cada vez que cambia el escudo; 0 = sin escudo"; `Estado` `EstadoClub`, "Obligatorio; `ACTIVO` al crearse"; `EstadoCambiadoPorUsuarioId` Guid y `EstadoCambiadoEn`, nulos hasta el primer cambio de estado (RF-031); `CreadoEn` obligatorio
  - `Usuario.cs`: `Id`; `Correo` texto (254), obligatorio; `CorreoNormalizado` texto (254), "Minúsculas y sin espacios en los extremos. Índice único (RF-002)"; `ContrasenaHash` texto, "Nulo solo en la cuenta DESARROLLADOR recién creada. Nunca sale por la API (RF-019)"; `Celular` texto (20), "Obligatorio al registrarse"; `EsDesarrollador` booleano, "Índice único parcial sobre `true`: como máximo una cuenta (RF-001)"; `IntentosFallidos` entero, "0 a 5"; `Bloqueada` booleano; `SelloSeguridad` Guid, "Cambia al cambiar la contraseña; invalida los tokens anteriores"; `VersionFoto` entero, "0 = sin foto"; `CreadoEn` obligatorio
  - `UsuarioRol.cs` (implementa `IPerteneceAClub`): `Id`; `ClubId`; `UsuarioId`; `Rol`, "`PRESIDENTE`, `DIRECTIVO`, `ENTRENADOR` o `JUGADOR`. Nunca `DESARROLLADOR`"; `EstadoIngreso`, "En esta funcionalidad, siempre `APROBADO` (RF-016)"; `Nombres` texto (80), obligatorio; `Apellidos` texto (80), obligatorio; `TipoDocumento` obligatorio; `NumeroDocumento` texto (20), "Obligatorio; sin espacios ni puntos y en minúsculas (RF-002)"; `FechaNacimiento` fecha, "Obligatoria; no futura"; `CreadoEn` obligatorio
  - `SolicitudRecuperacion.cs`: `Id`; `UsuarioId`; `TokenHash` texto (64), "SHA-256 del token. Índice único"; `CreadaEn`; `VenceEn` = "`CreadaEn` + 60 minutos"; `UsadaEn`, "Nulo hasta que se usa"
- [X] T011 Crear el contexto de EF en `backend/src/LFClub.Infraestructura/Datos/` (depende de T008 y T010): `ContextoLFClub.cs`; una clase por entidad en `Configuraciones/` (`ConfiguracionClub.cs`, `ConfiguracionUsuario.cs`, `ConfiguracionUsuarioRol.cs`, `ConfiguracionSolicitudRecuperacion.cs`) con las longitudes de T010, las enumeraciones guardadas como texto, el índice único de `Club.NombreNormalizado`, el de `Usuario.CorreoNormalizado`, el único parcial de `Usuario.EsDesarrollador` sobre `true`, el único `(ClubId, NumeroDocumento)` de `UsuarioRol`, el de `SolicitudRecuperacion.TokenHash`, y borrado en cascada `Club` → `UsuarioRol` y `Usuario` → `SolicitudRecuperacion`. El contexto aplica a toda entidad `IPerteneceAClub` un filtro global por el `ClubId` de `IContextoClub`; sin club en el contexto no devuelve ninguna fila (falla cerrado, research §2). Crear `ContextoClub.cs` (implementación por petición de `IContextoClub`), `UnidadDeTrabajo.cs` y la migración `Inicial` en `Migraciones/`
- [X] T012 Crear los repositorios base (depende de T011): `IRepositorioUsuarios.cs` e `IRepositorioSolicitudesRecuperacion.cs` e `IRepositorioPertenencias.cs` en `backend/src/LFClub.Aplicacion/Interfaces/`; `RepositorioUsuarios.cs` y `RepositorioSolicitudesRecuperacion.cs` en `backend/src/LFClub.Infraestructura/Repositorios/`; y `RepositorioPertenencias.cs` en `backend/src/LFClub.Infraestructura/Repositorios/Plataforma/`. `RepositorioPertenencias` es el único acceso entre clubes de una cuenta y cada consulta va acotada: los `UsuarioRol` de un `UsuarioId` con su club, el `UsuarioRol` de un `UsuarioId` en un `ClubId`, y la cuenta dueña de un `NumeroDocumento`
- [X] T013 [P] Crear `backend/src/LFClub.Infraestructura/Seguridad/HashContrasena.cs` (`PasswordHasher` de `Microsoft.AspNetCore.Identity`, sin el resto de Identity) y `EmisorTokenSesion.cs` (JWT firmado con `Sesion:ClaveFirma`, vigencia `Sesion:DiasVigencia`, que lleva solo el identificador del usuario y su `SelloSeguridad`; nunca el club ni el rol). Pruebas en `backend/pruebas/Unitarias/Seguridad/HashContrasenaPruebas.cs`
- [X] T014 [P] Crear en `backend/src/LFClub.Infraestructura/Correo/`: `ServicioCorreoBrevo.cs` (API transaccional de Brevo con `HttpClient`, sin SDK; llave en `Brevo:Llave`), `ServicioCorreoRegistro.cs` (escribe en el registro una línea que empieza por `Correo para <destinatario>` con el asunto y el enlace) y `PlantillasCorreo.cs` (textos en español de la invitación, con nombre del club y rol, y de la recuperación). Los enlaces son `{Frontend:UrlBase}/invitacion#<token>` y `{Frontend:UrlBase}/restablecer#<token>`. Si `Brevo:Llave` está vacía se registra `ServicioCorreoRegistro`
- [X] T015 Configurar la API en `backend/src/LFClub.Api/Program.cs` (dividir el registro de servicios en `backend/src/LFClub.Api/Configuracion/` si se acerca a 250 líneas): inyección de dependencias de las cuatro capas, autenticación JWT que en cada petición carga el usuario y rechaza el token si su `SelloSeguridad` no coincide (`backend/src/LFClub.Api/Autorizacion/ValidadorSesion.cs`), Swagger con el esquema `sesion`, CORS para `Frontend:UrlBase`, limitador de peticiones por IP con la política `anonimo` (429 `demasiadas_peticiones`), aplicación de migraciones al arrancar si `BaseDatos:MigrarAlArrancar` es verdadero, y `backend/src/LFClub.Api/Errores/ManejadorErrores.cs`, que traduce `ExcepcionDeAplicacion`, los errores de validación (`datos_invalidos` con `errores` por campo), la falta de sesión (401 `sin_sesion`) y cualquier error inesperado (500 sin detalles internos) a `application/problem+json`
- [X] T016 Crear la autorización (depende de T012 y T015): `backend/src/LFClub.Dominio/Reglas/ReglaAccesoPorEstado.cs` (ACTIVO: entran todos; SUSPENDIDO: solo PRESIDENTE; DADO_DE_BAJA: nadie) con pruebas en `backend/pruebas/Unitarias/Reglas/ReglaAccesoPorEstadoPruebas.cs`; `backend/src/LFClub.Api/Autorizacion/SoloDesarrolladorAttribute.cs` (sin sesión 401 `sin_sesion`; cualquier cuenta que no sea la DESARROLLADOR 403 `solo_desarrollador`, sin datos); y `backend/src/LFClub.Api/Autorizacion/IntegranteDelClubAttribute.cs`, que en cada petición a `/api/clubes/{clubId}/**` consulta la base de datos: sin `UsuarioRol` en ese club 404 `no_encontrado` (también para el DESARROLLADOR y para un club inexistente); club suspendido y rol distinto de PRESIDENTE 403 `club_suspendido`; club dado de baja 403 `club_dado_de_baja`; si pasa, fija el club en `IContextoClub` y deja el rol disponible para el controlador. Admite exigir un rol concreto (403 `rol_no_autorizado`)
- [X] T017 Crear `backend/src/LFClub.Infraestructura/Datos/CuentaInicial.cs` y llamarla al arrancar: si no existe ninguna cuenta con `EsDesarrollador = true`, crea una con el correo de `Plataforma:CorreoDesarrollador`, sin `ContrasenaHash` y sin ningún `UsuarioRol` (research §6). Ningún endpoint crea ni modifica esa marca
- [X] T018 Crear la base de las pruebas de integración en `backend/pruebas/Integracion/Base/` (depende de T015 a T017): `FabricaApi.cs` (`WebApplicationFactory` contra PostgreSQL 17 en Testcontainers, con migraciones aplicadas), `CorreoEnMemoria.cs` (sustituye `IServicioCorreo`, guarda los enlaces enviados y permite simular un fallo de envío), `Sembrador.cs` (crea clubes, cuentas e integrantes directamente en la base de datos) y `ClienteDePrueba.cs` (inicia sesión y envía peticiones con el token). Añadir `backend/pruebas/Integracion/Aislamiento/ModeloPruebas.cs`: recorre el modelo de EF y falla si una entidad no implementa `IPerteneceAClub` y no está en la lista expresa de entidades de la plataforma (`Club`, `Usuario`, `FotoPerfil`, `SolicitudRecuperacion`), si una entidad de club no tiene clave foránea a `Club` con cascada, o si aparece `IgnoreQueryFilters` en un archivo fuera de `Repositorios/Plataforma/`; y comprueba que una consulta de `UsuarioRol` sin club en el contexto devuelve cero filas. Añadir también `backend/pruebas/Integracion/Aislamiento/CuentaDesarrolladorPruebas.cs` (§20, RF-001): tras arrancar existe exactamente una cuenta con `EsDesarrollador = true`, sin `ContrasenaHash` y sin ningún `UsuarioRol`; ejecutar `CuentaInicial` por segunda vez no crea otra; insertar directamente una segunda cuenta con `EsDesarrollador = true` viola el índice único
- [X] T019 [P] Crear el cliente de la API en `frontend/src/compartido/api/`: `cliente.ts` (sobre `fetch`; añade `Authorization: Bearer`, admite JSON y `multipart/form-data`, y ante un 401 borra la sesión y lleva a `/entrar`), `errores.ts` (convierte `problem+json` en un error con `codigo`, `title` y `errores` por campo) y `tipos.ts` (tipos de todos los esquemas de `contracts/api.yaml`, con los mismos nombres)
- [X] T020 [P] Crear los estilos y componentes comunes: `frontend/src/compartido/tema/variables.css` (variables CSS de fondo, texto, bordes y estados de la plataforma para `data-tema="claro"` y `data-tema="oscuro"`, más `--color-club-principal`, `--color-club-acento` y sus colores de texto, con los valores neutros de la plataforma por defecto), `frontend/src/compartido/tema/base.css` (diseño adaptable sin desplazamiento horizontal a 360 px) y, en `frontend/src/compartido/componentes/`, `Boton.tsx`, `Campo.tsx` (etiqueta, entrada y error del campo), `Aviso.tsx`, `Tabla.tsx`, `Tarjeta.tsx`, `DialogoConfirmacion.tsx` y `EtiquetaEstado.tsx` (el estado siempre con texto además de color, RF-034)
- [X] T021 Crear la sesión y las rutas del frontend (depende de T019 y T020): `frontend/src/compartido/sesion/almacenToken.ts` (almacenamiento local), `ProveedorSesion.tsx` (carga `GET /api/sesion`; al abrir la aplicación renueva el token con `POST /api/sesion/renovacion` si le queda menos de la mitad de su vigencia), `useSesion.ts`, `RutaProtegida.tsx` (exige sesión y, según la ruta, ser DESARROLLADOR o no serlo) y `frontend/src/App.tsx` con todas las rutas de la tabla "Pantallas" del plan. La raíz `/` redirige: sin sesión a `/entrar`; DESARROLLADOR a `/plataforma`; el resto a `/club/:clubId` (el único club o el último elegido)
- [X] T022 Inicio de sesión de extremo a extremo (`POST /api/sesion`, `GET /api/sesion`, `POST /api/sesion/renovacion`). Backend: `backend/src/LFClub.Aplicacion/Servicios/IServicioSesion.cs`, `Implementaciones/ServicioSesion.cs`, `Mappers/MapperSesion.cs` y `Mappers/MapperIdentidadClub.cs` (`urlEscudo` nulo si `VersionEscudo` es 0; si no, `/api/publico/clubes/{clubId}/escudo?v={VersionEscudo}`), DTOs `IniciarSesionDto`, `TokenSesionDto`, `SesionDto`, `ClubDeSesionDto`, `IdentidadClubDto`, y `backend/src/LFClub.Api/Controladores/Cuenta/ControladorSesion.cs`. El `identificador` se limpia primero con `NormalizadorTexto` (sin espacios en los extremos y en minúsculas); con `@` se busca como correo normalizado; sin `@`, como número de documento normalizado. La contraseña se compara tal como se escribió. Una cuenta sin `ContrasenaHash` o con `Bloqueada = true` no entra. Cuenta inexistente, contraseña incorrecta y cuenta bloqueada devuelven el mismo 401 `credenciales_invalidas` con el mismo texto, que explica cómo recuperar la contraseña; si la cuenta no existe se calcula igualmente un hash. `POST /api/sesion` usa la política `anonimo`. `GET /api/sesion` devuelve los clubes de la cuenta con rol, estado, identidad, nombres y apellidos; lista vacía para el DESARROLLADOR. Pantalla `frontend/src/cuenta/Entrar.tsx` en `/entrar`: campo "Correo o documento", contraseña, enlace "Olvidé mi contraseña" y ningún enlace para registrarse (historia 2, escenario 4). Pruebas en `backend/pruebas/Integracion/Cuenta/SesionPruebas.cs`: entra con correo y con documento; entra igual con el correo en mayúsculas y con espacios antes y después; los tres fallos devuelven la misma respuesta; `GET /api/sesion` sin token da 401; un token con sello antiguo da 401; la renovación entrega un token nuevo
- [X] T023 Recuperación de contraseña de extremo a extremo (`POST /api/cuenta/recuperacion`, `POST /api/cuenta/recuperacion/confirmacion`). Backend: `Servicios/IServicioRecuperacion.cs`, `Implementaciones/ServicioRecuperacion.cs`, `Validadores/ValidadorContrasena.cs` (entre 8 y 128 caracteres, sin reglas de composición), DTOs `PedirRecuperacionDto` y `RestablecerContrasenaDto`, y `backend/src/LFClub.Api/Controladores/Cuenta/ControladorRecuperacion.cs`. Pedir: siempre 202, exista o no el correo; si existe, deja sin efecto las solicitudes anteriores de ese usuario, guarda solo el hash del token con vencimiento a 60 minutos y envía el correo; política `anonimo`. Confirmar: enlace usado, caducado o inexistente 410 `enlace_no_valido`; contraseña no válida 400 `datos_invalidos`; si es válido, guarda el hash nuevo, pone `IntentosFallidos = 0`, `Bloqueada = false` y un `SelloSeguridad` nuevo, marca `UsadaEn` y responde 204. Pantallas `frontend/src/cuenta/Recuperar.tsx` (`/recuperar`, muestra siempre el mismo mensaje) y `frontend/src/cuenta/Restablecer.tsx` (`/restablecer#<token>`, lee el token del fragmento y lo envía en el cuerpo). Pruebas en `backend/pruebas/Integracion/Cuenta/RecuperacionPruebas.cs`: el DESARROLLADOR sin contraseña crea la suya y entra; correo inexistente da 202 y no envía nada; el enlace sirve una sola vez; una solicitud nueva anula la anterior; un enlace vencido da 410; la contraseña anterior y los tokens anteriores dejan de servir

**Punto de control**: `docker compose up --build` levanta todo, el DESARROLLADOR crea su contraseña
con "olvidé mi contraseña" e inicia sesión (quickstart, paso 1)

---

## Fase 3: Historia 1 - El desarrollador crea un club e invita a su presidente (Prioridad: P1) 🎯 MVP

**Objetivo**: el DESARROLLADOR ve la lista de clubes, crea un club con el correo de su presidente,
gestiona las invitaciones y retira presidentes.

**Prueba independiente**: se inicia sesión como DESARROLLADOR, se crea un club con el correo de su
presidente y se comprueba que el club aparece en la lista y que la invitación llega a ese correo.

- [X] T024 [US1] Crear `backend/src/LFClub.Dominio/Entidades/Invitacion.cs` (implementa `IPerteneceAClub`): `Id`; `ClubId`, foránea a `Club` con cascada; `Rol`, "En esta funcionalidad, siempre `PRESIDENTE` (RF-012)"; `Correo` texto (254), "Obligatorio; se guarda normalizado"; `TokenHash` texto (64), "SHA-256 del token. Índice único. El token en claro no se guarda"; `EstadoEnvio` (`PENDIENTE`, `ENVIADO` o `FALLIDO`); `CreadaPorUsuarioId`; `CreadaEn`; `VenceEn` = "`CreadaEn` + 7 días"; `UsadaEn`, "Nulo hasta que se usa"; `AnuladaEn`, "Nulo hasta que otra la reemplaza"; y el estado derivado, que no se guarda: vigente = `UsadaEn` nulo y `AnuladaEn` nulo y ahora < `VenceEn`. Añadir `backend/src/LFClub.Infraestructura/Datos/Configuraciones/ConfiguracionInvitacion.cs`, la migración `Invitaciones`, y los repositorios del panel `IRepositorioClubesPlataforma.cs` e `IRepositorioInvitacionesPlataforma.cs` en `backend/src/LFClub.Aplicacion/Interfaces/` con `RepositorioClubesPlataforma.cs` y `RepositorioInvitacionesPlataforma.cs` en `backend/src/LFClub.Infraestructura/Repositorios/Plataforma/`. Estos repositorios solo exponen lo que §8 permite al DESARROLLADOR: clubes, presidentes e invitaciones
- [X] T025 [US1] Lista de clubes y armazón del panel (`GET /api/plataforma/clubes`). Backend: `Servicios/IServicioConsultaClubes.cs`, `Implementaciones/ServicioConsultaClubes.cs`, `Mappers/MapperClubPlataforma.cs`, `ClubResumenDto` (`presidenteRegistrado` es verdadero si el club tiene algún `UsuarioRol` con rol `PRESIDENTE`) y `backend/src/LFClub.Api/Controladores/Plataforma/ControladorClubes.cs` con `[SoloDesarrollador]`. Frontend: `frontend/src/plataforma/DisposicionPlataforma.tsx` (menú lateral, cabecera con identidad neutra de la plataforma y cierre de sesión) y `frontend/src/plataforma/ListaClubes.tsx` en `/plataforma` (tabla con nombre, estado con texto y "presidente sin registrar"; estado vacío). Pruebas en `backend/pruebas/Integracion/Plataforma/ListaClubesPruebas.cs`: el DESARROLLADOR ve todos los clubes; sin sesión 401; un PRESIDENTE, un DIRECTIVO, un ENTRENADOR y un JUGADOR reciben 403 `solo_desarrollador` sin datos (RF-003)
- [X] T026 [US1] Crear un club e invitar a su presidente (`POST /api/plataforma/clubes`). Backend: `Servicios/IServicioCreacionClub.cs`, `Implementaciones/ServicioCreacionClub.cs`, `Servicios/IServicioInvitacionPresidente.cs`, `Implementaciones/ServicioInvitacionPresidente.cs` (crea la invitación con el hash del token y, después de confirmar la transacción, intenta el envío y guarda `ENVIADO` o `FALLIDO`), `Validadores/ValidadorCrearClub.cs` (nombre obligatorio de hasta 120 caracteres; `correoPresidente` obligatorio y con formato de correo), `CrearClubDto`, `ClubDetalleDto`, `PresidenteDto`, `InvitacionDto` (nunca incluye el token) y la acción en `ControladorClubes.cs`. Club e invitación se crean en una sola transacción; el club nace `ACTIVO`, sin colores ni escudo. Sin correo o con correo no válido 400 `datos_invalidos` y no se crea nada; nombre repetido tras normalizar 409 `nombre_de_club_repetido`; si `correoPresidente`, ya normalizado, es el de la cuenta DESARROLLADOR, 409 `correo_del_desarrollador` y no se crea nada (esa cuenta no pertenece a ningún club, §8); si el correo falla, el club se crea igualmente y la invitación llega con `estadoEnvio: FALLIDO`; responde 201 con `ClubDetalleDto`. Frontend: `frontend/src/plataforma/FormularioCrearClub.tsx`, abierto desde `ListaClubes.tsx`, que muestra los errores por campo y lleva al detalle del club creado. Pruebas en `backend/pruebas/Integracion/Plataforma/CrearClubPruebas.cs`: creación correcta con invitación enviada; sin correo; correo mal escrito; nombre repetido con otras mayúsculas y espacios; el correo del DESARROLLADOR, también escrito con mayúsculas y espacios, da 409; fallo de correo; la respuesta no contiene el token (§20: no se puede crear un club sin presidente)
- [X] T027 [US1] Detalle del club e invitaciones (`GET /api/plataforma/clubes/{clubId}`, `POST …/invitaciones`, `POST …/invitaciones/{invitacionId}/reenvio`). Backend: detalle en `ServicioConsultaClubes.cs` (presidentes registrados e invitaciones sin usar ni anular, con `vencida`); invitar y reenviar en `ServicioInvitacionPresidente.cs`; `InvitarPresidenteDto`, `ReenviarInvitacionDto`; acción de detalle en `ControladorClubes.cs` y `backend/src/LFClub.Api/Controladores/Plataforma/ControladorInvitacionesClub.cs`. Invitar y reenviar con correo corregido: si el correo es el de la cuenta DESARROLLADOR 409 `correo_del_desarrollador`. Invitar: si ese correo ya es presidente del club 409 `ya_es_presidente`; si ya tenía una invitación vigente para el club, queda anulada; 201. Reenviar: anula la indicada y crea otra, al mismo correo o al corregido; invitación usada 409 `invitacion_ya_usada`; 201. Club inexistente 404. Frontend: `frontend/src/plataforma/DetalleClub.tsx` en `/plataforma/clubes/:clubId` (compone secciones) y `frontend/src/plataforma/SeccionInvitaciones.tsx` (lista con estado de envío y vencimiento, "Reenviar", "Corregir correo" e "Invitar a otro presidente"). Pruebas en `backend/pruebas/Integracion/Plataforma/InvitacionesPruebas.cs`: reenviar invalida el enlace anterior; corregir el correo envía al nuevo; presidente adicional en un club que ya tiene uno; `ya_es_presidente`; `correo_del_desarrollador` al invitar y al corregir; `invitacion_ya_usada`; solo el DESARROLLADOR
- [X] T028 [US1] Quitar el rol a un presidente (`POST /api/plataforma/clubes/{clubId}/presidentes/{usuarioRolId}/retiro`). Backend: `backend/src/LFClub.Dominio/Reglas/ReglaUltimoPresidente.cs` (no se puede quitar el rol ni eliminar a un PRESIDENTE si es el único del club con ese rol; las invitaciones pendientes no cuentan) con pruebas en `backend/pruebas/Unitarias/Reglas/ReglaUltimoPresidentePruebas.cs`, que cubren también la regla de RF-020 para el propio presidente (historia 2, escenario 14); `Servicios/IServicioRetiroPresidente.cs`, `Implementaciones/ServicioRetiroPresidente.cs`, `RetirarPresidenteDto` y `backend/src/LFClub.Api/Controladores/Plataforma/ControladorPresidentes.cs`. La regla se comprueba dentro de la misma transacción que el cambio, bloqueando la fila del club. `ASIGNAR_ROL` exige `rolNuevo` `DIRECTIVO` o `ENTRENADOR` (si falta, 400 `datos_invalidos`) y reemplaza el rol. `ELIMINAR_DEL_CLUB` borra físicamente el `UsuarioRol` y, si la cuenta se queda sin ninguno, borra también la cuenta. Único presidente 409 `ultimo_presidente`. Responde 200 con `ClubDetalleDto`. Frontend: `frontend/src/plataforma/SeccionPresidentes.tsx` dentro de `DetalleClub.tsx`: lista de presidentes y "Quitar rol", con un diálogo de confirmación que obliga a elegir entre asignar otro rol o eliminar del club. Pruebas en `backend/pruebas/Integracion/Plataforma/RetiroPresidentePruebas.cs` (con datos sembrados): con dos presidentes, asignar DIRECTIVO deja ese único rol; eliminar del club impide entrar a ese club y conserva los demás; si era su único club la cuenta deja de existir; único presidente 409; una invitación pendiente no cuenta como presidente

**Punto de control**: la historia 1 funciona y se prueba sola. La validación manual de T028
necesita presidentes registrados, que llegan con la historia 2; sus pruebas automáticas no.

---

## Fase 4: Historia 2 - El presidente invitado se registra y entra a su club (Prioridad: P1)

**Objetivo**: quien recibe la invitación se registra, queda como PRESIDENTE aprobado y entra a la
aplicación de su club; quien pertenece a varios clubes elige cuál ver.

**Prueba independiente**: con una invitación recién enviada, se abre el enlace, se completa el
registro y se comprueba que la persona queda como PRESIDENTE del club correcto; después se cierra
la sesión y se vuelve a entrar con el correo y con el documento.

- [X] T029 [US2] Club elegido (`GET /api/clubes/{clubId}`). Backend: `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioClub.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioClub.cs` (lee el club del `IContextoClub`), `Servicios/IServicioConsultaClub.cs`, `Implementaciones/ServicioConsultaClub.cs`, `Mappers/MapperClub.cs`, `ClubDto` (con `miRol`) y `backend/src/LFClub.Api/Controladores/Club/ControladorClub.cs` con `[IntegranteDelClub]`. Frontend: `frontend/src/privado/DisposicionClub.tsx` (menú lateral, cabecera con el nombre del club siempre visible, §7.2, y cierre de sesión, que borra el token y lleva a `/entrar`) y `frontend/src/privado/InicioClub.tsx` en `/club/:clubId` (pantalla de inicio con el nombre del club y, para el PRESIDENTE, el acceso a la configuración). Pruebas en `backend/pruebas/Integracion/Club/ClubElegidoPruebas.cs`: un integrante obtiene su club; un integrante de otro club recibe 404 `no_encontrado` aun conociendo el identificador (RF-024); el DESARROLLADOR recibe 404 (RF-004); un identificador inexistente da el mismo 404; sin sesión 401
- [X] T030 [US2] Registro con invitación (`POST /api/invitaciones/consulta`, `POST /api/invitaciones/registro`). Backend: `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioInvitacionesPorToken.cs` y `backend/src/LFClub.Infraestructura/Repositorios/Plataforma/RepositorioInvitacionesPorToken.cs` (busca una invitación solo por el hash de su token, con su club), `Servicios/IServicioRegistroConInvitacion.cs`, `Implementaciones/ServicioRegistroConInvitacion.cs`, `Validadores/ValidadorRegistro.cs` (`nombres` y `apellidos` obligatorios de hasta 80 caracteres; `tipoDocumento` de la enumeración; `numeroDocumento` obligatorio de hasta 20, guardado sin espacios ni puntos; `fechaNacimiento` obligatoria y no futura; `celular` obligatorio de hasta 20; `contrasena` de 8 a 128), `TokenDto`, `InvitacionVigenteDto`, `RegistrarConInvitacionDto` y `backend/src/LFClub.Api/Controladores/Cuenta/ControladorInvitaciones.cs` (anónimo, política `anonimo`). Consulta: devuelve nombre del club, rol, correo, identidad y `tieneCuenta` (existe una cuenta con el correo de la invitación); invitación usada, vencida, anulada o inexistente 410 `invitacion_no_valida`. Registro: el DTO no tiene correo y se usa siempre el de la invitación; en una sola transacción crea el `Usuario` y el `UsuarioRol` con el rol de la invitación y `EstadoIngreso = APROBADO`, y marca `UsadaEn`; 409 `correo_ya_registrado` si ese correo ya tiene cuenta, `documento_repetido_en_club` si el documento ya existe en ese club y `documento_en_otra_cuenta` si pertenece a otra cuenta (el mensaje pide entrar con esa cuenta); las violaciones de índice único se traducen a esos mismos 409; responde 201 con `TokenSesionDto`. No existe ningún otro endpoint que cree cuentas. Frontend: `frontend/src/cuenta/Invitacion.tsx` en `/invitacion#<token>`: muestra el nombre del club, el rol y el correo ya escrito, ninguno editable; formulario con nombres, apellidos, tipo y número de documento, fecha de nacimiento, celular y contraseña; errores por campo; al terminar guarda el token y entra a `/club/:clubId`; si la invitación no sirve lo explica y no muestra el formulario. Pruebas en `backend/pruebas/Integracion/Cuenta/RegistroConInvitacionPruebas.cs`: registro correcto queda PRESIDENTE y APROBADO en el club de la invitación y puede entrar con correo y con documento; invitación usada, vencida y reemplazada dan 410; un correo enviado en el cuerpo se ignora; los tres 409; sin token no hay forma de registrarse (RF-014); un segundo presidente invitado deja el club con dos (escenario 10); enviar `esDesarrollador: true` en el cuerpo del registro no cambia nada: la cuenta creada no es DESARROLLADOR
- [X] T031 [US2] Aceptar una invitación con una cuenta que ya existe (`POST /api/invitaciones/aceptacion`). Backend: `Servicios/IServicioAceptacionInvitacion.cs`, `Implementaciones/ServicioAceptacionInvitacion.cs` y la acción con sesión en `ControladorInvitaciones.cs`. La sesión debe ser la de la cuenta cuyo correo recibió la invitación; si no, 403 `invitacion_de_otro_correo`. En una transacción: si la cuenta ya tiene un `UsuarioRol` en ese club, reemplaza su rol por el de la invitación y responde 200; si no, crea el `UsuarioRol` copiando nombres, apellidos, tipo y número de documento y fecha de nacimiento del `UsuarioRol` más reciente de la cuenta, con el rol de la invitación y `APROBADO`, y responde 201. En ambos casos marca `UsadaEn` y devuelve `ClubDeSesionDto`; 410 `invitacion_no_valida`. No crea una segunda cuenta (RF-018). Frontend: en `frontend/src/cuenta/Invitacion.tsx`, cuando `tieneCuenta` es verdadero no se muestra el registro: se pide iniciar sesión (conservando el token de la invitación al pasar por `/entrar`) y después se ofrece "Aceptar" con el nombre del club y el rol; si la sesión abierta es de otro correo, se explica y se ofrece cerrar sesión. Pruebas en `backend/pruebas/Integracion/Cuenta/AceptacionInvitacionPruebas.cs`: la misma cuenta queda en dos clubes con su rol en cada uno y un solo inicio de sesión; otra cuenta recibe 403; la invitación no se puede usar dos veces; un DIRECTIVO invitado como presidente de su propio club queda PRESIDENTE con un solo `UsuarioRol` en él (historia 2, escenario 15)
- [X] T032 [US2] Desplegable de clubes. Frontend: `frontend/src/privado/DesplegableClubes.tsx` en la cabecera de `DisposicionClub.tsx`, visible solo con más de un club, que lista únicamente los clubes de `SesionDto` y navega a `/club/:clubId`; `frontend/src/compartido/sesion/ultimoClub.ts` (recuerda el último club elegido en el almacenamiento local); en `frontend/src/App.tsx`, quien tiene un solo club entra directamente a él y quien intenta abrir un club que no es suyo vuelve a uno propio. Pruebas en `backend/pruebas/Integracion/Club/VariosClubesPruebas.cs`: `GET /api/sesion` devuelve los dos clubes con su rol; la misma sesión obtiene cada club y recibe 404 en un tercero (§20: solo elige entre sus clubes y solo ve los datos del club elegido); y en `frontend/pruebas/ultimoClub.test.ts`
- [X] T033 [US2] Bloqueo al quinto fallo (RF-005). Backend: en `IRepositorioUsuarios.cs` y `RepositorioUsuarios.cs`, incremento atómico de `IntentosFallidos` en la base de datos que pone `Bloqueada = true` al llegar a 5; en `ServicioSesion.cs`, un acierto antes del quinto fallo reinicia el contador y una cuenta bloqueada no admite ni la contraseña correcta, siempre con el mismo 401 `credenciales_invalidas`. Frontend: `frontend/src/cuenta/Entrar.tsx` muestra, ante ese error, el mensaje del servidor con el enlace a `/recuperar`. Pruebas en `backend/pruebas/Integracion/Cuenta/BloqueoPruebas.cs`: cinco fallos bloquean y la contraseña correcta deja de servir; un acierto al cuarto fallo reinicia la cuenta; la recuperación por correo desbloquea; cinco fallos simultáneos no pierden ninguno; la respuesta no distingue cuenta bloqueada de cuenta inexistente

**Punto de control**: las historias 1 y 2 completan el alta de un club (quickstart, pasos 2, 3 y 5)

---

## Fase 5: Historia 3 - Cada club se ve con su propia identidad (Prioridad: P2)

**Objetivo**: el DESARROLLADOR configura escudo y colores, y cada integrante ve la aplicación con
la identidad del club elegido.

**Prueba independiente**: se configuran identidades distintas en dos clubes, se entra con un
integrante de cada uno y se comprueba que cada quien ve la de su club.

- [X] T034 [US3] Crear `backend/src/LFClub.Dominio/Entidades/EscudoClub.cs` (implementa `IPerteneceAClub`): `ClubId`, "Clave primaria y foránea a `Club`, con cascada"; `Contenido` binario, "Máximo 1 MB"; `TipoContenido` texto (30), "`image/png`, `image/jpeg` o `image/webp`, según la firma del archivo". Añadir `backend/src/LFClub.Infraestructura/Datos/Configuraciones/ConfiguracionEscudoClub.cs`, la migración `EscudoClub`, y `IRepositorioEscudos.cs` en `backend/src/LFClub.Aplicacion/Interfaces/` con `RepositorioEscudos.cs` en `backend/src/LFClub.Infraestructura/Repositorios/` (lee el escudo del club de `IContextoClub`, con el filtro activo; el escudo no es una excepción de §7.1). Guardar el escudo es una operación del panel: se añade a `IRepositorioClubesPlataforma.cs` y `RepositorioClubesPlataforma.cs`
- [X] T035 [P] [US3] Crear `frontend/src/compartido/tema/contraste.ts` (contraste WCAG entre dos colores `#RRGGBB`; elección de texto claro u oscuro sobre un color de relleno; y, para un color, en qué tema queda por debajo de 3:1 contra el fondo de ese tema) y `frontend/src/compartido/tema/coloresClub.ts` (convierte una `IdentidadClubDto` en los valores de `--color-club-principal`, `--color-club-acento` y sus colores de texto; con campos nulos devuelve la identidad neutra). Pruebas en `frontend/pruebas/contraste.test.ts` y `frontend/pruebas/coloresClub.test.ts`
- [X] T036 [US3] Colores del club (`PUT /api/plataforma/clubes/{clubId}/colores`). Backend: `Servicios/IServicioIdentidadClub.cs`, `Implementaciones/ServicioIdentidadClub.cs`, `Validadores/ValidadorColores.cs` (los dos colores obligatorios con el patrón `^#[0-9A-Fa-f]{6}$`), `ActualizarColoresDto` y `backend/src/LFClub.Api/Controladores/Plataforma/ControladorIdentidadClub.cs` con `[SoloDesarrollador]`; responde 200 con `ClubDetalleDto`. Frontend: `frontend/src/plataforma/SeccionIdentidad.tsx` dentro de `DetalleClub.tsx`: selectores de color principal y de acento, vista previa en tema claro y en oscuro y, antes de guardar, el aviso de `contraste.ts` que indica en qué tema se perdería el color; es un aviso y no impide guardar (RF-009). Pruebas en `backend/pruebas/Integracion/Plataforma/ColoresPruebas.cs`: guarda y devuelve los colores; formato no válido 400; un PRESIDENTE del propio club recibe 403 (RF-008)
- [X] T037 [US3] Escudo del club (`PUT /api/plataforma/clubes/{clubId}/escudo`, `GET /api/publico/clubes/{clubId}/escudo`). Backend: carga en `ServicioIdentidadClub.cs` con `ValidadorImagen` (400 `escudo_no_es_imagen` o `escudo_demasiado_grande`; el límite de la petición se fija por encima de 1 MB para que responda el validador), que guarda el `TipoContenido` detectado y sube `Club.VersionEscudo`; acción `multipart/form-data` con el campo `archivo` en `ControladorIdentidadClub.cs`; `Servicios/IServicioEscudoPublico.cs`, `Implementaciones/ServicioEscudoPublico.cs` y `backend/src/LFClub.Api/Controladores/Publico/ControladorEscudo.cs`, anónimo, que fija en `IContextoClub` el `clubId` de la ruta antes de leer, sirve la imagen con su tipo y con caché larga cuando llega `v`, responde 404 si el club no tiene escudo y la sirve en cualquier estado del club (supuesto 5). Frontend: carga y vista previa del escudo en `frontend/src/plataforma/SeccionIdentidad.tsx`, con el motivo del rechazo, y el escudo en miniatura en `frontend/src/plataforma/ListaClubes.tsx`. Pruebas en `backend/pruebas/Integracion/Plataforma/EscudoPruebas.cs`: PNG, JPEG y WebP se guardan y se obtienen sin sesión; un archivo que no es imagen y uno de más de 1 MB se rechazan y se conserva el escudo anterior; `urlEscudo` cambia de versión al reemplazarlo; solo el DESARROLLADOR lo carga
- [X] T038 [US3] Identidad en la aplicación del club. Frontend: `frontend/src/compartido/tema/IdentidadClub.tsx` aplica las variables de `coloresClub.ts` al contenedor de la aplicación del club; `frontend/src/privado/DisposicionClub.tsx` muestra el escudo (o un distintivo neutro si no hay), los colores y el nombre del club elegido, y cambia de identidad al cambiar de club en el desplegable; `frontend/src/cuenta/Invitacion.tsx` usa la identidad que devuelve la consulta de la invitación; `frontend/src/plataforma/DisposicionPlataforma.tsx` conserva la identidad neutra. Los colores del club se usan solo como relleno y el texto sobre ellos sale de `contraste.ts` (research §11). Pruebas en `backend/pruebas/Integracion/Club/IdentidadPruebas.cs`: `GET /api/sesion` y `GET /api/clubes/{clubId}` devuelven la identidad de cada club, y todos los campos nulos en un club recién creado

**Punto de control**: quickstart, paso 4

---

## Fase 6: Historia 4 - El presidente mantiene los datos de su club (Prioridad: P2)

**Objetivo**: el PRESIDENTE y el DESARROLLADOR editan nombre, sede, dirección y contacto.

**Prueba independiente**: un PRESIDENTE cambia la dirección de su club y se comprueba que el
cambio se ve en su aplicación y en el panel del DESARROLLADOR.

- [X] T039 [US4] El PRESIDENTE edita su club (`PUT /api/clubes/{clubId}/configuracion`). Backend: `Servicios/IServicioConfiguracionClub.cs`, `Implementaciones/ServicioConfiguracionClub.cs`, `Validadores/ValidadorConfiguracionClub.cs` (`nombre` obligatorio de hasta 120; `sede` hasta 120; `direccion` hasta 200; `correoContacto` con formato de correo y hasta 254; `telefonoContacto` hasta 20; los cuatro últimos opcionales), `ActualizarConfiguracionClubDto` (sin colores ni escudo) y la acción en `backend/src/LFClub.Api/Controladores/Club/ControladorClub.cs` con `[IntegranteDelClub]` exigiendo PRESIDENTE. Recalcula `NombreNormalizado`; nombre repetido 409 `nombre_de_club_repetido`; responde 200 con `ClubDto`. Frontend: `frontend/src/privado/ConfiguracionClub.tsx` en `/club/:clubId/configuracion`, visible en el menú solo para el PRESIDENTE, sin ninguna opción de escudo ni colores; al guardar, la cabecera muestra el nombre nuevo. Pruebas en `backend/pruebas/Integracion/Club/ConfiguracionClubPruebas.cs`: el presidente guarda y el cambio se ve en `GET /api/clubes/{clubId}`; el presidente de otro club recibe 404; un DIRECTIVO, un ENTRENADOR y un JUGADOR del club reciben 403 `rol_no_autorizado`; enviar colores en el cuerpo no los cambia; nombre repetido 409
- [X] T040 [US4] El DESARROLLADOR edita cualquier club (`PUT /api/plataforma/clubes/{clubId}/configuracion`). Backend: acción en `backend/src/LFClub.Api/Controladores/Plataforma/ControladorClubes.cs` que reutiliza `ServicioConfiguracionClub.cs` y `ValidadorConfiguracionClub.cs` y responde 200 con `ClubDetalleDto`. Frontend: `frontend/src/plataforma/SeccionDatos.tsx` dentro de `DetalleClub.tsx`. Pruebas en `backend/pruebas/Integracion/Plataforma/ConfiguracionPlataformaPruebas.cs`: el cambio del DESARROLLADOR lo ve el presidente y el del presidente lo ve el DESARROLLADOR; un PRESIDENTE recibe 403 en esta ruta; nombre repetido 409

**Punto de control**: quickstart, paso 5.7

---

## Fase 7: Historia 5 - El desarrollador suspende, da de baja o elimina un club (Prioridad: P3)

**Objetivo**: el DESARROLLADOR cambia el estado de un club y el acceso de sus integrantes cambia
con él, también en las sesiones abiertas.

**Prueba independiente**: se suspende un club, se comprueba quién puede entrar y qué ve el resto,
se levanta la suspensión y se comprueba que todo sigue exactamente como antes.

- [X] T041 [P] [US5] Crear `backend/src/LFClub.Dominio/Reglas/ReglaTransicionEstadoClub.cs`: solo permite `ACTIVO` → `SUSPENDIDO`, `SUSPENDIDO` → `ACTIVO`, `ACTIVO` → `DADO_DE_BAJA`, `SUSPENDIDO` → `DADO_DE_BAJA` y `DADO_DE_BAJA` → `ACTIVO`; cualquier otra, incluida la del mismo estado, se rechaza; y solo permite eliminar desde `DADO_DE_BAJA`. Pruebas con todas las combinaciones en `backend/pruebas/Unitarias/Reglas/ReglaTransicionEstadoClubPruebas.cs`
- [X] T042 [US5] Cambiar el estado de un club (`PUT /api/plataforma/clubes/{clubId}/estado`). Backend: `Servicios/IServicioEstadoClub.cs`, `Implementaciones/ServicioEstadoClub.cs` (aplica la regla, guarda `EstadoCambiadoPorUsuarioId` y `EstadoCambiadoEn` y no toca ningún otro dato), `CambiarEstadoClubDto` y `backend/src/LFClub.Api/Controladores/Plataforma/ControladorEstadoClub.cs` con `[SoloDesarrollador]`; transición no permitida 409 `transicion_no_permitida`; responde 200 con `ClubDetalleDto`. Frontend: `frontend/src/plataforma/SeccionEstado.tsx` dentro de `DetalleClub.tsx`: estado actual con texto, cuándo cambió y solo las acciones posibles desde ese estado (Suspender, Levantar la suspensión, Dar de baja, Revertir la baja), cada una con confirmación. Pruebas en `backend/pruebas/Integracion/Plataforma/EstadoClubPruebas.cs`: cada transición permitida; las no permitidas dan 409; queda registrado quién y cuándo (RF-031); tras suspender y levantar, los integrantes, las invitaciones y la configuración son idénticos (CE-007); solo el DESARROLLADOR
- [X] T043 [US5] Acceso según el estado del club (RF-027, RF-028, RF-030). Frontend: `frontend/src/privado/AvisoClubNoDisponible.tsx`, que `DisposicionClub.tsx` muestra en `/club/:clubId` cuando la API responde 403 `club_suspendido` ("incidencia temporal: comunícate con el presidente del club") o `club_dado_de_baja` ("club no disponible"), también si la respuesta llega con la sesión ya abierta; aviso fijo "Club suspendido" para el PRESIDENTE de un club suspendido; el desplegable de `frontend/src/privado/DesplegableClubes.tsx` sigue permitiendo pasar a otro club. Pruebas en `backend/pruebas/Integracion/Club/AccesoPorEstadoPruebas.cs`: en un club suspendido el PRESIDENTE obtiene 200 con `estado: SUSPENDIDO` y un DIRECTIVO recibe 403 `club_suspendido`; quien pertenece a un club suspendido y a otro activo usa el activo con normalidad; en un club dado de baja también el PRESIDENTE recibe 403 `club_dado_de_baja`; un token emitido antes del cambio de estado recibe la respuesta nueva; al levantar la suspensión o revertir la baja todos vuelven a entrar
- [X] T044 [US5] Eliminar un club (`POST /api/plataforma/clubes/{clubId}/eliminacion`). Backend: `Servicios/IServicioEliminacionClub.cs`, `Implementaciones/ServicioEliminacionClub.cs`, `EliminarClubDto` y la acción en `ControladorEstadoClub.cs`. En una sola transacción: club que no está dado de baja 409 `club_no_dado_de_baja`; `nombreDeConfirmacion` distinto del nombre del club 400 `confirmacion_no_coincide`; se borra el club (la cascada borra sus integrantes, invitaciones y escudo) y después las cuentas que se quedaron sin ningún `UsuarioRol`; la cuenta DESARROLLADOR nunca se toca; responde 204. Frontend: en `frontend/src/plataforma/SeccionEstado.tsx`, "Eliminar" aparece solo si el club está dado de baja y abre `frontend/src/plataforma/DialogoEliminarClub.tsx`, que advierte de que no se puede deshacer y solo habilita el botón al escribir el nombre del club; al terminar vuelve a `/plataforma`. Pruebas en `backend/pruebas/Integracion/Plataforma/EliminarClubPruebas.cs`: club activo y club suspendido dan 409; confirmación incorrecta da 400 y no borra nada; tras eliminar no queda ninguna fila con ese `ClubId` en ninguna tabla y los demás clubes tienen exactamente las mismas filas (CE-008); quien pertenecía también a otro club sigue entrando; quien solo pertenecía a ese club ya no tiene cuenta; la cuenta DESARROLLADOR sigue existiendo

**Punto de control**: quickstart, paso 7

---

## Fase 8: Historia 6 - Tema claro y tema oscuro (Prioridad: P3)

**Objetivo**: un botón visible cambia de tema en cualquier pantalla y la elección se recuerda en
el dispositivo.

**Prueba independiente**: se pulsa el botón de tema en cualquier pantalla, se comprueba que toda
la interfaz cambia, se cierra y se vuelve a abrir, y se comprueba que el tema se mantiene.

- [X] T045 [P] [US6] Crear `frontend/src/compartido/tema/tema.ts`: lee y guarda la elección (`claro` u `oscuro`) en el almacenamiento local; sin elección, usa `prefers-color-scheme`; aplica el resultado al atributo `data-tema` de `<html>`. Pruebas en `frontend/pruebas/tema.test.ts`: elección guardada, sin elección con dispositivo claro y con dispositivo oscuro, y almacenamiento no disponible
- [X] T046 [US6] Botón de tema en toda la aplicación: `frontend/src/compartido/tema/ProveedorTema.tsx`, `frontend/src/compartido/tema/useTema.ts`, `frontend/src/compartido/componentes/BotonTema.tsx` (con texto accesible que dice a qué tema cambia) y un script mínimo en `frontend/index.html` que aplica el tema antes de pintar, para que no parpadee. Colocar el botón en la cabecera de `frontend/src/plataforma/DisposicionPlataforma.tsx` y de `frontend/src/privado/DisposicionClub.tsx`, y en una disposición común de las pantallas sin sesión, `frontend/src/cuenta/DisposicionCuenta.tsx`, usada por `Entrar.tsx`, `Recuperar.tsx`, `Restablecer.tsx` e `Invitacion.tsx` (RF-032)
- [X] T047 [US6] Revisar los dos temas en todas las pantallas: completar en `frontend/src/compartido/tema/variables.css` los valores del tema oscuro que falten; comprobar que ningún componente de `frontend/src/` usa un color fijo en lugar de una variable; comprobar que el texto sobre los colores del club sale de `contraste.ts` en ambos temas (necesita la historia 3; sin ella se revisa solo la identidad neutra); y que todo estado se distingue por texto además de por color (RF-034)

**Punto de control**: quickstart, paso 8

---

## Fase 9: Historia 7 - Foto de perfil (Prioridad: P3)

**Objetivo**: cualquier cuenta carga, cambia y quita su foto de perfil, y nadie más puede verla.

**Prueba independiente**: se carga una foto, se comprueba que aparece en la cabecera, se cambia
de club y se comprueba que sigue siendo la misma; después se quita.

- [X] T048 [US7] Crear `backend/src/LFClub.Dominio/Entidades/FotoPerfil.cs`: `UsuarioId`, "Clave primaria y foránea a `Usuario`, con cascada"; `Contenido` binario, "Máximo 1 MB"; `TipoContenido` texto (30), "`image/png`, `image/jpeg` o `image/webp`, según la firma del archivo". Añadir `backend/src/LFClub.Infraestructura/Datos/Configuraciones/ConfiguracionFotoPerfil.cs`, la migración `FotoPerfil`, `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioFotosPerfil.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioFotosPerfil.cs` (todas sus operaciones reciben el `UsuarioId` de la sesión)
- [X] T049 [US7] Foto de perfil de extremo a extremo (`GET`, `PUT` y `DELETE /api/cuenta/foto`). Backend: `Servicios/IServicioFotoPerfil.cs`, `Implementaciones/ServicioFotoPerfil.cs` y `backend/src/LFClub.Api/Controladores/Cuenta/ControladorFotoPerfil.cs`. Ninguna ruta lleva identificador de cuenta: siempre es la de la sesión (RF-038). `PUT` con `multipart/form-data` y el campo `archivo` pasa por `ValidadorImagen` (400 `foto_no_es_imagen` o `foto_demasiado_grande`, conservando la foto anterior), sube `Usuario.VersionFoto` y responde 200 con `SesionDto`; `GET` responde 404 si no hay foto; `DELETE` pone `VersionFoto` en 0 y responde 204 aunque no hubiera foto; sin sesión, 401 en las tres. Frontend: `frontend/src/cuenta/MiPerfil.tsx` en `/perfil` (cargar, cambiar y quitar, con el motivo del rechazo) y `frontend/src/compartido/componentes/Avatar.tsx` en la cabecera de `DisposicionPlataforma.tsx` y `DisposicionClub.tsx`, que pide la foto con la sesión, la muestra desde memoria y, sin foto, muestra las iniciales (las del integrante en el club elegido; la inicial del correo para el DESARROLLADOR) y enlaza a `/perfil`. Pruebas en `backend/pruebas/Integracion/Cuenta/FotoPerfilPruebas.cs`: PNG, JPEG y WebP se guardan; un archivo que no es imagen y uno de más de 1 MB se rechazan y se conserva la anterior; sin sesión 401; cada sesión obtiene solo su propia foto; quitarla deja `versionFoto` en 0; la foto se borra al borrarse la cuenta

**Punto de control**: quickstart, paso 9

---

## Fase 10: Pulido y asuntos transversales

**Propósito**: comprobaciones que abarcan varias historias

- [X] T050 Crear `backend/pruebas/Integracion/Aislamiento/AccesoPlataformaPruebas.cs`: obtiene de la propia API la lista de todos los endpoints bajo `/api/plataforma/` y comprueba que cada uno responde 401 sin sesión y 403 `solo_desarrollador` a un integrante de cada rol (CE-004); y `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`: cada endpoint bajo `/api/clubes/{clubId}/` responde 404 a un integrante de otro club y al DESARROLLADOR (CE-003, RF-004), y cualquier endpoint sin `security: []` en el contrato responde 401 sin sesión (§9)
- [X] T051 [P] Comparar el Swagger generado por la API con `specs/001-base-multiclub/contracts/api.yaml`: rutas, DTOs, códigos de estado y códigos de error. Corregir el código donde se aparte del contrato; si el contrato tuvo que cambiar durante la implementación, actualizarlo y dejar constancia en `specs/001-base-multiclub/research.md`
- [X] T052 [P] Ejecutar `scripts/verificar-tamano.ps1` y `dotnet build backend/LFClub.sln` sin avisos; dividir por responsabilidad cualquier archivo que pase de 250 líneas (§2.3) y completar la documentación XML que falte (§3)
- [X] T053 Revisar a 360 px de ancho todas las pantallas de la tabla "Pantallas" del plan, en los dos temas, y corregir en `frontend/src/` cualquier desplazamiento horizontal o texto ilegible (RF-035, CE-009)
- [X] T054 [P] Crear `README.md` en la raíz con la puesta en marcha (`.env`, `docker compose up --build`, direcciones, cómo leer los correos sin Brevo) y cómo ejecutar las pruebas, a partir de `specs/001-base-multiclub/quickstart.md`
- [X] T055 Ejecutar `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, y recorrer los diez pasos de `specs/001-base-multiclub/quickstart.md` contra `docker compose up --build`; corregir lo que falle y añadir una prueba por cada defecto encontrado (§27.2)

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (fase 1)**: sin dependencias.
- **Cimientos (fase 2)**: depende de la fase 1 y bloquea todas las historias.
- **Historias (fases 3 a 9)**: todas dependen de la fase 2.
- **Pulido (fase 10)**: depende de las historias que se quieran entregar.

### Entre historias

- **Historia 1 (P1)**: solo necesita los cimientos.
- **Historia 2 (P1)**: necesita T024 (la entidad `Invitacion`) y, para probarla a mano, poder
  crear un club (T026).
- **Historia 3 (P2)**: necesita `DetalleClub.tsx` (T027), `DisposicionClub.tsx` (T029) e
  `Invitacion.tsx` (T030), que T038 modifica.
- **Historia 4 (P2)**: necesita `ControladorClub.cs` y `DisposicionClub.tsx` (T029) y
  `DetalleClub.tsx` (T027).
- **Historia 5 (P3)**: necesita `DetalleClub.tsx` (T027) y `DisposicionClub.tsx` (T029). T044
  borra en cascada lo que exista; no necesita las historias 3 ni 7.
- **Historia 6 (P3)**: T045 solo necesita los cimientos. T046 necesita las cabeceras de T025 y
  T029 e `Invitacion.tsx` (T030), es decir, las historias 1 y 2. T047 revisa los colores del club
  si la historia 3 está hecha.
- **Historia 7 (P3)**: necesita las cabeceras de T025 y T029.

Orden recomendado: 1 → 2 → 3 → 4 → 5 → 6 → 7.

### Dentro de la fase 2

T006 a T009 en paralelo → T010 → T011 → T012 → T015 → T016 → T017 → T018 → T022 → T023. T013 y
T014 en paralelo con T010 a T012. T019 y T020 en paralelo con todo el backend; T021 después de
ambas y antes de T022.

### Dentro de cada historia

Las tareas van en el orden en que aparecen. Varias comparten archivos (`DetalleClub.tsx`,
`ControladorClubes.cs`, `ServicioSesion.cs`), así que solo son paralelas las marcadas con [P].

---

## Ejemplos de trabajo en paralelo

```text
# Fase 1, después de T001:
T002  backend/Directory.Build.props y .editorconfig
T003  frontend/
T004  scripts/verificar-tamano.ps1

# Fase 2, arranque:
T006  Enumeraciones
T007  NormalizadorTexto y GeneradorTokens
T008  Puertos y ExcepcionDeAplicacion
T009  ValidadorImagen
T019  Cliente de la API del frontend
T020  Estilos y componentes comunes

# Historia 3, mientras se hace T034:
T035  contraste.ts y coloresClub.ts

# Con dos personas, tras las historias 1 y 2:
Persona A: historia 3 (T034 a T038)
Persona B: T041 y T045, que no comparten archivos con la historia 3
```

---

## Estrategia de implementación

### Primero el MVP

1. Fase 1: preparación.
2. Fase 2: cimientos. Parar y validar el paso 1 del quickstart.
3. Fase 3: historia 1. Parar y validar el paso 2 del quickstart.
4. Fase 4: historia 2. Con ella el alta de un club queda cerrada: es el MVP real, porque un club
   sin presidente registrado no puede usarse.

### Entrega por incrementos

Cada historia posterior añade valor sin romper las anteriores: 3 (identidad), 4 (datos del club),
5 (estados), 6 (temas), 7 (foto). Después de cada una se ejecutan todas las pruebas y el paso
correspondiente del quickstart.

---

## Puntos que la implementación no debe resolver por su cuenta (§25)

- **Consultas de cuenta entre clubes** (decidido por el propietario el 2026-10-07, constitución
  3.6.0 §7.1). Iniciar sesión, listar los clubes de una cuenta y abrir una invitación por su token
  cruzan clubes por necesidad. Viven en `Repositorios/Plataforma/` (`RepositorioPertenencias`,
  `RepositorioInvitacionesPorToken`), siempre acotados por cuenta, documento o token. El escudo no
  es una excepción: se lee con el filtro activo (T034, T037).
- **Invitación a quien ya es integrante del club** (decidido por el propietario el 2026-10-07).
  Aceptarla reemplaza su rol por el de la invitación y no crea un segundo integrante (RF-018,
  T031). Cuando existan fichas de jugador, el caso de un JUGADOR con historial queda sujeto a la
  decisión pendiente "Ficha con historial" de §28.
- **Invitación al correo del DESARROLLADOR** (decidido por el propietario el 2026-10-07, RF-001). La
  cuenta DESARROLLADOR usa un único correo, que no se usa para nada más: no se le puede invitar a
  ningún club (T026 y T027 lo rechazan con `correo_del_desarrollador`). Por eso T031 nunca recibe
  una sesión del DESARROLLADOR con una invitación válida.
- **Normalización de los datos de inicio de sesión** (decidido por el propietario el 2026-10-07,
  RF-002).
  Correo y documento se guardan y se buscan sin espacios en los extremos y en minúsculas (T007,
  T022). La contraseña no se toca: recortarla o pasarla a minúsculas la debilitaría.
- **Supuestos de research.md** sin confirmar (duración de la sesión, reglas de la contraseña,
  caducidad de la recuperación, datos de contacto, escudo en cualquier estado, foto visible solo
  para su dueño): van como valores de configuración o validaciones aisladas, fáciles de cambiar.

## Notas

- Las tareas sin [P] dentro de una historia tocan archivos compartidos: se hacen en orden.
- Confirmar en git después de cada tarea terminada.
- En cada punto de control se puede parar y validar la historia por separado.
- Las migraciones de EF son archivos generados y no cuentan para el límite de 250 líneas.
