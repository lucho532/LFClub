# Especificación de funcionalidad: Eliminar mi cuenta

**Rama de la funcionalidad**: `014-eliminar-cuenta`

**Creada**: 2026-10-13

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: Google Play exige que una app con cuentas
permita eliminarlas desde la propia app y ofrezca una dirección web pública para pedirlo. La
constitución (§12.4) ya dice que cualquier persona puede solicitar la eliminación de su cuenta, pero
la aplicación no lo ofrece. Se pide que cualquier persona con cuenta (presidente, directivo,
entrenador o familia) pueda eliminarla desde su perfil, que se cumpla sola tras confirmar con su
contraseña, que exista una página pública `/eliminar-cuenta` y que la política de privacidad
(`/privacidad`) lo describa.

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.12.0, en especial §7.4, §8, §12.3, §12.4, §14, §14.1, §14.2,
§14.4, §16.3 y §20.

**Depende de**: spec 001 (cuentas, sesión y foto de perfil), spec 005 (ficha del jugador y sus
documentos), spec 009 y 010 (convocatorias, asistencia, goles y carta) y spec 011 y 012 (comunicados
y avisos).

## Impacto en la constitución

La §12.4 ya prevé la eliminación de la cuenta, pero deja sin decidir tres cosas que esta
especificación necesita. Se añaden a la §12.4 en la versión 4.12.0, ya aplicada:

- La eliminación incluye a todos los jugadores de la cuenta, en todos los clubes. No hay una versión
  que conserve al jugador sin cuenta; para salir de un club sin perder la cuenta existe "Retirar del
  club" (§14.1).
- Lo que la persona publicó (comunicados, resultados) se conserva, pero su nombre se sustituye por
  "Persona eliminada".
- Existe una página pública, sin sesión, que explica cómo pedir la eliminación y cómo hacerlo sin
  acceso a la aplicación.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia 1 - Eliminar mi cuenta desde la aplicación (Prioridad: P1)

Una persona con sesión abre su perfil, elige "Eliminar mi cuenta", lee qué se borra y qué se
conserva, escribe su contraseña y confirma. La cuenta desaparece en el momento, la sesión se cierra
y la persona vuelve a la pantalla de inicio de sesión.

**Por qué esta prioridad**: es lo que exige Google Play para publicar la aplicación y lo que la
constitución ya promete a cada persona.

**Prueba independiente**: con una cuenta de familia con un jugador, eliminarla y comprobar que ya no
puede iniciar sesión, que su correo y el documento del jugador quedaron libres y que el jugador ya no
aparece en ninguna lista del club.

**Escenarios de aceptación**:

1. **Dado** una persona con sesión, **cuando** abre su perfil, **entonces** ve la opción "Eliminar mi
   cuenta".
2. **Dado** que abrió la opción, **cuando** la pantalla se muestra, **entonces** le dice, antes de
   pedir nada, qué se borra, qué se conserva y que no se puede deshacer.
3. **Dado** que escribió su contraseña correcta y confirmó, **cuando** se procesa, **entonces** la
   cuenta se elimina sin aprobación de nadie, se cierra su sesión y cualquier otra sesión suya deja
   de servir.
4. **Dado** que escribió una contraseña incorrecta, **cuando** confirma, **entonces** no se elimina
   nada y se le dice que la contraseña no es correcta.
5. **Dado** una cuenta eliminada, **cuando** alguien intenta entrar con su correo o con el documento
   de uno de sus jugadores, **entonces** se le dice lo mismo que a cualquier credencial desconocida,
   sin revelar que existió.

---

### Historia 2 - Qué pasa con los jugadores de la cuenta (Prioridad: P1)

Una familia con uno o varios jugadores elimina su cuenta. Se eliminan también todos los jugadores de
esa cuenta, en todos los clubes, con sus fichas, documentos, fotos, carta y estadísticas. La pantalla
lo dice con el nombre de cada jugador antes de confirmar.

**Por qué esta prioridad**: es la decisión que más datos afecta y la que más puede sorprender; debe
quedar clara y sin vuelta atrás accidental.

**Prueba independiente**: eliminar una cuenta con dos jugadores en un club y uno en otro, y comprobar
que en ambos clubes desaparecen sus listas, convocatorias, asistencias, goles y carta, y que en la
pantalla de confirmación figuraban los tres por nombre.

**Escenarios de aceptación**:

1. **Dado** una cuenta con jugadores, **cuando** abre "Eliminar mi cuenta", **entonces** ve la lista
   de sus jugadores, con el club de cada uno, y la advertencia de que se eliminarán con ella.
2. **Dado** que la cuenta se eliminó, **cuando** el club consulta sus listas, categorías, equipos,
   calendario y goleadores, **entonces** esos jugadores no aparecen ni en las estadísticas.
