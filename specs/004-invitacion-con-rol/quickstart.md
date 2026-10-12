# Guía de validación: Invitación con rol e ingreso directo al club

**Funcionalidad**: `004-invitacion-con-rol` | **Fecha**: 2026-10-08

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
- Esta funcionalidad no tiene migración: sirve la base de datos que deja la 003.

## Datos de partida

Con las guías de la 001 y la [003](../003-categorias-club/quickstart.md), dejar creados:

- **Club A**, con su presidente (presidente A) y la categoría **2014** activa. Sin categoría 2016.
- **Club B**, con otro presidente (presidente B).

El directivo, el entrenador y los jugadores del Club A se crean durante el recorrido, con
invitaciones.

## Pruebas automatizadas

```powershell
dotnet test backend/LFClub.sln    # unitarias e integración (la integración necesita Docker)
npm --prefix frontend test
```

Resultado esperado: todas en verde. Las pruebas de la 002 y la 003 que esperaban la sala de espera
tras registrarse, la elección de rol al aprobar o a un directivo invitando o aprobando están
reescritas con el comportamiento nuevo (CE-008).

## Recorrido manual

Cada paso indica qué historia de la spec valida.

### 1. El presidente invita indicando el rol (historia 1)

1. Entrar como presidente A y abrir "Ingresos". **Esperado**: el formulario pide correo y rol, y
   ofrece Jugador, Entrenador y Directivo, sin ninguno elegido.
2. Enviar sin elegir rol. **Esperado**: no se envía y se explica que el rol es obligatorio.
3. Invitar a `directivo@…` como Directivo, a `entrenador@…` como Entrenador y a `ana@…` y
   `beto@…` como Jugador. **Esperado**: las cuatro aparecen pendientes, cada una con su rol, quién
   la envió, cuándo y cuándo vence.
4. Leer los correos. **Esperado**: cada uno nombra el Club A y el rol, y ninguno dice que el club
   revisará el ingreso. Sin llave de Brevo el registro de la API solo escribe el destinatario, el
   asunto y el enlace de cada correo, no su texto: ahí se comprueba el club (va en el asunto) y, al
   abrir el enlace, el rol; el texto completo lo comprueban las pruebas de `PlantillasCorreoPruebas`.
5. Reenviar la de `entrenador@…`. **Esperado**: la nueva sigue siendo de Entrenador y el enlace
   anterior ya no sirve.
6. Invitar otra vez a `ana@…`, ahora como Entrenador, y después otra vez como Jugador.
   **Esperado**: en la lista queda una sola fila para ese correo, con el último rol; solo sirve el
   último enlace.
7. Registrar a `directivo@…` con su enlace (ver el bloque 2) y entrar como **directivo**.
   **Esperado**: el menú no tiene "Ingresos". Abrir `/club/{clubA}/ingresos` escribiendo la
   dirección: no muestra invitaciones, sala de espera ni aprobados; la pantalla lo devuelve al
   inicio del club sin pedir ninguna lista.
