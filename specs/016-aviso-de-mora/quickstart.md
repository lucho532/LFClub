# Quickstart: validar el aviso fijo de mora

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Contrato**: [contracts/api.yaml](contracts/api.yaml)

Recorrido para comprobar la funcionalidad de punta a punta. Prerrequisito: la spec 015 construida y las
enmiendas de la §16.7 y la §16.8 aprobadas (plan, nota 1).

## Preparación

- Un club con un PRESIDENTE, un DIRECTIVO, un ENTRENADOR y dos hermanos en una misma cuenta de familia
  (Ana y Beto), más una cuenta de otra familia.
- Valor de la mensualidad definido. El reloj de las pruebas de integración se fija en cada paso
  (`IReloj`); a mano, se prueba cambiando la fecha de los cargos o esperando al día.

## Recorrido

1. **Verde sin deuda.** Ana sin cargos o con todo pagado. Entrar como Ana: arriba del inicio, aviso
   verde «Estás al día». No tiene botón de cerrar.
2. **Ámbar en el plazo.** Día 1 a 5 con la mensualidad de Ana sin pagar. Recargar el inicio: aviso
   ámbar «mensualidad de este mes pendiente; vence el día 5». El estado de cuenta dice «Debe».
3. **Rojo desde el día 6.** El día 6 con lo mismo. Aviso rojo con el valor y «1 mensualidad vencida».
   Probar el día 5 a las 23:59 y el día 6 a las 00:00, hora de Colombia: cambia a las 00:00 sin
   importar la hora del aparato.
4. **Fijo.** En el rojo no hay forma de cerrarlo; recargar, cerrar sesión y volver a entrar lo deja
   igual. Tocarlo lleva a «Ver mi cuenta».
5. **Varios meses.** Dos meses sin pagar: un solo aviso con el total y «2 mensualidades vencidas».
6. **Pago parcial.** Como PRESIDENTE, registrar un pago que no cubre todo: sigue rojo, con lo que
   falta.
7. **A verde.** Registrar el pago que cubre lo vencido (y el del mes en plazo, si lo hay). Recargar el
   inicio de Ana: aviso verde. Si solo queda la del mes en plazo: ámbar.
8. **Anulación.** El PRESIDENTE anula ese pago: el aviso vuelve a rojo.
9. **Pago en revisión.** Ana informa una transferencia: el aviso sigue igual y añade «1 pago en
   revisión». Al confirmarlo el PRESIDENTE, pasa a verde.
10. **Hermanos.** Entrar con Beto (sin deuda): verde; con Ana: rojo. Cambiar de jugador cambia el aviso.
11. **Sin restricciones.** Con el rojo, abrir calendario, entrenamientos, convocatorias y ficha: todo
    funciona igual.
12. **Error.** Cortar la conexión o devolver un error: aviso neutro «No pudimos consultar…» con
    «Reintentar», nunca verde; el resto del inicio sigue funcionando.

## Autorización

Con una API de pruebas, pedir `GET /api/clubes/{clubId}/jugadores/{anaId}/cuenta/aviso`:

| Quién | Esperado |
| --- | --- |
| Ana | `200` |
| Beto (hermano, otro jugador) | `404` |
| Otra familia | `404` |
| Jugador de otro club | `404` |
| Jugador retirado | `404` |
| PRESIDENTE, DIRECTIVO, ENTRENADOR | `403` |
| Sin sesión | `401` |

Comprobar también que el inicio del PRESIDENTE, el DIRECTIVO y el ENTRENADOR no muestra ningún aviso, y
que no llega nada a la campana ni por correo.

## Pruebas automáticas

```text
dotnet test backend --filter "ReglaMora|AvisoDeMora|EndpointsDeLaApi"
npm --prefix frontend test -- AvisoDeMora
```

Resultado esperado: todo en verde; `EndpointsDeLaApi` con 132 operaciones (106 de club).
