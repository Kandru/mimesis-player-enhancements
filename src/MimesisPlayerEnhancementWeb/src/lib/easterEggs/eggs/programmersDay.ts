import { isDayOfYear } from '../dates';
import type { EasterEggDefinition } from '../types';

export const programmersDayEgg: EasterEggDefinition = {
  id: 'programmersDay',
  cssClass: 'egg-programmers-day',
  priority: 10,
  isActive: (date) => isDayOfYear(date, 256),
  flavorKey: 'dashboard.easter_egg_programmersDay_flavor',
  overlay: 'dots',
};
