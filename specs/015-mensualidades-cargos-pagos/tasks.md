---

description: "Lista de tareas de la funcionalidad 015: mensualidades, cargos y pagos"
---

# Tareas: Mensualidades, cargos y pagos

**Entrada**: documentos de diseño en `specs/015-mensualidades-cargos-pagos/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
quickstart.md

**Punto de partida confirmado (T002)**: contrato 113 y club 87, según `AccesoClubPruebas`; ningún
archivo escrito a mano supera las 250 líneas (T001, `verificar-tamano.ps1`).

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.14.0) y el plan (research §17).
Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa contra la API real
y sus pruebas; las tareas que solo traen pruebas, solo reglas puras o solo pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario. Orden de construcción: preparación,
base, historia 1 (P1), historia 2 (P1), historia 3 (P1), historia 4 (P1), historia 5 (P2),
historia 6 (P2), historia 7 (P2), historia 8 (P2), historia 9 (P3) y cierre.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US9]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 014, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): **una sola migración, `MensualidadesYPagos`** (T009): cinco tablas
  (`Cargos`, `Pagos`, `AplicacionesPago`, `SoportesPago`, `MediosPagoClub`) y cuatro columnas
  (`Clubes.ValorMensualidad`, `Clubes.UltimoRecibo`, `UsuariosRol.ValorMensualidadPropio`,
  `UsuariosRol.CobraDesde`). Si una tarea posterior parece necesitar otra tabla, columna o índice,
  se detiene y se informa (§25).
- **Contrato**: `specs/015-mensualidades-cargos-pagos/contracts/api.yaml` manda en rutas, cuerpos,
  estados y códigos de error. **Punto de partida esperado: contrato 113 y club 87** (confirmarlo en
  T002 con la expresión regular de `EndpointsDeLaApi` antes de fiarse). Recorrido: 113/87 →
  116/90 (historia 2, 3 operaciones) → 117/91 (historia 3, 1) → 119/93 (historia 4, 2) →
  123/97 (historia 5, 4) → 128/102 (historia 6, 5) → 129/103 (historia 7, 1) → 130/104
  (historia 8, 1) → **131/105** (historia 9, 1). El contrato ya describe las 18; el recuento real sube al construir
  cada una.
- **Lo anterior no cambia**: ninguna operación existente cambia, salvo el campo nulo `estadoCuenta`
  de `JugadorDeListaDto` (historia 3). Si una prueba de la 001 a la 014 falla y no está claro que
  sea una regresión, se detiene y se pregunta (§27.2).
- **Dinero** (§16.2): siempre `decimal` en C# y `numeric(14,2)` en la base; el validador exige un
  número entero de pesos y mayor que cero (RF-014). Nunca `float` ni `double`. El frontend lo muestra
  con formato de moneda colombiana (`$ 65.000`).
- **Pago confirmado inmutable** (§16.3, RF-018): no existe ninguna operación que edite o borre un
  pago confirmado; solo se anula.
- **Autorización en el servidor** (§15, RF-035): `IntegranteDelClub` deja al ENTRENADOR fuera con
  `403 rol_no_autorizado`; lo ajeno (otra familia, otro club) responde `404 no_encontrado`; el
  DESARROLLADOR no tiene ruta de club.
- **Sin recordatorios ni consecuencias por deber** (§16.8, RF-015, RF-016): nada envía avisos de
  cobro y deber no restringe ningún acceso.
- **Nombres** (§2.1, §2.2): todo en español: `Cargo`, `Pago`, `AplicacionPago`, `SoportePago`,
  `MedioPagoClub`, `ReglaMesDeCobro`, `ReglaEstadoDeCuenta`, `ReglaAplicacionDePago`,
  `ReglaAccesoACuenta`, `ReglaTransicionPago`, `ServicioCuentaDelJugador`.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en el
  frontend) con qué representa, su responsabilidad y qué no hace.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Medir con
  `scripts/verificar-tamano.ps1` al empezar (T001) y al cerrar. Los servicios se dividen por tarea
  (consulta, registro, revisión, anulación, finanzas), no por entidad.
- **Bash** (memoria del proyecto): los archivos nuevos se crean con la herramienta Write, no con
  heredocs; un `testhost` colgado se mata con `Get-Process testhost | Stop-Process -Force` antes de
  compilar; la batería de integración tarda unos 12 minutos.
- **Reloj**: las pruebas de integración que cruzan meses sustituyen `IReloj`; el mes de cobro es el
  de Colombia (UTC−5 fijo, research §3).

---

## Fase 1: Preparación

**Propósito**: comprobar el punto de partida.

- [X] T001 Ejecutar `scripts/verificar-tamano.ps1` y anotar los archivos por encima de 240 líneas que
      esta funcionalidad vaya a tocar (`frontend/src/privado/jugadores/TablaDeGrupo.tsx`,
      `frontend/src/privado/MenuDelClub.tsx`, `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEliminacionCuenta.cs`,
      `ServicioAprobacionIngreso`, `ServicioAceptacionInvitacion`, `ServicioRegistroConInvitacion`,
      `ServicioRetiroJugador`); si alguno está cerca del tope, dividirlo antes de añadirle código
- [X] T002 Contar las operaciones con la expresión regular de `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs`
      y confirmar el punto de partida 113/87; anotar el resultado en la cabecera de este archivo
- [X] T003 [P] Comprobar que `dotnet build backend` y `npm --prefix frontend run build` pasan antes de
      empezar, y que `docker`/Testcontainers están disponibles para la batería de integración

---

## Fase 2: Base (prerrequisitos que bloquean todas las historias)

**Propósito**: entidades, migración, reglas puras y tipos compartidos que usan todas las historias.

**CRÍTICO**: ninguna historia empieza hasta terminar esta fase.

### Enumeraciones y entidades

- [X] T004 [P] Crear las enumeraciones `TipoCargo` (`MENSUALIDAD`, `OTRO`), `MedioPago` (`EFECTIVO`,
      `TRANSFERENCIA`), `EstadoPago` (`EN_REVISION`, `CONFIRMADO`, `RECHAZADO`, `ANULADO`) y
      `EstadoDeCuenta` (`AL_DIA`, `PAGO_PARCIAL`, `DEBE`) en
      `backend/src/LFClub.Dominio/Enumeraciones/TipoCargo.cs`, `MedioPago.cs`, `EstadoPago.cs` y
      `EstadoDeCuenta.cs`, con documentación XML
- [X] T005 [P] Crear la entidad `Cargo` en `backend/src/LFClub.Dominio/Entidades/Cargo.cs`
      implementando `IPerteneceAClub`: `Id` (uuid v7), `ClubId`, `UsuarioRolId`, `Tipo` (texto(20)),
      `Mes` (`DateOnly`, "primer día del mes que cobra; obligatorio en `MENSUALIDAD`"), `Valor`
      (`decimal`, "mayor que cero"), `CategoriaId` (nulo, categoría del jugador cuando se generó, §13),
      `CreadoEn`; inmutable tras crearse (RF-013)
- [X] T006 [P] Crear las entidades `Pago`, `AplicacionPago`, `SoportePago` y `MedioPagoClub` en
      `backend/src/LFClub.Dominio/Entidades/Pago.cs`, `AplicacionPago.cs`, `SoportePago.cs` y
      `MedioPagoClub.cs`, todas con `IPerteneceAClub`, con los campos de data-model.md y estas
      restricciones verbatim: `Pago.Valor` "mayor que cero"; `Pago.Entidad` texto(40), "obligatoria en
      `TRANSFERENCIA`; nula en `EFECTIVO`"; `Pago.Referencia` texto(60) opcional; `Pago.Fecha` `date`,
      "no futura"; `Pago.JugadorNombre` texto(200), `JugadorTipoDocumento` y `JugadorNumeroDocumento`
      texto(30), copiados al crear el pago; `Pago.Concepto` texto(300) nulo; `MotivoRechazo` y
      `MotivoAnulacion` texto(300), "no vacíos cuando hay rechazo o anulación"; `NumeroRecibo` "solo con
      `CONFIRMADO` o `ANULADO`"; `AplicacionPago` con PK compuesta (`PagoId`, `CargoId`) y `Valor` mayor
      que cero; `SoportePago` con `Contenido` hasta 5 MB y `TipoContenido` texto(20) (`image/png`,
      `image/jpeg` o `image/webp`); `MedioPagoClub.Entidad` texto(40), `Titular` texto(100), `Numero`
      texto(60) obligatorios, `Nota` texto(200) nulo, `Activo`
- [X] T007 Añadir `ValorMensualidad` (`decimal?`, nulo = sin definir) y `UltimoRecibo` (`int`, 0) a
      `backend/src/LFClub.Dominio/Entidades/Club.cs`, y `ValorMensualidadPropio` (`decimal?`) y
      `CobraDesde` (`DateOnly?`, nulo en quien no es jugador) a
      `backend/src/LFClub.Dominio/Entidades/UsuarioRol.cs`

### Persistencia

- [X] T008 Crear las configuraciones de EF Core en
      `backend/src/LFClub.Infraestructura/Datos/Configuraciones/CargoConfiguracion.cs`,
      `PagoConfiguracion.cs`, `AplicacionPagoConfiguracion.cs`, `SoportePagoConfiguracion.cs` y
      `MedioPagoClubConfiguracion.cs` (enumeraciones como texto con `HasConversion<string>()`,
      `numeric(14,2)`, claves y borrados de data-model.md: `Cargo.UsuarioRolId` Cascade,
      `Cargo.CategoriaId` SetNull, `Pago.UsuarioRolId` y todas las claves "por usuario" SetNull, el resto
      Cascade; índice único `(UsuarioRolId, Mes)` donde `Tipo = 'MENSUALIDAD'`, índice único
      `(ClubId, NumeroRecibo)` donde no es nulo, `(ClubId, Estado)`, `(UsuarioRolId)`, `(ClubId,
      UsuarioRolId)` y `(ClubId, Mes)`; `CHECK` baratos de data-model.md); registrar los `DbSet` y el
      filtro de aislamiento en `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs`; mapear
      las cuatro columnas nuevas en las configuraciones de `Club` y `UsuarioRol`
- [X] T009 Generar la migración `MensualidadesYPagos` en
      `backend/src/LFClub.Infraestructura/Datos/Migraciones/` y editarla a mano para que fije
      `ValorMensualidad = 65000` en el club cuyo nombre normalizado empieza por "valfor" (§16.7) y
      `CobraDesde` en el primer día del mes de la migración (hora de Colombia) para los
      `UsuariosRol` que ya son jugadores (research §5, nota 2 del plan)
- [X] T010 Actualizar el guardián de esquema en
      `backend/pruebas/Integracion/Cuenta/EsquemaDeEliminacionPruebas.cs` para las claves nuevas (todas
      hacia `Usuario` o `UsuarioRol` son `Cascade` o `SetNull`) y añadir la prueba que exige que
      ningún `SoportePago` sobreviva a la eliminación de la cuenta de su jugador (queda en rojo hasta
      T079)

### Reglas puras (dominio) y sus pruebas

- [X] T011 [P] Crear `ReglaMesDeCobro` en `backend/src/LFClub.Dominio/Reglas/ReglaMesDeCobro.cs`:
      `MesEnCurso(DateTime ahoraUtc)` (primer día del mes en Colombia, UTC−5 fijo, sin `TimeZoneInfo`) y
      `PrimerMesDeCobro(DateTime ahoraUtc)` (el mes siguiente a la entrada, aprobación o
      reincorporación); y su prueba en `backend/pruebas/Unitarias/Reglas/ReglaMesDeCobroPruebas.cs`
      (31 de octubre 23:30 en Bogotá aunque sea noviembre en UTC, cambio de año, primer día del mes)
- [X] T012 [P] Crear `ReglaEstadoDeCuenta` en
      `backend/src/LFClub.Dominio/Reglas/ReglaEstadoDeCuenta.cs` (pendiente de cada cargo, saldo,
      `AL_DIA` si saldo 0, `PAGO_PARCIAL` si saldo > 0 y hay algo aplicado, `DEBE` si saldo > 0 y nada
      aplicado; solo suman aplicaciones de pagos `CONFIRMADO`) y su prueba en
      `backend/pruebas/Unitarias/Reglas/ReglaEstadoDeCuentaPruebas.cs` (dos cargos de 65.000 y abono de
      90.000 → saldo 40.000 y `PAGO_PARCIAL`; pago anulado no suma)
- [X] T013 [P] Crear `ReglaAplicacionDePago` en
      `backend/src/LFClub.Dominio/Reglas/ReglaAplicacionDePago.cs`:
      `Repartir(cargosPendientes, valor, cargoIdsElegidos?)` ordena por mes ascendente (RF-024), usa solo
      los elegidos si los hay, llena cada cargo hasta su pendiente, y devuelve error `pago_excede_deuda`
      (con lo que debe) o `sin_deuda`; y su prueba en
      `backend/pruebas/Unitarias/Reglas/ReglaAplicacionDePagoPruebas.cs` (más antiguo primero, tope,
      cargos elegidos, abonos parciales, sin pendiente)
- [X] T014 [P] Crear `ReglaAccesoACuenta` en
      `backend/src/LFClub.Dominio/Reglas/ReglaAccesoACuenta.cs` (PRESIDENTE y DIRECTIVO ven cualquier
      jugador del club, también retirado; la cuenta de jugador solo los suyos y no si está retirado;
      solo el PRESIDENTE opera; ENTRENADOR y DESARROLLADOR nada) y su prueba en
      `backend/pruebas/Unitarias/Reglas/ReglaAccesoACuentaPruebas.cs` (una fila por rol)
- [X] T015 [P] Crear `ReglaTransicionPago` en
      `backend/src/LFClub.Dominio/Reglas/ReglaTransicionPago.cs` (`EN_REVISION → CONFIRMADO | RECHAZADO`,
      `CONFIRMADO → ANULADO`, nada más; `RECHAZADO` y `ANULADO` son finales; códigos
      `pago_no_esta_en_revision` y `pago_no_confirmado`) y su prueba en
      `backend/pruebas/Unitarias/Reglas/ReglaTransicionPagoPruebas.cs`
- [X] T016 [P] Crear `ValidadorPago` (valor entero de pesos y mayor que cero, fecha no futura, entidad
      obligatoria en `TRANSFERENCIA`, referencia hasta 60, motivo hasta 300 y no vacío) en
      `backend/src/LFClub.Aplicacion/Validadores/ValidadorPago.cs`, y `ValidadorSoportePago` (PNG, JPEG
      o WebP por firma, hasta 5 MB; reutiliza la detección de `ValidadorImagen`) en
      `backend/src/LFClub.Aplicacion/Validadores/ValidadorSoportePago.cs`; pruebas en
      `backend/pruebas/Unitarias/Validadores/ValidadorPagoPruebas.cs` y `ValidadorSoportePagoPruebas.cs`

### Contratos de aplicación y frontend compartido

- [X] T017 [P] Crear las interfaces `IRepositorioCargos`, `IRepositorioPagos`,
      `IRepositorioMediosPago` e `IRepositorioFinanzas` en
      `backend/src/LFClub.Aplicacion/Interfaces/` (cada una con documentación XML; las operaciones se
      añaden en cada historia)
- [X] T018 [P] Crear los tipos TypeScript del contrato en
      `frontend/src/compartido/api/tiposPagos.ts` (`EstadoDeCuenta`, `EstadoPago`, `MedioPago`,
      `TipoCargo`, `MensualidadDto`, `MedioPagoDto`, `CargoDto`, `PagoDto`, `CuentaJugadorDto`,
      `PagosEnRevisionDto`, `FinanzasDto` y sus cuerpos de petición, tal como están en `contracts/api.yaml`)
- [X] T019 [P] Crear `frontend/src/privado/pagos/formatoMoneda.ts` (pesos colombianos, sin decimales,
      `$ 65.000`), `frontend/src/privado/pagos/textosPagos.ts` (etiquetas "Al día", "Pago parcial",
      "Debe", "En revisión", "Confirmado", "Rechazado", "Anulado": el estado siempre con texto y no solo
      con color) y su prueba `frontend/pruebas/pagos/formatoMoneda.test.ts`

**Punto de control**: la base compila, la migración se aplica y las reglas puras pasan; ya se puede
empezar cualquier historia.

---

## Fase 3: Historia 1 - La mensualidad se genera sola cada mes (Prioridad: P1) 🎯 MVP

**Objetivo**: cada mes hay un cargo MENSUALIDAD por jugador activo, sin intervención y sin duplicados;
quien entra o se reincorpora paga desde el mes siguiente.

**Prueba independiente**: club con tres jugadores activos y uno retirado: al llegar el día 1 hay
exactamente tres cargos con el valor vigente y ninguno para el retirado; un cuarto jugador aprobado
no recibe cargo ese mes y sí el siguiente (spec, historia 1).

### Pruebas de la historia 1

- [X] T020 [P] [US1] Escribir las pruebas de integración de generación en
      `backend/pruebas/Integracion/Pagos/GeneracionDeCargosPruebas.cs` (con `IReloj` sustituible):
      cargos de los activos con el valor del club o el propio; dos ejecuciones y dos instancias
      simuladas no duplican (CE-001); jugador en espera, retirado, ENTRENADOR y DIRECTIVO sin cargo;
      club sin valor sin cargos; club suspendido sí genera (§7.4); la categoría del cargo es la actual
      del jugador; sin cargos retroactivos
- [X] T021 [P] [US1] Escribir las pruebas de integración del primer mes de cobro en
      `backend/pruebas/Integracion/Pagos/PrimerMesDeCobroPruebas.cs`: ingreso aprobado, invitación
      como JUGADOR, hermano agregado y reincorporación fijan `CobraDesde` al mes siguiente, no hay
      cargo del mes en curso ni de los meses fuera, y sí al empezar el siguiente

### Implementación de la historia 1

- [X] T022 [US1] Añadir `GenerarDelMesAsync(DateTime ahoraUtc)` a `IRepositorioCargos` e implementarlo
      en `backend/src/LFClub.Infraestructura/Repositorios/RepositorioCargos.cs` como **una sentencia**
      `INSERT … SELECT … ON CONFLICT DO NOTHING` con `IgnoreQueryFilters` (nota 3 del plan): jugadores
      con `Rol = JUGADOR`, `EstadoIngreso = APROBADO`, `Activo`, `CobraDesde` ≤ mes en curso y club con
      valor; valor `COALESCE(ValorMensualidadPropio, Club.ValorMensualidad)`; categoría actual; sin
      devolver datos de ningún club
- [X] T023 [US1] Crear `IServicioGeneracionCargos` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioGeneracionCargos.cs` y su implementación en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioGeneracionCargos.cs`, que llama al
      repositorio con `IReloj.AhoraUtc`
- [X] T024 [US1] Crear `TrabajoCargosMensuales` (`BackgroundService` con `PeriodicTimer`: al arrancar y
      cada 30 minutos, captura y registra errores sin tumbar la API) en
      `backend/src/LFClub.Infraestructura/Trabajos/TrabajoCargosMensuales.cs` y registrarlo con
      `AddHostedService` junto con el servicio en la composición de dependencias de
      `backend/src/LFClub.Api/` (es el primer servicio en segundo plano de la API); desactivable en
      las pruebas de integración que no lo necesiten
- [X] T025 [US1] Fijar `CobraDesde = ReglaMesDeCobro.PrimerMesDeCobro(IReloj.AhoraUtc)` en los cuatro
      sitios de entrada, sin cambiar sus rutas ni respuestas:
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAprobacionIngreso.cs`,
      `ServicioAceptacionInvitacion.cs`, `ServicioRegistroConInvitacion.cs` y
      `ServicioRetiroJugador.cs` (en `ReincorporarAsync`); si un archivo se acerca a 250 líneas, extraer
      la asignación a una clase pequeña
