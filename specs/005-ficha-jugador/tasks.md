---

description: "Lista de tareas de la funcionalidad 005: ficha del jugador"
---

# Tareas: Ficha del jugador

**Entrada**: documentos de diseño en `specs/005-ficha-jugador/`

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

Son las de los `tasks.md` de la 001 a la 004, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): solo las dos tablas de T002 y su migración `FichaDelJugador`.
  Ninguna otra tarea crea tablas ni columnas; si parece necesitarlas, se detiene y se informa (§25).
- **Contrato**: `specs/005-ficha-jugador/contracts/api.yaml` manda en rutas, cuerpos, códigos de
  estado y códigos de error. Las pruebas de `AccesoClubPruebas` leen los contratos de todas las
  specs, así que una ruta descrita y sin construir las pone en rojo: T001 deja comentadas las
  rutas nuevas y cada tarea descomenta las suyas y sube los contadores que indica.
- **Nombres** (§2.1, §2.2): todo en español y con el vocabulario del dominio. `DocumentoPedido` es
  el archivo que pide la ficha; `TipoDocumento` es el tipo del documento de identidad. No se
  mezclan.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) que diga qué representa, su responsabilidad y qué no hace; toda clase que cambia de
  responsabilidad la actualiza en la misma tarea.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas.
  `frontend/src/compartido/api/tipos.ts` tiene 241: los tipos nuevos van en `tiposFicha.ts`. Las
  pruebas de integración van en los archivos que indica cada tarea.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. La regla de acceso, en
  `backend/src/LFClub.Dominio/Reglas/`. Los controladores solo delegan.
- **Acceso** (§15, research §2): toda operación de la ficha pasa por `AccesoAFicha` (T005) antes
  de leer o escribir nada. Quien no puede ver la ficha recibe `404 no_encontrado`; el rol que nunca
  puede hacer la operación recibe `403 rol_no_autorizado` del atributo `[IntegranteDelClub(...)]`.
  No se escribe otra comprobación de permisos.
- **Lo que no se ve no viaja** (RF-010, RF-015): `datosClinicos`, `documentos`, `ultimoCambio` y
  `documentosPendientes` se **omiten** del JSON (`[JsonIgnore(Condition =
  JsonIgnoreCondition.WhenWritingNull)]`), no van en `null`. Las pruebas comprueban la ausencia de
  la propiedad con `JsonElement.TryGetProperty`.
- **Aislamiento** (§7.1): ninguna consulta nueva usa `IgnoreQueryFilters()`, salvo la que ya
  existe para saber si un documento es de otra cuenta
  (`IRepositorioPertenencias.ObtenerCuentaPorDocumentoAsync`).
- **Cambios** (research §9 y §10): toda operación que cambia la ficha se ejecuta en
  `IUnidadDeTrabajo.EnTransaccionAsync` con `IRepositorioClub.BloquearAsync` como primera
  sentencia, y sella el último cambio en esa misma transacción solo si algún dato quedó distinto.
- **Datos sensibles**: los datos clínicos y el contenido de los archivos no se escriben en el
  registro de la API. Las respuestas de la ficha y de los archivos llevan `Cache-Control: private,
  no-store`.
- **Pantallas** (RF-035, §24): componentes `Campo`, `Tabla`, `Tarjeta`, `Aviso`, `Boton`,
  `EtiquetaEstado` y `DialogoConfirmacion` existentes; legibles a 360 px y en los dos temas; todo
  estado se dice con texto además de color. La pantalla muestra y permite lo que traen los grupos
  y `permisos` del DTO; no decide por el rol.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LFClub.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

El plan pidió confirmarlos (research.md, "Supuestos por confirmar"). Las tareas los dan por buenos;
si el propietario cambia alguno, cambia solo la tarea indicada.

| # | Supuesto | Tarea | Estado |
| --- | --- | --- | --- |
| 1 | No se admite un número de documento que ya usa otra cuenta en otro club (`409 documento_en_otra_cuenta`) | T013 | Por confirmar |
| 2 | Corregir la fecha de nacimiento no exige responsable aunque el jugador pase a ser menor | T014 | Por confirmar |
| 3 | Cuando el último cambio lo hizo la familia, la ficha dice "la cuenta del jugador" | T008 | Por confirmar |
| 4 | El último cambio se registra solo en la ficha desde la que se hizo | T008, T016 | Por confirmar |
| 5 | Cada archivo admite hasta 10 MB, en PDF, JPEG, PNG o WebP | T011 | Por confirmar |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: partir de una base en verde. No hay proyectos, paquetes ni tecnologías nuevas.

- [X] T001 Dejar la línea base en verde en la rama `005-ficha-jugador`. El contrato `specs/005-ficha-jugador/contracts/api.yaml` ya describe seis rutas que la API todavía no tiene, y `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` (`La_api_expone_exactamente_los_endpoints_del_contrato` y la del `401` sin sesión) lee todos los contratos. En ese contrato, comentar con `#` al inicio de cada línea los cuatro bloques de rutas nuevas completos (`/api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha`, `…/ficha/documento-identidad`, `…/ficha/identidad` y `…/ficha/documentos/{documento}`), cada uno precedido de una línea `# PENDIENTE: se descomenta en Txxx` (T007 para el `get` de `…/ficha`, T008 para su `put`, T011 para `…/documentos/{documento}`, T013 para `…/documento-identidad`, T014 para `…/identidad`). Las cuatro rutas existentes que cambian se quedan sin comentar: la prueba no las cuenta dos veces. Ejecutar `dotnet test backend/LFClub.sln`, `npm --prefix frontend test` y `scripts/verificar-tamano.ps1` y confirmar que todo pasa, con 54 endpoints en el contrato y 32 de club. Si algo más falla, detenerse e informarlo: no se construye sobre una base rota

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: las dos tablas, la regla de acceso, el colaborador que la aplica y las ayudas de
prueba. Todo es aditivo: al terminar la fase la aplicación se comporta igual que antes.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase

