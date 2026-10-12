# Guía de validación: Ficha del jugador

**Funcionalidad**: `005-ficha-jugador` | **Fecha**: 2026-10-09

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
- Esta funcionalidad tiene una migración (`FichaDelJugador`), que la API aplica al arrancar. Los
  jugadores que ya existan quedan con la ficha vacía y sin documentos.
- Tres archivos de prueba: un PDF pequeño, una foto JPEG o PNG y un archivo que no sea ni lo uno ni
  lo otro (por ejemplo un `.txt` renombrado a `.pdf`). Para el límite de tamaño, un PDF o una
  imagen de más de 10 MB.

## Datos de partida

Con las guías de la 003 y la [004](../004-invitacion-con-rol/quickstart.md), dejar creados:

- **Club A**, con su presidente (presidente A), un **directivo**, un **entrenador** y las
  categorías **2014** y **2015** activas. El entrenador, asignado solo a la 2014.
- En el Club A, registrados como JUGADOR: **Ana** (nacida en 2014, con registro civil), **Beto**
  (2015), **Caro** (2016, queda sin categoría) y **Dani** (2014), a quien el presidente retira.
- **Club B**, con otro presidente (presidente B) y una categoría 2014. Invitar al correo de Ana
  como Jugador y aceptar con su sesión: Ana queda como jugadora en los dos clubes.

## Pruebas automatizadas

```powershell
dotnet test backend/LFClub.sln    # unitarias e integración (la integración necesita Docker)
npm --prefix frontend test
```

Resultado esperado: todas en verde, incluidas las de las specs anteriores. La prueba de contrato
espera 60 endpoints.

## Recorrido manual

Cada paso indica qué historia de la spec valida.

### 1. La familia consulta y mantiene la ficha (historia 1)

1. Entrar con la cuenta de Ana y elegir el Club A. **Esperado**: el menú y el inicio ofrecen "Mi
   ficha".
2. Abrir "Mi ficha". **Esperado**: muestra nombres, apellidos, tipo y número de documento, fecha
   de nacimiento, categoría 2014 y equipos; correo, celular y responsable; y los apartados de
   contacto de emergencia, seguridad social, datos clínicos y documentos, vacíos. No aparece
   ningún "último cambio".
3. Escribir un contacto de emergencia, la entidad de salud, el lugar de atención, el grupo
   sanguíneo y una alergia, y guardar. Cerrar sesión, volver a entrar y abrir la ficha.
   **Esperado**: los datos siguen ahí y el último cambio dice que lo hizo la cuenta del jugador,
   con la fecha de hoy.
4. Vaciar todos los datos de salud y el contacto de emergencia y guardar. **Esperado**: se guarda.
5. Vaciar el nombre del responsable y guardar. **Esperado**: no se guarda y se explica que es
   obligatorio para un menor. Cambiar el celular y guardar. **Esperado**: se guarda.
