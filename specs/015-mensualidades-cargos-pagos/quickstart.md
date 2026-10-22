# Guía de validación: mensualidades, cargos y pagos

**Rama**: `015-mensualidades-cargos-pagos` | **Plan**: [plan.md](plan.md) | **Contrato**: [contracts/api.yaml](contracts/api.yaml)

Recorrido para comprobar la funcionalidad de punta a punta. Los detalles de tablas están en
[data-model.md](data-model.md). Nada de esto toca una pasarela: el dinero va directo al PRESIDENTE.

## Preparación

1. Levantar la plataforma en un proyecto de Compose aparte, sin la llave real de Brevo y con
   `Limites__AnonimoPorMinuto` alto (ver la memoria de validación local). Al arrancar se aplica la
   migración `MensualidadesYPagos` y corre `TrabajoCargosMensuales`.
2. Con el panel del DESARROLLADOR, crear dos clubes, **Club Uno** y **Club Dos**, cada uno con su
   presidente (**Presidente Uno** y **Presidente Dos**). Ninguno tiene valor de mensualidad todavía
   (los clubes nuevos nacen sin él; solo Valfor F.C. lo trae de la migración).
3. En Club Uno, invitar y registrar a: **Familia Mora** con dos jugadores, *Ana Mora* (ya aprobada) y
   *Beto Mora* (hermano, en espera); **Entrenadora Paz**, asignada a una categoría; y un **Directivo
   Ríos**.
4. Crear una categoría con *Ana*. En Club Dos, registrar **Familia Soto** con un jugador, *Dani Soto*.

Para recorrer el cambio de mes sin esperar, se usa el reloj sustituible de las pruebas de integración
(escenario B); en manual, el mes en curso basta para todo lo demás.

## Escenarios

### A. El club sin valor no genera cargos (historia 2, escenario 6)

1. Entrar como Presidente Uno. El inicio muestra el aviso "Defina el valor de la mensualidad".
2. Entrar como Familia Mora y abrir la cuenta de *Ana*: sin cargos, estado "Al día".
3. Como Presidente Uno, abrir **Pagos → Mensualidad** y fijar `65000`. El aviso desaparece.

### B. Los cargos nacen solos y nadie queda doble (historia 1)

1. Con el valor fijado, la cuenta de *Ana* no tiene cargo del mes en curso: entró este mes y su primer
   cobro es el mes siguiente (clarificación 1). *Beto*, en espera, tampoco tendrá.
2. Ejecutar las pruebas del escenario con el reloj adelantado al mes siguiente:
   `dotnet test backend/pruebas/Integracion --filter "FullyQualifiedName~Pagos.GeneracionDeCargos"`.
   Comprobar: *Ana* recibe una MENSUALIDAD de $65.000; *Beto* (en espera), la Entrenadora y el
   Directivo no; ejecutar la generación dos veces deja un solo cargo; un jugador retirado no recibe.
3. Fijar a *Ana* un valor propio de `50000` (**Mensualidad → Jugadores con valor propio**): el cargo
   ya generado sigue en $65.000 y el del mes siguiente nacerá en $50.000 (historia 2).

### C. El PRESIDENTE registra un pago y deja al día (historia 4)

1. Con un cargo pendiente de *Ana*, entrar como Presidente Uno y abrir su cuenta → **Registrar pago**:
   medio *Efectivo*, valor `65000`, fecha de hoy.
2. Comprobar: estado "Al día", un recibo con número 1, y el resguardo disponible. La lista de
   **Jugadores** muestra "Al día" para *Ana*.
3. Intentar un pago de `70000` para un jugador que debe `65000`: se rechaza diciendo cuánto debe
   (`pago_excede_deuda`). Intentar uno para un jugador sin deuda: `sin_deuda`.
4. Como Directivo Ríos y como Entrenadora Paz, intentar registrar: no pueden (403).

### D. La familia informa una transferencia (historias 5 y 6)

1. Como Presidente Uno, **Medios de pago** → añadir *Nequi, Club Uno, 300 000 0000*. Entrar como
   Familia Mora: la cuenta de *Ana* muestra el medio activo en "Cómo pagar".
