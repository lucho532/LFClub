# Investigación: pago del club por el uso de la plataforma

**Rama**: `017-pago-de-la-plataforma` | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

Cada punto tiene decisión, razón y alternativas. No quedan `NEEDS CLARIFICATION`.

## 1. Hallazgos del código (lo que el plan da por cierto)

Comprobado leyendo el repositorio el 2026-10-21:

- **Ya existe el conteo por rol** que pide la facturación: `RepositorioResumenClub` entrega
  `ConteoIntegrantesDto` (jugadores, entrenadores, directivos, presidentes; solo ingresos aprobados y no
  retirados). El cobro lo reutiliza; no se cuenta de otra forma (RF-003).
- **La suspensión ya existe y es solo manual.** `EstadoClub` = `ACTIVO`, `SUSPENDIDO`, `DADO_DE_BAJA`;
  `ServicioEstadoClub.CambiarAsync` aplica `ReglaTransicionEstadoClub`; `ReglaAccesoPorEstado` deja
  entrar solo al PRESIDENTE a un club suspendido. **No hay forma de saber por qué está suspendido** un
  club: falta el motivo (RF-018).
- **`Club` no tiene** mes de inicio de cobro ni motivo de suspensión. La spec 015 ya añadió
  `ValorMensualidad` (de las familias; no tiene relación con este cobro).
- **Un trabajo en segundo plano ya existe**: `TrabajoCargosMensuales` (cada 30 minutos, captura sus
  errores, apagable por configuración `Cargos:TrabajoActivo`). Es el modelo del trabajo nuevo.
- **`ReglaMesDeCobro`** ya calcula el mes y el día de Colombia con desfase fijo UTC−5 sin base de zonas
  horarias. Se reutiliza.
- `IReloj.AhoraUtc` existe y se sustituye en las pruebas.
- **No hay `HttpClient` hacia ningún servicio externo** salvo el correo (Brevo). `OpcionesCorreo` se
  llena desde `IConfiguration` con claves `Brevo:*`; sin llave, el servicio de correo cae a un registro.
  Es el modelo de la configuración de Wompi y de su caída elegante (RF-027).
- Los controladores de club usan `[IntegranteDelClub(roles…)]`; los de plataforma, `[SoloDesarrollador]`.
  Hay un filtro `FiltroOperacionAnonima` para operaciones sin sesión (las públicas); el webhook usa
  esa vía.
- `IntegranteDelClubAttribute` ya deja pasar al PRESIDENTE con el club suspendido: la sección de pago
  funciona en ese estado sin tocar la autorización (RF-020).
- **`EsquemaDeEliminacionPruebas` (014)**: toda clave foránea hacia `Usuario` o `UsuarioRol` debe ser
  `Cascade` o `SetNull`. Las tablas nuevas lo cumplen (quién marcó o anuló va con `SetNull`).
- `AccesoClubPruebas` y `AccesoPlataformaPruebas` comparan las rutas de la API con las de todos los
  `specs/*/contracts/api.yaml`: este contrato debe listar cada operación tal como la expone la API.
- Las enumeraciones se guardan como texto; los importes, como `decimal`; las fechas de mes, como
  `DateOnly` (primer día del mes), igual que `Cargo.Mes`.

## 2. Cómo se integra Wompi: enlace de checkout, sin llamar a la API de Wompi

**Decisión**: usar el **Web Checkout** de Wompi. El servidor arma una dirección de pago firmada
(`https://checkout.wompi.co/p/` con llave pública, moneda `COP`, valor en centavos, **referencia** del
intento, URL de retorno y la **firma de integridad**) y se la entrega al PRESIDENTE; el navegador
abre esa dirección. Wompi cobra y, después, **envía una notificación** (`transaction.updated`) a una
dirección del backend. El servidor **no llama a Wompi** en ningún momento: construye la dirección y
verifica la firma de lo que recibe.

- **Firma de integridad** del enlace: SHA-256 en hexadecimal de la concatenación
  `referencia + valorEnCentavos + moneda + secretoDeIntegridad`.