- [X] T026 [US1] Comprobar con las pruebas T020 y T021 y con la batería de ingresos, hermanos y retiro
      (no regresión, §27.2) que ningún flujo existente cambió

**Punto de control**: la historia 1 funciona sola: los cargos nacen, sin duplicados, aunque aún no haya
pantalla. Su pantalla es la cuenta del jugador (historia 3), que comparte el mismo backend de lectura.

---

## Fase 4: Historia 2 - El PRESIDENTE define el valor de la mensualidad (Prioridad: P1)

**Objetivo**: el PRESIDENTE fija el valor del club y el propio de uno o varios jugadores; solo cambia
cargos futuros.

**Prueba independiente**: cambiar de 65.000 a 70.000: los cargos ya generados siguen en 65.000 y los del
mes siguiente nacen en 70.000; fijar 50.000 a dos jugadores y que solo a ellos les cambie.

**Contrato**: 113/87 → 116/90.

### Pruebas de la historia 2

- [X] T027 [P] [US2] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/MensualidadPruebas.cs`: `GET`/`PUT /mensualidad` y
      `PUT /mensualidad/jugadores`; cargos ya generados conservan su valor (CE-006); valor propio y
      volver al del club; vacío, cero, negativo y centavos → `400 validacion`; DIRECTIVO, ENTRENADOR y
      cuenta de jugador no pueden cambiarlo (403) ni llamando al servidor; un jugador de otro club →
      `404` sin cambiar ninguno; fijar el valor a un club sin valor genera en el acto los cargos del
      mes en curso
- [X] T028 [P] [US2] Escribir la prueba de pantalla en
      `frontend/pruebas/pagos/PantallaMensualidad.test.tsx`: muestra el valor vigente, rechaza valores
      inválidos con mensaje, permite seleccionar jugadores y fijar o quitar su valor propio, el aviso
      "Defina el valor de la mensualidad" aparece si `valorClub` es nulo

### Implementación de la historia 2

- [X] T029 [P] [US2] Crear los DTOs `MensualidadDto`, `JugadorConValorPropioDto`,
      `FijarValorMensualidadDto` y `FijarValorPropioDto` en
      `backend/src/LFClub.Aplicacion/DTOs/` (`MensualidadDto.cs`, `FijarValorMensualidadDto.cs`,
      `FijarValorPropioDto.cs`)
- [X] T030 [US2] Crear `IServicioMensualidad` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioMensualidad.cs` e implementarlo en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioMensualidad.cs` (consultar, fijar valor
      del club con `ValidadorPago`-regla de entero de pesos, fijar o quitar valor propio de varios en una
      transacción con todo o nada, invocar `IServicioGeneracionCargos` tras fijar el valor del club)
- [X] T031 [US2] Crear `ControladorMensualidad` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorMensualidad.cs` con
      `GET /api/clubes/{clubId}/mensualidad` (`IntegranteDelClub(PRESIDENTE, DIRECTIVO)`),
      `PUT /api/clubes/{clubId}/mensualidad` y `PUT /api/clubes/{clubId}/mensualidad/jugadores`
      (`IntegranteDelClub(PRESIDENTE)`); delega en el servicio
