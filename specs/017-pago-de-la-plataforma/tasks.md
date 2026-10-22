---

description: "Lista de tareas de la funcionalidad 017: pago del club por el uso de la plataforma"
---

# Tareas: Pago del club por el uso de la plataforma

**Entrada**: documentos de diseño en `specs/017-pago-de-la-plataforma/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
quickstart.md

**Punto de partida**: constitución 4.16.0 (§7.4, §13 y §28 ya enmendadas), la spec 015 construida y
la 016 construida (contrato y club con las operaciones que haya; a confirmar en T001).

**Pruebas**: incluidas. Las exigen la constitución (§20) y el plan (research §13). Por §27.1, cada
tarea de una historia entrega el backend, la pantalla que lo usa contra la API real y sus pruebas; las
tareas que solo traen pruebas, solo reglas puras o solo pantalla lo dicen.

**Organización**: tareas agrupadas por historia de usuario de spec.md. Orden de las fases: preparación,
base, historia 4 (generación y tarifas), historia 1 (el PRESIDENTE ve y paga), historia 2 (la
notificación confirma), historia 3 (suspensión y reactivación), historia 6 (secretos), historia 5
(panel del DESARROLLADOR) y cierre. Las tarifas y el mes de inicio del cobro (parte de la historia 5)
se construyen en la historia 4, porque sin ellos no hay cobro que generar.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US6]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 016, que siguen vigentes. Resumen y lo que se añade:

- **Esquema**: una sola migración, `PagoDeLaPlataforma` (T012), con exactamente lo de `data-model.md`.
  Si una tarea parece necesitar otra columna, tabla o índice, se detiene y se informa (§25).
- **Contrato**: `specs/017-pago-de-la-plataforma/contracts/api.yaml` manda en ruta, cuerpo, estados y
  códigos. **11 operaciones nuevas** (T001 confirma el punto de partida con la expresión regular de
  `EndpointsDeLaApi`).
- **Lo anterior no cambia**: ninguna operación existente cambia de contrato salvo `ClubResumenDto`
  (campos añadidos, T065). Si una prueba de la 001 a la 016 falla y no está claro que sea una
  regresión, se detiene y se pregunta (§27.2).
- **Las reglas son del servidor** (§15): el cálculo del total, el vencimiento, la suspensión y la
  confirmación del pago se deciden solo en `ReglaCobroPlataforma`, `ReglaSuspensionPorImpago` y los
  servicios. El frontend no calcula fechas ni totales: pinta lo que recibe.
- **El retorno del navegador no es prueba de pago** (§7.4): ningún código cambia un cobro a `PAGADO`
  salvo `ServicioNotificacionPasarela` (notificación verificada) y la marca manual del DESARROLLADOR.
- **Secretos** (§7.4, RF-025, RF-026): ninguna llave, secreto ni valor de prueba se escribe en un
  archivo del repositorio, en las pruebas, en la documentación ni en el registro de eventos. Las
  pruebas usan secretos inventados que generan ellas mismas (por ejemplo `"secreto-de-prueba"`).
- **Dinero** (§16.2): siempre `decimal` en C#; pesos enteros (`numeric(18,0)`). El frontend usa
  `formatoMoneda` de `frontend/src/privado/pagos/formatoMoneda.ts`. Para la pasarela, los valores van
  en centavos (pesos × 100).
- **Fechas** (RF-029): el mes y el día de Colombia salen de `ReglaMesDeCobro`; nunca de la hora del
  aparato ni de `DateTime.Now`.
- **Autorización en el servidor** (§15, RF-021): las de club solo para `Rol.PRESIDENTE` con
  `[IntegranteDelClub(Rol.PRESIDENTE)]` (también con el club suspendido); lo de otro club responde
  `404 no_encontrado`; los demás roles, `403 rol_no_autorizado`. Las de plataforma con
  `[SoloDesarrollador]`. El webhook es anónimo (`FiltroOperacionAnonima`).
- **Nombres** (§2.1, §2.2): `CobroPlataforma`, `PagoPlataforma`, `IntentoPagoPlataforma`,
  `NotificacionPasarela`, `ConfiguracionPlataforma`, `PasarelaWompi`, `ReglaCobroPlataforma`,
  `ReglaSuspensionPorImpago`, "impago", "gracia", "mes de inicio del cobro".
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en el
  frontend) con qué representa, su responsabilidad y qué no hace.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas; medir con
  `scripts/verificar-tamano.ps1` al empezar (T001) y al cerrar (T074).
- **Bash** (memoria del proyecto): los archivos nuevos se crean con la herramienta Write, no con
  heredocs; un `testhost` colgado se mata con `Get-Process testhost | Stop-Process -Force` antes de
  compilar; la batería de integración tarda unos 12 minutos.
- **Reloj y pasarela en pruebas**: las pruebas de integración sustituyen `IReloj` (para fijar el día)
  y `IPasarelaDePago` (por una pasarela falsa con secretos de prueba) y apagan el trabajo con
  `Plataforma:TrabajoActivo=false`.

## Fase 1: Preparación

- [X] T001 Confirmar el punto de partida: contar las operaciones con la expresión regular de `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs` (anotar el recuento de contrato y de club en `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` y el de plataforma en `AccesoPlataformaPruebas.cs`) y medir tamaños con `scripts/verificar-tamano.ps1`; si algo no coincide con lo que dice `specs/016-aviso-de-mora/contracts/api.yaml`, detenerse y avisar
- [X] T002 [P] Verificar que la rama es `017-pago-de-la-plataforma`, que `backend` compila y que pasan las pruebas de `ReglaMesDeCobro` y de estado de club en `backend/pruebas/Unitarias/Reglas/` y `backend/pruebas/Integracion/Plataforma/`
- [X] T003 [P] Añadir al final del `README.md` de la raíz una sección «Pago de la plataforma (Wompi)» que liste las cuatro claves de configuración (`Wompi__LlavePublica`, `Wompi__SecretoIntegridad`, `Wompi__SecretoEventos`, `Wompi__UrlCheckout`), diga que se fijan como variables de entorno y que la dirección de eventos que se registra en Wompi es `<dirección de la API>/api/plataforma/pagos/notificaciones`; sin ningún valor real

## Fase 2: Base (bloquea a todas las historias)

**Propósito**: las entidades, la migración, las reglas puras, la pasarela y los repositorios que usan
todas las historias.

### Dominio

- [X] T004 [P] Crear las enumeraciones en `backend/src/LFClub.Dominio/Enumeraciones/`: `MotivoSuspension.cs` (`MANUAL`, `IMPAGO`), `EstadoCobroPlataforma.cs` (`PENDIENTE`, `PAGADO`, `ANULADO`), `OrigenPagoPlataforma.cs` (`PASARELA`, `MANUAL`), `ResultadoNotificacion.cs` (`APLICADA`, `REPETIDA`, `NO_APROBADA`, `PAGO_DUPLICADO`, `PAGO_A_COBRO_ANULADO`, `VALOR_NO_COINCIDE`, `REFERENCIA_DESCONOCIDA`, `FIRMA_INVALIDA`) y `EstadoDePagoClub.cs` (`SIN_COBRO`, `AL_DIA`, `PENDIENTE`, `VENCIDO`, `SUSPENDIDO_POR_IMPAGO`), con documentación XML; verificar cómo se guardan y serializan las demás enumeraciones (como texto, `HasConversion<string>()`) y hacer igual
- [X] T005 [P] Añadir a `backend/src/LFClub.Dominio/Entidades/Club.cs` las propiedades `MesInicioCobro` (`DateOnly?`: «Desde qué mes se cobra al club. Nulo = no se cobra») y `MotivoSuspension` (`MotivoSuspension?`: «Solo tiene valor si `Estado = SUSPENDIDO`»), con documentación XML
- [X] T006 [P] Crear la entidad `ConfiguracionPlataforma` en `backend/src/LFClub.Dominio/Entidades/ConfiguracionPlataforma.cs`: `Id` (int, siempre 1), `PrecioJugador`, `PrecioEntrenador`, `PrecioDirectivo`, `PrecioPresidente` (`decimal?`, ≥ 0), `DiasDeGracia` (int, 0 a 30; por defecto 5), `ActualizadoPorUsuarioId` (`Guid?`) y `ActualizadoEn` (`DateTime`, UTC); no implementa `IPerteneceAClub`
- [X] T007 [P] Crear la entidad `CobroPlataforma` en `backend/src/LFClub.Dominio/Entidades/CobroPlataforma.cs` con las columnas de data-model.md: `Id`, `ClubId`, `Periodo` (`DateOnly`, primer día del mes), `Estado`, `Jugadores`, `Entrenadores`, `Directivos`, `Presidentes` (int), `PrecioJugador`, `PrecioEntrenador`, `PrecioDirectivo`, `PrecioPresidente` (decimal; los nulos como 0), `Total` (decimal, siempre > 0), `GeneradoEn` (UTC), `DiasDeGracia`, `SuspendeDesde` (`DateOnly`: «primer día en que el club se suspende; el último día para pagar es el anterior»), `SuspensionOmitida` (bool, falso por defecto), `AnuladoPorUsuarioId`, `AnuladoEn` y `MotivoAnulacion` (hasta 300 caracteres); no implementa `IPerteneceAClub`
- [X] T008 [P] Crear las entidades `IntentoPagoPlataforma` (`Id`, `CobroId`, `Referencia` único «`LFC-` + identificador sin guiones», `Valor`, `CreadoPorUsuarioId` `Guid?`, `CreadoEn`), `PagoPlataforma` (`Id`, `CobroId` único, `Valor`, `PagadoEn`, `Origen`, `Referencia?`, `TransaccionId?`, `MedioDePago?`, `RegistradoPorUsuarioId?`) y `NotificacionPasarela` (`Id`, `RecibidaEn`, `Resultado`, `TransaccionId?`, `EstadoPasarela?`, `Referencia?`, `Valor?`, `CobroId?`; «no guarda el cuerpo completo ni la firma») en `backend/src/LFClub.Dominio/Entidades/`, una por archivo; `PagoPlataforma` nunca se edita (documentarlo)
- [X] T009 [P] Crear `ReglaCobroPlataforma` en `backend/src/LFClub.Dominio/Reglas/ReglaCobroPlataforma.cs`: recibe el conteo por rol y los cuatro precios (nulos = 0) y devuelve el detalle por rol y el total (Σ integrantes × precio); devuelve «no hay cobro» si todos los precios son nulos o si el total es 0
- [X] T010 [P] Crear `ReglaSuspensionPorImpago` en `backend/src/LFClub.Dominio/Reglas/ReglaSuspensionPorImpago.cs`: recibe `EstadoClub`, `MotivoSuspension?`, la lista de cobros (`Estado`, `SuspendeDesde`, `SuspensionOmitida`) y el día de Colombia, y devuelve `Suspender`, `Reactivar` o `SinCambio`: `ACTIVO` con algún cobro `PENDIENTE` con `SuspendeDesde <= hoy` y `SuspensionOmitida` falso → `Suspender`; `SUSPENDIDO` con motivo `IMPAGO` y ningún cobro `PENDIENTE` con `SuspendeDesde <= hoy` y `SuspensionOmitida` falso → `Reactivar`; cualquier otro caso (incluida toda suspensión `MANUAL` y los clubes dados de baja) → `SinCambio`
- [X] T011 [P] Pruebas unitarias de las dos reglas en `backend/pruebas/Unitarias/Reglas/ReglaCobroPlataformaPruebas.cs` (total por rol; precio nulo como 0; todos nulos → sin cobro; total 0 → sin cobro; presidentes a precio 0) y `ReglaSuspensionPorImpagoPruebas.cs` (tabla de estado × motivo × cobros: suspender, reactivar, manual intocable, dado de baja intocable, `SuspensionOmitida` ignora el cobro, dos cobros vencidos con uno pagado, `SuspendeDesde` igual a hoy suspende y a mañana no)

### Datos

- [X] T012 Crear las configuraciones de EF en `backend/src/LFClub.Infraestructura/Datos/Configuraciones/` (una por entidad, al estilo de las existentes), registrar los `DbSet` en `backend/src/LFClub.Infraestructura/Datos/ContextoLFClub.cs` y generar la migración `PagoDeLaPlataforma` con las restricciones de data-model.md: índice único `(ClubId, Periodo)` en `CobroPlataforma` con `ClubId` `Cascade`; `IntentoPagoPlataforma.Referencia` único y `CobroId` `Cascade`; `PagoPlataforma.CobroId` único y `Cascade`; `NotificacionPasarela`: índice único `(TransaccionId, EstadoPasarela)` solo donde `TransaccionId` no es nulo y `CobroId` `Cascade`; `CHECK (Id = 1)` en `ConfiguracionPlataforma`; los usuarios (`*PorUsuarioId`) con `SetNull`; importes `numeric(18,0)`; enumeraciones como texto; las tablas **no** implementan `IPerteneceAClub`; en la migración, `UPDATE "Clubes" SET "MotivoSuspension" = 'MANUAL' WHERE "Estado" = 'SUSPENDIDO'` y `MesInicioCobro` nulo en los clubes existentes; depende de T004 a T008
- [X] T013 Crear la fila única de configuración al arrancar si no existe (todos los precios nulos, `DiasDeGracia` 5) en `backend/src/LFClub.Api/Configuracion/ArranqueBaseDatos.cs`, al estilo de lo que ya hace ese archivo; depende de T012
- [X] T014 [P] Crear `IRepositorioCobrosPlataforma` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioCobrosPlataforma.cs` con las operaciones que piden las historias: `ExisteAsync(clubId, periodo)`, `AgregarAsync(cobro)`, `ListarDeClubAsync(clubId)` (el más reciente primero), `ObtenerDeClubAsync(clubId, cobroId)` (`null` si no es de ese club), `ObtenerBloqueandoAsync(cobroId)`, `ListarPendientesAsync(clubId)`, `ObtenerIntentoPorReferenciaAsync(referencia)`, `AgregarIntentoAsync`, `ObtenerPagoDeCobroAsync(cobroId)`, `AgregarPagoAsync`, `AgregarNotificacionAsync`, `ExisteNotificacionAsync(transaccionId, estado)`, `ListarNotificacionesAsync(resultado?, limite)`, `ListarClubesConCobroPendienteAsync()`, `HayIntentoRecienteAsync(cobroId, desde)`; documentar cada una
- [X] T015 [P] Crear `IRepositorioConfiguracionPlataforma` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioConfiguracionPlataforma.cs` (`ObtenerAsync`, `ObtenerBloqueandoAsync`) y `IRepositorioConteoDeIntegrantes` en `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioConteoDeIntegrantes.cs` (`ContarPorRolDeClubesAsync(IEnumerable<Guid>)` y `ContarPorRolAsync(Guid clubId)`) que reutilice la misma definición de «integrante activo» de `RepositorioClubesPlataforma.Activos` (ingreso aprobado y no retirado)
- [X] T016 Implementar los repositorios en `backend/src/LFClub.Infraestructura/Repositorios/Plataforma/RepositorioCobrosPlataforma.cs`, `RepositorioConfiguracionPlataforma.cs` y `RepositorioConteoDeIntegrantes.cs`: cada consulta filtra por `ClubId` de forma explícita (no hay filtro global en estas tablas, research §6); ninguna devuelve datos personales; `ObtenerBloqueandoAsync` usa bloqueo de fila como `RepositorioClubesPlataforma.ObtenerBloqueandoAsync`; depende de T012, T014 y T015
- [X] T017 [P] Crear `IPasarelaDePago` en `backend/src/LFClub.Aplicacion/Interfaces/IPasarelaDePago.cs` (`Disponible`; `CrearEnlaceDePago(SolicitudDePago)` → `Uri?`, nulo si no está configurada; `VerificarNotificacion(string cuerpo, string? checksumDeLaCabecera)` → `NotificacionVerificada?`, nulo si la firma no es válida) más los registros `SolicitudDePago` (referencia, valor en pesos, dirección de retorno) y `NotificacionVerificada` (transacción, estado, referencia, valor en pesos, medio de pago) en el mismo directorio
- [X] T018 [P] Crear `OpcionesWompi` en `backend/src/LFClub.Infraestructura/Pagos/OpcionesWompi.cs` (`LlavePublica`, `SecretoIntegridad`, `SecretoEventos`, `UrlCheckout` con el valor por defecto de Wompi `https://checkout.wompi.co/p/`) con una propiedad `Completa`; sin valores reales
- [X] T019 Crear `FirmaWompi` en `backend/src/LFClub.Infraestructura/Pagos/FirmaWompi.cs`: `Integridad(referencia, centavos, moneda, secreto)` = SHA-256 hexadecimal de `referencia + centavos + moneda + secreto`; `ChecksumDeEvento(valoresDeLasPropiedades, timestamp, secreto)` = SHA-256 hexadecimal de la concatenación en ese orden; `SonIguales` con comparación de tiempo constante (`CryptographicOperations.FixedTimeEquals`); contrastar el formato exacto con la documentación vigente de Wompi y anotar el enlace en el comentario de la clase
- [X] T020 [P] Pruebas de `FirmaWompi` en `backend/pruebas/Unitarias/Pagos/FirmaWompiPruebas.cs` con vectores conocidos calculados a mano con secretos inventados: la firma de integridad de un ejemplo, el checksum de un evento de ejemplo, un evento con un solo dígito cambiado no coincide, una cadena de distinto largo no rompe la comparación
- [X] T021 Crear `PasarelaWompi` en `backend/src/LFClub.Infraestructura/Pagos/PasarelaWompi.cs` que implementa `IPasarelaDePago` con `FirmaWompi`: arma `UrlCheckout` con `public-key`, `currency=COP`, `amount-in-cents`, `reference`, `redirect-url` y `signature:integrity`; `VerificarNotificacion` lee el JSON, toma de `data` los valores de `signature.properties`, compara el checksum calculado con el del cuerpo y con la cabecera `X-Event-Checksum` si viene, y devuelve la notificación solo si coincide; `Disponible` = `OpcionesWompi.Completa`; no hace llamadas salientes ni escribe secretos en el registro; depende de T017 a T019
- [X] T022 Registrar en `backend/src/LFClub.Api/Configuracion/RegistroDeServicios.cs`: `OpcionesWompi` desde `Wompi:*` de `IConfiguration`, `IPasarelaDePago` → `PasarelaWompi` (singleton), los tres repositorios nuevos, y el trabajo `TrabajoCobrosPlataforma` (T029) con el interruptor `configuracion.GetValue("Plataforma:TrabajoActivo", true)`; y añadir en `backend/src/LFClub.Api/appsettings.json` la sección `"Wompi": { "LlavePublica": "", "SecretoIntegridad": "", "SecretoEventos": "" }` con valores vacíos; depende de T016 y T021
- [X] T023 [P] Crear `IServicioSuspensionPorImpago` en `backend/src/LFClub.Aplicacion/Servicios/IServicioSuspensionPorImpago.cs` (`AplicarAsync(CancellationToken)`: revisa todos los clubes con cobro pendiente y devuelve cuántos suspendió; `ReevaluarAsync(Guid clubId, CancellationToken)`: revisa un club y lo reactiva si corresponde) e implementarlo en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioSuspensionPorImpago.cs`: usa `ReglaSuspensionPorImpago` y el día de `ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc)`; al suspender pone `Estado = SUSPENDIDO`, `MotivoSuspension = IMPAGO`, `EstadoCambiadoPorUsuarioId = null` y `EstadoCambiadoEn`; al reactivar pone `ACTIVO` y vacía el motivo; trabaja con `IUnidadDeTrabajo.EnTransaccionAsync` y `IRepositorioClubesPlataforma.ObtenerBloqueandoAsync`; depende de T010 y T016
- [X] T024 Registrar los servicios de Aplicacion de esta base en `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs` (empezando por `IServicioSuspensionPorImpago`); cada historia añade los suyos
- [X] T025 [P] Crear `frontend/src/compartido/api/tiposPagoPlataforma.ts` con los tipos del contrato (`CobroPlataformaDto`, `DetalleDeRolDto`, `PagoPlataformaDto`, `IntentoDePagoDto`, `TarifasDto`, `GuardarTarifasDto`, `FijarInicioDelCobroDto`, `EstadoDePagoClubDto`, `AnularCobroDto`, `NotificacionPasarelaDto` y las enumeraciones `EstadoCobroPlataforma`, `EstadoDePagoClub`, `ResultadoNotificacion`) con los nombres y tipos exactos de `contracts/api.yaml` (importes `number`, periodos `string` `AAAA-MM`, fechas `string`)

**Punto de control**: `backend` compila, la migración se aplica en una base vacía y en una con clubes
(los suspendidos quedan `MANUAL`), las reglas y la firma pasan sus pruebas y
`EsquemaDeEliminacionPruebas` (014) sigue en verde.

## Fase 3: Historia 4 — El cobro mensual se genera solo con el valor correcto (P1)

**Meta**: cada mes se crea el cobro de cada club con el detalle congelado; el DESARROLLADOR fija los
precios, los días de gracia y el mes de inicio de cada club.

**Prueba independiente**: con precios definidos y un club con integrantes de varios roles y mes de
inicio, forzar la generación: un cobro con el detalle y el total esperados; repetirla no duplica;
cambiar precios no altera el ya generado (quickstart pasos 1 a 4).

- [X] T026 [P] [US4] Crear `TarifasDto`, `GuardarTarifasDto`, `FijarInicioDelCobroDto` y `EstadoDePagoClubDto` en `backend/src/LFClub.Aplicacion/DTOs/` con los nombres y tipos de `contracts/api.yaml`; `TarifasDto.PasarelaConfigurada` es solo un booleano, nunca los valores
- [X] T027 [US4] Crear `IServicioTarifas` e `ServicioTarifas` en `backend/src/LFClub.Aplicacion/Servicios/IServicioTarifas.cs` y `Implementaciones/ServicioTarifas.cs`: `ObtenerAsync` y `GuardarAsync(GuardarTarifasDto, Guid usuarioId)`; validar con `ErroresDeValidacion` que cada precio sea nulo o ≥ 0 en pesos enteros y `DiasDeGracia` de 0 a 30, con mensajes claros (RF-002); guardar quién y cuándo; depende de T015 y T026
- [X] T028 [US4] Crear `IServicioCobrosPlataforma` e `ServicioCobrosPlataforma` en `backend/src/LFClub.Aplicacion/Servicios/IServicioCobrosPlataforma.cs` y `Implementaciones/ServicioCobrosPlataforma.cs` con `GenerarAsync(CancellationToken)` → cuántos creó: sin precios definidos no hace nada; para cada club `ACTIVO` o `SUSPENDIDO` con `MesInicioCobro <= ReglaMesDeCobro.MesEnCurso(ahora)` y sin cobro de ese periodo (`ExisteAsync`), cuenta los integrantes por rol, aplica `ReglaCobroPlataforma` y, si hay total, guarda el cobro con el detalle, los precios y los días de gracia vigentes, `GeneradoEn`, y `SuspendeDesde = ReglaMesDeCobro.DiaEnColombia(ahora) + DiasDeGracia`; no genera cobros de meses anteriores aunque el mes de inicio sea pasado (RF-005a); si dos instancias chocan por el índice único `(ClubId, Periodo)`, captura la violación y sigue; nunca genera para `DADO_DE_BAJA`; depende de T014 a T016 y T027
- [X] T029 [US4] Crear `TrabajoCobrosPlataforma` en `backend/src/LFClub.Infraestructura/Trabajos/TrabajoCobrosPlataforma.cs` copiando el patrón de `TrabajoCargosMensuales` (arranque y cada 30 minutos, errores capturados y registrados sin tumbar la API): llama a `IServicioCobrosPlataforma.GenerarAsync` y después a `IServicioSuspensionPorImpago.AplicarAsync`, cada uno en su propio `try` para que un fallo del primero no impida el segundo; registra solo cantidades; documentación XML
- [X] T030 [US4] Fijar `MesInicioCobro` de los clubes nuevos al mes siguiente en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioCreacionClub.cs` con `ReglaMesDeCobro.PrimerMesDeCobro(_reloj.AhoraUtc)`; ajustar la prueba existente de creación de club si compara el club completo
- [X] T031 [US4] Crear `ControladorTarifas` en `backend/src/LFClub.Api/Controladores/Plataforma/ControladorTarifas.cs` (`GET` y `PUT /api/plataforma/tarifas`, `[SoloDesarrollador]`, `Tags("Plataforma")`, `ProducesResponseType` de `contracts/api.yaml`) y la operación `PUT /api/plataforma/clubes/{clubId}/cobro` (`fijarInicioDelCobro`) en `ControladorCobrosPlataforma.cs` de la misma carpeta; implementar `FijarInicioDelCobroAsync(clubId, mesDeInicio?)` en un `IServicioInicioDelCobro` pequeño en `Servicios/` e `Implementaciones/` que acepte `AAAA-MM` o nulo, rechace con 400 un formato inválido, responda 404 si el club no existe y devuelva el `EstadoDePagoClubDto`; registrar los servicios nuevos en `RegistroDeCasosDeUso.cs`; depende de T027
- [X] T032 [P] [US4] Crear `frontend/src/plataforma/apiPagoPlataforma.ts` (`tarifas.obtener`, `tarifas.guardar`, `inicioDelCobro.fijar`) y la pantalla `frontend/src/plataforma/PantallaTarifas.tsx` (cuatro precios, días de gracia, aviso de que aplican a los cobros que se generen después, error de validación visible, indicador de si la pasarela está configurada); añadir la ruta `tarifas` bajo `/plataforma` en `frontend/src/App.tsx` y un acceso «Tarifas» en `frontend/src/plataforma/DisposicionPlataforma.tsx`; depende de T025 y T031
- [X] T033 [P] [US4] Crear `frontend/src/plataforma/SeccionInicioDelCobro.tsx`: selector de mes (o «No cobrar») y botón guardar, con el valor actual; mostrarlo en `frontend/src/plataforma/DetalleClub.tsx`; depende de T032
- [X] T034 [US4] Pruebas de integración en `backend/pruebas/Integracion/Plataforma/CobrosPlataformaPruebas.cs` con `IReloj` sustituido: sin precios no se crea nada; con precios y mes de inicio, un cobro con el conteo por rol, los precios y el total exactos; repetir `GenerarAsync` no duplica; dos llamadas concurrentes crean uno; cambiar precios e integrantes no cambia el cobro generado; un club suspendido también recibe cobro; uno dado de baja no; total 0 no genera; mes de inicio futuro o nulo no genera; mes de inicio pasado genera solo el mes en curso; reinicio el día 3 genera con `SuspendeDesde` desde el día 3; un club creado queda con `MesInicioCobro` del mes siguiente; el 1 de enero tras diciembre
- [X] T035 [US4] Pruebas de integración en `backend/pruebas/Integracion/Plataforma/TarifasPruebas.cs`: el DESARROLLADOR guarda y lee tarifas; precio negativo, decimal o `DiasDeGracia` de 31 → 400; PRESIDENTE y otros roles → 403; sin sesión → 401; fijar y borrar el mes de inicio; mes inválido → 400; club inexistente → 404
- [X] T036 [P] [US4] Pruebas Vitest en `frontend/pruebas/pagoPlataforma/PantallaTarifas.test.tsx` (guarda, muestra errores, muestra el aviso de que aplica hacia adelante) y `SeccionInicioDelCobro.test.tsx`
- [X] T037 [US4] Actualizar los recuentos de `backend/pruebas/Integracion/Aislamiento/AccesoPlataformaPruebas.cs` y de `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs` para las operaciones nuevas de esta fase y comprobar que la cobertura de roles las exige para el DESARROLLADOR

