# Guía de validación: fichas de personas y datos propios

**Funcionalidad**: `008-fichas-de-personas` | **Fecha**: 2026-10-10

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml) y el
modelo en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md), en un
  proyecto de Compose aparte para no enviar correos reales ni tocar los datos de desarrollo:
  `docker compose -p la-pecosa-validacion -f docker-compose.yml -f <override>` con
  `Brevo__Llave` vacía. Aplicación en <http://localhost:5173>, API en <http://localhost:8080>.
- Esta funcionalidad **no tiene migración**.
- Una imagen PNG o JPEG de menos de 1 MB para las fotos de perfil.

## Datos de partida

Con las guías de la 003 a la [007](../007-jugadores-inicio-por-rol/quickstart.md), dejar creados:

- **Club A** con:
  - **Presidente P**, que no entrena, con foto de perfil.
  - **Directivo D**, asignado como entrenador de la **2015**, sin foto.
  - **Entrenador E1**, asignado a la **2014** dirigiendo los equipos "A" y "B", con foto.
  - **Entrenador E2**, sin categorías.
  - Jugadores **Ana** (2014) y **Caro** (2015); **Eva**, retirada por P; y **Luis**, hermano de
    Ana, en espera.
  - La ficha de Ana cambiada por última vez por P.
- **Club B** con su presidente, y en el que **E1 es PRESIDENTE** (la misma cuenta), y un jugador.
- Una tercera categoría de A, **2016**, desactivada, en la que E1 tuvo asignación.

## Bloque 1: la ficha básica (historia 1)

1. Entrar con la cuenta de Ana. En el inicio, "Mi categoría" muestra a E1 con su foto en
   miniatura, su nombre y "dirige A, B". Pulsar el nombre: ficha básica con nombre, "Entrenador",
   foto grande y "Categoría 2014 · A, B". No aparece la 2016.
2. En esa ficha no hay documento, fecha de nacimiento, correo ni celular. En las herramientas del
   navegador, la respuesta de `GET /api/clubes/<A>/personas/<E1>` tampoco los trae.
3. "← Inicio" vuelve al inicio.
4. Abrir a mano la ficha básica de D (dirección `/club/<A>/personas/<D>`): "Directivo", iniciales
   en lugar de foto, "Categoría 2015". La de P: "Presidente", su foto, sin sección de categorías.
   La de E2: "Todavía no tiene categorías asignadas".
5. Repetir 1 y 4 como E2, como D y como P: lo mismo.
6. Entrar en el club B como su presidente y abrir `/club/<A>/personas/<E1>` cambiando solo el
   club de la dirección: la pantalla no muestra la ficha; la API responde `404 no_encontrado`.
7. En el club B, abrir la ficha básica de E1 (allí PRESIDENTE): "Presidente", la misma foto y sin
   las categorías del club A.
8. Abrir `/club/<A>/personas/<Ana>` como P: no hay ficha básica de un jugador (404).
9. A 360 px y en los dos temas: sin desplazamiento horizontal, también con un nombre largo.

## Bloque 2: nombres que llevan a la ficha (historia 2)

Recorrer con cada rol y comprobar a dónde lleva cada nombre:

| Pantalla | Como | Nombre | Debe |
| --- | --- | --- | --- |
| Inicio | familia de Ana | E1 | ficha básica |
| Inicio | familia de Ana | Ana | "Mi ficha" |
| Jugadores | P, D, E1 | Ana | detalle de Ana con lo que su rol ve (spec 007) |
| Categorías | P, D | E1, D | ficha básica |
| Detalle de la 2014 | P, D, E1 | E1 | ficha básica |
| Detalle de la 2014 | P, D, E1 | Ana | detalle de Ana |
| Retirados | P, D | Eva | su ficha |
| Retirados | P, D | "retirado por" P | ficha básica de P |
| Ingresos aprobados | P | persona y "aprobado por" | su ficha actual y la ficha básica |
| Sala de espera | P | Luis | texto: su tarjeta ya muestra sus datos |
| Sala de espera | P | "hermano de" Ana | ficha de Ana |
| Ficha de Ana | P, D, E1, familia | "último cambio por" P | ficha básica de P |

En cada caso, volver atrás regresa a la pantalla de origen con su nombre en el enlace.

Después:

1. Cambiar a D de rol a ENTRENADOR (P, desde la búsqueda por documento): su ficha básica dice
   "Entrenador" en la siguiente consulta.
2. Pedir al DESARROLLADOR que quite el rol a P eligiendo eliminarlo del club, con otro presidente
   ya registrado: en la ficha de Ana, "último cambio por" y en retirados "retirado por" se ven con
   el nombre de P pero como texto normal, y su ficha básica responde 404.
3. Un cambio de la ficha hecho por la familia sigue diciendo "la cuenta del jugador", sin enlace.

## Bloque 3: miniaturas de la familia (historia 3)

1. Con la 2014 con E1 (con foto) y un segundo entrenador sin foto: la familia de Ana ve la foto
   de uno y las iniciales del otro.
2. E1 cambia su foto en "Mi perfil": la familia ve la nueva al volver al inicio. La quita: ve sus
   iniciales.
3. Con un jugador sin categoría, "Mi categoría" sigue diciendo que todavía no tiene categoría.

## Bloque 4: "Mi perfil" (historia 4)

1. Como E1, pulsar el nombre en la barra superior → "Mi perfil": foto, nombres, apellidos, tipo y
   número de documento, fecha de nacimiento, correo y celular. Documento y correo sin poder
   editarse.
2. Cambiar el celular, los apellidos y la fecha de nacimiento y guardar: confirmación; la barra
   superior y su ficha básica muestran los apellidos nuevos en el club A y en el club B.
3. Dejar vacíos los nombres, poner una fecha futura y un celular de 25 caracteres: cada campo con
   su motivo; al recargar, los valores anteriores siguen.
4. En la ficha de Ana, "último cambio por" sigue mostrando el nombre que P tenía entonces, aunque
   P cambie sus apellidos (§13).
5. Entrar con la cuenta de Ana → "Mi perfil": foto de perfil y el acceso "Mi ficha" de Ana, sin
   formulario de datos personales.

## Comprobaciones directas contra la API

Con el token de cada cuenta (`Authorization: Bearer …`):

- `PUT /api/cuenta/datos-propios` de E1 con `numeroDocumento` y `correo` en el cuerpo: responde
  `200`, y ni el documento ni el correo cambian.
- `GET /api/cuenta/datos-propios` con la cuenta de Ana: `404 no_encontrado`.
- `GET /api/clubes/<A>/personas/<E1>/foto` con la cuenta de Ana: la imagen, con
  `Cache-Control: private, no-store`.
- La misma petición con el DESARROLLADOR o el presidente de B: `404`.
- `GET /api/clubes/<A>/personas/<E1>` con la cuenta de Luis elegido (en espera): `403
  ingreso_en_espera`; con la de Eva: `403 integrante_retirado`.
- Con el club A suspendido: E1 recibe `403 club_suspendido`; P abre las fichas.

## Pruebas automatizadas

```text
dotnet test backend/LaPecosa.sln
cd frontend && npm test
```

Deben pasar todas, también las de la 001 a la 007. `AccesoClubPruebas` cuenta 48 rutas de club y
72 en el contrato.

## Al terminar

Borrar el proyecto de Compose de validación y el contenedor mínimo si se dejó uno.