- [X] T002 [P] Modelo y migración (data-model.md). Crear `backend/src/LFClub.Dominio/Enumeraciones/DocumentoPedido.cs` (`COPIA_DOCUMENTO_IDENTIDAD`, `CERTIFICADO_SALUD`) y `GrupoSanguineo.cs` (`A_POSITIVO`, `A_NEGATIVO`, `B_POSITIVO`, `B_NEGATIVO`, `AB_POSITIVO`, `AB_NEGATIVO`, `O_POSITIVO`, `O_NEGATIVO`). Crear `backend/src/LFClub.Dominio/Entidades/FichaJugador.cs`, que implementa `IPerteneceAClub`: `UsuarioRolId` (Guid, "clave primaria y foránea a `UsuarioRol`: una ficha por jugador y club"), `ClubId`, `EmergenciaNombre` ("texto, máx. 160, opcional"), `EmergenciaParentesco` ("máx. 40, opcional"), `EmergenciaCelular` ("máx. 20, opcional"), `EntidadSalud` ("máx. 120, opcional"), `LugarAtencion` ("máx. 200, opcional"), `GrupoSanguineo` ("opcional, se guarda como texto"), `Alergias`, `Enfermedades`, `Medicamentos` y `Observaciones` ("máx. 1000, opcional" cada uno), `UltimoCambioEn` ("fecha y hora UTC, obligatorio: la fila solo existe desde el primer cambio"), `UltimoCambioPorUsuarioId` ("opcional; foránea a `Usuario`; pasa a nulo si esa cuenta se elimina") y `UltimoCambioPorNombre` ("máx. 161, obligatorio; nombres y apellidos de quien cambió, copiados en ese momento"). Crear `DocumentoJugador.cs`, que implementa `IPerteneceAClub`: `UsuarioRolId` y `Documento` (`DocumentoPedido`, se guarda como texto) como clave primaria compuesta, `ClubId`, `Contenido` ("bytes, obligatorio, máximo 10 MB"), `TipoContenido` ("máx. 30; `application/pdf`, `image/jpeg`, `image/png` o `image/webp`"), `TamanoBytes` (entero, obligatorio) y `SubidoEn` (UTC, obligatorio). Crear `backend/src/LFClub.Infraestructura/Datos/Configuraciones/ConfiguracionFichaJugador.cs` y `ConfiguracionDocumentoJugador.cs`: tablas `FichasJugador` y `DocumentosJugador`, las longitudes anteriores, enumeraciones como texto, foráneas a `UsuarioRol` y a `Club` con `DeleteBehavior.Cascade`, y la de `UltimoCambioPorUsuarioId` con `SetNull`. Añadir los dos `DbSet` a `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs` (el filtro por club se les aplica solo). Generar la migración `FichaDelJugador` en `backend/src/LFClub.Infraestructura/Datos/Migraciones/` con `dotnet ef migrations add`, sin editar a mano y sin tocar ninguna tabla existente. Comprobar que `backend/pruebas/Integracion/Aislamiento/ModeloPruebas.cs` sigue en verde y cubre las dos entidades nuevas
- [X] T003 [P] Regla de acceso (research §2). Crear `backend/src/LFClub.Dominio/Reglas/AlcanceDeFicha.cs`, un registro inmutable con cinco indicadores (`Ve`, `VeDatosClinicos`, `VeDocumentos`, `Cambia`, `CorrigeIdentidad`), y `ReglaAccesoAFicha.cs`, sin dependencias de HTTP ni de EF, con `Evaluar(Rol rolDeQuienPregunta, bool esDeSuCuenta, bool jugadorActivo, bool entrenaSuCategoria)`: PRESIDENTE, los cinco verdaderos siempre; DIRECTIVO, `Ve` y `VeDocumentos` siempre y los otros tres falsos, también con `entrenaSuCategoria` verdadero (RF-010); ENTRENADOR, `Ve` y `VeDatosClinicos` solo si `jugadorActivo` y `entrenaSuCategoria`, y nunca `VeDocumentos`, `Cambia` ni `CorrigeIdentidad` (RF-007, RF-008); JUGADOR, `Ve`, `VeDatosClinicos`, `VeDocumentos` y `Cambia` solo si `esDeSuCuenta`, y nunca `CorrigeIdentidad` (RF-005, RF-016, RF-017); DESARROLLADOR, todo falso. Si `Ve` es falso los demás también lo son. Pruebas en `backend/pruebas/Unitarias/Reglas/ReglaAccesoAFichaPruebas.cs`: la tabla de research §2 caso por caso, incluidos el DIRECTIVO y el PRESIDENTE asignados como entrenadores, el ENTRENADOR con un jugador retirado o de otra categoría y la cuenta de jugador ante un jugador ajeno
- [X] T004 [P] `ClubDto` gana `miUsuarioRolId` (`GET /api/clubes/{clubId}`; research §3). Backend: añadir `Guid MiUsuarioRolId` al final de `backend/src/LFClub.Aplicacion/DTOs/ClubDto.cs`; `backend/src/LFClub.Aplicacion/Mappers/MapperClub.cs` lo recibe y `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConsultaClub.cs` (y cualquier otro que construya un `ClubDto`, como `ServicioConfiguracionClub.cs`) le pasa el identificador del integrante de la petición. Frontend: `ClubDto` en `frontend/src/compartido/api/tipos.ts` gana `miUsuarioRolId: string`. Prueba en `backend/pruebas/Integracion/Club/ClubElegidoPruebas.cs`: la respuesta trae el identificador del integrante de quien pregunta, y dos integrantes del mismo club reciben cada uno el suyo
- [X] T005 Colaborador de acceso (depende de T002 y T003). Añadir a `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioJugadores.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioJugadores.cs` `ObtenerParaFichaAsync(Guid usuarioRolId)`: el integrante con rol JUGADOR e ingreso `APROBADO`, activo o retirado, con su cuenta (`Usuario`), su categoría y sus equipos, sin seguimiento; nulo para cualquier otro rol o para quien está en espera (RF-004). Crear `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeFicha.cs` (vacío de momento salvo su documentación; lo llenan T011 y T013). Crear `backend/src/LFClub.Aplicacion/Utilidades/AccesoAFicha.cs` con `ResolverAsync(Guid usuarioRolId, UsuarioRol quienPregunta)`, que devuelve el jugador y su `AlcanceDeFicha`: lee al jugador con el método anterior; `esDeSuCuenta` es `jugador.UsuarioId == quienPregunta.UsuarioId` (§12.3); `entrenaSuCategoria` solo se consulta si quien pregunta es ENTRENADOR, y es verdadero si el jugador tiene categoría, la categoría está activa y `IRepositorioAsignaciones.TieneActivaAsync(categoriaId, quienPregunta.Id)`; aplica `ReglaAccesoAFicha.Evaluar` y, si el jugador no existe o `Ve` es falso, lanza `ExcepcionDeAplicacion.NoEncontrado()`, el mismo error en los dos casos (RF-012). Añadir `ExigirCambio` y `ExigirCorreccionDeIdentidad`, que lanzan el mismo `404` si el alcance no lo permite. Registrarlo en `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs`. Sin endpoint todavía: lo prueban las historias
- [X] T006 [P] Ayudas de prueba (depende de T002). Crear `backend/pruebas/Integracion/Base/SembradorFichas.cs`, expuesto desde `FabricaApi` como los otros sembradores: `CrearFichaAsync(UsuarioRol jugador, …)` con datos clínicos, contacto de emergencia y seguridad social reconocibles; `CrearDocumentoAsync(UsuarioRol jugador, DocumentoPedido documento, byte[] contenido, string tipoContenido)`; y los archivos de prueba en memoria `Pdf()`, `Jpeg()`, `Png()`, `Webp()`, `TextoDisfrazado()` (texto con extensión falsa) y `Pdf(int tamanoBytes)` para el límite. Crear `backend/pruebas/Integracion/Ficha/EscenarioFicha.cs`, sobre `EscenarioCategorias`: un club con presidente, directivo, entrenador asignado a la categoría de un jugador (Ana), otro jugador en otra categoría (Beto), uno sin categoría (Caro) y uno retirado (Dani), cada uno con su cliente con sesión; otro club con su presidente; las rutas `Ficha(usuarioRolId)`, `Identidad(…)`, `DocumentoIdentidad(…)` y `Documento(usuarioRolId, DocumentoPedido)`; y `SubirAsync(this ClienteDePrueba cliente, string ruta, byte[] contenido, string nombre)`, que envía el archivo en el campo `archivo` de un formulario `multipart/form-data`

**Punto de control**: todas las pruebas de la 001 a la 004 siguen en verde y la aplicación no ha
cambiado de comportamiento

---

## Fase 3: Historia 1 - La familia consulta y mantiene la ficha de su jugador (Prioridad: P1) 🎯 MVP

**Objetivo**: la cuenta de un jugador abre "Mi ficha", ve sus datos agrupados y mantiene el
contacto, el contacto de emergencia, la seguridad social y los datos clínicos. No cambia nombres,
apellidos ni fecha de nacimiento, y no ve la ficha de nadie más.

**Prueba independiente**: se inicia sesión con la cuenta de un jugador, se abre "Mi ficha", se
escriben la entidad de salud, una alergia y un contacto de emergencia, se guarda, se cierra sesión,
se vuelve a entrar y se comprueba que los datos siguen ahí.