- **Firma de la notificación**: el cuerpo trae `signature.properties` (por ejemplo
  `transaction.id`, `transaction.status`, `transaction.amount_in_cents`) y `signature.checksum`. Se
  concatenan los valores de esas propiedades tomados de `data`, luego `timestamp`, luego el
  **secreto de eventos**, y se compara el SHA-256 hexadecimal con el `checksum` (comparación de
  tiempo constante). Wompi también manda el mismo valor en la cabecera `X-Event-Checksum`.
- Se confirma solo `status = APPROVED`. `DECLINED`, `VOIDED`, `ERROR` y `PENDING` no confirman nada.

*Detalle a confirmar al implementar*: los nombres exactos de los parámetros del enlace y de las
propiedades firmadas se contrastan con la documentación vigente de Wompi y se cubren con una prueba de
vector conocido (T de pruebas de `FirmaWompi`). Si Wompi cambiara el formato, solo cambia
`PasarelaWompi`.

**Razón**: no hay credenciales de API privadas que guardar ni llamadas salientes que fallen; la
seguridad descansa en dos secretos que viven en configuración (RF-025). Es el modo más simple y
cumple §7.4 (la confirmación es la notificación verificada).

**Alternativas**:
- *Widget embebido de Wompi*: exige cargar un script de terceros dentro de la aplicación (web, escritorio
  y Capacitor), con CSP y problemas en la app Android. Rechazado.
- *API de transacciones con llave privada y tokenización*: más superficie sensible (datos de tarjeta,
  llave privada, PCI). Rechazado.
- *Consultar la transacción en la API de Wompi al recibir la notificación (doble verificación)*:
  añade una dependencia saliente y una llave más. Queda fuera de alcance; la firma ya prueba el origen.
  Se deja anotada como mejora si se detectan notificaciones perdidas.

## 3. Una interfaz de pasarela en Aplicacion

**Decisión**: `IPasarelaDePago` en `LFClub.Aplicacion/Interfaces` con dos operaciones:
`CrearEnlaceDePago(SolicitudDePago)` → dirección (o nulo si la pasarela no está configurada) y
`VerificarNotificacion(cuerpo, cabeceraChecksum)` → `NotificacionVerificada` o nulo. Implementación
`PasarelaWompi` en `LFClub.Infraestructura/Pagos`, con `OpcionesWompi` (llave pública, secreto de
integridad, secreto de eventos, dirección base del checkout, dirección de retorno). Una propiedad
`Disponible` dice si hay configuración completa.

**Razón**: §4 y §6.2 piden que la pasarela quede detrás de una interfaz de Aplicacion; ya hay prueba de
arquitectura para eso. Los servicios se prueban con una pasarela falsa.

**Alternativa**: servicio concreto de Wompi inyectado directamente. Rechazada por §6.2.

## 4. Llaves y secretos (RF-025, RF-026, RF-027)

**Decisión**: claves de configuración `Wompi:LlavePublica`, `Wompi:SecretoIntegridad`,
`Wompi:SecretoEventos`, `Wompi:UrlCheckout` (por defecto la de Wompi) y reutiliza `Frontend:UrlBase`
para el retorno. En producción y desarrollo se fijan como variables de entorno (`Wompi__SecretoEventos`
…). En el repositorio solo hay una lista de las claves, con valores vacíos, en `appsettings.json`
(`"Wompi": { "LlavePublica": "", … }`) y la mención en el README.

- **Ninguna respuesta de la API** incluye llaves ni secretos: el DTO solo lleva la dirección de pago ya
  firmada y `pagoEnLineaDisponible`.
- El registro de eventos (`ILogger`) nunca imprime el cuerpo completo de la notificación ni secretos:
  registra id de transacción, referencia, estado y resultado.
- Sin configuración completa, `Disponible = false`: el resumen lo informa, el botón no aparece, el
  intento de pago responde `503 pago_no_disponible` y todo lo demás sigue (RF-027).
- Una prueba busca en el repositorio cadenas parecidas a llaves de Wompi (`prod_`, `test_`) fuera de los
  archivos de pruebas, y otra comprueba que ningún DTO tiene propiedades de llave o secreto (CE-008).

**Alternativas**: tabla de configuración en la base (rechazada: un secreto en la base se filtra en las
copias); archivo de secretos en el repositorio (prohibido por §7.4).

## 5. Datos nuevos: seis tablas pequeñas y dos columnas

**Decisión** (detalle en [data-model.md](data-model.md)):

