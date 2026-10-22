# Modelo de datos: pago del club por el uso de la plataforma

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Investigación**: [research.md](research.md)

Una migración (`PagoDeLaPlataforma`): dos columnas en `Clubes` y cuatro tablas nuevas, más una fila
única de configuración. Los importes son pesos enteros (`decimal`, `numeric(18,0)`); las fechas de mes
son `DateOnly` (primer día del mes). Las enumeraciones se guardan como texto.

## Cambios en `Club`

| Columna | Tipo | Significado |
| --- | --- | --- |
| `MesInicioCobro` | `DateOnly?` | Desde qué mes se cobra al club. Nulo = no se cobra. Lo fija el DESARROLLADOR; al crear un club queda en el mes siguiente (`ReglaMesDeCobro.PrimerMesDeCobro`). Los clubes existentes quedan en nulo. |
| `MotivoSuspension` | `MotivoSuspension?` | `MANUAL` o `IMPAGO`. Solo tiene valor si `Estado = SUSPENDIDO`. Al migrar, los suspendidos pasan a `MANUAL`. Cualquier cambio de estado lo reescribe o lo vacía. |

Enumeración nueva `MotivoSuspension { MANUAL, IMPAGO }`.

## `ConfiguracionPlataforma` (una fila, `Id = 1`)

| Columna | Tipo | Regla |
| --- | --- | --- |
| `Id` | int | Siempre 1 (restricción `CHECK`) |
| `PrecioJugador`, `PrecioEntrenador`, `PrecioDirectivo`, `PrecioPresidente` | `decimal?` | ≥ 0. Todos nulos = sin precios, no se genera nada |
| `DiasDeGracia` | int | 0 a 30; por defecto 5 |
| `ActualizadoPorUsuarioId` | `Guid?` | `SetNull` si se elimina la cuenta |
| `ActualizadoEn` | `DateTime` | UTC |

No es de ningún club. Un precio nulo suelto se trata como 0 al calcular, siempre que haya al menos uno
definido.

## `CobroPlataforma`

Índice único `(ClubId, Periodo)` (idempotencia). `ClubId` con `Cascade` (eliminar un club borra sus
cobros, §7.4).

| Columna | Tipo | Significado |
| --- | --- | --- |
| `Id` | Guid | Clave |
| `ClubId` | Guid | Club que paga |
| `Periodo` | `DateOnly` | Primer día del mes que cobra |
| `Estado` | `EstadoCobroPlataforma` | `PENDIENTE`, `PAGADO`, `ANULADO` |
| `Jugadores`, `Entrenadores`, `Directivos`, `Presidentes` | int | Conteo de integrantes activos al generar |
| `PrecioJugador`, `PrecioEntrenador`, `PrecioDirectivo`, `PrecioPresidente` | decimal | Precios vigentes al generar (los nulos, como 0) |
| `Total` | decimal | Suma de conteo × precio por rol; siempre > 0 |
| `GeneradoEn` | `DateTime` | UTC |
| `DiasDeGracia` | int | Los vigentes al generar |
| `SuspendeDesde` | `DateOnly` | Día de Colombia de la generación + días de gracia: **primer día** en que el club se suspende. El último día para pagar es el anterior. |
| `SuspensionOmitida` | bool | Verdadero si el DESARROLLADOR levantó a mano la suspensión por impago estando este cobro vencido: sigue `PENDIENTE` y pagable, pero ya no suspende. Falso por defecto |
| `AnuladoPorUsuarioId`, `AnuladoEn`, `MotivoAnulacion` | `Guid?`, `DateTime?`, `string?` | Solo si `ANULADO`; motivo obligatorio, hasta 300 caracteres |

El detalle y el total **no se modifican** después de generado (RF-004). Solo cambian `Estado` y los
campos de anulación.

## `IntentoPagoPlataforma`

| Columna | Tipo | Significado |
| --- | --- | --- |
| `Id` | Guid | Clave |
| `CobroId` | Guid | `Cascade` |
| `Referencia` | `string` (único) | `LFC-` + identificador sin guiones; viaja a Wompi |
| `Valor` | decimal | El del cobro al crear el intento |
| `CreadoPorUsuarioId` | `Guid?` | `SetNull` |
| `CreadoEn` | `DateTime` | UTC |

No prueba nada: sin notificación verificada, no hay pago.

## `PagoPlataforma` (inmutable)