6. Buscar cómo cambiar nombres, apellidos o fecha de nacimiento. **Esperado**: la pantalla no lo
   ofrece. Con Swagger (<http://localhost:8080/swagger>) y la sesión de Ana:
   - `PUT …/ficha` añadiendo `"nombres": "Otro"` al cuerpo. **Esperado**: `200` y el nombre no
     cambia.
   - `PUT …/ficha/identidad`. **Esperado**: `403 rol_no_autorizado`.
7. Con la sesión de Ana, pedir `GET`, `PUT` y `PUT …/documento-identidad` sobre la ficha de Beto
   (su identificador se ve en la dirección al abrirla como presidente). **Esperado**: `404
   no_encontrado` en las tres.
8. Entrar con la cuenta de Dani, el retirado. **Esperado**: solo el aviso de que ya no está en el
   club, sin menú ni ficha. Con Swagger, su `GET …/ficha` responde `403 integrante_retirado`.
9. Con un jugador adulto (invitar y registrar a uno): **Esperado**: usa "Mi ficha" igual y puede
   dejar vacío el responsable.

### 2. El club consulta la ficha según el rol (historia 2)

Antes, con la cuenta de Ana, dejar escritos una alergia, un medicamento y un contacto de
emergencia.

1. Como **presidente A**, abrir "Categorías" → 2014 y pulsar el nombre de Ana. **Esperado**: la
   ficha completa, con datos clínicos y documentos. Abrir también la de Caro desde "Sin categoría"
   y la de Dani desde "Retirados". **Esperado**: completas las dos.
2. Como **entrenador**, abrir la 2014 y la ficha de Ana. **Esperado**: identidad, contacto,
   contacto de emergencia, seguridad social y datos clínicos; no existe el apartado "Documentos" y
   no hay ningún botón para cambiar nada.
3. Con la sesión del entrenador y Swagger, pedir la ficha de Beto (2015), de Caro (sin categoría)
   y de Dani (retirado). **Esperado**: `404` en las tres.
4. Como **directivo**, abrir la ficha de Ana y la de Dani. **Esperado**: identidad, contacto,
   contacto de emergencia, seguridad social y documentos; no existe el apartado de datos clínicos.
   En Swagger, la respuesta no contiene la propiedad `datosClinicos`.
5. Como presidente A, asignar al directivo como entrenador de la 2014. Repetir el paso 4.
   **Esperado**: igual; sigue sin datos clínicos.
6. Con la sesión del directivo y con la del entrenador, llamar a `PUT …/ficha`, `PUT
   …/ficha/documento-identidad`, `PUT …/ficha/identidad` y `PUT …/ficha/documentos/CERTIFICADO_SALUD`
   sobre Ana. **Esperado**: `403 rol_no_autorizado` en todas y nada cambia.
7. Como presidente A, pasar a Ana a la 2015. Con el entrenador, volver a pedir su ficha.
   **Esperado**: `404`. Asignar el entrenador a la 2015: ya la ve. Devolver a Ana a la 2014.
8. Como **presidente B**, pedir con Swagger la ficha de Beto usando el identificador del Club A en
   la ruta del Club B y en la del Club A. **Esperado**: `404` en las dos.
9. Como **DESARROLLADOR**, pedir cualquier ficha. **Esperado**: `404`. El panel de la plataforma no
   tiene ninguna entrada a fichas.
10. Sin sesión, pedir cualquier ruta de la ficha. **Esperado**: `401`. El escudo público del club
    sigue respondiendo sin datos de ningún jugador.

### 3. La familia entrega la documentación (historia 3)

1. Con la cuenta de Ana, abrir "Mi ficha" → "Documentos". **Esperado**: los dos documentos
   pedidos, copia del documento de identidad y certificado de afiliación a salud, ambos
   "Pendiente".
2. Subir la foto como copia del documento de identidad. **Esperado**: pasa a "Entregado", con la
   fecha de hoy, y "Abrir" la muestra.
3. Subir el PDF en ese mismo documento. **Esperado**: lo reemplaza; "Abrir" muestra el PDF y la
   foto ya no se puede obtener.
4. Intentar subir el archivo falso y el de más de 10 MB. **Esperado**: los rechaza explicando el
   motivo, y "Abrir" sigue mostrando el PDF anterior.
5. Como **directivo**, abrir la categoría 2014. **Esperado**: Ana figura con "Falta 1" y los
   jugadores sin archivos con "Faltan 2", escrito con texto. Abrir la ficha de Ana y el archivo
   entregado. **Esperado**: se abre; no hay botón para subir.
6. Con la cuenta de Ana, subir el certificado de salud. Como presidente A, repetir el paso 5.
   **Esperado**: Ana figura con la documentación "Completa". "Sin categoría" y "Retirados"
   muestran también el estado de cada jugador.
7. Como **entrenador**, abrir la 2014. **Esperado**: la lista no tiene columna de documentación.
   Con Swagger, el detalle de la categoría no trae `documentosPendientes`, y `GET` y `PUT
   …/ficha/documentos/CERTIFICADO_SALUD` de Ana responden `403`.
8. Con la cuenta de Beto, pedir y subir con Swagger un documento de Ana. **Esperado**: `404` en
   las dos.

### 4. Cambio del documento y corrección de la identidad (historia 4)

1. Con la cuenta de Ana, cambiar el documento de registro civil a tarjeta de identidad con otro
   número. **Esperado**: se guarda; sigue en la 2014, con sus equipos, sus datos y sus archivos.
2. Cerrar sesión. Entrar con el número anterior. **Esperado**: no entra en el Club A… pero como
   Ana también está en el Club B con el número anterior, ese número sigue sirviendo hasta que se
   cambie allí. Para ver el caso simple, repetir el cambio con Beto, que solo está en un club:
   con el número nuevo entra y con el anterior no.
3. Intentar poner a Ana el número de documento de Beto y después el de Dani (retirado).
   **Esperado**: no se guarda y explica que ese documento ya está registrado en el club.
4. Como presidente A, corregir el nombre y los apellidos de Beto. **Esperado**: se guardan y la
   lista de la categoría los muestra.
5. Corregir la fecha de nacimiento de Beto a 2013. **Esperado**: se guarda y Beto sigue en la
   2015.
6. Corregir la fecha de nacimiento de Caro (sin categoría, 2016) a 2015. **Esperado**: Caro entra
   en la 2015. Repetir con otro jugador sin categoría hacia un año sin categoría: sigue sin
   categoría.
7. Intentar guardar una fecha de nacimiento futura. **Esperado**: no se guarda y explica el
   motivo.
8. Como presidente A, en la ficha de Beto cambiar el celular, el contacto de emergencia y una
   alergia, y subir un documento. **Esperado**: todo se guarda; con la cuenta de Beto se ve lo
   mismo, y el último cambio nombra al presidente A.

### 5. Entre clubes, estados del club y conservación

1. Con la cuenta de Ana, elegir el Club B y abrir "Mi ficha". **Esperado**: el celular y el
   responsable son los mismos que en el Club A; el contacto de emergencia, la salud y los
   documentos están vacíos y pendientes (RF-003).
2. Cambiar el celular desde la ficha del Club B. Como presidente A, abrir la ficha de Ana.
   **Esperado**: muestra el celular nuevo (RF-039).
3. Como presidente A, retirar a Ana y abrir su ficha desde "Retirados". **Esperado**: completa,
   con sus archivos. Reincorporarla y entrar con su cuenta. **Esperado**: la ficha está como la
   dejó (RF-036).
4. Como DESARROLLADOR, suspender el Club A. Con la cuenta de Ana y con la del entrenador.
   **Esperado**: ven el aviso de incidencia temporal y no hay ficha. El presidente A sigue
   abriendo fichas. Levantar la suspensión.
5. Abrir la misma ficha como presidente A y como la cuenta de Beto en dos navegadores, cambiar un
   dato distinto en cada uno y guardar casi a la vez. **Esperado**: los dos guardados terminan
   bien; al recargar, la ficha tiene completo lo del último que guardó y ningún dato a medias.

### 6. Teléfono y temas (RF-035, CE-011)

Repetir a 360 px de ancho, en tema claro y oscuro: "Mi ficha" con su formulario, el apartado
"Documentos", el diálogo de cambio de documento, el de corrección de identidad y las listas de
jugadores con el estado de la documentación. **Esperado**: sin desplazamiento horizontal y con
texto legible; "Entregado", "Pendiente", "Completa" y "Faltan N" se leen como texto, no solo por
color.
