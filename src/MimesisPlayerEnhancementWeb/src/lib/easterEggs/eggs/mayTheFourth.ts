import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const mayTheFourthEgg: EasterEggDefinition = {
  id: 'mayTheFourth',
  cssClass: 'egg-may-the-fourth',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 5, 4),
  flavorKey: 'dashboard.easter_egg_mayTheFourth_flavor',
  overlay: 'stars',
};