Índice único `CobroId` (un pago por cobro).

| Columna | Tipo | Significado |
| --- | --- | --- |
| `Id` | Guid | Clave |
| `CobroId` | Guid | `Cascade` |
| `Valor` | decimal | Lo pagado; igual al total del cobro |
| `PagadoEn` | `DateTime` | UTC |
| `Origen` | `OrigenPagoPlataforma` | `PASARELA` o `MANUAL` |
| `Referencia` | `string?` | Referencia del intento (solo `PASARELA`) |
| `TransaccionId` | `string?` | Identificador de la transacción de Wompi (solo `PASARELA`) |
| `MedioDePago` | `string?` | Tipo informado por la pasarela (PSE, NEQUI, CARD…) |
| `RegistradoPorUsuarioId` | `Guid?` | Quién lo marcó (solo `MANUAL`); `SetNull` |

Nunca se edita ni se borra (§13), salvo con el club.

## `NotificacionPasarela`

Índice único `(TransaccionId, EstadoPasarela)` donde `TransaccionId` no es nulo.

| Columna | Tipo | Significado |
| --- | --- | --- |
| `Id` | Guid | Clave |
| `RecibidaEn` | `DateTime` | UTC |
| `Resultado` | `ResultadoNotificacion` | ver abajo |
| `TransaccionId` | `string?` | Nulo si la firma no era válida |
| `EstadoPasarela` | `string?` | `APPROVED`, `DECLINED`… |
| `Referencia` | `string?` | |
| `Valor` | `decimal?` | |
| `CobroId` | `Guid?` | `Cascade` cuando se pudo asociar |

**No guarda el cuerpo completo ni la firma**, solo los campos anteriores.

`ResultadoNotificacion`: `APLICADA`, `REPETIDA`, `NO_APROBADA`, `PAGO_DUPLICADO`,
`PAGO_A_COBRO_ANULADO`, `VALOR_NO_COINCIDE`, `REFERENCIA_DESCONOCIDA`, `FIRMA_INVALIDA`.

## Estados y transiciones del cobro

```text
PENDIENTE ──(notificación APPROVED verificada / marca manual)──▶ PAGADO   (final)
PENDIENTE ──(anulación del DESARROLLADOR, con motivo)──────────▶ ANULADO  (final)
```

`PAGADO` y `ANULADO` son finales: no hay vuelta atrás ni se paga un cobro anulado.

## Reglas (Dominio)

- **`ReglaCobroPlataforma.Calcular(conteo, precios)`** → total = Σ conteo × precio por rol; devuelve
  también el detalle. Sin precios definidos o total 0, no hay cobro.
- **`ReglaSuspensionPorImpago.Evaluar(estado, motivo, cobros, hoyColombia)`** → `Suspender`,
  `Reactivar` o `SinCambio`:
  - `ACTIVO` y algún cobro `PENDIENTE` con `SuspendeDesde <= hoy` y `SuspensionOmitida` falso →
    `Suspender` (motivo `IMPAGO`).
  - `SUSPENDIDO` con motivo `IMPAGO` y ningún cobro `PENDIENTE` con `SuspendeDesde <= hoy` y
    `SuspensionOmitida` falso → `Reactivar`.
  - Cualquier otro caso, incluida toda suspensión `MANUAL` y los clubes dados de baja → `SinCambio`.
- **`ReglaMesDeCobro`** (existente): mes y día de Colombia (UTC−5).

## Estado de pago de un club (calculado, para el panel)

| Estado | Condición |
| --- | --- |
| `SIN_COBRO` | `MesInicioCobro` nulo o futuro |
| `AL_DIA` | Ningún cobro `PENDIENTE` |
| `PENDIENTE` | Cobro pendiente con `SuspendeDesde > hoy` |
| `VENCIDO` | Cobro pendiente con `SuspendeDesde <= hoy` (el trabajo lo suspenderá o ya lo suspendió; con `SuspensionOmitida` se muestra vencido pero no suspende) |
| `SUSPENDIDO_POR_IMPAGO` | Club `SUSPENDIDO` con motivo `IMPAGO` |

Se calcula al consultar; no se guarda.

## Aislamiento

Las tablas no implementan `IPerteneceAClub` (ver research §6). Los repositorios del club filtran por
`ClubId` de forma explícita y las operaciones del DESARROLLADOR solo devuelven conteos, precios,
totales y fechas (nunca datos personales, §8).
