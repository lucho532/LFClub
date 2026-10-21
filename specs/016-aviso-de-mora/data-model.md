# Modelo de datos: aviso fijo de mora

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

**No hay tablas, columnas ni migración nuevas.** El aviso es un valor calculado en cada consulta a
partir de `Cargo`, `Pago` y `AplicacionPago` de la spec 015.

## Entrada

Por cada cargo del jugador (`IRepositorioCargos.ListarConAplicadoAsync`):

| Dato | Origen |
| --- | --- |
| Tipo | `Cargo.Tipo` (solo cuenta `MENSUALIDAD`) |
| Mes | `Cargo.Mes` (primer día del mes que cobra) |
| Pendiente | `ReglaEstadoDeCuenta.Pendiente(Valor, Aplicado)`; `Aplicado` suma solo pagos confirmados no anulados |

Más el instante actual (`IReloj`, UTC) y la cantidad de pagos del jugador en estado `EN_REVISION`.

## Salida: `AvisoDeMoraDto`

| Campo | Tipo | Significado |
| --- | --- | --- |
| `estado` | `EstadoDeAvisoDeMora` | `AL_DIA`, `PENDIENTE_EN_PLAZO` o `EN_MORA` |
| `valorVencido` | decimal | Suma de lo pendiente de las mensualidades vencidas; 0 si no está en mora |
| `mesesVencidos` | int | Cantidad de mensualidades vencidas con pendiente |
| `valorEnPlazo` | decimal | Suma de lo pendiente de las mensualidades que aún no vencen; 0 si no hay |
| `venceEl` | fecha o nulo | Día 5 del mes pendiente más cercano dentro del plazo; nulo si no hay |
| `pagosEnRevision` | int | Pagos del jugador en revisión (informativo; no cambia el estado) |

Los importes son pesos enteros (`decimal`, §16.2).

## Reglas (`ReglaMora`)

1. Solo cuentan los cargos `MENSUALIDAD` con `Mes` y pendiente > 0.
2. El plazo de la mensualidad de un mes M termina el **día 5 de M inclusive**, en hora de Colombia.
   Está vencida desde el día 6 a las 00:00 (UTC−5). No hay reloj del aparato.
3. `EN_MORA` si hay alguna vencida. Si no, `PENDIENTE_EN_PLAZO` si hay alguna con pendiente. Si no,
   `AL_DIA`.
4. `valorVencido` y `mesesVencidos` solo cuentan las vencidas; `valorEnPlazo` y `venceEl`, las demás.
5. Un cargo `OTRO` nunca cuenta (no tiene plazo definido).
6. Un pago en revisión, rechazado o anulado no reduce lo pendiente; solo los confirmados.

## Transiciones (derivadas, sin estado guardado)

| De | A | Por qué |
| --- | --- | --- |
| `AL_DIA` | `PENDIENTE_EN_PLAZO` | La generación mensual crea la mensualidad del mes (día 1) |
| `PENDIENTE_EN_PLAZO` | `EN_MORA` | Empieza el día 6 en Colombia con lo pendiente sin cubrir |
| `PENDIENTE_EN_PLAZO` o `EN_MORA` | `AL_DIA` | El PRESIDENTE registra o confirma un pago que cubre todo lo pendiente |
| `EN_MORA` | `PENDIENTE_EN_PLAZO` | Se paga lo vencido pero queda la mensualidad del mes dentro del plazo |
| `AL_DIA` o `PENDIENTE_EN_PLAZO` | `EN_MORA` | El PRESIDENTE anula un pago y vuelve a haber una mensualidad vencida |

## Qué hace al eliminar una cuenta o un club

Nada: no guarda datos. Al eliminarse la cuenta (spec 014) o el club (§7.4) desaparecen sus cargos y
con ellos el aviso.
