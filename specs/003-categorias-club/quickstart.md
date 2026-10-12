# Guía de validación: Categorías del club

**Funcionalidad**: `003-categorias-club` | **Fecha**: 2026-10-08

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml) y el
modelo en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md):
  `docker compose up --build`, aplicación en <http://localhost:5173> y API en
  <http://localhost:8080>. Sin llave de Brevo, los enlaces de los correos se leen con
  `docker compose logs api | Select-String "Correo para"`.

## Datos de partida

Con las guías de la 001 y la [002](../002-ingreso-club/quickstart.md), dejar creados:

- **Club A**, con su presidente (presidente A), un **directivo** y un **entrenador** aprobados.
- En el Club A, aprobados como JUGADOR **antes de crear ninguna categoría**: dos nacidos en 2014
  (Ana y Beto) y uno nacido en 2016 (Caro).
- **Club B**, con otro presidente (presidente B).

## Pruebas automatizadas

```powershell
dotnet test backend/LFClub.sln    # unitarias e integración (la integración necesita Docker)
npm --prefix frontend test
```

Resultado esperado: todas en verde, incluidas las de la 001 y la 002.

## Recorrido manual

Cada paso indica qué historia de la spec valida.

### 1. El presidente crea las categorías (historia 1)

1. Entrar como presidente A y abrir "Categorías". **Esperado**: no hay categorías; "Sin
   categoría" muestra a Ana, Beto y Caro con nombre, apellidos y año de nacimiento, y nada más.
2. Crear con el año vacío, con `abcd` y con el año próximo. **Esperado**: no se crea y se explica
   el motivo.
3. Crear la categoría 2015. **Esperado**: aparece activa, sin jugadores ni entrenadores, y el
   sistema informa de que no entró ninguno.
4. Crear otra vez la 2015. **Esperado**: no se crea y se explica que ya existe.
5. Desactivar la 2015 y confirmar. **Esperado**: sigue en la lista, marcada como inactiva con
   texto. Intentar crearla de nuevo: se explica que existe inactiva y que se puede reactivar.
6. Reactivarla y después borrarla. **Esperado**: desaparece, y se puede volver a crear.
7. Como presidente B, crear la 2015 en el Club B. **Esperado**: se crea; en el Club A no aparece.

### 2. Los jugadores quedan en la categoría de su año (historia 2)

1. Como presidente A, crear la categoría 2014. **Esperado**: informa de que entraron 2 jugadores;
   Ana y Beto están en ella y "Sin categoría" muestra solo a Caro.
2. Intentar borrar la 2014. **Esperado**: se niega y explica que solo se puede desactivar.
3. Intentar desactivar la 2014. **Esperado**: se niega y explica que antes hay que pasar o retirar
   a sus jugadores.
4. Invitar, registrar y aprobar como JUGADOR a Dani, nacido en 2014. **Esperado**: aparece en la
   2014 sin hacer nada más.
5. Como **directivo**, aprobar como JUGADOR a otro nacido en 2014. **Esperado**: también queda en
   la 2014.
6. Aprobar como JUGADOR a Eli, nacida en 2017. **Esperado**: el ingreso se aprueba y queda en "Sin
   categoría".
7. Aprobar a una persona como ENTRENADOR. **Esperado**: no aparece en ninguna categoría ni en "Sin
   categoría". Tampoco aparece nadie que siga en la sala de espera.

### 3. Entrenadores de una categoría (historia 3)

1. Abrir la 2014 y pulsar "Asignar entrenador". **Esperado**: ofrece al entrenador, al directivo y
   al propio presidente A; no ofrece a ningún jugador ni a nadie en espera.
2. Asignar al entrenador y al presidente A. **Esperado**: la categoría muestra a los dos.
3. Crear la 2016 (entra Caro) y asignar allí al mismo entrenador. **Esperado**: entrena las dos.
4. Asignar al directivo a la 2016. **Esperado**: aparece como entrenador; al entrar con su cuenta
   sigue siendo directivo y no tiene ninguna opción para modificar categorías.
5. Retirar al entrenador de la 2016 y confirmar. **Esperado**: deja de aparecer allí y sigue en la
   2014.
6. Llamar a la API para asignar a Ana (jugadora) a la 2014:
   `PUT /api/clubes/{A}/categorias/{2014}/entrenadores/{id de Ana}`. **Esperado**:
   `409 no_asignable_como_entrenador`.
7. Con la sesión del directivo, repetir la llamada con el identificador del entrenador.
   **Esperado**: `403 rol_no_autorizado`.

### 4. Ubicar y cambiar de categoría (historia 4)

1. Como presidente A, ubicar a Eli (2017) en la 2016. **Esperado**: desaparece de "Sin categoría"
   y aparece en la 2016, señalada con su año de nacimiento.
2. Pasar a Beto de la 2014 a la 2016 y confirmar. **Esperado**: aparece solo en la 2016, señalado
   como fuera de su año; el total de la 2014 baja en uno.
3. Crear la categoría 2017. **Esperado**: informa de que no entró ninguno; Eli sigue en la 2016.
4. Llamar a la API para ubicar al entrenador en una categoría. **Esperado**: `409 no_es_jugador`.
5. Con la sesión del presidente B, ubicar a Ana usando los identificadores del Club A.
   **Esperado**: `404`.

