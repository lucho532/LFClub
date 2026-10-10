# Guía de validación: Jugadores, inicio por rol e identidad del club por el presidente

**Funcionalidad**: `007-jugadores-inicio-por-rol` | **Fecha**: 2026-10-10

Esta guía sirve para comprobar, de extremo a extremo, que la funcionalidad cumple la spec. No
describe cómo implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml) y el
modelo en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- La puesta en marcha es la de la [guía de la 001](../001-base-multiclub/quickstart.md), pero en un
  proyecto de Compose aparte para no enviar correos reales ni tocar los datos de desarrollo:
  `docker compose -p la-pecosa-validacion -f docker-compose.yml -f <override>` con
  `Brevo__Llave` vacía. Aplicación en <http://localhost:5173>, API en <http://localhost:8080>. Sin
  llave de Brevo, los enlaces de los correos salen en el registro de la API (`Correo para …`).
- Esta funcionalidad tiene una migración (`FotosDeJugadores`), que la API aplica al arrancar. No
  cambia ningún dato existente.
- Tres imágenes a mano: una PNG o JPEG de menos de 1 MB, otra imagen de más de 1 MB y un archivo
  que no sea imagen (por ejemplo un `.txt` renombrado a `.png`).

## Datos de partida

Con las guías de la 003 a la [006](../006-agregar-hermano/quickstart.md), dejar creados:

- **Club A**, con el **presidente A**, un **directivo**, el **entrenador E1** asignado solo a la
  2014 (dirigiendo el equipo "A") y el **entrenador E2** sin categorías. El directivo también está
  asignado como entrenador de la 2015.
- Categorías **2014** (equipos "A" y "B") y **2015** activas, y la **2016** desactivada.
- Jugadores de A: **Ana** (2014, en los equipos A y B), **Beto** (2014, sin equipo), **Caro**
  (2015, en un equipo), **Dani** (sin categoría: nacido en 2017), **Eva** (retirada) y un hermano
  de Ana, **Luis**, en espera. La cuenta de Ana tiene a Ana y a Luis.
- **Club B**, con su propio presidente y un jugador.

## Bloque 1: la lista de jugadores (historia 1)

1. Entrar como presidente A. El menú muestra "Jugadores". Abrirlo: grupos 2014 (Ana, Beto), 2015
   (Caro) y, al final, "Sin categoría" (Dani). No aparecen Eva ni Luis.
2. Ana muestra "A, B"; Beto, "Sin equipo". Todos muestran sus iniciales: nadie tiene foto todavía.
3. La columna "Al día" dice lo mismo en todas las filas y no se lee como "al día" ni como "debe".
4. Repetir como el directivo: la misma lista.
5. Entrar como E1: solo el grupo 2014, con Ana y Beto; sin "Sin categoría" ni 2015.
6. Entrar como E2: el mensaje de que todavía no tiene categorías asignadas.
7. Entrar con la cuenta de Ana: el menú no tiene "Jugadores". Abrir a mano
   `/club/<A>/jugadores`: la pantalla no muestra la lista, y la API responde `403
   rol_no_autorizado` (ver comprobaciones directas).
8. A 360 px y en los dos temas: sin desplazamiento horizontal, con un nombre largo en la lista.

## Bloque 2: el detalle y la foto (historia 2)

1. Como presidente A, abrir a Ana desde la lista: foto grande (iniciales), categoría 2014, equipos
   A y B y toda la ficha con los datos clínicos y los documentos. "← Jugadores" vuelve a la lista.
2. Cargar como foto la imagen válida: se ve en grande, y al volver a la lista, en miniatura. El
   último cambio de la ficha dice que lo hizo el presidente.
3. Cargar la imagen de más de 1 MB y luego el archivo que no es imagen: cada uno con su motivo, y
   la foto anterior sigue.
4. Como el directivo, abrir a Ana: foto grande, ficha sin datos clínicos, sin botones de foto ni
   de edición.
5. Como E1, abrir a Ana: foto grande, ficha con datos clínicos y de emergencia, sin documentos ni
   botones.
6. Como la cuenta de Ana (con Ana elegida), en "Mi ficha": la foto se ve; cambiarla por otra y
   comprobar que el presidente ve la nueva. La foto de perfil de la cuenta no cambió.
7. Con la misma cuenta, elegir a Luis: sigue en espera y no llega a ninguna ficha. (La foto de Ana
   no pasa a Luis.)
8. Quitar la foto de Ana desde "Mi ficha": vuelven las iniciales en la ficha y en la lista.
9. Como presidente, cargar una foto a Beto y retirarlo desde su categoría: la foto se borra.
   Reincorporarlo: vuelve a la 2014 con sus iniciales. En la ficha de Eva (retirada) no hay
   botones de foto.

