import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { debeMostrarArranque } from './arranque/debeMostrarArranque';
import { registrarInstalacionEscritorio } from './arranque/instalacionEscritorio';
import './arranque/arranque.css';
import './compartido/tema/variables.css';
import './compartido/tema/base.css';
import './compartido/tema/disposicion.css';
import './compartido/tema/componentes.css';
import './compartido/tema/personas.css';
import './compartido/tema/formularios.css';
import './compartido/tema/fichas.css';
import './compartido/tema/inicio.css';
import './compartido/tema/actividades.css';
import './compartido/tema/audiencia.css';
import './compartido/tema/imagenes.css';
import './compartido/tema/calendario.css';
import './compartido/tema/identidad.css';
import './compartido/tema/marcaPlataforma.css';
import './compartido/tema/carta.css';
import './compartido/tema/comunicados.css';
import './compartido/tema/pagos.css';
import './compartido/tema/movil.css';

registrarInstalacionEscritorio();

// Se decide una sola vez, fuera de React: la animación se ve una vez por apertura.
const conArranque = debeMostrarArranque();

createRoot(document.getElementById('raiz')!).render(
  <StrictMode>
    <App conArranque={conArranque} />
  </StrictMode>,
);