### 5. Equipos (historia 5)

1. En la 2014, crear los equipos "A" y "B". Intentar crear "a". **Esperado**: los dos primeros se
   crean vacíos; el tercero se rechaza porque ya existe.
2. Marcar a Ana en "A" y en "B", y a Dani solo en "B". **Esperado**: Ana aparece en los dos y el
   total de la categoría no cambia.
3. Indicar que el entrenador dirige "A". **Esperado**: aparece como entrenador del "A"; el
   presidente A, sin equipo, aparece como entrenador de la categoría en general.
4. Cambiar el nombre de "B" a "Élite". **Esperado**: conserva a sus jugadores.
5. Pasar a Ana a la 2016. **Esperado**: sale de "A" y de "Élite" y queda sin equipo en la 2016.
   Devolverla a la 2014: sigue sin equipo.
6. Llamar a la API para poner a Beto (que está en la 2016) en "A" de la 2014. **Esperado**:
   `409 jugador_de_otra_categoria`.
7. Desactivar "Élite" y confirmar. **Esperado**: deja de mostrarse; Dani sigue en la categoría, sin
   equipo.
8. Crear un equipo "C" y borrarlo. **Esperado**: desaparece. Intentar borrar "A": se niega y
   explica que solo se puede desactivar.

### 6. Retiro y reincorporación (historia 6)

1. Poner a Dani en "A". Como presidente A, pulsar "Retirar" sobre Dani. **Esperado**: pide
   confirmación.
2. Confirmar. **Esperado**: Dani no está en la 2014, ni en "A", ni en "Sin categoría"; aparece en
   "Retirados" con nombre, apellidos, año de nacimiento, quién lo retiró y cuándo.
3. En la ventana de Dani, que seguía abierta, pulsar cualquier enlace. **Esperado**: solo ve el
   aviso de que ya no está en el club. Con su sesión, `GET /api/clubes/{A}` responde
   `403 integrante_retirado`.
4. Como presidente A, invitar al correo de Dani. **Esperado**: se rechaza explicando que la
   persona está retirada y que puede reincorporarla.
5. Como directivo, abrir "Retirados". **Esperado**: ve la lista, sin opciones para reincorporar.
6. Como presidente A, reincorporar a Dani. **Esperado**: vuelve a la 2014, sin equipo; con su
   misma cuenta entra otra vez al club.
7. Llamar a la API de retiro sobre el entrenador. **Esperado**: `409 no_es_jugador`.
8. Retirar a todos los jugadores de la 2016 o pasarlos a otra, y desactivarla. **Esperado**: lo
   permite; el directivo deja de aparecer como su entrenador y, al reactivarla, sigue sin él.

### 7. Lo que ve cada quien (historia 7)

1. Como directivo, abrir "Categorías". **Esperado**: ve todas las categorías, sus equipos,
   entrenadores y jugadores, "Sin categoría" y "Retirados", sin ningún botón para modificar.
2. Como entrenador (asignado solo a la 2014), abrir "Categorías". **Esperado**: ve solo la 2014,
   con todos sus jugadores, estén o no en el equipo que dirige.
3. Con su sesión, pedir `GET /api/clubes/{A}/categorias/{id de la 2017}` y
   `GET /api/clubes/{A}/jugadores/sin-categoria`. **Esperado**: `404` y `403`, sin ningún dato.
4. Como presidente A, retirarle la 2014. Como entrenador, volver a "Categorías". **Esperado**: el
   mensaje de que todavía no tiene categorías asignadas. Volver a asignársela.
5. Entrar con la cuenta de Ana. **Esperado**: en el inicio ve "Categoría 2014", el equipo "A" y el
   nombre y los apellidos del entrenador (equipo A) y del presidente A; ningún correo, celular ni
   documento, y ningún enlace a "Categorías".
6. Con la sesión de Ana, pedir `GET /api/clubes/{A}/categorias` y
   `GET /api/clubes/{A}/categorias/{2014}`. **Esperado**: `403 rol_no_autorizado`.
7. Entrar con la cuenta de un jugador sin categoría. **Esperado**: ve que todavía no tiene
   categoría asignada.

### 8. Aislamiento y estados del club (RF-037 a RF-039)

1. Como presidente B, pedir por identificador una categoría, un equipo y un jugador del Club A, y
   probar cada operación de escritura. **Esperado**: `404` en todo.
2. Como DESARROLLADOR, pedir `GET /api/clubes/{A}/categorias`. **Esperado**: `404`.
3. Como DESARROLLADOR, suspender el Club A. **Esperado**: el presidente A sigue creando categorías
   y moviendo jugadores; el directivo, el entrenador y las familias ven el aviso de incidencia
   temporal.
4. Dar de baja el Club A. **Esperado**: nadie consulta ni gestiona sus categorías. Revertir la
   baja.

### 9. Teléfono y temas (RF-040, CE-015)

Repetir los pasos 1, 3, 5 y 7 con el navegador a 360 px de ancho, en tema claro y en oscuro.
**Esperado**: sin desplazamiento horizontal, con el texto legible en los dos temas y con
"inactiva" y "fuera de su año" indicados con texto, no solo con color.
