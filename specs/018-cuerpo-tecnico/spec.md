# Especificación de funcionalidad: Cuerpo técnico del club

**Rama de la funcionalidad**: `017-resumen-de-pagos-y-mensualidades` (se sigue en la misma serie de trabajo)

**Creada**: 2026-10-26

**Estado**: Implementada

**Entrada**: Descripción del propietario del proyecto: el PRESIDENTE necesita una pantalla con las personas
del club que no son jugadores (el «cuerpo técnico»): su nombre completo, su rol y las categorías que
entrenan. Cada una se puede abrir para ver todos sus datos (nombre completo, cédula, correo electrónico,
teléfono, categorías y equipos asignados) y, debajo, tarjetas con sus próximos entrenamientos si es
entrenador.

**Plataforma**: LFClub

## Historia de usuario única (P1)

Como PRESIDENTE quiero ver a mi cuerpo técnico y abrir a cada persona para ver sus datos y su agenda,
para saber quién está a cargo de qué categoría y poder contactarla.

### Escenarios de aceptación

1. **Dado** un club con presidente, directivos y entrenadores, **cuando** el PRESIDENTE abre «Cuerpo técnico»,
   **entonces** ve a cada uno con su nombre completo, su rol y las categorías que entrena (con sus equipos),
   ordenados por rol y apellido.
2. **Dado** esa lista, **cuando** abre a una persona, **entonces** ve su nombre completo, su documento, su
   correo, su celular y sus categorías y equipos.
3. **Dado** un entrenador, **cuando** el PRESIDENTE abre su ficha, **entonces** ve debajo tarjetas con sus
   próximos entrenamientos (los que no han terminado y que ve por sus categorías); un directivo o un
   presidente no tiene esa sección.
4. **Dado** cualquier otro rol (DIRECTIVO, ENTRENADOR, JUGADOR) o una persona de otro club, **cuando** pide
   la lista o una ficha, **entonces** recibe 403 o 404 y no se le entrega ningún dato.

## Requisitos

- **RF-001**: La lista y el detalle SOLO los entrega el PRESIDENTE del club de la ruta.
- **RF-002**: La lista incluye a los PRESIDENTE, DIRECTIVO y ENTRENADOR de ingreso aprobado y no retirados;
  nunca a un JUGADOR.
- **RF-003**: El detalle incluye documento, correo y celular: son datos personales, no se guardan en caché
  y solo salen por este endpoint.
- **RF-004**: Los próximos entrenamientos de un entrenador usan la misma regla de visibilidad que su propio
  inicio (spec 009 y 013) y son como máximo cinco.
- **RF-005**: Una persona que es JUGADOR, no está aprobada, fue retirada o es de otro club responde
  `404 no_encontrado`, sin distinguir los casos.

## Fuera de alcance

Editar los datos de una persona, asignar categorías desde esta pantalla o invitar nuevo personal: siguen en
sus pantallas actuales.
