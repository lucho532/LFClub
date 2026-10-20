# Modelo de datos: mensualidades, cargos y pagos

**Rama**: `015-mensualidades-cargos-pagos` | **Plan**: [plan.md](plan.md) | **Investigación**: [research.md](research.md)

Una migración (`MensualidadesYPagos`): **cinco tablas nuevas** y **cuatro columnas** en tablas
existentes. Toda entidad nueva implementa `IPerteneceAClub` y hereda el filtro de aislamiento. Las
enumeraciones se guardan como texto. Las fechas y horas, en UTC; el mes, como `date` del primer día.

## Columnas nuevas en tablas existentes

| Tabla | Columna | Tipo | Detalle |
| --- | --- | --- | --- |
| `Clubes` | `ValorMensualidad` | `numeric(14,2)` nulo | Valor del club; nulo = sin definir. Mayor que cero y entero de pesos (RF-014). La migración deja 65000 en Valfor F.C. |
| `Clubes` | `UltimoRecibo` | `integer` no nulo, 0 | Contador del recibo consecutivo del club (research §7). |
| `UsuariosRol` | `ValorMensualidadPropio` | `numeric(14,2)` nulo | Valor propio de un jugador que fijó el PRESIDENTE; nulo = usa el del club (RF-012). |
| `UsuariosRol` | `CobraDesde` | `date` nulo | Primer mes en que se le cobra: el primer día del mes siguiente a su entrada, aprobación o reincorporación. Nulo en quien no es jugador. La migración lo fija en el primer día del mes de la migración para los jugadores que ya existen. |

## `Cargos`

Lo que un jugador debe. Se crea una vez y **no cambia** (el valor cobrado queda fijo, RF-013).

| Columna | Tipo | Detalle |
| --- | --- | --- |
| `Id` | `uuid` | PK (versión 7). |
| `ClubId` | `uuid` | FK a `Clubes`, **Cascade**. |
| `UsuarioRolId` | `uuid` | FK a `UsuariosRol`, **Cascade**: los cargos de una cuenta eliminada se van con ella. |
| `Tipo` | texto(20) | `MENSUALIDAD` u `OTRO`. Esta funcionalidad solo crea `MENSUALIDAD`. |
| `Mes` | `date` | Primer día del mes que cobra. Obligatorio en `MENSUALIDAD`. |
| `Valor` | `numeric(14,2)` | Lo cobrado ese mes. Mayor que cero. |
| `CategoriaId` | `uuid` nulo | Categoría del jugador cuando se generó (§13). FK a `Categorias`, **SetNull**. |
| `CreadoEn` | `timestamptz` | Cuándo se generó. |

**Índices**: único `(UsuarioRolId, Mes)` donde `Tipo = 'MENSUALIDAD'` (un cargo por jugador y mes,
CE-001); `(ClubId, UsuarioRolId)` para el estado de cuenta; `(ClubId, Mes)` para finanzas.

## `Pagos`

Dinero recibido de un jugador. Un pago confirmado no se edita ni se borra (§16.3).