**Punto de control**: el DESARROLLADOR fija precios y mes de inicio desde su panel y el trabajo (o la
prueba) genera un único cobro correcto por club y periodo.

## Fase 4: Historia 1 — El PRESIDENTE ve el cobro del mes y lo paga (P1)

**Meta**: sección de pago de la plataforma solo para el PRESIDENTE, con el detalle, el botón que lleva
a la pasarela y la pantalla de retorno que solo consulta.

**Prueba independiente**: con un cobro pendiente, entrar como PRESIDENTE, ver el detalle y el total,
pulsar pagar y llegar al checkout con el valor exacto; el DIRECTIVO no ve nada (quickstart pasos 5 y
6).

- [X] T038 [P] [US1] Crear `CobroPlataformaDto`, `DetalleDeRolDto`, `PagoPlataformaDto` e `IntentoDePagoDto` en `backend/src/LFClub.Aplicacion/DTOs/` con los nombres y tipos de `contracts/api.yaml`; ningún campo de llave, secreto ni de datos personales
- [X] T039 [US1] Crear `IServicioConsultaPagoPlataforma` y su implementación en `backend/src/LFClub.Aplicacion/Servicios/` e `Implementaciones/`: `ObtenerAsync(clubId)` arma el resumen (pendientes, total pendiente, historial, `suspendidoPorImpago` leyendo `Club.Estado` y `MotivoSuspension`, `pagoEnLineaDisponible` desde `IPasarelaDePago.Disponible`) y `ObtenerCobroAsync(clubId, cobroId)` responde `ExcepcionDeAplicacion.NoEncontrado()` si el cobro no es del club; el detalle por rol sale del cobro guardado, nunca de un cálculo nuevo; `enVerificacion` es verdadero si hay un intento de ese cobro en los últimos 30 minutos y el cobro sigue pendiente; depende de T016, T017 y T038
- [X] T040 [US1] Crear `IServicioPagoPlataforma` y su implementación con `IniciarAsync(clubId, cobroId, usuarioId)`: 404 si el cobro no es del club; 409 `cobro_no_pendiente` si está pagado o anulado; 503 `pago_no_disponible` si `IPasarelaDePago.Disponible` es falso; crea un `IntentoPagoPlataforma` con referencia `LFC-` + `Guid.CreateVersion7().ToString("N")` y el valor **del cobro guardado**, pide el enlace a la pasarela con la dirección de retorno `{Frontend:UrlBase}/club/{clubId}/pago-plataforma/retorno?cobro={cobroId}` y devuelve `IntentoDePagoDto`; depende de T016 y T017
- [X] T041 [US1] Crear `ControladorPagoPlataforma` en `backend/src/LFClub.Api/Controladores/Club/ControladorPagoPlataforma.cs` con `GET /api/clubes/{clubId}/pago-plataforma`, `GET …/cobros/{cobroId}` y `POST …/cobros/{cobroId}/intentos` (201), todas con `[IntegranteDelClub(Rol.PRESIDENTE)]`, `Cache-Control: private, no-store` y los `ProducesResponseType` de `contracts/api.yaml`; registrar los servicios en `RegistroDeCasosDeUso.cs`; depende de T039 y T040
- [X] T042 [P] [US1] Crear `frontend/src/privado/pagoPlataforma/apiPagoPlataforma.ts` (`obtener`, `obtenerCobro`, `iniciarPago`) y `usePagoPlataforma.ts` con `useCarga`, al estilo de `frontend/src/privado/pagos/apiMensualidad.ts` y `useMensualidad.ts`; depende de T025
- [X] T043 [US1] Crear `frontend/src/privado/pagoPlataforma/TarjetaCobro.tsx` (periodo, integrantes por rol con precio y subtotal, total con `formatoMoneda`, «Paga hasta el {día anterior a suspendeDesde}», estado con texto e ícono y «En verificación» cuando `enVerificacion`; para un cobro pagado, comprobante con fecha, valor, referencia y medio; para uno anulado, el motivo) y `PagoPlataforma.tsx` (pendientes con el botón «Pagar» que llama a `iniciarPago` y abre `urlDePago` con `window.location.assign`; sin botón y con aviso «El pago en línea no está disponible» cuando `pagoEnLineaDisponible` es falso; historial; estado «Estás al día» cuando no hay pendientes); los textos van en `frontend/src/privado/pagoPlataforma/textosPagoPlataforma.ts`; depende de T042
- [X] T044 [US1] Crear `frontend/src/privado/pagoPlataforma/RetornoPagoPlataforma.tsx`: lee `cobro` de la dirección, consulta `obtenerCobro` cada 3 segundos hasta 30 segundos y muestra «En verificación» hasta que el cobro figura `PAGADO` (entonces «Pago confirmado» con el comprobante) y, pasados los 30 segundos, «Todavía estamos verificando tu pago; puedes volver a esta pantalla más tarde» con enlace a la sección; **no envía nada que cambie el estado** (RF-011); depende de T042
- [X] T045 [US1] Añadir las rutas `pago-plataforma` y `pago-plataforma/retorno` bajo `/club/:clubId` en `frontend/src/App.tsx` y la entrada «Pago de la plataforma» en `frontend/src/privado/MenuDelClub.tsx`, visible solo para el PRESIDENTE (mirar cómo el menú ya distingue roles); depende de T043 y T044
- [X] T046 [US1] Crear `frontend/src/privado/pagoPlataforma/AvisoPagoPlataforma.tsx` (aviso fijo con el valor y «Paga hasta el …» y enlace a la sección; solo cuando hay cobro pendiente) y mostrarlo en la rama del PRESIDENTE de `frontend/src/privado/InicioClub.tsx`; no en las demás ramas; que mientras carga no muestre nada y que un error no bloquee el resto del inicio; depende de T042
- [X] T047 [US1] Pruebas de integración en `backend/pruebas/Integracion/Pagos/PagoPlataformaPresidentePruebas.cs` con una pasarela falsa: el PRESIDENTE ve pendientes con el detalle por rol y el total; ve el historial; `POST …/intentos` devuelve una dirección con el valor y la referencia del intento, y no cambia el cobro; dos intentos seguidos crean dos referencias distintas; cobro pagado o anulado → 409; sin configuración de pasarela → 503 `pago_no_disponible` y el resumen trae `pagoEnLineaDisponible` falso; el valor del intento sale del cobro aunque el cliente mande otro; DIRECTIVO, ENTRENADOR y JUGADOR → 403; sin sesión → 401; cabecera `Cache-Control: private, no-store`
- [X] T048 [P] [US1] Pruebas de aislamiento en `backend/pruebas/Integracion/Aislamiento/AislamientoDePagoPlataformaPruebas.cs`: un PRESIDENTE de otro club pidiendo el cobro o iniciando el pago de un cobro ajeno por su identificador → 404; con varios presidentes, lo ven los dos; el DESARROLLADOR no tiene ruta de club
- [X] T049 [P] [US1] Pruebas Vitest en `frontend/pruebas/pagoPlataforma/PagoPlataforma.test.tsx` (detalle y total; botón «Pagar» abre la dirección; sin botón cuando no hay pasarela; «Estás al día»; comprobante de un cobro pagado) y `RetornoPagoPlataforma.test.tsx` (en verificación; pasa a confirmado cuando la consulta lo devuelve pagado; mensaje tras 30 segundos; nunca marca pagado por sí solo) y `AvisoPagoPlataforma.test.tsx` (visible para el PRESIDENTE, no se pide para otros roles)
- [X] T050 [US1] Actualizar los recuentos de `AccesoClubPruebas.cs` y `EndpointsDeLaApi.cs` para las tres operaciones de club y comprobar que la cobertura de roles las exige para el PRESIDENTE y las rechaza a los demás roles

