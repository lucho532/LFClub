# Guía de validación: comunicados y campana

**Funcionalidad**: `011-comunicados-y-campana` | **Fecha**: 2026-10-12

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml), el
comportamiento de las pantallas en [contracts/campana.md](contracts/campana.md) y el modelo en
[data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md), en un
  proyecto de Compose aparte para no enviar correos reales ni tocar los datos de desarrollo:
  `docker compose -p lfclub-validacion -f docker-compose.yml -f <override>` con
  `Brevo__Llave` vacía. Aplicación en <http://localhost:5173>, API en <http://localhost:8080>.
- La migración `ComunicadosYCampana` se aplica sola al arrancar la API.

## Datos de partida

Los de la [guía de la 009](../009-entrenamientos-y-partidos/quickstart.md): club A con P, E1 (2014),
E2 (2014 y 2015), D1 (asignado a la 2014), D2 (sin asignar), J1 a J10 en la 2014 (J1 y J2
hermanos, J2 en la 2015), K1 en la 2015, M1 en la 2016; club B con PB. Además:

- La 2014 con dos equipos: **A** (con J1, J3 y J4) y **B** (con J5).
- J11 sin categoría, activo.
- J12 en espera de aprobación (agregado como hermano de J3).
- J10 retirado.

## Bloque 1: publicar (historia 1)

1. Como E1, abrir "Comunicados" y "Nuevo comunicado": solo ofrece "Categorías" y "Equipos de una
   categoría", con la 2014 únicamente; no hay "Todo el club".
2. Como E1, publicar a la 2014 "Entrena mañana" con texto: la lista muestra "Publicado", "Categoría
   2014" y "N familias" con N igual al de jugadores activos de la 2014 (no cuenta a J10).
3. Con la API directa, como E1, `POST …/comunicados` con alcance `CATEGORIAS` y la 2015: 403
   `no_entrena_categoria`. Con alcance `CLUB`: 403.
4. Como E2 (2014 y 2015), publicar a ambas: lo reciben las familias de las dos; J2, hermano en las
   dos, lo recibe una sola vez.
5. Como D2 (sin asignar), publicar a "Todo el club": llega también a J11 (sin categoría); no llega
   a J12 (en espera) ni a J10 (retirado).
6. Como P, publicar al equipo A de la 2014: lo reciben J1, J3 y J4, y nadie más.
7. Publicar con título vacío, con 101 caracteres de título y con 1.001 de texto: 400
   `datos_invalidos`, y el formulario lo marca en el campo.
8. Publicar a una categoría sin jugadores activos: 400 `sin_destinatarios`.
9. Enviar dos veces la misma petición (misma `clave`): el segundo responde 200 con el mismo
   identificador; la lista tiene un solo comunicado.
10. Como J3 (JUGADOR), `POST …/comunicados`: 403 `rol_no_autorizado`. Como el DESARROLLADOR: 404.

## Bloque 2: la campana (historia 2)

1. Como J3 con 2 comunicados sin leer, abrir cualquier pantalla del club: la campana muestra 2 y su
   nombre accesible dice "Comunicados, 2 sin leer".
2. Como M1 (no destinatario de nada), la campana no tiene número y la lista dice "Todavía no hay
   comunicados del club."
3. Como J3, abrir la campana: aparecen del más reciente al más antiguo, con título, autor con su
   rol, fecha y "Sin leer". El nombre del autor abre su ficha básica.
4. Como J3, `GET …/comunicados/recibidos/<id de un comunicado que no le llegó>`: 404, y lo mismo
   con el de otro club.
5. Publicar uno nuevo con la aplicación de J3 abierta y esperar un minuto sin tocar nada: la
   campana sube sola.
6. Cerrar la aplicación (o el navegador) y publicar: no llega ningún aviso al teléfono ni al
   navegador, ni cambia el número sobre el icono. Al abrir, la campana ya muestra el nuevo.
7. Como P o E1, no hay campana en la barra superior; sí existe la entrada "Comunicados" del menú.

## Bloque 3: leído y no leído (historia 3)

1. Como J3 con 3 sin leer, abrir uno: queda "Leído" y la campana baja a 2.
2. "Marcar todos como leídos": la insignia desaparece; recargar la página lo mantiene.
3. Entrar con J3 desde otro navegador: sigue leído.
4. Como J5, que recibió el mismo comunicado: sigue "Sin leer".
5. Como la cuenta de J1 y J2 (hermanos) con un comunicado dirigido a las dos categorías: la
   campana cuenta 1, no 2; al abrirlo desde J1, también queda leído visto desde J2.
6. Con la sesión iniciada con el documento de J1, la lista y el contador solo cuentan lo que llegó
   a J1; marcar leído lo deja leído también para la cuenta entera.

## Bloque 4: ver y retirar (historia 4)

1. Como E1, "Comunicados publicados": solo los suyos. Como P y como D1: los de todo el club, con
   quién los publicó y a quién iban.
2. Como E1, retirar uno suyo y confirmar: pasa a "Retirado", con su nombre y la hora; J3 ya no lo ve y
   el contador baja si estaba sin leer.
3. Como E1, `POST …/comunicados/<id de uno de P>/retiro`: 404. Como D1, sobre uno de P: 403
   `no_retira_comunicado`. Como P, sobre uno de E1: 200.
4. Retirar uno ya retirado: 409 `comunicado_retirado`.
5. Cambiar a J3 de la 2014 a la 2015 (como P): J3 conserva lo que ya recibió y no recibe lo anterior
   dirigido a la 2015. Retirar y reincorporar a J4: vuelve a ver lo que ya le había llegado.
6. Mover a E1 fuera de la 2014: conserva lo que publicó y puede retirarlo, y ya no publica a la
   2014.

## Bloque 5: aislamiento y cierre

1. Como PB (club B), `GET …/clubes/<A>/comunicados`, `…/recibidos`, `…/contador`: 404. Como el
   DESARROLLADOR, 404.
2. En el club suspendido, J3 ve el aviso de incidencia (§7.4); al levantarse, la campana muestra lo
   publicado.
3. Un texto con `<script>alert(1)</script>` y con `**negrita**`: se ve tal cual, sin ejecutarse ni
   formatearse.
4. Un título de 100 caracteres y un texto de 1.000 sin espacios caben en el teléfono a 360 px sin
   desplazamiento horizontal, en tema claro y oscuro.
5. Revisar los registros de la API y de correo: ninguna publicación envió correo (CE-006).
6. Con el contador en más de 99, la insignia dice "99+" y el nombre accesible, "más de 99".
7. `dotnet test` (unitarias e integración) y `npm test` en verde, con el contrato de 108 operaciones
   y 84 de club.
