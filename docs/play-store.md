# LFClub en Google Play: material para Play Console

Todo lo que pide Play Console, listo para copiar. Id de la app: `com.lfclub.app`.
Versión actual del bundle: `versionCode 1`, `versionName 1.0`. SDK objetivo 36, mínimo 24.

## 1. Ficha de la tienda

**Nombre (máx. 30):** LFClub

**Descripción breve (máx. 80):**
Gestiona tu escuela o club de fútbol: equipos, entrenos, partidos y comunicados.

**Descripción completa (máx. 4000):**
LFClub es la plataforma para escuelas y clubes de fútbol. Cada club tiene su propio espacio, con su
escudo y sus colores, y sus datos están separados de los de los demás clubes.

Para el presidente y la directiva
- Invita a entrenadores, directivos y familias desde la app.
- Crea equipos, asigna entrenadores y organiza a los jugadores.
- Publica comunicados para todo el club.

Para entrenadores
- Programa entrenamientos y partidos por equipo y convoca a los jugadores.
- Registra asistencia, marcadores y goles.

Para las familias
- Consulta la agenda de entrenos y partidos de tus hijos.
- Confirma, corrige resultados y mantén al día la ficha del jugador.
- Mira la carta del jugador, al estilo de las cartas de colección, con sus estadísticas.
- Recibe avisos de las actividades y los comunicados del club.

Modo claro y oscuro, y fotos de perfil para cada persona.

**Categoría:** Deportes · **Tipo:** Aplicación · **Gratuita**
**Etiquetas sugeridas:** Fútbol, Gestión de equipos, Deportes
**Idioma predeterminado:** Español (Colombia o el que uses)

**Contacto (obligatorio):** correo de soporte público (el que quieras mostrar; no he usado el tuyo).
Sitio web y teléfono son opcionales.

## 2. Gráficos

Ya generados en `docs/play-store/`: `icono-512.png` y `grafico-1024x500.png`. Faltan las capturas.

| Recurso | Medida |
| --- | --- |
| Icono de la app | 512×512 PNG, hasta 1 MB (ya tienes el del launcher en `frontend/android/app/src/main/res/mipmap-xxxhdpi`, pero hay que exportarlo a 512) |
| Gráfico de funciones | 1024×500 PNG o JPG |
| Capturas de teléfono | mínimo 2, 16:9 o 9:16, lado entre 320 y 3840 px |

Las capturas salen de la app con datos de un club de prueba, sin datos reales de menores.

## 3. Contenido de la app

### Política de privacidad (URL obligatoria)
Play exige una URL pública. El repositorio no tiene una página de política: hay que publicarla
(por ejemplo en la misma web del frontend) con el texto de la sección 6 y pegar su dirección.

### Acceso a la app
Marca "Todas o algunas funciones están restringidas". La app exige cuenta (registro por
invitación). Crea en un club de prueba una cuenta de cada rol que el revisor pueda usar
(presidente, entrenador, familia) y pon correo y contraseña en las instrucciones de acceso. Sin
esto, Play rechaza la revisión.

### Anuncios
No contiene anuncios.

### Clasificación del contenido (cuestionario IARC)
Categoría "Utilidad / productividad" o "Referencia", todas las respuestas "No": sin violencia,
sin contenido sexual, sin lenguaje soez, sin drogas ni juego de azar. Contenido generado por
usuarios: marca que sí hay comunicados y fotos de perfil entre los miembros de un club cerrado.
Resultado esperado: apta para todos.

### Público objetivo
Elige **18 años o más** (la gestionan adultos: presidente, entrenadores y familias).
Los menores aparecen como jugadores, pero no usan la app. Al no apuntar a niños, no entra en la
política de Familias. No marques franjas de menores.

### Aplicación de noticias / COVID / gobierno / finanzas / salud
Todas "No".

### Seguridad de los datos (Data safety)
Respuestas, según los campos que guarda el backend:

| Pregunta | Respuesta |
| --- | --- |
| ¿Recopila o comparte datos? | Sí, recopila. No comparte con terceros para publicidad |
| ¿Cifrado en tránsito? | Sí (HTTPS) |
| ¿Puede el usuario pedir que se borren sus datos? | Sí, solo si implementas el borrado (ver 5) |