- [X] T007 [US1] Leer la ficha y abrir "Mi ficha" (`GET /api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha`; depende de T004, T005 y T006). Backend: crear en `backend/src/LFClub.Aplicacion/DTOs/` `FichaJugadorDto.cs`, `ContactoDeFichaDto.cs`, `ContactoEmergenciaDto.cs`, `SeguridadSocialDto.cs`, `DatosClinicosDto.cs`, `DocumentoDeFichaDto.cs`, `UltimoCambioDto.cs` y `PermisosDeFichaDto.cs`, con los campos del contrato; en `FichaJugadorDto`, `DatosClinicos`, `Documentos` y `UltimoCambio` son opcionales y se omiten del JSON cuando son nulos. Crear `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioFichas.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioFichas.cs` con `ObtenerAsync(usuarioRolId)` (sin seguimiento; nulo si nadie ha cambiado la ficha). Crear `backend/src/LFClub.Aplicacion/Mappers/MapperFicha.cs`, que arma el DTO a partir del jugador, su cuenta, su ficha (o nulo), el estado de sus documentos y el `AlcanceDeFicha`: identidad, categoría y nombres de equipos; `esMenorDeEdad` con `ReglaMayoriaDeEdad` y la fecha de `IReloj`; `contacto` con correo, celular y responsable de la cuenta; `datosClinicos` solo con `VeDatosClinicos`; `documentos` solo con `VeDocumentos`, siempre los dos pedidos (en esta tarea, los dos pendientes: el estado real llega en T011); `ultimoCambio` solo si hay ficha, con `porLaCuentaDelJugador` verdadero cuando `UltimoCambioPorUsuarioId` es la cuenta del jugador; `permisos` con `Cambia` y `CorrigeIdentidad`. Crear `backend/src/LFClub.Aplicacion/Servicios/IServicioConsultaFicha.cs` e `Implementaciones/ServicioConsultaFicha.cs` (`AccesoAFicha.ResolverAsync` y el mapper; no modifica nada). Crear `backend/src/LFClub.Api/Controladores/Club/ControladorFichaJugador.cs`, ruta `api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/ficha`, etiqueta "Ficha", con el `GET` bajo `[IntegranteDelClub]` sin roles y `Cache-Control: private, no-store`. Registrar repositorio y servicio en `RegistroDeServicios.cs` y `RegistroDeCasosDeUso.cs`. Contrato: descomentar el `get` de `…/ficha`; `AccesoClubPruebas` pasa a 55 endpoints en el contrato y 33 de club. Frontend: crear `frontend/src/compartido/api/tiposFicha.ts` con todos los tipos del contrato; `frontend/src/privado/ficha/textos.ts` (nombres visibles de `DocumentoPedido` y `GrupoSanguineo`); `frontend/src/privado/ficha/SeccionIdentidad.tsx` (nombres, apellidos, tipo y número de documento con `nombreDeTipoDocumento`, fecha de nacimiento, categoría y equipos, o "Sin categoría" y "Retirado" con texto; solo lectura); `frontend/src/privado/ficha/FichaJugador.tsx`, la pantalla, que carga la ficha con `useCarga`, pinta la cabecera, `SeccionIdentidad` y, como texto de solo lectura, contacto, contacto de emergencia, seguridad social y, si vienen, datos clínicos; un dato vacío se muestra como "Sin registrar". Añadir la ruta `jugadores/:usuarioRolId/ficha` dentro de `/club/:clubId` en `frontend/src/App.tsx`. En `frontend/src/privado/DisposicionClub.tsx`, el enlace "Mi ficha" hacia `/club/${clubId}/jugadores/${club.miUsuarioRolId}/ficha`, solo cuando el rol es JUGADOR (RF-033 y caso límite: a los demás roles no les aparece); el mismo acceso en `frontend/src/privado/InicioClub.tsx`, junto a `TarjetaMiCategoria`. Pruebas: crear `backend/pruebas/Integracion/Ficha/MiFichaPruebas.cs`: la cuenta de un jugador recibe nombres, apellidos, tipo y número de documento, fecha de nacimiento, categoría, equipos, correo, celular y responsable (escenario 1.1); sin ficha guardada, los grupos llegan vacíos, `documentos` trae los dos pedidos pendientes y no existe la propiedad `ultimoCambio` (1.10); con una ficha sembrada devuelve exactamente lo sembrado; `permisos` trae `puedeCambiar` verdadero y `puedeCorregirIdentidad` falso; un jugador adulto trae `esMenorDeEdad` falso; la respuesta lleva `Cache-Control: private, no-store`. Crear `backend/pruebas/Integracion/Ficha/FichaAjenaPruebas.cs`: la cuenta de un jugador recibe `404 no_encontrado` ante la ficha de otro jugador de su club y ante la de otro club (1.7, CE-002); el identificador de un ENTRENADOR, de un DIRECTIVO, de un PRESIDENTE o de alguien en espera responde `404` también al PRESIDENTE (RF-004); el jugador retirado recibe `403 integrante_retirado` (1.8, RF-013)
- [X] T008 [US1] Guardar contacto, contacto de emergencia, seguridad social y datos clínicos, con el último cambio (`PUT /api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha`; depende de T007). Backend: crear `backend/src/LFClub.Aplicacion/DTOs/ActualizarFichaDto.cs` con los doce campos del contrato, sin nombres, apellidos, fecha, documento ni correo (si llegan de más, el deserializador los ignora). Crear `backend/src/LFClub.Aplicacion/Validadores/ValidadorFicha.cs`: `celular` obligatorio, máximo 20; `nombreResponsable` máximo 160, obligatorio solo si el jugador de esta ficha es menor de 18 años ese día (`ReglaMayoriaDeEdad`, RF-020), con el mismo mensaje que el registro; `emergenciaNombre` máx. 160, `emergenciaParentesco` máx. 40, `emergenciaCelular` máx. 20, `entidadSalud` máx. 120, `lugarAtencion` máx. 200, `alergias`, `enfermedades`, `medicamentos` y `observaciones` máx. 1000, todos opcionales (RF-002); `grupoSanguineo` nulo o un valor de la enumeración; errores `400 datos_invalidos` por campo. Ampliar `IRepositorioFichas`/`RepositorioFichas` con `ObtenerOCrearParaCambiarAsync(jugador)` (la fila con seguimiento, o una nueva sin guardar) y `IRepositorioUsuarios` con lo necesario para leer la cuenta con seguimiento si aún no lo permite. Crear `backend/src/LFClub.Aplicacion/Servicios/IServicioFichaJugador.cs` e `Implementaciones/ServicioFichaJugador.cs`: `AccesoAFicha.ResolverAsync` y `ExigirCambio`; validar; en transacción y con el club bloqueado, escribir `Celular` y `NombreResponsable` en la **cuenta** del jugador (RF-039) y los diez datos restantes en la ficha, guardando como nulo todo texto vacío o solo con espacios; reemplaza el formulario entero: un campo ausente queda vacío; si algún dato quedó distinto, sellar `UltimoCambioEn` (de `IReloj`), `UltimoCambioPorUsuarioId` y `UltimoCambioPorNombre` (nombres y apellidos del integrante que actúa, copiados) y, si nada cambió, no crear la fila ni mover el sello (research §9). Extraer el sellado a un método reutilizable del repositorio de fichas (`SellarCambioAsync(jugador, quienCambia, ahoraUtc)`, que crea la fila si no existe), porque lo usarán T011, T013 y T014. Devuelve la ficha ya guardada. En `ControladorFichaJugador.cs`, el `PUT` con `[IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]`. Actualizar la documentación XML de `backend/src/LFClub.Dominio/Entidades/Usuario.cs` (celular y responsable ya se cambian desde la ficha). Contrato: descomentar el `put` de `…/ficha`; `AccesoClubPruebas` pasa a 56 y 34. Frontend: crear `frontend/src/privado/ficha/FormularioFicha.tsx`, un solo formulario con las secciones Contacto (correo como texto, celular, responsable), Contacto de emergencia, Seguridad social y Datos clínicos (grupo sanguíneo en un `select` con opción vacía; alergias, enfermedades, medicamentos y observaciones en áreas de texto) y un botón "Guardar"; marca el responsable como obligatorio con `esMenorDeEdad`; pinta cada error de la API junto a su campo y un aviso de guardado; crear `frontend/src/privado/ficha/UltimoCambio.tsx`, una línea con `fechaYHora` y el autor, que escribe "la cuenta del jugador" cuando `porLaCuentaDelJugador` (supuesto 3) y no se pinta si no hay último cambio; `FichaJugador.tsx` muestra el formulario cuando `permisos.puedeCambiar` y el texto de solo lectura en caso contrario, y sustituye la ficha por la que devuelve el `PUT`. Pruebas: `backend/pruebas/Unitarias/Validadores/ValidadorFichaPruebas.cs` (todo vacío salvo el celular es válido; cada máximo en su límite y un carácter por encima; celular vacío; responsable vacío con menor y con adulto; grupo sanguíneo desconocido). En `MiFichaPruebas.cs`: guardar y volver a leer devuelve exactamente lo guardado, también con otra sesión de la misma cuenta (1.2, CE-001); cambiar celular y responsable se guarda (1.3); todo vacío es válido y deja los datos en nulo (1.4); menor sin responsable, `400` en `nombreResponsable` y nada cambia (1.5); adulto sin responsable se guarda; `nombres`, `apellidos`, `fechaNacimiento`, `numeroDocumento` y `correo` enviados en el cuerpo se ignoran y no cambian (1.6); `PUT` sobre la ficha de otro jugador, `404` y nada cambia (1.7). Crear `backend/pruebas/Integracion/Ficha/UltimoCambioPruebas.cs`: tras guardar la familia, `ultimoCambio` trae la fecha, su nombre y `porLaCuentaDelJugador` verdadero; un segundo cambio sustituye al primero (CE-012); guardar lo mismo dos veces no mueve la fecha; no existe la propiedad antes del primer cambio