- `Club` gana `MesInicioCobro` (fecha, nulo) y `MotivoSuspension` (`MANUAL` o `IMPAGO`, nulo salvo
  suspendido).
- `ConfiguracionPlataforma`: una sola fila con los cuatro precios y los días de gracia.
- `CobroPlataforma`: el cobro de un club y un periodo, con el detalle congelado y estado.
- `IntentoPagoPlataforma`: cada vez que el PRESIDENTE inicia un pago; trae la referencia única que
  viaja a Wompi.
- `PagoPlataforma`: un pago confirmado por cobro (único), inmutable.
- `NotificacionPasarela`: lo recibido, con su resultado, para auditoría e idempotencia.

**Razón**: el spec exige conservar el detalle del cobro tal como se generó (RF-004), registrar pagos
inmutables (RF-014), tolerar repeticiones y desorden (RF-012) y mostrar duplicados al DESARROLLADOR
(RF-015). El motivo de suspensión es la única forma de cumplir RF-018 sin adivinar.

**Migración de lo existente**: los clubes ya `SUSPENDIDO` reciben `MotivoSuspension = MANUAL` (lo único
que existía); `MesInicioCobro` queda vacío en todos los clubes existentes (decisión de la clarificación
2: no se cobra a nadie hasta que el DESARROLLADOR lo fije).

**Alternativas**: guardar el pago dentro del cobro (rechazada: mezcla el cobro con su comprobante y
complica la inmutabilidad); deducir el motivo de suspensión por el estado de los cobros (rechazada: un
club suspendido a mano con el cobro pagado no se distinguiría).

## 6. El cobro no usa el filtro global de club

**Decisión**: `CobroPlataforma`, `IntentoPagoPlataforma`, `PagoPlataforma` y `NotificacionPasarela`
**no implementan `IPerteneceAClub`**. Los repositorios que las leen filtran siempre por `ClubId`
explícitamente (los del club) o son de plataforma (DESARROLLADOR, trabajo en segundo plano, notificación
sin sesión).

**Razón**: el trabajo en segundo plano y la notificación de la pasarela no tienen un club en el
contexto de la petición, y el filtro global "falla cerrado" (sin club, no devuelve nada). `Club` ya
sigue este patrón (la plataforma lo lee sin filtro). El aislamiento (§7.1) se prueba con
`AislamientoDePagosPlataformaPruebas`: un PRESIDENTE no puede leer ni pagar un cobro de otro club.

**Alternativa**: implementar `IPerteneceAClub` e ignorar el filtro en el trabajo y la notificación.
Rechazada: obliga a recordar `IgnoreQueryFilters` en cada consulta de plataforma, que es más propenso a
errores que filtrar por club de forma explícita en un repositorio pequeño.

## 7. Generación del cobro mensual

**Decisión**: `TrabajoCobrosPlataforma` (nuevo `BackgroundService`, mismo patrón que
`TrabajoCargosMensuales`): al arrancar la API y cada 30 minutos llama a
`IServicioCobrosPlataforma.GenerarAsync`, que crea el cobro del mes en curso (hora de Colombia) para
cada club `ACTIVO` o `SUSPENDIDO` con `MesInicioCobro <= mes en curso` que aún no lo tenga, usando el
conteo por rol y los precios vigentes, y solo si el total es mayor que cero y existen precios.
Después llama a `IServicioSuspensionPorImpago.AplicarAsync`.

- **Idempotente y seguro con varias instancias**: índice único `(ClubId, Periodo)`; el servicio
  comprueba antes y, si dos instancias chocan, captura la violación de unicidad y sigue.
- Si el servidor estuvo caído el día 1, el primer ciclo después crea el cobro del mes; su
  `SuspendeDesde` cuenta desde **el día en que se generó** (escenario 6 de la Historia 4).
- Un club cuyo `MesInicioCobro` es futuro no recibe nada; uno cuyo mes de inicio ya pasó **no genera
  cobros retroactivos**: solo el del mes en curso (RF-005a).
- Apagable con `Plataforma:TrabajoActivo` (las pruebas lo apagan, como `Cargos:TrabajoActivo`).

**Razón**: reutiliza un patrón probado y no añade programadores externos.

