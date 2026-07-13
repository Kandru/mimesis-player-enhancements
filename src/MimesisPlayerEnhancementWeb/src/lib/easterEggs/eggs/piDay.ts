import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const piDayEgg: EasterEggDefinition = {
  id: 'piDay',
  cssClass: 'egg-pi-day',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 3, 14),
  flavorKey: 'dashboard.easter_egg_piDay_flavor',
};