## Bloque 3: el inicio de cada rol (historia 3)

1. Presidente A, en el inicio: jugadores 4 (Ana, Beto, Caro, Dani), equipos 3 (A, B y el de la
   2015), entrenadores 3 (E1, E2 y el directivo que entrena la 2015) y categorías 2 (2014 y 2015).
   Debajo, "Próximos entrenamientos" con
   su espacio reservado, y la tarjeta del club con su nombre, el rol y "Editar los datos del club".
2. El directivo ve el mismo resumen, aunque entrena la 2015.
3. E1: la 2014 con 2 jugadores y "Dirige: A"; enlace a la categoría, que lista a Ana y a Beto.
4. E2: el mensaje de que todavía no tiene categorías.
5. La cuenta de Ana (con Ana elegida): su foto o sus iniciales, su nombre, 2014, equipos A y B,
   "Abrir mi ficha" y la tarjeta "Mi categoría" con sus entrenadores.
6. Un club recién creado desde el panel: su presidente ve las cuatro cifras en 0, sin errores.
7. Una persona del Club A que también es integrante del Club B con otro rol: al cambiar de club en
   el desplegable cambian el inicio y el menú.

## Bloque 4: escudo y colores por el presidente (historia 4)

1. Presidente A, en "Datos del club": además del formulario de la 001, la sección de identidad,
   igual que la del panel del desarrollador.
2. Cargar el escudo válido: se ve en el menú lateral, en la barra superior y en el desplegable de
   clubes. Entrar como el directivo: ve el nuevo escudo. El panel del desarrollador también.
3. Cargar la imagen de más de 1 MB y el archivo que no es imagen: rechazados con su motivo; el
   escudo sigue.
4. Elegir un color principal muy claro (por ejemplo `#FAFAFA`): la vista previa en los dos temas y
   el aviso de contraste antes de guardar. Guardar igual: queda guardado.
5. Desde el panel del desarrollador, cambiar los colores de A: el presidente, al recargar, ve los
   del desarrollador. El Club B no cambió en ningún momento.
6. Suspender el Club A desde el panel: el presidente A sigue pudiendo cambiar el escudo y los
   colores. Levantar la suspensión al terminar.

## Comprobaciones directas contra la API

Con el token de cada persona (`Authorization: Bearer …`) y, para la cuenta de Ana,
`X-Jugador-Elegido` con el identificador de Ana:

| Petición | Quién | Esperado |
| --- | --- | --- |
| `GET /api/clubes/<A>/jugadores` | cuenta de Ana | `403 rol_no_autorizado` |
| `GET /api/clubes/<A>/jugadores` | presidente de B | `404 no_encontrado` |
| `GET /api/clubes/<A>/jugadores` | desarrollador | `404 no_encontrado` |
| `GET /api/clubes/<A>/jugadores` | presidente A | Ningún campo de documento, contacto, salud ni pago |
| `GET /api/clubes/<A>/jugadores/<Caro>/foto` | E1 | `404 no_encontrado` (no es de su categoría) |
| `GET /api/clubes/<A>/jugadores/<Caro>/ficha` | E1 | `404 no_encontrado` |
| `PUT /api/clubes/<A>/jugadores/<Ana>/foto` | directivo o E1 | `403 rol_no_autorizado` |
| `PUT /api/clubes/<A>/jugadores/<Beto>/foto` | cuenta de Ana | `404 no_encontrado` |
| `PUT /api/clubes/<A>/jugadores/<Eva>/foto` | presidente A | `409 jugador_retirado` |
| `GET /api/clubes/<A>/resumen` | E1 o cuenta de Ana | `403 rol_no_autorizado` |
| `PUT /api/clubes/<A>/colores` | directivo, E1 o cuenta de Ana | `403 rol_no_autorizado` |
| `PUT /api/clubes/<B>/colores` | presidente A | `404 no_encontrado` |
| `PUT /api/clubes/<A>/escudo` | presidente A, con la imagen de más de 1 MB | `400 escudo_demasiado_grande` |

## Batería de pruebas

```powershell
dotnet test backend/LaPecosa.sln
npm --prefix frontend test
./scripts/verificar-tamano.ps1
```

Todo en verde, incluidas las pruebas de la 001 a la 006. La prueba del contrato cuenta 68
endpoints, y la de aislamiento recorre 46 de club.

Al terminar: `docker compose -p la-pecosa-validacion down -v` y borrar el contenedor que mantenía
Docker despierto, si se creó.