- [X] T032 [P] [US2] Crear el cliente de API y el gancho en `frontend/src/privado/pagos/apiMensualidad.ts`
      y `frontend/src/privado/pagos/useMensualidad.ts`
- [X] T033 [US2] Crear la pantalla `frontend/src/privado/pagos/PantallaMensualidad.tsx` (valor del club,
      lista "Jugadores con valor propio", selección múltiple con fijar y quitar; solo PRESIDENTE edita) y
      el componente `frontend/src/privado/pagos/SelectorDeJugadoresConValor.tsx`
- [X] T034 [US2] Añadir la ruta de **Pagos → Mensualidad** y la entrada **Pagos** en
      `frontend/src/privado/MenuDelClub.tsx` (visible a PRESIDENTE y DIRECTIVO; nunca al ENTRENADOR; la
      cuenta de jugador la verá en la historia 3) y la ruta en el enrutador de `frontend/src/privado/`
- [X] T035 [US2] Añadir el aviso "Defina el valor de la mensualidad" al inicio del PRESIDENTE en
      `frontend/src/privado/inicio/AvisoValorMensualidad.tsx` y montarlo desde
      `frontend/src/privado/inicio/ResumenDelClub.tsx` (usa `valorClub` nulo; no es un recordatorio de
      cobro, §16.8)