**Punto de control**: la familia consulta y mantiene su ficha (quickstart, bloque 1). El endpoint
de lectura ya está limitado por `ReglaAccesoAFicha` para todos los roles: no existe ninguna versión
que entregue datos clínicos a un DIRECTIVO

---

## Fase 4: Historia 2 - El club consulta la ficha según el rol (Prioridad: P1)

**Objetivo**: el PRESIDENTE, un DIRECTIVO o el entrenador de la categoría abren la ficha desde las
listas de jugadores. El PRESIDENTE ve todo, el entrenador todo menos los archivos y el DIRECTIVO
todo menos los datos clínicos.

**Prueba independiente**: con un jugador que tiene alergias y contacto de emergencia escritos, se
abre su ficha como entrenador de su categoría y se comprueba que se ven; se abre como DIRECTIVO y
se comprueba que los datos clínicos no aparecen; se intenta abrir como entrenador de otra categoría
y se comprueba que no se puede.

- [X] T009 [US2] La ficha vista por cada rol (los endpoints de T007 y T008; depende de T008). Backend: no debería hacer falta código nuevo, porque la regla ya decide desde T007; si alguna prueba falla, se corrige en `MapperFicha.cs`, `AccesoAFicha.cs` o `ReglaAccesoAFicha.cs`, nunca con una condición suelta en un servicio o un controlador. Actualizar la documentación XML de `backend/src/LFClub.Api/Controladores/Club/ControladorFichaJugador.cs` con quién ve qué. Frontend: en `frontend/src/privado/ficha/FichaJugador.tsx`, la cabecera nombra al jugador y enlaza de vuelta a su categoría o a "Categorías"; la sección de datos clínicos y la de documentos solo se pintan si el DTO las trae; quien no puede cambiar no ve ningún botón de guardar ni de editar; un `404` de la API muestra "Esta ficha no existe o no puedes verla", sin distinguir el motivo. Pruebas: crear `backend/pruebas/Integracion/Ficha/ConsultaPorRolPruebas.cs`, con la ficha de Ana sembrada: el PRESIDENTE recibe la ficha completa de un jugador con categoría, de uno sin categoría y de uno retirado, con `datosClinicos`, `documentos` y los dos permisos en verdadero (2.1); el DIRECTIVO recibe identidad, contacto, contacto de emergencia, seguridad social y `documentos` de cualquier jugador, incluido el retirado, y la respuesta **no contiene la propiedad** `datosClinicos` ni ninguno de los textos clínicos sembrados en ninguna parte del cuerpo (2.4, 2.5, CE-004); asignado como entrenador de la categoría del jugador, el DIRECTIVO recibe exactamente lo mismo (2.6); el PRESIDENTE asignado como entrenador sigue viendo todo; los dos permisos del DIRECTIVO y del ENTRENADOR llegan en falso; el presidente de otro club recibe `404` con el identificador en su propia ruta y en la del club ajeno (2.9, CE-006); el DESARROLLADOR recibe `404` (2.10); sin sesión, `401` (2.11). Crear `backend/pruebas/Integracion/Ficha/AlcanceDelEntrenadorPruebas.cs`: el ENTRENADOR recibe la ficha de un jugador de su categoría, de cualquier equipo, con `datosClinicos` y **sin la propiedad** `documentos` (2.2, CE-003); recibe `404` ante un jugador de otra categoría, uno sin categoría y uno retirado (2.3); al cambiar el jugador de categoría, el entrenador anterior recibe `404` y el de la nueva `200`, sin cerrar sesión (2.8); al retirarle la asignación recibe `404` de inmediato (caso límite); con la categoría inactiva, `404`. Crear `backend/pruebas/Integracion/Ficha/SoloLecturaPruebas.cs`: un DIRECTIVO y un ENTRENADOR, también el de la categoría del jugador, reciben `403 rol_no_autorizado` en `PUT …/ficha` y nada cambia en la base de datos (2.7, CE-005); el archivo se amplía en T011, T013 y T014 con las otras tres operaciones de cambio
- [X] T010 [P] [US2] Abrir la ficha desde las listas de jugadores (RF-034; depende de T007; archivos distintos de T009). Frontend: en `frontend/src/privado/categorias/SeccionJugadores.tsx`, `SeccionSinCategoria.tsx` y `SeccionRetirados.tsx`, el nombre del jugador pasa a ser un enlace (`Link`) a `/club/${clubId}/jugadores/${jugador.usuarioRolId}/ficha`, para todo el que ve la lista: PRESIDENTE, DIRECTIVO y ENTRENADOR en la de una categoría; PRESIDENTE y DIRECTIVO en "Sin categoría" y "Retirados". Añadir a `frontend/src/privado/categorias/textos.ts` `rutaDeFicha(clubId, usuarioRolId)` y usarla también en `DisposicionClub.tsx` e `InicioClub.tsx`. Actualizar los comentarios de las tres secciones, que hoy dicen "y nada más". Backend: actualizar la documentación XML de `backend/src/LFClub.Api/Controladores/Club/ControladorJugadores.cs` (la ficha tiene su propio controlador). Comprobar contra la API real que cada rol llega a la ficha desde sus listas y que el entrenador no tiene enlaces fuera de sus categorías