3. **Dado** un partido que esos jugadores disputaron, **cuando** se consulta, **entonces** conserva el
   marcador del equipo pero sin sus convocatorias ni sus goles.
4. **Dado** que solo quiere dejar un club, **cuando** abre la pantalla, **entonces** se le ofrece
   "Retirar del club" de la ficha como alternativa que no borra la cuenta.

---

### Historia 3 - Presidente, directivo y entrenador (Prioridad: P1)

El presidente, un directivo o un entrenador eliminan su cuenta igual que una familia. Un presidente
que es el único de un club no puede: la pantalla le dice en qué club y le indica que debe nombrar
antes a otro presidente.

**Por qué esta prioridad**: la constitución exige que un club conserve siempre un presidente
activo; sin esta regla, eliminar la cuenta dejaría un club sin nadie que lo gobierne.

**Prueba independiente**: con un club de un solo presidente, intentar eliminar su cuenta y ver el
bloqueo con el nombre del club; nombrar a otro presidente e intentarlo otra vez.

**Escenarios de aceptación**:

1. **Dado** un presidente único de un club, **cuando** intenta eliminar su cuenta, **entonces** no se
   elimina nada, se le indica el club y que debe nombrar antes a otro presidente.
2. **Dado** un presidente que no es el único en ninguno de sus clubes, **cuando** confirma, **entonces**
   se elimina.
3. **Dado** un entrenador o un directivo que se elimina, **cuando** se procesa, **entonces** desaparece
   de sus asignaciones y de las listas del club y los comunicados que publicó siguen visibles con
   "Persona eliminada" como autor.
4. **Dado** la cuenta DESARROLLADOR, **cuando** se mira su perfil, **entonces** no ofrece eliminarse.

---

### Historia 4 - Pedirlo desde la web, sin la aplicación (Prioridad: P2)

Cualquiera, incluso quien no tiene la aplicación instalada o no recuerda su contraseña, abre
`/eliminar-cuenta` sin iniciar sesión y encuentra: qué se elimina, qué se conserva, los pasos para
hacerlo desde la aplicación y cómo pedirlo por correo si no puede entrar.

**Por qué esta prioridad**: Google Play pide esa dirección en la ficha de la tienda, pero no cambia
lo que la persona puede hacer; la eliminación en sí está en la Historia 1.

**Prueba independiente**: abrir `/eliminar-cuenta` sin sesión y verificar que muestra los pasos, lo
que se borra y se conserva, y el correo al que escribir.

**Escenarios de aceptación**:

1. **Dado** una persona sin sesión, **cuando** abre `/eliminar-cuenta`, **entonces** ve la página sin
   que se le pida iniciar sesión.
2. **Dado** esa página, **cuando** la lee, **entonces** encuentra los pasos dentro de la aplicación,
   qué datos se borran y cuáles se conservan y el plazo para las solicitudes por correo.
3. **Dado** una persona que olvidó su contraseña, **cuando** lee la página, **entonces** se le
   indica que puede recuperarla y eliminar la cuenta, o escribir al correo de la plataforma.
4. **Dado** la pantalla de inicio de sesión y la política de privacidad, **cuando** se miran,
   **entonces** ambas enlazan a la página.

---

### Historia 5 - La política de privacidad lo describe (Prioridad: P2)

La política `/privacidad` explica que la cuenta puede eliminarse desde la aplicación, qué se borra,
qué se conserva y enlaza a `/eliminar-cuenta`.

**Por qué esta prioridad**: debe coincidir con lo que la aplicación hace; es parte de lo que Google
Play revisa.

**Prueba independiente**: abrir `/privacidad` y comprobar que la sección de derechos coincide con la
pantalla de la Historia 1.

**Escenarios de aceptación**:

1. **Dado** `/privacidad`, **cuando** se lee, **entonces** menciona la eliminación desde la
   aplicación, lo que se conserva y enlaza a `/eliminar-cuenta`.

---

### Casos límite

- Una persona con la sesión abierta en dos dispositivos: al eliminar, la otra sesión deja de
  servir en su siguiente petición.
- Una cuenta con un jugador que está en la sala de espera: se elimina con los demás, sin que el
  presidente tenga que rechazarlo.
- Una cuenta con un jugador retirado: se elimina igual; conservaba sus datos y se borran.
- Una persona que es jugador adulto en un club y entrenador en otro con la misma cuenta: se elimina
  todo.
- Una cuenta de dos clubes de los que es presidente único en uno y no en el otro: no se elimina
  hasta resolver el club donde es única.
- Una invitación aún sin aceptar que hizo la cuenta eliminada: deja de servir.
- Una cuenta que se vuelve a registrar con el mismo correo después: es una cuenta nueva, sin nada de
  lo anterior.
