import { registerRootComponent } from 'expo';
import App from './App';

// registerRootComponent chama AppRegistry.registerComponent('main', () => App)
// no nativo, e monta o componente no #root no web.
registerRootComponent(App);
