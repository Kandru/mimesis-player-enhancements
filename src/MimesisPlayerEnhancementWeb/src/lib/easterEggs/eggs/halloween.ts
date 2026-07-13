import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const halloweenEgg: EasterEggDefinition = {
  id: 'halloween',
  cssClass: 'egg-halloween',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 10, 31),
  flavorKey: 'dashboard.easter_egg_halloween_flavor',
};
