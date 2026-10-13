# Guía de validación: entrenamientos y partidos por equipo y jugadores añadidos

**Funcionalidad**: `013-actividades-por-equipo` | **Fecha**: 2026-10-12

Sirve para comprobar de extremo a extremo que la funcionalidad cumple la spec. No describe cómo
implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml) y el modelo en
[data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- Puesta en marcha como la de la [001](../001-base-multiclub/quickstart.md), en un proyecto de
  Compose aparte (`docker compose -p lfclub-validacion …`) con `Brevo__Llave` vacía, para no enviar
  correos reales ni tocar los datos de desarrollo. Aplicación en <http://localhost:5173>, API en
  <http://localhost:8080>.
- La migración `ActividadesPorEquipo` se aplica sola al arrancar la API.

## Datos de partida

Los de las guías de la [009](../009-entrenamientos-y-partidos/quickstart.md) y la
[012](../012-avisos-de-actividades/quickstart.md), más equipos: club A con P (presidente), E1
(entrena la 2014), E2 (entrena la 2014 y la 2015), D1 (asignado a la 2014), D2 (sin asignar). En la
2014, equipo **A** y equipo **B**: J1 solo en A, J2 en A y B, J3 solo en B, J4 sin equipo. K1 en la
2015 (sin equipos), M1 en la 2016. J10 retirado. Club B con PB.

## Bloque 1: entrenar solo a un equipo (historia 1)

1. Como E1, programar un entrenamiento de la 2014 para el sábado a las 9:00 marcando el **equipo A**.
   Como J1 y J2: lo ven y la campana suma 1. Como J3 (solo B), J4 (sin equipo), K1 y M1: no aparece
   en el calendario, en "próximos", en la campana, y `GET` por su identificador responde 404.
2. Como D1, E1, E2 y P: lo ven. Como D2 (sin asignar): no lo ve.
3. Programar otro para el **equipo B** a la misma hora y otro para **A y B**. J2 ve el de A, el de
   B y el de ambos; el de ambos aparece una sola vez.
4. Programar uno **sin equipos**. J1, J2, J3 **y J4** (sin equipo) lo ven; D2 también.
5. Como J1, responder "asistirá" al del equipo A. Como E1, la planilla lo muestra; J2 no ve la
   respuesta de J1.
6. Como E1, abrir el formulario de la 2016 (sin equipos): no ofrece elegir equipos.
7. Como E2, programar un entrenamiento de la 2014 y la 2015 marcando el equipo A: la audiencia es
   solo la del equipo A (J1 y J2). K1, de la 2015 (sin equipos), no lo ve: para incluirlo hay que
   sumarlo (research §10). La planilla de E2 trae a J1 y J2.
8. Programar tres días a la vez con el equipo A: cada día queda con A y se cambia por separado.

## Bloque 2: convocar por equipo (historia 2)

1. Como E1, crear un partido de la 2014 y abrir la convocatoria: la lista sale agrupada en "Equipo
   A", "Equipo B" y "Sin equipo"; J2 aparece en A y en B.
2. Marcar "todos del equipo A" (J1, J2), desmarcar a J2, marcar a J3 de B y guardar. Convocados:
   J1 y J3. J2 y J4 no ven el partido.
3. Marcar a J2 en A y en B: queda convocado una sola vez.

## Bloque 3: sumar jugadores de otros equipos y categorías (historia 3)

1. Como E1, en el entrenamiento del equipo A, sumar a J3 (equipo B) y a K1 (2015). J3 y K1 lo ven y
   reciben su aviso; ven solo ese entrenamiento: K1 no ve los demás entrenamientos, partidos ni
   jugadores de la 2014, y su categoría y equipos no cambian.
2. Como K1, responder "asistirá". Como E1, la planilla marca a J3 y K1 como "Añadido".
3. Como J1 (de A): el detalle no muestra a nadie añadido ni la lista de convocados.
4. Quitar a K1: deja de verlo; no hay ningún aviso nuevo en su campana ni nada que diga que se le
   quitó.
5. Como D2 (sin asignar) y como J1: intentar sumar a alguien responde 403 / 404 según vea o no la
   actividad. Como E2 (entrena solo la 2015, no la 2014): no ve el entrenamiento y recibe 404.
6. En un partido de la 2014, convocar a K1 (2015). Como K1, ver el partido y responder; como K1,
   publicar el resultado cuando ya empezó. Sus goles cuentan en su carta.
7. Buscar "ma" en el buscador de sumables: máximo 20 filas con solo nombre y categoría; con una
   letra, error de validación; como J1, 403.

## Bloque 4: ver y corregir la audiencia (historia 4)

1. Como E1, cambiar el entrenamiento de A a B: J1 deja de verlo sin aviso; J3 lo ve y recibe un
   aviso nuevo; J2 sigue viéndolo y **no** recibe un aviso repetido.
2. El detalle de quien dirige dice "Para: 2014 · Equipo B" y "Jugadores añadidos".
3. Cancelar y reactivar no cambia la audiencia.

## Bloque 5: casos límite

1. **Cambio de equipo**: mover a J1 del equipo A al B. Ve los entrenamientos de B y deja de ver los
   próximos de A; los de A en que ya tenía asistencia los conserva.
2. **Equipo desactivado**: como P, desactivar el equipo A con un entrenamiento próximo. J1 y J2 lo
   siguen viendo; J3 no; sigue sin pasar a toda la categoría; no se cancela. Cuando se realiza,
   deja de contar como próximo.
3. **Equipo sin jugadores**: dirigir un entrenamiento a un equipo vacío se guarda y solo lo ven
   quienes lo dirigen.
4. **Retirado**: J10 no aparece en el buscador y, si estaba añadido, no cuenta.
5. **Hermanos**: una cuenta con J1 (equipo A) y J3 (equipo B) ve cada entrenamiento solo para el
   hermano que corresponde.
6. **Sin regresión**: una categoría sin equipos se comporta como antes; los datos guardados antes
   de la migración siguen siendo "toda la categoría".

## Aislamiento y contrato

- Con la sesión de PB (club B), cualquier identificador de club A responde 404, incluidas las dos
  rutas nuevas.
- El DESARROLLADOR recibe 404 en las dos rutas nuevas.
- `AccesoClubPruebas` cuenta **111** operaciones en el contrato y **87** de club, con las dos rutas
  nuevas descomentadas. Antes de la tarea que las crea, **109 y 85**.

## Pantallas (360 px y escritorio, tema claro y oscuro)

- El formulario del entrenamiento no tiene desplazamiento horizontal con 4 equipos marcados.
- La pertenencia ("Equipo A", "Añadido", "Equipo desactivado") se lee en texto, no solo en color.
- El buscador de jugadores se maneja con teclado y su estado vacío dice qué hacer.

## Pruebas automáticas

- `dotnet test` de unitarias e integración, con foco en `Reglas`, `Entrenamientos`, `Partidos`,
  `Avisos`, `Categorias` (desactivar equipo) y `Aislamiento`.
- `npm test` del frontend y `scripts/verificar-tamano.ps1` (ningún archivo sobre 250 líneas).
