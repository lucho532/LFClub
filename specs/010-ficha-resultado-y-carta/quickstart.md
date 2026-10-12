# Guía de validación: ficha, resultado y carta por la familia

**Funcionalidad**: `010-ficha-resultado-y-carta` | **Fecha**: 2026-10-11

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml), el plano
de la carta en [contracts/carta.md](contracts/carta.md) y el modelo en
[data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md), en un
  proyecto de Compose aparte para no enviar correos reales ni tocar los datos de desarrollo:
  `docker compose -p lfclub-validacion -f docker-compose.yml -f <override>` con
  `Brevo__Llave` vacía. Aplicación en <http://localhost:5173>, API en <http://localhost:8080>.
- La migración `FichaResultadoYCarta` se aplica sola al arrancar la API.
- Las horas son de Colombia. "Un partido que ya empezó" es uno de hoy con una hora que ya pasó, o
  de ayer.

## Datos de partida

Los de la [guía de la 009](../009-entrenamientos-y-partidos/quickstart.md): club A con P, E1 (2014),
E2 (2014 y 2015), D1 (asignado a la 2014), D2 (sin asignar), J1 a J10 en la 2014 (J1 y J2
hermanos, J2 en la 2015), K1 en la 2015, M1 en la 2016; club B con PB. Además:

- El club A con escudo y colores puestos por P (spec 007).
- J3 con foto; J4 sin foto.
- Un partido **X** de la 2014, de ayer, sin resultado, con J1, J3, J4 y J5 convocados.
- Un partido **Y** de la 2014, de mañana, con J3 convocado.

## Bloque 1: resultado publicado por la familia (historia 1)

1. Como J3, abrir Y: no hay formulario de resultado; `PUT …/partidos/<Y>/resultado/mi-jugador`
   responde 409 `partido_sin_empezar`.
2. Como J6 (no convocado), pedir X por identificador: 404. `PUT …/resultado/mi-jugador`: 404.
3. Como J3, abrir X: se ve el formulario con el marcador y "¿Jugó?" solo para J3; ni la lista de
   convocados ni nadie más. Revisar en la red que la respuesta del detalle no trae convocados.
4. Como J3, intentar marcar goles con "No jugó": 400 `goles_de_quien_no_jugo`. Publicar 2-1, jugó,
   1 gol: el partido queda jugado.
5. Como J4, abrir X: se ve 2-1 publicado, sin poder cambiarlo, y su propio formulario. Enviar un
   marcador por la API: 409 `marcador_ya_publicado`. Indicar que jugó y 2 goles: 400
   `goles_superan_marcador`, y el motivo no nombra a J3. Indicar jugó y 1 gol: se guarda.
6. Como J5, sin indicar nada: J5 no cuenta como que jugó en sus estadísticas.
7. Como J3, cambiar lo suyo a jugó y 0 goles: se guarda; la ficha de J3 lo refleja.
8. Como cualquier jugador del club (J9) y como D2: X aparece jugado 2-1 con los goleadores, sin
   saber quién lo publicó.
9. Como E1, abrir X: dice "Publicado por una familia", con el nombre de J3 y la hora; la planilla
   muestra, por fila, quién indicó cada cosa y cuándo, y J5 "sin indicar".
10. Como E1, corregir a 3-1 y darle 2 goles a J4: el detalle dice "Corregido por quien entrena";
    goleadores y estadísticas cambian al instante. J3, cuya fila E1 no cambió, todavía puede
    cambiar lo suyo.
11. Como J4, intentar cambiar lo suyo: 409 `indicacion_corregida`; la pantalla dice que quien
    entrena lo corrigió y no muestra el formulario.
12. Como E1, bajar el marcador a 1-1 sin quitar goles: 400 `goles_superan_marcador`.
13. Cancelar un partido Z que ya empezó: la familia de un convocado recibe 409
    `actividad_cancelada`.
14. Con la cuenta de J1 y J2: con J1 elegido, X muestra el formulario de J1; con J2 elegido, X se
    ve jugado como para cualquier integrante y `PUT …/resultado/mi-jugador` responde 404 (J2 no
    fue convocado).