**Punto de control**: las historias 1 y 2 son el mínimo utilizable: la familia mantiene la ficha y
el club la consulta con el acceso restringido (quickstart, bloques 1 y 2)

---

## Fase 5: Historia 3 - La familia entrega la documentación del jugador (Prioridad: P2)

**Objetivo**: la familia sube un archivo para cada uno de los dos documentos pedidos, ve cuáles
entregó y puede reemplazarlos. El PRESIDENTE y los DIRECTIVOS los abren y ven en las listas a quién
le falta alguno. El ENTRENADOR no ve ni abre nada.

**Prueba independiente**: con la cuenta de un jugador se sube la copia del documento de identidad;
se entra como DIRECTIVO, se comprueba que el jugador aparece con el certificado de salud pendiente
y que el archivo entregado se puede abrir.

- [X] T011 [US3] Subir, abrir y reemplazar los archivos de la ficha (`GET` y `PUT /api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha/documentos/{documento}`; depende de T008). Backend: en `backend/src/LFClub.Aplicacion/Validadores/ValidadorImagen.cs`, hacer accesible la detección de firmas (`DetectarTipo`) sin cambiar su límite de 1 MB ni su resultado. Crear `backend/src/LFClub.Aplicacion/Validadores/ValidadorArchivoDeFicha.cs`: `TamanoMaximo` de 10 MB; acepta PDF (firma `%PDF-`) y JPEG, PNG y WebP por su firma binaria, nunca por la extensión ni por el tipo declarado; devuelve el tipo de contenido o el motivo del rechazo. Añadir a `backend/src/LFClub.Aplicacion/Utilidades/ErroresDeFicha.cs` `ArchivoDemasiadoGrande()` (`400 archivo_demasiado_grande`, "El archivo pesa más de 10 MB.") y `ArchivoNoAdmitido()` (`400 archivo_no_admitido`, "El archivo debe ser un PDF o una imagen JPEG, PNG o WebP."), este también cuando no se envía ninguno. Crear `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioDocumentosJugador.cs` y `backend/src/LFClub.Infraestructura/Repositorios/RepositorioDocumentosJugador.cs`: `EstadoAsync(usuarioRolId)` (documento, fecha, tipo y tamaño, con una proyección que **no** lee `Contenido`), `ObtenerAsync(usuarioRolId, documento)` (con contenido) y `GuardarAsync(…)`, que inserta la fila o sustituye contenido, tipo, tamaño y fecha de la que hay: "un solo archivo por documento pedido" (RF-029). Crear `backend/src/LFClub.Aplicacion/Servicios/IServicioDocumentosJugador.cs` e `Implementaciones/ServicioDocumentosJugador.cs`: **abrir** resuelve el acceso, exige `VeDocumentos` y responde `404` si el documento está pendiente; **subir** resuelve el acceso, `ExigirCambio`, valida el archivo antes de abrir la transacción y, con el club bloqueado, guarda el archivo con `SubidoEn` de `IReloj` y sella el último cambio (RF-038); si el archivo se rechaza no se toca el anterior (RF-030); devuelve la ficha. `MapperFicha` y `ServicioConsultaFicha` pasan a rellenar `documentos` con el estado real. En `backend/src/LFClub.Api/Controladores/ArchivoCargado.cs`, añadir `LimiteDePeticionDeDocumento` de 12 MB, para que sea el validador quien explique el motivo. Crear `backend/src/LFClub.Api/Controladores/Club/ControladorDocumentosJugador.cs`, ruta `api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/ficha/documentos/{documento}` **sin restricción de ruta en `{documento}`** (se recibe como texto y un valor que no es de `DocumentoPedido` responde `404`): `GET` con `[IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.JUGADOR)]`, que devuelve el archivo con su tipo de contenido, `Cache-Control: private, no-store`, `X-Content-Type-Options: nosniff` y `Content-Disposition: inline` con un nombre fijo según el documento y el formato (`copia-documento-identidad.pdf`, `certificado-salud.jpg`…); `PUT` con `[IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]`, `[RequestSizeLimit]` y `[RequestFormLimits]` con el límite nuevo. Registrar repositorio y servicio. Contrato: descomentar `…/ficha/documentos/{documento}`; `AccesoClubPruebas` pasa a 58 y 36. Frontend: crear `frontend/src/privado/ficha/SeccionDocumentos.tsx`: por cada documento pedido, su nombre, su estado con texto ("Entregado el …" con `fecha`, o "Pendiente") y `EtiquetaEstado`, "Abrir" (pide el archivo con `api.getArchivo`, lo abre en otra pestaña desde una dirección local y la libera después) y, con `permisos.puedeCambiar`, "Subir" o "Reemplazar" con `api.putArchivo`; antes de enviar avisa si el archivo pasa de 10 MB o no es PDF, JPEG, PNG ni WebP, y muestra igualmente el error que devuelva la API; un texto de ayuda dice los formatos y el tamaño. `FichaJugador.tsx` la pinta solo si el DTO trae `documentos` y actualiza la ficha con la respuesta. Pruebas: `backend/pruebas/Unitarias/Validadores/ValidadorArchivoDeFichaPruebas.cs` (cada firma con su tipo; texto disfrazado de PDF; vacío; exactamente 10 MB y un byte más). Crear `backend/pruebas/Integracion/Ficha/DocumentosPruebas.cs`: sin archivos, la ficha trae los dos documentos pendientes (3.1); subir un PDF y una imagen de cada formato deja el documento entregado con su fecha, su tipo y su tamaño, y `GET` devuelve los mismos bytes con las cuatro cabeceras (3.2); subir otro archivo al mismo documento lo reemplaza: `GET` devuelve el nuevo y en la tabla queda una sola fila (3.3); el archivo disfrazado, el vacío y la petición sin archivo responden `400 archivo_no_admitido`, y el de más de 10 MB `400 archivo_demasiado_grande`, y en los cuatro casos `GET` sigue devolviendo el anterior, o `404` si no había (3.4, CE-010); `GET` de un documento pendiente, `404`; un valor de `{documento}` desconocido, `404`; subir sella el último cambio; subir a un documento no cambia el otro. Crear `backend/pruebas/Integracion/Ficha/AccesoADocumentosPruebas.cs`: el PRESIDENTE y el DIRECTIVO abren el archivo de cualquier jugador del club, también retirado (3.5); el DIRECTIVO recibe `403` al subir; el ENTRENADOR de la categoría recibe `403 rol_no_autorizado` al abrir y al subir (3.7, CE-003); otra cuenta de jugador recibe `404` al abrir y al subir, y nada cambia (3.8, CE-002); el presidente de otro club y el DESARROLLADOR, `404` (RF-011). Añadir el `PUT` de documentos a `SoloLecturaPruebas.cs`
- [X] T012 [US3] Estado de la documentación en las listas de jugadores (`GET …/categorias/{categoriaId}`, `GET …/jugadores/sin-categoria` y `GET …/jugadores/retirados`; RF-032; depende de T011). Backend: `backend/src/LFClub.Aplicacion/DTOs/JugadorDeCategoriaDto.cs` gana `int? DocumentosPendientes`, omitido del JSON cuando es nulo, y `JugadorRetiradoDto.cs` gana `int DocumentosPendientes`; actualizar su documentación, que hoy dice "no lleva ningún otro dato suyo". Añadir a `IRepositorioDocumentosJugador`/`RepositorioDocumentosJugador` `ContarEntregadosAsync(IReadOnlyCollection<Guid> usuarioRolIds)`: una sola consulta agrupada por jugador, sin leer `Contenido`. `documentosPendientes` es 2 menos ese número; no se guarda ningún contador (CE-009). `backend/src/LFClub.Aplicacion/Mappers/MapperCategorias.cs` recibe el dato; `backend/src/LFClub.Aplicacion/Utilidades/LectorDeCategorias.cs` recibe si debe incluirlo; `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConsultaCategorias.cs` lo incluye en el detalle solo cuando `ReglaAlcanceDeCategorias.VeListasDelClub(quienPregunta.Rol)` (PRESIDENTE y DIRECTIVO) y siempre en "Sin categoría" y "Retirados"; las operaciones de la 003 que devuelven el detalle de una categoría, todas de PRESIDENTE, lo incluyen siempre. Un ENTRENADOR no recibe la propiedad (RF-007). Frontend: en `frontend/src/compartido/api/tiposCategorias.ts`, `documentosPendientes?: number` en `JugadorDeCategoriaDto` y `documentosPendientes: number` en `JugadorRetiradoDto`; en `frontend/src/privado/categorias/textos.ts`, `textoDeDocumentacion(pendientes)`: "Completa", "Falta 1" o "Faltan 2"; en `SeccionJugadores.tsx`, `SeccionSinCategoria.tsx` y `SeccionRetirados.tsx`, la columna "Documentación" con ese texto y una `EtiquetaEstado`, que solo existe cuando la propiedad viene. Pruebas: crear `backend/pruebas/Integracion/Ficha/DocumentacionEnListasPruebas.cs`: con un jugador sin archivos, otro con uno y otro con los dos, el PRESIDENTE y el DIRECTIVO reciben 2, 1 y 0 en el detalle de la categoría (3.6); lo mismo en "Sin categoría" y en "Retirados"; el número cambia al subir un archivo y no al reemplazarlo (CE-009); el ENTRENADOR recibe el detalle de su categoría **sin la propiedad** `documentosPendientes` en ningún jugador (3.7); ubicar a un jugador en un equipo, operación de la 003 que devuelve el detalle, la sigue trayendo. Crear `frontend/pruebas/documentacion.test.ts` para `textoDeDocumentacion` con 0, 1 y 2. Las pruebas de la 003 sobre esas tres listas siguen en verde sin tocarlas
  - **Diferencia anotada (T012)**: no fue posible dejarlas sin tocar. `UbicacionAutomaticaPruebas`
    (003) comprueba que las listas no llevan datos de contacto buscando la subcadena "documento"
    en toda la respuesta, y la propiedad nueva `documentosPendientes` la contiene. Se apartó ese
    nombre exacto antes de la comprobación, que sigue vigilando el documento de identidad. Por el
    mismo motivo (rutas de la ficha bajo `/jugadores/`), `SoloPresidentePruebas` y
    `CategoriasPorEstadoDelClubPruebas` (003) excluyen ahora las rutas `/ficha` de su selección
    de endpoints, y `EliminarClubPruebas` (001) siembra una ficha y un documento, porque exige
    filas en toda tabla de club antes de eliminar.