Tipos de datos recopilados, todos "necesarios para la función de la app", no opcionales, no para
publicidad ni analítica:

- **Información personal**: nombre, correo, identificadores de usuario, dirección, teléfono, fecha de
  nacimiento (jugadores), número de documento.
- **Fotos y vídeos**: fotos (perfil y jugador).
- **Salud**: alergias de la ficha del jugador (en "Salud y estado físico → Información de salud").
- **Información de la app**: sin datos de rendimiento ni diagnósticos.
- **Ubicación, contactos, mensajes, finanzas, historial de navegación**: no.

Compartición: el correo transaccional lo envía Brevo como encargado del tratamiento; Play lo
considera "procesamiento de servicio", no "compartir", así que se marca como **no compartido**.

## 4. Publicación de la app

1. Crea y guarda la **llave de firma** fuera del repo. El `build.gradle` ya lee
   `LFCLUB_KEYSTORE_PROPERTIES` (storeFile, storePassword, keyAlias, keyPassword):
   ```powershell
   keytool -genkeypair -v -keystore lfclub-subida.jks -alias lfclub -keyalg RSA -keysize 2048 -validity 10000
   ```
   Guarda el `.jks` y sus claves en dos sitios; si se pierde, no se puede actualizar la app.
2. Construye el bundle firmado (desde `frontend/`):
   ```powershell
   npm run build
   npx cap sync android
   $env:LFCLUB_KEYSTORE_PROPERTIES = "C:\ruta\fuera\del\repo\firma.properties"
   cd android; .\gradlew bundleRelease
   ```
   Sale en `android/app/build/outputs/bundle/release/app-release.aab`.
3. Activa **Play App Signing** al subir el primer bundle (es lo normal).
4. En cada versión nueva sube `versionCode` en `frontend/android/app/build.gradle`.
5. Las cuentas personales nuevas deben hacer una **prueba cerrada con al menos 12 testers durante
   14 días seguidos** antes de pedir producción. Si la cuenta es de organización, no aplica.
   Empieza por Pruebas → Pruebas cerradas.
6. La API de producción debe estar en HTTPS público: el APK lleva la web dentro pero llama al
   backend. Confirma qué URL usa `frontend/src` en el build de release.

## 5. Pendiente en el código antes de publicar

- **Borrado de cuenta**: Play exige que quien crea cuentas en la app ofrezca borrarla dentro de la
  app y una URL web para pedirlo. No encontré esa función en el frontend. Sin ella, la sección de
  seguridad de los datos debe decir que no se puede borrar, y la revisión puede rechazarla.
- **Página de política de privacidad** pública (texto abajo).
- `minifyEnabled false` en release: funciona, pero conviene activar R8 más adelante.

## 6. Texto de la política de privacidad (borrador)

> **LFClub — Política de privacidad.** Última actualización: [fecha].
>
> LFClub es una plataforma de gestión para escuelas y clubes de fútbol. Cada club es responsable de
> los datos que registra en su espacio; LFClub los almacena y los mantiene aislados entre clubes.
>
> **Datos que tratamos:** nombre, correo, teléfono, dirección, fecha de nacimiento y documento de los
> jugadores y sus responsables, alergias indicadas en la ficha, fotos de perfil, y los datos de
> actividad deportiva (convocatorias, asistencia, marcadores, goles).
>
> **Para qué:** operar la gestión del club, mostrar la agenda y las fichas a quienes el club autorice
> y enviar avisos y correos de invitación o recuperación de contraseña.
>
> **Con quién se comparten:** solo con el proveedor de correo (Brevo) para enviar mensajes. No
> vendemos datos ni los usamos para publicidad.
>
> **Menores:** los datos de menores los registran y mantienen sus responsables o el club; los menores
> no tienen cuenta.
>
> **Seguridad:** las comunicaciones usan HTTPS.
>
> **Tus derechos:** puedes consultar, corregir o pedir la eliminación de tus datos escribiendo a
> [correo de contacto].