**Punto de control**: el PRESIDENTE llega al checkout de Wompi (sandbox) con el valor exacto y los
demás roles no ven ni pueden pedir nada.

## Fase 5: Historia 2 — El pago solo cuenta cuando la pasarela lo confirma (P1)

**Meta**: la notificación verificada es la única que marca pagado; todo lo raro queda registrado.

**Prueba independiente**: una notificación auténtica de pago aprobado marca el cobro como pagado; una
falsa, alterada o de otro valor se rechaza o se registra sin cambiar nada (quickstart pasos 7 a 10).

- [X] T051 [US2] Crear `IServicioNotificacionPasarela` y `ServicioNotificacionPasarela` en `backend/src/LFClub.Aplicacion/Servicios/` e `Implementaciones/` con `RecibirAsync(string cuerpo, string? checksum)` → `ResultadoNotificacion`, siguiendo research §9: sin firma válida guarda `FIRMA_INVALIDA` (sin ids ni cuerpo) y lanza 401; busca el intento por referencia (`REFERENCIA_DESCONOCIDA` si no existe); compara el valor con el del cobro (`VALOR_NO_COINCIDE`); si el estado no es `APPROVED` guarda `NO_APROBADA`; si la misma `(transacción, estado)` ya está guardada devuelve `REPETIDA` sin tocar nada; si el cobro está `ANULADO` guarda `PAGO_A_COBRO_ANULADO`; si el cobro ya tiene pago guarda `PAGO_DUPLICADO`; si no, **en una sola transacción**: crea `PagoPlataforma` (origen `PASARELA`, valor, `PagadoEn` = reloj, referencia, transacción, medio), pasa el cobro a `PAGADO` y llama a `IServicioSuspensionPorImpago.ReevaluarAsync`, y guarda `APLICADA`; un cobro `PAGADO` nunca vuelve a `PENDIENTE`; un fallo por índice único se trata como `REPETIDA`; nunca registra el cuerpo ni secretos; depende de T016, T017 y T023
- [X] T052 [US2] Crear `ControladorNotificacionesPasarela` en `backend/src/LFClub.Api/Controladores/Plataforma/ControladorNotificacionesPasarela.cs` con `POST /api/plataforma/pagos/notificaciones`: anónimo (el mismo mecanismo que usan las operaciones públicas, `FiltroOperacionAnonima`), lee el cuerpo crudo y la cabecera `X-Event-Checksum`, responde `200` a toda notificación bien firmada aunque no cambie nada y `401` si la firma no es válida; documentación XML; registrar el servicio en `RegistroDeCasosDeUso.cs`; depende de T051
- [X] T053 [US2] Pruebas de integración en `backend/pruebas/Integracion/Plataforma/NotificacionPasarelaPruebas.cs` con la pasarela falsa (que firma con un secreto de prueba): una notificación aprobada cuya referencia y valor coinciden → cobro `PAGADO` con fecha, referencia y medio, y un solo `PagoPlataforma`; firma alterada → 401, cobro intacto y fila `FIRMA_INVALIDA`; valor distinto → 200, cobro intacto y `VALOR_NO_COINCIDE`; referencia desconocida → 200 y `REFERENCIA_DESCONOCIDA`; `DECLINED`, `VOIDED` y `ERROR` → cobro pendiente y `NO_APROBADA`, y se puede iniciar otro intento; la misma notificación dos veces (y dos en paralelo) → un solo pago y `REPETIDA`; un segundo pago aprobado con otro intento del mismo cobro → `PAGO_DUPLICADO` sin tocar nada; pago a un cobro anulado → `PAGO_A_COBRO_ANULADO`; una `DECLINED` posterior a una `APPROVED` no revierte; **volver del navegador con parámetros de aprobado sin notificación no cambia nada**; la notificación llega antes de que el PRESIDENTE consulte y este ya lo ve pagado; la fila guardada no contiene el cuerpo ni la firma
- [X] T054 [P] [US2] Prueba de arquitectura/secretos en `backend/pruebas/Integracion/Plataforma/SinSecretosPruebas.cs`: ningún DTO de `LFClub.Aplicacion.DTOs` tiene propiedades llamadas `Llave*`, `Secreto*` ni `Checksum*`; ninguna respuesta de las operaciones de esta funcionalidad contiene los secretos de la pasarela falsa; el registro de eventos capturado durante una notificación no contiene los secretos ni el cuerpo completo

