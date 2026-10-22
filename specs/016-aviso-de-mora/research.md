# Investigación: aviso fijo de mora

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

Hallazgos del código de la spec 015 y decisiones. No quedan marcadores sin resolver.

## 1. De dónde sale el estado: un cálculo, no un dato

- **Decisión**: el estado de mora se calcula en cada consulta a partir de los cargos y los pagos
  confirmados no anulados. No se guarda, no hay proceso que lo encienda y no hay migración.
- **Razón**: RF-004 lo exige; el cambio de ámbar a rojo ocurre solo con el paso del día (día 6) y un
  dato guardado exigiría un proceso diario y podría quedar desfasado. La 015 ya hace lo mismo con el
  saldo (`ReglaEstadoDeCuenta`).
- **Alternativas**: columna `EnMora` en el jugador con trabajo diario (rechazada: estado derivado
  guardado, riesgo de desfase); calcularlo en el frontend con el endpoint de la cuenta (rechazada, ver §3).

## 2. La regla: `ReglaMora`

- **Decisión**: regla pura en `Dominio/Reglas`. Recibe los cargos del jugador (tipo, mes, valor,
  aplicado) y el instante UTC; devuelve estado, valor vencido, meses vencidos, valor pendiente dentro
  del plazo y fecha de vencimiento.
- **Plazo**: la mensualidad de un mes vence el **día 5 de ese mes, inclusive**, en hora de Colombia.
  Está vencida desde el día 6 a las 00:00 (UTC−5). Se añade `ReglaMesDeCobro.DiaEnColombia(ahoraUtc)`
  junto a `MesEnCurso`, con el mismo desfase fijo (Colombia no tiene horario de verano).
- **Orden**: si hay alguna mensualidad vencida con pendiente > 0 → `EN_MORA`. Si no, y hay alguna
  pendiente dentro del plazo → `PENDIENTE_EN_PLAZO`. Si no → `AL_DIA`.
- **Qué cuenta**: solo `MENSUALIDAD` con `Mes` no nulo; `OTRO` no cuenta (RF-002). El pendiente es el
  de `ReglaEstadoDeCuenta.Pendiente`, así un abono parcial deja en mora solo lo que falta.
- **Meses vencidos**: cantidad de mensualidades vencidas con pendiente > 0.
- **Razón**: una sola regla comprobable por fechas (días 5, 6, fin de mes, cambio de año), sin leer
  el reloj ni la base de datos.

## 3. Endpoint nuevo en vez de reutilizar `…/cuenta`

- **Hallazgo**: `GET …/jugadores/{usuarioRolId}/cuenta` ya deja pasar al JUGADOR a su propia cuenta
  y devuelve cargos con `mes` y `pendiente`. El frontend podría deducir la mora.
- **Decisión**: operación nueva `GET …/jugadores/{usuarioRolId}/cuenta/aviso`.
- **Razón**: (1) la fecha de corte debe ser la del servidor, no la del aparato (CE-004); duplicar la
  regla en TypeScript abre la puerta a dos reglas distintas; (2) la cuenta carga pagos, medios de pago
  y soportes que el inicio no necesita; (3) el inicio recibe solo lo que muestra.
- **Alternativas**: calcularlo en el frontend con `…/cuenta` (rechazada por las razones anteriores);
  agregar el campo `aviso` a `CuentaJugadorDto` (rechazada: no resuelve el peso ni el acoplamiento del
  inicio con la cuenta completa).

## 4. Quién lo pide y qué responde

- **Decisión**: `[IntegranteDelClub(Rol.JUGADOR)]`. El servicio exige que el jugador consultado sea el
  mismo de la sesión (`jugador.Id == quienPregunta.Id`); si no, `404 no_encontrado`, como la cuenta.
  Retirado: `404` (la cuenta ya lo trata así con `ReglaAccesoACuenta.PuedeVer`). Reutiliza esa regla
  con `Rol.JUGADOR`, para que cuenta y aviso nunca se contradigan.
