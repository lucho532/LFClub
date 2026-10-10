# Guía de validación: Agregar un hermano y elegir el jugador

**Funcionalidad**: `006-agregar-hermano` | **Fecha**: 2026-10-09

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
- Esta funcionalidad tiene una migración (`HermanosDeLaCuenta`), que la API aplica al arrancar.
  No cambia ningún dato existente.

## Datos de partida

Con las guías de la 003, la 004 y la [005](../005-ficha-jugador/quickstart.md), dejar creados:

- **Club A**, con su presidente (presidente A), un **directivo**, un **entrenador** y la categoría
  **2014** activa. La 2016 no existe.
- En el Club A, registrados como JUGADOR: **Ana** (nacida en 2014; su cuenta es la de la familia
  de esta guía) y **Beto** (2015, otra cuenta).
- **Club B**, con otro presidente. Invitar al correo de Ana como Jugador y aceptar con su sesión:
  Ana queda como jugadora en los dos clubes.

Anotar el número de documento de Ana y tener pensados tres números nuevos para sus hermanos
**Luis** (nacido en 2014), **Mara** (2016) y **Nico** (cualquier año).

Para el bloque 5 (RF-034 a RF-040), además:

- En el Club B, la categoría **2013** activa con un equipo, y registrados como JUGADOR, cada uno
  con su propia cuenta: **Caro** (2013), en esa categoría y en el equipo, con algo escrito en su
  ficha, y **Dani** (2013).
- Anotar los documentos de Caro, de Dani y del **presidente B**, y las contraseñas de sus cuentas.

## Pruebas automatizadas

```powershell
dotnet test backend/LaPecosa.sln    # unitarias e integración (la integración necesita Docker)
npm --prefix frontend test
```

Resultado esperado: todas en verde, incluidas las de las specs anteriores sin haberlas cambiado.
La prueba de contrato espera 61 endpoints.

## Recorrido manual

Cada paso indica qué historia de la spec valida.

### 1. La familia agrega un hermano (historia 1)

1. Entrar con el **correo** de la cuenta de Ana y elegir el Club A. **Esperado**: entra
   directamente, sin lista de jugadores (todavía tiene uno solo).
2. Abrir "Mi ficha". **Esperado**: aparece "Agregar un hermano".
3. Abrir el formulario. **Esperado**: pide nombres, apellidos, tipo y número de documento y fecha
   de nacimiento; no pide correo, celular ni contraseña.
4. Confirmar con un dato vacío y después con una fecha futura. **Esperado**: no se crea y se
   explica el motivo.
5. Escribir el documento de Beto. **Esperado**: no se crea; "ese documento ya está registrado en
   el club".
6. Escribir los datos de **Luis** y confirmar. **Esperado**: confirmación de que su ingreso está
   pendiente de aprobación.
7. Agregar también a **Mara** y a **Nico**. **Esperado**: los tres quedan pendientes.
8. Entrar como presidente A y abrir una categoría y "Sin categoría". **Esperado**: Luis, Mara y
   Nico no aparecen en ninguna lista de jugadores.
9. Entrar como presidente A, directivo y entrenador y abrir la ficha de un jugador. **Esperado**:
   ninguno ve "Agregar un hermano".

### 2. La familia elige con cuál jugador continuar (historia 2)

1. Cerrar sesión y entrar con el **correo** de la cuenta de Ana, Club A. **Esperado**: antes de
   cualquier otra pantalla, la lista con Ana (activa) y Luis, Mara y Nico (pendientes de
   aprobación).
2. Elegir a Ana. **Esperado**: la aplicación del club, con el nombre de Ana a la vista; "Mi
   ficha" es la de Ana.
3. Usar "Cambiar de jugador" y elegir a Luis. **Esperado**: solo la pantalla de ingreso
   pendiente, con la opción de cambiar de jugador; ningún dato del club.
4. Con Ana elegida, copiar la dirección de su ficha, cambiar el identificador por el de Beto.
   **Esperado**: no se encuentra.
5. Recargar la página con Ana elegida. **Esperado**: sigue con Ana.
6. Cerrar sesión y volver a entrar con el correo. **Esperado**: vuelve a pedir que elija.
7. Cambiar al Club B en el desplegable. **Esperado**: entra directamente con Ana; en el Club B no
   hay hermanos ni lista.
8. Cerrar sesión y entrar con el **documento de Ana** y la contraseña de la cuenta. **Esperado**:
   entra directamente con Ana; no hay lista ni "Cambiar de jugador".
9. Cerrar sesión y entrar con el **documento de Luis** y la misma contraseña. **Esperado**: solo
   la pantalla de ingreso pendiente, sin opción de cambiar de jugador.

### 3. El PRESIDENTE aprueba o rechaza al hermano (historia 3)

1. Entrar como presidente A y abrir "Ingresos" → sala de espera. **Esperado**: Luis, Mara y Nico,
   cada uno con sus datos, el correo, el celular y el responsable de la cuenta, y "Hermano de
   Ana".