**Punto de control**: la familia entrega la documentación y el club ve a quién le falta
(quickstart, bloque 3)

---

## Fase 6: Historia 4 - Cambio del documento de identidad y corrección de la identidad (Prioridad: P3)

**Objetivo**: la familia cambia el tipo y el número de documento del jugador y sigue siendo el
mismo jugador; el PRESIDENTE corrige nombres, apellidos y fecha de nacimiento, y puede cambiar
cualquier otro dato de la ficha.

**Prueba independiente**: con la cuenta de un jugador registrado con registro civil se cambia a
tarjeta de identidad con otro número; se cierra sesión, se entra con el número nuevo y se comprueba
que el jugador conserva su categoría.

- [X] T013 [US4] Cambiar el tipo y el número del documento de identidad (`PUT /api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha/documento-identidad`; research §5; depende de T008). Backend: crear `backend/src/LFClub.Aplicacion/Validadores/ValidadorIdentidad.cs` con las comprobaciones de identidad que hoy están dentro de `ValidadorRegistro.cs` (nombres y apellidos obligatorios, máximo 80; tipo de documento de la enumeración; número normalizado con `NormalizadorTexto.Documento`, obligatorio y de máximo 20; fecha de nacimiento obligatoria y no futura), con los mismos campos y mensajes, como métodos que escriben en un `ErroresDeValidacion`; `ValidadorRegistro` pasa a delegar en ellos sin cambiar ningún resultado. Crear `backend/src/LFClub.Aplicacion/DTOs/CambiarDocumentoIdentidadDto.cs`. Añadir a `IRepositorioJugadores`/`RepositorioJugadores` `CambiarDocumentoAsync(usuarioRolId, tipo, numero)`, una sentencia sobre la misma fila. Añadir a `ErroresDeFicha.cs` `DocumentoRepetidoEnClub()` (`409 documento_repetido_en_club`, "Ese documento ya está registrado en el club.") y `DocumentoEnOtraCuenta()` (`409 documento_en_otra_cuenta`). Crear `backend/src/LFClub.Aplicacion/Servicios/IServicioIdentidadJugador.cs` e `Implementaciones/ServicioIdentidadJugador.cs` con `CambiarDocumentoAsync`: resolver el acceso y `ExigirCambio`; validar; si el tipo y el número son los que ya tiene, devolver la ficha sin registrar ningún cambio; si el número ya lo tiene **otro** integrante del club, activo o retirado, `409 documento_repetido_en_club` (RF-023, escenario 4.3); si lo tiene un integrante de **otra cuenta** en otro club, `409 documento_en_otra_cuenta` (supuesto 1), y si es de la misma cuenta en otro club se admite; en transacción y con el club bloqueado, cambiar tipo y número y sellar el último cambio; traducir la violación del índice `IndicesUnicos.DocumentoEnClub` al mismo `409`, como hace el registro. No cambia `Id`, `UsuarioId`, categoría, equipos, estado de ingreso, retiro, ficha ni archivos (RF-022), ni la contraseña ni las sesiones abiertas. En `ControladorFichaJugador.cs`, la acción con `[IntegranteDelClub(Rol.PRESIDENTE, Rol.JUGADOR)]`. Actualizar la documentación XML de `backend/src/LFClub.Dominio/Entidades/UsuarioRol.cs`. Contrato: descomentar `…/ficha/documento-identidad`; `AccesoClubPruebas` pasa a 59 y 37. Frontend: crear `frontend/src/privado/ficha/DialogoCambiarDocumento.tsx` (tipo en un `select` con `nombreDeTipoDocumento`, número, error junto al campo o como aviso si es un `409`); `SeccionIdentidad.tsx` gana el botón "Cambiar documento" con `permisos.puedeCambiar`, y tras guardar avisa de que desde ahora se entra con el número nuevo. Pruebas: `backend/pruebas/Unitarias/Validadores/ValidadorIdentidadPruebas.cs`; `ValidadorRegistroPruebas.cs` sigue en verde sin tocarlo. Crear `backend/pruebas/Integracion/Ficha/CambioDeDocumentoPruebas.cs`: la familia cambia de registro civil a tarjeta de identidad con otro número y el jugador conserva su identificador, su categoría, sus equipos, su ficha y sus archivos (4.1, CE-007); inicia sesión con el número nuevo y su contraseña, y con el anterior ya no (4.2, RF-024); la sesión abierta sigue sirviendo; el número se guarda normalizado; el número de otro integrante activo y el de uno retirado, `409 documento_repetido_en_club` y nada cambia (4.3, CE-008); el de otra cuenta en otro club, `409 documento_en_otra_cuenta`; una persona en dos clubes lo cambia en uno y el otro conserva el anterior, que sigue sirviendo para entrar; enviar el mismo documento no sella ningún cambio; tipo desconocido o número vacío, `400`; el PRESIDENTE también lo cambia; otra cuenta de jugador, `404`. Añadir la operación a `SoloLecturaPruebas.cs`
- [X] T014 [US4] Corregir nombres, apellidos y fecha de nacimiento (`PUT /api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha/identidad`; research §6; depende de T013). Backend: crear `backend/src/LFClub.Aplicacion/DTOs/CorregirIdentidadDto.cs`; añadir a `IRepositorioJugadores`/`RepositorioJugadores` `CorregirIdentidadAsync(usuarioRolId, nombres, apellidos, fechaNacimiento)`; añadir a `ServicioIdentidadJugador` `CorregirAsync`: resolver el acceso y `ExigirCorreccionDeIdentidad`; validar con `ValidadorIdentidad` (nombres y apellidos máximo 80; "la fecha no puede ser futura", RF-021); en transacción y con el club bloqueado, guardar, y si el jugador está activo y **sin categoría**, llamar a `UbicadorDeJugadores.UbicarAUnoAsync` con la fecha nueva (RF-026); a quien ya tiene categoría nunca se le mueve (RF-025) y a un retirado no se le ubica; sellar el último cambio si algo quedó distinto. No exige responsable aunque el jugador pase a ser menor (supuesto 2). No toca `AprobadoPorNombre`, `RetiradoPorNombre` ni ningún otro nombre ya copiado (§13). En `ControladorFichaJugador.cs`, la acción con `[IntegranteDelClub(Rol.PRESIDENTE)]`: a la cuenta del jugador, `403 rol_no_autorizado`. Contrato: descomentar `…/ficha/identidad`; `AccesoClubPruebas` pasa a 60 y 38, y en el contrato no queda ninguna línea `# PENDIENTE`. Frontend: crear `frontend/src/privado/ficha/DialogoCorregirIdentidad.tsx` (nombres, apellidos, fecha de nacimiento con máximo hoy, errores junto a cada campo); `SeccionIdentidad.tsx` gana "Corregir identidad" con `permisos.puedeCorregirIdentidad`; tras guardar, la ficha muestra la categoría en la que quedó. Pruebas: crear `backend/pruebas/Integracion/Ficha/CorreccionDeIdentidadPruebas.cs`: el PRESIDENTE corrige nombres y apellidos y la lista de la categoría y la sesión del jugador muestran los nuevos (4.4); corregir la fecha a otro año de un jugador con categoría lo deja en la que tenía (4.5); la de un jugador sin categoría hacia un año con categoría activa lo ubica en ella, y hacia un año sin categoría o con la categoría inactiva lo deja sin categoría (4.6); la de un retirado no lo ubica; una fecha futura, `400` en `fechaNacimiento` y nada cambia (4.7); nombres vacíos o de 81 caracteres, `400`; corregir mientras el presidente crea la categoría de ese año (`Task.WhenAll`, repetido, el patrón de `UbicacionAutomaticaPruebas`) lo deja siempre dentro; el nombre copiado en un retiro anterior no cambia; la cuenta del jugador recibe `403 rol_no_autorizado` y nada cambia (1.6); el DIRECTIVO y el ENTRENADOR, `403` (añadirlo a `SoloLecturaPruebas.cs`, que con esto cubre las cuatro operaciones de cambio)
- [X] T015 [US4] El PRESIDENTE cambia cualquier dato de la ficha (escenario 4.8, RF-018; los endpoints de T008, T011 y T013; depende de T014). Backend: no debería hacer falta código nuevo; si alguna prueba falla se corrige en la regla o en `AccesoAFicha`. Frontend: comprobar contra la API real que el PRESIDENTE ve en la ficha de un jugador de su club el formulario, "Cambiar documento", "Corregir identidad" y "Subir" o "Reemplazar", también en un jugador sin categoría y en uno retirado, y que el correo aparece como texto sin campo; corregir en `frontend/src/privado/ficha/FichaJugador.tsx` lo que falle. Pruebas: crear `backend/pruebas/Integracion/Ficha/PresidenteCambiaFichaPruebas.cs`: el PRESIDENTE guarda celular, responsable, contacto de emergencia, seguridad social y datos clínicos de un jugador, y la cuenta del jugador lee exactamente lo mismo (CE-001); sube y reemplaza un documento, que la familia abre; `ultimoCambio` trae el nombre del PRESIDENTE y `porLaCuentaDelJugador` falso, y la familia lo ve (1.9, CE-012); lo hace también sobre un jugador sin categoría y sobre uno retirado; no puede cambiar el correo: un `correo` en el cuerpo se ignora (RF-018); el presidente de otro club recibe `404` en las cuatro operaciones