- [X] T036 [US2] Actualizar `specs/015-mensualidades-cargos-pagos/contracts/api.yaml` si algo cambió al
      construir, y confirmar 116/90 con `EndpointsDeLaApi` y `AccesoClubPruebas`

**Punto de control**: historias 1 y 2 funcionan; el PRESIDENTE fija el valor y ve el aviso.

---

## Fase 5: Historia 3 - Consultar el estado de cuenta (Prioridad: P1)

**Objetivo**: la familia ve cargos, pagos, saldo y estado de su jugador; el PRESIDENTE y el DIRECTIVO ven
el de todos; el ENTRENADOR nada; la columna "Al día" deja de estar vacía.

**Prueba independiente**: dos mensualidades de 65.000 y un abono de 90.000: saldo 40.000, "pago parcial",
la familia ve el detalle y otra familia no ve nada de él.

**Contrato**: 116/90 → 117/91 (`GET /jugadores/{usuarioRolId}/cuenta`).

### Pruebas de la historia 3

- [X] T037 [P] [US3] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/EstadoDeCuentaPruebas.cs`: saldo y los tres estados (CE-005);
      mensualidad sin pagar después del día 5 sigue pendiente sin consecuencia; PRESIDENTE y DIRECTIVO
      ven cualquier jugador, también retirado; la familia solo el suyo (otro jugador, otra familia u otro
      club → `404`, incluso por identificador); jugador retirado sin acceso; ENTRENADOR `403` también
      para su categoría; DESARROLLADOR sin ruta; deber no cambia calendario, convocatorias ni
      entrenamientos (RF-015)
- [X] T038 [P] [US3] Escribir las pruebas de integración de la lista en
      `backend/pruebas/Integracion/Pagos/ListaConEstadoDeCuentaPruebas.cs`: `estadoCuenta` real para
      PRESIDENTE y DIRECTIVO con **una** consulta agrupada, nulo para el ENTRENADOR
- [X] T039 [P] [US3] Escribir las pruebas de pantalla en
      `frontend/pruebas/pagos/CuentaDelJugador.test.tsx` y `frontend/pruebas/pagos/ColumnaAlDia.test.tsx`
      (estado con texto y color, cargos y pagos, saldo, la entrada de menú del ENTRENADOR no existe y la
      columna "Al día" no se pinta para él)

### Implementación de la historia 3

- [X] T040 [P] [US3] Crear los DTOs `CuentaJugadorDto`, `CargoDto` y `PagoDto` en
      `backend/src/LFClub.Aplicacion/DTOs/CuentaJugadorDto.cs`, `CargoDto.cs` y `PagoDto.cs` (con
      `puedeRegistrar`, `puedeInformar`, `tieneSoporte`, `tieneResguardo` y los datos de cada paso)
- [X] T041 [US3] Añadir a `IRepositorioCargos` y `IRepositorioPagos` las consultas de cuenta (cargos con
      lo pagado por jugador, pagos del jugador con sus aplicaciones) e implementarlas en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioCargos.cs` y `RepositorioPagos.cs`
      más la consulta agrupada de estado por jugador del club
- [X] T042 [US3] Crear `IServicioCuentaDelJugador` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioCuentaDelJugador.cs` e implementarlo en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioCuentaDelJugador.cs` (usa
      `ReglaEstadoDeCuenta` y `ReglaAccesoACuenta`; 404 para lo ajeno; incluye medios activos cuando
      existan, historia 5)
- [X] T043 [US3] Crear `ControladorCuentaJugador` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorCuentaJugador.cs` con
      `GET /api/clubes/{clubId}/jugadores/{usuarioRolId}/cuenta`
      (`IntegranteDelClub(PRESIDENTE, DIRECTIVO, JUGADOR)`)
- [X] T044 [US3] Añadir `EstadoCuenta` (nulo permitido) a `JugadorDeListaDto` y rellenarlo en la
      implementación de `IServicioListaJugadores` solo para PRESIDENTE y DIRECTIVO con una consulta
      agrupada (research §16); sin cambiar la ruta de la 007
- [X] T045 [P] [US3] Crear el cliente y el gancho en `frontend/src/privado/pagos/apiCuenta.ts` y
      `frontend/src/privado/pagos/useCuentaJugador.ts`
- [X] T046 [US3] Crear la pantalla `frontend/src/privado/pagos/CuentaDelJugador.tsx` con
      `frontend/src/privado/pagos/ListaDeCargos.tsx`, `ListaDePagos.tsx` y `InsigniaDeEstado.tsx`
      (estado por texto además del color, saldo, cargos con tipo, mes, valor y pagado, pagos con medio y
      fecha); la familia llega desde su inicio (≤ 3 toques, CE-002) y el PRESIDENTE y el DIRECTIVO desde
      la lista de jugadores
- [X] T047 [US3] Añadir el acceso a la cuenta en `frontend/src/privado/inicio/TarjetaMiJugador.tsx`
      (familia) y en la fila de `frontend/src/privado/jugadores/TablaDeGrupo.tsx` (PRESIDENTE y
      DIRECTIVO), y la entrada **Pagos** del menú para la cuenta de jugador en
      `frontend/src/privado/MenuDelClub.tsx`
- [X] T048 [US3] Hacer que la columna "Al día" de `frontend/src/privado/jugadores/TablaDeGrupo.tsx` muestre
      `estadoCuenta` real ("Al día / Pago parcial / Debe") a PRESIDENTE y DIRECTIVO y no se pinte para el
      ENTRENADOR (RF-034); actualizar `frontend/src/privado/jugadores/textos.ts` y la documentación de
      `JugadorDeListaDto` (ya no es un espacio reservado)
- [X] T049 [US3] Actualizar `contracts/api.yaml` si cambió algo y confirmar 117/91 y el campo
      `estadoCuenta` de la lista; correr la batería de jugadores de la 007 (no regresión)

**Punto de control**: se consulta todo el estado de cuenta; la historia 4 permite crear pagos.

---

## Fase 6: Historia 4 - El PRESIDENTE registra un pago y pone al día al jugador (Prioridad: P1)

**Objetivo**: el PRESIDENTE registra efectivo o transferencia recibida; el pago queda confirmado y
aplicado, y el estado cambia en el acto con su recibo numerado.

**Prueba independiente**: registrar 65.000 en efectivo a un jugador con una mensualidad pendiente: queda
"al día", hay recibo y número, y el pago muestra quién lo registró y cuándo.

**Contrato**: 117/91 → 119/93 (`POST /jugadores/{usuarioRolId}/pagos`, `GET /pagos/{pagoId}`).

**Nota**: esta historia entrega el número de recibo y el recibo como datos; el resguardo (imagen) y la
pantalla del recibo llegan en la historia 8.

### Pruebas de la historia 4

- [X] T050 [P] [US4] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/RegistroDePagoPruebas.cs`: efectivo y transferencia con entidad
      y referencia; efectivo no pide entidad; queda confirmado, aplicado (más antiguo primero o los
      `cargoIds` elegidos) y el saldo baja; "al día" y "pago parcial"; valor mayor al pendiente →
      `409 pago_excede_deuda` con lo que debe; sin deuda → `409 sin_deuda`; cero, negativo y centavos →
      `400`; DIRECTIVO, ENTRENADOR (incluida su categoría) y cuenta de jugador → `403`; se puede registrar
      para un jugador retirado que debe; el pago guarda quién, cuándo, medio, entidad y cargos cubiertos