**Alternativas**: un cron del proveedor de hospedaje (rechazada: no hay uno; el contenedor ya tiene el
trabajo propio); generar el cobro al primer ingreso del PRESIDENTE (rechazada: no suspendería a un
club que nadie abre).

## 8. Suspensión por impago y reactivación

**Decisión**: regla pura `ReglaSuspensionPorImpago` en Dominio: dado el estado del club, su motivo y
sus cobros pendientes con `SuspendeDesde`, dice si debe **suspenderse por impago**, **reactivarse** o
**no cambiar**. Se aplica en dos momentos:

1. **Cada ciclo del trabajo** (suspender): club `ACTIVO` con algún cobro `PENDIENTE` cuyo `SuspendeDesde`
   es hoy o anterior (hora de Colombia) pasa a `SUSPENDIDO` con motivo `IMPAGO` y
   `EstadoCambiadoPorUsuarioId` nulo (lo hizo el sistema).
2. **En la misma transacción** que confirma un pago, marca uno manual o anula un cobro (reactivar): si
   el club está `SUSPENDIDO` con motivo `IMPAGO` y ya no tiene ningún cobro pendiente vencido, pasa a
   `ACTIVO` (inmediato, CE-005). Una suspensión `MANUAL` jamás se toca (RF-018).

`ServicioEstadoClub` guarda `MANUAL` al suspender a mano y vacía el motivo en cualquier otro cambio.
**Levantar a mano una suspensión por impago** (decisión del propietario, 2026-10-21): el club queda
activo hasta el siguiente cobro. `ServicioEstadoClub`, al pasar un club de `SUSPENDIDO` con motivo
`IMPAGO` a `ACTIVO`, marca como `SuspensionOmitida` sus cobros `PENDIENTE` vencidos en la misma
transacción. Esos cobros siguen pendientes, visibles y pagables, pero la regla los ignora; un cobro
posterior vencido sin pago sí suspende.

**Razón**: la regla pura se prueba por tabla; reactivar en la misma transacción del pago evita ventanas
en las que el club ya pagó y sigue suspendido.

**Alternativas**: no marcar nada y dejar que el trabajo vuelva a suspender (rechazada por el
propietario: la acción manual no serviría); guardar un "perdonado hasta" en `Club` (rechazada: un
solo dato por club se pisa si hay varios cobros vencidos). Reactivar solo desde el trabajo (rechazada: hasta 30 minutos de espera; CE-005 pide
menos de 1 minuto).

## 9. Confirmación del pago y la notificación

**Decisión**: `POST /api/plataforma/pagos/notificaciones` (anónimo, sin sesión; la autenticidad es la
firma). Flujo del `ServicioNotificacionPasarela`:

1. Verifica la firma con `IPasarelaDePago`. Sin firma válida: responde `401`, guarda una fila
   `NotificacionPasarela` con resultado `FIRMA_INVALIDA` (sin ids ni cuerpo) y no cambia nada.
2. Busca el intento por la **referencia**. Si no existe: resultado `REFERENCIA_DESCONOCIDA`, responde
   `200` (para que Wompi no reintente indefinidamente) y no cambia nada.
3. Si el valor no coincide con el del cobro: `VALOR_NO_COINCIDE`, `200`, sin cambios.
4. Si el estado no es `APPROVED`: resultado `NO_APROBADA`, `200`, sin cambios.
5. Si ya hay `PagoPlataforma` del cobro: resultado `PAGO_DUPLICADO`, `200`; no crea otro pago. Si la
   misma transacción llega repetida: resultado `REPETIDA`.
6. Si no: crea `PagoPlataforma` (origen `PASARELA`), pasa el cobro a `PAGADO` y reevalúa la
   suspensión, todo en una transacción. Resultado `APLICADA`.

Un índice único `(TransaccionId, EstadoPasarela)` hace a la notificación idempotente aunque Wompi la
envíe varias veces o dos instancias la reciban a la vez. Un cobro `PAGADO` nunca vuelve a `PENDIENTE`
(RF-012): una notificación vieja `DECLINED` solo queda registrada.
Un cobro `ANULADO` que recibe un pago aprobado queda registrado como `PAGO_A_COBRO_ANULADO` para que el
DESARROLLADOR lo revise y devuelva; no se aplica.