**Punto de control**: el flujo pagar → notificación → cobro pagado funciona de punta a punta en
pruebas y ningún intento de falsificación cambia un cobro.

## Fase 6: Historia 3 — Sin pago, el club se suspende solo, y al pagar vuelve (P1)

**Meta**: suspensión automática por impago, reactivación inmediata, y la regla de que una suspensión
manual no se levanta con un pago.

**Prueba independiente**: dejar vencer los días de gracia de un cobro sin pagar y comprobar que el club
queda suspendido; entrar como PRESIDENTE y como otro rol; pagar y comprobar que vuelve a activo
(quickstart pasos 11 a 16).

- [X] T055 [US3] Ajustar `backend/src/LFClub.Aplicacion/Implementaciones/ServicioEstadoClub.cs`: al pasar a `SUSPENDIDO` guardar `MotivoSuspension = MANUAL`; en cualquier otro cambio de estado vaciar el motivo; **al pasar de `SUSPENDIDO` con motivo `IMPAGO` a `ACTIVO`, marcar `SuspensionOmitida = true` en los cobros `PENDIENTE` con `SuspendeDesde <= hoy` del club, en la misma transacción** (el club queda activo hasta el siguiente cobro, RF-016a); actualizar el comentario de la clase; las pruebas existentes de `ServicioEstadoClub` no deben cambiar de resultado
- [X] T056 [US3] Hacer que la reactivación también ocurra en la misma transacción del pago: confirmar en `ServicioNotificacionPasarela` (T051) que `ReevaluarAsync` corre dentro de la transacción del pago y, cuando se implementen la marca manual y la anulación (T068), que lo llamen igual; no tocar nunca una suspensión `MANUAL`
- [X] T057 [US3] Comprobar que `ReglaAccesoPorEstado` y `IntegranteDelClubAttribute` ya dejan al PRESIDENTE llegar al resumen, al cobro y al intento de pago con el club `SUSPENDIDO`, y que a los demás roles les responden con el aviso de incidencia temporal sin ningún dato del cobro; si algo lo impide, corregirlo en `backend/src/LFClub.Api/Autorizacion/IntegranteDelClubAttribute.cs` con la menor modificación y avisar
- [X] T058 [US3] Crear `frontend/src/privado/pagoPlataforma/AvisoSuspensionPorImpago.tsx`: aviso rojo fijo («Tu club está suspendido por falta de pago», el valor, el botón «Pagar ahora» que lleva a la sección), mostrado en `frontend/src/privado/InicioClub.tsx` para el PRESIDENTE cuando `suspendidoPorImpago` es verdadero (en lugar del aviso amarillo de T046); comprobar en `frontend/src/privado/DisposicionClub.tsx` y `AvisoClubNoDisponible.tsx` que el PRESIDENTE de un club suspendido sigue pudiendo abrir inicio y «Pago de la plataforma» y que los demás roles ven el aviso de incidencia temporal sin datos del cobro
- [X] T059 [US3] Pruebas de integración en `backend/pruebas/Integracion/Plataforma/SuspensionPorImpagoPruebas.cs` con `IReloj` sustituido y el trabajo apagado (llamando `AplicarAsync` a mano): con la gracia de 5 días, el día 5 sigue activo y el día 6 a las 00:00 de Colombia queda `SUSPENDIDO` con motivo `IMPAGO` y `EstadoCambiadoPorUsuarioId` nulo; 04:59 UTC del día 6 sigue activo y 05:00 UTC suspende; el PRESIDENTE entra y los demás roles reciben el aviso de incidencia temporal sin datos del cobro; pagar (notificación aprobada) lo reactiva de inmediato y sus datos siguen intactos; un club suspendido **a mano** con el cobro pagado o pendiente **sigue suspendido** tras un pago; suspender a mano y levantar un club con el cobro ya pagado no deja motivo `IMPAGO`; dos cobros vencidos: pagar uno no reactiva, pagar el segundo sí; **levantar a mano una suspensión por impago deja el club activo, el cobro sigue pendiente y pagable, un nuevo ciclo no lo vuelve a suspender y un cobro posterior vencido sin pago sí**; un club dado de baja no cambia de estado; el cambio de estado por el sistema no sobrescribe `EstadoCambiadoPorUsuarioId` de un cambio manual anterior sin dejar constancia
- [X] T060 [P] [US3] Pruebas Vitest en `frontend/pruebas/pagoPlataforma/AvisoSuspensionPorImpago.test.tsx` (visible solo para el PRESIDENTE con `suspendidoPorImpago`; el botón lleva a la sección; el resto de roles no pide el resumen)

