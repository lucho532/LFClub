# Guía de validación: entrenamientos, partidos, convocatorias y estadísticas

**Funcionalidad**: `009-entrenamientos-y-partidos` | **Fecha**: 2026-10-11

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml) y el
modelo en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md), en un
  proyecto de Compose aparte para no enviar correos reales ni tocar los datos de desarrollo:
  `docker compose -p lfclub-validacion -f docker-compose.yml -f <override>` con
  `Brevo__Llave` vacía. Aplicación en <http://localhost:5173>, API en <http://localhost:8080>.
- La migración `EntrenamientosYPartidos` se aplica sola al arrancar la API.
- Las horas de la guía son de Colombia. Donde se pide "una actividad que ya empezó", programarla
  para hoy con una hora que ya pasó, o con fecha de ayer.

## Datos de partida

Con las guías de la 003 a la [008](../008-fichas-de-personas/quickstart.md), dejar creados:

- **Club A** con las categorías **2013**, **2014**, **2015** y **2016**, activas.
  - **Presidente P**, que no entrena.
  - **Entrenador E1**, asignado solo a la **2014**.
  - **Entrenador E2**, asignado a la **2014** y a la **2015**.
  - **Directivo D1**, asignado a la **2014**; **Directivo D2**, sin asignaciones.
  - En la 2014, diez jugadores: **J1** a **J10**. J1 y J2 son hermanos de la misma cuenta, y J2
    está en la **2015**. En la 2015, además, **K1**. En la 2016, **M1**.
- **Club B** con su presidente **PB** y un jugador.

## Bloque 1: entrenamientos (historia 1)

1. Como E2, "Calendario" → "Nuevo" → tipo "Entrenamiento", categorías 2014 y 2015, mañana de 4:00
   a 5:30 p. m. en "Cancha de Minitas", nota "Traer peto". Solo se ofrecen la 2014 y la 2015.
2. Entrar como J3 (2014), K1 (2015), P, D1 y D2: lo ven en su calendario y en "Próximos
   entrenamientos" del inicio. Como M1 (2016): no aparece.
3. Como E1, intentar la misma creación con la 2015 marcada a mano en la petición: 403
   `no_gestiona_actividad`. Con la 2016: 403. Con fin antes del inicio: 400 con el motivo.
4. Como P, programar uno para la 2016 aunque no la entrena: se guarda.
5. Como E1, abrir el entrenamiento conjunto: se ve, sin botones de cambiar ni cancelar. Como E2,
   cambiar el lugar: J3 y K1 ven el lugar nuevo.
6. Como E2, cancelarlo: J3, K1, P y D2 lo siguen viendo con la etiqueta "Cancelado" escrita.
   Reactivarlo: vuelve a "Programado" para todos. Cancelar uno de ayer e intentar reactivarlo:
   409 `actividad_empezada`.
7. Como PB (club B), pedir `GET /api/clubes/<A>/entrenamientos/<id>`: 404. Con el club B en la
   ruta: 404.
8. Con un club sin entrenamientos, el inicio dice que no hay entrenamientos próximos.

## Bloque 2: convocatoria a un partido (historia 2)

1. Como E1, "Nuevo" → tipo "Partido", categoría 2014, rival "Equipo C", sábado 9:00 a. m., local,
   amistoso. Convocar a J1 y J3 a J8 (siete). El aviso dice que solo los seleccionados lo verán.
2. Con cada una de las diez cuentas de la 2014: los siete convocados lo ven en su calendario con
   rival, fecha, hora, lugar y "Local". J9, J10 y J2 (que es de la 2015): no aparece en el
   calendario, ni en "Partidos", ni en el inicio, y `GET /partidos/<id>` responde 404.
3. Con la cuenta de J1 y J2 (hermanos), cambiar de jugador: con J1 aparece; con J2, no.
4. Como J3, abrir el partido: no hay lista de convocados. En la respuesta de la API tampoco.
5. Como D2: no aparece en "Partidos" ni por identificador (404). Como D1: aparece, con la
   convocatoria, y puede cambiarla. Como P: aparece con sus convocados.