- La eliminación falla a medias: no queda una cuenta parcialmente borrada; o se elimina todo o nada.
- Un comunicado de la persona eliminada que sigue vigente: lo siguen viendo las familias, con
  "Persona eliminada" como autor.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: Toda persona con sesión, salvo la cuenta DESARROLLADOR, DEBE poder eliminar su propia
  cuenta desde su perfil. Nadie puede eliminar la de otra persona por esta vía.
- **RF-002**: La eliminación se cumple sola, sin aprobación de nadie, y exige escribir la contraseña
  de la cuenta y confirmar de forma expresa.
- **RF-003**: Antes de confirmar, la pantalla DEBE decir qué se borra, qué se conserva, que no se
  puede deshacer y, si la cuenta tiene jugadores, listarlos por nombre con su club.
- **RF-004**: Eliminar la cuenta DEBE eliminar también a todos sus jugadores en todos los clubes, con
  su ficha, documentos, foto, carta, convocatorias, asistencia, respuestas y comunicados y avisos
  recibidos.
- **RF-005**: DEBEN borrarse todos los datos personales de la cuenta: correo, contraseña, celular,
  nombre del responsable, foto de perfil, nombre, documento, fecha de nacimiento, dirección y datos
  de salud.
- **RF-006**: Se DEBEN eliminar las estadísticas deportivas de la persona y de sus jugadores
  (goles, asistencias, dorsal, posición y rarezas). El marcador del equipo en cada partido se
  conserva.
- **RF-007**: Los pagos de la persona y de sus jugadores, cuando existan (§16), DEBEN conservarse
  intactos, y junto a ellos solo el nombre completo y el documento.
- **RF-008**: Lo que la persona publicó (comunicados y resultados) DEBE conservarse, con
  "Persona eliminada" en lugar de su nombre.
- **RF-009**: Un PRESIDENTE que es el único de algún club NO DEBE poder eliminar su cuenta; el sistema
  DEBE indicarle qué club lo impide. La regla se aplica en el servidor.
- **RF-010**: Al eliminarse, DEBEN dejar de servir todas las sesiones de la cuenta y quedar libres su
  correo y el documento de cada uno de sus jugadores.
- **RF-011**: La eliminación DEBE ser atómica: o se borra todo o no se borra nada.
- **RF-012**: Las invitaciones sin aceptar que creó la cuenta DEBEN dejar de servir, y su
  asignación a categorías y equipos DEBE retirarse.
- **RF-013**: Quien intente entrar con una cuenta eliminada DEBE recibir el mismo mensaje que con
  cualquier credencial desconocida.
- **RF-014**: DEBE existir `/eliminar-cuenta`, pública y sin sesión, con los pasos dentro de la
  aplicación, lo que se borra y se conserva, y cómo pedirlo por correo a quien no puede entrar.
- **RF-015**: `/privacidad` DEBE describir la eliminación y enlazar a `/eliminar-cuenta`; la
  pantalla de inicio de sesión DEBE enlazar a ambas.
- **RF-016**: La opción "Retirar del club" de la ficha DEBE seguir existiendo y la pantalla de
  eliminación DEBE ofrecerla como alternativa a quien solo quiere salir de un club.

### Entidades principales

- **Cuenta**: correo, contraseña y datos de contacto de una persona. Es lo que se elimina.
- **Integrante**: la pertenencia de la cuenta a un club con su rol; en un jugador, su identidad,
  ficha y carta. Se elimina con la cuenta.
- **Contenido publicado**: comunicados y resultados que la persona escribió; se conserva con un
  autor anónimo.
- **Pago**: lo que se cobró y se pagó. No existe aún; cuando exista, se conserva con nombre y
  documento.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Una persona llega a eliminar su cuenta en menos de 6 toques desde cualquier pantalla de
  la aplicación.
- **CE-002**: Después de eliminar, ningún dato personal de la cuenta ni de sus jugadores se puede
  consultar desde la aplicación ni desde ningún otro integrante del club.
- **CE-003**: El 100 % de los intentos de eliminar la cuenta de un presidente único son rechazados
  sin borrar nada.
- **CE-004**: La página `/eliminar-cuenta` carga sin iniciar sesión en cualquier navegador y
  contiene los pasos, lo que se borra y se conserva, y un correo de contacto.
- **CE-005**: Un revisor de Google Play puede verificar, siguiendo la página y la aplicación, que
  existe el camino de eliminación completo.

## Supuestos

- La eliminación es inmediata, no hay periodo de espera ni "deshacer".
- Los pagos aún no existen en la aplicación; la regla de conservarlos queda escrita y se
  cumplirá cuando existan.
- El plazo que se promete para las solicitudes por correo es de 15 días; la persona lo hace
  escribiendo desde el correo de su cuenta y esa solicitud la ejecuta el DESARROLLADOR.
- El correo de contacto público es el mismo de la política de privacidad.
- El marcador de un partido es un dato del equipo y no personal, por eso se conserva.
- Que una persona sea eliminada junto con su club (§7.4) reutiliza esta misma eliminación.