8. Con la sesión del directivo, llamar directamente a la API (Swagger en
   <http://localhost:8080/swagger>): listar, invitar con `rol: JUGADOR`, reenviar y cancelar una
   invitación pendiente. **Esperado**: `403 rol_no_autorizado` en las cuatro; nada cambia.
9. Con la sesión del presidente A, `POST /api/clubes/{clubA}/invitaciones` con `rol: PRESIDENTE`.
   **Esperado**: `403 rol_no_invitable` y no se envía nada.

### 2. La persona invitada se registra y entra directamente (historia 2)

1. Abrir el enlace de `directivo@…`. **Esperado**: muestra el Club A, el correo y "directivo", sin
   forma de cambiarlos; no pide el nombre del responsable.
2. Completar el registro. **Esperado**: entra a la aplicación del Club A como Directivo, sin
   pantalla de espera.
3. Abrir el enlace de `ana@…` (Jugador). **Esperado**: pide el nombre del padre, madre o
   responsable. Con fecha de nacimiento en 2014 y el responsable vacío no deja registrarse; con el
   responsable, sí.
4. Terminar el registro de Ana. **Esperado**: ve la aplicación del Club A de inmediato, con la
   tarjeta "Mi categoría" en la 2014. Como presidente A, "Categorías" la muestra en la 2014.
5. Registrar a `beto@…` (Jugador) con fecha de nacimiento en 2016. **Esperado**: el registro se
   completa igual y entra al club; como presidente A, Beto está en "Sin categoría". Crear la
   categoría 2016: Beto entra en ella.
6. Registrar a `entrenador@…`. **Esperado**: entra como Entrenador. Como presidente A, no aparece
   en ninguna categoría ni en "Sin categoría", y sí como candidato al asignar entrenadores.
7. Invitar como Jugador a un adulto y registrarlo. **Esperado**: el responsable es opcional; entra
   como Jugador y queda en "Sin categoría" si no existe la categoría de su año.
8. Como presidente A, abrir "Ingresos". **Esperado**: la sala de espera dice que no hay ingresos
   pendientes; "Ingresos aprobados" no tiene a ninguno de los anteriores; en la lista de
   invitaciones, las de Ana, Beto, el entrenador y el directivo figuran como usadas, con su rol y
   quién las envió.
9. Como presidente B, invitar a `entrenador@…` como Directivo al Club B. Abrir el enlace con la
   sesión del entrenador. **Esperado**: no hay formulario de registro, solo aceptar; al aceptar
   entra al Club B como Directivo, y en el desplegable conserva el Club A como Entrenador.
10. Volver a abrir un enlace ya usado, uno reemplazado (paso 1.6) y uno cancelado. **Esperado**: se
    explica que la invitación ya no sirve y no hay formulario.
11. Con Swagger, repetir un registro enviando además `"rol": "PRESIDENTE"` en el cuerpo, con una
    invitación de Jugador. **Esperado**: entra como Jugador.
12. Preparar un menor con cuenta y sin responsable: invitar un correo nuevo como Entrenador al
    Club A y registrarlo con fecha de nacimiento en 2014. Como presidente B, invitar ese correo
    como Jugador al Club B y abrir el enlace con su sesión. **Esperado**: además de "Aceptar", la
    pantalla pide el nombre del padre, madre o responsable; vacío no deja entrar y muestra el
    error junto al campo; con el nombre entra al Club B como Jugador. Repetirlo invitándolo como
    Directivo, o con una cuenta de adulto: solo aparece "Aceptar".

### 3. La sala de espera, solo para jugadores y sin elegir rol (historia 3)

Ninguna pantalla deja ya a nadie en espera, así que la persona en espera se prepara a mano. Con
`docker compose exec bd psql -U lfclub lfclub`, pasar a `EN_ESPERA` a un jugador recién
registrado del Club A nacido en 2014:

```sql
UPDATE "UsuariosRol" SET "EstadoIngreso" = 'EN_ESPERA', "CategoriaId" = NULL
WHERE "NumeroDocumento" = '<documento del jugador>';
```

1. Entrar con la cuenta de ese jugador. **Esperado**: ve la pantalla de ingreso pendiente.
2. Con Swagger y la sesión del **directivo**, pedir la sala de espera y los aprobados, y llamar a
   la aprobación y al rechazo de esa persona. **Esperado**: `403 rol_no_autorizado` en las cuatro;
   la persona sigue en espera.
3. Como presidente A, abrir la sala de espera y pulsar aprobar. **Esperado**: pide confirmar y no
   ofrece ningún rol. Antes de confirmar, con Swagger y la sesión del presidente A, llamar a la
   aprobación con `rol: ENTRENADOR` y con `rol: DIRECTIVO`. **Esperado**: `403 rol_no_asignable`
   las dos veces; la persona sigue en espera.
4. Confirmar la aprobación. **Esperado**: entra como Jugador, queda en la 2014 y aparece en
   "Ingresos aprobados" con el rol Jugador, quién lo aprobó y cuándo.
5. Preparar a otra persona en espera y, como presidente A, rechazarla. **Esperado**: pide confirmación y la persona
   desaparece del club, como hasta ahora.

### 4. El desarrollador solo invita al crear el club (historia 4)

1. Como DESARROLLADOR, abrir el detalle del Club A. **Esperado**: muestra sus presidentes y sus
   invitaciones sin usar, pero no ofrece invitar a otro presidente.
2. Con Swagger y la sesión del DESARROLLADOR, buscar `POST /api/plataforma/clubes/{clubId}/invitaciones`.
   **Esperado**: no aparece; llamándola a mano responde `404` o `405` y no se envía ningún correo.
3. Crear el Club C con el correo de su presidente mal escrito. **Esperado**: el club se crea y la
   invitación aparece en su detalle.
4. Corregir el correo de esa invitación y reenviarla. **Esperado**: llega al correo nuevo, el
   enlace anterior ya no sirve y, al registrarse, la persona queda como presidente del Club C.

### 5. Aislamiento y estados del club (RF-020, RF-021)

1. Como presidente B, pedir por identificador una invitación o un ingreso del Club A. **Esperado**:
   `404`; en las listas del Club B no aparece nada del Club A.
2. Como DESARROLLADOR, suspender el Club A. Como presidente A, invitar con los tres roles.
   **Esperado**: se envían. Como directivo, entrar al club. **Esperado**: ve el aviso de
   incidencia temporal.
3. Registrarse con uno de esos enlaces. **Esperado**: el registro se completa y, al entrar, ve el
   aviso de incidencia temporal. Al levantar la suspensión entra con su rol, sin pasar por ninguna
   aprobación.
4. Dar de baja el Club A. **Esperado**: su presidente no entra y un enlace pendiente explica que
   la invitación ya no sirve. Revertir la baja.

### 6. Teléfono y temas (RF-022, CE-009)

Repetir a 360 px de ancho, en tema claro y oscuro: el formulario de invitar con su selector de
rol, la tabla de invitaciones con la columna "Rol", la confirmación de aprobar y la pantalla del
enlace de invitación con cada rol. **Esperado**: sin desplazamiento horizontal y con texto legible;
el rol se lee como texto, no solo por color.