**Punto de control**: el documento y la identidad se mantienen al día sin crear otro jugador
(quickstart, bloque 4)

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: lo que afecta a varias historias y la validación de extremo a extremo.

- [X] T016 [P] Pruebas transversales en `backend/pruebas/Integracion/Ficha/`. Crear `FichaEntreClubesPruebas.cs`: una cuenta que es jugador en dos clubes tiene una ficha en cada uno; lo guardado de salud, el contacto de emergencia y los archivos de un club no aparecen en el otro, ni para la familia ni para el presidente del otro club (RF-003); el celular y el responsable cambiados desde la ficha de un club, por la familia o por su PRESIDENTE, se leen en la ficha del otro (RF-039); ese cambio sella el último cambio solo en la ficha desde la que se hizo (supuesto 4). Crear `ConservacionPruebas.cs`: retirar a un jugador no cambia ninguna fila de `FichasJugador` ni de `DocumentosJugador`, el PRESIDENTE y el DIRECTIVO siguen viendo su ficha y abriendo sus archivos, y al reincorporarlo la familia la encuentra igual (RF-036); rechazar a un jugador en espera con ficha y archivos sembrados, y eliminar el club, no dejan ninguna fila suya en las dos tablas, y las de otro club quedan intactas (RF-037); eliminar la cuenta de quien hizo el último cambio conserva `UltimoCambioPorNombre` y deja la referencia en nulo. Crear `FichaPorEstadoDelClubPruebas.cs`: en un club suspendido el PRESIDENTE lee y cambia fichas y los demás roles reciben `403 club_suspendido` en las seis operaciones; en uno dado de baja nadie accede. Añadir a `UltimoCambioPruebas.cs` dos `PUT …/ficha` simultáneos sobre la misma ficha (`Task.WhenAll`, repetido): los dos responden `200`, queda una sola fila y coincide por completo con uno de los dos cuerpos, sin mezcla (caso límite)
- [X] T017 [P] Repasar la documentación de lo que cambió de responsabilidad (§3). Buscar con `Grep` en `backend/src/` y `frontend/src/` las frases que ya no son ciertas ("no lleva ningún otro dato", "y nada más", "todavía no existe la entidad Jugador" sin mencionar la ficha, "se fija al registrarse" referido a documento, nombres, fecha, celular o responsable, "solo sale por la API en la sala de espera") y corregir cada comentario y cada documentación XML que las contenga, sin tocar código. Deben quedar revisados, como mínimo: `UsuarioRol.cs`, `Usuario.cs`, `IPerteneceAClub.cs`, `ContextoLFClub.cs`, `ControladorJugadores.cs`, `JugadorDeCategoriaDto.cs`, `JugadorRetiradoDto.cs`, `ValidadorImagen.cs`, `ArchivoCargado.cs` y las tres secciones de listas del frontend
- [X] T018 [P] Contrastar la API con `specs/005-ficha-jugador/contracts/api.yaml`: arrancar la API, abrir Swagger y comprobar en los seis endpoints nuevos y los cuatro que cambian las rutas, los cuerpos, los códigos de estado, los nombres de DTO y los campos del contrato, y que en respuestas reales `datosClinicos`, `documentos`, `ultimoCambio` y `documentosPendientes` se omiten cuando corresponde en lugar de ir en `null`. Comprobar que ningún endpoint bajo `/api/publico` ni ningún DTO público contiene datos de la ficha (RF-014). Corregir el código o, si el contrato tenía un error, el contrato, y anotar la diferencia debajo de esta tarea
  - **Resultado (T018)**: Swagger de la API en marcha contrastado con el contrato: 60 operaciones, los
    seis endpoints nuevos y los cuatro que cambian coinciden en ruta, método, códigos de estado,
    nombres de DTO y campos; sin diferencias. En respuestas reales las propiedades se omiten cuando
    corresponde. El único endpoint público es el escudo, que devuelve una imagen.