**Punto de control**: la suspensión y la reactivación funcionan solas, la suspensión manual no se
levanta con un pago y el club suspendido sigue dejando pagar al PRESIDENTE.

## Fase 7: Historia 6 — Las llaves de la pasarela están protegidas (P1)

**Meta**: ningún secreto en el repositorio, en las respuestas ni en el registro; la aplicación sigue
funcionando sin configuración.

**Prueba independiente**: revisar el repositorio y las respuestas de la API en busca de llaves; arrancar
sin configuración y comprobar que el pago se indica como no disponible sin romper el resto
(quickstart pasos 20 y 21).

- [X] T061 [US6] Añadir `<UserSecretsId>` a `backend/src/LFClub.Api/LFClub.Api.csproj` (si no lo tiene) para poder guardar los secretos de pruebas fuera del repositorio con `dotnet user-secrets`; **pedir al propietario las claves de Wompi del ambiente de pruebas (sandbox)** y guardarlas con `dotnet user-secrets set "Wompi:LlavePublica" …`, `Wompi:SecretoIntegridad` y `Wompi:SecretoEventos` desde `backend/src/LFClub.Api`; nunca escribirlas en un archivo del repositorio, en un comentario ni en un mensaje de commit
- [X] T062 [US6] Prueba en `backend/pruebas/Integracion/Plataforma/SinSecretosEnElRepositorioPruebas.cs`: recorre los archivos versionados del repositorio (excluyendo `bin`, `obj`, `node_modules` y `.git`) y falla si encuentra cadenas con los prefijos de llaves de Wompi (`pub_prod_`, `pub_test_`, `prod_integrity_`, `test_integrity_`, `prod_events_`, `test_events_`) seguidas de caracteres; y comprueba que `appsettings.json` y `appsettings.Development.json` tienen la sección `Wompi` con valores vacíos o ausente
- [X] T063 [US6] Pruebas de integración en `backend/pruebas/Integracion/Pagos/PasarelaNoDisponiblePruebas.cs` arrancando la API **sin** configuración de Wompi: el resumen del PRESIDENTE trae el cobro y `pagoEnLineaDisponible` falso; `POST …/intentos` → 503 `pago_no_disponible`; `GET /api/plataforma/tarifas` indica `pasarelaConfigurada` falso y no devuelve valores; el trabajo sigue generando cobros y suspendiendo; la marca manual (T068) sigue funcionando y reactiva; cualquier otra parte de la aplicación responde normal
- [X] T064 [P] [US6] Comprobar el frontend sin pasarela en `frontend/pruebas/pagoPlataforma/PagoPlataforma.test.tsx` (sin botón, con el aviso «El pago en línea no está disponible», sin error en el resto de la pantalla)