**Retorno del navegador**: la dirección de retorno lleva a `/pago-plataforma/retorno`, una pantalla
que **solo consulta** el estado del cobro (cada 3 segundos hasta 30) y muestra "en verificación" hasta
que el cobro figura pagado. No manda nada al servidor que cambie estados (RF-011).

**Alternativas**: confirmar con el retorno (prohibido por §7.4); confirmar desde la pantalla de retorno
consultando a Wompi (descartado en el punto 2).

## 10. El intento de pago

**Decisión**: `POST …/cobros/{cobroId}/intentos` crea un `IntentoPagoPlataforma` con una **referencia
única** (`LFC-` + identificador sin guiones) y devuelve la dirección de checkout firmada para el valor
exacto del cobro. Se puede crear uno nuevo cuando se quiera mientras el cobro esté `PENDIENTE`; los
anteriores no estorban. No se acepta si el cobro está `PAGADO` o `ANULADO` (`409`).

**Razón**: Wompi rechaza reutilizar una referencia ya aprobada; una referencia por intento evita
choques y permite saber qué intento pagó. El valor sale siempre del cobro guardado, nunca del cliente.

## 11. Precios, mes de inicio y panel del DESARROLLADOR

**Decisión**:
- `ConfiguracionPlataforma`: una fila (`Id = 1`) que se crea vacía en el arranque si no existe;
  `GET` y `PUT /api/plataforma/tarifas`. Validación: precios enteros no negativos, días de gracia de 0 a
  30. Sin precios definidos (todos nulos), no se genera nada (Historia 4, escenario 8). Un precio en cero
  es válido (por ejemplo, presidentes gratis).
- `PUT /api/plataforma/clubes/{clubId}/cobro` fija o borra `MesInicioCobro` (un mes `AAAA-MM`; nulo =
  no cobrar).
- `ClubResumenDto` (lista del panel) gana el estado de pago del club y el valor adeudado; el detalle
  del club añade su historial de cobros (`GET …/cobros`), el pago manual y la anulación.
- El DESARROLLADOR no ve datos personales: los cobros solo traen conteos, precios y totales (§8).
- Los duplicados y notificaciones raras se listan en `GET /api/plataforma/notificaciones`
  (últimas 100, filtrable por resultado).

## 12. Qué ve el PRESIDENTE y en qué momento

**Decisión**:
- Una sección nueva "Pago de la plataforma" en el menú del club, **solo para el PRESIDENTE**, con el
  resumen (pendientes, historial, estado) y el botón de pagar.
- Un aviso fijo arriba del inicio del PRESIDENTE cuando hay un cobro pendiente (con la fecha en que se
  suspende) y, si el club está suspendido por impago, un aviso rojo con el cobro y el botón. Los otros
  roles no ven nada (RF-020); el aviso de incidencia temporal de la 001 no cambia.
- El PRESIDENTE de un club suspendido solo puede entrar al inicio y a esta sección (comportamiento
  ya existente de §7.4, ver tarea de comprobación).

*Pendiente de diseño visual*: el lienzo no tiene estas pantallas. Se diseñan con los tokens y
componentes del proyecto (`Aviso`, `Tarjeta`, `Boton`, `EtiquetaEstado`) y las revisa el propietario en
la validación.

## 13. Pruebas y calidad

- Unitarias: `ReglaCobroPlataforma` (total por rol, redondeo, total cero, sin precios),
  `ReglaSuspensionPorImpago` (tabla de estados y motivos), `FirmaWompi` (vector conocido de integridad
  y de evento, firma alterada), `ReglaMesDeCobro` (sin cambios).
- Integración (`WebApplicationFactory` + Testcontainers + `IReloj` y `IPasarelaDePago` falsos): la
  generación (idempotencia, caída el día 1, mes de inicio), el flujo de notificación completo (todos los
  resultados del punto 9), la suspensión y la reactivación (incluida la manual que no se levanta),
  autorización por rol y aislamiento entre clubes, y la ausencia de secretos en las respuestas.
- Actualizar `AccesoClubPruebas` y `AccesoPlataformaPruebas` con las operaciones nuevas.
- Frontend (Vitest): sección del PRESIDENTE, aviso, pantalla de retorno, panel de tarifas y de cobros.