- [X] T051 [P] [US4] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/ConcurrenciaYReciboPruebas.cs`: dos confirmaciones simultáneas
      sobre el mismo jugador no cubren más de lo que vale el cargo; el recibo es consecutivo por club (1,
      2, 3) y dos clubes pueden repetir número
- [X] T052 [P] [US4] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/ConservacionDePagosPruebas.cs`: al eliminar la cuenta de un
      jugador el pago se conserva sin jugador con nombre completo y documento y sus cargos desaparecen y
      dejan de contar; al eliminar a un PRESIDENTE su nombre pasa a "Persona eliminada" en los pagos que
      registró; un jugador retirado conserva cargos y pagos; eliminar el club borra todo; nombre o
      documento que cambian después no alteran pagos anteriores
- [X] T053 [P] [US4] Escribir la prueba de pantalla en `frontend/pruebas/pagos/RegistrarPago.test.tsx`
      (medio, entidad solo si es transferencia, muestra el pendiente y rechaza más, cargos opcionales,
      botón visible solo con `puedeRegistrar`)

### Implementación de la historia 4

- [X] T054 [P] [US4] Crear los DTOs `RegistrarPagoDto` y `PagoDetalleDto` (recibo como datos) en
      `backend/src/LFClub.Aplicacion/DTOs/RegistrarPagoDto.cs` y `PagoDetalleDto.cs`
- [X] T055 [US4] Añadir a `IRepositorioPagos` el bloqueo del jugador (`SELECT … FOR UPDATE` sobre
      `UsuariosRol`), la siguiente numeración (`UPDATE "Clubes" SET "UltimoRecibo" = "UltimoRecibo" + 1 …
      RETURNING`) y la inserción de pago con aplicaciones; implementarlos en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioPagos.cs` (dividir en un segundo
      archivo si pasa de 250 líneas)
- [X] T056 [US4] Crear `IServicioRegistroDePagos` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioRegistroDePagos.cs` e implementarlo en
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioRegistroDePagos.cs`: una sola
      `EnTransaccionAsync` con bloqueo del jugador, `ReglaAplicacionDePago`, copia de nombre, documento y
      concepto, `NumeroRecibo`, `SaldoTrasPago`, `ConfirmadoPorNombre`; sin efectos fuera del club
- [X] T057 [US4] Crear `IServicioConsultaDePagos` e implementarlo en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioConsultaDePagos.cs` y
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConsultaDePagos.cs` (detalle y recibo de un
      pago con `ReglaAccesoACuenta`; 404 para lo ajeno)
- [X] T058 [US4] Crear `ControladorPagos` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorPagos.cs` con
      `POST /api/clubes/{clubId}/jugadores/{usuarioRolId}/pagos` (`IntegranteDelClub(PRESIDENTE)`) y
      `GET /api/clubes/{clubId}/pagos/{pagoId}` (`IntegranteDelClub(PRESIDENTE, DIRECTIVO, JUGADOR)`),
      con los códigos `pago_excede_deuda` y `sin_deuda` en `problem+json`
- [X] T059 [US4] Ajustar `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEliminacionCuenta.cs`
      para que `AnonimizarAutoriaAsync` escriba "Persona eliminada" en `CreadoPorNombre`,
      `ConfirmadoPorNombre`, `RechazadoPorNombre` y `AnuladoPorNombre` de los pagos del PRESIDENTE
      eliminado; confirmar con T052 y T010
- [X] T060 [P] [US4] Crear el cliente y la mutación en `frontend/src/privado/pagos/apiPagos.ts` y
      `frontend/src/privado/pagos/useRegistrarPago.ts`
- [X] T061 [US4] Crear `frontend/src/privado/pagos/RegistrarPago.tsx` y
      `frontend/src/privado/pagos/SelectorDeCargos.tsx`, y el botón **Registrar pago** en
      `frontend/src/privado/pagos/CuentaDelJugador.tsx` (solo PRESIDENTE; mensaje claro con lo que debe
      cuando se excede; al guardar, la cuenta se refresca)
- [X] T062 [US4] Actualizar `contracts/api.yaml` si cambió algo y confirmar 119/93; correr la batería de
      pagos y de eliminación de cuenta de la 014 (no regresión)

**Punto de control**: el club ya puede cobrar y llevar sus cuentas dentro de la aplicación (MVP completo).

---

## Fase 7: Historia 5 - El PRESIDENTE publica dónde se le paga (Prioridad: P2)

**Objetivo**: el PRESIDENTE publica los medios de pago del club; las familias los ven en la cuenta de su
jugador.

**Prueba independiente**: se agrega un Nequi y una cuenta Bancolombia; la familia los ve y otro club no.

**Contrato**: 119/93 → 123/97 (`GET`, `POST`, `PUT`, `DELETE` de `/medios-de-pago`). Va después de las
historias 3 y 4 porque el cobro funciona sin ella.

### Pruebas de la historia 5

- [X] T063 [P] [US5] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/MediosDePagoPruebas.cs`: crear, editar, desactivar y quitar; la
      familia solo ve los activos; PRESIDENTE y DIRECTIVO ven todos; DIRECTIVO, ENTRENADOR y jugador no
      cambian nada (403); otro club ve `404`; quitar un medio no cambia los pagos hechos con esa entidad
- [X] T064 [P] [US5] Escribir las pruebas de pantalla en `frontend/pruebas/pagos/MediosDePago.test.tsx`
      (lista, alta, edición, desactivar; "Cómo pagar" en la cuenta; mensaje "consulte al PRESIDENTE" si no
      hay medios)

### Implementación de la historia 5

- [X] T065 [P] [US5] Crear los DTOs `MedioPagoDto` y `GuardarMedioPagoDto` en
      `backend/src/LFClub.Aplicacion/DTOs/MedioPagoDto.cs` y `GuardarMedioPagoDto.cs` (entidad hasta 40,
      titular hasta 100, número hasta 60, nota hasta 200, todos obligatorios salvo la nota)
- [X] T066 [US5] Implementar `IRepositorioMediosPago` en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioMediosPago.cs`; crear
      `IServicioMediosPago` e implementarlo en `backend/src/LFClub.Aplicacion/Servicios/IServicioMediosPago.cs`
      y `backend/src/LFClub.Aplicacion/Implementaciones/ServicioMediosPago.cs`
- [X] T067 [US5] Crear `ControladorMediosPago` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorMediosPago.cs` con
      `GET`/`POST /api/clubes/{clubId}/medios-de-pago` y `PUT`/`DELETE
      /api/clubes/{clubId}/medios-de-pago/{medioId}`; lectura para PRESIDENTE, DIRECTIVO y JUGADOR (este
      solo activos), escritura solo PRESIDENTE; incluir los medios activos en `CuentaJugadorDto` en
      `ServicioCuentaDelJugador.cs`
- [X] T068 [P] [US5] Crear el cliente y el gancho en `frontend/src/privado/pagos/apiMediosPago.ts` y
      `useMediosPago.ts`, y las pantallas `frontend/src/privado/pagos/PantallaMediosDePago.tsx` y
      `FormularioMedioDePago.tsx` (PRESIDENTE), con la entrada **Pagos → Medios de pago** en
      `frontend/src/privado/MenuDelClub.tsx`
- [X] T069 [US5] Mostrar "Cómo pagar" en `frontend/src/privado/pagos/CuentaDelJugador.tsx` con
      `frontend/src/privado/pagos/ComoPagar.tsx` (medios activos, indicación de enviar el soporte después
      de pagar, mensaje de consultar al PRESIDENTE si no hay medios, RF-028)
- [X] T070 [US5] Actualizar `contracts/api.yaml` si cambió algo y confirmar 123/97

**Punto de control**: las familias saben a dónde transferir.

---

## Fase 8: Historia 6 - La familia informa una transferencia con su soporte (Prioridad: P2)

**Objetivo**: la familia informa una transferencia con imagen; queda en revisión sin tocar saldos; el
PRESIDENTE confirma o rechaza.

**Prueba independiente**: informar 65.000 con imagen: el saldo no cambia; al confirmarla, el jugador queda
"al día" y la familia ve su resguardo; otra se rechaza con motivo visible.

**Contrato**: 123/97 → 128/102 (`POST …/pagos/informados`, `GET /pagos/{pagoId}/soporte`,
`GET /pagos/en-revision`, `POST /pagos/{pagoId}/confirmacion`, `POST /pagos/{pagoId}/rechazo`).

### Pruebas de la historia 6

- [X] T071 [P] [US6] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/InformarTransferenciaPruebas.cs`: queda `EN_REVISION` sin
      aplicaciones ni recibo y sin cambiar saldo, estado de cuenta ni recaudo; varios en revisión a la vez
      (clarificación 4); sin imagen `soporte_requerido`, PDF `soporte_invalido`, más de 5 MB
      `soporte_demasiado_grande` y no queda nada registrado; solo sobre el propio jugador (otro →
      `404`); valor mayor al pendiente → `409`; ENTRENADOR, DIRECTIVO y PRESIDENTE no usan esta operación
