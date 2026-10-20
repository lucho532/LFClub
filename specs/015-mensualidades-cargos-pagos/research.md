# Investigación: mensualidades, cargos y pagos

**Rama**: `015-mensualidades-cargos-pagos` | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

Cada punto tiene decisión, razón y alternativas. No quedan `NEEDS CLARIFICATION`.

## 1. Hallazgos del código (lo que el plan da por cierto)

Comprobado leyendo el repositorio el 2026-10-20:

- **No existe ninguna tabla de pagos ni de cargos.** `Club` no tiene valor de mensualidad.
- El integrante es `UsuarioRol` (rol JUGADOR, `EstadoIngreso.APROBADO`, `Activo`); no hay entidad
  Jugador aparte. Su categoría vive en `UsuarioRol.CategoriaId`. Una cuenta puede tener varios jugadores
  (hermanos), cada uno con su fila.
- Los jugadores nacen aprobados en `ServicioAceptacionInvitacion` y `ServicioRegistroConInvitacion`
  (invitación con rol JUGADOR) y pasan de espera a aprobado en `ServicioAprobacionIngreso`. Se
  reincorporan en `ServicioRetiroJugador.ReincorporarAsync`. Son los cuatro sitios donde hay que fijar
  desde cuándo se cobra.
- **No hay ningún `BackgroundService` ni `AddHostedService`** en la API: esta es la primera tarea en
  segundo plano. Hay un solo contenedor de API (Railway), pero el plan no se apoya en eso.
- `IReloj.AhoraUtc` existe y se sustituye en las pruebas. No hay noción de zona horaria de Colombia.
- `IPerteneceAClub` + `ContextoLFClub` aplican el filtro de aislamiento a toda entidad que lo
  implemente; las operaciones de plataforma usan `IgnoreQueryFilters`.
- Las enumeraciones se guardan como texto (`HasConversion<string>()`), no como número.
- Las fotos van en la base (`FotosJugador`, `bytea`), se piden con sesión y se sirven con
  `Cache-Control: private, no-store` y `X-Content-Type-Options: nosniff`. `ValidadorImagen` reconoce
  PNG, JPEG y WebP por su firma y limita a 1 MB; `ArchivoCargado.LimiteDePeticion` ya es de 5 MB.
- `GeneradorImagenCarta` dibuja un PNG con SkiaSharp usando Barlow Condensed incrustada y
  `ContrasteDeColor` para el texto sobre los colores del club. Es el modelo del resguardo.
- `IntegranteDelClub(roles…)` responde 403 a quien no tiene un rol permitido; los servicios responden
  404 a quien pide algo que no es suyo (`ReglaAccesoAFicha`, `ReglaAccesoACarta`).
- `JugadorDeListaDto` dice expresamente que "Al día" es un espacio reservado: el estado de pago llega
  con esta funcionalidad. `TablaDeGrupo.tsx` ya pinta la columna con `TEXTO_AL_DIA`.
- El guardián de esquema de la 014 (`EsquemaDeEliminacionPruebas`) ya prevé la tabla de pagos: toda
  clave foránea hacia `Usuario` o `UsuarioRol` debe ser `Cascade` o `SetNull`, y el mensaje dice que
  una tabla de pagos "debe copiar nombre y documento y usar SetNull" (RF-007 de la 014).
- `AccesoClubPruebas` compara las rutas de la API con las de todos los `specs/*/contracts/api.yaml`;
  este `api.yaml` tiene que listar cada operación exactamente como la expone la API.

## 2. Dinero

**Decisión**: columna `numeric(14,2)` y `decimal` en C#; el validador exige un número entero de pesos
y mayor que cero (RF-014); el frontend lo muestra como `$ 65.000`. Nunca `float` ni `double`
(§16.2).

**Razón**: `numeric(14,2)` es lo habitual y deja espacio a centavos sin cambiar el esquema; la regla
"múltiplo de un peso" es de negocio y se aplica en el validador, no en el tipo.

**Alternativas**: `numeric(14,0)` (cierra una puerta que la §16.2 deja abierta) y `long` de pesos
(rompe "tipo decimal").

## 3. El mes y la zona horaria

