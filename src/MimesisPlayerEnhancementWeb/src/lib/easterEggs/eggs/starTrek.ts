import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const starTrekEgg: EasterEggDefinition = {
  id: 'starTrek',
  cssClass: 'egg-star-trek',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 3, 2),
  flavorKey: 'dashboard.easter_egg_starTrek_flavor',
  overlay: 'stars',
};