- [X] T072 [P] [US6] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/RevisionDePagosPruebas.cs`: lista en revisión del más antiguo al
      más reciente con `total` y cargos pendientes; confirmar (aplica solo lo pendiente, corrige el valor
      y guarda `ValorInformado`, genera recibo); rechazar con motivo obligatorio y visible a la familia;
      un rechazado no se reabre (`pago_no_esta_en_revision`) y la familia informa uno nuevo; confirmar
      lo ya pagado por otra vía deja aplicar solo lo pendiente; DIRECTIVO y ENTRENADOR no confirman ni
      rechazan (403)
- [X] T073 [P] [US6] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/SoporteDePagoPruebas.cs`: solo la familia del jugador, el
      PRESIDENTE y el DIRECTIVO abren la imagen; otros `404` también por dirección directa; cabeceras
      `Cache-Control: private, no-store` y `X-Content-Type-Options: nosniff`; sin dirección pública; el
      soporte se borra al eliminar la cuenta del jugador y con el club (hace pasar el guardián de T010)
- [X] T074 [P] [US6] Escribir las pruebas de pantalla en
      `frontend/pruebas/pagos/InformarTransferencia.test.tsx` y `RevisionDePagos.test.tsx` (sin imagen no
      envía, muestra el pendiente, el rechazo con su motivo se ve en la cuenta, confirmar con valor
      corregible, rechazar exige motivo, el contador en el inicio)

### Implementación de la historia 6

- [X] T075 [P] [US6] Crear los DTOs `InformarPagoDto`, `ConfirmarPagoDto`, `MotivoDto`,
      `PagoEnRevisionDto` y `PagosEnRevisionDto` en `backend/src/LFClub.Aplicacion/DTOs/`
- [X] T076 [US6] Añadir a `IRepositorioPagos` e implementar en `RepositorioPagos.cs` (o en un archivo
      aparte `RepositorioPagosRevision.cs`) la inserción del pago informado con su `SoportePago`, la lista
      en revisión y la lectura del soporte
- [X] T077 [US6] Crear `IServicioRevisionDePagos` e implementarlo en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioRevisionDePagos.cs` y
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioRevisionDePagos.cs` (informar con
      `ValidadorSoportePago`, confirmar en una transacción con el bloqueo del jugador reutilizando la
      lógica de T056, rechazar con motivo; `ReglaTransicionPago`); extraer lo compartido con
      `ServicioRegistroDePagos` a una clase pequeña para no repetir
- [X] T078 [US6] Añadir a `ControladorPagos` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorPagos.cs` (o dividir en
      `ControladorRevisionDePagos.cs`) las operaciones `POST …/pagos/informados` (`multipart/form-data`,
      `IntegranteDelClub(JUGADOR)`, límite de petición de `ArchivoCargado.LimiteDePeticion` más margen),
      `GET /pagos/{pagoId}/soporte` (con las cabeceras de seguridad), `GET /pagos/en-revision`
      (PRESIDENTE y DIRECTIVO), `POST /pagos/{pagoId}/confirmacion` y `POST /pagos/{pagoId}/rechazo`
      (solo PRESIDENTE)
- [X] T079 [US6] Borrar los soportes de los pagos del jugador eliminado en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEliminacionCuenta.cs` (research §13) y
      comprobar con T010 y T073
- [X] T080 [P] [US6] Crear el cliente y los ganchos en `frontend/src/privado/pagos/apiRevision.ts`,
      `useInformarPago.ts` y `usePagosEnRevision.ts`
- [X] T081 [US6] Crear `frontend/src/privado/pagos/InformarTransferencia.tsx` (entidad de la lista del
      club u otra, valor, fecha, referencia, imagen por cámara o galería, muestra el pendiente) con el
      botón "Informé una transferencia" en `CuentaDelJugador.tsx` (solo familia, `puedeInformar`) y el
      estado "En revisión" y el rechazo con su motivo en `ListaDePagos.tsx`
- [X] T082 [US6] Crear `frontend/src/privado/pagos/PagosEnRevision.tsx`,
      `frontend/src/privado/pagos/TarjetaPagoEnRevision.tsx` y `frontend/src/privado/pagos/ConfirmarPago.tsx`
      con el soporte a un toque, confirmar (valor y cargos editables) y rechazar con motivo (solo
      PRESIDENTE; el DIRECTIVO solo consulta), y la entrada **Pagos → En revisión** en
      `frontend/src/privado/MenuDelClub.tsx`
- [X] T083 [US6] Añadir la tarjeta de pagos en revisión (contador `total`, solo PRESIDENTE) en
      `frontend/src/privado/inicio/TarjetaPagosEnRevision.tsx` y montarla desde
      `frontend/src/privado/inicio/ResumenDelClub.tsx` (contador interno, no recordatorio de cobro, §16.8)
- [X] T084 [US6] Actualizar `contracts/api.yaml` si cambió algo y confirmar 128/102

**Punto de control**: el soporte ahorra al PRESIDENTE verificar por fuera; el cobro directo sigue
funcionando sin él.

---

## Fase 9: Historia 7 - Anular un pago equivocado (Prioridad: P2)

**Objetivo**: un pago confirmado no se edita ni se borra; se anula con autor, fecha y motivo.

**Prueba independiente**: anular 65.000: el pago sigue visible como anulado, el saldo vuelve a subir y no
hay forma de editarlo ni borrarlo.

**Contrato**: 128/102 → 129/103 (`POST /pagos/{pagoId}/anulacion`).

### Pruebas de la historia 7

- [X] T085 [P] [US7] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/AnulacionDePagoPruebas.cs`: anular con motivo (autor, fecha,
      motivo), saldo recalculado, el pago sigue en historial y estado de cuenta marcado como anulado; sin
      motivo se rechaza; anular dos veces se rechaza (`pago_no_confirmado`); anular uno en revisión o
      rechazado se rechaza; ENTRENADOR y DIRECTIVO `403`; no existe ruta para editar ni borrar un pago
      confirmado (`PUT`/`PATCH`/`DELETE` → 404 o 405); un pago anulado de una transferencia conserva su
      soporte; la anulación no cuenta en el recaudo
- [X] T086 [P] [US7] Escribir la prueba de pantalla en `frontend/pruebas/pagos/AnularPago.test.tsx`
      (motivo obligatorio, solo PRESIDENTE ve el botón, el pago queda marcado como anulado)

### Implementación de la historia 7

- [X] T087 [US7] Crear `IServicioAnulacionDePagos` e implementarlo en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioAnulacionDePagos.cs` y
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAnulacionDePagos.cs` (transacción con el
      bloqueo del jugador, `ReglaTransicionPago`, motivo con `ValidadorPago`, `AnuladoPor*` con nombre
      copiado; las aplicaciones se conservan y el cálculo las ignora)
- [X] T088 [US7] Añadir `POST /api/clubes/{clubId}/pagos/{pagoId}/anulacion` (`IntegranteDelClub(PRESIDENTE)`)
      en `backend/src/LFClub.Api/Controladores/Club/ControladorPagos.cs` (o en
      `ControladorRevisionDePagos.cs` si se dividió)
- [X] T089 [US7] Crear `frontend/src/privado/pagos/AnularPago.tsx` y añadir el botón "Anular" en
      `frontend/src/privado/pagos/DetalleDePago.tsx` (solo PRESIDENTE, diálogo con motivo obligatorio,
      marca de anulado con quién, cuándo y por qué)
- [X] T090 [US7] Actualizar `contracts/api.yaml` si cambió algo y confirmar 129/103

**Punto de control**: el control del dinero queda protegido.

---

## Fase 10: Historia 8 - Recibo y resguardo de pago (Prioridad: P2)

**Objetivo**: cada pago confirmado tiene su recibo numerado y su resguardo en imagen, que la familia puede
descargar o compartir.

**Prueba independiente**: confirmar un pago: la cuenta muestra el recibo y el resguardo con el mismo
número, y el resguardo se descarga como imagen.

**Contrato**: 129/103 → 130/104 (`GET /pagos/{pagoId}/resguardo`).

### Pruebas de la historia 8

- [X] T091 [P] [US8] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/ReciboYResguardoPruebas.cs`: el recibo trae club, jugador, valor,
      medio y entidad, fecha, quién confirmó, cargos cubiertos, `saldoTrasPago` y número; el resguardo es
      un PNG de 1080 × 1350 con el mismo número, sin documento; pagos en revisión o rechazados no tienen
      recibo ni resguardo (`404`); pago anulado conserva ambos y el resguardo muestra ANULADO; otra
      familia u otro club `404`; PRESIDENTE y DIRECTIVO los abren, ENTRENADOR `403`; tras eliminar la
      cuenta el recibo se conserva y el resguardo deja de existir; cabeceras `Cache-Control: private,
      no-store` y `nosniff`