**Decisión**: un mes se representa con `DateOnly` del primer día (`2026-11-01`). El mes de cobro se
calcula en la hora de Colombia, **UTC−5 fijo** (`ReglaMesDeCobro`): Colombia no tiene horario de
verano. `CobraDesde` de un jugador es el primer día del mes **siguiente** a su aprobación o
reincorporación en esa hora (clarificación 1).

**Razón**: `TimeZoneInfo` con `America/Bogota` falla si el contenedor Linux no tiene tzdata; un
desfase fijo no falla nunca y es exacto para este país. Un jugador aprobado el 31 de octubre a las
23:30 en Bogotá es del mes de octubre aunque sea noviembre en UTC.

**Alternativas**: UTC (cobra un mes antes a quien entra de noche el último día) y `TimeZoneInfo`
(dependencia del contenedor).

## 4. Generación mensual de cargos

**Decisión**: una operación `GenerarDelMes(ahoraUtc)` en `IRepositorioCargos`, que ejecuta **una
sentencia `INSERT … SELECT … ON CONFLICT DO NOTHING`** sobre todos los clubes. Crea un cargo
MENSUALIDAD del mes en curso por cada integrante que sea jugador del club (`Rol = JUGADOR`,
`EstadoIngreso = APROBADO`, `Activo`), tenga `CobraDesde` ≤ mes en curso y cuyo club tenga valor
definido. El valor es `COALESCE(ValorMensualidadPropio, Club.ValorMensualidad)`; la categoría, la
actual del jugador. Un índice único `(UsuarioRolId, Mes)` para el tipo MENSUALIDAD hace que correr la
sentencia dos veces, o desde dos instancias, no duplique (RF-008, CE-001).

La corre `TrabajoCargosMensuales` (un `BackgroundService`) al arrancar y luego cada 30 minutos, con
`PeriodicTimer`; y también se llama justo después de que el PRESIDENTE fija el valor del club, para que
el aviso "defina el valor" desaparezca y los cargos nazcan en el acto.

**Razón**: la sentencia es idempotente por diseño, así que no hace falta saber si el día 1 se
ejecutó, ni elegir una instancia líder, ni guardar "último mes generado". Si la API estuvo apagada el
día 1, el siguiente arranque genera el mes (supuesto de la spec). El coste es una consulta cada 30
minutos sobre decenas de filas.

**Sin cargos retroactivos**: solo se genera el mes en curso. Un mes que pasó sin valor o sin API no se
recupera (nota 1 del plan).

**Alternativas**: generar al leer el estado de cuenta (la lectura escribe; dos lecturas a la vez
compiten) y un cron externo (otra pieza de despliegue para algo que cabe en el proceso).

## 5. Valor de la mensualidad y jugadores existentes

**Decisión**: `Club.ValorMensualidad` (`numeric`, nulo = sin definir) y
`UsuarioRol.ValorMensualidadPropio` (nulo = usa el del club). El PRESIDENTE cambia el del club y el
propio de uno o varios jugadores; un cambio solo afecta cargos que se generen después (RF-013), porque
cada cargo guarda su valor. La migración deja `ValorMensualidad = 65000` en el club cuyo nombre
normalizado empieza por "valfor" (§16.7) y `CobraDesde` en el primer día del mes de la migración para
los jugadores que ya existen.

**Razón**: la constitución fija 65.000 para Valfor F.C. como dato de configuración; ponerlo en una
migración y no en código respeta §7.2. Los demás clubes no tienen valor hasta que su PRESIDENTE lo
define (RF-011, escenario 6 de la historia 2). Un jugador existente no pasa por ninguno de los cuatro
sitios de entrada, así que necesita un `CobraDesde` de partida.

**Alternativas**: valor por defecto de 65.000 para todos (inventa un precio ajeno) y `CobraDesde`
nulo = cobra siempre (un jugador aprobado a mitad de mes pagaría ese mes, contra la clarificación 1).

## 6. Modelo: cargo, pago, aplicación

**Decisión**: tres entidades y ninguna tabla de saldos.

- `Cargo`: jugador, tipo, mes, valor, categoría de ese momento. No cambia.
- `Pago`: jugador (nulo si la cuenta se elimina), valor, medio, entidad, referencia, fecha, estado,
  quién y cuándo en cada paso, número de recibo, concepto y copia del nombre y del documento.