**Punto de control**: no hay ningún secreto versionado, las pruebas sin configuración pasan y las
claves de pruebas del propietario están guardadas solo en sus `user-secrets`.

## Fase 8: Historia 5 — El DESARROLLADOR ve cómo van los clubes (P2)

**Meta**: estado de pago por club en la lista, historial de cobros, pago manual, anulación y revisión de
notificaciones raras (los precios y el mes de inicio ya se hicieron en la fase 3).

**Prueba independiente**: abrir el panel y comprobar el estado de pago de cada club y su historial;
marcar un pago manual y anular un cobro (quickstart pasos 17 a 19).

- [X] T065 [P] [US5] Añadir a `ClubResumenDto` (en `backend/src/LFClub.Aplicacion/DTOs/ClubResumenDto.cs`) `EstadoDePago`, `ValorAdeudado`, `MesDeInicioDelCobro` (`AAAA-MM` o nulo) y `MotivoDeSuspension` (nulo salvo suspendido), con documentación; **nunca** datos personales ni llaves; actualizar `backend/src/LFClub.Aplicacion/Mappers/MapperClubPlataforma.cs`
- [X] T066 [US5] Calcular el estado de pago en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioConsultaClubes.cs` (lista y detalle) con una función pura `ReglaEstadoDePagoClub` en `backend/src/LFClub.Dominio/Reglas/ReglaEstadoDePagoClub.cs` según la tabla de data-model.md (`SIN_COBRO`, `AL_DIA`, `PENDIENTE`, `VENCIDO`, `SUSPENDIDO_POR_IMPAGO`) y el valor adeudado (suma de los cobros `PENDIENTE`); leer los cobros pendientes de todos los clubes en una sola consulta (`ListarClubesConCobroPendienteAsync`), sin una consulta por club; depende de T016 y T065
- [X] T067 [P] [US5] Pruebas unitarias de `ReglaEstadoDePagoClub` en `backend/pruebas/Unitarias/Reglas/ReglaEstadoDePagoClubPruebas.cs` (cada estado, el mes de inicio futuro, suspendido por impago frente a manual, `SuspensionOmitida` sigue como vencido)
- [X] T068 [US5] Crear `IServicioCobrosDeUnClub` y su implementación con: `ListarAsync(clubId)` (404 si el club no existe), `MarcarPagadoAsync(clubId, cobroId, usuarioId)` que crea un `PagoPlataforma` con origen `MANUAL`, `RegistradoPorUsuarioId` y la fecha del reloj, pasa el cobro a `PAGADO` y llama a `ReevaluarAsync` en la misma transacción (409 si el cobro no está `PENDIENTE`) y `AnularAsync(clubId, cobroId, usuarioId, AnularCobroDto)` que exige un motivo de 3 a 300 caracteres, deja quién, cuándo y por qué, pasa el cobro a `ANULADO` y llama a `ReevaluarAsync`; ambas dejan el club activo si estaba suspendido por impago y ya no le quedan cobros vencidos, y no tocan una suspensión manual; depende de T016 y T023
- [X] T069 [US5] Crear `IServicioConsultaNotificaciones` y su implementación (`ListarAsync(ResultadoNotificacion? resultado)`, hasta 100, la más reciente primero, con `clubId` y `periodo` del cobro cuando existe) y los DTO `AnularCobroDto` y `NotificacionPasarelaDto` en `backend/src/LFClub.Aplicacion/DTOs/`; completar `backend/src/LFClub.Api/Controladores/Plataforma/ControladorCobrosPlataforma.cs` con `GET /api/plataforma/clubes/{clubId}/cobros`, `POST …/cobros/{cobroId}/pago-manual`, `POST …/cobros/{cobroId}/anulacion` y `GET /api/plataforma/notificaciones` (`[SoloDesarrollador]`; si el archivo se acerca a 250 líneas, partir las notificaciones a `ControladorNotificacionesRecibidas.cs`); registrar los servicios; depende de T068
- [X] T070 [US5] Crear `frontend/src/plataforma/SeccionCobros.tsx` (historial del club con periodo, detalle, total, estado, fecha de pago, origen y referencia; botones «Marcar pagado» con confirmación y «Anular» con un motivo obligatorio) y mostrarla en `frontend/src/plataforma/DetalleClub.tsx`; añadir en `frontend/src/plataforma/ListaClubes.tsx` la columna «Pago» con `EtiquetaEstado` (al día, pendiente, vencido, suspendido por impago, sin cobro) y el valor adeudado, y en `SeccionEstado.tsx` el aviso «Si levantas una suspensión por impago, el club queda activo hasta el próximo cobro»; ampliar `ClubResumenDto` en `frontend/src/compartido/api/tipos.ts`; crear `frontend/src/plataforma/PantallaNotificaciones.tsx` (lista filtrable por resultado, con los duplicados resaltados), con su ruta y su acceso desde `DisposicionPlataforma.tsx`; depende de T032, T066 y T069
- [X] T071 [US5] Pruebas de integración en `backend/pruebas/Integracion/Plataforma/CobrosDeUnClubPruebas.cs`: el DESARROLLADOR lista los cobros de un club sin datos personales; pago manual crea el pago con origen `MANUAL` y quién y cuándo, marca el cobro y reactiva el club suspendido por impago pero no el suspendido a mano; marcar uno ya pagado o anulado → 409; anular exige motivo de 3 a 300 caracteres y deja el cobro no pagable (`POST …/intentos` → 409) y sin efecto sobre la suspensión; la lista de clubes trae el estado de pago y el valor adeudado de cada club; `GET /api/plataforma/notificaciones` lista duplicados y filtra por resultado; todo rechazado al PRESIDENTE y a los demás roles (403) y a quien no tiene sesión (401); el PRESIDENTE no ve el panel
- [X] T072 [P] [US5] Pruebas Vitest en `frontend/pruebas/pagoPlataforma/SeccionCobros.test.tsx` (marcar pagado pide confirmación; anular exige motivo), `ListaClubesPago.test.tsx` (etiqueta de estado y valor adeudado) y `PantallaNotificaciones.test.tsx`
- [X] T073 [US5] Actualizar los recuentos de `AccesoPlataformaPruebas.cs` y `EndpointsDeLaApi.cs` para las operaciones de esta fase y comprobar que las 11 operaciones de la funcionalidad coinciden con `contracts/api.yaml` ruta por ruta

## Fase 9: Cierre

- [X] T074 Medir tamaños con `scripts/verificar-tamano.ps1` (ningún archivo escrito a mano por encima de 250 líneas; partir `ServicioNotificacionPasarela` o los controladores si hace falta) y correr el lint y la compilación del frontend y del backend sin advertencias nuevas
- [X] T075 Correr la batería: `dotnet test backend` (unitarias, integración y el guardián de esquema de la 014, que debe seguir en verde con las tablas nuevas) y `npm --prefix frontend test`; todo en verde
- [ ] T076 Recorrer `specs/017-pago-de-la-plataforma/quickstart.md` de punta a punta contra la API real **en el ambiente de pruebas de Wompi** (pasos 1 a 21 y la tabla de autorización), con los dos temas (claro y oscuro) y en teléfono y escritorio, incluido el abrir el checkout desde la aplicación Android; registrar en Wompi la dirección de eventos `<API>/api/plataforma/pagos/notificaciones` completa (con la ruta, no solo la base)
- [ ] T077 Validación con el propietario del diseño de las pantallas nuevas (sección «Pago de la plataforma», avisos amarillo y rojo del PRESIDENTE, retorno, tarifas, cobros y notificaciones del DESARROLLADOR), que no está en el lienzo (plan, nota 3); anotar los cambios que pida
- [ ] T078 Antes de pasar a producción: el propietario regenera los secretos de producción de Wompi (se compartieron en una conversación), los fija como variables de entorno del hospedaje y fija el mes de inicio del cobro de cada club desde el panel; nada se cobra hasta entonces
- [ ] T079 Actualizar `specs/017-pago-de-la-plataforma/spec.md` (estado «Implementada», fecha) y marcar el resultado en `checklists/requirements.md`

## Dependencias y orden

- **Fase 1** → **Fase 2** (bloquea todo) → historias.
- **US4 (fase 3)** primero: crea los cobros que las demás historias leen. **US1** y **US2** dependen de
  US4 y de la base; **US2** necesita US1 solo para obtener una referencia (los intentos), así que puede
  construirse con intentos creados por las pruebas.
- **US3** depende de la base (`ServicioSuspensionPorImpago`, T023) y de US2 para probar la
  reactivación por pago; el cambio de `ServicioEstadoClub` (T055) es independiente.
- **US6** puede hacerse en cualquier momento tras la base; T062 es mejor al final de cada fase.
- **US5** depende de US4 (pantalla del club) y de la base; T068 y T069 usan `ReevaluarAsync`.
- Entre archivos: T012 (migración) antes de T013 y T016; T021 antes de T022; T027 antes de T031; T041
  antes de T045; T051 antes de T052.

## Oportunidades de paralelo

- Base: T004 a T011 (enumeraciones, entidades y reglas con sus pruebas), T014, T015, T017, T018, T020 y
  T025 son de archivos distintos.
- US4: T026, T032 y T036 se pueden hacer a la vez una vez existen los DTO y la API.
- US1: T042, T048 y T049 en paralelo al resto de las pantallas.
- US5: T065, T067 y T072.

## Estrategia de implementación

1. **Primer corte útil (MVP)**: fases 1 y 2, US4, US1, US2 y US6: el DESARROLLADOR fija precios y mes de
   inicio, se generan los cobros, el PRESIDENTE paga en el checkout de pruebas y el cobro se confirma
   solo por la notificación. Todavía sin suspensión automática.
2. **Segundo corte**: US3 (suspensión por impago y reactivación). Hasta aquí ningún club se suspende
   solo, por lo que es seguro desplegar el primer corte con el mes de inicio vacío en todos los clubes.
3. **Tercer corte**: US5 (panel del DESARROLLADOR) y el cierre.
4. En cada corte, correr las pruebas de la fase y el recorrido de su parte del quickstart antes de seguir.