2. Aprobar a **Luis**. **Esperado**: sale de la sala de espera y aparece en la categoría 2014,
   sin equipo.
3. Aprobar a **Mara**. **Esperado**: aparece en "Sin categoría".
4. Rechazar a **Nico**. **Esperado**: desaparece de la sala de espera. En la lista de
   invitaciones sigue la invitación usada con la que entró Ana.
5. Entrar como directivo. **Esperado**: no ve "Ingresos".
6. Entrar con el correo de la cuenta de Ana. **Esperado**: la lista muestra a Ana, Luis y Mara,
   activos; Nico ya no está.
7. Elegir a Luis y abrir "Mi ficha". **Esperado**: la ficha de Luis, con su identidad, el
   contacto de la cuenta y el resto vacío, sin último cambio.
8. Escribir una alergia en la ficha de Luis, cambiar a Ana y abrir su ficha. **Esperado**: la de
   Ana no tiene esa alergia.
9. En la ficha de Luis, cambiar el celular; cambiar a Mara y abrir su ficha. **Esperado**: el
   celular nuevo (es de la cuenta).
10. Desde la ficha de Ana, agregar de nuevo a **Nico** con el mismo documento. **Esperado**: se
    acepta y vuelve a quedar pendiente.
11. Como presidente A, retirar a Luis. Entrar con el correo de la cuenta de Ana. **Esperado**:
    Luis figura como retirado; al elegirlo solo se ve el aviso de retiro y la opción de cambiar;
    Ana y Mara siguen igual.

### 4. Comprobaciones transversales

1. Con una cuenta de un solo jugador (Beto), recorrer el inicio, "Mi categoría" y "Mi ficha".
   **Esperado**: todo igual que antes de esta funcionalidad, sin lista ni "Cambiar de jugador".
2. Repetir los pasos 1.3, 2.1 y 3.1 a 360 px de ancho y en tema claro y oscuro. **Esperado**: sin
   desplazamiento horizontal y con el texto legible.