- [X] T092 [P] [US8] Escribir las pruebas unitarias del plano en
      `backend/pruebas/Unitarias/Imagenes/PlanoResguardoPruebas.cs` (medidas, capas, contraste del texto
      sobre los colores del club)
- [X] T093 [P] [US8] Escribir las pruebas de pantalla en `frontend/pruebas/pagos/ReciboYResguardo.test.tsx`
      (recibo visible, resguardo con "Descargar" y "Compartir" cuando el navegador lo permite, sello
      ANULADO)

### Implementación de la historia 8

- [X] T094 [P] [US8] Crear `IGeneradorImagenResguardo` en
      `backend/src/LFClub.Aplicacion/Interfaces/IGeneradorImagenResguardo.cs`
- [X] T095 [US8] Crear `PlanoResguardo` en
      `backend/src/LFClub.Infraestructura/Imagenes/PlanoResguardo.cs` (medidas y capas, 1080 × 1350) y
      `GeneradorImagenResguardo` en `backend/src/LFClub.Infraestructura/Imagenes/GeneradorImagenResguardo.cs`
      con SkiaSharp, siguiendo `GeneradorImagenCarta` (escudo, colores y Barlow Condensed del club,
      `ContrasteDeColor`): club, jugador, valor, medio y entidad, fecha, concepto, quién confirmó,
      "Al día" o saldo pendiente según `SaldoTrasPago`, número, y sello ANULADO en diagonal si el pago
      se anuló; sin documento del jugador; dibujado a demanda y no guardado
- [X] T096 [US8] Añadir `GET /api/clubes/{clubId}/pagos/{pagoId}/resguardo`
      (`IntegranteDelClub(PRESIDENTE, DIRECTIVO, JUGADOR)`) en
      `backend/src/LFClub.Api/Controladores/Club/ControladorPagos.cs` con las cabeceras de seguridad, y
      el método de consulta del resguardo en `ServicioConsultaDePagos.cs` (solo mientras el pago tenga
      jugador y estado `CONFIRMADO` o `ANULADO`)
- [X] T097 [US8] Registrar el generador en la composición de dependencias de `backend/src/LFClub.Api/`
- [X] T098 [P] [US8] Crear `frontend/src/privado/pagos/DetalleDePago.tsx` (recibo como pantalla, con
      estado y datos de cada paso), `frontend/src/privado/pagos/ResguardoDePago.tsx` (imagen, "Descargar" y
      "Compartir" con Web Share con archivo o `Share` de Capacitor en Android; sin enlaces públicos) y
      `frontend/src/privado/pagos/compartirResguardo.ts`
- [X] T099 [US8] Enlazar recibo y resguardo desde cada pago confirmado o anulado de
      `frontend/src/privado/pagos/ListaDePagos.tsx`
- [X] T100 [US8] Actualizar `contracts/api.yaml` si cambió algo y confirmar 130/104

**Punto de control**: ambas constancias existen y se conservan por separado.

---

## Fase 11: Historia 9 - Finanzas generales del club (Prioridad: P3)

**Objetivo**: el PRESIDENTE y el DIRECTIVO ven recaudo del mes, cartera y totales por categoría.

**Prueba independiente**: con pagos de dos categorías en el mes, el recaudo, la cartera y los totales por
categoría coinciden con la suma de pagos y cargos de cada una.

**Contrato**: 130/104 → **131/105** (`GET /finanzas`).

### Pruebas de la historia 9

- [X] T101 [P] [US9] Escribir las pruebas de integración en
      `backend/pruebas/Integracion/Pagos/FinanzasPruebas.cs`: recaudo del mes (solo confirmados no
      anulados cuyo `ConfirmadoEn` cae en el mes de Colombia), no cuenta pagos en revisión ni anulados;
      cartera incluye jugadores retirados que deben y no cuenta cargos de cuentas eliminadas; totales por
      categoría con la categoría del cargo (un cambio de categoría no reordena hacia atrás); lista de los
      que deben; ENTRENADOR y cuenta de jugador `403`; DESARROLLADOR sin ruta; ningún dato de otro club
      (CE-009); el DIRECTIVO solo consulta
- [X] T102 [P] [US9] Escribir la prueba de pantalla en `frontend/pruebas/pagos/Finanzas.test.tsx`
      (recaudo, cartera, totales por categoría, lista de quienes deben, sin entrada para ENTRENADOR)

### Implementación de la historia 9

- [X] T103 [P] [US9] Crear los DTOs `FinanzasDto`, `TotalPorCategoriaDto` y `JugadorQueDebeDto` en
      `backend/src/LFClub.Aplicacion/DTOs/FinanzasDto.cs`, `TotalPorCategoriaDto.cs` y
      `JugadorQueDebeDto.cs`
- [X] T104 [US9] Implementar `IRepositorioFinanzas` en
      `backend/src/LFClub.Infraestructura/Repositorios/RepositorioFinanzas.cs` con tres consultas
      agrupadas (recaudo del mes por categoría del cargo, cartera, quién debe; research §15), sin tabla de
      totales; crear `IServicioFinanzas` e implementarlo en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioFinanzas.cs` y
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioFinanzas.cs`
- [X] T105 [US9] Crear `ControladorFinanzas` en
      `backend/src/LFClub.Api/Controladores/Club/ControladorFinanzas.cs` con
      `GET /api/clubes/{clubId}/finanzas` (`IntegranteDelClub(PRESIDENTE, DIRECTIVO)`)
- [X] T106 [P] [US9] Crear `frontend/src/privado/pagos/apiFinanzas.ts`, `useFinanzas.ts`,
      `frontend/src/privado/pagos/PantallaFinanzas.tsx` y `frontend/src/privado/pagos/TablaPorCategoria.tsx`,
      y la entrada **Pagos → Finanzas** en `frontend/src/privado/MenuDelClub.tsx` (PRESIDENTE y DIRECTIVO)
- [X] T107 [US9] Actualizar `contracts/api.yaml` si cambió algo y confirmar 131/105 con
      `EndpointsDeLaApi` y `AccesoClubPruebas`

**Punto de control**: las nueve historias funcionan.

---

## Fase 12: Cierre y aspectos transversales

**Propósito**: validación de extremo a extremo y no regresión.

- [X] T108 [P] Escribir la prueba de aislamiento entre clubes en
      `backend/pruebas/Integracion/Aislamiento/AislamientoDePagosPruebas.cs`: ningún cargo, pago, medio,
      soporte, recibo, resguardo ni total de un club aparece en consultas de otro (CE-009, RF-036), y el
      DESARROLLADOR no tiene ruta (RF-033)