2. Con un cargo pendiente, **Informé una transferencia**: entidad *Nequi*, valor, fecha y una foto del
   comprobante. Intentar enviar sin imagen, con un PDF o con una imagen de más de 5 MB: se rechaza
   y no queda nada.
3. Enviar bien. El pago queda "En revisión" y el saldo no cambia. Informar un segundo pago: se acepta
   (sin límite, clarificación 4).
4. Como Presidente Uno, **Pagos en revisión**: aparecen los dos, el más antiguo primero, con el
   soporte a un toque y el contador en el inicio. **Confirmar** el primero: la cuenta pasa a "Al día",
   se genera el recibo y el resguardo. **Rechazar** el segundo con un motivo.
5. Como Familia Mora: ve el rechazo con su motivo y puede informar un pago nuevo; el rechazado no se
   reabre (clarificación 3). Intentar que el PRESIDENTE confirme el rechazado: `pago_no_esta_en_revision`.
6. Entrenadora Paz y Directivo Ríos intentan confirmar o rechazar: no pueden.

### E. Anular un pago (historia 7)

1. Como Presidente Uno, anular el pago confirmado con el motivo "Se registró dos veces". Sin motivo, se
   rechaza.
2. Comprobar: el pago sigue en el historial como anulado, con quién, cuándo y por qué; el saldo vuelve
   a subir; el recibo y el resguardo siguen y el resguardo muestra el sello ANULADO. No existe forma de
   editar ni borrar un pago confirmado.

### F. Recibo y resguardo (historia 8)

1. Abrir el recibo de un pago confirmado: club, jugador, valor, medio y entidad, fecha, quién lo
   confirmó, cargos que cubre, saldo tras el pago y número.
2. Confirmar otro pago: el número es el siguiente (2). En Club Dos, el primer pago confirmado vuelve a
   ser el número 1.
3. **Descargar** el resguardo: es un PNG con el escudo y los colores del club y el mismo número del
   recibo, sin el documento del jugador. En el teléfono, **Compartir** abre el menú del sistema.
4. Un pago en revisión o rechazado no tiene recibo ni resguardo.

### G. Quién ve qué (historias 3 y 9)

1. La Entrenadora Paz no ve **Pagos** en el menú; la lista de **Jugadores** no tiene la columna "Al
   día"; pedir por su dirección `/api/clubes/{clubId}/jugadores/{id}/cuenta`, `/finanzas` o un `/pagos/{id}`
   responde 403.
2. Familia Mora no ve la cuenta de *Dani Soto* (Club Dos) ni la de otra familia: 404, también por
   identificador. Un jugador retirado no accede a su cuenta.
3. Presidente Dos no ve ni cuenta ni pagos ni medios de Club Uno.
4. Como Directivo Ríos: ve cuentas, pagos, soportes y finanzas, y no puede registrar, confirmar,
   rechazar, anular ni cambiar valores ni medios.
5. **Finanzas** (Presidente Uno y Directivo): el recaudo del mes suma solo pagos confirmados no
   anulados; no cuenta los que están en revisión; la cartera incluye a los retirados que deben.
6. El DESARROLLADOR no tiene ninguna ruta de pagos.

### H. Eliminar cuentas y clubes (spec 014)

1. Eliminar la cuenta de Familia Mora: el pago confirmado de *Ana* se conserva sin jugador, con su nombre
   completo y documento; su soporte y su resguardo ya no existen (`404`); sus cargos se fueron.
2. Eliminar la cuenta de Presidente Uno (con otro presidente nombrado antes): en los pagos que registró
   su nombre pasa a "Persona eliminada".
3. Eliminar Club Dos desde el panel: se borran sus cargos, pagos, soportes y medios; Club Uno no cambia.

## Pruebas automáticas

```text
dotnet test backend/pruebas/Unitarias
dotnet test backend/pruebas/Integracion --filter "FullyQualifiedName~Pagos|FullyQualifiedName~Cuenta.Esquema"
npm --prefix frontend test -- pagos
.\scripts\verificar-tamano.ps1
```

Resultado esperado: todo en verde; el contrato `api.yaml` coincide con las rutas de la API
(`AccesoClubPruebas`); ningún archivo escrito a mano supera las 250 líneas.
