import { mount } from 'svelte';
import './app.css';
import { initEasterEgg } from './lib/easterEggs/apply';
import App from './App.svelte';

initEasterEgg();

const app = mount(App, { target: document.getElementById('app')! });
export default app;
