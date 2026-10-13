/**
 * Lo que se borra y lo que se conserva al eliminar una cuenta (spec 014, RF-004 a RF-008). Lo usan
 * la pantalla privada y la página pública para decir exactamente lo mismo, con las mismas palabras
 * que la política de privacidad.
 */
export function QueSeBorraYQueSeConserva() {
  return (
    <section className="tarjeta columna" aria-labelledby="que-se-borra">
      <h2 id="que-se-borra">Qué se borra</h2>
      <ul>
        <li>Tu cuenta: correo, contraseña, celular y foto de perfil.</li>
        <li>Tus jugadores, en todos los clubes, con su ficha, documentos, fotos, carta y estadísticas.</li>
        <li>Tu lugar en las listas, convocatorias, asistencia y goles de esos jugadores.</li>
        <li>Tus roles y asignaciones en los clubes donde participas.</li>
        <li>Tu correo y tus documentos quedan libres para registrarse de nuevo.</li>
      </ul>
      <h2>Qué se conserva</h2>
      <ul>
        <li>
          Los comunicados y resultados que publicaste, sin tu nombre: aparecen como «Persona eliminada».
        </li>
        <li>El marcador de los partidos, sin los jugadores ni los goles individuales.</li>
        <li>
          Los pagos registrados, cuando existan, con el nombre y el documento que tenían, porque el club
          los necesita para su contabilidad.
        </li>
      </ul>
    </section>
  );
}