6. Como E2, crear un partido de torneo para 2014 y 2015 sin nombre de torneo: rechazado. Con
   nombre: se guarda; la lista de convocables trae jugadores de las dos categorías y de ninguna más.
7. Como E1, abrir ese partido conjunto: lo ve con su convocatoria, sin poder cambiarla (403 si se
   fuerza por la API).
8. Quitar a J8 de la convocatoria del primero: J8 deja de verlo en todas partes, sin aviso. Agregar
   a J9: lo ve.
9. Cancelar el partido conjunto: sus convocados, E1, E2, D1 y P lo ven "Cancelado"; los demás
   siguen sin verlo. Reactivarlo como E2: vuelve a "Próximo" con la misma convocatoria y las
   respuestas que ya había, y los no convocados siguen sin verlo.

## Bloque 3: resultado y goles (historia 3)

1. Antes de la hora del primer partido, "Registrar resultado" no se ofrece y la API responde 409
   `partido_sin_empezar`.
2. Con el partido ya empezado: el formulario lista solo a los convocados, todos marcados como que
   jugaron. Registrar 3-1, dos goles de J3 y uno de J4.
3. Probar los rechazos: un gol a quien se desmarcó como que no jugó; 4 goles de jugadores con 3 del
   club. Aceptar 2 goles de jugadores con 3 del club (uno sin autor).
4. Como J10 (no convocado), D2 y P: ven el partido jugado con rival, fecha, condición, tipo,
   marcador y goleadores. J10 no ve convocados ni quiénes jugaron.
5. Corregir a 3-2 cambiando un goleador: la ficha de cada uno y los goleadores cambian; aparece
   quién hizo el último cambio y cuándo.
6. Como P, pasar a J3 a la 2013: el partido y sus goles siguen contando para la 2014.
7. Intentar cancelar el partido jugado: 409 `partido_jugado`.

## Bloque 4: asistencia (historia 4)

1. Programar un entrenamiento conjunto 2014 y 2015 para hoy. Como E1, abrir la planilla: solo
   jugadores de la 2014. Marcar presentes y ausentes y guardar.
2. Como E2: la planilla trae los de las dos categorías; corregir a uno de la 2014.
3. Para un entrenamiento de mañana o uno cancelado: 409.
4. Como J3, en sus entrenamientos pasados ve solo "Asististe" o "No asististe", nunca la lista.

## Bloque 5: respuestas de la familia (historia 5)

1. En un entrenamiento futuro de la 2014 y un partido próximo con convocados, tres familias
   responden "Asistirá", "No asistirá" y nada.
2. Como quien entrena: los conteos y los nombres de cada opción y de quienes no respondieron.
3. Ninguna familia ve las respuestas de otras, tampoco en la API.
4. Después de la hora de inicio, o en uno cancelado, responder da 409.
5. Registrar la asistencia real distinta de la respuesta: las dos se conservan.

## Bloque 6: estadísticas y goleadores (historia 6)

1. Abrir la ficha de J3 como P: partidos jugados, goles y porcentaje de asistencia, en total y por
   categoría (2014 y 2013). Contarlos a mano desde los bloques 3 y 4: coinciden.
2. Un jugador sin registros: todo en cero.
3. "Goleadores", categoría 2014: nombres y goles, de mayor a menor, sin otros datos; J3 aparece
   aunque ya está en la 2013.
4. Retirar a J4 (con goles): sigue en goleadores. Si estaba convocado a un partido próximo, sale de
   esa convocatoria.
5. Como J5, pedir `GET /jugadores/<J3>/estadisticas`: 404.

## Bloque 7: aislamiento y pantallas (transversal)

1. Como el DESARROLLADOR, cualquier ruta nueva del club A: 404.
2. A 360 px y en escritorio, en los dos temas: calendario, formulario de actividad, detalle,
   planillas, resultado y goleadores sin desplazamiento horizontal; estados leídos como texto.

## Pruebas automáticas

```bash
cd backend && dotnet test
cd frontend && npm test && npm run lint && npm run build
```

Resultado esperado: todo en verde, con el contrato en 94 endpoints y 70 de club.

## Al terminar

`docker compose -p lfclub-validacion down -v` y borrar el contenedor que mantenía Docker
despierto.
