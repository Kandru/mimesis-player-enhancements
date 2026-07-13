import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const valentinesEgg: EasterEggDefinition = {
  id: 'valentines',
  cssClass: 'egg-valentines',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 2, 14),
  flavorKey: 'dashboard.easter_egg_valentines_flavor',
  overlay: 'hearts',
};