- `AplicacionPago`: qué parte de un pago cubre qué cargo.

El **saldo** de un cargo es `Valor − Σ aplicaciones de pagos CONFIRMADOS`; el del jugador, la suma. El
**estado de cuenta** (`ReglaEstadoDeCuenta`) es AL_DIA si no queda nada pendiente, PAGO_PARCIAL si
queda algo y ya se aplicó dinero a sus cargos, y DEBE si queda algo y nada se aplicó (RF-002, RF-003).
Un pago ANULADO o RECHAZADO o EN_REVISION no suma, así que anular recalcula solo (RF-019).

**Razón**: no existe un saldo editable a mano (§16.1) porque no existe el campo. Las aplicaciones
conservan qué cubrió cada pago aunque después se anule.

**Alternativas**: columna `Pagado` en el cargo (se puede desincronizar) y estados del cargo (§18).

## 7. Concurrencia y recibo consecutivo

**Decisión**: confirmar, registrar y anular ocurren en `EnTransaccionAsync`. Antes de calcular lo
pendiente se **bloquea la fila del jugador** (`SELECT … FOR UPDATE` sobre `UsuariosRol`), así dos
confirmaciones simultáneas sobre el mismo jugador se ejecutan una detrás de otra y la segunda ve el
saldo ya descontado: "no se cubre más de lo que vale" (caso límite de la spec). El número de recibo
sale de `UPDATE "Clubes" SET "UltimoRecibo" = "UltimoRecibo" + 1 … RETURNING` en la misma
transacción, con índice único `(ClubId, NumeroRecibo)`.

**Razón**: es la misma técnica de bloqueo ordenado que usa la 014 para el presidente único. Un contador
por club en su fila da números seguidos y distintos por club sin tabla extra (RF-024, escenario 4 de la
historia 8).

**Alternativas**: secuencia de PostgreSQL por club (una por club creado) y `MAX + 1` (carrera).

## 8. Cómo se aplica un pago a los cargos

**Decisión**: `ReglaAplicacionDePago.Repartir(cargosPendientes, valor, cargoIdsElegidos?)` es pura:
ordena los cargos por mes, del más antiguo al más reciente (RF-024); si el PRESIDENTE eligió cargos,
usa solo esos; reparte el valor llenando cada cargo hasta su pendiente; si el valor supera lo
pendiente de los cargos disponibles devuelve un error (`pago_excede_deuda`, RF-023, clarificación 2).
La familia **no elige cargos** al informar (nota 6 del plan); el PRESIDENTE los elige, o no, al
confirmar.

**Razón**: probar el reparto sin base de datos; un solo lugar para el tope.

**Alternativas**: elegir cargos al informar (el estado cambia entre el aviso y la revisión).

## 9. Estados del pago y trazabilidad

**Decisión**: `EN_REVISION → CONFIRMADO` o `RECHAZADO`, y `CONFIRMADO → ANULADO`
(`ReglaTransicionPago`); nada más. Un pago registrado por el PRESIDENTE nace CONFIRMADO. Cada paso
guarda quién (cuenta y nombre copiado), cuándo y, en rechazo y anulación, el motivo. Si el PRESIDENTE
corrige el valor al confirmar, se guarda el valor que informó la familia (`ValorInformado`) y el
confirmado. No hay historial de estados (§18): son campos de la propia fila, la excepción que la
§16.3 pide para los pagos.

**Razón**: todo pago "conserva siempre su trazabilidad completa" (§18) sin crear una tabla de eventos.

**Alternativas**: tabla de eventos (sobreingeniería, §19) y reabrir un rechazado (descartado en la
clarificación 3).

## 10. Soporte de pago

**Decisión**: tabla `SoportesPago` (una por pago, `bytea`), validada por `ValidadorSoportePago`
(PNG, JPEG o WebP por firma, hasta **5 MB**, supuesto de la spec); la petición de carga usa
`ArchivoCargado.LimiteDePeticion` (5 MB) más un margen para que sea el validador quien explique el
rechazo. Se sirve solo con sesión, con `Cache-Control: private, no-store` y `nosniff`. Lo ven la familia
dueña del jugador, el PRESIDENTE y el DIRECTIVO (RF-030, `ReglaAccesoACuenta`).