3. Comparar Swagger (<http://localhost:8080/swagger>) con [contracts/api.yaml](contracts/api.yaml).
   **Esperado**: el endpoint de hermanos existe, y la sesión y la sala de espera traen los campos
   nuevos.

### 5. El documento que ya usa otra cuenta (RF-034 a RF-040)

Este bloque valida el incremento. Los bloques 1 a 4 ya se recorrieron (T022) y no cambian. Como
entonces, conviene levantarlo en un proyecto de Compose aparte, sin llave de Brevo.

1. Con el correo de la cuenta de Ana, Club A, Ana elegida: agregar un hermano con el **documento
   del presidente B**. **Esperado**: no se crea; "ese documento ya está registrado con otra
   cuenta" (historia 1.12).
2. Agregar un hermano con el **documento de Caro**. **Esperado**: queda pendiente y, junto a la
   confirmación, el aviso de que al aprobarse dejará de estar en el otro club (1.11).
3. Agregar otro con el **documento de Dani**. **Esperado**: lo mismo.
4. Entrar como presidente B y abrir la categoría 2013 y la ficha de Caro. **Esperado**: Caro y
   Dani siguen en su categoría y en su equipo; nada cambió (RF-035).
5. Entrar con el **documento de Caro** y la contraseña de **su** cuenta. **Esperado**: entra a su
   club como siempre. Con ese documento y la contraseña de la cuenta de Ana. **Esperado**: no
   entra; el mensaje de datos incorrectos (2.13).
6. Entrar como presidente A y abrir la sala de espera. **Esperado**: en los dos ingresos, el aviso
   de que el documento está activo en otro club y de que aprobarlo lo retirará de allí. No aparece
   el nombre del Club B ni el correo, el celular o el responsable de la otra familia (3.9).
7. **Rechazar** al del documento de Dani. **Esperado**: desaparece de la sala de espera. Como
   presidente B, Dani sigue activo, en su categoría (3.11).
8. Como presidente A, abrir "Aprobar" en el del documento de Caro. **Esperado**: el diálogo repite
   el aviso. Aprobar. **Esperado**: queda en el Club A, en la categoría de su año o sin categoría.
9. Entrar como presidente B. **Esperado**: Caro ya no está en la categoría 2013 ni en el equipo;
   figura en "Retirados" con la fecha y sin nadie en "Lo retiró". Su ficha conserva lo escrito
   (3.10, RF-037).
10. Entrar con el **correo** de la cuenta de Caro. **Esperado**: entra y solo ve el aviso de que
    ya no está en ese club (3.12).
11. Entrar con el **documento de Caro** y la contraseña de **su** cuenta. **Esperado**: no entra;
    datos incorrectos. Con ese documento y la contraseña de la cuenta de Ana. **Esperado**: entra
    al Club A con el hermano recién aprobado, sin lista ni "Cambiar de jugador" (2.12).
12. Repetir los pasos 2 y 6 a 360 px de ancho y en tema claro y oscuro. **Esperado**: los dos
    avisos se leen sin desplazamiento horizontal.

### 6. La familia retira y la reincorporación (RF-041 a RF-043)

Sigue donde termina el bloque 5: Caro está retirada en el Club B y su documento está activo en el
Club A, en el hermano que aprobó el presidente A (aquí, "el hermano de Caro").

1. Como presidente B, en "Retirados", reincorporar a Caro. **Esperado**: no se reincorpora; el
   mensaje dice que el documento está activo en otro club y que primero debe retirarse de allí,
   sin nombrar el Club A ni a la familia de Ana. Caro sigue en "Retirados" (2.13, RF-041).
2. Con el correo de la cuenta de Ana, Club A, elegir al hermano de Caro y abrir "Mi ficha".
   **Esperado**: aparece "Retirar del club". Pulsarlo y **cancelar**. **Esperado**: no cambia
   nada.
3. Pulsarlo de nuevo y confirmar. **Esperado**: solo se ve el aviso de que ya no está en el
   Club A, con "Cambiar de jugador". Al cambiar, Ana y sus otros hermanos siguen igual (2.14,
   RF-042).
4. Como presidente A, abrir "Retirados". **Esperado**: el hermano de Caro figura con la fecha y,
   en "Lo retiró", su propio nombre.
5. Como presidente B, reincorporar a Caro. **Esperado**: se reincorpora en la categoría 2013, sin
   el equipo que tenía (2.15).
6. Como presidente A, reincorporar al hermano de Caro. **Esperado**: no se reincorpora, con el
   mismo mensaje del paso 1. Un mismo documento no queda activo en dos cuentas (CE-013).
7. Entrar con el **documento de Caro** y la contraseña de **su** cuenta. **Esperado**: entra al
   Club B (RF-040: la cuenta con el jugador activo).
8. Como directivo y como entrenador del Club A, abrir la ficha de Ana. Con la cuenta de Beto,
   abrir la suya. **Esperado**: el directivo y el entrenador no ven "Retirar del club"; Beto solo
   lo ve en su propia ficha (RF-043).
9. Repetir los pasos 1 a 3 a 360 px de ancho y en tema claro y oscuro. **Esperado**: el botón, el
   diálogo y el mensaje se leen sin desplazamiento horizontal.

## Comprobaciones directas contra la API

Para lo que la pantalla no deja intentar. `$t` es el token de la cuenta de Ana obtenido con el
correo; `$ana` y `$luis`, sus identificadores (vienen en `GET /api/sesion`).

| Petición | Esperado |
| --- | --- |
| `GET /api/clubes/{A}` sin cabecera | `409 jugador_sin_elegir` |
| La misma, con `X-Jugador-Elegido: $ana` | `200` |
| La misma, con la cabecera de Beto | `404` |
| `GET …/jugadores/$luis/ficha` con la cabecera de Ana | `404` |
| `POST …/jugadores/$luis/hermanos` con la cabecera de Ana | `404` |
| `GET /api/sesion` con un token obtenido con el documento de Ana | El Club A trae `jugadores` vacía y los datos de Ana |
| `GET …/jugadores/$luis/ficha` con ese token y la cabecera de Luis | `404` |
| `POST …/hermanos` como presidente A | `403 rol_no_autorizado` |
| `POST …/hermanos` dos veces con el mismo cuerpo | `201` y después `200`, con el mismo jugador |

Del incremento (bloque 5):

| Petición | Esperado |
| --- | --- |
| `POST …/hermanos` con el documento de Caro, activa en el Club B | `201` con `retiraDeOtroClub: true` |
| La misma, después de que el presidente B retire a Caro | `201` con `retiraDeOtroClub: false` |
| `POST …/hermanos` con el documento del presidente B | `409 documento_en_otra_cuenta` |
| `GET …/ingresos/en-espera` como presidente A | `retiraDeOtroClub` en cada ingreso; el cuerpo no contiene el nombre ni el identificador del Club B, ni el correo de la otra cuenta |
| `POST /api/sesion` con el documento de Caro y la contraseña de la cuenta que no lo tiene activo | `401 credenciales_invalidas`, con el mismo cuerpo que una contraseña equivocada |

Del incremento (bloque 6). `$hermano` es el identificador de otro jugador aprobado de la cuenta de
Ana en el Club A:

| Petición | Esperado |
| --- | --- |
| `POST …/jugadores/$hermano/retiro` con `$t` y la cabecera de Ana | `404 no_encontrado`; el hermano sigue activo |
| `POST …/jugadores/$ana/retiro` con el token de Beto | `404 no_encontrado` |
| `POST …/jugadores/$ana/retiro` como directivo o entrenador | `403 rol_no_autorizado` |
| `POST …/jugadores/{retirado}/reincorporacion` con `$t` | `403 rol_no_autorizado` |
| `POST …/jugadores/{Caro}/reincorporacion` como presidente B, con el hermano de Caro activo en el Club A | `409 documento_activo_en_otro_club`; el cuerpo no contiene el nombre ni el identificador del Club A, ni el correo de la otra cuenta |