15. Dos familias publican a la vez (dos pestañas, mismo segundo): una gana, la otra recibe
    `marcador_ya_publicado` y su pantalla muestra el marcador ganador.

## Bloque 2: la familia corrige la ficha (historia 2)

1. Como J3 (2014), en su ficha, "Corregir nombre y fecha": cambiar un apellido y la fecha de
   nacimiento a un día de 2013. Se guarda; J3 sigue en la 2014. La ficha muestra el último cambio
   con fecha y "la familia".
2. Como P, E1 y en los goleadores: el apellido nuevo aparece en todas partes.
3. Como J3, fecha futura o nombres vacíos: 400 con el motivo.
4. Como la cuenta de J1 y J2, con J1 elegido: cambiar los apellidos de J1; J2 no cambia. Con J1
   elegido, `PUT …/jugadores/<J2>/ficha/identidad`: 404.
5. Como E1 y como D1: `PUT …/ficha/identidad` de J3: 403.
6. Un jugador sin categoría corrige su fecha a un año con categoría: sigue sin categoría.

## Bloque 3: la carta (historia 3)

1. Como E1, abrir la ficha de J3 → "Ver carta": foto, nombre, escudo y fondo con los colores del
   club; sin números ni valoración; rareza escrita.
2. Como E1, poner 70, 65, 80, 72, 60, 88: la valoración es 73 (72,5 redondea hacia arriba). El
   último cambio dice E1.
3. Valores 0, 100 o 50,5, o solo cinco de seis: 400 `datos_invalidos`.
4. Como D1 (asignado a la 2014): puede cambiarlas. Como D2: ve la carta y recibe 403
   `no_entrena_categoria` al cambiarlas. Como E1 sobre K1 (2015): 404. Como J3: 403.
5. Como J3, poner dorsal 10 y posición "Delantero": se ve en la carta. Como E2, cambiar el dorsal a
   7: vale el 7. J5 también puede tener el 7.
6. Como J4: su carta muestra la figura genérica en lugar de la foto.
7. Como J4, `GET …/jugadores/<J3>/carta`: 404. Como PB: 404. Como el DESARROLLADOR: 404.
8. Registrar asistencias de J3 este año en 10 entrenamientos: con 5 presentes, Bronce; con 6,
   Plata; con 9, Oro. Corregir una asistencia y volver a abrir la carta: la rareza cambia y las
   seis estadísticas no.
9. Como P, cambiar los colores y el escudo del club: la carta los muestra al volver a abrirla.
10. Un jugador con nombres y apellidos muy largos: el nombre cabe en la carta, a 360 px y en
    escritorio.
11. Colores del club muy parecidos entre sí (por ejemplo dos amarillos claros): el texto se sigue
    leyendo, en tema claro y oscuro.
12. Recorrer todas las pantallas del club: ninguna lista, tabla ni orden usa las estadísticas o la
    valoración de la carta.

## Bloque 4: exportar la carta (historia 4)

1. Como J3, "Descargar imagen": se descarga `carta-<apellidos>-<nombres>.png` de 750 × 1050 que
   coincide con la carta en pantalla (mismos datos, mismo orden, mismos colores) y no lleva ningún
   otro dato.
2. Como P, descargar la de J4 y la de un jugador retirado: se descargan.
3. Como E1 y como D1: no ven el botón; `GET …/carta/imagen` responde 403.
4. Retirar a J5 desde su cuenta: al entrar ya no accede al club ni a su carta.

## Pruebas automáticas

```text
dotnet test backend/LFClub.sln
npm --prefix frontend test
npm --prefix frontend run lint
npm --prefix frontend run build
```

Esperado: todo en verde, con las de la 001 a la 009 incluidas; `AccesoClubPruebas` con 75
endpoints de club y un contrato de 99.

## Limpieza

`docker compose -p lfclub-validacion down -v` y borrar el contenedor mínimo si se creó.