**Razón**: igual que las fotos: sin dirección pública (§14.2), mismo patrón y mismas cabeceras. Aparte
del pago para no cargar bytes en cada consulta.

**Alternativas**: almacenamiento de objetos externo (una dependencia nueva para decenas de imágenes) y
comprimir en el servidor (cambia el comprobante; el PRESIDENTE necesita el original).

## 11. Recibo y resguardo

**Decisión**: el **recibo** es el propio registro del pago confirmado más su número: se consulta como
datos (`GET /pagos/{pagoId}`). El **resguardo** es un PNG que dibuja `GeneradorImagenResguardo` con
SkiaSharp **a demanda**, a partir del pago, y no se guarda. Mide 1080 × 1350, usa el escudo, los
colores y la tipografía del club como la carta, y lleva: club, jugador, valor, medio y entidad,
fecha, concepto (los cargos que cubre), quién confirmó, el estado de cuenta resultante ("Al día" o
saldo pendiente) y el número; con el sello ANULADO en diagonal si el pago se anuló (clarificación y
RF-025). Sin documento del jugador.

El estado de cuenta resultante que figura es el que el pago dejó **al confirmarse** (`SaldoTrasPago`,
guardado en el pago), no el de hoy: el resguardo es una constancia de ese momento.

El resguardo solo se entrega mientras el pago tenga jugador (`UsuarioRolId` no nulo). Al eliminarse la
cuenta, el recibo (con nombre y documento) se conserva y el resguardo deja de existir (spec 014).

**Razón**: nada que guardar ni borrar; el sello ANULADO sale solo del estado; mismo patrón probado de
la carta. El concepto copiado en el pago permite dibujar el resguardo aunque el cargo ya no exista.

**Alternativas**: guardar el PNG (almacena imágenes que se pueden regenerar y habría que reescribirlas
al anular) y un PDF (otra dependencia).

## 12. Autorización

**Decisión**: `IntegranteDelClub(PRESIDENTE, DIRECTIVO, JUGADOR)` en las lecturas; solo
`PRESIDENTE` en registrar, confirmar, rechazar, anular, medios y valor; solo `JUGADOR` en informar. Un
ENTRENADOR recibe 403 en todo (RF-034). Dentro del servicio, `ReglaAccesoACuenta` decide: PRESIDENTE y
DIRECTIVO ven cualquier jugador del club (también retirados); la cuenta de jugador solo los que son de
su cuenta, y un jugador retirado no accede (§14.1); lo ajeno responde 404, no 403. El DESARROLLADOR no
tiene ruta de club (§8).

**Razón**: el mismo reparto que usan ficha y carta; la regla pura se prueba por roles sin base de
datos.

## 13. Eliminación de cuenta y de club

**Decisión**:

- `Cargo.UsuarioRolId`: `Cascade`. Los cargos de una cuenta eliminada se van con ella.
- `Pago.UsuarioRolId`: `SetNull`, y el pago ya tiene copiados el nombre completo, el tipo y número de
  documento y el concepto desde que se crea (RF-007 de la 014, §12.4). Las claves "registrado por",
  "confirmado por", "rechazado por" y "anulado por": `SetNull`, con el nombre copiado; al eliminar a un
  PRESIDENTE, `AnonimizarAutoriaAsync` escribe "Persona eliminada" en esos nombres.
- `SoportePago`: se borra a mano en `RepositorioEliminacionCuenta` para los pagos del jugador
  eliminado; es una clave hacia `Pago`, no hacia la cuenta, así que el guardián no la ve y se cubre con
  una prueba nueva.
- `AplicacionPago.CargoId`: `Cascade` (cae con el cargo); `PagoId`: `Cascade`.
- Todo lo anterior cuelga del club con `Cascade`: eliminar un club borra cargos, pagos, soportes y
  medios (RF-042).

**Razón**: cumple la regla que la 014 dejó escrita para esta tabla y no cambia el guardián de esquema.

## 14. Pantallas

**Decisión**: sigue los tokens y componentes del proyecto; sin estados solo por color.

- **Cuenta del jugador** (todas las familias y PRESIDENTE/DIRECTIVO): estado (texto y color), saldo,
  lista de cargos y de pagos con su medio, "Cómo pagar" con los medios del club y el botón "Informé una
  transferencia" (familia) o "Registrar pago" (PRESIDENTE).
