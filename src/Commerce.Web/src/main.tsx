import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import App from './App.tsx'
import { store } from './app/store'
import './styles.css'

const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Root element with id "root" was not found.')
}

createRoot(rootElement).render(
  <StrictMode>
    <Provider store={store}>
      <App />
    </Provider>
  </StrictMode>,
)