- [X] T019 Revisar a 360 px de ancho, en tema claro y en oscuro, con la identidad de un club con colores propios (RF-035, CE-011; quickstart, bloque 6): la ficha vista por la familia con su formulario y sus errores, la ficha de solo lectura del entrenador y la del directivo, el apartado "Documentos" con un documento entregado y otro pendiente, el diálogo de cambio de documento, el de corrección de identidad, la línea del último cambio y las tres listas de jugadores con la columna "Documentación". Sin desplazamiento horizontal, con texto legible y con "Entregado", "Pendiente", "Completa" y "Faltan N" leídos como texto, no solo por color. Si el lienzo de §24 (<https://claude.ai/artifact/PiDB5BQXRYCeN6nnB4TikR>) trae una pantalla de ficha, contrastarla con él. Corregir lo que falle en los archivos de `frontend/src/privado/ficha/` y `frontend/src/privado/categorias/`
  - **Resultado (T019)**: 36 pantallas revisadas a 360 px (18 por tema) con el Club A en vinotinto y
    dorado: sin desplazamiento horizontal y con los estados como texto. No hubo que corregir nada.
  - **Lienzo de §24**: sí trae una pantalla de ficha (`M-Jugador`). Coincide la estructura en
    tarjetas, con un apartado de datos médicos y de contacto y otro de documentos con su estado en
    una etiqueta. El lienzo muestra además foto, posición, dorsal, estadísticas y evaluación, tres
    documentos con vencimiento ("Vence en 20 días") y el texto "Solo lo ven la familia y los
    entrenadores de su categoría": todo eso queda fuera del alcance de la spec 005 (dos documentos
    fijos, sin vencimientos; el PRESIDENTE también ve los datos clínicos) y no se construyó.
- [X] T020 Recorrer `specs/005-ficha-jugador/quickstart.md`, bloques 1 a 6, y ejecutar la batería entera (`dotnet test backend/LFClub.sln`, `npm --prefix frontend test`, `scripts/verificar-tamano.ps1`). Levantar el recorrido en un proyecto de Compose aparte, con la llave de Brevo vacía y su propio volumen, para no enviar correos reales ni tocar los datos de desarrollo, y borrarlo al terminar. Anotar en este archivo, debajo de esta tarea, cualquier paso que no dé el resultado esperado, y corregirlo antes de marcarla
  - **Resultado (T020)**: quickstart recorrido contra la API real en un proyecto de Compose aparte
    (`lfclub-validacion`, Brevo vacío, volumen propio, borrado al terminar): 48 de 48 pasos de los
    bloques 1 a 5 con el resultado esperado, y el bloque 6 con la revisión de T019. Batería: 222
    pruebas unitarias, 499 de integración y 98 de frontend en verde; `tsc`, `eslint` y
    `verificar-tamano.ps1` sin errores. Ningún paso dio un resultado distinto del esperado.

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (fase 1)**: sin dependencias.
- **Cimientos (fase 2)**: depende de la fase 1. Bloquea todas las historias.
- **Historia 1 (fase 3)**: depende de los cimientos.
- **Historia 2 (fase 4)**: depende de la historia 1: consulta la ficha que la familia escribe, sobre
  los mismos endpoints.
- **Historia 3 (fase 5)**: depende de la historia 1 (T008, por el sellado del último cambio). No
  depende de la historia 2.
- **Historia 4 (fase 6)**: depende de la historia 1 (T008). No depende de las historias 2 ni 3,
  salvo T015, que prueba también la subida de archivos de T011.
- **Cierre (fase 7)**: depende de las historias que se quieran entregar.

### Entre tareas

```text
T001 ─┬─ T002 ─┬─ T005 ─┐
      ├─ T003 ─┘        ├─ T007 ── T008 ─┬─ T009 ───────────────┐
      ├─ T004 ──────────┤      │         ├─ T011 ── T012 ───────┼─ T016, T017, T018 ── T019 ── T020
      └─ (T002) ─ T006 ─┘      └─ T010   └─ T013 ── T014 ── T015 ┘
```

- T007 → T008: primero leer, después guardar; T008 crea el sellado del último cambio que usan
  T011, T013 y T014.
- T011 → T012: el estado en las listas cuenta los archivos que T011 permite subir.
- T013 → T014: T013 extrae `ValidadorIdentidad` y crea `ServicioIdentidadJugador`; T014 los amplía.
- T011, T013 y T014 tocan `AccesoClubPruebas.cs`, `SoloLecturaPruebas.cs` y el contrato: si se
  hacen en otro orden, los contadores se ajustan al número real de rutas descomentadas.

### Dentro de cada tarea

- Entidad o DTO → repositorio → servicio → controlador → contrato → pantalla → pruebas.
- La batería completa en verde antes de pasar a la tarea siguiente.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos: archivos distintos, sin dependencias entre sí
T002  entidades, configuraciones y migración
T003  ReglaAccesoAFicha.cs y sus pruebas
T004  ClubDto.miUsuarioRolId
T006  SembradorFichas.cs y EscenarioFicha.cs   (cuando T002 esté hecha)

# Historia 2
T009  pruebas por rol y pantalla de la ficha
T010  enlaces en las tres secciones de listas

# Tras T008, las historias 3 y 4 no comparten archivos de producción
T011 → T012   documentos
T013 → T014   documento de identidad e identidad
      (coinciden en ControladorFichaJugador.cs solo T013 y T014 entre sí; con T011, en
       ErroresDeFicha.cs, SoloLecturaPruebas.cs, AccesoClubPruebas.cs y el contrato)

# Cierre
T016  pruebas transversales    T017  documentación    T018  contrato frente a Swagger
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T007, T008) e historia 2 (T009, T010).
3. **Parar y validar**: quickstart, bloques 1 y 2.

### Entrega incremental

1. Historias 1 y 2 → la familia mantiene la ficha y el club la consulta con acceso restringido.
2. Historia 3 → documentos entregados y estado en las listas.
3. Historia 4 → cambio de documento y corrección de identidad.
4. Cierre → pruebas transversales, documentación, contrato, 360 px y quickstart completo.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- La API aplica la migración `FichaDelJugador` al arrancar; los jugadores que ya existen quedan
  con la ficha vacía y sin documentos, sin ningún tratamiento.
- Persona en dos clubes: el documento se cambia club por club y el número anterior sigue sirviendo
  para entrar mientras el otro club lo conserve. Es una consecuencia asumida en el plan, no una
  tarea.
- El hermano agregado desde la ficha, el cambio de rol que elimina la ficha (§12.2) y la
  eliminación de la cuenta a petición quedan fuera (research §13).
- Si una prueba de la 001 a la 004 falla al tocar `ValidadorRegistro`, `ValidadorImagen`,
  `LectorDeCategorias` o los DTO de listas y no está claro si es una regresión o un cambio
  previsto, se detiene esa parte y se pregunta (§25).
- Confirmar el cambio en git al terminar cada tarea o cada historia.