- **Informar transferencia**: entidad (de la lista del club u otra), valor, fecha, referencia y foto
  del comprobante (cámara o galería); muestra el pendiente y rechaza más.
- **Registrar pago**: medio, entidad, valor, fecha, referencia, cargos a cubrir (opcional).
- **Pagos en revisión** (PRESIDENTE; consulta para el DIRECTIVO): lista del más antiguo al más
  reciente con el soporte a un toque, y confirmar (con valor y cargos editables) o rechazar con motivo.
- **Medios de pago y valor de la mensualidad** (PRESIDENTE): lista y alta de medios; valor del club;
  selección de jugadores con valor propio.
- **Recibo y resguardo**: el recibo como pantalla; el resguardo como imagen con "Descargar" y, donde se
  pueda, "Compartir" (Web Share con archivo; Capacitor Share en Android).
- **Finanzas** (PRESIDENTE, DIRECTIVO): recaudo del mes, cartera, totales por categoría y quién debe.
- **Inicio del PRESIDENTE**: tarjeta con el número de pagos en revisión y el aviso "defina el valor de
  la mensualidad" si falta. Es un contador dentro de la aplicación, no un recordatorio de cobro (§16.8).
- **Lista de jugadores**: la columna "Al día" muestra "Al día / Pago parcial / Debe" al PRESIDENTE y al
  DIRECTIVO; al ENTRENADOR no se le devuelve el dato ni se pinta la columna (RF-037).

## 15. Finanzas

**Decisión**: tres consultas agrupadas, sin tabla de totales. **Recaudo del mes**: suma de las
aplicaciones de pagos CONFIRMADOS cuyo `ConfirmadoEn` cae en ese mes de Colombia, por categoría del
cargo. **Cartera**: suma de lo pendiente de todos los cargos del club, incluidos los de jugadores
retirados (RF-041). **Quién debe**: jugadores con pendiente, con su estado. Un pago EN_REVISION o
ANULADO no cuenta (historia 9).

**Razón**: con decenas de jugadores, calcular al pedir es más simple y siempre coincide con los pagos
(CE-005).

## 16. Lista de jugadores

**Decisión**: `IServicioListaJugadores` recibe el rol de quien pregunta y rellena `EstadoCuenta`
solo si es PRESIDENTE o DIRECTIVO, con **una** consulta agrupada por jugador; para el ENTRENADOR el
campo va nulo. `JugadorDeListaDto` gana `EstadoCuenta` (nulo permitido); la spec 007 queda cumplida
sin cambiar su ruta.

**Razón**: evita N+1 y no depende del frontend para ocultarlo (§15).

## 17. Pruebas

- **Unitarias**: `ReglaMesDeCobro` (31 de octubre 23:30 Bogotá, cambio de año), `ReglaEstadoDeCuenta`,
  `ReglaAplicacionDePago` (más antiguo primero, tope, cargos elegidos, abonos), `ReglaAccesoACuenta`
  (cada rol), `ReglaTransicionPago`, `ValidadorPago`, `ValidadorSoportePago`.
- **Integración**: generación y sin duplicados (dos ejecuciones y dos instancias simuladas); jugador en
  espera, retirado, entrenador y directivo sin cargo; primer cargo el mes siguiente; reincorporación;
  club suspendido; club sin valor; cambio de valor sin tocar cargos; saldo y estados; registro del
  PRESIDENTE y rechazo a los demás; informar con soporte; confirmar, rechazar, anular; recibo
  consecutivo por club; resguardo normal y ANULADO; ENTRENADOR 403 en todo; familia ajena 404; soporte
  privado; finanzas sin contar revisión ni anulados; eliminación de cuenta (pago conservado, soporte y
  resguardo fuera) y de club; columna "Al día" real y nula para el ENTRENADOR; guardián de esquema.
- **Frontend (Vitest)**: estados y formato de moneda, formulario de informar (sin imagen no envía),
  revisión, ENTRENADOR sin entrada de menú ni columna.

## 18. Notas al propietario

Ver las notas 1 a 6 del [plan.md](plan.md). La única pendiente de decidir por el propietario es la
**nota 1** (meses sin cobrar no se recuperan).
