# Guía de validación: eliminar mi cuenta

**Rama**: `014-eliminar-cuenta` | **Plan**: [plan.md](plan.md) | **Contrato**: [contracts/api.yaml](contracts/api.yaml)

Recorrido para comprobar la funcionalidad de punta a punta. Los detalles de tablas están en
[data-model.md](data-model.md).

## Preparación

1. Levantar la plataforma en un proyecto de Compose aparte, sin la llave real de Brevo y con
   `Limites__AnonimoPorMinuto` alto (ver la memoria de validación local). Sin llave, el enlace de cada
   correo sale en el registro de la API.
2. Con el panel del DESARROLLADOR, crear dos clubes, **Club Uno** y **Club Dos**, cada uno con su
   presidente (**Presidente Uno** y **Presidente Dos**).
3. En Club Uno, invitar y registrar a:
   - **Familia Mora** (correo propio) con dos jugadores: *Ana Mora* y *Beto Mora* (hermano, aprobado).
   - **Entrenadora Paz**, asignada a una categoría.
4. En Club Dos, invitar la misma cuenta **Familia Mora** con un tercer jugador, *Cleo Mora*.
5. Crear una categoría con *Ana* y *Beto*; programar un partido con los dos convocados; la familia
   publica el marcador y un gol de Ana; la entrenadora publica un comunicado.

## Escenarios

### A. Eliminar con una familia (historias 1 y 2)

1. Entrar como Familia Mora con su correo. Abrir el menú del perfil → **Mi perfil** →
   **Eliminar mi cuenta** (tres toques).
2. Comprobar que la pantalla, antes de pedir nada, dice qué se borra, qué se conserva y que no se
   puede deshacer, y que lista a *Ana*, *Beto* (Club Uno) y *Cleo* (Club Dos), y ofrece "Retirar del
   club".
3. Escribir una contraseña incorrecta y confirmar. Esperado: mensaje "La contraseña no es correcta"
   y nada borrado; la sesión sigue.
4. Escribir la contraseña correcta y confirmar. Esperado: vuelve a `/entrar` con un aviso.
5. Intentar entrar con el correo y con el documento de *Ana*. Esperado: el mismo mensaje que con
   cualquier credencial desconocida.
6. Como Presidente Uno: los tres jugadores no aparecen en listas, calendario ni goleadores; el
   partido conserva el marcador, sin convocatorias ni goles.
7. Registrar una cuenta nueva con el mismo correo y con el documento de *Ana*. Esperado: se permite y
   es una cuenta nueva, sin nada de lo anterior.

### B. Presidente único (historia 3)

1. Entrar como Presidente Uno y abrir **Eliminar mi cuenta**. Esperado: aviso con "Club Uno" y la
   indicación de nombrar antes a otro presidente; sin campo de contraseña ni botón.
2. Nombrar a otro presidente (dándole el rol a alguien ya registrado) y volver. Esperado: ya se puede
   eliminar.
3. Forzar `POST /api/cuenta/eliminacion` con la contraseña correcta mientras era único. Esperado: 409
   `presidente_unico` con `clubes` y nada borrado.

### C. Entrenador y comunicados (historia 3)

1. Entrar como Entrenadora Paz y eliminar su cuenta con su contraseña.
2. Como una familia de la categoría: el comunicado sigue visible con **Persona eliminada** como
   autor; la entrenadora ya no aparece en listas ni asignaciones.

### D. Sesiones y DESARROLLADOR

1. Con dos navegadores con la misma cuenta, eliminarla en uno. Esperado: en el otro, la siguiente
   petición lleva a `/entrar`.
2. Entrar como DESARROLLADOR. Esperado: "Mi perfil" no ofrece eliminar la cuenta y
   `GET /api/cuenta/eliminacion` responde 404.

### E. Páginas públicas (historias 4 y 5)

1. Sin sesión, abrir `/eliminar-cuenta`. Esperado: carga sin pedir inicio de sesión y muestra los
   pasos, lo que se borra y se conserva, el correo `luisrak25@gmail.com` y el plazo de 15 días.
2. Abrir `/entrar` y `/privacidad`. Esperado: ambas enlazan a `/eliminar-cuenta`; `/privacidad`
   describe la eliminación, lo que se conserva y coincide con la pantalla del escenario A.

## Procedimiento para una solicitud por correo

Fuera de la aplicación, por quien atienda `luisrak25@gmail.com` (research §9):

1. Verificar que el mensaje llega desde el correo de la cuenta.
2. Pedirle que use "Olvidé mi contraseña" en `/entrar`: el enlace llega a ese mismo correo. Con él
   cambia la contraseña y elimina la cuenta como en el escenario A.
3. Si no puede, la eliminación la hace el propietario con acceso operativo a la base de datos,
   siguiendo el orden del [data-model.md](data-model.md) y dentro de una transacción. Se responde al
   correo en un máximo de 15 días.

## Pruebas automáticas

| Qué | Dónde |
| --- | --- |
| Regla del presidente único | `backend/pruebas/Unitarias/ReglaEliminacionDeCuentaPruebas.cs` |
| Borrado total, marcador, sesiones, correo y documento libres | `backend/pruebas/Integracion/Cuenta/EliminarCuentaPruebas.cs` |
| Presidente único | `backend/pruebas/Integracion/Cuenta/EliminarCuentaPresidentePruebas.cs` |
| Autoría anónima e invitaciones | `backend/pruebas/Integracion/Cuenta/AutoriaAnonimaPruebas.cs` |
| Claves foráneas hacia cuenta e integrante | `backend/pruebas/Integracion/Cuenta/EsquemaDeEliminacionPruebas.cs` |
| Pantalla y página pública | `frontend/pruebas/EliminarCuenta.test.tsx` |
| Contrato y acceso (automático) | `AccesoClubPruebas` / `EndpointsDeLaApi` |

Comandos: `dotnet test` desde la solución del backend (la batería de integración tarda unos 12
minutos), `npm test` y `npm run lint` en `frontend/`, y `scripts/verificar-tamano.ps1`.
