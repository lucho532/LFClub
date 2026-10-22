# Quickstart: validar el pago del club por el uso de la plataforma

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Contrato**: [contracts/api.yaml](contracts/api.yaml) |
**Datos**: [data-model.md](data-model.md)

Recorrido para comprobar la funcionalidad de punta a punta. Prerrequisitos: la constitución 4.16.0
(ya aplicada) y una cuenta de comercio en el **ambiente de pruebas** (sandbox)
de Wompi.

## Preparación

1. Configurar la API con las claves de la pasarela **por variables de entorno**, nunca en el
   repositorio (los valores salen del panel de comercio de Wompi, en ambiente de pruebas):

   ```text
   Wompi__LlavePublica
   Wompi__SecretoIntegridad
   Wompi__SecretoEventos
   Frontend__UrlBase
   ```

2. Para recibir la notificación en local, exponer la API con un túnel y registrar su dirección
   `…/api/plataforma/pagos/notificaciones` como "URL de eventos" en el panel de pruebas de Wompi.
3. Un club con un PRESIDENTE, un DIRECTIVO, un ENTRENADOR y algunos jugadores. Entrar como
   DESARROLLADOR.
4. El reloj de las pruebas de integración se fija en cada paso (`IReloj`); a mano, se prueba cambiando
   las fechas de `CobroPlataforma` en la base de pruebas.

## Recorrido

1. **Sin precios no pasa nada.** Con las tarifas vacías, forzar la generación: no se crea ningún cobro y
   ningún club se suspende.
2. **Tarifas.** Como DESARROLLADOR, fijar precios por rol (por ejemplo jugador 2.000, entrenador 5.000,
   directivo 5.000, presidente 0) y 5 días de gracia. Un precio negativo se rechaza.
3. **Mes de inicio.** Sin mes de inicio, forzar la generación: el club no recibe cobro. Fijar el mes en
   curso: el siguiente ciclo crea **un** cobro con el detalle y el total esperados; repetir la
   generación no crea un segundo.
4. **Congelado.** Cambiar un precio y añadir un jugador: el cobro ya generado no cambia.
5. **El PRESIDENTE ve y paga.** Entrar como PRESIDENTE: aviso arriba del inicio y sección "Pago de la
   plataforma" con periodo, integrantes por rol, total y "paga hasta el día 5". El DIRECTIVO y el
   ENTRENADOR no ven nada (y las peticiones directas dan `403`).
6. **Pago en la pasarela.** Pulsar "Pagar": se abre el checkout de pruebas con el valor exacto. Pagar con
   una tarjeta o cuenta de prueba aprobada.
7. **Retorno sin confirmación.** Al volver, la pantalla dice "en verificación". Si la notificación aún
   no llegó, el cobro **no** figura pagado.
8. **Confirmación.** Al llegar la notificación el cobro pasa a pagado en pocos segundos, con comprobante
   (fecha, valor, referencia). No se puede pagar otra vez (`409`).
9. **Falsificación.** Enviar a `/api/plataforma/pagos/notificaciones` un evento con firma alterada:
   `401` y el cobro no cambia; con firma válida pero valor distinto o referencia desconocida: `200` y
   el cobro no cambia (aparece en `GET /api/plataforma/notificaciones`). Volver a Wompi con parámetros
   de "aprobado" en la URL de retorno sin notificación: no cambia nada.
10. **Idempotencia.** Reenviar la misma notificación aprobada: no crea un segundo pago.

### Suspensión

11. **Gracia.** Con un cobro pendiente, fijar el reloj al día 5 (hora de Colombia): club activo. Al día 6
    a las 00:00: el siguiente ciclo lo deja `SUSPENDIDO` con motivo `IMPAGO`, sin intervención.
12. **Suspendido.** Entrar como PRESIDENTE: ve que está suspendido por falta de pago, el cobro y el
    botón. Entrar como otro rol: aviso de incidencia temporal, sin ningún dato del cobro.
13. **Reactivación.** Pagar: el club vuelve a `ACTIVO` en menos de un minuto, sin tocar nada; sus datos
    siguen intactos.
14. **Suspensión manual.** Suspender a mano un club con el cobro pagado, pagar o marcar un pago: el club
    **sigue suspendido** hasta que el DESARROLLADOR lo levante.
15. **Levantar a mano.** Con un club suspendido por impago, levantar la suspensión desde el panel: el
    club queda activo, el cobro sigue pendiente y pagable, y tras otro ciclo del trabajo **no** se
    vuelve a suspender. Al mes siguiente, con el nuevo cobro vencido sin pago, sí se suspende.
16. **Varios cobros.** Dos cobros vencidos: pagar uno no reactiva el club; pagar el segundo sí.

### Panel del DESARROLLADOR

17. La lista de clubes muestra el estado de pago (al día, pendiente, vencido, suspendido por impago) y
    lo adeudado; el detalle del club muestra su historial de cobros sin datos personales.
18. **Pago manual** de un cobro: queda marcado como manual, con quién y cuándo, y reactiva si
    corresponde. **Anular** un cobro con motivo: no se puede pagar y no cuenta para suspender.
19. Pagar dos veces el mismo cobro (dos pestañas): el segundo aparece en
    `GET /api/plataforma/notificaciones` como `PAGO_DUPLICADO`.

### Secretos

20. Arrancar la API **sin** claves de Wompi: la sección del PRESIDENTE muestra el cobro con el aviso
    "el pago en línea no está disponible" y sin botón; todo lo demás funciona, el trabajo sigue
    generando y suspendiendo.
21. Revisar las respuestas de la API y el registro de eventos: ninguna contiene llaves ni secretos.

## Autorización

| Operación | PRESIDENTE | DIRECTIVO / ENTRENADOR / JUGADOR | PRESIDENTE de otro club | DESARROLLADOR | Sin sesión |
| --- | --- | --- | --- | --- | --- |
| Ver pago, cobro, iniciar pago | `200`/`201` | `403` | `404` o `403` | sin ruta (`403`) | `401` |
| Tarifas, inicio del cobro, historial, pago manual, anulación, notificaciones | `403` | `403` | `403` | `200` | `401` |
| Notificación de la pasarela | — | — | — | — | `200` con firma válida; `401` sin ella |

## Pruebas automáticas

```text
dotnet test backend --filter "ReglaCobroPlataforma|ReglaSuspensionPorImpago|FirmaWompi|PagoPlataforma|CobrosPlataforma|EndpointsDeLaApi|EsquemaDeEliminacion"
npm --prefix frontend test -- plataforma pagoPlataforma
```
