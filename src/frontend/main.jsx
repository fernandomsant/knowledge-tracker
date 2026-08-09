import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import { AuthenticationGate } from './components/authentication/AuthenticationGate';
import './styles.css';

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <AuthenticationGate><App/></AuthenticationGate>
  </StrictMode>
);