| Columna | Tipo | Detalle |
| --- | --- | --- |
| `Id` | `uuid` | PK. |
| `ClubId` | `uuid` | FK a `Clubes`, **Cascade**. |
| `UsuarioRolId` | `uuid` nulo | FK a `UsuariosRol`, **SetNull**: al eliminar la cuenta el pago se conserva sin jugador (spec 014). |
| `JugadorNombre` | texto(200) | Nombres y apellidos del jugador, copiados al crear el pago. |
| `JugadorTipoDocumento` | texto(30) | Copiado al crear el pago. |
| `JugadorNumeroDocumento` | texto(30) | Copiado al crear el pago. |
| `Medio` | texto(20) | `EFECTIVO` o `TRANSFERENCIA`. |
| `Entidad` | texto(40) nulo | Nequi, Bancolombia, Daviplata u otra. Obligatoria en `TRANSFERENCIA`; nula en `EFECTIVO`. |
| `Referencia` | texto(60) nulo | Número de comprobante, opcional. |
| `Fecha` | `date` | Fecha en que se pagó. No futura. |
| `Valor` | `numeric(14,2)` | Lo que cubre el pago (el confirmado). Mayor que cero. |
| `ValorInformado` | `numeric(14,2)` nulo | Lo que informó la familia, solo si el PRESIDENTE lo corrigió al confirmar (RF-022). |
| `Estado` | texto(20) | `EN_REVISION`, `CONFIRMADO`, `RECHAZADO` o `ANULADO`. |
| `Concepto` | texto(300) nulo | Texto de lo que cubre ("Mensualidad octubre 2026; …"), copiado al confirmar. |
| `NumeroRecibo` | `integer` nulo | Consecutivo del club; solo con `CONFIRMADO` o `ANULADO`. |
| `SaldoTrasPago` | `numeric(14,2)` nulo | Lo que le quedaba debiendo al jugador al confirmarse; 0 = "al día". Lo usa el resguardo. |
| `CreadoEn` | `timestamptz` | Cuándo se registró o informó. |
| `CreadoPorUsuarioId` / `CreadoPorNombre` | `uuid` nulo / texto(200) | Quién lo registró o informó. FK a `Usuarios`, **SetNull**; el nombre queda copiado (§13). |
| `ConfirmadoEn` / `ConfirmadoPorUsuarioId` / `ConfirmadoPorNombre` | `timestamptz` nulo / `uuid` nulo / texto | Quién confirmó y cuándo. FK **SetNull**. |
| `RechazadoEn` / `RechazadoPorUsuarioId` / `RechazadoPorNombre` / `MotivoRechazo` | ídem + texto(300) | Quién rechazó, cuándo y por qué. FK **SetNull**. |
| `AnuladoEn` / `AnuladoPorUsuarioId` / `AnuladoPorNombre` / `MotivoAnulacion` | ídem + texto(300) | Quién anuló, cuándo y por qué. FK **SetNull**. |

**Índices**: único `(ClubId, NumeroRecibo)` donde `NumeroRecibo` no es nulo; `(ClubId, Estado)` para la
lista en revisión; `(UsuarioRolId)` para el estado de cuenta.

**Restricciones** (validadas en el servicio y con `CHECK` donde es barato): `Valor > 0`; `Entidad` no
nula si `Medio = 'TRANSFERENCIA'`; `NumeroRecibo` no nulo si el estado es `CONFIRMADO` o `ANULADO`;
`MotivoRechazo` y `MotivoAnulacion` no vacíos cuando hay rechazo o anulación.

### Transiciones (`ReglaTransicionPago`)

```text
(informado por la familia) ─▶ EN_REVISION ─▶ CONFIRMADO ─▶ ANULADO
                                         └─▶ RECHAZADO
(registrado por el PRESIDENTE) ─▶ CONFIRMADO
```

`RECHAZADO` y `ANULADO` son finales: no se reabren ni se editan. Un pago `EN_REVISION` no tiene
aplicaciones, recibo ni resguardo.

## `AplicacionesPago`

Qué parte de un pago cubre qué cargo. Se crean al confirmar y no cambian; si el pago se anula, siguen
ahí y el cálculo las ignora.

| Columna | Tipo | Detalle |
| --- | --- | --- |
| `PagoId` | `uuid` | PK compuesta con `CargoId`. FK a `Pagos`, **Cascade**. |
| `CargoId` | `uuid` | FK a `Cargos`, **Cascade**. |
| `ClubId` | `uuid` | FK a `Clubes`, **Cascade**. |
| `Valor` | `numeric(14,2)` | Lo que cubre de ese cargo. Mayor que cero. |

La suma de las aplicaciones de un pago es su `Valor`; la de las aplicaciones confirmadas de un cargo
nunca supera su `Valor` (lo garantiza el servicio con el bloqueo del jugador, research §7).

## `SoportesPago`

La imagen que la familia sube como comprobante de una transferencia.

| Columna | Tipo | Detalle |
| --- | --- | --- |
| `PagoId` | `uuid` | PK y FK a `Pagos`, **Cascade**. Uno por pago. |
| `ClubId` | `uuid` | FK a `Clubes`, **Cascade**. |
| `Contenido` | `bytea` | Los bytes. Hasta 5 MB. |
| `TipoContenido` | texto(20) | `image/png`, `image/jpeg` o `image/webp`, según la firma. |
| `GuardadaEn` | `timestamptz` | Cuándo se subió. |

Se borra a mano al eliminar la cuenta del jugador (research §13). Nunca tiene dirección pública.

## `MediosPagoClub`

Por dónde el club recibe pagos; lo publica el PRESIDENTE y lo ven las familias.

