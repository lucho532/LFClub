# LFClub

Plataforma multiclub para escuelas y clubes de fútbol. Una misma instalación atiende a varios
clubes, cada uno con sus datos aislados, su escudo y sus colores.

Este repositorio contiene la funcionalidad **001: base multiclub y panel de administración de la
plataforma**: clubes aislados entre sí, cuentas con un rol por club, el panel desde el que el
desarrollador crea y administra clubes, el registro del presidente por invitación, la identidad
visual de cada club, los temas claro y oscuro y la foto de perfil.

- `backend/`: API en ASP.NET Core 10 por capas (`LFClub.Api`, `Aplicacion`, `Dominio`,
  `Infraestructura`) sobre PostgreSQL 17, con sus pruebas en `backend/pruebas/`.
- `frontend/`: una sola aplicación React 19 con TypeScript y Vite.
- `specs/001-base-multiclub/`: especificación, plan, modelo de datos, contrato de la API y guía de
  validación.
- `.specify/memory/constitution.md`: las reglas del proyecto.

## Requisitos

- Docker Desktop en marcha.
- Solo para ejecutar las pruebas fuera de Docker: SDK de .NET 10 y Node 22.

## Puesta en marcha

Desde la raíz del repositorio:

```powershell
Copy-Item .env.ejemplo .env
```

Edita `.env` y pon en `Plataforma__CorreoDesarrollador` tu correo: es el de la única cuenta de
administración de la plataforma. Cambia también `Sesion__ClaveFirma` por una clave propia de al
menos 32 caracteres. El archivo `.env` no se sube al repositorio.

```powershell
docker compose up --build
```

| Servicio | Dirección |
| --- | --- |
| Aplicación | <http://localhost:5173> |
| API | <http://localhost:8080> |
| Documentación de la API (Swagger) | <http://localhost:8080/swagger> |

La base de datos se crea sola: la API aplica las migraciones al arrancar.

### Primera entrada

La cuenta de administración nace **sin contraseña**; nadie asigna la contraseña de otra persona.
Para crearla:

1. Abre <http://localhost:5173> y pulsa "Olvidé mi contraseña".
2. Escribe el correo que pusiste en `Plataforma__CorreoDesarrollador`.
3. Abre el enlace del correo (ver el apartado siguiente), crea tu contraseña e inicia sesión.

### Leer los correos sin Brevo

Con `Brevo__Llave` vacía en `.env`, la API no envía correos: escribe cada uno, con su enlace, en su
registro. Sirve para probar las invitaciones y la recuperación de contraseña sin cuenta de Brevo.

```powershell
docker compose logs api | Select-String "Correo para"
```

Para enviar correos de verdad, pon en `.env` la llave de la API de Brevo en `Brevo__Llave` y un
remitente verificado en `Brevo__Remitente`.

## Pruebas

```powershell
dotnet test backend/LFClub.sln    # unitarias e integración
npm --prefix frontend ci            # solo la primera vez
npm --prefix frontend test          # contraste, temas y club de entrada
```

Las pruebas de integración arrancan la API real contra un PostgreSQL 17 en un contenedor
(Testcontainers), así que necesitan Docker en marcha. No usan la base de datos de `docker compose`.

Otras comprobaciones:

```powershell
npm --prefix frontend run lint
npm --prefix frontend run build
./scripts/verificar-tamano.ps1      # ningún archivo de código supera las 250 líneas
```

El recorrido manual completo, paso a paso y con el resultado esperado de cada uno, está en
[specs/001-base-multiclub/quickstart.md](specs/001-base-multiclub/quickstart.md).

## Desarrollo sin Docker para la API y el frontend

`docker-compose.yml` no publica el puerto de la base de datos. Para ejecutar la API fuera de
Docker hace falta un PostgreSQL 17 accesible (por ejemplo, añadiendo `ports: ["5432:5432"]` al
servicio `bd` en un archivo `docker-compose.override.yml` propio) y pasar a
`dotnet run --project backend/src/LFClub.Api` la misma configuración de `.env` como variables de
entorno, con `Host=localhost` en la cadena de conexión. El frontend se ejecuta con
`npm --prefix frontend run dev`; la dirección de la API que usa se fija con `VITE_URL_API`.

Para crear una migración tras cambiar el modelo:

```powershell
dotnet ef migrations add <Nombre> -p backend/src/LFClub.Infraestructura -s backend/src/LFClub.Infraestructura -o Datos/Migraciones
```
