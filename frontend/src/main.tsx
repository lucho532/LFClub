import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { debeMostrarArranque } from './arranque/debeMostrarArranque';
import './arranque/arranque.css';
import './compartido/tema/variables.css';
import './compartido/tema/base.css';
import './compartido/tema/disposicion.css';
import './compartido/tema/componentes.css';
import './compartido/tema/fichas.css';
import './compartido/tema/inicio.css';
import './compartido/tema/actividades.css';
import './compartido/tema/identidad.css';
import './compartido/tema/movil.css';

// Se decide una sola vez, fuera de React: la animación se ve una vez por apertura.
const conArranque = debeMostrarArranque();

createRoot(document.getElementById('raiz')!).render(
  <StrictMode>
    <App conArranque={conArranque} />
  </StrictMode>,
);
