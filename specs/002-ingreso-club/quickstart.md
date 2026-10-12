# Guía de validación: Ingreso de personas al club

**Funcionalidad**: `002-ingreso-club` | **Fecha**: 2026-10-07

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml) y los
cambios del modelo en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md):
  `docker compose up --build`, aplicación en <http://localhost:5173> y API en
  <http://localhost:8080>. Sin llave de Brevo, los enlaces de los correos se leen con
  `docker compose logs api | Select-String "Correo para"`.

## Datos de partida

Con los pasos 1 a 3 de la guía de la 001, dejar creados:

- **Club A**, con su presidente registrado (presidente A).
- **Club B**, con otro presidente registrado (presidente B).

## Pruebas automatizadas

```powershell
dotnet test backend/LFClub.sln    # unitarias e integración (la integración necesita Docker)
npm --prefix frontend test
```

Resultado esperado: todas en verde, incluidas las de la 001.

## Recorrido manual

Cada paso indica qué historia de la spec valida.

### 1. El club invita por correo (historia 1)

1. Entrar como presidente A y abrir "Ingresos". **Esperado**: tres secciones (sala de espera,
   invitaciones, ingresos aprobados), todas vacías.
2. Enviar una invitación a `directivo@ejemplo.com`. **Esperado**: aparece como pendiente, con el
   correo, quién la envió, la fecha de envío y la de vencimiento (7 días después), y el enlace
   sale en el registro de la API.
3. Enviar con el correo vacío y con un correo mal escrito. **Esperado**: no se envía nada y se
   explica el motivo.
4. Invitar al correo del presidente A y al correo del DESARROLLADOR. **Esperado**: se rechazan
   las dos, cada una con su mensaje.
5. Reenviar la invitación pendiente. **Esperado**: llega un enlace nuevo; el anterior ya no sirve
   y la lista sigue mostrando una sola fila para ese correo.
6. Invitar a `error@ejemplo.com` y cancelar esa invitación. **Esperado**: aparece como cancelada
   y su enlace explica que ya no sirve.

### 2. Registro y sala de espera (historia 2)

1. Abrir el enlace vigente de `directivo@ejemplo.com` en una ventana privada. **Esperado**: se ve
   el nombre del Club A y el correo ya escrito; ninguno se puede cambiar y no se nombra ningún
   rol. La pantalla explica con qué documento se registra un jugador y con cuál un entrenador o
   directivo.
2. Poner una fecha de nacimiento de hace 10 años y dejar vacío el responsable. **Esperado**: se
   rechaza indicando que el responsable es obligatorio. Con una fecha de adulto, se acepta vacío.
3. Completar el registro como adulto. **Esperado**: aparece solo la pantalla de sala de espera,
   con el nombre y los colores del Club A, el botón de tema y cerrar sesión; no hay menú.
4. Cerrar sesión y entrar con el correo; cerrar y entrar con el documento. **Esperado**: las dos
   veces, solo la sala de espera.
5. Con esa sesión, pedir a la API `GET /api/clubes/{id de Club A}`. **Esperado**: `403` con código
   `ingreso_en_espera` y ningún dato.
6. Abrir de nuevo el enlace ya usado. **Esperado**: explica que la invitación ya no sirve.
7. Invitar desde el Club A a un correo nuevo y registrarlo con el mismo documento del paso 3.
   **Esperado**: se rechaza por documento repetido en el club.

### 3. Aprobación (historia 3)

1. Como presidente A, abrir "Ingresos". **Esperado**: la sala de espera muestra a la persona con
   nombre, apellidos, documento, fecha de nacimiento, correo, celular, responsable y fecha de
   registro.
2. Aprobarla eligiendo DIRECTIVO. **Esperado**: desaparece de la sala de espera y aparece en
   "Ingresos aprobados" con su nombre, el rol DIRECTIVO, quién la aprobó y cuándo; esa lista no
   ofrece ninguna acción.
3. En la ventana del directivo, pulsar "Actualizar". **Esperado**: entra a la aplicación del
   club como directivo, sin registrarse de nuevo, y ve el apartado "Ingresos".
4. Invitar y registrar a otras dos personas (un jugador menor, con responsable, y un adulto).
   Como **directivo**, aprobar al menor como JUGADOR y al adulto como ENTRENADOR. **Esperado**:
   las dos aprobaciones funcionan y el directivo no tiene la opción DIRECTIVO.
5. Como directivo, llamar a la API de aprobación con `{"rol":"DIRECTIVO"}` y con
   `{"rol":"PRESIDENTE"}` sobre una persona en espera. **Esperado**: `403 rol_no_asignable`.
6. Con la sesión del jugador aprobado, pedir `GET /api/clubes/{id}/ingresos/en-espera`.
   **Esperado**: `403 rol_no_autorizado`. Su menú no muestra "Ingresos".
7. Con dos ventanas (presidente y directivo) y una persona en espera a la vista en ambas, aprobar
   en una y después en la otra. **Esperado**: la segunda ve que ya estaba aprobada y no cambia
   nada.

### 4. Rechazo (historia 4)

1. Invitar y registrar a `rechazo@ejemplo.com`. Como directivo, pulsar "Rechazar". **Esperado**:
   pide confirmación y avisa de que el registro se borrará.
2. Confirmar. **Esperado**: desaparece de la sala de espera y su invitación ya no está en la
   lista de invitaciones. No sale ningún correo en el registro de la API.
3. Intentar entrar con esa cuenta. **Esperado**: el mensaje normal de datos incorrectos.
4. Invitar otra vez a `rechazo@ejemplo.com` y registrarse con el mismo documento. **Esperado**:
   funciona y vuelve a quedar en espera.
5. Llamar a la API de rechazo sobre el directivo ya aprobado. **Esperado**:
   `409 ingreso_ya_aprobado`.

### 5. Varios clubes y aislamiento (RF-013, RF-018, RF-028, RF-029)

1. Como presidente B, invitar al correo del directivo del Club A. Abrir el enlace. **Esperado**:
   pide iniciar sesión, no registrarse; al aceptar, queda en espera en el Club B.
2. **Esperado**: el desplegable muestra los dos clubes; en el Club A trabaja con normalidad y al
   elegir el Club B ve solo la sala de espera.
3. Como presidente B, rechazarlo. **Esperado**: su cuenta sigue existiendo y sigue entrando al
   Club A; el Club B ya no aparece en su desplegable.
4. Con la sesión del presidente B, pedir las invitaciones y la sala de espera del Club A, y
   aprobar por identificador a una persona en espera del Club A. **Esperado**: `404` en todo.
5. Como DESARROLLADOR, abrir el detalle del Club A. **Esperado**: solo ve invitaciones de
   presidente. Pedir `GET /api/clubes/{id de Club A}/ingresos/en-espera`: `404`.

### 6. Club suspendido y dado de baja (RF-030)

1. Dejar una invitación pendiente y una persona en espera en el Club A. Como DESARROLLADOR,
   suspenderlo.
2. **Esperado**: el presidente A sigue invitando, aprobando y rechazando; el directivo ve el
   aviso de incidencia temporal; el enlace pendiente sigue permitiendo registrarse.
3. Dar de baja el Club A. **Esperado**: no entra nadie y el enlace pendiente explica que la
   invitación ya no sirve. Revertir la baja: el enlace vuelve a servir si no ha vencido.

### 7. Teléfono y temas (RF-031, CE-009)

Repetir los pasos 1, 2 y 3 con el navegador a 360 px de ancho, en tema claro y en oscuro.
**Esperado**: sin desplazamiento horizontal y con el texto legible en los dos temas.