- **Roles**: PRESIDENTE, DIRECTIVO y ENTRENADOR reciben `403 rol_no_autorizado` (RF-015). El
  DESARROLLADOR no tiene ruta de club (§8). Quien además es familia de un jugador entra con ese rol y
  lo ve (caso de la spec).
- **Razón**: RF-015 y RF-016; es la regla ya probada de la 015.
- **Encabezados**: `Cache-Control: private, no-store`, como la cuenta, para que una recarga no
  muestre un estado viejo.

## 5. Pagos en revisión

- **Decisión**: el DTO trae `pagosEnRevision` (cantidad de pagos del jugador con estado
  `EN_REVISION`). No altera el estado (RF-003, historia 2 escenario 3); el aviso solo lo dice.
- **Cómo**: filtra `IRepositorioPagos.ListarDelJugadorAsync`; son pocos por jugador. Si más adelante
  pesara, se añade un conteo al repositorio.
- **Razón**: ya existe, no se crea una consulta nueva por un detalle informativo.

## 6. Cómo se ve (resuelve las notas de diseño)

- **Decisión**: un componente `AvisoDeMora` con los tonos que ya existen: `error` (rojo), `aviso`
  (ámbar), `exito` (verde) de `Aviso`/`tono-*`. Sin botón de cerrar. Cada estado lleva ícono y texto:
  - rojo: «Estás en mora: debes $X de N mensualidades vencidas» + enlace «Ver mi cuenta»;
  - ámbar: «Tienes la mensualidad de este mes pendiente. Vence el día 5» + enlace;
  - verde: «Estás al día» + enlace discreto a la cuenta.
  Si hay pagos en revisión, una segunda línea: «Tienes N pagos en revisión».
- **Posición**: arriba del inicio, antes de `TarjetaMiJugador`, solo en la rama `JUGADOR` de `DelRol`.
- **Accesibilidad**: el rojo y el ámbar usan `role="status"` (no `alert`, para no interrumpir cada vez
  que se abre el inicio), contraste suficiente en los dos temas y nunca solo color.
- **Error de consulta**: aviso `neutro` «No pudimos consultar el estado de tu cuenta», con
  «Reintentar»; nunca verde por defecto (RF-012). No bloquea el resto del inicio.
- **Carga**: mientras llega, no se muestra nada (ni verde ni rojo) para no parpadear un estado falso.
- **Razón**: componentes y tokens del proyecto, sin CSS nuevo si `tono-aviso` ya cubre el ámbar.

## 7. Actualización

- **Hallazgo**: `useCarga` pide al montar y al cambiar la clave; el inicio se monta al volver a él.
- **Decisión**: la clave incluye el jugador (`${clubId}/aviso/${miUsuarioRolId}`). No se añade
  actualización en tiempo real (fuera de alcance). Cambiar de jugador (hermanos) cambia la clave y
  recarga.

## 8. Pruebas

- **Unitarias** `ReglaMoraPruebas`: sin cargos → verde; mensualidad del mes en el día 1, 5 → ámbar;
  día 6 → rojo (también a las 04:59 UTC del día 6 → todavía día 5 en Colombia, y a las 05:00 → día 6);
  mes anterior sin pagar → rojo aunque el actual esté pagado; abono parcial → solo lo que falta; varios
  meses → un solo estado con el total y la cantidad; `OTRO` no cuenta; fin de año.
- **Integración** `AvisoDeMoraPruebas` con `IReloj` fijo: los tres estados por la API; el pago
  confirmado pasa a verde; el pago anulado vuelve a rojo; el pago en revisión no cambia el estado y
  cuenta en `pagosEnRevision`; otra familia → 404; otro club → 404; PRESIDENTE, DIRECTIVO y ENTRENADOR
  → 403; retirado → 404; sin sesión → 401.
- **Frontend**: Vitest para los tres estados, para el error y para que no haya botón de cerrar.
- **Guardián**: se actualiza la cuenta de `EndpointsDeLaApi` (+1).
