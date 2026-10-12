# Guía de validación: Base multiclub y panel de administración de la plataforma

**Funcionalidad**: `001-base-multiclub` | **Fecha**: 2026-10-07

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato de la API está en [contracts/api.yaml](contracts/api.yaml)
y las entidades en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).

## Puesta en marcha

Desde la raíz del repositorio:

```powershell
Copy-Item .env.ejemplo .env      # y poner en Plataforma__CorreoDesarrollador tu correo
docker compose up --build
```

| Servicio | Dirección |
| --- | --- |
| Aplicación (frontend) | <http://localhost:5173> |
| API | <http://localhost:8080> |
| Swagger | <http://localhost:8080/swagger> |

Sin llave de Brevo en `.env`, la API no envía correos: escribe cada correo, con su enlace, en su
registro. Para leerlos:

```powershell
docker compose logs api | Select-String "Correo para"
```

## Pruebas automatizadas

```powershell
dotnet test backend/LFClub.sln    # unitarias e integración (la integración necesita Docker)
npm --prefix frontend test          # contraste y tema
```

Resultado esperado: todas en verde.

## Recorrido manual

Cada paso indica qué historia de la spec valida.

### 1. Primera entrada del DESARROLLADOR (RF-001, RF-005a)

1. Abrir <http://localhost:5173>. Aparece el inicio de sesión; no hay ningún enlace para
   registrarse (historia 2, escenario 4).
2. Pulsar "Olvidé mi contraseña", escribir el correo del DESARROLLADOR y enviar.
3. Tomar el enlace del registro de la API, abrirlo y crear una contraseña.
4. Iniciar sesión. **Esperado**: se llega al panel de administración, con la lista de clubes vacía.

### 2. Crear un club e invitar a su presidente (historia 1)

1. Crear el club "Valfor F.C." con un correo de presidente. **Esperado**: aparece en la lista como
   activo, con identidad neutra y la indicación de que su presidente no se ha registrado.
2. Intentar crear otro sin correo, con un correo mal escrito y con el mismo nombre. **Esperado**:
   las tres veces se rechaza con un mensaje claro.
3. En el detalle del club, reenviar la invitación. **Esperado**: llega un enlace nuevo y el
   primero deja de servir.

### 3. Registro del presidente (historia 2)

1. Abrir el enlace vigente en una ventana privada. **Esperado**: se ve el nombre del club, el rol
   PRESIDENTE y el correo ya escrito; ninguno se puede cambiar.
2. Completar los datos y la contraseña. **Esperado**: se entra a la aplicación del club, con su
   nombre en pantalla, sin sala de espera.
3. Cerrar sesión y entrar con el correo; cerrar y entrar con el documento. **Esperado**: ambas
   funcionan.
4. Abrir de nuevo el enlace ya usado. **Esperado**: explica que la invitación ya no sirve.
5. Fallar la contraseña 5 veces y probar después con la correcta. **Esperado**: no entra y la
   pantalla indica cómo recuperarla. Recuperarla por correo y entrar.
6. Pedir la recuperación de un correo que no existe. **Esperado**: el mismo mensaje y ningún
   correo en el registro.

### 4. Identidad del club (historia 3)

1. Como DESARROLLADOR, cargar un escudo y definir los colores de "Valfor F.C.". Probar un color
   casi blanco. **Esperado**: aviso de contraste antes de guardar.
2. Cargar un archivo que no es una imagen y otro de más de 1 MB. **Esperado**: se rechazan.
3. Como presidente, recargar. **Esperado**: la aplicación muestra el escudo, los colores y el
   nombre del club.

### 5. Varios clubes y aislamiento (historias 2 y 4, RF-024)

1. Como DESARROLLADOR, crear "Club B" invitando al mismo correo del presidente de Valfor.
2. Abrir la invitación. **Esperado**: pide iniciar sesión, no registrarse; al aceptar, la misma
   cuenta queda como presidente de los dos clubes.
3. **Esperado**: aparece el desplegable de clubes; al cambiar, cambian el nombre, el escudo y los
   colores.
4. Crear "Club C" con otro presidente. Con la sesión del primero, pedir a la API
   `GET /api/clubes/{id de Club C}`. **Esperado**: `404`.
5. Con esa misma sesión, pedir `GET /api/plataforma/clubes`. **Esperado**: `403`, sin datos.
6. Con la sesión del DESARROLLADOR, pedir `GET /api/clubes/{id de Valfor}`. **Esperado**: `404`.
7. Como presidente, editar la dirección del club. **Esperado**: se guarda y se ve también en el
   panel del DESARROLLADOR. No existe ninguna opción para cambiar escudo o colores.

### 6. Presidentes (historia 1, escenarios 6 a 7)

1. Como DESARROLLADOR, intentar quitar el rol al único presidente de "Club C". **Esperado**: se
   niega y pide registrar antes a otro.
2. Invitar a un segundo presidente a "Club C" y registrarlo. Quitarle el rol al primero
   asignándole DIRECTIVO. **Esperado**: sigue en el club como directivo.
3. Repetir con otro presidente eligiendo "eliminar del club". **Esperado**: ya no puede entrar a
   ese club; si era su único club, su cuenta deja de existir.

### 7. Estados del club (historia 5)

1. Suspender "Club C". **Esperado**: su presidente entra y ve que está suspendido; el directivo ve
   el aviso de incidencia temporal, también si ya tenía la sesión abierta.
2. Levantar la suspensión. **Esperado**: todo como antes.
3. Intentar eliminar el club estando activo. **Esperado**: no es posible.
4. Darlo de baja. **Esperado**: no entra nadie, tampoco el presidente. Revertir la baja y
   comprobar que vuelve.
5. Darlo de baja de nuevo y eliminarlo. **Esperado**: exige escribir el nombre; después, el club
   no aparece, sus integrantes que no tenían otro club ya no pueden entrar y los demás clubes
   están intactos.

### 8. Tema claro y oscuro (historia 6)

1. Pulsar el botón de tema en el inicio de sesión, en el registro, en el panel y en la aplicación
   del club. **Esperado**: toda la interfaz cambia de inmediato.
2. Cerrar y volver a abrir. **Esperado**: se mantiene el tema elegido.
3. En una ventana privada, con el sistema en modo oscuro. **Esperado**: abre en oscuro.
4. Con los colores de Valfor, revisar ambos temas. **Esperado**: el texto se lee y el club se
   reconoce.

### 9. Foto de perfil (RF-036 a RF-038)

1. Con cualquier cuenta, abrir "Mi perfil" y cargar una foto PNG, JPEG o WebP de menos de 1 MB.
   **Esperado**: aparece en la cabecera y en "Mi perfil", y es la misma al cambiar de club.
2. Cargar un archivo que no es una imagen y otro de más de 1 MB. **Esperado**: se rechazan con un
   mensaje claro y la foto anterior se conserva.
3. Sin sesión, pedir `GET /api/cuenta/foto`. **Esperado**: `401`.
4. Quitar la foto. **Esperado**: vuelven a verse las iniciales.

### 10. Teléfono (RF-035, CE-009)

Repetir los pasos 2, 3 y 5 con el navegador a 360 px de ancho. **Esperado**: sin desplazamiento
horizontal.