- [X] T109 [P] Escribir la prueba de "ENTRENADOR fuera de todo" en
      `backend/pruebas/Integracion/Pagos/EntrenadorSinAccesoPruebas.cs`: las 18 operaciones nuevas
      responden `403 rol_no_autorizado` (incluida la categoría que tiene asignada), el campo `estadoCuenta` va
      nulo en la lista y ninguna respuesta lleva datos financieros (CE-007)
- [X] T110 [P] Revisar la documentación XML y los comentarios de componente de todo lo nuevo (§3) y
      actualizar `README` o las notas del repositorio si describen módulos (sin crear documentos nuevos)
- [X] T111 Ejecutar `scripts/verificar-tamano.ps1` y dividir cualquier archivo escrito a mano por encima
      de 250 líneas (§2.3)
- [X] T112 Ejecutar `dotnet test backend/pruebas/Unitarias` y
      `dotnet test backend/pruebas/Integracion --filter "FullyQualifiedName~Pagos|FullyQualifiedName~Cuenta.Esquema"`
      y la batería completa de integración (unos 12 minutos); confirmar que el contrato es 131/105
- [X] T113 Ejecutar `npm --prefix frontend test -- pagos`, `npm --prefix frontend run lint` y
      `npm --prefix frontend run build`
- [ ] T114 Recorrer `specs/015-mensualidades-cargos-pagos/quickstart.md` de la A a la H contra la
      plataforma levantada en Compose y anotar lo que no coincida; revisar con el propietario el diseño
      de las pantallas y del resguardo (nota 5 del plan, research §14)
- [X] T115 Marcar como hechas las tareas cuyos criterios de §27.2 se verificaron, y actualizar el estado
      de `specs/015-mensualidades-cargos-pagos/spec.md` y de las specs 002 a 007 y 009 que dejaron esta
      funcionalidad pendiente (solo la referencia, sin reescribirlas)

---

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación (Fase 1)**: sin dependencias.
- **Base (Fase 2)**: depende de la Preparación y **bloquea todas las historias**.
- **Historias (Fases 3 a 11)**: todas dependen de la Base. En orden de prioridad: US1, US2, US3, US4
  (P1), US5, US6, US7, US8 (P2) y US9 (P3).
- **Cierre (Fase 12)**: depende de las historias que se vayan a entregar.

### Dependencias entre historias

- **US1 (P1)**: tras la Base; no depende de otra historia. Sin pantalla propia: se ve en la cuenta de US3.
- **US2 (P1)**: tras la Base; su `PUT` invoca el servicio de generación de US1 (T023), así que va después.
- **US3 (P1)**: tras la Base; lee cargos de US1 y pagos de US4, pero se prueba con datos sembrados.
- **US4 (P1)**: tras la Base y T041 (consultas de la cuenta de US3); crea el pago, el recibo numerado y
  el reparto que reutilizan US6 y US7.
- **US5 (P2)**: tras la Base; añade los medios a la cuenta de US3 (T067, T069).
- **US6 (P2)**: depende de US4 (la confirmación reutiliza el registro, T077) y de US3 (la pantalla de la
  cuenta).
- **US7 (P2)**: depende de US4 (anula pagos confirmados).
- **US8 (P2)**: depende de US4 (número de recibo y `SaldoTrasPago`) y de US7 para el sello ANULADO.
- **US9 (P3)**: depende de US4 y US7 para que el recaudo cuente solo lo confirmado no anulado.

### Dentro de cada historia

- Pruebas primero: se escriben y fallan antes de la implementación.
- Reglas y DTOs, luego repositorio, luego servicio, luego controlador y por último pantalla.
- Backend y pantalla en la misma historia (§27.1); la historia termina cuando compila, pasan sus pruebas y
  el contrato coincide.

### Oportunidades de trabajo en paralelo

- Base: T004, T005 y T006 en paralelo; T011 a T016 (una regla por archivo) en paralelo; T017 a T019 en
  paralelo.
- Cada historia: todas sus pruebas `[P]` en paralelo y los DTO `[P]` en paralelo.
- Con varias personas, tras la Base: A en US1 y US2, B en US3 y US4, C en US5 (medios) y D en lo visual del
  resguardo (T094, T095, T092) mientras US4 termina.

### Ejemplo en paralelo: Base

```text
Tarea: "Crear ReglaMesDeCobro y su prueba en backend/src/LFClub.Dominio/Reglas/ReglaMesDeCobro.cs"
Tarea: "Crear ReglaEstadoDeCuenta y su prueba en backend/src/LFClub.Dominio/Reglas/ReglaEstadoDeCuenta.cs"
Tarea: "Crear ReglaAplicacionDePago y su prueba en backend/src/LFClub.Dominio/Reglas/ReglaAplicacionDePago.cs"
Tarea: "Crear ReglaAccesoACuenta y su prueba en backend/src/LFClub.Dominio/Reglas/ReglaAccesoACuenta.cs"
Tarea: "Crear ReglaTransicionPago y su prueba en backend/src/LFClub.Dominio/Reglas/ReglaTransicionPago.cs"
```

### Ejemplo en paralelo: historia 4

```text
Tarea: "Pruebas de registro en backend/pruebas/Integracion/Pagos/RegistroDePagoPruebas.cs"
Tarea: "Pruebas de concurrencia y recibo en backend/pruebas/Integracion/Pagos/ConcurrenciaYReciboPruebas.cs"
Tarea: "Pruebas de conservación en backend/pruebas/Integracion/Pagos/ConservacionDePagosPruebas.cs"
Tarea: "Prueba de pantalla en frontend/pruebas/pagos/RegistrarPago.test.tsx"
```

---

## Estrategia de implementación

### Primero el MVP (historias 1 a 4)

1. Fase 1: Preparación.
2. Fase 2: Base (bloquea todo).
3. Historias 1, 2, 3 y 4 (todas P1): los cargos nacen, el PRESIDENTE fija el valor, la familia consulta y
   el PRESIDENTE registra pagos. Con esto el club ya cobra y lleva sus cuentas.
4. **Detenerse y validar** con los escenarios A a C y G del quickstart.
5. Entregar o mostrar al propietario.

### Entrega incremental

1. Base lista → historias 1 a 4 (MVP) → validar.
2. Añadir la historia 5 (medios de pago) → validar el escenario D, primera parte.
3. Añadir la historia 6 (transferencias con soporte) → validar el escenario D.
4. Añadir las historias 7 y 8 (anulación, recibo y resguardo) → validar los escenarios E y F.
5. Añadir la historia 9 (finanzas) → validar el escenario G, punto 5, y el H.
6. Cada historia suma valor sin romper las anteriores.

### Estrategia con varias personas

1. El equipo completa Preparación y Base junto.
2. Después: A en US1 y US2, B en US3 y US4, C en US5 y US6 en cuanto US4 tenga su confirmación.
3. Las historias se integran sin chocar: comparten la entidad y las reglas de la Base y solo se tocan en
   `ControladorPagos`, `CuentaDelJugador.tsx` y `MenuDelClub.tsx`, que se dividen si crecen.

---

## Notas

- `[P]` = archivos distintos, sin dependencias.
- `[US#]` ata la tarea a una historia de spec.md para trazabilidad.
- Cada historia debe poder completarse y probarse por separado.
- Verificar que las pruebas fallan antes de implementar.
- Hacer una confirmación (commit) por tarea o por grupo lógico.
- Detenerse en cualquier punto de control para validar la historia.
- Evitar tareas vagas, conflictos en un mismo archivo y dependencias entre historias que rompan su
  independencia.
- **Nota 1 del plan (decisión del propietario, sin tarea)**: los meses que pasan sin valor o sin API no se
  recuperan; no hay cargos retroactivos. Si se quiere recuperarlos, es una decisión nueva.
- **Nota 6 del plan**: la familia no elige cargos al informar; solo el PRESIDENTE los elige al confirmar
  o al registrar.