| Columna | Tipo | Detalle |
| --- | --- | --- |
| `Id` | `uuid` | PK. |
| `ClubId` | `uuid` | FK a `Clubes`, **Cascade**. |
| `Entidad` | texto(40) | Nequi, Bancolombia, Daviplata u otra. Obligatoria. |
| `Titular` | texto(100) | Nombre del titular. Obligatorio. |
| `Numero` | texto(60) | Número de cuenta, celular o llave. Obligatorio. |
| `Nota` | texto(200) nulo | Por ejemplo, "Ahorros" o "Poner el nombre del jugador". |
| `Activo` | `boolean` | Falso = no se muestra a las familias, sin borrarlo. |
| `CreadoEn` | `timestamptz` | Cuándo se creó. |

Borrar un medio no toca los pagos hechos con esa entidad: el pago guarda el nombre de la entidad como
texto.

## Cálculos (no se guardan)

| Dato | Cálculo |
| --- | --- |
| Pendiente de un cargo | `Cargo.Valor − Σ AplicacionPago.Valor` de pagos `CONFIRMADO` |
| Saldo del jugador | Σ pendientes de sus cargos |
| Estado de cuenta | `AL_DIA` si saldo = 0; `PAGO_PARCIAL` si saldo > 0 y hay algo aplicado; `DEBE` si saldo > 0 y nada aplicado |
| Recaudo del mes | Σ aplicaciones de pagos `CONFIRMADO` cuyo `ConfirmadoEn` cae en el mes (hora de Colombia) |
| Cartera | Σ saldos de todos los jugadores del club, también retirados |
| Pagos en revisión | Cuenta de `Pagos` con `Estado = 'EN_REVISION'` |

## Qué pasa con cada tabla al eliminar

| Hecho | Cargos | Pagos | Soportes | Aplicaciones | Medios |
| --- | --- | --- | --- | --- | --- |
| Se elimina la cuenta de un jugador | Se borran (cascada) | **Se conservan**, sin jugador, con nombre y documento copiados | Se borran a mano | Se borran con los cargos | No cambian |
| Se elimina la cuenta de un PRESIDENTE | No cambian | Se conservan; su nombre pasa a "Persona eliminada" donde figura como autor | No cambian | No cambian | No cambian |
| Se retira un jugador | Se conservan | Se conservan | Se conservan | Se conservan | No cambian |
| Se elimina el club | Se borran | Se borran | Se borran | Se borran | Se borran |

El **guardián de esquema** (`EsquemaDeEliminacionPruebas`) no necesita cambios de regla: todas las
claves nuevas hacia `Usuario` y `UsuarioRol` son `Cascade` o `SetNull`. Se añade una prueba que exige
que ningún `SoportePago` sobreviva a la eliminación de la cuenta de su jugador.

## DTOs (resumen; el detalle está en [contracts/api.yaml](contracts/api.yaml))

- `MensualidadDto`: `valorClub` (nulo si falta), `jugadoresConValorPropio[]`.
- `MedioPagoDto`: `id`, `entidad`, `titular`, `numero`, `nota`, `activo`.
- `CuentaJugadorDto`: `jugador`, `estado`, `saldo`, `cargos[]`, `pagos[]`, `mediosPago[]`, `puedeRegistrar`,
  `puedeInformar`.
- `CargoDto`: `id`, `tipo`, `mes`, `valor`, `pagado`, `pendiente`.
- `PagoDto`: `id`, `estado`, `valor`, `medio`, `entidad`, `referencia`, `fecha`, `numeroRecibo`,
  `concepto`, `saldoTrasPago`, `tieneSoporte`, `tieneResguardo`, y los datos de cada paso
  (quién, cuándo, motivo).
- `PagoEnRevisionDto`: `PagoDto` más el jugador y los cargos pendientes.
- `FinanzasDto`: `recaudoMes`, `carteraPendiente`, `porCategoria[]`, `jugadoresQueDeben[]`,
  `pagosEnRevision`.
- `JugadorDeListaDto` (cambia): gana `estadoCuenta` (`AL_DIA`, `PAGO_PARCIAL`, `DEBE` o nulo).
- El inicio del PRESIDENTE no cambia `ResumenClubDto`: la tarjeta de pagos usa `total` de la lista de
  pagos en revisión y `valorClub` nulo de la mensualidad para el aviso "defina el valor".
